using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuxStructureLib
{
    public  class ConflictDetection
    {
        public SDS SDSMap = null;

        public ConflictDetection(SDS map)
        {
            SDSMap = map;
        }
        #region 冲突检测算法
        public List<Conflict> ConflictDetect(double PLDtol, double PPDtol)
        {
            List<Conflict> InitConflictPL= ConflictDetect(PLDtol);
            List<Conflict> InitConflictPP = ConflictDetect(PPDtol);
            List<Conflict> ResConflict = new List<Conflict>();
            if (InitConflictPL != null && InitConflictPL.Count > 0)
            {
                foreach (Conflict curCandidatePLC in InitConflictPL)
                {
                    if (curCandidatePLC.ConflictType == "PL")
                        ResConflict.Add(curCandidatePLC);
                }
            }
            if (InitConflictPP != null && InitConflictPP.Count > 0)
            {
                foreach (Conflict curCandidatePPC in InitConflictPP)
                {
                    if (curCandidatePPC.ConflictType == "PP")
                        ResConflict.Add(curCandidatePPC);
                }
            }

            return ResConflict;
        }
        
        
        /// <summary>
        /// 冲突检测
        /// </summary>
        /// <param name="Dtol"></param>
        /// <returns></returns>
        public List<Conflict> ConflictDetect(double Dtol)
        {
            List<Conflict> ConflictList = new List<Conflict>();
            foreach (SDS_PolygonO O in this.SDSMap.PolygonOObjs)
            {
                List<ISDS_MapObject> curObjList = FindToleranceSetwithDistance(O, Dtol);//计算距离
                if (curObjList == null || curObjList.Count == 0)
                    continue;
                foreach (ISDS_MapObject curObj in curObjList)
                {
                    if (!IsContainConflict(ConflictList, curObj, O))
                    {
                        Conflict curCon = new Conflict(O, curObj);
                        curCon.Distance = curObj.SomeAtriValue;
                        ConflictList.Add(curCon);
                    }
                }
            }
            return ConflictList;
        }

        /// <summary>
        /// 仅检查单个目标涉及的冲突
        /// </summary>
        /// <param name="Obj"></param>
        /// <param name="Dtol"></param>
        /// <returns></returns>
        public List<Conflict> ObjectConflictDetect(ISDS_MapObject Obj, double Dtol)
        {
            List<Conflict> ConflictList = new List<Conflict>();

            List<ISDS_MapObject> curObjList = FindToleranceSetwithDistance(Obj, Dtol);
            if (curObjList == null || curObjList.Count == 0)
                return null;
            foreach (ISDS_MapObject curObj in curObjList)
            {
                if (!IsContainConflict(ConflictList, curObj, Obj))
                {
                    Conflict curCon = new Conflict(Obj, curObj);
                    curCon.Distance = curObj.SomeAtriValue;
                    ConflictList.Add(curCon);
                }

            }
            return ConflictList;
        }

        /// <summary>
        /// 检查单个目标涉及的PP冲突和PL冲突
        /// </summary>
        /// <param name="Obj">对象</param>
        /// <param name="PPtol">PP阈值</param>
        /// <param name="PLtol">PL阈值</param>
        /// <returns></returns>
        public List<Conflict> ObjectConflictDetectPP_PL(ISDS_MapObject Obj, double PPtol, double PLtol)
        {

            List<Conflict> InitConflictPL = ObjectConflictDetect(Obj,PLtol);
            List<Conflict> InitConflictPP = ObjectConflictDetect(Obj,PPtol);
            List<Conflict> ResConflict = new List<Conflict>();
            if (InitConflictPL != null && InitConflictPL.Count > 0)
            {
                foreach (Conflict curCandidatePLC in InitConflictPL)
                {
                    if (curCandidatePLC.ConflictType == "PL")
                        ResConflict.Add(curCandidatePLC);
                }
            }
            if (InitConflictPP != null && InitConflictPP.Count > 0)
            {
                foreach (Conflict curCandidatePPC in InitConflictPP)
                {
                    if (curCandidatePPC.ConflictType == "PP")
                        ResConflict.Add(curCandidatePPC);
                }
            }
            return ResConflict;
        }
        /// <summary>
        /// 是否存在冲突
        /// </summary>
        /// <param name="O1"></param>
        /// <param name="O2"></param>
        /// <returns></returns>
        private bool IsContainConflict(List<Conflict> CLsit, ISDS_MapObject O1, ISDS_MapObject O2)
        {
            foreach (Conflict curConflict in CLsit)
            {
                if ((curConflict.Obj1 == O1 && curConflict.Obj2 == O2) || (curConflict.Obj1 == O2 && curConflict.Obj2 == O1))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 搜索一个对象O周围阈值为Dtol内的其它所有对象
        /// </summary>
        /// <param name="O">目标对象</param>
        /// <param name="Dtol">距离阈值</param>
        /// <returns>与目标冲突的对象集合</returns>
        public List<ISDS_MapObject> FindToleranceSet(ISDS_MapObject O, double Dtol)
        {
            List<ISDS_MapObject> ConfectedObjectList = new List<ISDS_MapObject>();//定义冲突对象几何
            List<SDS_Edge> OEdges = GetObjectEdges(O);
            foreach (SDS_Edge e in OEdges)
            {
                Queue<SDS_Edge> QueueEdges = new Queue<SDS_Edge>();
                List<SDS_Edge> TestedEdges = new List<SDS_Edge>();
                List<SDS_Triangle> TestedTriangles = new List<SDS_Triangle>();

                QueueEdges.Enqueue(e); //入队列
                while (QueueEdges.Count > 0)//队列不为空
                {
                    SDS_Edge e1 = QueueEdges.Dequeue();
                    TestedEdges.Add(e1);
                    //for each triangle T adjacent to e1
                    {
                        SDS_Triangle T = e1.LeftTriangle;
                        if (T != null && !SDS.IsContainTri(TestedTriangles, T))
                        {
                            SDS_PolygonO objP = GetobjectbyTri(T);
                            if ((objP as ISDS_MapObject) != O)
                            {
                                if (objP != null && !IsContainObj(ConfectedObjectList, objP))
                                {
                                    ConfectedObjectList.Add(objP);
                                }
                                else
                                {
                                    SDS_PolylineObj objL = GetobjectbyEdge(e1);
                                    if (objL != null && !IsContainObj(ConfectedObjectList, objL))
                                    {
                                        ConfectedObjectList.Add(objL);
                                    }
                                }

                                foreach (SDS_Edge e2 in T.Edges)
                                {
                                    if (!IsContainEdge(QueueEdges, e2) && !IsContainEdge(TestedEdges, e2))
                                    {
                                        if (CalDistance(e, e2) < Dtol)
                                        {
                                            QueueEdges.Enqueue(e2);
                                        }
                                    }
                                }
                            }
                        }

                        T = e1.RightTriangle;
                        if (T != null && !SDS.IsContainTri(TestedTriangles, T))
                        {
                            SDS_PolygonO objP = GetobjectbyTri(T);
                            if ((objP as ISDS_MapObject) != O)
                            {
                                if (objP != null && !IsContainObj(ConfectedObjectList, objP))
                                {
                                    ConfectedObjectList.Add(objP);
                                }
                                else
                                {
                                    SDS_PolylineObj objL = GetobjectbyEdge(e1);
                                    if (objL != null && !IsContainObj(ConfectedObjectList, objL))
                                    {
                                        ConfectedObjectList.Add(objL);
                                    }
                                }

                                foreach (SDS_Edge e2 in T.Edges)
                                {
                                    if (!IsContainEdge(QueueEdges, e2) && !IsContainEdge(TestedEdges, e2))
                                    {
                                        if (CalDistance(e, e2) < Dtol)
                                        {
                                            QueueEdges.Enqueue(e2);
                                        }
                                    }
                                }
                            }
                        }
                        //for each triangle T adjacent to e1
                        TestedTriangles.Add(T);
                    }

                }
            }
            return ConfectedObjectList;
        }


        /// <summary>
        /// 搜索一个对象O周围阈值为Dtol内的其它所有对象无法得到最近距离。
        /// </summary>
        /// <param name="O">目标对象</param>
        /// <param name="Dtol">距离阈值</param>
        /// <returns>与目标冲突的对象集合</returns>
        public List<ISDS_MapObject> FindToleranceSetwithDistance(ISDS_MapObject O, double Dtol)
        {
            List<ISDS_MapObject> ConfectedObjectList = new List<ISDS_MapObject>();//定义冲突对象几何
            List<SDS_Edge> OEdges = GetObjectEdges(O);
            foreach (SDS_Edge e in OEdges)
            {
                Queue<SDS_Edge> QueueEdges = new Queue<SDS_Edge>();
                Queue<double> QueueDistance = new Queue<double>();
                List<SDS_Edge> TestedEdges = new List<SDS_Edge>();
                List<SDS_Triangle> TestedTriangles = new List<SDS_Triangle>();

                QueueEdges.Enqueue(e); //入队列
                QueueDistance.Enqueue(0.0);//对应的距离
                while (QueueEdges.Count > 0)//队列不为空
                {
                    SDS_Edge e1 = QueueEdges.Dequeue();
                    double d1 = QueueDistance.Dequeue();
                    TestedEdges.Add(e1);
                    //for each triangle T adjacent to e1
                    {
                        SDS_Triangle T = e1.LeftTriangle;
                        if (T != null && !SDS.IsContainTri(TestedTriangles, T))
                        {
                            SDS_PolygonO objP = GetobjectbyTri(T);
                            if ((objP as ISDS_MapObject) != O)
                            {
                                if (objP != null)
                                {
                                    if (!IsContainObj(ConfectedObjectList, objP))
                                    {
                                        objP.SomeAtriValue = d1;
                                        ConfectedObjectList.Add(objP);
                                    }
                                    else
                                    {
                                        if (objP.SomeAtriValue > d1)
                                        {
                                            objP.SomeAtriValue = d1;;
                                        }
                                    }
                                }
                                else
                                {
                                    SDS_PolylineObj objL = GetobjectbyEdge(e1);
                                    if (objL != null)
                                    {
                                        if (!IsContainObj(ConfectedObjectList, objL))
                                        {
                                            objL.SomeAtriValue = d1;
                                            ConfectedObjectList.Add(objL);
                                        }
                                        else
                                        {
                                            if (objL.SomeAtriValue > d1)
                                            {
                                                objL.SomeAtriValue = d1; ;
                                            }
                                        }
                                    }
                                }

                                foreach (SDS_Edge e2 in T.Edges)
                                {
                                    if (!IsContainEdge(QueueEdges, e2) && !IsContainEdge(TestedEdges, e2))
                                    {
                                        double d2 = CalDistance(e, e2);
                                        if (d2 < Dtol)
                                        {
                                            QueueEdges.Enqueue(e2);
                                            QueueDistance.Enqueue(d2);
                                        }
                                    }
                                }
                            }
                        }

                        T = e1.RightTriangle;
                        if (T != null && !SDS.IsContainTri(TestedTriangles, T))
                        {
                            SDS_PolygonO objP = GetobjectbyTri(T);
                            if ((objP as ISDS_MapObject) != O)
                            {
                                if (objP != null)
                                {
                                    if (!IsContainObj(ConfectedObjectList, objP))
                                    {
                                        objP.SomeAtriValue = d1;
                                        ConfectedObjectList.Add(objP);
                                    }
                                    else
                                    {
                                        if (objP.SomeAtriValue > d1)
                                        {
                                            objP.SomeAtriValue = d1; ;
                                        }
                                    }
                                }
                                else
                                {
                                    SDS_PolylineObj objL = GetobjectbyEdge(e1);
                                    if (objL != null)
                                    {
                                        if (!IsContainObj(ConfectedObjectList, objL))
                                        {
                                            objL.SomeAtriValue = d1;
                                            ConfectedObjectList.Add(objL);
                                        }
                                        else
                                        {
                                            if (objL.SomeAtriValue > d1)
                                            {
                                                objL.SomeAtriValue = d1; ;
                                            }
                                        }
                                    }
                                }

                                foreach (SDS_Edge e2 in T.Edges)
                                {
                                    if (!IsContainEdge(QueueEdges, e2) && !IsContainEdge(TestedEdges, e2))
                                    {
                                        double d2 = CalDistance(e, e2);
                                        if (d2 < Dtol)
                                        {
                                            QueueEdges.Enqueue(e2);
                                            QueueDistance.Enqueue(d2);
                                        }
                                    }
                                }
                            }
                        }
                        //for each triangle T adjacent to e1
                        TestedTriangles.Add(T);
                    }

                }
            }
            return ConfectedObjectList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="QueueEdges"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        private bool IsContainEdge(Queue<SDS_Edge> QueueEdges, SDS_Edge edge)
        {
            foreach (SDS_Edge curEdge in QueueEdges)
            {
                if (curEdge == edge)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="QueueEdges"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        private bool IsContainEdge(List<SDS_Edge> QueueEdges, SDS_Edge edge)
        {
            foreach (SDS_Edge curEdge in QueueEdges)
            {
                if (curEdge == edge)
                    return true;
            }
            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Edges"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        private bool IsContainObj(List<ISDS_MapObject> Objs, ISDS_MapObject O)
        {
            foreach (ISDS_MapObject curO in Objs)
            {
                if (curO == O)
                    return true;
            }
            return false;
        }
        /// <summary>
        /// 计算两线段之间的最短距离
        /// </summary>
        /// <param name="e1">线段1</param>
        /// <param name="e2">线段2</param>
        /// <returns>距离值</returns>
        private double CalDistance(SDS_Edge e1, SDS_Edge e2)
        {
            SDS_Node p1 = e1.StartPoint;
            SDS_Node p2 = e1.EndPoint;
            SDS_Node p3 = e2.StartPoint;
            SDS_Node p4 = e2.EndPoint;

            double d1 = ComFunLib.CalMinDisPoint2Line(p1, p3, p4);
            double d2 = ComFunLib.CalMinDisPoint2Line(p2, p3, p4);
            if (d1 > d2)
                d1 = d2;
            d2 = ComFunLib.CalMinDisPoint2Line(p3, p1, p2);
            double d3 = ComFunLib.CalMinDisPoint2Line(p4, p1, p2);
            if (d2 > d3)
                d2 = d3;
            if (d1 > d2)
                d1 = d2;
            return d1;
        }

        private SDS_PolygonO GetobjectbyTri(SDS_Triangle T)
        {
            if (T.Polygon != null)
                return T.Polygon as SDS_PolygonO;

            return null;
        }



        private SDS_PolylineObj GetobjectbyEdge(SDS_Edge e)
        {
            if (e.MapObject != null)
                return e.MapObject as SDS_PolylineObj;
            return null;
        }

        /// <summary>
        /// 获得一个对象的所有边
        /// </summary>
        /// <param name="O">目标对象</param>
        /// <returns>所有边</returns>
        private List<SDS_Edge> GetObjectEdges(ISDS_MapObject O)
        {
            string objectType = O.ToString();
            if (objectType == @"AuxStructureLib.SDS_PolylineObj")
            {
                return (O as SDS_PolylineObj).Edges;
            }
            else if (objectType == @"AuxStructureLib.SDS_PolygonO")
            {
                return (O as SDS_PolygonO).Edges;
            }
            return null;
        }
        /// <summary>
        /// 获取与一条边毗邻的所有三角形
        /// </summary>
        /// <param name="e">边</param>
        /// <returns>三角形数组</returns>
        private List<SDS_Triangle> GetEdgeAdjacentedTris(SDS_Edge e)
        {
            List<SDS_Triangle> TriList = new List<SDS_Triangle>();
            SDS_Triangle leftTri = e.LeftTriangle;
            SDS_Triangle rightTri = e.RightTriangle;
            if (leftTri != null)
                TriList.Add(leftTri);
            if (rightTri != null)
                TriList.Add(rightTri);
            SDS_Node startN = e.StartPoint;
            SDS_Node endN = e.EndPoint;
            //与起点关联的三角形
            foreach (SDS_Triangle curTri in startN.Triangles)
            {
                if (curTri != null && curTri != leftTri && curTri != rightTri)
                    TriList.Add(curTri);
            }
            //与终点关联的三角形
            foreach (SDS_Triangle curTri in endN.Triangles)
            {
                if (curTri != null && curTri != leftTri && curTri != rightTri)
                    TriList.Add(curTri);
            }
            return TriList;
        }
        #endregion

        /// <summary>
        /// 判断是否存在三角形的穿越
        /// </summary>
        /// <param name="Polygon">多边i型你</param>
        /// <returns></returns>
        /// 
        public bool HasTriangleInverted(SDS_PolygonO Polygon)
        {
            List<SDS_Triangle> triList = Polygon.GetSurroundingTris();
            foreach (SDS_Triangle curTri in triList)
            {
                if (!curTri.IsAnticlockwise())
                    return true;
            }
            return false;
        }
    }
}
