using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuxStructureLib
{
    /// <summary>
    /// 借组邻近图聚类
    /// </summary>
    public class ClusterbyProxiGraph
    {
        public ProxiGraph InitProxiGraph = null;//初始邻近图
        public List<GroupGraph> ResClustersList = null;//结果
        /// <summary>
        /// 构造函数
        /// </summary>
        public ClusterbyProxiGraph(ProxiGraph proxiGraph)
        {
            this.InitProxiGraph = proxiGraph;
            ResClustersList = new List<GroupGraph>();
        }
        /// <summary>
        /// 李志林2004-基于Alignment检测和Peso-Voronoi的分组
        /// </summary>
        /// <param name="thresholdD">距离阈值</param>
        /// <param name="thresholdA">面积阈值</param>
        /// <param name="Scale">目标比例尺</param>
        public void AlgClusterLI2004(double thresholdD, double thresholdA, double Scale)
        {
            //Z. Li, H. Yan, and T. Ai. “Automated building generalization based on urban morphology and gestalt
            //theory,"International Journal of Geographical Information Science , Vol. 18(5):513 –534, 2004
        }
        /// <summary>
        /// 采用与闫浩文2008类似的算法
        /// </summary>
        /// <param name="thresholdD">距离阈值</param>
        /// <param name="thresholdA">面积阈值</param>
        /// <param name="Scale">目标比例尺</param>
        public void AlgClusterYAN2008(double thresholdD,double thresholdA,double Scale)
        {
            if(this.InitProxiGraph==null||InitProxiGraph.EdgeList==null||this.InitProxiGraph.NodeList==null||this.InitProxiGraph.EdgeList.Count==0||this.InitProxiGraph.NodeList.Count==0)
                return;
            //第一步：初始分组（2-Object groups）
            foreach (ProxiEdge curEdge in InitProxiGraph.EdgeList)
            {
                GroupGraph curGroup = new GroupGraph();
                curGroup.EdgeList.Add(curEdge);
                curGroup.NodeList.Add(curEdge.Node1);
                curGroup.NodeList.Add(curEdge.Node1);
                curGroup.AveA = curEdge.Ske_Arc.GapArea;
                curGroup.AveD = curEdge.NearestEdge.NearestDistance;
                this.ResClustersList.Add(curGroup);//加入分组
            }

            //第二步：中间分离（intermediate groups）
            foreach (GroupGraph curGroup in ResClustersList)
            {
                //弱weak group
                if (curGroup.AveD > thresholdD && curGroup.AveA > thresholdA)
                {
                    //Delete
                    ResClustersList.Remove(curGroup);
                }
                //一般 average group
                else
                {
                    if (curGroup.AveD > thresholdD || curGroup.AveA > thresholdA)
                    {
                        curGroup.Type = "AVERAGE";
                    }
                    else if (curGroup.AveD <= thresholdD || curGroup.AveA <= thresholdA)
                    {
                        curGroup.Type = "STRONG";
                    }
                }

            }

           //



        }
        /// <summary>
        /// 寻找同时属于两个组的对象（以邻近图顶点指代）
        /// </summary>
        /// <param name="group1">组1</param>
        /// <param name="group2">组2</param>
        /// <returns></returns>
        private ProxiNode FindCommonNode(out GroupGraph group1, out GroupGraph group2)
        {
            group1 = null;
            group2 = null;
            return null;
        }
    }
}
