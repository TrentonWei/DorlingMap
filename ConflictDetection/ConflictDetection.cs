using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;

namespace ConflictDetection
{
    /// <summary>
    /// 冲突，指Skeleton_Segment上一段连续的冲突三角形
    /// </summary>
    public class Conflict
    {
        //所属的骨架线段
        public Skeleton_Segment SkeSeg = null;
        //最狭窄的地方的间隔距离
        public float MinSeparate;
        //最狭窄的地方的对应的三角形
        public Triangle MinSeparateTri = null;
        //构成冲突范围德尔三角形
        public List<Triangle> TriangleList = null;

        public List<Force> LSeg = null;              //左边的路段及其受力
        public List<Force> RSeg = null;              //右边的路段及其受力

        public int ConflictType = 0;//冲突类型：0-冲突；1-潜在冲突
      
        //该段冲突骨架线上三角形的起止下标
        public int StartTriIndex = -1;
        public int EndTriIndex = -1;


        /// <summary>
        /// 冲突构造函数
        /// </summary>
        /// <param name="seg"></param>
        public Conflict(Skeleton_Segment seg)
        {
            SkeSeg = seg;
        }

    }

    /// <summary>
    /// 顶点的受力
    /// </summary>
    public class Force
    {
        public double F;
        public double Sin;
        public double Cos;
        public double Fx;
        public double Fy;
        public int ID;

        public Force(double f, double sin, double cos, double fx, double fy, int Id)
        {
            F = f;
            Sin = sin;
            Cos = cos;
            Fx = fx;
            Fy = fy;
            ID = Id;
        }
    }
    /// <summary>
    /// 检测冲突
    /// </summary>
    public class ConflictDetection
    {
        /// <summary>
        /// 骨架线对象
        /// </summary>
        public Skeleton Skel = null;
        /// <summary>
        /// 冲突列表
        /// </summary>
        public List<Conflict> ConflictList = null;

        public List<Force> ForceList = null;

        public List<PolylineObject> PLList = null;

        List<ConNode> CNList = null;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ske"></param>
        public ConflictDetection(Skeleton ske, List<PolylineObject> pLList, List<ConNode> cnList)
        {
            Skel = ske;
            PLList = pLList;
            ConflictList = new List<Conflict>();
            ForceList = new List<Force>();
            CNList = cnList;
        }

