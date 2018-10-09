using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TasksApplication.SysInfo
{
    interface ISysInfoHost
    {
        void AddFile(string path, string internalName, bool delete = false);
        IReadOnlyList<FileInfo> Files { get; }

    }
}
