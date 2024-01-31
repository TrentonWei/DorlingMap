using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;
using MatrixOperation;
using System.Data;
using AuxStructureLib.IO;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using AuxStructureLib.ConflictLib;

namespace AlgEMLib
{
    /// <summary>
    /// 受力向量基类
    /// </summary>
    public class ForceVectorBase
    {
        /// <summary>
        /// 外力列表字段
        /// </summary>
        protected List<Force> forceList = null;
        /// <summary>
        ///  外力列表属性
        /// </summary>
        public List<Force> ForceList
        {
            get { return forceList; }
            set { this.forceList = value; }

        }

        /// <summary>
        /// 邻近图字段
        /// </summary>
        protected ProxiGraph proxiGraph = null;
        /// <summary>
        /// 邻近图属性
        /// </summary>
        public ProxiGraph ProxiGraph
        {
            get { return proxiGraph; }
            set { this.proxiGraph = value; }
        }

        /// <summary>
        /// 原始邻近图字段
        /// </summary>
        protected ProxiGraph origialProxiGraph = null;
        /// <summary>
        /// 原始邻近图属性
        /// </summary>
        public ProxiGraph OrigialProxiGraph
        {
            get { return origialProxiGraph; }
            set { this.origialProxiGraph = value; }
        }

        /// <summary>
        /// 是否开启吸引力模式字段
        /// </summary>
        protected bool isDragForce = false;
        /// <summary>
        /// 是否开启吸引力模式属性
        /// </summary>
        public bool IsDragForce
        {
            get { return isDragForce; }
            set { this.isDragForce = value; }
        }

        /// <summary>
        /// 地图精度限制（mm）字段
        /// </summary>
        protected double rmse = 0.5;
        /// <summary>
        /// 地图精度限制（mm）属性
        /// </summary>
        public double RMSE
        {
            get { return rmse; }
            set { this.rmse = value; }
        }

        /// <summary>
        /// 地图对象字段
        /// </summary>
        protected SMap map = null;
        /// <summary>
        /// 地图对象属性
        /// </summary>
        public SMap Map
        {
            get { return map; }
            set { this.map = value; }
        }

        #region 通用
        /// <summary>
        /// z判断是否还有冲突
        /// </summary>
        /// <param name="forceList">受力列表</param>
        /// <returns>bool结果</returns>
        protected bool IsHasForce(List<Force> forceList)
        {
            foreach (Force curF in forceList)
            {
                if (curF.F > 0.0001)//判断受力为0的条件
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 读取文件中的受力值
        /// </summary>
        /// <param name="forcefile"></param>
        /// <returns></returns>
        protected List<Force> ReadForceListfrmFile(string forcefile)
        {
            List<Force> forceList = new List<Force>();
            //读文件========
            DataTable dt = TestIO.ReadData(forcefile);
            foreach (DataRow curR in dt.Rows)
            {
                int curID = Convert.ToInt32(curR[0]);
                double curFx = Convert.ToDouble(curR[1]);
                double curFy = Convert.ToDouble(curR[2]);
                Force curForce = new Force(curID);
                curForce.Fx = curFx;
                curForce.Fy = curFy;
                curForce.F = Math.Sqrt(curFx * curFx + curFy * curFy);
                forceList.Add(curForce);
            }
            return forceList;
        }



        public Force GetForcebyIndex(int index)
        {
            foreach (Force curF in this.ForceList)
            {
                if (curF.ID == index)
                    return curF;
            }
            return null;
        }

        /// <summary>
        /// 通过索引号获取受力值
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        protected VertexForce GetvForcebyIndex(int index, List<VertexForce> vForceList)
        {
            foreach (VertexForce curvF in vForceList)
            {
                if (curvF.ID == index)
                    return curvF;
            }
            return null;
        }
        #endregion

        #region 道路网
        /// <summary>
        /// 根据冲突计算受力
        /// </summary>
        /// <param name="conflictList">冲突列表</param>
        /// <returns>受力列表</returns>
        public List<Force> CalForcefrmConflicts(List<ConflictBase> conflictList)
        {
            bool isp = true;
            Node nearestPoint = null;
            List<VertexForce> vForceList = null;
            vForceList = new List<VertexForce>();
            double d = -1;
            double lw = -1;
            double rw = -1;
            //计算所有的VertexForce并存入数组vForceList
            foreach (ConflictBase conflict in conflictList)
            {
                Conflict_L curConflict = conflict as Conflict_L;
                if (curConflict != null)
                {
                    //以地图符号的宽度作为受力的权值
                    double w1 = curConflict.Skel_arc.LeftMapObj.SylWidth;
                    double w2 = curConflict.Skel_arc.RightMapObj.SylWidth;
                    double w = w1 + w2;
                    lw = w2 / w; //左权值
                    rw = w1 / w; //右权值
                    d = curConflict.DisThreshold;
                    //左边
                    if (curConflict.LeftPointList != null && curConflict.LeftPointList.Count > 0)
                    {
                        foreach (TriNode point in curConflict.LeftPointList)
                        {
                            nearestPoint = AuxStructureLib.ComFunLib.MinDisPoint2Polyline(point, curConflict.Skel_arc.PointList, out isp);//isp是否垂直
                            double dis = AuxStructureLib.ComFunLib.CalLineLength(nearestPoint, point);
                            if (dis < 0.5 * d)
                            {
                                double f = lw * (d - 2 * dis);
                                double r = Math.Sqrt((nearestPoint.Y - point.Y) * (nearestPoint.Y - point.Y) + (nearestPoint.X - point.X) * (nearestPoint.X - point.X));
                                double s = (point.Y - nearestPoint.Y) / r;
                                double c = (point.X - nearestPoint.X) / r;
                                //这里将力平分给两个对象
                                double fx = f * c;
                                double fy = f * s;
                                int ID = point.ID;
                                Force force = new Force(ID, fx, fy, s, c, f);
                                VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);
                                if (vForce == null)
                                {
                                    vForce = new VertexForce(ID);
                                    vForceList.Add(vForce);

                                }
                                vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                            }
                        }
                    }
                    //右边
                    if (curConflict.RightPointList != null && curConflict.RightPointList.Count > 0)
                    {
                        foreach (TriNode point in curConflict.RightPointList)
                        {
                            nearestPoint = AuxStructureLib.ComFunLib.MinDisPoint2Polyline(point, curConflict.Skel_arc.PointList, out isp);

                            double dis = AuxStructureLib.ComFunLib.CalLineLength(nearestPoint, point);
                            if (dis < 0.5 * d)
                            {
                                double f = rw * (d - 2 * dis);
                                double r = Math.Sqrt((nearestPoint.Y - point.Y) * (nearestPoint.Y - point.Y) + (nearestPoint.X - point.X) * (nearestPoint.X - point.X));
                                double s = (point.Y - nearestPoint.Y) / r;
                                double c = (point.X - nearestPoint.X) / r;
                                //这里将力平分给两个对象
                                double fx = f * c;
                                double fy = f * s;
                                int ID = point.ID;
                                Force force = new Force(ID, fx, fy, s, c, f);
                                VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);
                                if (vForce == null)
                                {
                                    vForce = new VertexForce(ID);
                                    vForceList.Add(vForce);
                                }
                                vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                            }
                        }
                    }

                }
            }

            #region 求合力
            //先求最大力，以该力的方向为X轴方向建立局部坐标系，求四个主方向上的最大力，最后就合力
            List<Force> rforceList = new List<Force>();
            foreach (VertexForce vForce in vForceList)
            {
                if (vForce.forceList.Count == 1)//当只受一个力作用时
                {
                    rforceList.Add(vForce.forceList[0]);
                }
                else if (vForce.forceList.Count > 1)
                {
                    int index = 0;
                    double maxFx = 0;
                    double minFx = 0;
                    double maxFy = 0;
                    double minFy = 0;
                    Force maxF = GetMaxForce(out  index, vForce.forceList);
                    maxFx = maxF.F;
                    double s = maxF.Sin;
                    double c = maxF.Cos;

                    for (int i = 0; i < vForce.forceList.Count; i++)
                    {

                        if (i == index)
                        {
                            Force F = vForce.forceList[i];
                            double fx = F.Fx * c + F.Fy * s;
                            double fy = F.Fy * c - F.Fx * s;

                            if (minFx > fx) minFx = fx;
                            if (maxFy < fy) maxFy = fy;
                            if (minFy > fy) minFy = fy;
                        }
                    }
                    double FFx = maxFx + minFx;
                    double FFy = maxFy + minFy;
                    double Fx = FFx * c - FFy * s;
                    double Fy = FFx * s + FFy * c;
                    double f = Math.Sqrt(Fx * Fx + Fy * Fy);
                    Force rForce = new Force(vForce.ID, Fx, Fy, f);
                    rforceList.Add(rForce);
                }
            }

            #endregion
            return rforceList;
        }

