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
    [Export(nameof(DependenciesTask), typeof(ITask))]
    public class DependenciesTask : ITask
    {
        const string DEPENDS_EXE = "Dependencies.exe";
        const string DEPENDS_DIR = "Dependencies";

        const string AGENT_EXE = "APDAgent.exe";

        const string WRITE_DLL = "WriteDllToFile.bat";

        private static ILog _log = LogManager.GetLogger(nameof(DependenciesTask));

        public void Execute(CompositionContainer container)
        {            
            try
            {
                var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);                               

                string agentExePath = Path.Combine(Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings["agentDirectory"]), AGENT_EXE);

                Tools.WriteLine($"{nameof(DependenciesTask)}: Running Dependencies check on Agent executable");
                
                // Run Dependencies on Agent exe                
                int rc = Tools.RunCmd($@"{location}\{DEPENDS_DIR}\{DEPENDS_EXE}", $"\"{agentExePath}\" -chain", $@"{location}\{AGENT_EXE}.dependes.txt");
                if (rc != 0)
                {
                    throw new Exception($"Run Dependencies on Agent exe failed");
                }

                // add output file to collect
                var host = container.GetExportedValue<ISysInfoHost>();
                Debug.Assert(host != null);
                host.AddFile($@"{location}\{AGENT_EXE}.dependes.txt", $"{AGENT_EXE}.dependes.txt", true);

                // write desired dll to file
                var dllId = ConfigurationManager.AppSettings["DependencyDllId"];
                var dllFile = $@"{location}\{dllId}.dll";
                rc = Tools.RunCmd($@"{location}\{WRITE_DLL}", $"{dllId} {dllFile}");
                if (rc != 0)
                {
                    throw new Exception($"Write desired dll to file failed");
                }

                // check if dll was created
                if (!File.Exists($"{dllFile}"))
                {
                    Tools.WriteLine($"{nameof(DependenciesTask)}: Didn't find sensor {dllId}. Skipping its check");
                }
                else
                {
                    Tools.WriteLine($"{nameof(DependenciesTask)}: Running Dependencies check on {dllFile}");

                    // Run Dependencies on dll file
                    rc = Tools.RunCmd($@"{location}\{DEPENDS_DIR}\{DEPENDS_EXE}", $@"{dllFile} -chain",$@"{location}\{Path.GetFileName(dllFile)}.dependes.txt");
                    if (rc != 0)
                    {
                        throw new Exception($"Run Dependencies on Agent exe failed");
                    }

                    // add output file and dll to collected files
                    host.AddFile($@"{location}\{Path.GetFileName(dllFile)}.dependes.txt", $"{Path.GetFileName(dllFile)}.dependes.txt", true);
                    host.AddFile($"{dllFile}", $"{Path.GetFileName(dllFile)}", true);
                }

                Tools.WriteLine($"{nameof(DependenciesTask)}: Finished successfully", ConsoleColor.DarkGreen);
            }
            catch (Exception ex)
            {
                _log.Error($"FAILED: {ex.ToString()}");
                Tools.WriteError($"{nameof(DependenciesTask)}: TASK FAILED: {ex.Message}");
            }
        }
    }
}
