using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;

namespace RoadDisAlg
{
    /// <summary>
    /// 顶点坐标及编号
    /// </summary>
    public class PointCoord
    {
        public double X;
        public double Y;
        public int ID;
        public double SylWidth=-1.0;
        public int tagID=-1;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="x">X坐标值</param>
        /// <param name="y">Y坐标值</param>
        /// <param name="id">编号</param>
        ///  /// <param name="sylWidth">所属道路符号宽度的最大值</param>
        public PointCoord(double x, double y, int id, double sylWidth)
        {
            X = x;
            Y = y;
            ID = id;
            if (sylWidth > SylWidth)
            {
                SylWidth = sylWidth;
            }
        }

        public PointCoord()
        {

        }
        /// <summary>
        /// 返回Esri点对象
        /// </summary>
        public IPoint EsriPoint
        {
            get
            {
                IPoint p=new PointClass();
                p.PutCoords(this.X,this.Y);
                return p;
               
            }
        }
    }
}
