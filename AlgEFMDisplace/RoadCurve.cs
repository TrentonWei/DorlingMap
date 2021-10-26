using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace AlgEFMDisplace
{
    /// <summary>
    /// 道路弯曲段
    /// </summary>
    public class RoadCurve
    {
        public List<int> PointList = null;          //包含的顶点编号列表
        public Road Road;                           //所属的道路对象
        public double k;                            //曲率
        public double a;                             //弹性参数
        public double b;                             //刚性参数                     

        /// <summary>
        ///  构造函数
        /// </summary>
        /// <param name="road">所属的道路对象</param>
        public RoadCurve(Road road)
        {
            Road = road;
            PointList = new List<int>();
        }
        /// <summary>
        /// 获取顶点坐标
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public PointCoord GetCoord(int i)
        {
            PointCoord point;
            try
            {
                point = this.Road.NetWork.PointList[this.PointList[i]];
            }
            catch
            {
                return null;
            }
            return point;
        }

        /// <summary>
        /// 计算曲率
        /// </summary>
        /// <returns>如何返回false说明计算中出错</returns>
        public bool ComQulv()
        {
            double l = 0.0;
            double d = 0.0;
            if (this.PointList == null || this.PointList.Count == 0)
                return false;
            else
            {

                int n=this.PointList.Count;
                d = this.ComDis(GetCoord(0), GetCoord(n-1));
                if (Math.Abs(d + 1.0) <= 0.0001)
                    return false;
                for(int i=0;i<n-1;i++)
                {
                    double dd = ComDis(GetCoord(i), GetCoord(i+1));
                    if (Math.Abs(dd + 1.0) <= 0.0001)
                        return false;
                    l += dd;
                }
                this.k = l / d;             
            }
            return true;
        }

        /// <summary>
        /// 计算弯曲段的形状参数
        /// </summary>
        public void Comab()
        {
            double f= this.Comy();
            this.a = this.Road.RoadGrade.a * f;
            this.b = this.Road.RoadGrade.b * f;
        }

        /// <summary>
        /// 抛物线函数
        /// </summary>
        /// <returns></returns>
        private double Comy()
        {
            double c = AlgSnakes.c;
            double k0 = AlgSnakes.k0;
            double k1 = AlgSnakes.k1;
            double k2 = AlgSnakes.k2;
            double f = 0;
            if (AlgSnakes.isParabola == true)
            {
                if (k < k0)
                {
                    f = 4 * (c - 1) * Math.Pow((this.k - ((1 + k0) / 2)), 2.0) / Math.Pow((k0 - 1), 2.0) + 1;
                }
                else
                {
                    f = c;
                }
            }
            else
            {
                if (k >= k1 && k <= k2)
                {
                    f = 1;
                }

                else
                {
                    f = c;
                }
            }
            return f;
        }

        /// <summary>
        /// 计算两点间距离
        /// </summary>
        /// <param name="p1">第一个点</param>
        /// <param name="p2">第二个点</param>
        /// <returns></returns>
        private double ComDis(PointCoord p1,PointCoord p2)
        {
            if (p1 == null || p2 == null)
                return -1.0;
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            double d = Math.Sqrt(dx * dx + dy * dy);
            return d;
        }
    }
}
