using System;
using System.IO;
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
        public override void Validate(string name, ref string arg)
        {
            arg = arg.Trim('"');
            if (File.Exists(arg))
            {
                if (!arg.EndsWith(".vpack", true, null))
                {
                    throw new ValidationArgException("Specified file is not '.vpack'");
                }
                arg = Path.GetFullPath(arg);
            }
            else
            {
                throw new ValidationArgException("Pack file not Found.");
            }
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
        [ArgShortcut("-vvvv")]
        [IsVVVVExe]
        public string VVVVExe { get; set; }

        [ArgRequired(PromptIfMissing = true)]
        [ArgShortcut("-p")]
        [IsVPack]
        public string VPackFile { get; set; }

        [ArgShortcut("-q")]
        public bool Quiet { get; set; }
    }
}
