using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AuxStructureLib
{
    /// <summary>
    /// Voronoi多边形
    /// </summary>
    public class VoronoiPolygon
    {
        public List<TriNode> PointSet = null; //构成VoronoiPolygon的点击
        public TriNode Point = null;          //Voronoi内部点,如果是多边形则为其多边形的中心
        public bool IsPolygon = true;         //是否是封闭的多边形
        public List<Skeleton_Arc> ArcList;    //骨架线弧段列表
        public MapObject MapObj = null;
        private double area =-1;
        public VoronoiPolygon(MapObject mapObj, TriNode point)
        {

            this.MapObj = mapObj;
            this.Point = point;
            ArcList = new List<Skeleton_Arc>();
            PointSet = new List<TriNode>();
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        public VoronoiPolygon(TriNode point)
        {
            PointSet = new List<TriNode>();
            Point = point;
        }
        /// <summary>
        /// 构造V多边形的点序列
        /// </summary>
        public void CreateVoronoiPolygonfrmSkeletonArcList()
        {
            if (this.ArcList == null || this.ArcList.Count == 0)
            {
                this.PointSet = null;
                return;
            }
            bool[] flags = new bool[ArcList.Count];
            bool isComplete=false;
            TriNode curNode = null;
            //第一条弧段
            if (ArcList[0].PointList != null && ArcList[0].PointList.Count!=0)
            {
                foreach (TriNode node in ArcList[0].PointList)
                {
                    this.PointSet.Add(node);
                }
            } 
            flags[0] = true;
            curNode = ArcList[0].PointList[ArcList[0].PointList.Count - 1];
            while (!isComplete)
            {
                for (int j = 0; j < ArcList.Count; j++)
                {
                    if (flags[j] != true)
                    {
                        Skeleton_Arc curArc = ArcList[j];
                        int n = curArc.PointList.Count;
                        if ((Math.Abs(curArc.PointList[0].X - curNode.X) < 0.00001)
                            && (Math.Abs(curArc.PointList[0].Y - curNode.Y) < 0.00001))

                        {
                            for (int i = 1; i <= n - 1; i++)
                            {
                                this.PointSet.Add(curArc.PointList[i]);
                            }
                            curNode = curArc.PointList[n - 1];
                            flags[j] = true;
                        }
                        else if ((Math.Abs(curArc.PointList[n - 1].X - curNode.X) < 0.00001)
                            && (Math.Abs(curArc.PointList[n - 1].Y - curNode.Y) < 0.00001))
                       
                        {
                            for (int i = n - 2; i >= 0; i--)
                            {
                                this.PointSet.Add(curArc.PointList[i]);
                            }
                            curNode = curArc.PointList[0];
                            flags[j] = true;

                        }
                    }
                }
                foreach (bool f in flags)
                {
                    if (f == false)
                    {
                        isComplete = false;
                        break;
                    }
                    isComplete = true;
                }
            }
        }

        /// <summary>
        /// 计算多边形面积
        /// </summary>
        /// <returns></returns>
        public double Area
        {
            get
            {
                if (this.area != -1)
                    return this.area;
                else
                {
                    this.area = 0;
                    int n = this.PointSet.Count;
                    this.PointSet.Add(PointSet[0]);
                    for (int i = 0; i < n; i++)
                    {
                        area += (PointSet[i].X * PointSet[i + 1].Y - PointSet[i + 1].X * PointSet[i].Y);

                    }
                    area = 0.5 * Math.Abs(area);
                    this.PointSet.RemoveAt(n);
                    return area;
                }
            }
        }
        /// <summary>
        /// VoronoiPolygonduo多边形
        /// </summary>
        /// <param name="_isPolygon"></param>
        public VoronoiPolygon(bool _isPolygon,TriNode point)
        {
            IsPolygon = _isPolygon;
            PointSet = new List<TriNode>();
            Point = point;
        }
        /// <summary>
        /// 利用格雷厄姆凸包生成算法对m_VoronoiPoint中的
        /// </summary>
        public void CreateVoronoiPolygon()
        {
            if (PointSet == null || PointSet.Count == 0)
                return;
            //构成封闭多边形
            if (this.IsPolygon == true)
            {
                TriNode.GetAssortedPoints(this.PointSet);
                this.PointSet.Add(PointSet[0]);
            }
            else
            {
                TriNode.GetAssortedPoints(this.PointSet);
                this.PointSet.Add(PointSet[0]);
            }
        }


        /// <summary>
        /// 拓扑关系纠正-2014-4-3
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="odx"></param>
        /// <param name="ody"></param>
        /// <param name="e">一个很小的值，目的是为了防止两目标相切</param>
        public void TopologicalConstraint2(double dx,double dy,double e, out double odx,out double ody)
        {
            odx = dx;
            ody = dy;
            double l = Math.Sqrt(dx * dx + dy * dy);
            double sin = dy / l;
            double cos = dx / l;
            double orgX = this.Point.X;
            double orgY = this.Point.Y;
            List<TriNode> polyTrasedPointList = new List<TriNode>();
            List<TriNode> vPolyTrasedPointList = new List<TriNode>();
            #region 坐标转换
            //求V多边形转换后的坐标
            int index = 0;
            foreach (TriNode curNode in this.PointSet)
            {
                double x = curNode.X - orgX;
                double y = curNode.Y - orgY;
                double x1 = x * cos + y * sin;
                double y1 = y * cos - x * sin;
                TriNode p = new TriNode(x1, y1, index);
                index++;
                vPolyTrasedPointList.Add(p);
            }
            //求P多边形转换后的坐标
            index = 0;
            foreach (TriNode curNode in (this.MapObj as PolygonObject).PointList)
            {
                double x = curNode.X - orgX;
                double y = curNode.Y - orgY;
                double x1 = x * cos + y * sin;
                double y1 = y * cos - x * sin;
                TriNode p = new TriNode(x1, y1, index);
                index++;
                polyTrasedPointList.Add(p);
            }
            #endregion

            #region 找到最右点，最上点和最下点
            //找到最右点，最上点和最下点
            double maxX = double.NegativeInfinity;
            double minY = double.PositiveInfinity;
            double maxY = double.NegativeInfinity; 
            TriNode maxPXNode = null;
            TriNode minPYNode = null;
            TriNode maxPYNode = null;
            foreach (TriNode curNode in polyTrasedPointList)
            {
                if (curNode.X > maxX)
                {
                    maxX = curNode.X;
                    maxPXNode = curNode;
                }
                if (curNode.Y > maxY)
                {
                    maxY=curNode.Y;
                    maxPYNode = curNode;
                }
                if(curNode.Y < minY)
                {
                    minY = curNode.Y;
                    minPYNode = curNode;
                }
            }
            #endregion

            #region 将多边形从最上和最下点处分成两段
            List<TriNode> P1PointList = new List<TriNode>();
            List<TriNode> P2PointList = new List<TriNode>();

            int i = minPYNode.ID;
            int j = maxPYNode.ID;
            int n=polyTrasedPointList.Count;
            double sumX1 = 0;
            double minPx1 = double.PositiveInfinity;
            TriNode minXP1Ndoe = null;
            double maxPx1 = double.NegativeInfinity;
            TriNode maxXP1Ndoe = null;
            while (i % n != (j + 1) % n)
            {
                P1PointList.Add(polyTrasedPointList[i % n]);
                sumX1 += polyTrasedPointList[i % n].X;
                if (polyTrasedPointList[i % n].X < minPx1)
                {
                    minPx1 = polyTrasedPointList[i % n].X;
                    minXP1Ndoe = polyTrasedPointList[i % n];
                }
                if (polyTrasedPointList[i % n].X > maxPx1)
                {
                    maxPx1 = polyTrasedPointList[i % n].X;
                    maxXP1Ndoe = polyTrasedPointList[i % n];
                }
                i = i + 1;
            }

            i = maxPYNode.ID;
            j = minPYNode.ID;
            TriNode minXP2Ndoe = null;
            double sumX2 = 0;
            double minPx2 = double.PositiveInfinity;
            double maxPx2 = double.NegativeInfinity;
            TriNode maxXP2Ndoe = null;
            while (i % n != (j + 1) % n)
            {
                P2PointList.Add(polyTrasedPointList[i % n]);
                sumX2 += polyTrasedPointList[i % n].X;
                if (polyTrasedPointList[i % n].X < minPx2)
                {
                    minPx2 = polyTrasedPointList[i % n].X;
                    minXP2Ndoe = polyTrasedPointList[i % n];
                }
                if (polyTrasedPointList[i % n].X > maxPx2)
                {
                    maxPx2 = polyTrasedPointList[i % n].X;
                    maxXP2Ndoe = polyTrasedPointList[i % n];
                }
                i = i + 1;
            }

            //保持P2PointLis的X值之和小于P1PointList的
            if (sumX2 > sumX1)
            {
                double dtemp = maxPx1;
                maxPx1 = maxPx2;
                maxPx2 = dtemp;

                double dtempm = minPx1;
                minPx1 = minPx2;
                minPx2 = dtempm;


                TriNode tn = minXP2Ndoe;
                minXP2Ndoe = minXP1Ndoe;
                minXP1Ndoe = tn;

                TriNode tnm = maxXP2Ndoe;
                maxXP2Ndoe = maxXP1Ndoe;
                maxXP1Ndoe = tnm;

                List<TriNode> temp = null;
                temp = P1PointList;
                P1PointList = P2PointList;
                P2PointList = temp;
            }
            #endregion

            #region V多边线上的线段
            double vMinX=double.PositiveInfinity;
            TriNode vMinXNode = null;

            TriNode vMinYNode = null;
            double vMinY = double.PositiveInfinity;

            TriNode vMaxYNode = null;
            double vMaxY = double.NegativeInfinity;
            foreach (TriNode curNode in vPolyTrasedPointList)
            {
                if (curNode.Y >= minY && curNode.Y <= maxY && curNode.X > minPx1)
                {
                    if (curNode.Y > vMaxY)
                    {
                        vMaxYNode = curNode;
                        vMaxY = curNode.Y;
                    }
                    if (curNode.Y < vMinY)
                    {
                        vMinYNode = curNode;
                        vMinY = curNode.Y;
                    }
                    if(curNode.X < vMinX)
                    {
                        vMinXNode = curNode;
                        vMinX = curNode.X;
                    }
                }
            }
            if (vMinYNode == null || vMaxYNode == null)//多边形太小的情况
            {
                double d1 = double.PositiveInfinity;
                double d2 = double.PositiveInfinity;
                foreach (TriNode curNode in vPolyTrasedPointList)
                {
                
                    if (curNode.X > minPx1)
                    {
                        double d11 = minY-curNode.Y;
                        double d22 = curNode.Y-maxY;
                        if (d11 >= 0 && d11 < d1)
                        {
                            vMinYNode = curNode;
                            vMinY = curNode.Y;

                        }

                        if (d22 >= 0 && d22 < d2)
                        {
                            vMaxYNode = curNode;
                            vMaxY = curNode.Y;

                        }
                       
                        if (curNode.X < vMinX)
                        {
                            vMinXNode = curNode;
                            vMinX = curNode.X;
                        }
                    }
                }
            }
            #endregion

            #region 将多边形从最上和最下点处分成两段
            List<TriNode> V1PointList = new List<TriNode>();
            List<TriNode> V2PointList = new List<TriNode>();

            i = vMinYNode.ID+n-1;
            j = vMaxYNode.ID+n+1;

            n = vPolyTrasedPointList.Count;
            double vsumX1 = 0;
            double vminPx1 = double.PositiveInfinity;

            while (i % n != (j + 1) % n)
            {
                V1PointList.Add(vPolyTrasedPointList[i % n]);
                vsumX1 += vPolyTrasedPointList[i % n].X;
                if (vPolyTrasedPointList[i % n].X < vminPx1)
                    vminPx1 = vPolyTrasedPointList[i % n].X;
                i = i + 1;
            }

            i = vMaxYNode.ID + n - 1; ;
            j = vMinYNode.ID + n + 1; ;
            double vsumX2 = 0;
            double vminPx2 = double.PositiveInfinity;

            while (i % n != (j + 1) % n)
            {
                V2PointList.Add(vPolyTrasedPointList[i % n]);
                vsumX2 += vPolyTrasedPointList[i % n].X;
                if (vPolyTrasedPointList[i % n].X < vminPx2)
                    vminPx2 = vPolyTrasedPointList[i % n].X;
                i = i + 1;
            }

            //保持P2PointLis的X值之和小于P1PointList的
            if (vsumX2 > vsumX1)
            {
                double dtemp = vminPx1;
                vminPx1 = vminPx2;
                vminPx2 = dtemp;
                List<TriNode> temp = null;
                temp = V1PointList;
                V1PointList = V2PointList;
                V2PointList = temp;
            }
            #endregion

            #region P1PointList， V1PointList，vMinXNode, minXP1Ndoe,求minXP1Ndoe到V1PointList的距离，和vMinXNode到P1PointList的距离
            //P->V       
            double minD = double.PositiveInfinity;
            int m=P1PointList.Count;
            double x0 = 0;
            double y0 = 0;
            for (int p = 0; p < m; p++)
            {
                x0 = P1PointList[p].X; 
                y0 = P1PointList[p].Y;
                double dis = CalDistance(x0, y0, V1PointList);
                if (dis < minD)
                    minD = dis;
            }


            //V->P
            n = V1PointList.Count;
            for (int k = 1; k < n - 1; k++)
            {
                x0 = V1PointList[k].X;
                y0 = V1PointList[k].Y;
                double dis = CalDistance(x0, y0, P1PointList);
                if (dis < minD)
                    minD = dis;
       
            }
            #endregion

            double olength = Math.Sqrt(dx* dx + dy * dy);
            if (minD < olength)
            {
                double s = minD / olength;
                odx = dx * s - e;
                ody = dy * s - e;
            }
        }


        /// <summary>
        /// 拓扑关系纠正-2014-4-1
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="odx"></param>
        /// <param name="ody"></param>
        /// <param name="e">一个很小的值，目的是为了防止两目标相切</param>
        public void TopologicalConstraint(double dx, double dy, double e, out double odx, out double ody)
        {
            odx = dx;
            ody = dy;
            double l = Math.Sqrt(dx * dx + dy * dy);
            double sin = dy / l;
            double cos = dx / l;
            double orgX = this.Point.X;
            double orgY = this.Point.Y;
            List<TriNode> polyTrasedPointList = new List<TriNode>();
            List<TriNode> vPolyTrasedPointList = new List<TriNode>();
            #region 坐标转换
            //求V多边形转换后的坐标
            int index = 0;
            foreach (TriNode curNode in this.PointSet)
            {
                double x = curNode.X - orgX;
                double y = curNode.Y - orgY;
                double x1 = x * cos + y * sin;
                double y1 = y * cos - x * sin;
                TriNode p = new TriNode(x1, y1, index);
                index++;
                vPolyTrasedPointList.Add(p);
            }
            //求P多边形转换后的坐标
            index = 0;
            foreach (TriNode curNode in (this.MapObj as PolygonObject).PointList)
            {
                double x = curNode.X - orgX;
                double y = curNode.Y - orgY;
                double x1 = x * cos + y * sin;
                double y1 = y * cos - x * sin;
                TriNode p = new TriNode(x1, y1, index);
                index++;
                polyTrasedPointList.Add(p);
            }
            #endregion

            #region P1PointList， V1PointList，vMinXNode, minXP1Ndoe,求minXP1Ndoe到V1PointList的距离，和vMinXNode到P1PointList的距离
            //P->V       
            double minD = double.PositiveInfinity;
            int m = polyTrasedPointList.Count;
            double x0 = 0;
            double y0 = 0;
            for (int p = 0; p < m; p++)
            {
                x0 = polyTrasedPointList[p].X;
                y0 = polyTrasedPointList[p].Y;
                double dis = CalDistance(x0, y0, vPolyTrasedPointList);
                if (dis < minD)
                    minD = dis;
            }


            //V->P
            int n = vPolyTrasedPointList.Count;
            for (int k = 1; k < n - 1; k++)
            {
                //double x00 = V1PointList[k - 1].X;
                //double y00 = V1PointList[k - 1].Y;
                //double x11 = V1PointList[k].X;
                //double y11 = V1PointList[k].Y;
                //double x22 = V1PointList[k + 1].X;
                //double y22 = V1PointList[k + 1].Y;

                x0 = vPolyTrasedPointList[k].X;
                y0 = vPolyTrasedPointList[k].Y;
                double dis = CalDistance(x0, y0, polyTrasedPointList);
                if (dis < minD)
                    minD = dis;

            }
            #endregion

            double olength = Math.Sqrt(dx * dx + dy * dy);
            if (minD < olength)
            {
                double s = minD / olength;
                odx = dx * s - e;
                ody = dy * s - e;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="pointList"></param>
        /// <returns></returns>
        private double CalDistance(double x, double y, List<TriNode> pointList)
        {
            double minDis = double.PositiveInfinity;
            int  n = pointList.Count;
            for (int i = 0; i < n - 1; i++)
            {
                double y1 = pointList[i].Y;
                double y2 = pointList[i + 1].Y;
                double x1 = pointList[i].X;
                double x2 = pointList[i + 1].X;
                double x3 = 0;
                double y3 = 0;

                //让y1>y2
                if (y1 > y2)
                {
                    y1 = pointList[i + 1].Y;
                    y2 = pointList[i].Y;
                    x1 = pointList[i + 1].X;
                    x2 = pointList[i].X;
                }

                if (y>= y1 && y <= y2)
                {
                    if (x1 == x2)
                    {
                        x3 = x1;
                        y3 = y;
                    }
                    else
                    {
                        double kk = (y1 - y2) / (x1 - x2);
                        x3 = (y - y1) / kk + x1;
                        y3 = y;
                    }
                    double len = Math.Sqrt((x - x3) * (x - x3) + (y - y3) * (y - y3));
                    if (len < minDis)
                        minDis = len;
                }
            }
            return minDis;
        }

        /// <summary>
        /// 判断移位后
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool IsPolygonObjectInPolygon(double x, double y)
        {
     
            MapObject MO = this.MapObj;
            PolygonObject O =null;
            //这里只考虑面对新，暂时忽略点和弦
            if (this.MapObj.FeatureType == FeatureType.PolygonType)
            {
                O = MO as PolygonObject;
            }
            else
                return false;

            foreach (TriNode p in O.PointList)
            {
                TriNode ShfitP = new TriNode(p.X + x, p.Y + y);

                if (!ComFunLib.IsPointinPolygon(ShfitP, this.PointSet))
                {
                    return false;
                }
            }
            //判断线段是否相交，若相交即使点均在多边形内部也说明多边形不在V图之内
            int n = O.PointList.Count;
            int m = this.PointSet.Count;
            for (int i = 0; i < n; i++)
            {
                TriNode p1=new TriNode(O.PointList[i].X+x,O.PointList[i].Y+y);
                TriNode p2=new TriNode(O.PointList[(i + 1)%n].X+x,O.PointList[(i + 1)%n].Y+y);
                for (int j = 0; j < m; j++)
                {
                    if (ComFunLib.IsLineSegCross(p1,p2, this.PointSet[j], this.PointSet[(j + 1)%m]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 判断移位后
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool IsPolygonObjectInPolygon(double x, double y,PolygonObject oo)
        {
            foreach (TriNode p in oo.PointList)
            {
                TriNode ShfitP = new TriNode(p.X + x, p.Y + y);

                if (!ComFunLib.IsPointinPolygon(ShfitP, this.PointSet))
                {
                    return false;
                }
            }
            //判断线段是否相交，若相交即使点均在多边形内部也说明多边形不在V图之内
            int n = oo.PointList.Count;
            int m = this.PointSet.Count;
            for (int i = 0; i < n; i++)
            {
                TriNode p1 = new TriNode(oo.PointList[i].X + x, oo.PointList[i].Y + y);
                TriNode p2 = new TriNode(oo.PointList[(i + 1) % n].X + x, oo.PointList[(i + 1) % n].Y + y);
                for (int j = 0; j < m; j++)
                {
                    if (ComFunLib.IsLineSegCross(p1, p2, this.PointSet[j], this.PointSet[(j + 1) % m]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
