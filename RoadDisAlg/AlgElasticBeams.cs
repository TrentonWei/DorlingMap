using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using MatrixOperation;
using ESRI.ArcGIS.Geometry;
using System.IO;

namespace RoadDisAlg
{
    /// <summary>
    /// 弹性梁算法
    /// </summary>
    public class AlgElasticBeams
    {
        public RoadNetWork _NetWork = null;                            //道路网络对象
        //private RoadNetWork _CopyNetWork = null;                     //原始道路网的拷贝

        public static float A = 2F;                           //横截面积，尚不好确定-一般设为符号宽度的1/10，地图单位;而武芳：A=k*d*d
        public static float E = 100000000000F;                              //弹性模量,一般设为1-2
        public static float I = 1F;                            //惯性力,一般为符号宽度的两倍与平均曲率的乘积

        public static double r = 1;                               //迭代步长
        public static int time =4;                                  //迭代次数

        public static double minDis = 0.2;                             //图上最小间距单位为毫米
        public static double scale = 500000.0;                       //比例尺分母
  
        public static StatisticDisValue statisticDisValue;
        public static ForceType ForceType = ForceType.Vetex;

        Force[] forceList = null;                                            //每个点上的受力
        private Matrix _K = null;                                            //刚度矩阵
        private Matrix _F = null;                                            //受力向量                            
        private Matrix _d = null;                                            //移位结果

        /// <summary> 
        /// 构造函数
        /// </summary>
        /// <param name="netWork">待移位的道路网对象</param>
        public AlgElasticBeams(RoadNetWork netWork)
        {
            _NetWork = netWork;
            int n = netWork.PointList.Count;
            _K = new Matrix(3 * n, 3 * n);
        }

        /// <summary>
        /// 计算刚度矩阵
        /// </summary>
        private void ComMatrix_K()
        {
            foreach (Road curRoad in _NetWork.RoadList)
            {
                int n = curRoad.PointList.Count;
                for (int i = 0; i < n - 1; i++)
                {
                    CalcuLineMatrix(curRoad.GetCoord(i), curRoad.GetCoord(i + 1));
                }
            }
        }

