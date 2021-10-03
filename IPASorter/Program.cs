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
            string argsFilePath = args.Length != 0 ? args[0] : ".\\";
            if (!argsFilePath.EndsWith("/") || !argsFilePath.EndsWith("\\")) argsFilePath += "/";

            // run steps
            FileScanner(argsFilePath);
            // MD5Eliminator();  obsolete
            InfoPlistRenamer();
            if(args.Length > 1 && args[1] == "-si")
            {
                SortByiOSCompatibility();
            }

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
                Console.WriteLine($"fixing name of {i.fileName}");

                // extract ipa
                Directory.CreateDirectory($".\\sortertemp\\{i.fileName}");
                ZipFile.ExtractToDirectory(i.path, $".\\sortertemp\\{i.fileName}");
                string appPath = $".\\sortertemp\\{i.fileName}\\Payload\\{Directory.GetDirectories($".\\sortertemp\\{i.fileName}\\Payload\\")[0].Split('\\')[Directory.GetDirectories($".\\sortertemp\\{i.fileName}\\Payload\\")[0].Split('\\').Length - 1]}";

                // parse plist
                Dictionary<string, object> plist = (Dictionary<string, object>)Plist.readPlist(appPath + "\\Info.plist");
                Directory.Delete($".\\sortertemp\\{i.fileName}", true);
                i.CFBundleIdentifier = plist["CFBundleIdentifier"].ToString();
                i.CFBundleVersion = plist["CFBundleVersion"].ToString();
                i.MinimumOSVersion = plist["MinimumOSVersion"].ToString();

                // rename file
                string newFileName = $"{plist["CFBundleIdentifier"]}-{plist["CFBundleVersion"]}-(iOS_{plist["MinimumOSVersion"]})-{i.md5sum}.ipa";
                File.Move(i.path, i.path.Replace(i.fileName, newFileName), true);
                i.path = i.path.Replace(i.fileName, newFileName);
                i.fileName = newFileName;
            }
            Directory.Delete(".\\sortertemp", true);
        }

        // optional step 3
        static void SortByiOSCompatibility()
        {

        }

        // othet stuff
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

