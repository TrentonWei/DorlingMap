using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Carto;

namespace AuxStructureLib
{
    public class ComFunLib
    {
        /// <summary>
        /// 返回等级
        /// </summary>
        /// <param name="w"></param>
        /// <returns></returns>
        public static double getGrade(double w)
        {
            if (w == 1.2)
            {
                return 1;
            }
            else if (w == 1.0)
            {
                return 2;
            }

            else
            {
                return 3;
            }
        }
        /// <summary>
        /// 计算长度
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double CalLineLength(Node p1, Node p2)
        {
            return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
        }

        /// <summary>
        /// 计算长度
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double CalLineLength(TriNode p1, TriNode p2)
        {
            return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
        }

        /// <summary>
        /// 计算直线的方位角[-PI/20~PI/20]
        /// </summary>
        /// <param name="p1">起点</param>
        /// <param name="p2">终点</param>
        /// <returns>方位角</returns>
        public static double CalDirect(Node p1, Node p2)
        {
            double dy = p1.Y - p2.Y;
            double dx = p1.X - p2.X;
            if(dx==0)
            {
                return Math.PI / 2;
            }
            return Math.Atan(dy / dx);
        }
        /// <summary>
        /// 判断外包矩形是否相交
        /// </summary>
        /// <param name="x1">第一个对象最小X</param>
        /// <param name="x2">第一个对象最大X</param>
        /// <param name="y1">第一个对象最小X</param>
        /// <param name="y2">第一个对象最大Y</param>
        /// <param name="x3">第二个对象最小X</param>
        /// <param name="x4">第二个对象最大X</param>
        /// <param name="y3">第二个对象最小Y</param>
        /// <param name="y4">第二个对象最大Y</param>
        /// <returns>是否相交</returns>
        public static bool IsRectIntersect(float x1, float x2, float y1, float y2, float x3, float x4, float y3, float y4)
        {
            if (((x1 <= x3 && x3 <= x2) || (x3 <= x1 && x1 <= x4)) && ((y1 <= y3 && y3 <= y2) || (y3 <= y1 && y1 <= y4)))
                return true;
            return false;
        }
        /// <summary>
        /// 判断两线段是否相交
        /// </summary>
        /// <param name="p1">第一条线段的起点</param>
        /// <param name="p2">第一条线段的终点</param>
        /// <param name="p3">第二条线段的起点</param>
        /// <param name="p4">第一条线段的终点</param>
        /// <returns>是否相交</returns>
        public static bool IsLineSegCross(TriNode p1, TriNode p2, TriNode p3, TriNode p4)
        {
            #region 确定外包矩形
            //先得到外包矩形
            float x1, x2, x3, x4, y1, y2, y3, y4;

            if (p1.X >= p2.X)
            {
                x2 = (float)p1.X; x1 = (float)p2.X;
            }
            else
            {
                x2 = (float)p2.X; x1 = (float)p1.X;
            }

            if (p1.Y >= p2.Y)
            {
                y2 = (float)p1.Y; y1 = (float)p2.Y;
            }
            else
            {
                y2 = (float)p2.Y; y1 = (float)p1.Y;
            }

            if (p3.X >= p4.X)
            {
                x4 = (float)p1.X; x3 = (float)p2.X;
            }
            else
            {
                x4 = (float)p2.X; x3 = (float)p1.X;
            }

            if (p3.Y >= p4.Y)
            {
                y4 = (float)p1.Y; y3 = (float)p2.Y;
            }
            else
            {
                y4 = (float)p2.Y; y3 = (float)p1.Y;
            }
            #endregion

            if (!IsRectIntersect(x1, x2, y1, y2, x3, x4, y3, y4))
                return false;
            double v1, v2, v3, v4;
            v1 = (p2.X - p1.X) * (p4.Y - p1.Y) - (p2.Y - p1.Y) * (p4.X - p1.X);
            v2 = (p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X);
            if (v1 * v2 >= 0)
                return false;
            v3 = (p4.X - p3.X) * (p2.Y - p3.Y) - (p4.Y - p3.Y) * (p2.X - p3.X);
            v4 = (p4.X - p3.X) * (p1.Y - p3.Y) - (p4.Y - p3.Y) * (p1.X - p3.X);
            if (v3 * v4 >= 0)
                return false;
            return true;
        }

