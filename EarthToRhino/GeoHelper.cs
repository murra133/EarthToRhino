using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Rhino;
using Rhino.DocObjects;
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

            var earthAnchor = activeDoc.EarthAnchorPoint;

            var transform = earthAnchor.GetModelToEarthTransform(activeDoc.ModelUnitSystem);
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

        // public static bool IsTileInBoundary(Rectangle3d boundary, BoundingVolumeDTO bbox)
        // {
        //     // Get the EarthAnchorPoint from the active Rhino document
        //     EarthAnchorPoint earthAnchor = Rhino.RhinoDoc.ActiveDoc.EarthAnchorPoint;
        //
        //     // Get the model to earth transformation
        //     Transform modelToEarth = earthAnchor.GetModelToEarthTransform(Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem);
        //
        //     // Convert boundary rectangle to ECEF
        //     Point3d[] rectangleCorners = new Point3d[4];
        //     for (int i = 0; i < 4; i++)
        //     {
        //         rectangleCorners[i] = boundary.Corner(i);
        //     }
        //
        //     List<Point3d> rectangleCornersECEF = new List<Point3d>();
        //
        //     foreach (Point3d corner in rectangleCorners)
        //     {
        //         // Transform the corner to Earth coordinates
        //         Point3d earthCorner = corner;
        //         earthCorner.Transform(modelToEarth);
        //
        //         double cornerLatitude = earthCorner.Y;   // Latitude in degrees
        //         double cornerLongitude = earthCorner.X;  // Longitude in degrees
        //         double cornerAltitude = earthCorner.Z;   // Altitude in meters
        //
        //         // Convert to ECEF coordinates
        //         Vector3d cornerECEF = LatLonToECEF(cornerLatitude, cornerLongitude, cornerAltitude);
        //         rectangleCornersECEF.Add(new Point3d(cornerECEF.X, cornerECEF.Y, cornerECEF.Z));
        //     }
        //
        //     // Compute min and max X and Y
        //     double minX = rectangleCornersECEF.Min(p => p.X);
        //     double minY = rectangleCornersECEF.Min(p => p.Y);
        //     double maxX = rectangleCornersECEF.Max(p => p.X);
        //     double maxY = rectangleCornersECEF.Max(p => p.Y);
        //
        //     // Set minZ and maxZ to large values to encompass all altitudes
        //     double minZ = -1e6; // Adjust as needed
        //     double maxZ = 1e6;
        //
        //     // Create the query AABB with extended Z bounds
        //     BoundingBox queryAABB = new BoundingBox(
        //         new Point3d(minX, minY, minZ),
        //         new Point3d(maxX, maxY, maxZ)
        //     );
        //
        //     // Parse the bounding volume to create an OrientedBoundingBox
        //     Vector3d center = new Vector3d(bbox.Box[0], bbox.Box[1], bbox.Box[2]);
        //     Vector3d halfAxisX = new Vector3d(bbox.Box[3], bbox.Box[4], bbox.Box[5]);
        //     Vector3d halfAxisY = new Vector3d(bbox.Box[6], bbox.Box[7], bbox.Box[8]);
        //     Vector3d halfAxisZ = new Vector3d(bbox.Box[9], bbox.Box[10], bbox.Box[11]);
        //
        //     // Compute the extents (lengths) of the half-axes
        //     double extentX = halfAxisX.Length;
        //     double extentY = halfAxisY.Length;
        //     double extentZ = halfAxisZ.Length;
        //
        //     // Normalize the half-axes to get the orientation axes
        //     Vector3d axisX = halfAxisX / extentX;
        //     Vector3d axisY = halfAxisY / extentY;
        //     Vector3d axisZ = halfAxisZ / extentZ;
        //
        //     // Build the rotation matrix
        //     Transform rotation = Transform.Identity;
        //     rotation.M00 = axisX.X; rotation.M01 = axisY.X; rotation.M02 = axisZ.X;
        //     rotation.M10 = axisX.Y; rotation.M11 = axisY.Y; rotation.M12 = axisZ.Y;
        //     rotation.M20 = axisX.Z; rotation.M21 = axisY.Z; rotation.M22 = axisZ.Z;
        //
        //     // Create the OrientedBoundingBox
        //     Vector3d halfExtents = new Vector3d(extentX, extentY, extentZ);
        //     Point3d obbCenter = new Point3d(center.X, center.Y, center.Z);
        //     OrientedBoundingBox obb = new OrientedBoundingBox(obbCenter, halfExtents, rotation);
        //
        //     // Perform the intersection test using SAT
        //     bool intersects = OBBIntersectsAABB(obb, queryAABB);
        //
        //     return intersects;
        // }
        //
        // public static bool OBBIntersectsAABB(OrientedBoundingBox obb, BoundingBox aabb)
        // {
        //     // Get the center and half-extents of the AABB
        //     Point3d aabbCenter = 0.5 * (aabb.Min + aabb.Max);
        //     Vector3d aabbHalfExtents = 0.5 * (aabb.Max - aabb.Min);
        //
        //     // Compute rotation matrix expressing OBB in AABB coordinate frame
        //     Vector3d[] obbAxes = new Vector3d[3];
        //     obbAxes[0] = new Vector3d(obb.Rotation.M00, obb.Rotation.M10, obb.Rotation.M20);
        //     obbAxes[1] = new Vector3d(obb.Rotation.M01, obb.Rotation.M11, obb.Rotation.M21);
        //     obbAxes[2] = new Vector3d(obb.Rotation.M02, obb.Rotation.M12, obb.Rotation.M22);
        //
        //     // AABB axes
        //     Vector3d[] aabbAxes = { Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis };
        //
        //     // Translation vector from AABB center to OBB center
        //     Vector3d t = obb.Center - aabbCenter;
        //
        //     // Project t onto AABB axes
        //     double[] tAABB = { t * aabbAxes[0], t * aabbAxes[1], t * aabbAxes[2] };
        //
        //     double[,] R = new double[3, 3];
        //     double[,] AbsR = new double[3, 3];
        //     double EPSILON = 1e-6;
        //
        //     // Compute rotation matrix and its absolute value
        //     for (int i = 0; i < 3; i++)
        //     {
        //         for (int j = 0; j < 3; j++)
        //         {
        //             R[i, j] = obbAxes[i] * aabbAxes[j];
        //             AbsR[i, j] = Math.Abs(R[i, j]) + EPSILON;
        //         }
        //     }
        //
        //     // Test axes L = AABB axis
        //     for (int i = 0; i < 3; i++)
        //     {
        //         double ra = obb.HalfExtents.X * AbsR[0, i] + obb.HalfExtents.Y * AbsR[1, i] + obb.HalfExtents.Z * AbsR[2, i];
        //         double rb = aabbHalfExtents[i];
        //         if (Math.Abs(tAABB[i]) > ra + rb)
        //         {
        //             return false;
        //         }
        //     }
        //
        //     // Test axes L = OBB axis
        //     for (int i = 0; i < 3; i++)
        //     {
        //         double ra = obb.HalfExtents[i];
        //         double rb = aabbHalfExtents.X * AbsR[i, 0] + aabbHalfExtents.Y * AbsR[i, 1] + aabbHalfExtents.Z * AbsR[i, 2];
        //         double tOBB = t * obbAxes[i];
        //         if (Math.Abs(tOBB) > ra + rb)
        //         {
        //             return false;
        //         }
        //     }
        //
        //     // Test axis L = AABB axis[i] x OBB axis[j]
        //     for (int i = 0; i < 3; i++)
        //     {
        //         for (int j = 0; j < 3; j++)
        //         {
        //             double ra = obb.HalfExtents[(i + 1) % 3] * AbsR[(i + 2) % 3, j] + obb.HalfExtents[(i + 2) % 3] * AbsR[(i + 1) % 3, j];
        //             double rb = aabbHalfExtents[(j + 1) % 3] * AbsR[i, (j + 2) % 3] + aabbHalfExtents[(j + 2) % 3] * AbsR[i, (j + 1) % 3];
        //             double tCross = Math.Abs(t[(i + 2) % 3] * R[(i + 1) % 3, j] - t[(i + 1) % 3] * R[(i + 2) % 3, j]);
        //             if (tCross > ra + rb)
        //             {
        //                 return false;
        //             }
        //         }
        //     }
        //
        //     // No separating axis found; boxes intersect
        //     return true;
        // }

        // public static bool BoundingBoxesIntersect(BoundingBox bb1, BoundingBox bb2)
        // {
        //     return !(bb1.Max.X < bb2.Min.X || bb1.Min.X > bb2.Max.X ||
        //              bb1.Max.Y < bb2.Min.Y || bb1.Min.Y > bb2.Max.Y ||
        //              bb1.Max.Z < bb2.Min.Z || bb1.Min.Z > bb2.Max.Z);
        // }

        public static Point3d ModelPointToECEF(Point3d modelPoint)
        {
            // Get the EarthAnchorPoint from the active Rhino document
            EarthAnchorPoint earthAnchor = Rhino.RhinoDoc.ActiveDoc.EarthAnchorPoint;

            // Get the model to earth transformation
            Transform modelToEarth = earthAnchor.GetModelToEarthTransform(Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem);

            // Transform the point to Earth coordinates (latitude, longitude, altitude)
            Point3d earthPoint = modelPoint;
            earthPoint.Transform(modelToEarth);

            double targetLatitude = earthPoint.Y;  // Latitude in degrees
            double targetLongitude = earthPoint.X; // Longitude in degrees
            double targetAltitude = earthPoint.Z;  // Altitude in meters

            // Convert the point to ECEF coordinates
            Vector3d ecefVector = LatLonToECEF(targetLatitude, targetLongitude, targetAltitude);

            // Return the ECEF point as Point3d
            return new Point3d(ecefVector.X, ecefVector.Y, ecefVector.Z);
        }

        public static List<Point3d> ConvertBoundaryToECEF(Rectangle3d boundary)
        {
            // Get the EarthAnchorPoint from the active Rhino document
            EarthAnchorPoint earthAnchor = Rhino.RhinoDoc.ActiveDoc.EarthAnchorPoint;

            // Get the model to earth transformation
            Transform modelToEarth = earthAnchor.GetModelToEarthTransform(Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem);

            // Get the corners of the rectangle
            Point3d[] rectangleCorners = new Point3d[4];
            for (int i = 0; i < 4; i++)
            {
                rectangleCorners[i] = boundary.Corner(i);
            }

            List<Point3d> rectangleCornersECEF = new List<Point3d>();

            // Convert each corner to ECEF
            foreach (Point3d corner in rectangleCorners)
            {
                // Transform the corner to Earth coordinates
                Point3d earthCorner = corner;
                earthCorner.Transform(modelToEarth);

                double cornerLatitude = earthCorner.Y;   // Latitude in degrees
                double cornerLongitude = earthCorner.X;  // Longitude in degrees
                double cornerAltitude = earthCorner.Z;   // Altitude in meters

                // Convert to ECEF coordinates
                Vector3d cornerECEF = LatLonToECEF(cornerLatitude, cornerLongitude, cornerAltitude);
                rectangleCornersECEF.Add(new Point3d(cornerECEF.X, cornerECEF.Y, cornerECEF.Z));
            }

            return rectangleCornersECEF;
        }


    }
}
