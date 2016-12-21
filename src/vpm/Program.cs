using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using PowerArgs;

namespace vpm
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("vpm at your service!");
            Console.ResetColor();
            try
            {
                Args.Parse<VpmArgs>(args);
            }
            catch (ArgException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                Console.WriteLine(ArgUsage.GenerateUsageFromTemplate<VpmArgs>());
                VpmUtils.CleanUp();
            }
            try
            {
                UpdateTimer.Instance.Reset();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Parsing input Pack");
                Console.ResetColor();
                var vpxml = VpmUtils.ParseAndValidateXmlFile(Args.GetAmbientArgs<VpmArgs>().VPackFile);

                var namenode = vpxml.SelectSingleNode("/vpack/meta/name");
                var srcnode = vpxml.SelectSingleNode("/vpack/meta/source");
                var aliasesnode = vpxml.SelectSingleNode("/vpack/meta/aliases");

                if (namenode == null) throw new Exception("VPack name is not specified");

                var name = namenode.InnerText.Trim();

                var src = "";
                if(srcnode != null) src = srcnode.InnerText.Trim();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Input pack is " + name);
                Console.ResetColor();

                List<string> aliaslist = null;
                if (aliasesnode != null)
                {
                    var antext = aliasesnode.InnerText;
                    aliaslist = antext.Split(',').ToList();
                    for (int j = 0; j < aliaslist.Count; j++)
                    {
                        aliaslist[j] = aliaslist[j].Trim();
                    }
                }
                string matchalias;
                if (VpmUtils.IsPackExisting(name, aliaslist, out matchalias))
                {
                    Console.WriteLine("Input pack seems to be already existing as " + matchalias);
                    if (VpmUtils.PromptYayOrNay("Do you want to overwrite?"))
                    {
                        var packdir = Path.Combine(Args.GetAmbientArgs<VpmArgs>().VVVVDir, "packs", matchalias);
                        VpmUtils.DeleteDirectory(packdir, true);
                    }
                    else
                    {
                        VpmUtils.CleanUp();
                        Environment.Exit(0);
                    }
                }
                var vpack = new VPack(name, src, aliaslist, vpxml);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Initialization complete.");
                Console.ResetColor();

                vpack.Install();
                VpmUtils.CleanUp();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("All good in theory.");
                Console.WriteLine("Enjoy!");
                Thread.Sleep(5000);
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong:");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(e.StackTrace);
                VpmUtils.CleanUp();
                Console.ResetColor();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
                Environment.Exit(0);
            }
        }
    }
}
