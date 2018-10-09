using TasksApplication.Common;
using log4net;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TasksApplication.Tasks
{
    [Export(nameof(ConfigureFullMemDumpTask), typeof(ITask))]
    public class ConfigureFullMemDumpTask : ITask
    {
        private static ILog _log = LogManager.GetLogger(nameof(LogToDebugTask));

        public void Execute(CompositionContainer container)
        {            
            
            try
            {

                Tools.WriteLine($"{nameof(ConfigureFullMemDumpTask)}: Configuring Registry Keys for full memory dump");

                var crashControlKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Control\CrashControl", true);

                // set values for Registry key
                crashControlKey.SetValue("CrashDumpEnabled", 0x1, RegistryValueKind.DWord);
                crashControlKey.SetValue("Enabled", 0x1, RegistryValueKind.DWord);

                Tools.WriteLine($"{nameof(ConfigureFullMemDumpTask)}: Finished successfully (Machine Restart must be done!)", ConsoleColor.DarkGreen);

            }
            catch (Exception ex)
            {
                _log.Error($"TASK FAILED: {ex.ToString()}");
                Tools.WriteError($"{nameof(ConfigureFullMemDumpTask)}: TASK FAILED: {ex.Message}");
            }
        }
    }
}
