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
    /// 多边形对象
    /// </summary>
         [Serializable]
    public abstract class SDS_Polygon : ISDS_MapObject
    {
        public int ID;
        public List<TriNode> PointList = null;
        public List<SDS_Node> Points = null;
        public List<SDS_Edge> Edges = null;//边
        public List<bool> Wises = null;    //边的方向
        public List<SDS_Triangle> Triangles = null; //包含的三角形


        #region ISDS_MapObject 成员

        public string ObjType
        {
            get { return this.ToString(); }
        }

        #endregion

        #region ISDS_MapObject 成员

        /// <summary>
        /// 面平移
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        public void Translate(double dx, double dy)
        {
            if (dx != 0 || dy != 0)
            {
                foreach (SDS_Node vetex in this.Points)
                {
                    vetex.X += dx;
                    vetex.Y += dy;
                }
            }
        }

        #endregion

        #region ISDS_MapObject 成员

        private double someAtrValue;
        public double SomeAtriValue
        {
            get
            {
                return this.someAtrValue;
            }
            set
            {
                this.someAtrValue = value;
            }
        }

        #endregion


        #region ISDS_MapObject 成员


        public int AID
        {
            get { return ID; }
        }

        #endregion
    }
}
