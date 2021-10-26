using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;

namespace RoadDisAlg
{
    public class ComForce
    {
        /// <summary>
        /// 获取conflictShape上离targetPoint最近的点
        /// </summary>
        /// <param name="targetPoint">目标点-目标线上的点</param>
        /// <param name="conflictShape">冲突对的几何图形</param>
        /// <param name="nearestPoint">冲突对象上与targetPoint最近的点</param>
        /// <param name="shortestDis">冲突对象上与targetPoint最近的距离值</param>
        private static void GetProximityPoint_Distance(IPoint targetPoint, IGeometry conflictShape, out IPoint nearestPoint, out double shortestDis)
        {
            IProximityOperator Prxop = conflictShape as IProximityOperator;
            shortestDis = Prxop.ReturnDistance(targetPoint);
            nearestPoint = Prxop.ReturnNearestPoint(targetPoint, esriSegmentExtension.esriNoExtension);
        }

        /// <summary>
        /// 检测几何图形A是否与几何图形B相交
        /// </summary>
        /// <param name="pGeometryA">几何图形A</param>
        /// <param name="pGeometryB">几何图形B</param>
        /// <returns>True为相交，False为不相交</returns>
        private static  bool  CheckGeometryCrosses(IGeometry pGeometryA, IGeometry pGeometryB)
        {
            IRelationalOperator pRelOperator = pGeometryA as IRelationalOperator;
            if (pRelOperator.Crosses(pGeometryB))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 计算两点之间线段的长度
        /// </summary>
        /// <param name="point1">起点</param>
        /// <param name="point2">终点</param>
        /// <returns></returns>
        private static  double LineLength(PointCoord point1, PointCoord point2)
        {
            double dx = point2.X - point1.X;
            double dy = point2.Y - point1.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            return len;
        }

        /// <summary>
        /// 计算两点之间线段的长度
        /// </summary>
        /// <param name="point1">起点</param>
        /// <param name="point2">终点</param>
        /// <returns></returns>
        private static double LineLength(IPoint point1, IPoint point2)
        {
            double dx = point2.X - point1.X;
            double dy = point2.Y - point1.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            return len;
        }

        /// <summary>
        /// 计算顶点的受力-线受力模型
        /// </summary>
        /// <returns>返回受力数组</returns>
        private static Force[] ComForce_L(RoadNetWork netWork)
        {
            //用于存受力的数组
            Force[] forceList = new Force[netWork.PointList.Count];
            for (int i = 0; i < netWork.PointList.Count; i++)
            {
                forceList[i] = new Force();
            }

            ILine Line = null;
            ITopologicalOperator pTopop1 = null;
            ITopologicalOperator pTopop2 = null;
            IPolygon pgon1 = null;
            IPolygon pgon2 = null;
            double bDis1 = 0.0;
            double bDis2 = 0.0;
            Polyline pline1 = null;
            Polyline pline2 = null;
            double Dmin = 0.0;
            foreach (Road curRoad in netWork.RoadList)
            {
                bDis1 = (0.5 * curRoad.RoadGrade.SylWidth + AlgFEM.minDis) * AlgFEM.scale * 0.001;
                pline1 = curRoad.EsriPolyline;
                pTopop1 = pline1 as ITopologicalOperator;
                pgon1 = pTopop1.Buffer(bDis1) as IPolygon;
                foreach (Road curRoad2 in netWork.RoadList)
                {
                    if (curRoad2.RID != curRoad.RID)//不是同一条道路
                    {
                        bDis2 = (0.5 * curRoad2.RoadGrade.SylWidth) * AlgFEM.scale * 0.001; 
                        pline2 = curRoad.EsriPolyline;
                        pTopop2 = pline1 as ITopologicalOperator;
                        pgon2 = pTopop2.Buffer(bDis2) as IPolygon;
                        bool iscosses = CheckGeometryCrosses(pgon1, pgon2);
                        //if (iscosses)
                        {
                            Dmin = (0.5 * (curRoad.RoadGrade.SylWidth + curRoad2.RoadGrade.SylWidth) + AlgFEM.minDis) * AlgFEM.scale * 0.001; ;
                            int n = curRoad.PointList.Count;
                            double l = 0.0;
                            for (int i = 0; i < n - 1; i++)
                            {
                                Line = new LineClass();
                                Line.PutCoords(curRoad.GetCoord(i).EsriPoint, curRoad.GetCoord(i + 1).EsriPoint);
                                l = LineLength(curRoad.GetCoord(i), curRoad.GetCoord(i + 1));
                                double curFx = 0.0;
                                double curFy = 0.0;
                                int m = pline2.PointCount;
                                for (int j = 0; j < m; j++)
                                {
                                    IPoint curPoint = pline2.get_Point(j);
                                    IPoint nearPoint = null;
                                    double nearDis = 0.0;
                                    double absForce = 0.0;
                                    double sin = 0;
                                    double cos = 0;

                                    double l1 = 0.0;
                                    double l2 = 0.0;

                                    GetProximityPoint_Distance(curPoint, Line, out nearPoint, out nearDis);
                                    if (nearDis == 0.0)//如果该点与冲突对象重合，暂不处理
                                        continue;
                                    //受力大小
                                    absForce = Dmin - nearDis;
                                    //当Dmin>dis，才受力
                                    if (absForce > 0)
                                    {
                                        //受力向量方位角的COS
                                        sin = (nearPoint.Y - curPoint.Y) / nearDis;
                                        //受力向量方位角的SIN
                                        cos = (nearPoint.X - curPoint.X) / nearDis;

                                        l1 = LineLength(curRoad.GetCoord(i).EsriPoint, nearPoint);
                                        l2 = LineLength(curRoad.GetCoord(i + 1).EsriPoint, nearPoint);
                                        curFx = absForce * cos;
                                        curFy = absForce * sin;
                                        forceList[curRoad.PointList[i]].Fx += curFx * l1 / l;
                                        forceList[curRoad.PointList[i]].Fy += curFy * l1 / l;
                                        forceList[curRoad.PointList[i + 1]].Fx += curFx * l2 / l;
                                        forceList[curRoad.PointList[i + 1]].Fy += curFy * l2 / l;
                                    }
                                }
                            }

                        }

                    }
                }
                //与河流的处理  
                foreach (RoadLyrInfo curlyr in netWork.RoadLyrInfoList)
                {
                    if (curlyr.RoadGrade.Grade == 999)
                    {
                        bDis2 = (0.5 * curlyr.RoadGrade.SylWidth) * AlgFEM.scale * 0.001; 
                        int s = curlyr.GeoSet.GeometryCount;
                        for (int k = 0; k < s; k++)
                        {
                            pline2 = curlyr.GeoSet.get_Geometry(k) as Polyline;
                            pTopop2 = pline1 as ITopologicalOperator;
                            pgon2 = pTopop2.Buffer(bDis2) as IPolygon;
                            bool iscosses = CheckGeometryCrosses(pgon1, pgon2);
                            // if (iscosses)
                            {
                                Dmin = (0.5 * (curRoad.RoadGrade.SylWidth + curlyr.RoadGrade.SylWidth) + AlgFEM.minDis) * AlgFEM.scale * 0.001; ;
                                int n = curRoad.PointList.Count;
                                double l = 0.0;
                                for (int i = 0; i < n - 1; i++)
                                {
                                    Line = new LineClass();
                                    Line.PutCoords(curRoad.GetCoord(i).EsriPoint, curRoad.GetCoord(i + 1).EsriPoint);
                                    l = LineLength(curRoad.GetCoord(i), curRoad.GetCoord(i + 1));
                                    double curFx = 0.0;
                                    double curFy = 0.0;
                                    int m = pline2.PointCount;
                                    for (int j = 0; j < m; j++)
                                    {
                                        IPoint curPoint = pline2.get_Point(j);
                                        IPoint nearPoint = null;
                                        double nearDis = 0.0;
                                        double absForce = 0.0;
                                        double sin = 0;
                                        double cos = 0;

                                        double l1 = 0.0;
                                        double l2 = 0.0;

                                        GetProximityPoint_Distance(curPoint, Line, out nearPoint, out nearDis);
                                        if (nearDis == 0.0)//如果该点与冲突对象重合，暂不处理
                                            continue;
                                        //受力大小
                                        absForce = Dmin - nearDis;
                                        //当Dmin>dis，才受力
                                        if (absForce > 0)
                                        {
                                            //受力向量方位角的COS
                                            sin = (nearPoint.Y - curPoint.Y) / nearDis;
                                            //受力向量方位角的SIN
                                            cos = (nearPoint.X - curPoint.X) / nearDis;

                                            l1 = LineLength(curRoad.GetCoord(i).EsriPoint, nearPoint);
                                            l2 = LineLength(curRoad.GetCoord(i + 1).EsriPoint, nearPoint);
                                            curFx = absForce * cos;
                                            curFy = absForce * sin;
                                            forceList[curRoad.PointList[i]].Fx += curFx * l1 / l;
                                            forceList[curRoad.PointList[i]].Fy += curFy * l1 / l;
                                            forceList[curRoad.PointList[i + 1]].Fx += curFx * l2 / l;
                                            forceList[curRoad.PointList[i + 1]].Fy += curFy * l2 / l;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            foreach (Force curForce in forceList)
            {
                curForce.F = Math.Sqrt(curForce.Fx * curForce.Fx + curForce.Fy * curForce.Fy);
            }
            return forceList;
        }



        /// <summary>
        /// 计算顶点的受力-点模型
        /// </summary>
        /// <returns>返回受力数组</returns>
        private static Force[] ComForce_V(RoadNetWork netWork)
        {
            //所有点的数组
            List<PointCoord> pointList = netWork.PointList;
            int n = pointList.Count;
            //用于存受力的数组
            Force[] forceList = new Force[n];
            IPoint curPoint = null;         //当前顶点 
            IGeometry curShape = null;     //当前几何体-？目前还不知用什么方法选取，只能人工选择了
            IPoint nearPoint = null;         //当前几何对象到起点的最近点
            double nearDis = 0.0;             //当前几何对象到起点的最近距离
            double cos = 0.0;
            double sin = 0.0;
            double absForce = 0.0;              //记录线段终点点的受力大小
            double curFx = 0.0;
            double curFy = 0.0;
            //距离阈值，小于该阈值将产生冲突
            double Dmin = 0.0;
            double sylWidthP = 0.0;
            double sylWidthL = 0.0;
            //将当前Path上各个点对所有的Geometry求受力，并累加起来求合力（均分解为全局坐标系下（X，Y）的力）
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;
            for (int i = 0; i < n; i++)
            {
                sylWidthP = pointList[i].SylWidth;
                curPoint = new PointClass();
                curPoint.PutCoords(pointList[i].X, pointList[i].Y);
                curFx = 0;
                curFy = 0;
                foreach (RoadLyrInfo curLyrinfo in netWork.RoadLyrInfoList)
                {
                    sylWidthL = curLyrinfo.RoadGrade.SylWidth;
                    Dmin = (0.5 * (sylWidthL + sylWidthP) + AlgFEM.minDis) * 0.001 * AlgFEM.scale;
                    int m = curLyrinfo.GeoSet.GeometryCount;
                    for (int j = 0; j < m; j++)
                    {
                        curShape = curLyrinfo.GeoSet.get_Geometry(j);
                        GetProximityPoint_Distance(curPoint, curShape, out nearPoint, out nearDis);
                        if (nearDis == 0.0)//如果该点与冲突对象重合，暂不处理
                            continue;
                        //受力大小
                        absForce = Dmin - nearDis;
                        //当Dmin>dis，才受力
                        if (absForce > 0)
                        {
                            //受力向量方位角的COS
                            sin = (curPoint.Y - nearPoint.Y) / nearDis;
                            //受力向量方位角的SIN
                            cos = (curPoint.X - nearPoint.X) / nearDis;
                            curFx += absForce * cos;
                            curFy += absForce * sin;     
                        }
                    }
                    forceList[i] = new Force();
                    forceList[i].Fx = curFx;
                    forceList[i].Fy = curFy;
                    forceList[i].F = Math.Sqrt(curFx * curFx + curFy * curFy);
                }
            }
            return forceList;
        }

        /// <summary>
        /// 计算顶点的受力-混合模型
        /// </summary>
        /// <returns>返回受力数组</returns>
        private static  Force[] ComForce_Combine(RoadNetWork netWork)
        {
            Force[] forceV = ComForce_V(netWork);
            Force[] forceL = ComForce_L(netWork);

            int n = forceV.Length;
            for (int i = 0; i < n; i++)
            {
                forceV[i].Fx += forceV[i].Fx + forceL[i].Fx;
                forceV[i].Fy += forceV[i].Fy + forceL[i].Fy;
            }
            return forceV;
        }

  
        /// <summary>
        /// 计算顶点的受力-最大值模型
        /// </summary>
        /// <returns>返回受力数组</returns>
        private static  Force[] ComForce_Max(RoadNetWork netWork)
        {
            Force[] forceV = ComForce_V(netWork);
            Force[] forceL = ComForce_L(netWork);

            int n = forceV.Length;
            for (int i = 0; i < n; i++)
            {
                if (forceV[i].F < forceL[i].F)
                {
                    forceV[i].Fx = forceL[i].Fx;
                    forceV[i].Fy = forceL[i].Fy;
                }
            }
            return forceV;
        }

        /// <summary>
        /// 获取受力
        /// </summary>
        /// <returns></returns>
        public static  Force[] GetForce(RoadNetWork netWork)
        {
            switch (AlgFEM.ForceType)
            {
                case ForceType.Vetex:
                    return ComForce_V(netWork);
   
                case ForceType.Line:
                    return ComForce_L(netWork);
          
                case ForceType.Combine:
                    return ComForce_Combine(netWork);
                 
                case ForceType.Max:
                    return ComForce_Max(netWork);
                default:
                    return ComForce_Max(netWork);
             
            }
        }
        /// <summary>
        /// z判断是否还有冲突
        /// </summary>
        /// <param name="forceList"></param>
        /// <returns></returns>
        public static bool IsHasForce(Force[] forceList)
        {
            foreach (Force curF in forceList)
            {
                if (curF.F < 0.0001)//判断条件有误
                {
                    return true;
                }
            }
            return false;
        }
    }
}
