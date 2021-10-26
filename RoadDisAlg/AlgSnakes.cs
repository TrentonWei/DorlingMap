using System;
using System.Collections.Generic;
using System.Text;
using MatrixOperation;
using ESRI.ArcGIS.Geometry;
using System.IO;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Carto;
using System.Data;
namespace RoadDisAlg
{
  
    /// <summary>
    /// 移位统计量
    /// </summary>
    public struct StatisticDisValue
    {
        double max;
        double min;
        double ave;
        double sum;
        double stdDev;
    }
    /// <summary>
    /// Snakes算法实现
    /// </summary>
    public class AlgSnakes
    {
        public RoadNetWork _NetWork = null;                         //道路网络对象

        public List<ConflictDetection.Force> ForceList = null;       //来自三角网的受力列表
        //private RoadNetWork _CopyNetWork = null;                     //原始道路网的拷贝

        public static double a =10000000;                              //弹性参数初值
        public static double b = 100000000;                              //刚性参数初值
        public static double minDis = 0.2;                           //图上最小间距单位为毫米
        public static double k0 = 1.8;                                //狭窄弯曲阈值

        public static StatisticDisValue statisticDisValue;

        public static double k1 = 1.1;                                //一般弯曲的最小值
        public static double k2 = 1.8;                                //一般弯曲的最大值

        public static double c = 10;                                //抛物线最小值
        public static double r = 0.1;                               //迭代不长
        public static int time = 2;                                  //迭代次数
        public static bool isCurved = true;                         //是否分段设置形状参数
        public static bool isParabola = true;                        //是否抛物线模型
        public static double scale = 500000.0;                       //比例尺分母
        public static ForceType ForceType=ForceType.Vetex;
        public static GradeModelType GradeModelType = GradeModelType.Ratio;  //分级模式
        public static double g1 = 3;                                         //a的公比
        public static double d1 = 0;                                       //a的公差
        public static double g2 = 5;                                        //b的公比
        public static double d2 = 0;                                       //a的公差

        public static DataTable dtPara = null;                               //参数表格 

        public Force[] forceList = null;                                      //每个点上的受力
        private Matrix _K = null;                                      //刚度矩阵
        private Matrix _Fx = null;                                      //X方向受力向量
        private Matrix _Fy = null;                                     //Y方向受力向量

        private Matrix _dx = null;                                      //移位结果
        private Matrix _dy = null;                                      //移位结果

        private  double[] tdx = null;                                    //记录X方向最终的累积移位量
        private  double[] tdy = null;                                    //记录Y方向最终的累积移位量
        private  double[] d = null;                                      //最终的移位量
        public double max;
        public double min;
        public double sum;
        public double ave;
        public double std;
        public int indexMax;
        
        public int indexMin;
        public int count;

        public float maxF;
        public float minF;
        public float MaxF2;          //最大移位对应的受力
        public int indexMaxF;
        public int indexMinF;

        public float maxFD;

        /// <summary> 
        /// 构造函数
        /// </summary>
        /// <param name="netWork">待移位的道路网对象</param>
        public AlgSnakes(RoadNetWork netWork)
        {
            _NetWork = netWork;
            int n = netWork.PointList.Count;
            _K = new Matrix(2 * n, 2 * n);

            tdx = new double[n];                                    //记录X方向最终的累积移位量
            tdy = new double[n];                                    //记录Y方向最终的累积移位量
            d = new double[n];

        }

        #region 子网处理
        /// <summary>
        /// 获取初始子网2013-8-30
        /// </summary>
        /// <param name="k">力的传播幅度系数</param>
        /// <returns></returns>
        private List<RoadNetWork> GetInitSubNetWorks(int k)
        {
            List<RoadNetWork> networkList = new List<RoadNetWork>();

            this.MakeForceVectorTri();

            for(int i=0;i<forceList.Length;i++)
            {
                Force curForce=forceList[i];
                if (curForce.F > 0.0000001)
                {
                    float range = (float)(curForce.F * k * AlgSnakes.scale / 1000);//传播范围
                    ConnNode node = ConnNode.GetConnNodebyPID(this._NetWork.ConnNodeList, i);

                    if (node != null)
                    {
                        RoadNetWork curRoadNetWork = ConstrSubNetWorkfrmNode(node, range);
                        continue;
                    }
                    else
                    {

                        foreach (Road curR in this._NetWork.RoadList)
                        {

                            for (int j=0;j<curR.PointList.Count;j++)
                            {
                                if (curR.PointList[j] == i)
                                {
                                    RoadNetWork curRoadNetwork = ConstrSubNetWorkfrmRoad(i, curR, range);
                                    networkList.Add(curRoadNetwork);
                                    break ;  
                                }
                            }
                            break;  
                        }
                    }
                }
            }
            return networkList;
        }

        /// <summary>
        /// 构造子网(路段中间节点)
        /// </summary>
        /// <returns>返回子网</returns>
        private RoadNetWork ConstrSubNetWorkfrmRoad(int pID,Road road,float range)
        {
            RoadNetWork curNetWork = new RoadNetWork(_NetWork.RoadLyrInfoList);
            int index = 0;
            float rfRange = range;
            float rbRange = range;
            int fNIndex = -1;
            int bNIndex = -1;
            for (int i = 0; i < road.PointList.Count; i++)
            {
                if (_NetWork.PointList[road.PointList[i]].ID == pID)
                {
                    index = i;
                    break;
                }
            }
            if (index == 0)
                return null;

            int fc = index;//前半部分点数
            int bc = road.PointList.Count - index;//后半部分点数
            //前半部
            for (int i = fc - 1; i >= 0; i--)
            {
                PointCoord p1= _NetWork.PointList[road.PointList[i+1]];
                PointCoord p2= _NetWork.PointList[road.PointList[i]];
                float l =(float) Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
                rfRange = rfRange - l;//剩下的传播范围
                if (rfRange <= 0)
                {
                    //传播终止
                    fNIndex = i;
                    break;
                }
            }

            //后半部分
            for (int i = index; i < bc; i++)
            {
                PointCoord p1 = _NetWork.PointList[road.PointList[i ]];
                PointCoord p2 = _NetWork.PointList[road.PointList[i+1]];
                float l = (float)Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
                rbRange = rbRange - l;//剩下的传播范围
                if (rbRange <= 0)
                {
                    //传播终止
                    bNIndex = i;
                    break;
                }
            }
            if (fNIndex == -1 && bNIndex == -1)
            {
                curNetWork.RoadList.Add(road);

                //两端传播
                ConnNode snode = ConnNode.GetConnNodebyPID(this._NetWork.ConnNodeList,0);
                ConnNode enode = ConnNode.GetConnNodebyPID(this._NetWork.ConnNodeList, road.PointList.Count-1);

                this.GetRoadsbyNode(snode, rfRange, road.RID,ref curNetWork);
                this.GetRoadsbyNode(enode, rbRange, road.RID, ref curNetWork);
            }
            else if (fNIndex == -1&&bNIndex!=-1)//超出该路段情况
            {
                Road newRoad=new Road(null,null);
                newRoad.RID=road.RID;
                for(int i=0;i<=bNIndex;i++ )
                {
                   newRoad.PointList.Add(road.PointList[i]);
                }
                curNetWork.RoadList.Add(newRoad);
                //向前传播
                ConnNode snode = ConnNode.GetConnNodebyPID(this._NetWork.ConnNodeList, 0);
                this.GetRoadsbyNode(snode, rfRange, road.RID, ref curNetWork);
            }
            else if (fNIndex != -1 && bNIndex == -1)
            {
                Road newRoad = new Road(null, null);
                newRoad.RID = road.RID;
                for (int i = fNIndex; i < road.PointList.Count; i++)
                {
                    newRoad.PointList.Add(road.PointList[i]);
                }
                curNetWork.RoadList.Add(newRoad);
                //向后传播
                ConnNode enode = ConnNode.GetConnNodebyPID(this._NetWork.ConnNodeList, road.PointList.Count - 1);
                this.GetRoadsbyNode(enode, rbRange, road.RID, ref curNetWork);
            }
            else if (fNIndex != -1 && bNIndex != -1)
            {
                Road newRoad = new Road(null, null);
                newRoad.RID = road.RID;
                for (int i = fNIndex; i <=bNIndex; i++)
                {
                    newRoad.PointList.Add(road.PointList[i]);
                }
                curNetWork.RoadList.Add(newRoad);
                //不传播
            }
            return curNetWork;
        }
        /// <summary>
        /// 通过端点获得子网
        /// </summary>
        /// <param name="node">端点</param>
        /// <param name="range">范围</param>
        /// <returns>返回结果</returns>
        private RoadNetWork ConstrSubNetWorkfrmNode(ConnNode node, float range)
        {
            RoadNetWork netWork = new RoadNetWork(_NetWork.RoadLyrInfoList);
            List<int> rIDList = node.ConRoadList;

            if (rIDList.Count == 1)
            {
                //该端点出没有与其他路段关联,且是受力点，需要进一步研究8-30
            }
            foreach (int curRID in rIDList)
            {
                Road curRoad = _NetWork.RoadList[curRID];
                GetSubRoadbyRoad(node, curRoad, range, ref netWork);
            }
            return netWork;
        }

