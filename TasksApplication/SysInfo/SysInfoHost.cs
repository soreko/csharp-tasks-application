using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TasksApplication.SysInfo
{
    sealed class FileInfo
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public bool Delete { get; set; }
    }

    [Export(typeof(ISysInfoHost))]
    class SysInfoHost : ISysInfoHost
    {
        List<FileInfo> _files = new List<FileInfo>();

        public void AddFile(string path, string internalName = null, bool delete = false)
        {
            if (string.IsNullOrWhiteSpace(internalName))
                internalName = Path.GetFileName(path);
            _files.Add(new FileInfo
            {
                Path = path,
                Name = internalName,
                Delete = delete
            });
        }

        public IReadOnlyList<FileInfo> Files => _files;

        public bool DeleteFiles { get; set; }
    }
}
