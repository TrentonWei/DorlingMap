using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System.Windows.Forms;
using AuxStructureLib.IO;
using System.Data;
using System.IO;

namespace AuxStructureLib
{
    /// <summary>
    /// 地图类-用于处理数据读入和组织
    /// </summary>
    [Serializable]
    public class SMap
    {
        /// <summary>
        /// 数据列表
        /// </summary>
        private List<IFeatureLayer> lyrList = null;
        /// <summary>
        /// 点对象列表
        /// </summary>
        public List<PointObject> PointList = null;
        /// <summary>
        /// 线对象列表
        /// </summary>
        public List<PolylineObject> PolylineList = null;
        /// <summary>
        /// 多边形对象列表
        /// </summary>
        public List<PolygonObject> PolygonList = null;
        /// <summary>
        /// 坐标顶点列表
        /// </summary>
        public List<TriNode> TriNodeList = null;
        /// <summary>
        /// 线的关联点列表
        /// </summary>
        public List<ConNode> ConNodeList = null;

        /// <summary>
        /// 地图对象个数
        /// </summary>
        public int NumberofMapObject
        {
            get 
            {
                int n = 0;
                if (this.PointList != null && this.PointList.Count > 0)
                {
                    n += this.PointList.Count;
                }
                if (this.PolygonList != null && this.PolygonList.Count > 0)
                {
                    n += this.PolygonList.Count;
                }
                if (this.PolylineList != null && this.PolylineList.Count > 0)
                {
                    n += this.PolylineList.Count;
                }
                return n;
            }
        }

        /// <summary>
        /// 地图对象个数
        /// </summary>
        public List<MapObject> MapObjectList
        {
            get
            {
                List<MapObject> mapObjectList = new List<MapObject>();

                if (this.PointList != null && this.PointList.Count > 0)
                {
                    mapObjectList.AddRange(this.PointList);
                }
                if (this.PolygonList != null && this.PolygonList.Count > 0)
                {
                    mapObjectList.AddRange(this.PolygonList);
                }
                if (this.PolylineList != null && this.PolylineList.Count > 0)
                {
                    mapObjectList.AddRange(this.PolylineList);
                }
                return mapObjectList;
            }
        }

        /// <summary>
        /// 地图构造函数
        /// </summary>
        public SMap(List<IFeatureLayer> lyrs)
        {
            lyrList = lyrs;
            PointList = new List<PointObject>();
            PolylineList = new List<PolylineObject>();
            ConNodeList = new List<ConNode>();
            PolygonList = new List<PolygonObject>();
            TriNodeList = new List<TriNode>();
        }
        /// <summary>
        /// 地图构造函数
        /// </summary>
        public SMap()
        {
            PointList = new List<PointObject>();
            PolylineList = new List<PolylineObject>();
            ConNodeList = new List<ConNode>();
            PolygonList = new List<PolygonObject>();
            TriNodeList = new List<TriNode>();
        }



        public string strPath="";
        /// <summary>
        /// 从ArcGIS图层中读取地图数据
        /// </summary>
        public void ReadDateFrmEsriLyrs()
        {

            if (lyrList == null || lyrList.Count == 0)
            {
                return;
            }

            #region 读符号文件
            List<Symbol> symbolList = new List<Symbol>();
            //读文件========
            DataTable dt = TestIO.ReadData(strPath+@"\Symbol.xml");
            if (dt != null)
            {
                foreach (DataRow curR in dt.Rows)
                {
                    int ID = Convert.ToInt32(curR[0]);
                    int sylID = Convert.ToInt32(curR[1]);
                    string LayName = Convert.ToString(curR[2]);
                    double size = Convert.ToDouble(curR[3]);
                    string FillColor = Convert.ToString(curR[4]);
                    string BorderColor = Convert.ToString(curR[5]);

                    Symbol s = new Symbol(ID, sylID, LayName, size, FillColor, BorderColor);

                    symbolList.Add(s);
                }
            }

            #endregion

            #region 创建列表
            int pCount = 0;
            int lCount = 0;
            int aCount = 0;
            foreach (IFeatureLayer curLyr in lyrList)
            {
                if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPoint)
                {
                    pCount++;

                }
                else if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                {
                    lCount++;
                }
                else if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                {
                    aCount++;
                }
            }
            if (pCount > 0)
            {
                PointList = new List<PointObject>();
            }
            if (lCount > 0)
            {
                PolylineList = new List<PolylineObject>();
                ConNodeList = new List<ConNode>();
            }
            if (aCount > 0)
            {
                PolygonList = new List<PolygonObject>();
            }
            TriNodeList = new List<TriNode>();
            #endregion

            int vextexID = 0;
            double sylSize=0;
            int pID =0;
            int plID =0;
            int ppID =0;

