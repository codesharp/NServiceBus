namespace MyServer
{
    using System.Net;
    using NServiceBus;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization, IWantCustomLogging
    {
        public void Init()
        {
            WebRequest.DefaultWebProxy = new WebProxy("http://localhost:8888", false);

            SetLoggingLibrary.Log4Net(log4net.Config.XmlConfigurator.Configure);
    
            Configure.With()
                .DefaultBuilder()
                .UseRavenTimeoutPersister();
        }
    }
}