        /// <summary>
        /// 根据冲突计算受力
        /// </summary>
        /// <param name="conflictList">冲突列表</param>
        /// <returns>受力列表</returns>
        public List<Force> CalInitDisVectorsfrmConflicts(List<ConflictBase> conflictList)
        {
            bool isp = true;
            Node nearestPoint = null;
            List<VertexForce> vForceList = null;
            vForceList = new List<VertexForce>();
            double d = -1;
            double lw = -1;
            double rw = -1;
            //计算所有的VertexForce并存入数组vForceList
            foreach (ConflictBase conflict in conflictList)
            {
                Conflict_L curConflict = conflict as Conflict_L;

                if (curConflict != null)
                {
                    //以地图符号的宽度作为受力的权值
                    double w1 = curConflict.Skel_arc.LeftMapObj.SylWidth;
                    double w2 = curConflict.Skel_arc.RightMapObj.SylWidth;
                    double w = w1 + w2;
                    lw = w2 / w; //左权值
                    rw = w1 / w; //右权值
                    d = curConflict.DisThreshold;

                    //瓶颈三角形
                    List<Triangle> triList = this.FindBottle_NeckTriangle(curConflict.TriangleList);
                    //三角形序列中包含的顶点
                    List<TriNode> pointList = new List<TriNode>();
                    foreach (Triangle t in triList)
                    {
                        if (!pointList.Contains(t.point1)) pointList.Add(t.point1);
                        if (!pointList.Contains(t.point2)) pointList.Add(t.point2);
                        if (!pointList.Contains(t.point3)) pointList.Add(t.point3);
                    }
                    List<TriNode> removeRange = null;
                    //左边
                    if (curConflict.LeftPointList != null && curConflict.LeftPointList.Count > 0)
                    {

                        removeRange = new List<TriNode>();

                        foreach (TriNode p in curConflict.LeftPointList)
                        {
                            if (!pointList.Contains(p))
                                removeRange.Add(p);

                        }
                        foreach (TriNode p in removeRange)
                        {
                            curConflict.LeftPointList.Remove(p);
                        }

                        foreach (TriNode point in curConflict.LeftPointList)
                        {
                            nearestPoint = AuxStructureLib.ComFunLib.MinDisPoint2Polyline(point, curConflict.Skel_arc.PointList, out isp);//isp是否垂直
                            double dis = AuxStructureLib.ComFunLib.CalLineLength(nearestPoint, point);
                            if (dis < 0.5 * d)
                            {
                                double f = lw * (d - 2 * dis);
                                double r = Math.Sqrt((nearestPoint.Y - point.Y) * (nearestPoint.Y - point.Y) + (nearestPoint.X - point.X) * (nearestPoint.X - point.X));
                                double s = (point.Y - nearestPoint.Y) / r;
                                double c = (point.X - nearestPoint.X) / r;
                                //这里将力平分给两个对象
                                double fx = f * c;
                                double fy = f * s;
                                int ID = point.ID;
                                Force force = new Force(ID, fx, fy, s, c, f);
                                VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);
                                if (vForce == null)
                                {
                                    vForce = new VertexForce(ID);
                                    vForceList.Add(vForce);

                                }
                                vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                            }
                        }
                    }
                    //右边
                    if (curConflict.RightPointList != null && curConflict.RightPointList.Count > 0)
                    {
                        //右边冲突点
                        removeRange = new List<TriNode>();
                        foreach (TriNode p in curConflict.RightPointList)
                        {
                            if (!pointList.Contains(p))
                                removeRange.Add(p);
                        }
                        foreach (TriNode p in removeRange)
                        {
                            curConflict.RightPointList.Remove(p);
                        }
                        foreach (TriNode point in curConflict.RightPointList)
                        {
                            nearestPoint = AuxStructureLib.ComFunLib.MinDisPoint2Polyline(point, curConflict.Skel_arc.PointList, out isp);

                            double dis = AuxStructureLib.ComFunLib.CalLineLength(nearestPoint, point);
                            if (dis < 0.5 * d)
                            {
                                double f = rw * (d - 2 * dis);
                                double r = Math.Sqrt((nearestPoint.Y - point.Y) * (nearestPoint.Y - point.Y) + (nearestPoint.X - point.X) * (nearestPoint.X - point.X));
                                double s = (point.Y - nearestPoint.Y) / r;
                                double c = (point.X - nearestPoint.X) / r;
                                //这里将力平分给两个对象
                                double fx = f * c;
                                double fy = f * s;
                                int ID = point.ID;
                                Force force = new Force(ID, fx, fy, s, c, f);
                                VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);
                                if (vForce == null)
                                {
                                    vForce = new VertexForce(ID);
                                    vForceList.Add(vForce);
                                }
                                vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                            }
                        }
                    }
                }
            }

            #region 求合力
            //先求最大力，以该力的方向为X轴方向建立局部坐标系，求四个主方向上的最大力，最后就合力
            List<Force> rforceList = new List<Force>();
            foreach (VertexForce vForce in vForceList)
            {
                if (vForce.forceList.Count == 1)//当只受一个力作用时
                {
                    rforceList.Add(vForce.forceList[0]);
                }
                else if (vForce.forceList.Count > 1)
                {
                    int index = 0;
                    double maxFx = 0;
                    double minFx = 0;
                    double maxFy = 0;
                    double minFy = 0;
                    Force maxF = GetMaxForce(out  index, vForce.forceList);
                    maxFx = maxF.F;
                    double s = maxF.Sin;
                    double c = maxF.Cos;

                    for (int i = 0; i < vForce.forceList.Count; i++)
                    {

                        if (i == index)
                        {
                            Force F = vForce.forceList[i];
                            double fx = F.Fx * c + F.Fy * s;
                            double fy = F.Fy * c - F.Fx * s;

                            if (minFx > fx) minFx = fx;
                            if (maxFy < fy) maxFy = fy;
                            if (minFy > fy) minFy = fy;
                        }
                    }
                    double FFx = maxFx + minFx;
                    double FFy = maxFy + minFy;
                    double Fx = FFx * c - FFy * s;
                    double Fy = FFx * s + FFy * c;
                    double f = Math.Sqrt(Fx * Fx + Fy * Fy);
                    Force rForce = new Force(vForce.ID, Fx, Fy, f);
                    rforceList.Add(rForce);
                }
            }