        /// <summary>
        /// 在端点处的传播到达的道路
        /// </summary>
        /// <param name="node">端点</param>
        /// <param name="range">传播范围</param>
        /// <param name="netWork">结果道路网</param>
        /// <param name="rID">道路号</param>
        private void GetRoadsbyNode(ConnNode node, float range,int rID, ref RoadNetWork netWork)
        {
            List<int> rIDList = node.ConRoadList;
            if (rIDList.Count == 1)
            {
                //该端点出没有与其他路段关联，需要做延长线
                return;
            }
            foreach (int curRID in rIDList)
            {
                if (curRID == rID)
                    continue;
                Road curRoad = _NetWork.RoadList[curRID];
                GetSubRoadbyRoad(node, curRoad, range, ref netWork);
            }
        }

        /// <summary>
        /// 从一个端点出发获取部分道路段
        /// </summary>
        /// <param name="road">路段</param>
        /// <param name="range">传播范围</param>
        private void GetSubRoadbyRoad(ConnNode node,Road road, float range,ref RoadNetWork netWork)
        {
            int index = -1;
            int nodePID = node.PointID;
            if (nodePID == road.FNode)
            {
                for (int i = 1; i < road.PointList.Count; i++)
                {
                    PointCoord p1 = _NetWork.PointList[road.PointList[i -1]];
                    PointCoord p2 = _NetWork.PointList[road.PointList[i]];
                    float l = (float)Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
                    range = range - l;//剩下的传播范围
                    if (range <= 0)
                    {
                        index = i;
                        break;
                    }
                }
                if (index == -1)
                {
                    ConnNode nextNode = ConnNode.GetConnNodebyPID(_NetWork.ConnNodeList, road.PointList[road.PointList.Count - 1]);
                    GetRoadsbyNode(nextNode, range, road.RID, ref netWork);
                }
                else
                {
                    Road newRoad = new Road(null, null);
                    newRoad.RID = road.RID;
                    for (int i = 0; i <= index; i++)
                    {
                        newRoad.PointList.Add(road.PointList[i]);
                    }
                    netWork.RoadList.Add(newRoad);
                }
            }
            else
            {
                for (int i = road.PointList.Count-2; i >= 0; i--)
                {
                    PointCoord p1 = _NetWork.PointList[road.PointList[i +1]];
                    PointCoord p2 = _NetWork.PointList[road.PointList[i]];
                    float l = (float)Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
                    range = range - l;//剩下的传播范围
                    if (range <= 0)
                    {
                        index = i;
                        break;
                    }
                }

                if (index == -1)
                {
                    ConnNode nextNode = ConnNode.GetConnNodebyPID(_NetWork.ConnNodeList, road.PointList[0]);
                    GetRoadsbyNode(nextNode, range, road.RID, ref netWork);
                }
                else
                {
                    Road newRoad = new Road(null, null);
                    newRoad.RID = road.RID;
                    for (int i = index; i <= road.PointList.Count; i++)
                    {
                        newRoad.PointList.Add(road.PointList[i]);
                    }
                    netWork.RoadList.Add(newRoad);
                }
            }
        }

        #endregion

        /// <summary>
        /// 自动调整参数
        /// </summary>
        public void DoDisplaceAdaptiveParams()
        {
            ComMatrix_K();       //求刚度矩阵
            //this.WriteMatrix(@"E:\map\实验数据\network", this._K, "K.txt");
            MakeForceVector();  //建立并计算力向量

            // this.WriteMatrix(@"E:\map\实验数据\network", this._Fx, "Fx.txt");
            //this.WriteMatrix(@"E:\map\实验数据\network", this._Fy, "Fy.txt");
            //this.WriteMatrix(@"E:\map\实验数据\network", this._Fy, "Fy.txt");

            SetBoundPointParams();//设置边界条件
            ComDisplace();       //求移位量

            foreach (PointCoord curPoint in this._NetWork.PointList)
            {
                int index = curPoint.ID;
                //curPoint.X += this._dx[2 * index, 0] * AlgSnakes.scale / 1000;
               // curPoint.Y += this._dy[2 * index, 0] * AlgSnakes.scale / 1000;
                tdx[index] = 0;
                tdy[index] = 0;
                tdx[index] += this._dx[2 * index, 0];
                tdy[index] += this._dy[2 * index, 0];
            }

            StaticDis();

            if (this.maxF > 0)
            {
                double k = this.maxFD / this.maxF;
                AlgSnakes.a *= k;
                AlgSnakes.b *= k;

                foreach (PointCoord curPoint in this._NetWork.PointList)
                {
                    int index = curPoint.ID;
                    //curPoint.X += this._dx[2 * index, 0] * AlgSnakes.scale / 1000;
                    // curPoint.Y += this._dy[2 * index, 0] * AlgSnakes.scale / 1000;
                    tdx[index] = 0;
                    tdy[index] = 0;
                }

                DoDispace();
            }

            WriteDandF(@"C:\学习\PHD\map\result", "FaD.txt");
        }

