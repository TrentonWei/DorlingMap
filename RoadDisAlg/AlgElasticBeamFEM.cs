using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatrixOperation;

namespace RoadDisAlg
{
    /// <summary>
    /// 弹性杆件方法实现
    /// </summary>
    public class AlgElasticBeamFEM:AlgFEM
    {
        public static double A = 0.0002F;                           //横截面积，尚不好确定-一般设为符号宽度的1/10，地图单位;而武芳：A=k*d*d
        public static double E = 0.00001F;                //弹性模量,一般设为1-2
        public static double I = 0.00001F;                           //惯性力,一般为符号宽度的两倍与平均曲率的乘积
        public static double c1 = 0.2;
        public static double k1 = 1.1;                                //一般弯曲的最小值
        public static double k2 = 1.8;                                //一般弯曲的最大值

        public static double g1 = 10;                                         //a的公比
        public static double d1 = 1000;                                       //a的公差

        public static double c = 100;                                //抛物线最小值

        public static double r = 1;                               //迭代步长
        public static int time =4;                                  //迭代次数

        public static StatisticDisValue statisticDisValue;
        public static bool isCurved = true;                         //是否分段设置形状参数

        private Matrix _K = null;                                            //刚度矩阵
        private Matrix _F = null;                                            //受力向量                            
        private Matrix _d = null;                                            //移位结果

        /// <summary> 
        /// 构造函数
        /// </summary>
        /// <param name="netWork">待移位的道路网对象</param>
        public AlgElasticBeamFEM(RoadNetWork netWork)
        {
            _NetWork = netWork;
            int n = netWork.PointList.Count;
            _K = new Matrix(3 * n, 3 * n);

            tdx = new double[n];                                    //记录X方向最终的累积移位量
            tdy = new double[n];                                    //记录Y方向最终的累积移位量
            d = new double[n];
        }
        /// <summary>
        /// 不迭代
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
        /// 迭代
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
            StaticDis();
        }

