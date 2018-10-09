using TasksApplication.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TasksApplication.Tasks
{
    [Export(nameof(AgentAutoDumpOnTask), typeof(ITask))]
    public class AgentAutoDumpOnTask : ITask
    {
        const string DUMPREG = "CreateProcessDump.reg";
        const string DUMPDIR = @"C:\dumps";

        private static ILog _log = LogManager.GetLogger(nameof(AgentAutoDumpOnTask));

        public void Execute(CompositionContainer container)
        {
            try
            {
                Tools.WriteLine($"{nameof(AgentAutoDumpOnTask)}: Turning ON Agent Auto Dump");

                // disable agent protection
                Tools.DisableAgentProtection();

                // create dumps directory                 
                if (!Directory.Exists(DUMPDIR))
                {
                    // Try to create the directory.
                    _log.Debug($"Creating dumps directory [{DUMPDIR}]");
                    DirectoryInfo di = Directory.CreateDirectory(DUMPDIR);
                }

                var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                // run set registry command              
                int rc = Tools.RunCmd("regedit.exe", $@"/s {location}\{DUMPREG}");

                // add dumps to collected files
                AutoDumpOnTask.AddFilesToCollect(container);

                // enable agent protection
                Tools.EnableAgentProtection();

                Tools.WriteLine($"{nameof(AgentAutoDumpOnTask)}: Finished successfully", ConsoleColor.DarkGreen);

            }
            catch (Exception ex)
            {
                _log.Error($"TASK FAILED: {ex.ToString()}");
                Tools.WriteError($"{nameof(AgentAutoDumpOnTask)}: TASK FAILED: {ex.Message}");
            }
        }        
    }
}
