using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;
using MatrixOperation;
using System.Data;
using AuxStructureLib.IO;
using AuxStructureLib.ConflictLib;
using ESRI.ArcGIS.Geometry;

namespace AlgEMLib
{

    public class AlgSnake : AlgEM
    {
        public double a=100;
        public double b=100;
        public bool isContinue=true;//是否继续迭代
        public Matrix Fy = null;//Snake算法需要分开计算X,Y 方向的受力与移位
        public Matrix Dy = null;
        SnakesStiffMatrix sM=null;//用于计算刚度矩阵的类
        SnakesForceVector fV = null;//用于计算受力的类
        public SMap Map;

        #region 单条线移位
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="proxiGraph">邻近图</param>
        public AlgSnake(PolylineObject polyline, double a,double b,string forceFile)
        {
            this.a = a;
            this.b = b;
            this.strPath = forceFile;
            Polyline = polyline;
            int n = Polyline.PointList.Count;
            for (int i = 0; i < n; i++)
            {
                Polyline.PointList[i].ID = i;
            }
        }

        /// <summary>
        /// 根据边界条件移位
        /// </summary>
        public void DoDisplaceBoundCon()
        {
            sM = new SnakesStiffMatrix(Polyline, a, b);
            fV = new SnakesForceVector(Polyline);

            this.K = sM.Matrix_K;
            this.F = fV.Fx;
            this.Fy = fV.Fy;

            SetBoundaryPointListfrmFile(strPath);
            //SetBoundPointParamsInteractive(); //人工设置边界点
            this.D = this.K.Inverse() * this.F;
            this.Dy = this.K.Inverse() * this.Fy;     //求移位量
            this.UpdataCoords();      //更新坐标
        }

        /// <summary>
        /// 不迭代
        /// </summary>
        public override void DoDispace()
        {
            sM = new SnakesStiffMatrix(Polyline, a, b);
            fV = new SnakesForceVector(Polyline, strPath);

            this.K = sM.Matrix_K;
            this.F = fV.Fx;
            this.Fy = fV.Fy;

            SetBoundPointParamsforLine();//设置边界条件

            this.D = this.K.Inverse() * this.F;
            this.Dy = this.K.Inverse() * this.Fy;

            double MaxD;
            double MaxF;
            int indexMaxD = -1;
            int indexMaxF = -1;

            StaticDis(out MaxD, out MaxF, out indexMaxD, out indexMaxF);

            if (MaxF > 0)
            {
                double k = MaxD / MaxF;
                this.a *= k;
                this.b *= k;

                sM = new SnakesStiffMatrix(Polyline, a, b);
                this.K = sM.Matrix_K;

                SetBoundPointParamsforLine();//设置边界条件

                this.D = this.K.Inverse() * this.F;
                this.Dy = this.K.Inverse() * this.Fy;
            }

            UpdataCoords();      //更新坐标

            if (MaxF <= DisThresholdPP * 0.01)
            {
                this.isContinue = false;
            }
        }
        /// <summary>
        /// 设置边界
        /// </summary>
        private void SetBoundPointParamsforLine()
        {
            int n = this.Polyline.PointList.Count;
            //如果线的端点出不受力的作用则，将它们设置为不移动的边界点
            if(this.F[0,0]==0&&this.Fy[0,0]==0)
                this.SetBoundPointParamsBigNumber(0, 0, 0);
            if (this.F[2 * (n - 1), 0] == 0 && this.Fy[2 * (n - 1), 0] == 0)
                this.SetBoundPointParamsBigNumber(n - 1, 0, 0);
        }

        /// <summary>
        /// 读取文件中的边界条件并设置
        /// </summary>
        /// <param name="forcefile"></param>
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
        /// 统计移位值
        /// </summary>
        private void StaticDis(out double MaxD, out double MaxF, out int indexMaxD, out int indexMaxF)
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
            MaxD = Math.Sqrt(this.D[2* indexMaxF, 0] * this.D[2 * indexMaxF, 0] + this.Dy[2 * indexMaxF, 0] * this.Dy[2 * indexMaxF, 0]);
        }

        /// <summary>
        /// 置大数法
        /// </summary>
        public void SetBoundPointParamsBigNumber(int index, double dx, double dy)
        {
            int r1 = index * 2;
            int r2 = index * 2 + 1;


            this.F[r1, 0] = K[r1, r1] * dx * 100000000;
            this.Fy[r1, 0] = K[r1, r1] * dy * 100000000;


            this.F[r2, 0] = 0;
            this.Fy[r2, 0] = 0;

            K[r1, r1] = 100000000 * K[r1, r1];
            K[r2, r2] = 100000000 * K[r2, r2];
        }