        /// <summary>
        /// 判断点在线段的左/右边
        /// </summary>
        /// <param name="poiA">起点</param>
        /// <param name="poiB">终点</param>
        /// <param name="poiM">一点</param>
        /// <returns>结果</returns>
        public static string funReturnRightOrLeft(TriNode poiA, TriNode poiB, TriNode poiM)
        {
            string strResult = "";
            double ax = poiB.X - poiA.X;
            double ay = poiB.Y - poiA.Y;
            double bx = poiM.X - poiA.X;
            double by = poiM.Y - poiA.Y;
            double judge = ax * by - ay * bx;
            if (judge > 0)
            {
                strResult = "LEFT";
            }
            else if (judge < 0)
            {
                strResult = "RIGHT";
            }
            else
            {
                strResult = "ONTHELINE";
            }
            return strResult;
        }
        /// <summary>
        /// 求垂足
        /// </summary>
        /// <param name="s">起点</param>
        /// <param name="e">终点</param>
        /// <param name="p">线段外的点</param>
        /// <returns>垂足</returns>
        public static TriNode ComChuizu(TriNode s, TriNode e, TriNode p)
        {
            double x = 0, y = 0;
            if ((e.X - s.X) == 0 && (e.Y - s.Y) != 0)//平行于y轴
            {
                x = e.X;
                y = p.Y;
            }
            else if ((e.Y - s.Y) == 0 && (e.X - s.X) != 0)//平行于X轴
            {
                x = p.X;
                y = e.Y;
            }
            else if ((e.Y - s.Y) == 0 && (e.X - s.X) == 0)
            {
                x = e.X;
                y = e.Y;
            }
            else if ((e.Y - s.Y) != 0 && (e.X - s.X) != 0)
            {
                double k = (e.Y - s.Y) / (e.X - s.X);
                double k1 = -1 / k;
                x = (k * s.X + k1 * p.X + p.Y - s.Y) / (k + k1);
                y = (k * k1 * (s.X - p.X) + k * p.Y - k1 * s.Y) / (k - k1);
            }
            return new TriNode(x, y);
        }

        /// <summary>
        /// 求垂足
        /// </summary>
        /// <param name="s">起点</param>
        /// <param name="e">终点</param>
        /// <param name="p">线段外的点</param>
        /// <returns>垂足</returns>
        public static Node ComChuizu(Node s, Node e, Node p)
        {
            double x = 0, y = 0;
            if ((e.X - s.X) == 0 && (e.Y - s.Y) != 0)//平行于y轴
            {
                x = e.X;
                y = p.Y;
            }
            else if ((e.Y - s.Y) == 0 && (e.X - s.X) != 0)//平行于X轴
            {
                x = p.X;
                y = e.Y;
            }
            else if ((e.Y - s.Y) == 0 && (e.X - s.X) == 0)
            {
                x = e.X;
                y = e.Y;
            }
            else if ((e.Y - s.Y) != 0 && (e.X - s.X) != 0)
            {
                double k = (e.Y - s.Y) / (e.X - s.X);
                double k1 = -1 / k;
                x = (k * s.X + k1 * p.X + p.Y - s.Y) / (k + k1);
                y = (k * k1 * (s.X - p.X) + k * p.Y - k1 * s.Y) / (k - k1);
            }
            return new TriNode(x, y) as Node;
        }



