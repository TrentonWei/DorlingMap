using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatrixOperation;
using ESRI.ArcGIS.Geometry;
using AuxStructureLib;

namespace RoadDisAlg
{
    /// <summary>
    /// 材料力学方法
    /// 源自Højholt的EFM算法
    /// </summary>
    public class AlgElasticTriangulation
    {
        public RoadNetWork _NetWork = null;                            //道路网络对象
        private List<Triangular> _TriList = null;                            //道路网络对象
        /*材料在弹性变形阶段内，正应力和对应的正应变的比值。*/
        public static float E =0.8F;            //弹性模量
        /*在固体力学中，材料的横向变形系数，
         * 即泊松比，又称泊松系数。材料的横
         * 向应变与纵向应变之比就是泊松比。
         * 泊松比（英语：Poisson's ratio），又译蒲松比，
         * 是材料力学和弹性力学中的名词，
         * 定义为材料受拉伸或压缩力时，材料会发生变形，
         * 而其横向变形量与纵向变形量的比值，
         * 是一无量纲(无因次)的物理量。
         * 当材料在一个方向被压缩，
         * 它会在与该方向垂直的另外两个方向伸长，
         * 这就是泊松现象，
         * 泊松比是用来反映柏松现象的无量纲的物理量。*/
        public static float ν= 0.2F;                  //泊松系数
        public static float λ= (1-ν)/2.0F;          //系数

        Force[] forceList = null;                                            //每个点上的受力
        private Matrix _K = null;                                            //刚度矩阵
        private Matrix _F = null;                                            //受力向量                            
        private Matrix _d = null;                                            //移位结果
       
        /// 构造函数
        /// </summary>
        /// <param name="netWork">待移位的道路网对象</param>
        public AlgElasticTriangulation(RoadNetWork netWork, List<Triangular> trilist)
        {
            _NetWork = netWork;
            _TriList = trilist;
            int n = netWork.PointList.Count;
            _K = new Matrix(2 * n, 2 * n);
        }

        /// 构造函数
        /// </summary>
        /// <param name="netWork">待移位的道路网对象</param>
        public AlgElasticTriangulation(RoadNetWork netWork)
        {
            _NetWork = netWork;
            _TriList = null;
            int n = netWork.PointList.Count;
            _K = new Matrix(2 * n, 2 * n);
        }

        public void CreateTrigulation()
        {
            List<PointCoord> pointList = _NetWork.PointList;
            List<TriNode> triNodeList = new List<TriNode>();

            foreach (PointCoord p in pointList)
            {
                TriNode node = new TriNode((float)p.X, (float)p.Y, p.ID,p.ID);
                triNodeList.Add(node);
            }
            DelaunayTin dt = new DelaunayTin(triNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);

            List<Triangle> triList = dt.TriangleList;
            _TriList = new List<Triangular>();
            foreach (Triangle tri in triList)
            {
                Triangular t = new Triangular(tri.point1.ID, tri.point2.ID, tri.point3.ID);
                _TriList.Add(t);
            }
        }
        /// <summary>
        /// 计算三角形面积
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <returns></returns>
        private double ComTriArea(PointCoord p1, PointCoord p2, PointCoord p3)
        {
            double a = Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
            double b = Math.Sqrt((p2.X - p3.X) * (p2.X - p3.X) + (p2.Y - p3.Y) * (p2.Y - p3.Y));
            double c = Math.Sqrt((p3.X - p1.X) * (p3.X - p1.X) + (p3.Y - p1.Y) * (p3.Y - p1.Y));

            double s = (a + b + c) / 2;

            return Math.Sqrt(s * (s - a) * (s - b) * (s - c));
        }
        /// <summary>
        /// 计算线段单元的刚度矩阵
        /// </summary>
        /// <param name="line">线段对象</param>
        /// <returns>返回刚度矩阵4*4</returns>
        private void CalcuTriangularMatrix(PointCoord p1, PointCoord p2,PointCoord p3)
        {
            double b1 = p2.Y - p3.Y;
            double b2 = p3.Y - p1.Y;
            double b3 = p1.Y - p2.Y;

            double c1 = p3.X - p2.X;
            double c2 = p1.X - p2.X;
            double c3 = p2.X - p1.X;
            
            //下标
            int i = p1.ID;
            int j = p2.ID;
            int k = p3.ID;
            /*
             * 知道三角形的三个顶点的坐标，可以求出三角形的三条边，
             * 进而可以求p（p=(a+b+c)/2） 
             * 则S=√[p(p-a)(p-b)(p-c)] 
             * 多算少动脑的方法
             * */

            double area = this.ComTriArea(p1, p2, p3);

            double K = E / (4 * area * (1 - ν * ν));
            //对角线元素
            _K[2 * i, 2 * i] += K*(b1 * b1 + λ * c1 * c1);
            _K[2 * i + 1, 2 * i + 1] += K*(c1 * c1 + λ * b1 * b1);


            _K[2 * j, 2 * j] += K*(b2 * b2 + λ * c2 * c2);
            _K[2 * j + 1, 2 * j + 1] += K * (c2 * c2 + λ * b2 * b2);

            _K[2 * k, 2 * k] += K*(b3 * b3 + λ * c3 * c3);
            _K[2 * k + 1, 2 * k + 1] += K*(c3 * c3 + λ * b3 * b3);
            //其他元素
            _K[2 * i, 2 * i + 1] +=K*( (ν + λ) * b1 * c1);
            _K[2 * i + 1, 2 * i] += K*((ν + λ) * b1 * c1);

            _K[2 * i, 2 * j] += K*(b1 * b2 + λ * c1 * c2);
            _K[2 * j, 2 * i] +=K*( b1 * b2 + λ * c1 * c2);

            _K[2 * i, 2 * j + 1] += K*(ν * b1 * c2 + λ * b2 * c1);
            _K[2 * j + 1, 2 * i] += K*(ν * b1 * c2 + λ * b2 * c1);

            _K[2 * i, 2 * k] += K*(b1 * b3 + λ * c1 * c3);
            _K[2 * k, 2 * i] += K*(b1 * b3 + λ * c1 * c3);

            _K[2 * i, 2 * k + 1] += K*(ν * b1 * c3 + λ * b3 * c1);
            _K[2 * k + 1, 2 * i] += K*(ν * b1 * c3 + λ * b3 * c1);

            _K[2 * i + 1, 2 * j] += K*(ν * b2 * c1 + λ * b1 * c2);
            _K[2 * j, 2 * i + 1] += K*(ν * b2 * c1 + λ * b1 * c2);

            _K[2 * i + 1, 2 * j + 1] += K*(c1 * c2 + λ * b1 * b2);
            _K[2 * j + 1, 2 * i + 1] += K*(c1 * c2 + λ * b1 * b2);

            _K[2 * i + 1, 2 * k] += K*(ν * b3 * c1 + λ * b1 * c3);
            _K[2 * k, 2 * i + 1] += K*(ν * b3 * c1 + λ * b1 * c3);

            _K[2 * i + 1, 2 * k + 1] +=K*( c1 * c3 + λ * b1 * b3);
            _K[2 * k + 1, 2 * i + 1] += K*(c1 * c3 + λ * b1 * b3);

            _K[2 * j, 2 * j + 1] += K*((ν + λ) * b2 * c2);
            _K[2 * j + 1, 2 * j] += K*((ν + λ) * b2 * c2);

            _K[2 * j, 2 * k] += K*(b2 * b3 + λ * c2 * c3);
            _K[2 * k, 2 * j] += K*(b2 * b3 + λ * c2 * c3);

            _K[2 * j, 2 * k + 1] += K*(ν * b2 * c3 + λ * b3 * c2);
            _K[2 * k + 1, 2 * j] += K*(ν * b2 * c3 + λ * b3 * c2);

            _K[2 * j + 1, 2 * k] += K*(ν * b3 * c2 + λ * b2 * c3);
            _K[2 * k, 2 * j + 1] += K*(ν * b3 * c2 + λ * b2 * c3);

            _K[2 * j + 1, 2 * k + 1] += K*(c2 * c3 + λ * b2 * b3);
            _K[2 * k + 1, 2 * j + 1] += K*(c2 * c3 + λ * b2 * b3);

            _K[2 * k, 2 * k + 1] += K*((ν + λ) * b3 * c3);
            _K[2 * k + 1, 2 * k] += K*((ν + λ) * b3 * c3);
        }

