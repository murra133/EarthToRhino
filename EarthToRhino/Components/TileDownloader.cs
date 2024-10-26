using System;
using System.Collections.Generic;
using System.Net;
using Grasshopper.Kernel;
using Rhino.Geometry;


using System.IO;

using System.Threading.Tasks;

using Grasshopper.Kernel.Types;
using Newtonsoft.Json;
using DocumentFormat.OpenXml.Drawing;


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
            pManager[2].Optional = true;
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
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager
 pManager)
        {
            pManager.AddTextParameter("Message",
 "Message", "Status message", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string apiKey = "";
            GH_Rectangle rect = null;
            string outputDir = "";

            //bool a = DA.GetData(3, ref apiKey);
            //bool b = DA.GetData(1, ref rect);
            //bool c = DA.GetData(2, ref outputDir);

            if (!DA.GetData(3, ref apiKey) || !DA.GetData(1, ref rect) || !DA.GetData(2, ref outputDir))
                return;

            // Construct the Google Maps API URL
            //double x=0.0, y=0.0; 
            string url = $"https://maps.googleapis.com/maps/api/elevation_api/json?locations={rect.Boundingbox.Center.X},{rect.Boundingbox.Center.Y}&key={apiKey}";
            //string url = $"https://maps.googleapis.com/maps/api/elevation_api/json?locations={x},{y}&key={apiKey}";
            // Download the JSON response
            using (WebClient client = new WebClient())
            {
                string json = client.DownloadString(url);

                // Parse the JSON response to extract tile URLs
                // ... (Implement JSON parsing and tile URL extraction)
                // Parse the JSON response
                dynamic jsonData = JsonConvert.DeserializeObject(json);

                // Extract tile URLs from the JSON object
                List<string> tileUrlsList = new List<string> { "tile1.jpg", "tile2.jpg", "tile3.jpg" };
                IEnumerable<string> tileUrls = tileUrlsList;
                //IEnumerable<string> tileUrls = jsonData.tiles?.Select(tile => tile?.url);

                // Download each tile and save to the output directory
                foreach (string tileUrl in tileUrls)
                {
                    using (WebClient tileClient = new WebClient())
                    {
                        tileClient.DownloadFile(tileUrl, System.IO.Path.Combine(outputDir, System.IO.Path.GetFileName(tileUrl)));
                    }
                }

                DA.SetData(0, "Tiles downloaded successfully.");
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