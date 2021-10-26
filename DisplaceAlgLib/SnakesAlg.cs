using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Carto;

namespace DisplaceAlgLib
{
   

    public class SnakesAlg
    {
        public static double fminDistance = 0.0002;
        public static double fDenominatorofMapScale = 50;
        public static double fDefaultA=1.0;
        public static double fDefaultB= 1.0;
        public static double fDefaultSymWidth = 0.002;
      

        /// <summary>
        /// 计算线段单元的刚度矩阵
        /// </summary>
        /// <param name="line">线段对象</param>
        /// <returns>返回刚度矩阵4*4</returns>
        private static Matrix CalcuLineMatrix(IPoint fromPoint, IPoint toPoint,double a,double b)
        {
             //线段长度
             double h = Math.Sqrt((fromPoint.Y - toPoint.Y) * (fromPoint.Y - toPoint.Y) + (fromPoint.X - toPoint.X) * (fromPoint.X - toPoint.X));
             //计算该线段的刚度矩阵
             Matrix lineStiffMatrix = new Matrix(4, 4);
             //计算用的临时变量
             double temp1 =(6.0  * ((a * h * h + 10 * b)))/ ( 5.0*(h * h * h));
             double temp2 = (a * h * h + 60 * b) /  (10.0*(h * h));
             double temp3 = (2.0 * (a * h * h + 30 * b)) / (15.0* h);
             double temp4 =(1.0 * (a * h * h - 60 * b)) / (30.0 * h);

             lineStiffMatrix[0, 0] = temp1;
             lineStiffMatrix[2, 2] = temp1;
             lineStiffMatrix[0, 1] = temp2;
             lineStiffMatrix[1, 0] = temp2;
             lineStiffMatrix[3, 0] = temp2;
             lineStiffMatrix[0, 3] = temp2;
             lineStiffMatrix[0, 2] = -1 * temp1;
             lineStiffMatrix[2, 0] = -1 * temp1;
             lineStiffMatrix[1, 1] = temp3;
             lineStiffMatrix[3, 3] = temp3;
             lineStiffMatrix[1, 2] = -1 * temp2;
             lineStiffMatrix[2, 1] = -1 * temp2;
             lineStiffMatrix[2, 3] = -1 * temp2;
             lineStiffMatrix[3, 2] = -1 * temp2;
             lineStiffMatrix[1, 3] = -1 * temp4;
             lineStiffMatrix[3, 1] = -1 * temp4;
             return lineStiffMatrix;
        }

        /// <summary>
        /// 计算线段单元的刚度矩阵
        /// </summary>
        /// <param name="line">线段对象</param>
        /// <returns>返回刚度矩阵4*4</returns>
        private static void  CalcuLineMatrix(out Matrix linestiffMatrix_X,out Matrix linestiffMatrix_Y,IPoint fromPoint, IPoint toPoint, double a, double b)
        {
            //计算该线段的刚度矩阵
            linestiffMatrix_X = new Matrix(4, 4);
            linestiffMatrix_Y = new Matrix(4, 4);

            //线段长度当hx或hy==0时出现矩阵值为Infinited的情况，暂无处理
            double hx = Math.Abs(fromPoint.X - toPoint.X);
            double hy = Math.Abs(fromPoint.Y - toPoint.Y);

            //计算用的临时变量
            double temp1 = (6.0 * ((a * hx * hx + 10 * b))) / (5.0 * (hx * hx * hx));
            double temp2 = (a * hx * hx + 60 * b) / (10.0 * (hx * hx));
            double temp3 = (2.0 * (a * hx * hx + 30 * b)) / (15.0 * hx);
            double temp4 = (1.0 * (a * hx * hx - 60 * b)) / (30.0 * hx);

            linestiffMatrix_X[0, 0] = temp1;
            linestiffMatrix_X[2, 2] = temp1;
            linestiffMatrix_X[0, 1] = temp2;
            linestiffMatrix_X[1, 0] = temp2;
            linestiffMatrix_X[3, 0] = temp2;
            linestiffMatrix_X[0, 3] = temp2;
            linestiffMatrix_X[0, 2] = -1 * temp1;
            linestiffMatrix_X[2, 0] = -1 * temp1;
            linestiffMatrix_X[1, 1] = temp3;
            linestiffMatrix_X[3, 3] = temp3;
            linestiffMatrix_X[1, 2] = -1 * temp2;
            linestiffMatrix_X[2, 1] = -1 * temp2;
            linestiffMatrix_X[2, 3] = -1 * temp2;
            linestiffMatrix_X[3, 2] = -1 * temp2;
            linestiffMatrix_X[1, 3] = -1 * temp4;
            linestiffMatrix_X[3, 1] = -1 * temp4;

            temp1 = (6.0 * ((a * hy * hy + 10 * b))) / (5.0 * (hy * hy * hy));
            temp2 = (a * hy * hy + 60 * b) / (10.0 * (hy * hy));
            temp3 = (2.0 * (a * hy * hy + 30 * b)) / (15.0 * hy);
            temp4 = (1.0 * (a * hy * hy - 60 * b)) / (30.0 * hy);

            linestiffMatrix_Y[0, 0] = temp1;
            linestiffMatrix_Y[2, 2] = temp1;
            linestiffMatrix_Y[0, 1] = temp2;
            linestiffMatrix_Y[1, 0] = temp2;
            linestiffMatrix_Y[3, 0] = temp2;
            linestiffMatrix_Y[0, 3] = temp2;
            linestiffMatrix_Y[0, 2] = -1 * temp1;
            linestiffMatrix_Y[2, 0] = -1 * temp1;
            linestiffMatrix_Y[1, 1] = temp3;
            linestiffMatrix_Y[3, 3] = temp3;
            linestiffMatrix_Y[1, 2] = -1 * temp2;
            linestiffMatrix_Y[2, 1] = -1 * temp2;
            linestiffMatrix_Y[2, 3] = -1 * temp2;
            linestiffMatrix_Y[3, 2] = -1 * temp2;
            linestiffMatrix_Y[1, 3] = -1 * temp4;
            linestiffMatrix_Y[3, 1] = -1 * temp4;
        }

