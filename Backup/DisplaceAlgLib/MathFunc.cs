using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;

namespace DisplaceAlgLib
{
    public class MathFunc
    {
        /// <summary>
        /// 求一个向量的方位角的正弦、余弦值和长度
        /// </summary>
        /// <param name="p1">起点</param>
        /// <param name="p2">终点</param>
        /// <param name="sin">正弦</param>
        /// <param name="cos">余弦</param>
        /// <param name="len">长度</param>
        public static void SinCosofVector(IPoint p1, IPoint p2, out double  sin, out double cos,out double len)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            len = Math.Sqrt(dx * dx + dy * dy);

            sin = dy / len;
            cos = dx / len;
        }
        /// <summary>
        /// 计算两点之间线段的长度
        /// </summary>
        /// <param name="point1">起点</param>
        /// <param name="point2">终点</param>
        /// <returns></returns>
        public static double LineLength(IPoint point1, IPoint point2)
        {
            double dx = point2.X - point1.X;
            double dy = point2.Y - point1.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            return len;
        }


    }
}
