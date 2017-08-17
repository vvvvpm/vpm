using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using MahApps.Metro.Controls;
using NuGet;
using PowerArgs;

namespace vpm
{
    public class VpmConfig
    {
        private static VpmConfig _instance;
        public static VpmConfig Instance => _instance ?? (_instance = new VpmConfig());

        private IPackageRepository _defaultNugetRepository;
        public IPackageRepository DefaultNugetRepository => _defaultNugetRepository ??
            (_defaultNugetRepository = PackageRepositoryFactory.Default.CreateRepository("https://packages.nuget.org/api/v2"));

        public VpmArgs Arguments { get; set; }

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
                    var vpmfolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    foreach (var file in Directory.GetFiles(vpmfolder, "*.dll"))
                    {
                        Assembly ass;
                        try
                        {
                            ass = Assembly.LoadFrom(file);
                        }
                        catch (Exception e)
                        {
                            continue;
                        }
                        _referencedAssemblies.Add(ass);
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
        public bool WaitSignal {get; set; }
        public bool AgreementsAgreed { get; set; }

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
        public Application WinApp
        {
            get
            {
                if (_winApp == null)
                {
                    VpmConfig.Instance.WaitSignal = true;
                    ApplicationTask = VpmUtils.StartSTATask<bool>(() =>
                    {
                        var winapp = _winApp = new Application();
                        winapp.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                        winapp.Run();
                        return true;
                    });
                    while (_winApp == null)
                    {
                        Thread.Sleep(10);
                    }
                }
                return _winApp;
            }
            //set => _winApp = value;
        }

        private Application _winApp;
        public Task ApplicationTask { get; set; }
        public Thread ApplicationThread { get; set; }

        public Window AgreeWindow { get; set; }

        public Window DirWindow { get; set; }

        public List<VPack> _packlist = new List<VPack>();
        public List<VPack> PackList => _packlist;
        public bool InstallationCancelled = true;
    }
}
