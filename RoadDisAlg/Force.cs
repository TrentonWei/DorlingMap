using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoadDisAlg
{
    /// <summary>
    /// 顶点的受力
    /// </summary>
    public class Force
    {
        public int ID;//顶点编号
        public double F;           
        public double Sin;
        public double cos;
        public double Fx;
        public double Fy;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">顶点编号</param>
        public Force(int id)
        {
            this.ID = id;
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        public Force()
        {
        }

    }
}
