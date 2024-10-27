using Newtonsoft.Json;
using Rhino.Geometry;
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
        public Rectangle3d Boundary { get; private set; }

        public Brep QueryVolume { get; private set; }

        public int MaxRecursionDepth { get; private set; }

        public List<string> DownloadedFilePaths { get; private set; }

        public List<ChildDTO> ChildrenToDownload { get; private set; }

        public TileHandler( Rectangle3d boundary)
        {
            ChildrenToDownload = new List<ChildDTO>();
            Boundary = boundary;
            DownloadedFilePaths = new List<string>();
        }

        public void SetRecursionDepth(int depth)
        {
            MaxRecursionDepth = depth;
        }

        public void DownloadAllChildren()
        {
            foreach (ChildDTO child in this.ChildrenToDownload)
            {
                TrySaveChild(child);
            }
        }

        public List<BoundingVolumeDTO> GetAllBoundingVolumes()
        {
            return this.ChildrenToDownload.Select(c => c.BoundingVolume).ToList();
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
            bool result = WebAPI.DownloadGLB(uri, fullpath);
            if (result) DownloadedFilePaths.Add(fullpath);
            return result;
        }

        public void UnpackTileRecursive(ChildDTO child, int recursionDepth)
        {
            // If max recursion depth is reached, add the child 
            // to the download list and return
            if (recursionDepth >= this.MaxRecursionDepth)
            {
                this.ChildrenToDownload.Add(child);
                return;
            }

            NextStepCondition nextStep = EvaluateNextStep(child);

            switch (nextStep)
            {
                case NextStepCondition.HasUnpackableChildren:
                    foreach (ChildDTO c in child.Children)
                    {
                        if (IsViableTile(c))
                        {
                            UnpackTileRecursive(c, recursionDepth + 1);
                        }
                    }
                    break;
                case NextStepCondition.NextIsJSON:
                    ChildDTO jsonChild = child.Children.First();

                    TileClusterDTO cluster = this.GetTileCluster(jsonChild.Content.Uri);

                    foreach (ChildDTO c in cluster.Root.Children)
                    {
                        if (IsViableTile(c))
                        {
                            UnpackTileRecursive(c, recursionDepth + 1);
                        }
                    }
                    break;
                case NextStepCondition.IsLeaf:
                    this.ChildrenToDownload.Add(child);
                    return;
            }
        }

        public bool IsViableTile(ChildDTO child)
        {
            // Check if the tile intersects with the boundary
            bool isInBoundary = GeoHelper.IsTileInBoundary(this.Boundary, child.BoundingVolume);

            return isInBoundary;
        }


        public NextStepCondition EvaluateNextStep(ChildDTO child)
        {
            if (child.Children == null || child.Children.Count == 0)
            {
                return NextStepCondition.IsLeaf;
            }

            if (child.Children.Any(c => c.Content != null && !string.IsNullOrEmpty(c.Content.Uri) && c.Content.Uri.Contains(".json")))
            {
                return NextStepCondition.NextIsJSON;
            }

            if (child.Children.Any(c => (c.Content != null && !string.IsNullOrEmpty(c.Content.Uri) && c.Content.Uri.EndsWith(".glb")) || (c.Children != null && c.Children.Count > 0)))
            {
                return NextStepCondition.HasUnpackableChildren;
            }

            return NextStepCondition.IsLeaf;
        }

        public enum NextStepCondition
        {
            HasUnpackableChildren = 0,
            NextIsJSON = 1,
            IsLeaf = 2
        }


    }
}
