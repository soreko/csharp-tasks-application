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
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace TasksApplication.Tasks
{
    [Export(nameof(LogToDebugTask), typeof(ITask))]
    public class LogToDebugTask : ITask
    {
        const string CHANGELOGBAT = "ChangeLogLevel.bat";        

        private static ILog _log = LogManager.GetLogger(nameof(LogToDebugTask));

        public void Execute(CompositionContainer container)
        {            
            var minutesToDebug = int.Parse(ConfigurationManager.AppSettings["MinutesToDebug"]);            

            try
            {
                // change log level to DEBUG
                Tools.WriteLine($"{nameof(LogToDebugTask)}: Changing log level to DEBUG");
                ChangeLogLevel("DEBUG");

                // stop agent
                Tools.WriteLine($"{nameof(LogToDebugTask)}: Stopping and Starting agent...");
                Tools.StopAgent();

                // start agent                
                Tools.StartAgent();

                // wait some time
                Tools.WriteLine($"{nameof(LogToDebugTask)}: Waiting for {minutesToDebug} minutes");
                System.Threading.Thread.Sleep(1000 * 60 * minutesToDebug);

                // change log level back to INFO   
                Tools.WriteLine($"{nameof(LogToDebugTask)}: Reverting log level back to INFO");
                ChangeLogLevel("INFO");

                // stop agent
                Tools.WriteLine($"{nameof(LogToDebugTask)}: Stopping and Starting agent...");
                Tools.StopAgent();

                // start agent                
                Tools.StartAgent();

                // add agent logs to collected files
                AddFilesToCollect(container);

                Tools.WriteLine($"{nameof(LogToDebugTask)}: Finished successfully", ConsoleColor.DarkGreen);

            }
            catch (Exception ex)
            {
                _log.Error($"FAILED: {ex.ToString()}");
                Tools.WriteError($"{nameof(LogToDebugTask)}: TASK FAILED: {ex.Message}");
            }                                                         
        }

        private void AddFilesToCollect(CompositionContainer container)
        {
            var host = container.GetExportedValue<ISysInfoHost>();
            Debug.Assert(host != null);

            var logsDir = Path.Combine(Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings["agentDirectory"]), "logs");

            string[] logFiles = Directory.GetFiles(logsDir);

            foreach (string s in logFiles)
            {
                if (Path.GetFileName(s).StartsWith("log"))
                {
                    host.AddFile(s, Path.GetFileName(s));
                }
            }
        }        

        private void ChangeLogLevel(string level)
        {
            var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // run change log level command
            int rc = Tools.RunCmd($@"{location}\{CHANGELOGBAT}", level);
            
            if (rc != 0)
            {
                throw new Exception($"Change log level failed");                
            }

            _log.Debug($"Changed log level to {level}");
        }                
    }
}
