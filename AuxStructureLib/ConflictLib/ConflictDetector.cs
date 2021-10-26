using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using AuxStructureLib.IO;

namespace AuxStructureLib.ConflictLib
{
    /// <summary>
    /// 2014-2-26冲突检测类
    /// </summary>
    public class ConflictDetector
    {
        public List<ConflictBase> ConflictList = null;//冲突列表
        public List<Skeleton_Arc> Skel_arcList = null;
        public ProxiGraph PG = null;

        public double threshold = 0.2;//mm
        public double targetScale;//目标比例尺分母

        public int CountS = -1;
        public double SumS = -1;
        public double MaxS = -1;
        public double MinS = -1;
        public double MeanS = -1;
        /// <summary>
        /// 统计冲突的严重程度-仅针对Conflict_R
        /// </summary>
        public void Statistic()
        {
            if (this.ConflictList != null && this.ConflictList.Count != 0)
            {
                this.CountS = this.ConflictList.Count;
                MaxS = double.NegativeInfinity;
                MinS = double.PositiveInfinity;
                SumS = 0;
                foreach (Conflict_R c in this.ConflictList)
                {
                    double s = (c.DisThreshold - c.Distance);
                    SumS += s;
                    if (MaxS < s)
                        MaxS = s;
                    if (MinS > s)
                        MinS = s;
                }

                MeanS = SumS / CountS;
            }
        }

        /// <summary>
        /// 潜在冲突检测
        /// </summary>
        public List<ConflictBase> DetectPotentialConflict(int k)
        {
            List<ConflictBase> PotentialConflictList = new List<ConflictBase>();
            foreach (Skeleton_Arc curArc in this.Skel_arcList)
            {
                if (curArc == null || curArc.TriangleList == null | curArc.TriangleList.Count < 2)
                    continue;
                double wl = curArc.LeftMapObj.SylWidth;
                double wr = curArc.RightMapObj.SylWidth;
                double d = (0.5 * (wl + wr) + threshold) * targetScale / 1000;
                //Linear conflict
                if (curArc.LeftMapObj.FeatureType == FeatureType.PolylineType
                    && curArc.RightMapObj.FeatureType == FeatureType.PolylineType)
                {
                    //调用linearconflict检测函数
                    PotentialConflictList.AddRange(this.DetectPotentialLinearConflict(curArc, d, k * d));
                }

                //Rigid conflict
                else
                {
                    if (curArc.NearestEdge.NearestDistance > d && curArc.NearestEdge.NearestDistance<k*d)
                    {
                        TriNode leftPoint = new TriNode(curArc.NearestEdge.Point1.X, curArc.NearestEdge.Point1.Y, curArc.NearestEdge.Point1.ID);
                        TriNode rightPoint = new TriNode(curArc.NearestEdge.Point2.X, curArc.NearestEdge.Point2.Y, curArc.NearestEdge.Point2.ID);
                        Conflict_R conflict = new Conflict_R(curArc, leftPoint, rightPoint, null, curArc.NearestEdge.NearestDistance, d);
                        PotentialConflictList.Add(conflict);
                    }
                }
            }
            if (ConflictList.Count == 0)
            {
                return null;//没有冲突
            }
            else
                return PotentialConflictList; ;
        }

        /// <summary>
        /// 冲突检测2015-2-11,全部标识为三角形区域
        /// </summary>
        public bool DetectConflictasTriRegions()
        {
            ConflictList = new List<ConflictBase>();
            foreach (Skeleton_Arc curArc in this.Skel_arcList)
            {
                if (curArc == null || curArc.TriangleList == null | curArc.TriangleList.Count < 2)
                    continue;
                if (curArc.LeftMapObj == null || curArc.RightMapObj == null)
                    continue;
                double wl = curArc.LeftMapObj.SylWidth;
                double wr = curArc.RightMapObj.SylWidth;
                double d = (0.5 * (wl + wr) + threshold) * targetScale / 1000;

                    //调用linearconflict检测函数
                    this.ConflictList.AddRange(this.DetectAllConflict(curArc, d));
            }
            if (ConflictList.Count == 0)
            {
                return false;//没有冲突
            }
            else
                return true;
        }

