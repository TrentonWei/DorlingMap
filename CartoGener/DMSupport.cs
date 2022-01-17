using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.GlobeCore;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesRaster;

using AuxStructureLib;
using AuxStructureLib.IO;

namespace CartoGener
{
    class DMSupport
    {
          /// <summary>
        /// 参数
        /// </summary>
        public AxESRI.ArcGIS.Controls.AxMapControl pMapControl;
        public Symbolization sb = new Symbolization();

        #region 构造函数
        public DMSupport(AxESRI.ArcGIS.Controls.AxMapControl pMapControl)
        {
            this.pMapControl = pMapControl;
        }

        public DMSupport()
        {
        }
        #endregion

        /// <summary>
        /// 计算给定两个点之间的距离
        /// </summary>
        /// <param name="Point1"></param>
        /// <param name="Point2"></param>
        /// <returns></returns>
        public double GetDis(IPoint Point1, IPoint Point2)
        {
            return Math.Sqrt((Point1.X - Point2.X) * (Point1.X - Point2.X) + (Point1.Y - Point2.Y) * (Point1.Y - Point2.Y));
        }

        /// <summary>
        /// 计算两个点的距离
        /// </summary>
        /// <param name="Node1"></param>
        /// <param name="Node2"></param>
        /// <returns></returns>
        public double GetDis(ProxiNode Node1, ProxiNode Node2)
        {
            double Dis = Math.Sqrt((Node1.X - Node2.X) * (Node1.X - Node2.X) + (Node1.Y - Node2.Y) * (Node1.Y - Node2.Y));
            return Dis;
        }

        /// <summary>
        /// 依据ID获取对应的建筑物
        /// </summary>
        /// <param name="PoList"></param>
        /// <param name="ID"></param>
        /// <returns></returns>
        public PolygonObject GetObjectByID(List<PolygonObject> PoList, int ID)
        {
            bool NullLabel = false; int TID = 0;
            for (int i = 0; i < PoList.Count; i++)
            {
                if (PoList[i].ID == ID)
                {
                    NullLabel = true;
                    TID = ID;
                    break;
                }
            }

            if (!NullLabel)
            {
                return null;
            }
            else
            {
                return PoList[TID];
            }
        }

        /// <summary>
        /// 计算给定的两个圆的距离
        /// </summary>
        /// <param name="C1"></param>
        /// <param name="C2"></param>
        /// <returns></returns>
        public double GetDis(Circle C1, Circle C2)
        {
            double PDis = Math.Sqrt((C1.CenterX - C2.CenterX) * (C1.CenterX - C2.CenterX) + (C1.CenterY - C2.CenterY) * (C1.CenterY - C2.CenterY));
            double RDis = C1.Radius + C2.Radius;

            return PDis - RDis;
        }

