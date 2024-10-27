using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Versioning;
using EarthToRhino.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Render;
using System.Drawing;
using Point = Rhino.Geometry.Point;
using GH_IO.Serialization;

namespace EarthToRhino.Components
{
    public class TileImporter : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TileImporter class.
        /// </summary>
        /// 

        RhinoDoc doc;
        public TileImporter()
          : base("Tile Importer", "TI",
              "Loads Tile into View of Rhino",
              Utilities.CATEGORY_NAME, Utilities.SUBCATEGORY_NAME)
        {
            doc = RhinoDoc.CreateHeadless(null);
            doc.ModelUnitSystem = UnitSystem.Meters;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Folder Path", "TP", "Folder Path to the tile", GH_ParamAccess.item);
            pManager.AddPointParameter("AnchorPoint", "AP", "The geolocated anchor point", GH_ParamAccess.item);
            pManager.AddCurveParameter("AreaRect", "AR", "AreaRect", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("All Files", "FP", "Lists all Files", GH_ParamAccess.list);
            pManager.AddMeshParameter("Mesh", "M", "Meshes", GH_ParamAccess.list);
            pManager.AddMeshParameter("OrientedMesh", "M", "Meshes", GH_ParamAccess.list);
            pManager.AddGenericParameter("Material", "M2", "Material", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string folderPath = "";
            List<Mesh> meshList = new List<Mesh>();
            List<GH_Material> materialList = new List<GH_Material>();
            if (!DA.GetData(0, ref folderPath)) return;
            Point3d anchorP = new Point3d();
            if (!DA.GetData(1, ref anchorP)) return;
            Curve rect = new PolyCurve();
            if (!DA.GetData(2, ref rect)) return;

            List<string> allFiles = getAllFiles(folderPath);

            if (allFiles.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No files found in the folder");
                return;
            }

            foreach (RhinoObject obj in doc.Objects)
            {

                if (obj == null) continue;
                Mesh[] m = obj.GetMeshes(MeshType.Render);
                try
                {
                    if (obj.Geometry is GeometryBase geom)
                    {
                        switch (geom.ObjectType)
                        {
                            case ObjectType.Mesh:
                                Mesh mesh = (Mesh)geom;
                                mesh.Normals.ComputeNormals();
                                meshList.Add(mesh);
                                RenderMaterial renderMaterial = obj.RenderMaterial;
                                if (renderMaterial != null)
                                {
                                    // Create a new material with texture information
                                    materialList.Add(new GH_Material(renderMaterial));

                                }
                                break;
                            default:
                                break;
                        }
                    }

                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);

                }
            }
            var oritentedMeshes = OrientTiles(anchorP, rect, meshList);
            doc.Objects.Clear();
            DA.SetDataList(0, allFiles);
            DA.SetDataList(1, meshList);
            DA.SetDataList(2, oritentedMeshes);
            DA.SetDataList(3, materialList);

        }


        private List<string> getAllFiles(string folderPath)
        {
            List<string> allFiles = new List<string>();
            DirectoryInfo d = new DirectoryInfo(folderPath); //Assuming Test is your Folder

            FileInfo[] Files = d.GetFiles("*.glb"); //Getting Text files

            foreach (FileInfo file in Files)
            {
                allFiles.Add(file.Name);
                importFile(folderPath + "\\" + file.Name);

            }

            return allFiles;
        }

        private bool importFile(string filePath)
        {
            // Check if the file path is valid
            if (string.IsNullOrEmpty(filePath))
            {
                RhinoApp.WriteLine("Invalid file path. Please provide a valid path.");
                return false;
            }

            // Try importing the file into the active document
            //var success = doc.Import(filePath);
            var c = doc.Import(filePath);

            // Output the result of the import operation
            if (c)
            {
                return true;
            }
            else
            {
                RhinoApp.WriteLine("File import failed.");
                return true; // Output false if the import failed
            }
        }


        public static List<Mesh> OrientTiles(Point3d point, Curve rect, List<Mesh> meshes)
        {

            GH_GeometryGroup g = new GH_GeometryGroup();
            foreach (var m in meshes)
                g.Objects.Add(Grasshopper.Kernel.GH_Convert.ToGeometricGoo(m.DuplicateMesh()));

            Surface srf = Brep.CreatePlanarBreps(rect, 0.1)[0].Surfaces[0];
            double u, v;
            var closestPt = srf.ClosestPoint(point, out u, out v);
            srf.SetDomain(0, new Interval(0, 1));
            srf.SetDomain(1, new Interval(0, 1));
            Point3d evalPoint;
            Vector3d[] ders;
            srf.Evaluate(u, v, 1, out evalPoint, out ders);


            Plane pl2 = new Plane(evalPoint, ders[0], ders[1]);


            List<Point3d> vertsFlat = new List<Point3d>();

            foreach (var m in meshes)
            {
                vertsFlat.AddRange(m.Vertices.ToPoint3dArray());
            }

            List<Point3d> sortedByX = new List<Point3d>(vertsFlat);
            sortedByX.Sort((a, b) => b.X.CompareTo(a.X));

            List<Point3d> sortedByY = new List<Point3d>(vertsFlat);
            sortedByY.Sort((a, b) => b.Y.CompareTo(a.Y));

            Point3d upX = sortedByX[sortedByX.Count - 1],
                douwnX = sortedByX[0],
                upY = sortedByY[sortedByY.Count - 1],
                downY = sortedByY[0];


            Mesh tempMesh = new Mesh();
            var corners = new Point3d[] {
            upX,
            upY,
            douwnX,
            downY
        };

            tempMesh.Vertices.AddVertices(corners);
            tempMesh.Faces.AddFace(0, 1, 2, 3);
            tempMesh.RebuildNormals();

            var combined =
                Brep.CreateFromMesh(tempMesh, true);
            //    Brep.CreateFromCornerPoints(
            //    upX,
            //    upY,
            //    douwnX,
            //    downY,
            //    1000000
            //);


            Point3d evalPoint_comb;
            Vector3d[] ders_comb;
            combined.Surfaces[0].Evaluate(u, v, 1, out evalPoint_comb, out ders_comb);


            Plane pl1 = new Plane(evalPoint_comb, ders_comb[0], ders_comb[1]);




            var ClosPoints = new List<Point3d>();
            var distances = new List<double>();
            foreach (var m in meshes)
            {
                var mClosestPoint = m.ClosestPoint(evalPoint);

                ClosPoints.Add(mClosestPoint);
                distances.Add((mClosestPoint - evalPoint).Length);
            }

            var sortedPoints = new List<Point3d>(ClosPoints);

            for (int i = 0; i < ClosPoints.Count - 1; i++)
            {
                for (int j = i + 1; j < ClosPoints.Count; j++)
                {
                    if (distances[i] < distances[j])
                    {
                        var tempValue = distances[i];
                        distances[i] = distances[j];
                        distances[j] = tempValue;

                        // Swap corresponding points
                        var tempPoint = ClosPoints[i];
                        ClosPoints[i] = ClosPoints[j];
                        ClosPoints[j] = tempPoint;
                    }
                }
            }


            var target = sortedPoints[0];

            Rhino.Geometry.Transform orient = Rhino.Geometry.Transform.ChangeBasis(pl2, pl1);
            BoundingBox bbGroup = new BoundingBox(vertsFlat);
            Rhino.Geometry.Transform translate = Rhino.Geometry.Transform.Translation(point - bbGroup.Center);
            g.Transform(orient);
            g.Transform(translate);
            
            var orientedMeshes = new List<Mesh>();
            foreach (var m in meshes)
            {
                var gO = m.DuplicateMesh();
                if (gO.Transform(orient))
                    orientedMeshes.Add(gO);
            }

            //foreach (var m in g.Objects)
            //{

            //    var nms = new Mesh();
            //    Grasshopper.Kernel.GH_Convert.ToMesh(m, ref nms, new GH_Conversion());
            //    orientedMeshes.Add(nms);

            //}

            return orientedMeshes;
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
            MemoryStream stream_ = new MemoryStream(Properties.Resources.artboard_3);
            Bitmap bitmap = new Bitmap(stream_);
            return bitmap;
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("07EFA3E5-9FE9-404D-A743-E8614A8B3661"); }
        }
    }
}