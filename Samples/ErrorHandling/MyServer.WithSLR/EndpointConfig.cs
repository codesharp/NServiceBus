using NServiceBus;

namespace MyServer
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization, IWantCustomLogging
    {
        public void Init()
        {
            SetLoggingLibrary.Log4Net(log4net.Config.XmlConfigurator.Configure);
    
            Configure.With()
                .DefaultBuilder()
                //.UseNHibernateTimeoutPersister();
                .UseRavenTimeoutPersister();
        }
    }
}
