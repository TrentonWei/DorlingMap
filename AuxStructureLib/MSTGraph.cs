using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;

namespace AuxStructureLib
{
    /// <summary>
    /// 最小生成树，Prim算法，并聚类
    /// </summary>
    public class MSTGraph
    {
        public double[,] AdjMatrix = null;   //邻接矩阵
        public ProxiGraph MST = null;        //最小生成树

        /// <summary>
        /// 从邻近图创建邻近矩阵
        /// </summary>
        /// <param name="pg">邻近图</param>
        public void CreateAdjMatrixfrmProximityGraph(ProxiGraph pg)
        {
           // pg.CalWeightbyNearestDistance();
            int n = pg.NodeList.Count;
            this.AdjMatrix=new double[n,n];
            //初始化矩阵值，对角线上为0，其他地方为无穷大
            for(int i=0;i<n;i++)
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                        AdjMatrix[i, j] = 0;
                    else
                        AdjMatrix[i, j] = double.PositiveInfinity;
                }

            //给存在边的矩阵元素赋值
            foreach (ProxiEdge edge in pg.EdgeList)
            {
                int i = edge.Node1.ID;
                int j = edge.Node2.ID;
                double w=edge.NearestEdge.NearestDistance;
                AdjMatrix[i, j] = w;
                AdjMatrix[j, i] = w;
            }
        }

        /// <summary>
        /// Prim生成最小生成树
        /// </summary>
        /// <param name="pg"></param>
        /// <returns></returns>
        public void AlgPrim(ProxiGraph pg)
        {
            MST = new ProxiGraph();
            MST.NodeList = pg.NodeList;
            int n = pg.NodeList.Count;
            double[] lowcost = new double[n];//记录最小权重
            int[] closest = new int[n];      //记录最近边
            double min=double.PositiveInfinity;
            int k = 0;
            //设初值
            for (int i = 1; i < n; i++)
            {
                lowcost[i] = this.AdjMatrix[0, i];
                closest[i] = 0;
            }
            lowcost[0] = 0;
            for (int i = 1; i < n; i++)
            {
                min = double.PositiveInfinity;
                k = 0;
                //寻找满足边的一个顶点在U一个顶点在V的最小边
                for (int j = 1; j < n; j++)
                {
                    if (lowcost[j] < min && lowcost[j] != 0)
                    {
                        min = lowcost[j];
                        k = j;
                    }
                }

                lowcost[k] = 0;//顶点k加入U
                //修改顶点k到其它顶点边的权重
                for (int j = 1; j < n; j++)
                {
                    if (AdjMatrix[k, j] < lowcost[j])
                    {
                        lowcost[j] = AdjMatrix[k, j];
                        closest[j] = k;
                    }
                }
            }
            for (int i = 1; i < n; i++)
            {
                int j = closest[i];
                ProxiEdge edge = pg.GetEdgebyNodeIndexs(i, j);
                if (edge != null)
                {
                    MST.EdgeList.Add(edge);
                }
            }
        }
        /// <summary>
        /// 获取具有最大权重的边
        /// </summary>
        /// <returns>最大边</returns>
        private ProxiEdge GetEdgeofMaximunW(double thresholdW,ProxiGraph PG)
        {
           if( PG.EdgeList==null||PG.EdgeList.Count==0) return null;
            double max=thresholdW;
            ProxiEdge resEdge = null;
            foreach (ProxiEdge edge in PG.EdgeList)
            {
                if (edge.Weight > max)
                {
                    resEdge = edge;
                    max = edge.Weight;
                }
            }
            return resEdge;
        }

