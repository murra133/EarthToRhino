using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthToRhino
{
    public class TileClusterDTO
    {
        [JsonProperty("geometricError")]
        public double GeometricError { get; set; }
        [JsonProperty("root")]
        public ChildDTO Root { get; set; }
    }
}
