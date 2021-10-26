using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;
using MatrixOperation;

namespace AlgEMLib
{
    /// <summary>
    /// Snakes刚度矩阵
    /// </summary>
    public class SnakesStiffMatrix : StiffMatrix
    {
        public double a;
        public double b;

        /// <summary>
        /// 构造函数-单个线对象
        /// </summary>
        /// <param name="polyline">线对象</param>
        /// <param name="a">形状参数-弹性系数</param>
        /// <param name="b">形状参数-刚性系数</param>
        public SnakesStiffMatrix(PolylineObject polyline,  double a, double b)
        {
            this.a = a;
            this.b = b;
            int n = polyline.PointList.Count;
            this._K = new MatrixOperation.Matrix(2 * n, 2* n);
            CalStiffMatrix(polyline);
        }
                /// <summary>
        /// 构造函数-邻近图刚度矩阵
        /// </summary>
        /// <param name="proxiGraph"></param>
        /// <param name="e"></param>
        /// <param name="i"></param>
        /// <param name="a"></param>
        public SnakesStiffMatrix(ProxiGraph proxiGraph, double a, double b)
        {
            this.a = a;
            this.b = b;
            int n = proxiGraph.NodeList.Count;
            this._K = new MatrixOperation.Matrix(2 * n, 2 * n);
            CalStiffMatrix(proxiGraph);
        }


        /// <summary>
        /// 构造函数-道路网
        /// </summary>
        /// <param name="polylineList">道路网线列表</param>
        /// <param name="a">形状参数-弹性系数</param>
        /// <param name="b">形状参数-刚性系数</param>
        public SnakesStiffMatrix(List<PolylineObject> polylineList, int CountofNode,double a, double b)
        {
            this.a = a;
            this.b = b;
            this._K = new MatrixOperation.Matrix(2 * CountofNode, 2 * CountofNode);
            CalStiffMatrix(polylineList);
        }
        ///// <summary>
        ///// 获取编号为id的点在线目标上的顺序好
        ///// </summary>
        ///// <param name="polyline">线</param>
        ///// <param name="id">编号</param>
        ///// <returns></returns>
        //private int getIndex(PolylineObject polyline,int id)
        //{
        //    int index=-1;
        //    foreach (Node p in polyline.PointList)
        //    {
        //        index++;
        //        if (p.ID == id)
        //            return index;
        //    }
        //    return -1;
        //}

         /// <summary>
         /// 计算一个线对象的刚度矩阵
         /// </summary>
         /// <param name="polyline"></param>
        private void CalStiffMatrix(PolylineObject polyline)
        {
            for (int i = 0; i < polyline.PointList.Count - 1; i++)
            {
                CalcuLineMatrix(polyline.PointList[i], polyline.PointList[i + 1], a, b);
            }
        }

        /// <summary>
        /// 计算一个线对象的刚度矩阵
        /// </summary>
        /// <param name="polyline"></param>
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
        private void CalcuLineMatrix(Node fromPoint, Node toPoint, double a, double b)
        {

            double h = Math.Sqrt((fromPoint.Y - toPoint.Y) * (fromPoint.Y - toPoint.Y) + (fromPoint.X - toPoint.X) * (fromPoint.X - toPoint.X));
           // h = h * 1000 / AlgSnakes.scale;
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
        /// 计算线段单元的刚度矩阵
        /// </summary>
        /// <param name="line">线段对象</param>
        /// <returns>返回刚度矩阵4*4</returns>
        private void CalcuLineMatrix(Node fromPoint, Node toPoint)
        {

            double h = Math.Sqrt((fromPoint.Y - toPoint.Y) * (fromPoint.Y - toPoint.Y) + (fromPoint.X - toPoint.X) * (fromPoint.X - toPoint.X));
            // h = h * 1000 / AlgSnakes.scale;
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
    }
}
