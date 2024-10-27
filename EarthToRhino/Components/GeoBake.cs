using System;
using System.Collections.Generic;
using Grasshopper.Kernel.Types;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Render;
using Rhino;
using System.Diagnostics;

namespace EarthToRhino.Components
{
    public class GeoBake : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GeoBake class.
        /// </summary>
        public GeoBake()
          : base("GeoBake", "GeoBake",
              "Bakes mesh with texture",
              Utilities.CATEGORY_NAME, Utilities.SUBCATEGORY_NAME)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Meshes", GH_ParamAccess.list);
            pManager.AddGenericParameter("Material", "M2", "Material", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Bake", "B", "Bake mesh", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Rhino.Geometry.Mesh> meshes = new List<Rhino.Geometry.Mesh>();
            List<GH_Material> materials = new List<GH_Material>();
            bool toggle = false;
            bool baked = false;

            if (!DA.GetDataList(0, meshes)) return;
            if (!DA.GetDataList(1, materials)) return;
            if (!DA.GetData(2, ref toggle)) return;

            if (toggle == true && baked == false)
            {
                if (meshes.Count != materials.Count)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input length mismatch");
                }
                else
                {
                    for (int i = 0; i < meshes.Count; i++)
                    {
                        Rhino.Geometry.Mesh mesh = meshes[i];

                        RenderMaterial material = materials[i].MaterialBestGuess();

                        bool exists = false;

                        foreach (RenderMaterial m in Rhino.RhinoDoc.ActiveDoc.RenderMaterials)
                        {
                            Debug.WriteLine(m);
                            if (m.Equals(material) == true)
                            {
                                exists = true;
                                material = m;

                                Debug.WriteLine("material exists");
                            }
                        }

                        if (exists == false)
                        {
                            Rhino.RhinoDoc.ActiveDoc.RenderMaterials.Add(material);
                        }
                                               

                        Rhino.DocObjects.ObjectAttributes attributes = new Rhino.DocObjects.ObjectAttributes();

                        attributes.RenderMaterial = material;
                        attributes.MaterialSource = Rhino.DocObjects.ObjectMaterialSource.MaterialFromObject;

                        Rhino.RhinoDoc.ActiveDoc.Objects.AddMesh(mesh, attributes);

                    }

                }

                baked = true;
            }
            if (toggle == false)
            {
                baked = false;
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
            get { return new Guid("E53FD5AC-E035-4AAE-9ADD-58C73E9024C3"); }
        }
    }
}