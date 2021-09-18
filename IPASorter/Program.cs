using System;
using System.Collections.Generic;
using System.IO;
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
            // parse filepath if given
            string argsFilePath = args.Length != 0 ? args[0] : "./";
            if (!argsFilePath.EndsWith("/") || !argsFilePath.EndsWith("\\")) argsFilePath += "/";

            // create temp dir
            if (Directory.Exists("%appdata%/IPASorterTemp/")) Directory.CreateDirectory("%appdata%/IPASorterTemp/");

            // run steps
            FileScanner(argsFilePath);
            MD5Eliminator();

            //done
            if (Directory.Exists("%appdata%/IPASorterTemp/")) Directory.Delete("%appdata%/IPASorterTemp/");
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
        static void MD5Eliminator()
        {
            foreach (IPAFile i in files)
            {
                Console.WriteLine($"checking against {i.path} ({i.md5sum})");
            }
        }

        // step 3
        static void InfoPlistEliminator()
        {

        }

        // step 4
        static void InfoPlistRenamer()
        {
            foreach (IPAFile i in files)
            {
                File.Move(i.path, i.path.Replace(i.fileName, ""));
            }
        }

        // step 5???
        static void Sort()
        {

        }

        // misc functions
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

