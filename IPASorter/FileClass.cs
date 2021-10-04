using System;
using System.Collections.Generic;
using System.Text;

namespace IPASorter
{
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
