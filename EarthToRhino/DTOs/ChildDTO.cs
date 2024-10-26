using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthToRhino
{
    public class ChildDTO
    {
        [JsonProperty("boundingVolume")]
        public BoundingVolumeDTO BoundingVolume { get; set; }
        [JsonProperty("geometricError")]
        public double GeometricError { get; set; }
        [JsonProperty("refine")]
        public string Refine { get; set; }
        [JsonProperty("content")]
        public ContentDTO Content { get; set; }
        [JsonProperty("children")]
        public List<ChildDTO> Children { get; set; }
        [JsonProperty("extras")]
        public ExtrasDTO Extras { get; set; }
    }

    public class ContentDTO
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }
    }

    public class ExtrasDTO
    {
        [JsonProperty("comment")]
        public string Comment { get; set; }
    }
}
