using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

        public static void ClearTempFolder()
        {
            string[] files = Directory.GetFiles(TempFolder, "*.glb");

            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);

                // Check if the filename length (without extension) is exactly 64 characters
                if (fileName.Length == 64)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }
    }
}
