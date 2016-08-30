using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using PowerArgs;

namespace vpm
{
    public class VpmConfig
    {
        private static VpmConfig _instance;
        public static VpmConfig Instance => _instance ?? (_instance = new VpmConfig());

        private string _vvvvarch;
        public string VVVVArcitecture
        {
            get
            {
                if (_vvvvarch == null)
                {
                    var vpath = Path.Combine(Args.GetAmbientArgs<VpmArgs>().VVVVDir, "vvvv.exe");
                    if (File.Exists(vpath))
                    {
                        var arch = VpmUtils.GetMachineType(vpath).ToString();
                        Console.WriteLine("VVVV architecture seems to be " + arch);
                        _vvvvarch = arch;
                    }
                    else
                    {
                        _vvvvarch = VpmUtils.PromptYayOrNay(
                            "It looks like there's no VVVV in destination folder.\nPlease specify architecture manually",
                            ch1: "x86",
                            ch2: "x64"
                            ) ? "x86" : "x64";
                    }
                }
                return _vvvvarch;
            }
        }

        private List<Assembly> _referencedAssemblies;
        public List<Assembly> ReferencedAssemblies
        {
            get
            {
                if (_referencedAssemblies == null)
                {
                    _referencedAssemblies = new List<Assembly>();
                    foreach (var ass in Assembly.GetExecutingAssembly().GetReferencedAssemblies())
                    {
                        var lass = Assembly.Load(ass);
                        if (string.IsNullOrWhiteSpace(lass.Location)) continue;
                        _referencedAssemblies.Add(lass);
                    }
                }
                return _referencedAssemblies;
            }
        }

        private string _vpmtempdir;

        public string VpmTempDir
        {
            get
            {
                if (_vpmtempdir == null)
                {
                    var vvvvdir = Args.GetAmbientArgs<VpmArgs>().VVVVDir;
                    var tempdir = Directory.CreateDirectory(Path.Combine(vvvvdir, ".vpm"));
                    tempdir.Attributes = FileAttributes.Directory | FileAttributes.Hidden;

                    Console.WriteLine("Temp folder created successfully:\n" + tempdir.FullName);
                    _vpmtempdir = tempdir.FullName;
                }
                return _vpmtempdir;
            }
        }

        private XmlDocument _openedpack;

        public XmlDocument OpenedPackXml
        {
            get
            {
                if (_openedpack == null)
                {
                    _openedpack = VpmUtils.ParseAndValidateXmlFile(Args.GetAmbientArgs<VpmArgs>().VPackFile);
                    Console.WriteLine("Parsed input .vpack file");
                }
                return _openedpack;
            }
        }
    }
}
