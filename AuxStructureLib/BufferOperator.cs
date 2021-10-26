using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;

namespace AuxStructureLib
{
    /// <summary>
    /// 生成缓冲区操作
    /// </summary>
    public class BufferOperator
    {
        /// <summary>
        /// 调用ERSI中的ITopologicalOperator接口生成地图对象缓冲区
        /// Liuygis:2014-7-24
        /// </summary>
        /// <param name="mo">地图对象</param>
        /// <param name="bufferDis">半径</param>
        /// <returns>缓冲区多边形对象SMap：PolygonObject列表</returns>
        public static List<PolygonObject> CreateBufferbyEsri_Polygon(MapObject mo, double bufferDis)
        {
            if (mo == null)
                return null;
            FeatureType type = mo.FeatureType;
            ITopologicalOperator pTopoOp = null;

            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            #region 从MapObject对象转换成IGeometry

            IGeometry shp = null;
            if (type == FeatureType.PointType)
            {
                PointObject p = mo as PointObject;
                shp = new PointClass();
                ((PointClass)shp).PutCoords(p.Point.X, p.Point.Y);

            }
            else if (type == FeatureType.PolylineType)
            {
                PolylineObject polyline = mo as PolylineObject;
                shp = new PolylineClass();
                IPointCollection pointSet = shp as IPointCollection;
                IPoint curResultPoint = null;
                TriNode curPoint = null;

                int m = polyline.PointList.Count;

                for (int k = 0; k < m; k++)
                {
                    curPoint = polyline.PointList[k];
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                }
            }
            else if (type == FeatureType.PolygonType)
            {
                PolygonObject polygon = mo as PolygonObject;
                shp = new PolygonClass();
                IPointCollection pointSet = shp as IPointCollection;
                IPoint curResultPoint = null;
                TriNode curPoint = null; ;
                int m = polygon.PointList.Count;

                for (int k = 0; k < m; k++)
                {
                    curPoint = polygon.PointList[k];
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                }
                curPoint = polygon.PointList[0];
                curResultPoint = new PointClass();
                curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
            }
            #endregion

            //调用ITopologicalOperator接口生成Buffer
            pTopoOp = shp as ITopologicalOperator;
            if (pTopoOp == null)
                return null;
            IPolygon pPly = new PolygonClass();
            pPly = pTopoOp.Buffer(bufferDis) as IPolygon;
            if (pPly == null)
                return null;

            #region 转换成PolygonObject列表
            List<PolygonObject> polylist = new List<PolygonObject>();
            IGeometryCollection pathSet = pPly as IGeometryCollection;
            int count = pathSet.GeometryCount;
            //Path对象
            IPath curPath = null;
            for (int i = 0; i < count; i++)
            {
                PolygonObject curPP = null;                      //当前道路
                TriNode curVextex = null;                  //当前关联点
                List<TriNode> curPointList = new List<TriNode>();
                double curX;
                double curY;
                curPath = pathSet.get_Geometry(i) as IPath;
                IPointCollection pointSet = curPath as IPointCollection;
                int pointCount = pointSet.PointCount;
                if (pointCount >= 3)
                {
                    //ArcGIS中将起点和终点重复存储
                    for (int j = 0; j < pointCount - 1; j++)
                    {
                        //添加起点
                        curX = pointSet.get_Point(j).X;
                        curY = pointSet.get_Point(j).Y;
                        curVextex = new TriNode(curX, curY, -1, -1, FeatureType.PolygonType);

                        curPointList.Add(curVextex);

                    }

                    //添加起点
                    curPP = new PolygonObject(-1, curPointList);
                    polylist.Add(curPP);
                }
            }
            #endregion

            if (polylist == null || polylist.Count == 0)
                return null;
            return polylist;
        }

        /// <summary>
        /// 调用ERSI中的ITopologicalOperator接口生成地图对象缓冲区
        /// Liuygis:2014-7-24
        /// </summary>
        /// <param name="mo">地图对象</param>
        /// <param name="bufferDis">半径</param>
        /// <returns>缓冲区多边形对象ESRI：IPolygon对象 </returns>
        public static IPolygon CreateBufferbyEsri_Esri(MapObject mo, double bufferDis)
        {
            if (mo == null)
                return null;
            FeatureType type = mo.FeatureType;
            ITopologicalOperator pTopoOp = null;

            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            #region 从MapObject对象转换成IGeometry

            IGeometry shp = null;
            if (type == FeatureType.PointType)
            {
                PointObject p = mo as PointObject;
                shp = new PointClass();
                ((PointClass)shp).PutCoords(p.Point.X, p.Point.Y);

            }
            else if (type == FeatureType.PolylineType)
            {
                PolylineObject polyline = mo as PolylineObject;
                shp = new PolylineClass();
                IPointCollection pointSet = shp as IPointCollection;
                IPoint curResultPoint = null;
                TriNode curPoint = null;

                int m = polyline.PointList.Count;

                for (int k = 0; k < m; k++)
                {
                    curPoint = polyline.PointList[k];
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                }
            }
            else if (type == FeatureType.PolygonType)
            {
                PolygonObject polygon = mo as PolygonObject;
                shp = new PolygonClass();
                IPointCollection pointSet = shp as IPointCollection;
                IPoint curResultPoint = null;
                TriNode curPoint = null; ;
                int m = polygon.PointList.Count;

                for (int k = 0; k < m; k++)
                {
                    curPoint = polygon.PointList[k];
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                }
                curPoint = polygon.PointList[0];
                curResultPoint = new PointClass();
                curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
            }
            #endregion

            //调用ITopologicalOperator接口生成Buffer
            pTopoOp = shp as ITopologicalOperator;
            if (pTopoOp == null)
                return null;
            IPolygon pPly = new PolygonClass();
            pPly = pTopoOp.Buffer(bufferDis) as IPolygon;
            return pPly;
        }
        /// <summary>
        /// 将多个缓冲区合并
        /// </summary>
        /// <param name="polygonList">IPolygon列表</param>
        /// <returns>多边形融合</returns>
        public static IPolygon CreateUnionedBuffers(List<IPolygon> polygonList)
        {
            IPolygon curIPolygon=polygonList[0];
            ITopologicalOperator top = curIPolygon as ITopologicalOperator;
            for(int i=1;i<polygonList.Count;i++)
            {

                top = top.Union(polygonList[i]) as ITopologicalOperator;
            }
            IPolygon resPolygon = top as IPolygon;
            //resPolygon.
            return resPolygon;
        }
        /// <summary>
        /// 将一个具有多部分path组成的复杂多边形拆开
        /// </summary>
        /// <param name="multiPartPolygon">多部分path组成的复杂多边</param>
        /// <returns>结果IPolygon列表</returns>
        public static List<IPolygon> SegmentMultiPartPolygon(IPolygon multiPartPolygon)
        {
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;
            List<IPolygon> EsriPolygonList = new List<IPolygon>();
            IGeometryCollection pathSet = multiPartPolygon as IGeometryCollection;
            int n = pathSet.GeometryCount;
            for (int i=0;i<n;i++)
            {
                IGeometry curPath = pathSet.get_Geometry(i) as IGeometry;
                IPolygon curPolygon = new PolygonClass();
                (curPolygon as IGeometryCollection).AddGeometry(curPath, missing1, missing2);
                EsriPolygonList.Add(curPolygon);
            }
            return EsriPolygonList;
        }
    }

   
}