        /// <summary>
        /// 判断点是否在多边形内部
        /// </summary>
        /// <param name="p">点</param>
        /// <param name="polygon">多边形</param>
        /// true在多边形内；false不在多边形内
        /// <returns>是否在多边形内部</returns>
        public static bool IsPointinPolygon(TriNode p, List<TriNode> polygon)
        {
            int n = polygon.Count;
            if (n < 3)
            {
                return false;
            }
            //double Max_X = -99999;
            double Min_X = polygon[0].X;
            //double Max_Y = -99999;
            //double Min_Y = 99999;

            double max_x = -99999;
            double min_x = 99999;
            double max_y = -99999;
            double min_y = 99999;

             //找到最大值
             for (int i = 0; i < n; i++)
             {
                //Max_X = max(Max_X, polygon[i].X);
                 //Max_Y = max(Max_Y, polygon[i].Y);
                 Min_X = min(Min_X, polygon[i].X);
                 //Min_Y = min(Min_Y, polygon[i].Y);
             }
             TriNode B = new TriNode();
             B.X = Min_X-10000;                          //将对象 B 的成员初始化
             B.Y = p.Y;
           
            int number = 0;                      //定义整型变量 number 用于记录 AB 与多边形边交点个数
            for (int i = 0; i < n; i++)
            {
                TriNode p1 = null;
                TriNode p2 = null;

                if (i == n - 1)
                {
                    p1 = polygon[n-1];
                    p2 = polygon[0];
                }
                else
                {
                    p1 = polygon[i];
                    p2 = polygon[i+1];
                }
                max_x = max(p1.X, p2.X);
                max_y = max(p1.Y, p2.Y);
                min_x = min(p1.X, p2.X);
                min_y = min(p1.Y, p2.Y);
                //max_x max_y min_x min_y 四个变量用于辅助界定边界
                if (cross(p1, p2, p) == 0 && p.X >= min_x && p.X <= max_x && p.Y >= min_y && p.Y <= max_y)
                {
                    //如果三点共线，而且 A 点满足边界条件 number=1 ，并退出循环
                    number = 1;
                    break;
                }
                else if (p1.Y !=p2.Y)  //当  p[i].y!=p[i-1],y 时
                {
                    if (IsLineSegCross(p, B, p1, p2))
                    {
                        number++;
                    }
                }
            }
            if (number % 2 == 1) return true;          //如果 number 是奇数，函数返回 true
            else return false;                    //否则函数返回 false
        }

        private static double max(double a, double b)          //判断最大值函数
        {
            return a > b ? a : b;
        }
        private static double min(double a, double b)          //判断最小值函数
        {
            return a < b ? a : b;
        }

        private static double cross(TriNode a, TriNode b, TriNode c) //计算叉积函数
        {
            return (b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y);
        }

        /// <summary>
        /// 计算三角形重心
        /// </summary>
        /// <param name="triangle">三角形</param>
        /// <returns>返回中心点</returns>
        public static TriNode CalCenter(Triangle triangle)
        {
            TriNode pt1 = triangle.point1;
            TriNode pt2 = triangle.point2;
            TriNode pt3 = triangle.point3;

            double x = pt1.X + pt2.X + pt3.X;
            x /= 3;
            double y = pt1.Y + pt2.Y + pt3.Y;
            y /= 3;
            return new TriNode(x, y);
        }

        /// <summary>
        /// 求线段的中点
        /// </summary>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <returns></returns>
        public static TriNode CalLineCenterPoint(TriNode pt1, TriNode pt2)
        {
            double x = pt1.X + pt2.X;
            x /= 2;
            double y = pt1.Y + pt2.Y;
            y /= 2;
            return new TriNode(x, y);
        }

        /// <summary>
        /// 求线段的中点
        /// </summary>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <returns></returns>
        public static TriNode CalLineCenterPoint(TriEdge e)
        {
            TriNode pt1=e.startPoint;
            TriNode pt2=e.endPoint;
            double x = pt1.X + pt2.X;
            x /= 2;
            double y = pt1.Y + pt2.Y;
            y /= 2;
            return new TriNode(x, y);
        }
        /// <summary>
        /// 计算三角形面积
        /// </summary>
        /// <param name="tri">三角形</param>
        /// <returns>面积</returns>
        public static double ComTriArea(Triangle tri)
        {
            double x1 = tri.point1.X;
            double y1 = tri.point1.Y;
            double x2 = tri.point2.X;
            double y2 = tri.point2.Y;
            double x3 = tri.point3.X;
            double y3 = tri.point3.Y;

            double a = Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
            double b = Math.Sqrt((x2 - x3) * (x2 - x3) + (y2 - y3) * (y2 - y3));
            double c = Math.Sqrt((x3 - x1) * (x3 - x1) + (y3 - y1) * (y3 - y1));
            double s = 0.5 * (a + b + c);
            return s;
        }


