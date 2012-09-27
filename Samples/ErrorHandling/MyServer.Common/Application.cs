using System;
using NServiceBus;

namespace MyServer.Common
{
    using System.Diagnostics;
    using System.IO;
    using System.Security;
    using System.Security.Permissions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    public class Startup : IWantToRunAtStartup
    {
        public IBus Bus { get; set; }        

        public void Run()
        {
            if (!File.Exists("crashed.txt") && File.Exists("log.txt"))
            {
                File.Copy("log.txt", string.Format("log{0:ddMMYYYYHHmmfff}.txt", DateTime.Now));
                File.Delete("log.txt");
            }

            while (true)            
            {
                Console.WriteLine("");

                Console.Out.Write("Sending 50...");
                Parallel.For(0, 50, i =>
                    {
                        var m = new MyMessage {Id = Guid.NewGuid()};
                        Bus.SendLocal(m);
                    });
                Console.Out.WriteLine("Done");

                if (File.Exists("crashed.txt"))
                {
                    File.Delete("crashed.txt");

                    Console.Out.WriteLine("Going to run to the end");

                    break;
                }

                Console.Out.WriteLine("Waiting for 20 seconds before sending another batch of messages");
                Thread.Sleep(TimeSpan.FromSeconds(20)); //Waiting 18 seconds before sending another batch of messages

                Console.Out.Write("Sending another 50...");
                Parallel.For(0, 50, i =>
                {
                    var m = new MyMessage { Id = Guid.NewGuid() };
                    Bus.SendLocal(m);
                });
                Console.Out.WriteLine("Done");

                Console.Out.WriteLine("Going to crash soon.");
                File.WriteAllText("crashed.txt", String.Empty);
                Thread.Sleep(5300);
                Restart();
            }
        }

        private void Restart()
        {
            var currentStartInfo = Process.GetCurrentProcess().StartInfo;
            currentStartInfo.FileName = Application.ExecutablePath;

            Console.Out.WriteLine(Application.ExecutablePath);
            Process.Start(currentStartInfo);

            Environment.FailFast("boom...");
        }

        public void Stop()
        {            
        }
    }
}