        /// <summary>
        /// 计算线段单元的刚度矩阵
        /// </summary>
        /// <param name="line">线段对象</param>
        /// <returns>返回刚度矩阵4*4</returns>
        private void CalcuLineMatrix(PointCoord fromPoint, PointCoord toPoint)
        {
            //线段长度
            double L = Math.Sqrt((fromPoint.Y - toPoint.Y) * (fromPoint.Y - toPoint.Y) + (fromPoint.X - toPoint.X) * (fromPoint.X - toPoint.X));
            //计算该线段的刚度矩阵
            
            //线段方位角的COS
            double sin = (toPoint.Y - fromPoint.Y) / L;
            //线段方位角的SIN
            double cos = (toPoint.X - fromPoint.X) / L;

            int i = fromPoint.ID;
            int j = toPoint.ID;

            //计算用的临时变量
            double EL = E / L;
            double IL2 = 12 * I / (L * L);
            double IL1 = 6 * I / L;
            double cc = cos * cos;
            double ss = sin * sin;
            double cs = cos * sin;
            double mA = A * scale;//尚不好确定，武芳：A=kd*kd
            double ACCIL2SS = EL * (A * cc + IL2 * ss);
            double ASSIL2CC = EL * (A * ss + IL2 * cc);
            double AIL2 = A - IL2;

            _K[i * 3, i * 3] += ACCIL2SS;
            _K[j * 3, j * 3] += ACCIL2SS;

            _K[i * 3, j * 3] += -1 * ACCIL2SS;
            _K[j * 3, i * 3] += -1 * ACCIL2SS;

            _K[i * 3 + 1,i * 3+1] += ASSIL2CC;
            _K[j * 3+1, j * 3+1] += ASSIL2CC;

            _K[i * 3 + 1, j * 3 + 1] += -1 * ASSIL2CC;
            _K[j * 3 + 1, i * 3 + 1] += -1 * ASSIL2CC;

            _K[i * 3, i * 3 + 1] += EL * AIL2 * cs;
            _K[i * 3 + 1, i * 3] += EL * AIL2 * cs;

            _K[i * 3, i * 3+2] += -1 * EL * IL1 * sin;
            _K[i * 3+2, i * 3] += -1 * EL * IL1 * sin;
            _K[i * 3, j * 3+2] += -1 * EL * IL1 * sin;
            _K[j * 3 + 2, i * 3] += -1 * EL * IL1 * sin;

            _K[i * 3 + 1, i * 3+2] += EL * IL1 * cos;
            _K[i * 3 + 2, i * 3+1] += EL * IL1 * cos;

            _K[i * 3 + 2, j * 3] += EL * IL1 * sin;
            _K[j * 3, i * 3 + 2] += EL * IL1 * sin;

            _K[i * 3 + 2, j * 3 + 1] += -1 * EL * IL1 * cos;
            _K[j * 3 + 1, i * 3 + 2] += -1 * EL * IL1 * cos;

            _K[i * 3 + 2, i * 3 + 2] += 4 * EL * I;
            _K[j * 3 + 2, j * 3 + 2] += 4 * EL * I;

            _K[i * 3 + 2, j * 3 + 2] += 2 * EL * I;
            _K[j * 3 + 2, i * 3 + 2] += 2 * EL * I;

            _K[i * 3, j * 3 + 1] += -1 * EL * AIL2 * cs;
            _K[j * 3 + 1, i * 3] += -1 * EL * AIL2 * cs;

            _K[j * 3 , j * 3 + 1] += EL * AIL2 * cs;
            _K[j * 3 + 1, j * 3] += EL * AIL2 * cs;

            _K[i * 3 + 1, j * 3 + 2] += EL * IL1 * cos;
            _K[j * 3 + 2, i * 3 + 1] += EL * IL1 * cos;

            _K[j * 3, j * 3 + 2] += EL * IL1 * sin;
            _K[j * 3 + 2, j * 3] += EL * IL1 * sin;

            _K[j * 3 + 1, j * 3 + 2] += -1 * EL * IL1 * cos;
            _K[j * 3 + 2, j * 3 + 1] += -1 * EL * IL1 * cos;

            _K[i * 3 + 1, j * 3] += -1 * EL * AIL2 * cs;
            _K[j * 3, i * 3+1] += -1 * EL * AIL2 * cs;  
        }

