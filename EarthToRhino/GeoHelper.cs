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



        public static bool IsTileInBoundary(Rectangle3d boundary, BoundingVolumeDTO bbox)
        {
            // Step 1: Get the latitude and longitude range of the boundary rectangle
            var boundaryLatLonRange = GetBoundaryLatLonRange(boundary);

            // Step 2: Get the latitude and longitude range of the tile's bounding volume
            var tileLatLonRange = GetTileLatLonRange(bbox);

            // Step 3: Check for overlap between the boundary and tile ranges
            bool intersects = RangesOverlap(boundaryLatLonRange, tileLatLonRange);

            return intersects;
        }

        public static bool OBBIntersectsAABB(OrientedBoundingBox obb, BoundingBox aabb)
        {
            // Get the center and half-extents of the AABB
            Point3d aabbCenter = 0.5 * (aabb.Min + aabb.Max);
            Vector3d aabbHalfExtents = 0.5 * (aabb.Max - aabb.Min);

            // Compute rotation matrix expressing OBB in AABB coordinate frame
            Vector3d[] obbAxes = new Vector3d[3];
            obbAxes[0] = new Vector3d(obb.Rotation.M00, obb.Rotation.M10, obb.Rotation.M20);
            obbAxes[1] = new Vector3d(obb.Rotation.M01, obb.Rotation.M11, obb.Rotation.M21);
            obbAxes[2] = new Vector3d(obb.Rotation.M02, obb.Rotation.M12, obb.Rotation.M22);

            // AABB axes
            Vector3d[] aabbAxes = { Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis };

            // Translation vector from AABB center to OBB center
            Vector3d t = obb.Center - aabbCenter;

            // Project t onto AABB axes
            double[] tAABB = { t * aabbAxes[0], t * aabbAxes[1], t * aabbAxes[2] };

            double[,] R = new double[3, 3];
            double[,] AbsR = new double[3, 3];
            double EPSILON = 1e-6;

            // Compute rotation matrix and its absolute value
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    R[i, j] = obbAxes[i] * aabbAxes[j];
                    AbsR[i, j] = Math.Abs(R[i, j]) + EPSILON;
                }
            }

            // Test axes L = AABB axis
            for (int i = 0; i < 3; i++)
            {
                double ra = obb.HalfExtents.X * AbsR[0, i] + obb.HalfExtents.Y * AbsR[1, i] + obb.HalfExtents.Z * AbsR[2, i];
                double rb = aabbHalfExtents[i];
                if (Math.Abs(tAABB[i]) > ra + rb)
                {
                    return false;
                }
            }

            // Test axes L = OBB axis
            for (int i = 0; i < 3; i++)
            {
                double ra = obb.HalfExtents[i];
                double rb = aabbHalfExtents.X * AbsR[i, 0] + aabbHalfExtents.Y * AbsR[i, 1] + aabbHalfExtents.Z * AbsR[i, 2];
                double tOBB = t * obbAxes[i];
                if (Math.Abs(tOBB) > ra + rb)
                {
                    return false;
                }
            }

            // Test axis L = AABB axis[i] x OBB axis[j]
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    double ra = obb.HalfExtents[(i + 1) % 3] * AbsR[(i + 2) % 3, j] + obb.HalfExtents[(i + 2) % 3] * AbsR[(i + 1) % 3, j];
                    double rb = aabbHalfExtents[(j + 1) % 3] * AbsR[i, (j + 2) % 3] + aabbHalfExtents[(j + 2) % 3] * AbsR[i, (j + 1) % 3];
                    double tCross = Math.Abs(t[(i + 2) % 3] * R[(i + 1) % 3, j] - t[(i + 1) % 3] * R[(i + 2) % 3, j]);
                    if (tCross > ra + rb)
                    {
                        return false;
                    }
                }
            }

            // No separating axis found; boxes intersect
            return true;
        }

        public static bool BoundingBoxesIntersect(BoundingBox bb1, BoundingBox bb2)
        {
            return !(bb1.Max.X < bb2.Min.X || bb1.Min.X > bb2.Max.X ||
                     bb1.Max.Y < bb2.Min.Y || bb1.Min.Y > bb2.Max.Y ||
                     bb1.Max.Z < bb2.Min.Z || bb1.Min.Z > bb2.Max.Z);
        }

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

        // Helper function to get the lat/lon range of the boundary rectangle
        private static (double MinLat, double MaxLat, double MinLon, double MaxLon) GetBoundaryLatLonRange(Rectangle3d boundary)
        {
            // Get the EarthAnchorPoint from the active Rhino document
            EarthAnchorPoint earthAnchor = Rhino.RhinoDoc.ActiveDoc.EarthAnchorPoint;

            // Get the model to earth transformation
            Transform modelToEarth = earthAnchor.GetModelToEarthTransform(Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem);

            // Convert boundary rectangle corners to lat/lon
            Point3d[] rectangleCorners = new Point3d[4];
            for (int i = 0; i < 4; i++)
            {
                rectangleCorners[i] = boundary.Corner(i);
            }

            List<(double Lat, double Lon)> boundaryLatLon = new List<(double, double)>();

            foreach (Point3d corner in rectangleCorners)
            {
                // Transform the corner to Earth coordinates
                Point3d earthCorner = corner;
                earthCorner.Transform(modelToEarth);

                double cornerLatitude = earthCorner.Y;   // Latitude in degrees
                double cornerLongitude = earthCorner.X;  // Longitude in degrees

                boundaryLatLon.Add((cornerLatitude, cornerLongitude));
            }

            double minLat = boundaryLatLon.Min(p => p.Lat);
            double maxLat = boundaryLatLon.Max(p => p.Lat);
            double minLon = boundaryLatLon.Min(p => p.Lon);
            double maxLon = boundaryLatLon.Max(p => p.Lon);

            return (minLat, maxLat, minLon, maxLon);
        }

        // Helper function to get the lat/lon range of the tile's bounding volume
        private static (double MinLat, double MaxLat, double MinLon, double MaxLon) GetTileLatLonRange(BoundingVolumeDTO bbox)
        {
            // Extract the center and axes from the bounding volume DTO
            Point3d center = new Point3d(bbox.Box[0], bbox.Box[1], bbox.Box[2]);
            Vector3d halfAxisX = new Vector3d(bbox.Box[3], bbox.Box[4], bbox.Box[5]);
            Vector3d halfAxisY = new Vector3d(bbox.Box[6], bbox.Box[7], bbox.Box[8]);
            Vector3d halfAxisZ = new Vector3d(bbox.Box[9], bbox.Box[10], bbox.Box[11]);

            // Compute the eight corners of the OBB
            Point3d[] corners = new Point3d[8];
            int i = 0;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3d cornerOffset = (x * halfAxisX) + (y * halfAxisY) + (z * halfAxisZ);
                        Point3d corner = center + cornerOffset;
                        corners[i++] = corner;
                    }
                }
            }

            // Convert the corner points from ECEF to geodetic coordinates
            List<(double Lat, double Lon)> tileLatLon = new List<(double, double)>();
            foreach (Point3d corner in corners)
            {
                var (lat, lon, _) = EcefToGeodetic(corner.X, corner.Y, corner.Z);
                tileLatLon.Add((lat, lon));
            }

            double minLat = tileLatLon.Min(p => p.Lat);
            double maxLat = tileLatLon.Max(p => p.Lat);
            double minLon = tileLatLon.Min(p => p.Lon);
            double maxLon = tileLatLon.Max(p => p.Lon);

            return (minLat, maxLat, minLon, maxLon);
        }

        // Helper function to check for range overlap
        private static bool RangesOverlap(
            (double MinLat, double MaxLat, double MinLon, double MaxLon) boundaryRange,
            (double MinLat, double MaxLat, double MinLon, double MaxLon) tileRange)
        {
            // Normalize longitude ranges to 0° to 360°
            (double MinLon, double MaxLon) boundaryLonRange = NormalizeLongitudeRange(boundaryRange.MinLon, boundaryRange.MaxLon);
            (double MinLon, double MaxLon) tileLonRange = NormalizeLongitudeRange(tileRange.MinLon, tileRange.MaxLon);

            // Check latitude overlap using the general condition
            bool latOverlap = boundaryRange.MaxLat >= tileRange.MinLat && tileRange.MaxLat >= boundaryRange.MinLat;

            // Check longitude overlap, accounting for wrap-around
            bool lonOverlap = LongitudesOverlap(boundaryLonRange.MinLon, boundaryLonRange.MaxLon, tileLonRange.MinLon, tileLonRange.MaxLon);

            return latOverlap && lonOverlap;
        }


        // Function to normalize longitude to 0° to 360°
        private static (double MinLon, double MaxLon) NormalizeLongitudeRange(double minLon, double maxLon)
        {
            minLon = (minLon + 360) % 360;
            maxLon = (maxLon + 360) % 360;

            // Ensure minLon <= maxLon
            if (minLon > maxLon)
            {
                double temp = minLon;
                minLon = maxLon;
                maxLon = temp;
            }

            return (minLon, maxLon);
        }

        // Function to check longitude overlap, accounting for dateline crossing
        private static bool LongitudesOverlap(double minLon1, double maxLon1, double minLon2, double maxLon2)
        {
            // Handle cases where ranges might cross the 0°/360° point
            if (maxLon1 < minLon1) maxLon1 += 360;
            if (maxLon2 < minLon2) maxLon2 += 360;

            // Check for overlap
            bool overlap = maxLon1 >= minLon2 && maxLon2 >= minLon1;

            // Also check wrap-around overlap
            if (maxLon1 >= minLon2 + 360)
                overlap |= (maxLon1 - 360) >= minLon2 && maxLon2 >= (minLon1 - 360);

            if (maxLon2 >= minLon1 + 360)
                overlap |= (maxLon2 - 360) >= minLon1 && maxLon1 >= (minLon2 - 360);

            return overlap;
        }


        // Function to convert ECEF coordinates to geodetic coordinates (latitude, longitude, altitude)
        private static (double Latitude, double Longitude, double Altitude) EcefToGeodetic(double x, double y, double z)
        {
            // WGS84 ellipsoid constants
            double a = 6378137.0; // Semi-major axis in meters
            double e2 = 6.69437999014e-3; // First eccentricity squared

            double b = Math.Sqrt(a * a * (1 - e2));
            double ep = Math.Sqrt((a * a - b * b) / (b * b));
            double p = Math.Sqrt(x * x + y * y);
            double th = Math.Atan2(a * z, b * p);

            double lon = Math.Atan2(y, x);
            double lat = Math.Atan2((z + ep * ep * b * Math.Pow(Math.Sin(th), 3)),
                                    (p - e2 * a * Math.Pow(Math.Cos(th), 3)));

            double N = a / Math.Sqrt(1 - e2 * Math.Sin(lat) * Math.Sin(lat));
            double alt = p / Math.Cos(lat) - N;

            // Convert radians to degrees
            lat = lat * (180.0 / Math.PI);
            lon = lon * (180.0 / Math.PI);

            return (lat, lon, alt);
        }


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
}
