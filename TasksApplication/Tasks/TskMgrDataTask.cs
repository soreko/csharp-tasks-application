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
using System.Xml.Linq;

namespace TasksApplication.Tasks
{
    [Export(nameof(TskMgrDataTask), typeof(ITask))]
    public class TskMgrDataTask : ITask
    {
        private static ILog _log = LogManager.GetLogger(nameof(LogToDebugTask));        

        public void Execute(CompositionContainer container)
        {
            try
            {
                // get System info provider
                var systemProvider = container.GetExportedValues<IInfoProvider>()
                    .Where(x => x.GetType().GetCustomAttribute<InfoProviderAttribute>().RootName == "System")
                    .FirstOrDefault();

                // set config XML element
                var config = new XElement("System",
                    new XAttribute("Processes", "True"),
                    new XAttribute("TaskManagerData", "True"),
                    new XAttribute("Services", "False"),
                    new XAttribute("Drivers", "False"),
                    new XAttribute("SystemEnvironmentVariables", "False"),
                    new XAttribute("UserEnvironmentVariables", "False"));

                Tools.WriteLine($"{nameof(TskMgrDataTask)}: Collecting Task Manager data");

                // Run provider
                var results = RunProvider(config, systemProvider);
                var result = new XElement("TskMgrData", results);

                // save XML to file
                string resultFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    $"{ConfigurationManager.AppSettings["ResultDir"]}",
                    "TskMgrData.xml");

                result.Save(resultFile);

                // add XML result file to collected files
                var host = container.GetExportedValue<ISysInfoHost>();
                Debug.Assert(host != null);
                host.AddFile(resultFile, Path.GetFileName(resultFile), true);

                Tools.WriteLine($"{nameof(TskMgrDataTask)}: Finished successfully", ConsoleColor.DarkGreen);
            }
            catch (Exception ex)
            {
                _log.Error($"FAILED: {ex.ToString()}");
                Tools.WriteError($"{nameof(TskMgrDataTask)}: TASK FAILED: {ex.Message}");
            }

        }

        private static List<XElement> RunProvider(XElement config, IInfoProvider provider)
        {
            List<XElement> result = new List<XElement>();
            DateTime endTime = DateTime.Now.AddMinutes(double.Parse(ConfigurationManager.AppSettings["TskMgrMinutesTotal"]));
                        
            while (DateTime.Now < endTime)
            {
                Tools.WriteLine($"{nameof(TskMgrDataTask)}: Collecting info (until {endTime.ToString("dd-MM-yyyy  HH:mm:ss")})...");
                var info = provider.ProvideInfo(config);
                if (info != null)
                {
                    result.Add(new XElement(provider.GetType().Name, 
                        new XAttribute("Date", DateTime.Now.ToString("dd-MM-yyyy  HH:mm:ss")),
                        info));
                }

                if(DateTime.Now < endTime)
                {
                    Tools.WriteLine($"{nameof(TskMgrDataTask)}: Info collected. Going to sleep for {int.Parse(ConfigurationManager.AppSettings["TskMgrMinutesInterval"])} minutes");
                    System.Threading.Thread.Sleep(1000 * 60 * int.Parse(ConfigurationManager.AppSettings["TskMgrMinutesInterval"]));
                }
                
            }

            return result;            
        }
    }
}
