using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace EarthToRhino
{
    public class TileHandler
    {
        public TileHandler()
        {

        }

        public TileClusterDTO GetTileCluster(string partialUri)
        {
            // Check if the partialUri has the session
            Uri uri = new Uri(RoutesController.GetFullUri(partialUri));
            NameValueCollection session = HttpUtility.ParseQueryString(uri.Query);
            if (session["session"] != null)
            {
                WebAPI.SetSession(session["session"]);
            }

            string rootObject = WebAPI.GetFromPartialUri(partialUri);

            TileClusterDTO data = JsonConvert.DeserializeObject<TileClusterDTO>(rootObject);

            return data;
        }

        public bool TrySaveChild(ChildDTO child)
        {
            if (child.Content == null)
            {
                return false;
            }

            string uri = child.Content.Uri;

            

            if (string.IsNullOrEmpty(uri) || !uri.EndsWith(".glb"))
            {
                return false;
            }

            string filename = Utilities.GenerateHash(uri.Split('/').Last()) + ".glb";
            //string filename = Guid.NewGuid().ToString() + ".glb";

            string fullpath = Path.Combine(PathController.TempFolder, filename);
            return WebAPI.DownloadGLB(uri, fullpath);
        }

        
    }
}
