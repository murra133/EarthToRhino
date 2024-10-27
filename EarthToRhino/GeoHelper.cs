using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;
using Plane = Rhino.Geometry.Plane;

namespace EarthToRhino
{
    internal class GeoHelper
    {
        public static void FindTilesOfInterest(Tile tile, Vector3 targetPoint, List<Tile> tilesOfInterest)
        {
            // Check if the target point is inside the tile's bounding volume
            if (IsPointInsideOBB(targetPoint, tile.BoundingVolume.Center, tile.BoundingVolume.HalfAxes))
            {
                // If it has children, recurse
                if (tile.Children != null && tile.Children.Count > 0)
                {
                    foreach (var child in tile.Children)
                    {
                        FindTilesOfInterest(child, targetPoint, tilesOfInterest);
                    }
                }
                else
                {
                    // Leaf tile that contains the point
                    tilesOfInterest.Add(tile);
                }
            }
        }

        public static double DMStoDDLon(string dms)
        {
            ///Regex pattern for verifying DMS Longitude is with -180 to 180
            var lonDMSPattern = @"[ ,]*(-?(180[ :°d]*00[ :\'\'m]*00(\.0+)?|(1[0-7][0-9]|0[0-9][0-9]|[0-9][0-9])[ :°d]*[0-5][0-9][ :\'\'m]*[0-5][0-9](\.\d+)?)[ :\?\""s]*(E|e|W|w)?)";

            ///Regex pattern DD Longitude is with -180 to 180
            var lonDDPattern = @"^(\+|-)?(?:180(?:(?:\.0{1,20})?)|(?:[0-9]|[1-9][0-9]|1[0-7][0-9])(?:(?:\.[0-9]{1,20})?))$";

            ///Get rid of any white spaces
            dms = dms.Trim();

            double coordinate = Double.NaN;

            ///Test if the Lon is DD format and return as double if valid
            if (Regex.Match(dms, lonDDPattern).Success)
            {
                return Double.Parse(dms);
            }

            ///Else test if the Lon is DMS format and convert to double DD and return if valid
            else if (Regex.Match(dms, lonDMSPattern).Success && Regex.Match(dms, lonDMSPattern).Value.Length == dms.Length)
            {
                bool sw = dms.ToLower().EndsWith("w");
                int f = sw ? -1 : 1;
                var bits = Regex.Matches(dms, @"[\d.]+", RegexOptions.IgnoreCase);
                coordinate = 0;
                double result;
                for (int i = 0; i < bits.Count; i++)
                {
                    if (Double.TryParse(bits[i].ToString(), out result))
                    {
                        coordinate += result / f;
                        f *= 60;
                    }
                }

                return coordinate;
            }

