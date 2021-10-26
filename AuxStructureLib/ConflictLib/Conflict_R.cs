using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib.IO;
using ESRI.ArcGIS.Geometry;

namespace AuxStructureLib.ConflictLib
{
    /// <summary>
    /// Rigid conflict class 2014-2-26
    /// </summary>
    public class Conflict_R : ConflictBase
    {
        public TriNode LeftPoint = null;
        public TriNode RightPoint = null;
        public double Distance;
        public Triangle MinTriangle = null;
        /// <summary>
        ///Constructor
        /// </summary>
        /// <param name="skel_arc">Skeleton_Arc</param>
        /// <param name="leftPoint">Nearest point on left object</param>
        /// <param name="rightPoint">Nearest point on right object</param>
        public Conflict_R(Skeleton_Arc skel_arc, TriNode leftPoint, TriNode rightPoint,Triangle minTriangle, double distance,double t)
        {
            Skel_arc = skel_arc;
            LeftPoint = leftPoint;
            RightPoint = rightPoint;
            MinTriangle = minTriangle;
            if (this.Skel_arc.LeftMapObj.FeatureType == FeatureType.PolylineType || this.Skel_arc.RightMapObj.FeatureType == FeatureType.PolylineType)
            {
                this.Type = "RL";
            }
            else
            {
                this.Type = "RR";
            }
            this.Distance = distance;
            this.DisThreshold = t;

        }

        /// <summary>
        /// 输出冲突
        /// </summary>
        /// <param name="filepath"></param>
        public void WriteConflict(string filepath)
        {
            List<PolylineObject> PolylineList = new List<PolylineObject>();
            PolylineObject p = new PolylineObject();
            p.PointList = new List<TriNode>();
            p.PointList.Add(this.LeftPoint);
            p.PointList.Add(this.RightPoint);
            PolylineList.Add(p);
          
            AEIO.Open_WriteFeatures(filepath,
                       PolylineList,
                       @"Conflict",
                       esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);

        }
    }
}
