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
    [Export(nameof(ProcmonTask), typeof(ITask))]
    public class ProcmonTask : ITask
    {
        const string PROCMON = "Procmon.exe";
        const string OUTPUT = "ProcmonOutput.PML";

        private static ILog _log = LogManager.GetLogger(nameof(ProcmonTask));

        public void Execute(CompositionContainer container)
        {
            var minutesToProcmon = int.Parse(ConfigurationManager.AppSettings["MinutesToProcmon"]);

            try
            {               
                var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                // Start procmon on silent mode
                Tools.WriteLine($"{nameof(ProcmonTask)}: Starting Procmon");                
                var task = Task.Run(() => Tools.RunCmd($@"{location}\{PROCMON}", $@"/Quiet /AcceptEula /Minimized /BackingFile {location}\{OUTPUT}"));

                // stop agent
                Tools.WriteLine($"{nameof(ProcmonTask)}: Stopping and Starting agent...");
                Tools.StopAgent();

                // start agent                
                Tools.StartAgent();

                // wait some time
                Tools.WriteLine($"{nameof(ProcmonTask)}: Waiting for {minutesToProcmon} minutes (until {DateTime.Now.AddMinutes(minutesToProcmon).ToString("dd-MM-yyyy  HH:mm:ss")})");
                System.Threading.Thread.Sleep(1000 * 60 * minutesToProcmon);

                // Stop Procmon                
                if(Tools.RunCmd($@"{location}\{PROCMON}", "/Terminate") != 0)
                {
                    throw new Exception($"Stop procmon failed");
                }

                // add agent logs to collected files
                var host = container.GetExportedValue<ISysInfoHost>();
                Debug.Assert(host != null);
                host.AddFile($@"{location}\{OUTPUT}", $"{OUTPUT}", true);

                Tools.WriteLine($"{nameof(ProcmonTask)}: Finished successfully", ConsoleColor.DarkGreen);

            }
            catch (Exception ex)
            {
                _log.Error($"FAILED: {ex.ToString()}");
                Tools.WriteError($"{nameof(ProcmonTask)}: TASK FAILED: {ex.Message}");
            }            
        }        
    }
}
