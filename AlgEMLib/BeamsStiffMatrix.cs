using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;
using MatrixOperation;

namespace AlgEMLib
{
    public class BeamsStiffMatrix : StiffMatrix
    {
        public double E;
        public double I;
        public double A;

        public double[,] Test_K = null;

        /// <summary>
        /// 构造函数-邻近图刚度矩阵
        /// </summary>
        /// <param name="proxiGraph"></param>
        /// <param name="e"></param>
        /// <param name="i"></param>
        /// <param name="a"></param>
        public BeamsStiffMatrix(ProxiGraph proxiGraph, double e, double i, double a)
        {
            this.E = e;
            this.I = i;
            this.A = a;
            int n = proxiGraph.NodeList.Count;
            this._K = new MatrixOperation.Matrix(3 * n, 3 * n);
            CalStiffMatrix(proxiGraph);
        }

        /// 构造函数-邻近图刚度矩阵
        /// </summary>
        /// <param name="proxiGraph"></param>
        /// <param name="e"></param>
        /// <param name="i"></param>
        /// <param name="a"></param>
        public BeamsStiffMatrix(ProxiGraph proxiGraph, double e, double i, double a,int test)
        {
            this.E = e;
            this.I = i;
            this.A = a;
            int n = proxiGraph.NodeList.Count;
            this.Test_K = new double[3 * n, 3 * n];
            CalStiffMatrix_2(proxiGraph);
        }

        /// <summary>
        /// 构造函数-道路网
        /// </summary>
        /// <param name="proxiGraph"></param>
        /// <param name="e"></param>
        /// <param name="i"></param>
        /// <param name="a"></param>
        public BeamsStiffMatrix(List<PolylineObject> polylineList,int n, double e, double i, double a)
        {
            this.E = e;
            this.I = i;
            this.A = a;

            this._K = new MatrixOperation.Matrix(3 * n, 3 * n);
            CalStiffMatrix(polylineList);
        }
        /// <summary>
        /// 构造函数-线对象刚度矩阵
        /// </summary>
        /// <param name="proxiGraph"></param>
        /// <param name="e"></param>
        /// <param name="i"></param>
        /// <param name="a"></param>
        public BeamsStiffMatrix(PolylineObject polyline, double e, double i, double a)
        {
            this.E = e;
            this.I = i;
            this.A = a;
            int n = polyline.PointList.Count;
            this._K = new MatrixOperation.Matrix(3 * n, 3 * n);
            CalStiffMatrix(polyline);
        }

        /// <summary>
        /// 构造函数-计算邻近图的刚度矩阵
        /// </summary>
        /// <param name="proxiGraph"></param>
        private void CalStiffMatrix(ProxiGraph proxiGraph)
        {
            foreach (ProxiEdge curEdge in proxiGraph.EdgeList)
            {
                CalcuLineMatrix(curEdge.Node1, curEdge.Node2);
            }
        }

        /// <summary>
        /// 构造函数-计算邻近图的刚度矩阵
        /// </summary>
        /// <param name="proxiGraph"></param>
        private void CalStiffMatrix_2(ProxiGraph proxiGraph)
        {
            foreach (ProxiEdge curEdge in proxiGraph.EdgeList)
            {
                CalcuLineMatrix_2(curEdge.Node1, curEdge.Node2);
            }
        }

        /// <summary>
        /// 构造函数-计算线对象的刚度矩阵
        /// </summary>
        /// <param name="proxiGraph"></param>
        private void CalStiffMatrix(PolylineObject polyline)
        {

            for (int i = 0; i < polyline.PointList.Count - 1; i++)
            {
                CalcuLineMatrix(polyline.PointList[i], polyline.PointList[i + 1]);
            }
        }


        /// <summary>
        /// 构造函数-计算道路网的刚度矩阵
        /// </summary>
        /// <param name="proxiGraph"></param>
        private void CalStiffMatrix(List<PolylineObject> polylineList)
        {
            foreach (PolylineObject polyline in polylineList)
            {
                for (int i = 0; i < polyline.PointList.Count - 1; i++)
                {
                    CalcuLineMatrix(polyline.PointList[i], polyline.PointList[i + 1]);
                }
            }
        }
        /// <summary>
        /// 刚度矩阵
        /// </summary>
        public Matrix Matrix_K
        {
            get { return this._K; }
        }

