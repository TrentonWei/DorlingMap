using System;
using System.Collections.Generic;
using System.Text;
using ESRI.ArcGIS.Geometry;
using System.Runtime.InteropServices;
using System.IO;

namespace DisplaceAlgLib
{
    /// <summary>
    /// 弹性梁移位算法
    /// </summary>
    public class ElasticBeamsAlg
    {
        #region 字段
        public static float fDefaultParaA = 0.02F;                //横截面积，尚不好确定-一般设为符号宽度的1/10，地图单位;而武芳：A=k*d*d
        public static float fDefaultParaE = 1F;                 //弹性模量,一般设为1-2
        public static float fDefaultParaI = 1F;                 //惯性力,一般为符号宽度的两倍与平均曲率的乘积

        public static float fDefaultSymWidth = 0.002F;    //线状符号的宽度
        public static float fDenominatorofMapScale = 100F;  //比例尺分母
        public static float fminDistance = 0.0002F; //地图上视觉最小距离阈值，单位为m
        #endregion

    
        /// <summary>
        /// 计算线段单元的刚度矩阵
        /// </summary>
        /// <param name="line">线段对象</param>
        /// <returns>返回刚度矩阵6*6</returns>
        private static Matrix CalcuLineMatrix(IPoint fromPoint, IPoint toPoint, double fDefaultParaA, double fDefaultParaE,double fDefaultParaI)
        {
             //线段长度
            double lenofLine = Math.Sqrt((fromPoint.Y - toPoint.Y) * (fromPoint.Y - toPoint.Y) + (fromPoint.X - toPoint.X) * (fromPoint.X - toPoint.X));
            //线段方位角的COS
            double sin = (toPoint.Y - fromPoint.Y) / lenofLine;
            //线段方位角的SIN
            double cos = (toPoint.X - fromPoint.X) / lenofLine;

            //计算该线段的刚度矩阵
            Matrix curLineMatrix = new Matrix(6, 6);

            #region 未经化简的代码
            /*  //第一行/列
            curLineMatrix[0, 0] =  (ParaE/lenofLine)*(fParaA*fDenominatorofMapScale * cos * cos +( 12 * fParaI * sin * sin) / (lenofLine * lenofLine));
            curLineMatrix[0, 1] = curLineMatrix[1, 0]=(ParaE/lenofLine)*( fParaA * fDenominatorofMapScale -(12 * fParaI * sin * cos) / (lenofLine * lenofLine));
            curLineMatrix[0, 2] = curLineMatrix[2, 0] = -(ParaE / lenofLine) * (6 * fParaI * sin / lenofLine);
            curLineMatrix[0, 3] = curLineMatrix[3, 0] = -(ParaE / lenofLine) * (fParaA * fDenominatorofMapScale * cos * cos + (12 * fParaI * sin * sin) / (lenofLine * lenofLine));
            curLineMatrix[0, 4] = curLineMatrix[4, 0] = -(ParaE / lenofLine) * (fParaA * fDenominatorofMapScale - (12 * fParaI * sin * cos) / (lenofLine * lenofLine));
            curLineMatrix[0, 5] = curLineMatrix[5, 0] = -(ParaE / lenofLine) * (6 * fParaI * sin / lenofLine);

            //第二行/列
            curLineMatrix[1, 1] = (ParaE / lenofLine) * (fParaA * fDenominatorofMapScale * sin * sin + (12 * fParaI * cos * cos) / (lenofLine * lenofLine));
            curLineMatrix[1, 2] = curLineMatrix[2, 1]=(ParaE / lenofLine) * (6 * fParaI * cos / lenofLine);
            curLineMatrix[1, 3] = curLineMatrix[3, 1] = -(ParaE / lenofLine) * (fParaA * fDenominatorofMapScale - 12 * fParaI / (lenofLine * lenofLine)) * sin * cos;
            curLineMatrix[1, 4] = curLineMatrix[4, 1] =  -(ParaE / lenofLine) * (fParaA * fDenominatorofMapScale*sin*sin+ 12 * fParaI / (lenofLine * lenofLine)cos*cos);
            curLineMatrix[1, 5] = curLineMatrix[5, 1] = (ParaE / lenofLine) * (6 * fParaI * cos / lenofLine);

            //第三行/列
            curLineMatrix[2,2]=(ParaE / lenofLine) * 4*ParaI;
            curLineMatrix[2,3]=curLineMatrix[3,2]=(ParaE / lenofLine) * (6 * fParaI * sin / lenofLine);
            curLineMatrix[2,4]=curLineMatrix[4,2]=-(ParaE / lenofLine) * (6 * fParaI * cos / lenofLine);
            curLineMatrix[2,5]=curLineMatrix[5,2]=(ParaE / lenofLine) * 2*ParaI;

            //第四行/列
            curLineMatrix[3,3]= (ParaE/lenofLine)*(fParaA*fDenominatorofMapScale * cos * cos +( 12 * fParaI * sin * sin) / (lenofLine * lenofLine));
            curLineMatrix[3,4]= curLineMatrix[4,3] =(ParaE/lenofLine)*(fParaA * fDenominatorofMapScale - 12 * fParaI / (lenofLine * lenofLine))*cos*cos;
            curLineMatrix[3,5]= curLineMatrix[5,3] =-(ParaE / lenofLine) * (6 * fParaI * cos / lenofLine);

            //第五行/列
            curLineMatrix[4,4]=(ParaE / lenofLine) * (fParaA * fDenominatorofMapScale * sin * sin + (12 * fParaI * cos * cos) / (lenofLine * lenofLine));
            curLineMatrix[4,5]=curLineMatrix[5,4]=-(ParaE / lenofLine) * (6 * fParaI * cos / lenofLine);
            
            //第六行/列
            curLineMatrix[5,5]=(ParaE / lenofLine) * 4*ParaI;*/
            #endregion

            //计算用的临时变量
            double EL = fDefaultParaE / lenofLine;
            double IL2 = 12 * fDefaultParaI / (lenofLine * lenofLine);
            double IL1 = 6 * fDefaultParaI / lenofLine;
            double cc=cos*cos;
            double ss=sin*sin;
            double cs=cos*sin;
            double A = fDefaultParaA * fDenominatorofMapScale;//尚不好确定，武芳：A=kd*kd
            double ACCIL2SS=EL*(A*cc+IL2*ss);
            double ASSIL2CC=EL*(A*ss+IL2*cc);
            double AIL2 = A - IL2;
             
            curLineMatrix[0, 0]=curLineMatrix[3, 3]=ACCIL2SS;
            curLineMatrix[0,3]=curLineMatrix[3,0]=-1*ACCIL2SS;
            curLineMatrix[1,1]=curLineMatrix[4,4]=ASSIL2CC;
            curLineMatrix[1, 4] = curLineMatrix[4, 1] = -1 * ASSIL2CC;
            curLineMatrix[0, 1]=curLineMatrix[1, 0]=EL*(A-IL2*cs);
            curLineMatrix[0, 2]=curLineMatrix[2, 0]=curLineMatrix[0,5]=curLineMatrix[5, 0]=-1*EL*IL1*sin;
            curLineMatrix[1, 2]=curLineMatrix[2, 1]=curLineMatrix[1,5]=curLineMatrix[5, 1]=EL*IL1*cos;
            curLineMatrix[2, 3]=curLineMatrix[3, 2]=EL*IL1*sin;
            curLineMatrix[2, 4]=curLineMatrix[4, 2]=curLineMatrix[3, 5]=curLineMatrix[5, 3]=curLineMatrix[4, 5]=curLineMatrix[5, 4]=-1*EL*IL1*cos;
            curLineMatrix[2, 2] = curLineMatrix[5, 5] = 4 * EL * fDefaultParaI;
            curLineMatrix[2, 5] = curLineMatrix[5, 2] = 2 * EL * fDefaultParaI;
            curLineMatrix[0,4]=curLineMatrix[4,0]=curLineMatrix[1,3]=curLineMatrix[3,1]=-1*EL*AIL2*cs;
            curLineMatrix[3,4]=curLineMatrix[4,3]=EL*AIL2*cc;
 
            return curLineMatrix;
        }

