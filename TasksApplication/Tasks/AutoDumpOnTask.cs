using TasksApplication.Common;
using TasksApplication.SysInfo;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TasksApplication.Tasks
{
    [Export(nameof(AutoDumpOnTask), typeof(ITask))]
    public class AutoDumpOnTask : ITask
    {
        const string DUMPREG = "CreateProcessDump.reg";
        const string DUMPDIR = @"C:\dumps";

        private static ILog _log = LogManager.GetLogger(nameof(AutoDumpOnTask));

        public void Execute(CompositionContainer container)
        {

            try
            {                
                Tools.WriteLine($"{nameof(AutoDumpOnTask)}: Turning ON Auto Dump");

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
                AddFilesToCollect(container);

                Tools.WriteLine($"{nameof(AutoDumpOnTask)}: Finished successfully", ConsoleColor.DarkGreen);

            }
            catch (Exception ex)
            {
                _log.Error($"TASK FAILED: {ex.ToString()}");
                Tools.WriteError($"{nameof(AutoDumpOnTask)}: TASK FAILED: {ex.Message}");
            }
        }

        public static void AddFilesToCollect(CompositionContainer container)
        {
            var host = container.GetExportedValue<ISysInfoHost>();
            Debug.Assert(host != null);            

            string[] logFiles = Directory.GetFiles(DUMPDIR);

            foreach (string s in logFiles)
            {                
                host.AddFile(s, Path.GetFileName(s));                
            }
        }
    }
}