        /// <summary>
        /// 两个双精度型数字是否整除
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool isDivide(double a, double b)
        {
            double d = a % b;
            const double epsilon = 1.0e-6;
            if (d < epsilon)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 获取给定Feature的属性
        /// </summary>
        /// <param name="CurFeature"></param>
        /// <param name="FieldString"></param>
        /// <returns></returns>
        public double GetValue(IFeature curFeature, string FieldString)
        {
            double Value = 0;

            IFields pFields = curFeature.Fields;
            int field1 = pFields.FindField(FieldString);
            Value = Convert.ToDouble(curFeature.get_Value(field1));

            return Value;
        }

        /// <summary>
        /// 获取给定Feature的属性
        /// </summary>
        /// <param name="CurFeature"></param>
        /// <param name="FieldString"></param>
        /// <returns></returns>
        public string GetStringValue(IFeature curFeature, string FieldString)
        {
            string Value = null;

            IFields pFields = curFeature.Fields;
            int field1 = pFields.FindField(FieldString);
            Value = Convert.ToString(curFeature.get_Value(field1));

            return Value;
        }

        /// <summary>
        /// 将建筑物转化为IPolygon
        /// </summary>
        /// <param name="pPolygonObject"></param>
        /// <returns></returns>
        public IPolygon PolygonObjectConvert(PolygonObject pPolygonObject)
        {
            Ring ring1 = new RingClass();
            object missing = Type.Missing;

            IPoint curResultPoint = new PointClass();
            TriNode curPoint = null;
            if (pPolygonObject != null)
            {
                for (int i = 0; i < pPolygonObject.PointList.Count; i++)
                {
                    curPoint = pPolygonObject.PointList[i];
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    ring1.AddPoint(curResultPoint, ref missing, ref missing);
                }
            }

            curPoint = pPolygonObject.PointList[0];
            curResultPoint.PutCoords(curPoint.X, curPoint.Y);
            ring1.AddPoint(curResultPoint, ref missing, ref missing);

            IGeometryCollection pointPolygon = new PolygonClass();
            pointPolygon.AddGeometry(ring1 as IGeometry, ref missing, ref missing);
            IPolygon pPolygon = pointPolygon as IPolygon;

            //PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
            //object PolygonSymbol = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 0, 20, 20);

            //pMapControl.DrawShape(pPolygon, ref PolygonSymbol);
            //pMapControl.Map.RecalcFullExtent();

            pPolygon.SimplifyPreserveFromTo();
            return pPolygon;
        }

        /// <summary>
        /// polygon转换成polygonobject
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <returns></returns>
        public PolygonObject PolygonConvert(IPolygon pPolygon)
        {
            int ppID = 0;//（polygonobject自己的编号，应该无用）
            List<TriNode> trilist = new List<TriNode>();
            //Polygon的点集
            IPointCollection pointSet = pPolygon as IPointCollection;
            int count = pointSet.PointCount;
            double curX;
            double curY;
            //ArcGIS中，多边形的首尾点重复存储
            for (int i = 0; i < count - 1; i++)
            {
                curX = pointSet.get_Point(i).X;
                curY = pointSet.get_Point(i).Y;
                //初始化每个点对象
                TriNode tPoint = new TriNode(curX, curY, ppID, 1);
                trilist.Add(tPoint);
            }
            //生成自己写的多边形
            PolygonObject mPolygonObject = new PolygonObject(ppID, trilist);

            return mPolygonObject;
        }

        /// <summary>
        /// 由给定的圆生成一个IPolygon
        /// </summary>
        /// <param name="CacheCircle"></param>
        /// <returns></returns>
        public IPolygon CircleToPolygon(Circle CacheCircle)
        {
            IConstructCircularArc pCircle = new CircularArcClass();

            #region Get the Center
            IPoint CenterPoint = new PointClass();
            CenterPoint.X = CacheCircle.CenterX;
            CenterPoint.Y = CacheCircle.CenterY;
            #endregion

            pCircle.ConstructCircle(CenterPoint, CacheCircle.Radius,false);//Generate a circle

            #region Circle to polygons
            ICircularArc pArc = pCircle as ICircularArc;
            ISegment pSegment1 = pArc as ISegment;
            object missing = Type.Missing;
            ISegmentCollection pSegmentColl = new RingClass();
            pSegmentColl.AddSegment(pSegment1, missing, missing);
            IRing pRing = pSegmentColl as IRing;
            pRing.Close(); //得到闭合的环
            IGeometryCollection pGeometryCollection = new PolygonClass();
            pGeometryCollection.AddGeometry(pRing, ref missing, ref missing); //环转面
            IPolygon pPolygon = (IPolygon)pGeometryCollection;
            #endregion

            //#region symbolizaiton
            //object cPolygonSb = sb.PolygonSymbolization(1, 100, 100, 100, 0, 100, 100, 100);
            //pMapControl.DrawShape(pPolygon, ref cPolygonSb);
            //#endregion

            return pPolygon;
        }

        /// <summary>
        /// 由给定的圆生成一个近似圆（正36多边形）
        /// </summary>
        /// <param name="CacheCircle"></param>
        /// <returns></returns>
        public PolygonObject CircleToPo(Circle CacheCircle)
        {
            int ppID = 0;//（polygonobject自己的编号，应该无用）
            List<TriNode> trilist = new List<TriNode>();
            //Polygon的点集
            double curX;
            double curY;
            for (int i = 0; i < 37; i++)
            {
                curX = CacheCircle.CenterX + CacheCircle.Radius * Math.Cos(i * Math.PI / 18);
                curY = CacheCircle.CenterY + CacheCircle.Radius * Math.Sin(i * Math.PI / 18);
                //初始化每个点对象
                TriNode tPoint = new TriNode(curX, curY, ppID, 1);
                trilist.Add(tPoint);
            }
            //生成自己写的多边形
            PolygonObject mPolygonObject = new PolygonObject(ppID, trilist);
            return mPolygonObject;
        }

        /// <summary>
        /// PG和Map的regulation
        /// </summary>
        /// <param name="Pg"></param>
        /// <param name="pMap"></param>
        /// <returns></returns>
        public SMap regulation(ProxiGraph Pg, SMap pMap)
        {
            SMap newMap = new SMap();

            #region 获取PoList
            List<PolygonObject> PoList = new List<PolygonObject>();
            foreach (ProxiNode Pn in Pg.NodeList)
            {
                foreach (PolygonObject Po in pMap.PolygonList)
                {
                    if (Po.ID == Pn.TagID && !PoList.Contains(Po))
                    {
                        PolygonObject CachePo = new PolygonObject(PoList.Count, Po.PointList);
                        CachePo.R = Po.R;
                        CachePo.Value = Po.Value;
                        CachePo.Name = Po.Name;

                        PoList.Add(CachePo);
                    }
                }
            }
            #endregion

            #region 更新Pg和Map
            for (int i = 0; i < Pg.NodeList.Count; i++)
            {
                Pg.NodeList[i].ID = i;
                Pg.NodeList[i].TagID = i;
            }
            #endregion

            newMap.PolygonList = PoList;
            return newMap;
        }
    }
}
