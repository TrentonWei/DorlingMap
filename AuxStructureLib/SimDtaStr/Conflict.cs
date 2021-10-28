using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;
using ESRI.ArcGIS.Geometry;

namespace AuxStructureLib
{

    /// <summary>
    /// 冲突描述类
    /// </summary>
    [Serializable]
    public class Conflict
    {
        public ISDS_MapObject Obj1 = null;//对象1
        public ISDS_MapObject Obj2 = null;//对象2
        public double Distance = -1;
        public double cost = -1;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="obj1">对象1</param>
        /// <param name="obj2">对象2</param>
        public Conflict(ISDS_MapObject obj1, ISDS_MapObject obj2)
        {
            Obj1 = obj1;
            Obj2 = obj2;
        }
        /// <summary>
        /// 冲突
        /// </summary>
        public string ConflictType
        {
            get
            {
                if (Obj1.ToString() == @" AuxStructureLib.SDS_PolylineObj" && Obj2.ToString() == @"AuxStructureLib.SDS_PolylineObj")
                {
                    return @"LL";
                }
                else if (Obj1.ToString() == @"AuxStructureLib.SDS_PolygonO" && Obj2.ToString() == @"AuxStructureLib.SDS_PolygonO")
                {
                    return @"PP";
                }
                else
                {
                    return @"PL";
                }
            }
        }

        /// <summary>
        /// 输出冲突邻近图
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <param name="map"></param>
        /// <param name="pg"></param>
        /// <param name="prj"></param>
        public static void WriteConflict2File(List<Conflict> Conflicts, string filePath, string fileName, SMap map, ProxiGraph pg, ISpatialReference prj)
        {
            ProxiGraph pg1 = new ProxiGraph();
            pg1.CreateProxiGraphfrmConflicts(map, Conflicts);
            foreach (ProxiEdge edge in pg1.EdgeList)
            {
                foreach (ProxiEdge edge1 in pg.EdgeList)
                {
                    if ((edge.Node1.TagID == edge1.Node1.TagID && edge.Node1.FeatureType == edge1.Node1.FeatureType && edge.Node2.TagID == edge1.Node2.TagID && edge.Node2.FeatureType == edge1.Node2.FeatureType)
                        || (edge.Node1.TagID == edge1.Node2.TagID && edge.Node1.FeatureType == edge1.Node2.FeatureType && edge.Node2.TagID == edge1.Node1.TagID && edge.Node2.FeatureType == edge1.Node1.FeatureType))
                    {
                        edge.NearestEdge = edge1.NearestEdge;
                    }
                }
            }
            pg1.WriteProxiGraph2Shp(filePath, fileName, prj);
        }
    }
}
