using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthToRhino
{
    public static class PathController
    {
        public static string TempFolder { get; private set; }

        public static void SetTempFolder(string tempFolder)
        {
            TempFolder = tempFolder;
            EnsureDirectory(TempFolder);
        }

        public static void EnsureDirectory(string path)
        {
            string directory = System.IO.Path.GetDirectoryName(path);

            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
        }
    }
}