        /// <summary>
        /// 计算线段单元的刚度矩阵
        /// </summary>
        /// <param name="line">线段对象</param>
        /// <returns>返回刚度矩阵4*4</returns>
        private void CalcuTriangularMatrix(Triangular tri)
        {
            PointCoord p1 = this._NetWork.PointList[tri.point1];
            PointCoord p2 = this._NetWork.PointList[tri.point2];
            PointCoord p3 = this._NetWork.PointList[tri.point3];
            //计算刚度矩阵
            CalcuTriangularMatrix(p1, p2, p3);
        }

        /// <summary>
        /// 计算刚度矩阵
        /// </summary>
        private void ComMatrix_K()
        {
            foreach (Triangular tri in this._TriList)
            {
                this.CalcuTriangularMatrix(tri);
            }
        }

        /// <summary>
        /// 设置边界条件
        /// </summary>
        private void SetBoundPointParams()
        {
            int r1, r2;
            int index = 0;
            foreach (ConnNode curNode in _NetWork.ConnNodeList)
            {
                if (curNode.ConRoadList.Count == 1/* && curNode.PointID != 135 && curNode.PointID != 154*/)
                {
                    index = curNode.PointID;

                    r1 = index * 2;
                    r2 = index * 2 + 1;

                    this._F[r1, 0] = 0;
                    this._F[r2, 0] = 0;

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
                    }
                }
            }
        }

        /// <summary>
        /// 执行移位操作
        /// </summary>
        public void DoDispace()
        {
            //创建三角网
            CreateTrigulation();

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


        /// <summary>
        /// 更新坐标位置
        /// </summary>
        private void UpdataCoords()
        {
            foreach (PointCoord curPoint in this._NetWork.PointList)
            {
                int index = curPoint.ID;
                curPoint.X += this._d[2 * index, 0];
                curPoint.Y += this._d[2 * index + 1, 0];
            }
        }

        /// <summary>
        /// 计算移位向量
        /// </summary>
        private void ComDisplace()
        {

            this._d = this._K.Inverse() * this._F;
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
            _F = new Matrix(2 * n, 1);

          
            forceList = GetForce();//求受力

            if (!IsHasForce(forceList))
            {
                return false;
            }

            for(int i=0;i<n;i++)
            {
                _F[2 * i,0] = forceList[i].Fx;
                _F[2 * i+1, 0] = forceList[i].Fy;
            }
            return true;
        }
    }
}
