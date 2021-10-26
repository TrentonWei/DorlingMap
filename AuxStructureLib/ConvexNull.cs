using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace AuxStructureLib
{
    /// <summary>
    /// 凸包
    /// </summary>
    public class ConvexNull
    {
        public List<TriNode> PointSet = null;        //节点列表
        public List<TriNode> ConvexVertexSet = null; //凸包上的顶点
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="_triNodeList">节点列表</param>
        public ConvexNull(List<TriNode> _pointSet)
        {
            PointSet = _pointSet;
            ConvexVertexSet = new List<TriNode>();
        }

        public List<TriEdge> GetConvexNull_Edges()
        {
            List<TriEdge> edges = new List<TriEdge>();

            if (this.ConvexVertexSet == null || ConvexVertexSet.Count < 3)
            {
                return null;
            }
            else
            {
                for (int i = 0; i < ConvexVertexSet.Count; i++)
                {
                    TriEdge newEdge = new TriEdge(ConvexVertexSet[i], PointSet[(i + 1) % ConvexVertexSet.Count]);
                    edges.Add(newEdge);
                }
                return edges;
            }
        }

        /// <summary>
        /// 生成凸包_Graham's Scan
        /// </summary>
        public bool CreateConvexNull()
        {
            List<TriNode> tempPointSet = new List<TriNode>();
            int n = PointSet.Count;
            if (n <= 3)
            {
                return false;
            }

            int i = 0;
            int a = 0, b = 0, c = 0, d = 0;   //用于存储四个最值点的索引号

            //首先寻找x和y的最值
            for (i = 1; i < n; i++)
            {
                if (PointSet[i].X <= PointSet[a].X)//左
                {
                    a = i;
                }
                if (PointSet[i].Y <= PointSet[b].Y)//下
                {
                    b = i;
                }
                if (PointSet[i].X >= PointSet[c].X)//右
                {
                    c = i;
                }
                if (PointSet[i].Y >= PointSet[d].Y)//上
                {
                    d = i;
                }
            }


            //创建一个多边形
            List<TriNode> boundPolygon = new List<TriNode>();
            boundPolygon.Add(PointSet[a]);
            boundPolygon.Add(PointSet[b]);
            boundPolygon.Add(PointSet[c]);
            boundPolygon.Add(PointSet[d]);
            boundPolygon.Add(PointSet[a]);

            tempPointSet.Add(PointSet[a]);
            tempPointSet.Add(PointSet[b]);
            tempPointSet.Add(PointSet[c]);
            tempPointSet.Add(PointSet[d]);

            //先排除掉这个四边形内的点
            for (i = 0; i < n; i++)
            {

                if (PointSet[i].InsidePolygon(boundPolygon) || ((i == a || i == b || i == c || i == d)))
                {
                    continue;
                }
                else
                {
                    tempPointSet.Add(PointSet[i]);
                }
            }

            TriNode.GetAssortedPoints(tempPointSet);

            //前3个点入栈
            Stack<TriNode> s = new Stack<TriNode>();
            s.Push(tempPointSet[0]);
            s.Push(tempPointSet[1]);
            s.Push(tempPointSet[2]);

            //从第4点开始去除凹点
            for (i = 3; i < tempPointSet.Count; i++)
            {
                //当前点，栈中栈顶点和顶点的下面一个点3个点的转折方向是顺时针方向，就要退栈
                /*  while (Miltiply(s.ElementAt(1),s.ElementAt(0),tempPointSet[i]) <=0)
                  {
                      s.Pop();		 //出栈
                  }*/
                //不满足向左转的关系,栈顶元素出栈   
                while (TriNode.multiply(tempPointSet[i], s.ElementAt(0), s.ElementAt(1)) >= 0)
                    s.Pop();		 //出栈 

                s.Push(tempPointSet[i]);  //入栈
            }
            s.Push(s.ElementAt(s.Count - 1));//将启点加入，形成闭合的多边形

            int size = s.Count;


            for (int j = size - 1; j >= 0; j--)
            {
                this.ConvexVertexSet.Add(s.ElementAt(j));
            }
            return true;
        }

        /// <summary>
        /// 求三点矢量叉积
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <returns>返回矢量叉积</returns>
        private double Miltiply(TriNode p1, TriNode p2, TriNode p3)
        {
            return (p2.X - p1.X) * (p3.Y - p2.Y) - (p3.X - p2.X) * (p2.Y - p2.Y);
        }

        /// <summary>
        /// 是否包含该点
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
         public bool ContainPoint(TriNode p,out int index)
         {
             index=0;
             foreach (TriNode curPoint in this.ConvexVertexSet)
             {
                 if (curPoint.ID == p.ID)
                 {
                     return true;
                 }
                 index++;
             }
             return false;
         }
    }
}
