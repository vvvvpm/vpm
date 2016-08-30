using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using LibGit2Sharp;
using Microsoft.Win32;
using PowerArgs;

namespace vpm
{
    public class UpdateTimer
    {
        private static UpdateTimer _instance;
        public static UpdateTimer Instance => _instance ?? (_instance = new UpdateTimer());

        private Stopwatch Stopwatch = new Stopwatch();

        public long msLimit = 50;

        public UpdateTimer()
        {
            Stopwatch.Start();
        }
        public void Reset()
        {
            Stopwatch.Restart();
        }

        public bool Update()
        {
            if (ms > msLimit)
            {
                Reset();
                return true;
            }
            return false;
        }

        public long ms => Stopwatch.ElapsedMilliseconds;
    }
    public enum MachineType
    {
        Native = 0, x86 = 0x014c, Itanium = 0x0200, x64 = 0x8664
    }

    public class VersionRelation
    {
        // value is current - existing
        public int MajorDiff { get; private set; }
        public int MinorDiff { get; private set; }
        public int BuildDiff { get; private set; }
        public int RevisionDiff { get; private set; }

        public VersionRelation(System.Version curr, System.Version existing)
        {
            MajorDiff = curr.Major - existing.Major;
            MinorDiff = curr.Minor - existing.Minor;
            BuildDiff = curr.Build - existing.Build;
            RevisionDiff = curr.Revision - existing.Revision;
        }
    }
    public static class VpmUtils
    {
        public static void ConsoleClearLine()
        {
            Console.SetCursorPosition(0, Math.Max(Console.CursorTop - 1, 0));
            Console.Write(new String(' ', Console.BufferWidth));
            Console.SetCursorPosition(0, Math.Max(Console.CursorTop - 1, 0));
        }
        public static MachineType GetMachineType(string fileName)
        {
            const int PE_POINTER_OFFSET = 60;
            const int MACHINE_OFFSET = 4;
            byte[] data = new byte[4096];
            using (Stream s = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                s.Read(data, 0, 4096);
            }
            // dos header is 64 bytes, last element, long (4 bytes) is the address of the PE header
            int PE_HEADER_ADDR = BitConverter.ToInt32(data, PE_POINTER_OFFSET);
            int machineUint = BitConverter.ToUInt16(data, PE_HEADER_ADDR + MACHINE_OFFSET);
            return (MachineType) machineUint;
        }

        public static void CleanUp()
        {
            var tempdir = VpmConfig.Instance.VpmTempDir;
            Console.WriteLine("Removing vpm temp folder.");
            File.SetAttributes(tempdir, FileAttributes.Normal);
            PatientDeleteDirectory(tempdir, true, 0, 3);
        }

        public static void PatientDeleteDirectory(string path, bool recursive, int currtry, int maxtry)
        {
            try
            {
                DeleteDirectory(path, recursive);
            }
            catch (Exception e)
            {
                if (e is IOException || e is UnauthorizedAccessException)
                {
                    if (currtry < maxtry)
                    {
                        Console.WriteLine("It seems to be a file is not released yet. Waiting 5 seconds");
                        Thread.Sleep(5000);
                        PatientDeleteDirectory(path, recursive, currtry + 1, maxtry);
                    }
                    else
                    {
                        Console.WriteLine("Files didn't unlock in {0}.\nYou might have to delete it yourself.", VpmConfig.Instance.VpmTempDir);
                    }
                }
                else throw e;
            }
        }

        public static void DeleteDirectory(string path, bool recursive)
        {
            // Delete all files and sub-folders?
            if (recursive)
            {
                // Yep... Let's do this
                var subfolders = Directory.GetDirectories(path);
                foreach (var s in subfolders)
                {
                    DeleteDirectory(s, recursive);
                }
            }

            // Get all files of the folder
            var files = Directory.GetFiles(path);
            foreach (var f in files)
            {
                // Get the attributes of the file
                var attr = File.GetAttributes(f);

                // Is this file marked as 'read-only'?
                if ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    // Yes... Remove the 'read-only' attribute, then
                    File.SetAttributes(f, attr ^ FileAttributes.ReadOnly);
                }

                // Delete the file
                File.Delete(f);
            }

