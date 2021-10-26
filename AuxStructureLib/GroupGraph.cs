using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuxStructureLib
{
    /// <summary>
    /// 用于聚类的分组图-用于Yan2008算法
    /// 2013-10
    /// Liuygis
    /// </summary>
    public  class GroupGraph:ProxiGraph
    {
        public double AveD;  //平均距离
        public double AveA;  //平均间隔面积
        public string Type; //“STRONG ；WEAK；AVERAGE”
    }
}
