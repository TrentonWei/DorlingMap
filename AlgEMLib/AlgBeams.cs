using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatrixOperation;
using AuxStructureLib;
using ESRI.ArcGIS.Geometry;
using System.Data;
using AuxStructureLib.IO;
using AuxStructureLib.ConflictLib;

namespace AlgEMLib
{
    public class AlgBeams : AlgEM
    {
        #region 参数
        //public double PAT = 0.6;
        public double E = 10000;
        public double I = 2;
        public double A = 2;
        public string strPath = "";
        BeamsStiffMatrix bM = null;
        BeamsForceVector fV = null;
        #endregion

        #region 邻近图移位
        /// <summary>
        /// 构造函数-从建筑物群邻近图构建Beams模型
        /// </summary>
        /// <param name="proxiGraph">邻近图</param>
        /// <param name="map">地图对象</param>
        /// <param name="e">弹性模量</param>
        /// <param name="i">惯性力矩</param>
        /// <param name="a">横截面积</param>
        /// <param name="disThresholdPP">邻近冲突距离阈值</param>
        public AlgBeams(ProxiGraph proxiGraph, SMap map, double e, double i, double a, double disThreshold)
        {
            this.E = e;
            this.I = i;
            this.A = a;
            this.DisThreshold = disThreshold;
            this.ProxiGraph = proxiGraph;
            this.Map = map;
        }

        /// <summary>
        /// 构造函数-从建筑物群邻近图构建Beams模型
        /// </summary>
        /// <param name="proxiGraph">邻近图</param>
        /// <param name="map">地图对象</param>
        /// <param name="e">弹性模量</param>
        /// <param name="i">惯性力矩</param>
        /// <param name="a">横截面积</param>
        /// <param name="disThresholdPP">邻近冲突距离阈值</param>
        public AlgBeams(ProxiGraph proxiGraph, SMap map, double e, double i, double a)
        {
            this.E = e;
            this.I = i;
            this.A = a;
            this.ProxiGraph = proxiGraph;
            this.Map = map;
        }

        /// <summary>
        /// 构造函数-Stable DorlingBeams
        /// </summary>
        /// <param name="proxiGraph">邻近图</param>
        /// <param name="map">地图对象</param>
        /// <param name="e">弹性模量</param>
        /// <param name="i">惯性力矩</param>
        /// <param name="a">横截面积</param>
        /// <param name="disThresholdPP">邻近冲突距离阈值</param>
        public AlgBeams(List<ProxiGraph> PgList, List<SMap> maps, double e, double i, double a)
        {
            this.E = e;
            this.I = i;
            this.A = a;
            this.PgList = PgList;
            this.MapLists = maps;
            this.ProxiGraph = PgList[0];
        }

        /// <summary>
        /// 构造函数-CTP
        /// </summary>
        /// <param name="proxiGraph">邻近图</param>
        /// <param name="map">地图对象</param>
        /// <param name="e">弹性模量</param>
        /// <param name="i">惯性力矩</param>
        /// <param name="a">横截面积</param>
        /// <param name="disThresholdPP">邻近冲突距离阈值</param>
        public AlgBeams(ProxiGraph Pg, double e, double i, double a)
        {
            this.E = e;
            this.I = i;
            this.A = a;
            this.ProxiGraph = Pg;
        }

        /// <summary>
        /// 构造函数-从建筑物群邻近图构建Beams模型
        /// </summary>
        /// <param name="proxiGraph">邻近图</param>
        /// <param name="map">地图对象</param>
        /// <param name="e">弹性模量</param>
        /// <param name="i">惯性力矩</param>
        /// <param name="a">横截面积</param>
        /// <param name="disThresholdPP">邻近冲突距离阈值</param>
        public AlgBeams(ProxiGraph proxiGraph, List<SMap> maps, double e, double i, double a)
        {
            this.E = e;
            this.I = i;
            this.A = a;
            this.ProxiGraph = proxiGraph;
            this.MapLists = maps;
        }


        /// <summary>
        /// 构造函数-从建筑物群邻近图构建Beams模型
        /// </summary>
        /// <param name="proxiGraph">邻近图</param>
        /// <param name="map">地图对象</param>
        /// <param name="e">弹性模量</param>
        /// <param name="i">惯性力矩</param>
        /// <param name="a">横截面积</param>
        /// <param name="disThresholdLP">线-面邻近冲突距离阈值</param>
        /// <param name="disThresholdPP">面-面邻近冲突距离阈值</param>
        public AlgBeams(ProxiGraph proxiGraph, SMap map, double e, double i, double a,
            double disThresholdLP, double disThresholdPP)
        {
            this.E = e;
            this.I = i;
            this.A = a;
            this.DisThresholdLP = disThresholdLP;
            this.DisThresholdPP = disThresholdPP;
            this.ProxiGraph = proxiGraph;
            this.Map = map;
        }