            foreach (IFeatureLayer curLyr in lyrList)
            {
                IFeatureCursor cursor = null;
                IFeature curFeature = null;
                IGeometry shp = null;
                Symbol curSyl= null;

                curSyl = Symbol.GetSymbolbyLyrName(curLyr.Name, symbolList);
                if (curSyl != null)
                    sylSize = curSyl.Size;
                switch (curLyr.FeatureClass.ShapeType)
                {
                       
                    case esriGeometryType.esriGeometryPoint:
                        {


                            #region 点要素
                            //点要素
                            cursor = curLyr.Search(null, false);

                            while ((curFeature = cursor.NextFeature()) != null)
                            {
                                shp = curFeature.Shape;
                                //pID = curFeature.OID;
                                IPoint point = null;
                                double TT = 0;

                                #region 读取Travel Time
                                try
                                {
                                    IFields pFields = curFeature.Fields;
                                    int field1 = pFields.FindField("TTD");
                                    TT = Convert.ToInt16(curFeature.get_Value(field1));
                                }

                                catch { }
                                #endregion

                                //几何图形
                                if (shp.GeometryType == esriGeometryType.esriGeometryPoint)
                                {
                                    point = shp as IPoint;
                                    PointObject curPoint = null;           //当前道路
                                    TriNode curVextex = null;                  //当前关联点
                                    double curX;
                                    double curY;

                                    curX = point.X;
                                    curY = point.Y;
                                    curVextex = new TriNode((float)curX, (float)curY, vextexID, pID, FeatureType.PointType);
                                    curVextex.Initial_X = curX; curVextex.Initial_Y = curY;

                                    curPoint = new PointObject(pID, curVextex);

                                    curPoint.TT = TT;

                                    curPoint.SylWidth = sylSize;
                                    TriNodeList.Add(curVextex);
                                    PointList.Add(curPoint);
                                    vextexID++;
                                    pID++;
                                }

                            }
                            #endregion
                            break;
                        }
                    case esriGeometryType.esriGeometryPolyline:
                        {

                            #region 线要素
                            cursor = curLyr.Search(null, false);
                            curFeature = null;
                            shp = null;
                            while ((curFeature = cursor.NextFeature()) != null)
                            {
                                shp = curFeature.Shape;
                                IPolyline polyline = null;
                                IGeometryCollection pathSet = null;
                                int indexofType=curFeature.Fields.FindField("Type");
                                int typeID = 1;
                                if (indexofType!=-1&&curFeature.get_Value(indexofType) != null)
                                {
                                    try
                                    {
                                        typeID = (Int16)(curFeature.get_Value(indexofType));
                                    }
                                    catch
                                    {
                                        typeID = 1;
                                    }
                                }
                                //几何图形
                                if (shp.GeometryType == esriGeometryType.esriGeometryPolyline)
                                {
                                    polyline = shp as IPolyline;
                                    //plID = curFeature.OID;
                                    pathSet = polyline as IGeometryCollection;
                                    int count = pathSet.GeometryCount;
                                    //Path对象
                                    IPath curPath = null;
                                    for (int i = 0; i < count; i++)
                                    {
                                        PolylineObject curPL = null;                      //当前道路
                                        TriNode curVextex = null;                  //当前关联点
                                        List<TriNode> curPointList = new List<TriNode>();
                                        double curX;
                                        double curY;

                                        curPath = pathSet.get_Geometry(i) as IPath;
                                        IPointCollection pointSet = curPath as IPointCollection;
                                        int pointCount = pointSet.PointCount;
                                        if (pointCount >= 2)
                                        {
                                            curX = pointSet.get_Point(0).X;
                                            curY = pointSet.get_Point(0).Y;
                                            TriNode cNode = ConNode.GetContainNode(ConNodeList, TriNodeList, curX, curY);
                                            if (cNode == null)   //该关联点还未加入的情况
                                            {
                                                curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                                                TriNodeList.Add(curVextex);
                                                ConNode curNode = new ConNode(vextexID, 0.2f, curVextex);
                                                ConNodeList.Add(curNode);
                                                curPointList.Add(curVextex);
                                                vextexID++;
                                            }
                                            else //该关联点已经加入的情况
                                            {
                                                curPointList.Add(cNode);
                                                cNode.TagValue = -1;
                                                cNode.FeatureType = FeatureType.ConnNode;
                                            }
                                            //加入中间顶点
                                            for (int j = 1; j < pointCount - 1; j++)
                                            {
                                                curX = pointSet.get_Point(j).X;
                                                curY = pointSet.get_Point(j).Y;
                                                curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                                                TriNodeList.Add(curVextex);
                                                curPointList.Add(curVextex);
                                                vextexID++;
                                            }
                                            //加入终点
                                            curX = pointSet.get_Point(pointCount - 1).X;
                                            curY = pointSet.get_Point(pointCount - 1).Y;
                                            cNode = ConNode.GetContainNode(ConNodeList, TriNodeList, curX, curY);
                                            if (cNode == null)   //该关联点还未加入的情况
                                            {
                                                curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                                                TriNodeList.Add(curVextex);

                                                ConNode curNode = new ConNode(vextexID, -1f, curVextex);
                                                ConNodeList.Add(curNode);
                                                curPointList.Add(curVextex);
                                                vextexID++;
                                            }
                                            else //该关联点已经加入的情况
                                            {
                                                curPointList.Add(cNode);
                                                cNode.TagValue = -1;
                                                cNode.FeatureType = FeatureType.ConnNode;
                                            }

                                            //添加起点
                                            curPL = new PolylineObject(plID, curPointList, sylSize);
                                            curPL.TypeID = typeID;
                                            PolylineList.Add(curPL);
                                            plID++;
                                        }
                                    }
                                }
                            }

                            #endregion
                            break;
                        }

                    case esriGeometryType.esriGeometryPolygon:
                        {

                            #region 面要素
                            cursor = curLyr.Search(null, false);
                            curFeature = null;
                            shp = null;
                            while ((curFeature = cursor.NextFeature()) != null)
                            {
                                shp = curFeature.Shape;
                                IPolygon polygon = null;
                                IGeometryCollection pathSet = null;
                                //几何图形
                                if (shp.GeometryType == esriGeometryType.esriGeometryPolygon)
                                {
                                   // ppID = curFeature.OID;
                                    polygon = shp as IPolygon;
                                    pathSet = polygon as IGeometryCollection;
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
                                                curVextex = new TriNode(curX, curY, vextexID, ppID, FeatureType.PolygonType);
                                                TriNodeList.Add(curVextex);
                                                curPointList.Add(curVextex);
                                                vextexID++;
                                            }

                                            //添加起点
                                            curPP = new PolygonObject(ppID, curPointList);
                                            curPP.SylWidth = sylSize;
                                            this.PolygonList.Add(curPP);
                                            ppID++;
                                        }
                                    }
                                }
                            }
                            #endregion
                            break;
                        }
                }
            }
        }

        /// <summary>
        /// 从ArcGIS图层中读取地图数据
        /// </summary>
        public void ReadDateFrmMaps(SMap Map)
        {
            #region 初始化
            if (Map.PointList.Count > 0)
            {
                PointList = new List<PointObject>();
            }
            if (Map.PolylineList.Count > 0)
            {
                PolylineList = new List<PolylineObject>();
                ConNodeList = new List<ConNode>();
            }
            if (Map.PolygonList.Count > 0)
            {
                PolygonList = new List<PolygonObject>();
            }
            TriNodeList = new List<TriNode>();
            #endregion

            int vextexID = 0;
            double sylSize = 0;
            int pID = 0;
            int plID = 0;
            int ppID = 0;

            #region 点要素
            for (int i = 0; i < Map.PointList.Count; i++)
            {
                PointObject point = Map.PointList[i];
                PointObject curPoint = null;           //当前道路
                TriNode curVextex = null;                  //当前关联点
                double curX;
                double curY;

                curX = point.Point.X;
                curY = point.Point.Y;
                curVextex = new TriNode((float)curX, (float)curY, vextexID, pID, FeatureType.PointType);
                curVextex.Initial_X = curX; curVextex.Initial_Y = curY;

                curPoint = new PointObject(pID, curVextex);

                curPoint.TT = point.TT;
                TriNodeList.Add(curVextex);
                PointList.Add(curPoint);
                vextexID++;
                pID++;
            }
            #endregion

            #region 线要素
            for (int i = 0; i < Map.PolylineList.Count; i++)
            {
                PolylineObject polyline = Map.PolylineList[i];

                PolylineObject curPL = null;                      //当前道路
                TriNode curVextex = null;
                List<TriNode> curPointList = new List<TriNode>();
                double curX;
                double curY;

                for (int j = 0; j < polyline.PointList.Count; j++)
                {
                    curX = polyline.PointList[j].X;
                    curY = polyline.PointList[j].Y;

                    curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                    TriNodeList.Add(curVextex);
                    ConNode curNode = new ConNode(vextexID, 0.2f, curVextex);
                    ConNodeList.Add(curNode);
                    curPointList.Add(curVextex);
                    vextexID++;

                    curPL = new PolylineObject(plID, curPointList, sylSize);
                    PolylineList.Add(curPL);
                    plID++;
                }
            }
            #endregion

            #region 面要素
            for (int i = 0; i < Map.PolygonList.Count; i++)
            {
                PolygonObject polygon = Map.PolygonList[i];
                PolygonObject curPP = null;                      //当前道路
                TriNode curVextex = null;                  //当前关联点
                List<TriNode> curPointList = new List<TriNode>();
                double curX;
                double curY;

                for (int j = 0; j < polygon.PointList.Count; j++)
                {
                    curX = polygon.PointList[j].X;
                    curY = polygon.PointList[j].Y;
                    curVextex = new TriNode(curX, curY, vextexID, ppID, FeatureType.PolygonType);
                    TriNodeList.Add(curVextex);
                    curPointList.Add(curVextex);
                    vextexID++;
                }

                //添加起点
                curPP = new PolygonObject(ppID, curPointList);
                this.PolygonList.Add(curPP);
                ppID++;
            }
            #endregion
        }


        /// <summary>
        /// 从ArcGIS图层中读取地图数据ForEnrichNetWork
        /// </summary>
        public void ReadDateFrmEsriLyrsForEnrichNetWork()
        {

            if (lyrList == null || lyrList.Count == 0)
            {
                return;
            }

            #region 读符号文件
            List<Symbol> symbolList = new List<Symbol>();
            //读文件========
            DataTable dt = TestIO.ReadData(strPath + @"\Symbol.xml");
            if (dt != null)
            {
                foreach (DataRow curR in dt.Rows)
                {
                    int ID = Convert.ToInt32(curR[0]);
                    int sylID = Convert.ToInt32(curR[1]);
                    string LayName = Convert.ToString(curR[2]);
                    double size = Convert.ToDouble(curR[3]);
                    string FillColor = Convert.ToString(curR[4]);
                    string BorderColor = Convert.ToString(curR[5]);

                    Symbol s = new Symbol(ID, sylID, LayName, size, FillColor, BorderColor);

                    symbolList.Add(s);
                }
            }

            #endregion

            #region 创建列表
            int pCount = 0;
            int lCount = 0;
            int aCount = 0;
            foreach (IFeatureLayer curLyr in lyrList)
            {
                if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPoint)
                {
                    pCount++;

                }
                else if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                {
                    lCount++;
                }
                else if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                {
                    aCount++;
                }
            }
            if (pCount > 0)
            {
                PointList = new List<PointObject>();
            }
            if (lCount > 0)
            {
                PolylineList = new List<PolylineObject>();
                ConNodeList = new List<ConNode>();
            }
            if (aCount > 0)
            {
                PolygonList = new List<PolygonObject>();
            }
            TriNodeList = new List<TriNode>();
            #endregion

            int vextexID = 0;
            double sylSize = 0;
            int pID = 0;
            int plID = 0;
            int ppID = 0;

            foreach (IFeatureLayer curLyr in lyrList)
            {
                IFeatureCursor cursor = null;
                IFeature curFeature = null;
                IGeometry shp = null;
                Symbol curSyl = null;

                curSyl = Symbol.GetSymbolbyLyrName(curLyr.Name, symbolList);
                if (curSyl != null)
                    sylSize = curSyl.Size;
                if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                {

                    #region 线要素
                    cursor = curLyr.Search(null, false);
                    curFeature = null;
                    shp = null;
                    while ((curFeature = cursor.NextFeature()) != null)
                    {
                        shp = curFeature.Shape;
                        IPolyline polyline = null;
                        IGeometryCollection pathSet = null;
                        int indexofType = curFeature.Fields.FindField("Type");
                        int typeID = 1;
                        if (indexofType != -1 && curFeature.get_Value(indexofType) != null)
                        {
                            typeID = (Int16)(curFeature.get_Value(indexofType));
                        }
                        //几何图形
                        if (shp.GeometryType == esriGeometryType.esriGeometryPolyline)
                        {
                            polyline = shp as IPolyline;
                            //plID = curFeature.OID;
                            pathSet = polyline as IGeometryCollection;
                            int count = pathSet.GeometryCount;
                            //Path对象
                            IPath curPath = null;
                            for (int i = 0; i < count; i++)
                            {
                                PolylineObject curPL = null;                      //当前道路
                                TriNode curVextex = null;                  //当前关联点
                                List<TriNode> curPointList = new List<TriNode>();
                                double curX;
                                double curY;

                                curPath = pathSet.get_Geometry(i) as IPath;
                                IPointCollection pointSet = curPath as IPointCollection;
                                int pointCount = pointSet.PointCount;
                                if (pointCount >= 2)
                                {
                                    curX = pointSet.get_Point(0).X;
                                    curY = pointSet.get_Point(0).Y;
                                    TriNode cNode = ConNode.GetContainNode(ConNodeList, TriNodeList, curX, curY);
                                    if (cNode == null)   //该关联点还未加入的情况
                                    {
                                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                                        TriNodeList.Add(curVextex);
                                        ConNode curNode = new ConNode(vextexID, 0.2f, curVextex);
                                        ConNodeList.Add(curNode);
                                        curPointList.Add(curVextex);
                                        vextexID++;
                                    }
                                    else //该关联点已经加入的情况
                                    {
                                        curPointList.Add(cNode);
                                        cNode.TagValue = -1;
                                        cNode.FeatureType = FeatureType.ConnNode;
                                    }
                                    //加入中间顶点
                                    for (int j = 1; j < pointCount - 1; j++)
                                    {
                                        curX = pointSet.get_Point(j).X;
                                        curY = pointSet.get_Point(j).Y;
                                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                                        TriNodeList.Add(curVextex);
                                        curPointList.Add(curVextex);
                                        vextexID++;
                                    }
                                    //加入终点
                                    curX = pointSet.get_Point(pointCount - 1).X;
                                    curY = pointSet.get_Point(pointCount - 1).Y;
                                    cNode = ConNode.GetContainNode(ConNodeList, TriNodeList, curX, curY);
                                    if (cNode == null)   //该关联点还未加入的情况
                                    {
                                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                                        TriNodeList.Add(curVextex);

                                        ConNode curNode = new ConNode(vextexID, -1f, curVextex);
                                        ConNodeList.Add(curNode);
                                        curPointList.Add(curVextex);
                                        vextexID++;
                                    }
                                    else //该关联点已经加入的情况
                                    {
                                        curPointList.Add(cNode);
                                        cNode.TagValue = -1;
                                        cNode.FeatureType = FeatureType.ConnNode;
                                    }

                                    //添加起点
                                    curPL = new PolylineObject(plID, curPointList, sylSize);
                                    curPL.TypeID = typeID;
                                    PolylineList.Add(curPL);
                                    plID++;
                                }
                            }
                        }
                    }

                    #endregion
                }
            }

            //this.InterpretatePoint(2);

            vextexID = this.TriNodeList.Count;

            foreach (IFeatureLayer curLyr in lyrList)
            {
                IFeatureCursor cursor = null;
                IFeature curFeature = null;
                IGeometry shp = null;
                Symbol curSyl = null;

                curSyl = Symbol.GetSymbolbyLyrName(curLyr.Name, symbolList);
                if (curSyl != null)
                    sylSize = curSyl.Size;
                if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                {
                    #region 面要素
                    cursor = curLyr.Search(null, false);
                    curFeature = null;
                    shp = null;
                    while ((curFeature = cursor.NextFeature()) != null)
                    {
                        shp = curFeature.Shape;
                        IPolygon polygon = null;
                        IGeometryCollection pathSet = null;
                        //几何图形
                        if (shp.GeometryType == esriGeometryType.esriGeometryPolygon)
                        {
                            // ppID = curFeature.OID;
                            polygon = shp as IPolygon;
                            pathSet = polygon as IGeometryCollection;
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
                                        curVextex = new TriNode(curX, curY, vextexID, ppID, FeatureType.PolygonType);
                                        TriNodeList.Add(curVextex);
                                        curPointList.Add(curVextex);
                                        vextexID++;
                                    }

                                    //添加起点
                                    curPP = new PolygonObject(ppID, curPointList);
                                    curPP.SylWidth = sylSize;
                                    this.PolygonList.Add(curPP);
                                    ppID++;
                                }
                            }
                        }
                    }
                    #endregion
                }
            }

        }


        public void ReadDateFrmEsriLyrswithIDField()
        {

            if (lyrList == null || lyrList.Count == 0)
            {
                return;
            }

            #region 读符号文件
            List<Symbol> symbolList = new List<Symbol>();
            //读文件========
            DataTable dt = TestIO.ReadData(strPath + @"\Symbol.xml");
            if (dt != null)
            {
                foreach (DataRow curR in dt.Rows)
                {
                    int ID = Convert.ToInt32(curR[0]);
                    int sylID = Convert.ToInt32(curR[1]);
                    string LayName = Convert.ToString(curR[2]);
                    double size = Convert.ToDouble(curR[3]);
                    string FillColor = Convert.ToString(curR[4]);
                    string BorderColor = Convert.ToString(curR[5]);

                    Symbol s = new Symbol(ID, sylID, LayName, size, FillColor, BorderColor);

                    symbolList.Add(s);
                }
            }

            #endregion

            #region 创建列表
            int pCount = 0;
            int lCount = 0;
            int aCount = 0;
            foreach (IFeatureLayer curLyr in lyrList)
            {
                if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPoint)
                {
                    pCount++;

                }
                else if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                {
                    lCount++;
                }
                else if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                {
                    aCount++;
                }
            }
            if (pCount > 0)
            {
                PointList = new List<PointObject>();
            }
            if (lCount > 0)
            {
                PolylineList = new List<PolylineObject>();
                ConNodeList = new List<ConNode>();
            }
            if (aCount > 0)
            {
                PolygonList = new List<PolygonObject>();
            }
            TriNodeList = new List<TriNode>();
            #endregion

            int vextexID = 0;
            double sylSize = 0;
            int pID = 0;
            int plID = 0;
            int ppID = 0;

            foreach (IFeatureLayer curLyr in lyrList)
            {
                IFeatureCursor cursor = null;
                IFeature curFeature = null;
                IGeometry shp = null;
                Symbol curSyl = null;

                curSyl = Symbol.GetSymbolbyLyrName(curLyr.Name, symbolList);
                if (curSyl != null)
                    sylSize = curSyl.Size;
                switch (curLyr.FeatureClass.ShapeType)
                {

                    case esriGeometryType.esriGeometryPoint:
                        {


                            #region 点要素
                            //点要素
                            cursor = curLyr.Search(null, false);

                            while ((curFeature = cursor.NextFeature()) != null)
                            {
                                shp = curFeature.Shape;
                                //ID
                                int index=curFeature.Fields.FindField("ID");
                                pID = (int)(curFeature.get_Value(index));
                                IPoint point = null;
                                //几何图形
                                if (shp.GeometryType == esriGeometryType.esriGeometryPoint)
                                {
                                    point = shp as IPoint;
                                    PointObject curPoint = null;           //当前道路
                                    TriNode curVextex = null;                  //当前关联点
                                    double curX;
                                    double curY;

                                    curX = point.X;
                                    curY = point.Y;
                                    curVextex = new TriNode((float)curX, (float)curY, vextexID, pID, FeatureType.PointType);
                                    curPoint = new PointObject(pID, curVextex);
                                    curPoint.SylWidth = sylSize;
                                    TriNodeList.Add(curVextex);
                                    PointList.Add(curPoint);
                                    vextexID++;
                                    //pID++;
                                }

                            }
                            #endregion
                            break;
                        }
                    case esriGeometryType.esriGeometryPolyline:
                        {

                            #region 线要素
                            cursor = curLyr.Search(null, false);
                            curFeature = null;
                            shp = null;
                            while ((curFeature = cursor.NextFeature()) != null)
                            {
                                shp = curFeature.Shape;
                                IPolyline polyline = null;
                                IGeometryCollection pathSet = null;
                                int indexofType = curFeature.Fields.FindField("Type");
                                int typeID = 1;
                                if (curFeature.get_Value(indexofType) != null)
                                {
                                    typeID = (Int16)(curFeature.get_Value(indexofType));
                                }
                                //几何图形
                                if (shp.GeometryType == esriGeometryType.esriGeometryPolyline)
                                {
                                    polyline = shp as IPolyline;
                                    //ID
                                    int index = curFeature.Fields.FindField("ID");
                                    pID = (int)(curFeature.get_Value(index));

                                    pathSet = polyline as IGeometryCollection;
                                    int count = pathSet.GeometryCount;
                                    //Path对象
                                    IPath curPath = null;
                                    for (int i = 0; i < count; i++)
                                    {
                                        PolylineObject curPL = null;                      //当前道路
                                        TriNode curVextex = null;                  //当前关联点
                                        List<TriNode> curPointList = new List<TriNode>();
                                        double curX;
                                        double curY;

                                        curPath = pathSet.get_Geometry(i) as IPath;
                                        IPointCollection pointSet = curPath as IPointCollection;
                                        int pointCount = pointSet.PointCount;
                                        if (pointCount >= 2)
                                        {
                                            curX = pointSet.get_Point(0).X;
                                            curY = pointSet.get_Point(0).Y;
                                            TriNode cNode = ConNode.GetContainNode(ConNodeList, TriNodeList, curX, curY);
                                            if (cNode == null)   //该关联点还未加入的情况
                                            {
                                                curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                                                TriNodeList.Add(curVextex);
                                                ConNode curNode = new ConNode(vextexID, 0.2f, curVextex);
                                                ConNodeList.Add(curNode);
                                                curPointList.Add(curVextex);
                                                vextexID++;
                                            }
                                            else //该关联点已经加入的情况
                                            {
                                                curPointList.Add(cNode);
                                                cNode.TagValue = -1;
                                                cNode.FeatureType = FeatureType.ConnNode;
                                            }
                                            //加入中间顶点
                                            for (int j = 1; j < pointCount - 1; j++)
                                            {
                                                curX = pointSet.get_Point(j).X;
                                                curY = pointSet.get_Point(j).Y;
                                                curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                                                TriNodeList.Add(curVextex);
                                                curPointList.Add(curVextex);
                                                vextexID++;
                                            }
                                            //加入终点
                                            curX = pointSet.get_Point(pointCount - 1).X;
                                            curY = pointSet.get_Point(pointCount - 1).Y;
                                            cNode = ConNode.GetContainNode(ConNodeList, TriNodeList, curX, curY);
                                            if (cNode == null)   //该关联点还未加入的情况
                                            {
                                                curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                                                TriNodeList.Add(curVextex);

                                                ConNode curNode = new ConNode(vextexID, -1f, curVextex);
                                                ConNodeList.Add(curNode);
                                                curPointList.Add(curVextex);
                                                vextexID++;
                                            }
                                            else //该关联点已经加入的情况
                                            {
                                                curPointList.Add(cNode);
                                                cNode.TagValue = -1;
                                                cNode.FeatureType = FeatureType.ConnNode;
                                            }

                                            //添加起点
                                            curPL = new PolylineObject(plID, curPointList, sylSize);
                                            curPL.TypeID = typeID;
                                            PolylineList.Add(curPL);
                                            //plID++;
                                        }
                                    }
                                }
                            }

                            #endregion
                            break;
                        }

                    case esriGeometryType.esriGeometryPolygon:
                        {

                            #region 面要素
                            cursor = curLyr.Search(null, false);
                            curFeature = null;
                            shp = null;
                            while ((curFeature = cursor.NextFeature()) != null)
                            {
                                shp = curFeature.Shape;
                                IPolygon polygon = null;
                                IGeometryCollection pathSet = null;
                                //几何图形
                                if (shp.GeometryType == esriGeometryType.esriGeometryPolygon)
                                {
                                    //ID
                                    int index = curFeature.Fields.FindField("ID");
                                    pID = (int)(curFeature.get_Value(index));

                                    polygon = shp as IPolygon;
                                    pathSet = polygon as IGeometryCollection;
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
                                                curVextex = new TriNode(curX, curY, vextexID, ppID, FeatureType.PolygonType);
                                                TriNodeList.Add(curVextex);
                                                curPointList.Add(curVextex);
                                                vextexID++;
                                            }

                                            //添加起点
                                            curPP = new PolygonObject(ppID, curPointList);
                                            curPP.SylWidth = sylSize;
                                            this.PolygonList.Add(curPP);
                                           // ppID++;
                                        }
                                    }
                                }
                            }
                            #endregion
                            break;
                        }
                }
            }
        }

        /// <summary>
        /// 加密顶点
        /// </summary>
        /// <param name="k">加密系数，平均距离的多少倍</param>
        public void InterpretatePoint(int k)
        {
            AuxStructureLib.Interpretation Inter = new AuxStructureLib.Interpretation(this.PolylineList, this.PolygonList, this.TriNodeList);
            Inter.Interpretate(k);
            this.PolylineList = Inter.PLList;
            this.PolygonList = Inter.PPList;
        }

        /// <summary>
        /// 获取地图对象
        /// </summary>
        /// <param name="ID">ID</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public MapObject GetObjectbyID(int ID, FeatureType type)
        {
            if (type == FeatureType.PointType)
            {
                return PointObject.GetPPbyID(this.PointList, ID);
            }
            else if (type == FeatureType.PolylineType)
            {
                return PolylineObject.GetPLbyID(this.PolylineList, ID);
            }
            else if (type == FeatureType.PolygonType)
            {
                return PolygonObject.GetPPbyID(this.PolygonList, ID);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 判断是否约束边，如果是返回TagValue 和 类型FeatureType
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        public int GetConsEdge(TriNode p1, TriNode p2, out FeatureType featureType)
        {
            featureType=FeatureType.Unknown;
            //线
            foreach (PolylineObject polyline in this.PolylineList)
            {
                for (int i = 0; i < polyline.PointList.Count - 1; i++)
                {
                    if((p1== polyline.PointList[i]&& p2==polyline.PointList[i+1])
                        ||(p2== polyline.PointList[i]&& p1==polyline.PointList[i+1]))
                    {
                        featureType=FeatureType.PolylineType;
                        return polyline.ID;
                       
                    }
                }
            }
            //面
            foreach (PolygonObject polygon in this.PolygonList)
            {
                for (int i = 0; i < polygon.PointList.Count - 1; i++)
                {
                    if((p1== polygon.PointList[i]&& p2==polygon.PointList[i+1])
                        ||(p2== polygon.PointList[i]&& p1==polygon.PointList[i+1]))
                    {
                        featureType=FeatureType.PolygonType;
                        return polygon.ID;
                       
                    }
                }
                 if((p1== polygon.PointList[polygon.PointList.Count - 1]&& p2==polygon.PointList[0])
                        ||(p2== polygon.PointList[0]&& p1==polygon.PointList[polygon.PointList.Count - 1]))
                    {
                        featureType=FeatureType.PolygonType;
                        return polygon.ID;
                       
                    }
            }

            return -1;

        }
        /// <summary>
        /// 将结果写入SHP
        /// </summary>
        /// <param name="filePath">目录</param>
        /// <param name="prj">投影</param>
        public void WriteResult2Shp(string filePath, ISpatialReference prj)
        {
            if(this.TriNodeList!=null&&this.TriNodeList.Count>0)
            {
                TriNode.Create_WriteVetex2Shp(filePath, @"Vertices", this.TriNodeList, prj);
            }
            if (this.PointList != null && this.PointList.Count > 0)
            {
                this.Create_WritePointObject2Shp(filePath, @"PointObjecrt", prj);
            }
            if (this.PolylineList != null && this.PolylineList.Count > 0)
            {
                this.Create_WritePolylineObject2Shp(filePath, @"PolylineObjecrt", prj);
            }
            if (this.PolygonList != null && this.PolygonList.Count > 0)
            {
                this.Create_WritePolygonObject2Shp(filePath, @"PolygonObjecrt", prj);
            }
        }

        /// <summary>
        /// 将结果写入SHP
        /// </summary>
        /// <param name="filePath">目录</param>
        /// <param name="prj">投影</param>
        public void WriteResult2Shp(string filePath, string name,ISpatialReference prj)
        {
            if (this.TriNodeList != null && this.TriNodeList.Count > 0)
            {
                TriNode.Create_WriteVetex2Shp(filePath, @name+"Vertices", this.TriNodeList, prj);
            }
            if (this.PointList != null && this.PointList.Count > 0)
            {
                this.Create_WritePointObject2Shp(filePath, @name+"PointObject"+name, prj);
            }
            if (this.PolylineList != null && this.PolylineList.Count > 0)
            {
                this.Create_WritePolylineObject2Shp(filePath, @name+"PolylineObject"+name, prj);
            }
            if (this.PolygonList != null && this.PolygonList.Count > 0)
            {
                this.Create_WritePolygonObject2Shp(filePath, @name+"PolygonObject"+name, prj);
            }
        }
        /// <summary>
        /// 创建要素
        /// </summary>
        public void Export2FeatureClasses(out IFeatureClass pPointFeatClass, 
            out IFeatureClass pPolylineFeatClass, 
            out IFeatureClass pPolygonFeatClass)
        {
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            #region 点
            pPointFeatClass = new FeatureCacheClass() as IFeatureClass;
            IFeatureClassWrite pPointFeatClassW = pPointFeatClass as IFeatureClassWrite;
            // List<Skeleton_Segment> Skeleton_SegmentList = ske.Skeleton_SegmentList;
            if (this.PointList != null && this.PointList.Count != 0)
            {
                int n = this.PointList.Count;
                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pPointFeatClass.CreateFeature();
                    IGeometry shp = new PointClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    //IPoint curResultPoint = null;
                    TriNode curPoint = null;
                    if (TriNodeList[i] == null)
                        continue;
                    curPoint = this.PointList[i].Point; ;
                    ((PointClass)shp).PutCoords(curPoint.X, curPoint.Y);
                    feature.Shape = shp;
                    feature.set_Value(2, this.PointList[i].ID);
                    feature.Store();//保存IFeature对象  
                    pPointFeatClassW.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                 
                }
            }
            #endregion

            #region 线
            pPolylineFeatClass = new FeatureCacheClass() as IFeatureClass;
            IFeatureClassWrite pPolylineFeatClassW= pPolylineFeatClass as IFeatureClassWrite;
            // List<Skeleton_Segment> Skeleton_SegmentList = ske.Skeleton_SegmentList;
            if (this.PolylineList != null && this.PolylineList.Count != 0)
            {

                int n = this.PolylineList.Count;
                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pPolylineFeatClass.CreateFeature();
                    IGeometry shp = new PolylineClass();
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = null;
                    TriNode curPoint = null;
                    if (this.PolylineList[i] == null)
                        continue;
                    int m = this.PolylineList[i].PointList.Count;

                    for (int k = 0; k < m; k++)
                    {
                        curPoint = this.PolylineList[i].PointList[k];
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    }
                    feature.Shape = shp;
                    feature.set_Value(2, this.PolylineList[i].ID);//编号 


                    feature.Store();//保存IFeature对象  
                    pPolylineFeatClassW.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }
            }
            #endregion

            #region 面
            pPolygonFeatClass = new FeatureCacheClass() as IFeatureClass;
            IFeatureClassWrite   pPolygonFeatClassW=pPolygonFeatClass as IFeatureClassWrite;
            // List<Skeleton_Segment> Skeleton_SegmentList = ske.Skeleton_SegmentList;
            if (this.PolygonList != null && this.PolygonList.Count != 0)
            {

                int n = this.PolygonList.Count;
                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pPolygonFeatClass.CreateFeature();
                    IGeometry shp = new PolygonClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = null;
                    TriNode curPoint = null;
                    if (this.PolygonList[i] == null)
                        continue;
                    int m = this.PolygonList[i].PointList.Count;

                    for (int k = 0; k < m; k++)
                    {
                        curPoint = this.PolygonList[i].PointList[k];
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    }
                    curPoint = this.PolygonList[i].PointList[0];
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    feature.Shape = shp;
                    feature.Store();//保存IFeature对象  
                    pPolygonFeatClassW.WriteFeature(feature);
                }
            }
            #endregion
        }

        /// <summary>
        /// 将面写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="Skeleton_SegmentList">线列表</param>
        public void Create_WritePolygonObject2Shp(string filePath, string fileName, ISpatialReference prj)
        {
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);
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

            //ISpatialReferenceFactory ispfac = new SpatialReferenceEnvironmentClass();
            //IProjectedCoordinateSystem iprcoorsys = ispfac.CreateProjectedCoordinateSystem((int)prj);
            //ISpatialReference pSpatialReference = iprcoorsys as ISpatialReference;

            IGeometryDef pGeomDef = new GeometryDefClass();
            IGeometryDefEdit pGeomDefEdit = pGeomDef as IGeometryDefEdit;
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolygon;
            pGeomDefEdit.SpatialReference_2 = prj;
            pFieldEdit.GeometryDef_2 = pGeomDef;
            pFieldsEdit.AddField(pField);
            #endregion

            #region 创建属性字段
            //属性字段1
            IField pField1;
            IFieldEdit pFieldEdit1;
            pField1 = new FieldClass();
            pFieldEdit1 = pField1 as IFieldEdit;
            pFieldEdit1.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit1.Name_2 = "ID";
            pFieldEdit1.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField1);
            #endregion

            #region 创建属性字段Value
            //属性字段1
            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit2.Name_2 = "Value";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeDouble;
            pFieldsEdit.AddField(pField2);
            #endregion

            #region 创建属性字段Name
            //属性字段1
            IField pField3;
            IFieldEdit pFieldEdit3;
            pField3 = new FieldClass();
            pFieldEdit3 = pField3 as IFieldEdit;
            pFieldEdit3.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit3.Name_2 = "Name";
            pFieldEdit3.Type_2 = esriFieldType.esriFieldTypeString;
            pFieldsEdit.AddField(pField3);
            #endregion

            #region 创建要素类
            IFeatureClass pFeatClass;
            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
            #endregion
            #endregion

            #region 向线层添加线要素

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

                int n = this.PolygonList.Count;
                // List<Skeleton_Segment> Skeleton_SegmentList = ske.Skeleton_SegmentList;
                if (n == 0)
                    return;

                for (int i = 0; i < n; i++)
                {


                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PolygonClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = null;
                    TriNode curPoint = null;
                    if (this.PolygonList[i] == null)
                        continue;
                    int m = this.PolygonList[i].PointList.Count;

                    for (int k = 0; k < m; k++)
                    {
                        curPoint = this.PolygonList[i].PointList[k];
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    }
                    curPoint = this.PolygonList[i].PointList[0];
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    feature.Shape = shp;
                    feature.set_Value(2, this.PolygonList[i].ID);//编号 
                    feature.set_Value(3, this.PolygonList[i].Value);//数值
                    feature.set_Value(4, this.PolygonList[i].Name);//数值

                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }

                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("异常信息" + ex.Message);
            }
            #endregion
        }  

        /// <summary>
        /// 将线写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="Skeleton_SegmentList">线列表</param>
        public void Create_WritePolylineObject2Shp(string filePath, string fileName, ISpatialReference prj)
        {
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);
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

            //ISpatialReferenceFactory ispfac = new SpatialReferenceEnvironmentClass();
            //IProjectedCoordinateSystem iprcoorsys = ispfac.CreateProjectedCoordinateSystem((int)prj);
            //ISpatialReference pSpatialReference = iprcoorsys as ISpatialReference;

            IGeometryDef pGeomDef = new GeometryDefClass();
            IGeometryDefEdit pGeomDefEdit = pGeomDef as IGeometryDefEdit;
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolyline;
            pGeomDefEdit.SpatialReference_2 = prj;
            pFieldEdit.GeometryDef_2 = pGeomDef;
            pFieldsEdit.AddField(pField);
            #endregion

            #region 创建属性字段
            //属性字段1
            IField pField1;
            IFieldEdit pFieldEdit1;
            pField1 = new FieldClass();
            pFieldEdit1 = pField1 as IFieldEdit;
            pFieldEdit1.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit1.Name_2 = "ID";
            pFieldEdit1.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField1);
            #endregion

            #region 创建要素类
            IFeatureClass pFeatClass;
            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
            #endregion
            #endregion

            #region 向线层添加线要素

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

                int n = this.PolylineList.Count;
                // List<Skeleton_Segment> Skeleton_SegmentList = ske.Skeleton_SegmentList;
                if (n == 0)
                    return;

                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PolylineClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = null;
                    TriNode curPoint = null;
                    if (this.PolylineList[i] == null)
                        continue;
                    int m = this.PolylineList[i].PointList.Count;

                    for (int k = 0; k < m; k++)
                    {
                        curPoint = this.PolylineList[i].PointList[k];
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    }
                    feature.Shape = shp;
                    feature.set_Value(2, this.PolylineList[i].ID);//编号 


                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }

                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                 MessageBox.Show("异常信息" + ex.Message);
            }
            #endregion
        }


        /// <summary>
        /// 将点写入Shp
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <param name="TriEdgeList"></param>
        /// <param name="prj"></param>
        public void Create_WritePointObject2Shp(string filePath, string fileName, ISpatialReference prj)
        {
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);
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

            //ISpatialReferenceFactory ispfac = new SpatialReferenceEnvironmentClass();
            //IProjectedCoordinateSystem iprcoorsys = ispfac.CreateProjectedCoordinateSystem((int)prj);
            //ISpatialReference pSpatialReference = iprcoorsys as ISpatialReference;

            IGeometryDef pGeomDef = new GeometryDefClass();
            IGeometryDefEdit pGeomDefEdit = pGeomDef as IGeometryDefEdit;
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPoint;
            pGeomDefEdit.SpatialReference_2 = prj;
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
            #endregion

            #region 创建要素类
            IFeatureClass pFeatClass;
            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
            #endregion
            #endregion

            #region 添加要素

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

                int n = this.PointList.Count;
                if (n == 0)
                    return;

                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PointClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    //IPoint curResultPoint = null;
                    TriNode curPoint = null;
                    //if (TriNodeList[i] == null)
                    //    continue;

                    curPoint = this.PointList[i].Point; ;
                    ((PointClass)shp).PutCoords(curPoint.X, curPoint.Y);

                    feature.Shape = shp;
                    feature.set_Value(2, this.PointList[i].ID);
