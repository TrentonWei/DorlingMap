using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib.IO;
using ESRI.ArcGIS.Geometry;

namespace AuxStructureLib.ConflictLib
{
    /// <summary>
    /// Linear conflict class2014-2-26
    /// </summary>
    public class Conflict_L:ConflictBase
    {
        public List<TriNode> LeftPointList = null;
        public List<TriNode> RightPointList = null;
        public List<Triangle> TriangleList = null;
         /// <summary>
        ///Constructor
        /// </summary>
        /// <param name="skel_arc">Skeleton_Arc</param>
        /// <param name="leftPoint">conflicted vertexes on the left object</param>
        /// <param name="rightPoint">conflicted vertexes on the right objectt</param>
        public Conflict_L(Skeleton_Arc skel_arc, List<TriNode> leftPointList, List<TriNode> rightPointList, List<Triangle> TriangleList, string type, double t)
        {
            Skel_arc = skel_arc;
            LeftPointList = leftPointList;
            RightPointList = rightPointList;
            TriangleList = new List<Triangle>();
            Type = "LL";
            this.DisThreshold = t;
        }

        /// <summary>
        ///Constructor
        /// </summary>
        /// <param name="skel_arc">Skeleton_Arc</param>
        public Conflict_L(Skeleton_Arc skel_arc,double t)
        {
            Skel_arc = skel_arc;
            LeftPointList = new List<TriNode>();
            RightPointList = new List<TriNode>();
            TriangleList = new List<Triangle>();
            Type = "LL";
            this.DisThreshold = t;
        }

        /// <summary>
        /// 向左顶点数数组添加顶点
        /// </summary>
        /// <param name="point"></param>
        public void AddLeftConfPoint(TriNode point)
        {
            if(this.LeftPointList!=null)
                this.LeftPointList.Add(point);

        }

        /// <summary>
        /// 向左顶点数数组添加顶点
        /// </summary>
        /// <param name="point"></param>
        public void AddReftConfPoint(TriNode point)
        {
            if (this.RightPointList != null)
                this.RightPointList.Add(point);
        }
        /// <summary>
        /// 将冲突存入数据库MDB
        /// </summary>
        /// <param name="filepath"></param>
        public void WriteConflict(string filepath)
        {
            List<PolylineObject> PolylineList = new List<PolylineObject>();
            PolylineObject p1 = new PolylineObject();
            p1.PointList = this.LeftPointList;
            PolylineList.Add(p1);

            PolylineObject p2 = new PolylineObject();
            p2.PointList = this.RightPointList;
            PolylineList.Add(p2);

           AEIO.Open_WriteFeatures(filepath,
                      PolylineList,
                      @"Conflict",
                      esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
           AEIO.Open_WriteTriangles(filepath,
                     this.TriangleList,
                     @"ConflictTri",
                     esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
        }

        /// <summary>
        /// 将冲突写入SHP
        /// </summary>
        /// <param name="filepath">写冲突到SHP</param>
        /// <param name="i">冲突号</param>
        public void WriteConflict2Shp(string filepath,int i)
        {
            List<PolylineObject> PolylineList = new List<PolylineObject>();
            PolylineObject p1 = new PolylineObject();
            p1.PointList = this.LeftPointList;
            PolylineList.Add(p1);

            PolylineObject p2 = new PolylineObject();
            p2.PointList = this.RightPointList;
            PolylineList.Add(p2);

            AEIO.Create_Write_FeatureClass(filepath,
                       PolylineList,
                       @"Conflict"+i.ToString(),
                       esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
            AuxStructureLib.Triangle.Create_WriteTriange2Shp(filepath,
                      @"ConflictTri" + i.ToString(), this.TriangleList,
                      esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
        }
    }
}