            ///If DD or DMS format is invalid, return NaN
            else
            {
                return coordinate;
            }
        }

        public static double DMStoDDLat(string dms)
        {
            ///Regex pattern for verifying DMS Latitude is with -90 to 90
            var latDMSPattern = @"(-?(90[ :°d]*00[ :\'\'m]*00(\.0+)?|[0-8][0-9][ :°d]*[0-5][0-9][ :\'\'m]*[0-5][0-9](\.\d+)?)[ :\?\""s]*(N|n|S|s)?)";

            ///Regex pattern DD Latitude is with -90 to 90
            var latDDPattern = @"^(\+|-)?(?:90(?:(?:\.0{1,20})?)|(?:[0-9]|[1-8][0-9])(?:(?:\.[0-9]{1,20})?))$";

            ///Get rid of any white spaces
            dms = dms.Trim();

            double coordinate = Double.NaN;

            ///Test if the Lat is DD format and return as double if valid
            if (Regex.Match(dms, latDDPattern).Success)
            {
                return Double.Parse(dms);
            }

            ///Else test if the Lat is DMS format and convert to double DD and return if valid
            else if (Regex.Match(dms, latDMSPattern).Success && Regex.Match(dms, latDMSPattern).Value.Length == dms.Length)
            {
                bool sw = dms.ToLower().EndsWith("s");
                int f = sw ? -1 : 1;
                var bits = Regex.Matches(dms, @"[\d.]+", RegexOptions.IgnoreCase);
                coordinate = 0;
                double result;
                for (int i = 0; i < bits.Count; i++)
                {
                    if (Double.TryParse(bits[i].ToString(), out result))
                    {
                        coordinate += result / f;
                        f *= 60;
                    }
                }
                return coordinate;
            }

            ///If DD or DMS format is invalid, return NaN
            else
            {
                return coordinate;
            }
        }

        public static Transform GetEarthAnchorTransform()
        {
            var activeDoc = Rhino.RhinoDoc.ActiveDoc;

            var earthAchor = activeDoc.EarthAnchorPoint;

            var lat = earthAchor.EarthBasepointLatitude;
            var lon = earthAchor.EarthBasepointLongitude;

            var transform = earthAchor.GetModelToEarthTransform(activeDoc.ModelUnitSystem);
            return transform;
        }


        public static bool IsPointInsideOBB(Vector3 point, Vector3 center, Vector3[] halfAxes)
        {
            // Compute the vector from the center of the box to the point
            Vector3 d = point - center;

            // Project d onto each half-axis
            for (int i = 0; i < 3; i++)
            {
                double dist = Vector3.Dot(d, halfAxes[i]);
                double extent = halfAxes[i].Length();

                if (Math.Abs(dist) > extent)
                {
                    return false;
                }
            }
            return true;
        }

        public class Tile
        {
            public BoundingVolume BoundingVolume { get; set; }
            public List<Tile> Children { get; set; }
            public string ContentUri { get; set; }
            // Other properties as needed
        }

        public class BoundingVolume
        {
            public Vector3 Center { get; set; }
            public Vector3[] HalfAxes { get; set; }
        }

        public static Vector3d LatLonToECEF(double lat, double lon, double alt = 0)
        {
            // WGS84 ellipsoid constants
            double a = 6378137; // Equatorial radius
            double e2 = 6.69437999014e-3; // Square of eccentricity

            double latRad = RhinoMath.ToRadians(lat);
            double lonRad = RhinoMath.ToRadians(lon);

            double N = a / Math.Sqrt(1 - e2 * Math.Sin(latRad) * Math.Sin(latRad));

            double x = (N + alt) * Math.Cos(latRad) * Math.Cos(lonRad);
            double y = (N + alt) * Math.Cos(latRad) * Math.Sin(lonRad);
            double z = ((1 - e2) * N + alt) * Math.Sin(latRad);

            return new Vector3d(x, y, z);
        }

        public class OrientedBoundingBox
        {
            public Point3d Center { get; set; }
            public Vector3d HalfExtents { get; set; }
            public Transform Rotation { get; set; }

            public OrientedBoundingBox(Point3d center, Vector3d halfExtents, Transform rotation)
            {
                Center = center;
                HalfExtents = halfExtents;
                Rotation = rotation;
            }

            public bool Contains(Point3d point)
            {
                // Compute the vector from the box center to the point
                Vector3d d = point - Center;

                // Project d onto each of the local axes
                double dx = d * new Vector3d(Rotation.M00, Rotation.M10, Rotation.M20);
                double dy = d * new Vector3d(Rotation.M01, Rotation.M11, Rotation.M21);
                double dz = d * new Vector3d(Rotation.M02, Rotation.M12, Rotation.M22);

                // Check if the projected distances are within the half extents
                return Math.Abs(dx) <= HalfExtents.X &&
                       Math.Abs(dy) <= HalfExtents.Y &&
                       Math.Abs(dz) <= HalfExtents.Z;
            }
        }
        public static bool BoundingBoxesIntersect(BoundingBox bb1, BoundingBox bb2)
        {
            return !(bb1.Max.X < bb2.Min.X || bb1.Min.X > bb2.Max.X ||
                     bb1.Max.Y < bb2.Min.Y || bb1.Min.Y > bb2.Max.Y ||
                     bb1.Max.Z < bb2.Min.Z || bb1.Min.Z > bb2.Max.Z);
        }

        public static bool OBBIntersectsAABB(OrientedBoundingBox obb, BoundingBox aabb)
        {
            // Convert the OBB to a Box
            Rhino.Geometry.Plane plane = new Rhino.Geometry.Plane(obb.Center,
                new Vector3d(obb.Rotation.M00, obb.Rotation.M10, obb.Rotation.M20),
                new Vector3d(obb.Rotation.M01, obb.Rotation.M11, obb.Rotation.M21));

            Box obbBox = new Box(plane,
                new Interval(-obb.HalfExtents.X, obb.HalfExtents.X),
                new Interval(-obb.HalfExtents.Y, obb.HalfExtents.Y),
                new Interval(-obb.HalfExtents.Z, obb.HalfExtents.Z));

            // Get the bounding box of the OBB
            BoundingBox obbBoundingBox = obbBox.BoundingBox;

            // Use the custom method to test intersection
            return BoundingBoxesIntersect(obbBoundingBox, aabb);
        }

    }
}
