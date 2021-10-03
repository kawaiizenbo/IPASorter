using PlistCS;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace IPASorter
{
    class Program
    {
        // renaming format: "com.bundle.id-1.0-(iOS4.3).ipa"

        public static List<IPAFile> files = new List<IPAFile>();
        static void Main(string[] args)
        {
            Console.WriteLine("IPASorter by KawaiiZenbo");
            if(Directory.Exists(".\\sortertemp"))
            {
                Directory.Delete(".\\sortertemp", true);
            }

            // parse filepath if given
            string argsFilePath = args.Length != 0 ? args[0] : "./";
            if (!argsFilePath.EndsWith("/") || !argsFilePath.EndsWith("\\")) argsFilePath += "/";

            // run steps
            FileScanner(argsFilePath);
            // MD5Eliminator();  obsolete
            InfoPlistRenamer();

            Console.WriteLine("complete :)");
        }

        // step 1
        static void FileScanner(string path)
        {
            List<string> tmp = Directory.GetFiles(path, "*.ipa", SearchOption.AllDirectories).ToList();
            foreach (string s in tmp)
            {
                files.Add(new IPAFile
                {
                    fileName = s.Split('/')[s.Split('/').Length -1].Split('\\')[s.Split('/')[s.Split('/').Length - 1].Split('\\').Length - 1],
                    path = s,
                    md5sum = CalculateMD5(s)
                }) ;
            }
        }

        // step 2
        static void InfoPlistRenamer()
        {
            Directory.CreateDirectory(".\\sortertemp");
            foreach (IPAFile i in files)
            {
                // extract ipa
                Directory.CreateDirectory($".\\sortertemp\\{i.fileName}");
                ZipFile.ExtractToDirectory(i.path, $".\\sortertemp\\{i.fileName}");
                string plistpath = $".\\sortertemp\\{i.fileName}\\Payload\\{Directory.GetDirectories($".\\sortertemp\\{i.fileName}\\Payload\\")[0].Split('\\')[Directory.GetDirectories($".\\sortertemp\\{i.fileName}\\Payload\\")[0].Split('\\').Length - 1]}\\Info.plist";
                Dictionary<string, object> plist = (Dictionary<string, object>)Plist.readPlist(plistpath);
                Directory.Delete($".\\sortertemp\\{i.fileName}", true);
                i.CFBundleIdentifier = plist["CFBundleIdentifier"].ToString();
                i.CFBundleVersion = plist["CFBundleVersion"].ToString();
                i.MinimumOSVersion = plist["MinimumOSVersion"].ToString();
                File.Move(i.path, i.path.Replace(i.fileName, $"{plist["CFBundleIdentifier"]}-{plist["CFBundleVersion"]}-(iOS_{plist["MinimumOSVersion"]}).ipa"));
                i.path = i.path.Replace(i.fileName, $"{plist["CFBundleIdentifier"]}-{plist["CFBundleVersion"]}-(iOS_{plist["MinimumOSVersion"]}).ipa");
                i.fileName = $"{plist["CFBundleIdentifier"]}-{plist["CFBundleVersion"]}-(iOS_{plist["MinimumOSVersion"]}).ipa";
            }
            Directory.Delete(".\\sortertemp", true);
        }

        // step 3???
        static void Sort()
        {

        }

        // keeping this around just in case
        static string CalculateMD5(string fileName)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(fileName))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}

