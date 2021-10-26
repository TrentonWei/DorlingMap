using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuxStructureLib
{
    /// <summary>
    /// 候选状态位置
    /// </summary>
    public class TrialPosition
    {
        public int ID;
        public double Distance;
        public double Dx;
        public double Dy;
        public double Angle;
        public double cost=0;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">ID号</param>
        /// <param name="distance">距离</param>
        /// <param name="dx">X方向偏移</param>
        /// <param name="dy">Y方向偏移</param>
        /// <param name="angle">角度</param>
        public TrialPosition(int id, double distance, double dx, double dy, double angle)
        {
            ID = id;
            Distance = distance;
            Dx = dx;
            Dy = dy;
            Angle = angle;
        }
    }

}