        /// <summary>
        /// 计算v1到v2v3的距离，三角形的高
        /// </summary>
        /// <param name="v1">顶点1</param>
        /// <param name="v2">顶点2</param>
        /// <param name="v3">顶点3</param>
        /// <returns>过顶点1的高</returns>
        public static double ComHeight(TriNode v1, TriNode v2, TriNode v3)
        {
            double x1 = v1.X;
            double y1 = v1.Y;
            double x2 = v2.X;
            double y2 = v2.Y;
            double x3 = v3.X;
            double y3 = v3.Y;
            double a = Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
            double b = Math.Sqrt((x2 - x3) * (x2 - x3) + (y2 - y3) * (y2 - y3));
            double c = Math.Sqrt((x3 - x1) * (x3 - x1) + (y3 - y1) * (y3 - y1));
            double s = 0.5 * (a + b + c);
            s = Math.Sqrt(s * (s - a) * (s - b) * (s - c));
            double d = 2 * s / b;
            return d;
        }

        /// <summary>
        /// 点到线段的最小距离
        /// </summary>
        /// <param name="v1">顶点</param>
        /// <param name="v2">底边顶点1</param>
        /// <param name="v3">底边顶点2</param>
        /// <param name="nearestPoint">最近距离</param>
        /// <returns>最小距离</returns>
        public static double CalMinDisPoint2Line(Node v1, Node v2, Node v3)
        {
            if((v1.X==v2.X&&v1.Y==v2.Y)||(v1.X==v3.X&&v1.Y==v3.Y))
            {
                return 0;
            }

            double A = (v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y);
            double B = (v2.X - v3.X) * (v2.X - v3.X) + (v2.Y - v3.Y) * (v2.Y - v3.Y);
            double C = (v3.X - v1.X) * (v3.X - v1.X) + (v3.Y - v1.Y) * (v3.Y - v1.Y);
            double a = Math.Sqrt(A);
            double b = Math.Sqrt(B);
            double c = Math.Sqrt(C);

            double cosB = (A + B - C) / (2 * a * b);
            double cosC = (C + B - A) / (2 * b * c);

            if (cosB > 0 && cosC > 0)
            {
                double d = Math.Sqrt((v2.X - v3.X) * (v2.X - v3.X) + (v2.Y - v3.Y) * (v2.Y - v3.Y));
                double e = (v3.X - v2.X) * (v1.X - v2.X) + (v3.Y - v2.Y) * (v1.Y - v2.Y);
                e = e / (d * d);
                double x4 = v2.X + (v3.X - v2.X) * e;
                double y4 = v2.Y + (v3.Y - v2.Y) * e;
                return Math.Sqrt((x4 - v1.X) * (x4 - v1.X) + (y4 - v1.Y) * (y4 - v1.Y));
            }
            else
            {
                if (cosB <= 0)
                {
                    return a;
                }
                else if (cosC <= 0)
                {
                    return c;
                }
            }
            return 9999999;
        }