        /// <summary>
        /// 计算一条线的刚度矩阵
        /// </summary>
        /// <param name="path">简单线对象</param>
        /// <returns>线目标的刚度矩阵</returns>
        public static Matrix CalcuPolyLineMatrix(Snake snake)
        {

            IPath path = snake.Path;
            IPointCollection pointSet = path as IPointCollection;
            int pointCount = pointSet.PointCount;
            int n = 2 * pointCount;
            Matrix matrixofPath = new Matrix(n, n);
            Matrix curLineMatrix;
            // int i,j,k,m,n
            string fileName;
            for (int i = 0; i < pointCount - 1; i++)
            {
                fileName = @"D:\record\LineMatrix" + i.ToString() + ".txt";
                //获取第i个线段的刚度矩阵
                curLineMatrix = CalcuLineMatrix(pointSet.get_Point(i), pointSet.get_Point(i + 1), snake.a,snake.b);
                curLineMatrix.WriteTxtFile(fileName);
                //将第i个线段的刚度矩阵填入单线路径矩阵
                for (int j =0; j <4; j++)
                {
                    for (int k = 0; k <4; k++)
                    {
                        matrixofPath[2 * i + j, 2 * i + k] += curLineMatrix[j, k];
                    }
                }
            }
            return matrixofPath;
        }

