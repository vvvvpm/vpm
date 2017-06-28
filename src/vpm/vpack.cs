using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Xml;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using PowerArgs;
using NuGet;

namespace vpm
{
    public class JsVPackInterop
    {
        public VPack CurrentPack;
        public UserAgree UserAgreeWindow;

        public string GetPackXml()
        {
            return CurrentPack.RawXml;
        }

        public void Continue(string data)
        {
            SetInstallData(data);
            UserAgreeWindow.ContinueFromJS();
        }
        public void SetInstallData(string data)
        {
            CurrentPack.InstallDataFromJS = data;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Licensing page passed data for the installation script.");
            Console.ResetColor();
        }
        public void DisableAgree()
        {
            UserAgreeWindow.DisableAgree();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Licensing page Disabled the \"Agree\" checkbox.");
            Console.ResetColor();
        }
        public void EnableAgree()
        {
            UserAgreeWindow.EnableAgree();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Licensing page Enabled the \"Agree\" checkbox.");
            Console.ResetColor();
        }
    }
    public class VPack
    {
        public string Name;
        public string Author;
        public string Source;
        public string TempDir;
        public string LicenseUrl;
        public string InstallDataFromJS;
        public string InstallScript;
        public string RawXml;
        public bool Agreed = false;
        public List<string> Aliases = new List<string>();
        public XmlNodeList NugetPackages;
        public List<VPack> Dependencies = new List<VPack>();
        
        public VPack(string name, string source, IEnumerable<string> aliases = null, XmlDocument srcxml = null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Initializing " + name);
            Console.ResetColor();

            Name = name;
            Source = source;
            TempDir = VpmConfig.Instance.VpmTempDir + "\\" + name;
            if(aliases != null)
                Aliases.AddRange(aliases);

            var authornode = srcxml?.SelectSingleNode("/vpack/meta/author");
            if (authornode != null) Author = authornode.InnerText.Trim();

            Directory.CreateDirectory(TempDir);

            var xmldoc = srcxml;
            if (source.StartsWith("vpm://", true, CultureInfo.InvariantCulture) ||
                source.StartsWith("vpms://", true, CultureInfo.InvariantCulture))
            {
                xmldoc = VpmUtils.ParseAndValidateXmlFile(source);
            }

            if (CloneFromGit(true))
            {
                var vpackfiles = Directory.GetFiles(TempDir, "*.vpack");
                if ((vpackfiles.Length > 0) && (xmldoc == null))
                {
                    xmldoc = VpmUtils.ParseAndValidateXmlFile(vpackfiles[0]);
                }
            }
            if (xmldoc != null)
            {
                RawXml = xmldoc.ToString();
                var licensenode = xmldoc.SelectSingleNode("/vpack/meta/license");
                LicenseUrl = licensenode?.InnerText.Trim() ?? "http://www.imxprs.com/free/microdee/vpmnolicense";

                var namenode = xmldoc.SelectSingleNode("/vpack/meta/name");
                if (namenode == null)
                {
                    Console.WriteLine(
                        "WARNING: VPack XML doesn't contain name. Using " + Name + " from other sources.");
                }
                else
                {
                    if (!string.Equals(Name.Trim(), namenode.InnerText.Trim(), StringComparison.CurrentCultureIgnoreCase))
                    {
                        Console.WriteLine(
                            "WARNING: VPack XML says pack is called " + namenode.InnerText.Trim() +
                            ", but using " + Name + " to avoid conflicts");
                    }
                }
                var installnode = xmldoc.SelectSingleNode("/vpack/install");
                if (installnode == null)
                {
                    throw new Exception("VPack doesn't contain installing script.");
                }
                InstallScript = installnode.InnerText;
                
                var nugetnode = xmldoc.SelectSingleNode("/vpack/nuget");
                if (nugetnode != null)
                {
                    NugetPackages = nugetnode.ChildNodes;
                }

                var dependenciesnode = xmldoc.SelectSingleNode("/vpack/meta/dependencies");
                if (dependenciesnode != null)
                {
                    var nodelist = dependenciesnode.ChildNodes;
                    for (int i = 0; i < nodelist.Count; i++)
                    {
                        var dependencynode = nodelist.Item(i);
                        if (dependencynode == null) continue;
                        if (dependencynode.Name != "dependency")
                        {
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine("Unknown node in Dependencies: " + dependencynode.Name + ". Moving on.");
                            Console.ResetColor();
                            continue;
                        }
                        var dnamenode = dependencynode["name"];
                        var dsrcnode = dependencynode["source"];
                        var daliasesnode = dependencynode["aliases"];

                        if ((dnamenode == null) || (dsrcnode == null))
                            throw new Exception("Insufficient data to parse dependency.");

                        var dname = dnamenode.InnerText.Trim();
                        if (VpmUtils.IsPackAlreadyInTemp(dname))
                        {
                            Console.WriteLine(dname + " is already in vpm temp folder. Ignoring");
                            continue;
                        }
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("Parsing " + dname);
                        Console.ResetColor();
                        var dsrc = dsrcnode.InnerText.Trim();

                        List<string> aliaslist = null;
                        if (daliasesnode != null)
                        {
                            var dantext = daliasesnode.InnerText;
                            aliaslist = dantext.Split(',').ToList();
                            for (int j = 0; j < aliaslist.Count; j++)
                            {
                                aliaslist[j] = aliaslist[j].Trim();
                            }
                        }
                        string matchedname;
                        if (VpmUtils.IsPackExisting(dname, aliaslist, out matchedname))
                        {
                            Console.WriteLine(dname + " seems to be there already.");
                            var replaceit = !Args.GetAmbientArgs<VpmArgs>().Quiet;
                            if (replaceit)
                            {
                                replaceit = VpmUtils.PromptYayOrNay(
                                    "Do you want to replace it?",
                                    "WARNING: Original pack will be deleted!\nWARNING: If anything goes wrong during installation original pack won't be recovered.");
                            }
                            if (!replaceit) continue;
                            var aliasdir = Path.Combine(Args.GetAmbientArgs<VpmArgs>().VVVVDir, "packs", matchedname);
                            VpmUtils.DeleteDirectory(aliasdir, true);
                        }
                        var newvpack = new VPack(dname, dsrc, aliaslist);
                        Dependencies.Add(newvpack);
                    }
                }
            }
            else
            {
                ScriptSource();
            }
            VpmConfig.Instance.PackList.Add(this);
        }

        protected bool CloneFromGit(bool submodules)
        {
            if (Source.EndsWith(".git"))
            {
                Console.WriteLine("Cloning git repository of " + Name);
                Console.WriteLine("To: " + TempDir);
                VpmUtils.CloneGit(Source, TempDir, true);
                return true;
            }
            return false;
        }

        protected bool ScriptSource()
        {
            if (Source.EndsWith(".csx"))
            {
                if (Regex.IsMatch(Source, @"(?:https?|ftp):\/\/.*?\.csx"))
                {
                    Console.WriteLine("Fetching install script for " + Name);
                    var client = new WebClient();
                    try
                    {
                        var stream = client.OpenRead(Source);
                        var reader = new StreamReader(stream);
                        InstallScript = reader.ReadToEnd();
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Problem with URL " + Source);
                        throw;
                    }
                    Console.WriteLine("Done");
                }
                else
                {
                    throw new Exception("No local scripts supported currently. Sorry :(");
                }
                return true;
            }
            return false;
        }

        public void Install()
        {
            foreach (var d in Dependencies)
            {
                d.Install();
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Installing " + Name);
            Console.ResetColor();

            var vpmglobal = new VpmGlobals(this);
            var assemblies = VpmConfig.Instance.ReferencedAssemblies;
            if (NugetPackages != null && NugetPackages.Count > 0)
            {
                Console.WriteLine("Initializing Nuget for this pack");
                var packdir = Path.Combine(TempDir, "NugetPackages");
                Directory.CreateDirectory(packdir);
                var repo = VpmConfig.Instance.DefaultNugetRepository;
                var packman = new PackageManager(repo, packdir);
                packman.PackageInstalled += (sender, args) =>
                {
                    Console.WriteLine("Installed " + args.Package.Id);
                };
                for (int i = 0; i < NugetPackages.Count; i++)
                {
                    var packnode = NugetPackages[i];
                    Console.WriteLine("Installing Nuget Package " + packnode.InnerText);
                    var version = packnode.Attributes?["version"]?.Value;
                    
                    var packages = repo.FindPackagesById(packnode.InnerText);
                    IPackage package = null;
                    foreach (var p in packages)
                    {
                        bool versioncheck = p.IsLatestVersion;
                        if (version != null)
                            versioncheck = SemanticVersion.Parse(version) == p.Version;

                        if (versioncheck)
                        {
                            package = p;
                            break;
                        }
                    }
                    if (package == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("No nuget package found with those conditions");
                        Console.ResetColor();
                    }
                    else
                    {
                        packman.InstallPackage(package, false, true);
                    }
                }
            }
            try
            {
                CSharpScript.EvaluateAsync(InstallScript,
                    globals: vpmglobal,
                    options: ScriptOptions.Default.WithReferences(assemblies));
            }
            catch (Exception e)
            {
                if (e is CompilationErrorException)
                {
                    var ee = (CompilationErrorException) e;
                    Console.WriteLine("Compilation error:");
                    Console.WriteLine(string.Join(Environment.NewLine, ee.Diagnostics));
                }
                else
                {
                    Console.WriteLine("Script error:");
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
                VpmUtils.CleanUp();
                Environment.Exit(0);
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
