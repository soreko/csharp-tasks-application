using TasksApplication.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TasksApplication.Common
{
    public class ParameterToTask
    {
        // dictionary for translating user input to task name
        public static Dictionary<string, string> _dict = new Dictionary<string, string>
        {
            {"1", nameof(CurrentDiagnosticsTask)},
            {"2", nameof(LogToDebugTask)},
            {"3", nameof(TskMgrDataTask)},
            {"4", nameof(ConfigureFullMemDumpTask)},
            {"5", nameof(AutoDumpOnTask)},
            {"6", nameof(AgentAutoDumpOnTask)},
            {"7", nameof(ProcmonTask)},
            {"8", nameof(DumpProcessTask)},
            {"9", nameof(DependenciesTask)},
            {"10", nameof(OfflineModeTask)}
        };

        public static string Print()
        {
            string result = "";

            foreach (KeyValuePair<string, string> kvp in _dict)
            {
                //textBox3.Text += ("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                result += $"    {kvp.Key}   -   {kvp.Value}\n";
            }

            return result;
        }
    }
}
