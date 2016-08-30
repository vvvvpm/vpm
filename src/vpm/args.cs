using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using BrendanGrant.Helpers.FileAssociation;
using Microsoft.Win32;
using PowerArgs;

namespace vpm
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class VVVVPathValidator : ArgValidator
    {
        public override bool ImplementsValidateAlways => true;

        public override void ValidateAlways(CommandLineArgument argument, ref string arg)
        {
            if (arg != null)
            {
                arg = arg.Trim('"');
                arg = Path.GetFullPath(arg);
                if (Directory.Exists(arg) || File.Exists(arg))
                {
                    var dstattr = File.GetAttributes(arg);
                    if (!dstattr.HasFlag(FileAttributes.Directory))
                    {
                        arg = Path.GetDirectoryName(arg);
                    }
                }
                else
                {
                    Console.WriteLine("Destination directory doesn't exist. Creating it.");
                    try
                    {
                        Directory.CreateDirectory(arg);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Couldn't do that.");
                        throw;
                    }
                }
            }
            else
            {
                var regkey = (string)Registry.GetValue("HKEY_CLASSES_ROOT\\VVVV\\Shell\\Open\\Command", "", "");
                if (string.IsNullOrWhiteSpace(regkey))
                {
                    arg = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                }
                else
                {
                    var exepath = regkey.Split(' ')[0].Replace("\"", "");
                    Console.WriteLine("Found a VVVV in registry.");
                    arg = Path.GetDirectoryName(Path.GetFullPath(exepath));
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class IsVPack : ArgValidator
    {
        public override bool ImplementsValidateAlways => true;
        public override void ValidateAlways(CommandLineArgument argument, ref string arg)
        {
            if (string.IsNullOrEmpty(arg))
            {
                var exeName = Assembly.GetExecutingAssembly().Location;
                if (VpmUtils.PromptYayOrNay(
                        "Do you want to register this vpm instance? (Open vpm:// or vpms:// url's and open .vpack files)",
                        "It makes life so much easier."))
                {
                    try
                    {
                        VpmUtils.RegisterURIScheme("vpm");
                        VpmUtils.RegisterURIScheme("vpms");

                        var fai = new FileAssociationInfo(".vpack");
                        if (!fai.Exists)
                        {
                            fai.Create("vpm");
                            fai.ContentType = "text/vpack";
                        }
                        var pai = new ProgramAssociationInfo(fai.ProgID);
                        var progverb = new ProgramVerb("Open", exeName + " %1");
                        if (pai.Exists)
                        {
                            foreach (var pv in pai.Verbs)
                            {
                                pai.RemoveVerb(pv);
                            }
                            pai.AddVerb(progverb);
                        }
                        else
                        {
                            pai.Create("VVVV Package Definition", progverb);
                            pai.DefaultIcon = new ProgramIcon(exeName);
                        }

                        Console.WriteLine("Registered protocols successfully");
                    }
                    catch (Exception)
                    {
                        if (VpmUtils.PromptYayOrNay("Can't write to registry. Retry as Admin?"))
                        {
                            try
                            {
                                var startInfo = new ProcessStartInfo(exeName)
                                {
                                    Arguments = "-RegisterVpmUri",
                                    Verb = "runas"
                                };
                                Process.Start(startInfo);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Error occured while trying to run elevated process.");
                                Thread.Sleep(5000);
                            }
                            Environment.Exit(0);
                        }
                    }
                }

                Console.WriteLine("Alright, enjoy!");
                Thread.Sleep(5000);
                Environment.Exit(0);
            }
            arg = arg.Trim('"');
            if (arg.StartsWith("vpm://", true, CultureInfo.InvariantCulture))
            {
                if (arg.EndsWith(".vpack", true, CultureInfo.InvariantCulture))
                {
                    return;
                }
            }
            if (arg.StartsWith("vpms://", true, CultureInfo.InvariantCulture))
            {
                if (arg.EndsWith(".vpack", true, CultureInfo.InvariantCulture))
                {
                    return;
                }
            }
            if (File.Exists(arg))
            {
                if (arg.EndsWith(".vpack", true, CultureInfo.InvariantCulture))
                {
                    arg = Path.GetFullPath(arg);
                    return;
                }
            }
            throw new ValidationArgException("File not found or file is not .vpack");
        }
    }

    public class VpmArgs
    {
        [ArgDescription("Specify VVVV exe location. If not specified vpm will attempt to read location from Windows Registry.")]
        [ArgPosition(1)]
        [ArgShortcut("-vvvv")]
        [VVVVPathValidator]
        public string VVVVDir { get; set; }

        [ArgDescription(
@"The .vpack file specifying pack to be installed.
Note: default first argument without -p is this argument.
vpm(s):// URL's are coming here too")]
        [ArgPosition(0)]
        [ArgShortcut("-p")]
        [IsVPack]
        public string VPackFile { get; set; }

        [ArgDescription("Not asking questions.")]
        [ArgShortcut("-q")]
        public bool Quiet { get; set; }
    }
}