        /// <summary>
        /// Given a threshold weight, the graph is split by eliminating the edges which have 
        ///a weight higher than the threshold. This creates a hierarchy of graphs, with as many subgraphs as there are 
        ///isolated groups of nodes created by the edge elimination. This is used for identifying clusters of features.
        /// </summary>
        /// <param name="W">权重</param>
        /// <returns>返回结果图</returns>
        private ProxiGraph SplitByWeight(double thresholdW, ProxiGraph mst)
        {
            if (mst == null || mst.EdgeList == null || mst.NodeList == null || mst.NodeList.Count == 0 || mst.EdgeList.Count == null) return null;
            ProxiGraph curGraph = null;
            ProxiEdge curEdge = GetEdgeofMaximunW(thresholdW, mst);

            if (curEdge != null)
            {
                curGraph = new ProxiGraph();
                // curGraph.ParentGraph = null;
                curGraph.SubGraphs = new List<ProxiGraph>();
                curGraph.EdgeList.Add(curEdge);
                Queue<ProxiNode> NodeQueue = new Queue<ProxiNode>();
                Queue<ProxiEdge> EdgeQueue = new Queue<ProxiEdge>();
                ProxiNode node1 = curEdge.Node1;
                ProxiNode node2 = curEdge.Node2;
                ProxiNode curNode = null;
                ProxiEdge curE = null;
                List<ProxiEdge> curEdgeList = null;

                ProxiGraph childGraph1 = new ProxiGraph();
                ProxiGraph childGraph2 = new ProxiGraph();

                childGraph1.NodeList.Add(node1);
                NodeQueue.Enqueue(node1);
                EdgeQueue.Enqueue(curEdge);
                while (NodeQueue.Count > 0)
                {
                    curNode = NodeQueue.Dequeue();
                    curE = EdgeQueue.Dequeue();
                    curEdgeList = mst.GetEdgesbyNode(curNode);
                    if (curEdgeList != null)
                    {
                        foreach (ProxiEdge e in curEdgeList)
                        {
                            if (e != curE)
                            {
                                ProxiNode n = e.GetNode(curNode);
                                childGraph1.NodeList.Add(n);
                                childGraph1.EdgeList.Add(e);
                                NodeQueue.Enqueue(n);
                                EdgeQueue.Enqueue(e);
                            }
                        }
                    }
                }

                childGraph2.NodeList.Add(node2);
                NodeQueue.Enqueue(node2);
                EdgeQueue.Enqueue(curEdge);
                while (NodeQueue.Count > 0)
                {
                    curNode = NodeQueue.Dequeue();
                    curE = EdgeQueue.Dequeue();
                    curEdgeList = mst.GetEdgesbyNode(curNode);
                    if (curEdgeList != null)
                    {
                        foreach (ProxiEdge e in curEdgeList)
                        {
                            if (e != curE)
                            {
                                ProxiNode n = e.GetNode(curNode);
                                childGraph2.NodeList.Add(n);
                                childGraph2.EdgeList.Add(e);
                                NodeQueue.Enqueue(n);
                                EdgeQueue.Enqueue(e);
                            }
                        }
                    }
                }
                childGraph1 = SplitByWeight(thresholdW, childGraph1);
                childGraph1.ParentGraph = curGraph;

                childGraph2 = SplitByWeight(thresholdW, childGraph2);
                childGraph2.ParentGraph = curGraph;

                curGraph.SubGraphs.Add(childGraph1);
                curGraph.SubGraphs.Add(childGraph2);
            }

            //不拆分的情况
            else
            {
                curGraph = mst;
                //mst.ParentGraph = curGraph;
                mst.SubGraphs = null;
            }
            return curGraph;
        }

        /// <summary>
        /// 根据阈值拆分最小生成树实现层次聚类
        /// </summary>
        /// <param name="thresholdW">阈值</param>
        /// <returns>结果二叉树</returns>
        public ProxiGraph SplitByWeight(double thresholdW)
        {
            if (this.MST == null || this.MST.EdgeList == null || this.MST.NodeList == null || this.MST.NodeList.Count == 0 || this.MST.EdgeList.Count == 0) return null;
            ProxiGraph RootGraph = new ProxiGraph();
            RootGraph.ParentGraph = null;
            RootGraph = SplitByWeight(thresholdW, this.MST);
            return RootGraph;
        }
        /// <summary>
        /// 广度遍历二叉树，并输出叶子节点对应的分类
        /// </summary>
        /// <param name="?"></param>
        public static void Create_WriteClusters(ProxiGraph pg)
        {
            Queue<ProxiGraph> GraphQueue = new Queue<ProxiGraph>();
            GraphQueue.Enqueue(pg);
            ProxiGraph curPG = pg;
            ProxiGraph c1=null;
            ProxiGraph c2=null;
            List<ProxiGraph> listPG = new List<ProxiGraph>();
            
            while (GraphQueue.Count>0)
            {
                curPG = GraphQueue.Dequeue();
                c1 = curPG.SubGraphs[0];
                c2 = curPG.SubGraphs[1];
                if (c1.SubGraphs == null)
                {
                    listPG.Add(c1);
                }
                else
                {
                    GraphQueue.Enqueue(c1);
                }
                if (c2.SubGraphs == null)
                {
                    listPG.Add(c2);
                }
                else
                {
                    GraphQueue.Enqueue(c2);
                }
            }
            int i=0;
            foreach (ProxiGraph cPG in listPG)
            {
                i++;
                cPG.WriteProxiGraph2Shp(@"E:\DelaunayShape",@"Cluster"+i.ToString(), esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
            }
        }
    }
}