        /// <summary>
        /// 计算刚度矩阵——？？待分段？？
        /// </summary>
        private void ComMatrix_K()
        {
            if (AlgElasticBeamFEM.isCurved == true)
            {
                foreach (Road curRoad in _NetWork.RoadList)
                {
                    foreach (RoadCurve curCuv in curRoad.RoadCurveList)
                    {
                        int n = curCuv.PointList.Count;
                        double E = curCuv.E;
                        double I = curCuv.I;
                        double A = curCuv.A;
                        for (int i = 0; i < n - 1; i++)
                        {
                            CalcuLineMatrix(curCuv.GetCoord(i), curCuv.GetCoord(i + 1),E,I,A);
                        }
                    }
                }
            }
            else
            {
                foreach (Road curRoad in _NetWork.RoadList)
                {

                    int n = curRoad.PointList.Count;
                    double E = curRoad.RoadGrade.E;
                    double I = curRoad.RoadGrade.I;
                    double A = curRoad.RoadGrade.A;
                    for (int i = 0; i < n - 1; i++)
                    {
                        CalcuLineMatrix(curRoad.GetCoord(i), curRoad.GetCoord(i + 1),E,I,A);
                    }
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

            _K[i * 3 + 1, i * 3 + 1] += ASSIL2CC;
            _K[j * 3 + 1, j * 3 + 1] += ASSIL2CC;

            _K[i * 3 + 1, j * 3 + 1] += -1 * ASSIL2CC;
            _K[j * 3 + 1, i * 3 + 1] += -1 * ASSIL2CC;

            _K[i * 3, i * 3 + 1] += EL * AIL2 * cs;
            _K[i * 3 + 1, i * 3] += EL * AIL2 * cs;

            _K[i * 3, i * 3 + 2] += -1 * EL * IL1 * sin;
            _K[i * 3 + 2, i * 3] += -1 * EL * IL1 * sin;
            _K[i * 3, j * 3 + 2] += -1 * EL * IL1 * sin;
            _K[j * 3 + 2, i * 3] += -1 * EL * IL1 * sin;

            _K[i * 3 + 1, i * 3 + 2] += EL * IL1 * cos;
            _K[i * 3 + 2, i * 3 + 1] += EL * IL1 * cos;

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

            _K[j * 3, j * 3 + 1] += EL * AIL2 * cs;
            _K[j * 3 + 1, j * 3] += EL * AIL2 * cs;

            _K[i * 3 + 1, j * 3 + 2] += EL * IL1 * cos;
            _K[j * 3 + 2, i * 3 + 1] += EL * IL1 * cos;

            _K[j * 3, j * 3 + 2] += EL * IL1 * sin;
            _K[j * 3 + 2, j * 3] += EL * IL1 * sin;

            _K[j * 3 + 1, j * 3 + 2] += -1 * EL * IL1 * cos;
            _K[j * 3 + 2, j * 3 + 1] += -1 * EL * IL1 * cos;

            _K[i * 3 + 1, j * 3] += -1 * EL * AIL2 * cs;
            _K[j * 3, i * 3 + 1] += -1 * EL * AIL2 * cs;
        }

        /// <summary>
        /// 计算线段单元的刚度矩阵
        /// </summary>
        /// <param name="line">线段对象</param>
        /// <returns>返回刚度矩阵4*4</returns>
        private void CalcuLineMatrix(PointCoord fromPoint, PointCoord toPoint, double E, double I, double A)
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

            _K[i * 3 + 1, i * 3 + 1] += ASSIL2CC;
            _K[j * 3 + 1, j * 3 + 1] += ASSIL2CC;

            _K[i * 3 + 1, j * 3 + 1] += -1 * ASSIL2CC;
            _K[j * 3 + 1, i * 3 + 1] += -1 * ASSIL2CC;

            _K[i * 3, i * 3 + 1] += EL * AIL2 * cs;
            _K[i * 3 + 1, i * 3] += EL * AIL2 * cs;

            _K[i * 3, i * 3 + 2] += -1 * EL * IL1 * sin;
            _K[i * 3 + 2, i * 3] += -1 * EL * IL1 * sin;
            _K[i * 3, j * 3 + 2] += -1 * EL * IL1 * sin;
            _K[j * 3 + 2, i * 3] += -1 * EL * IL1 * sin;

            _K[i * 3 + 1, i * 3 + 2] += EL * IL1 * cos;
            _K[i * 3 + 2, i * 3 + 1] += EL * IL1 * cos;

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

            _K[j * 3, j * 3 + 1] += EL * AIL2 * cs;
            _K[j * 3 + 1, j * 3] += EL * AIL2 * cs;

            _K[i * 3 + 1, j * 3 + 2] += EL * IL1 * cos;
            _K[j * 3 + 2, i * 3 + 1] += EL * IL1 * cos;

            _K[j * 3, j * 3 + 2] += EL * IL1 * sin;
            _K[j * 3 + 2, j * 3] += EL * IL1 * sin;

            _K[j * 3 + 1, j * 3 + 2] += -1 * EL * IL1 * cos;
            _K[j * 3 + 2, j * 3 + 1] += -1 * EL * IL1 * cos;

            _K[i * 3 + 1, j * 3] += -1 * EL * AIL2 * cs;
            _K[j * 3, i * 3 + 1] += -1 * EL * AIL2 * cs;
        }


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

            forceList =ComForce.GetForce(this._NetWork);//求受力

            if (!ComForce.IsHasForce(forceList))
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

                    L = CommonRes.LineLength(fromPoint, nextPoint);
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

                    r1 = index * 3;
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

                        if (i == r3)
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

        /// <summary>
        /// 计算移位向量
        /// </summary>
        private void ComDisplace()
        {

            this._d = this._K.Inverse() * this._F;
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
                curPoint.Y += this._d[3 * index + 1, 0];

                tdx[index] += this._d[3 * index, 0];
                tdy[index] += this._d[3 * index + 1, 0];
            }
        }

    }
}
