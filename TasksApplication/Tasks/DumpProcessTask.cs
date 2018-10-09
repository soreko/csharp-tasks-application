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
    [Export(nameof(DumpProcessTask), typeof(ITask))]
    public class DumpProcessTask : ITask
    {
        const string PROCDUMP = "procdump.exe";
        const string OUTPUT_EXT = "dmp";
        const string AGENT_PROCESS_NAME = "apdagent";

        private static ILog _log = LogManager.GetLogger(nameof(DumpProcessTask));

        public void Execute(CompositionContainer container)
        {            
            try
            {
                var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                // calculate number of dumps
                int numOfDumps = int.Parse(ConfigurationManager.AppSettings["DumpProcessMinutesTotal"]) / int.Parse(ConfigurationManager.AppSettings["DumpProcessMinutesInterval"]);

                // retrieve process
                var process = GetProcess();

                // disable agent protection if needed
                if (process.ProcessName.ToLower().Contains(AGENT_PROCESS_NAME))
                    Tools.DisableAgentProtection();

                Tools.WriteLine($"{nameof(DumpProcessTask)}: Collecting {numOfDumps} process dumps of [{process.ProcessName} - {process.Id}] until {DateTime.Now.AddMinutes(int.Parse(ConfigurationManager.AppSettings["DumpProcessMinutesTotal"])).ToString("dd-MM-yyyy  HH:mm:ss")}");

                // run process dump
                int rc = Tools.RunCmd($@"{location}\{PROCDUMP}", $@"-accepteula -s {int.Parse(ConfigurationManager.AppSettings["DumpProcessMinutesInterval"]) * 60} -n {numOfDumps} -ma {process.Id} {location}\{process.ProcessName}.{OUTPUT_EXT}");

                if (rc != numOfDumps)
                {
                    throw new Exception($"Run process dump failed {rc}");
                }

                // add dump files to collected files
                AddFilesToCollect(container);

                // enable agent protection if needed
                if (process.ProcessName.ToLower().Contains(AGENT_PROCESS_NAME))
                    Tools.EnableAgentProtection();

                Tools.WriteLine($"{nameof(DumpProcessTask)}: Finished successfully", ConsoleColor.DarkGreen);
            }
            catch (Exception ex)
            {
                _log.Error($"FAILED: {ex.ToString()}");
                Tools.WriteError($"{nameof(DumpProcessTask)}: TASK FAILED: {ex.Message}");
            }
        }

        private Process GetProcess()
        {
            Process process = null;

            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["DumpProcessPID"]))
            {
                // retrieve process by its PID
                process = Process.GetProcessById(int.Parse(ConfigurationManager.AppSettings["DumpProcessPID"]));
            }               
            else
            {
                // retrieve process by its name
                process = Process.GetProcessesByName(ConfigurationManager.AppSettings["DumpProcessName"]).FirstOrDefault();
            }
            

            if (process == null)
                throw new Exception($"Process [{ConfigurationManager.AppSettings["DumpProcessName"]}] not found");

            return process;
        }

        private void AddFilesToCollect(CompositionContainer container)
        {
            var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var host = container.GetExportedValue<ISysInfoHost>();
            Debug.Assert(host != null);            

            string[] logFiles = Directory.GetFiles(location);

            foreach (string s in logFiles)
            {
                if (Path.GetFileName(s).EndsWith(OUTPUT_EXT))
                {
                    host.AddFile(s, Path.GetFileName(s));
                }
            }
        }
    }
}
