using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EarthToRhino
{
    public static class WebAPI
    {
        public static string ApiKey { get; private set; }
        private static readonly HttpClient client = new HttpClient();


        public static void SetApiKey(string apiKey)
        {
            ApiKey = apiKey;
        }

        public static string Get(string url, bool authenticated)
        {
            List<KeyValuePair<string, string>> queryParams = null;
            
            if (authenticated)
            {
                queryParams = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("key", ApiKey)
                };
            }
            
            return Get(url, queryParams);
        }

        public static string Get(string url, List<KeyValuePair<string, string>> queryParams = null)
        {
            string finalUrl = url;

            if (queryParams != null)
            {
                finalUrl += "?";
                foreach (KeyValuePair<string, string> queryParam in queryParams)
                {
                    finalUrl += $"&{queryParam.Key}={queryParam.Value}";
                }
            }

            HttpResponseMessage response = client.GetAsync(finalUrl).Result;
            string responseBody = response.Content.ReadAsStringAsync().Result;

            return responseBody;

        }


    }
}
