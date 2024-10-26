using Rhino.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EarthToRhino
{
    public static class WebAPI
    {
        public static string ApiKey { get; private set; }
        public static string Session { get; private set; }
        private static readonly HttpClient client = new HttpClient();


        public static void SetApiKey(string apiKey)
        {
            ApiKey = apiKey;
        }

        public static void SetSession(string session)
        {
            Session = session;
        }

        public static bool DownloadGLB(string partialUri, string filePath)
        {
            try
            {
                string finalUri = RoutesController.GetFullUri(partialUri);

                HttpResponseMessage response = client.GetAsync(finalUri).Result;

                using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    response.Content.CopyToAsync(fileStream).Wait();
                }

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public static string GetFromPartialUri(string partialUri)
        {
            return Get(RoutesController.GetFullUri(partialUri), true);
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

            if (!RoutesController.IsRoot(url))
            {
                queryParams.Add(new KeyValuePair<string, string>("session", Session));
            }
            
            return Get(url, queryParams);
        }

        public static string Get(string url, List<KeyValuePair<string, string>> queryParams = null)
        {
            string finalUrl = url;

            if (queryParams != null)
            {
                if (!finalUrl.Contains("?"))
                {
                    finalUrl += "?";
                }

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
