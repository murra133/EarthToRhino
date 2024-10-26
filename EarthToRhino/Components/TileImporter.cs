using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

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
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("All Files", "FP", "Lists all Files", GH_ParamAccess.list);
            pManager.AddMeshParameter("Mesh","M","Meshes",GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string folderPath = "";
            List<Mesh> meshList = new List<Mesh>();
            if (!DA.GetData(0, ref folderPath)) return;

            List<string> allFiles = getAllFiles(folderPath);

            if (allFiles.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No files found in the folder");
                return;
            }

            foreach (RhinoObject obj in doc.Objects)
            {
                if (obj == null) continue;
                try
                {
                    if (obj.Geometry is GeometryBase geom)
                    {
                        meshList.Add((Mesh)geom); // Add geometry to the list
                    }

                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);

                }
            }

            DA.SetDataList(0, allFiles);
            DA.SetDataList(1, meshList);

        }


        private List<string> getAllFiles(string folderPath)
        {
            List<string> allFiles = new List<string>();
            DirectoryInfo d = new DirectoryInfo(folderPath); //Assuming Test is your Folder

            FileInfo[] Files = d.GetFiles("*.glb"); //Getting Text files

            foreach (FileInfo file in Files)
            {
                Debug.WriteLine(file.Name);
                allFiles.Add(file.Name);
                importFile(folderPath + "\\" + file.Name);

            }

            return allFiles;
        }

        private bool importFile(string filePath)
        {
            Debug.WriteLine(filePath);
            // Check if the file path is valid
            if (string.IsNullOrEmpty(filePath))
            {
                RhinoApp.WriteLine("Invalid file path. Please provide a valid path.");
                return false;
            }

            // Try importing the file into the active document
            var success = doc.Import(filePath);

            // Output the result of the import operation
            if (success)
            {
                RhinoApp.WriteLine("File imported successfully.");

                return true;
            }
            else
            {
                RhinoApp.WriteLine("File import failed.");
                return true; // Output false if the import failed
            }
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
                return null;
            }
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