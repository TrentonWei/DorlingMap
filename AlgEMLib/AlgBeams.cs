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
    public class AlgBeams:AlgEM
    {
        //public double PAT = 0.6;
        public double E=10000;
        public double I=2;
        public double A=2;
        public string strPath = "";

        
        BeamsStiffMatrix bM=null;
        BeamsForceVector fV = null;
       

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
        public AlgBeams(ProxiGraph proxiGraph, SMap map,double e, double i, double a, double disThreshold)
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
            double disThresholdLP,double disThresholdPP)
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
            bM=new BeamsStiffMatrix(this.ProxiGraph,E,I,A);
            this.K = bM.Matrix_K;
            fV = new BeamsForceVector(this.ProxiGraph);
            fV.isDragForce = this.isDragF;
            fV.CreateForceVectorfrmGraph(this.DisThresholdLP,this.DisThresholdPP);
            fV.Create_WriteForceVector2Shp(strPath, @"ForceVector", esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
            this.F = fV.Vector_F;

            SetBoundPointParams();//设置边界条件

            this.D= this.K.Inverse() * this.F;

            double MaxD;
            double MaxF;
            int indexMaxD = -1;
            int indexMaxF = -1;

            StaticDis(out MaxD,out MaxF,out indexMaxD,out indexMaxF);

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
        private void StaticDis(out double MaxD,out double MaxF,out int indexMaxD,out int indexMaxF)
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
        private void StaticDisforPGNewDF(out double MaxFD, out double MaxD, out double MaxDF,out double MaxF, out int indexMaxD, out int indexMaxF)
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


        ///// <summary>
        ///// 设置边界点的受力
        ///// </summary>
        ///// <param name="index">顶点索引号</param>
        //private void SetBoundPointParamsForce(int index, float fx, float fy)
        //{
        //    int r1 = index * 2;

        //    for (int i = 0; i < this._Fx.Row; i++)
        //    {
        //        if (i == r1)
        //        {
        //            this._Fx[r1, 0] = fx;
        //            this._Fy[r1, 0] = fy;
        //        }
        //        else
        //        {
        //            this._Fx[i, 0] = this._Fx[i, 0] - fx * _K[i, r1];
        //            this._Fy[i, 0] = this._Fy[i, 0] - fy * _K[i, r1];
        //        }
        //    }
        //    for (int i = 0; i < _K.Col; i++)
        //    {
        //        if (i == r1)
        //        {
        //            _K[r1, i] = 1;//对角线上元素赋值为1
        //        }
        //        else
        //        {
        //            _K[r1, i] = 0;//其他地方赋值为0
        //        }
        //    }
        //}





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
                curPoint.Y += this.D[3 * index+1, 0];
            }
            
        }

        ///// <summary>
        ///// 迭代
        ///// </summary>
        //public override void DoDispaceIterate()
        //{
        //    ComMatrix_K();       //求刚度矩阵

        //    if (!MakeForceVector())   //建立并计算力向量
        //    {
        //        return;
        //    }
        //    #region 求1+rK
        //    int n = this._K.Col;
        //    this._d = new Matrix(n, 1);
        //    Matrix identityMatrix = new Matrix(n, n);
        //    for (int i = 0; i < n; i++)
        //    {
        //        this._F[i, 0] *= r;
        //        this._d[i, 0] = 0.0;
        //        for (int j = 0; j < n; j++)
        //        {
        //            this._K[i, j] *= r;
        //            if (i == j)
        //                identityMatrix[i, j] = 1.0;
        //            else
        //                identityMatrix[i, j] = 0;
        //        }
        //    }
        //    this._K = identityMatrix + this._K;
        //    #endregion
        //    this.SetBoundPointParams();

        //    for (int i = 0; i < time; i++)
        //    {
        //        this._F = this._d + r * this._F;

        //        this.SetBoundPointParamsForce();
        //        ComDisplace();
        //        this.UpdataCoords();
        //        CreategeoSetFromRes();
        //        if (!MakeForceVector())   //建立并计算力向量
        //        {
        //            return;
        //        }
        //    }
        //    StaticDis();
        //}

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
            if (this.F[3 * (n - 1), 0] == 0 && this.F[3 * (n - 1)+1, 0] == 0)
                this.SetBoundPointParamsBigNumber(n - 1, 0, 0);
               // SetBoundPointParamsOld(n - 1, 0, 0);
        }
        /// <summary>
        /// 设置边界条件
        /// </summary>
        /// <param name="index"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        private void SetBoundPointParamsOld(int index, double dx,double dy)
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
                this.D = new Matrix(this.ProxiGraph.NodeList.Count*3, 1);
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



        ///// <summary>
        ///// 输出移位值和力
        ///// </summary>
        ///// <param name="ForceList">力列表</param>
        //private void OutputDisplacementandForces(List<Force> ForceList)
        //{
        //    if (ForceList == null || ForceList.Count == 0)
        //        return;
        //    DataSet ds = new DataSet();
        //    //创建一个表
        //    DataTable tableforce = new DataTable();
        //    tableforce.TableName = "DispalcementandForces";
        //    tableforce.Columns.Add("ID", typeof(int));
        //    tableforce.Columns.Add("TagID", typeof(int));
        //    tableforce.Columns.Add("F", typeof(double));
        //    tableforce.Columns.Add("D", typeof(double));
        //    tableforce.Columns.Add("Fx", typeof(double));
        //    tableforce.Columns.Add("Dx", typeof(double));
        //    tableforce.Columns.Add("Fy", typeof(double));
        //    tableforce.Columns.Add("Dy", typeof(double));

        //    foreach (Force force in ForceList)
        //    {
        //        double dx = this.D[3 * force.ID, 0];
        //        double dy = this.D[3 * force.ID + 1, 0];
        //        double d = Math.Sqrt(dx * dx + dy * dy);
        //        DataRow dr = tableforce.NewRow();
        //        dr[0] = force.ID;
        //      //  dr[1] = force.SID;
        //        dr[2] = force.F;
        //        dr[3] = d;
        //        dr[4] = force.Fx;
        //        dr[5] = dx;
        //        dr[6] = force.Fy;
        //        dr[7] = dy;
        //        tableforce.Rows.Add(dr);
        //    }
        //    TXTHelper.ExportToTxt(tableforce, this.strPath + @"-DandF.txt");
        //}

        ///// <summary>
        ///// 统计移位值
        ///// </summary>
        //private void StaticDis(out double MaxD, out double MaxF, out int indexMaxD, out int indexMaxF)
        //{
        //    int n = this.D.Row / 3;
        //    MaxD = -1;
        //    MaxF = -1;
        //    double curD;
        //    indexMaxD = -1;
        //    indexMaxF = -1;

        //    for (int i = 0; i < n; i++)
        //    {
        //        // curD = Math.Sqrt(this.D[3 * i, 0] * this.D[3 * i, 0] + this.D[3 * i + 1, 0] * this.D[3 * i + 1, 0]);

        //        // if (curD > MaxD)
        //        // {
        //        //     indexMaxD = i;
        //        //      MaxD = curD;
        //        //   }


        //        if (this.fV.ForceList[i].F > MaxF)
        //        {
        //            indexMaxF = i;
        //            MaxF = fV.ForceList[i].F;
        //        }
        //    }

        //    MaxD = Math.Sqrt(this.D[3 * indexMaxF, 0] * this.D[3 * indexMaxF, 0] + this.D[3 * indexMaxF + 1, 0] * this.D[3 * indexMaxF + 1, 0]);
        //}




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

                    /*  this.F[r1, 0] = 0;
                      this.F[r2, 0] = 0;
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
                      }*/
                }
            }
        }

        /// <summary>
        /// 设置邻近图的边界点2014-3-2
        /// </summary>
        //private void SetBoundPointParamforPG()
        //{
        //    Force curForce = null;
        //    foreach (ProxiNode curNode in this.ProxiGraph.NodeList)
        //    {
        //        if (curNode.FeatureType == FeatureType.PolylineType)
        //        {
        //            int index = curNode.ID;
        //            this.SetBoundPointParamsBigNumber(index, 0, 0);

        //            /* 
        //             *                       
        //             * int  r1 = index * 3;
        //           int r2 = index * 3 + 1;
        //          int  r3 = index * 3 + 2;
        //             * this.F[r1, 0] = 0;
        //              this.F[r2, 0] = 0;
        //              this.F[r3, 0] = 0;

        //              for (int i = 0; i < K.Col; i++)
        //              {
        //                  if (i == r1)
        //                  {
        //                      K[r1, i] = 1;//对角线上元素赋值为1
        //                  }
        //                  else
        //                  {
        //                      K[r1, i] = 0;//其他地方赋值为0
        //                  }

        //                  if (i == r2)
        //                  {
        //                      K[r2, i] = 1;//对角线上元素赋值为1
        //                  }
        //                  else
        //                  {
        //                      K[r2, i] = 0;//其他地方赋值为0
        //                  }

        //                  if (i == r3)
        //                  {
        //                      K[r3, i] = 1;//对角线上元素赋值为1
        //                  }
        //                  else
        //                  {
        //                      K[r3, i] = 0;//其他地方赋值为0
        //                  }
        //              }*/
        //        }
        //    }
        //}

        ///// <summary>
        ///// 设置邻近图的边界点2014-3-2
        ///// </summary>
        //private void SetBoundPointParamforPG_BC()
        //{
        //    foreach (ProxiNode curNode in this.ProxiGraph.NodeList)
        //    {
        //        int index = curNode.ID;
        //        if (curNode.FeatureType == FeatureType.PolylineType)
        //        {
                
        //            this.SetBoundPointParamsBigNumber(index, 0, 0);
        //        }
        //        else if (curNode.FeatureType == FeatureType.PolygonType)
        //        {
        //            Force force = this.fV.GetForcebyIndex(index);
        //            if (force != null)
        //            {
        //                double dx = force.Fx;
        //                double dy = force.Fy;
        //                this.SetBoundPointParamsBigNumber(index, dx, dy);
        //            }
        //        }
        //    }
        //}


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
                VoronoiPolygon vp=null;

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
                    double curDx0 =0;
                    double curDy0 = 0;
                    double curDx =0;
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


        ///// <summary>
        ///// 迭代
        ///// </summary>
        //public override void DoDispaceIterate()
        //{
        //    ComMatrix_K();       //求刚度矩阵

        //    if (!MakeForceVector())   //建立并计算力向量
        //    {
        //        return;
        //    }
        //    #region 求1+rK
        //    int n = this._K.Col;
        //    this._d = new Matrix(n, 1);
        //    Matrix identityMatrix = new Matrix(n, n);
        //    for (int i = 0; i < n; i++)
        //    {
        //        this._F[i, 0] *= r;
        //        this._d[i, 0] = 0.0;
        //        for (int j = 0; j < n; j++)
        //        {
        //            this._K[i, j] *= r;
        //            if (i == j)
        //                identityMatrix[i, j] = 1.0;
        //            else
        //                identityMatrix[i, j] = 0;
        //        }
        //    }
        //    this._K = identityMatrix + this._K;
        //    #endregion
        //    this.SetBoundPointParams();

        //    for (int i = 0; i < time; i++)
        //    {
        //        this._F = this._d + r * this._F;

        //        this.SetBoundPointParamsForce();
        //        ComDisplace();
        //        this.UpdataCoords();
        //        CreategeoSetFromRes();
        //        if (!MakeForceVector())   //建立并计算力向量
        //        {
        //            return;
        //        }
        //    }
        //    StaticDis();
        //}

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
        public AlgBeams(SMap map,double e, double i, double a, List<ConflictBase> conflictList)
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

            if (MaxF > 0 && indexMaxF>=0)
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
                if (node.Point.IsBoundaryPoint==true)
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

            for (int i = 0; i <Map.TriNodeList.Count; i++)
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
        private void OutputTotalDisplacementforProxmityGraph(ProxiGraph orginal,ProxiGraph current,SMap map)
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

            foreach(PolygonObject obj in map.PolygonList)
            {
                int id = obj.ID;
                ProxiNode oNode = orginal.GetNodebyTagIDandType(id, FeatureType.PolygonType);
                ProxiNode cNode = current.GetNodebyTagIDandType(id, FeatureType.PolygonType);
                if (oNode != null && cNode != null)
                {
                    double dx = cNode.X - oNode.X;
                    double dy = cNode.Y - oNode.Y;
                    double d=Math.Sqrt(dx * dx + dy * dy);
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

            fV.CreateForceVectorfrmConflict_Group(ConflictList,Groups);

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
        /// </summary>
        public void DoDisplacePgDorling(SMap pMap)
        {
            fV = new BeamsForceVector(this.ProxiGraph);
            //求吸引力-2014-3-20所用
            fV.OrigialProxiGraph = this.OriginalGraph;
            fV.RMSE = this.PAT * this.Scale / 1000;
            fV.isDragForce = this.isDragF;
            fV.CreateForceVectorForDorling(pMap.PolygonList);
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

                UpdataCoordsforPGDorling();      //更新坐标
            }

            this.OutputDisplacementandForces(fV.ForceList);

            if (MaxF <=  0.01)
            {
                this.isContinue = false;
            }
        }

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
            fV.CreateForceVectorfrmConflict_Group(ConflictList,this.Groups);
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



        ///// <summary>
        ///// 输出移位值和力
        ///// </summary>
        ///// <param name="ForceList">力列表</param>
        //private void OutputDisplacementandForces(List<Force> ForceList)
        //{
        //    if (ForceList == null || ForceList.Count == 0)
        //        return;
        //    DataSet ds = new DataSet();
        //    //创建一个表
        //    DataTable tableforce = new DataTable();
        //    tableforce.TableName = "DispalcementandForces";
        //    tableforce.Columns.Add("ID", typeof(int));
        //    tableforce.Columns.Add("TagID", typeof(int));
        //    tableforce.Columns.Add("F", typeof(double));
        //    tableforce.Columns.Add("D", typeof(double));
        //    tableforce.Columns.Add("Fx", typeof(double));
        //    tableforce.Columns.Add("Dx", typeof(double));
        //    tableforce.Columns.Add("Fy", typeof(double));
        //    tableforce.Columns.Add("Dy", typeof(double));

        //    foreach (Force force in ForceList)
        //    {
        //        double dx = this.D[3 * force.ID, 0];
        //        double dy = this.D[3 * force.ID + 1, 0];
        //        double d = Math.Sqrt(dx * dx + dy * dy);
        //        DataRow dr = tableforce.NewRow();
        //        dr[0] = force.ID;
        //      //  dr[1] = force.SID;
        //        dr[2] = force.F;
        //        dr[3] = d;
        //        dr[4] = force.Fx;
        //        dr[5] = dx;
        //        dr[6] = force.Fy;
        //        dr[7] = dy;
        //        tableforce.Rows.Add(dr);
        //    }
        //    TXTHelper.ExportToTxt(tableforce, this.strPath + @"-DandF.txt");
        //}

        ///// <summary>
        ///// 统计移位值
        ///// </summary>
        //private void StaticDis(out double MaxD, out double MaxF, out int indexMaxD, out int indexMaxF)
        //{
        //    int n = this.D.Row / 3;
        //    MaxD = -1;
        //    MaxF = -1;
        //    double curD;
        //    indexMaxD = -1;
        //    indexMaxF = -1;

        //    for (int i = 0; i < n; i++)
        //    {
        //        // curD = Math.Sqrt(this.D[3 * i, 0] * this.D[3 * i, 0] + this.D[3 * i + 1, 0] * this.D[3 * i + 1, 0]);

        //        // if (curD > MaxD)
        //        // {
        //        //     indexMaxD = i;
        //        //      MaxD = curD;
        //        //   }


        //        if (this.fV.ForceList[i].F > MaxF)
        //        {
        //            indexMaxF = i;
        //            MaxF = fV.ForceList[i].F;
        //        }
        //    }

        //    MaxD = Math.Sqrt(this.D[3 * indexMaxF, 0] * this.D[3 * indexMaxF, 0] + this.D[3 * indexMaxF + 1, 0] * this.D[3 * indexMaxF + 1, 0]);
        //}




        ///// <summary>
        ///// 将Force的属性IsBouldPoint为True的点设置为边界点，
        ///// 具体地，邻近图中道路上的顶点设为不动的边界条件
        ///// </summary>
        //private void SetBoundPointParams()
        //{
        //    int r1, r2, r3;//x,y,a
        //    int index = 0;
        //    Force curF = null;
        //    for (int k = 0; k < fV.ForceList.Count; k++)
        //    {
        //        curF = fV.ForceList[k];
        //        if (curF.IsBouldPoint)
        //        {

        //            r1 = k * 3;
        //            r2 = k * 3 + 1;
        //            r3 = k * 3 + 2;

        //            this.SetBoundPointParamsBigNumber(k, 0, 0);

        //            /*  this.F[r1, 0] = 0;
        //              this.F[r2, 0] = 0;
        //              this.F[r3, 0] = 0;

        //              for (int i = 0; i < K.Col; i++)
        //              {
        //                  if (i == r1)
        //                  {
        //                      K[r1, i] = 1;//对角线上元素赋值为1
        //                  }
        //                  else
        //                  {
        //                      K[r1, i] = 0;//其他地方赋值为0
        //                  }

        //                  if (i == r2)
        //                  {
        //                      K[r2, i] = 1;//对角线上元素赋值为1
        //                  }
        //                  else
        //                  {
        //                      K[r2, i] = 0;//其他地方赋值为0
        //                  }

        //                  if (i == r3)
        //                  {
        //                      K[r3, i] = 1;//对角线上元素赋值为1
        //                  }
        //                  else
        //                  {
        //                      K[r3, i] = 0;//其他地方赋值为0
        //                  }
        //              }*/
        //        }
        //    }
        //}

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

                    /* 
                     *                       
                     * int  r1 = index * 3;
                   int r2 = index * 3 + 1;
                  int  r3 = index * 3 + 2;
                     * this.F[r1, 0] = 0;
                      this.F[r2, 0] = 0;
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
                      }*/
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

                PolygonObject po = this.GetPoByID(tagID, this.Map.PolygonList);

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


        ///// <summary>
        ///// 迭代
        ///// </summary>
        //public override void DoDispaceIterate()
        //{
        //    ComMatrix_K();       //求刚度矩阵

        //    if (!MakeForceVector())   //建立并计算力向量
        //    {
        //        return;
        //    }
        //    #region 求1+rK
        //    int n = this._K.Col;
        //    this._d = new Matrix(n, 1);
        //    Matrix identityMatrix = new Matrix(n, n);
        //    for (int i = 0; i < n; i++)
        //    {
        //        this._F[i, 0] *= r;
        //        this._d[i, 0] = 0.0;
        //        for (int j = 0; j < n; j++)
        //        {
        //            this._K[i, j] *= r;
        //            if (i == j)
        //                identityMatrix[i, j] = 1.0;
        //            else
        //                identityMatrix[i, j] = 0;
        //        }
        //    }
        //    this._K = identityMatrix + this._K;
        //    #endregion
        //    this.SetBoundPointParams();

        //    for (int i = 0; i < time; i++)
        //    {
        //        this._F = this._d + r * this._F;

        //        this.SetBoundPointParamsForce();
        //        ComDisplace();
        //        this.UpdataCoords();
        //        CreategeoSetFromRes();
        //        if (!MakeForceVector())   //建立并计算力向量
        //        {
        //            return;
        //        }
        //    }
        //    StaticDis();
        //}

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
            DataTable tableConflict= new DataTable();
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
            Matrix identityMatrix = new Matrix(3*n, 3*n);
            this.D = new Matrix(3*n, 1);
            for (int i = 0; i < 3*n; i++)
            {
                this.D[i, 0] = 0.0;
                for (int j = 0; j < 3*n; j++)
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
             
                OutputDisplacement(strPath, "Displacement" + i.ToString()+".txt", mstpg);
            }

            TXTHelper.ExportToTxt(tableConflict, strPath + @"\ConflictsCount.txt");
            
            //输出受力点出的受力与当前移位值
          //  this.OutputDisplacementandForces(fV.ForceList);
            //输出各点的移位总和
         //   OutputTotalDisplacementforProxmityGraph(this.OriginalGraph, this.ProxiGraph, this.Map);
        }

        public void DoDispaceBader1(double r, int times, double scale, double disThreshold,ISpatialReference prj)
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


        public override void DoDispace()
        {
            throw new NotImplementedException();
        }
        public override void DoDispaceIterate()
        {
            throw new NotImplementedException();
        }


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
            int rowCount=this.D.Row;
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
                        curPoint.X+= curDx;
                        curPoint.Y+= curDy;
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
                    dr[3] = Math.Sqrt(curDx*curDx+curDy*curDy);
                    tableforce.Rows.Add(dr);
                }
            }
            TXTHelper.ExportToTxt(tableforce, strPath + @"\" + fileName);
        }
        #endregion

    }
}
