using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DisplaceAlgLib
{
    public class BoundPointDisplaceParams
    {
        private int index;   //该点的索引-在刚度矩阵中的索引[i/3]
        private double dx;   //X方向移位量
        private double dy;   //Y方向移位量
        private double a;    //角度
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="_index"></param>
        /// <param name="_dx"></param>
        /// <param name="_dy"></param>
        /// <param name="_a"></param>
        public BoundPointDisplaceParams(int _index, double _dx, double _dy,double _a)
        {
            index = _index;
            dx = _dx;
            dy = _dy;
            a = _a;
        }
        /// <summary>
        /// 该点的索引
        /// </summary>
        public int Index
        {
            get
            {
                return index;
            }
            set
            {
                if (index == value)
                    return;
                index = value;
            }
        }
        /// <summary>
        /// 该点的X方向移位量
        /// </summary>
        public double Dx
        {
            get
            {
                return dx;
            }
            set
            {
                if (dx == value)
                    return;
                dx = value;
            }
        }
        /// <summary>
        /// 该点的Y方向移位量
        /// </summary>
        public double Dy
        {
            get
            {
                return dy;
            }
            set
            {
                if (dy == value)
                    return;
                dy = value;
            }
        }
        /// <summary>
        /// 该点角度变形
        /// </summary>
        public double A
        {
            get
            {
                return a;
            }
            set
            {
                if (a == value)
                    return;
                a = value;
            }
        }

    }
}
