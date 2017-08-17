using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PowerArgs;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

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
    public class VpmVVVV : MarshalByRefObject
    {
        public string Dir => Args.GetAmbientArgs<VpmArgs>().VVVVDir;
        public string Exe => Path.Combine(Args.GetAmbientArgs<VpmArgs>().VVVVDir, "vvvv.exe");
        public string Architecture => VpmConfig.Instance.VVVVArcitecture;
    }

    public class VpmEnv : MarshalByRefObject
    {
        public string Exe => Assembly.GetExecutingAssembly().Location;
        public string TempDir => VpmConfig.Instance.VpmTempDir;
    }
    public class VpmGlobals : MarshalByRefObject
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

        public void ThrowException(Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Script thrown an Exception, contact the pack developer:");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(e.Message);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(e.StackTrace);
            Console.WriteLine("");
            Console.WriteLine("Pack is not installed most probably");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        public void CopyDir(string srcdir, string dstdir, string[] ignore = null, string[] match = null)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Copy folder:");
            Console.WriteLine("From: " + srcdir);
            Console.WriteLine("To: " + dstdir);
            Console.WriteLine("");
            VpmUtils.CopyDirectory(srcdir, dstdir, ignore, match, o =>
            {
                if (o is FileInfo)
                {
                    if (UpdateTimer.Instance.Update())
                    {
                        VpmUtils.ConsoleClearLine();
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine("File: " + ((FileInfo)o).Name);
                    }
                }
            });
            Console.ResetColor();
        }

        public void GitClone(string srcrepo, string dstdir, bool submodules = false, string branch = "")
        {
            VpmUtils.CloneGit(srcrepo, dstdir, submodules, branch);
        }

        public void BuildSolution(int vsversion, string slnpath, string args, bool restorenugets)
        {
            if (restorenugets)
            {
                var nugetexe = Path.GetDirectoryName(VPM.Exe) + "\\NuGet.exe";
                var nugetp = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = nugetexe,
                        Arguments = "restore \"" + slnpath + "\" -Verbosity detailed"
                    }
                };
                Console.WriteLine("Restoring NuGet packages");
                nugetp.Start();
                nugetp.WaitForExit();
            }
            BuildSolution(vsversion, slnpath, args);
        }

        public void BuildSolution(int vsversion, string slnpath, string args)
        {
            Console.ForegroundColor = ConsoleColor.White;
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
            Console.ResetColor();
            Console.WriteLine("Starting devenv.exe");
            devenvp.Start();
            devenvp.WaitForExit();
        }

        public void Download(string src, string dst)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Downloading " + src);
            Console.WriteLine("To " + dst);
            Console.WriteLine("");
            var client = new WebClient();
            Console.ForegroundColor = ConsoleColor.Gray;
            client.DownloadProgressChanged += (sender, args) =>
            {
                if (UpdateTimer.Instance.Update())
                {
                    VpmUtils.ConsoleClearLine();
                    Console.WriteLine("Progress: {0} / {1}, {2}%", args.BytesReceived, args.TotalBytesToReceive, args.ProgressPercentage);
                }
            };
            var dltask = client.DownloadFileTaskAsync(src, dst);
            try
            {
                dltask.Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine("Problem with waiting for downloader. Skipping wait.");
            }
            Console.ResetColor();
            Console.WriteLine("Done");
        }

        private Stopwatch ArchiveTimeout;
        private IArchive OpenArchive(string src)
        {
            if (ArchiveTimeout == null)
            {
                ArchiveTimeout = new Stopwatch();
                ArchiveTimeout.Start();
            }
            try
            {
                return ArchiveFactory.Open(src);
            }
            catch (Exception)
            {
                if (ArchiveTimeout.Elapsed.TotalSeconds < 10)
                    return OpenArchive(src);
                throw;
            }
        }

        public void Extract(string src, string dstdir)
        {
            var archive = OpenArchive(src);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Extracting " + src);
            Console.WriteLine("To " + dstdir);
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Gray;
            foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
            {
                if (UpdateTimer.Instance.Update())
                {
                    VpmUtils.ConsoleClearLine();
                    Console.WriteLine(entry.Key);
                }
                entry.WriteToDirectory(dstdir, new ExtractionOptions
                {
                    ExtractFullPath = true,
                    Overwrite = true
                });
            }
            archive.Dispose();
            Console.ResetColor();
            Console.WriteLine("Done");
        }
    }
}
