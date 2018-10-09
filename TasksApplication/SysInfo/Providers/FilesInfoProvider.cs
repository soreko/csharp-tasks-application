using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TasksApplication.SysInfo.Providers
{
    [Export(typeof(IInfoProvider)), InfoProvider("Files")]
    class FilesInfoProvider : IInfoProvider
    {
#pragma warning disable 649
        [Import]
        ISysInfoHost _host;
#pragma warning restore 649

        private static ILog _log = LogManager.GetLogger(nameof(FilesInfoProvider));

        public IEnumerable<XElement> ProvideInfo(XElement root)
        {
            foreach (var element in root.Elements("File"))
            {
                var path = Environment.ExpandEnvironmentVariables((string)element.Attribute("Path"));
                var filename = (string)element.Attribute("Filename") ?? Path.GetFileName(path);

                if ("True" == (string)element.Attribute("IncludeFile"))
                    _host.AddFile(path, filename);

                var file = new System.IO.FileInfo(path);
                if (!file.Exists)
                {
                    yield return new XElement("File",
                        new XAttribute("Path", path),
                        new XAttribute("Exists", "False"));
                }
                else
                {

                    var version = FileVersionInfo.GetVersionInfo(path);


                    yield return new XElement("File",
                        new XAttribute("Path", path),
                        new XAttribute("Filename", filename),
                        new XAttribute("Size", file.Length),
                        new XAttribute("Attributes", file.Attributes),
                        new XAttribute("CreationTime", file.CreationTime),
                        new XAttribute("ModifiedTime", file.LastWriteTime),
                        new XAttribute("CompanyName", ((version.CompanyName != null) ? (version.CompanyName) : (""))),
                        new XAttribute("FileDescription", ((version.FileDescription != null) ? (version.FileDescription) : (""))),
                        new XAttribute("FileVersion", ((version.FileVersion != null) ? (version.FileVersion) : (""))),
                        new XAttribute("Debug", version.IsDebug),
                        new XAttribute("ProductName", ((version.ProductName != null) ? (version.ProductName) : (""))),
                        new XAttribute("ProductVersion", ((version.ProductVersion != null) ? (version.ProductVersion) : (""))),
                        new XAttribute("OriginalFilename", ((version.OriginalFilename != null) ? (version.OriginalFilename) : (""))),
                        new XAttribute("Language", ((version.Language != null) ? (version.Language) : (""))),
                        new XAttribute("Copyright", ((version.LegalCopyright != null) ? (version.LegalCopyright) : (""))));

                }
            }
        }
    }
}