        /// <summary>
        /// 自动调整参数
        /// </summary>
        public void DoDisplaceAdaptiveParamsTri()
        {
            ComMatrix_K();       //求刚度矩阵
            //this.WriteMatrix(@"E:\map\实验数据\network", this._K, "K.txt");
            MakeForceVectorTri();  //建立并计算力向量

            // this.WriteMatrix(@"E:\map\实验数据\network", this._Fx, "Fx.txt");
            //this.WriteMatrix(@"E:\map\实验数据\network", this._Fy, "Fy.txt");
            //this.WriteMatrix(@"E:\map\实验数据\network", this._Fy, "Fy.txt");

            SetBoundPointParams();//设置边界条件
            //SetBoundPointParamsInteractive(); //人工设置边界点
            ComDisplace();       //求移位量

            foreach (PointCoord curPoint in this._NetWork.PointList)
            {
                int index = curPoint.ID;
                //curPoint.X += this._dx[2 * index, 0] * AlgSnakes.scale / 1000;
                // curPoint.Y += this._dy[2 * index, 0] * AlgSnakes.scale / 1000;
                tdx[index] = 0;
                tdy[index] = 0;
                tdx[index] += this._dx[2 * index, 0];
                tdy[index] += this._dy[2 * index, 0];
            }

            StaticDis();

            if (this.maxF > 0)
            {
                double k = this.maxFD / this.maxF;
                AlgSnakes.a *= k;
                AlgSnakes.b *= k;

                foreach (PointCoord curPoint in this._NetWork.PointList)
                {
                    int index = curPoint.ID;
                   // curPoint.X += this._dx[2 * index, 0] * AlgSnakes.scale / 1000;
                   // curPoint.Y += this._dy[2 * index, 0] * AlgSnakes.scale / 1000;
                    tdx[index] = 0;
                    tdy[index] = 0;
                }

                DoDispaceTri();
            }

           // WriteDandF(@"E:\map\实验数据\result", "FaD.txt");
        }

        /// <summary>
        /// 执行移位操作
        /// </summary>
        public void DoDispace()
        {
            ComMatrix_K();       //求刚度矩阵
            //this.WriteMatrix(@"E:\map\实验数据\network", this._K, "K.txt");
            MakeForceVector();  //建立并计算力向量

            //this.WriteMatrix(@"E:\map\实验数据\network", this._Fx, "Fx.txt");
            //this.WriteMatrix(@"E:\map\实验数据\network", this._Fy, "Fy.txt");
            //this.WriteMatrix(@"E:\map\实验数据\network", this._Fy, "Fy.txt");

            SetBoundPointParams();//设置边界条件
            //SetBoundPointParamsInteractive(); //人工设置边界点
            ComDisplace();       //求移位量
            UpdataCoords();      //更新坐标
             
            StaticDis();
        }

        /// <summary>
        /// 通过设置边界条件进行移位
        /// </summary>
        public void DoDisplaceBoundCon()
        {
            ComMatrix_K();       //求刚度矩阵
            //this.WriteMatrix(@"E:\map\实验数据\network", this._K, "K.txt");
            MakeForceVector0();  //建立并计算力向量
            //this.SetForceVectorInteractive();

            //this.WriteMatrix(@"E:\map\实验数据\network", this._Fx, "Fx.txt");
            //this.WriteMatrix(@"E:\map\实验数据\network", this._Fy, "Fy.txt");
            //this.WriteMatrix(@"E:\map\实验数据\network", this._Fy, "Fy.txt");
            //o-1法
           /* SetBoundPointParamsSE(this._NetWork.PointList.Count-1,0, 0);//设置边界条件
            SetBoundPointParamsSE(0, 133, -159);//设置边界条件*/
            //置大数法
            //SetBoundPointParamsBigNumber(this._NetWork.PointList.Count - 1, 0, 0);//
            SetBoundPointParamsBigNumber(13, 1330 / 500, -1590 / 500);
            SetBoundPointParamsBigNumber(76, 286 / 500, 1591 / 500);
            SetBoundPointParamsBigNumber(106, -2714 / 500, -1441 / 500);
           // SetBoundPointParamsBigNumber(104, -720, -300);
            SetBoundPointParamsBigNumber(48, -738 / 500, -1816 / 500);
            SetBoundPointParamsBigNumber(63, -1545 / 500, -719 / 500);

            SetBoundPointParamsBigNumber(0, 0, 0); 
            SetBoundPointParamsBigNumber(34, 0,0);
            SetBoundPointParamsBigNumber(93, 0, 0);
            SetBoundPointParamsBigNumber(49, 0, 0);
            SetBoundPointParamsBigNumber(77, 0, 0);
            SetBoundPointParamsBigNumber(120, 0, 0);
            SetBoundPointParamsBigNumber(35, 0, 0);
            SetBoundPointParamsBigNumber(107, 0, 0); 
            SetBoundPointParamsBigNumber(64, 0, 0); 
            //SetBoundPointParamsInteractive(); //人工设置边界点
            ComDisplace();       //求移位量
            this.UpdataCoords();      //更新坐标

           // StaticDis();
        }

        /// <summary>
        /// 通过设置边界条件进行移位
        /// </summary>
        public void DoDisplaceBoundForce()
        {
            AlgSnakes.isCurved = false;
            ComMatrix_K();       //求刚度矩阵
            //this.WriteMatrix(@"E:\map\实验数据\network", this._K, "K.txt");
            //MakeForceVector0();  //建立并计算力向量
            this.SetForceVectorInteractive();
            

            //this.WriteMatrix(@"E:\map\实验数据\network", this._Fx, "Fx.txt");
            //this.WriteMatrix(@"E:\map\实验数据\network", this._Fy, "Fy.txt");
            //this.WriteMatrix(@"E:\map\实验数据\network", this._Fy, "Fy.txt");
            //o-1法
            /* SetBoundPointParamsSE(this._NetWork.PointList.Count-1,0, 0);//设置边界条件
             SetBoundPointParamsSE(0, 133, -159);//设置边界条件*/
            //置大数法
            //SetBoundPointParamsBigNumber(this._NetWork.PointList.Count - 1, 0, 0);//
            /* SetBoundPointParamsBigNumber(13, 1330 / 500, -1590 / 500);
             SetBoundPointParamsBigNumber(76, 286 / 500, 1591 / 500);
             SetBoundPointParamsBigNumber(106, -2714 / 500, -1441 / 500);
            // SetBoundPointParamsBigNumber(104, -720, -300);
             SetBoundPointParamsBigNumber(48, -738 / 500, -1816 / 500);
             SetBoundPointParamsBigNumber(63, -1545 / 500, -719 / 500);*/

            /*  SetBoundPointParamsBigNumber(0, 0, 0);
            SetBoundPointParamsBigNumber(34, 0, 0);
           SetBoundPointParamsBigNumber(93, 0, 0);
           SetBoundPointParamsBigNumber(49, 0, 0);
           SetBoundPointParamsBigNumber(77, 0, 0);
           SetBoundPointParamsBigNumber(120, 0, 0);
           SetBoundPointParamsBigNumber(35, 0, 0);
           SetBoundPointParamsBigNumber(107, 0, 0);
           SetBoundPointParamsBigNumber(64, 0, 0);*/
            SetBoundPointParams();//设置边界条件

            ComDisplace();       //求移位量

          //  StaticDis();

            foreach (PointCoord curPoint in this._NetWork.PointList)
            {
                int index = curPoint.ID;
                //curPoint.X += this._dx[2 * index, 0] * AlgSnakes.scale / 1000;
                // curPoint.Y += this._dy[2 * index, 0] * AlgSnakes.scale / 1000;
                tdx[index] = 0;
                tdy[index] = 0;
                tdx[index] += this._dx[2 * index, 0];
                tdy[index] += this._dy[2 * index, 0];
            }

            StaticDis();

            if (this.maxF > 0)
            {
                double k = this.maxFD / this.maxF;
                AlgSnakes.a *= k;
                AlgSnakes.b *= k;

                foreach (PointCoord curPoint in this._NetWork.PointList)
                {
                    int index = curPoint.ID;
                    //curPoint.X += this._dx[2 * index, 0] * AlgSnakes.scale / 1000;
                    //curPoint.Y += this._dy[2 * index, 0] * AlgSnakes.scale / 1000;
                    tdx[index] = 0;
                    tdy[index] = 0;
                }

                ComMatrix_K();       //求刚度矩阵
                //this.WriteMatrix(@"E:\map\实验数据\network", this._K, "K.txt");
                this.SetForceVectorInteractive();

                /*   SetBoundPointParamsBigNumber(0, 0, 0);
             SetBoundPointParamsBigNumber(34, 0, 0);
                SetBoundPointParamsBigNumber(93, 0, 0);
                SetBoundPointParamsBigNumber(49, 0, 0);
                SetBoundPointParamsBigNumber(77, 0, 0);
                SetBoundPointParamsBigNumber(120, 0, 0);
                SetBoundPointParamsBigNumber(35, 0, 0);
                SetBoundPointParamsBigNumber(107, 0, 0);
                SetBoundPointParamsBigNumber(64, 0, 0);*/

                //SetBoundPointParamsInteractive(); //人工设置边界点
                SetBoundPointParams();//设置边界条件


                ComDisplace();       //求移位量
                this.UpdataCoords();      //更新坐标

                StaticDis();
            }

            WriteDandF(@"E:\map\实验数据\result", "FaD.txt");
        }


