using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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


        /// <summary>
        /// convert your target latitude and longitude to ECEF coordinates
        /// Earth-Centered, Earth-Fixed (ECEF) coordinate system
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <param name="alt"></param>
        /// <returns></returns>
        public static Vector3 LatLonToECEF(double lat, double lon, double alt = 0)
        {
            // WGS84 ellipsoid constants
            double a = 6378137; // Equatorial radius
            double e2 = 6.69437999014e-3; // Square of eccentricity

            double latRad = lat * Math.PI / 180.0;
            double lonRad = lon * Math.PI / 180.0;

            double N = a / Math.Sqrt(1 - e2 * Math.Sin(latRad) * Math.Sin(latRad));

            double x = (N + alt) * Math.Cos(latRad) * Math.Cos(lonRad);
            double y = (N + alt) * Math.Cos(latRad) * Math.Sin(lonRad);
            double z = ((1 - e2) * N + alt) * Math.Sin(latRad);

            return new Vector3((float)x, (float)y, (float)z);
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
    }
}
