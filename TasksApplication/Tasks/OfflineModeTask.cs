using TasksApplication.Common;
using TasksApplication.SysInfo;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TasksApplication.Tasks
{
    [Export(nameof(OfflineModeTask), typeof(ITask))]
    public class OfflineModeTask : ITask
    {
        const string ACSMODE = "ACSMode.bat";
        const string ACS_MODE_OFFLINE = "1";
        const string ACS_MODE_ONLINE = "2";
        const string ACS_OUTPUT = "acsOutput.txt";

        private static ILog _log = LogManager.GetLogger(nameof(OfflineModeTask));

        public void Execute(CompositionContainer container)
        {
            var minutesToWait = int.Parse(ConfigurationManager.AppSettings["OfflineModeMinutes"]);

            try
            {
                var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                Tools.WriteLine($"{nameof(OfflineModeTask)}: Changing ACS Mode to offline");

                // Set ACS Mode to 1      
                int rc = Tools.RunCmd($@"{location}\{ACSMODE}", ACS_MODE_OFFLINE);
                if (rc != 0)
                {
                    throw new Exception($"Set ACS Mode to {ACS_MODE_OFFLINE} failed");
                }

                // stop agent
                Tools.WriteLine($"{nameof(OfflineModeTask)}: Stopping and Starting agent...");
                Tools.StopAgent();

                // start agent                
                Tools.StartAgent();

                // wait some time
                Tools.WriteLine($"{nameof(OfflineModeTask)}: Waiting for {minutesToWait} minutes...");
                System.Threading.Thread.Sleep(1000 * 60 * minutesToWait);

                Tools.WriteLine($"{nameof(OfflineModeTask)}: Reverting to old ACS Mode");

                // Set ACS Mode back to 2      
                rc = Tools.RunCmd($@"{location}\{ACSMODE}", ACS_MODE_ONLINE);
                if (rc != 0)
                {
                    throw new Exception($"Set ACS Mode to {ACS_MODE_ONLINE} failed");
                }

                // stop agent                
                Tools.StopAgent();

                // start agent                
                Tools.StartAgent();

                // add output file to collect
                var host = container.GetExportedValue<ISysInfoHost>();
                Debug.Assert(host != null);
                var outputFile = Path.Combine(Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings["agentDirectory"]), ACS_OUTPUT);
                host.AddFile($"{outputFile}", $"{Path.GetFileName(outputFile)}");

                Tools.WriteLine($"{nameof(OfflineModeTask)}: Finished successfully", ConsoleColor.DarkGreen);
            }
            catch (Exception ex)
            {
                _log.Error($"FAILED: {ex.ToString()}");
                Tools.WriteError($"{nameof(OfflineModeTask)}: TASK FAILED: {ex.Message}");
            }
        }
    }
}
