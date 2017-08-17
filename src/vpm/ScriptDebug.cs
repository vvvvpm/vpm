using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace vpm
{
    public static class ScriptDebug
    {
        public static Action<VpmGlobals> SelectedDebugScript = GetLatestGithubReleaseViaWebclient;

        public static void GetLatestGithubReleaseViaWebclient(VpmGlobals vpmg)
        {
            /*
            var baseurl = "https://api.github.com";
            var initreguest = string.Format("/repos/{0}/{1}/releases", "mrvux", "dx11-vvvv");
            Console.WriteLine(baseurl + initreguest);
            var filename = string.Format("vvvv-packs-dx11-{0}.zip", vpmg.VVVV.Architecture);

            var client = new WebClient();
            client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            Console.WriteLine("Fetching releases from github");

            var stream = client.OpenRead(baseurl + initreguest);
            var reader = new StreamReader(stream).ReadToEnd();
            var jsondata = JArray.Parse(reader);
            var jsonres = jsondata.Children().ToArray()[0];
            var asset = jsonres["assets"].Where(a => a["name"].ToString() == filename).ToArray()[0];

            Console.WriteLine("Downloading {0} from {1}", filename, jsonres["tag_name"]);

            vpmg.Download(
                asset["browser_download_url"].ToString(),
                Path.Combine(vpmg.VPM.TempDir, filename)
            );
            vpmg.Extract(Path.Combine(vpmg.VPM.TempDir, filename), vpmg.Pack.TempDir);
            vpmg.CopyDir(
                vpmg.Pack.TempDir,
                vpmg.VVVV.Dir
            );
            */
        }
    }
}
