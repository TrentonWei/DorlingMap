using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace AlgEFMDisplace
{
    /// <summary>
    /// 道路关联点
    /// </summary>
    public class ConnNode
    {
        public int PointID;
        public List<int> ConRoadList = null; //所关联的道路对象
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="pID">对应顶点的序号</param>
        public ConnNode(int pID)
        {
            PointID = pID;
            ConRoadList = new List<int>();
        }
    }
}