        /// <summary>
        /// 计算一条线的刚度矩阵
        /// </summary>
        /// <param name="path">简单线对象</param>
        /// <returns>线目标的刚度矩阵</returns>
        public static Matrix CalcuPolyLineMatrix(ElasticBeam beam)
        {
            IPath path = beam.Path;
            IPointCollection pointSet = path as IPointCollection;
            int pointCount = pointSet.PointCount;
            int n=3*pointCount;
            Matrix matrixofPath = new Matrix(n, n);
            Matrix curLineMatrix;

            string fileName;
           // int i,j,k,m,n
            for (int i = 0; i < pointCount - 1; i++)
            {
                fileName = @"D:\record\LineMatrix" + i.ToString() + ".txt";
                //获取第i个线段的刚度矩阵
                curLineMatrix = CalcuLineMatrix(pointSet.get_Point(i),pointSet.get_Point(i+1),beam.A,beam.E,beam.I);
                curLineMatrix.WriteTxtFile(fileName);
                //将第i个线段的刚度矩阵填入单线路径矩阵
                for (int j = 0; j <6; j++)
                {
                    for (int k = 0; k < 6; k++)
                    {
                        matrixofPath[3 * i + j, 3 * i + k] += curLineMatrix[j, k];
                    }
                }
            }
            return matrixofPath;
        }

