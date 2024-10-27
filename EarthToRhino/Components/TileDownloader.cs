using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using static Rhino.Runtime.ViewCaptureWriter;
using System.Numerics;
using static EarthToRhino.GeoHelper;
using Rhino.DocObjects;

namespace EarthToRhino.Components
{
    public class TileDownloader : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TileDownloader class.
        /// </summary>
        public TileDownloader()
          : base("Tile Downloader", "TD",
              "Download Cesium 3D Tiles through API connection.",
              Utilities.CATEGORY_NAME, Utilities.SUBCATEGORY_NAME)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Level of Detail", "D", "The level of detail (expressed as recursion depth)", GH_ParamAccess.item);
            pManager.AddRectangleParameter("Boundary", "B", "The boundary of the area to download", GH_ParamAccess.item);
            pManager.AddTextParameter("Temp Folder", "F", "The temporary folder to store the tiles", GH_ParamAccess.item);
            pManager.AddTextParameter("API Key", "K", "The API key", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Clear Temp Folder", "C", "Clear the temp folder before downloading", GH_ParamAccess.item, true);

            pManager[4].Optional = true;
        }

        /// <summary>
        /// Override the Help Description so we can make a link to the Google Terms of Use and Privaty Policy
        /// </summary>
        protected override string HelpDescription =>
            base.Description + "<br>" +
            "This component utilizes the Google Map Tiles API.<br>" +
            "By using this application you are bound to their <a href=\"https://cloud.google.com/maps-platform/terms/\">Terms of Use</a> <br>" +
            "and their <a href=\"https://policies.google.com/privacy\">Privacy Policy</a>.";

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("BBoxes", "BB", "All bounding boxes", GH_ParamAccess.list);
            pManager.AddPointParameter("Query Point ECEF", "QPE", "Query point in ECEF coordinates", GH_ParamAccess.item);
            pManager.AddTextParameter("Loaded Files", "LF", "Lists of all loaded files", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int levelOfDetail = 0;
            Rectangle3d boundary = new Rectangle3d();
            string tempFolder = "";
            string apiKey = "";
            bool clearTempFolder = true;


            DA.GetData(1, ref boundary);
            DA.GetData(2, ref tempFolder);
            DA.GetData(3, ref apiKey);
            DA.GetData(4, ref clearTempFolder);

            if (string.IsNullOrEmpty(apiKey))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "API Key is missing");
                return;
            }

            if (string.IsNullOrEmpty(tempFolder))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Temp Folder is missing");
                return;
            }

            PathController.SetTempFolder(tempFolder);
            WebAPI.SetApiKey(apiKey);

            if (clearTempFolder)
            {
                PathController.ClearTempFolder();
            }

            TileHandler tileHandler = new TileHandler(boundary);
 

            if (DA.GetData(0, ref levelOfDetail))
            {
                tileHandler.SetRecursionDepth(levelOfDetail);
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Level of Detail is missing");
                return;
            }


            TileClusterDTO rootCluster = tileHandler.GetTileCluster(RoutesController.Root);

            tileHandler.UnpackTileRecursive(rootCluster.Root, 0);

            tileHandler.DownloadAllChildren();

            List<BoundingVolumeDTO> bboxes = tileHandler.GetAllBoundingVolumes();

            // Output the query point for visualization
            // Convert the boundary center to ECEF for visualization
            Point3d queryPoint = boundary.Center;
            Point3d queryPointECEFPoint = GeoHelper.ModelPointToECEF(queryPoint);


            // Output bounding boxes
            GH_Structure<GH_Number> dataTree = new GH_Structure<GH_Number>();

            for (int i = 0; i < bboxes.Count; i++)
            {
                GH_Path path = new GH_Path(i);
                BoundingVolumeDTO dto = bboxes[i];

                foreach (double num in dto.Box)
                {
                    dataTree.Append(new GH_Number(num), path);
                }
            }

            DA.SetDataTree(0, dataTree);
            DA.SetData(1, queryPointECEFPoint);
            DA.SetDataList(2, tileHandler.DownloadedFilePaths);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return getMap();
            }
        }

        private Bitmap getMap()
        {
            MemoryStream stream_ = new MemoryStream(Properties.Resources.artboard_2);
            Bitmap bitmap = new Bitmap(stream_);
            return bitmap;
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0A6BE53D-7951-4D8D-9424-88E5FC8202DF"); }
        }
    }
}