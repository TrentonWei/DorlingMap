using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlgEMLib
{
    /// <summary>
    /// 顶点的受力
    /// </summary>
    public class Force
    {
        public int ID=-1;
        public double F=0;
        public double a=0;//方向角
        public double Fx=0;
        public double Fy=0;
        public double QanSumF = 0;
        public double RF = 0; // curForce.RF = curForce.F / curForce.QanSumF;//越大说明越该优先移位
        public int SID = -1;
        public double Sin=-1;
        public double Cos=-1;
        public bool IsBouldPoint = false;
        public double distance = -1;
        public double w = 1;
  
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id"></param>
        public Force(int id)
        {
            this.ID = id;
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id"></param>
        public Force(int id,double fx,double fy,double f)
        {
            this.ID = id;
            this.Fx = fx;
            this.Fy = fy;
            this.F = f;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id"></param>
        public Force(int id, double fx, double fy, double s,double c, double f)
        {
            this.ID = id;
            this.Fx = fx;
            this.Fy = fy;
            this.F = f;
            this.Sin = s;
            this.Cos = c;
        }

        /// <summary>
        /// 拷贝构造函数
        /// </summary>
        /// <param name="id"></param>
        public Force(Force source)
        {
            this.ID = source.ID;
            this.Fx = source.Fx;
            this.Fy = source.Fy;
            this.F = source.F;
            this.Sin = source.Sin;
            this.Cos = source.Cos;
        }

    }
}