            // When we get here, all the files of the folder were
            // already deleted, so we just delete the empty folder
            Directory.Delete(path);
        }

        public static bool PromptYayOrNay(string question, string note = "", string ch1 = "Y", string ch2 = "N")
        {
            bool first = true;
            while (true)
            {
                if (first)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(question + " (" + ch1 + " or " + ch2 + ")");
                    if (!string.IsNullOrEmpty(note)) Console.WriteLine(note);
                    Console.ResetColor();
                }
                var decision = Console.ReadLine();
                if (decision == null) continue;
                decision = decision.ToLower();
                if (decision.Contains(ch1.ToLower()))
                {
                    return true;
                }
                if (decision.Contains(ch2.ToLower()))
                {
                    return false;
                }
                first = false;
            }
        }

        public static void ConfirmContinue()
        {
            if (Args.GetAmbientArgs<VpmArgs>().Quiet) return;
            if (PromptYayOrNay("Do you still want to continue?")) return;
            CleanUp();
            Environment.Exit(0);
        }

        public static bool IsAliasExisting(string name)
        {
            var packsdir = Path.Combine(Args.GetAmbientArgs<VpmArgs>().VVVVDir, "packs");
            if (!Directory.Exists(packsdir))
            {
                Directory.CreateDirectory(packsdir);
                return false;
            }
            foreach (var d in Directory.GetDirectories(packsdir))
            {
                var cname = Path.GetFullPath(d)
                    .TrimEnd(Path.DirectorySeparatorChar)
                    .Split(Path.DirectorySeparatorChar)
                    .Last();
                if (string.Equals(cname, name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsPackAlreadyInTemp(string name)
        {
            var packsdir = VpmConfig.Instance.VpmTempDir;
            foreach (var d in Directory.GetDirectories(packsdir))
            {
                var cname = Path.GetFullPath(d)
                    .TrimEnd(Path.DirectorySeparatorChar)
                    .Split(Path.DirectorySeparatorChar)
                    .Last();
                if (string.Equals(cname, name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsPackExisting(string name, IEnumerable<string> aliases, out string matched)
        {
            if (IsAliasExisting(name))
            {
                matched = name;
                return true;
            }
            if (aliases != null)
            {
                foreach (var a in aliases.Where(IsAliasExisting))
                {
                    matched = a;
                    return true;
                }
            }
            matched = "";
            return false;
        }

        public static XmlDocument ParseAndValidateXmlFile(string xmlfile)
        {
            /*
            var xmlsettings = new XmlReaderSettings
            {
                Async = false,
                DtdProcessing = DtdProcessing.Parse,
                ValidationType = ValidationType.DTD
            };
            xmlsettings.ValidationEventHandler += (sender, args) =>
            {
                throw new Exception(".vpack validation error: " + args.Message);
            };
            var reader = XmlReader.Create(Args.GetAmbientArgs<VpmArgs>().VPackFile, xmlsettings);
            while (reader.Read());
            */

            var doc = new XmlDocument();
            string xmltext = "";
            string url = "";
            if (xmlfile.StartsWith("http://", true, CultureInfo.InvariantCulture))
                url = xmlfile;
            if (xmlfile.StartsWith("https://", true, CultureInfo.InvariantCulture))
                url = xmlfile;

            if (xmlfile.StartsWith("vpm://", true, CultureInfo.InvariantCulture))
                url = xmlfile.Replace("vpm://", "http://");
            if (xmlfile.StartsWith("vpms://", true, CultureInfo.InvariantCulture))
                url = xmlfile.Replace("vpms://", "https://");

            if (url != "")
            {
                var client = new WebClient();
                try
                {
                    var stream = client.OpenRead(url);
                    var reader = new StreamReader(stream);
                    xmltext = reader.ReadToEnd();
                }
                catch (Exception)
                {
                    Console.WriteLine("Problem with URL " + xmlfile);
                    throw;
                }
            }
            else
            {
                xmltext = File.ReadAllText(xmlfile);
            }

            var installregex = new Regex(@"<install>(.*?)<\/install>", RegexOptions.Multiline | RegexOptions.Singleline);
            var imatch = installregex.Match(xmltext);
            var installtext = imatch.Groups[1].Value;
            installtext = installtext.Replace("<", "&lt;");
            installtext = installtext.Replace(">", "&gt;");

            xmltext = Regex.Replace(
                xmltext, @"<install>.*?<\/install>", "<install>" + installtext + "</install>",
                RegexOptions.Multiline | RegexOptions.Singleline);

            doc.LoadXml(xmltext);
            return doc;
        }

        public static void CopyDirectory(
            string src,
            string dst,
            string[] ignore = null,
            string[] match = null,
            Action<object> progress = null)
        {

            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(src);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + src);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            if (match != null)
            {
                dirs = dirs.Where(info =>
                {
                    return match.Any(pattern => new WildcardPattern(pattern).IsMatch(info.Name));
                }).ToArray();
            }
            if (ignore != null)
            {
                dirs = dirs.Where(info =>
                {
                    return !ignore.Any(pattern => new WildcardPattern(pattern).IsMatch(info.Name));
                }).ToArray();
            }
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(dst))
            {
                Directory.CreateDirectory(dst);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();

            if (match != null)
            {
                files = files.Where(info =>
                {
                    return match.Any(pattern => new WildcardPattern(pattern).IsMatch(info.Name));
                }).ToArray();
            }
            if (ignore != null)
            {
                files = files.Where(info =>
                {
                    return !ignore.Any(pattern => new WildcardPattern(pattern).IsMatch(info.Name));
                }).ToArray();
            }

            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(dst, file.Name);
                progress?.Invoke(file);
                file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(dst, subdir.Name);
                progress?.Invoke(subdir);
                CopyDirectory(subdir.FullName, temppath, ignore, match, progress);
            }
        }

        public static void CloneGit(string srcrepo, string dstdir, bool submodules = false, string branch = "")
        {
            var options = new CloneOptions
            {
                RepositoryOperationStarting = context =>
                {
                    Console.WriteLine("Cloning: " + context.RepositoryPath + " / " + branch);
                    if (!string.IsNullOrEmpty(context.SubmoduleName))
                    {
                        Console.WriteLine("From submodule " + context.SubmoduleName);
                    }
                    Console.WriteLine("");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return true;
                },
                RepositoryOperationCompleted = context =>
                {
                    Console.ResetColor();
                    Console.WriteLine("");
                    Console.WriteLine("Done: " + context.RepositoryPath);
                    if (!string.IsNullOrEmpty(context.SubmoduleName))
                    {
                        Console.WriteLine("a.k.a " + context.SubmoduleName);
                    }
                },
                OnCheckoutProgress = (path, steps, totalSteps) =>
                {
                    if (UpdateTimer.Instance.Update())
                    {
                        ConsoleClearLine();
                        Console.WriteLine("Checking out: {0} / {1}", steps, totalSteps);
                    }
                },
                OnTransferProgress = progress =>
                {
                    if (UpdateTimer.Instance.Update())
                    {
                        ConsoleClearLine();
                        Console.WriteLine("Recieving: {0} / {1} ({2} kb)",
                            progress.IndexedObjects,
                            progress.TotalObjects,
                            progress.ReceivedBytes/1024);
                    }
                    return true;
                },
                RecurseSubmodules = submodules
            };
            if (!string.IsNullOrEmpty(branch)) options.BranchName = branch;
            Repository.Clone(srcrepo, dstdir, options);

            Console.ResetColor();
            Console.WriteLine("Done");
        }

        public static void RegisterURIScheme(string scheme)
        {
            var key = Registry.ClassesRoot.CreateSubKey(scheme);
            key.SetValue("", "URL:" + scheme + " Protocol");
            key.SetValue("URL Protocol", "");
            var icon = key.CreateSubKey("DefaultIcon");
            icon.SetValue("", Assembly.GetExecutingAssembly().Location);
            var command = key.CreateSubKey("shell\\open\\command");
            command.SetValue("", Assembly.GetExecutingAssembly().Location + " %1 %2 %3 %4 %5 %6 %7 %8 %9");
        }
    }
}
