using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using static EarthToRhino.GeoHelper;

namespace EarthToRhino
{
    public static class Utilities
    {
        public static string CATEGORY_NAME = "Earth To Rhino";
        public static string SUBCATEGORY_NAME = "Tiles";

        public static string GenerateHash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Convert the input string to a byte array
                byte[] bytes = Encoding.UTF8.GetBytes(input);

                // Compute the hash
                byte[] hashBytes = sha256.ComputeHash(bytes);

                // Convert the byte array to a hexadecimal string
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }

        public static GH_Structure<GH_Point> PointsToDataTree(List<Point3d> points)
        {
            GH_Structure<GH_Point> dataTree = new GH_Structure<GH_Point>();
            GH_Path path = new GH_Path(0);
            foreach (var pt in points)
            {
                dataTree.Append(new GH_Point(pt), path);
            }
            return dataTree;
        }

    }
}