        /// <summary>
        /// 邻近图的移位算法实现
        /// </summary>
        public void DoDispacePG()
        {
            bM = new BeamsStiffMatrix(this.ProxiGraph, E, I, A);
            this.K = bM.Matrix_K;
            fV = new BeamsForceVector(this.ProxiGraph);
            fV.isDragForce = this.isDragF;
            fV.CreateForceVectorfrmGraph(this.DisThresholdLP, this.DisThresholdPP);
            fV.Create_WriteForceVector2Shp(strPath, @"ForceVector", esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
            this.F = fV.Vector_F;

            SetBoundPointParams();//设置边界条件

            this.D = this.K.Inverse() * this.F;

            double MaxD;
            double MaxF;
            int indexMaxD = -1;
            int indexMaxF = -1;

            StaticDis(out MaxD, out MaxF, out indexMaxD, out indexMaxF);

            if (MaxF > 0)
            {
                double k = MaxD / MaxF;
                this.E *= k;

                bM = new BeamsStiffMatrix(this.ProxiGraph, E, I, A);
                this.K = bM.Matrix_K;
                SetBoundPointParams();//设置边界条件
                this.D = this.K.Inverse() * this.F;
            }
            else
            {
                this.isContinue = false;
                return;
            }
            UpdataCoordsforPG();      //更新坐标

            if (MaxF <= DisThresholdPP * 0.01)
            {
                this.isContinue = false;
            }
        }

        /// <summary>
        /// 统计移位值
        /// </summary>
        private void StaticDis(out double MaxD, out double MaxF, out int indexMaxD, out int indexMaxF)
        {
            int n = this.fV.ForceList.Count;
            MaxD = -1;
            MaxF = -1;
            double curD;
            indexMaxD = -1;
            indexMaxF = -1;

            for (int i = 0; i < n; i++)
            {
                // curD = Math.Sqrt(this.D[3 * i, 0] * this.D[3 * i, 0] + this.D[3 * i + 1, 0] * this.D[3 * i + 1, 0]);

                // if (curD > MaxD)
                // {
                //     indexMaxD = i;
                //      MaxD = curD;
                //   }


                if (this.fV.ForceList[i].F > MaxF)
                {
                    indexMaxF = i;
                    MaxF = fV.ForceList[i].F;
                }
            }

            MaxD = Math.Sqrt(this.D[3 * indexMaxF, 0] * this.D[3 * indexMaxF, 0] + this.D[3 * indexMaxF + 1, 0] * this.D[3 * indexMaxF + 1, 0]);
        }

        /// <summary>
        /// 统计最大的受力极其对应的移位值
        /// </summary>
        /// <param name="MaxD"></param>
        /// <param name="MaxF"></param>
        /// <param name="indexMaxD"></param>
        /// <param name="indexMaxF"></param>
        private void StaticDisforPGNew(out double MaxD, out double MaxF, out int indexMaxD, out int indexMaxF)
        {
            int n = this.fV.ForceList.Count;
            MaxD = -1;
            MaxF = -1;
            double curD;
            indexMaxD = -1;
            indexMaxF = -1;

            for (int i = 0; i < n; i++)
            {
                // curD = Math.Sqrt(this.D[3 * i, 0] * this.D[3 * i, 0] + this.D[3 * i + 1, 0] * this.D[3 * i + 1, 0]);

                // if (curD > MaxD)
                // {
                //     indexMaxD = i;
                //      MaxD = curD;
                //   }


                if (this.fV.ForceList[i].F > MaxF)
                {
                    indexMaxF = this.fV.ForceList[i].ID;
                    MaxF = fV.ForceList[i].F;
                }
            }

            MaxD = Math.Sqrt(this.D[3 * indexMaxF, 0] * this.D[3 * indexMaxF, 0] + this.D[3 * indexMaxF + 1, 0] * this.D[3 * indexMaxF + 1, 0]);
        }

        /// <summary>
        /// 统计最大的受力极其对应的移位值
        /// </summary>
        /// <param name="MaxD"></param>
        /// <param name="MaxF"></param>
        /// <param name="indexMaxD"></param>
        /// <param name="indexMaxF"></param>
        private void StaticDisforPGNewDF(out double MaxFD, out double MaxD, out double MaxDF, out double MaxF, out int indexMaxD, out int indexMaxF)
        {
            int n = this.fV.ForceList.Count;
            MaxD = -1;
            MaxF = -1;
            MaxFD = -1;
            MaxDF = -1;
            double curD;
            indexMaxD = -1;
            indexMaxF = -1;

            for (int i = 0; i < n; i++)
            {
                curD = Math.Sqrt(this.D[3 * i, 0] * this.D[3 * i, 0] + this.D[3 * i + 1, 0] * this.D[3 * i + 1, 0]);

                if (curD > MaxD)
                {
                    indexMaxD = i;
                    MaxD = curD;
                }


                if (this.fV.ForceList[i].F > MaxF)
                {
                    indexMaxF = this.fV.ForceList[i].ID;
                    MaxF = fV.ForceList[i].F;
                }
            }
            if (indexMaxF != -1)
            {
                MaxFD = Math.Sqrt(this.D[3 * indexMaxF, 0] * this.D[3 * indexMaxF, 0] + this.D[3 * indexMaxF + 1, 0] * this.D[3 * indexMaxF + 1, 0]);
            }
            if (indexMaxD != -1)
            {
                MaxDF = fV.ForceList[indexMaxD].F;
            }
        }

        /// <summary>
        /// 统计最大的受力极其对应的移位值
        /// </summary>
        /// <param name="MaxD"></param>
        /// <param name="MaxF"></param>
        /// <param name="indexMaxD"></param>
        /// <param name="indexMaxF"></param>
        private void StaticDisforPGNewDF_2(out double MaxFD, out double MaxD, out double MaxDF, out double MaxF, out int indexMaxD, out int indexMaxF)
        {
            int n = this.fV.ForceList.Count;
            MaxD = -1;
            MaxF = -1;
            MaxFD = -1;
            MaxDF = -1;
            double curD;
            indexMaxD = -1;
            indexMaxF = -1;

            for (int i = 0; i < n; i++)
            {
                curD = Math.Sqrt(this.Test_D[3 * i, 0] * this.Test_D[3 * i, 0] + this.Test_D[3 * i + 1, 0] * this.Test_D[3 * i + 1, 0]);

                if (curD > MaxD)
                {
                    indexMaxD = i;
                    MaxD = curD;
                }

                if (this.fV.ForceList[i].F > MaxF)
                {
                    indexMaxF = this.fV.ForceList[i].ID;
                    MaxF = fV.ForceList[i].F;
                }
            }
            if (indexMaxF != -1)
            {
                MaxFD = Math.Sqrt(this.Test_D[3 * indexMaxF, 0] * this.Test_D[3 * indexMaxF, 0] + this.Test_D[3 * indexMaxF + 1, 0] * this.Test_D[3 * indexMaxF + 1, 0]);
            }
            if (indexMaxD != -1)
            {
                MaxDF = fV.ForceList[indexMaxD].F;
            }
        }


        /// <summary>
        /// 更新坐标位置
        /// </summary>
        private void UpdataCoordsforLine()
        {

            for (int i = 0; i < Polyline.PointList.Count; i++)
            {
                Node curPoint = Polyline.PointList[i];
                int index = curPoint.ID;
                curPoint.X += this.D[3 * index, 0];
                curPoint.Y += this.D[3 * index + 1, 0];
            }

        }
        #endregion

        #region 单条线的移位

        /// <summary>
        /// 构造函数-从一个线对象构建Beams模型2014-1-18
        /// </summary>
        /// <param name="polyline">线对象</param>
        /// <param name="e">弹性模量</param>
        /// <param name="i">惯性力矩</param>
        /// <param name="a">横截面积</param>
        /// <param name="forceFile">受力文件</param>
        public AlgBeams(PolylineObject polyline, double e, double i, double a, string forceFile)
        {
            this.Polyline = polyline;
            this.E = e;
            this.I = i;
            this.A = a;
            this.strPath = forceFile;
            //重新编号
            int n = Polyline.PointList.Count;
            for (int j = 0; j < n; j++)
            {
                Polyline.PointList[j].ID = j;
            }
        }

        /// <summary>
        /// 根据边界条件移位
        /// </summary>
        public void DoDisplaceLineBoundCon()
        {
            bM = new BeamsStiffMatrix(Polyline, E, I, A);
            fV = new BeamsForceVector(Polyline);

            this.K = bM.Matrix_K;
            this.F = fV.Vector_F;


            SetBoundaryPointListfrmFile(strPath);
            //SetBoundPointParamsInteractive(); //人工设置边界点
            this.D = this.K.Inverse() * this.F;

            this.UpdataCoordsforLine();      //更新坐标
        }

        /// <summary>
        /// 不迭代
        /// </summary>
        public void DoLineDispace()
        {
            this.bM = new BeamsStiffMatrix(Polyline, E, I, A);
            this.fV = new BeamsForceVector(Polyline, strPath);

            this.K = bM.Matrix_K;
            this.F = fV.Vector_F;

            SetBoundPointParamsforLine();//设置边界条件

            this.D = this.K.Inverse() * this.F;

            double MaxD;
            double MaxF;
            int indexMaxD = -1;
            int indexMaxF = -1;

            StaticDisforLine(out MaxD, out MaxF, out indexMaxD, out indexMaxF);

            if (MaxF > 0)
            {
                double k = MaxD / MaxF;
                this.E *= k;

                bM = new BeamsStiffMatrix(Polyline, E, I, A);
                this.K = bM.Matrix_K;

                SetBoundPointParamsforLine();//设置边界条件

                this.D = this.K.Inverse() * this.F;
            }

            this.UpdataCoordsforLine();      //更新坐标

            if (MaxF <= DisThresholdPP * 0.01)
            {
                this.isContinue = false;
            }
        }

        /// <summary>
        /// 统计移位值
        /// </summary>
        private void StaticDisforLine(out double MaxD, out double MaxF, out int indexMaxD, out int indexMaxF)
        {
            int n = fV.ForceList.Count;
            MaxD = -1;
            MaxF = -1;
            double curD;
            indexMaxD = -1;
            indexMaxF = -1;

            for (int i = 0; i < n; i++)
            {
                if (this.fV.ForceList[i].F > MaxF)
                {
                    MaxF = fV.ForceList[i].F;
                    indexMaxF = fV.ForceList[i].ID;
                }
            }
            MaxD = Math.Sqrt(this.D[3 * indexMaxF, 0] * this.D[3 * indexMaxF, 0] + this.D[3 * indexMaxF + 1, 0] * this.D[3 * indexMaxF + 1, 0]);
        }

        /// <summary>
        /// 设置边界--如果两个端点不受力，则将它们设置为边界点
        /// </summary>
        private void SetBoundPointParamsforLine()
        {
            int n = this.Polyline.PointList.Count;
            //如果线的端点出不受力的作用则，将它们设置为不移动的边界点
            if (this.F[0, 0] == 0 && this.F[1, 0] == 0)
                this.SetBoundPointParamsBigNumber(0, 0, 0);
            // SetBoundPointParamsOld(0, 0, 0);
            if (this.F[3 * (n - 1), 0] == 0 && this.F[3 * (n - 1) + 1, 0] == 0)
                this.SetBoundPointParamsBigNumber(n - 1, 0, 0);
            // SetBoundPointParamsOld(n - 1, 0, 0);
        }

        /// <summary>
        /// 设置边界条件
        /// </summary>
        /// <param name="index"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        private void SetBoundPointParamsOld(int index, double dx, double dy)
        {
            int r1, r2, r3;//x,y,a


            r1 = index * 3;
            r2 = index * 3 + 1;
            r3 = index * 3 + 2;

            this.F[r1, 0] = dx;
            this.F[r2, 0] = dy;
            this.F[r3, 0] = 0;

            for (int i = 0; i < K.Col; i++)
            {
                if (i == r1)
                {
                    K[r1, i] = 1;//对角线上元素赋值为1
                }
                else
                {
                    K[r1, i] = 0;//其他地方赋值为0
                }

                if (i == r2)
                {
                    K[r2, i] = 1;//对角线上元素赋值为1
                }
                else
                {
                    K[r2, i] = 0;//其他地方赋值为0
                }

                if (i == r3)
                {
                    K[r3, i] = 1;//对角线上元素赋值为1
                }
                else
                {
                    K[r3, i] = 0;//其他地方赋值为0
                }

            }
        }

        /// <summary>
        /// 读取文件中的边界条件并设置
        /// </summary>
        /// <param name="boundaryfile">边界点文件</param>
        /// <returns></returns>
        private void SetBoundaryPointListfrmFile(string boundaryfile)
        {
            List<Force> forceList = new List<Force>();
            //读文件========
            DataTable dt = TestIO.ReadData(boundaryfile);
            foreach (DataRow curR in dt.Rows)
            {
                int curID = Convert.ToInt32(curR[0]);
                double curDx = Convert.ToDouble(curR[1]);
                double curDy = Convert.ToDouble(curR[2]);
                SetBoundPointParamsBigNumber(curID, curDx, curDy);
            }
        }

        /// <summary>
        /// 置大数法
        /// </summary>
        public void SetBoundPointParamsBigNumber(int index, double dx, double dy)
        {

            int r1 = index * 3;
            int r2 = index * 3 + 1;
            int r3 = index * 3 + 2;
            if (r1 < K.Col)
            {
                this.F[r1, 0] = K[r1, r1] * dx * 100000000;

                this.F[r2, 0] = K[r2, r2] * dy * 100000000;
                this.F[r3, 0] = 0;

                //else if (i == r3)
                //{
                //    //this.F[i, 0] = 0;//不知对不对？？？将力矩项设为0对面
                //}


                K[r1, r1] = 100000000 * K[r1, r1];
                K[r2, r2] = 100000000 * K[r2, r2];
                K[r3, r3] = 10000000000000000 * K[r3, r3];
            }
        }
        #endregion

        #region 邻近图移位-2014-2-28
        /// <summary>
        /// 构造函数-从建筑物群邻近图构建Beams模型
        /// </summary>
        /// <param name="proxiGraph">邻近图</param>
        /// <param name="map">地图对象</param>
        /// <param name="e">弹性模量</param>
        /// <param name="i">惯性力矩</param>
        /// <param name="a">横截面积</param>
        /// <param name="conflictList">邻近冲突列表</param>
        public AlgBeams(ProxiGraph proxiGraph, SMap map, double e, double i, double a, List<ConflictBase> conflictList)
        {
            this.E = e;
            this.I = i;
            this.A = a;
            this.ProxiGraph = proxiGraph;
            this.Map = map;
            this.ConflictList = conflictList;
        }

        /// <summary>
        /// 邻近图的移位算法实现
        /// </summary>
        public void DoDispacePGNew()
        {
            fV = new BeamsForceVector(this.ProxiGraph);
            //求吸引力-2014-3-20所用
            fV.OrigialProxiGraph = this.OriginalGraph;
            fV.RMSE = this.PAT * this.Scale / 1000;
            fV.isDragForce = this.isDragF;
            fV.CreateForceVectorfrmConflict(ConflictList);
            fV.Create_WriteForceVector2Shp(strPath, @"ForceVector", esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
            this.F = fV.Vector_F;

            double MaxD;
            double MaxF;
            double MaxDF;
            double MaxFD;
            int indexMaxD = -1;
            int indexMaxF = -1;

            if ((this.ProxiGraph.PolygonCount <= 3 && this.AlgType == 2) || this.AlgType == 1)
            {
                //基本几何算法
                this.D = new Matrix(this.ProxiGraph.NodeList.Count * 3, 1);
                this.UpdataCoordsforPGbyForce();
                StaticDisforPGNewDF(out MaxFD, out MaxD, out MaxDF, out MaxF, out indexMaxD, out indexMaxF);
                if (MaxF > 0)
                {
                    this.isContinue = true;
                }
                else
                {
                    this.isContinue = false;
                    return;
                }
            }
            else
            {
                //计算刚度矩阵
                bM = new BeamsStiffMatrix(this.ProxiGraph, E, I, A);
                this.K = bM.Matrix_K;

                this.SetBoundPointParamforPG();//设置边界条件

                this.D = this.K.Inverse() * this.F;


                StaticDisforPGNewDF(out MaxFD, out MaxD, out MaxDF, out MaxF, out indexMaxD, out indexMaxF);

                if (MaxF > 0)
                {
                    double k = 1;
                    if (MaxD / MaxFD <= 5)
                    {
                        k = MaxFD / MaxF;
                    }
                    else
                    {
                        k = MaxD / MaxDF;
                    }

                    this.E *= k;
                    //再次计算刚度矩阵
                    bM = new BeamsStiffMatrix(this.ProxiGraph, E, I, A);
                    this.K = bM.Matrix_K;
                    SetBoundPointParamforPG();//设置边界条件
                    this.D = this.K.Inverse() * this.F;
                }
                else
                {
                    this.isContinue = false;
                    return;
                }

                UpdataCoordsforPG();      //更新坐标
            }
            //输出受力点出的受力与当前移位值
            this.OutputDisplacementandForces(fV.ForceList);
            //输出各点的移位总和
            OutputTotalDisplacementforProxmityGraph(this.OriginalGraph, this.ProxiGraph, this.Map);

            if (MaxF <= DisThresholdPP * 0.01)
            {
                this.isContinue = false;
            }
        }

        /// <summary>
        /// 邻近图的移位算法实现-设置边界条件
        /// </summary>
        public void DoDispacePGNew_BC()
        {
            double MaxD;
            double MaxF;
            double MaxDF;
            double MaxFD;
            int indexMaxD = -1;
            int indexMaxF = -1;
            fV = new BeamsForceVector(this.ProxiGraph);
            //求吸引力-2014-3-20所用
            fV.OrigialProxiGraph = this.OriginalGraph;
            fV.RMSE = this.PAT * this.Scale / 1000;
            fV.isDragForce = this.isDragF;
            fV.CreateForceVectorfrmConflict(ConflictList);
            fV.Create_WriteForceVector2Shp(strPath, @"ForceVector", esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
            this.F = fV.Vector_F;

            if ((this.ProxiGraph.PolygonCount <= 3 && this.AlgType == 2) || this.AlgType == 1)
            {
                //基本几何算法
                this.D = new Matrix(this.ProxiGraph.NodeList.Count * 3, 1);
                this.UpdataCoordsforPGbyForce();
                StaticDisforPGNewDF(out MaxFD, out MaxD, out MaxDF, out MaxF, out indexMaxD, out indexMaxF);
                if (MaxF > 0)
                {
                    this.isContinue = true;
                }
                else
                {
                    this.isContinue = false;
                    return;
                }
            }
            else
            {
                bM = new BeamsStiffMatrix(this.ProxiGraph, E, I, A);
                this.K = bM.Matrix_K;


                this.SetBoundPointParamforPG_BC();//设置边界条件

                this.D = this.K.Inverse() * this.F;



                StaticDisforPGNewDF(out MaxFD, out MaxD, out MaxDF, out MaxF, out indexMaxD, out indexMaxF);

                UpdataCoordsforPG();      //更新坐标
                this.OutputDisplacementandForces(fV.ForceList);
            }

            if (MaxF <= DisThresholdPP * 0.01)
            {
                this.isContinue = false;
            }
        }

        /// <summary>
        /// 将Force的属性IsBouldPoint为True的点设置为边界点，
        /// 具体地，邻近图中道路上的顶点设为不动的边界条件
        /// </summary>
        private void SetBoundPointParams()
        {
            int r1, r2, r3;//x,y,a
            int index = 0;
            Force curF = null;
            for (int k = 0; k < fV.ForceList.Count; k++)
            {
                curF = fV.ForceList[k];
                if (curF.IsBouldPoint)
                {

                    r1 = k * 3;
                    r2 = k * 3 + 1;
                    r3 = k * 3 + 2;

                    this.SetBoundPointParamsBigNumber(k, 0, 0);
                }
            }
        }

        /// <summary>
        /// 更新坐标位置
        /// </summary>
        private void UpdataCoordsforPG()
        {
            foreach (ProxiNode curNode in this.ProxiGraph.NodeList)
            {
                int index = curNode.ID;
                int tagID = curNode.TagID;
                FeatureType fType = curNode.FeatureType;
                VoronoiPolygon vp = null;

                if (fType == FeatureType.PolygonType)
                {

                    PolygonObject po = this.Map.GetObjectbyID(tagID, fType) as PolygonObject;

                    double curDx0 = this.D[3 * index, 0];
                    double curDy0 = this.D[3 * index + 1, 0];
                    double curDx = this.D[3 * index, 0];
                    double curDy = this.D[3 * index + 1, 0];

                    if (this.IsTopCos == true)
                    {
                        vp = this.VD.GetVPbyIDandType(tagID, fType);
                        vp.TopologicalConstraint(curDx0, curDy0, 0.001, out curDx, out curDy);
                        curDx = this.D[3 * index, 0] = curDx;
                        curDy = this.D[3 * index + 1, 0] = curDy;
                    }

                    //纠正拓扑错误
                    curNode.X += curDx;
                    curNode.Y += curDy;

                    foreach (TriNode curPoint in po.PointList)
                    {
                        curPoint.X += curDx;
                        curPoint.Y += curDy;
                    }
                }


            }
        }

        /// <summary>
        /// 直接采用受力更新坐标位置
        /// </summary>
        private void UpdataCoordsforPGbyForce()
        {
            foreach (ProxiNode curNode in this.ProxiGraph.NodeList)
            {
                int index = curNode.ID;
                int tagID = curNode.TagID;
                FeatureType fType = curNode.FeatureType;
                VoronoiPolygon vp = null;

                if (fType == FeatureType.PolygonType)
                {

                    PolygonObject po = this.Map.GetObjectbyID(tagID, fType) as PolygonObject;
                    Force force = fV.GetForcebyIndex(index);
                    double curDx0 = 0;
                    double curDy0 = 0;
                    double curDx = 0;
                    double curDy = 0;
                    if (force != null)
                    {

                        curDx0 = this.fV.GetForcebyIndex(index).Fx;
                        curDy0 = this.fV.GetForcebyIndex(index).Fy;
                        curDx = this.fV.GetForcebyIndex(index).Fx;
                        curDy = this.fV.GetForcebyIndex(index).Fy;
                        if (this.IsTopCos == true)
                        {
                            vp = this.VD.GetVPbyIDandType(tagID, fType);
                            vp.TopologicalConstraint(curDx0, curDy0, 0.001, out curDx, out curDy);
                            this.D[3 * index, 0] = curDx;
                            this.D[3 * index + 1, 0] = curDy;
                        }
                        //纠正拓扑错误
                        curNode.X += curDx;
                        curNode.Y += curDy;

                        foreach (TriNode curPoint in po.PointList)
                        {
                            curPoint.X += curDx;
                            curPoint.Y += curDy;
                        }
                    }
                    else
                    {
                        this.D[3 * index, 0] = curDx;
                        this.D[3 * index + 1, 0] = curDy;
                    }
                }
            }
        }
        #endregion

        #region 道路网移位-2014-2-28
        /// <summary>
        /// 构造函数-从一个线对象构建Beams模型2014-1-18
        /// </summary>
        /// <param name="map">地图</param>
        /// <param name="e">弹性模量</param>
        /// <param name="i">惯性力矩</param>
        /// <param name="a">横截面积</param>
        /// <param name="conflictList">冲突列表</param>
        public AlgBeams(SMap map, double e, double i, double a, List<ConflictBase> conflictList)
        {
            this.Map = map;

            this.E = e;
            this.I = i;
            this.A = a;
            this.ConflictList = conflictList;
        }

        /// <summary>
        /// 构造函数-从一个线对象构建Beams模型2014-1-18
        /// </summary>
        /// <param name="map">地图</param>
        /// <param name="e">弹性模量</param>
        /// <param name="i">惯性力矩</param>
        /// <param name="a">横截面积</param>
        /// <param name="conflictList">冲突列表</param>
        public AlgBeams(SMap map, double e, double i, double a, BeamsForceVector fv)
        {
            this.Map = map;

            this.E = e;
            this.I = i;
            this.A = a;
            this.fV = fv;
        }

        /// <summary>
        /// 自适应设置E
        /// </summary>
        public void DoDisplaceAdaptiveforNT()
        {
            bM = new BeamsStiffMatrix(Map.PolylineList, Map.TriNodeList.Count, E, I, A);
            fV = new BeamsForceVector(Map, ConflictList);
            fV.Create_WriteForceVector2Shp(strPath, @"ForceVector", esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
            this.K = bM.Matrix_K;
            this.F = fV.Vector_F;

            SetBoundaryPointListAtEndNode();//设置边界条件

            this.D = this.K.Inverse() * this.F;

            double MaxD;
            double MaxF;
            int indexMaxD = -1;
            int indexMaxF = -1;

            StaticDisforNT(out MaxD, out MaxF, out indexMaxD, out indexMaxF);

            if (MaxF > 0 && indexMaxF >= 0)
            {
                double k = MaxD / MaxF;
                this.E *= k;

                bM = new BeamsStiffMatrix(Map.PolylineList, Map.TriNodeList.Count, E, I, A);
                this.K = bM.Matrix_K;

                SetBoundaryPointListAtEndNode();//设置边界条件

                this.D = this.K.Inverse() * this.F;

            }
            else
            {
                this.isContinue = false;
                return;
            }
            this.UpdataCoordsforNT();      //更新坐标

            if (MaxF <= DisThresholdPP * 0.01)
            {
                this.isContinue = false;
            }

            OutputDisplacementandForces(fV.ForceList);
        }

        /// <summary>
        ///道路网设置边界条件移位
        /// </summary>
        public void DoDisplaceBoundaryConforNT()
        {
            bM = new BeamsStiffMatrix(Map.PolylineList, Map.TriNodeList.Count, E, I, A);
            this.fV.MakeForceVectorfrmPolylineList();
            this.K = bM.Matrix_K;
            this.F = fV.Vector_F;

            SetBoundaryPointListAtBoundaryPoint();//设置边界条件
            SetBoundaryPointListAtInitDisVectors();//设置边界条件


            this.D = this.K.Inverse() * this.F;
            this.UpdataCoordsforNT();      //更新坐标
            OutputDisplacementandForces(fV.ForceList);
        }

        /// <summary>
        /// 统计移位值
        /// </summary>
        private void StaticDisforNT(out double MaxD, out double MaxF, out int indexMaxD, out int indexMaxF)
        {
            int n = fV.ForceList.Count;
            MaxD = -1;
            MaxF = -1;
            double curD;
            indexMaxD = -1;
            indexMaxF = -1;

            if (n == 0)
                return;

            for (int i = 0; i < n; i++)
            {
                if (this.fV.ForceList[i].F > MaxF)
                {
                    MaxF = fV.ForceList[i].F;
                    indexMaxF = fV.ForceList[i].ID;
                }
            }
            MaxD = Math.Sqrt(this.D[3 * indexMaxF, 0] * this.D[3 * indexMaxF, 0] + this.D[3 * indexMaxF + 1, 0] * this.D[3 * indexMaxF + 1, 0]);
        }

        /// <summary>
        /// 设置边界--如果两个端点不受力，则将它们设置为边界点
        /// </summary>
        private void SetBoundaryPointListAtEndNode()
        {
            foreach (ConNode node in Map.ConNodeList)
            {
                if (node.Point.TagValue != -1)
                {
                    int index = node.Point.ID;
                    this.SetBoundPointParamsBigNumber(index, 0, 0);
                }
            }

        }
        /// <summary>
        /// 设置边界--如果两个端点不受力，则将它们设置为边界点
        /// </summary>
        private void SetBoundaryPointListAtBoundaryPoint()
        {
            foreach (ConNode node in Map.ConNodeList)
            {
                if (node.Point.IsBoundaryPoint == true)
                {
                    int index = node.Point.ID;
                    this.SetBoundPointParamsBigNumber(index, 0, 0);
                }
            }

        }

        /// <summary>
        /// 设置边界--如果两个端点不受力，则将它们设置为边界点
        /// </summary>
        private void SetBoundaryPointListAtInitDisVectors()
        {
            foreach (Force f in this.fV.ForceList)
            {
                int index = f.ID;
                this.SetBoundPointParamsBigNumber(index, f.Fx, f.Fy);
            }
        }

        /// <summary>
        /// 更新坐标位置
        /// </summary>
        private void UpdataCoordsforNT()
        {

            for (int i = 0; i < Map.TriNodeList.Count; i++)
            {
                Node curPoint = Map.TriNodeList[i];
                int index = curPoint.ID;
                curPoint.X += this.D[3 * index, 0];
                curPoint.dx += this.D[3 * index, 0];//记录移位值
                curPoint.Y += this.D[3 * index + 1, 0];
                curPoint.dy += this.D[3 * index + 1, 0];//记录移位值
            }

        }
        /// <summary>
        /// 输出移位值和力
        /// </summary>
        /// <param name="ForceList">力列表</param>
        private void OutputDisplacementandForces(List<Force> ForceList)
        {
            if (ForceList == null || ForceList.Count == 0)
                return;
            DataSet ds = new DataSet();
            //创建一个表
            DataTable tableforce = new DataTable();
            tableforce.TableName = "DispalcementandForces";
            tableforce.Columns.Add("ID", typeof(int));
            tableforce.Columns.Add("F", typeof(double));
            tableforce.Columns.Add("D", typeof(double));
            tableforce.Columns.Add("Fx", typeof(double));
            tableforce.Columns.Add("Dx", typeof(double));
            tableforce.Columns.Add("Fy", typeof(double));
            tableforce.Columns.Add("Dy", typeof(double));
            foreach (Force force in ForceList)
            {
                double dx = this.D[3 * force.ID, 0];
                double dy = this.D[3 * force.ID + 1, 0];
                double d = Math.Sqrt(dx * dx + dy * dy);
                DataRow dr = tableforce.NewRow();
                dr[0] = force.ID;
                dr[1] = force.F;
                dr[2] = d;
                dr[3] = force.Fx;
                dr[4] = dx;
                dr[5] = force.Fy;
                dr[6] = dy;
                tableforce.Rows.Add(dr);
            }
            TXTHelper.ExportToTxt(tableforce, this.strPath + @"-DandF.txt");
        }

        /// <summary>
        /// 输出移位值和力
        /// </summary>
        /// <param name="ForceList">力列表</param>
        private void OutputTotalDisplacementforProxmityGraph(ProxiGraph orginal, ProxiGraph current, SMap map)
        {
            if (orginal == null || current == null)
                return;
            DataSet ds = new DataSet();
            //创建一个表
            DataTable tableforce = new DataTable();
            tableforce.TableName = "TotalDisplacement";
            tableforce.Columns.Add("ID", typeof(int));
            tableforce.Columns.Add("Dx", typeof(double));
            tableforce.Columns.Add("Dy", typeof(double));
            tableforce.Columns.Add("D", typeof(double));

            foreach (PolygonObject obj in map.PolygonList)
            {
                int id = obj.ID;
                ProxiNode oNode = orginal.GetNodebyTagIDandType(id, FeatureType.PolygonType);
                ProxiNode cNode = current.GetNodebyTagIDandType(id, FeatureType.PolygonType);
                if (oNode != null && cNode != null)
                {
                    double dx = cNode.X - oNode.X;
                    double dy = cNode.Y - oNode.Y;
                    double d = Math.Sqrt(dx * dx + dy * dy);
                    DataRow dr = tableforce.NewRow();
                    dr[0] = id;
                    dr[1] = dx;
                    dr[2] = dy;
                    dr[3] = d;
                    tableforce.Rows.Add(dr);
                }

            }
            TXTHelper.ExportToTxt(tableforce, this.strPath + @"-Displacement.txt");
        }
        #endregion

        #region 邻近图移位-2014-4-20
        /// <summary>
        /// 构造函数-从建筑物群邻近图构建Beams模型
        /// </summary>
        /// <param name="proxiGraph">邻近图</param>
        /// <param name="map">地图对象</param>
        /// <param name="e">弹性模量</param>
        /// <param name="i">惯性力矩</param>
        /// <param name="a">横截面积</param>
        /// <param name="conflictList">邻近冲突列表</param>
        public AlgBeams(ProxiGraph proxiGraph, SMap map, List<GroupofMapObject> groups, double e, double i, double a, List<ConflictBase> conflictList)
        {
            this.E = e;
            this.I = i;
            this.A = a;
            this.ProxiGraph = proxiGraph;
            this.Map = map;
            this.ConflictList = conflictList;
            this.Groups = groups;
        }

        /// <summary>
        /// 邻近图的移位算法实现
        /// </summary>
        public void DoDispacePGNew_Group()
        {
            fV = new BeamsForceVector(this.ProxiGraph);
            //求吸引力-2014-3-20所用
            fV.OrigialProxiGraph = this.OriginalGraph;
            fV.RMSE = this.PAT * this.Scale / 1000;
            fV.isDragForce = this.isDragF;

            fV.CreateForceVectorfrmConflict_Group(ConflictList, Groups);

            fV.Create_WriteForceVector2Shp(strPath, @"ForceVector", esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
            this.F = fV.Vector_F;

            double MaxD;
            double MaxF;
            double MaxDF;
            double MaxFD;
            int indexMaxD = -1;
            int indexMaxF = -1;

            if ((this.ProxiGraph.PolygonCount <= 3 && this.AlgType == 2) || this.AlgType == 1)
            {
                //基本几何算法
                this.D = new Matrix(this.ProxiGraph.NodeList.Count * 3, 1);

                this.UpdataCoordsforPGbyForce_Group();

                StaticDisforPGNewDF(out MaxFD, out MaxD, out MaxDF, out MaxF, out indexMaxD, out indexMaxF);
                if (MaxF > 0)
                {
                    this.isContinue = true;
                }
                else
                {
                    this.isContinue = false;
                    return;
                }
            }
            else
            {
                //计算刚度矩阵
                bM = new BeamsStiffMatrix(this.ProxiGraph, E, I, A);
                this.K = bM.Matrix_K;

                this.SetBoundPointParamforPG();//设置边界条件

                this.D = this.K.Inverse() * this.F;


                StaticDisforPGNewDF(out MaxFD, out MaxD, out MaxDF, out MaxF, out indexMaxD, out indexMaxF);

                if (MaxF > 0)
                {
                    double k = 1;
                    if (MaxD / MaxFD <= 5)
                    {
                        k = MaxFD / MaxF;
                    }
                    else
                    {
                        k = MaxD / MaxDF;
                    }

                    this.E *= k;
                    //再次计算刚度矩阵
                    bM = new BeamsStiffMatrix(this.ProxiGraph, E, I, A);
                    this.K = bM.Matrix_K;
                    SetBoundPointParamforPG();//设置边界条件
                    this.D = this.K.Inverse() * this.F;
                }
                else
                {
                    this.isContinue = false;
                    return;
                }

                UpdataCoordsforPG_Group();      //更新坐标
            }

            this.OutputDisplacementandForces(fV.ForceList);

            if (MaxF <= DisThresholdPP * 0.01)
            {
                this.isContinue = false;
            }
        }

        /// <summary>
        /// DorlingDisplace
        /// MaxTd 吸力的作用范围
        /// </summary>
        public void DoDisplacePgCTP(List<ProxiNode> NodeList, List<ProxiNode> FinalLocation, double MinDis, double StopT)
        {
            fV = new BeamsForceVector(this.ProxiGraph);
            fV.OrigialProxiGraph = this.OriginalGraph;
            fV.RMSE = this.PAT * this.Scale / 1000;
            fV.isDragForce = this.isDragF;
            fV.CreateForceVectorForCTP(NodeList, FinalLocation, MinDis);//ForceList
            this.F = fV.Vector_F;

            double MaxD;
            double MaxF = 100;
            double MaxDF;
            double MaxFD;
            int indexMaxD = -1;
            int indexMaxF = -1;

            #region 移位
            if (this.ProxiGraph.NodeList.Count > 0)
            {
                //计算刚度矩阵
                bM = new BeamsStiffMatrix(this.ProxiGraph, E, I, A);
                this.K = bM.Matrix_K;
                this.SetBoundPointParamforPG_CTP();//设置边界条件

                try
                {
                    this.D = this.K.Inverse() * this.F;//this.K可能不可逆，故用Try Catch来判断
                }
                catch
                {
                    this.isContinue = false;
                    return;
                }


                StaticDisforPGNewDF(out MaxFD, out MaxD, out MaxDF, out MaxF, out indexMaxD, out indexMaxF);

                if (MaxF > 0)
                {
                    double k = 1;
                    if (MaxD / MaxFD <= 5)
                    {
                        k = MaxFD / MaxF;
                    }
                    else
                    {
                        k = MaxD / MaxDF;
                    }

                    this.E *= k;

                    bM = new BeamsStiffMatrix(this.ProxiGraph, E, I, A);
                    this.K = bM.Matrix_K;
                    SetBoundPointParamforPG_CTP();//设置边界条件
                    try
                    {
                        this.D = this.K.Inverse() * this.F;//this.K可能不可逆，故用Try Catch来判断
                    }
                    catch
                    {
                        this.isContinue = false;
                        return;
                    }
                }
                else
                {
                    this.isContinue = false;
                    return;
                }

                UpdataCoordsforPGCTP();      //更新坐标
            }
            #endregion

            ///this.OutputDisplacementandForces(fV.ForceList);//输出移位向量和力

            if (MaxF <= StopT)
            {
                this.isContinue = false;
            }
        }

        /// <summary>
        /// DorlingDisplace
        /// MaxTd 吸力的作用范围
        /// </summary>
        public void DoDisplacePgCTP_2(List<ProxiNode> NodeList, List<ProxiNode> FinalLocation, double MinDis, double StopT)
        {
            fV = new BeamsForceVector(this.ProxiGraph);
            fV.OrigialProxiGraph = this.OriginalGraph;
            fV.RMSE = this.PAT * this.Scale / 1000;
            fV.isDragForce = this.isDragF;
            fV.CreateForceVectorForCTP_2(NodeList, FinalLocation, MinDis);//ForceList
            this.Test_F = fV.test_Vector_F;

            double MaxD;
            double MaxF = 100;
            double MaxDF;
            double MaxFD;
            int indexMaxD = -1;
            int indexMaxF = -1;

            #region 移位
            if (this.ProxiGraph.NodeList.Count > 0)
            {
                //计算刚度矩阵
                bM = new BeamsStiffMatrix(this.ProxiGraph, E, I, A, 1);
                this.Test_K = bM.Test_K;

                this.SetBoundPointParamforPG();//设置边界条件
                try
                {
                    double[,] Inverse_K = this.ReverseMatrix(this.Test_K, 3 * this.ProxiGraph.NodeList.Count);

                    #region 计算Test_D
                    for (int i = 0; i < 3 * this.ProxiGraph.NodeList.Count; i++)
                    {
                        for (int j = 0; j < 3 * this.ProxiGraph.NodeList.Count; j++)
                        {
                            Test_D[3 * i, 0] += Inverse_K[3 * i, j] * Test_F[j, 0];
                            Test_D[3 * i + 1, 0] += Inverse_K[3 * i + 1, j] * Test_F[j, 0];
                            Test_D[3 * i + 2, 0] += Inverse_K[3 * i + 2, j] * Test_F[j, 0];
                        }
                    }
                    #endregion

                    //this.D = this.K.Inverse() * this.F;//this.K可能不可逆，故用Try Catch来判断
                }
                catch
                {
                    this.isContinue = false;
                    return;
                }


                StaticDisforPGNewDF_2(out MaxFD, out MaxD, out MaxDF, out MaxF, out indexMaxD, out indexMaxF);

                if (MaxF > 0)
                {
                    double k = 1;
                    if (MaxD / MaxFD <= 5)
                    {
                        k = MaxFD / MaxF;
                    }
                    else
                    {
                        k = MaxD / MaxDF;
                    }

                    this.E *= k;
                    bM = new BeamsStiffMatrix(this.ProxiGraph, E, I, A, 1);
                    this.Test_K = bM.Test_K;
                   
                    SetBoundPointParamforPG();//设置边界条件
                    try
                    {
                        double[,] Inverse_K = this.ReverseMatrix(this.Test_K, 3 * this.ProxiGraph.NodeList.Count);
                        
                        #region 计算Test_D
                        for (int i = 0; i < 3 * this.ProxiGraph.NodeList.Count; i++)
                        {
                            for (int j = 0; j < 3 * this.ProxiGraph.NodeList.Count; j++)
                            {
                                Test_D[3 * i, 0] += Inverse_K[3 * i, j] * Test_F[j, 0];
                                Test_D[3 * i + 1, 0] += Inverse_K[3 * i + 1, j] * Test_F[j, 0];
                                Test_D[3 * i + 2, 0] += Inverse_K[3 * i + 2, j] * Test_F[j, 0];
                            }
                        }
                        #endregion
                    }
                    catch
                    {
                        this.isContinue = false;
                        return;
                    }
                }
                else
                {
                    this.isContinue = false;
                    return;
                }

                UpdataCoordsforPGCTP_2();      //更新坐标
            }
            #endregion

            ///this.OutputDisplacementandForces(fV.ForceList);//输出移位向量和力

            if (MaxF <= StopT)
            {
                this.isContinue = false;
            }
        }
        #endregion

        #region DorlingMap的移位
        /// <summary>
        /// DorlingDisplace
        /// MaxTd 吸力的作用范围
        /// </summary>
        public void DoDisplacePgDorling(SMap pMap, double StopT, double MaxTd, int ForceType, bool WeightConsi, double InterDis)
        {
            fV = new BeamsForceVector(this.ProxiGraph);
            fV.OrigialProxiGraph = this.OriginalGraph;
            fV.RMSE = this.PAT * this.Scale / 1000;
            fV.isDragForce = this.isDragF;
            fV.CreateForceVectorForDorling(pMap.PolygonList, MaxTd, ForceType, WeightConsi, InterDis);//ForceList
            this.F = fV.Vector_F;

            double MaxD;
            double MaxF;
            double MaxDF;
            double MaxFD;
            int indexMaxD = -1;
            int indexMaxF = -1;

            if (this.ProxiGraph.NodeList.Count <= 2)
            {
                #region 基本几何算法（直接移位）
                BeamsForceVector BFV = new BeamsForceVector();
                PolygonObject Po1 = this.GetPoByID(this.ProxiGraph.NodeList[0].TagID, pMap.PolygonList);
                PolygonObject Po2 = this.GetPoByID(this.ProxiGraph.NodeList[1].TagID, pMap.PolygonList);
                if (this.ProxiGraph.EdgeList!=null)
                {
                    List<Force> ForceList = BFV.GetForce(this.ProxiGraph.NodeList[0], this.ProxiGraph.NodeList[1], Po1, Po2, 1, this.ProxiGraph.EdgeList[0].adajactLable, this.ProxiGraph.EdgeList[0].LongEdge, MaxTd, WeightConsi, this.ProxiGraph.EdgeList[0].MSTLable, InterDis);//考虑引力            

                    #region 更新坐标
                    if (ForceList.Count > 0)
                    {
                        for (int i = 0; i < this.ProxiGraph.NodeList.Count; i++)
                        {
                            int Cachei = -1;

                            if (i == 0)
                            {
                                Cachei = 1;

                            }
                            else
                            {
                                Cachei = 0;
                            }

                            ProxiNode curNode = this.ProxiGraph.NodeList[Cachei];
                            curNode.X += ForceList[Cachei].Fx;//更新邻近图
                            curNode.Y += ForceList[Cachei].Fy;

                            PolygonObject po = this.GetPoByID(curNode.TagID, this.Map.PolygonList);
                            foreach (TriNode curPoint in po.PointList)
                            {
                                curPoint.X += ForceList[Cachei].Fx;
                                curPoint.Y += ForceList[Cachei].Fy;
                            }
                        }
                    }
                    #endregion

                    MaxF = 0;
                    this.isContinue = false;
                }
                return; 
                #endregion
            }

            else
            {
                //计算刚度矩阵
                bM = new BeamsStiffMatrix(this.ProxiGraph, E, I, A);
                this.K = bM.Matrix_K;

                this.SetBoundPointParamforPG();//设置边界条件
                try
                {
                    this.D = this.K.Inverse() * this.F;//this.K可能不可逆，故用Try Catch来判断
                }
                catch
                {
                    this.isContinue = false;
                    return;
                }


                StaticDisforPGNewDF(out MaxFD, out MaxD, out MaxDF, out MaxF, out indexMaxD, out indexMaxF);

                if (MaxF > 0)
                {
                    double k = 1;
                    if (MaxD / MaxFD <= 5)
                    {
                        k = MaxFD / MaxF;
                    }
                    else
                    {
                        k = MaxD / MaxDF;
                    }

                    this.E *= k;
                    //再次计算刚度矩阵
                    bM = new BeamsStiffMatrix(this.ProxiGraph, E, I, A);
                    this.K = bM.Matrix_K;
                    SetBoundPointParamforPG();//设置边界条件
                    try
                    {
                        this.D = this.K.Inverse() * this.F;//this.K可能不可逆，故用Try Catch来判断
                    }
                    catch
                    {
                        this.isContinue = false;
                        return;
                    }
                }
                else
                {
                    this.isContinue = false;
                    return;
                }

                UpdataCoordsforPGDorling();      //更新坐标
            }

            ///this.OutputDisplacementandForces(fV.ForceList);//输出移位向量和力

            if (MaxF <= StopT)
            {
                this.isContinue = false;
            }
        }

        /// <summary>
        /// DorlingDisplace
        /// MaxTd 吸力的作用范围
        /// </summary>
        /// GroupForceType=0 平均力；GroupForceType=1最大力；GroupForceType=0 最小力；
        public void DoDisplacePgStableDorling(List<SMap> SubMaps, double StopT, double MaxTd, int ForceType, bool WeightConsi, double InterDis, int GroupForceType)
        {
            fV = new BeamsForceVector(this.ProxiGraph);
            fV.OrigialProxiGraph = this.OriginalGraph;
            fV.RMSE = this.PAT * this.Scale / 1000;
            fV.isDragForce = this.isDragF;
            fV.CreateForceVectorForStableDorling(SubMaps, MaxTd, ForceType, WeightConsi, InterDis, GroupForceType);//ForceList
            this.F = fV.Vector_F;

            double MaxD;
            double MaxF = 0;
            double MaxDF;
            double MaxFD;
            int indexMaxD = -1;
            int indexMaxF = -1;

            if (this.ProxiGraph.NodeList.Count == 2)
            {
                #region 力计算
                double eSumFx = 0; double eSumFy = 0; double eSum = 0;
                double sSumFx = 0; double sSumFy = 0; double sSum = 0;
                double s = 0; double c = 0;
                for (int i = 0; i < SubMaps.Count; i++)
                {
                    #region 基本几何算法（直接移位）
                    BeamsForceVector BFV = new BeamsForceVector();
                    PolygonObject Po1 = this.GetPoByID(this.ProxiGraph.NodeList[0].TagID, SubMaps[i].PolygonList);
                    PolygonObject Po2 = this.GetPoByID(this.ProxiGraph.NodeList[1].TagID, SubMaps[i].PolygonList);
                    List<Force> CacheForceList = BFV.GetForce(this.ProxiGraph.NodeList[0], this.ProxiGraph.NodeList[1], Po1, Po2, 1, this.ProxiGraph.EdgeList[0].adajactLable, this.ProxiGraph.EdgeList[0].LongEdge, MaxTd, WeightConsi, this.ProxiGraph.EdgeList[0].MSTLable, InterDis);//考虑引力
                    if (CacheForceList.Count > 0)
                    {
                        eSumFx = CacheForceList[0].Fx + eSumFx; eSumFy = CacheForceList[0].Fy + eSumFy; eSum = CacheForceList[0].F + eSum;
                        sSumFx = CacheForceList[1].Fx + sSumFx; sSumFy = CacheForceList[1].Fy + sSumFy; sSum = CacheForceList[1].F + sSum;
                        s = CacheForceList[0].Sin; c = CacheForceList[0].Cos;
                    }
                    #endregion
                }

                List<Force> ForceList = new List<Force>();
                Force eForce = new Force(this.ProxiGraph.NodeList[1].TagID, eSumFx / SubMaps.Count, eSumFy / SubMaps.Count, s, c, eSum / SubMaps.Count);
                Force sForce = new Force(this.ProxiGraph.NodeList[0].TagID, sSumFx / SubMaps.Count, sSumFy / SubMaps.Count, s * (-1), c * (-1), sSum / SubMaps.Count);
                ForceList.Add(sForce);
                ForceList.Add(eForce);
                #endregion

                #region 更新坐标
                if (ForceList.Count > 0)
                {
                    for (int i = 0; i < this.ProxiGraph.NodeList.Count; i++)
                    {
                        int Cachei = -1;

                        if (i == 0)
                        {
                            Cachei = 1;

                        }
                        else
                        {
                            Cachei = 0;
                        }

                        ProxiNode curNode = this.ProxiGraph.NodeList[Cachei];
                        curNode.X += ForceList[Cachei].Fx;//更新邻近图
                        curNode.Y += ForceList[Cachei].Fy;

                        for (int j = 0; j < SubMaps.Count; j++)
                        {
                            PolygonObject po = this.GetPoByID(curNode.TagID, SubMaps[j].PolygonList);
                            foreach (TriNode curPoint in po.PointList)
                            {
                                curPoint.X += ForceList[Cachei].Fx;
                                curPoint.Y += ForceList[Cachei].Fy;
                            }
                        }
                    }
                }
                #endregion

                MaxF = 0;
                this.isContinue = false;
                return;
            }

            else if (this.ProxiGraph.NodeList.Count > 2)
            {
                //计算刚度矩阵
                bM = new BeamsStiffMatrix(this.ProxiGraph, E, I, A);
                this.K = bM.Matrix_K;

                this.SetBoundPointParamforPG();//设置边界条件
                try
                {
                    this.D = this.K.Inverse() * this.F;//this.K可能不可逆，故用Try Catch来判断
                }
                catch
                {
                    this.isContinue = false;
                    return;
                }

                StaticDisforPGNewDF(out MaxFD, out MaxD, out MaxDF, out MaxF, out indexMaxD, out indexMaxF);

                if (MaxF > 0)
                {
                    double k = 1;
                    if (MaxD / MaxFD <= 5)
                    {
                        k = MaxFD / MaxF;
                    }
                    else
                    {
                        k = MaxD / MaxDF;
                    }

                    this.E *= k;
                    //再次计算刚度矩阵
                    bM = new BeamsStiffMatrix(this.ProxiGraph, E, I, A);
                    this.K = bM.Matrix_K;
                    SetBoundPointParamforPG();//设置边界条件
                    try
                    {
                        this.D = this.K.Inverse() * this.F;//this.K可能不可逆，故用Try Catch来判断
                    }
                    catch
                    {
                        this.isContinue = false;
                        return;
                    }
                }
                else
                {
                    this.isContinue = false;
                    return;
                }

                UpdataCoordsforPGDorling(SubMaps);      //更新坐标
            }

            ///this.OutputDisplacementandForces(fV.ForceList);//输出移位向量和力

            if (MaxF <= StopT)
            {
                this.isContinue = false;
            }
        }

        /// <summary>
        /// DorlingDisplace
        /// MaxTd 吸力的作用范围
        /// </summary>
        public void GroupDoDisplacePgDorling(SMap pMap, double StopT, double MaxTd, int ForceType, bool WeightConsi)
        {
            fV = new BeamsForceVector(this.ProxiGraph);
            //求吸引力-2014-3-20所用
            fV.OrigialProxiGraph = this.OriginalGraph;
            fV.RMSE = this.PAT * this.Scale / 1000;
            fV.isDragForce = this.isDragF;
            fV.GroupCreateForceVectorForDorling(pMap.PolygonList, MaxTd, ForceType, WeightConsi);//ForceList
            this.F = fV.Vector_F;

            double MaxD;
            double MaxF;
            double MaxDF;
            double MaxFD;
            int indexMaxD = -1;
            int indexMaxF = -1;

            if (this.ProxiGraph.NodeList.Count <= 2)
            {
                #region 基本几何算法（直接移位）
                BeamsForceVector BFV = new BeamsForceVector();
                List<PolygonObject> PoList1 = this.GetPoList(this.ProxiGraph.NodeList[0], pMap.PolygonList);
                List<PolygonObject> PoList2 = this.GetPoList(this.ProxiGraph.NodeList[1], pMap.PolygonList);
                List<Force> ForceList = BFV.GetForceGroup(this.ProxiGraph.NodeList[0], this.ProxiGraph.NodeList[1], PoList1, PoList2, 1, MaxTd, WeightConsi);//考虑引力
                #endregion

                #region 更新坐标
                for (int i = 0; i < this.ProxiGraph.NodeList.Count; i++)
                {
                    int Cachei = -1;

                    if (i == 0)
                    {
                        Cachei = 1;

                    }
                    else
                    {
                        Cachei = 0;
                    }

                    ProxiNode curNode = this.ProxiGraph.NodeList[Cachei];
                    curNode.X += ForceList[Cachei].Fx;//更新邻近图
                    curNode.Y += ForceList[Cachei].Fy;

                    List<PolygonObject> PoList = this.GetPoList(curNode, this.Map.PolygonList);
                    foreach (PolygonObject po in PoList)
                    {
                        foreach (TriNode curPoint in po.PointList)
                        {
                            curPoint.X += ForceList[Cachei].Fx;
                            curPoint.Y += ForceList[Cachei].Fy;
                        }
                    }
                }
                #endregion

                MaxF = 0;
                this.isContinue = false;
            }

            else
            {
                //计算刚度矩阵
                bM = new BeamsStiffMatrix(this.ProxiGraph, E, I, A);
                this.K = bM.Matrix_K;

                this.SetBoundPointParamforPG();//设置边界条件
                try
                {
                    this.D = this.K.Inverse() * this.F;//this.K可能不可逆，故用Try Catch来判断
                }
                catch
                {
                    this.isContinue = false;
                    return;
                }


                StaticDisforPGNewDF(out MaxFD, out MaxD, out MaxDF, out MaxF, out indexMaxD, out indexMaxF);

                if (MaxF > 0)
                {
                    double k = 1;
                    if (MaxD / MaxFD <= 5)
                    {
                        k = MaxFD / MaxF;
                    }
                    else
                    {
                        k = MaxD / MaxDF;
                    }

                    this.E *= k;
                    //再次计算刚度矩阵
                    bM = new BeamsStiffMatrix(this.ProxiGraph, E, I, A);
                    this.K = bM.Matrix_K;
                    SetBoundPointParamforPG();//设置边界条件
                    try
                    {
                        this.D = this.K.Inverse() * this.F;//this.K可能不可逆，故用Try Catch来判断
                    }
                    catch
                    {
                        this.isContinue = false;
                        return;
                    }
                }
                else
                {
                    this.isContinue = false;
                    return;
                }

                UpdataCoordsforPGDorlingGroup();      //更新坐标
            }

            ///this.OutputDisplacementandForces(fV.ForceList);//输出移位向量和力

            if (MaxF <= StopT)
            {
                this.isContinue = false;
            }
        }
        #endregion

        #region TagMap的移位
        /// <summary>
        /// DorlingDisplace
        /// MaxTd 吸力的作用范围
        /// </summary>
        public void DoDisplacePgTagMap(SMap pMap, double StopT, double MaxTd, int ForceType, bool WeightConsi, double InterDis)
        {
            fV = new BeamsForceVector(this.ProxiGraph);
            fV.OrigialProxiGraph = this.OriginalGraph;
            fV.RMSE = this.PAT * this.Scale / 1000;
            fV.isDragForce = this.isDragF;
            fV.CreateForceVectorForTagMap(pMap.PolygonList, MaxTd,WeightConsi, InterDis);//ForceList
            this.F = fV.Vector_F;

            double MaxD;
            double MaxF;
            double MaxDF;
            double MaxFD;
            int indexMaxD = -1;
            int indexMaxF = -1;

            if (this.ProxiGraph.NodeList.Count <= 2)
            {
                #region 基本几何算法（直接移位）
                BeamsForceVector BFV = new BeamsForceVector();
                PolygonObject Po1 = this.GetPoByID(this.ProxiGraph.NodeList[0].TagID, pMap.PolygonList);
                PolygonObject Po2 = this.GetPoByID(this.ProxiGraph.NodeList[1].TagID, pMap.PolygonList);
                if (this.ProxiGraph.EdgeList != null)
                {
                    List<Force> ForceList = BFV.GetForce(this.ProxiGraph.NodeList[0], this.ProxiGraph.NodeList[1], Po1, Po2, 1, this.ProxiGraph.EdgeList[0].adajactLable, this.ProxiGraph.EdgeList[0].LongEdge, MaxTd, WeightConsi, this.ProxiGraph.EdgeList[0].MSTLable, InterDis);//考虑引力            

                    #region 更新坐标
                    if (ForceList.Count > 0)
                    {
                        for (int i = 0; i < this.ProxiGraph.NodeList.Count; i++)
                        {
                            int Cachei = -1;

                            if (i == 0)
                            {
                                Cachei = 1;

                            }
                            else
                            {
                                Cachei = 0;
                            }

                            ProxiNode curNode = this.ProxiGraph.NodeList[Cachei];
                            curNode.X += ForceList[Cachei].Fx;//更新邻近图
                            curNode.Y += ForceList[Cachei].Fy;

                            PolygonObject po = this.GetPoByID(curNode.TagID, this.Map.PolygonList);
                            foreach (TriNode curPoint in po.PointList)
                            {
                                curPoint.X += ForceList[Cachei].Fx;
                                curPoint.Y += ForceList[Cachei].Fy;
                            }
                        }
                    }
                    #endregion

                    MaxF = 0;
                    this.isContinue = false;
                }
                return;
                #endregion
            }

            else
            {
                //计算刚度矩阵
                bM = new BeamsStiffMatrix(this.ProxiGraph, E, I, A);
                this.K = bM.Matrix_K;

                this.SetBoundPointParamforPG();//设置边界条件
                try
                {
                    this.D = this.K.Inverse() * this.F;//this.K可能不可逆，故用Try Catch来判断
                }
                catch
                {
                    this.isContinue = false;
                    return;
                }


                StaticDisforPGNewDF(out MaxFD, out MaxD, out MaxDF, out MaxF, out indexMaxD, out indexMaxF);

                if (MaxF > 0)
                {
                    double k = 1;
                    if (MaxD / MaxFD <= 5)
                    {
                        k = MaxFD / MaxF;
                    }
                    else
                    {
                        k = MaxD / MaxDF;
                    }

                    this.E *= k;
                    //再次计算刚度矩阵
                    bM = new BeamsStiffMatrix(this.ProxiGraph, E, I, A);
                    this.K = bM.Matrix_K;
                    SetBoundPointParamforPG();//设置边界条件
                    try
                    {
                        this.D = this.K.Inverse() * this.F;//this.K可能不可逆，故用Try Catch来判断
                    }
                    catch
                    {
                        this.isContinue = false;
                        return;
                    }
                }
                else
                {
                    this.isContinue = false;
                    return;
                }

                UpdataCoordsforPGDorling();      //更新坐标
            }

            ///this.OutputDisplacementandForces(fV.ForceList);//输出移位向量和力

            if (MaxF <= StopT)
            {
                this.isContinue = false;
            }
        }
        #endregion

        /// <summary>
        /// TileMap移位
        /// </summary>
        public void DoDisplaceTileMap(SMap sMap, double MaxForce, double MaxForce_2, double Size)
        {
            fV = new BeamsForceVector(this.ProxiGraph);
            fV.OrigialProxiGraph = this.OriginalGraph;
            fV.RMSE = this.PAT * this.Scale / 1000;
            fV.isDragForce = this.isDragF;
            fV.CreateForceVectorfrm_TileMap(this.ProxiGraph.NodeList, this.ProxiGraph.EdgeList, MaxForce, MaxForce_2, Size); ;//ForceList
            this.F = fV.Vector_F;

            double MaxD;
            double MaxF;
            double MaxDF;
            double MaxFD;
            int indexMaxD = -1;
            int indexMaxF = -1;

            if (this.ProxiGraph.NodeList.Count <= 2)
            {
                return;
            }

            else
            {
                //计算刚度矩阵
                bM = new BeamsStiffMatrix(this.ProxiGraph, E, I, A);
                this.K = bM.Matrix_K;

                this.SetBoundPointParamforPG();//设置边界条件
                try
                {
                    this.D = this.K.Inverse() * this.F;//this.K可能不可逆，故用Try Catch来判断
                }
                catch
                {
                    this.isContinue = false;
                    return;
                }


                StaticDisforPGNewDF(out MaxFD, out MaxD, out MaxDF, out MaxF, out indexMaxD, out indexMaxF);

                if (MaxF > 0)
                {
                    double k = 1;
                    if (MaxD / MaxFD <= 5)
                    {
                        k = MaxFD / MaxF;
                    }
                    else
                    {
                        k = MaxD / MaxDF;
                    }

                    this.E *= k;
                    //再次计算刚度矩阵
                    bM = new BeamsStiffMatrix(this.ProxiGraph, E, I, A);
                    this.K = bM.Matrix_K;
                    SetBoundPointParamforPG();//设置边界条件
                    try
                    {
                        this.D = this.K.Inverse() * this.F;//this.K可能不可逆，故用Try Catch来判断
                    }
                    catch
                    {
                        this.isContinue = false;
                        return;
                    }
                }
                else
                {
                    this.isContinue = false;
                    return;
                }

                UpdataCoordsforPG_CTP3();  //更新坐标
            }

            ///this.OutputDisplacementandForces(fV.ForceList);//输出移位向量和力

            if (MaxF <= 0.01)
            {
                this.isContinue = false;
            }
        }

        #region 基础支持
        /// <summary>
        /// 邻近图的移位算法实现-设置边界条件
        /// </summary>
        public void DoDispacePGNew_BC_Group()
        {
            double MaxD;
            double MaxF;
            double MaxDF;
            double MaxFD;
            int indexMaxD = -1;
            int indexMaxF = -1;
            fV = new BeamsForceVector(this.ProxiGraph);
            //求吸引力-2014-3-20所用
            fV.OrigialProxiGraph = this.OriginalGraph;
            fV.RMSE = 0.5 * this.Scale / 1000;
            fV.isDragForce = this.isDragF;
            fV.CreateForceVectorfrmConflict_Group(ConflictList, this.Groups);
            fV.Create_WriteForceVector2Shp(strPath, @"ForceVector", esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
            this.F = fV.Vector_F;

            if ((this.ProxiGraph.PolygonCount <= 3 && this.AlgType == 2) || this.AlgType == 1)
            {
                //基本几何算法
                this.D = new Matrix(this.ProxiGraph.NodeList.Count * 3, 1);
                this.UpdataCoordsforPGbyForce_Group();
                StaticDisforPGNewDF(out MaxFD, out MaxD, out MaxDF, out MaxF, out indexMaxD, out indexMaxF);
                if (MaxF > 0)
                {
                    this.isContinue = true;
                }
                else
                {
                    this.isContinue = false;
                    return;
                }
            }
            else
            {
                bM = new BeamsStiffMatrix(this.ProxiGraph, E, I, A);
                this.K = bM.Matrix_K;


                this.SetBoundPointParamforPG_BC();//设置边界条件

                this.D = this.K.Inverse() * this.F;



                StaticDisforPGNewDF(out MaxFD, out MaxD, out MaxDF, out MaxF, out indexMaxD, out indexMaxF);

                UpdataCoordsforPG_Group();      //更新坐标
                this.OutputDisplacementandForces(fV.ForceList);
            }

            if (MaxF <= DisThresholdPP * 0.01)
            {
                this.isContinue = false;
            }
        }

        /// <summary>
        /// 设置邻近图的边界点2014-3-2
        /// </summary>
        private void SetBoundPointParamforPG()
        {
            Force curForce = null;
            foreach (ProxiNode curNode in this.ProxiGraph.NodeList)
            {
                if (curNode.FeatureType == FeatureType.PolylineType)
                {
                    int index = curNode.ID;
                    this.SetBoundPointParamsBigNumber(index, 0, 0);
                }
            }
        }

        /// <summary>
        /// 设置邻近图的边界点2014-3-2
        /// </summary>
        private void SetBoundPointParamforPG_CTP()
        {
            Force curForce = null;
            foreach (ProxiNode curNode in this.ProxiGraph.NodeList)
            {
                if (curNode.FeatureType == FeatureType.PointType && curNode.TagID == 0)
                {
                    int index = curNode.ID;
                    this.SetBoundPointParamsBigNumber(index, 0, 0);
                }
            }
        }

        /// <summary>
        /// 设置邻近图的边界点2014-3-2
        /// </summary>
        private void SetBoundPointParamforPG_BC()
        {
            foreach (ProxiNode curNode in this.ProxiGraph.NodeList)
            {
                int index = curNode.ID;
                if (curNode.FeatureType == FeatureType.PolylineType)
                {

                    this.SetBoundPointParamsBigNumber(index, 0, 0);
                }
                else if (curNode.FeatureType == FeatureType.PolygonType)
                {
                    Force force = this.fV.GetForcebyIndex(index);
                    if (force != null)
                    {
                        double dx = force.Fx;
                        double dy = force.Fy;
                        this.SetBoundPointParamsBigNumber(index, dx, dy);
                    }
                }
            }
        }

        /// <summary>
        /// 更新坐标位置
        /// </summary>
        private void UpdataCoordsforPG_Group()
        {
            foreach (ProxiNode curNode in this.ProxiGraph.NodeList)
            {
                int index = curNode.ID;
                int tagID = curNode.TagID;
                FeatureType fType = curNode.FeatureType;
                VoronoiPolygon vp = null;

                if (fType == FeatureType.PolygonType)
                {

                    PolygonObject po = this.Map.GetObjectbyID(tagID, fType) as PolygonObject;

                    double curDx0 = this.D[3 * index, 0];
                    double curDy0 = this.D[3 * index + 1, 0];
                    double curDx = this.D[3 * index, 0];
                    double curDy = this.D[3 * index + 1, 0];

                    if (this.IsTopCos == true)
                    {
                        vp = this.VD.GetVPbyIDandType(tagID, fType);
                        vp.TopologicalConstraint(curDx0, curDy0, 0.001, out curDx, out curDy);
                        this.D[3 * index, 0] = curDx;
                        this.D[3 * index + 1, 0] = curDy;
                    }

                    //纠正拓扑错误
                    curNode.X += curDx;
                    curNode.Y += curDy;

                    foreach (TriNode curPoint in po.PointList)
                    {
                        curPoint.X += curDx;
                        curPoint.Y += curDy;
                    }
                }

                else if (fType == FeatureType.Group)//如果是分组
                {
                    GroupofMapObject group = GroupofMapObject.GetGroup(tagID, this.Groups);
                    double curDx0 = this.D[3 * index, 0];
                    double curDy0 = this.D[3 * index + 1, 0];
                    double curDx = this.D[3 * index, 0];
                    double curDy = this.D[3 * index + 1, 0];
                    if (this.IsTopCos == true)
                    {
                        foreach (PolygonObject obj in group.ListofObjects)
                        {
                            tagID = obj.ID;
                            fType = obj.FeatureType;
                            vp = this.VD.GetVPbyIDandType(tagID, fType);
                            vp.TopologicalConstraint(curDx0, curDx0, 0.001, out curDx, out curDy);
                            curDx0 = curDx;
                            curDy0 = curDy;
                        }
                        this.D[3 * index, 0] = curDx;
                        this.D[3 * index + 1, 0] = curDy;
                    }
                    //纠正拓扑错误
                    curNode.X += curDx;
                    curNode.Y += curDy;
                    foreach (PolygonObject obj in group.ListofObjects)
                    {
                        foreach (TriNode curPoint in obj.PointList)
                        {
                            curPoint.X += curDx;
                            curPoint.Y += curDy;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 更新坐标位置
        /// </summary>
        private void UpdataCoordsforPGDorling()
        {
            foreach (ProxiNode curNode in this.ProxiGraph.NodeList)
            {
                int index = curNode.ID;
                int tagID = curNode.TagID;
                FeatureType fType = curNode.FeatureType;
                VoronoiPolygon vp = null;
                Force force = fV.GetForcebyIndex(index);

                PolygonObject po = this.GetPoByID(tagID, this.Map.PolygonList);
                double curDx0 = 0;
                double curDy0 = 0;
                double curDx = 0;
                double curDy = 0;
                if (force != null)
                {

                    curDx0 = this.fV.GetForcebyIndex(index).Fx;
                    curDy0 = this.fV.GetForcebyIndex(index).Fy;
                    curDx = this.fV.GetForcebyIndex(index).Fx;
                    curDy = this.fV.GetForcebyIndex(index).Fy;
                    if (this.IsTopCos == true)
                    {
                        vp = this.VD.GetVPbyIDandType(tagID, fType);
                        vp.TopologicalConstraint(curDx0, curDy0, 0.001, out curDx, out curDy);
                        this.D[3 * index, 0] = curDx;
                        this.D[3 * index + 1, 0] = curDy;
                    }
                    //纠正拓扑错误
                    curNode.X += curDx;//更新邻近图
                    curNode.Y += curDy;

                    foreach (TriNode curPoint in po.PointList)
                    {
                        curPoint.X += curDx;
                        curPoint.Y += curDy;
                    }
                }
                else
                {
                    this.D[3 * index, 0] = curDx;
                    this.D[3 * index + 1, 0] = curDy;
                }
            }

        }

        /// <summary>
        /// 更新坐标位置
        /// </summary>
        private void UpdataCoordsforPGCTP()
        {
            foreach (ProxiNode curNode in this.ProxiGraph.NodeList)
            {
                int index = curNode.ID;
                int tagID = curNode.TagID;
                FeatureType fType = curNode.FeatureType;
                VoronoiPolygon vp = null;
                Force force = fV.GetForcebyIndex(index);

                double curDx0 = 0;
                double curDy0 = 0;
                double curDx = 0;
                double curDy = 0;
                if (force != null)
                {

                    curDx0 = this.fV.GetForcebyIndex(index).Fx;
                    curDy0 = this.fV.GetForcebyIndex(index).Fy;
                    curDx = this.fV.GetForcebyIndex(index).Fx;
                    curDy = this.fV.GetForcebyIndex(index).Fy;
                    if (this.IsTopCos == true)
                    {
                        vp = this.VD.GetVPbyIDandType(tagID, fType);
                        vp.TopologicalConstraint(curDx0, curDy0, 0.001, out curDx, out curDy);
                        this.D[3 * index, 0] = curDx;
                        this.D[3 * index + 1, 0] = curDy;
                    }
                    //纠正拓扑错误
                    curNode.X += curDx;//更新邻近图
                    curNode.Y += curDy;
                }
                else
                {
                    this.D[3 * index, 0] = curDx;
                    this.D[3 * index + 1, 0] = curDy;
                }
            }
        }

        /// <summary>
        /// 更新坐标位置
        /// </summary>
        private void UpdataCoordsforPGCTP_2()
        {
            foreach (ProxiNode curNode in this.ProxiGraph.NodeList)
            {
                int index = curNode.ID;
                int tagID = curNode.TagID;
                FeatureType fType = curNode.FeatureType;
                VoronoiPolygon vp = null;
                Force force = fV.GetForcebyIndex(index);

                double curDx0 = 0;
                double curDy0 = 0;
                double curDx = 0;
                double curDy = 0;
                if (force != null)
                {

                    curDx0 = this.fV.GetForcebyIndex(index).Fx;
                    curDy0 = this.fV.GetForcebyIndex(index).Fy;
                    curDx = this.fV.GetForcebyIndex(index).Fx;
                    curDy = this.fV.GetForcebyIndex(index).Fy;
                    if (this.IsTopCos == true)
                    {
                        vp = this.VD.GetVPbyIDandType(tagID, fType);
                        vp.TopologicalConstraint(curDx0, curDy0, 0.001, out curDx, out curDy);
                        this.Test_D[3 * index, 0] = curDx;
                        this.Test_D[3 * index + 1, 0] = curDy;
                    }
                    //纠正拓扑错误
                    curNode.X += curDx;//更新邻近图
                    curNode.Y += curDy;
                }
                else
                {
                    this.Test_D[3 * index, 0] = curDx;
                    this.Test_D[3 * index + 1, 0] = curDy;
                }
            }

        }

        /// <summary>
        /// 更新坐标位置(更新了Map中要素的信息)
        /// </summary>
        private void UpdataCoordsforPG_CTP3()
        {         
            foreach (ProxiNode curNode in this.ProxiGraph.NodeList)
            {
                int index = curNode.ID;
                int tagID = curNode.TagID;
                FeatureType fType = curNode.FeatureType;

                double curDx0 = this.D[3 * index, 0];
                double curDy0 = this.D[3 * index + 1, 0];
                double curDx = this.D[3 * index, 0];
                double curDy = this.D[3 * index + 1, 0];

                if (this.IsTopCos == true)
                {
                    this.D[3 * index, 0] = curDx;
                    this.D[3 * index + 1, 0] = curDy;
                }

                //纠正拓扑错误
                curNode.X += curDx;
                curNode.Y += curDy;

                #region 更新TriNodeList
                TriNode CacheTriNode = this.GetPointByID(curNode.ID, this.Map.TriNodeList);
                if (CacheTriNode != null)
                {
                    CacheTriNode.X += curDx;
                    CacheTriNode.Y += curDy;
                }
                #endregion
            }
        }

        /// <summary>
        /// GetPointByID
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        public TriNode GetPointByID(int ID, List<TriNode> NodeList)
        {
            TriNode No = null;
            foreach (TriNode CacheNo in NodeList)
            {
                if (CacheNo.ID == ID)
                {
                    No = CacheNo;
                    break;
                }
            }

            return No;
        }

        /// <summary>
        /// 更新群组坐标位置
        /// </summary>
        private void UpdataCoordsforPGDorling(List<SMap> Maps)
        {
            foreach (ProxiNode curNode in this.ProxiGraph.NodeList)
            {
                int index = curNode.ID;
                int tagID = curNode.TagID;
                FeatureType fType = curNode.FeatureType;
                VoronoiPolygon vp = null;
                Force force = fV.GetForcebyIndex(index);


                double curDx0 = 0;
                double curDy0 = 0;
                double curDx = 0;
                double curDy = 0;
                if (force != null)
                {
                    curDx0 = this.fV.GetForcebyIndex(index).Fx;
                    curDy0 = this.fV.GetForcebyIndex(index).Fy;
                    curDx = this.fV.GetForcebyIndex(index).Fx;
                    curDy = this.fV.GetForcebyIndex(index).Fy;
                    if (this.IsTopCos == true)
                    {
                        vp = this.VD.GetVPbyIDandType(tagID, fType);
                        vp.TopologicalConstraint(curDx0, curDy0, 0.001, out curDx, out curDy);
                        this.D[3 * index, 0] = curDx;
                        this.D[3 * index + 1, 0] = curDy;
                    }

                    #region 更新群组邻近图
                    for (int i = 0; i < this.PgList.Count; i++)
                    {
                        ProxiNode CacheCurNode = this.GetPointByID(index, this.PgList[i].NodeList);

                        //纠正拓扑错误
                        CacheCurNode.X += curDx;
                        CacheCurNode.Y += curDy;
                    }
                    #endregion

                    #region 更新Polygons
                    for (int i = 0; i < Maps.Count; i++)
                    {
                        PolygonObject po = this.GetPoByID(tagID, Maps[i].PolygonList);
                        foreach (TriNode curPoint in po.PointList)
                        {
                            curPoint.X += curDx;
                            curPoint.Y += curDy;
                        }
                    }
                    #endregion
                }
                else
                {
                    this.D[3 * index, 0] = curDx;
                    this.D[3 * index + 1, 0] = curDy;
                }
            }

        }

        /// <summary>
        /// 更新坐标位置
        /// </summary>
        private void UpdataCoordsforPGDorlingGroup()
        {
            foreach (ProxiNode curNode in this.ProxiGraph.NodeList)
            {
                int index = curNode.ID;
                int tagID = curNode.TagID;
                FeatureType fType = curNode.FeatureType;
                VoronoiPolygon vp = null;
                Force force = fV.GetForcebyIndex(index);

                List<PolygonObject> PoList = this.GetPoList(curNode, this.Map.PolygonList);
                double curDx0 = 0;
                double curDy0 = 0;
                double curDx = 0;
                double curDy = 0;
                if (force != null)
                {

                    curDx0 = this.fV.GetForcebyIndex(index).Fx;
                    curDy0 = this.fV.GetForcebyIndex(index).Fy;
                    curDx = this.fV.GetForcebyIndex(index).Fx;
                    curDy = this.fV.GetForcebyIndex(index).Fy;
                    if (this.IsTopCos == true)
                    {
                        vp = this.VD.GetVPbyIDandType(tagID, fType);
                        vp.TopologicalConstraint(curDx0, curDy0, 0.001, out curDx, out curDy);
                        this.D[3 * index, 0] = curDx;
                        this.D[3 * index + 1, 0] = curDy;
                    }
                    //纠正拓扑错误
                    curNode.X += curDx;//更新邻近图
                    curNode.Y += curDy;
                    foreach (PolygonObject po in PoList)
                    {
                        foreach (TriNode curPoint in po.PointList)
                        {
                            curPoint.X += curDx;
                            curPoint.Y += curDy;
                        }
                    }
                }
                else
                {
                    this.D[3 * index, 0] = curDx;
                    this.D[3 * index + 1, 0] = curDy;
                }
            }

        }

        /// <summary>
        /// GetPoByID
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        public PolygonObject GetPoByID(int ID, List<PolygonObject> PoList)
        {
            PolygonObject Po = null;
            foreach (PolygonObject CachePo in PoList)
            {
                if (CachePo.ID == ID)
                {
                    Po = CachePo;
                    break;
                }
            }

            return Po;
        }

        /// <summary>
        /// GetPointByID
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        public ProxiNode GetPointByID(int ID, List<ProxiNode> NodeList)
        {
            ProxiNode No = null;
            foreach (ProxiNode CacheNo in NodeList)
            {
                if (CacheNo.ID == ID)
                {
                    No = CacheNo;
                    break;
                }
            }

            return No;
        }

        /// <summary>
        /// 获取PoList
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        public List<PolygonObject> GetPoList(ProxiNode Node, List<PolygonObject> PoList)
        {
            List<PolygonObject> OutPoList = new List<PolygonObject>();
            for (int i = 0; i < Node.TagIds.Count; i++)
            {
                PolygonObject CachePo = this.GetPoByID(Node.TagIds[i], PoList);
                OutPoList.Add(CachePo);
            }

            return OutPoList;
        }

        /// <summary>
        /// 直接采用受力更新坐标位置
        /// </summary>
        private void UpdataCoordsforPGbyForce_Group()
        {
            foreach (ProxiNode curNode in this.ProxiGraph.NodeList)
            {
                int index = curNode.ID;
                int tagID = curNode.TagID;
                FeatureType fType = curNode.FeatureType;
                VoronoiPolygon vp = null;
                Force force = fV.GetForcebyIndex(index);
                if (fType == FeatureType.PolygonType)
                {
                    PolygonObject po = this.Map.GetObjectbyID(tagID, fType) as PolygonObject;
                    double curDx0 = 0;
                    double curDy0 = 0;
                    double curDx = 0;
                    double curDy = 0;
                    if (force != null)
                    {

                        curDx0 = this.fV.GetForcebyIndex(index).Fx;
                        curDy0 = this.fV.GetForcebyIndex(index).Fy;
                        curDx = this.fV.GetForcebyIndex(index).Fx;
                        curDy = this.fV.GetForcebyIndex(index).Fy;
                        if (this.IsTopCos == true)
                        {
                            vp = this.VD.GetVPbyIDandType(tagID, fType);
                            vp.TopologicalConstraint(curDx0, curDy0, 0.001, out curDx, out curDy);
                            this.D[3 * index, 0] = curDx;
                            this.D[3 * index + 1, 0] = curDy;
                        }
                        //纠正拓扑错误
                        curNode.X += curDx;
                        curNode.Y += curDy;

                        foreach (TriNode curPoint in po.PointList)
                        {
                            curPoint.X += curDx;
                            curPoint.Y += curDy;
                        }
                    }
                    else
                    {
                        this.D[3 * index, 0] = curDx;
                        this.D[3 * index + 1, 0] = curDy;
                    }
                }
                else if (fType == FeatureType.Group)//如果是分组
                {
                    GroupofMapObject group = GroupofMapObject.GetGroup(tagID, this.Groups);
                    double curDx0 = 0;
                    double curDy0 = 0;
                    double curDx = 0;
                    double curDy = 0;
                    if (force != null)
                    {
                        curDx0 = this.fV.GetForcebyIndex(index).Fx;
                        curDy0 = this.fV.GetForcebyIndex(index).Fy;
                        curDx = this.fV.GetForcebyIndex(index).Fx;
                        curDy = this.fV.GetForcebyIndex(index).Fy;
                        if (this.IsTopCos == true)
                        {
                            foreach (PolygonObject obj in group.ListofObjects)
                            {
                                tagID = obj.ID;
                                fType = obj.FeatureType;
                                vp = this.VD.GetVPbyIDandType(tagID, fType);
                                vp.TopologicalConstraint(curDx0, curDx0, 0.001, out curDx, out curDy);
                                curDx0 = curDx;
                                curDy0 = curDy;
                            }
                            this.D[3 * index, 0] = curDx;
                            this.D[3 * index + 1, 0] = curDy;
                        }
                        //纠正拓扑错误
                        curNode.X += curDx;
                        curNode.Y += curDy;
                        foreach (PolygonObject obj in group.ListofObjects)
                        {
                            foreach (TriNode curPoint in obj.PointList)
                            {
                                curPoint.X += curDx;
                                curPoint.Y += curDy;
                            }
                        }
                    }
                    else
                    {
                        this.D[3 * index, 0] = curDx;
                        this.D[3 * index + 1, 0] = curDy;
                    }
                }
            }
        }

        /// <summary>
        /// 求矩阵的逆
        /// </summary>
        /// <param name="dMatrix"></param>
        /// <param name="Level"></param>
        /// <returns></returns>
        private double[,] ReverseMatrix(double[,] dMatrix, int Level)
        {

            double dMatrixValue = MatrixValue(dMatrix, Level);

            if (dMatrixValue == 0) return null;



            double[,] dReverseMatrix = new double[Level, 2 * Level];

            double x, c;

            // Init Reverse matrix

            for (int i = 0; i < Level; i++)
            {

                for (int j = 0; j < 2 * Level; j++)
                {

                    if (j < Level)

                        dReverseMatrix[i, j] = dMatrix[i, j];

                    else

                        dReverseMatrix[i, j] = 0;

                }



                dReverseMatrix[i, Level + i] = 1;

            }



            for (int i = 0, j = 0; i < Level && j < Level; i++, j++)
            {

                if (dReverseMatrix[i, j] == 0)
                {

                    int m = i;

                    for (; dMatrix[m, j] == 0; m++) ;

                    if (m == Level)

                        return null;

                    else
                    {

                        // Add i-row with m-row

                        for (int n = j; n < 2 * Level; n++)

                            dReverseMatrix[i, n] += dReverseMatrix[m, n];

                    }

                }



                // Format the i-row with "1" start

                x = dReverseMatrix[i, j];

                if (x != 1)
                {

                    for (int n = j; n < 2 * Level; n++)

                        if (dReverseMatrix[i, n] != 0)

                            dReverseMatrix[i, n] /= x;

                }



                // Set 0 to the current column in the rows after current row

                for (int s = Level - 1; s > i; s--)
                {

                    x = dReverseMatrix[s, j];

                    for (int t = j; t < 2 * Level; t++)

                        dReverseMatrix[s, t] -= (dReverseMatrix[i, t] * x);

                }

            }



            // Format the first matrix into unit-matrix

            for (int i = Level - 2; i >= 0; i--)
            {

                for (int j = i + 1; j < Level; j++)

                    if (dReverseMatrix[i, j] != 0)
                    {

                        c = dReverseMatrix[i, j];

                        for (int n = j; n < 2 * Level; n++)

                            dReverseMatrix[i, n] -= (c * dReverseMatrix[j, n]);

                    }

            }



            double[,] dReturn = new double[Level, Level];

            for (int i = 0; i < Level; i++)

                for (int j = 0; j < Level; j++)

                    dReturn[i, j] = dReverseMatrix[i, j + Level];

            return dReturn;

        }

        /// <summary>
        /// 存储MatrixValue
        /// </summary>
        /// <param name="MatrixList"></param>
        /// <param name="Level"></param>
        /// <returns></returns>
        private double MatrixValue(double[,] MatrixList, int Level)
        {

            double[,] dMatrix = new double[Level, Level];

            for (int i = 0; i < Level; i++)

                for (int j = 0; j < Level; j++)

                    dMatrix[i, j] = MatrixList[i, j];

            double c, x;

            int k = 1;

            for (int i = 0, j = 0; i < Level && j < Level; i++, j++)
            {

                if (dMatrix[i, j] == 0)
                {

                    int m = i;

                    for (; dMatrix[m, j] == 0; m++) ;

                    if (m == Level)

                        return 0;

                    else
                    {

                        // Row change between i-row and m-row

                        for (int n = j; n < Level; n++)
                        {

                            c = dMatrix[i, n];

                            dMatrix[i, n] = dMatrix[m, n];

                            dMatrix[m, n] = c;

                        }



                        // Change value pre-value

                        k *= (-1);

                    }

                }



                // Set 0 to the current column in the rows after current row

                for (int s = Level - 1; s > i; s--)
                {

                    x = dMatrix[s, j];

                    for (int t = j; t < Level; t++)

                        dMatrix[s, t] -= dMatrix[i, t] * (x / dMatrix[i, j]);

                }

            }



            double sn = 1;

            for (int i = 0; i < Level; i++)
            {

                if (dMatrix[i, i] != 0)

                    sn *= dMatrix[i, i];

                else

                    return 0;

            }

            return k * sn;

        }
        #endregion

        #region Bader Truss移位
        /// <summary>
        /// 构造函数-从建筑物群邻近图构建Beams模型
        /// </summary>
        /// <param name="proxiGraph">邻近图</param>
        /// <param name="map">地图对象</param>
        /// <param name="e">弹性模量</param>
        /// <param name="i">惯性力矩</param>
        /// <param name="a">横截面积</param>
        /// <param name="conflictList">邻近冲突列表</param>
        public AlgBeams(SMap map, double e, double i, double a)
        {
            this.E = e;
            this.I = i;
            this.A = a;
            this.Map = map;
            // this.ConflictList = conflictList;
            this.IsTopCos = false;
        }


        /// <summary>
        /// 邻近图的移位算法实现
        /// </summary>
        public void DoDispaceBader(double r, int times, double scale, double disThreshold)
        {
            DataSet ds = new DataSet();
            //创建一个表
            DataTable tableConflict = new DataTable();
            tableConflict.TableName = "ConflictsCount";
            tableConflict.Columns.Add("ID", typeof(int));
            tableConflict.Columns.Add("ConflictCount", typeof(int));

            #region 三角网+骨架线+邻近图+冲突检测
            DelaunayTin dt = new DelaunayTin(this.Map.TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);

            ConvexNull cn = new ConvexNull(dt.TriNodeList);
            cn.CreateConvexNull();

            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(this.Map.PolylineList, this.Map.PolygonList);

            Triangle.WriteID(dt.TriangleList);
            TriEdge.WriteID(dt.TriEdgeList);

            AuxStructureLib.Skeleton ske = new AuxStructureLib.Skeleton(cdt, this.Map);
            ske.TranverseSkeleton_Segment_NT_DeleteIntraSkel();

            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGraphfrmSkeletonBuildings(this.Map, ske);
            #endregion

            #region 生成最小生成树+构建Truss
            MSTGraph mst = new MSTGraph();
            mst.CreateAdjMatrixfrmProximityGraph(pg);
            mst.AlgPrim(pg);
            ProxiGraph mstpg = mst.MST;

            ////删除街道与建筑物之间的边
            //List<ProxiEdge> LPedgeList = new List<ProxiEdge>();
            //foreach (ProxiEdge edge in mstpg.EdgeList)
            //{
            //    if (edge.Node1.FeatureType == FeatureType.PolylineType || edge.Node2.FeatureType == FeatureType.PolylineType)
            //    {
            //        LPedgeList.Add(edge);
            //    }
            //}

            //foreach (ProxiEdge edge in LPedgeList)
            //{
            //    mstpg.EdgeList.Remove(edge);
            //}

            AlgDijkstra dijkstra = new AlgDijkstra();
            dijkstra.CreateAdjMatrixfrmProximityGraph(mstpg);

            int n = mstpg.NodeList.Count;
            foreach (ProxiEdge edge in pg.EdgeList)
            {
                //if (edge.Node1.FeatureType != FeatureType.PolylineType && edge.Node2.FeatureType != FeatureType.PolylineType)
                //{
                double dis = edge.NearestEdge.NearestDistance;
                if (dis <= (disThreshold * 10 * scale) / 1000)
                {
                    double w = dijkstra.AdjMatrix[edge.Node1.ID, edge.Node2.ID];
                    if (w == double.PositiveInfinity)
                    {
                        double minDis = double.PositiveInfinity;
                        dijkstra.OneToOneSP(edge.Node1.ID, edge.Node2.ID, n, double.PositiveInfinity, out minDis);
                        if (dis <= (4.0 / 7.0) * minDis)
                        {
                            mstpg.EdgeList.Add(edge);
                        }
                    }
                }
                //}
            }
            #endregion

            #region 求1+rK
            //计算刚度矩阵
            bM = new BeamsStiffMatrix(mstpg, E, I, A);
            this.K = bM.Matrix_K;
            Matrix identityMatrix = new Matrix(3 * n, 3 * n);
            this.D = new Matrix(3 * n, 1);
            for (int i = 0; i < 3 * n; i++)
            {
                this.D[i, 0] = 0.0;
                for (int j = 0; j < 3 * n; j++)
                {
                    this.K[i, j] *= r;
                    if (i == j)
                        identityMatrix[i, j] = 1.0;
                    else
                        identityMatrix[i, j] = 0;
                }
            }
            this.K = identityMatrix + this.K;
            #endregion
            AuxStructureLib.ConflictLib.ConflictDetector cd = null;

            for (int i = 0; i < times; i++)
            {
                #region 三角网+骨架线+邻近图+冲突检测
                dt = new DelaunayTin(this.Map.TriNodeList);
                dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);

                cn = new ConvexNull(dt.TriNodeList);
                cn.CreateConvexNull();

                cdt = new ConsDelaunayTin(dt);
                cdt.CreateConsDTfromPolylineandPolygon(this.Map.PolylineList, this.Map.PolygonList);

                Triangle.WriteID(dt.TriangleList);
                TriEdge.WriteID(dt.TriEdgeList);

                ske = new AuxStructureLib.Skeleton(cdt, this.Map);
                ske.TranverseSkeleton_Segment_NT_DeleteIntraSkel();

                pg = new ProxiGraph();
                pg.CreateProxiGraphfrmSkeletonBuildings(this.Map, ske);

                //pg.WriteProxiGraph2Shp(strPath, @"PG"+i.ToString(), esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);

                cd = new AuxStructureLib.ConflictLib.ConflictDetector();
                cd.Skel_arcList = ske.Skeleton_ArcList;
                cd.PG = pg;
                cd.threshold = disThreshold;
                cd.targetScale = scale;
                cd.DetectConflictByPG();
                cd.OutputConflicts(strPath, i);


                DataRow dr = tableConflict.NewRow();
                dr[0] = i;
                dr[1] = cd.CountConflicts();
                tableConflict.Rows.Add(dr);
                #endregion

                this.ProxiGraph = pg;
                fV = new BeamsForceVector(this.ProxiGraph);
                //求吸引力-2014-3-20所用
                fV.isDragForce = false;
                fV.CreateForceVectorfrmConflictforBader(cd.ConflictList, mstpg);
                // fV.Create_WriteForceVector2Shp(strPath, @"ForceVector", esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);

                // this.ReCoverCoordsforMST(mstpg);
                this.F = fV.Vector_F;
                this.SetBoundPointParamforMST(mstpg);//设置边界条件
                this.F = this.D + r * this.F;        //d(t-1)+rf(t-1)
                this.D = this.K.Inverse() * this.F;

                if (!IsDisplaceValueNaN())
                    break;

                this.UpdataCoordsforMST(mstpg);      //更新坐标

                OutputDisplacement(strPath, "Displacement" + i.ToString() + ".txt", mstpg);
            }

            TXTHelper.ExportToTxt(tableConflict, strPath + @"\ConflictsCount.txt");

            //输出受力点出的受力与当前移位值
            //  this.OutputDisplacementandForces(fV.ForceList);
            //输出各点的移位总和
            //   OutputTotalDisplacementforProxmityGraph(this.OriginalGraph, this.ProxiGraph, this.Map);
        }

        public void DoDispaceBader1(double r, int times, double scale, double disThreshold, ISpatialReference prj)
        {
            #region 三角网+骨架线+邻近图+冲突检测
            DelaunayTin dt = new DelaunayTin(this.Map.TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);

            ConvexNull cn = new ConvexNull(dt.TriNodeList);
            cn.CreateConvexNull();

            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(this.Map.PolylineList, this.Map.PolygonList);

            Triangle.WriteID(dt.TriangleList);
            TriEdge.WriteID(dt.TriEdgeList);

            AuxStructureLib.Skeleton ske = new AuxStructureLib.Skeleton(cdt, this.Map);
            ske.TranverseSkeleton_Segment_NT_DeleteIntraSkel();

            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGraphfrmSkeletonBuildings(this.Map, ske);
            #endregion

            #region 生成最小生成树+构建Truss
            MSTGraph mst = new MSTGraph();
            mst.CreateAdjMatrixfrmProximityGraph(pg);
            mst.AlgPrim(pg);
            ProxiGraph mstpg = mst.MST;


            pg.WriteProxiGraph2Shp(strPath, @"Proximity", prj);
            mstpg.WriteProxiGraph2Shp(strPath, @"MSTPg", prj);

            AlgDijkstra dijkstra = new AlgDijkstra();
            dijkstra.CreateAdjMatrixfrmProximityGraph(mstpg);

            int n = mstpg.NodeList.Count;
            foreach (ProxiEdge edge in pg.EdgeList)
            {
                //if (edge.Node1.FeatureType != FeatureType.PolylineType && edge.Node2.FeatureType != FeatureType.PolylineType)
                //{
                double dis = edge.NearestEdge.NearestDistance;
                if (dis <= (disThreshold * 10 * scale) / 1000)
                {
                    double w = dijkstra.AdjMatrix[edge.Node1.ID, edge.Node2.ID];
                    if (w == double.PositiveInfinity)
                    {
                        double minDis = double.PositiveInfinity;
                        dijkstra.OneToOneSP(edge.Node1.ID, edge.Node2.ID, n, double.PositiveInfinity, out minDis);
                        if (dis <= (4.0 / 7.0) * minDis)
                        {
                            mstpg.EdgeList.Add(edge);
                        }
                    }
                }
                //}
            }
            #endregion

            #region 求K
            //计算刚度矩阵
            bM = new BeamsStiffMatrix(mstpg, E, I, A);
            this.K = bM.Matrix_K;
            #endregion

            AuxStructureLib.ConflictLib.ConflictDetector cd = null;

            for (int i = 0; i < times; i++)
            {
                #region 三角网+骨架线+邻近图+冲突检测
                dt = new DelaunayTin(this.Map.TriNodeList);
                dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);

                cn = new ConvexNull(dt.TriNodeList);
                cn.CreateConvexNull();

                cdt = new ConsDelaunayTin(dt);
                cdt.CreateConsDTfromPolylineandPolygon(this.Map.PolylineList, this.Map.PolygonList);

                Triangle.WriteID(dt.TriangleList);
                TriEdge.WriteID(dt.TriEdgeList);

                ske = new AuxStructureLib.Skeleton(cdt, this.Map);
                ske.TranverseSkeleton_Segment_NT_DeleteIntraSkel();

                pg = new ProxiGraph();
                pg.CreateProxiGraphfrmSkeletonBuildings(this.Map, ske);

                cd = new AuxStructureLib.ConflictLib.ConflictDetector();
                cd.Skel_arcList = ske.Skeleton_ArcList;
                cd.PG = pg;
                cd.threshold = disThreshold;
                cd.targetScale = scale;
                cd.DetectConflictByPG();
                #endregion
                this.ProxiGraph = pg;
                fV = new BeamsForceVector(this.ProxiGraph);
                //求吸引力-2014-3-20所用
                fV.isDragForce = false;
                fV.CreateForceVectorfrmConflict(cd.ConflictList);
                // fV.Create_WriteForceVector2Shp(strPath, @"ForceVector", esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
                this.F = fV.Vector_F;
                this.SetBoundPointParamforPG();//设置边界条件
                this.D = this.K.Inverse() * this.F;
                UpdataCoordsforPG();      //更新坐标
            }

            //输出受力点出的受力与当前移位值
            //  this.OutputDisplacementandForces(fV.ForceList);
            //输出各点的移位总和
            //   OutputTotalDisplacementforProxmityGraph(this.OriginalGraph, this.ProxiGraph, this.Map);
        }
        #endregion

        #region forBader
        /// <summary>
        /// 设置邻近图的边界点2014-3-2
        /// </summary>
        private void SetBoundPointParamforMST(ProxiGraph mstPg)
        {
            Force curForce = null;
            foreach (ProxiNode curNode in mstPg.NodeList)
            {
                if (curNode.FeatureType == FeatureType.PolylineType)
                {
                    int index = curNode.ID;
                    this.SetBoundPointParamsBigNumber(index, 0, 0);
                }
            }
        }


        /// <summary>
        /// 更新坐标位置
        /// </summary>
        private void ReCoverCoordsforMST(ProxiGraph mstPg)
        {
            foreach (ProxiNode curNode in mstPg.NodeList)
            {
                int index = curNode.ID;
                int tagID = curNode.TagID;
                FeatureType fType = curNode.FeatureType;
                VoronoiPolygon vp = null;

                if (fType == FeatureType.PolygonType)
                {

                    PolygonObject po = this.Map.GetObjectbyID(tagID, fType) as PolygonObject;

                    double curDx0 = this.D[3 * index, 0];
                    double curDy0 = this.D[3 * index + 1, 0];
                    double curDx = this.D[3 * index, 0];
                    double curDy = this.D[3 * index + 1, 0];

                    if (this.IsTopCos == true)
                    {
                        vp = this.VD.GetVPbyIDandType(tagID, fType);
                        vp.TopologicalConstraint(curDx0, curDy0, 0.001, out curDx, out curDy);
                        curDx = this.D[3 * index, 0] = curDx;
                        curDy = this.D[3 * index + 1, 0] = curDy;
                    }

                    //纠正拓扑错误
                    curNode.X -= curDx;
                    curNode.Y -= curDy;

                    foreach (TriNode curPoint in po.PointList)
                    {
                        curPoint.X -= curDx;
                        curPoint.Y -= curDy;
                    }
                }


            }
        }
        /// <summary>
        /// 判断结果是否为非法值
        /// </summary>
        /// <returns></returns>
        private bool IsDisplaceValueNaN()
        {
            int rowCount = this.D.Row;
            for (int i = 0; i < rowCount; i++)
            {
                double curV = this.D[i, 0];
                if (double.IsNaN(curV))
                {
                    return false;
                }
            }
            return true;
        }


        /// <summary>
        /// 更新坐标位置
        /// </summary>
        private void UpdataCoordsforMST(ProxiGraph mstPg)
        {
            foreach (ProxiNode curNode in mstPg.NodeList)
            {
                int index = curNode.ID;
                int tagID = curNode.TagID;
                FeatureType fType = curNode.FeatureType;
                VoronoiPolygon vp = null;

                if (fType == FeatureType.PolygonType)
                {

                    PolygonObject po = this.Map.GetObjectbyID(tagID, fType) as PolygonObject;

                    double curDx0 = this.D[3 * index, 0];
                    double curDy0 = this.D[3 * index + 1, 0];
                    double curDx = this.D[3 * index, 0];
                    double curDy = this.D[3 * index + 1, 0];

                    if (this.IsTopCos == true)
                    {
                        vp = this.VD.GetVPbyIDandType(tagID, fType);
                        vp.TopologicalConstraint(curDx0, curDy0, 0.001, out curDx, out curDy);
                        curDx = this.D[3 * index, 0] = curDx;
                        curDy = this.D[3 * index + 1, 0] = curDy;
                    }

                    //纠正拓扑错误
                    curNode.X += curDx;
                    curNode.Y += curDy;

                    foreach (TriNode curPoint in po.PointList)
                    {
                        curPoint.X += curDx;
                        curPoint.Y += curDy;
                    }
                }


            }
        }


        /// <summary>
        /// 输出冲突到TXT文件
        /// </summary>
        /// <param name="strPath"></param>
        /// <param name="iteraID"></param>
        public void OutputDisplacement(string strPath, string fileName, ProxiGraph mstPg)
        {
            DataSet ds = new DataSet();
            //创建一个表
            DataTable tableforce = new DataTable();
            tableforce.TableName = "Displecement";
            tableforce.Columns.Add("ID", typeof(int));
            tableforce.Columns.Add("X", typeof(double));
            tableforce.Columns.Add("Y", typeof(double));
            tableforce.Columns.Add("Dis", typeof(double));

            foreach (ProxiNode curNode in mstPg.NodeList)
            {
                int index = curNode.ID;
                int tagID = curNode.TagID;
                FeatureType fType = curNode.FeatureType;
                VoronoiPolygon vp = null;

                if (fType == FeatureType.PolygonType)
                {

                    PolygonObject po = this.Map.GetObjectbyID(tagID, fType) as PolygonObject;

                    double curDx = this.D[3 * index, 0];
                    double curDy = this.D[3 * index + 1, 0];

                    DataRow dr = tableforce.NewRow();
                    dr[0] = index;
                    dr[1] = curDx;
                    dr[2] = curDy;
                    dr[3] = Math.Sqrt(curDx * curDx + curDy * curDy);
                    tableforce.Rows.Add(dr);
                }
            }
            TXTHelper.ExportToTxt(tableforce, strPath + @"\" + fileName);
        }
        #endregion

        #region 其它
        public override void DoDispace()
        {
            throw new NotImplementedException();
        }
        public override void DoDispaceIterate()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
