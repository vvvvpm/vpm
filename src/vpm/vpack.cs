using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using PowerArgs;

namespace vpm
{
    public class VPack
    {
        public string Name;
        public string Author;
        public string Source;
        public string TempDir;
        public string InstallScript;
        public List<string> Aliases = new List<string>();
        public List<VPack> Dependencies = new List<VPack>();
        
        public VPack(string name, string source, IEnumerable<string> aliases = null, XmlDocument srcxml = null)
        {
            Console.WriteLine("Initializing " + name);

            Name = name;
            Source = source;
            TempDir = VpmConfig.Instance.VpmTempDir + "\\" + name;
            if(aliases != null)
                Aliases.AddRange(aliases);

            var authornode = srcxml?.SelectSingleNode("/vpack/meta/author");
            if (authornode != null) Author = authornode.InnerText.Trim();

            Directory.CreateDirectory(TempDir);

            var xmldoc = srcxml;

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
                            Console.WriteLine("Unknown node in Dependencies: " + dependencynode.Name + ". Moving on.");
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
                        Console.WriteLine("Parsing " + dname);
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
                            if(replaceit)
                                replaceit = VpmUtils.PromptYayOrNay(
                                    "Do you want to replace it?",
                                    "WARNING: Original pack will be deleted!\nWARNING: If anything goes wrong during installation original pack won't be recovered.");
                            if (!replaceit) continue;
                            var aliasdir = Path.GetDirectoryName(Args.GetAmbientArgs<VpmArgs>().VVVVExe) + "\\packs\\" + matchedname;
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
            Console.WriteLine("Installing " + Name);
            var vpmglobal = new VpmGlobals(this);
            try
            {
                CSharpScript.EvaluateAsync(InstallScript, globals: vpmglobal);
            }
            catch (CompilationErrorException e)
            {
                Console.WriteLine("Compilation error:");
                Console.WriteLine(string.Join(Environment.NewLine, e.Diagnostics));
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
                VpmUtils.CleanUp();
                Environment.Exit(0);
            }
        }
    }
}
