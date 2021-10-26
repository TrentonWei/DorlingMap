using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuxStructureLib
{
    /// <summary>
    /// 最短路径算法
    /// </summary>
    public class AlgDijkstra
    {
        /// <summary>
        /// 邻近图矩阵
        /// </summary>
        public double[,] AdjMatrix = null;   //邻接矩阵

        public AlgDijkstra()
        {

        }
        /// <summary>
        /// 从邻近图创建邻近矩阵
        /// </summary>
        /// <param name="pg">邻近图</param>
        public void CreateAdjMatrixfrmProximityGraph(ProxiGraph pg)
        {
            // pg.CalWeightbyNearestDistance();
            int n = pg.NodeList.Count;
            this.AdjMatrix = new double[n, n];
            //初始化矩阵值，对角线上为0，其他地方为无穷大
            for (int i = 0; i < n; i++)
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
                double w = edge.NearestEdge.NearestDistance;
                AdjMatrix[i, j] = w;
                AdjMatrix[j, i] = w;
            }
        }
        //从某一源点出发，找到到某一结点的最短路径
        public  int[] OneToOneSP(int vo, int vi,int n, double MaxSize, out double resmindist)
        {
            resmindist = double.PositiveInfinity;
            bool[] s = new bool[n];       //true时表示已找到最短路径 false时表示未找到           
            double[] dist = new double[n];      //保存从源点到终点vi的目前最短路径长度
            int[] prev = new int[n];      //保存从源点到终点vi当前最短路径中的前一个顶点的编号，它的初值为原点vo（vo到vi有边时），或者－1（vo到vi无边时）
            double mindis;                   //最小距离临时变量
            int u;                        //临时结点，记录当前正计算结点            

            //初始结点信息
            for (int i = 0; i < n; i++)
            {
                s[i] = false;            //s[]表置空
                dist[i] = this.AdjMatrix[vo, i];      //距离初始化 
                if (dist[i] >= MaxSize)   //路径初始化
                    prev[i] = -1;
                else
                    prev[i] = vo;
            }
            s[vo] = true;   //将源点编号放入s表中  
            prev[vo] = 0;   //因为一共有n个顶点，而且prev的个数是n，所以应把初始顶点的前一点设为0，从而凑够n    

            //主循环
            for (int i = 0; i < n; i++)
            {
                u = -1;                        //初始化中间顶点
                mindis = MaxSize;              //假设点与点之间的距离在未知的情况下为无穷                            
                for (int j = 0; j < n; j++)    //选取不在s中且具有最小距离的顶点u
                {
                    if (!s[j] && dist[j] < mindis)
                    {
                        u = j;
                        mindis = dist[j];
                    }
                }
                s[u] = true;   //将顶点u加入s中


                if (vi == u)   //当找到vi时跳出循环，没找到时继续找
                {
                    break;
                }
                else
                {
                    for (int j = 0; j < n; j++)
                        if (s[j] == false)
                            if (this.AdjMatrix[u, j] < MaxSize && mindis + this.AdjMatrix[u, j] < dist[j])  //以u为新考虑的中间点，修改不在s中各顶点的距离；若从源点vo到不在s中的顶点的距离（经过顶点u）比原来距离（不经过顶点u）短，则修改不在s中的顶点的距离值，修改后的距离值为顶点u的距离加上边<j,u>上的权。
                            {
                                dist[j] = mindis + this.AdjMatrix[u, j];
                                prev[j] = u;
                            }
                }
            }

            //输出路径结点
            int e = vi;
            int step = 0;
            int[] path = new int[n];
            path[0] = vi;     //将vi放进去       
            while (e != vo)   //从后面往前面找vi的最短路径
            {
                step++;
                path[step] = prev[e];
                e = prev[e];
            }

            for (int i = step; i > step / 2; i--)//将顺序颠倒,记录从源点到终点的编号
            {
                int temp = path[step - i];
                path[step - i] = path[i];
                path[i] = temp;
            }

            //将临时路径表复制到属性ShortPath
            int[] ShortPath = new int[step + 1];
            for (int i = 0; i <= step; i++)
            {
                ShortPath[i] = path[i];
            }
            //输出最短路径长度
            resmindist= dist[vi];
            return ShortPath;
        } 


    }
}
