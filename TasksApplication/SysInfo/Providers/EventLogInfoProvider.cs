using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TasksApplication.SysInfo.Providers
{
    [Export(typeof(IInfoProvider)), InfoProvider("EventLog")]
    class EventLogInfoProvider : IInfoProvider
    {
#pragma warning disable 649
        [Import]
        ISysInfoHost _host;
#pragma warning restore 649

        private static ILog _log = LogManager.GetLogger(nameof(EventLogInfoProvider));

        public IEnumerable<XElement> ProvideInfo(XElement root)
        {
            foreach (var element in root.Elements("Log"))
            {
                var name = (string)element.Attribute("Name");
                var filename = (string)element.Attribute("Filename");
                var query = (string)element.Attribute("Query") ?? "*";

                var temp = Path.GetTempPath();
                var success = false;
                try
                {
                    using (var session = new EventLogSession())
                    {
                        session.ExportLogAndMessages(name, PathType.LogName, query, temp + filename);
                    }

                    _host.AddFile(temp + filename, filename, true);
                    success = true;
                }
                catch (Exception ex)
                {
                    _log.Warn($"Error: {ex.Message}");
                    continue;
                }
                if (success)
                    yield return new XElement("Log",
                        new XAttribute("Name", name),
                        new XAttribute("Filename", filename),
                        new XAttribute("Query", query));
            }
        }
    }
}