            #endregion
            return rforceList;
        }
        /// <summary>
        /// 根据冲突计算受力
        /// </summary>
        /// <param name="conflictList">冲突列表</param>
        /// <param name="isLog">是否采用对数衰减</param>
        /// <returns>受力列表</returns>
        public List<Force> CalInitDisVectorsfrmConflicts_LinearorLog(List<ConflictBase> conflictList, bool isLog)
        {
            bool isp = true;
            Node nearestPoint = null;
            List<VertexForce> vForceList = null;
            vForceList = new List<VertexForce>();
            double d = -1;
            double lw = -1;
            double rw = -1;
            //计算所有的VertexForce并存入数组vForceList
            foreach (ConflictBase conflict in conflictList)
            {
                Conflict_L curConflict = conflict as Conflict_L;

                if (curConflict != null)
                {
                    //以地图符号的宽度作为受力的权值
                    double w1 = curConflict.Skel_arc.LeftMapObj.SylWidth;
                    double w2 = curConflict.Skel_arc.RightMapObj.SylWidth;
                    double w = w1 + w2;
                    lw = w2 / w; //左权值
                    rw = w1 / w; //右权值
                    d = curConflict.DisThreshold;

                    //瓶颈三角形
                    List<Triangle> triList = this.FindBottle_NeckTriangle(curConflict.TriangleList);
                    //三角形序列中包含的顶点
                    List<TriNode> pointList = new List<TriNode>();
                    foreach (Triangle t in triList)
                    {
                        if (!pointList.Contains(t.point1)) pointList.Add(t.point1);
                        if (!pointList.Contains(t.point2)) pointList.Add(t.point2);
                        if (!pointList.Contains(t.point3)) pointList.Add(t.point3);
                    }
                    List<TriNode> removeRange = null;

                    List<Force> forceList = new List<Force>();
                    double minDis = double.PositiveInfinity;

                    //左边
                    if (curConflict.LeftPointList != null && curConflict.LeftPointList.Count > 0)
                    {

                        removeRange = new List<TriNode>();

                        foreach (TriNode p in curConflict.LeftPointList)
                        {
                            if (!pointList.Contains(p))
                                removeRange.Add(p);

                        }
                        foreach (TriNode p in removeRange)
                        {
                            curConflict.LeftPointList.Remove(p);
                        }

                        foreach (TriNode point in curConflict.LeftPointList)
                        {
                            nearestPoint = AuxStructureLib.ComFunLib.MinDisPoint2Polyline(point, (curConflict.Skel_arc.RightMapObj as PolylineObject).PointList, out isp);//isp是否垂直
                            double distance = AuxStructureLib.ComFunLib.CalLineLength(nearestPoint, point);
                            if (distance < d)
                            {
                                if (minDis > distance && distance > 0)
                                {
                                    minDis = distance;
                                }
                                // double f = lw * (d - distance); 
                                double r = Math.Sqrt((nearestPoint.Y - point.Y) * (nearestPoint.Y - point.Y) + (nearestPoint.X - point.X) * (nearestPoint.X - point.X));
                                double s = (point.Y - nearestPoint.Y) / r;
                                double c = (point.X - nearestPoint.X) / r;
                                //这里将力平分给两个对象
                                //double fx = f * c;
                                //double fy = f * s;
                                int ID = point.ID;
                                Force force = new Force(ID, 0, 0, s, c, 0);
                                force.distance = distance;
                                force.w = lw;
                                forceList.Add(force);
                                //VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);
                                //if (vForce == null)
                                //{
                                //    vForce = new VertexForce(ID);
                                //    vForceList.Add(vForce);

                                //}
                                //vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                            }
                        }
                    }
                    //右边
                    if (curConflict.RightPointList != null && curConflict.RightPointList.Count > 0)
                    {
                        //右边冲突点
                        removeRange = new List<TriNode>();
                        foreach (TriNode p in curConflict.RightPointList)
                        {
                            if (!pointList.Contains(p))
                                removeRange.Add(p);
                        }
                        foreach (TriNode p in removeRange)
                        {
                            curConflict.RightPointList.Remove(p);
                        }
                        foreach (TriNode point in curConflict.RightPointList)
                        {
                            nearestPoint = AuxStructureLib.ComFunLib.MinDisPoint2Polyline(point, (curConflict.Skel_arc.LeftMapObj as PolylineObject).PointList, out isp);

                            double distance = AuxStructureLib.ComFunLib.CalLineLength(nearestPoint, point);
                            if (distance < d)
                            {
                                if (minDis > distance && distance > 0)
                                {
                                    minDis = distance;
                                }
                                //double f = rw * (d - distance);
                                double r = Math.Sqrt((nearestPoint.Y - point.Y) * (nearestPoint.Y - point.Y) + (nearestPoint.X - point.X) * (nearestPoint.X - point.X));
                                double s = (point.Y - nearestPoint.Y) / r;
                                double c = (point.X - nearestPoint.X) / r;
                                //这里将力平分给两个对象
                                //double fx = f * c;
                                //double fy = f * s;
                                int ID = point.ID;
                                Force force = new Force(ID, 0, 0, s, c, 0);
                                force.distance = distance;
                                force.w = rw;
                                forceList.Add(force);
                                //VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);
                                //if (vForce == null)
                                //{
                                //    vForce = new VertexForce(ID);
                                //    vForceList.Add(vForce);
                                //}
                                //vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                            }
                        }
                    }

                    foreach (Force curForce in forceList)
                    {
                        if (isLog == true)
                        {
                            curForce.F = curForce.w * ((Math.Log((curForce.distance / minDis)) + 1) * d - curForce.distance);//线性比例函数取对数
                        }
                        else
                        {
                            curForce.F = curForce.w * ((curForce.distance / minDis) * d - curForce.distance);//线性比例函数
                        }
                        curForce.Fx = curForce.F * curForce.Cos;
                        curForce.Fy = curForce.F * curForce.Sin;
                        VertexForce vForce = this.GetvForcebyIndex(curForce.ID, vForceList);
                        if (vForce == null)
                        {
                            vForce = new VertexForce(curForce.ID);
                            vForceList.Add(vForce);
                        }
                        vForce.forceList.Add(curForce);//将当前的受力加入VertexForce数组
                    }
                }
            }

            #region 求合力
            //先求最大力，以该力的方向为X轴方向建立局部坐标系，求四个主方向上的最大力，最后就合力
            List<Force> rforceList = new List<Force>();
            foreach (VertexForce vForce in vForceList)
            {
                if (vForce.forceList.Count == 1)//当只受一个力作用时
                {
                    rforceList.Add(vForce.forceList[0]);
                }
                else if (vForce.forceList.Count > 1)
                {
                    int index = 0;
                    double maxFx = 0;
                    double minFx = 0;
                    double maxFy = 0;
                    double minFy = 0;
                    Force maxF = GetMaxForce(out  index, vForce.forceList);
                    maxFx = maxF.F;
                    double s = maxF.Sin;
                    double c = maxF.Cos;

                    for (int i = 0; i < vForce.forceList.Count; i++)
                    {

                        if (i == index)
                        {
                            Force F = vForce.forceList[i];
                            double fx = F.Fx * c + F.Fy * s;
                            double fy = F.Fy * c - F.Fx * s;

                            if (minFx > fx) minFx = fx;
                            if (maxFy < fy) maxFy = fy;
                            if (minFy > fy) minFy = fy;
                        }
                    }
                    double FFx = maxFx + minFx;
                    double FFy = maxFy + minFy;
                    double Fx = FFx * c - FFy * s;
                    double Fy = FFx * s + FFy * c;
                    double f = Math.Sqrt(Fx * Fx + Fy * Fy);
                    Force rForce = new Force(vForce.ID, Fx, Fy, f);
                    rforceList.Add(rForce);
                }
            }

            #endregion
            return rforceList;
        }
        /// <summary>
        /// 找到瓶颈三角形
        /// </summary>
        /// <param name="TriList">三角形列表</param>
        /// <returns>返回一个新的三角形列表</returns>
        public List<Triangle> FindBottle_NeckTriangle(List<Triangle> TriList)
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
        /// 通过索引号获取受力值
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        protected Force GetMaxForce(out int index, List<Force> ForceList)
        {
            double MaxF = -1;
            index = 0;
            for (int i = 0; i < ForceList.Count; i++)
            {
                if (ForceList[i].F > MaxF)
                {
                    index = i;
                    MaxF = ForceList[i].F;
                }
            }
            return ForceList[index];
        }

        #endregion

        #region 邻近图
        /// <summary>
        /// 初始化受力向量
        /// </summary>
        /// <param name="proxiGraph">邻近图</param>
        protected void InitForceListfrmGraph(ProxiGraph proxiGraph)
        {
            Force curForce = null;
            foreach (ProxiNode curNode in proxiGraph.NodeList)
            {
                curForce = new Force(curNode.ID);
                if (curNode.FeatureType == FeatureType.PolylineType)
                {
                    curForce.IsBouldPoint = true;
                }
                this.ForceList.Add(curForce);
            }
        }
        /// <summary>
        /// 计算邻近图上个点的最终受力-最大力做主方向的局部最大值法
        /// </summary>
        /// <returns></returns>
        protected List<Force> CalForceforProxiGraph(List<ConflictBase> conflictList)
        {
            #region 计算每个点的各受力分量
            List<VertexForce> vForceList = new List<VertexForce>();
            foreach (ConflictBase conflict in conflictList)
            {
                Conflict_R curConflict = conflict as Conflict_R;
                double d = curConflict.DisThreshold;
                if (curConflict.Type == "RR")
                {
                    #region 暂时仅考虑面与面
                    PolygonObject leftObject = curConflict.Skel_arc.LeftMapObj as PolygonObject;
                    PolygonObject RightObject = curConflict.Skel_arc.RightMapObj as PolygonObject;
                    double larea = leftObject.Area;
                    double rarea = RightObject.Area;
                    double area = larea + rarea;
                    double lw = rarea / area;
                    double rw = larea / area;
                    //左边受力
                    double f = lw * (d - curConflict.Distance);
                    double r = Math.Sqrt((curConflict.LeftPoint.Y - curConflict.RightPoint.Y) * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) + (curConflict.LeftPoint.X - curConflict.RightPoint.X) * (curConflict.LeftPoint.X - curConflict.RightPoint.X));
                    double s = (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) / r;
                    double c = (curConflict.LeftPoint.X - curConflict.RightPoint.X) / r;
                    //这里将力平分给两个对象
                    double fx = f * c;
                    double fy = f * s;
                    int ID = curConflict.LeftPoint.ID;
                    Force force = new Force(ID, fx, fy, s, c, f);
                    VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);
                    if (vForce == null)
                    {
                        vForce = new VertexForce(ID);
                        vForceList.Add(vForce);
                    }
                    vForce.forceList.Add(force);//将当前的受力加入VertexForce数组

                    //右边
                    f = rw * (d - curConflict.Distance);
                    s = -1 * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) / r;
                    c = -1 * (curConflict.LeftPoint.X - curConflict.RightPoint.X) / r;
                    //这里将力平分给两个对象
                    fx = f * c;
                    fy = f * s;
                    ID = curConflict.RightPoint.ID;
                    force = new Force(ID, fx, fy, s, c, f);
                    vForce = this.GetvForcebyIndex(ID, vForceList);
                    if (vForce == null)
                    {
                        vForce = new VertexForce(ID);
                        vForceList.Add(vForce);
                    }
                    vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                    #endregion
                }
                else if (curConflict.Type == "RL")
                {
                    if (curConflict.Skel_arc.LeftMapObj.FeatureType == FeatureType.PolylineType)//线在左边
                    {
                        double f = d - curConflict.Distance;
                        double r = Math.Sqrt((curConflict.LeftPoint.Y - curConflict.RightPoint.Y) * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) + (curConflict.LeftPoint.X - curConflict.RightPoint.X) * (curConflict.LeftPoint.X - curConflict.RightPoint.X));
                        double s = -1 * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) / r;
                        double c = -1 * (curConflict.LeftPoint.X - curConflict.RightPoint.X) / r;
                        //这里将力平分给两个对象
                        double fx = f * c;
                        double fy = f * s;
                        int ID = curConflict.RightPoint.ID;
                        Force force = new Force(ID, fx, fy, s, c, f);
                        VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);

                        if (vForce == null)
                        {
                            vForce = new VertexForce(ID);
                            vForceList.Add(vForce);
                        }
                        vForce.forceList.Add(force);//将当前的受力加入VertexForce数组


                        //线上的边界点
                        f = 0;
                        s = 0;
                        c = 0;
                        //这里将力平分给两个对象
                        fx = 0;
                        fy = 0;
                        ID = curConflict.LeftPoint.ID;
                        force = new Force(ID, fx, fy, f);
                        force.IsBouldPoint = true;
                        vForce = this.GetvForcebyIndex(ID, vForceList);
                        if (vForce == null)
                        {
                            vForce = new VertexForce(ID);
                            vForceList.Add(vForce);
                        }
                        vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                    }
                    else if (curConflict.Skel_arc.RightMapObj.FeatureType == FeatureType.PolylineType)
                    {
                        double f = d - curConflict.Distance;
                        double r = Math.Sqrt((curConflict.LeftPoint.Y - curConflict.RightPoint.Y) * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) + (curConflict.LeftPoint.X - curConflict.RightPoint.X) * (curConflict.LeftPoint.X - curConflict.RightPoint.X));
                        double s = (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) / r;
                        double c = (curConflict.LeftPoint.X - curConflict.RightPoint.X) / r;
                        //这里将力平分给两个对象
                        double fx = f * c;
                        double fy = f * s;
                        int ID = curConflict.LeftPoint.ID;
                        Force force = new Force(ID, fx, fy, s, c, f);
                        VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);
                        if (vForce == null)
                        {
                            vForce = new VertexForce(ID);
                            vForceList.Add(vForce);
                        }
                        vForce.forceList.Add(force);//将当前的受力加入VertexForce数组


                        //线上的边界点
                        f = 0;
                        s = 0;
                        c = 0;
                        //这里将力平分给两个对象
                        fx = 0;
                        fy = 0;
                        ID = curConflict.RightPoint.ID;
                        force = new Force(ID, fx, fy, f);
                        force.IsBouldPoint = true;
                        vForce = this.GetvForcebyIndex(ID, vForceList);
                        if (vForce == null)
                        {
                            vForce = new VertexForce(ID);
                            vForceList.Add(vForce);
                        }
                        vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                    }
                }
            }
            #endregion

            #region  求吸引力-2014-3-20
            if (this.isDragForce == true)
            {
                int n = this.ProxiGraph.NodeList.Count;
                for (int i = 0; i < n; i++)
                {

                    ProxiNode curNode = this.ProxiGraph.NodeList[i];

                    int id = curNode.ID;
                    //  int tagID = curNode.TagID;
                    FeatureType type = curNode.FeatureType;
                    ProxiNode originalNode = this.OrigialProxiGraph.GetNodebyID(id);
                    if (originalNode == null)
                    {
                        continue;
                    }
                    double distance = ComFunLib.CalLineLength(curNode, originalNode);
                    if (distance > RMSE && (type != FeatureType.PolylineType))
                    {
                        //右边
                        double f = distance - RMSE;
                        double s = (originalNode.Y - curNode.Y) / distance;
                        double c = (originalNode.X - curNode.X) / distance;
                        //这里将力平分给两个对象
                        double fx = f * c;
                        double fy = f * s;
                        Force force = new Force(id, fx, fy, s, c, f);
                        VertexForce vForce = this.GetvForcebyIndex(id, vForceList);
                        if (vForce == null)
                        {
                            vForce = new VertexForce(id);
                            vForceList.Add(vForce);
                        }
                        vForce.forceList.Add(force);
                    }
                }
                //foreach(Node cur Node )
            }
            #endregion

            #region 求合力
            //先求最大力，以该力的方向为X轴方向建立局部坐标系，求四个主方向上的最大力，最后就合力

            List<Force> rforceList = new List<Force>();
            foreach (VertexForce vForce in vForceList)
            {
                if (vForce.forceList.Count == 1)//当只受一个力作用时
                {
                    rforceList.Add(vForce.forceList[0]);

                }
                else if (vForce.forceList.Count > 1)
                {
                    int index = 0;
                    double maxFx = 0;
                    double minFx = 0;
                    double maxFy = 0;
                    double minFy = 0;
                    Force maxF = GetMaxForce(out  index, vForce.forceList);
                    maxFx = maxF.F;
                    double s = maxF.Sin;
                    double c = maxF.Cos;

                    for (int i = 0; i < vForce.forceList.Count; i++)
                    {

                        if (i != index)
                        {
                            Force F = vForce.forceList[i];
                            double fx = F.Fx * c + F.Fy * s;
                            double fy = F.Fy * c - F.Fx * s;

                            if (minFx > fx) minFx = fx;
                            if (maxFy < fy) maxFy = fy;
                            if (minFy > fy) minFy = fy;
                        }
                    }
                    double FFx = maxFx + minFx;
                    double FFy = maxFy + minFy;
                    double Fx = FFx * c - FFy * s;
                    double Fy = FFx * s + FFy * c;
                    double f = Math.Sqrt(Fx * Fx + Fy * Fy);
                    Force rForce = new Force(vForce.ID, Fx, Fy, f);
                    rforceList.Add(rForce);
                }
            }

            #endregion
            return rforceList;
        }

       /// <summary>
        ///  /// <summary>
        /// 计算邻近图上个点的最终受力-最大力做主方向的局部最大值法
        /// </summary>
        /// <returns></returns>
       /// </summary>
       /// <param name="NodeList"></param>
       /// <param name="FinalLocation"></param>
       /// <param name="MinDis"></param>
       /// <param name="MaxForce">用于限制计算两个力时的最大力</param>
       /// <param name="MaxForce_2">用于限制全力力时的最大力</param>
       /// <returns></returns>
        protected List<Force> CalForceforProxiGraph_CTP(List<ProxiNode> NodeList, List<ProxiNode> FinalLocation, double MinDis,double MaxForce,double MaxForce_2)
        {
            #region 计算受力
            if (ProxiGraph == null || ProxiGraph.NodeList == null || ProxiGraph.EdgeList == null)
                return null;

            InitForceListfrmGraph(ProxiGraph);//初始化受力向量
            List<VertexForce> vForceList = new List<VertexForce>();

            foreach (ProxiNode sNode in NodeList)
            {
                if (sNode.FeatureType == FeatureType.PointType)
                {
                    ProxiNode eNode = this.GetPNodeByID(sNode.TagID, FinalLocation);//获得对应的FinalLocation中的Points（TagValue=ID）
                    int TestID = sNode.ID;

                    if (eNode != null) //只计算给定目的地的受力
                    {
                        List<Force> ForceList = this.GetForceCTP(sNode, eNode, MinDis,MaxForce);//获的起点到终点的力

                        if (ForceList.Count > 0)
                        {
                            #region 添加Force
                            VertexForce svForce = this.GetvForcebyIndex(sNode.ID, vForceList);//受力的标志还是用的ID来标识
                            if (svForce == null)
                            {
                                svForce = new VertexForce(sNode.ID);
                                vForceList.Add(svForce);
                            }
                            svForce.forceList.Add(ForceList[0]);//将当前的受力加入VertexForce数组
                            #endregion
                        }
                    }
                }
            }
            #endregion

            #region 计算合力
            List<Force> rforceList = new List<Force>();
            foreach (VertexForce vForce in vForceList)
            {
                if (vForce.forceList.Count == 1)//当只受一个力作用时
                {
                    rforceList.Add(vForce.forceList[0]);

                }
                else if (vForce.forceList.Count > 1)
                {
                    int index = 0;
                    double maxFx = 0;
                    double minFx = 0;
                    double maxFy = 0;
                    double minFy = 0;
                    Force maxF = GetMaxForce(out  index, vForce.forceList);
                    maxFx = maxF.F;
                    double s = maxF.Sin;
                    double c = maxF.Cos;

                    for (int i = 0; i < vForce.forceList.Count; i++)
                    {

                        if (i != index)
                        {
                            Force F = vForce.forceList[i];
                            double fx = F.Fx * c + F.Fy * s;
                            double fy = F.Fy * c - F.Fx * s;

                            if (minFx > fx) minFx = fx;
                            if (maxFy < fy) maxFy = fy;
                            if (minFy > fy) minFy = fy;
                        }
                    }
                    double FFx = maxFx + minFx;
                    double FFy = maxFy + minFy;
                    double Fx = FFx * c - FFy * s;
                    double Fy = FFx * s + FFy * c;
                    double f = Math.Sqrt(Fx * Fx + Fy * Fy);
                    Force rForce = new Force(vForce.ID, Fx, Fy, f);
                    rforceList.Add(rForce);
                }
            }
            #endregion

            #region 将力减小
            double Maxf = 0;
            for (int i = 0; i < rforceList.Count; i++)
            {
                if (Math.Abs(rforceList[i].F) > Maxf)
                {
                    Maxf = Math.Abs(rforceList[i].F);
                }
            }

            if (Maxf > MaxForce_2)
            {
                double Scale_p = MaxForce_2 / Maxf;

                for (int i = 0; i < rforceList.Count; i++)
                {
                    rforceList[i].F = rforceList[i].F * Scale_p;
                    rforceList[i].Fx = rforceList[i].Fx* Scale_p;
                    rforceList[i].Fy = rforceList[i].Fy * Scale_p;
                }
            }
            #endregion

            return rforceList;
        }



        /// <summary>
        ///  /// <summary>
        /// 计算邻近图上个点的最终受力-最大力做主方向的局部最大值法
        /// </summary>
        /// <returns></returns>
        /// </summary>
        /// <param name="NodeList"></param>
        /// <param name="MaxForce">用于限制计算两个力时的最大力</param>
        /// <param name="MaxForce_2">用于限制全力力时的最大力</param>
        /// Size TileMap的尺寸
        /// <returns></returns>
        protected List<Force> CalForceforProxiGraph_TileMap(List<ProxiNode> NodeList, List<ProxiEdge> EdgeList,double MaxForce, double MaxForce_2,double Size)
        {
            #region 计算受力
            if (ProxiGraph == null || ProxiGraph.NodeList == null || ProxiGraph.EdgeList == null)
                return null;

            InitForceListfrmGraph(ProxiGraph);//初始化受力向量
            List<VertexForce> vForceList = new List<VertexForce>();

            foreach (ProxiNode pNode in NodeList) //计算每一个中心点的受力
            {
                if (pNode.FeatureType == FeatureType.PointType)//如果点Pn是重心点
                {
                    Tuple<double, double> NodeXY = this.NodeShiftXY(EdgeList, pNode, Size);
                    double curForce = Math.Sqrt(NodeXY.Item1 * NodeXY.Item1 + NodeXY.Item2 * NodeXY.Item2);
                    Force sForce = new Force(pNode.ID, NodeXY.Item1, NodeXY.Item2, NodeXY.Item2 / curForce, NodeXY.Item1 / curForce, curForce);

                    #region 添加Force
                    VertexForce svForce = this.GetvForcebyIndex(pNode.ID, vForceList);//受力的标志还是用的ID来标识
                    if (svForce == null)
                    {
                        svForce = new VertexForce(pNode.ID);
                        vForceList.Add(svForce);
                    }
                    svForce.forceList.Add(sForce);//将当前的受力加入VertexForce数组
                    #endregion
                }
            }
            #endregion

            #region 计算合力
            List<Force> rforceList = new List<Force>();
            foreach (VertexForce vForce in vForceList)
            {
                if (vForce.forceList.Count == 1)//当只受一个力作用时
                {
                    rforceList.Add(vForce.forceList[0]);

                }
                else if (vForce.forceList.Count > 1)
                {
                    int index = 0;
                    double maxFx = 0;
                    double minFx = 0;
                    double maxFy = 0;
                    double minFy = 0;
                    Force maxF = GetMaxForce(out  index, vForce.forceList);
                    maxFx = maxF.F;
                    double s = maxF.Sin;
                    double c = maxF.Cos;

                    for (int i = 0; i < vForce.forceList.Count; i++)
                    {

                        if (i != index)
                        {
                            Force F = vForce.forceList[i];
                            double fx = F.Fx * c + F.Fy * s;
                            double fy = F.Fy * c - F.Fx * s;

                            if (minFx > fx) minFx = fx;
                            if (maxFy < fy) maxFy = fy;
                            if (minFy > fy) minFy = fy;
                        }
                    }
                    double FFx = maxFx + minFx;
                    double FFy = maxFy + minFy;
                    double Fx = FFx * c - FFy * s;
                    double Fy = FFx * s + FFy * c;
                    double f = Math.Sqrt(Fx * Fx + Fy * Fy);
                    Force rForce = new Force(vForce.ID, Fx, Fy, f);
                    rforceList.Add(rForce);
                }
            }
            #endregion

            #region 将力减小
            double Maxf = 0;
            for (int i = 0; i < rforceList.Count; i++)
            {
                if (Math.Abs(rforceList[i].F) > Maxf)
                {
                    Maxf = Math.Abs(rforceList[i].F);
                }
            }

            if (Maxf > MaxForce_2)
            {
                double Scale_p = MaxForce_2 / Maxf;

                for (int i = 0; i < rforceList.Count; i++)
                {
                    rforceList[i].F = rforceList[i].F * Scale_p;
                    rforceList[i].Fx = rforceList[i].Fx * Scale_p;
                    rforceList[i].Fy = rforceList[i].Fy * Scale_p;
                }
            }
            #endregion

            return rforceList;
        }

        /// <summary>
        /// 获得给定节点的1阶邻近(且都是区域重心点)
        /// </summary>
        /// <param name="EdgeList"></param>
        /// <param name="Pn"></param>
        /// PointConnect判断是否是点连接
        /// <returns></returns>
        public List<ProxiNode> GetNeibors(List<ProxiEdge> EdgeList, ProxiNode Pn)
        {
            List<ProxiNode> NeiborNodes = new List<ProxiNode>();
            foreach (ProxiEdge Pe in EdgeList)
            {
                if (Pe.Node1.TagID == Pn.TagID && Pe.Node1.FeatureType == FeatureType.PointType)
                {
                    if (!NeiborNodes.Contains(Pe.Node2))
                    {
                        NeiborNodes.Add(Pe.Node2);
                    }
                }
                if (Pe.Node2.TagID == Pn.TagID && Pe.Node2.FeatureType == FeatureType.PointType)
                {
                    if (!NeiborNodes.Contains(Pe.Node1))
                    {
                        NeiborNodes.Add(Pe.Node1);
                    }
                }
            }

            return NeiborNodes;
        }

        /// <summary>
        /// 获得给定节点的1阶邻近(且都是区域重心点)
        /// </summary>
        /// <param name="EdgeList"></param>
        /// <param name="Pn"></param>
        /// PointConnect判断是否是点连接
        /// <returns></returns>
        public List<ProxiNode> GetNeibors2(List<ProxiEdge> EdgeList, ProxiNode Pn)
        {
            List<ProxiNode> NeiborNodes = new List<ProxiNode>();
            foreach (ProxiEdge Pe in EdgeList)
            {
                if (Pe.Node1.TagID == Pn.TagID && Pe.Node1.FeatureType == FeatureType.PointType)
                {
                    if (!NeiborNodes.Contains(Pe.Node2) && Pe.Node2.FeatureType==FeatureType.PointType)
                    {
                        NeiborNodes.Add(Pe.Node2);
                    }
                }
                if (Pe.Node2.TagID == Pn.TagID && Pe.Node2.FeatureType == FeatureType.PointType)
                {
                    if (!NeiborNodes.Contains(Pe.Node1) && Pe.Node1.FeatureType == FeatureType.PointType)
                    {
                        NeiborNodes.Add(Pe.Node1);
                    }
                }
            }

            return NeiborNodes;
        }

        /// <summary>
        /// 获得给定点偏移后的位置[x,y]
        /// </summary>
        /// <param name="EdgeList"></param>
        /// <param name="Pn"></param>
        /// <param name="BoundaryPo"></param>
        /// <param name="TileCount"></param>
        /// <returns></returns>
        public Tuple<double, double> NodeShiftXY(List<ProxiEdge> EdgeList, ProxiNode Pn, double Size)
        {
            #region MainProcess
            List<ProxiNode> NeiborNodes = this.GetNeibors2(EdgeList, Pn);//Get NeinorNodes

            double SumX = 0; double SumY = 0;
            for (int i = 0; i < NeiborNodes.Count; i++)
            {
                Tuple<double, double> NodePairVector = this.GetNodePairVector(Pn, NeiborNodes[i]);
                SumX = SumX + (NeiborNodes[i].X + Size * NodePairVector.Item1);
                SumY = SumY + (NeiborNodes[i].Y + Size * NodePairVector.Item2);
            }
            #endregion

            double NewX = SumX / NeiborNodes.Count;
            double NewY = SumY / NeiborNodes.Count;

            double ADDX = NewX - Pn.X;
            double ADDY = NewY - Pn.Y;
            Tuple<double, double> ShiftXY = new Tuple<double, double>(ADDX, ADDY);

            return ShiftXY;
        }

        /// <summary>
        /// Get unit displacement vector betweeo two nodes (Pn1 to Pn2)
        /// </summary>
        /// <param name="Pn1"></param>
        /// <param name="Pn2"></param>
        /// <returns></returns>
        public Tuple<double, double> GetNodePairVector(ProxiNode Pn1, ProxiNode Pn2)
        {
            double Dis = this.GetDis(Pn1, Pn2);
            double CosX = (Pn1.X - Pn2.X) / Dis;
            double SinY = (Pn1.Y - Pn2.Y) / Dis;
            Tuple<double, double> VectorXY = new Tuple<double, double>(CosX, SinY);
            return VectorXY;
        }


        /// <summary>
        /// 计算邻近图上个点的最终受力-最大力做主方向的局部最大值法
        /// 输出BoundingPoints（每次只取受力前十名的力进行处理！！）
        /// </summary>
        /// <returns></returns>
        protected List<Force> CalForceforProxiGraph_HierCTP(List<ProxiNode> NodeList, List<ProxiNode> FinalLocation, double MinDis,out List<int> BoundingPoint,double MaxForce,double ForceRate)
        {
            BoundingPoint = new List<int>();

            #region 计算受力
            if (ProxiGraph == null || ProxiGraph.NodeList == null || ProxiGraph.EdgeList == null)
                return null;

            InitForceListfrmGraph(ProxiGraph);//初始化受力向量
            List<VertexForce> vForceList = new List<VertexForce>();

            foreach (ProxiNode sNode in NodeList)
            {
                if (sNode.FeatureType == FeatureType.PointType)
                {
                    ProxiNode eNode = this.GetPNodeByID(sNode.TagID, FinalLocation);//获得对应的FinalLocation中的Points（TagValue=ID）
                    int TestID = sNode.ID;

                    if (eNode != null) //只计算给定目的地的受力
                    {
                        List<Force> ForceList = this.GetForceCTP(sNode, eNode, MinDis, MaxForce);//获的起点到终点的力

                        if (ForceList.Count > 0)
                        {
                            #region 添加Force
                            VertexForce svForce = this.GetvForcebyIndex(sNode.ID, vForceList);//受力的标志还是用的ID来标识
                            if (svForce == null)
                            {
                                svForce = new VertexForce(sNode.ID);
                                vForceList.Add(svForce);
                            }
                            svForce.forceList.Add(ForceList[0]);//将当前的受力加入VertexForce数组
                            #endregion
                        }
                    }
                }
            }
            #endregion

            #region 计算合力
            List<Force> rforceList = new List<Force>();
            foreach (VertexForce vForce in vForceList)
            {
                if (vForce.forceList.Count == 1)//当只受一个力作用时
                {
                    rforceList.Add(vForce.forceList[0]);

                }
                else if (vForce.forceList.Count > 1)
                {
                    int index = 0;
                    double maxFx = 0;
                    double minFx = 0;
                    double maxFy = 0;
                    double minFy = 0;
                    Force maxF = GetMaxForce(out  index, vForce.forceList);
                    maxFx = maxF.F;
                    double s = maxF.Sin;
                    double c = maxF.Cos;

                    for (int i = 0; i < vForce.forceList.Count; i++)
                    {

                        if (i != index)
                        {
                            Force F = vForce.forceList[i];
                            double fx = F.Fx * c + F.Fy * s;
                            double fy = F.Fy * c - F.Fx * s;

                            if (minFx > fx) minFx = fx;
                            if (maxFy < fy) maxFy = fy;
                            if (minFy > fy) minFy = fy;
                        }
                    }
                    double FFx = maxFx + minFx;
                    double FFy = maxFy + minFy;
                    double Fx = FFx * c - FFy * s;
                    double Fy = FFx * s + FFy * c;
                    double f = Math.Sqrt(Fx * Fx + Fy * Fy);
                    Force rForce = new Force(vForce.ID, Fx, Fy, f);
                    rforceList.Add(rForce);
                }
            }
            #endregion

            #region 计算受力最大的十名
            //List<Force> subrforceList = rforceList.GetRange(0, 10);

            //List<double> CacheFList = new List<double>();
            //for (int i = 0; i < subrforceList.Count; i++)
            //{
            //    CacheFList.Add(subrforceList[i].F);
            //}
            //double MinF = CacheFList.Min();

            //for (int i = 10; i < rforceList.Count; i++)
            //{
            //    if (rforceList[i].F > MinF)
            //    {
            //        subrforceList.RemoveAt(CacheFList.IndexOf(MinF));
            //        subrforceList.Add(rforceList[i]);

            //        CacheFList.Remove(MinF);
            //        CacheFList.Add(rforceList[i].F);
            //        MinF = CacheFList.Min();
            //    }
            //}
            #endregion

            #region 计算受力最大的十名
            int ForceCount =Convert.ToInt16(Math.Ceiling(FinalLocation.Count * ForceRate));
            List<Force> subrforceList = rforceList.GetRange(0, ForceCount);

            List<double> CacheFList = new List<double>();
            for (int i = 0; i < subrforceList.Count; i++)
            {
                CacheFList.Add(subrforceList[i].F);
            }
            double MinF = CacheFList.Min();

            for (int i = ForceCount; i < rforceList.Count; i++)
            {
                if (rforceList[i].F > MinF)
                {
                    subrforceList.RemoveAt(CacheFList.IndexOf(MinF));
                    subrforceList.Add(rforceList[i]);

                    CacheFList.Remove(MinF);
                    CacheFList.Add(rforceList[i].F);
                    MinF = CacheFList.Min();
                }
            }
            #endregion

            return subrforceList;
        }

        /// <summary>
        /// 计算起点到终点的力
        /// </summary>
        /// <param name="sNode">起点</param>
        /// <param name="eNode">终点</param>
        /// <param name="MinDis">最小距离</param>
        /// <returns></returns>
        public List<Force> GetForceCTP(ProxiNode sNode, ProxiNode eNode, double MinDis,double MaxForce)
        {
            //ProxiNode tNode1 = sPo1.CalProxiNode();
            //ProxiNode tNode2 = ePo2.CalProxiNode();

            double EdgeDis = this.GetDis(sNode, eNode);
            List<Force> ForceList = new List<Force>();
            List<VertexForce> vForceList = new List<VertexForce>();

            #region 判断控制点是否靠近其最终位置
            sNode.NearFinal = false;
            if (EdgeDis < MinDis)
            {
                sNode.NearFinal = true;
            }
            #endregion

            if (EdgeDis > 0)
            {
                double curForce = EdgeDis;
                if (curForce > MaxForce)
                {
                    curForce = MaxForce;

                    sNode.MaxForce = true;//表示该点已受到规定范围的最大力
                }

                double r = Math.Sqrt((eNode.Y - sNode.Y) * (eNode.Y - sNode.Y) + (eNode.X - sNode.X) * (eNode.X - sNode.X));
                double s = (eNode.Y - sNode.Y) / r;
                double c = (eNode.X - sNode.X) / r;

                double fx = curForce * c;
                double fy = curForce * s;
                Force sForce = new Force(sNode.ID, fx, fy, s, c, curForce);//力的ID就是Node的ID
                ForceList.Add(sForce);
            }

            return ForceList;
        }

        /// <summary>
        /// Distance between two trinode
        /// </summary>
        /// <param name="sNode"></param>
        /// <param name="eNode"></param>
        /// <returns></returns>
        public double GetDis(ProxiNode sNode, ProxiNode eNode)
        {
            double Dis = Math.Sqrt((sNode.X - eNode.X) * (sNode.X - eNode.X) + (sNode.Y - eNode.Y) * (sNode.Y - eNode.Y));
            return Dis;
        }

        /// <summary>
        /// GetPoByID
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        public ProxiNode GetPNodeByID(int ID, List<ProxiNode> NodeList)
        {
            ProxiNode eNode = null;
            foreach (ProxiNode CacheNode in NodeList)
            {
                if (CacheNode.TagID == ID)
                {
                    eNode = CacheNode;
                    break;
                }
            }

            return eNode;
        }

        /// <summary>
        /// 考虑分组
        /// </summary>
        /// <param name="conflictList">冲突</param>
        /// <param name="groups">分组</param>
        /// <returns></returns>
        protected List<Force> CalForceforProxiGraph_Group(List<ConflictBase> conflictList, List<GroupofMapObject> groups)
        {
            #region 计算每个点的各受力分量
            List<VertexForce> vForceList = new List<VertexForce>();
            foreach (ConflictBase conflict in conflictList)
            {
                Conflict_R curConflict = conflict as Conflict_R;
                double d = curConflict.DisThreshold;
                if (curConflict.Type == "RR")
                {
                    #region 暂时仅考虑面与面
                    PolygonObject leftObject = curConflict.Skel_arc.LeftMapObj as PolygonObject;
                    PolygonObject RightObject = curConflict.Skel_arc.RightMapObj as PolygonObject;

                    double larea = 0;
                    double rarea = 0;
                    double area = 0;
                    double lw = 0;
                    double rw = 0;

                    int leftTagID = -1;
                    FeatureType lefttype = FeatureType.Unknown;

                    int rightTagID = -1;
                    FeatureType righttype = FeatureType.Unknown;

                    GroupofMapObject leftGroup = GroupofMapObject.GetGroup(leftObject, groups);
                    GroupofMapObject rightGroup = GroupofMapObject.GetGroup(RightObject, groups);
                    if (leftGroup != null)
                    {
                        larea = leftGroup.Area;
                        leftTagID = leftGroup.ID;
                        lefttype = FeatureType.Group; ;
                    }
                    else
                    {
                        larea = leftObject.Area;
                        leftTagID = leftObject.ID;
                        lefttype = FeatureType.PolygonType; ;
                    }

                    if (rightGroup != null)
                    {
                        rarea = rightGroup.Area;
                        rightTagID = rightGroup.ID;
                        righttype = FeatureType.Group;
                    }
                    else
                    {
                        rarea = RightObject.Area;
                        rightTagID = RightObject.ID;
                        righttype = FeatureType.PolygonType; ;
                    }
                    area = larea + rarea;
                    lw = rarea / area;
                    rw = larea / area;

                    //左边受力
                    double f = lw * (d - curConflict.Distance);
                    double r = Math.Sqrt((curConflict.LeftPoint.Y - curConflict.RightPoint.Y) * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) + (curConflict.LeftPoint.X - curConflict.RightPoint.X) * (curConflict.LeftPoint.X - curConflict.RightPoint.X));
                    double s = (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) / r;
                    double c = (curConflict.LeftPoint.X - curConflict.RightPoint.X) / r;
                    //这里将力按面积大小的反比分给两个对象
                    double fx = f * c;
                    double fy = f * s;

                    int ID = ProxiGraph.GetNodebyTagIDandType(leftTagID, lefttype).ID;

                    Force force = new Force(ID, fx, fy, s, c, f);
                    VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);
                    if (vForce == null)
                    {
                        vForce = new VertexForce(ID);
                        vForceList.Add(vForce);
                    }
                    vForce.forceList.Add(force);//将当前的受力加入VertexForce数组


                    //右边
                    f = rw * (d - curConflict.Distance);
                    s = -1 * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) / r;
                    c = -1 * (curConflict.LeftPoint.X - curConflict.RightPoint.X) / r;
                    //这里将力平分给两个对象
                    fx = f * c;
                    fy = f * s;
                    ID = ProxiGraph.GetNodebyTagIDandType(rightTagID, righttype).ID;
                    force = new Force(ID, fx, fy, s, c, f);
                    vForce = this.GetvForcebyIndex(ID, vForceList);
                    if (vForce == null)
                    {
                        vForce = new VertexForce(ID);
                        vForceList.Add(vForce);
                    }
                    vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                    #endregion
                }
                else if (curConflict.Type == "RL")
                {
                    if (curConflict.Skel_arc.LeftMapObj.FeatureType == FeatureType.PolylineType)//线在左边
                    {
                        double f = d - curConflict.Distance;
                        double r = Math.Sqrt((curConflict.LeftPoint.Y - curConflict.RightPoint.Y) * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) + (curConflict.LeftPoint.X - curConflict.RightPoint.X) * (curConflict.LeftPoint.X - curConflict.RightPoint.X));
                        double s = -1 * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) / r;
                        double c = -1 * (curConflict.LeftPoint.X - curConflict.RightPoint.X) / r;
                        //这里将力平分给两个对象
                        double fx = f * c;
                        double fy = f * s;
                        GroupofMapObject rightGroup = GroupofMapObject.GetGroup(curConflict.Skel_arc.RightMapObj, groups);
                        int rightTagID = 0;
                        FeatureType type = FeatureType.Unknown;
                        if (rightGroup != null)
                        {
                            rightTagID = rightGroup.ID;
                            type = FeatureType.Group; ;
                        }
                        else
                        {
                            rightTagID = curConflict.Skel_arc.RightMapObj.ID;
                            type = FeatureType.PolygonType;
                        }
                        int ID = ProxiGraph.GetNodebyTagIDandType(rightTagID, type).ID;
                        Force force = new Force(ID, fx, fy, s, c, f);
                        VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);

                        if (vForce == null)
                        {
                            vForce = new VertexForce(ID);
                            vForceList.Add(vForce);
                        }
                        vForce.forceList.Add(force);//将当前的受力加入VertexForce数组

                    }
                    else if (curConflict.Skel_arc.RightMapObj.FeatureType == FeatureType.PolylineType)
                    {


                        double f = d - curConflict.Distance;
                        double r = Math.Sqrt((curConflict.LeftPoint.Y - curConflict.RightPoint.Y) * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) + (curConflict.LeftPoint.X - curConflict.RightPoint.X) * (curConflict.LeftPoint.X - curConflict.RightPoint.X));
                        double s = (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) / r;
                        double c = (curConflict.LeftPoint.X - curConflict.RightPoint.X) / r;
                        //这里将力平分给两个对象
                        double fx = f * c;
                        double fy = f * s;

                        GroupofMapObject leftGroup = GroupofMapObject.GetGroup(curConflict.Skel_arc.LeftMapObj, groups);
                        int leftTagID = 0;
                        FeatureType type = FeatureType.Unknown;
                        if (leftGroup != null)
                        {
                            leftTagID = leftGroup.ID;
                            type = FeatureType.Group; ;
                        }
                        else
                        {
                            leftTagID = curConflict.Skel_arc.LeftMapObj.ID;
                            type = FeatureType.PolygonType;
                        }

                        int ID = ProxiGraph.GetNodebyTagIDandType(leftTagID, type).ID;
                        Force force = new Force(ID, fx, fy, s, c, f);
                        VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);
                        if (vForce == null)
                        {
                            vForce = new VertexForce(ID);
                            vForceList.Add(vForce);
                        }
                        vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                    }
                }
            }
            #region 边界上的点
            foreach (ProxiNode curNode in this.ProxiGraph.NodeList)
            {
                if (curNode.FeatureType == FeatureType.PolylineType)
                {
                    Force force = new Force(curNode.ID, 0, 0, 0);
                    force.IsBouldPoint = true;
                    VertexForce vForce = this.GetvForcebyIndex(curNode.ID, vForceList);
                    if (vForce == null)
                    {
                        vForce = new VertexForce(curNode.ID);
                        vForceList.Add(vForce);
                    }
                    vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                }
            }
            #endregion

            #endregion

            #region  求吸引力-2014-3-20
            if (this.isDragForce == true)
            {
                int n = this.ProxiGraph.NodeList.Count;
                for (int i = 0; i < n; i++)
                {

                    ProxiNode curNode = this.ProxiGraph.NodeList[i];

                    int id = curNode.ID;
                    //  int tagID = curNode.TagID;
                    FeatureType type = curNode.FeatureType;
                    ProxiNode originalNode = this.OrigialProxiGraph.GetNodebyID(id);
                    if (originalNode == null)
                    {
                        continue;
                    }
                    double distance = ComFunLib.CalLineLength(curNode, originalNode);
                    if (distance > RMSE && (type != FeatureType.PolylineType))
                    {
                        //右边
                        double f = distance - RMSE;
                        double s = (originalNode.Y - curNode.Y) / distance;
                        double c = (originalNode.X - curNode.X) / distance;
                        //这里将力平分给两个对象
                        double fx = f * c;
                        double fy = f * s;
                        Force force = new Force(id, fx, fy, s, c, f);
                        VertexForce vForce = this.GetvForcebyIndex(id, vForceList);
                        if (vForce == null)
                        {
                            vForce = new VertexForce(id);
                            vForceList.Add(vForce);
                        }
                        vForce.forceList.Add(force);
                    }
                }
            }
            #endregion

            #region 求合力
            //先求最大力，以该力的方向为X轴方向建立局部坐标系，求四个主方向上的最大力，最后就合力

            List<Force> rforceList = new List<Force>();
            foreach (VertexForce vForce in vForceList)
            {
                if (vForce.forceList.Count == 1)//当只受一个力作用时
                {
                    rforceList.Add(vForce.forceList[0]);

                }
                else if (vForce.forceList.Count > 1)
                {
                    int index = 0;
                    double maxFx = 0;
                    double minFx = 0;
                    double maxFy = 0;
                    double minFy = 0;
                    Force maxF = GetMaxForce(out  index, vForce.forceList);
                    maxFx = maxF.F;
                    double s = maxF.Sin;
                    double c = maxF.Cos;

                    for (int i = 0; i < vForce.forceList.Count; i++)
                    {

                        if (i != index)
                        {
                            Force F = vForce.forceList[i];
                            double fx = F.Fx * c + F.Fy * s;
                            double fy = F.Fy * c - F.Fx * s;

                            if (minFx > fx) minFx = fx;
                            if (maxFy < fy) maxFy = fy;
                            if (minFy > fy) minFy = fy;
                        }
                    }
                    double FFx = maxFx + minFx;
                    double FFy = maxFy + minFy;
                    double Fx = FFx * c - FFy * s;
                    double Fy = FFx * s + FFy * c;
                    double f = Math.Sqrt(Fx * Fx + Fy * Fy);
                    Force rForce = new Force(vForce.ID, Fx, Fy, f);
                    rforceList.Add(rForce);
                }
            }

            #endregion
            return rforceList;
        }
        #endregion

        #region 输出结果
        /// <summary>
        /// 将受力向量写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="">受力向量列表</param>
        public void Create_WriteForceVector2Shp(string filePath, string fileName, esriSRProjCS4Type prj)
        {
            #region 创建一个线的shape文件
            string Folderpathstr = filePath;
            string LyrName = fileName;
            string strFolder = Folderpathstr;
            string strName = LyrName;
            string strShapeFieldName = "Shape";

            IFeatureWorkspace pFWS;//ESRI.ArcGIS.Geodatabase;
            IWorkspaceFactory pWorkspaceFactory = new ESRI.ArcGIS.DataSourcesFile.ShapefileWorkspaceFactoryClass();//ESRI.ArcGIS.DataSourcesFile
            pFWS = pWorkspaceFactory.OpenFromFile(strFolder, 0) as IFeatureWorkspace;

            //创建一个字段集
            IFields pFields = new ESRI.ArcGIS.Geodatabase.FieldsClass();
            IFieldsEdit pFieldsEdit;
            pFieldsEdit = pFields as IFieldsEdit;

            #region 创建图形字段
            IField pField;
            IFieldEdit pFieldEdit;
            //创建图形字段
            pField = new FieldClass();
            pFieldEdit = pField as IFieldEdit;
            pFieldEdit.Name_2 = strShapeFieldName;
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;

            ISpatialReferenceFactory ispfac = new SpatialReferenceEnvironmentClass();
            IProjectedCoordinateSystem iprcoorsys = ispfac.CreateProjectedCoordinateSystem((int)prj);
            ISpatialReference pSpatialReference = iprcoorsys as ISpatialReference;

            IGeometryDef pGeomDef = new GeometryDefClass();
            IGeometryDefEdit pGeomDefEdit = pGeomDef as IGeometryDefEdit;
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolyline;
            pGeomDefEdit.SpatialReference_2 = pSpatialReference;
            pFieldEdit.GeometryDef_2 = pGeomDef;
            pFieldsEdit.AddField(pField);
            #endregion

            #region 创建属性字段
            //ID
            IField pField1;
            IFieldEdit pFieldEdit1;
            pField1 = new FieldClass();
            pFieldEdit1 = pField1 as IFieldEdit;
            pFieldEdit1.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit1.Name_2 = "ID";
            pFieldEdit1.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField1);

            //起点
            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;
            pFieldEdit2.Name_2 = "F";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField2);

            //终点
            IField pField3;
            IFieldEdit pFieldEdit3;
            pField3 = new FieldClass();
            pFieldEdit3 = pField3 as IFieldEdit;
            pFieldEdit3.Length_2 = 30;
            pFieldEdit3.Name_2 = "Fx";
            pFieldEdit3.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField3);

  
            IField pField4;
            IFieldEdit pFieldEdit4;
            pField4 = new FieldClass();
            pFieldEdit4 = pField4 as IFieldEdit;
            pFieldEdit4.Length_2 = 30;
            pFieldEdit4.Name_2 = "Fy";
            pFieldEdit4.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField4);
    
            IField pField5;
            IFieldEdit pFieldEdit5;
            pField5 = new FieldClass();
            pFieldEdit5 = pField5 as IFieldEdit;
            pFieldEdit5.Length_2 = 30;
            pFieldEdit5.Name_2 = "SID";
            pFieldEdit5.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField5);


            #endregion

            #region 创建要素类
            IFeatureClass pFeatClass;
            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
            #endregion
            #endregion

            #region 向线层添加线要素

            //IFeatureClass featureClass = null;
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            IWorkspaceEdit pIWorkspaceEdit = null;
            IDataset pIDataset = (IDataset)pFeatClass;

            if (pIDataset != null)
            {
                pIWorkspaceEdit = (IWorkspaceEdit)pIDataset.Workspace;
            }
            try
            {
                if (pFeatClass == null)
                    return;
                //获取顶点图层的数据集，并创建工作空间
                IDataset dataset = (IDataset)pFeatClass;
                IWorkspace workspace = dataset.Workspace;
                IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
                //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
                IFeatureClassWrite fr = (IFeatureClassWrite)pFeatClass;
                //注意：此时，所编辑数据不能被其他程序打开
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();

                int n = this.ForceList.Count;
                if (n == 0)
                    return;

                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PolylineClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = null;
                    if (this.ForceList[i] == null)
                        continue;
                    Node node = null;
                    //邻近图
                    if (this.ProxiGraph != null)
                    {
                        node = this.ProxiGraph.GetNodebyID(this.ForceList[i].ID);
                    }
                    //线目标
                    else
                    {
                       node= this.Map.TriNodeList[this.ForceList[i].ID];
                    }

                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(node.X, node.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(node.X + 5*this.ForceList[i].Fx, node.Y + 5*this.ForceList[i].Fy);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                    feature.Shape = shp;
                    feature.set_Value(2, this.ForceList[i].ID);
                    feature.set_Value(3, this.ForceList[i].F);
                    feature.set_Value(4, this.ForceList[i].Fx);
                    feature.set_Value(5, this.ForceList[i].Fy);
                    feature.set_Value(6, this.ForceList[i].SID);
                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }

                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                //MessageBox.Show("异常信息" + ex.Message);
            }
            #endregion
        }
        /// <summary>
        /// 将受力向量写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="">受力向量列表</param>
        public void Create_WriteForceVector2Shp(string filePath, string fileName, esriSRProjCS4Type prj,double  k)
        {
            #region 创建一个线的shape文件
            string Folderpathstr = filePath;
            string LyrName = fileName;
            string strFolder = Folderpathstr;
            string strName = LyrName;
            string strShapeFieldName = "Shape";

            IFeatureWorkspace pFWS;//ESRI.ArcGIS.Geodatabase;
            IWorkspaceFactory pWorkspaceFactory = new ESRI.ArcGIS.DataSourcesFile.ShapefileWorkspaceFactoryClass();//ESRI.ArcGIS.DataSourcesFile
            pFWS = pWorkspaceFactory.OpenFromFile(strFolder, 0) as IFeatureWorkspace;

            //创建一个字段集
            IFields pFields = new ESRI.ArcGIS.Geodatabase.FieldsClass();
            IFieldsEdit pFieldsEdit;
            pFieldsEdit = pFields as IFieldsEdit;

            #region 创建图形字段
            IField pField;
            IFieldEdit pFieldEdit;
            //创建图形字段
            pField = new FieldClass();
            pFieldEdit = pField as IFieldEdit;
            pFieldEdit.Name_2 = strShapeFieldName;
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;

            ISpatialReferenceFactory ispfac = new SpatialReferenceEnvironmentClass();
            IProjectedCoordinateSystem iprcoorsys = ispfac.CreateProjectedCoordinateSystem((int)prj);
            ISpatialReference pSpatialReference = iprcoorsys as ISpatialReference;

            IGeometryDef pGeomDef = new GeometryDefClass();
            IGeometryDefEdit pGeomDefEdit = pGeomDef as IGeometryDefEdit;
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolyline;
            pGeomDefEdit.SpatialReference_2 = pSpatialReference;
            pFieldEdit.GeometryDef_2 = pGeomDef;
            pFieldsEdit.AddField(pField);
            #endregion

            #region 创建属性字段
            //ID
            IField pField1;
            IFieldEdit pFieldEdit1;
            pField1 = new FieldClass();
            pFieldEdit1 = pField1 as IFieldEdit;
            pFieldEdit1.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit1.Name_2 = "ID";
            pFieldEdit1.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField1);

            //起点
            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;
            pFieldEdit2.Name_2 = "F";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField2);

            //终点
            IField pField3;
            IFieldEdit pFieldEdit3;
            pField3 = new FieldClass();
            pFieldEdit3 = pField3 as IFieldEdit;
            pFieldEdit3.Length_2 = 30;
            pFieldEdit3.Name_2 = "Fx";
            pFieldEdit3.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField3);


            IField pField4;
            IFieldEdit pFieldEdit4;
            pField4 = new FieldClass();
            pFieldEdit4 = pField4 as IFieldEdit;
            pFieldEdit4.Length_2 = 30;
            pFieldEdit4.Name_2 = "Fy";
            pFieldEdit4.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField4);

            IField pField5;
            IFieldEdit pFieldEdit5;
            pField5 = new FieldClass();
            pFieldEdit5 = pField5 as IFieldEdit;
            pFieldEdit5.Length_2 = 30;
            pFieldEdit5.Name_2 = "SID";
            pFieldEdit5.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField5);


            #endregion

            #region 创建要素类
            IFeatureClass pFeatClass;
            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
            #endregion
            #endregion

            #region 向线层添加线要素

            //IFeatureClass featureClass = null;
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            IWorkspaceEdit pIWorkspaceEdit = null;
            IDataset pIDataset = (IDataset)pFeatClass;

            if (pIDataset != null)
            {
                pIWorkspaceEdit = (IWorkspaceEdit)pIDataset.Workspace;
            }
            try
            {
                if (pFeatClass == null)
                    return;
                //获取顶点图层的数据集，并创建工作空间
                IDataset dataset = (IDataset)pFeatClass;
                IWorkspace workspace = dataset.Workspace;
                IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
                //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
                IFeatureClassWrite fr = (IFeatureClassWrite)pFeatClass;
                //注意：此时，所编辑数据不能被其他程序打开
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();

                int n = this.ForceList.Count;
                if (n == 0)
                    return;

                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PolylineClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = null;
                    if (this.ForceList[i] == null)
                        continue;
                    Node node = null;
                    //邻近图
                    if (this.ProxiGraph != null)
                    {
                        node = this.ProxiGraph.GetNodebyID(this.ForceList[i].ID);
                    }
                    //线目标
                    else
                    {
                        node = this.Map.TriNodeList[this.ForceList[i].ID];
                    }

                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(node.X, node.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(node.X + k * this.ForceList[i].Fx, node.Y + k * this.ForceList[i].Fy);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                    feature.Shape = shp;
                    feature.set_Value(2, this.ForceList[i].ID);
                    feature.set_Value(3, this.ForceList[i].F);
                    feature.set_Value(4, this.ForceList[i].Fx);
                    feature.set_Value(5, this.ForceList[i].Fy);
                    feature.set_Value(6, this.ForceList[i].SID);
                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }

                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                //MessageBox.Show("异常信息" + ex.Message);
            }
            #endregion
        }
        /// <summary>
        /// 将受力向量写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="">受力向量列表</param>
        public void Create_WriteForceVector2Shp(int times, string filePath, string fileName, esriSRProjCS4Type prj)
        {
            #region 创建一个线的shape文件
            string Folderpathstr = filePath;
            string LyrName = fileName;
            string strFolder = Folderpathstr;
            string strName = LyrName;
            string strShapeFieldName = "Shape";

            IFeatureWorkspace pFWS;//ESRI.ArcGIS.Geodatabase;
            IWorkspaceFactory pWorkspaceFactory = new ESRI.ArcGIS.DataSourcesFile.ShapefileWorkspaceFactoryClass();//ESRI.ArcGIS.DataSourcesFile
            pFWS = pWorkspaceFactory.OpenFromFile(strFolder, 0) as IFeatureWorkspace;

            //创建一个字段集
            IFields pFields = new ESRI.ArcGIS.Geodatabase.FieldsClass();
            IFieldsEdit pFieldsEdit;
            pFieldsEdit = pFields as IFieldsEdit;

            #region 创建图形字段
            IField pField;
            IFieldEdit pFieldEdit;
            //创建图形字段
            pField = new FieldClass();
            pFieldEdit = pField as IFieldEdit;
            pFieldEdit.Name_2 = strShapeFieldName;
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;

            ISpatialReferenceFactory ispfac = new SpatialReferenceEnvironmentClass();
            IProjectedCoordinateSystem iprcoorsys = ispfac.CreateProjectedCoordinateSystem((int)prj);
            ISpatialReference pSpatialReference = iprcoorsys as ISpatialReference;

            IGeometryDef pGeomDef = new GeometryDefClass();
            IGeometryDefEdit pGeomDefEdit = pGeomDef as IGeometryDefEdit;
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolyline;
            pGeomDefEdit.SpatialReference_2 = pSpatialReference;
            pFieldEdit.GeometryDef_2 = pGeomDef;
            pFieldsEdit.AddField(pField);
            #endregion

            #region 创建属性字段
            //ID
            IField pField1;
            IFieldEdit pFieldEdit1;
            pField1 = new FieldClass();
            pFieldEdit1 = pField1 as IFieldEdit;
            pFieldEdit1.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit1.Name_2 = "ID";
            pFieldEdit1.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField1);

            //起点
            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;
            pFieldEdit2.Name_2 = "F";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField2);

            //终点
            IField pField3;
            IFieldEdit pFieldEdit3;
            pField3 = new FieldClass();
            pFieldEdit3 = pField3 as IFieldEdit;
            pFieldEdit3.Length_2 = 30;
            pFieldEdit3.Name_2 = "Fx";
            pFieldEdit3.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField3);


            IField pField4;
            IFieldEdit pFieldEdit4;
            pField4 = new FieldClass();
            pFieldEdit4 = pField4 as IFieldEdit;
            pFieldEdit4.Length_2 = 30;
            pFieldEdit4.Name_2 = "Fy";
            pFieldEdit4.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField4);

            IField pField5;
            IFieldEdit pFieldEdit5;
            pField5 = new FieldClass();
            pFieldEdit5 = pField5 as IFieldEdit;
            pFieldEdit5.Length_2 = 30;
            pFieldEdit5.Name_2 = "SID";
            pFieldEdit5.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField5);


            #endregion

            #region 创建要素类
            IFeatureClass pFeatClass;
            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
            #endregion
            #endregion

            #region 向线层添加线要素

            //IFeatureClass featureClass = null;
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            IWorkspaceEdit pIWorkspaceEdit = null;
            IDataset pIDataset = (IDataset)pFeatClass;

            if (pIDataset != null)
            {
                pIWorkspaceEdit = (IWorkspaceEdit)pIDataset.Workspace;
            }
            try
            {
                if (pFeatClass == null)
                    return;
                //获取顶点图层的数据集，并创建工作空间
                IDataset dataset = (IDataset)pFeatClass;
                IWorkspace workspace = dataset.Workspace;
                IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
                //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
                IFeatureClassWrite fr = (IFeatureClassWrite)pFeatClass;
                //注意：此时，所编辑数据不能被其他程序打开
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();

                int n = this.ForceList.Count;
                if (n == 0)
                    return;

                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PolylineClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = null;
                    if (this.ForceList[i] == null)
                        continue;
                    Node node = null;
                    //邻近图
                    if (this.ProxiGraph != null)
                    {
                        node = this.ProxiGraph.GetNodebyID(this.ForceList[i].ID);
                    }
                    //线目标
                    else
                    {
                        node = this.Map.TriNodeList[this.ForceList[i].ID];
                    }

                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(node.X, node.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(node.X + times * this.ForceList[i].Fx, node.Y + times*this.ForceList[i].Fy);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                    feature.Shape = shp;
                    feature.set_Value(2, this.ForceList[i].ID);
                    feature.set_Value(3, this.ForceList[i].F);
                    feature.set_Value(4, this.ForceList[i].Fx);
                    feature.set_Value(5, this.ForceList[i].Fy);
                    feature.set_Value(6, this.ForceList[i].SID);
                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }

                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                //MessageBox.Show("异常信息" + ex.Message);
            }
            #endregion
        }
        #endregion
    }
}
