using PlistCS;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
            if(Directory.Exists("./sortertemp"))
            {
                Directory.Delete("./sortertemp", true);
            }

            // parse filepath if given
            string argsFilePath = args.Length != 0 ? args[0] : "./";
            if (!argsFilePath.EndsWith("/")) argsFilePath += "/";

            // run steps
            FileScanner(argsFilePath);
            // MD5Eliminator();  obsolete
            InfoPlistRenamer(argsFilePath);
            SortByiOSCompatibility(argsFilePath);
            GenerateJson();

            Console.WriteLine("complete :)");
        }

        // step 1
        static void FileScanner(string path)
        {
            Console.WriteLine("Scanning subdirectories for IPA files");
            List<string> tmp = Directory.GetFiles(path, "*.ipa", SearchOption.AllDirectories).ToList();
            foreach (string s in tmp)
            {
                Console.WriteLine($"Found {s}");
                files.Add(new IPAFile
                {
                    fileName = s.Split('/')[s.Split('/').Length -1],
                    path = s,
                    md5sum = CalculateMD5(s)
                }) ;
            }
        }

        // step 2
        static void InfoPlistRenamer(string path)
        {
            Directory.CreateDirectory("./sortertemp");
            Directory.CreateDirectory($"{path}/incomplete");
            foreach (IPAFile i in files)
            {
                Console.WriteLine($"fixing name of {i.fileName}");

                // extract ipa
                Directory.CreateDirectory($"./sortertemp/{i.fileName}");
                ZipFile.ExtractToDirectory(i.path, $"./sortertemp/{i.fileName}");
                // parse plist
                Dictionary<string, object> plist = new Dictionary<string, object>();
                try
                {
                    string appPath = $"./sortertemp/{i.fileName}/Payload/{Directory.GetDirectories($"./sortertemp/{i.fileName}/Payload/")[0].Split('/')[Directory.GetDirectories($"./sortertemp/{i.fileName}/Payload/")[0].Split('/').Length - 1]}";
                    plist = (Dictionary<string, object>)Plist.readPlist(appPath + "/Info.plist");
                }
                catch(Exception)
                {
                    Console.WriteLine($"{i.fileName} has a missing/damaged Info.plist. moving to the broken directory...");
                    File.Move(i.path, $"{path}/incomplete/{i.fileName.Replace(".ipa", $"-{i.md5sum}.ipa")}", true);
                    i.path = $"{path}/incomplete/{i.fileName.Replace(".ipa", $"-{i.md5sum}.ipa")}";
                    continue;
                }
                Directory.Delete($"./sortertemp/{i.fileName}", true);
                i.CFBundleIdentifier = plist["CFBundleIdentifier"].ToString();
                try
                {
                    i.CFBundleDisplayName = RemoveIllegalFileNameChars(plist["CFBundleDisplayName"].ToString());
                }
                catch (KeyNotFoundException)
                {
                    i.CFBundleDisplayName = i.CFBundleIdentifier.Split('.')[2];
                }
                string whichToUse = "CFBundleVersion";
                if (plist["CFBundleVersion"].ToString() == "1")
                {
                    whichToUse = "CFBundleShortVersionString";
                }
                i.CFBundleVersion = plist[whichToUse].ToString();
                try
                {
                    i.MinimumOSVersion = plist["MinimumOSVersion"].ToString();
                }
                catch (KeyNotFoundException)
                {
                    i.MinimumOSVersion = "2.0";
                }

                // rename file
                string newFileName = $"{i.CFBundleDisplayName}-({i.CFBundleIdentifier})-{i.CFBundleVersion}-(iOS_{i.MinimumOSVersion})-{i.md5sum}.ipa";
                File.Move(i.path, i.path.Replace(i.fileName, newFileName), true);
                i.path = i.path.Replace(i.fileName, newFileName);
                i.fileName = newFileName;
            }
            Directory.Delete("./sortertemp", true);
        }

        // step 3
        static void SortByiOSCompatibility(string path)
        {
            Console.WriteLine("Sorting apps by minimum iOS version");

            foreach(IPAFile i in files)
            {
                if (i.path == "DO NOT ENUMERATE") continue;
                Directory.CreateDirectory($"{path}/iOS{i.MinimumOSVersion.Split('.')[0]}/{i.CFBundleIdentifier}");
                File.Move(i.path, $"{path}/iOS{i.MinimumOSVersion.Split('.')[0]}/{i.CFBundleIdentifier}/{i.fileName}", true);
                i.path = $"{path}/iOS{i.MinimumOSVersion.Split('.')[0]}/{i.CFBundleIdentifier}/{i.fileName}";
            }
        }

        // step 4
        static void GenerateJson()
        {
            AppList apps = new AppList();
            apps.apps = files.ToArray();
            string appsJson = JsonSerializer.Serialize(apps);
            File.WriteAllText("./apps.json", appsJson);
        }

        // other stuff (hate)
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

        public static string RemoveIllegalFileNameChars(string input, string replacement = "")
        {
            var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(input, replacement);
        }
    }
}

