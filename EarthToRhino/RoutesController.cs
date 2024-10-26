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
        public static string Root => $"{BaseUrl}/v1/3dtiles/root.json";
    }
}
