using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuxStructureLib
{
    public class Skeleton_Arc : PolylineObject
    {
        public MapObject LeftMapObj = null;
        public MapObject RightMapObj = null;
        public MapObject FrontMapObj = null;       //可能并不需要
        public MapObject BackMapObj = null;        //可能并不需要
        public List<Triangle> TriangleList = null; //三角形路径
        public NearestEdge NearestEdge = null;
        public double AveDistance = 0;
        public double WAD = 0;                      // weighted average distance
        public double Length = 0;
        public double GapArea = 0;
        //Yan的8方向DVD模型
        //{N<337.5-0,0-22.5>,NE<22.5-67.5>,E<67.5-112.5>,SE<112.5-157.5>,
        //S<157.5,202.5>,SW<202.5,247.5>,W<247.5,292.5>,NW<292.5-337.5>}
        public double[] DVD = null;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">编号</param>
        public Skeleton_Arc(int id)
        {
            ID = id;
            TriangleList = new List<Triangle>();
            PointList = new List<TriNode>();
            NearestEdge = new NearestEdge(id, null, null, 999999999);
            DVD = new double[8];//Yan的8方向DVD模型
        }

        /// <summary>
        /// 计算Yan的DVD方向
        /// </summary>
        public void CalDVD()
        {
            double sum = 0;
           /* int n = this.TriangleList.Count;
            for (int i = 0; i < n - 1; i++)
            {
                Triangle curTri= this.TriangleList[i];
                //如果第一个或最后一个三角形
              if (i == 0 || i == n - 1)
                {
                    if (curTri.TriType == 0)
                    {
                        continue;
                    }
                }

                //计算各段所属的角度范围并计入相应的数组元素


            }*/
            TriNode p1, p2;
            double A = 0;
            double dy = 0;
            double dx = 0;
            double curL=0;
            for(int i=0;i<this.PointList.Count-1;i++)
            {
                p1 = PointList[i];
                p2 = PointList[i+1];
                dx = (p1.X - p2.X);
                dy = (p1.Y - p2.Y);
                if (dx == 0)
                    A = Math.PI / 2;
                A = dy / dx;
                A = 180.0 * Math.Atan(A) / Math.PI;
                curL=ComFunLib.CalLineLength(p1,p2);
                sum += curL;
                if (A > -22.5 && A <= 22.5)
                {
                    DVD[0] += curL;//N
                }
                else if (A > 22.5 && A <= 67.5)
                {
                    DVD[1] += curL;//NE
                }
                else if (A > 67.5 && A <=90)
                {
                    DVD[2] += curL;//E
                }
                else if (A > -67.5 && A <= -22.5)
                {
                    DVD[3] += curL;//NW
                }
                else if (A > -90 && A <= -67.5)
                {
                    DVD[4] += curL;//W
                }
            }
            //计算比例
            for (int i = 0; i < 8; i++)
            {
                DVD[i] = DVD[i] / sum;
            }
        }

        /// <summary>
        /// 计算间隔距离
        /// </summary>
        public void CalGapArea()
        {
            if (this.TriangleList == null || this.TriangleList.Count == 0)
                return;

            Triangle curTri = null;
            double curArea = 0;
            if (this.TriangleList.Count > 1)
            {
                int n = this.TriangleList.Count;
                curTri = this.TriangleList[0];
                if (curTri.TriType == 0)
                {
                    curArea += ComFunLib.ComTriArea(curTri) / 3.0;
                }
                else
                {
                    curArea += ComFunLib.ComTriArea(curTri);
                }

                for (int i = 1; i < n - 1; i++)
                {
                    curTri = this.TriangleList[i];
                    curArea += ComFunLib.ComTriArea(curTri);
                }
                curTri = this.TriangleList[n - 1];
                if (curTri.TriType == 0)
                {
                    curArea += ComFunLib.ComTriArea(curTri) / 3.0;
                }
                else
                {
                    curArea += ComFunLib.ComTriArea(curTri);
                }
            }
            else
            {
                curTri = this.TriangleList[0];
                curArea = ComFunLib.ComTriArea(curTri);
                curArea = ComFunLib.ComTriArea(curTri);
            }
            this.GapArea = curArea;
        }

    }
}
