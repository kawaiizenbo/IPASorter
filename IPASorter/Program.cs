using PlistCS;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IPASorter
{
    class Program
    {
        // renaming format: "com.bundle.id-1.0-(iOS4.3)-md5.ipa"

        public static List<IPAFile> files = new List<IPAFile>();
        public static List<string> problematics = new List<string>();
        public static Stopwatch LocalWatch = new Stopwatch();
        public static Stopwatch GlobalWatch = new Stopwatch();
        public static TimeSpan FSElapsedTime;
        public static TimeSpan IPElapsedTime;
        public static TimeSpan SFElapsedTime;
        static void Main(string[] args)
        {
            Console.WriteLine("IPASorter by KawaiiZenbo");
            // start timer
            GlobalWatch.Restart();

            // parse filepath if given
            string argsFilePath = args.Length != 0 ? args[0] : "./";
            if (!argsFilePath.EndsWith("/")) argsFilePath += "/";
            if (Directory.Exists($"./{argsFilePath}/sortertemp"))
            {
                Directory.Delete($"./{argsFilePath}/sortertemp", true);
            }
            Console.WriteLine($"Using path \"{argsFilePath}\"");
            // run steps
            LocalWatch.Restart();
            FileScanner(argsFilePath);
            LocalWatch.Restart();
            InfoPlistRenamer(argsFilePath);
            LocalWatch.Restart();
            SortByiOSCompatibility(argsFilePath);
            GlobalWatch.Stop();

            Console.WriteLine("Generating apps JSON");
            AppList apps = new AppList();
            apps.apps = files.ToArray();
            string appsJson = JsonSerializer.Serialize(apps);
            File.WriteAllText($"{argsFilePath}/apps.json", appsJson);

            Console.WriteLine("complete :)");
            string timeData = $"Total elapsed time (hh:mm:ss): {GlobalWatch.Elapsed}\n" +
                $"FileScanner: {FSElapsedTime}\n" +
                $"InfoPlistRenamer: {IPElapsedTime}\n" +
                $"SortByiOSCompatibility: {SFElapsedTime}\n";
            Console.WriteLine(timeData);
            File.WriteAllText($"{argsFilePath}/timeData.txt", timeData);
        }

        // step 1
        static void FileScanner(string path)
        {
            Console.WriteLine("Scanning subdirectories for IPA files");
            List<string> tmp = Directory.GetFiles(path, "*.ipa", SearchOption.AllDirectories).ToList();
            foreach (string s in tmp)
            {
                Console.WriteLine($"Found {s}");
                try
                {
                    string smd5 = CalculateMD5(s);
                    if (smd5.StartsWith("ERROR: ")) throw new IOException();
                    files.Add(new IPAFile
                    {
                        fileName = Path.GetFileName(s),
                        path = s,
                        md5sum = smd5
                    });
                }
                catch 
                {
                    Console.WriteLine($"{s} was unable to be added");
                }
            }
            FSElapsedTime = LocalWatch.Elapsed;
        }

        // step 2
        static void InfoPlistRenamer(string path)
        {
            Directory.CreateDirectory("./sortertemp");
            Directory.CreateDirectory($"{path}/incomplete");
            foreach (IPAFile i in files)
            {
                try
                {
                    Console.WriteLine($"fixing name of {i.fileName}");

                    // extract ipa
                    Directory.CreateDirectory($"./sortertemp/{i.fileName}");
                    ZipFile.ExtractToDirectory(i.path, $"./sortertemp/{i.fileName}");
                    // parse plist
                    Dictionary<string, object> plist = new Dictionary<string, object>();
                    string appPath = $"./sortertemp/{i.fileName}/Payload/{Path.GetFileName(Directory.GetDirectories($"./sortertemp/{i.fileName}/Payload/")[0])}";
                    plist = (Dictionary<string, object>)Plist.readPlist(appPath + "/Info.plist");
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
                    if (i.CFBundleDisplayName.Trim() == "")
                    {
                        i.CFBundleDisplayName = i.CFBundleIdentifier.Split('.')[2];
                    }
                    i.CFBundleVersion = plist["CFBundleVersion"].ToString();
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
                catch (Exception)
                {
                    Console.WriteLine($"{i.fileName} is damaged. moving to the broken directory...");
                    File.Move(i.path, $"{path}/incomplete/{i.fileName.Replace(".ipa", $"-{i.md5sum}.ipa")}", true);
                    i.path = $"{path}/incomplete/{i.fileName.Replace(".ipa", $"-{i.md5sum}.ipa")}";
                    i.MinimumOSVersion = "DO NOT ENUMERATE";
                    Directory.Delete($"./sortertemp/{i.fileName}", true);
                    continue;
                }
            }
            Directory.Delete("./sortertemp", true);
            IPElapsedTime = LocalWatch.Elapsed;
        }

        // step 3
        static void SortByiOSCompatibility(string path)
        {
            Console.WriteLine("Sorting apps by minimum iOS version");

            foreach(IPAFile i in files)
            {
                try
                {
                    if (i.MinimumOSVersion == "DO NOT ENUMERATE") continue;
                    string newPath = $"{path}/iOS-{i.MinimumOSVersion.Split('.')[0]}/{i.CFBundleIdentifier}";
                    Directory.CreateDirectory(newPath);
                    File.Move(i.path, newPath + $"/{Path.GetFileName(i.path)}", true);
                    i.path = newPath + $"/{Path.GetFileName(i.path)}";
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Couldnt move {i.path}: {e.Message}");
                }
            }
            SFElapsedTime = LocalWatch.Elapsed;
        }

        // other stuff (hate)
        static string CalculateMD5(string fileName)
        {
            try
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
            catch (Exception ex)
            {
                return $"ERROR: {ex.Message}";
            }
        }

        public static string RemoveIllegalFileNameChars(string input, string replacement = "")
        {
            var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(input, replacement);
        }
    }

    public class IPAFile
    {
        public string path { get; set; }
        public string md5sum { get; set; }
        public string fileName { get; set; }
        public string CFBundleIdentifier { get; set; }
        public string CFBundleVersion { get; set; }
        public string MinimumOSVersion { get; set; }
        public string CFBundleDisplayName { get; set; }
    }

    public class AppList
    {
        public IPAFile[] apps { get; set; }
    }
}

