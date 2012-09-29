namespace NServiceBus.PowershellTools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation;
    using System.Reflection;
    using System.Security.Principal;
    using Installation;
    using Installation.Environments;

    [Cmdlet(VerbsLifecycle.Install, "Infrastructure")]
    public class InstallInfrastructureCommand : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            GetInstallers<Windows>(typeof(INeedToInstallInfrastructure<>))
                .ToList()
                .ForEach(t => ((INeedToInstallInfrastructure)Activator.CreateInstance(t)).Install(WindowsIdentity.GetCurrent()));
        }

        private static IEnumerable<Type> GetInstallers<TEnvtype>(Type openGenericInstallType) where TEnvtype : IEnvironment
        {
            var listOfCompatibleTypes = new List<Type>();

            var envType = typeof(TEnvtype);
            while (envType != typeof(object))
            {
                listOfCompatibleTypes.Add(openGenericInstallType.MakeGenericType(envType));
                envType = envType.BaseType;
            }

            foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
                foreach (var i in listOfCompatibleTypes)
                    if (i.IsAssignableFrom(t))
                        yield return t;
        }
    }
}