;

                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }

                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("异常信息" + ex.Message);
            }
            #endregion
        }

        /// <summary>
        /// 输出冲突到TXT文件
        /// </summary>
        /// <param name="strPath"></param>
        /// <param name="iteraID"></param>
        public void OutputConflictCount(string strPath,string fileName)
        {
            DataSet ds = new DataSet();
            //创建一个表
            DataTable tableforce = new DataTable();
            tableforce.TableName = "ConflictsCount";
            tableforce.Columns.Add("ID", typeof(int));
            tableforce.Columns.Add("Type", typeof(string));
            tableforce.Columns.Add("Count", typeof(int));
            if (this.PointList != null && PointList.Count != 0)
            {
                foreach (PointObject p in this.PointList)
                {
                    DataRow dr = tableforce.NewRow();
                    dr[0] = p.ID;
                    dr[1] = "Point";
                    dr[2] = p.ConflictCount;
                    tableforce.Rows.Add(dr);
                }
            }
            if (this.PointList != null && PointList.Count != 0)
            {
                foreach (PointObject p in this.PointList)
                {
                    DataRow dr = tableforce.NewRow();
                    dr[0] = p.ID;
                    dr[1] = "Point";
                    dr[2] = p.ConflictCount;
                    tableforce.Rows.Add(dr);
                }
            }
            if (this.PolylineList != null && PolylineList.Count != 0)
            {
                foreach (PolylineObject l in this.PolylineList)
                {
                    DataRow dr = tableforce.NewRow();
                    dr[0] = l.ID;
                    dr[1] = "Line";
                    dr[2] = l.ConflictCount;
                    tableforce.Rows.Add(dr);
                }
            }
            if (this.PolygonList != null && PolygonList.Count != 0)
            {
                foreach (PolygonObject pp in this.PolygonList)
                {
                    DataRow dr = tableforce.NewRow();
                    dr[0] = pp.ID;
                    dr[1] = "Polygon";
                    dr[2] = pp.ConflictCount;
                    tableforce.Rows.Add(dr);
                }
            }
           
            TXTHelper.ExportToTxt(tableforce, strPath + @"\"+fileName);
        }
        /// <summary>
        /// 提取在缓冲区多边形内部的额子网
        /// </summary>
        /// <param name="bufferArea">缓冲区</param>
        public void GetSubNetWorkbyBufferArea(IPolygon bufferArea)
        {
            IRelationalOperator iro = bufferArea as IRelationalOperator;
            foreach(PolylineObject l in this.PolylineList)
            {
                List<TriNode> rList = new List<TriNode>();

                foreach (TriNode p in l.PointList)
                {
                    IPoint point = new PointClass();
                    point.PutCoords(p.X, p.Y);
                    if (!iro.Contains(point))
                    {
                        rList.Add(p);
                    }
                }
                foreach (TriNode p in rList)
                {
                    l.PointList.Remove(p);
                    this.TriNodeList.Remove(p);
                }
            }
        }

        /// <summary>
        /// 提取在缓冲区多边形内部的额子网
        /// </summary>
        /// <param name="bufferArea">缓冲区</param>
        public SMap GetSubNetWorkbyBufferAreaClip(IPolygon bufferArea)
        {
            SMap map = new SMap();
            map.PolylineList = new List<PolylineObject>();
            map.ConNodeList = new List<ConNode>();
            map.TriNodeList = new List<TriNode>();
            int vextexID = 0;
            int plID = 0;

            List<PolylineObject> esriPolylineList = new List<PolylineObject>();
            int id = 0;
            foreach (PolylineObject l in this.PolylineList)
            {
                int typeID = l.TypeID;
                //if (l.ID == -200)
                //{

                //    int error = 0;
                //}
                double sylSize = l.SylWidth;
                IPolyline esriPolyline = l.ToEsriPolyline();
                ITopologicalOperator ito = esriPolyline as ITopologicalOperator;
                IGeometry geo = ito.Intersect(bufferArea, esriGeometryDimension.esriGeometry1Dimension);
                if (geo != null)
                {
                    IGeometryCollection pathSet = geo as IGeometryCollection;
                    if (pathSet.GeometryCount > 0)
                    {
                        if (l.ID == -200)
                        {
                            this.ProxiEdge2PolylineObjects(ref vextexID, ref plID, map, l, typeID, sylSize);
                        }
                        else
                        {
                            Paths2PolylineObjects(ref vextexID, ref plID, map, pathSet, typeID, sylSize);
                        }
                    }
                }
            }
            DetermineBoundaryPoints(bufferArea);
            return map;
        }

        /// <summary>
        /// 提取在缓冲区多边形内部的额子网
        /// </summary>
        /// <param name="bufferArea">缓冲区</param>
        public SMap GetSubNetWorkbyBufferAreaClip(IPolygon bufferArea,SMap oMap)
        {
            SMap map = new SMap();
            map.PolylineList = new List<PolylineObject>();
            map.ConNodeList = new List<ConNode>();
            map.TriNodeList = new List<TriNode>();
            int vextexID = 0;
            int plID = 0;

            List<PolylineObject> esriPolylineList = new List<PolylineObject>();
            int id = 0;
            foreach (PolylineObject l in this.PolylineList)
            {
                int typeID = l.TypeID;
                

                double sylSize = l.SylWidth;
                IPolyline esriPolyline = l.ToEsriPolyline();
                ITopologicalOperator ito = esriPolyline as ITopologicalOperator;
                IGeometry geo = ito.Intersect(bufferArea, esriGeometryDimension.esriGeometry1Dimension);
                if (geo != null)
                {

                    IGeometryCollection pathSet = geo as IGeometryCollection;
                    if (pathSet.GeometryCount > 0)
                    {
                        if (l.ID == -200)
                        {
                            ProxiEdge2PolylineObjects(ref vextexID, ref plID, map, oMap, l, typeID, sylSize);
                        }
                        else
                        {
                            Paths2PolylineObjects(ref vextexID, ref plID, map, oMap, pathSet, typeID, sylSize);
                        }
                    }
                }
            }
          //  DetermineBoundaryPoints(bufferArea);
            return map;
        }

        /// <summary>
        /// 从PathSet中读取坐标构建道路网地图
        /// </summary>
        public void ProxiEdge2PolylineObjects(
            ref int vextexID,
            ref int plID, 
            SMap map,
            PolylineObject l, 
            int typeID, 
            double sylSize)
        {
            int count = l.PointList.Count;
            if (count <= 1)
                return;
     
                PolylineObject curPL = null; //当前道路
                TriNode curVextex = null; //当前关联点
                List<TriNode> curPointList = new List<TriNode>();
                double curX;
                double curY;


       
                    curX = l.PointList[0].X;
                    curY =  l.PointList[0].Y;
                    TriNode cNode = ConNode.GetContainNode(map.ConNodeList, TriNodeList, curX, curY);
                    if (cNode == null)   //该关联点还未加入的情况
                    {
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                        map.TriNodeList.Add(curVextex);
                        ConNode curNode = new ConNode(vextexID, 0.2f, curVextex);
                        map.ConNodeList.Add(curNode);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    else //该关联点已经加入的情况
                    {
                        curPointList.Add(cNode);
                        cNode.TagValue = -1;
                        cNode.FeatureType = FeatureType.ConnNode;
                    }
                    //加入中间顶点
                    for (int j = 1; j < l.PointList.Count - 1; j++)
                    {
                        curX = l.PointList[j].X;
                        curY = l.PointList[j].Y;
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                        map.TriNodeList.Add(curVextex);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    //加入终点
                    curX = l.PointList[l.PointList.Count- 1].X;
                     curY = l.PointList[l.PointList.Count- 1].Y;
                    cNode = ConNode.GetContainNode(map.ConNodeList, TriNodeList, curX, curY);
                    if (cNode == null)   //该关联点还未加入的情况
                    {
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                        map.TriNodeList.Add(curVextex);

                        ConNode curNode = new ConNode(vextexID,-1f, curVextex);
                        map.ConNodeList.Add(curNode);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    else //该关联点已经加入的情况
                    {
                        curPointList.Add(cNode);
                        cNode.TagValue = -1;
                        cNode.FeatureType = FeatureType.ConnNode;
                    }

                    //添加起点
                    curPL = new PolylineObject(plID, curPointList, sylSize);
                    curPL.TypeID = typeID;
                    map.PolylineList.Add(curPL);
                    plID++;

        }


        /// <summary>
        /// 从PathSet中读取坐标构建道路网地图
        /// </summary>
        public void Paths2PolylineObjects(
            ref int vextexID,
            ref int plID,
            SMap map,
            IGeometryCollection pathSet,
            int typeID,
            double sylSize)
        {
            int count = pathSet.GeometryCount;
            //Path对象
            IPath curPath = null;
            for (int i = 0; i < count; i++)
            {
                PolylineObject curPL = null; //当前道路
                TriNode curVextex = null; //当前关联点
                List<TriNode> curPointList = new List<TriNode>();
                double curX;
                double curY;

                curPath = pathSet.get_Geometry(i) as IPath;
                IPointCollection pointSet = curPath as IPointCollection;
                int pointCount = pointSet.PointCount;
                if (pointCount >= 2)
                {
                    curX = pointSet.get_Point(0).X;
                    curY = pointSet.get_Point(0).Y;
                    TriNode cNode = ConNode.GetContainNode(map.ConNodeList, TriNodeList, curX, curY);
                    if (cNode == null)   //该关联点还未加入的情况
                    {
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                        map.TriNodeList.Add(curVextex);
                        ConNode curNode = new ConNode(vextexID, 0.2f, curVextex);
                        map.ConNodeList.Add(curNode);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    else //该关联点已经加入的情况
                    {
                        curPointList.Add(cNode);
                        cNode.TagValue = -1;
                        cNode.FeatureType = FeatureType.ConnNode;
                    }
                    //加入中间顶点
                    for (int j = 1; j < pointCount - 1; j++)
                    {
                        curX = pointSet.get_Point(j).X;
                        curY = pointSet.get_Point(j).Y;
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                        map.TriNodeList.Add(curVextex);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    //加入终点
                    curX = pointSet.get_Point(pointCount - 1).X;
                    curY = pointSet.get_Point(pointCount - 1).Y;
                    cNode = ConNode.GetContainNode(map.ConNodeList, TriNodeList, curX, curY);
                    if (cNode == null)   //该关联点还未加入的情况
                    {
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                        map.TriNodeList.Add(curVextex);

                        ConNode curNode = new ConNode(vextexID, -1f, curVextex);
                        map.ConNodeList.Add(curNode);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    else //该关联点已经加入的情况
                    {
                        curPointList.Add(cNode);
                        cNode.TagValue = -1;
                        cNode.FeatureType = FeatureType.ConnNode;
                    }

                    //添加起点
                    curPL = new PolylineObject(plID, curPointList, sylSize);
                    curPL.TypeID = typeID;
                    map.PolylineList.Add(curPL);
                    plID++;
                }
            }
        }

        /// <summary>
        /// 从PathSet中读取坐标构建道路网地图
        /// </summary>
        public void Paths2PolylineObjects(
            ref int vextexID,
            ref int plID,
            SMap map,
            SMap oMap,
            IGeometryCollection pathSet,
            int typeID,
            double sylSize)
        {
            int count = pathSet.GeometryCount;
            //Path对象
            IPath curPath = null;
            for (int i = 0; i < count; i++)
            {
                PolylineObject curPL = null; //当前道路
                TriNode curVextex = null; //当前关联点
                List<TriNode> curPointList = new List<TriNode>();
                double curX;
                double curY;

                curPath = pathSet.get_Geometry(i) as IPath;
                IPointCollection pointSet = curPath as IPointCollection;
                int pointCount = pointSet.PointCount;
                if (pointCount >= 2)
                {
                    curX = pointSet.get_Point(0).X;
                    curY = pointSet.get_Point(0).Y;
                    TriNode cNode = ConNode.GetContainNode(map.ConNodeList, TriNodeList, curX, curY);
                    if (cNode == null)   //该关联点还未加入的情况
                    {
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                        TriNode oNode = ConNode.GetContainNode(oMap.TriNodeList, curX, curY);
                        if (oNode != null)
                        {
                            curVextex.SomeValue = oNode.ID;

                            if (oNode.FeatureType == FeatureType.PointType)
                            {
                                curVextex.SomeValue1 = 1;
                                curVextex.TagValue = oNode.TagValue;
                            }
                            else if (oNode.FeatureType == FeatureType.PolygonType)
                            {
                                curVextex.SomeValue1 = 2;
                                curVextex.TagValue = oNode.TagValue;
                            }
                        }
                        map.TriNodeList.Add(curVextex);
                        ConNode curNode = new ConNode(vextexID, 0.2f, curVextex);
                        map.ConNodeList.Add(curNode);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    else //该关联点已经加入的情况
                    {
                        curPointList.Add(cNode);
                        cNode.TagValue = -1;
                        cNode.FeatureType = FeatureType.ConnNode;
                    }
                    //加入中间顶点
                    for (int j = 1; j < pointCount - 1; j++)
                    {
                        curX = pointSet.get_Point(j).X;
                        curY = pointSet.get_Point(j).Y;
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                        
                        TriNode oNode = ConNode.GetContainNode(oMap.TriNodeList, curX, curY);
                        if (oNode != null)
                        {
                            curVextex.SomeValue = oNode.ID;

                            if (oNode.FeatureType == FeatureType.PointType)
                            {
                                curVextex.SomeValue1 = 1;
                                curVextex.TagValue = oNode.TagValue;
                            }
                            else if (oNode.FeatureType == FeatureType.PolygonType)
                            {
                                curVextex.SomeValue1 = 2;
                                curVextex.TagValue = oNode.TagValue;
                            }
                        }

                        map.TriNodeList.Add(curVextex);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    //加入终点
                    curX = pointSet.get_Point(pointCount - 1).X;
                    curY = pointSet.get_Point(pointCount - 1).Y;
                    cNode = ConNode.GetContainNode(map.ConNodeList, TriNodeList, curX, curY);
                    if (cNode == null)   //该关联点还未加入的情况
                    {
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                        
                        TriNode oNode = ConNode.GetContainNode(oMap.TriNodeList, curX, curY);
                        if (oNode != null)
                        {
                            curVextex.SomeValue = oNode.ID;

                            if (oNode.FeatureType == FeatureType.PointType)
                            {
                                curVextex.SomeValue1 = 1;
                                curVextex.TagValue = oNode.TagValue;
                            }
                            else if (oNode.FeatureType == FeatureType.PolygonType)
                            {
                                curVextex.SomeValue1 = 2;
                                curVextex.TagValue = oNode.TagValue;
                            }
                        }
                        map.TriNodeList.Add(curVextex);

                        ConNode curNode = new ConNode(vextexID, -1f, curVextex);
                        map.ConNodeList.Add(curNode);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    else //该关联点已经加入的情况
                    {
                        curPointList.Add(cNode);
                        cNode.TagValue = -1;
                        cNode.FeatureType = FeatureType.ConnNode;
                    }

                    //添加起点
                    curPL = new PolylineObject(plID, curPointList, sylSize);
                    curPL.TypeID = typeID;
                    map.PolylineList.Add(curPL);
                    plID++;
                }
            }
        }

        /// <summary>
        /// 从PathSet中读取坐标构建道路网地图
        /// </summary>
        public void ProxiEdge2PolylineObjects(
            ref int vextexID,
            ref int plID,
            SMap map,
            SMap oMap,
            PolylineObject l, 
            int typeID,
            double sylSize)
        {
            int count = l.PointList.Count;
            if (count < 2)
                return;
           
                PolylineObject curPL = null; //当前道路
                TriNode curVextex = null; //当前关联点
                List<TriNode> curPointList = new List<TriNode>();
                double curX;
                double curY;


                    curX = l.PointList[0].X;
                    curY =l.PointList[0].Y;
                    TriNode cNode = ConNode.GetContainNode(map.ConNodeList, TriNodeList, curX, curY);
                    if (cNode == null)   //该关联点还未加入的情况
                    {
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                        TriNode oNode = ConNode.GetContainNode(oMap.TriNodeList, curX, curY);
                        if (oNode != null)
                        {
                            curVextex.SomeValue = oNode.ID;

                            if (oNode.FeatureType == FeatureType.PointType)
                            {
                                curVextex.SomeValue1 = 1;
                                curVextex.TagValue = oNode.TagValue;
                            }
                            else if (oNode.FeatureType == FeatureType.PolygonType)
                            {
                                curVextex.SomeValue1 = 2;
                                curVextex.TagValue = oNode.TagValue;
                            }
                        }
                        map.TriNodeList.Add(curVextex);
                        ConNode curNode = new ConNode(vextexID, 0.2f, curVextex);
                        map.ConNodeList.Add(curNode);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    else //该关联点已经加入的情况
                    {
                        curPointList.Add(cNode);
                        cNode.TagValue = -1;
                        cNode.FeatureType = FeatureType.ConnNode;
                    }
                    //加入中间顶点
                    for (int j = 1; j < count - 1; j++)
                    {
                        curX = l.PointList[j].X;
                        curY =l.PointList[j].Y;
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);

                        TriNode oNode = ConNode.GetContainNode(oMap.TriNodeList, curX, curY);
                        if (oNode != null)
                        {
                            curVextex.SomeValue = oNode.ID;

                            if (oNode.FeatureType == FeatureType.PointType)
                            {
                                curVextex.SomeValue1 = 1;
                                curVextex.TagValue = oNode.TagValue;
                            }
                            else if (oNode.FeatureType == FeatureType.PolygonType)
                            {
                                curVextex.SomeValue1 = 2;
                                curVextex.TagValue = oNode.TagValue;
                            }
                        }

                        map.TriNodeList.Add(curVextex);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    //加入终点
                    curX = l.PointList[count - 1].X;
                    curY = l.PointList[count - 1].Y;
                    cNode = ConNode.GetContainNode(map.ConNodeList, TriNodeList, curX, curY);
                    if (cNode == null)   //该关联点还未加入的情况
                    {
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);

                        TriNode oNode = ConNode.GetContainNode(oMap.TriNodeList, curX, curY);
                        if (oNode != null)
                        {
                            curVextex.SomeValue = oNode.ID;

                            if (oNode.FeatureType == FeatureType.PointType)
                            {
                                curVextex.SomeValue1 = 1;
                                curVextex.TagValue = oNode.TagValue;
                            }
                            else if (oNode.FeatureType == FeatureType.PolygonType)
                            {
                                curVextex.SomeValue1 = 2;
                                curVextex.TagValue = oNode.TagValue;
                            }
                        }
                        map.TriNodeList.Add(curVextex);

                        ConNode curNode = new ConNode(vextexID, -1f, curVextex);
                        map.ConNodeList.Add(curNode);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    else //该关联点已经加入的情况
                    {
                        curPointList.Add(cNode);
                        cNode.TagValue = -1;
                        cNode.FeatureType = FeatureType.ConnNode;
                    }

                    //添加起点
                    curPL = new PolylineObject(plID, curPointList, sylSize);
                    curPL.TypeID = typeID;
                    map.PolylineList.Add(curPL);
                    plID++;
        }
        /// <summary>
        /// 确定边界点7-28
        /// </summary>
        /// <param name="buffer">缓冲区</param>
        public void DetermineBoundaryPoints(IPolygon buffer)
        {
            IRelationalOperator iro = buffer as IRelationalOperator;
            foreach(ConNode connode in this.ConNodeList)
            {
                if (connode.Point.TagValue != -1)
                {
                    IPoint curPoint = new PointClass();
                    curPoint.PutCoords(connode.Point.X, connode.Point.Y);
                    if (!iro.Contains(curPoint))
                    {
                        connode.Point.IsBoundaryPoint = true;
                    }
                }

            }
        }

        /// <summary>
        /// 通过坐标获取对应点在Map对象TriNode数组中的索引号
        /// 主要用于获取初始移位点在子图中的坐标索引号
        /// Liuygis:7-25
        /// </summary>
        /// <param name="coords">坐标点</param>
        /// <param name="delta">判断两点等同的距离阈值（e.g.,0.000001f）</param>
        /// <returns>索引号</returns>
        public int GetIndexofVertexbyX_Y(TriNode coords, double delta)
        {
            int n= this.TriNodeList.Count;
            double x = coords.X;
            double y = coords.Y;
            for (int i = 0; i < n; i++)
            {
                TriNode curV = TriNodeList[i];
                if(Math.Abs((1-curV.X/x)) <= delta && Math.Abs((1-curV.Y / y)) <= delta)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 将每个点的移位值写入文本文件
        /// </summary>
        /// <param name="strPath"></param>
        public void WritetodxdytoText(string strPath,string strFileName)
        {
            DataSet ds = new DataSet();
            //创建一个表
            DataTable tableforce = new DataTable();
            tableforce.TableName = @"DisplayStatic";
            tableforce.Columns.Add(@"ID", typeof(int));
            tableforce.Columns.Add(@"dx", typeof(double));
            tableforce.Columns.Add(@"dy", typeof(double));
            tableforce.Columns.Add(@"d", typeof(double));
            tableforce.Columns.Add(@"tagType", typeof(int));
            tableforce.Columns.Add(@"tagID", typeof(int));
            foreach (TriNode  curNode in this.TriNodeList)
            {
                int index = curNode.ID;
                double dx = curNode.dx;
                double dy = curNode.dy;
                double d = Math.Sqrt(dx * dx + dy * dy);
                int tagType = (curNode as TriNode).SomeValue1;
                int tagID = (curNode as TriNode).TagValue;


                    DataRow dr = tableforce.NewRow();
                    dr[0] = index;
                    dr[1] = dx;
                    dr[2] =dy;
                    dr[3] = d;
                    dr[4] = tagType;
                    dr[5] = tagID;
                    tableforce.Rows.Add(dr);
     
            }
            TXTHelper.ExportToTxt(tableforce, strPath + @"\" +strFileName+ @".txt");
        
        }

        /// <summary>
        /// 将MapObject中Polygon按顺序编号
        /// </summary>
        public void MapObjectRegulation()
        {
            for (int i = 0; i < this.PolygonList.Count; i++)
            {
                this.PolygonList[i].ID = i;
                this.PolygonList[i].TargetID = i;
            }
        }
    }
}

                
         

