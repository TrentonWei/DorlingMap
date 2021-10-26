using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System.Windows.Forms;

namespace AuxStructureLib
{

    /// <summary>
    /// 空白区域对象
    /// </summary>
         [Serializable]
    public class SDS_PolygonF : SDS_Polygon
    {
        public SDS_PolygonF(int id, List<TriNode> pointList)
        {
            this.ID = id;
            this.PointList = pointList;
            Edges = new List<SDS_Edge>();
            Wises = new List<bool>();
            Triangles = new List<SDS_Triangle>();
        }
    }
}