        /// <summary>
        /// 采用边界条件移位——建立受向量2014-1-14
        /// </summary>
        public void MakeForceVector0()
        {
            int n = this._NetWork.PointList.Count;
            _Fx = new Matrix(2 * n, 1);
            _Fy = new Matrix(2 * n, 1);
        }
        /// <summary>
        /// 置大数法
        /// </summary>
        public void SetBoundPointParamsBigNumber(int index, double dx, double dy)
        {
            int r1 = index * 2;
            int r2 = index * 2 + 1;

            for (int i = 0; i < _K.Col; i++)
            {
                if (i % 2 == 0)
                {
                    if (i == r1)
                    {
                        this._Fx[i, 0] = _K[r1, r1] * dx*100000000;
                        this._Fy[i, 0] = _K[r1, r1] * dy*100000000;
                    }
                    else if(i==r2)
                    {
                        this._Fx[i, 0] =0;
                        this._Fy[i, 0] =0;
                    }
                }
            }
            _K[r1, r1] = 100000000 * _K[r1, r1];
            _K[r2, r2] = 100000000 * _K[r2, r2];
        }
        /// <summary>
        /// 采用边界条件移位——设置边界点2014-1-14
        /// </summary>
        public void SetBoundPointParamsSE(int index,double dx,double dy)
        {
            int r1 = index * 2;
            int r2 = index * 2+1;

            for (int i = 0; i < _K.Col; i++)
            {
                if (i % 2 == 0)
                {
                    this._Fx[i, 0] = this._Fx[i, 0] - _K[i, r1] * dx;
                    this._Fy[i, 0] = this._Fy[i, 0] - _K[i, r1] * dy;

                    if (i == r1)
                    {
                        this._Fx[i, 0] = _K[r1, r1] * dx;
                        this._Fy[i, 0] = _K[r1, r1] * dy;
                    }
                }
            }

            for (int i = 0; i < _K.Col; i++)
            {
                if (i == r1)
                {
                   // _K[r1, i] = 1;//对角线上元素赋值为1
                }
                else
                {
                    _K[r1, i] = 0;//其他地方赋值为0
                    _K[i, r1] = 0;//其他地方赋值为0
                }

                if (i == r2)
                {
                    //_K[r2, i] = 1;//对角线上元素赋值为1
                }
                else
                {
                    _K[r2, i] = 0;//其他地方赋值为0
                    _K[i, r2] = 0;//其他地方赋值为0
                }
            }
        }

        /// <summary>
        /// 执行移位操作
        /// </summary>
        public void DoDispaceTri()
        {
            ComMatrix_K();       //求刚度矩阵
            //this.WriteMatrix(@"E:\map\实验数据\network", this._K, "K.txt");
            MakeForceVectorTri();  //建立并计算力向量

            //this.WriteMatrix(@"E:\map\实验数据\network", this._Fx, "Fx.txt");
            //this.WriteMatrix(@"E:\map\实验数据\network", this._Fy, "Fy.txt");
            //this.WriteMatrix(@"E:\map\实验数据\network", this._Fy, "Fy.txt");

            SetBoundPointParams();//设置边界条件
            //SetBoundPointParamsInteractive(); //人工设置边界点
            ComDisplace();       //求移位量
            UpdataCoordsTri();      //更新坐标

            StaticDis();
        }
        /// <summary>
        /// 统计移位量
        /// </summary>
        private void StaticDis()
        {
            int n = tdx.Length;
            max = -1;
            min = 9999999;
            sum = 0;
            indexMax = -1;
            indexMin = -1;
            indexMaxF = -1;
            indexMinF = -1;
            maxF = -1;
            minF = 999999999999;
            for (int i = 0; i < n; i++)
            {
                d[i] = Math.Sqrt(tdx[i] * tdx[i] + tdy[i] * tdy[i]);

                if (d[i] > max)
                {
                    indexMax = i;
                    max = d[i];
                }
                if (d[i] >= 0.0001 && d[i] < min)
                {
                    indexMin = i;
                    min = d[i];
                }

                if (this.forceList[i].F > maxF)
                {
                    indexMaxF = i;
                    maxF = (float)this.forceList[i].F;
                }
                if (this.forceList[i].F >= 0.0001 && this.forceList[i].F < minF)
                {
                    indexMinF = i;
                    minF = (float)this.forceList[i].F;
                }
            }


            double s = 0;
            double dif = 0;
            count = 0;
            for (int i = 0; i < n; i++)
            {
                if (d[i] >= 0.0001)
                {
                    count++;
                    dif = d[i] - ave;
                    s += dif * dif;
                    sum += d[i];
                }
            }
            std = Math.Sqrt(s / count);
            ave = sum / count;
            if (indexMaxF >= 0)
            {
                maxFD = (float)d[this.indexMaxF];

                
            }
            if (indexMax >= 0)
            {

                this.MaxF2 = (float)this.forceList[this.indexMax].F;
            }
        }

        /// <summary>
        /// 计算刚度矩阵
        /// </summary>
        private void ComMatrix_K()
        {
            if (AlgSnakes.isCurved == true)
            {
                foreach (Road curRoad in _NetWork.RoadList)
                {
                    foreach (RoadCurve curCuv in curRoad.RoadCurveList)
                    {
                        int n = curCuv.PointList.Count;
                        double a = curCuv.a;
                        double b = curCuv.b;
                        for (int i = 0; i < n - 1; i++)
                        {
                            CalcuLineMatrix(curCuv.GetCoord(i), curCuv.GetCoord(i + 1), a, b);
                        }
                    }
                }
            }
            else
            {
                foreach (Road curRoad in _NetWork.RoadList)
                {

                    int n = curRoad.PointList.Count;
                   // double a = curRoad.RoadGrade.a;
                    double b = curRoad.RoadGrade.b;
                    for (int i = 0; i < n - 1; i++)
                    {
                        CalcuLineMatrix(curRoad.GetCoord(i), curRoad.GetCoord(i + 1), AlgSnakes.a, AlgSnakes.b);

                    }
                }
            }
        }

