using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace EarthToRhino.Components
{
    public class TileImporter : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TileImporter class.
        /// </summary>
        public TileImporter()
          : base("Tile Importer", "TI",
              "Description",
              Utilities.CATEGORY_NAME, Utilities.SUBCATEGORY_NAME)
        {
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
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string folderPath = "";
            if (!DA.GetData(0, ref folderPath)) return;

            List<string> allFiles = getAllFiles(folderPath);
            
            //foreach (string file in allFiles)
            //{
            //    RhinoDoc.Import(file);
            //}
            DA.SetDataList(0, allFiles);

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
            }

            importFile(folderPath + "\\" + allFiles[0]);
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
            var success = RhinoDoc.ActiveDoc.Import(filePath);

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