        /// <summary>
        /// 计算一条线的刚度矩阵
        /// </summary>
        /// <param name="path">简单线对象</param>
        /// <returns>线目标的刚度矩阵</returns>
        public static void CalcuPolyLineMatrix(out Matrix stiffMatrix_X,out Matrix stiffMatrix_Y,Snake snake)
        {

            IPath path = snake.Path;
            IPointCollection pointSet = path as IPointCollection;
            int pointCount = pointSet.PointCount;
            int n = 2 * pointCount;
            stiffMatrix_X = new Matrix(n, n);
            stiffMatrix_Y = new Matrix(n, n);
            Matrix curLineMatrix_X;
            Matrix curLineMatrix_Y;
            // int i,j,k,m,n
            for (int i = 0; i < pointCount - 1; i++)
            {
                //获取第i个线段的刚度矩阵
                CalcuLineMatrix(out curLineMatrix_X, out  curLineMatrix_Y, pointSet.get_Point(i), pointSet.get_Point(i + 1), snake.a, snake.b);
                //将第i个线段的刚度矩阵填入单线路径矩阵
                for (int j =0; j < 4; j++)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        stiffMatrix_X[2 * i + j, 2 * i + k] += curLineMatrix_X[j, k];
                        stiffMatrix_Y[2 * i + j, 2 * i + k] += curLineMatrix_Y[j, k];
                    }
                }
            }
        }

        /// <summary>
        /// 基于顶点的受力模型
        /// </summary>
        /// <param name="path">简单线对象</param>
        /// <param name="shapeCollection">可能与之冲突的周围对象的</param>
        /// <returns>返回受力向量（没算力矩）</returns>
        private  static Matrix CalcuVetexModelForceVector(Snake snake, IGeometryCollection shapeCollection)
        {
            int count = shapeCollection.GeometryCount;
            IPath path = snake.Path;
            IPointCollection pointCollection = path as IPointCollection;
            int n = pointCollection.PointCount;
            Matrix forceVector = new Matrix(2 * n, 1);  //创建一个2n行1列的矩阵
            IPoint curPoint = null;         //当前起点 
            IGeometry curShape = null;          //当前几何体-？目前还不知用什么方法选取，只能人工选择了
            IPoint nearPoint = null;         //当前几何对象到起点的最近点
            double nearDis = 0.0;             //当前几何对象到起点的最近距离
            //  double lastAbsForce = 0.0;      //记录上一线段末端点的受力，用于下一段求力矩
            //点受力向量的方位角的正弦、余弦
            double cos = 0.0;
            double sin = 0.0;
            double absForce = 0.0;              //记录线段终点点的受力大小
            double curFx = 0.0;
            double curFy = 0.0;

            //距离阈值，小于该阈值将产生冲突
            double Dmin = fDenominatorofMapScale * (snake.SymbolWidth  + fminDistance);
            //将当前Path上各个点对所有的Geometry求受力，并累加起来求合力（均分解为全局坐标系下（X，Y）的力）
            //??力矩也可以求合吗？
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;
            IPolyline poly = new PolylineClass();
            IGeometryCollection polySet = poly as IGeometryCollection;
            polySet.AddGeometry(snake.Path, ref missing1, ref missing2);
            IRelationalOperator pRelop1 = poly as IRelationalOperator;
            //生成缓冲区
            ITopologicalOperator pTopop = poly as ITopologicalOperator;
            IPolygon bufferPolygon = pTopop.Buffer(Dmin) as IPolygon;

            //空间关系运算
            IRelationalOperator pRelop2 = bufferPolygon as IRelationalOperator;

            for (int i = 0; i < count; i++)
            {
                //取得第i个几何对象
                curShape = shapeCollection.get_Geometry(i);
                //如果不是同一根线或与缓冲区相交，则存在冲突
                if (!(pRelop1.Equals(curShape) || pRelop2.Disjoint(curShape)))//判断是否存在冲突
                {
                    //分别求n个顶点受力
                    for (int j = 0; j < n; j++)
                    {
                        curPoint = pointCollection.get_Point(j);
                        GetProximityPoint_Distance(curPoint, curShape, out nearPoint, out nearDis);
                        if (nearDis ==0.0)//如果该点与冲突对象重合，暂不处理
                            continue;
                       // WriteNearPoint(lyr,nearPoint);
                        //受力大小
                        absForce = Dmin - nearDis;
                      
                        //当Dmin>dis，才受力
                        if (absForce > 0)
                        {
                            //受力向量方位角的COS
                            sin = (curPoint.Y - nearPoint.Y) / nearDis;
                            //受力向量方位角的SIN
                            cos = (curPoint.X - nearPoint.X) / nearDis;
                            curFx = absForce * cos;
                            curFy = absForce * sin; 
                            forceVector[j * 2, 0] += curFx;                //Fx
                            forceVector[j *2 + 1, 0] += curFy;             //Fy

                        }
                    }
                }
            }
            return forceVector;
        }


        /// <summary>
        /// 受力向量
        /// </summary>
        /// <param name="forceVector_X">X方向受力向量</param>
        /// <param name="forceVector_Y">Y方向受力向量</param>
        /// <param name="forceVector">受力向量，不含D撇</param>
        /// <param name="snake">样条</param>
        public  static void CalcuforceVector(out Matrix forceVector_X, out Matrix forceVector_Y, Matrix forceVector,Snake snake)
        {
            IPath path = snake.Path;
            IPointCollection pointSet = path as IPointCollection;
            int n = pointSet.PointCount;

            forceVector_X = new Matrix(2 * n, 1);
            forceVector_Y = new Matrix(2 * n, 1);
         
            double h;
            IPoint fromPoint;
            IPoint toPoint;
            IPoint nextPoint;
            for (int i = 0; i < n; i++)
            {
                //fromPoin = pointSet.get_Point(i);
               // toPoint = pointSet.get_Point(i + 1);

                if (i == 0)
                {
                    fromPoint = pointSet.get_Point(i);
                    toPoint = pointSet.get_Point(i + 1);
                    h = MathFunc.LineLength(fromPoint, toPoint);
                    forceVector_X[0, 0] = 0.5 * h * forceVector[0, 0];
                    forceVector_X[1, 0] = 1.0 / 12.0 * h * h * forceVector[0, 0];
                    forceVector_Y[0, 0] = 0.5 * h * forceVector[1, 0];
                    forceVector_Y[1, 0] = 1.0 / 12.0 * h * h * forceVector[1, 0];
                }
                else if (i == n - 1)
                {
                    fromPoint = pointSet.get_Point(i - 1);
                    toPoint = pointSet.get_Point(i);
                    h = MathFunc.LineLength(fromPoint, toPoint);
                    forceVector_X[2 * i, 0] = 0.5 * h * forceVector[2 * i, 0];
                    forceVector_X[2 * i + 1, 0] = -1.0 / 12.0 * h * h * forceVector[2 * i, 0];
                    forceVector_Y[2 * i, 0] = 0.5 * h * forceVector[2 * i+1, 0];
                    forceVector_Y[2 * i + 1, 0] = -1.0 / 12.0 * h * h * forceVector[2 * i+1, 0];
                }
                else
                {
                    fromPoint = pointSet.get_Point(i - 1);
                    toPoint = pointSet.get_Point(i );
                    nextPoint = pointSet.get_Point(i+1);
                    h = MathFunc.LineLength(fromPoint, toPoint);
                    forceVector_X[2 * i, 0] += 0.5 * h * forceVector[2 * i, 0];
                    forceVector_X[2 * i + 1, 0] += -1.0 / 12.0 * h * h * forceVector[2 * i, 0];
                    forceVector_Y[2 * i, 0] += 0.5 * h * forceVector[2 * i + 1, 0];
                    forceVector_Y[2 * i + 1, 0] += -1.0 / 12.0 * h * h * forceVector[2 * i + 1, 0];

                    h = MathFunc.LineLength(fromPoint, nextPoint);
                    forceVector_X[2 * i, 0] += 0.5 * h * forceVector[2 * i, 0];
                    forceVector_X[2 * i + 1, 0] += 1.0 / 12.0 * h * h * forceVector[2 * i, 0];
                    forceVector_Y[2 * i, 0] += 0.5 * h * forceVector[2 * i + 1, 0];
                    forceVector_Y[2 * i + 1, 0] += 1.0 / 12.0 * h * h * forceVector[2 * i + 1, 0];
                }
            }
        }



        /// <summary>
        /// 受力向量
        /// </summary>
        /// <param name="forceVector_X">X方向受力向量</param>
        /// <param name="forceVector_Y">Y方向受力向量</param>
        /// <param name="forceVector">受力向量，不含D撇</param>
        /// <param name="snake">样条</param>
        public static void CalcuforceVectorXY(out Matrix forceVector_X, out Matrix forceVector_Y, Matrix forceVector, Snake snake)
        {
            IPath path = snake.Path;
            IPointCollection pointSet = path as IPointCollection;
            int n = pointSet.PointCount;

            forceVector_X = new Matrix(2 * n, 1);
            forceVector_Y = new Matrix(2 * n, 1);

            double hx;
            double hy;
            IPoint fromPoint;
            IPoint toPoint;
            IPoint nextPoint;
            for (int i = 0; i < n; i++)
            {
                //fromPoin = pointSet.get_Point(i);
                // toPoint = pointSet.get_Point(i + 1);

                if (i == 0)
                {
                    fromPoint = pointSet.get_Point(i);
                    toPoint = pointSet.get_Point(i + 1);
                    hx = Math.Abs(fromPoint.X - toPoint.X);
                    hy = Math.Abs(fromPoint.Y - toPoint.Y);
                    forceVector_X[0, 0] = 0.5 * hx * forceVector[0, 0];
                    forceVector_X[1, 0] = 1.0 / 12.0 * hx * hx * forceVector[0, 0];
                    forceVector_Y[0, 0] = 0.5 * hy * forceVector[1, 0];
                    forceVector_Y[1, 0] = 1.0 / 12.0 * hy * hy * forceVector[1, 0];
                }
                else if (i == n - 1)
                {
                    fromPoint = pointSet.get_Point(i - 1);
                    toPoint = pointSet.get_Point(i);
                    hx = Math.Abs(fromPoint.X - toPoint.X);
                    hy = Math.Abs(fromPoint.Y - toPoint.Y);
                   
                    forceVector_X[2 * i, 0] = 0.5 * hx * forceVector[2 * i, 0];
                    forceVector_X[2 * i + 1, 0] = -1.0 / 12.0 * hx * hx * forceVector[2 * i, 0];
                    forceVector_Y[2 * i, 0] = 0.5 * hy * forceVector[2 * i + 1, 0];
                    forceVector_Y[2 * i + 1, 0] = -1.0 / 12.0 * hy * hy * forceVector[2 * i + 1, 0];
                }
                else
                {
                    fromPoint = pointSet.get_Point(i - 1);
                    toPoint = pointSet.get_Point(i);
                    nextPoint = pointSet.get_Point(i + 1);
                    hx = Math.Abs(fromPoint.X - toPoint.X);
                    hy = Math.Abs(fromPoint.Y - toPoint.Y);
                    forceVector_X[2 * i, 0] += 0.5 * hx * forceVector[2 * i, 0];
                    forceVector_X[2 * i + 1, 0] += -1.0 / 12.0 * hx * hx * forceVector[2 * i, 0];
                    forceVector_Y[2 * i, 0] += 0.5 * hy * forceVector[2 * i + 1, 0];
                    forceVector_Y[2 * i + 1, 0] += -1.0 / 12.0 * hy * hy * forceVector[2 * i + 1, 0];

                    hx = Math.Abs(fromPoint.X - nextPoint.X);
                    hy = Math.Abs(fromPoint.Y - nextPoint.Y);
                    forceVector_X[2 * i, 0] += 0.5 * hx * forceVector[2 * i, 0];
                    forceVector_X[2 * i + 1, 0] += 1.0 / 12.0 * hx * hx * forceVector[2 * i, 0];
                    forceVector_Y[2 * i, 0] += 0.5 * hy * forceVector[2 * i + 1, 0];
                    forceVector_Y[2 * i + 1, 0] += 1.0 / 12.0 * hy * hy * forceVector[2 * i + 1, 0];
                }
            }
        }
        /// <summary>
        /// 求移位向量
        /// </summary>
        /// <param name="displaceVector_X">X方向移位向量</param>
        /// <param name="displaceVector_Y">Y方向移位向量</param>
        /// <param name="snake">样条对象</param>
        /// <param name="shapeCollection">可能的冲突对象</param>
        public static Snake CalcuDisplaceVector(out Matrix displaceVector_X, out Matrix displaceVector_Y, Snake snake, IGeometryCollection shapeCollection, List<BoundPointDisplaceParams> boundPoints,int time, double gr, double u)
        {
            Matrix stiffMatrix=CalcuPolyLineMatrix(snake);
            Matrix forceVector=CalcuVetexModelForceVector(snake, shapeCollection);
            stiffMatrix.WriteTxtFile(@"D:\record\stiffMatrix.txt");
            forceVector.WriteTxtFile(@"D:\record\forceVector.txt");
            Matrix forceVector_X;
            Matrix forceVector_Y;
            CalcuforceVector(out forceVector_X, out forceVector_Y, forceVector, snake);
            forceVector_X.WriteTxtFile(@"D:\record\forceVector_X.txt");
            forceVector_Y.WriteTxtFile(@"D:\record\forceVector_Y.txt");
            
            SetBoundPointParams(ref stiffMatrix, ref forceVector_X, ref forceVector_Y, boundPoints);
            displaceVector_X = (stiffMatrix.Inverse()) * forceVector_X;
            displaceVector_Y = (stiffMatrix.Inverse()) * forceVector_Y;
            displaceVector_X.WriteTxtFile(@"D:\record\displaceVector_X.txt");
            displaceVector_Y.WriteTxtFile(@"D:\record\displaceVector_Y.txt");

            return  SnakesIterate(stiffMatrix, ref  forceVector_X, ref  forceVector_Y, ref  displaceVector_X, ref  displaceVector_Y, shapeCollection, snake, time, gr, u,boundPoints);

    
        }


        /// <summary>
        /// 求移位向量
        /// </summary>
        /// <param name="displaceVector_X">X方向移位向量</param>
        /// <param name="displaceVector_Y">Y方向移位向量</param>
        /// <param name="snake">样条对象</param>
        /// <param name="shapeCollection">可能的冲突对象</param>
        public static Snake CalcuDisplaceVectorXY(out Matrix displaceVector_X, out Matrix displaceVector_Y, Snake snake, IGeometryCollection shapeCollection, List<BoundPointDisplaceParams> boundPoints, int time, double gr, double u)
        {
            Matrix stiffMatrix_X;
            Matrix stiffMatrix_Y;

            CalcuPolyLineMatrix(out stiffMatrix_X, out stiffMatrix_Y, snake);

            Matrix forceVector = CalcuVetexModelForceVector(snake, shapeCollection);
            stiffMatrix_X.WriteTxtFile(@"D:\record\stiffMatrix_X.txt");
            stiffMatrix_Y.WriteTxtFile(@"D:\record\stiffMatrix_Y.txt");

            forceVector.WriteTxtFile(@"D:\record\forceVector.txt");
            Matrix forceVector_X;
            Matrix forceVector_Y;
            CalcuforceVectorXY(out forceVector_X, out forceVector_Y, forceVector, snake);
            forceVector_X.WriteTxtFile(@"D:\record\forceVector_X.txt");
            forceVector_Y.WriteTxtFile(@"D:\record\forceVector_Y.txt");

            SetBoundPointParams(ref stiffMatrix_X, ref stiffMatrix_Y, ref forceVector_X, ref forceVector_Y, boundPoints);
            displaceVector_X = (stiffMatrix_X.Inverse()) * forceVector_X;
            displaceVector_Y = (stiffMatrix_Y.Inverse()) * forceVector_Y;

            return SnakesIterate(stiffMatrix_X,stiffMatrix_X, ref  forceVector_X, ref  forceVector_Y, ref  displaceVector_X, ref  displaceVector_Y, shapeCollection, snake, time, gr, u, boundPoints);

            displaceVector_X.WriteTxtFile(@"D:\record\displaceVector_X.txt");
            displaceVector_Y.WriteTxtFile(@"D:\record\displaceVector_Y.txt");
        }
        /// <summary>
        /// 迭代（1+rK）d(t)=d(t-1)+ruf(t-1)
        /// </summary>
        /// <param name="forceVector_X"></param>
        /// <param name="forceVector_Y"></param>
        /// <param name="displaceVector_X"></param>
        /// <param name="displaceVector_Y"></param>
        /// <param name="snake"></param>
        /// <param name="time"></param>
        /// <param name="gr"></param>
        /// <param name="u"></param>
        public static Snake SnakesIterate(Matrix stiffMatrix, ref Matrix forceVector_X, ref Matrix forceVector_Y, ref Matrix displaceVector_X, ref Matrix displaceVector_Y, IGeometryCollection shapeCollection, Snake snake, int time, double gr, double u, List<BoundPointDisplaceParams> boundPoints)
        {
           // Matrix stiffMatrix = CalcuPolyLineMatrix(snake);//计算广度矩阵
            IPath path = snake.Path;
            IPointCollection pointSet = path as IPointCollection;
            int n = pointSet.PointCount;
            Matrix identityM = new Matrix(2*n, 2*n);
            Matrix leftMatrix;
            Matrix rightVector_X;
            Matrix rightVector_Y;
            Snake oldSnake=snake;;
            Snake newSnake;
            Matrix forceVector;
           // Matrix forceVector_X;
           // Matrix forceVector_Y;
            
            for (int i = 0; i < 2 * n; i++)
            {
                for (int j = 0; j < 2 * n; j++)
                {

                    if (i == j)
                    {
                        identityM[i, j] = 1;

                    }
                    else
                    {
                        identityM[i, j] = 0;
                    }
                   
                }
            }

            string fileName = "";
            for (int i = 0; i < time; i++)
            {
                fileName=@"D:\record\stiffMatrix"+i.ToString()+".txt";

                UpdatePosition(out newSnake,oldSnake,displaceVector_X, displaceVector_Y);
                stiffMatrix = CalcuPolyLineMatrix(newSnake);

                stiffMatrix.WriteTxtFile(fileName);
                for (int j = 0; j < 2 * n; j++)
                {
                    for (int k = 0; k< 2 * n; k++)
                    {

                        stiffMatrix[j, k] = gr * stiffMatrix[j, k];

                    }
                }

                for(int j=0;j<2*n;j++)
                {
                    forceVector_X[j, 0] = gr * u * forceVector_X[j, 0];
                    forceVector_Y[j, 0] = gr * u * forceVector_Y[j, 0];
                }
                leftMatrix = identityM + stiffMatrix;
                

                rightVector_X=displaceVector_X+forceVector_X;
                rightVector_Y = displaceVector_Y + forceVector_Y;

                SetBoundPointParams(ref leftMatrix, ref rightVector_X, ref rightVector_Y, boundPoints);

                leftMatrix = leftMatrix.Inverse();

                displaceVector_X = leftMatrix * rightVector_X;
                displaceVector_Y = leftMatrix * rightVector_Y;
                fileName=@"D:\record\displaceVector_X"+i.ToString()+".txt";
                displaceVector_X.WriteTxtFile(fileName);
                fileName = @"D:\record\displaceVector_Y" + i.ToString() + ".txt";
                displaceVector_Y.WriteTxtFile(fileName);


                forceVector = CalcuVetexModelForceVector(newSnake, shapeCollection);
                CalcuforceVector(out forceVector_X, out forceVector_Y, forceVector, newSnake);
                fileName = @"D:\record\forceVector_X" + i.ToString() + ".txt";
                forceVector_X.WriteTxtFile(fileName);

                fileName = @"D:\record\forceVector_Y" + i.ToString() + ".txt";
                forceVector_Y.WriteTxtFile(fileName);

                oldSnake = newSnake;
              //  newSnake.Disp
            }

            UpdatePosition(out newSnake, oldSnake, displaceVector_X, displaceVector_Y);

            return newSnake;

        }

        /// <summary>
        /// 迭代（1+rK）d(t)=d(t-1)+ruf(t-1)
        /// </summary>
        /// <param name="forceVector_X"></param>
        /// <param name="forceVector_Y"></param>
        /// <param name="displaceVector_X"></param>
        /// <param name="displaceVector_Y"></param>
        /// <param name="snake"></param>
        /// <param name="time"></param>
        /// <param name="gr"></param>
        /// <param name="u"></param>
        public static Snake SnakesIterate(Matrix stiffMatrix_X,Matrix stiffMatrix_Y,ref Matrix forceVector_X, ref Matrix forceVector_Y, ref Matrix displaceVector_X, ref Matrix displaceVector_Y, IGeometryCollection shapeCollection, Snake snake, int time, double gr, double u, List<BoundPointDisplaceParams> boundPoints)
        {
            // Matrix stiffMatrix = CalcuPolyLineMatrix(snake);//计算广度矩阵
            IPath path = snake.Path;
            IPointCollection pointSet = path as IPointCollection;
            int n = pointSet.PointCount;
            Matrix identityM = new Matrix(2 * n, 2 * n);
            Matrix leftMatrix_X;
            Matrix leftMatrix_Y;
            Matrix rightVector_X;
            Matrix rightVector_Y;
            Snake oldSnake = snake; ;
            Snake newSnake;
            Matrix forceVector;
            // Matrix forceVector_X;
            // Matrix forceVector_Y;

            for (int i = 0; i < 2 * n; i++)
            {
                for (int j = 0; j < 2 * n; j++)
                {

                    if (i == j)
                    {
                        identityM[i, j] = 1;

                    }
                    else
                    {
                        identityM[i, j] = 0;
                    }

                }
            }

            string fileName = "";
            for (int i = 0; i < time; i++)
            {
                

                UpdatePosition(out newSnake, oldSnake, displaceVector_X, displaceVector_Y);
                CalcuPolyLineMatrix(out stiffMatrix_X, out stiffMatrix_Y,newSnake);

                fileName = @"D:\record\stiffMatrix_X" + i.ToString() + ".txt";
                stiffMatrix_X.WriteTxtFile(fileName);
                fileName = @"D:\record\stiffMatrix_Y" + i.ToString() + ".txt";
                stiffMatrix_Y.WriteTxtFile(fileName);
                for (int j = 0; j < 2 * n; j++)
                {
                    for (int k = 0; k < 2 * n; k++)
                    {

                        stiffMatrix_X[j, k] = gr * stiffMatrix_X[j, k];
                        stiffMatrix_Y[j, k] = gr * stiffMatrix_X[j, k];

                    }
                }

                for (int j = 0; j < 2 * n; j++)
                {
                    forceVector_X[j, 0] = gr * u * forceVector_X[j, 0];
                    forceVector_Y[j, 0] = gr * u * forceVector_Y[j, 0];
                }
                leftMatrix_X= identityM + stiffMatrix_X;
                leftMatrix_Y = identityM + stiffMatrix_Y;

                rightVector_X = displaceVector_X + forceVector_X;
                rightVector_Y = displaceVector_Y + forceVector_Y;

                SetBoundPointParams(ref leftMatrix_X, ref leftMatrix_Y,ref rightVector_X, ref rightVector_Y, boundPoints);

                leftMatrix_X = leftMatrix_X.Inverse();
                leftMatrix_Y = leftMatrix_Y.Inverse();

                displaceVector_X = leftMatrix_X * rightVector_X;
                displaceVector_Y = leftMatrix_Y * rightVector_Y;
                fileName = @"D:\record\displaceVector_X" + i.ToString() + ".txt";
                displaceVector_X.WriteTxtFile(fileName);
                fileName = @"D:\record\displaceVector_Y" + i.ToString() + ".txt";
                displaceVector_Y.WriteTxtFile(fileName);


                forceVector = CalcuVetexModelForceVector(newSnake, shapeCollection);
                CalcuforceVector(out forceVector_X, out forceVector_Y, forceVector, newSnake);
                fileName = @"D:\record\forceVector_X" + i.ToString() + ".txt";
                forceVector_X.WriteTxtFile(fileName);

                fileName = @"D:\record\forceVector_Y" + i.ToString() + ".txt";
                forceVector_Y.WriteTxtFile(fileName);

                oldSnake = newSnake;
                //  newSnake.Disp
            }

            UpdatePosition(out newSnake, oldSnake, displaceVector_X, displaceVector_Y);

            return newSnake;

        }
        /// <summary>
        /// 更新点的位置
        /// </summary>
        /// <param name="newsnake"></param>
        /// <param name="snake"></param>
        /// <param name="displaceVector_X"></param>
        /// <param name="displaceVector_Y"></param>
        private static  void UpdatePosition(out Snake newsnake,Snake snake,Matrix displaceVector_X,Matrix displaceVector_Y)
        {
            object ms1=Type.Missing;
            object ms2=Type.Missing;
            IPath path = snake.Path;
            IPath newPath=new PathClass();
            newsnake = new Snake(snake.a, snake.b, snake.SymbolWidth, newPath);
            IPointCollection pointSet = path as IPointCollection;
            IPointCollection newPointSet= newPath as IPointCollection;
            int n = pointSet.PointCount;
            IPoint curPoint;
            IPoint newPoint;
            for (int i = 0; i < n; i++)
            {
                newPoint=new PointClass();
                curPoint = pointSet.get_Point(i);
                newPoint.PutCoords(curPoint.X+ displaceVector_X[2 * i, 0], curPoint.Y + displaceVector_Y[2 * i, 0]);
                newPointSet.AddPoint(newPoint,ref ms1,ref ms2);
            }
        }

          /// <summary>
        /// 判断线目标与给定几何对象是否存在冲突
        /// </summary>
        /// <param name="targetLine">线目标</param>
        /// <param name="conflictShape">可能存在冲突的几何对象</param>
        /// <returns>是否冲突</returns>
        private static bool IsConflict(IPath targetLine, IGeometry conflictShape, double flineSymbolWidth)
        {
           // if (IsEnvelopeConflict(targetLine, conflictShape,flineSymbolWidth))
           // {
            if (IsBufferConflict(targetLine, conflictShape, flineSymbolWidth))
                {
                    return true;
                }
           // }
            return false;
        }

        /// <summary>
        /// 判断线目标的外包矩形的缓冲区与给定几何对象是否存在冲突
        /// </summary>
        /// <param name="targetLine">线目标</param>
        /// <param name="conflictShape">可能存在冲突的几何对象</param>
        /// <returns></returns>
        private static bool IsEnvelopeConflict(IPath targetLine, IGeometry conflictShape, double fWidthofLineSymbol)
        {
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;
            IEnvelope  envelopeofPath = targetLine.Envelope;
            IPolygon poly = new PolygonClass();
            IPointCollection pSet = poly as IPointCollection;
            IPoint lbPoint=new PointClass();
            IPoint ltPoint=new PointClass();
            IPoint rtPoint=new PointClass();
            IPoint rbPoint = new PointClass();

            lbPoint.PutCoords(envelopeofPath.XMin,envelopeofPath.YMin);
            ltPoint.PutCoords(envelopeofPath.XMin,envelopeofPath.YMax);
            rtPoint.PutCoords(envelopeofPath.XMax,envelopeofPath.YMax);
            rbPoint.PutCoords(envelopeofPath.XMax,envelopeofPath.YMin);
    
            pSet.AddPoint(lbPoint,ref missing1,ref missing2);
            pSet.AddPoint(ltPoint, ref missing1, ref missing2);
            pSet.AddPoint(rtPoint, ref missing1, ref missing2);
            pSet.AddPoint(rbPoint, ref missing1, ref missing2);

            IPolyline polyline = new PolylineClass();
            IGeometryCollection polySet = polyline as IGeometryCollection;
            polySet.AddGeometry(targetLine, ref missing1, ref missing2);

            ITopologicalOperator pTopop = poly as ITopologicalOperator;
            IPolygon bufferPolygon = pTopop.Buffer(fDenominatorofMapScale * (fWidthofLineSymbol+ fminDistance)) as IPolygon;
            IRelationalOperator pRelop = bufferPolygon as IRelationalOperator;
            IRelationalOperator pRelop1 = polyline as IRelationalOperator;
            if (pRelop.Disjoint(conflictShape) || pRelop1.Equals(conflictShape))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 通过缓冲区判断线目标与给定几何对象是否存在冲突
        /// </summary>
        /// <param name="targetLine">线目标</param>
        /// <param name="conflictShape">可能存在冲突的几何对象</param>
        /// <returns>是否冲突</returns>
        private static bool IsBufferConflict(IPath targetLine, IGeometry conflictShape,double fWidthofLineSymbol)
        {
             object missing1=Type.Missing;
             object missing2 = Type.Missing;
            IPolyline   poly= new PolylineClass();
            IGeometryCollection polySet = poly as IGeometryCollection;
            polySet.AddGeometry(targetLine,ref missing1,ref missing2);
            //生成缓冲区
            ITopologicalOperator pTopop = poly as ITopologicalOperator;
            IPolygon bufferPolygon=pTopop.Buffer(fDenominatorofMapScale*(fWidthofLineSymbol+fminDistance)) as IPolygon;


            IPointCollection pset = bufferPolygon as IPointCollection;
            IPoint point;
            for (int i = 0; i < pset.PointCount; i++)
            {
                point = pset.get_Point(i);
            }

            //空间关系运算
            IRelationalOperator pRelop = bufferPolygon as IRelationalOperator;
            if (pRelop.Disjoint(conflictShape) || pRelop.Equals(conflictShape))
            {
                return false;
            }
            return true;
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
            nearestPoint = Prxop.ReturnNearestPoint(targetPoint,esriSegmentExtension.esriNoExtension);
        }

        /// <summary>
        /// 设置边界条件
        /// </summary>
        /// <param name="stiffMaxtrix">刚度矩阵</param>
        /// <param name="forceVector">受力向量</param>
        /// <param name="boundPoints">边界点</param>
        public static void SetBoundPointParams(ref Matrix stiffMaxtrix, ref Matrix forceVector_X, ref Matrix forceVector_Y,List<BoundPointDisplaceParams> boundPoints)
        {
            int r1, r2;

            foreach (BoundPointDisplaceParams curBound in boundPoints)
            {
                r1 = curBound.Index *2;
                r2 = curBound.Index * 2 +1;

                forceVector_X[r1, 0] = curBound.Dx;
                forceVector_X[r2, 0] = 0;

                forceVector_Y[r1, 0] = curBound.Dy;
                forceVector_Y[r2, 0] = 0;

                for (int i = 0; i < stiffMaxtrix.Columns; i++)
                {
                    if (i == r1)
                    {
                        stiffMaxtrix[r1, i] = 1;//对角线上元素赋值为1
                    }
                    else
                    {
                        stiffMaxtrix[r1, i] = 0;//其他地方赋值为0
                    }


                    if (i == r2)
                    {
                        stiffMaxtrix[r2, i] = 1;//对角线上元素赋值为1
                    }
                    else
                    {
                        stiffMaxtrix[r2, i] = 0;//其他地方赋值为0
                    }

                }

            }
        }

        /// <summary>
        /// 设置边界条件
        /// </summary>
        /// <param name="stiffMaxtrix">刚度矩阵</param>
        /// <param name="forceVector">受力向量</param>
        /// <param name="boundPoints">边界点</param>
        public static void SetBoundPointParams(ref Matrix stiffMaxtrix_X, ref Matrix stiffMaxtrix_Y, ref Matrix forceVector_X, ref Matrix forceVector_Y, List<BoundPointDisplaceParams> boundPoints)
        {
            int r1, r2;

            foreach (BoundPointDisplaceParams curBound in boundPoints)
            {
                r1 = curBound.Index * 2;
                r2 = curBound.Index * 2 + 1;

                forceVector_X[r1, 0] = curBound.Dx;
                forceVector_X[r2, 0] = 0;

                forceVector_Y[r1, 0] = curBound.Dy;
                forceVector_Y[r2, 0] = 0;

                for (int i = 0; i < stiffMaxtrix_X.Columns; i++)
                {
                    if (i == r1)
                    {
                        stiffMaxtrix_X[r1, i] = 1;//对角线上元素赋值为1
                        stiffMaxtrix_Y[r1, i] = 1;//对角线上元素赋值为1
                    }
                    else
                    {
                        stiffMaxtrix_X[r1, i] = 0;//其他地方赋值为0
                        stiffMaxtrix_Y[r1, i] = 0;//对角线上元素赋值为1
                    }


                    if (i == r2)
                    {
                        stiffMaxtrix_X[r2, i] = 1;//对角线上元素赋值为1
                        stiffMaxtrix_Y[r2, i] = 1;//对角线上元素赋值为1
                    }
                    else
                    {
                        stiffMaxtrix_X[r2, i] = 0;//其他地方赋值为0
                        stiffMaxtrix_Y[r2, i] = 0;//对角线上元素赋值为1 = 0;//其他地方赋值为0
                    }

                }

            }
        }

        /// <summary>
        /// 将最近点写入最近点图层
        /// </summary>
        /// <param name="resultLyr">结果图层对象</param>
        /// <param name="path">原来的线对象</param>
        /// <param name="dispaceVector">移位向量</param>
        private static void WriteNearPoint(IFeatureLayer resultLyr, IPoint nearPoint)
        {
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            if (resultLyr == null)
                return;
            if (nearPoint == null)
                return;

            IFeatureClass featureClass = resultLyr.FeatureClass;
            if (featureClass == null)
                return;
            //获取凸壳图层的数据集，并创建工作空间
            IDataset dataset = (IDataset)resultLyr;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;

            //注意：此时，所编辑数据不能被其他程序打开
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();



            IFeature feature = featureClass.CreateFeature();
            IPoint p = new PointClass();
            p.PutCoords(nearPoint.X, nearPoint.Y);

            feature.Shape = p;
            feature.Store();
            //关闭编辑
            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
        }

    }
}
