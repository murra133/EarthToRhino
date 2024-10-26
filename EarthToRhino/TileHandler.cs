using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthToRhino
{
    public class TileHandler
    {
        public TileHandler()
        {

        }

        public void DownloadTiles()
        {

        }

        public void GetRoot()
        {
            string rootObject = WebAPI.Get(RoutesController.Root, true);
        }
    }
}