        /// <summary>
        /// 更新坐标位置--不考虑比例尺
        /// </summary>
        private void UpdataCoords()
        {
             for (int i = 0; i < Polyline.PointList.Count; i++)
            {
                Node curPoint = Polyline.PointList[i];
                int index = curPoint.ID;
                curPoint.X += this.D[2 * index, 0];
                curPoint.Y += this.Dy[2 * index, 0];
            }
        }

        public override void DoDispaceIterate()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region 道路网移位
        /// <summary>
        /// 构造函数-从一个线对象构建Beams模型2014-1-18
        /// </summary>
        /// <param name="map">地图</param>
        /// <param name="e">弹性模量</param>
        /// <param name="i">惯性力矩</param>
        /// <param name="a">横截面积</param>
        /// <param name="conflictList">冲突列表</param>
        public AlgSnake(SMap map, double a, double b, List<ConflictBase> conflictList)
        {
            this.Map = map;
            this.a = a;
            this.b = b;
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
        public AlgSnake(SMap map, double a, double b, SnakesForceVector fv)
        {
            this.Map = map;
            this.a = a;
            this.b = b;
            this.fV = fv;
        }
        /// <summary>
        /// 自适应设置a,b
        /// </summary>
        public void DoDisplaceAdaptiveforNT()
        {
            sM = new SnakesStiffMatrix(Map.PolylineList, Map.TriNodeList.Count, a, b);
            fV = new SnakesForceVector(Map, ConflictList);
            fV.Create_WriteForceVector2Shp(strPath, @"ForceVector", esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
            this.K = sM.Matrix_K;
            this.F = fV.Fx;
            this.Fy = fV.Fy;

            SetBoundaryPointListAtEndNode();//设置边界条件

            this.D = this.K.Inverse() * this.F;
            this.Dy = this.K.Inverse() * this.Fy;
            double MaxD;
            double MaxF;
            int indexMaxD = -1;
            int indexMaxF = -1;

            StaticDisforNT(out MaxD, out MaxF, out indexMaxD, out indexMaxF);

            if (MaxF > 0 && indexMaxF >= 0)
            {
                double k = MaxD / MaxF;
                this.a *= k;
                this.b *= k;
                sM = new SnakesStiffMatrix(Map.PolylineList, Map.TriNodeList.Count,a,b);
                this.K = sM.Matrix_K;

                SetBoundaryPointListAtEndNode();//设置边界条件

                this.D = this.K.Inverse() * this.F;
                this.Dy = this.K.Inverse() * this.Fy;
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
        ///道路网设置边界条件移位
        /// </summary>
        public void DoDisplaceBoundaryConforNT()
        {
            sM = new SnakesStiffMatrix(Map.PolylineList, Map.TriNodeList.Count,this.a,this.b);
            this.fV.MakeForceVectorfrmPolylineList();
            this.K = sM.Matrix_K;
            this.F = fV.Fx;
            this.Fy = fV.Fy;

            SetBoundaryPointListAtBoundaryPoint();//设置边界条件
            SetBoundaryPointListAtInitDisVectors();//设置边界条件

            //SetBoundPointParamsInteractive(); //人工设置边界点
            this.D = this.K.Inverse() * this.F;
            this.Dy = this.K.Inverse() * this.Fy;     //求移位量
            this.UpdataCoordsforNT();      //更新坐标
            OutputDisplacementandForces(fV.ForceList);
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
                double dx = this.D[2 * force.ID, 0];
                double dy = this.Dy[2 * force.ID, 0];
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
        /// 更新坐标位置
        /// </summary>
        private void UpdataCoordsforNT()
        {

            for (int i = 0; i < Map.TriNodeList.Count; i++)
            {
                Node curPoint = Map.TriNodeList[i];
                int index = curPoint.ID;
                curPoint.X += this.D[2 * index, 0];
                curPoint.Y += this.Dy[2 * index, 0];
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
        private void SetBoundaryPointListAtInitDisVectors()
        {
            foreach (Force f in this.fV.ForceList)
            {
                int index = f.ID;
                this.SetBoundPointParamsBigNumber(index, f.Fx, f.Fy);
            }
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
        public AlgSnake(ProxiGraph proxiGraph, SMap map, List<GroupofMapObject> groups, double a, double b, List<ConflictBase> conflictList)
        {
            this.a = a;
            this.b = b;
            this.ProxiGraph = proxiGraph;
            this.Map = map;
            this.ConflictList = conflictList;
            this.Groups = groups;
        }
                /// <summary>
        /// 构造函数-从建筑物群邻近图构建Beams模型
        /// </summary>
        /// <param name="proxiGraph">邻近图</param>
        /// <param name="map">地图对象</param>
        /// <param name="e">弹性模量</param>
        /// <param name="i">惯性力矩</param>
        /// <param name="a">横截面积</param>
        /// <param name="conflictList">邻近冲突列表</param>
        public AlgSnake(ProxiGraph proxiGraph, SMap map, double a, double b, List<ConflictBase> conflictList)
        {
            this.a = a;
            this.b = b;
            this.ProxiGraph = proxiGraph;
            this.Map = map;
            this.ConflictList = conflictList;
        }

        /// 默认构造函数
        public AlgSnake()
        {

        }

        /// 构造函数-从建筑物群邻近图构建Beams模型
        /// </summary>
        /// <param name="proxiGraph">邻近图</param>
        /// <param name="map">地图对象</param>
        /// <param name="e">弹性模量</param>
        /// <param name="i">惯性力矩</param>
        /// <param name="a">横截面积</param>
        /// <param name="conflictList">邻近冲突列表</param>
        public AlgSnake(ProxiGraph proxiGraph, double a, double b)
        {
            this.a = a;
            this.b = b;
            this.ProxiGraph = proxiGraph;
        }

        /// 构造函数-从建筑物群邻近图构建Beams模型
        /// </summary>
        /// <param name="proxiGraph">邻近图</param>
        /// <param name="map">地图对象</param>
        /// <param name="e">弹性模量</param>
        /// <param name="i">惯性力矩</param>
        /// <param name="a">横截面积</param>
        /// <param name="conflictList">邻近冲突列表</param>
        public AlgSnake(ProxiGraph proxiGraph, SMap sMap,double a, double b)
        {
            this.a = a;
            this.b = b;
            this.ProxiGraph = proxiGraph;
            this.Map = sMap;
        }

        /// <summary>
        /// 邻近图的移位算法实现
        /// </summary>
        public void DoDispacePGNew()
        {
            fV = new SnakesForceVector(this.ProxiGraph);
            //求吸引力-2014-3-20所用
            fV.OrigialProxiGraph = this.OriginalGraph;
            fV.RMSE = this.PAT * this.Scale / 1000;
            fV.IsDragForce = this.isDragF;
            fV.CreateForceVectorfrmConflict(ConflictList);
            fV.Create_WriteForceVector2Shp(strPath, @"ForceVector", esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
           
            this.F = fV.Fx;
            this.Fy = fV.Fy;

            double MaxD;
            double MaxF;
            double MaxDF;
            double MaxFD;
            int indexMaxD = -1;
            int indexMaxF = -1;

            if ((this.ProxiGraph.PolygonCount <= 3 && this.AlgType == 2) || this.AlgType == 1)
            {
                //基本几何算法
                this.D = new Matrix(this.ProxiGraph.NodeList.Count * 2, 1);
                this.Dy = new Matrix(this.ProxiGraph.NodeList.Count * 2, 1);
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
                sM = new SnakesStiffMatrix(this.ProxiGraph, a, b);
                this.K = sM.Matrix_K;

                this.SetBoundPointParamforPG();//设置边界条件

                this.D = this.K.Inverse() * this.F;
                this.Dy = this.K.Inverse() * this.Fy;

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

                    this.a *= k;
                    this.b *= k;
                    //再次计算刚度矩阵
                    sM = new SnakesStiffMatrix(this.ProxiGraph, a,b);
                    this.K = sM.Matrix_K;
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
        /// 邻近图的移位算法实现
        /// <param name="MaxForce">用于限制计算两个力时的最大力</param>
        /// <param name="MaxForce_2">用于限制全力力时的最大力</param>
        /// </summary>
        public void DoDispacePG_CTP(List<ProxiNode> FinalLocation, double MinDis,double MaxForce,double MaxForce_2)
        {
            fV = new SnakesForceVector(this.ProxiGraph);
            //求吸引力-2014-3-20所用
            fV.OrigialProxiGraph = this.OriginalGraph;
            fV.RMSE = this.PAT * this.Scale / 1000;
            fV.IsDragForce = this.isDragF;
            fV.CreateForceVectorfrm_CTP(this.ProxiGraph.NodeList, FinalLocation, MinDis, MaxForce, MaxForce_2);

            this.F = fV.Fx;
            this.Fy = fV.Fy;

            double MaxD;
            double MaxF;
            double MaxDF;
            double MaxFD;
            int indexMaxD = -1;
            int indexMaxF = -1;

            //#region 随机数
            //Random Random_Gene = new Random();
            //List<int> SelectedIDs = new List<int>();
            //for (int i = 0; i < this.ProxiGraph.NodeList.Count * 0.02; i++)
            //{
            //    int Random_ID = Random_Gene.Next(0, this.ProxiGraph.NodeList.Count - 1);
            //    SelectedIDs.Add(Random_ID);
            //}
            //#endregion

            if ((this.ProxiGraph.PolygonCount <= 3 && this.AlgType == 2) || this.AlgType == 1)
            {
                //基本几何算法
                this.D = new Matrix(this.ProxiGraph.NodeList.Count * 2, 1);
                this.Dy = new Matrix(this.ProxiGraph.NodeList.Count * 2, 1);
                this.UpdataCoordsforPGbyForce_CTP();
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
                sM = new SnakesStiffMatrix(this.ProxiGraph, a, b);
                this.K = sM.Matrix_K;

                //this.SetBoundPointParamforPG_CTP2(SelectedIDs);//设置边界条件
                this.SetBoundPointParamforPG_CTP();

                try
                {
                    this.D = this.K.Inverse() * this.F;
                    this.Dy = this.K.Inverse() * this.Fy;
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

                    this.a *= k;
                    this.b *= k;
                    //再次计算刚度矩阵
                    sM = new SnakesStiffMatrix(this.ProxiGraph, a, b);
                    this.K = sM.Matrix_K;
                    //SetBoundPointParamforPG_CTP2(SelectedIDs);//设置边界条件
                    this.SetBoundPointParamforPG_CTP();

                    try
                    {
                        this.D = this.K.Inverse() * this.F;
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

                UpdataCoordsforPG_CTP();      //更新坐标
            }
            //输出受力点出的受力与当前移位值
            //O0999999999999999999999999999999999999999999999999999999999999999PO0Lthis.OutputDisplacementandForces(fV.ForceList);
            //输出各点的移位总和
            //OutputTotalDisplacementforProxmityGraph(this.OriginalGraph, this.ProxiGraph, this.Map);

            if (MaxF <= 0.001)
            {
                this.isContinue = false;
            }
        }

        /// <summary>
        /// 邻近图的移位算法实现
        /// <param name="MaxForce">用于限制计算两个力时的最大力</param>
        /// <param name="MaxForce_2">用于限制全力力时的最大力</param> 
        /// </summary>
        public void DoDispacePG_CTP(List<ProxiNode> FinalLocation,SMap sMap, double MinDis,double MaxForce,double MaxForce_2)
        {
            fV = new SnakesForceVector(this.ProxiGraph);
            //求吸引力-2014-3-20所用
            fV.OrigialProxiGraph = this.OriginalGraph;
            fV.RMSE = this.PAT * this.Scale / 1000;
            fV.IsDragForce = this.isDragF;
            fV.CreateForceVectorfrm_CTP(this.ProxiGraph.NodeList, FinalLocation, MinDis, MaxForce, MaxForce_2);

            this.F = fV.Fx;
            this.Fy = fV.Fy;

            double MaxD;
            double MaxF;
            double MaxDF;
            double MaxFD;
            int indexMaxD = -1;
            int indexMaxF = -1;

            //#region 随机数
            //Random Random_Gene = new Random();
            //List<int> SelectedIDs = new List<int>();
            //for (int i = 0; i < this.ProxiGraph.NodeList.Count * 0.02; i++)
            //{
            //    int Random_ID = Random_Gene.Next(0, this.ProxiGraph.NodeList.Count - 1);
            //    SelectedIDs.Add(Random_ID);
            //}
            //#endregion

            if ((this.ProxiGraph.PolygonCount <= 3 && this.AlgType == 2) || this.AlgType == 1)
            {
                //基本几何算法
                this.D = new Matrix(this.ProxiGraph.NodeList.Count * 2, 1);
                this.Dy = new Matrix(this.ProxiGraph.NodeList.Count * 2, 1);
                this.UpdataCoordsforPGbyForce_CTP();
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
                sM = new SnakesStiffMatrix(this.ProxiGraph, a, b);
                this.K = sM.Matrix_K;

                //this.SetBoundPointParamforPG_CTP2(SelectedIDs);//设置边界条件
                this.SetBoundPointParamforPG_CTP();

                try
                {
                    this.D = this.K.Inverse() * this.F;
                    this.Dy = this.K.Inverse() * this.Fy;
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

                    this.a *= k;
                    this.b *= k;
                    //再次计算刚度矩阵
                    sM = new SnakesStiffMatrix(this.ProxiGraph, a, b);
                    this.K = sM.Matrix_K;
                    //SetBoundPointParamforPG_CTP2(SelectedIDs);//设置边界条件
                    this.SetBoundPointParamforPG_CTP();

                    try
                    {
                        this.D = this.K.Inverse() * this.F;
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

                UpdataCoordsforPG_CTP2();      //更新坐标
            }
            //输出受力点出的受力与当前移位值
            //O0999999999999999999999999999999999999999999999999999999999999999PO0Lthis.OutputDisplacementandForces(fV.ForceList);
            //输出各点的移位总和
            //OutputTotalDisplacementforProxmityGraph(this.OriginalGraph, this.ProxiGraph, this.Map);

            Console.WriteLine(MaxF.ToString());
            if (MaxF <= 0.001)
            {
                this.isContinue = false;
            }
        }

        /// <summary>
        /// 邻近图的移位算法实现
        /// <param name="MaxForce">用于限制计算两个力时的最大力</param>
        /// <param name="MaxForce_2">用于限制全力力时的最大力</param>
        /// </summary>
        public void DoDispacePG_CTP_AdpPg(List<ProxiNode> FinalLocation, SMap sMap, double MinDis, double MaxForce,double MaxForce_2)
        {
            fV = new SnakesForceVector(this.ProxiGraph);
            //求吸引力-2014-3-20所用
            fV.OrigialProxiGraph = this.OriginalGraph;
            fV.RMSE = this.PAT * this.Scale / 1000;
            fV.IsDragForce = this.isDragF;
            fV.CreateForceVectorfrm_CTP_AdpPg(this.ProxiGraph.NodeList, FinalLocation, MinDis, MaxForce, MaxForce_2);

            this.F = fV.Fx;
            this.Fy = fV.Fy;

            double MaxD;
            double MaxF;
            double MaxDF;
            double MaxFD;
            int indexMaxD = -1;
            int indexMaxF = -1;

            //#region 随机数
            //Random Random_Gene = new Random();
            //List<int> SelectedIDs = new List<int>();
            //for (int i = 0; i < this.ProxiGraph.NodeList.Count * 0.02; i++)
            //{
            //    int Random_ID = Random_Gene.Next(0, this.ProxiGraph.NodeList.Count - 1);
            //    SelectedIDs.Add(Random_ID);
            //}
            //#endregion

            if ((this.ProxiGraph.PolygonCount <= 3 && this.AlgType == 2) || this.AlgType == 1)
            {
                //基本几何算法
                this.D = new Matrix(this.ProxiGraph.NodeList.Count * 2, 1);
                this.Dy = new Matrix(this.ProxiGraph.NodeList.Count * 2, 1);
                this.UpdataCoordsforPGbyForce_CTP();
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
                sM = new SnakesStiffMatrix(this.ProxiGraph, a, b);
                this.K = sM.Matrix_K;

                //this.SetBoundPointParamforPG_CTP2(SelectedIDs);//设置边界条件
                this.SetBoundPointParamforPG_CTP();

                try
                {
                    this.D = this.K.Inverse() * this.F;
                    this.Dy = this.K.Inverse() * this.Fy;
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

                    this.a *= k;
                    this.b *= k;
                    //再次计算刚度矩阵
                    sM = new SnakesStiffMatrix(this.ProxiGraph, a, b);
                    this.K = sM.Matrix_K;
                    //SetBoundPointParamforPG_CTP2(SelectedIDs);//设置边界条件
                    this.SetBoundPointParamforPG_CTP();

                    try
                    {
                        this.D = this.K.Inverse() * this.F;
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

                UpdataCoordsforPG_CTP2();      //更新坐标
            }
            //输出受力点出的受力与当前移位值
            //O0999999999999999999999999999999999999999999999999999999999999999PO0Lthis.OutputDisplacementandForces(fV.ForceList);
            //输出各点的移位总和
            //OutputTotalDisplacementforProxmityGraph(this.OriginalGraph, this.ProxiGraph, this.Map);

            Console.WriteLine(MaxF.ToString());
            if (MaxF <= 0.001)
            {
                this.isContinue = false;
            }
        }

        /// <summary>
        /// 邻近图的移位算法实现（层次Snake）
        /// </summary>
        public void DoDispacePG_HierCTP(List<ProxiNode> FinalLocation, SMap sMap, double MinDis,double MaxForce,double ForceRate)
        {
            List<int> BoundingPoint = new List<int>();

            fV = new SnakesForceVector(this.ProxiGraph);
            //求吸引力-2014-3-20所用
            fV.OrigialProxiGraph = this.OriginalGraph;
            fV.RMSE = this.PAT * this.Scale / 1000;
            fV.IsDragForce = this.isDragF;
            fV.CreateForceVectorfrm_HierCTP(this.ProxiGraph.NodeList, FinalLocation, MinDis, out BoundingPoint, MaxForce, ForceRate);

            this.F = fV.Fx;
            this.Fy = fV.Fy;

            double MaxD;
            double MaxF;
            double MaxDF;
            double MaxFD;
            int indexMaxD = -1;
            int indexMaxF = -1;

            //#region 随机数
            //Random Random_Gene = new Random();
            //List<int> SelectedIDs = new List<int>();
            //for (int i = 0; i < this.ProxiGraph.NodeList.Count * 0.02; i++)
            //{
            //    int Random_ID = Random_Gene.Next(0, this.ProxiGraph.NodeList.Count - 1);
            //    SelectedIDs.Add(Random_ID);
            //}
            //#endregion

            if ((this.ProxiGraph.PolygonCount <= 3 && this.AlgType == 2) || this.AlgType == 1)
            {
                //基本几何算法
                this.D = new Matrix(this.ProxiGraph.NodeList.Count * 2, 1);
                this.Dy = new Matrix(this.ProxiGraph.NodeList.Count * 2, 1);
                this.UpdataCoordsforPGbyForce_CTP();
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
                sM = new SnakesStiffMatrix(this.ProxiGraph, a, b);
                this.K = sM.Matrix_K;

                //this.SetBoundPointParamforPG_CTP2(SelectedIDs);//设置边界条件
                this.SetBoundPointParamforPG_CTP();

                try
                {
                    this.D = this.K.Inverse() * this.F;
                    this.Dy = this.K.Inverse() * this.Fy;
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

                    this.a *= k;
                    this.b *= k;
                    //再次计算刚度矩阵
                    sM = new SnakesStiffMatrix(this.ProxiGraph, a, b);
                    this.K = sM.Matrix_K;
                    //SetBoundPointParamforPG_CTP2(SelectedIDs);//设置边界条件
                    this.SetBoundPointParamforPG_CTP();

                    try
                    {
                        this.D = this.K.Inverse() * this.F;
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

                UpdataCoordsforPG_CTP2();      //更新坐标
            }
            //输出受力点出的受力与当前移位值
            //O0999999999999999999999999999999999999999999999999999999999999999PO0Lthis.OutputDisplacementandForces(fV.ForceList);
            //输出各点的移位总和
            //OutputTotalDisplacementforProxmityGraph(this.OriginalGraph, this.ProxiGraph, this.Map);

            Console.WriteLine(MaxF.ToString());
            if (MaxF <= 0.1)
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
            fV = new SnakesForceVector(this.ProxiGraph);
            //求吸引力-2014-3-20所用
            fV.OrigialProxiGraph = this.OriginalGraph;
            fV.RMSE = this.PAT * this.Scale / 1000;
            fV.IsDragForce = this.isDragF;
            fV.CreateForceVectorfrmConflict(ConflictList);
            fV.Create_WriteForceVector2Shp(strPath, @"ForceVector", esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
            this.F = fV.Fx;
            this.Fy = fV.Fy;
            if ((this.ProxiGraph.PolygonCount <= 3 && this.AlgType == 2) || this.AlgType == 1)
            {
                //基本几何算法
                this.D = new Matrix(this.ProxiGraph.NodeList.Count * 2, 1);
                this.Dy = new Matrix(this.ProxiGraph.NodeList.Count * 2, 1);
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
                sM = new SnakesStiffMatrix(this.ProxiGraph,a,b);
                this.K = sM.Matrix_K;
                this.SetBoundPointParamforPG_BC();//设置边界条件

                this.D = this.K.Inverse() * this.F;
                this.Dy = this.K.Inverse() * this.Fy;


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

                    double curDx0 = this.D[2 * index, 0];
                    double curDy0 = this.Dy[2 * index , 0];
                    double curDx = this.D[2 * index, 0];
                    double curDy = this.Dy[2 * index , 0];

                    if (this.IsTopCos == true)
                    {
                        vp = this.VD.GetVPbyIDandType(tagID, fType);
                        vp.TopologicalConstraint(curDx0, curDy0, 0.001, out curDx, out curDy);
                        curDx = this.D[2 * index, 0] = curDx;
                        curDy = this.Dy[2 * index, 0] = curDy;
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
        /// 更新坐标位置
        /// </summary>
        private void UpdataCoordsforPG_CTP()
        {
            foreach (ProxiNode curNode in this.ProxiGraph.NodeList)
            {
                int index = curNode.ID;
                int tagID = curNode.TagID;
                //FeatureType fType = curNode.FeatureType;
                //VoronoiPolygon vp = null;

                double curDx0 = this.D[2 * index, 0];
                double curDy0 = this.Dy[2 * index, 0];
                double curDx = this.D[2 * index, 0];
                double curDy = this.Dy[2 * index, 0];

                if (this.IsTopCos == true)
                {
                    //vp = this.VD.GetVPbyIDandType(tagID, fType);
                    //vp.TopologicalConstraint(curDx0, curDy0, 0.001, out curDx, out curDy);
                    curDx = this.D[2 * index, 0] = curDx;
                    curDy = this.Dy[2 * index, 0] = curDy;
                }

                //纠正拓扑错误
                curNode.X += curDx;
                curNode.Y += curDy;

            }
        }

        /// <summary>
        /// 更新坐标位置(更新了Map中要素的信息)
        /// </summary>
        private void UpdataCoordsforPG_CTP2()
        {
            foreach (ProxiNode curNode in this.ProxiGraph.NodeList)
            {
                int index = curNode.ID;
                int tagID = curNode.TagID;
                //FeatureType fType = curNode.FeatureType;
                //VoronoiPolygon vp = null;

                double curDx0 = this.D[2 * index, 0];
                double curDy0 = this.Dy[2 * index, 0];
                double curDx = this.D[2 * index, 0];
                double curDy = this.Dy[2 * index, 0];

                if (this.IsTopCos == true)
                {
                    //vp = this.VD.GetVPbyIDandType(tagID, fType);
                    //vp.TopologicalConstraint(curDx0, curDy0, 0.001, out curDx, out curDy);
                    curDx = this.D[2 * index, 0] = curDx;
                    curDy = this.Dy[2 * index, 0] = curDy;
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

                #region 更新PointList
                //if (curNode.FeatureType == FeatureType.PointType)
                //{
                //    PointObject CachePoint = this.GetPointObjectByID(curNode.TagID, this.Map.PointList);
                //    if (CachePoint != null)
                //    {
                //        CachePoint.Point.X += curDx;
                //        CachePoint.Point.Y += curDy;
                //    }
                //}
                #endregion

                #region 更新PolygonList
                //if (curNode.FeatureType == FeatureType.PolygonType)
                //{
                //    PolygonObject Po = this.GetPoByID(curNode.TagID, this.Map.PolygonList);
                //    TriNode CachePoint = this.GetPointByID(curNode.ID, Po.PointList);

                //    if (CachePoint != null)
                //    {
                //        CachePoint.X += curDx;
                //        CachePoint.Y += curDy;
                //    }
                //}
                #endregion
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
        /// GetPointByID
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        public PointObject GetPointObjectByID(int ID, List<PointObject> PointObjectList)
        {
            PointObject No = null;
            foreach (PointObject CacheNo in PointObjectList)
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
                            this.D[2 * index, 0] = curDx;
                            this.Dy[2 * index , 0] = curDy;
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
                        this.D[2 * index, 0] = curDx;
                        this.Dy[2 * index, 0] = curDy;
                    }
                }
            }
        }

        /// <summary>
        /// 直接采用受力更新坐标位置
        /// </summary>
        private void UpdataCoordsforPGbyForce_CTP()
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
                        this.D[2 * index, 0] = curDx;
                        this.Dy[2 * index, 0] = curDy;
                    }
                    //纠正拓扑错误
                    curNode.X += curDx;
                    curNode.Y += curDy;

                }
                else
                {
                    this.D[2 * index, 0] = curDx;
                    this.Dy[2 * index, 0] = curDy;
                }
            }
        }

        /// <summary>
        /// 邻近图的移位算法实现
        /// </summary>
        public void DoDispacePGNew_Group()
        {
            fV = new SnakesForceVector(this.ProxiGraph);
            //求吸引力-2014-3-20所用
            fV.OrigialProxiGraph = this.OriginalGraph;
            fV.RMSE = 0.5 * this.Scale / 1000;
            fV.IsDragForce = this.isDragF;

            fV.CreateForceVectorfrmConflict_Group(ConflictList,Groups);

            fV.Create_WriteForceVector2Shp(strPath, @"ForceVector", esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
            this.F = fV.Fx;
            this.Fy = fV.Fy;

            double MaxD;
            double MaxF;
            double MaxDF;
            double MaxFD;
            int indexMaxD = -1;
            int indexMaxF = -1;

            if ((this.ProxiGraph.PolygonCount <= 3 && this.AlgType == 2) || this.AlgType == 1)
            {
                //基本几何算法
                this.D = new Matrix(this.ProxiGraph.NodeList.Count * 2, 1);
                this.Dy = new Matrix(this.ProxiGraph.NodeList.Count * 2, 1);
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
                sM = new SnakesStiffMatrix(this.ProxiGraph, a,b);
                this.K = sM.Matrix_K;

                this.SetBoundPointParamforPG();//设置边界条件

                this.D = this.K.Inverse() * this.F;
                this.Dy = this.K.Inverse() * this.Fy;

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
       
                    this.a *= k;
                    this.b *= k;
        
                    //再次计算刚度矩阵
                    sM = new SnakesStiffMatrix(this.ProxiGraph, a,b);
                    this.K = sM.Matrix_K;
                    SetBoundPointParamforPG();//设置边界条件
                    this.D = this.K.Inverse() * this.F;
                    this.Dy = this.K.Inverse() * this.Fy;
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
            fV = new SnakesForceVector(this.ProxiGraph);
            //求吸引力-2014-3-20所用
            fV.OrigialProxiGraph = this.OriginalGraph;
            fV.RMSE = 0.5 * this.Scale / 1000;
            fV.IsDragForce = this.isDragF;
            fV.CreateForceVectorfrmConflict_Group(ConflictList,this.Groups);
            fV.Create_WriteForceVector2Shp(strPath, @"ForceVector", esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
            this.F = fV.Fx;
            this.Fy = fV.Fy;
            ;

            if ((this.ProxiGraph.PolygonCount <= 3 && this.AlgType == 2) || this.AlgType == 1)
            {
                //基本几何算法
                this.D = new Matrix(this.ProxiGraph.NodeList.Count * 2, 1);
                this.Dy = new Matrix(this.ProxiGraph.NodeList.Count * 2, 1);
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
                sM = new SnakesStiffMatrix(this.ProxiGraph, a,b);
                this.K = sM.Matrix_K;


                this.SetBoundPointParamforPG_BC();//设置边界条件

                this.D = this.K.Inverse() * this.F;
                this.Dy = this.K.Inverse() * this.Fy;


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
        private void SetBoundPointParamforPG_CTP(List<int> BoundingPoint)
        {
            Force curForce = null;
            foreach (ProxiNode curNode in this.ProxiGraph.NodeList)
            {
                if (curNode.FeatureType == FeatureType.PointType && (curNode.TagID == 0 || BoundingPoint.Contains(curNode.ID)))
                {
                    int index = curNode.ID;
                    this.SetBoundPointParamsBigNumber(index, 0, 0);
                }
            }
        }

        /// <summary>
        /// 设置邻近图的边界点2014-3-2 每一轮选10%的点作为边界点来控制
        /// </summary>
        private void SetBoundPointParamforPG_CTP2(List<int> SelectedIDs)
        {
            Force curForce = null;
          
            foreach (ProxiNode curNode in this.ProxiGraph.NodeList)
            {
                if ((curNode.FeatureType == FeatureType.PointType && curNode.TagID == 0) || (SelectedIDs.Contains(curNode.ID)))
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

                    double curDx0 = this.D[2 * index, 0];
                    double curDy0 = this.Dy[2 * index , 0];
                    double curDx = this.D[2 * index, 0];
                    double curDy = this.Dy[2 * index, 0];

                    if (this.IsTopCos == true)
                    {
                        vp = this.VD.GetVPbyIDandType(tagID, fType);
                        vp.TopologicalConstraint(curDx0, curDy0, 0.001, out curDx, out curDy);
                        this.D[2 * index, 0] = curDx;
                        this.Dy[2 * index, 0] = curDy;
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
                    double curDx0 = this.D[2 * index, 0];
                    double curDy0 = this.Dy[2 * index, 0];
                    double curDx = this.D[2 * index, 0];
                    double curDy = this.Dy[2 * index, 0];
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
                        this.D[2 * index, 0] = curDx;
                        this.Dy[2 * index, 0] = curDy;
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
                            this.D[2 * index, 0] = curDx;
                            this.Dy[2 * index, 0] = curDy;
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
                        //this.D[3 * index, 0] = curDx;
                        //this.D[3 * index + 1, 0] = curDy;
                        this.D[2 * index, 0] = curDx;
                        this.Dy[2 * index, 0] = curDy;
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
                            this.D[2 * index, 0] = curDx;
                            this.Dy[2 * index, 0] = curDy;
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
                        this.D[2 * index, 0] = curDx;
                        this.Dy[2 * index, 0] = curDy;
                    }
                }
            }
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
                curD = Math.Sqrt(this.D[2 * i, 0] * this.D[2 * i, 0] + this.Dy[2 * i, 0] * this.Dy[2 * i, 0]);

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
                MaxFD = Math.Sqrt(this.D[2 * indexMaxF, 0] * this.D[2 * indexMaxF, 0] + this.Dy[2 * indexMaxF , 0] * this.Dy[2 * indexMaxF, 0]);
            }
            if (indexMaxD != -1)
            {
                MaxDF = fV.ForceList[indexMaxD].F;
            }
        }
        #endregion
    }
}
