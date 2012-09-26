namespace NServiceBus.Timeout.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using NUnit.Framework;
    using Raven.Client.Document;



    public class DummyEnlistmentNotification : IEnlistmentNotification
    {
        public static readonly Guid Id = Guid.NewGuid();

        static Random rand = new Random();

        public bool WasCommitted { get; set; }

        public bool RandomRollback { get; set; }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {


            if (rand.Next(0, 10) > 7)
                preparingEnlistment.ForceRollback();
            else
                preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            WasCommitted = true;
            enlistment.Done();

        }

        public void Rollback(Enlistment enlistment)
        {
            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }
    }

    [TestFixture]
    public class MyTests
    {
        DocumentStore store;
        Guid ResourceManagerId = new Guid("05216603-dd72-4ec5-88b6-0c88e7b74e05");

        [SetUp]
        public void Setup()
        {
            //store = new EmbeddableDocumentStore { RunInMemory = true };
            store = new DocumentStore { Url = "http://localhost:8080", DefaultDatabase = "MyTest" };
            store.ResourceManagerId = new Guid("5402132f-32b5-423e-8b3c-b6e27c5e00fa");
            store.Initialize();
        }

        [TearDown]
        public void Cleanup()
        {
            store.Dispose();
        }


        void ForceDistributedTransaction(bool randomRollback = false)
        {
            Transaction.Current.EnlistDurable(ResourceManagerId, new DummyEnlistmentNotification { RandomRollback = randomRollback },
                                              EnlistmentOptions.None);
        }

        [Test,Explicit]
        public void MultipleDeletes()
        {
            string id;


            using (var tx = new TransactionScope(TransactionScopeOption.Required, TimeSpan.FromSeconds(5)))
            {
                ForceDistributedTransaction();

                using (var session = store.OpenSession())
                {
                    var t1 = new MyData { Dispatched = false };
                    session.Store(t1);
                    session.SaveChanges();
                    tx.Complete();

                    id = t1.Id;
                }
            }


            Parallel.For(0, 10, i =>
                                    {
                                        try
                                        {
                                            using (
                                                var tx = new TransactionScope(TransactionScopeOption.Required,
                                                                              TimeSpan.FromSeconds(10)))
                                            {
                                                ForceDistributedTransaction();

                                                using (var session = store.OpenSession())
                                                {
                                                    session.Advanced.UseOptimisticConcurrency = true;
                                                    session.Advanced.AllowNonAuthoritativeInformation = false;
                                                    var myData = session.Load<MyData>(id);
                                                    Thread.Sleep(1000);

                                                    if (myData.Dispatched)
                                                        throw new Exception("Already dispatched (not the way we'll do in production code)");

                                                    myData.Dispatched = true;
                                                    session.SaveChanges();

                                                    session.Delete(myData);
                                                    session.SaveChanges();
                                                    tx.Complete();
                                                }
                                            }

                                            Console.Out.WriteLine("Success!");

                                        }
                                        catch (Exception ex)
                                        {
                                            Console.Out.WriteLine("Failed!");
                                        }

                                    });
        }


        [Test,Explicit]
        public void MultipleDeletesWithRollbacks()
        {
            var queue = new ConcurrentQueue<string>();
            int numDocuments = 1000;

            for (int i = 0; i < numDocuments; i++)
            {

                using (var tx = new TransactionScope(TransactionScopeOption.Required, TimeSpan.FromSeconds(5)))
                {
                    using (var session = store.OpenSession())
                    {
                        var t1 = new MyData
                            {
                                Dispatched = false
                            };
                        session.Store(t1);
                        session.SaveChanges();
                        tx.Complete();

                        queue.Enqueue(t1.Id);
                    }
                }


            }

            int success = 0;

            Parallel.For(0, 100, i =>
                {
                    string id = null;
                    while (queue.TryDequeue(out id))
                    {


                        try
                        {
                            using (
                                var tx = new TransactionScope(TransactionScopeOption.Required,
                                                              TimeSpan.FromSeconds(10)))
                            {
                                ForceDistributedTransaction(true);

                                using (var session = store.OpenSession())
                                {
                                    session.Advanced.UseOptimisticConcurrency = true;
                                    session.Advanced.AllowNonAuthoritativeInformation = false;
                                    var myData = session.Load<MyData>(id);
                                  

                                    if (myData.Dispatched)
                                        throw new Exception("Already dispatched (not the way we'll do in production code)");

                                    myData.Dispatched = true;
                                    session.SaveChanges();

                                    session.Delete(myData);
                                    session.SaveChanges();
                                    tx.Complete();
                                }

                            }
                            Interlocked.Increment(ref success);
                        }
                        catch (Exception ex)
                        {
                            queue.Enqueue(id);
                            Console.Out.WriteLine("Rollback!");
                        }
                    }

                });

            Assert.AreEqual(numDocuments, success);
        }
        public class MyData
        {
            public string Id { get; set; }

            public bool Dispatched { get; set; }
        }
    }
}