        /// <summary>
        /// 通过骨架线段探测冲突2013-8-15
        /// </summary>
        public void DetectConflict()
        {
            if (Skel.Skeleton_SegmentList == null || Skel.Skeleton_SegmentList.Count == 0)
                return;
            foreach (Skeleton_Segment seg in Skel.Skeleton_SegmentList)
            {

                if (seg.TriangleList == null || seg.TriangleList.Count == 0)
                    continue;

                #region 确定左右路段ID
                int triCount = seg.TriangleList.Count;
                Triangle tri = null;
                if (triCount < 2)//没有1类三角形的情况
                {
                    continue;
                }
                else if (triCount == 2)
                {
                    if (seg.TriangleList[0].TriType == 2 || seg.TriangleList[0].TriType == 1)
                    {
                        tri = seg.TriangleList[0];
                    }
                    else if (seg.TriangleList[1].TriType == 2 || seg.TriangleList[1].TriType == 1)
                    {
                        tri = seg.TriangleList[1];
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    tri = seg.TriangleList[1];
                }

                #endregion

                TriNode p = null;



                GetRoadSegIDs(tri , seg.Axis_Points[1],seg.Axis_Points[2], out seg.LeftRoadSegID,out seg.RightRoadSegID,out p);
             
                
                
                /*int curIndex = 2;
                 while ((seg.LeftRoadSegID == -1 || seg.RightRoadSegID == -1)&& curIndex<triCount)
               {
                   GetRoadSegIDs(seg.TriangleList[curIndex], seg.Axis_Points[curIndex], seg.Axis_Points[curIndex + 1], out seg.LeftRoadSegID, out seg.RightRoadSegID);
                   curIndex++;
               }

               if (seg.LeftRoadSegID == -1 || seg.RightRoadSegID == -1)
               {
                   int err = 0;
               }
               */
    
                //获取距离阈值
                double thresholdDis = this.GetThreshold(seg.LeftRoadSegID, seg.RightRoadSegID,p);//由道路段ID号确定距离阈值

                bool isCf = false;
                Conflict curConflict = null;
                for (int i = 1; i < triCount - 1; i++)
                {
                    Triangle curTri = seg.TriangleList[i];
                    //如果是冲突三角形
                    float minDis = 9999999;

                    if (isConflict(curTri, (float)thresholdDis, out minDis,ref this.ForceList))
                    {
                        //如果是起始冲突三角形
                        if (!isCf)
                        {
                            curConflict = new Conflict(seg);
                            curConflict.MinSeparate = 99999999;
                            curConflict.MinSeparateTri = null;
                            this.ConflictList.Add(curConflict);
                            curConflict.TriangleList = new List<Triangle>();
                            curConflict.TriangleList.Add(curTri);
                            isCf = true;
                        }
                  
                        else
                        {
                            curConflict.TriangleList.Add(curTri);

                        }
                        //更新最小距离及其对应三角形
                        if (curConflict.MinSeparate > minDis)
                        {
                            curConflict.MinSeparate = minDis;//赋予当前最小值
                            curConflict.MinSeparateTri = curTri;
                        }
                        //当前处理的是最后一个三角形时
                        if (i == triCount - 2 && isCf == true)
                        {
                            if (curConflict != null)
                            {
                                if (curConflict.MinSeparate < thresholdDis)
                                {
                                    curConflict.ConflictType = 0;
                                }
                                else
                                {
                                    curConflict.ConflictType = 1;
                                }
                            }
                        }
                    }
                    //如果不是冲突三角形
                    else
                    {
                        if (isCf == true)
                        {

                            isCf = false;
                            if (curConflict.MinSeparate < thresholdDis)
                            {
                                curConflict.ConflictType = 0;
                            }
                            else
                            {
                                curConflict.ConflictType = 1;
                            }
                            curConflict = null;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 获取左右ID
        /// </summary>
        private void GetRoadSegIDs(Triangle tri ,TriNode p1,TriNode p2, out int leftID,out int rightID,out TriNode p)
        {
            leftID = -1;
            rightID = -1;
            p = null;
            if (tri.TriType == 1)
            {
                //找出实边
                TriEdge realEdge = null;
                if (tri.edge1.tagID != -1)
                    realEdge = tri.edge1;
                else if (tri.edge2.tagID != -1)
                    realEdge = tri.edge2;
                else if (tri.edge3.tagID != -1)
                    realEdge = tri.edge3;

                //确定道路段的ID号
                int tagID1 = realEdge.tagID;
                int ID1 = realEdge.startPoint.ID;
                int ID2 = realEdge.endPoint.ID;
                int tagID2 = -1;

                if (tri.point1.ID != ID1&&tri.point1.ID != ID2)
                {
                    tagID2 = tri.point1.TagValue;
                    p = tri.point1;
                }
                else if (tri.point2.ID != ID1 && tri.point2.ID != ID2)
                {
                    tagID2 = tri.point2.TagValue;
                    p = tri.point2;
                }
                else if (tri.point3.ID != ID1 && tri.point3.ID != ID2)
                {
                    tagID2 = tri.point3.TagValue;
                    p = tri.point3;
                }

                if (p == null)
                {
                    leftID = tagID1;
                    rightID = tagID1;
                }
                else
                {
                    string res = ComFunLib.funReturnRightOrLeft(p1, p2, p);

                    if (res == "LEFT")
                    {
                        leftID = tagID2;
                        rightID = tagID1;
                    }
                    else if (res == "RIGHT")
                    {
                        leftID = tagID1;
                        rightID = tagID2;
                    }
                }
            }
            else if (tri.TriType == 2)
            {

                TriEdge redge1 = null;
                TriEdge redge2 = null;

                if (tri.edge1.tagID == -1)
                {
                    redge1 = tri.edge2;
                    redge2 = tri.edge3;

                }
                else if (tri.edge2.tagID == -1)
                {
                    redge1 = tri.edge3;
                    redge2 = tri.edge1;
                }
                else if (tri.edge3.tagID == -1)
                {
                    redge1 = tri.edge1;
                    redge2 = tri.edge2;
                }

                leftID = redge1.tagID;
                rightID = redge2.tagID;
            }
        }

        /// <summary>
        /// 判断是否冲突
        /// </summary>
        /// <param name="tri">三角形</param>
        /// <param name="thresholdDis">距离阈值</param>
        /// <returns>是否冲突</returns>
        private bool isConflict(Triangle tri, float thresholdDis, out float minDis,ref List<Force> forceList)
        {
            minDis = 99999999;
            TriNode v1 = null;
            TriNode v2 = null;
            TriNode v3 = null;

            if (tri.TriType == 1)
            {
                #region 区分实边上的点和虚边上的点，为求高做准备
                TriEdge realEdge = null;
                if (tri.edge1.tagID !=-1)
                    realEdge = tri.edge1;
                else if (tri.edge2.tagID != -1)
                    realEdge = tri.edge2;
                else if (tri.edge3.tagID != -1)
                    realEdge = tri.edge3;

                v2 = realEdge.startPoint;
                v3 = realEdge.endPoint;
                if (tri.point1.ID != v2.ID && tri.point1.ID != v3.ID)
                {
                    v1 = tri.point1;
                }
                else if (tri.point2.ID != v2.ID && tri.point2.ID != v3.ID)
                {
                    v1 = tri.point2;
                }
                else if (tri.point3.ID != v2.ID && tri.point3.ID != v3.ID)
                {
                    v1 = tri.point3;
                }
                #endregion
                TriNode cz = null;
                minDis = MinDis(v1, v2, v3,out cz);

                if (minDis < 2*thresholdDis)
                {
                    if (minDis < thresholdDis)
                    {
                        //受力大小
                        double absForce = (thresholdDis - minDis) * 1000 / 500000;
                        //当Dmin>dis，才受力
                        if (absForce > 0)
                        {
                            //受力向量方位角的COS
                            double sin = (v1.Y - cz.Y) / minDis;
                            //受力向量方位角的SIN
                            double cos = (v1.X - cz.X) / minDis;
                            double curFx = absForce * cos;
                            double curFy = absForce * sin;
                            forceList.Add(new Force(absForce, sin, cos, curFx, curFy, v1.ID));
                        }
                    }
                    return true;
                }
            }
            return false; ;
        }

        /// <summary>
        /// 获取间隔阈值
        /// </summary>
        /// <param name="ID1">路段1的ID</param>
        /// <param name="ID2">路段2的ID</param>
        /// <returns>距离阈值</returns>
        private double GetThreshold(int ID1, int ID2,TriNode p)
        {
 
            PolylineObject l1 = PolylineObject.GetPLbyID(this.PLList, ID1);
            PolylineObject l2 = PolylineObject.GetPLbyID(this.PLList, ID2);
            double w1=-1;
            double w2=-1;
            if (l1 != null)
                w1 = l1.SylWidth;
            else
                if (p != null)
                {
                    w1 = ConNode.GetPLbyID(this.CNList, p.ID).SylWidth;
                }
                else
                {
                    w1 = 2;
                }
            if (l2 != null)
                w2 = l2.SylWidth;
            else
                if (p != null)
                {
                    w2 = ConNode.GetPLbyID(this.CNList, p.ID).SylWidth;
                }
                else
                {
                    w2 = 2;
                }

            double w = ((w1 + w2) / 2.0 + 0.2) * 500000 / 1000;
            return w;
        }

        /// <summary>
        /// 求最小距离
        /// </summary>
        /// <param name="v1">顶点</param>
        /// <param name="v2">底边顶点1</param>
        /// <param name="v3">底边顶点2</param>
        /// <returns></returns>
        private double MinDis(TriNode v1, TriNode v2, TriNode v3)
        {
            double A = (v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y);
            double B = (v2.X - v3.X) * (v2.X - v3.X) + (v2.Y - v3.Y) * (v2.Y - v3.Y);
            double C = (v3.X - v1.X) * (v3.X - v1.X) + (v3.Y - v1.Y) * (v3.Y - v1.Y);
            double a = Math.Sqrt(A);
            double b = Math.Sqrt(B);
            double c = Math.Sqrt(C);

            double cosB = (A + B - C) / (2 * a * b);
            double cosC = (C + B - A) / (2 * b * c);

            if (cosB > 0 && cosC > 0)
            {
                return ComFunLib.ComHeight(v1, v2, v3);
            }
            else
            {
                if (cosB <= 0)
                {
                    return (float)a;
                }
                else if (cosC <= 0)
                {
                    return (float)c;
                }
            }
            return 9999999;
        }


        /// <summary>
        /// 求最小距离
        /// </summary>
        /// <param name="v1">顶点</param>
        /// <param name="v2">底边顶点1</param>
        /// <param name="v3">底边顶点2</param>
        /// <returns></returns>
        private float MinDis(TriNode v1, TriNode v2, TriNode v3,out TriNode cz)
        {

            cz = new TriNode();
            double A = (v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y);
            double B = (v2.X - v3.X) * (v2.X - v3.X) + (v2.Y - v3.Y) * (v2.Y - v3.Y);
            double C = (v3.X - v1.X) * (v3.X - v1.X) + (v3.Y - v1.Y) * (v3.Y - v1.Y);
            double a = Math.Sqrt(A);
            double b = Math.Sqrt(B);
            double c = Math.Sqrt(C);

            double cosB = (A + B - C) / (2 * a * b);
            double cosC = (C + B - A) / (2 * b * c);

            if (cosB > 0 && cosC > 0)
            {
                double d = Math.Sqrt((v2.X - v3.X) * (v2.X - v3.X) + (v2.Y - v3.Y) * (v2.Y - v3.Y));
                double e = (v3.X - v2.X) * (v1.X - v2.X) + (v3.Y - v2.Y) * (v1.Y - v2.Y);
                e = e / (d * d);
                double x4 = v2.X + (v3.X - v2.X) * e;
                double y4 = v2.Y + (v3.Y - v2.Y) * e;
                cz.X = (float) x4;
                cz.Y = (float) y4;
                return (float)Math.Sqrt((x4 - v1.X) * (x4 - v1.X) + (y4 - v1.Y) * (y4 - v1.Y));
            }
            else
            {
                if (cosB <= 0)
                {
                    cz.X = v2.X;
                    cz.Y = v2.Y;
                    return (float)a;
                }
                else if (cosC <= 0)
                {
                    cz.X = v3.X;
                    cz.Y = v3.Y;
                    return (float)c;
                }
            }
            return 9999999;
        }

    }
}
