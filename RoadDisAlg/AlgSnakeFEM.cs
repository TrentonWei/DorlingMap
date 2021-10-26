using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using MatrixOperation;
using ESRI.ArcGIS.Geometry;

namespace RoadDisAlg
{
    public class AlgSnakeFEM:AlgFEM
    {
       

        public static double a = 10000000;                              //弹性参数初值
        public static double b = 1000000;                              //刚性参数初值
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
       //public static double scale = 500000.0;                       //比例尺分母
       //public static ForceType ForceType=ForceType.Vetex;
       //public static GradeModelType GradeModelType = GradeModelType.Ratio;  //分级模式
        
        public static double g1 = 10;                                         //a的公比
        public static double d1 = 1000;                                       //a的公差
        public static double g2 = 10;                                        //b的公比
        public static double d2 = 1000;                                       //a的公差

        public static DataTable dtPara = null;                               //参数表格 

        private Matrix _K = null;                                      //刚度矩阵
        private Matrix _Fx = null;                                      //X方向受力向量
        private Matrix _Fy = null;                                     //Y方向受力向量
        private Matrix _dx = null;                                      //移位结果
        private Matrix _dy = null;                                      //移位结果

        /// <summary> 
        /// 构造函数
        /// </summary>
        /// <param name="netWork">待移位的道路网对象</param>
        public AlgSnakeFEM(RoadNetWork netWork)
        {
            _NetWork = netWork;
            int n = netWork.PointList.Count;
            _K = new Matrix(2 * n, 2 * n);

            tdx = new double[n];                                    //记录X方向最终的累积移位量
            tdy = new double[n];                                    //记录Y方向最终的累积移位量
            d = new double[n];
        }

        /// <summary>
        /// 移位
        /// </summary>
        public override void DoDispace()
        {
            ComMatrix_K();       //求刚度矩阵
            if (!MakeForceVector())   //建立并计算力向量
            {
                return;
            }

            SetBoundPointParams();//设置边界条件
            ComDisplace();       //求移位量
            UpdataCoords();      //更新坐标
            StaticDis();
        }

        /// <summary>
        /// 移位迭代
        /// </summary>
        public override void DoDispaceIterate()
        {
            ComMatrix_K();       //求刚度矩阵
            if (!MakeForceVector())   //建立并计算力向量
            {
                return;
            }
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

            this.SetBoundPointParams();

            for (int i = 0; i < time; i++)
            {
                this._Fx = this._dx + r * this._Fx;
                this._Fy = this._dy + r * this._Fy;

                this.SetBoundPointParamsForce();

                ComDisplace();

                this.UpdataCoords();

                CreategeoSetFromRes();//创建新的Geometry，以便于下一次迭代时计算受力
                if (!MakeForceVector())   //建立并计算力向量
                {
                    return;
                }
            }
            StaticDis();
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
                if (curNode.ConRoadList.Count == 1)
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
        /// 更新坐标位置
        /// </summary>
        private void UpdataCoords()
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
        /// 计算移位向量
        /// </summary>
        private void ComDisplace()
        {
            this._dx = this._K.Inverse() * this._Fx;
            this._dy = this._K.Inverse() * this._Fy;
        }


        /// <summary>
        /// 计算刚度矩阵
        /// </summary>
        private void ComMatrix_K()
        {
            if (AlgSnakeFEM.isCurved == true)
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
                    double a = curRoad.RoadGrade.a;
                    double b = curRoad.RoadGrade.b;
                    for (int i = 0; i < n - 1; i++)
                    {
                        CalcuLineMatrix(curRoad.GetCoord(i), curRoad.GetCoord(i + 1), a, b);
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
           // h = 1000*(h / AlgFEM.scale);

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
            _K[toPoint.ID * 2 + 1, fromPoint.ID * 2] += temp2;
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
        /// 建立受力向量
        /// </summary>
        private bool MakeForceVector()
        {
            int n = this._NetWork.PointList.Count;
            _Fx = new Matrix(2 * n, 1);
            _Fy = new Matrix(2 * n, 1);
            double h = 0.0;
            forceList = ComForce.GetForce(this._NetWork);//求受力

            if (!ComForce.IsHasForce(forceList))
            {
                return false;
            }

            //WriteForce(@"E:\map\实验数据\network", "F.txt", forceList);
            PointCoord fromPoint = null;
            PointCoord nextPoint = null;
            int index0 = -1;
            int index1 = -1;
            if (AlgSnakeFEM.isCurved == true)//弯曲分段
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

                        h = CommonRes.LineLength(fromPoint, nextPoint);

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

                            h = CommonRes.LineLength(fromPoint, nextPoint);

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
    }
}
