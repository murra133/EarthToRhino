using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace EarthToRhino
{
    public class ResponseBodyDTO
    {
        [JsonProperty("error")]
        public ErrorDTO Error { get; set; }
        public class ErrorDTO
        {
            [JsonProperty("message")]
            public string Message { get; set; }
        }
    }
}
