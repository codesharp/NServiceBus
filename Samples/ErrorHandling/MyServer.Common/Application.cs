using System;
using NServiceBus;

namespace MyServer.Common
{
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;

    public class Startup : IWantToRunAtStartup
    {
        public IBus Bus { get; set; }        

        public void Run()
        {
            while (true)
            {
                Console.ReadKey();

                Console.Out.Write("Sending...");
                Parallel.For(0, 100, i =>
                {
                    var m = new MyMessage { Id = Guid.NewGuid() };
                    Bus.SendLocal(m);
                });
                Console.Out.WriteLine("Done");
            }
        }

        public void Stop()
        {            
        }
    }
}
