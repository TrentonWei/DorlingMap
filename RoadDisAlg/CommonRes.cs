using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;

namespace RoadDisAlg
{

    /// <summary>
    /// 受力模型枚举
    /// </summary>
    public enum ForceType
    {
        Vetex,
        Line,
        Combine,
        Max,
        Triangle,
        Interactive
    }

    public enum GradeModelType
    {
        Ratio,
        Squence,
        Grade,
        Interactive
    }

    public class CommonRes
    {
        /// <summary>
        /// 计算两点之间线段的长度
        /// </summary>
        /// <param name="point1">起点</param>
        /// <param name="point2">终点</param>
        /// <returns></returns>
        public static double LineLength(PointCoord point1, PointCoord point2)
        {
            double dx = point2.X - point1.X;
            double dy = point2.Y - point1.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            return len;
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
