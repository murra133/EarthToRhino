using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace EarthToRhino
{

    public class BoundingBox
    {
        public Vector3 Center { get; }
        public Vector3 XHalf { get; }
        public Vector3 YHalf { get; }
        public Vector3 ZHalf { get; }

        public BoundingBox(Vector3 center, Vector3 xHalf, Vector3 yHalf, Vector3 zHalf)
        {
            Center = center;
            XHalf = xHalf;
            YHalf = yHalf;
            ZHalf = zHalf;
        }

        public BoundingBox(BoundingVolumeDTO bbox)
        {
            // Parse the bounding volume to create an OrientedBoundingBox
            Vector3 center = new Vector3((float)bbox.Box[0], (float)bbox.Box[1], (float)bbox.Box[2]);
            Vector3 xHalf = new Vector3((float)bbox.Box[3], (float)bbox.Box[4], (float)bbox.Box[5]);
            Vector3 yHalf = new Vector3((float)bbox.Box[6], (float)bbox.Box[7], (float)bbox.Box[8]);
            Vector3 zHalf = new Vector3((float)bbox.Box[9], (float)bbox.Box[10], (float)bbox.Box[11]);

            Center = center;
            XHalf = xHalf;
            YHalf = yHalf;
            ZHalf = zHalf;
        }

        public BoundingBox(List<Point3d> bbox)
        {
            if (bbox == null || bbox.Count == 0)
                throw new ArgumentException("Bounding box point list cannot be null or empty.");

            // Find the minimum and maximum points in each direction
            double minX = bbox.Min(p => p.X);
            double minY = bbox.Min(p => p.Y);
            double minZ = bbox.Min(p => p.Z);

            double maxX = bbox.Max(p => p.X);
            double maxY = bbox.Max(p => p.Y);
            double maxZ = bbox.Max(p => p.Z);

            // Calculate the center point
            Center = new Vector3(
                (float)((minX + maxX) / 2),
                (float)((minY + maxY) / 2),
                (float)((minZ + maxZ) / 2)
            );

            // Calculate half-extents in each direction
            XHalf = new Vector3((float)((maxX - minX) / 2), 0, 0);
            YHalf = new Vector3(0, (float)((maxY - minY) / 2), 0);
            ZHalf = new Vector3(0, 0, (float)((maxZ - minZ) / 2));
        }

        public Vector3[] GetCorners()
        {
            return new[]
            {
            Center + XHalf + YHalf + ZHalf,
            Center + XHalf + YHalf - ZHalf,
            Center + XHalf - YHalf + ZHalf,
            Center + XHalf - YHalf - ZHalf,
            Center - XHalf + YHalf + ZHalf,
            Center - XHalf + YHalf - ZHalf,
            Center - XHalf - YHalf + ZHalf,
            Center - XHalf - YHalf - ZHalf
        };
        }
    }

    public class Region
    {
        public List<Vector3> Corners { get; }

        public Region(List<Vector3> corners)
        {
            if (corners.Count != 4) throw new ArgumentException("Region must be defined by 4 corners.");
            Corners = corners;
        }

        public Region(Rectangle3d boundary)
        {
            var corners = GeoHelper.ConvertBoundaryToECEF(boundary);
            Corners = corners.ConvertAll(ConvertPoint3dToVector3);
        }

        private static Vector3 ConvertPoint3dToVector3(Point3d point)
        {
            return new Vector3((float)point.X, (float)point.Y, (float)point.Z);
        }
    }

    public class EcefChecker
    {
        public static bool IsBoundingBoxInRegion(BoundingBox bbox, Region region)
        {
            // Compute the normal vectors for the region's planes
            Vector3 normal1 = Vector3.Cross(region.Corners[1] - region.Corners[0], region.Corners[2] - region.Corners[0]);
            Vector3 normal2 = Vector3.Cross(region.Corners[2] - region.Corners[1], region.Corners[3] - region.Corners[1]);
            Vector3 normal3 = Vector3.Cross(region.Corners[3] - region.Corners[2], region.Corners[0] - region.Corners[2]);
            Vector3 normal4 = Vector3.Cross(region.Corners[0] - region.Corners[3], region.Corners[1] - region.Corners[3]);

            // Extend region's boundary outward in the direction of the Earth’s center
            Vector3 origin = Vector3.Zero;
            Vector3 outward1 = Vector3.Normalize(region.Corners[0] - origin);
            Vector3 outward2 = Vector3.Normalize(region.Corners[1] - origin);
            Vector3 outward3 = Vector3.Normalize(region.Corners[2] - origin);
            Vector3 outward4 = Vector3.Normalize(region.Corners[3] - origin);

            // Check each corner of the bounding box
            foreach (var corner in bbox.GetCorners())
            {
                // Project corner onto each extended plane
                if (IsOutsidePlane(corner, region.Corners[0], normal1, outward1) &&
                    IsOutsidePlane(corner, region.Corners[1], normal2, outward2) &&
                    IsOutsidePlane(corner, region.Corners[2], normal3, outward3) &&
                    IsOutsidePlane(corner, region.Corners[3], normal4, outward4))
                {
                    return true; // At least one corner is within the extended region
                }
            }
            return false;
        }

        private static bool IsOutsidePlane(Vector3 point, Vector3 planePoint, Vector3 normal, Vector3 outward)
        {
            // Compute the vector from the plane point to the point
            Vector3 vec = point - planePoint;

            // Check if point is inside the plane based on normal and outward direction
            return Vector3.Dot(vec, normal) >= 0 || Vector3.Dot(vec, outward) >= 0;
        }
    }


    public static class CoordinateConverter
    {
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

                // Convert to ECEF coordinates as Vector3
                Vector3 cornerECEF = LatLonToECEF(cornerLatitude, cornerLongitude, cornerAltitude);
                rectangleCornersECEF.Add(ConvertVector3ToPoint3d(cornerECEF));
            }

            return rectangleCornersECEF;
        }

        private static Vector3 LatLonToECEF(double latitude, double longitude, double altitude)
        {
            // Perform conversion from latitude, longitude, and altitude to ECEF
            // Calculation here depends on the earth model (e.g., WGS-84)
            double a = 6378137.0; // Equatorial radius in meters
            double e2 = 0.00669437999014; // Eccentricity squared

            double latRad = latitude * (Math.PI / 180);
            double lonRad = longitude * (Math.PI / 180);

            double N = a / Math.Sqrt(1 - e2 * Math.Sin(latRad) * Math.Sin(latRad));

            double x = (N + altitude) * Math.Cos(latRad) * Math.Cos(lonRad);
            double y = (N + altitude) * Math.Cos(latRad) * Math.Sin(lonRad);
            double z = ((1 - e2) * N + altitude) * Math.Sin(latRad);

            return new Vector3((float)x, (float)y, (float)z);
        }

        public static Vector3d ConvertVector3ToVector3d(Vector3 vector)
        {
            return new Vector3d(vector.X, vector.Y, vector.Z);
        }

        public static Point3d ConvertVector3ToPoint3d(Vector3 vector)
        {
            return new Point3d(vector.X, vector.Y, vector.Z);
        }
    }
}