        /// <summary>
        /// 冲突检测
        /// </summary>
        public bool DetectConflict()
        {
            ConflictList = new List<ConflictBase>();
            foreach (Skeleton_Arc curArc in this.Skel_arcList)
            {
                if (curArc == null || curArc.TriangleList == null | curArc.TriangleList.Count < 2)
                    continue;
                double wl = curArc.LeftMapObj.SylWidth;
                double wr = curArc.RightMapObj.SylWidth;
                double d = (0.5 * (wl + wr) + threshold) * targetScale / 1000;
                //Linear conflict
                if (curArc.LeftMapObj.FeatureType == FeatureType.PolylineType
                    && curArc.RightMapObj.FeatureType == FeatureType.PolylineType)
                {
                    //调用linearconflict检测函数
                    this.ConflictList.AddRange(this.DetectLinearConflict(curArc, d));
                }

                //Rigid conflict
                else
                {
                    if (curArc.NearestEdge.NearestDistance < d)
                    {
                        TriNode leftPoint = new TriNode(curArc.NearestEdge.Point1.X, curArc.NearestEdge.Point1.Y, curArc.NearestEdge.Point1.ID);
                        TriNode rightPoint = new TriNode(curArc.NearestEdge.Point2.X, curArc.NearestEdge.Point2.Y, curArc.NearestEdge.Point2.ID);
                        Conflict_R conflict = new Conflict_R(curArc, leftPoint, rightPoint, null, curArc.NearestEdge.NearestDistance, d);
                        ConflictList.Add(conflict);
                    }
                }
            }
            if (ConflictList.Count == 0)
            {
                return false;//没有冲突
            }
            else
                return true;
        }

