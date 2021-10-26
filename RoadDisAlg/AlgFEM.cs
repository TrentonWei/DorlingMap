using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatrixOperation;
using ESRI.ArcGIS.Geometry;
using System.Data;

namespace RoadDisAlg
{
    /// <summary>
    /// 有限元通用算法
    /// </summary>
    public abstract class AlgFEM
    {

        public RoadNetWork _NetWork = null;                         //道路网络对象
      
        public static double minDis = 0.2;                           //图上最小间距单位为毫米
        public static double scale = 500000.0;                       //比例尺分母

        public static ForceType ForceType = ForceType.Vetex;
        public static GradeModelType GradeModelType = GradeModelType.Ratio;  //分级模式
        public static DataTable dtPara = null;                               //参数表格 

        protected Force[] forceList = null;                                //每个点上的受力
        protected double[] tdx = null;                                    //记录X方向最终的累积移位量
        protected double[] tdy = null;                                    //记录Y方向最终的累积移位量
        protected double[] d = null;                                      //最终的移位量
        public double max;
        public double min;
        public double sum;
        public double ave;
        public double std;

        /// <summary>
        /// 执行移位操作
        /// </summary>
        public abstract void DoDispace();
        
        /// <summary>
        /// 计算移位向量
        /// </summary>
        public abstract void DoDispaceIterate();

        /// <summary>
        /// 移位后的点中重新生成集合对象几何（用于计算受力的）
        /// </summary>
        protected void CreategeoSetFromRes()
        {
            ClearRoadLyrGeoSet();// 清空道路图层
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;
            string lyrName = "";
            RoadLyrInfo curLyrInfo = null;
            foreach (Road curRoad in this._NetWork.RoadList)
            {
                lyrName = curRoad.RoadGrade.LyrName;
                RoadLyrInfo lyrInfo = GetLyrinfo(lyrName);
                IGeometry shp = new PolylineClass();
                IPointCollection pointSet = shp as IPointCollection;
                IPoint curResultPoint = null;
                PointCoord curPoint = null;
                int h = curRoad.PointList.Count;
                for (int k = 0; k < h; k++)
                {
                    curPoint = curRoad.GetCoord(k);
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                }
                if (lyrInfo != null)
                {
                    lyrInfo.GeoSet.AddGeometry(shp, ref missing1, ref missing2);
                }
            }
        }

        /// <summary>
        /// 根据图层名称返回
        /// </summary>
        /// <param name="lyrName">图层名称</param>
        /// <returns>图层信息对象</returns>
        private RoadLyrInfo GetLyrinfo(string lyrName)
        {
            foreach (RoadLyrInfo curLyrInfo in this._NetWork.RoadLyrInfoList)
            {
                if (curLyrInfo.RoadGrade.LyrName == lyrName)
                {
                    return curLyrInfo;
                }
            }
            return null;
        }
        /// <summary>
        /// 清空道路图层的所有图形对象
        /// </summary>
        private void ClearRoadLyrGeoSet()
        {
            foreach (RoadLyrInfo curLyrInfo in this._NetWork.RoadLyrInfoList)
            {
                if (curLyrInfo.RoadGrade.Grade != 999)
                {
                    int count = curLyrInfo.GeoSet.GeometryCount;
                    curLyrInfo.GeoSet.RemoveGeometries(0, count);
                }
            }
        }

        /// <summary>
        /// 统计移位量
        /// </summary>
        protected void StaticDis()
        {
            int n = tdx.Length;
            max = -1;
            min = 9999999;
            sum = 0;
            for (int i = 0; i < n; i++)
            {
                d[i] = Math.Sqrt(tdx[i] * tdx[i] + tdy[i] * tdy[i]);
                sum += d[i];
                if (d[i] > max)
                {
                    max = d[i];
                }
                if (d[i] < min)
                {
                    min = d[i];
                }
            }
            ave = sum / n;
            double s = 0;
            double dif = 0;
            for (int i = 0; i < n; i++)
            {
                dif = d[i] - ave;
                s += dif * dif;
            }
            std = Math.Sqrt(s / n);

            //转换成毫米单位
            max = 1000 * max / AlgFEM.scale;
            min = 1000 * min / AlgFEM.scale;
            sum = 1000 * sum / AlgFEM.scale;
            ave = 1000 * ave / AlgFEM.scale;
            std = 1000 * std / AlgFEM.scale;
        }
    }
}
