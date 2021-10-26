using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlgEFMDisplace
{
    /// <summary>
    /// 结构体
    /// </summary>
    public struct Force
    {
        public double F;
        public double Sin;
        public double cos;
        public double Fx;
        public double Fy;
        public double a;
    }

    /// <summary>
    /// 受力模型枚举
    /// </summary>
    public enum ForceType
    {
        Vetex,
        Line,
        Combine,
        Max
    }

    /// <summary>
    /// 计算受力
    /// </summary>
    public class ComForce
    {
        public static RoadNetWork _NetWork = null;                   //道路网络对象
        public static double minDis = 0.2;                           //图上最小间距单位为毫米
        public static ForceType ForceType = ForceType.Vetex;
        public static Force[] forceList = null;                      //每个 点上的受力


    }
}