      /*  /// <summary>
        /// 基于线段受力模型
        /// </summary>
        /// <param name="path">简单线对象</param>
        /// <param name="shapeCollection">可能与之冲突的周围对象的</param>
        /// <returns>返回受力向量（没算力矩）</returns>
        public static Matrix CalcuLineModelForceVector(IPath path, IGeometryCollection shapeCollection)
        {
            int count = shapeCollection.GeometryCount;
            ISegmentCollection lineCollection = path as ISegmentCollection;
            int n = lineCollection.SegmentCount;
            IPointCollection2 curShpPointSet = null;    //当前邻近对象上的点集合
            int m = 0;                                  //当前邻近对象上的点数

            Matrix forceVector = new Matrix(3 *(n+1), 1);  //创建一个3n行1列的矩阵
            ILine curLine = null;
            double curLineLen = 0.0;
            IPoint curFromPoint = null;                 //当前起点 
            IPoint curToPoint = null;                  //当前起点 
            IGeometry curShape = null;                  //当前几何体-？目前还不知用什么方法选取，只能人工选择了
            IPoint curInPoint = null;                   //邻近几何对象上的一个点，用于与当前线段求最短距离和最近点
            IPoint nearPoint = null;                    //当前几何对象到起点的最近点
            double nearDis = 0.0;                       //当前几何对象到起点的最近距离
            //点受力向量的方位角的正弦、余弦
            double cos = 0.0;
            double sin = 0.0;
            double absForce = 0.0;              //记录线段受力大小
            double absForce1 = 0.0;              //记录线段起点受力大小
            double absForce2 = 0.0;              //记录线段终点受力大小

            //距离阈值，小于该阈值将产生冲突
            double Dmin = fDenominatorofMapScale * (fDefaultSymWidth / 2 + fminDistance);
            //将当前Path上各个点对所有的Geometry求受力，并累加起来求合力（均分解为全局坐标系下（X，Y）的力）
            //??力矩也可以求合吗？
            for (int i = 0; i < count; i++)
            {
                //取得第i个几何对象
                curShape = shapeCollection.get_Geometry(i);
                if (IsConflict(path, curShape))//判断是否存在冲突
                {
                    //n-1段线段分别求端点受力
                    for (int j = 0; j < n; j++)
                    {
                        curLine = (ILine)(lineCollection.get_Segment(j));
                        curFromPoint = curLine.FromPoint;
                        curToPoint = curLine.ToPoint;
                        curShpPointSet=curShape as IPointCollection2;
                        m=curShpPointSet.PointCount;
                        curLineLen = curLine.Length;
                        //此处有待优化，？需不需要逐点求，可否先求出在缓冲区中的点再求？
                        for (int k = 0; k < m; k++)
                        {
                            curInPoint = curShpPointSet.get_Point(k);
                            GetProximityPoint_Distance(curInPoint, curLine, out nearPoint, out nearDis);
                            absForce = Dmin - nearDis;
                            if (absForce > 0)
                            {
                                //力向量方位角的COS
                                sin = (nearPoint.Y - curInPoint.Y) / nearDis;
                                //力向量方位角的SIN
                                cos = (nearPoint.X - curInPoint.X) / nearDis;
                                absForce1 = absForce * MathFunc.LineLength(curFromPoint, nearPoint) / curLineLen;
                                absForce2 = absForce * MathFunc.LineLength(curToPoint, nearPoint) / curLineLen;
                               //终点受力
                                forceVector[j * 3, 0] += absForce1*cos;                      //Fx
                                forceVector[j * 3 + 1, 0] += absForce1*sin;                  //Fy

                                //起点受力
                                forceVector[(j +1)* 3, 0] += absForce2 * cos;                      //Fx
                                forceVector[(j + 1) * 3 + 1, 0] += absForce2 * sin;                  //Fy
                            }

                        }
                    }
                }
            }
            return forceVector;
                       

        }*/
        /// <summary>
        /// 基于顶点的受力模型
        /// </summary>
        /// <param name="path">简单线对象</param>
        /// <param name="shapeCollection">可能与之冲突的周围对象的</param>
        /// <returns>返回受力向量（没算力矩）</returns>
        public static Matrix CalcuVetexModelForceVector(ElasticBeam beam, IGeometryCollection shapeCollection)
        {
            IPath path = beam.Path;
            int count = shapeCollection.GeometryCount;
            IPointCollection pointCollection = path as IPointCollection;
            int n = pointCollection.PointCount;
            Matrix forceVector = new Matrix(3 * n, 1);  //创建一个3n行1列的矩阵
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
            double Dmin = fDenominatorofMapScale * (beam.SymbolWidth + fminDistance);
            //将当前Path上各个点对所有的Geometry求受力，并累加起来求合力（均分解为全局坐标系下（X，Y）的力）
            //??力矩也可以求合吗？

            object missing1 = Type.Missing;
            object missing2 = Type.Missing;
            IPolyline poly = new PolylineClass();
            IGeometryCollection polySet = poly as IGeometryCollection;
            polySet.AddGeometry(beam.Path, ref missing1, ref missing2);
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
                if (!(pRelop1.Equals(curShape) || pRelop2.Disjoint(curShape)))//判断是否存在冲突
                {
                    //分别求n个顶点受力
                    for (int j = 0; j < n; j++)
                    {
                        curPoint = pointCollection.get_Point(j);
                        GetProximityPoint_Distance(curPoint, curShape, out nearPoint, out nearDis);
                        //受力大小
                        if (nearDis == 0.0)//如果该点与冲突对象重合，暂不处理
                            continue;
                        absForce = Dmin - nearDis;
                        //当Dmin>dis，才受力
                        if (absForce > 0)
                        {
                            //受力向量方位角的COS
                            sin = (curPoint.Y - nearPoint.Y) / nearDis;
                            //受力向量方位角的SIN
                            cos = (curPoint.X - nearPoint.X) / nearDis;
                            curFx = absForce * cos;
                            curFy = absForce * sin; ;
                            forceVector[j * 3, 0] += curFx;                //Fx
                            forceVector[j * 3 + 1, 0] += curFy;                  //Fy
                        }
                    }
                }
            }
            //计算力矩
            CalcuMoment(beam.Path, ref forceVector);

            return forceVector;
                       
        }

