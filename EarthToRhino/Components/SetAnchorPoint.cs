using Grasshopper;
using Grasshopper.Kernel;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace EarthToRhino.Components
{
    public class SetAnchorPoint : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public SetAnchorPoint()
          : base("Set Anchor Point", "SAP",
            "Define the Earth Anchor Point for your project.",
            Utilities.CATEGORY_NAME, Utilities.SUBCATEGORY_NAME)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Set", "set", "Set the EarthAnchorPoint", GH_ParamAccess.item, false);
            pManager.AddPointParameter("Model Base Point", "MBP", "The point in rhino to set earth anchor", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager.AddTextParameter("Latitude", "LAT", "The latitude of the anchor point", GH_ParamAccess.item);
            pManager.AddTextParameter("Longitude", "LON", "The longitude of the anchor point", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Base Point", "BP", "The anchor point", GH_ParamAccess.item);
            pManager.AddTextParameter("Earth Anchor Point", "EAP", "EarthAnchorPoint Longitude/Latitude", GH_ParamAccess.item);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string latString = string.Empty;
            string lonString = string.Empty;
            double lat = Double.NaN;
            double lon = Double.NaN;
            bool EAP = false;
            Point3d basePoint = new Point3d();
            string lonlatString = string.Empty;

            DA.GetData<bool>(0, ref EAP);
            DA.GetData<Point3d>(1, ref basePoint);
            DA.GetData<string>(2, ref latString);
            DA.GetData<string>(3, ref lonString);

            if (EAP == true)
            {
                EarthAnchorPoint ePt = new EarthAnchorPoint();

                

                lat = GeoHelper.DMStoDDLat(latString);
                lon = GeoHelper.DMStoDDLon(lonString);

                if (Double.IsNaN(lat) && !string.IsNullOrEmpty(latString))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Latitude value is invalid. Please enter value in valid Decimal Degree format (-79.976666) " +
                        "or valid Degree Minute Second format (79°58′36″W | 079:56:55W | 079d 58′ 36″ W | 079 58 36.0 | 079 58 36.4 E)");
                    return;
                }

                if (Double.IsNaN(lon) && !string.IsNullOrEmpty(lonString))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Longitude value is invalid. Please enter value in valid Decimal Degree format (40.446388) " +
                        "or valid Degree Minute Second format (40°26′47″N | 40:26:46N | 40d 26m 47s N | 40 26 47.1 | 40 26 47.4141 N)");
                    return;
                }

                else
                {
                    if (!Double.IsNaN(lat) && !Double.IsNaN(lon))
                    {
                        ePt.EarthBasepointLatitude = lat;
                        ePt.EarthBasepointLongitude = lon;
                        ePt.ModelBasePoint = basePoint;
                        ePt.Description = "user defined earth anchor point";
                    }
                }

                if ((ePt.EarthBasepointLatitude > -90) && (ePt.EarthBasepointLatitude < 90) && (ePt.EarthBasepointLongitude > -180) && (ePt.EarthBasepointLongitude < 180))
                {
                    //set new EAP
                    Rhino.RhinoDoc.ActiveDoc.EarthAnchorPoint = ePt;
                }

            }

            //check if EAP has been set and if so what is it
            if (!Rhino.RhinoDoc.ActiveDoc.EarthAnchorPoint.EarthLocationIsSet())
            {
                lonlatString = "The Earth Anchor Point has not been set yet";
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "EAP has not been set yet");
            }

            else lonlatString = "Longitude: " + Rhino.RhinoDoc.ActiveDoc.EarthAnchorPoint.EarthBasepointLongitude.ToString() +
                " / Latitude: " + Rhino.RhinoDoc.ActiveDoc.EarthAnchorPoint.EarthBasepointLatitude.ToString();


            DA.SetData(0, basePoint);
            DA.SetData(1, lonlatString);
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
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
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
            MemoryStream stream_ = new MemoryStream(Properties.Resources.artboard_1);
            Bitmap bitmap = new Bitmap(stream_);
            return bitmap;
        }


        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("ed883b28-24cb-4dbe-8370-151b99d9926a");
    }
}