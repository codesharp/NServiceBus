using NServiceBus;

namespace MyServer
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization {
        public void Init()
        {
            Configure.With()
                .DefaultBuilder()
                //.UseNHibernateTimeoutPersister();
                .UseRavenTimeoutPersister();
        }
    }
}