        /// <summary>
        /// 求力矩并返回力和力矩向量
        /// </summary>
        /// <param name="path">简单线对象</param>
        /// <param name="forceVector">已经计算出受力的力矩阵</param>
        /// <returns>返回受力向量（没算力矩）</returns>
        public static Matrix CalcuMoment(IPath path, ref Matrix forceVector)
        {
            IPointCollection pointCollection = path as IPointCollection;
            int n = pointCollection.PointCount;
            double curFx = 0.0;
            double curFy = 0.0;
            IPoint curPrePoint = null;
            IPoint curPoint = null;
            IPoint curNextPoint = null;
            double sin = 0.0;
            double cos = 0.0;
            double lenofSegLine = 0.0;
            for (int i = 0; i < n; i++)
            {
                //分别取出X/Y方向的受力
                curFx = forceVector[i * 3, 0];
                curFy = forceVector[i * 3 + 1, 0];

                //第一个点
                if (i == 0)
                {
                    curPoint = pointCollection.get_Point(i);
                    curNextPoint = pointCollection.get_Point(i + 1);

                    MathFunc.SinCosofVector(curNextPoint, curPoint, out sin, out cos, out lenofSegLine);
                    forceVector[i * 3 + 2, 0] += curFx * lenofSegLine * sin + curFy * lenofSegLine * cos;
                }
                //最后一个点
                else if (i == n - 1)
                {
                    curPoint = pointCollection.get_Point(i);
                    curPrePoint = pointCollection.get_Point(i - 1);
                    MathFunc.SinCosofVector(curPrePoint, curPoint, out sin, out cos, out lenofSegLine);
                    forceVector[i * 3 + 2, 0] += curFx * lenofSegLine * sin + curFy * lenofSegLine * cos;
                }
                //中间的点
                else
                {
                    curPoint = pointCollection.get_Point(i);
                    curPrePoint = pointCollection.get_Point(i - 1);
                    curNextPoint = pointCollection.get_Point(i + 1);

                    MathFunc.SinCosofVector(curPrePoint, curPoint, out sin, out cos, out lenofSegLine);
                    forceVector[i * 3 + 2, 0] += curFx * lenofSegLine * sin + curFy * lenofSegLine * cos;
                    MathFunc.SinCosofVector(curNextPoint, curPoint, out sin, out cos, out lenofSegLine);
                    forceVector[i * 3 + 2, 0] += curFx * lenofSegLine * sin + curFy * lenofSegLine * cos;  //M力矩
                }
            }
            return forceVector;
        }
         
                 
    /*      /// <summary>
        /// 混合受力模型
        /// </summary>
        /// <param name="path">简单线对象</param>
        /// <param name="shapeCollection">可能与之冲突的周围对象的</param>
        /// <returns>返回受力向量（没算力矩）</returns>
        public static Matrix CalcuCombiModelForceVector(IPath path, IGeometryCollection shapeCollection)
        {
            int count = shapeCollection.GeometryCount;
            ISegmentCollection lineCollection = path as ISegmentCollection;
            int n = lineCollection.SegmentCount;
            IPointCollection2 curShpPointSet = null;    //当前邻近对象上的点集合
            int m = 0;                                  //当前邻近对象上的点数

            Matrix forceVector = new Matrix(3 * (n+1), 1);  //创建一个3n行1列的矩阵
            ILine curLine = null;
            double curLineLen = 0.0;
            IPoint curFromPoint = null;                 //当前起点 
            IPoint curToPoint = null;                  //当前起点 
            IGeometry curShape = null;                  //当前几何体-？目前还不知用什么方法选取，只能人工选择了
            IPoint curInPoint = null;                   //邻近几何对象上的一个点，用于与当前线段求最短距离和最近点
            IPoint nearPoint = null;                    //当前几何对象到起点的最近点
            double nearDis = 0.0;                       //当前几何对象到起点的最近距离
            //点受力向量的方位角的正弦、余弦
            double cos = 0.0;
            double sin = 0.0;
            double absForce = 0.0;              //记录线段受力大小
            double absForce1 = 0.0;              //记录线段起点受力大小
            double absForce2 = 0.0;              //记录线段终点受力大小

            //距离阈值，小于该阈值将产生冲突
            double Dmin = fDenominatorofMapScale * (fDefaultSymWidth / 2 + fminDistance);
            //将当前Path上各个点对所有的Geometry求受力，并累加起来求合力（均分解为全局坐标系下（X，Y）的力）
            //??力矩也可以求合吗？
            for (int i = 0; i < count; i++)
            {
                //取得第i个几何对象
                curShape = shapeCollection.get_Geometry(i);
                if (IsConflict(path, curShape))//判断是否存在冲突
                {
                    //n-1段线段分别求端点受力
                    for (int j = 0; j < n; j++)
                    {
                        curLine = (ILine)(lineCollection.get_Segment(j));
                        curFromPoint = curLine.FromPoint;
                        curToPoint = curLine.ToPoint;

                        //求起点受力
                        GetProximityPoint_Distance(curFromPoint, curShape, out nearPoint, out nearDis);
                        //受力大小
                        absForce = Dmin - nearDis;
                        //当Dmin>dis，才受力
                        if (absForce > 0)
                        {
                            //受力向量方位角的COS
                            sin = (curFromPoint.Y - nearPoint.Y) / nearDis;
                            //受力向量方位角的SIN
                            cos = (curFromPoint.X - nearPoint.X) / nearDis;

                            forceVector[j * 3, 0] += absForce * cos;                      //Fx
                            forceVector[j * 3 + 1, 0] += absForce * sin;                  //Fy

                        }
                        //求终点受力
                        GetProximityPoint_Distance(curToPoint, curShape, out nearPoint, out nearDis);
                        //受力大小
                        absForce = Dmin - nearDis;
                        //当Dmin>dis，才受力
                        if (absForce > 0)
                        {
                            //受力向量方位角的COS
                            sin = (curToPoint.Y - nearPoint.Y) / nearDis;
                            //受力向量方位角的SIN
                            cos = (curToPoint.X - nearPoint.X) / nearDis;
               
                            forceVector[(j + 1) * 3, 0] += absForce * cos;                       //Fx
                            forceVector[(j + 1) * 3 + 1, 0] += absForce * sin;                 //Fy

                        }

                        curShpPointSet = curShape as IPointCollection2;
                        m = curShpPointSet.PointCount;
                        curLineLen = curLine.Length;
                        //此处有待优化，？需不需要逐点求，可否先求出在缓冲区中的点再求？
                        for (int k = 0; k < m; k++)
                        {
                            curInPoint = curShpPointSet.get_Point(k);
                            GetProximityPoint_Distance(curInPoint, curLine, out nearPoint, out nearDis);
                            absForce = Dmin - nearDis;
                            if (absForce > 0)
                            {
                                //力向量方位角的COS
                                sin = (nearPoint.Y - curInPoint.Y) / nearDis;
                                //力向量方位角的SIN
                                cos = (nearPoint.X - curInPoint.X) / nearDis;
                                absForce1 = absForce * MathFunc.LineLength(curFromPoint, nearPoint) / curLineLen;
                                absForce2 = absForce * MathFunc.LineLength(curToPoint, nearPoint) / curLineLen;
                                //终点受力
                                forceVector[j * 3, 0] += absForce1 * cos;                      //Fx
                                forceVector[j * 3 + 1, 0] += absForce1 * sin;                  //Fy

                                //起点受力
                                forceVector[(j + 1) * 3, 0] += absForce2 * cos;                      //Fx
                                forceVector[(j + 1) * 3 + 1, 0] += absForce2 * sin;                  //Fy
                            }

                        }
                    }
                }
            }
            return forceVector;

        }*/
      /*  /// <summary>
        ///  最大力受力模型
        /// </summary>
        /// <param name="path">简单线对象</param>
        /// <param name="shapeCollection">可能与之冲突的周围对象的</param>
        /// <returns>返回受力向量（没算力矩）</returns>
        public static Matrix CalcuMaxmumModelForceVector(IPath path, IGeometryCollection shapeCollection)
        {
            int count = shapeCollection.GeometryCount;
            ISegmentCollection lineCollection = path as ISegmentCollection;
            int n = lineCollection.SegmentCount;
            IPointCollection2 curShpPointSet = null;    //当前邻近对象上的点集合
            int m = 0;                                  //当前邻近对象上的点数

            Matrix forceVector = new Matrix(3 * (n+1), 1);  //创建一个3n行1列的矩阵
            ILine curLine = null;
            double curLineLen = 0.0;
            IPoint curFromPoint = null;                 //当前起点 
            IPoint curToPoint = null;                  //当前起点 
            IGeometry curShape = null;                  //当前几何体-？目前还不知用什么方法选取，只能人工选择了
            IPoint curInPoint = null;                   //邻近几何对象上的一个点，用于与当前线段求最短距离和最近点
            IPoint nearPoint = null;                    //当前几何对象到起点的最近点
            double nearDis = 0.0;                       //当前几何对象到起点的最近距离
            //点受力向量的方位角的正弦、余弦
            double cos = 0.0;
            double sin = 0.0;
            double absForce = 0.0;  
            double absLineForce1 = 0.0;              //记录线段起点受力大小
            double vFx1 = 0.0;
            double vFy1 = 0.0;
            double absLineForce2 = 0.0;              //记录线段终点受力大小
            double vFx2 = 0.0;
            double vFy2 = 0.0;

            double absVetexForce1 = 0.0;             //记录起点顶点受力大小
            double lFx1 = 0.0;
            double lFy1 = 0.0;                       //记录起点顶点受力大小
            double absVetexForce2 = 0.0;             //记录线段终点顶点受力大小
            double lFx2 = 0.0;
            double lFy2 = 0.0;  

            //距离阈值，小于该阈值将产生冲突
            double Dmin = fDenominatorofMapScale * (beam / 2 + fminDistance);
            //将当前Path上各个点对所有的Geometry求受力，并累加起来求合力（均分解为全局坐标系下（X，Y）的力）
            //??力矩也可以求合吗？
            for (int i = 0; i < count; i++)
            {
                //取得第i个几何对象
                curShape = shapeCollection.get_Geometry(i);
                if (IsConflict(path, curShape))//判断是否存在冲突
                {
                    //n-1段线段分别求端点受力
                    for (int j = 0; j < n; j++)
                    {
                        curLine = (ILine)(lineCollection.get_Segment(j));
                        curFromPoint = curLine.FromPoint;
                        curToPoint = curLine.ToPoint;

                        //求起点受力
                        GetProximityPoint_Distance(curFromPoint, curShape, out nearPoint, out nearDis);
                        //受力大小
                        absVetexForce1 = Dmin - nearDis;
                        //当Dmin>dis，才受力
                        if (absVetexForce1 > 0)
                        {
                            //受力向量方位角的COS
                            sin = (curFromPoint.Y - nearPoint.Y) / nearDis;
                            //受力向量方位角的SIN
                            cos = (curFromPoint.X - nearPoint.X) / nearDis;
                            vFx1 = absVetexForce1 * cos;
                            vFy1 = absVetexForce1 * sin; ;
                        }

                        //求终点受力
                        GetProximityPoint_Distance(curToPoint, curShape, out nearPoint, out nearDis);
                        //受力大小
                        absVetexForce2 = Dmin - nearDis;
                        //当Dmin>dis，才受力
                        if (absVetexForce2 > 0)
                        {
                            //受力向量方位角的COS
                            sin = (curToPoint.Y - nearPoint.Y) / nearDis;
                            //受力向量方位角的SIN
                            cos = (curToPoint.X - nearPoint.X) / nearDis;
                            vFx2 = absVetexForce2 * cos;
                            vFy2 = absVetexForce2 * sin; 
                        }

                        curShpPointSet = curShape as IPointCollection2;
                        m = curShpPointSet.PointCount;
                        curLineLen = curLine.Length;
                        //此处有待优化，？需不需要逐点求，可否先求出在缓冲区中的点再求？
                        for (int k = 0; k < m; k++)
                        {
                            curInPoint = curShpPointSet.get_Point(k);
                            GetProximityPoint_Distance(curInPoint, curLine, out nearPoint, out nearDis);
                            absForce = Dmin - nearDis;
                            if (absForce > 0)
                            {
                                //力向量方位角的COS
                                sin = (nearPoint.Y - curInPoint.Y) / nearDis;
                                //力向量方位角的SIN
                                cos = (nearPoint.X - curInPoint.X) / nearDis;
                                absLineForce1 = absForce * MathFunc.LineLength(curFromPoint, nearPoint) / curLineLen;
                                absLineForce2 = absForce * MathFunc.LineLength(curToPoint, nearPoint) / curLineLen;
                                //终点受力
                                lFx1 += absLineForce1 * cos;                  //Fx
                                lFy1 += absLineForce1 * sin;                  //Fy

                                //起点受力
                                lFx2 += absLineForce2 * cos;                  //Fx
                                lFy2 += absLineForce2 * sin;                  //Fy
                            }
                        }

                        absLineForce1 = Math.Sqrt(lFx1 * lFx1 + lFy1 * lFy1);//起点处线段合力大小
                        absLineForce2 = Math.Sqrt(lFx2 * lFx2 + lFy2 * lFy2);//终点处线段合力大小

                        if (absVetexForce1 >= absLineForce1)
                        {
                            forceVector[j * 3, 0] += vFx1;                      //Fx
                            forceVector[j * 3 + 1, 0] += vFy1;                  //Fy
                        }
                        else
                        {
                            forceVector[j * 3, 0] += lFx1;                      //Fx
                            forceVector[j * 3 + 1, 0] += lFy1;                  //Fy
                        }


                        if (absVetexForce2 >= absLineForce2)
                        {
                            forceVector[(j + 1) * 3, 0] += vFx2;                      //Fx
                            forceVector[(j + 1) * 3 + 1, 0] += vFy2;                  //Fy
                        }
                        else
                        {
                            forceVector[(j + 1) * 3, 0] += lFx2;                      //Fx
                            forceVector[(j + 1) * 3 + 1, 0] += lFy2;                  //Fy
                        }
                    }
                }
            }
            return forceVector;
        }
        
        /// <summary>
        ///点受力模型+力矩
        /// </summary>
        /// <param name="path">简单线对象</param>
        /// <param name="shapeCollection">可能与之冲突的周围对象的</param>
        /// <returns>返回受力向量（没算力矩）</returns>
        public static Matrix CalcuPolyLineForceVector(IPath path, IGeometryCollection shapeCollection)
        { 
            int count = shapeCollection.GeometryCount;
            IPointCollection pointCollection = path as IPointCollection;
            int n=pointCollection.PointCount;
            Matrix forceVector = new Matrix(3*n,1);  //创建一个3n行1列的矩阵

            IPoint curFromPoint = null;         //当前起点 
            IPoint curToPoint = null;           //当前终点
            IPoint curNextPoint = null;           //当前起点的前一点

            IGeometry curShape=null;            //当前几何体-？目前还不知用什么方法选取，只能人工选择了

            IPoint frmNearPoint = null;         //当前几何对象到起点的最近点
            IPoint toNearPoint = null;          //当前几何对象到终点的最近点

            double frmPntDis = 0.0;              //当前几何对象到起点的最近距离
            double toPntDis = 0.0;               //当前几何对象到终点的最近距离
           
          //  double lastAbsForce = 0.0;          //记录上一线段末端点的受力，用于下一段求力矩
            //点受力向量的方位角的正弦、余弦
            double cos = 0.0;
            double sin = 0.0;
            double lenofSegLine = 0.0;
            double lastForce = 0.0;             //记录线段起点的受力大小
            double absForce = 0.0;              //记录线段终点点的受力大小
            double curFx = 0.0;
            double curFy= 0.0; 
  
            //距离阈值，小于该阈值将产生冲突
            double Dmin =fDenominatorofMapScale * (fWidthofLineSymbol / 2 + fminDistance);
            //将当前Path上各个点对所有的Geometry求受力，并累加起来求合力（均分解为全局坐标系下（X，Y）的力）
            //??力矩也可以求合吗？
            for (int i = 0; i < count; i++)
            {
                //取得第i个几何对象
                curShape=shapeCollection.get_Geometry(i);
                if (IsConflict(path, curShape))//判断是否存在冲突
                {
                    //n-1段线段分别求端点受力和力矩
                    for (int j = 0; j < n - 1; j++)
                    {
                        #region 求点受力及力矩
                        //将上一线端终点的受力大小传递给lastForce
                        lastForce = absForce;
                        curFromPoint = pointCollection.get_Point(j);
                        curToPoint = pointCollection.get_Point(j + 1);
                        //求首点到冲突对象的最短距离和最近点
                        //第一段需要计算起点到冲突对象的最短距离和最近点
                        if (j == 0)
                        {
                            curNextPoint = pointCollection.get_Point(j + 2);
                            GetProximityPoint_Distance(curFromPoint, curShape, out frmNearPoint, out frmPntDis);
                            //首点受力大小
                            absForce = Dmin - frmPntDis;
                            //当Dmin>dis，才受力
                            if (absForce > 0)
                            {
                                //首点受力向量方位角的COS
                                sin =(curFromPoint.Y - frmNearPoint.Y) / frmPntDis;
                                //首点受力向量方位角的SIN
                                cos = (curFromPoint.X - frmNearPoint.X) / frmPntDis;
                                curFx = absForce * cos;
                                curFy = absForce * sin; ;  
                                forceVector[0, 0] += curFx ;                //Fx
                                forceVector[1, 0] +=curFy;                  //Fy
                                //求力矩并累加
                                MathFunc.SinCosofVector(curToPoint, curFromPoint, out sin, out cos,out lenofSegLine);
                                forceVector[2, 0] += curFx * lenofSegLine * sin + curFy * lenofSegLine*cos;                        //M力矩
                            }

                            GetProximityPoint_Distance(curToPoint, curShape, out toNearPoint, out toPntDis);
                            absForce = Dmin - frmPntDis;//第二个点受力
                            //当Dmin>dis，才受力
                            if (absForce > 0)
                            {
                                //首点受力向量方位角的COS
                                sin = (curToPoint.Y - toNearPoint.Y) / toPntDis;
                                //首点受力向量方位角的SIN
                                cos = (curToPoint.X - toNearPoint.X) / toPntDis;
                                curFx = absForce * cos;
                                curFy = absForce * sin; ;
                                forceVector[3, 0] += curFx;                //Fx
                                forceVector[4, 0] += curFy;                  //Fy
                                //求力矩并累加
                                MathFunc.SinCosofVector(curFromPoint, curToPoint, out sin, out cos, out lenofSegLine);
                                forceVector[5, 0] += curFx * lenofSegLine * sin + curFy * lenofSegLine * cos;
                                MathFunc.SinCosofVector(curNextPoint, curToPoint, out sin, out cos, out lenofSegLine);
                                forceVector[5, 0] += curFx * lenofSegLine * sin + curFy * lenofSegLine * cos;  //M力矩

                            }

                        }
                        else//非第一段的起点为上一段的终点
                        { 
                            //继承上次计算的结果
                            frmNearPoint = toNearPoint;
                            frmPntDis = toPntDis;
                            //求终点到冲突对象的最短距离和最近点
                            GetProximityPoint_Distance(curToPoint, curShape, out toNearPoint, out toPntDis);
                            //首点受力大小
                            absForce = Dmin - toPntDis;
                            //当Dmin>dis，才受力
                            if (absForce > 0)
                            {
                                //当为首段时，需要专门求首点受力
                                sin = (curToPoint.Y - toNearPoint.Y) / toPntDis;
                                //首点受力向量方位角的SIN
                                cos = (curToPoint.X - toNearPoint.X) / toPntDis;
                                curFx = absForce * cos;
                                curFy = absForce * sin;
                                forceVector[3 * (j + 1), 0] += curFx;             //Fx
                                forceVector[3 * (j + 1) + 1, 0] += curFy;         //Fy

                                if (j < n - 2)
                                {
                                    curNextPoint = pointCollection.get_Point(j + 2);
                                    MathFunc.SinCosofVector(curFromPoint, curToPoint, out sin, out cos, out lenofSegLine);
                                    forceVector[3 * (j + 1) + 2, 0] += curFx * lenofSegLine * sin + curFy * lenofSegLine * cos;
                                    MathFunc.SinCosofVector(curNextPoint,curToPoint, out sin, out cos, out lenofSegLine);
                                    forceVector[3 * (j + 1) + 2, 0] += curFx * lenofSegLine * sin + curFy * lenofSegLine * cos;  //M力矩
                                }
                                else if (j == n - 2)
                                {
                                    MathFunc.SinCosofVector(curFromPoint, curToPoint, out sin, out cos, out lenofSegLine);
                                    forceVector[3 * (j + 1) + 2, 0] += curFx * lenofSegLine * sin + curFy * lenofSegLine * cos;
                                }
                            }

                        }
                      
                        #endregion
                    }
                      
                }
            }
            return forceVector;
        }*/
        /// <summary>
        /// 设置边界条件
        /// </summary>
        /// <param name="stiffMaxtrix">刚度矩阵</param>
        /// <param name="forceVector">受力向量</param>
        /// <param name="boundPoints">边界点</param>
        public static void SetBoundPointParams(ref Matrix stiffMaxtrix, ref Matrix forceVector, List<BoundPointDisplaceParams> boundPoints)
        {
            int r1,r2,r3;

            foreach (BoundPointDisplaceParams curBound in boundPoints)
            {
                r1=curBound.Index * 3;
                r2=curBound.Index * 3 + 1;
                r3=curBound.Index * 3 + 2;
                forceVector[r1, 0] = curBound.Dx;
                forceVector[r2, 0] = curBound.Dy;
                forceVector[r3, 0] = curBound.A;

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

                    if (i == r3)
                    {
                        stiffMaxtrix[r3, i] = 1;//对角线上元素赋值为1
                    }
                    else
                    {
                        stiffMaxtrix[r3, i] = 0;//其他地方赋值为0
                    }
                }
                
            }
        }

   /*     /// <summary>
        /// 计算移位向量
        /// </summary>
        /// <param name="path">被移线</param>
        /// <param name="shapeCollection">邻近图形对象</param>
        /// <param name="boundPoints"></param>
        /// <param name="g">time step迭代步长</param>
        /// <param name="u">受力系数？</param>
        /// <param name="stiffMatrix">刚度矩阵</param>
        /// <param name="forceVector">初始受力向量</param>
        /// <param name="displaceVector">位移向量</param>
        /// <param name="time">迭代次数</param>
        /// <returns>最终位移向量</returns>
        public  static Matrix CalcuDisplaceVector(out ElasticBeam beam,IGeometryCollection shapeCollection, List<BoundPointDisplaceParams> boundPoints,double g, double u, int time)
        {
            Matrix stiffMatrix = CalcuPolyLineMatrix();
            Matrix forceVector=CalcuVetexModelForceVector(path,shapeCollection);
            forceVector = CalcuMoment(path, ref forceVector);
            SetBoundPointParams(ref stiffMatrix, ref forceVector, boundPoints);
            Matrix displaceVector = (stiffMatrix.Inverse()) * forceVector;
    
            //UpdatePath(ref path, displaceVector);
            Matrix leftMatrix;
            Matrix rightVector;
            int size = stiffMatrix.Rows;
            leftMatrix = new Matrix(size, size);
            rightVector = new Matrix(size, 1);

            for (int k = 0; k < time; k++)
            {
                UpdatePath(ref path, displaceVector);
                stiffMatrix = CalcuPolyLineMatrix(path);

                for (int i = 0; i < size; i++)
                {


                    for (int j = 0; j < size; j++)
                    {
                        stiffMatrix[i, j] = g * stiffMatrix[i, j];
                        if (i == j)
                        {
                            leftMatrix[i, j] = 1.0;
                        }
                    }
                }
                leftMatrix = leftMatrix + stiffMatrix;
                leftMatrix = leftMatrix.Inverse();

                for (int j = 0; j < size; j++)
                {
                    rightVector[j, 0] = displaceVector[j, 0] + g * forceVector[j, 0];
                }

                displaceVector = leftMatrix * rightVector;

                forceVector = CalcuVetexModelForceVector(path, shapeCollection);
                forceVector = CalcuMoment(path, ref forceVector);
            }
            return displaceVector;
        }


        /// <summary>
        /// 计算移位向量
        /// </summary>
        /// <param name="path">被移线</param>
        /// <param name="shapeCollection">邻近图形对象</param>
        /// <param name="boundPoints"></param>
        /// <param name="g">time step迭代步长</param>
        /// <param name="u">受力系数？</param>
        /// <param name="stiffMatrix">刚度矩阵</param>
        /// <param name="forceVector">初始受力向量</param>
        /// <param name="displaceVector">位移向量</param>
        /// <param name="time">迭代次数</param>
        /// <returns>最终位移向量</returns>
        public static Matrix CalcuDisplaceVector(ref IPath path, IGeometryCollection shapeCollection, List<BoundPointDisplaceParams> boundPoints,int time)
        {
            Matrix stiffMatrix = CalcuPolyLineMatrix(path);
            Matrix forceVector = CalcuVetexModelForceVector(path, shapeCollection);
            Matrix leftMatrix;

            forceVector = CalcuMoment(path, ref forceVector);
            SetBoundPointParams(ref stiffMatrix, ref forceVector, boundPoints);
            leftMatrix = stiffMatrix.Inverse();
            Matrix displaceVector = leftMatrix * forceVector;

            UpdatePath(ref path, displaceVector);
            int size = stiffMatrix.Rows;

            for (int k = 0; k < time; k++)
            {
                
                UpdatePath(ref path, displaceVector);
                forceVector = CalcuVetexModelForceVector(path, shapeCollection);
                forceVector = CalcuMoment(path, ref forceVector);
                stiffMatrix = CalcuPolyLineMatrix(path);
                leftMatrix = stiffMatrix.Inverse();
                displaceVector = leftMatrix * forceVector;

            }
            return displaceVector;
        }*/

        /// <summary>
        /// 求移位向量
        /// </summary>
        /// <param name="displaceVector_X">X方向移位向量</param>
        /// <param name="displaceVector_Y">Y方向移位向量</param>
        /// <param name="snake">样条对象</param>
        /// <param name="shapeCollection">可能的冲突对象</param>
        public static ElasticBeam CalcuDisplaceVector(ElasticBeam beam, IGeometryCollection shapeCollection, List<BoundPointDisplaceParams> boundPoints, int time, double gr, double u)
        {
            Matrix stiffMatrix=CalcuPolyLineMatrix(beam);
            Matrix forceVector=CalcuVetexModelForceVector(beam, shapeCollection);
            stiffMatrix.WriteTxtFile(@"D:\record\stiffMatrix.txt");
            forceVector.WriteTxtFile(@"D:\record\forceVector.txt");


            SetBoundPointParams(ref stiffMatrix, ref forceVector, boundPoints);//设置边界条件

            Matrix displaceVector = (stiffMatrix.Inverse()) * forceVector;
            displaceVector.WriteTxtFile(@"D:\record\displaceVector.txt");

            return ElasticBeamIterate(stiffMatrix, ref  forceVector, ref  displaceVector, shapeCollection, beam, time, gr, u, boundPoints);    
        }

        /// <summary>
        /// 更新点的位置
        /// </summary>
        /// <param name="beam">弹性梁</param>
        /// <param name="displaceVector">移位向量</param>
        /// <returns>返回新的弹性梁</returns>
        private static ElasticBeam UpdatePosition(ElasticBeam beam, Matrix displaceVector)
        {
            object ms1 = Type.Missing;
            object ms2 = Type.Missing;
            IPath path = beam.Path;
            IPath newPath = new PathClass();
            ElasticBeam newbeam = new ElasticBeam(beam.A, beam.E, beam.I, beam.SymbolWidth, newPath);
            IPointCollection pointSet = path as IPointCollection;
            IPointCollection newPointSet = newPath as IPointCollection;
            int n = pointSet.PointCount;
            IPoint curPoint;
            IPoint newPoint;
            for (int i = 0; i < n; i++)
            {
                newPoint = new PointClass();
                curPoint = pointSet.get_Point(i);
                newPoint.PutCoords(curPoint.X + displaceVector[3 * i, 0], curPoint.Y + displaceVector[3 * i+1, 0]);
                newPointSet.AddPoint(newPoint, ref ms1, ref ms2);
            }

            return newbeam;
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
        public static ElasticBeam ElasticBeamIterate(Matrix stiffMatrix, ref Matrix forceVector, ref Matrix displaceVector, IGeometryCollection shapeCollection, ElasticBeam beam, int time, double gr, double u, List<BoundPointDisplaceParams> boundPoints)
        {
            // Matrix stiffMatrix = CalcuPolyLineMatrix(snake);//计算广度矩阵
            IPath path = beam.Path;
            IPointCollection pointSet = path as IPointCollection;
            int n = pointSet.PointCount;
            Matrix identityM = new Matrix(3 * n, 3 * n);
            Matrix leftMatrix;
            Matrix rightVector;
            ElasticBeam oldBeam = beam; ;
            ElasticBeam newBeam;
           // Matrix forceVector;
            // Matrix forceVector_X;
            // Matrix forceVector_Y;

            for (int i = 0; i < 3 * n; i++)
            {
                for (int j = 0; j < 3 * n; j++)
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
                fileName = @"D:\record\stiffMatrix" + i.ToString() + ".txt";

                newBeam = UpdatePosition(oldBeam, displaceVector);
                stiffMatrix = CalcuPolyLineMatrix(newBeam);

                stiffMatrix.WriteTxtFile(fileName);

                for (int j = 0; j < 3 * n; j++)
                {
                    for (int k = 0; k < 3 * n; k++)
                    {

                        stiffMatrix[j, k] = gr * stiffMatrix[j, k];

                    }
                }

                for (int j = 0; j < 3 * n; j++)
                {
                    forceVector[j, 0] = gr * u * forceVector[j, 0];
                }

                leftMatrix = identityM + stiffMatrix;


                rightVector = displaceVector + forceVector;

                SetBoundPointParams(ref leftMatrix, ref rightVector, boundPoints);

                leftMatrix = leftMatrix.Inverse();

                displaceVector = leftMatrix * rightVector;
               
                fileName = @"D:\record\displaceVector" + i.ToString() + ".txt";
                displaceVector.WriteTxtFile(fileName);
               
                forceVector = CalcuVetexModelForceVector(newBeam, shapeCollection);

                oldBeam = newBeam;
            }

            newBeam=UpdatePosition(oldBeam, displaceVector);

            return newBeam;

        }

   /*    /// <summary>
        /// 判断线目标与给定几何对象是否存在冲突
        /// </summary>
        /// <param name="targetLine">线目标</param>
        /// <param name="conflictShape">可能存在冲突的几何对象</param>
        /// <returns>是否冲突</returns>
        private static bool IsConflict(IPath targetLine, IGeometry conflictShape)
        {
            if (IsEnvelopeConflict(targetLine, conflictShape))
            {
               // if (IsBufferConflict(targetLine, conflictShape))
              //  {
                    return true;
                //}
            }
            return false;
        }

        /// <summary>
        /// 判断线目标的外包矩形的缓冲区与给定几何对象是否存在冲突
        /// </summary>
        /// <param name="targetLine">线目标</param>
        /// <param name="conflictShape">可能存在冲突的几何对象</param>
        /// <returns></returns>
        private static bool IsEnvelopeConflict(IPath targetLine, IGeometry conflictShape)
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
            IPolygon bufferPolygon = pTopop.Buffer(fDenominatorofMapScale * (fWidthofLineSymbol / 2 + fminDistance)) as IPolygon;
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
        private static bool IsBufferConflict(IPath targetLine, IGeometry conflictShape)
        {
             object missing1=Type.Missing;
             object missing2 = Type.Missing;
            IPolyline   poly= new PolylineClass();
            IGeometryCollection polySet = poly as IGeometryCollection;
            polySet.AddGeometry(targetLine,ref missing1,ref missing2);
            //生成缓冲区
            ITopologicalOperator pTopop = poly as ITopologicalOperator;
            IPolygon bufferPolygon=pTopop.Buffer(fDenominatorofMapScale*(fWidthofLineSymbol/2+fminDistance)) as IPolygon;

            //空间关系运算
            IRelationalOperator pRelop = bufferPolygon as IRelationalOperator;
            if (pRelop.Disjoint(conflictShape) || pRelop.Equals(conflictShape))
            {
                return false;
            }
            return true;
        }*/

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
    }
}
