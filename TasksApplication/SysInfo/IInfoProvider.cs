using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TasksApplication.SysInfo
{
    public interface IInfoProvider
    {
        IEnumerable<XElement> ProvideInfo(XElement root);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class InfoProviderAttribute : Attribute
    {
        public string RootName { get; }
        public InfoProviderAttribute(string rootName)
        {
            RootName = rootName;
        }
    }
}
