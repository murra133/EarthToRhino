﻿using System;
using System.Collections.Generic;
using System.Net;
using Grasshopper.Kernel;
using Rhino.Geometry;

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
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {

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

            foreach (ChildDTO child in rootCluster.Root.Children)
            {
                foreach (ChildDTO grandChild in child.Children)
                {
                    TileClusterDTO firstLayer = tileHandler.GetTileCluster(grandChild.Content.Uri);

                    foreach (ChildDTO firstLayerChild in firstLayer.Root.Children)
                    {
                        foreach (ChildDTO firstLayerGrandchild in firstLayerChild.Children)
                        {
                            tileHandler.TrySaveChild(firstLayerGrandchild);
                        }
                        
                    }
                }
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
            get { return new Guid("0A6BE53D-7951-4D8D-9424-88E5FC8202DF"); }
        }
    }
}