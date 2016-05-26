using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using PowerArgs;
using SharpCompress.Archive;
using SharpCompress.Common;

namespace vpm
{
    public class VSVersion
    {
        private static Dictionary<int, VSVersion> _vsdict;
        public static Dictionary<int, VSVersion> VSDict => _vsdict ?? (_vsdict = new Dictionary<int, VSVersion>
        {
            {
                2008, new VSVersion
                {
                    Version = "9.0",
                    Year = 2008,
                    DownloadUrl = "https://go.microsoft.com/fwlink/?LinkId=104679"
                }
            },
            {
                2010, new VSVersion
                {
                    Version = "10.0",
                    Year = 2010,
                    DownloadUrl = "http://download.microsoft.com/download/1/E/5/1E5F1C0A-0D5B-426A-A603-1798B951DDAE/VS2010Express1.iso"
                }
            },
            {
                2012, new VSVersion
                {
                    Version = "11.0",
                    Year = 2012,
                    DownloadUrl = "http://go.microsoft.com/fwlink/?LinkId=623358"
                }
            },
            {
                2013, new VSVersion
                {
                    Version = "12.0",
                    Year = 2013,
                    DownloadUrl = "https://go.microsoft.com/fwlink/?LinkId=532495&clcid=0x409"
                }
            },
            {
                2015, new VSVersion
                {
                    Version = "14.0",
                    Year = 2015,
                    DownloadUrl = "https://www.visualstudio.com/post-download-vs?sku=community&clcid=0x409"
                }
            }
        });

        public string Version;
        public int Year;
        public string DownloadUrl;

        public string DevenvExe
        {
            get
            {
                var programfiles = Environment.GetFolderPath(Environment.Is64BitOperatingSystem ?
                    Environment.SpecialFolder.ProgramFilesX86 :
                    Environment.SpecialFolder.ProgramFiles);
                var devenv = programfiles + @"\Microsoft Visual Studio " + Version + @"\Common7\IDE\devenv.exe";
                if (!File.Exists(devenv))
                {
                    Console.WriteLine("Visual Studio {0} doesn't seem to be installed on your system.", Year);
                    if (VpmUtils.PromptYayOrNay(
                        "Do you want to install it?",
                        "(a browser window will be opened to download Visual Studio)"))
                    {
                        Process.Start(DownloadUrl);
                        while (!File.Exists(devenv))
                        {
                            Console.WriteLine("Press any key when installation is finished...");
                            Console.ReadKey(true);
                        }
                    }
                    else
                    {
                        throw new Exception("Cannot build without proper Visual Studio version.");
                    }
                }
                return devenv;
            }
        }
    }
    public class VpmVVVV
    {
        public string Dir => Path.GetDirectoryName(Args.GetAmbientArgs<VpmArgs>().VVVVExe);
        public string Exe => Args.GetAmbientArgs<VpmArgs>().VVVVExe;
        public string Architecture => VpmConfig.Instance.VVVVArcitecture;
    }

    public class VpmEnv
    {
        public string Exe => Assembly.GetExecutingAssembly().Location;
        public string TempDir => VpmConfig.Instance.VpmTempDir;
    }
    public class VpmGlobals
    {
        private VpmVVVV _vvvv;
        public VpmVVVV VVVV => _vvvv ?? (_vvvv = new VpmVVVV());

        private VpmEnv _vpm;
        public VpmEnv VPM => _vpm ?? (_vpm = new VpmEnv());

        public VPack Pack { get; }

        public VpmGlobals(VPack currentPack)
        {
            Pack = currentPack;
        }

        public void CopyDir(string srcdir, string dstdir, string[] ignore = null, string[] match = null)
        {
            Console.WriteLine("Copy folder:");
            Console.WriteLine("From: " + srcdir);
            Console.WriteLine("To: " + dstdir);
            Console.WriteLine("");
            VpmUtils.CopyDirectory(srcdir, dstdir, ignore, match, o =>
            {
                if (o is FileInfo)
                {
                    Console.WriteLine("File: " + ((FileInfo) o).Name);
                }
                if (o is DirectoryInfo)
                {
                    Console.WriteLine("Dir: " + ((DirectoryInfo) o).FullName);
                }
            });
        }

        public void GitClone(string srcrepo, string dstdir, bool submodules = false, string branch = "")
        {
            VpmUtils.CloneGit(srcrepo, dstdir, submodules, branch);
        }

        public void BuildSolution(int vsversion, string slnpath, string args)
        {
            Console.WriteLine("Building " + slnpath);
            var devenv = VSVersion.VSDict[vsversion].DevenvExe;
            var devenvp = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = devenv,
                    Arguments = "\"" + slnpath + "\" /Build " + args,
                    CreateNoWindow = true
                }
            };
            Console.WriteLine("Starting devenv.exe");
            devenvp.Start();
            devenvp.WaitForExit();
        }

        public void Download(string src, string dst)
        {
            Console.WriteLine("Downloading " + src);
            Console.WriteLine("To " + dst);
            Console.WriteLine("");
            var client = new WebClient();
            client.DownloadProgressChanged += (sender, args) =>
            {
                VpmUtils.ConsoleClearLine();
                Console.WriteLine("Progress: {0} / {1}, {2}%", args.BytesReceived, args.TotalBytesToReceive, args.ProgressPercentage);
            };
            client.DownloadFileTaskAsync(src, dst).RunSynchronously();
            Console.WriteLine("Done");
        }

        public void Extract(string src, string dstdir)
        {
            var archive = ArchiveFactory.Open(src);
            Console.WriteLine("Extracting " + src);
            Console.WriteLine("To " + dstdir);
            Console.WriteLine("");
            foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
            {
                VpmUtils.ConsoleClearLine();
                Console.WriteLine(entry.Key);
                entry.WriteToDirectory(dstdir, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
            }
            Console.WriteLine("Done");
        }
    }
}
