using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlgEFMDisplace
{
    /// <summary>
    /// 三角形
    /// </summary>
    public class Triangular
    {
        public int point1;
        public int point2;
        public int point3;

        public float E = 20F;
        public float ν = 0.2F;

        public Triangular(int p1, int p2, int p3)
        {
            point1 = p1;
            point2 = p2;
            point3 = p3;
        }
        /// <summary>
        /// 计算面积
        /// </summary>
        /// <param name="tri">三角形</param>
        /// <returns>面积值</returns>
        public static double ComArea(Triangular tri)
        {
            return 0.0;
        }
        /// <summary>
        /// 计算面积
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="x3"></param>
        /// <param name="y3"></param>
        /// <returns>面积值</returns>
        public static double ComArea(float x1,float y1,float x2,float y2,float x3,float y3)
        {
            return 0.0;
        }
    }
}