        #region 受力模型
        /// <summary>
        /// 获取受力
        /// </summary>
        /// <returns></returns>
        private Force[] GetForce()
        {
            switch (AlgSnakes.ForceType)
            {
                case ForceType.Vetex:
                    return this.ComForce_V();
                    break;
                case ForceType.Line:
                    return this.ComForce_L();
                    break;
                case ForceType.Combine:
                    return this.ComForce_Combine();
                    break;
                case ForceType.Max:
                    return this.ComForce_Max();
                    break;
                default:
                    return this.ComForce_Max();
            }
        }
        /// <summary>
        /// z判断是否还有冲突
        /// </summary>
        /// <param name="forceList"></param>
        /// <returns></returns>
        private bool IsHasForce(Force[] forceList)
        {
            foreach (Force curF in forceList)
            {
                if (curF.F != 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 计算顶点的受力-线受力模型
        /// </summary>
        /// <returns>返回受力数组</returns>
        private Force[] ComForce_L()
        {
            //用于存受力的数组
            Force[] forceList = new Force[this._NetWork.PointList.Count];
            for (int i = 0; i < this._NetWork.PointList.Count; i++)
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
            foreach (Road curRoad in this._NetWork.RoadList)
            {
                bDis1 = (0.5 * curRoad.RoadGrade.SylWidth + 0.02) * AlgSnakes.scale * 0.001;
                pline1 = curRoad.EsriPolyline;
                pTopop1 = pline1 as ITopologicalOperator;
                pgon1 = pTopop1.Buffer(bDis1) as IPolygon;
                foreach (Road curRoad2 in this._NetWork.RoadList)
                {
                    if (curRoad2.RID != curRoad.RID)//不是同一条道路
                    {
                        bDis2 = (0.5 * curRoad2.RoadGrade.SylWidth) * AlgSnakes.scale * 0.001; ;
                        pline2 = curRoad.EsriPolyline;
                        pTopop2 = pline1 as ITopologicalOperator;
                        pgon2 = pTopop2.Buffer(bDis2) as IPolygon;
                        bool iscosses = CheckGeometryCrosses(pgon1, pgon2);
                        // if (iscosses)
                        {
                            Dmin = (0.5 * (curRoad.RoadGrade.SylWidth + curRoad2.RoadGrade.SylWidth) + 0.02) * AlgSnakes.scale * 0.001; ;
                            int n = curRoad.PointList.Count;
                            double l = 0.0;
                            for (int i = 0; i < n - 1; i++)
                            {
                                Line = new LineClass();
                                Line.PutCoords(curRoad.GetCoord(i).EsriPoint, curRoad.GetCoord(i + 1).EsriPoint);
                                l = this.LineLength(curRoad.GetCoord(i), curRoad.GetCoord(i + 1));
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

                                        l1 = this.LineLength(curRoad.GetCoord(i).EsriPoint, nearPoint);
                                        l2 = this.LineLength(curRoad.GetCoord(i + 1).EsriPoint, nearPoint);
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
                foreach (RoadLyrInfo curlyr in this._NetWork.RoadLyrInfoList)
                {
                    if (curlyr.RoadGrade.Grade == 999)
                    {
                        bDis2 = (0.5 * curlyr.RoadGrade.SylWidth) * AlgSnakes.scale * 0.001; ;
                        int s = curlyr.GeoSet.GeometryCount;
                        for (int k = 0; k < s; k++)
                        {
                            pline2 = curlyr.GeoSet.get_Geometry(k) as Polyline;
                            pTopop2 = pline1 as ITopologicalOperator;
                            pgon2 = pTopop2.Buffer(bDis2) as IPolygon;
                            bool iscosses = CheckGeometryCrosses(pgon1, pgon2);
                            // if (iscosses)
                            {
                                Dmin = (0.5 * (curRoad.RoadGrade.SylWidth + curlyr.RoadGrade.SylWidth) + 0.02) * AlgSnakes.scale * 0.001; ;
                                int n = curRoad.PointList.Count;
                                double l = 0.0;
                                for (int i = 0; i < n - 1; i++)
                                {
                                    Line = new LineClass();
                                    Line.PutCoords(curRoad.GetCoord(i).EsriPoint, curRoad.GetCoord(i + 1).EsriPoint);
                                    l = this.LineLength(curRoad.GetCoord(i), curRoad.GetCoord(i + 1));
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

                                            l1 = this.LineLength(curRoad.GetCoord(i).EsriPoint, nearPoint);
                                            l2 = this.LineLength(curRoad.GetCoord(i + 1).EsriPoint, nearPoint);
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
        private Force[] ComForce_V()
        {
            //所有点的数组
            List<PointCoord> pointList = _NetWork.PointList;
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
                foreach (RoadLyrInfo curLyrinfo in this._NetWork.RoadLyrInfoList)
                {
                    sylWidthL = curLyrinfo.RoadGrade.SylWidth;
                    Dmin = (0.5 * (sylWidthL + sylWidthP) + AlgSnakes.minDis) * 0.001 * AlgSnakes.scale;
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
        private Force[] ComForce_Combine()
        {
            Force[] forceV = this.ComForce_V();
            Force[] forceL = this.ComForce_L();

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
        private Force[] ComForce_Max()
        {
            Force[] forceV = this.ComForce_V();
            Force[] forceL = this.ComForce_L();

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
        /// 检测几何图形A是否与几何图形B相交
        /// </summary>
        /// <param name="pGeometryA">几何图形A</param>
        /// <param name="pGeometryB">几何图形B</param>
        /// <returns>True为相交，False为不相交</returns>
        private bool CheckGeometryCrosses(IGeometry pGeometryA, IGeometry pGeometryB)
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
        private double LineLength(PointCoord point1, PointCoord point2)
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
        private double LineLength(IPoint point1, IPoint point2)
        {
            double dx = point2.X - point1.X;
            double dy = point2.Y - point1.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            return len;
        }

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
        #endregion

        /// <summary>
        /// 建立受力向量
        /// </summary>
        private bool MakeForceVector()
        {
            int n = this._NetWork.PointList.Count;
            _F = new Matrix(3 * n, 1);

            double L = 0.0;
            double sin = 0.0;
            double cos = 0.0;

            forceList = GetForce();//求受力

            if (!IsHasForce(forceList))
            {
                return false;
            }

            //WriteForce(@"E:\map\实验数据\network", "F.txt", forceList);
            PointCoord fromPoint = null;
            PointCoord nextPoint = null;
            int index0 = -1;
            int index1 = -1;

            foreach (Road curRoad in this._NetWork.RoadList)
            {

                int m = curRoad.PointList.Count;
                for (int i = 0; i < m - 1; i++)
                {
                    fromPoint = curRoad.GetCoord(i);
                    nextPoint = curRoad.GetCoord(i + 1);
                    index0 = curRoad.PointList[i];
                    index1 = curRoad.PointList[i + 1];

                    L = LineLength(fromPoint, nextPoint);
                    sin = (nextPoint.Y - fromPoint.Y) / L;
                    cos = (nextPoint.X - fromPoint.X) / L;

                    _F[3 * index0, 0] += forceList[index0].Fx;
                    _F[3 * index0 + 1, 0] += forceList[index0].Fy;
                    _F[3 * index0 + 2, 0] += -1.0 * L * (forceList[index0].Fx * sin + forceList[index0].Fy * cos);
                    _F[3 * index1, 0] += forceList[index1].Fx;
                    _F[3 * index1 + 1, 0] += forceList[index1].Fy;
                    _F[3 * index1 + 2, 0] += L * (forceList[index1].Fx * sin + forceList[index1].Fy * cos);
                }
            }
            return true;
        }

        #region 迭代求解
        /// <summary>
        /// 计算移位向量
        /// </summary>
        public void DoDispaceIterate()
        {
            ComMatrix_K();       //求刚度矩阵

            WriteMatrix(@"E:\map\实验数据\network", this._K, @"Ek.txt");

            if (!MakeForceVector())   //建立并计算力向量
            {
                return;
            }
            #region 求1+rK
            int n = this._K.Col;
            this._d = new Matrix(n, 1);
            Matrix identityMatrix = new Matrix(n, n);
            for (int i = 0; i < n; i++)
            {
                this._F[i, 0] *= r;
                this._d[i, 0] = 0.0;
                for (int j = 0; j < n; j++)
                {
                    this._K[i, j] *= r;
                    if (i == j)
                        identityMatrix[i, j] = 1.0;
                    else
                        identityMatrix[i, j] = 0;
                }
            }
            this._K = identityMatrix + this._K;
            #endregion
            this.SetBoundPointParams();

            for (int i = 0; i < time; i++)
            {
                this._F = this._d + r * this._F;

                this.SetBoundPointParamsForce();
                ComDisplace();
                this.UpdataCoords();
                CreategeoSetFromRes();
                if (!MakeForceVector())   //建立并计算力向量
                {
                    return;
                }
            }
          //  StaticDis();
        }

        /// <summary>
        /// 更新坐标位置
        /// </summary>
        private void UpdataCoords()
        {
            foreach (PointCoord curPoint in this._NetWork.PointList)
            {
                int index = curPoint.ID;
                curPoint.X += this._d[3 * index, 0];
                curPoint.Y += this._d[3 * index+1, 0];
            }
        }

      

        /// <summary>
        /// 计算移位向量
        /// </summary>
        private void ComDisplace()
        {

            this._d = this._K.Inverse() * this._F;
        }
        /// <summary>
        /// 设置边界条件
        /// </summary>
        private void SetBoundPointParams()
        {
            int r1, r2, r3;
            int index = 0;
            foreach (ConnNode curNode in _NetWork.ConnNodeList)
            {
                if (curNode.ConRoadList.Count == 1/* && curNode.PointID != 135 && curNode.PointID != 154*/)
                {
                    index = curNode.PointID;

                    r1 = index *3;
                    r2 = index * 3 + 1;
                    r3 = index * 3 + 2;

                    this._F[r1, 0] = 0;
                    this._F[r2, 0] = 0;
                    this._F[r3, 0] = 0;

                    for (int i = 0; i < _K.Col; i++)
                    {
                        if (i == r1)
                        {
                            _K[r1, i] = 1;//对角线上元素赋值为1
                        }
                        else
                        {
                            _K[r1, i] = 0;//其他地方赋值为0
                        }

                        if (i == r2)
                        {
                            _K[r2, i] = 1;//对角线上元素赋值为1
                        }
                        else
                        {
                            _K[r2, i] = 0;//其他地方赋值为0
                        }

                        if (i== r3)
                        {
                            _K[r3, i] = 1;//对角线上元素赋值为1
                        }
                        else
                        {
                            _K[r3, i] = 0;//其他地方赋值为0
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 设置边界条件
        /// </summary>
        private void SetBoundPointParamsForce()
        {
            int r1, r2, r3;
            int index = 0;
            foreach (ConnNode curNode in _NetWork.ConnNodeList)
            {
                if (curNode.ConRoadList.Count == 1)
                {
                    index = curNode.PointID;

                    r1 = index * 3;
                    r2 = index * 3 + 1;
                    r3 = index * 3 + 2;

                    this._F[r1, 0] = 0;
                    this._F[r2, 0] = 0;
                    this._F[r3, 0] = 0;
                }

            }
        }
        #endregion


        /// <summary>
        /// 清空道路图层的所有图形对象
        /// </summary>
        private void ClearRoadLyrGeoSet()
        {
            foreach (RoadLyrInfo curLyrInfo in this._NetWork.RoadLyrInfoList)
            {
                if (curLyrInfo.RoadGrade.Grade != 999)
                {
                    int count = curLyrInfo.GeoSet.GeometryCount;
                    curLyrInfo.GeoSet.RemoveGeometries(0, count);
                }
            }
        }
        /// <summary>
        /// 移位后的点中重新生成集合对象几何（用于计算受力的）
        /// </summary>
        private void CreategeoSetFromRes()
        {
            ClearRoadLyrGeoSet();// 清空道路图层
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;
            string lyrName = "";
            RoadLyrInfo curLyrInfo = null;
            foreach (Road curRoad in this._NetWork.RoadList)
            {
                lyrName = curRoad.RoadGrade.LyrName;
                RoadLyrInfo lyrInfo = GetLyrinfo(lyrName);
                IGeometry shp = new PolylineClass();
                IPointCollection pointSet = shp as IPointCollection;
                IPoint curResultPoint = null;
                PointCoord curPoint = null;
                int h = curRoad.PointList.Count;
                for (int k = 0; k < h; k++)
                {
                    curPoint = curRoad.GetCoord(k);
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                }
                if (lyrInfo != null)
                {
                    lyrInfo.GeoSet.AddGeometry(shp, ref missing1, ref missing2);
                }
            }
        }

        /// <summary>
        /// 根据图层名称返回
        /// </summary>
        /// <param name="lyrName">图层名称</param>
        /// <returns>图层信息对象</returns>
        private RoadLyrInfo GetLyrinfo(string lyrName)
        {
            foreach (RoadLyrInfo curLyrInfo in this._NetWork.RoadLyrInfoList)
            {
                if (curLyrInfo.RoadGrade.LyrName == lyrName)
                {
                    return curLyrInfo;
                }
            }
            return null;
        }

        /// <summary>
        /// 输出刚度矩阵
        /// </summary>
        /// <param name="filepath">文件夹名</param>
        /// <param name="M">矩阵</param>
        /// <param name="fileName">文件名</param>
        public void WriteMatrix(string filepath, Matrix M, string fileName)
        {

            StreamWriter streamw = File.CreateText(filepath + "\\" + fileName);
            streamw.Write(M.ToString());
            streamw.Close();
        }

        /// <summary>
        /// 执行移位操作
        /// </summary>
        public void DoDispace()
        {
            ComMatrix_K();       //求刚度矩阵
           // this.WriteMatrix(@"E:\map\实验数据\network", this._K, "K.txt");
            if (!MakeForceVector())   //建立并计算力向量
            {
                return;
            }

            //this.WriteMatrix(@"E:\map\实验数据\network", this._F, "F.txt");
            //this.WriteMatrix(@"E:\map\实验数据\network", this._Fy, "Fy.txt");

            SetBoundPointParams();//设置边界条件
            //SetBoundPointParamsInteractive(); //人工设置边界点
            ComDisplace();       //求移位量
            UpdataCoords();      //更新坐标
           // this.WriteMatrix(@"E:\map\实验数据\network", this._d, "D.txt");
            //StaticDis();
        }
    }
}
