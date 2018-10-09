using log4net;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TasksApplication.SysInfo.Providers
{
    [Export(typeof(IInfoProvider)), InfoProvider("Registry")]
    sealed class RegistryInfoProvider : IInfoProvider
    {
        private static ILog _log = LogManager.GetLogger(nameof(RegistryInfoProvider));

        public IEnumerable<XElement> ProvideInfo(XElement root)
        {
            foreach (var elementKey in root.Elements("Key"))
            {
                var name = (string)elementKey.Attribute("Name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    _log.Warn("Error: no Name attribute found for Key element.");
                    continue;
                }
                var valueName = (string)elementKey.Attribute("ValueName");
                if (valueName != null)
                {
                    // read a specific value
                    RegistryValueKind kind;
                    var svalue = ReadValue(name, valueName, out kind);

                    yield return new XElement("Key",
                        new XAttribute("Name", name),
                        new XAttribute("Type", kind.ToString()),
                        new XAttribute("ValueName", valueName),
                        new XAttribute("Value", svalue));
                    continue;
                }

                if ((string)elementKey.Attribute("AllValues") == "True")
                {
                    foreach (var e in ReadAllValues(name))
                        yield return e;
                }
            }
        }

        IEnumerable<XElement> ReadAllValues(string name)
        {
            using (var key = OpenKey(name))
            {
                foreach (var valueName in key.GetValueNames())
                {
                    RegistryValueKind kind;
                    var svalue = ToValueString(key.GetValue(valueName), kind = key.GetValueKind(valueName));
                    yield return new XElement("Key",
                        new XAttribute("Name", name),
                        new XAttribute("Type", kind.ToString()),
                        new XAttribute("ValueName", valueName),
                        new XAttribute("Value", svalue));
                }
            }
        }

        private string ReadValue(string name, string valueName, out RegistryValueKind kind)
        {
            using (var key = OpenKey(name))
            {  
                if (key != null)
                {
                    return ToValueString(key.GetValue(valueName), kind = key.GetValueKind(valueName));
                }
                else
                {
                    _log.Warn($"Error: No Registry Key named {name}");
                    kind = new RegistryValueKind();
                    return "";
                }
            }
        }

        private string ToValueString(object value, RegistryValueKind kind)
        {
            switch (kind)
            {
                //case RegistryValueKind.ExpandString:
                //	return Environment.ExpandEnvironmentVariables(value.ToString());

                case RegistryValueKind.MultiString:
                    return string.Join(",", ((string[])value));

                case RegistryValueKind.Binary:
                    return string.Join(",", ((byte[])value).Select(b => b.ToString("X2")));

                default:
                    return value.ToString();
            }
        }

        private RegistryKey OpenKey(string name)
        {
            var hive = name.Substring(0, 4).ToUpperInvariant();
            name = name.Substring(5);
            RegistryKey key = null;
            switch (hive)
            {
                case "HKLM":
                    key = Registry.LocalMachine;
                    break;

                case "HKCR":
                    key = Registry.ClassesRoot;
                    break;

                default:
                    throw new ArgumentException("Bad registry hive name");

            }

            Debug.Assert(key != null);
            return key.OpenSubKey(name);
        }
    }
}
