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



            //_K[0, 0] += (A * cos * cos + ((12 * I) / (L * L)) * sin * sin)*(E/L);
            //_K[0, 1] += ((A - (12 * I) / (L * L)) * sin * cos)*(E/L);
            //_K[0, 2] += (-1 * (6 * I / L) * sin)*(E/L);
            //_K[0, 3] += (-1 * (A * cos * cos + ((12 * I) / (L * L)) * sin * sin))*(E/L);
            //_K[0, 4] += (-1 * (A - (12 * I) / (L * L)) * cos * sin)*(E/L);
            //_K[0, 5] += (-1 * ((6 * I) / (L)) * sin)*(E/L);

            //_K[1, 0] += _K[0, 1];
            //_K[1, 1] += (A * sin * sin + ((12 * I) / (L * L)) * cos * cos)*(E/L);
            //_K[1, 2] += 6 * (I / L) * cos*(E/L);
            //_K[1, 3] += (-1 * (A - ((12 * I) / (L * L))) * cos * sin)*(E/L);
            //_K[1, 4] += (-1 * A * sin * sin + ((12 * I) / (L * L)) * cos * cos)*(E/L);//注意
            //_K[1, 5] += (6 * I / L) * cos * (E / L);

            //_K[2, 0] += _K[0, 2];
            //_K[2, 1] += _K[1, 2];
            //_K[2, 2] += (4 * I) * (E / L);
            //_K[2, 3] += (6 * I / L) * sin* (E / L);
            //_K[2, 4] += -1 * (6 * I / L) * cos * (E / L);
            //_K[2, 5] += (2 * I) * (E / L);

            //_K[3, 0] += _K[0, 3];
            //_K[3, 1] += _K[1, 3];
            //_K[3, 2] += _K[2, 3];
            //_K[3, 3] += _K[0, 0];
            //_K[3, 4] += (A - ((12 * I) / (L * L))) * sin * cos * (E / L);
            //_K[3, 5] += (6 * I / L) * sin* (E / L);//到底是负的还是正的？

            //_K[4, 0] += _K[0, 4];
            //_K[4, 1] += _K[1, 4];
            //_K[4, 2] += _K[2, 4];
            //_K[4, 3] += _K[3, 4];
            //_K[4, 4] += (A * sin * sin + ((12 * I) / (L * L)) * cos * cos) * (E / L);
            //_K[4, 5] += -1 * (6 * I / L) * cos *(E / L);

            //_K[5, 0] += _K[0, 5];
            //_K[5, 1] += _K[1, 5];
            //_K[5, 2] += _K[2, 5];
            //_K[5, 3] += _K[3, 5];
            //_K[5, 4] += _K[4, 5];
            //_K[5, 5] += (4 * I) * (E / L);



            //_K[3 * i, 3 * i] += (A * cos * cos + ((12 * I) / (L * L)) * sin * sin) * (E / L);
            //_K[3 * i, 3 * i + 1] += ((A - (12 * I) / (L * L)) * sin * cos) * (E / L);
            //_K[3 * i, 3 * i + 2] += (-1 * (6 * I / L) * sin) * (E / L);
            //_K[3 * i, 3 * j] += (-1 * (A * cos * cos + ((12 * I) / (L * L)) * sin * sin)) * (E / L);
            //_K[3 * i, 3 * j + 1] += (-1 * (A - (12 * I) / (L * L)) * cos * sin) * (E / L);
            //_K[3 * i, 3 * j + 2] += (-1 * ((6 * I) / (L)) * sin) * (E / L);

            //_K[3 * i + 1, 3 * i] += _K[3 * i, 3 * i + 1];
            //_K[3 * i + 1, 3 * i + 1] += (A * sin * sin + ((12 * I) / (L * L)) * cos * cos) * (E / L);
            //_K[3 * i + 1, 3 * i + 2] += 6 * (I / L) * cos * (E / L);
            //_K[3 * i + 1, 3 * j] += (-1 * (A - ((12 * I) / (L * L))) * cos * sin) * (E / L);
            //_K[3 * i + 1, 3 * j + 1] += -1 * (A * sin * sin + ((12 * I) / (L * L)) * cos * cos) * (E / L);//注意
            //_K[3 * i + 1, 3 * j + 2] += (6 * I / L) * cos * (E / L);

            //_K[3 * i + 2, 3 * i] += _K[3 * i, 3 * i + 2];
            //_K[3 * i + 2, 3 * i + 1] += _K[3 * i + 1, 3 * i + 2];
            //_K[3 * i + 2, 3 * i + 2] += (4 * I) * (E / L);
            //_K[3 * i + 2, 3 * j] += (6 * I / L) * sin * (E / L);
            //_K[3 * i + 2, 3 * j + 1] += -1 * (6 * I / L) * cos * (E / L);
            //_K[3 * i + 2, 3 * j + 2] += (2 * I) * (E / L);

            //_K[3 * j, 3 * i] += _K[3 * i, 3 * j];
            //_K[3 * j, 3 * i + 1] += _K[3 * i + 1, 3 * j];
            //_K[3 * j, 3 * i + 2] += _K[3 * i + 2, 3 * j];
            //_K[3 * j, 3 * j] += _K[3 * i, 3 * i];
            //_K[3 * j, 3 * j + 1] += (A - ((12 * I) / (L * L))) * sin * cos * (E / L);
            //_K[3 * j, 3 * j + 2] += (6 * I / L) * sin * (E / L);//到底是负的还是正的？

            //_K[3 * j + 1, 3 * i] += _K[3 * i, 3 * j + 1];
            //_K[3 * j + 1, 3 * i + 1] += _K[3 * i + 1, 3 * j + 1];
            //_K[3 * j + 1, 3 * i + 2] += _K[3 * i + 2, 3 * j + 1];
            //_K[3 * j + 1, 3 * j] += _K[3 * j, 3 * j + 1];
            //_K[3 * j + 1, 3 * j + 1] += (A * sin * sin + ((12 * I) / (L * L)) * cos * cos) * (E / L);
            //_K[3 * j + 1, 3 * j + 2] += -1 * (6 * I / L) * cos * (E / L);

            //_K[3 * j + 2, 3 * i] += _K[3 * i, 3 * j + 2];
            //_K[3 * j + 2, 3 * i + 1] += _K[3 * i + 1, 3 * j + 2];
            //_K[3 * j + 2, 3 * i + 2] += _K[3 * i + 2, 3 * j + 2];
            //_K[3 * j + 2, 3 * j] += _K[3 * j, 3 * j + 2];
            //_K[3 * j + 2, 3 * j + 1] += _K[3 * j + 1, 3 * j + 2];
            //_K[3 * j + 2, 3 * j + 2] += (4 * I) * (E / L);
        }
    }
}
