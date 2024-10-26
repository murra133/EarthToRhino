using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthToRhino
{
    public class BoundingVolumeDTO
    {
        [JsonProperty("box")]
        public List<double> Box { get; set; }
    }
}
