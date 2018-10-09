using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TasksApplication.SysInfo;
using System.IO.Compression;
using System.Configuration;
using TasksApplication.Common;

namespace TasksApplication.Tasks
{
    [Export(nameof(CurrentDiagnosticsTask), typeof(ITask))]
    public class CurrentDiagnosticsTask : ITask
    {        
        private static ILog _log = LogManager.GetLogger(nameof(CurrentDiagnosticsTask));
                
        public void Execute(CompositionContainer container)
        {
            try
            {                
                var providers = container.GetExportedValues<IInfoProvider>();

                var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                var root = XElement.Load(location + @"\\Config.xml");

                var host = container.GetExportedValue<ISysInfoHost>();
                Debug.Assert(host != null);

                Tools.WriteLine($"{nameof(CurrentDiagnosticsTask)}: Collecting Diagnostics");

                // Run all Info providers
                var results = RunProviders(root, providers);
                var result = new XElement($"SystemInfo",
                    new XAttribute("Date", DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss")), 
                    results);                               

                // save XML to file
                string resultFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    $"{ConfigurationManager.AppSettings["ResultDir"]}", 
                    "Diagnostics.xml");

                result.Save(resultFile);
                
                // add XML result file to collected files
                host.AddFile(resultFile, Path.GetFileName(resultFile), true);                
            }
            catch (Exception ex)
            {
                _log.Error($"TASK FAILED: {ex.ToString()}.");
                Tools.WriteError($"{nameof(CurrentDiagnosticsTask)}: TASK FAILED: {ex.Message}");
            }

            Tools.WriteLine($"{nameof(CurrentDiagnosticsTask)}: Finished successfully.", ConsoleColor.DarkGreen);

        }

        private static IEnumerable<XElement> RunProviders(XElement root, IEnumerable<IInfoProvider> providers)
        {
            foreach (var provider in providers)
            {
                var infoAttribute = provider.GetType().GetCustomAttribute<InfoProviderAttribute>();
                if (infoAttribute == null)
                {
                    _log.Warn($"Error: Provider {provider.GetType().Name} with no InfoProvider attribute");
                    continue;
                }
                var rootName = infoAttribute.RootName;
                foreach (var providerRoot in root.Elements(rootName))
                {

                    var result = provider.ProvideInfo(providerRoot);
                    if (result != null)
                        yield return new XElement(rootName, result);
                }
            }
        }
    }
}
