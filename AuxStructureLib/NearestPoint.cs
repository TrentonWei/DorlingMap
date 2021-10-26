using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuxStructureLib
{
    /// <summary>
    /// 多边形与多边形之间最近点
    /// </summary>
    public class NearestPoint : Node
    {
        /// <summary>
        /// 最近点
        /// </summary>
        /// <param name="id">编号</param>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        public NearestPoint(int id,double x, double y)
        {
            ID = id;
            X = x;
            Y = y;
        }
        /// <summary>
        /// 最近距离点
        /// </summary>
        public NearestPoint()
        {
        }
    }
}
