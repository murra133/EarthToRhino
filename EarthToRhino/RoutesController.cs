using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthToRhino
{
    public static class RoutesController
    {
        public static string BaseUrl => "https://tile.googleapis.com";
        public static string Root => $"/v1/3dtiles/root.json";

        public static bool IsRoot(string uri)
        {
            return uri.Contains(Root);
        }

        public static string GetFullUri(string partialUri)
        {
            return $"{BaseUrl}{partialUri}";
        }
    }
}