        /// <summary>
        /// 点到线段的最小距离
        /// </summary>
        /// <param name="v1">顶点</param>
        /// <param name="v2">底边顶点1</param>
        /// <param name="v3">底边顶点2</param>
        /// <param name="nearestPoint">最近距离</param>
        /// <returns>最小距离</returns>
        public double pCalMinDisPoint2Line(Node v1, Node v2, Node v3)
        {
            if ((v1.X == v2.X && v1.Y == v2.Y) || (v1.X == v3.X && v1.Y == v3.Y))
            {
                return 0;
            }

            double A = (v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y);
            double B = (v2.X - v3.X) * (v2.X - v3.X) + (v2.Y - v3.Y) * (v2.Y - v3.Y);
            double C = (v3.X - v1.X) * (v3.X - v1.X) + (v3.Y - v1.Y) * (v3.Y - v1.Y);
            double a = Math.Sqrt(A);
            double b = Math.Sqrt(B);
            double c = Math.Sqrt(C);

            double cosB = (A + B - C) / (2 * a * b);
            double cosC = (C + B - A) / (2 * b * c);

            if (cosB > 0 && cosC > 0)
            {
                double d = Math.Sqrt((v2.X - v3.X) * (v2.X - v3.X) + (v2.Y - v3.Y) * (v2.Y - v3.Y));
                double e = (v3.X - v2.X) * (v1.X - v2.X) + (v3.Y - v2.Y) * (v1.Y - v2.Y);
                e = e / (d * d);
                double x4 = v2.X + (v3.X - v2.X) * e;
                double y4 = v2.Y + (v3.Y - v2.Y) * e;
                return Math.Sqrt((x4 - v1.X) * (x4 - v1.X) + (y4 - v1.Y) * (y4 - v1.Y));
            }
            else
            {
                if (cosB <= 0)
                {
                    return a;
                }
                else if (cosC <= 0)
                {
                    return c;
                }
            }
            return 9999999;
        }
        ///// <summary>
        ///// 计算多边形的重心（中心）
        ///// </summary>
        ///// <param name="polygonBorderLine">边界线</param>
        ///// <returns>返回多边形重心</returns>
        //public static TriNode CalpolygonCenterPoint(List<TriNode> polygonBorderLine)
        //{
        //    int count= polygonBorderLine.Count;
        //    double x = 0;
        //    double y = 0;
        //    for(int i=0;i<count;i++)
        //    {
        //        x = x + polygonBorderLine[i].X;
        //        y = y + polygonBorderLine[i].Y;
        //    }

        //    TriNode resNode=new TriNode(x/count,y/count);
        //    return resNode;
        //}

        /// <summary>
        /// 计算分组的中心
        /// </summary>
        /// <param name="Nodes">边界线</param>
        /// <returns>返回分组的中心</returns>
        public static ProxiNode CalGroupCenterPoint(List<ProxiNode> Nodes)
        {
            int count = Nodes.Count;
            double x = 0;
            double y = 0;
            for (int i = 0; i < count; i++)
            {
                x = x + Nodes[i].X;
                y = y + Nodes[i].Y;
            }

            ProxiNode resNode = new ProxiNode(x / count, y / count);
            return resNode;
        }
        /// <summary>
        /// 计算多边形的重心（中心）
        /// </summary>
        /// <param name="polygonBorderLine">边界线</param>
        /// <returns>返回多边形重心</returns>
        public static TriNode CalpolygonCenterPoint(List<TriNode> polygonBorderLine)
        {
            int count = polygonBorderLine.Count;
            double x = 0;
            double y = 0;
            for (int i = 0; i < count; i++)
            {
                x = x + polygonBorderLine[i].X;
                y = y + polygonBorderLine[i].Y;
            }

            TriNode resNode = new TriNode(x / count, y / count);
            return resNode;
        }
        /// <summary>
        /// 求点到折线的最近距离
        /// </summary>
        /// <param name="p">点</param>
        /// <param name="pline">折线</param>
        ///<param name="isPerpendicular">是否垂直</param>
        /// <returns>最近距离</returns>
        public static Node MinDisPoint2Polyline(Node p, PolylineObject pline,out bool  isPerpendicular)
        {
            int n = pline.PointList.Count;
            Node p1 = pline.PointList[0];
            Node p2 = pline.PointList[1];
            Node mP = null;
            isPerpendicular = false;
            bool Perpendicular = false;
            double minDis = ComFunLib.CalMinDisPoint2Line(p, p1, p2, out mP, out Perpendicular);
            for (int i = 1; i < n - 1; i++)
            {
                p1 = pline.PointList[i];
                p2 = pline.PointList[i+1];
                Node r = null;
                double Dis = ComFunLib.CalMinDisPoint2Line(p, p1, p2, out r,out Perpendicular);
                if (Dis < minDis)
                {
                    isPerpendicular = Perpendicular;
                    minDis = Dis;
                    mP = r;
                }
            }
            return mP;
        }