        /// <summary>
        /// 计算线段单元的刚度矩阵
        /// </summary>
        /// <param name="line">线段对象</param>
        /// <returns>返回刚度矩阵4*4</returns>
        private void CalcuLineMatrix(PointCoord fromPoint, PointCoord toPoint, double a, double b)
        {

            //线段长度
            double h = Math.Sqrt((fromPoint.Y - toPoint.Y) * (fromPoint.Y - toPoint.Y) + (fromPoint.X - toPoint.X) * (fromPoint.X - toPoint.X));
            h = h * 1000 / AlgSnakes.scale;
            //计算该线段的刚度矩阵
            Matrix lineStiffMatrix = new Matrix(4, 4);
            //计算用的临时变量
            double temp1 = (6.0 * ((a * h * h + 10 * b))) / (5.0 * (h * h * h));
            double temp2 = (a * h * h + 60 * b) / (10.0 * (h * h));
            double temp3 = (2.0 * (a * h * h + 30 * b)) / (15.0 * h);
            double temp4 = (1.0 * (a * h * h - 60 * b)) / (30.0 * h);

            _K[fromPoint.ID * 2, fromPoint.ID * 2] += temp1;
            _K[toPoint.ID * 2, toPoint.ID * 2] += temp1;
            _K[fromPoint.ID * 2, fromPoint.ID * 2 + 1] += temp2;
            _K[fromPoint.ID * 2 + 1, fromPoint.ID * 2] += temp2;
            _K[toPoint.ID * 2 + 1, fromPoint.ID * 2]+= temp2;
            _K[fromPoint.ID * 2, toPoint.ID * 2 + 1] += temp2;
            _K[fromPoint.ID * 2, toPoint.ID * 2] += -1 * temp1;
            _K[toPoint.ID * 2, fromPoint.ID * 2] += -1 * temp1;
            _K[fromPoint.ID * 2 + 1, fromPoint.ID * 2 + 1] += temp3;
            _K[toPoint.ID * 2 + 1, toPoint.ID * 2 + 1] += temp3;
            _K[fromPoint.ID * 2 + 1, toPoint.ID * 2] += -1 * temp2;
            _K[toPoint.ID * 2, fromPoint.ID * 2 + 1] += -1 * temp2;
            _K[toPoint.ID * 2, toPoint.ID * 2 + 1] += -1 * temp2;
            _K[toPoint.ID * 2 + 1, toPoint.ID * 2] += -1 * temp2;
            _K[fromPoint.ID * 2 + 1, toPoint.ID * 2 + 1] += -1 * temp4;
            _K[toPoint.ID * 2 + 1, fromPoint.ID * 2 + 1] += -1 * temp4;
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
        /// 写入缓冲区图层
        /// </summary>
        public void BufferTest(IFeatureLayer Layer)
        {
            IFeatureClass featureClass = null;
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            ITopologicalOperator pTopop1 = null;
            IPolygon pgon1 = null;
            double bDis1 = 0.0;
            Polyline pline1 = null;
            featureClass = Layer.FeatureClass;
            if (featureClass == null)
                return;
            //获取顶点图层的数据集，并创建工作空间
            IDataset dataset = (IDataset)Layer;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
            //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
            IFeatureClassWrite fr = (IFeatureClassWrite)featureClass;
            //注意：此时，所编辑数据不能被其他程序打开
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            foreach (Road curRoad in this._NetWork.RoadList)
            {
                bDis1 = (0.5 * curRoad.RoadGrade.SylWidth + minDis) * AlgSnakes.scale * 0.001;
                pline1 = curRoad.EsriPolyline;
                pTopop1 = pline1 as ITopologicalOperator;
                pgon1 = pTopop1.Buffer(bDis1) as IPolygon;
                IFeature feature = featureClass.CreateFeature();
                feature.Shape = pgon1;
                feature.Store();//保存IFeature对象  
                fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上   
            }
            //关闭编辑
            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
        }

        /// <summary>
        /// 判断相交情况
        /// </summary>
        public void Cross(IFeatureLayer Layer)
        {
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;
            GeometryBagClass GeoSet = new GeometryBagClass();
            IFeatureCursor cursor = Layer.Search(null, false);
            IFeature curFeature = null;
            IGeometry shp = null;
            while ((curFeature = cursor.NextFeature()) != null)
            {
                shp = curFeature.Shape;
                GeoSet.AddGeometry(shp, ref missing1, ref missing2);
            }
            int n = GeoSet.GeometryCount;
            for (int i = 0; i < n; i++)
            {
                IGeometry shp1 = GeoSet.get_Geometry(i);
                IGeometry shp2 = null;
                for (int j = 0; j < n; j++)
                {
                    shp2 = GeoSet.get_Geometry(j);
                    if (this.CheckGeometryCrosses(shp1, shp2))
                    {
                        return;
                    }
                }
            }
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
                bDis1 = (0.5 * curRoad.RoadGrade.SylWidth + minDis) * AlgSnakes.scale * 0.001;
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
                            Dmin = (0.5 * (curRoad.RoadGrade.SylWidth + curRoad2.RoadGrade.SylWidth) + minDis) * AlgSnakes.scale * 0.001; ;
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
                                Dmin = (0.5 * (curRoad.RoadGrade.SylWidth + curlyr.RoadGrade.SylWidth) + minDis) * AlgSnakes.scale * 0.001; ;
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
                    Dmin =((0.5 * (sylWidthL + sylWidthP )+ AlgSnakes.minDis)) * 0.001 * AlgSnakes.scale;
                    int m = curLyrinfo.GeoSet.GeometryCount;
                    for(int j=0;j<m;j++)
                    {
                        curShape = curLyrinfo.GeoSet.get_Geometry(j);
                        GetProximityPoint_Distance(curPoint, curShape, out nearPoint, out nearDis);
                        if (nearDis == 0.0)//如果该点与冲突对象重合，暂不处理
                            continue;
                        //受力大小
                        absForce = 0.5*(Dmin - nearDis) *1000/ AlgSnakes.scale;
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
            Force[] forceV=this.ComForce_V();
            Force[] forceL = this.ComForce_L();

            int n = forceV.Length;
            for(int i=0;i<n;i++)
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
        /// 交互设置力
        /// </summary>
        /// <returns></returns>
        private Force[] ComForce_Interactive()
        {
            //所有点的数组
            List<PointCoord> pointList = _NetWork.PointList;
            int n = pointList.Count;
            //用于存受力的数组
            Force[] forceList = new Force[n];
            for (int i = 0; i < n; i++)
            {
      
                    forceList[i] = new Force();
                    forceList[i].Fx = 0;
                    forceList[i].Fy = 0;
                    forceList[i].F = 0;
            }
            return forceList;
        }

  

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
                case ForceType.Interactive:
                    return this.ComForce_Interactive();
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
                if (curF.F >0.0001)//判断受力为0的条件
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 建立受力向量
        /// </summary>
        private bool MakeForceVector()
        {
            int n = this._NetWork.PointList.Count;
            _Fx = new Matrix(2 * n, 1);
            _Fy = new Matrix(2 * n, 1);
            double h = 0.0;
            forceList = GetForce();//求受力
            //调试==手动添加力
            //SetForceVectorEle(5, 1330 * 1000 / AlgSnakes.scale, -1590 * 1000 / AlgSnakes.scale);

            if (!IsHasForce(forceList))
            {
                return false;
            }

            WriteForce(@"C:\学习\PHD\map\network", "F.txt", forceList);
            PointCoord fromPoint = null;
            PointCoord nextPoint = null;
            int index0 = -1;
            int index1 = -1;
            if (AlgSnakes.isCurved == true)//弯曲分段
            {
                foreach (Road curRoad in this._NetWork.RoadList)
                {
                    int m = curRoad.PointList.Count;
                    for (int i = 0; i < m - 1; i++)
                    {
                        fromPoint = curRoad.GetCoord(i);
                        nextPoint = curRoad.GetCoord(i + 1);
                        index0 = curRoad.PointList[i];
                        index1 = curRoad.PointList[i + 1];

                        h = LineLength(fromPoint, nextPoint);
                        h = h * 1000 / AlgSnakes.scale;

                        _Fx[2 * index0, 0] += 0.5 * h * forceList[index0].Fx;
                        _Fx[2 * index0 + 1, 0] += (1.0 / 12) * h * h * forceList[index0].Fx;
                        _Fx[2 * index1, 0] += 0.5 * h * forceList[index1].Fx;
                        _Fx[2 * index1 + 1, 0] += -(1.0 / 12) * h * h * forceList[index1].Fx;

                        _Fy[2 * index0, 0] += 0.5 * h * forceList[index0].Fy;
                        _Fy[2 * index0 + 1, 0] += (1.0 / 12) * h * h * forceList[index0].Fy;
                        _Fy[2 * index1, 0] += 0.5 * h * forceList[index1].Fy;
                        _Fy[2 * index1 + 1, 0] += -(1.0 / 12) * h * h * forceList[index1].Fy;
                    }
                }
            }

            else//不分段
            {
                foreach (Road curRoad in this._NetWork.RoadList)
                {
                    foreach (RoadCurve curCuv in curRoad.RoadCurveList)
                    {
                        int m = curCuv.PointList.Count;
                        for (int i = 0; i < m - 1; i++)
                        {
                            fromPoint = curCuv.GetCoord(i);
                            nextPoint = curCuv.GetCoord(i + 1);
                            index0 = curCuv.PointList[i];
                            index1 = curCuv.PointList[i + 1];

                            h = LineLength(fromPoint, nextPoint);
                            h = h * 1000 / AlgSnakes.scale;
                            _Fx[2 * index0, 0] += 0.5 * h * forceList[index0].Fx;
                            _Fx[2 * index0 + 1, 0] += (1.0 / 12) * h * h * forceList[index0].Fx;
                            _Fx[2 * index1, 0] += 0.5 * h * forceList[index1].Fx;
                            _Fx[2 * index1 + 1, 0] += -(1.0 / 12) * h * h * forceList[index1].Fx;

                            _Fy[2 * index0, 0] += 0.5 * h * forceList[index0].Fy;
                            _Fy[2 * index0 + 1, 0] += (1.0 / 12) * h * h * forceList[index0].Fy;
                            _Fy[2 * index1, 0] += 0.5 * h * forceList[index1].Fy;
                            _Fy[2 * index1 + 1, 0] += -(1.0 / 12) * h * h * forceList[index1].Fy;
                        }
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// 设置第i点的受力
        /// </summary>
        /// <param name="i">顶点序号</param>
        /// <param name="fx"></param>
        /// <param name="fy"></param>
        private void SetForceVectorEle(int i, double fx, double fy)
        {
            this.forceList[i].Fx = fx;
            this.forceList[i].Fy = fy;
            this.forceList[i].F=Math.Sqrt(fx*fx+fy*fy);
        }

        /// <summary>
        /// 交互设置设置受力-DoDisplaceCon专用
        /// </summary>
        /// <param name="i">顶点序号</param>
        /// <param name="fx"></param>
        /// <param name="fy"></param>
        private void SetForceVectorInteractive()
        {
            AlgSnakes.ForceType = ForceType.Interactive;
            this.forceList = this.GetForce();

         /*   SetForceVectorEle(13, 1330, -1590 );
            SetForceVectorEle(76, 286, 1591);
            SetForceVectorEle(106, -2714, -1441 );
            // SetBoundPointParamsBigNumber(104, -720, -300);
            SetForceVectorEle(48, -738, -1816 );
            SetForceVectorEle(63, -1545, -719 );*/
            SetForceVectorEle(5, 1330 * 1000 / AlgSnakes.scale, -1590 * 1000 / AlgSnakes.scale);

            int n = this._NetWork.PointList.Count;
            _Fx = new Matrix(2 * n, 1);
            _Fy = new Matrix(2 * n, 1);
            double h = 0.0;

            if (!IsHasForce(forceList))
            {
                return;
            }

            WriteForce(@"E:\map\实验数据\network", "F.txt", forceList);
            PointCoord fromPoint = null;
            PointCoord nextPoint = null;
            int index0 = -1;
            int index1 = -1;
            if (AlgSnakes.isCurved == true)//弯曲分段
            {
                foreach (Road curRoad in this._NetWork.RoadList)
                {
                    int m = curRoad.PointList.Count;
                    for (int i = 0; i < m - 1; i++)
                    {
                        fromPoint = curRoad.GetCoord(i);
                        nextPoint = curRoad.GetCoord(i + 1);
                        index0 = curRoad.PointList[i];
                        index1 = curRoad.PointList[i + 1];

                        h = LineLength(fromPoint, nextPoint);
                        h = h * 1000 / AlgSnakes.scale;

                        _Fx[2 * index0, 0] += 0.5 * h * forceList[index0].Fx;
                        _Fx[2 * index0 + 1, 0] += (1.0 / 12) * h * h * forceList[index0].Fx;
                        _Fx[2 * index1, 0] += 0.5 * h * forceList[index1].Fx;
                        _Fx[2 * index1 + 1, 0] += -(1.0 / 12) * h * h * forceList[index1].Fx;

                        _Fy[2 * index0, 0] += 0.5 * h * forceList[index0].Fy;
                        _Fy[2 * index0 + 1, 0] += (1.0 / 12) * h * h * forceList[index0].Fy;
                        _Fy[2 * index1, 0] += 0.5 * h * forceList[index1].Fy;
                        _Fy[2 * index1 + 1, 0] += -(1.0 / 12) * h * h * forceList[index1].Fy;
                    }
                }
            }

            else//不分段
            {
                foreach (Road curRoad in this._NetWork.RoadList)
                {
                    foreach (RoadCurve curCuv in curRoad.RoadCurveList)
                    {
                        int m = curCuv.PointList.Count;
                        for (int i = 0; i < m - 1; i++)
                        {
                            fromPoint = curCuv.GetCoord(i);
                            nextPoint = curCuv.GetCoord(i + 1);
                            index0 = curCuv.PointList[i];
                            index1 = curCuv.PointList[i + 1];

                            h = LineLength(fromPoint, nextPoint);
                            h = h * 1000 / AlgSnakes.scale;
                            _Fx[2 * index0, 0] += 0.5 * h * forceList[index0].Fx;
                            _Fx[2 * index0 + 1, 0] += (1.0 / 12) * h * h * forceList[index0].Fx;
                            _Fx[2 * index1, 0] += 0.5 * h * forceList[index1].Fx;
                            _Fx[2 * index1 + 1, 0] += -(1.0 / 12) * h * h * forceList[index1].Fx;

                            _Fy[2 * index0, 0] += 0.5 * h * forceList[index0].Fy;
                            _Fy[2 * index0 + 1, 0] += (1.0 / 12) * h * h * forceList[index0].Fy;
                            _Fy[2 * index1, 0] += 0.5 * h * forceList[index1].Fy;
                            _Fy[2 * index1 + 1, 0] += -(1.0 / 12) * h * h * forceList[index1].Fy;
                        }
                    }
                }
            }

        }

        /// <summary>
        /// 三角网探测冲突计算受力
        /// </summary>
        /// <param name="netWork"></param>
        public  Force[] ComForce_Triangle()
        {
            //所有点的数组
            List<PointCoord> pointList = this._NetWork.PointList;
            int n = pointList.Count;
            //用于存受力的数组
            Force[] forceArray = new Force[n];
            for (int i = 0; i < n; i++)
            {
                forceArray[i] = new Force();
            }

            foreach (ConflictDetection.Force curForce in ForceList)
            {
                int curID = curForce.ID;
                forceArray[curID].Fx += curForce.Fx;
                forceArray[curID].Fy += curForce.Fy;
            }

            for (int i = 0; i < n; i++)
            {
                forceArray[i].F = Math.Sqrt(forceArray[i].Fx * forceArray[i].Fx + forceArray[i].Fy * forceArray[i].Fy);
            }

            return forceArray;
        }

        /// <summary>
        /// 建立受力向量
        /// </summary>
        private bool MakeForceVectorTri()
        {
            int n = this._NetWork.PointList.Count;
            _Fx = new Matrix(2 * n, 1);
            _Fy = new Matrix(2 * n, 1);
            double h = 0.0;
            forceList = ComForce_Triangle();//求受力

           // WriteForce(@"E:\map\实验数据\network", "F.txt", forceList);
            PointCoord fromPoint = null;
            PointCoord nextPoint = null;
            int index0 = -1;
            int index1 = -1;
            if (AlgSnakes.isCurved == true)//弯曲分段
            {
                foreach (Road curRoad in this._NetWork.RoadList)
                {
                    int m = curRoad.PointList.Count;
                    for (int i = 0; i < m - 1; i++)
                    {
                        fromPoint = curRoad.GetCoord(i);
                        nextPoint = curRoad.GetCoord(i + 1);
                        index0 = curRoad.PointList[i];
                        index1 = curRoad.PointList[i + 1];

                        h = LineLength(fromPoint, nextPoint);
                        h = h * 1000 / AlgSnakes.scale;

                        _Fx[2 * index0, 0] += 0.5 * h * forceList[index0].Fx;
                        _Fx[2 * index0 + 1, 0] += (1.0 / 12) * h * h * forceList[index0].Fx;
                        _Fx[2 * index1, 0] += 0.5 * h * forceList[index1].Fx;
                        _Fx[2 * index1 + 1, 0] += -(1.0 / 12) * h * h * forceList[index1].Fx;

                        _Fy[2 * index0, 0] += 0.5 * h * forceList[index0].Fy;
                        _Fy[2 * index0 + 1, 0] += (1.0 / 12) * h * h * forceList[index0].Fy;
                        _Fy[2 * index1, 0] += 0.5 * h * forceList[index1].Fy;
                        _Fy[2 * index1 + 1, 0] += -(1.0 / 12) * h * h * forceList[index1].Fy;
                    }
                }
            }

            else//不分段
            {
                foreach (Road curRoad in this._NetWork.RoadList)
                {
                    foreach (RoadCurve curCuv in curRoad.RoadCurveList)
                    {
                        int m = curCuv.PointList.Count;
                        for (int i = 0; i < m - 1; i++)
                        {
                            fromPoint = curCuv.GetCoord(i);
                            nextPoint = curCuv.GetCoord(i + 1);
                            index0 = curCuv.PointList[i];
                            index1 = curCuv.PointList[i + 1];

                            h = LineLength(fromPoint, nextPoint);
                            h = h * 1000 / AlgSnakes.scale;
                            _Fx[2 * index0, 0] += 0.5 * h * forceList[index0].Fx;
                            _Fx[2 * index0 + 1, 0] += (1.0 / 12) * h * h * forceList[index0].Fx;
                            _Fx[2 * index1, 0] += 0.5 * h * forceList[index1].Fx;
                            _Fx[2 * index1 + 1, 0] += -(1.0 / 12) * h * h * forceList[index1].Fx;

                            _Fy[2 * index0, 0] += 0.5 * h * forceList[index0].Fy;
                            _Fy[2 * index0 + 1, 0] += (1.0 / 12) * h * h * forceList[index0].Fy;
                            _Fy[2 * index1, 0] += 0.5 * h * forceList[index1].Fy;
                            _Fy[2 * index1 + 1, 0] += -(1.0 / 12) * h * h * forceList[index1].Fy;
                        }
                    }
                }
            }
            return true;
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
        /// 人功交互设置边界点
        /// </summary>
        public void  SetBoundPointParamsInteractive()
        {
            SetBoundPointParams(40);
            SetBoundPointParams(75);
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
                if (curNode.ConRoadList.Count == 1 )
                {
                    index = curNode.PointID;

                    r1 = index * 2;
                    r2 = index * 2 + 1;

                    this._Fx[r1, 0] = 0;
                    this._Fx[r2, 0] = 0;

                    this._Fy[r1, 0] = 0;
                    this._Fy[r2, 0] = 0;

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
                            _K[r2, i] = 0;//对角线上元素赋值为1
                        }
                    }
                }
            }
        }

     

        /// <summary>
        ///设置边界点
        /// </summary>
        /// <param name="index">顶点索引号</param>
        private void SetBoundPointParams(int index)
        {
                int  r1 = index * 2;
                int r2 = index * 2 + 1;

                this._Fx[r1, 0] = 0;
                this._Fx[r2, 0] = 0;

                this._Fy[r1, 0] = 0;
                this._Fy[r2, 0] = 0;

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
                        _K[r2, i] = 0;//对角线上元素赋值为1
                    }
                }
            }
        

        /// <summary>
        /// 设置边界点的受力
        /// </summary>
        /// <param name="index">顶点索引号</param>
        private void SetBoundPointParamsForce(int index,float fx,float fy)
        {
            int r1 = index * 2;

            for (int i = 0; i < this._Fx.Row; i++)
            {
                if (i == r1)
                {
                    this._Fx[r1, 0] = fx;
                    this._Fy[r1, 0] = fy;
                }
                else
                {
                    this._Fx[i, 0] = this._Fx[i, 0] - fx * _K[i, r1];
                    this._Fy[i, 0] = this._Fy[i, 0] - fy * _K[i, r1];
                }
            }
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
            }
        }

        /// <summary>
        /// 设置边界条件
        /// </summary>
        private void SetBoundPointParamsForce()
        {
            int r1, r2;
            int index = 0;
            foreach (ConnNode curNode in _NetWork.ConnNodeList)
            {
                if (curNode.ConRoadList.Count == 1 && curNode.PointID != 16)
                {
                    index = curNode.PointID;
                    r1 = index * 2;
                    r2 = index * 2 + 1;
                    this._Fx[r1, 0] = 0;
                    this._Fx[r2, 0] = 0;
                    this._Fy[r1, 0] = 0;
                    this._Fy[r2, 0] = 0;
                }
            }
        }
        /// <summary>
        /// 计算移位向量
        /// </summary>
        private void ComDisplace()
        {

            this._dx = this._K.Inverse() * this._Fx;
            this._dy = this._K.Inverse() * this._Fy;
        }
      

        /// <summary>
        /// 计算移位向量
        /// </summary>
        public void DoDispaceIterate()
        {
            ComMatrix_K();       //求刚度矩阵
            if (!MakeForceVector())   //建立并计算力向量
            {
                return;
            }
           

            this.SetBoundPointParams();
           // SetBoundPointParamsForce(16, 1, 1);

            #region 求1+rK
            int n = this._K.Col;
            this._dx = new Matrix(n, 1);
            this._dy = new Matrix(n, 1);
            Matrix identityMatrix = new Matrix(n, n);
            for (int i = 0; i < n; i++)
            {
                this._Fx[i, 0] *= r;
                this._Fy[i, 0] *= r;

                this._dx[i, 0] = 0.0;
                this._dy[i, 0] = 0.0;

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


            for (int i = 0; i < time; i++)
            {
                this._Fx = this._dx + r*this._Fx;
                this._Fy = this._dy + r*this._Fy;

                this.SetBoundPointParamsForce();
               
                ComDisplace();

                this.UpdataCoords();

                CreategeoSetFromRes();

               
              if (!MakeForceVector())   //建立并计算力向量
                {
                    return;
                }
            }

            StaticDis();


        }

        /// <summary>
        /// 更新坐标位置
        /// </summary>
        private void UpdataCoords()
        {
            foreach (PointCoord curPoint in this._NetWork.PointList)
            {
                int index = curPoint.ID;
                curPoint.X += this._dx[2 * index, 0]*AlgSnakes.scale/1000;
                curPoint.Y += this._dy[2 * index, 0] * AlgSnakes.scale / 1000;
                tdx[index] += this._dx[2 * index, 0];
                tdy[index] += this._dy[2 * index, 0];
            }
        }

        /// <summary>
        /// 更新坐标位置--不考虑比例尺
        /// </summary>
        private void UpdataCoordsNoScale()
        {
            foreach (PointCoord curPoint in this._NetWork.PointList)
            {
                int index = curPoint.ID;
                curPoint.X += this._dx[2 * index, 0];
                curPoint.Y += this._dy[2 * index, 0];
                tdx[index] += this._dx[2 * index, 0];
                tdy[index] += this._dy[2 * index, 0];
            }
        }

        /// <summary>
        /// 更新坐标位置
        /// </summary>
        private void UpdataCoordsTri()
        {
            foreach (PointCoord curPoint in this._NetWork.PointList)
            {
                int index = curPoint.ID;
                curPoint.X += this._dx[2 * index, 0] * AlgSnakes.scale / 1000; 
                curPoint.Y += this._dy[2 * index, 0] * AlgSnakes.scale / 1000; 
                tdx[index] += this._dx[2 * index, 0];
                tdy[index] += this._dy[2 * index, 0];
            }
        }

        /// <summary>
        /// 更新坐标位置-不迭代
        /// </summary>
        private void UpdataCoords1()
        {
            foreach (PointCoord curPoint in this._NetWork.PointList)
            {
                int index = curPoint.ID;
                curPoint.X += this._dx[2 * index, 0] * AlgSnakes.scale / 1000;
                curPoint.Y += this._dy[2 * index, 0] * AlgSnakes.scale / 1000;
                tdx[index] = this._dx[2 * index, 0];
                tdy[index] = this._dy[2 * index, 0];
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
        /// 清空道路图层的所有图形对象
        /// </summary>
        private void ClearRoadLyrGeoSet()
        {
            foreach (RoadLyrInfo curLyrInfo in this._NetWork.RoadLyrInfoList)
            {
                if (curLyrInfo.RoadGrade.Grade!= 999)
                {
                    int count=curLyrInfo.GeoSet.GeometryCount;
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
            RoadLyrInfo curLyrInfo=null;
            foreach (Road curRoad in this._NetWork.RoadList)
            {
                lyrName=curRoad.RoadGrade.LyrName;
                RoadLyrInfo lyrInfo=GetLyrinfo(lyrName);
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
                if(lyrInfo!=null)
                {
                    lyrInfo.GeoSet.AddGeometry(shp, ref missing1, ref missing2);
                }
            }
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
        /// 输出移位值和受力到文件中
        /// </summary>
        /// <param name="filepath">路径</param>
        /// <param name="fileName">文件</param>
        public void WriteDandF(string filepath, string fileName)
        {
            StreamWriter streamw = File.CreateText(filepath + "\\" + fileName);
            for (int i = 0; i < this.d.Length; i++)
            {
                streamw.WriteLine(i.ToString() + " " + d[i].ToString() + " " + this.forceList[i].F.ToString());
            }
            streamw.Close();
        }
        /// <summary>
        /// 输出受力
        /// </summary>
        /// <param name="filepath">文件夹名</param>
        /// <param name="fileName">文件名</param>
        /// <param name="forceList">受力数组</param>
        public void WriteForce(string filepath, string fileName, Force[] forceList)
        {
          
            StreamWriter streamw = File.CreateText(filepath + "\\" + fileName);
            int n = forceList.Length;
            for (int i = 0; i < n; i++)
            {
                streamw.Write(i.ToString() + "  " + forceList[i].Fx.ToString() + "  " + forceList[i].Fy.ToString());
                streamw.WriteLine();
            }
            streamw.Close();
        }
        /// <summary>
        /// 返回道路分级模型
        /// </summary>
        /// <returns></returns>
        public static string GetRoadGradeModelType()
        {
            string strRoadGradeModelTyp = "";
            switch (AlgSnakes.GradeModelType)
            {
                case GradeModelType.Grade:
                    strRoadGradeModelTyp = "绝对等级模型";
                    break;
                case GradeModelType.Ratio:
                    strRoadGradeModelTyp = "比率等级模型";
                    break;
                    case  GradeModelType.Interactive:
                    strRoadGradeModelTyp = "用户交互";
                     break;
                case  GradeModelType.Squence:
                     strRoadGradeModelTyp = "等差等级模型";
                     break;
            }
            return strRoadGradeModelTyp;
        }
        /// <summary>
        /// 返回剩余冲突点的ID
        /// </summary>
        /// <returns>ID列表</returns>
        public List<int> GetConflictNo()
        {
            List<int>  nolist = new List<int>();
            for (int i=0;i<this.forceList.Length;i++)
            {
                if (forceList[i].Fx >= 0.0001 || forceList[i].Fy >= 0.0001)
                {
                    nolist.Add(i);
                }
            }
            return nolist;
        }
    }
}