        /// <summary>
        /// 计算线段单元的刚度矩阵
        /// </summary>
        /// <param name="line">线段对象</param>
        /// <returns>返回刚度矩阵4*4</returns>
        private void CalcuLineMatrix(Node fromPoint, Node toPoint)
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
        private void CalcuLineMatrix_2(Node fromPoint, Node toPoint)
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
            double ACCIL2SS = EL * (A * cc + IL2 * ss);
            double ASSIL2CC = EL * (A * ss + IL2 * cc);
            double AIL2 = A - IL2;

            Test_K[i * 3, i * 3] += ACCIL2SS;
            Test_K[j * 3, j * 3] += ACCIL2SS;

            Test_K[i * 3, j * 3] += -1 * ACCIL2SS;
            Test_K[j * 3, i * 3] += -1 * ACCIL2SS;

            Test_K[i * 3 + 1, i * 3 + 1] += ASSIL2CC;
            Test_K[j * 3 + 1, j * 3 + 1] += ASSIL2CC;

            Test_K[i * 3 + 1, j * 3 + 1] += -1 * ASSIL2CC;
            Test_K[j * 3 + 1, i * 3 + 1] += -1 * ASSIL2CC;

            Test_K[i * 3, i * 3 + 1] += EL * AIL2 * cs;
            Test_K[i * 3 + 1, i * 3] += EL * AIL2 * cs;

            Test_K[i * 3, i * 3 + 2] += -1 * EL * IL1 * sin;
            Test_K[i * 3 + 2, i * 3] += -1 * EL * IL1 * sin;
            Test_K[i * 3, j * 3 + 2] += -1 * EL * IL1 * sin;
            Test_K[j * 3 + 2, i * 3] += -1 * EL * IL1 * sin;

            Test_K[i * 3 + 1, i * 3 + 2] += EL * IL1 * cos;
            Test_K[i * 3 + 2, i * 3 + 1] += EL * IL1 * cos;

            Test_K[i * 3 + 2, j * 3] += EL * IL1 * sin;
            Test_K[j * 3, i * 3 + 2] += EL * IL1 * sin;

            Test_K[i * 3 + 2, j * 3 + 1] += -1 * EL * IL1 * cos;
            Test_K[j * 3 + 1, i * 3 + 2] += -1 * EL * IL1 * cos;

            Test_K[i * 3 + 2, i * 3 + 2] += 4 * EL * I;
            Test_K[j * 3 + 2, j * 3 + 2] += 4 * EL * I;

            Test_K[i * 3 + 2, j * 3 + 2] += 2 * EL * I;
            Test_K[j * 3 + 2, i * 3 + 2] += 2 * EL * I;

            Test_K[i * 3, j * 3 + 1] += -1 * EL * AIL2 * cs;
            Test_K[j * 3 + 1, i * 3] += -1 * EL * AIL2 * cs;

            Test_K[j * 3, j * 3 + 1] += EL * AIL2 * cs;
            Test_K[j * 3 + 1, j * 3] += EL * AIL2 * cs;

            Test_K[i * 3 + 1, j * 3 + 2] += EL * IL1 * cos;
            Test_K[j * 3 + 2, i * 3 + 1] += EL * IL1 * cos;

            Test_K[j * 3, j * 3 + 2] += EL * IL1 * sin;
            Test_K[j * 3 + 2, j * 3] += EL * IL1 * sin;

            Test_K[j * 3 + 1, j * 3 + 2] += -1 * EL * IL1 * cos;
            Test_K[j * 3 + 2, j * 3 + 1] += -1 * EL * IL1 * cos;

            Test_K[i * 3 + 1, j * 3] += -1 * EL * AIL2 * cs;
            Test_K[j * 3, i * 3 + 1] += -1 * EL * AIL2 * cs;
        }
    }
}