        /// <summary>
        /// 冲突检测
        /// </summary>
        public bool DetectConflictByPG()
        {
            ConflictList = new List<ConflictBase>();
            foreach (ProxiEdge edge in PG.EdgeList)
            {
                if (edge == null)
                    continue;
                double wl = edge.Ske_Arc.LeftMapObj.SylWidth;
                double wr = edge.Ske_Arc.RightMapObj.SylWidth;
                double d = (0.5 * (wl + wr) + threshold) * targetScale / 1000;
                //Linear conflict
                if (!(edge.Ske_Arc.LeftMapObj.FeatureType == FeatureType.PolylineType
                    && edge.Ske_Arc.RightMapObj.FeatureType == FeatureType.PolylineType))
                {
                    if (edge.Ske_Arc.LeftMapObj.TypeID == 1 && edge.Ske_Arc.RightMapObj.TypeID == 1)//4-10排除Buffer边界线，TypeID==2
                    {

                        if (edge.NearestEdge.NearestDistance < d)
                        {
                            TriNode leftPoint = new TriNode(edge.NearestEdge.Point1.X, edge.NearestEdge.Point1.Y, edge.Node1.ID);
                            TriNode rightPoint = new TriNode(edge.NearestEdge.Point2.X, edge.NearestEdge.Point2.Y, edge.Node2.ID);
                            Conflict_R conflict = new Conflict_R(edge.Ske_Arc, leftPoint, rightPoint, null, edge.NearestEdge.NearestDistance, d);
                            ConflictList.Add(conflict);
                        }
                    }
                }
            }
            if (ConflictList.Count == 0)
            {
                return false;//没有冲突
            }
            else
                return true;
        }
        /// <summary>
        /// 检测潜在的Linear Conflict
        /// </summary>
        /// <param name="skelarc">骨架线弧段</param>
        /// <param name="d">距离阈值</param>
        /// <returns>冲突列表</returns>
        public List<Conflict_L> DetectPotentialLinearConflict(Skeleton_Arc skelarc, double d1,double d2)
        {

        List<Conflict_L> conflictList = new List<Conflict_L>();
            int triCount = skelarc.TriangleList.Count;
            if (triCount < 2)//没有1类三角形的情况
            {
                return null;
            }
            bool isCf = false;
            Conflict_L curConflict = null;
            Triangle curTri = null;
            //List<Triangle> curTriangleList = null;

            for (int i = 0; i < triCount; i++)
            {
                curTri = skelarc.TriangleList[i];
                if (curTri.TriType == 1)//只考虑1类三角形
                {
                    if (curTri.W < d2 && curTri.W>d1)
                    {

                        //如果是起始冲突三角形
                        if (!isCf)
                        {
                            curConflict = new Conflict_L(skelarc, d1);
                            //curConflict. = new List<Triangle>();
                            curConflict.TriangleList.Add(curTri);
                            isCf = true;
                        }
                        else
                        {
                            curConflict.TriangleList.Add(curTri);

                        }
                    }
                    //如果不是冲突三角形
                    else
                    {
                        if (isCf == true)
                        {
                            conflictList.Add(curConflict);

                            //三角形序列中包含的顶点
                            List<TriNode> pointList = new List<TriNode>();
                            foreach (Triangle t in curConflict.TriangleList)
                            {
                                if (!pointList.Contains(t.point1)) pointList.Add(t.point1);
                                if (!pointList.Contains(t.point2)) pointList.Add(t.point2);
                                if (!pointList.Contains(t.point3)) pointList.Add(t.point3);
                            }
                            List<TriNode> removeRange = null;
                            //左边冲突点
                            List<TriNode> leftObjectPointList = (skelarc.LeftMapObj as PolylineObject).PointList;//左边线目标的顶点列表
                            curConflict.LeftPointList = new List<TriNode>();//左边线目标的顶点列表
                            curConflict.LeftPointList.AddRange(leftObjectPointList);
                            removeRange = new List<TriNode>();
                            foreach (TriNode p in curConflict.LeftPointList)
                            {
                                if (!pointList.Contains(p))
                                    removeRange.Add(p);

                            }
                            foreach (TriNode p in removeRange)
                            {
                                curConflict.LeftPointList.Remove(p);
                            }
                            //右边冲突点
                            List<TriNode> rightObjectPointList = (skelarc.RightMapObj as PolylineObject).PointList;//右边线目标的顶点列表
                            curConflict.RightPointList = new List<TriNode>();//左边线目标的顶点列表
                            curConflict.RightPointList.AddRange(rightObjectPointList);
                            removeRange = new List<TriNode>();
                            foreach (TriNode p in curConflict.RightPointList)
                            {
                                if (!pointList.Contains(p))
                                    removeRange.Add(p);

                            }
                            foreach (TriNode p in removeRange)
                            {
                                curConflict.RightPointList.Remove(p);
                            }

                            isCf = false;
                            curConflict = null;

                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }

            if (isCf == true)
            {

                conflictList.Add(curConflict);

                //三角形序列中包含的顶点
                List<TriNode> pointList = new List<TriNode>();
                foreach (Triangle t in curConflict.TriangleList)
                {
                    if (!pointList.Contains(t.point1)) pointList.Add(t.point1);
                    if (!pointList.Contains(t.point2)) pointList.Add(t.point2);
                    if (!pointList.Contains(t.point3)) pointList.Add(t.point3);
                }
                List<TriNode> removeRange = null;
                //左边冲突点
                List<TriNode> leftObjectPointList = (skelarc.LeftMapObj as PolylineObject).PointList;//左边线目标的顶点列表
                curConflict.LeftPointList = new List<TriNode>();//左边线目标的顶点列表
                curConflict.LeftPointList.AddRange(leftObjectPointList);
                removeRange = new List<TriNode>();
                foreach (TriNode p in curConflict.LeftPointList)
                {
                    if (!pointList.Contains(p))
                        removeRange.Add(p);

                }
                foreach (TriNode p in removeRange)
                {
                    curConflict.LeftPointList.Remove(p);
                }
                //右边冲突点
                List<TriNode> rightObjectPointList = (skelarc.RightMapObj as PolylineObject).PointList;//右边线目标的顶点列表
                curConflict.RightPointList = new List<TriNode>();//左边线目标的顶点列表
                curConflict.RightPointList.AddRange(rightObjectPointList);
                removeRange = new List<TriNode>();
                foreach (TriNode p in curConflict.RightPointList)
                {
                    if (!pointList.Contains(p))
                        removeRange.Add(p);

                }
                foreach (TriNode p in removeRange)
                {
                    curConflict.RightPointList.Remove(p);
                }

                isCf = false;
                curConflict = null;
            }

            //#region 删除交叉点附近的伪冲突点
            ////
            // TriNode node;
            // TriEdge vEdge,redge1,redge2;
            // if (skelarc.TriangleList[0].TriType == 2)
            // {
            //     node = skelarc.TriangleList[0].GetStartPointofT2(out  vEdge, out  redge1, out  redge2);
            //     PolylineObject leftline=skelarc.LeftMapObj as PolylineObject;
            //     List<TriNode> excluLPList = new List<TriNode>();
            //     if (leftline.PointList[0] == node)
            //     {
            //         int i = 1;
            //         double culLen =0;
            //         while (i < leftline.PointList.Count && culLen < d)
            //         {
            //             if (!excluLPList.Contains(leftline.PointList[i]))
            //             {
            //                 excluLPList.Add(leftline.PointList[i]);
            //             }
            //             culLen = +AuxStructureLib.ComFunLib.CalLineLength(leftline.PointList[i - 1], leftline.PointList[i]);
            //             i++;
            //         }
            //     }
            //     else if (leftline.PointList[leftline.PointList.Count-1] == node)
            //     {
            //         int i = leftline.PointList.Count - 2;
            //         double culLen = 0;
            //         while (i > 0 && culLen < d)
            //         {
            //             if (!excluLPList.Contains(leftline.PointList[i]))
            //             {
            //                 excluLPList.Add(leftline.PointList[i]);
            //             }
            //             culLen = +AuxStructureLib.ComFunLib.CalLineLength(leftline.PointList[i+1], leftline.PointList[i]);
            //             i--;
            //         }
            //     }
            // }


            //#endregion
            #region 删除交叉点处的伪冲突点
            int n = skelarc.TriangleList.Count;
            if (skelarc.TriangleList[0].TriType == 2 || skelarc.TriangleList[n - 1].TriType == 2)
            {
                List<TriNode> excludeLPList = new List<TriNode>();
                List<TriNode> excludeRPList = new List<TriNode>();
                PolylineObject leftLineObject = skelarc.LeftMapObj as PolylineObject;
                PolylineObject rightLineObject = skelarc.RightMapObj as PolylineObject;

                if (skelarc.TriangleList[0].TriType == 2)
                {
                    this.ExcludePointatCrossover(ref excludeLPList, skelarc.TriangleList[0], leftLineObject, d2);
                    this.ExcludePointatCrossover(ref excludeRPList, skelarc.TriangleList[0], rightLineObject, d2);
                }
                if (skelarc.TriangleList[n - 1].TriType == 2)
                {
                    this.ExcludePointatCrossover(ref excludeLPList, skelarc.TriangleList[n - 1], leftLineObject, d2);
                    this.ExcludePointatCrossover(ref excludeRPList, skelarc.TriangleList[n - 1], rightLineObject, d2);
                }

                foreach (TriNode p in excludeLPList)
                {
                    foreach (Conflict_L c in conflictList)
                    {
                        if (c.LeftPointList.Contains(p))
                            c.LeftPointList.Remove(p);
                    }
                }

                foreach (TriNode p in excludeRPList)
                {
                    foreach (Conflict_L c in conflictList)
                    {
                        if (c.RightPointList.Contains(p))
                            c.RightPointList.Remove(p);
                    }
                }
            }

            #endregion

            #region 删除左右冲突的点个数均为0的冲突
            List<Conflict_L> delCList = new List<Conflict_L>();
            foreach (Conflict_L c in conflictList)
            {
                if (c.RightPointList.Count == 0 && c.LeftPointList.Count == 0)
                {
                    delCList.Add(c);
                }
            }
            foreach (Conflict_L c in delCList)
            {
                conflictList.Remove(c);
            }


            #endregion
            return conflictList;

        }

        /// <summary>
        /// 检测Linear Conflict
        /// </summary>
        /// <param name="skelarc">骨架线弧段</param>
        /// <param name="d">距离阈值</param>
        /// <returns>冲突列表</returns>
        private List<Conflict_L> DetectLinearConflict(Skeleton_Arc skelarc, double d)
        {
            if (skelarc.ID == 108)
            {
                int error = 0;
            }

            List<Conflict_L> conflictList = new List<Conflict_L>();
            int triCount = skelarc.TriangleList.Count;
            if (triCount < 2)//没有1类三角形的情况
            {
                return null;
            }
            bool isCf = false;
            Conflict_L curConflict = null;
            Triangle curTri = null;
            //List<Triangle> curTriangleList = null;

            for (int i = 0; i < triCount; i++)
            {
                curTri = skelarc.TriangleList[i];
                if (curTri.TriType == 1)//只考虑1类三角形
                {
                    if (curTri.W < d)
                    {

                        //如果是起始冲突三角形
                        if (!isCf)
                        {
                            curConflict = new Conflict_L(skelarc, d);
                            //curConflict. = new List<Triangle>();
                            curConflict.TriangleList.Add(curTri);
                            isCf = true;
                        }
                        else
                        {
                            curConflict.TriangleList.Add(curTri);

                        }
                    }
                    //如果不是冲突三角形
                    else
                    {
                        if (isCf == true)
                        {
                            conflictList.Add(curConflict);

                            //三角形序列中包含的顶点
                            List<TriNode> pointList = new List<TriNode>();
                            foreach (Triangle t in curConflict.TriangleList)
                            {
                                if (!pointList.Contains(t.point1)) pointList.Add(t.point1);
                                if (!pointList.Contains(t.point2)) pointList.Add(t.point2);
                                if (!pointList.Contains(t.point3)) pointList.Add(t.point3);
                            }
                            List<TriNode> removeRange = null;
                            //左边冲突点
                            List<TriNode> leftObjectPointList = (skelarc.LeftMapObj as PolylineObject).PointList;//左边线目标的顶点列表
                            curConflict.LeftPointList = new List<TriNode>();//左边线目标的顶点列表
                            curConflict.LeftPointList.AddRange(leftObjectPointList);
                            removeRange = new List<TriNode>();
                            foreach (TriNode p in curConflict.LeftPointList)
                            {
                                if (!pointList.Contains(p))
                                    removeRange.Add(p);

                            }
                            foreach (TriNode p in removeRange)
                            {
                                curConflict.LeftPointList.Remove(p);
                            }
                            //右边冲突点
                            List<TriNode> rightObjectPointList = (skelarc.RightMapObj as PolylineObject).PointList;//右边线目标的顶点列表
                            curConflict.RightPointList = new List<TriNode>();//左边线目标的顶点列表
                            curConflict.RightPointList.AddRange(rightObjectPointList);
                            removeRange = new List<TriNode>();
                            foreach (TriNode p in curConflict.RightPointList)
                            {
                                if (!pointList.Contains(p))
                                    removeRange.Add(p);

                            }
                            foreach (TriNode p in removeRange)
                            {
                                curConflict.RightPointList.Remove(p);
                            }

                            isCf = false;
                            curConflict = null;

                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }

            if (isCf == true)
            {

                conflictList.Add(curConflict);

                //三角形序列中包含的顶点
                List<TriNode> pointList = new List<TriNode>();
                foreach (Triangle t in curConflict.TriangleList)
                {
                    if (!pointList.Contains(t.point1)) pointList.Add(t.point1);
                    if (!pointList.Contains(t.point2)) pointList.Add(t.point2);
                    if (!pointList.Contains(t.point3)) pointList.Add(t.point3);
                }
                List<TriNode> removeRange = null;
                //左边冲突点
                List<TriNode> leftObjectPointList = (skelarc.LeftMapObj as PolylineObject).PointList;//左边线目标的顶点列表
                curConflict.LeftPointList = new List<TriNode>();//左边线目标的顶点列表
                curConflict.LeftPointList.AddRange(leftObjectPointList);
                removeRange = new List<TriNode>();
                foreach (TriNode p in curConflict.LeftPointList)
                {
                    if (!pointList.Contains(p))
                        removeRange.Add(p);

                }
                foreach (TriNode p in removeRange)
                {
                    curConflict.LeftPointList.Remove(p);
                }
                //右边冲突点
                List<TriNode> rightObjectPointList = (skelarc.RightMapObj as PolylineObject).PointList;//右边线目标的顶点列表
                curConflict.RightPointList = new List<TriNode>();//左边线目标的顶点列表
                curConflict.RightPointList.AddRange(rightObjectPointList);
                removeRange = new List<TriNode>();
                foreach (TriNode p in curConflict.RightPointList)
                {
                    if (!pointList.Contains(p))
                        removeRange.Add(p);

                }
                foreach (TriNode p in removeRange)
                {
                    curConflict.RightPointList.Remove(p);
                }

                isCf = false;
                curConflict = null;
            }

            #region 删除交叉点处的伪冲突点
            int n = skelarc.TriangleList.Count;
            if (skelarc.TriangleList[0].TriType == 2 || skelarc.TriangleList[n - 1].TriType == 2)
            {
                List<TriNode> excludeLPList = new List<TriNode>();
                List<TriNode> excludeRPList = new List<TriNode>();
                PolylineObject leftLineObject = skelarc.LeftMapObj as PolylineObject;
                PolylineObject rightLineObject = skelarc.RightMapObj as PolylineObject;

                if (skelarc.TriangleList[0].TriType == 2)
                {
                    this.ExcludePointatCrossover(ref excludeLPList, skelarc.TriangleList[0], leftLineObject, d);
                    this.ExcludePointatCrossover(ref excludeRPList, skelarc.TriangleList[0], rightLineObject, d);
                }
                if (skelarc.TriangleList[n - 1].TriType == 2)
                {
                    this.ExcludePointatCrossover(ref excludeLPList, skelarc.TriangleList[n - 1], leftLineObject, d);
                    this.ExcludePointatCrossover(ref excludeRPList, skelarc.TriangleList[n - 1], rightLineObject, d);
                }

                foreach (TriNode p in excludeLPList)
                {
                    foreach (Conflict_L c in conflictList)
                    {
                        if (c.LeftPointList.Contains(p))
                            c.LeftPointList.Remove(p);
                    }
                }

                foreach (TriNode p in excludeRPList)
                {
                    foreach (Conflict_L c in conflictList)
                    {
                        if (c.RightPointList.Contains(p))
                            c.RightPointList.Remove(p);
                    }
                }
            }

            #endregion

            #region 删除左右冲突的点个数均为0的冲突
            List<Conflict_L> delCList = new List<Conflict_L>();
            foreach (Conflict_L c in conflictList)
            {
                if (c.RightPointList.Count == 0 && c.LeftPointList.Count == 0)
                {
                    delCList.Add(c);
                }
            }
            foreach (Conflict_L c in delCList)
            {
                conflictList.Remove(c);
            }


            #endregion
            return conflictList;

        }

        /// <summary>
        /// 检测只要三角形状
        /// </summary>
        /// <param name="skelarc">骨架线弧段</param>
        /// <param name="d">距离阈值</param>
        /// <returns>冲突列表</returns>
        private List<Conflict_L> DetectAllConflict(Skeleton_Arc skelarc, double d)
        {
            List<Conflict_L> conflictList = new List<Conflict_L>();
            int triCount = skelarc.TriangleList.Count;
            if (triCount < 2)//没有1类三角形的情况
            {
                return null;
            }
            bool isCf = false;
            Conflict_L curConflict = null;
            Triangle curTri = null;
            //List<Triangle> curTriangleList = null;

            for (int i = 0; i < triCount; i++)
            {
                curTri = skelarc.TriangleList[i];
                if (curTri.TriType == 1)//只考虑1类三角形
                {
                    if (curTri.W < d)
                    {

                        //如果是起始冲突三角形
                        if (!isCf)
                        {
                            curConflict = new Conflict_L(skelarc, d);
                            //curConflict. = new List<Triangle>();
                            curConflict.TriangleList.Add(curTri);
                            isCf = true;
                        }
                        else
                        {
                            curConflict.TriangleList.Add(curTri);

                        }
                    }
                    //如果不是冲突三角形
                    else
                    {
                        if (isCf == true)
                        {
                            conflictList.Add(curConflict);

                            ////三角形序列中包含的顶点
                            //List<TriNode> pointList = new List<TriNode>();
                            //foreach (Triangle t in curConflict.TriangleList)
                            //{
                            //    if (!pointList.Contains(t.point1)) pointList.Add(t.point1);
                            //    if (!pointList.Contains(t.point2)) pointList.Add(t.point2);
                            //    if (!pointList.Contains(t.point3)) pointList.Add(t.point3);
                            //}
                            //List<TriNode> removeRange = null;
                            ////左边冲突点
                            //List<TriNode> leftObjectPointList = (skelarc.LeftMapObj as PolylineObject).PointList;//左边线目标的顶点列表
                            //curConflict.LeftPointList = new List<TriNode>();//左边线目标的顶点列表
                            //curConflict.LeftPointList.AddRange(leftObjectPointList);
                            //removeRange = new List<TriNode>();
                            //foreach (TriNode p in curConflict.LeftPointList)
                            //{
                            //    if (!pointList.Contains(p))
                            //        removeRange.Add(p);

                            //}
                            //foreach (TriNode p in removeRange)
                            //{
                            //    curConflict.LeftPointList.Remove(p);
                            //}
                            ////右边冲突点
                            //List<TriNode> rightObjectPointList = (skelarc.RightMapObj as PolylineObject).PointList;//右边线目标的顶点列表
                            //curConflict.RightPointList = new List<TriNode>();//左边线目标的顶点列表
                            //curConflict.RightPointList.AddRange(rightObjectPointList);
                            //removeRange = new List<TriNode>();
                            //foreach (TriNode p in curConflict.RightPointList)
                            //{
                            //    if (!pointList.Contains(p))
                            //        removeRange.Add(p);

                            //}
                            //foreach (TriNode p in removeRange)
                            //{
                            //    curConflict.RightPointList.Remove(p);
                            //}

                            isCf = false;
                            curConflict = null;

                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }

            if (isCf == true)
            {

                conflictList.Add(curConflict);

                ////三角形序列中包含的顶点
                //List<TriNode> pointList = new List<TriNode>();
                //foreach (Triangle t in curConflict.TriangleList)
                //{
                //    if (!pointList.Contains(t.point1)) pointList.Add(t.point1);
                //    if (!pointList.Contains(t.point2)) pointList.Add(t.point2);
                //    if (!pointList.Contains(t.point3)) pointList.Add(t.point3);
                //}
                //List<TriNode> removeRange = null;
                ////左边冲突点
                //List<TriNode> leftObjectPointList = (skelarc.LeftMapObj as PolylineObject).PointList;//左边线目标的顶点列表
                //curConflict.LeftPointList = new List<TriNode>();//左边线目标的顶点列表
                //curConflict.LeftPointList.AddRange(leftObjectPointList);
                //removeRange = new List<TriNode>();
                //foreach (TriNode p in curConflict.LeftPointList)
                //{
                //    if (!pointList.Contains(p))
                //        removeRange.Add(p);

                //}
                //foreach (TriNode p in removeRange)
                //{
                //    curConflict.LeftPointList.Remove(p);
                //}
                ////右边冲突点
                //List<TriNode> rightObjectPointList = (skelarc.RightMapObj as PolylineObject).PointList;//右边线目标的顶点列表
                //curConflict.RightPointList = new List<TriNode>();//左边线目标的顶点列表
                //curConflict.RightPointList.AddRange(rightObjectPointList);
                //removeRange = new List<TriNode>();
                //foreach (TriNode p in curConflict.RightPointList)
                //{
                //    if (!pointList.Contains(p))
                //        removeRange.Add(p);

                //}
                //foreach (TriNode p in removeRange)
                //{
                //    curConflict.RightPointList.Remove(p);
                //}

                isCf = false;
                curConflict = null;
            }

            //#region 删除交叉点处的伪冲突点
            //int n = skelarc.TriangleList.Count;
            //if (skelarc.TriangleList[0].TriType == 2 || skelarc.TriangleList[n - 1].TriType == 2)
            //{
            //    List<TriNode> excludeLPList = new List<TriNode>();
            //    List<TriNode> excludeRPList = new List<TriNode>();
            //    PolylineObject leftLineObject = skelarc.LeftMapObj as PolylineObject;
            //    PolylineObject rightLineObject = skelarc.RightMapObj as PolylineObject;

            //    if (skelarc.TriangleList[0].TriType == 2)
            //    {
            //        this.ExcludePointatCrossover(ref excludeLPList, skelarc.TriangleList[0], leftLineObject, d);
            //        this.ExcludePointatCrossover(ref excludeRPList, skelarc.TriangleList[0], rightLineObject, d);
            //    }
            //    if (skelarc.TriangleList[n - 1].TriType == 2)
            //    {
            //        this.ExcludePointatCrossover(ref excludeLPList, skelarc.TriangleList[n - 1], leftLineObject, d);
            //        this.ExcludePointatCrossover(ref excludeRPList, skelarc.TriangleList[n - 1], rightLineObject, d);
            //    }

            //    foreach (TriNode p in excludeLPList)
            //    {
            //        foreach (Conflict_L c in conflictList)
            //        {
            //            if (c.LeftPointList.Contains(p))
            //                c.LeftPointList.Remove(p);
            //        }
            //    }

            //    foreach (TriNode p in excludeRPList)
            //    {
            //        foreach (Conflict_L c in conflictList)
            //        {
            //            if (c.RightPointList.Contains(p))
            //                c.RightPointList.Remove(p);
            //        }
            //    }
            //}

            //#endregion

            //#region 删除左右冲突的点个数均为0的冲突
            //List<Conflict_L> delCList = new List<Conflict_L>();
            //foreach (Conflict_L c in conflictList)
            //{
            //    if (c.RightPointList.Count == 0 && c.LeftPointList.Count == 0)
            //    {
            //        delCList.Add(c);
            //    }
            //}
            //foreach (Conflict_L c in delCList)
            //{
            //    conflictList.Remove(c);
            //}


            //#endregion
            return conflictList;

        }

        /// <summary>
        /// 找出交叉点d内的所有点，以便排除
        /// </summary>
        /// <param name="excludePList">需排除的点</param>
        /// <param name="crossOver">交叉点</param>
        /// <param name="lineObject">线对象</param>
        private void ExcludePointatCrossover(ref List<TriNode> excludePList, Triangle crossOverTri, PolylineObject lineObject, double d)
        {
            if (excludePList == null)
                excludePList = new List<TriNode>();
            TriNode node;
            TriEdge vEdge, redge1, redge2;
            node = crossOverTri.GetStartPointofT2(out  vEdge, out  redge1, out  redge2);
            if (lineObject.PointList[0] == node)
            {
                int i = 1;
                double culLen = 0;
                while (i < lineObject.PointList.Count && culLen < d)
                {
                    if (!excludePList.Contains(lineObject.PointList[i]))
                    {
                        excludePList.Add(lineObject.PointList[i]);
                    }
                    culLen += AuxStructureLib.ComFunLib.CalLineLength(lineObject.PointList[i - 1], lineObject.PointList[i]);
                    i++;
                }
            }

            else if (lineObject.PointList[lineObject.PointList.Count - 1] == node)
            {
                int i = lineObject.PointList.Count - 2;
                double culLen = 0;
                while (i > 0 && culLen < d)
                {
                    if (!excludePList.Contains(lineObject.PointList[i]))
                    {
                        excludePList.Add(lineObject.PointList[i]);
                    }
                    culLen +=AuxStructureLib.ComFunLib.CalLineLength(lineObject.PointList[i + 1], lineObject.PointList[i]);
                    i--;
                }
            }
            //如果保留超出半径的前一个点，冲突覆盖范围就过大，反之，冲突覆盖范围又太小
            //int ct = excludePList.Count;
            //if (ct > 1)
            //{
            //    excludePList.RemoveAt(ct - 1);
            //}
        }

        /// <summary>
        /// 输出冲突到TXT文件
        /// </summary>
        /// <param name="strPath"></param>
        /// <param name="iteraID"></param>
        public void OutputConflicts(string strPath, int iteraID)
        {

            if (this.ConflictList == null || ConflictList.Count == 0)
                return;
            DataSet ds = new DataSet();
            //创建一个表
            DataTable tableforce = new DataTable();
            tableforce.TableName = "Conflicts"+iteraID.ToString();
            tableforce.Columns.Add("ID", typeof(int));
            tableforce.Columns.Add("LeftObjID", typeof(int));
            tableforce.Columns.Add("LeftObjType", typeof(string));
            tableforce.Columns.Add("RightObjID", typeof(int));
            tableforce.Columns.Add("RightObjType", typeof(string));
            tableforce.Columns.Add("Severity", typeof(double));
            int id = 1;
            foreach (Conflict_R c in ConflictList)
            {

                DataRow dr = tableforce.NewRow();
                dr[0] = id;
                dr[1] = c.Skel_arc.LeftMapObj.ID;
               
                dr[2] = c.Skel_arc.LeftMapObj.FeatureType.ToString();
                dr[3] = c.Skel_arc.RightMapObj.ID;
                dr[4] = c.Skel_arc.RightMapObj.FeatureType.ToString();
            
                dr[5] = c.DisThreshold - c.Distance;
                tableforce.Rows.Add(dr);
                id++;   
                
                c.Skel_arc.RightMapObj.ConflictCount++; 
                c.Skel_arc.LeftMapObj.ConflictCount++;
            }
            TXTHelper.ExportToTxt(tableforce, strPath + @"\Conflict" + iteraID.ToString() + @".txt");
        }


        /// <summary>
        /// 输出冲突到TXT文件
        /// </summary>
        /// <param name="strPath"></param>
        /// <param name="iteraID"></param>
        public int  CountConflicts()
        {
            int count = 0;
            foreach (Conflict_R c in ConflictList)
            {

                double server = c.DisThreshold - c.Distance;
                double t = 0.1 * c.DisThreshold;
                if (server > t)
                { count++; }
            }
            return count;

        }
    }
}


