using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace RoadDisAlg
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
        /// <summary>
        /// 返回点ID为PID的结点
        /// </summary>
        /// <param name="nodeList">结点列表</param>
        /// <param name="pID">点ID</param>
        /// <returns></returns>
        public static ConnNode GetConnNodebyPID(List<ConnNode> nodeList, int pID)
        {
            foreach(ConnNode curNode in nodeList)
            {
                if(curNode.PointID==pID)
                {
                    return curNode;
                }
            }
            return null;
        }
    }
}
