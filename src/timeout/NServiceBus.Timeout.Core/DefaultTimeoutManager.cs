namespace NServiceBus.Timeout.Core
{
    using System;
    using Unicast.Queuing;
    using log4net;

    public class DefaultTimeoutManager : IManageTimeouts
    {
        public IPersistTimeouts TimeoutsPersister { get; set; }

        public ISendMessages MessageSender { get; set; }

        public event EventHandler<TimeoutData> TimeoutPushed;

        public void PushTimeout(TimeoutData timeout)
        {
            if (timeout.Time.AddSeconds(-1) <= DateTime.UtcNow)
            {
                var message = timeout.ToTransportMessage();

                MessageSender.Send(message, timeout.Destination);
                Logger.DebugFormat("TimeoutId={0}, MessageId={1}", timeout.Id, message.Id);
                return;
            }

            TimeoutsPersister.Add(timeout);

            if (TimeoutPushed != null)
            {
                TimeoutPushed.BeginInvoke(this, timeout, ar => {}, null);
            }
        }

        public void RemoveTimeout(string timeoutId)
        {
            TimeoutData timeoutData;

            TimeoutsPersister.TryRemove(timeoutId, out timeoutData);
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
            TimeoutsPersister.RemoveTimeoutBy(sagaId);
        }

        static readonly ILog Logger = LogManager.GetLogger("DebuggingTimeouts");
    }
}
