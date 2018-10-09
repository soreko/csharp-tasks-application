using TasksApplication.Common;
using TasksApplication.SysInfo;
using TasksApplication.Tasks;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TasksApplication
{
    class Program
    {
        private static CompositionContainer _container;
        private static readonly ILog _log = LogManager.GetLogger(nameof(TasksApplication));        

        static void Main(string[] args)
        {
            // configure log
            log4net.Config.XmlConfigurator.Configure();            

            // create Result directory 
            string resultDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $@"\{ConfigurationManager.AppSettings["ResultDir"]}";
            if (!Directory.Exists(resultDir))
            {
                // try to create the directory.
                DirectoryInfo di = Directory.CreateDirectory(resultDir);                                
            }

            // initialize container
            _container = InitContainer();

            // no need to load PE for those commands
            if ((args.Length == 0) || args.Contains("-h") || args.Contains("-help"))
            {
                DumpUsage();
                return;
            }

            foreach (string arg in args)
            {
                if (ParameterToTask._dict.ContainsKey(arg))
                {
                    // extract task and execute
                    var value = ParameterToTask._dict[arg];
                    var task = _container.GetExportedValue<ITask>(value);
                    task.Execute(_container);
                }
                else
                {
                    Tools.WriteLine($"Unknown argument [{arg}]. Skipping", ConsoleColor.Yellow);
                }                
            }                        
            try
            {
                // collect all results
                Tools.WriteLine("Collecting results");
                BuildResultZipFile(_container);

                Tools.WriteLine($@"Done. Results in {Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\{ConfigurationManager.AppSettings["ResultDir"]}",
                    ConsoleColor.DarkGreen);
            }
            catch (Exception ex)
            {
                Tools.WriteError($"Failed to collect results: {ex.Message}");
                _log.Error($"Failed to collect results: {ex.ToString()}");
            }                        
        }

        private static void BuildResultZipFile(CompositionContainer container)
        {
            var host = container.GetExportedValue<ISysInfoHost>();
            Debug.Assert(host != null);

            string resultFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                $"{ConfigurationManager.AppSettings["ResultDir"]}",
                $"Result-{DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss")}.zip");            

            using (var fs = File.Create(resultFile))
            {
                using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
                {                    
                    foreach (var file in host.Files)
                    {
                        FileStream fileStream = null;
                        ZipArchiveEntry zipEntry;
                        Stream zipStream = null;

                        try
                        {
                            fileStream = File.Open(file.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            zipEntry = zip.CreateEntry(file.Name);
                            zipStream = zipEntry.Open();

                            fileStream.CopyTo(zipStream);
                            fileStream.Close();
                            zipStream.Close();
                        }
                        catch (FileNotFoundException)
                        {
                        }
                        catch (Exception ex)
                        {
                            if (ex is System.UnauthorizedAccessException)
                            {
                                //_log.Info("Can't write to output file. Please choose another output path");
                            }

                            fileStream.Close();
                            zipStream.Close();
                        }
                    }
                }
            }

            foreach (var file in host.Files)
                if (file.Delete)
                    File.Delete(file.Path);

            _log.Info($"Done. Results in file {resultFile}.");
        }

        private static CompositionContainer InitContainer()
        {
            // build MEF container
            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(Program).Assembly));
            catalog.Catalogs.Add(new DirectoryCatalog("./"));

            var container = new CompositionContainer(catalog);

            try
            {
                container.ComposeParts();
            }
            catch (CompositionException compositionException)
            {
                Tools.WriteError(compositionException.Message);
                _log.Error(compositionException.ToString());                
            }

            return container;
        }

        public static void DumpUsage()
        {
            string Usage = string.Join(Environment.NewLine,
                "AgentInfoCollector.exe : command line tool for collecting information on EDR Agent.",
                "",
                "Usage : AgentInfoCollector.exe [OPTIONS]",
                "",
                "Example : AgentInfoCollector.exe 1 6 8",
                "",
                "Options :",
                "   -h -help : display this help",
                $"{ParameterToTask.Print()}"

            );

            Tools.WriteLine(Usage, ConsoleColor.DarkYellow);
        }
    }
}
