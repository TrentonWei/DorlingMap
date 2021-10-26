using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuxStructureLib.CG
{
    /// <summary>
    /// 用旋转卡壳算法求两个凸多边形之间的最小距离，及其对应线段
    /// </summary>
    public class NearestDisBtwConvexNull
    {
        public List<TriNode> ConvexNull1 = null;
        public List<TriNode> ConvexNull2 = null;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="p"></param>
        /// <param name="q"></param>
        public NearestDisBtwConvexNull(List<TriNode> p, List<TriNode> q)
        {
            ConvexNull1=p;
            ConvexNull2 = q;
        }
        /// <summary>
        /// 求最近距离
        /// </summary>
        /// <returns></returns>
        public double CalNearestDistance()
        {
            return this.NearestPoints(this.ConvexNull1, this.ConvexNull2);
        }

        /// <summary>
        /// 求最近距离
        /// </summary>
        /// <returns></returns>
        public NearestEdge CalNearestEdge()
        {
            return this.NearestPoints1(this.ConvexNull1, this.ConvexNull2);
        }

        double EPS = 1e-8;
        /// <summary>
        ///求两点距离
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private double Pdis(TriNode a, TriNode b)
        {
            return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        } 

        /// <summary>
        /// 矢量叉积
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        /// 
        private double Difcross(TriNode p0, TriNode p1, TriNode p2)
        {
            return (p1.X - p0.X) * (p2.Y - p0.Y) - (p2.X - p0.X) * (p1.Y - p0.Y);
        }

        /// <summary>
        /// 矢量点积
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        private double Dotcross(TriNode p0, TriNode p1, TriNode p2)
        {
            return (p1.X - p0.X) * (p2.X - p0.X) + (p1.Y - p0.Y) * (p2.Y - p0.Y);
        }
        /// <summary>
        /// 点到线段的距离
        /// </summary>
        /// <param name="a"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        private double P2segline(TriNode a, TriNode p1, TriNode p2)
        {
            return Math.Abs(Difcross(a, p1, p2)) / Pdis(p1, p2);
        }

        /// <summary>
        /// 点到线段的距离
        /// </summary>
        /// <param name="a"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        private NearestEdge P2segline1(TriNode a, TriNode p1, TriNode p2)
        {
            Node n= ComChuizu(p1, p2, a);
            double d= Math.Abs(Difcross(a, p1, p2)) / Pdis(p1, p2);
            NearestEdge nEdge = new NearestEdge(-1, new NearestPoint(-1, a.X, a.Y), new NearestPoint(-1, n.X, n.Y), d);
            return nEdge;
        }


        /// <summary>
        /// 求垂足
        /// </summary>
        /// <param name="s">起点</param>
        /// <param name="e">终点</param>
        /// <param name="p">线段外的点</param>
        /// <returns>垂足</returns>
        private TriNode ComChuizu(TriNode p1, TriNode p2, TriNode a)
        {
            double x = 0, y = 0;
            if ((p2.X - p1.X) == 0 && (p2.Y - p1.Y) != 0)//平行于y轴
            {
                x = p2.X;
                y = a.Y;
            }
            else if ((p2.Y - p1.Y) == 0 && (p2.X - p1.X) != 0)//平行于X轴
            {
                x = a.X;
                y = p2.Y;
            }
            else if ((p2.Y - p1.Y) == 0 && (p2.X - p1.X) == 0)
            {
                x = p2.X;
                y = p2.Y;
            }
            else if ((p2.Y - p1.Y) != 0 && (p2.X - p1.X) != 0)
            {
                double k = (p2.Y - p1.Y) / (p2.X - p1.X);
                double k1 = -1 / k;
                x = (k * p1.X + k1 * a.X + a.Y - p1.Y) / (k + k1);
                y = (k * k1 * (p1.X - a.X) + k * a.Y - k1 * p1.Y) / (k - k1);
            }
            return new TriNode(x, y);
        }
        /// <summary>
        /// 求点到线段的最小距离
        /// </summary>
        /// <param name="a"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        private double P2seg(TriNode a, TriNode p1, TriNode p2)
        {
            if (Dotcross(p1, p2, a) < (-1) * EPS) return Pdis(a, p1);
            if (Dotcross(p2, p1, a) < (-1) * EPS) return Pdis(a, p2);
            double d = P2segline(a, p1, p2);
            return d;
        }

        /// <summary>
        /// 求点到线段的最小距离
        /// </summary>
        /// <param name="a"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        private NearestEdge P2seg1(TriNode a, TriNode p1, TriNode p2)
        {
            if (Dotcross(p1, p2, a) < (-1) * EPS)
            {
                NearestEdge nEdge = new NearestEdge(-1, new NearestPoint(-1, a.X, a.Y), new NearestPoint(-1, p1.X, p1.Y), Pdis(a, p1));
                return nEdge;
            }
            if (Dotcross(p2, p1, a) < (-1) * EPS)
            {

                NearestEdge nEdge = new NearestEdge(-1, new NearestPoint(-1, a.X, a.Y), new NearestPoint(-1, p2.X, p2.Y), Pdis(a, p2));
                return nEdge;
            }
            return P2segline1(a, p1, p2);
        }
        /// <summary>
        /// 当两条边平行时，出现四个点
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        private double FourPoints(TriNode p1, TriNode p2, TriNode q1, TriNode q2)
        {
            double ans1 = Math.Min(P2seg(p1, q1, q2), P2seg(p2, q1, q2));
            double ans2 = Math.Min(P2seg(q1, p1, p2), P2seg(q2, p1, p2));
            return Math.Min(ans1, ans2);
        }

        /// <summary>
        /// 当两条边平行时，出现四个点
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        private NearestEdge FourPoints1(TriNode p1, TriNode p2, TriNode q1, TriNode q2)
        {
            NearestEdge nedge1 = P2seg1(p1, q1, q2);
            NearestEdge nedge2 = P2seg1(p2, q1, q2);
            NearestEdge nedge3 = P2seg1(q1, p1, p2);
            NearestEdge nedge4 = P2seg1(q2, p1, p2);

            if (nedge1.NearestDistance > nedge2.NearestDistance)
                nedge1 = nedge2;
            if (nedge3.NearestDistance > nedge4.NearestDistance)
                nedge3 = nedge4;
            if(nedge1.NearestDistance>nedge3.NearestDistance)
                return nedge3;
            return nedge1;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        private double solve(List<TriNode> p, List<TriNode> q)
        {
            int np = p.Count;
            int nq = q.Count;

            p.Add(p[0]);
            q.Add(q[0]);
            int sp = 0, sq = 0;
            for (int i = 0; i < np; ++i)
            {
                if (p[i].Y + EPS < p[sp].Y) sp = i;
            }
            for (int i = 0; i < nq; ++i)
            {
                if (q[i].Y + EPS < q[sq].Y) sq = i;
            }
            double tmp, ans = 1e99;
            for (int i = 0; i < np; ++i)
            {
                while ((tmp = Difcross(q[sq], p[sp], p[sp + 1]) -
                       Difcross(q[sq + 1], p[sp], p[sp + 1])) < -EPS)
                    sq = (sq + 1) % nq;
                if (tmp > EPS)
                {
                    ans = Math.Min(ans, P2seg(q[sq], p[sp], p[sp + 1]));
                }
                else ans = Math.Min(ans, FourPoints(p[sp], p[sp + 1], q[sq], q[sq + 1]));
                sp = (sp + 1) % np;
            }

            p.RemoveAt(p.Count-1);
            q.RemoveAt(q.Count - 1);
            return ans;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        private NearestEdge solve1(List<TriNode> p, List<TriNode> q)
        {
            int np = p.Count;
            int nq = q.Count;

            p.Add(p[0]);
            q.Add(q[0]);
            int sp = 0, sq = 0;
            for (int i = 0; i < np; ++i)
            {
                if (p[i].Y + EPS < p[sp].Y) sp = i;
            }
            for (int i = 0; i < nq; ++i)
            {
                if (q[i].Y + EPS < q[sq].Y) sq = i;
            }
            double tmp;
            double ans = 1e99;
            NearestEdge minEdge = null;
            NearestEdge curEdge = null;
            for (int i = 0; i < np; ++i)
            {
                while ((tmp = Difcross(q[sq], p[sp], p[sp + 1]) -
                       Difcross(q[sq + 1], p[sp], p[sp + 1])) < -EPS)
                    sq = (sq + 1) % nq;
                if (tmp > EPS)
                {
                    curEdge = P2seg1(q[sq], p[sp], p[sp + 1]);
                    if (ans > curEdge.NearestDistance)
                    {
                        minEdge = curEdge;
                        ans = curEdge.NearestDistance;
                    }
                }
                else
                {
                    curEdge = FourPoints1(p[sp], p[sp + 1], q[sq], q[sq + 1]);
                    if (ans > curEdge.NearestDistance)
                    {
                        minEdge = curEdge;
                        ans = curEdge.NearestDistance;
                    }
                }
                sp = (sp + 1) % np;
            }
            p.RemoveAt(p.Count - 1);
            q.RemoveAt(q.Count - 1);
            return minEdge;
        }

        /// <summary>
        /// 求最近距离
        /// </summary>
        /// <param name="p"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        private double NearestPoints(List<TriNode> p, List<TriNode> q)
        {
            return Math.Min(solve(p, q), solve(q, p));
        }

        /// <summary>
        /// 求最近距离
        /// </summary>
        /// <param name="p"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        private NearestEdge NearestPoints1(List<TriNode> p, List<TriNode> q)
        {
            NearestEdge e1 = solve1(p, q);
            NearestEdge e2 = solve1(q, p);
            if (e1.NearestDistance < e2.NearestDistance)
                return e1;
            else
                return e2;
        } 
    }
}