        /// <summary>
        /// 求点到折线的最近距离
        /// </summary>
        /// <param name="p">点</param>
        /// <param name="pline">折线</param>
        ///<param name="isPerpendicular">是否垂直</param>
        /// <returns>最近距离</returns>
        public static Node MinDisPoint2PolylineVertices(Node p, PolylineObject pline)
        {
            int n = pline.PointList.Count;
            Node p1 = pline.PointList[0];


            double minDis = ComFunLib.CalLineLength(p,p1);
            Node mP = p1;
            for (int i = 1; i < n; i++)
            {
                p1 = pline.PointList[i];
                Node r = null;
                double Dis = ComFunLib.CalLineLength(p, p1);
                if (Dis < minDis)
                {
                    minDis = Dis;
                    mP = p1;
                }
            }
            return mP;
        }
        /// <summary>
        /// 求点到折线的最近距离
        /// </summary>
        /// <param name="p">点</param>
        /// <param name="pline">折线坐标序列</param>
        ///<param name="isPerpendicular">是否垂直</param>
        /// <returns>最近距离</returns>
        public static Node MinDisPoint2Polyline(Node p, List<TriNode> PointList, out bool isPerpendicular)
        {
            int n = PointList.Count;
            Node p1 = PointList[0];
            Node p2 = PointList[1];
            Node mP = null;
            isPerpendicular = false;
            bool Perpendicular = false;
            double minDis = ComFunLib.CalMinDisPoint2Line(p, p1, p2, out mP, out Perpendicular);
            for (int i = 1; i < n - 1; i++)
            {
                p1 = PointList[i];
                p2 = PointList[i + 1];
                Node r = null;
                double Dis = ComFunLib.CalMinDisPoint2Line(p, p1, p2, out r, out Perpendicular);
                if (Dis < minDis)
                {
                    isPerpendicular = Perpendicular;
                    minDis = Dis;
                    mP = r;
                }
            }
            return mP;
        }


        /// <summary>
        /// 点到线段的最小距离
        /// </summary>
        /// <param name="v1">顶点</param>
        /// <param name="v2">底边顶点1</param>
        /// <param name="v3">底边顶点2</param>
        /// <param name="nearestPoint">最近距离</param>
        /// <param name="v4">最近距离对应的点</param>
        /// <param name="isPerpendicular">最近距离是否是沿着垂线上</param>
        /// <returns>最小距离</returns>
        public static double CalMinDisPoint2Line(Node v1, Node v2, Node v3, out Node v4, out bool isPerpendicular)
        {
            //点在线上的情况
            if ((v1.X == v2.X && v1.Y == v2.Y) || (v1.X == v3.X && v1.Y == v3.Y))
            {
                v4 = null;
                isPerpendicular = false;
                return 0;
            }

            double A = (v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y);
            double B = (v2.X - v3.X) * (v2.X - v3.X) + (v2.Y - v3.Y) * (v2.Y - v3.Y);
            double C = (v3.X - v1.X) * (v3.X - v1.X) + (v3.Y - v1.Y) * (v3.Y - v1.Y);
            double a = Math.Sqrt(A);
            double b = Math.Sqrt(B);
            double c = Math.Sqrt(C);

            double cosB = (A + B - C) / (2 * a * b);
            double cosC = (C + B - A) / (2 * b * c);

            if (cosB > 0 && cosC > 0)
            {
                double d = Math.Sqrt((v2.X - v3.X) * (v2.X - v3.X) + (v2.Y - v3.Y) * (v2.Y - v3.Y));
                double e = (v3.X - v2.X) * (v1.X - v2.X) + (v3.Y - v2.Y) * (v1.Y - v2.Y);
                e = e / (d * d);
                double x4 = v2.X + (v3.X - v2.X) * e;
                double y4 = v2.Y + (v3.Y - v2.Y) * e;
                v4 = new TriNode(x4, y4);
                isPerpendicular = true;
                return Math.Sqrt((x4 - v1.X) * (x4 - v1.X) + (y4 - v1.Y) * (y4 - v1.Y));
            }
            else
            {
                if (cosB <= 0)
                {
                    v4 = v2;
                    isPerpendicular = false;
                    return a;
                }
                else if (cosC <= 0)
                {
                    v4 = v3;
                    isPerpendicular = false;
                    return c;
                }
            }
            v4 = null;
            isPerpendicular = false;
            return 9999999;
        }

