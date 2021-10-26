using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlgIterative.AlgGA
{
    public class ContinuousDVT
    {
        public double X_r = -1;
        public double Y_a = -1;

        public bool IsPolarCoors = true;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="x_r">X或极径r</param>
        /// <param name="y_a">Y或方位角a</param>
        /// <param name="IsPolarCoors">是否极坐标</param>
        public ContinuousDVT(double x_r, double y_a, bool isPolarCoors)
        {
            this.X_r = x_r;
            this.Y_a = y_a;
            this.IsPolarCoors = isPolarCoors;
        }
    }
}
