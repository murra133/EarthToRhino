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
              "Description",
              Utilities.CATEGORY_NAME, Utilities.SUBCATEGORY_NAME)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Level of Detail", "D", "The level of detail", GH_ParamAccess.item);
            pManager.AddRectangleParameter("Boundary", "B", "The boundary of the area to download", GH_ParamAccess.item);
            pManager.AddTextParameter("Temp Folder", "F", "The temporary folder to store the tiles", GH_ParamAccess.item);
            pManager.AddTextParameter("API Key", "K", "The API key", GH_ParamAccess.item);

            pManager[0].Optional = true;
            pManager[1].Optional = true;
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
            pManager.AddNumberParameter("BBoxes", "BB", "Bounding boxes", GH_ParamAccess.list);
            pManager.AddNumberParameter("Query BBoxes", "QBB", "Bounding boxes", GH_ParamAccess.list);
            pManager.AddPointParameter("Query Point ECEF", "QPE", "Query point in ECEF coordinates", GH_ParamAccess.item);

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

            DA.GetData(0, ref levelOfDetail);
            DA.GetData(1, ref boundary);
            DA.GetData(2, ref tempFolder);
            DA.GetData(3, ref apiKey);
            

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

            TileHandler tileHandler = new TileHandler();

            TileClusterDTO rootCluster = tileHandler.GetTileCluster(RoutesController.Root);

            List<BoundingVolumeDTO> bboxes = new List<BoundingVolumeDTO>();

            foreach (ChildDTO child in rootCluster.Root.Children)
            {
                foreach (ChildDTO grandChild in child.Children)
                {
                    TileClusterDTO firstLayer = tileHandler.GetTileCluster(grandChild.Content.Uri);

                    foreach (ChildDTO firstLayerChild in firstLayer.Root.Children)
                    {
                        foreach (ChildDTO firstLayerGrandchild in firstLayerChild.Children)
                        {
                            if (tileHandler.TrySaveChild(firstLayerGrandchild))
                            {
                                bboxes.Add(firstLayerGrandchild.BoundingVolume);
                            }
                            
                        }
                        
                    }
                }
            }

            // Initialize data structures
            GH_Structure<GH_Number> dataTree = new GH_Structure<GH_Number>();
            GH_Structure<GH_Number> queryBboxes = new GH_Structure<GH_Number>();

            Point3d queryPoint = boundary.Center;
            var earthAnchor = Rhino.RhinoDoc.ActiveDoc.EarthAnchorPoint;
            Transform modelToEarth = earthAnchor.GetModelToEarthTransform(Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem);
            queryPoint.Transform(modelToEarth);
            double targetLongitude = queryPoint.X;
            double targetLatitude = queryPoint.Y;
            double targetAltitude = queryPoint.Z;

            // Convert the query point to ECEF coordinates
            Vector3d queryPointECEF = GeoHelper.LatLonToECEF(targetLatitude, targetLongitude, targetAltitude);

            // Convert queryPointECEF to Point3d for use in Rhino
            Point3d queryPointECEFPoint = new Point3d(queryPointECEF.X, queryPointECEF.Y, queryPointECEF.Z);

            // Loop through the bounding volumes
            for (int i = 0; i < bboxes.Count; i++)
            {
                GH_Path path = new GH_Path(i);
                BoundingVolumeDTO dto = bboxes[i];

                // Parse the bounding volume
                Vector3d center = new Vector3d(dto.Box[0], dto.Box[1], dto.Box[2]);
                Vector3d halfAxisX = new Vector3d(dto.Box[3], dto.Box[4], dto.Box[5]);
                Vector3d halfAxisY = new Vector3d(dto.Box[6], dto.Box[7], dto.Box[8]);
                Vector3d halfAxisZ = new Vector3d(dto.Box[9], dto.Box[10], dto.Box[11]);

                // Compute the extents (lengths) of the half-axes
                double extentX = halfAxisX.Length;
                double extentY = halfAxisY.Length;
                double extentZ = halfAxisZ.Length;

                // Normalize the half-axes to get the orientation axes
                Vector3d axisX = halfAxisX / extentX;
                Vector3d axisY = halfAxisY / extentY;
                Vector3d axisZ = halfAxisZ / extentZ;

                // Build the rotation matrix
                Transform rotation = Transform.Identity;
                rotation.M00 = axisX.X; rotation.M01 = axisY.X; rotation.M02 = axisZ.X;
                rotation.M10 = axisX.Y; rotation.M11 = axisY.Y; rotation.M12 = axisZ.Y;
                rotation.M20 = axisX.Z; rotation.M21 = axisY.Z; rotation.M22 = axisZ.Z;

                // Create the OrientedBoundingBox
                Vector3d halfExtents = new Vector3d(extentX, extentY, extentZ);
                Point3d obbCenter = new Point3d(center.X, center.Y, center.Z);
                OrientedBoundingBox obb = new OrientedBoundingBox(obbCenter, halfExtents, rotation);

                // Check if the query point is inside the OBB
                bool containsPoint = obb.Contains(queryPointECEFPoint);

                // If true - add to queryBboxes
                if (containsPoint)
                {
                    foreach (double num in dto.Box)
                    {
                        queryBboxes.Append(new GH_Number(num), path);
                    }
                }

                // Append the bounding volume to dataTree
                foreach (double num in dto.Box)
                {
                    dataTree.Append(new GH_Number(num), path);
                }
            }

            // Set output data
            DA.SetDataTree(0, dataTree);
            DA.SetDataTree(1, queryBboxes);
            DA.SetData(2, queryPointECEFPoint);
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