        /// <summary>
        /// 找到瓶颈三角形
        /// </summary>
        /// <param name="TriList">三角形列表</param>
        /// <returns>返回一个新的三角形列表</returns>
        public static List<Triangle> FindBottle_NeckTriangle(List<Triangle> TriList)
        {
            if (TriList == null || TriList.Count == 0)
                return null;
            List<Triangle> triList = new List<Triangle>();
            if (TriList.Count == 1)
            {
                return TriList;
            }

            else if (TriList.Count == 2)
            {
                // List<Triangle> triList = new List<Triangle>();
                if (TriList[0].W > TriList[1].W)
                {
                    //List<Triangle> triList = new List<Triangle>();
                    triList.Add(TriList[1]);
                    return triList;
                }
                else
                {
                    //List<Triangle> triList = new List<Triangle>();
                    triList.Add(TriList[0]);
                    return triList;
                }
            }
            else if (TriList.Count >= 3)
            {

                int n = TriList.Count;
                if (TriList[0].W < TriList[1].W)
                {
                    triList.Add(TriList[0]);
                }
                for (int i = 1; i < n - 1; i++)
                {
                    if (TriList[i - 1].W >= TriList[i].W && TriList[i + 1].W >= TriList[i].W)
                    {
                        triList.Add(TriList[i]);
                    }
                }
                if (TriList[n - 1].W < TriList[n - 2].W)
                {
                    triList.Add(TriList[n - 1]);
                }
            }
            return triList;
        }

        /// <summary>
        /// 获取两条线段的交点
        /// </summary>
        /// <param name="p1">第一条线段的起点</param>
        /// <param name="p2">第一条线段的终点</param>
        /// <param name="p3">第二条线段的起点</param>
        /// <param name="p4">第一条线段的终点</param>
        /// <returns>是否相交</returns>
        public static TriNode CrossNode(TriNode p1, TriNode p2, TriNode p3, TriNode p4)
        {
            #region 相交，求交点
            if (ComFunLib.IsLineSegCross(p1, p2, p3, p4))
            {
                double Area_abc = (p1.X - p3.X) * (p2.Y - p3.Y) - (p1.Y - p3.Y) * (p2.X - p3.X);
                double Area_abd = (p1.X - p4.X) * (p2.Y - p4.Y) - (p1.Y - p4.Y) * (p2.X - p4.X);
                double Area_cda = (p3.X - p1.X) * (p4.Y - p1.Y) - (p3.Y - p1.Y) * (p4.X - p1.X);

                double t = Area_cda / (Area_abd - Area_abc);
                double Dx = t * (p2.X - p1.X);
                double Dy = t * (p2.Y - p1.Y);
                TriNode OutNode = new TriNode(p1.X + Dx, p1.Y + Dy);
                return OutNode;
            }
            #endregion

            #region 不相交，返回空
            else
            {
                return null;
            }
            #endregion
        }

        /// <summary>
        /// 给定两点，获得CurPoint沿該条直线的延长线
        /// </summary>
        /// <param name="CurPoint"></param>
        /// <param name="EndPoint"></param>
        /// <returns></returns>
        public static List<TriNode> GetExtendingLine(TriNode CurPoint)
        {
            TriNode StartNode = new TriNode();
            TriNode EndNode = new TriNode();

            double sExtend_X = CurPoint.Initial_X - 100 * (CurPoint.X - CurPoint.Initial_X);
            double sExtend_Y = CurPoint.Initial_Y - 100 * (CurPoint.Y - CurPoint.Initial_Y);

            double eExtend_X = 100 * (CurPoint.X - CurPoint.Initial_X) + CurPoint.X;
            double eExtend_Y = 100 * (CurPoint.Y - CurPoint.Initial_Y) + CurPoint.Y;

            StartNode.X = sExtend_X; StartNode.Y = sExtend_Y;
            EndNode.X = eExtend_X; EndNode.Y = eExtend_Y;

            List<TriNode> EdgeNodes = new List<TriNode>();
            EdgeNodes.Add(StartNode);
            EdgeNodes.Add(EndNode);

            return EdgeNodes;
        }
    }
}
