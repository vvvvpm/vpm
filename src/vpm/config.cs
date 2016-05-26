using System;
using System.IO;
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
                    var arch = VpmUtils.GetMachineType(Args.GetAmbientArgs<VpmArgs>().VVVVExe).ToString();
                    Console.WriteLine("VVVV architecture seems to be " + arch);
                    _vvvvarch = arch;
                }
                return _vvvvarch;
            }
        }

        private string _vpmtempdir;

        public string VpmTempDir
        {
            get
            {
                if (_vpmtempdir == null)
                {
                    var vvvvdir = Path.GetDirectoryName(Args.GetAmbientArgs<VpmArgs>().VVVVExe);
                    var tempdir = Directory.CreateDirectory(vvvvdir + "\\.vpm");
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
