namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using Core;
    using Faults;
    using ObjectBuilder;
    using Unicast;
    using Unicast.Queuing;
    using Unicast.Queuing.Msmq;
    using Unicast.Transport;
    using Unicast.Transport.Transactional;
    using log4net;

    public class TimeoutDispatcherProcessor : IWantToRunWhenTheBusStarts, IDisposable
    {
        static readonly ILog Logger = LogManager.GetLogger("DebuggingTimeouts");

        TimeoutPersisterReceiver timeoutPersisterReceiver;
        ITransport inputTransport;

        public static readonly Address TimeoutDispatcherAddress;

        public ISendMessages MessageSender { get; set; }

        public IPersistTimeouts TimeoutsPersister { get; set; }

        public TransactionalTransport MainTransport { get; set; }

        public IBuilder Builder { get; set; }

        static TimeoutDispatcherProcessor()
        {
            TimeoutDispatcherAddress = Address.Parse(Configure.EndpointName).SubScope("TimeoutsDispatcher");
        }

        public void Run()
        {
            if (!Configure.Instance.IsTimeoutManagerEnabled())
            {
                return;
            }

            timeoutPersisterReceiver = new TimeoutPersisterReceiver(Builder.Build<IManageTimeouts>());
            timeoutPersisterReceiver.MessageSender = MessageSender;
            timeoutPersisterReceiver.TimeoutsPersister = TimeoutsPersister;
            timeoutPersisterReceiver.Start();

            inputTransport = new TransactionalTransport
                {
                    MessageReceiver = TimeoutMessageProcessor.MessageReceiverFactory != null ? TimeoutMessageProcessor.MessageReceiverFactory() : new MsmqMessageReceiver(),
                    IsTransactional = true,
                    NumberOfWorkerThreads = MainTransport.NumberOfWorkerThreads == 0 ? 1 : MainTransport.NumberOfWorkerThreads,
                    MaxRetries = MainTransport.MaxRetries,
                    FailureManager = Builder.Build(MainTransport.FailureManager.GetType()) as IManageMessageFailures //Need to change this!
                };

            inputTransport.TransportMessageReceived += OnTransportMessageReceived;

            inputTransport.Start(TimeoutDispatcherAddress);
        }

        private void OnTransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            var transportMessage = e.Message;
            var timeoutId = transportMessage.Headers["Timeout.Id"];
            TimeoutData timeoutData;

            if(TimeoutsPersister.TryRemove(timeoutId, out timeoutData))
            {
                var message = timeoutData.ToTransportMessage();
                MessageSender.Send(message, timeoutData.Destination);

                Logger.DebugFormat("TimeoutId={0}, MessageId={1}, DispatchMessageId={2}", timeoutId, message.Id, transportMessage.Id);
            }
        }

        public void Dispose()
        {
            timeoutPersisterReceiver.Stop();

            if (inputTransport != null)
            {
                inputTransport.Dispose();
            }
        }
    }
}