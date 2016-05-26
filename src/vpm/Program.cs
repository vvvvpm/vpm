using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PowerArgs;

namespace vpm
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("vpm 1.0 at your service!");
            try
            {
                Args.Parse<VpmArgs>(args);
            }
            catch (ArgException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ArgUsage.GenerateUsageFromTemplate<VpmArgs>());
                VpmUtils.CleanUp();
            }
            try
            {
                Console.WriteLine("Parsing input Pack");
                var vpxml = VpmUtils.ParseAndValidateXmlFile(Args.GetAmbientArgs<VpmArgs>().VPackFile);

                var namenode = vpxml.SelectSingleNode("/vpack/meta/name");
                var srcnode = vpxml.SelectSingleNode("/vpack/meta/source");
                var aliasesnode = vpxml.SelectSingleNode("/vpack/meta/aliases");

                if (namenode == null) throw new Exception("VPack name is not specified");
                if (srcnode == null) throw new Exception("VPack source is not specified");

                var name = namenode.InnerText.Trim();
                var src = srcnode.InnerText.Trim();

                Console.WriteLine("Input pack is " + name);

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
                    VpmUtils.CleanUp();
                    Environment.Exit(0);
                }
                var vpack = new VPack(name, src, aliaslist, vpxml);
                Console.WriteLine("Initialization complete.");
                vpack.Install();
                VpmUtils.CleanUp();
                Console.WriteLine("All good in theory.");
                Console.WriteLine("Enjoy!");
                Thread.Sleep(5000);
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong:");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                VpmUtils.CleanUp();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
                Environment.Exit(0);
            }
        }
    }
}
