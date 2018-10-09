using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TasksApplication.SysInfo.Providers
{
    [Export(typeof(IInfoProvider)), InfoProvider("System")]
    sealed class SystemInfoProvider : IInfoProvider
    {
        private static ILog _log = LogManager.GetLogger(nameof(SystemInfoProvider));

        public IEnumerable<XElement> ProvideInfo(XElement root)
        {
            yield return new XElement("OperatingSystem",
                new XAttribute("Version", Environment.OSVersion.Version.ToString()),
                new XAttribute("VersionString", Environment.OSVersion.VersionString),
                new XAttribute("Platform", Environment.OSVersion.Platform),
                new XAttribute("ServicePack", Environment.OSVersion.ServicePack)

                );

            if ("True" == (string)root.Attribute("Processes"))
            {
                yield return new XElement("Processes",
                    Process.GetProcesses().OrderBy(p => p.Id).Select(p => new XElement("Process",
                        new XAttribute("Name", p.ProcessName),
                        new XAttribute("PID", p.Id),
                        new XAttribute("StartTime", GetStartTime(p)),
                        new XAttribute("CpuUsage", getProcessCpuUsage(p, root)),
                        getProcessCpuTime(p),
                        getProcessMemCommitSize(p),
                        new XElement("ProcessModules", getProcessModules(p)))));
            }

            if ("True" == (string)root.Attribute("Services"))
            {
                yield return new XElement("Services",
                ServiceController.GetServices().OrderBy(sc => sc.ServiceName).Select(sc => new XElement("Service",
                    new XAttribute("Name", sc.ServiceName),
                    new XAttribute("DisplayName", sc.DisplayName),
                    new XAttribute("Status", sc.Status),
                    new XAttribute("DependsOn", GetAllServicesThatServiceIsDependedOn(sc)))));
            }

            if ("True" == (string)root.Attribute("Drivers"))
            {
                yield return new XElement("Drivers",
                ServiceController.GetDevices().OrderBy(sc => sc.ServiceName).Select(sc => new XElement("Driver",
                    new XAttribute("Name", sc.ServiceName),
                    new XAttribute("DisplayName", sc.DisplayName),
                    new XAttribute("Status", sc.Status))));
            }

            if ("True" == (string)root.Attribute("SystemEnvironmentVariables"))
            {
                var vars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);
                if (vars != null)
                {
                    yield return new XElement("SystemEnvironmentVariables", GetVariables(vars));
                }
            }

            if ("True" == (string)root.Attribute("UserEnvironmentVariables"))
            {
                var vars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User);
                if (vars != null)
                {
                    yield return new XElement("UserEnvironmentVariables", GetVariables(vars));
                }

            }
            if ("True" == (string)root.Attribute("TaskManagerData"))
            {                             
                yield return new XElement("TaskManagerData",
                    getCpuUsage(),
                    getNetworkUsage(),
                    getDiskUsage());
            }
        }

        private string getProcessCpuUsage(Process p, XElement config)
        {
            string result = string.Empty;
            try
            {
                if ("True" != (string)config.Attribute("TaskManagerData"))
                    throw new ArgumentOutOfRangeException();

                // Get CPU usage
                var cpuCounter = new PerformanceCounter("Process", "% Processor Time", p.ProcessName);
                cpuCounter.NextValue();
                Thread.Sleep(80);

                // divide CPU usage by number of CPUs
                result = Math.Round(cpuCounter.NextValue(), 1).ToString();
                if (result != "0")
                {
                    result = (double.Parse(result) / Environment.ProcessorCount).ToString();
                }                 
            }
            catch (Exception ex)
            {
                _log.Error($"{nameof(SystemInfoProvider)}: {ex.ToString()}");
            }
            return result; 
        }

        private XElement getNetworkUsage()
        {
            XElement result = null;
            try
            {
                if (!NetworkInterface.GetIsNetworkAvailable())
                    throw new Exception("Network is unavailable");

                NetworkInterface[] interfaces
                    = NetworkInterface.GetAllNetworkInterfaces();

                List<XElement> interfacesNwUsage = new List<XElement>();
                long totalBytesSent = 0;
                long totalBytesReceived = 0;

                foreach (NetworkInterface ni in interfaces)
                {
                    interfacesNwUsage.Add(new XElement("InterfaceUsage",
                        new XAttribute("Name", ni.Name.ToString()),
                        new XAttribute("BytesSent", ni.GetIPv4Statistics().BytesSent),
                        new XAttribute("BytesReceived", ni.GetIPv4Statistics().BytesReceived)));

                    totalBytesSent += ni.GetIPv4Statistics().BytesSent;
                    totalBytesReceived += ni.GetIPv4Statistics().BytesReceived;
                }

                result = new XElement("NetworkUsage",
                    new XAttribute("totalBytesSent", totalBytesSent),
                    new XAttribute("totalBytesReceived", totalBytesReceived),
                    interfacesNwUsage);
            }
            catch (Exception ex)
            {
                _log.Debug("{0} Exception caught. ", ex);
                _log.Debug("Writing default error Element to XML. ");
                result = new XElement("NetworkUsage");                    
            }
            finally
            {

            }

            return result;
        }

        private XElement getCpuUsage()
        {
            XElement result = null;
            try
            {
                // Get total CPU usage
                var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpuCounter.NextValue();
                Thread.Sleep(200);
                               
                var cpuUsage = Math.Round(cpuCounter.NextValue(), 1);

                // divide CPU usage by number of CPUs
                if (cpuUsage != 0)
                {
                    cpuUsage = cpuUsage / Environment.ProcessorCount;
                }                

                result = new XElement("TotalCpuUsage",
                    new XAttribute("UsagePercent", cpuUsage));
            }
            catch (Exception ex)
            {
                _log.Debug("{0} Exception caught. ", ex);
                _log.Debug("Writing default error Element to XML. ");
                result = new XElement("TotalCpuUsage");
            }
            finally
            {

            }

            return result;
        }

        private XElement getDiskUsage()
        {
            XElement result = null;
            try
            {
                List<XElement> drivesUsage = new List<XElement>();
                
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (DriveInfo drive in drives)
                {
                    if (drive.IsReady)
                    {
                        drivesUsage.Add(new XElement("DriveUsage",
                        new XAttribute("Name", drive.Name),
                        new XAttribute("TotalFreeSpaceBytes", drive.TotalFreeSpace),
                        new XAttribute("TotalSize", drive.TotalSize)));
                    }
                }

                result = new XElement("TotalDiskUsage", drivesUsage);
            }
            catch (Exception ex)
            {
                _log.Debug("{0} Exception caught. ", ex);
                _log.Debug("Writing default error Element to XML. ");
                result = new XElement("TotalDiskUsage");
            }
            finally
            {

            }

            return result;
        }        

        private static List<XElement> GetVariables(IDictionary vars)
        {
            var varsRoot = new List<XElement>(vars.Count);
            foreach (DictionaryEntry v in vars)
                varsRoot.Add(new XElement("Variable",
                    new XAttribute("Name", (string)v.Key),
                    new XAttribute("Value", (string)v.Value)));
            return varsRoot;
        }

        private string GetStartTime(Process p)
        {
            try
            {
                return p.StartTime.ToShortDateString() + " " + p.StartTime.ToLongTimeString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private string GetAllServicesThatServiceIsDependedOn(ServiceController sc)
        {
            try
            {
                return string.Join(",", sc.ServicesDependedOn.Select(s => s.ServiceName));
            }
            catch
            {
                return string.Empty;
            }
        }

        private XElement getProcessCpuTime(Process p)
        {
            XElement result = null;
            try
            {
                result = new XElement("ProcessCpuTime",
                    new XAttribute("TotalDays", p.TotalProcessorTime.TotalDays.ToString()),
                    new XAttribute("TotalHours", p.TotalProcessorTime.TotalHours.ToString()),
                    new XAttribute("TotalMinutes", p.TotalProcessorTime.TotalMinutes.ToString()),
                    new XAttribute("TotalSeconds", p.TotalProcessorTime.TotalSeconds.ToString()),
                    new XAttribute("TotalMilliSeconds", p.TotalProcessorTime.TotalMilliseconds.ToString()));
            }
            catch (System.Exception e)
            {
                _log.Debug("{0} Exception caught. ", e);
                _log.Debug("Writing default error Element to XML. ");
                result = new XElement("ProcessCpuTime",
                    new XAttribute("TotalDays", "-1"),
                        new XAttribute("TotalHours", "-1"),
                        new XAttribute("TotalMinutes", "-1"),
                        new XAttribute("TotalSeconds", "-1"),
                        new XAttribute("TotalMilliSeconds", "-1"));
            }
            finally
            {

            }

            return result;
        }

        private XElement getProcessMemCommitSize(Process p)
        {
            return new XElement("ProcessMemCommitSize",
                new XAttribute("PagedMemInBytes", p.PagedMemorySize64.ToString()));
        }

        private List<XElement> getProcessModules(Process p)
        {
            var modulesRoot = new List<XElement>();

            try
            {
                foreach (ProcessModule module in p.Modules)
                {
                    modulesRoot.Add(new XElement("ProcessModule",
                         new XAttribute("ModuleName", module.ModuleName),
                         new XAttribute("FileName", module.FileName),
                         new XAttribute("FileVersion", module.FileVersionInfo.FileVersion)));
                }
            }
            catch (System.Exception e)
            {
                _log.Debug("{0} Exception caught. ", e);
                _log.Debug("Writing default error Element to XML. ");
                modulesRoot.Add(new XElement("ProcessModule",
                    new XAttribute("ModuleName", "N/A"),
                    new XAttribute("FileName", "N/A"),
                    new XAttribute("FileVersion", "N/A")));
            }
            finally
            {

            }
            return modulesRoot;
        }





    }
}
