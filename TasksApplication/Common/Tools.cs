using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace TasksApplication.Common
{
    public class Tools
    {
        const string AGENTDEACTIVATOR = "AgentDeactivator.exe";
        const string AGENTSERVICENAME = "apdagent";

        private static ILog _log = LogManager.GetLogger(nameof(Tools));        

        public static int RunCmd(string fileName, string args="", string outputFile=null)
        {
            _log.Debug($@"About to launch command: {fileName} {args}");

            ProcessStartInfo sqlCmdStartInfo = new ProcessStartInfo(fileName, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            Process sqlcmdProc = Process.Start(sqlCmdStartInfo);

            while (!sqlcmdProc.StandardOutput.EndOfStream)
            {
                if (outputFile != null)
                {
                    File.WriteAllText(outputFile, sqlcmdProc.StandardOutput.ReadToEnd());
                }
                _log.Debug(sqlcmdProc.StandardOutput.ReadLine());
            }

            sqlcmdProc.WaitForExit();

            return sqlcmdProc.ExitCode;
        }

        public static void StopAgent()
        {
            var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // run AgentDeactivator -m 1
            int rc = Tools.RunCmd($@"{AGENTDEACTIVATOR}", "-m 1");

            if (rc != 0)
            {
                throw new Exception($"Stop Agent failed");

            }

            _log.Debug($"Stop Agent completed");
        }

        public static void StartAgent()
        {
            ServiceController agentService = new ServiceController(AGENTSERVICENAME);

            // wait for agent service to be stopped
            while (agentService.Status != ServiceControllerStatus.Stopped)
            {
                // wait 2 seconds
                System.Threading.Thread.Sleep(2000);

                // refresh service
                agentService.Refresh();
            }

            agentService.Start();

            // wait for agent service to be running
            while (agentService.Status != ServiceControllerStatus.Running)
            {
                // wait 2 seconds
                System.Threading.Thread.Sleep(2000);

                // refresh service
                agentService.Refresh();
            }
        }

        public static void DisableAgentProtection()
        {
            var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // run AgentDeactivator -m 6
            int rc = Tools.RunCmd($@"{AGENTDEACTIVATOR}", "-m 6");

            if (rc != 0)
            {
                throw new Exception($"Disable Agent Protection failed");

            }

            _log.Debug($"Disable Agent Protection completed");
        }

        public static void EnableAgentProtection()
        {
            var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // run AgentDeactivator -m 7
            int rc = Tools.RunCmd($@"{AGENTDEACTIVATOR}", "-m 7");

            if (rc != 0)
            {
                throw new Exception($"Enable Agent Protection failed");

            }

            _log.Debug($"Enable Agent Protection completed");
        }

        public static void WriteLine(string message, ConsoleColor color = ConsoleColor.White)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(GetTimeStamp());
            Console.ForegroundColor = originalColor;
            Console.Write("] ");
            Console.ForegroundColor = color;
            Console.Write(message + Environment.NewLine);
            Console.ForegroundColor = originalColor;
        }

        private static string GetTimeStamp()
        {
            return DateTime.Now.ToString("dd/MM/yy HH:mm:ss");
        }

        public static void WriteError(string header, string message = null)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("ERROR: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(header);
            if (message != null)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine(message);
            }
            Console.ForegroundColor = originalColor;
        }
    }
}
