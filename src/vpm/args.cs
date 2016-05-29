using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.Win32;
using PowerArgs;

namespace vpm
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class IsVVVVExe : ArgValidator
    {
        public override bool ImplementsValidateAlways => true;

        public override void ValidateAlways(CommandLineArgument argument, ref string arg)
        {
            if (arg != null)
            {
                arg = arg.Trim('"');
                if (File.Exists(arg))
                {
                    if (!arg.EndsWith("vvvv.exe", true, null))
                    {
                        throw new ValidationArgException("Specified file is not an instance of vvvv.");
                    }
                    arg = Path.GetFullPath(arg);
                }
                else
                {
                    throw new ValidationArgException("VVVV not Found.");
                }
            }
            else
            {
                Console.WriteLine("An instance of VVVV was not specified, looking for one in registry.");
                var regkey = (string)Registry.GetValue("HKEY_CLASSES_ROOT\\VVVV\\Shell\\Open\\Command", "", "");
                if (regkey == "")
                {
                    throw new ValidationArgException(
                        "VVVV was not found in registry.\nPlease register a VVVV with setup.exe or specify vvvv.exe with the '-vvvv' argument.");
                }
                var exepath = regkey.Split(' ')[0].Replace("\"", "");
                arg = Path.GetFullPath(exepath);
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
                if (VpmUtils.PromptYayOrNay(
                        "Do you want to make this vpm instance to open vpm:// or vpms:// url's for downloading .vpack files?"))
                {

                    try
                    {
                        VpmUtils.RegisterURIScheme("vpm");
                        VpmUtils.RegisterURIScheme("vpms");
                        Console.WriteLine("Registered protocols successfully");
                    }
                    catch (Exception)
                    {
                        if (VpmUtils.PromptYayOrNay("Can't write to registry. Retry as Admin?"))
                        {
                            try
                            {
                                var exeName = Process.GetCurrentProcess().MainModule.FileName;
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

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class QuietValidator : ArgValidator
    {
        public override bool ImplementsValidateAlways => true;

        public override void ValidateAlways(CommandLineArgument argument, ref string arg)
        {
            if (arg != null)
            {
                if (File.Exists(arg))
                {
                    if (!arg.EndsWith("vvvv.exe", true, null))
                    {
                        throw new ValidationArgException("Specified file is not an instance of vvvv.");
                    }
                    arg = Path.GetFullPath(arg);
                }
                else
                {
                    throw new ValidationArgException("VVVV not Found.");
                }
            }
            else
            {
                Console.WriteLine("An instance of VVVV was not specified, looking for one in registry.");
                var regkey = (string)Registry.GetValue("HKEY_CLASSES_ROOT\\VVVV\\Shell\\Open\\Command", "", "");
                if (regkey == "")
                {
                    throw new ValidationArgException(
                        "VVVV was not found in registry.\nPlease register a VVVV with setup.exe or specify vvvv.exe with the '-vvvv' argument.");
                }
                var exepath = regkey.Split(' ')[0].Replace("\"", "");
                arg = Path.GetFullPath(exepath);
            }
        }
    }

    public class VpmArgs
    {
        [ArgDescription("Specify VVVV exe location. If not specified vpm will attempt to read location from Windows Registry.")]
        [ArgShortcut("-vvvv")]
        [IsVVVVExe]
        public string VVVVExe { get; set; }

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
