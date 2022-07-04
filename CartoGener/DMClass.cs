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
using AuxStructureLib;
using AlgEMLib;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace CartoGener
{
    /// <summary>
    /// Class for the generation of Dorling Map
    /// </summary>
    class DMClass
    {       
        #region 构造函数
        public DMClass(AxESRI.ArcGIS.Controls.AxMapControl pMapControl)
        {
            this.pMapControl = pMapControl;
        }

        public DMClass()
        {
        }
        #endregion

        ///Parameters       
        public AxESRI.ArcGIS.Controls.AxMapControl pMapControl;
        DMSupport DMS = new DMSupport();
        public bool continueLable = true;

        /// <summary>
        /// 获取已经无冲突的Circle群
        /// </summary>
        /// <param name="PoList"></param>
        /// <returns></returns>
        public List<List<PolygonObject>> CircleGroup(List<PolygonObject> PoList,double outTd,double inTd)
        {
            List<List<PolygonObject>> CircleList = new List<List<PolygonObject>>();
            List<PolygonObject> CopyPoList = Clone((object)PoList) as List<PolygonObject>;

            while(CopyPoList.Count > 0)
            {
                List<PolygonObject> PoToContinue = new List<PolygonObject>();
                PoToContinue.Add(CopyPoList[0]);
                List<PolygonObject> subPoList = new List<PolygonObject>();
                subPoList.Add(CopyPoList[0]);
                CopyPoList.RemoveAt(0);

                while (PoToContinue.Count > 0)
                {
                    ProxiNode tNode1 = PoToContinue[0].CalProxiNode(); ;
                    for (int i = CopyPoList.Count-1; i >= 0; i--)
                    {                        
                        ProxiNode tNode2 = CopyPoList[i].CalProxiNode();
                        double EdgeDis = this.GetDis(tNode1, tNode2);
                        double RSDis = PoToContinue[0].R + CopyPoList[i].R;

                        if ((RSDis-EdgeDis)<inTd && (EdgeDis - RSDis) < outTd)
                        {
                            PoToContinue.Add(CopyPoList[i]);
                            subPoList.Add(CopyPoList[i]);
                            CopyPoList.RemoveAt(i);
                        }
                    }

                    PoToContinue.RemoveAt(0);
                }

                CircleList.Add(subPoList);
            }

            return CircleList;
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
        /// Compute the radius for each Circle（Only consider the minR）
        /// </summary>
        /// <param name="ValueList"></param>
        /// <param name="MinR"></param>
        /// <param name="Rate"></param>a unit for 100
        /// <param name="RType">=0 linear R</param>=1 SinR；=2 sqrt
        /// <returns></returns>
        public List<Double> GetR(List<double> ValueList, double MinR, double scale, int RType,double DefaultMin)
        {
            double MinValue = ValueList.Min();//Get the minValue
            if (MinValue < DefaultMin)
            {
                MinValue = DefaultMin;
            }
            List<double> RList = new List<double>();//Return R

            #region Linear R
            if (RType == 0)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    if (ValueList[i] < DefaultMin)
                    {
                        double R =  MinR * scale;
                        RList.Add(R);
                    }

                    else
                    {
                        double R = ValueList[i] / MinValue * MinR * scale;
                        RList.Add(R);
                    }
                }
            }
            #endregion

            #region SinR
            else if (RType == 1)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    if (ValueList[i] < DefaultMin)
                    {
                        double R = MinR * scale;
                        RList.Add(R);
                    }

                    else
                    {
                        double R = Math.Sin(ValueList[i] / MinValue) * MinR * scale;
                        RList.Add(R);
                    }
                }
            }
            #endregion

            #region SqrtR
            else if (RType == 2)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    if (ValueList[i] < DefaultMin)
                    {
                        double R = MinR * scale;
                        RList.Add(R);
                    }

                    else
                    {
                        double R = Math.Sqrt(ValueList[i] / MinValue) * MinR * scale;
                        RList.Add(R);
                    }
                }
            }
            #endregion

            return RList;
        }

        /// <summary>
        /// Compute the radius for each Circle（Only consider the minR）
        /// </summary>
        /// <param name="ValueList"></param>
        /// <param name="MinR"></param>
        /// <param name="Rate"></param>a unit for 100
        /// <param name="RType">=0 linear R</param>=1 SinR；=2 sqrt
        /// <returns></returns>
        public List<Double> GetR(List<double> ValueList, double MinR, double scale, int RType)
        {
            double MinValue = ValueList.Min();//Get the minValue
            List<double> RList = new List<double>();//Return R

            #region Linear R
            if (RType == 0)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    double R = ValueList[i] / MinValue * MinR * scale;
                    RList.Add(R);
                }
            }
            #endregion

            #region SinR
            else if (RType == 1)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    double R = Math.Sin(ValueList[i] / MinValue) * MinR * scale;
                    RList.Add(R);
                }
            }
            #endregion

            #region SqrtR
            else if (RType == 2)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {

                    double R = Math.Sqrt(ValueList[i] / MinValue) * MinR * scale;
                    RList.Add(R);
                }
            }
            #endregion

            return RList;
        }

        /// <summary>
        /// Compute the radius for each Circle（Only consider the minR）For stableMap
        /// </summary>
        /// <param name="ValueList"></param>
        /// <param name="MinR"></param>
        /// <param name="Rate"></param>a unit for 100
        /// <param name="RType">=0 linear R</param>=1 SinR；=2 sqrt
        /// <returns></returns>
        public List<Double> GetSR(List<double> ValueList, double MinR, double scale, int RType, double DefaultMin)
        {
            List<double> RList = new List<double>();//Return R

            #region Linear R
            if (RType == 0)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    double R = ValueList[i] / DefaultMin * MinR * scale;
                    RList.Add(R);
                }
            }
            #endregion

            #region SinR
            else if (RType == 1)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    double R = Math.Sin(ValueList[i] / DefaultMin) * MinR * scale;
                    RList.Add(R);
                }
            }
            #endregion

            #region SqrtR
            else if (RType == 2)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    double R = Math.Sqrt(ValueList[i] / DefaultMin) * MinR * scale;
                    RList.Add(R);

                }
            }
            #endregion

            return RList;
        }

        /// <summary>
        /// Compute the radius for each Circle （consider minR and maxR）
        /// </summary>
        /// <param name="ValueList"></param>
        /// <param name="MinR"></param>
        /// <param name="Rate"></param>a unit for 100
        /// <param name="RType">=0 linear R</param>=1 SinR；=2 sqrt
        /// <returns></returns>
        public List<Double> GetR(List<double> ValueList,double MinR,double MaxR, double scale,int RType)
        {
            double MinValue = ValueList.Min();//Get the minValue
            double MaxValue = ValueList.Max();//Get the maxValue
            List<double> RList = new List<double>();//Return R

            #region Linear R
            if (RType == 0)
            { 
                for (int i = 0; i < ValueList.Count; i++)
                {
                    double R = ((ValueList[i] - MinValue) / (MaxValue - MinValue) * (MaxR - MinR) + MinR) * scale;
                    RList.Add(R);
                }
            }
            #endregion

            #region SinR
            else if (RType == 1)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    double R = (Math.Sin(((ValueList[i] - MinValue) / (MaxValue - MinValue)) * Math.PI / 2) * (MaxR - MinR) + MinR) * scale;
                    RList.Add(R);
                }
            }

            #endregion

            #region SqrtR
            else if (RType == 2)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    double R = (Math.Sqrt(((ValueList[i] - MinValue) / (MaxValue - MinValue)) * Math.PI / 2) * (MaxR - MinR) + MinR) * scale;
                    RList.Add(R);
                }
            }
            #endregion 

            return RList;
        }

        /// <summary>
        /// 获得平均冲突半径
        /// </summary>
        /// <param name="pg"></param>
        /// <param name="PoList"></param>
        /// <param name="ValueList"></param>
        /// <param name="MinR"></param>
        /// <param name="MaxR"></param>
        /// <param name="scale"></param>
        /// <param name="RType"></param>
        /// Percent=取前百分比的类别
        /// <returns></returns>
        public double GetAveCR(ProxiGraph pg, List<double> ValueList, double MinR, double MaxR, double scale, int RType,int AveType,double Percent)
        {
            double AveCR = 0;
            int CCount = 0;
            double CCSumDis = 0;
            List<double> RList = this.GetR(ValueList, MinR, MaxR, scale, RType);

            #region Compute the AveCR
            if (AveType == 0)//冲突点的阈值
            {
                for (int i = 0; i < pg.NodeList.Count - 1; i++)
                {
                    for (int j = i + 1; j < pg.NodeList.Count; j++)
                    {
                        double EdgeDis = DMS.GetDis(pg.NodeList[i], pg.NodeList[j]);
                        double RSDis = RList[pg.NodeList[i].ID] + RList[pg.NodeList[j].ID];

                        if (EdgeDis < RSDis)
                        {
                            CCount++;
                            CCSumDis = (RSDis - EdgeDis) + CCSumDis;
                        }
                    }
                }
            }

            else if (AveType == 1)//冲突边的阈值
            {
                foreach (ProxiEdge Pe in pg.EdgeList)
                {
                    ProxiNode Node1 = Pe.Node1;
                    ProxiNode Node2 = Pe.Node2;

                    double EdgeDis = DMS.GetDis(Node1, Node2);
                    double RSDis = RList[Node1.ID] + RList[Node2.ID];

                    if (EdgeDis < RSDis)
                    {
                        CCount++;
                        CCSumDis = (RSDis - EdgeDis) + CCSumDis;
                    }
                }
            }

            else if (AveType == 2)//前n%的平均值
            {
                List<double> DisList = new List<double>();
                foreach (ProxiEdge Pe in pg.EdgeList)
                {
                    ProxiNode Node1 = Pe.Node1;
                    ProxiNode Node2 = Pe.Node2;

                    double EdgeDis = DMS.GetDis(Node1, Node2);
                    double RSDis = RList[Node1.ID] + RList[Node2.ID];

                    double KDis = RSDis - EdgeDis;
                    if (KDis < 0)
                    {
                        KDis = 0;
                    }

                    DisList.Add(KDis);
                }
                List<double> largerList = DisList.Take(Convert.ToInt16(Math.Ceiling(DisList.Count * Percent))).ToList<double>();
                CCSumDis = largerList.Sum();
                CCount = largerList.Count;
            }
            #endregion
            if (CCount > 0)
            {
                return AveCR = CCSumDis / CCount;
            }

            return AveCR;
        }

        /// <summary>
        /// 依据阻尼振荡法获取MaxR
        /// </summary>
        /// <param name="pg"></param>
        /// <param name="PoList"></param>
        /// <param name="ValueList"></param>
        /// <param name="MinR"></param>
        /// <param name="initialMaxR"></param>
        /// <param name="scale"></param>
        /// <param name="RType"></param>
        /// <returns></returns>
        public List<double> GetFinalListR(ProxiGraph pg, List<double> ValueList, double MinR, double initialMaxR, double scale, int RType, double CCTd,double Percent)
        {
            double AveCR = this.GetAveCR(pg, ValueList, MinR, initialMaxR, scale, RType, 2, Percent);
            List<double> RList = this.GetR(ValueList, MinR, initialMaxR, scale, RType);//Return R

            #region 智能阻尼振荡过程
            while(Math.Abs(AveCR - CCTd) > 0.001)
            {

                if (AveCR > CCTd)
                {
                    initialMaxR = 0.5 * (initialMaxR - MinR) + MinR;
                }
                else
                {
                    initialMaxR = initialMaxR + 0.5 * (initialMaxR - MinR);
                }

                RList = this.GetR(ValueList, MinR, initialMaxR, scale, RType);//Return R
                AveCR = this.GetAveCR(pg, ValueList, MinR, initialMaxR, scale, RType, 2,Percent);
            }
            #endregion

            return RList;
        }

        /// <summary>
        /// Get the initial Circles with R（有偏置）
        /// </summary>
        /// <param name="pFeatureClass"></param>
        /// <returns></returns>
        public List<Circle> GetInitialCircle(IFeatureClass pFeatureClass, string ValueField, string NameField,double MinR, double scale, int RType)
        {
            int i = 0;
            List<Circle> InitialCircleList = new List<Circle>();
            List<double> ValueList = new List<double>();

            #region Circles without R
            IFeatureCursor pFeatureCursor = pFeatureClass.Update(null, true);
            IFeature pFeature = pFeatureCursor.NextFeature();
            while (pFeature != null)
            {
                Circle CacheCircle = new Circle(i);
                double Value = DMS.GetValue(pFeature, ValueField);
                ValueList.Add(Value);
                CacheCircle.Value = Value;
                CacheCircle.scale = scale;

                string Name = DMS.GetStringValue(pFeature, NameField);
                CacheCircle.Name = Name;

                IArea pArea = pFeature.Shape as IArea;
                CacheCircle.CenterX = pArea.Centroid.X;
                CacheCircle.CenterY = pArea.Centroid.Y;
                InitialCircleList.Add(CacheCircle);

                i++;
                pFeature = pFeatureCursor.NextFeature();
            }
            #endregion

            #region assign R for circles
            List<double> RList = this.GetR(ValueList, MinR, scale, RType);

            for (int j = 0; j < InitialCircleList.Count; j++)
            {
                InitialCircleList[j].Radius = RList[j];
            }
            #endregion

            return InitialCircleList;
        }

        /// <summary>
        /// Get the initial Circles with R（有偏置）
        /// </summary>
        /// <param name="pFeatureClass"></param>
        /// <returns></returns>
        public List<Circle> GetInitialCircle(IFeatureClass pFeatureClass, string ValueField,double MinR,double MaxR,double scale, int RType)
        {
            int i = 0; 
            List<Circle> InitialCircleList = new List<Circle>();         
            List<double> ValueList = new List<double>();

            #region Circles without R
            IFeatureCursor pFeatureCursor = pFeatureClass.Update(null, true);
            IFeature pFeature = pFeatureCursor.NextFeature();
            while (pFeature != null)
            {
                Circle CacheCircle = new Circle(i);
                double Value = DMS.GetValue(pFeature, ValueField);
                ValueList.Add(Value);
                CacheCircle.Value = Value;
                CacheCircle.scale = scale;

                IArea pArea = pFeature.Shape as IArea;
                CacheCircle.CenterX = pArea.Centroid.X;
                CacheCircle.CenterY = pArea.Centroid.Y;
                InitialCircleList.Add(CacheCircle);

                i++;
                pFeature = pFeatureCursor.NextFeature();
            }
            #endregion

            #region assign R for circles
            List<double> RList = this.GetR(ValueList, MinR, MaxR, scale, RType);
           
            for (int j = 0; j < InitialCircleList.Count; j++)
            {
                InitialCircleList[j].Radius = RList[j];
            }
            #endregion

            return InitialCircleList;
        }

        /// <summary>
        /// Get the initial Circles with 
        /// ValueField+NameField
        /// 阻尼振荡法
        /// </summary>
        /// <param name="pFeatureClass"></param>
        /// <returns></returns>
        public List<Circle> GetInitialCircle(IFeatureClass pFeatureClass, ProxiGraph Pg, string ValueField, string NameField, double MinR, double MaxR, double scale, int RType,double CCTd,double Percent)
        {
            int i = 0;
            List<Circle> InitialCircleList = new List<Circle>();
            List<double> ValueList = new List<double>();

            #region Circles without R
            IFeatureCursor pFeatureCursor = pFeatureClass.Update(null, true);
            IFeature pFeature = pFeatureCursor.NextFeature();
            while (pFeature != null)
            {
                Circle CacheCircle = new Circle(i);
                double Value = DMS.GetValue(pFeature, ValueField);
                ValueList.Add(Value);
                CacheCircle.Value = Value;
                CacheCircle.scale = scale;

                string Name = DMS.GetStringValue(pFeature, NameField);
                CacheCircle.Name = Name;

                IArea pArea = pFeature.Shape as IArea;
                CacheCircle.CenterX = pArea.Centroid.X;
                CacheCircle.CenterY = pArea.Centroid.Y;
                InitialCircleList.Add(CacheCircle);

                i++;
                pFeature = pFeatureCursor.NextFeature();
            }
            #endregion

            #region assign R for circles
            List<double> RList = this.GetFinalListR(Pg, ValueList, MinR, MaxR, scale, RType, CCTd,Percent);

            for (int j = 0; j < InitialCircleList.Count; j++)
            {
                InitialCircleList[j].Radius = RList[j];
            }
            #endregion

            return InitialCircleList;
        }

        /// <summary>
        /// Get the initial Circles with R
        /// 阻尼振荡法
        /// ValueField
        /// </summary>
        /// <param name="pFeatureClass"></param>
        /// <returns></returns>
        public List<Circle> GetInitialCircle(IFeatureClass pFeatureClass, ProxiGraph Pg, string ValueField, double MinR, double MaxR, double scale, int RType, double CCTd, double Percent)
        {
            int i = 0;
            List<Circle> InitialCircleList = new List<Circle>();
            List<double> ValueList = new List<double>();

            #region Circles without R
            IFeatureCursor pFeatureCursor = pFeatureClass.Update(null, true);
            IFeature pFeature = pFeatureCursor.NextFeature();
            while (pFeature != null)
            {
                Circle CacheCircle = new Circle(i);
                double Value = DMS.GetValue(pFeature, ValueField);
                ValueList.Add(Value);
                CacheCircle.Value = Value;
                CacheCircle.scale = scale;

                IArea pArea = pFeature.Shape as IArea;
                CacheCircle.CenterX = pArea.Centroid.X;
                CacheCircle.CenterY = pArea.Centroid.Y;
                InitialCircleList.Add(CacheCircle);

                i++;
                pFeature = pFeatureCursor.NextFeature();
            }
            #endregion

            #region assign R for circles
            List<double> RList = this.GetFinalListR(Pg, ValueList, MinR, MaxR, scale, RType, CCTd, Percent);

            for (int j = 0; j < InitialCircleList.Count; j++)
            {
                InitialCircleList[j].Radius = RList[j];
            }
            #endregion

            return InitialCircleList;
        }

        /// <summary>
        /// Get the initial Circles with R（不用阻尼振荡法 设置最小参数的阈值）
        /// </summary>
        /// <param name="pFeatureClass"></param>
        /// <returns></returns>
        public List<Circle> GetInitialCircle(Dictionary<IPolygon, double> TimeSeriesData, double MinR, double scale, int RType,double DefaultMin)
        {
            List<Circle> InitialCircleList = new List<Circle>();
            List<double> ValueList = new List<double>();

            #region Circles without R
            for (int i = 0; i < TimeSeriesData.Keys.ToList().Count; i++)
            {
                Circle CacheCircle = new Circle(i);
                ValueList.Add(TimeSeriesData[TimeSeriesData.Keys.ToList()[i]]);
                CacheCircle.Value = TimeSeriesData[TimeSeriesData.Keys.ToList()[i]];
                CacheCircle.scale = scale;

                IArea pArea = TimeSeriesData.Keys.ToList()[i] as IArea;
                CacheCircle.CenterX = pArea.Centroid.X;
                CacheCircle.CenterY = pArea.Centroid.Y;
                InitialCircleList.Add(CacheCircle);
            }
            #endregion

            #region assign R for circles
            List<double> RList = this.GetR(ValueList, MinR, scale, RType,DefaultMin);

            for (int j = 0; j < InitialCircleList.Count; j++)
            {
                InitialCircleList[j].Radius = RList[j];
            }
            #endregion

            return InitialCircleList;
        }

        /// <summary>
        /// Get the initial Circles with R（不用阻尼振荡法 不设置最小参数的阈值）
        /// </summary>
        /// <param name="pFeatureClass"></param>
        /// <returns></returns>
        public List<Circle> GetInitialCircle(Dictionary<IPolygon, double> TimeSeriesData, double MinR, double scale, int RType)
        {
            List<Circle> InitialCircleList = new List<Circle>();
            List<double> ValueList = new List<double>();

            #region Circles without R
            for (int i = 0; i < TimeSeriesData.Keys.ToList().Count; i++)
            {
                Circle CacheCircle = new Circle(i);
                ValueList.Add(TimeSeriesData[TimeSeriesData.Keys.ToList()[i]]);
                CacheCircle.Value = TimeSeriesData[TimeSeriesData.Keys.ToList()[i]];
                CacheCircle.scale = scale;

                IArea pArea = TimeSeriesData.Keys.ToList()[i] as IArea;
                CacheCircle.CenterX = pArea.Centroid.X;
                CacheCircle.CenterY = pArea.Centroid.Y;
                InitialCircleList.Add(CacheCircle);
            }
            #endregion

            #region assign R for circles
            List<double> RList = this.GetR(ValueList, MinR, scale, RType);

            for (int j = 0; j < InitialCircleList.Count; j++)
            {
                InitialCircleList[j].Radius = RList[j];
            }
            #endregion

            return InitialCircleList;
        }

        /// <summary>
        /// Get the initial Circles with R for StableDorling
        /// </summary>
        /// <param name="pFeatureClass"></param>
        /// <returns></returns>
        public List<Circle> GetInitialCircleSDorling(Dictionary<IPolygon, double> TimeSeriesData, double MinR, double scale, int RType, double DefaultMin)
        {
            List<Circle> InitialCircleList = new List<Circle>();
            List<double> ValueList = new List<double>();

            #region Circles without R
            for (int i = 0; i < TimeSeriesData.Keys.ToList().Count; i++)
            {
                Circle CacheCircle = new Circle(i);
                ValueList.Add(TimeSeriesData[TimeSeriesData.Keys.ToList()[i]]);
                CacheCircle.Value = TimeSeriesData[TimeSeriesData.Keys.ToList()[i]];
                CacheCircle.scale = scale;

                IArea pArea = TimeSeriesData.Keys.ToList()[i] as IArea;
                CacheCircle.CenterX = pArea.Centroid.X;
                CacheCircle.CenterY = pArea.Centroid.Y;
                InitialCircleList.Add(CacheCircle);
            }
            #endregion

            #region assign R for circles
            List<double> RList = this.GetSR(ValueList, MinR, scale, RType, DefaultMin);

            for (int j = 0; j < InitialCircleList.Count; j++)
            {
                InitialCircleList[j].Radius = RList[j];
            }
            #endregion

            return InitialCircleList;
        }

        /// <summary>
        /// Get the initial Circles with R（不用阻尼振荡法 设置最小参数的阈值）
        /// </summary>
        /// <param name="pFeatureClass"></param>
        /// <returns></returns>
        public List<Circle> GetInitialCircle(Dictionary<IFeature, double> TimeSeriesData, double MinR, double scale, int RType, double DefaultMin)
        {
            List<Circle> InitialCircleList = new List<Circle>();
            List<double> ValueList = new List<double>();

            #region Circles without R
            for (int i = 0; i < TimeSeriesData.Keys.ToList().Count; i++)
            {
                Circle CacheCircle = new Circle(i);
                ValueList.Add(TimeSeriesData[TimeSeriesData.Keys.ToList()[i]]);
                CacheCircle.Value = TimeSeriesData[TimeSeriesData.Keys.ToList()[i]];
                CacheCircle.scale = scale;

                IArea pArea = TimeSeriesData.Keys.ToList()[i].Shape as IArea;
                CacheCircle.CenterX = pArea.Centroid.X;
                CacheCircle.CenterY = pArea.Centroid.Y;
                InitialCircleList.Add(CacheCircle);
            }
            #endregion

            #region assign R for circles
            List<double> RList = this.GetR(ValueList, MinR, scale, RType, DefaultMin);

            for (int j = 0; j < InitialCircleList.Count; j++)
            {
                InitialCircleList[j].Radius = RList[j];
            }
            #endregion

            return InitialCircleList;
        }

        /// <summary>
        /// Circles to PolygonObjects
        /// </summary>
        /// <param name="CircleList"></param>
        /// <returns></returns>
        public List<PolygonObject> GetInitialPolygonObject(List<Circle> CircleList)
        {
            List<PolygonObject> PoList = new List<PolygonObject>();

            for (int i = 0; i < CircleList.Count; i++)
            {
                DMS.pMapControl = pMapControl;
                IPolygon pPolygon = DMS.CircleToPolygon(CircleList[i]);
                PolygonObject pPo = DMS.PolygonConvert(pPolygon);
                pPo.ID = i;
                pPo.R = CircleList[i].Radius;

                PoList.Add(pPo);
            }

            return PoList;
        }

        /// <summary>
        /// Circles to PolygonObjects
        /// </summary>
        /// <param name="CircleList"></param>
        /// <returns></returns>
        public List<PolygonObject> GetInitialPolygonObject2(List<Circle> CircleList)
        {
            List<PolygonObject> PoList = new List<PolygonObject>();

            for (int i = 0; i < CircleList.Count; i++)
            {
                DMS.pMapControl = pMapControl;
                PolygonObject pPo = DMS.CircleToPo(CircleList[i]);
                pPo.ID = i;
                pPo.TargetID = i;
                pPo.R = CircleList[i].Radius;
                pPo.Value = CircleList[i].Value;
                pPo.Name = CircleList[i].Name;

                PoList.Add(pPo);
            }

            return PoList;
        }

        /// <summary>
        /// Circles to PolygonObjects
        /// </summary>
        /// <param name="CircleList"></param>
        /// <returns></returns>
        public List<List<PolygonObject>> GetInitialPolygonObjectForStableDorling(List<Dictionary<IPolygon, double>> TimeSeriesData,double MinR,double Scale,int RType,double MinValue)
        {
            #region 获得最小值
            double CacheMinValue = 100000000;
            for (int i = 0; i < TimeSeriesData.Count; i++)
            {
                if (TimeSeriesData[i].Values.ToList().Min() < CacheMinValue)
                {
                    CacheMinValue = TimeSeriesData[i].Values.ToList().Min();
                }
            }
            #endregion

            #region 比较获取圆赋值最小值
            if (CacheMinValue > MinValue)
            {
                MinValue = CacheMinValue;
            }
            #endregion

            #region 生成圆
            List<List<Circle>> CircleLists = new List<List<Circle>>();
            for (int i = 0; i < TimeSeriesData.Count; i++)
            {
                List<Circle> CircleList = this.GetInitialCircleSDorling(TimeSeriesData[i], MinR, Scale, RType, MinValue);
                CircleLists.Add(CircleList);
            }
            List<List<PolygonObject>> CircleObjectList = new List<List<PolygonObject>>();
            for (int i = 0; i < CircleLists.Count; i++)
            {
                List<PolygonObject> PoList = this.GetInitialPolygonObject2(CircleLists[i]);
                CircleObjectList.Add(PoList);
            }
            #endregion

            return CircleObjectList;
        }

        /// <summary>
        /// Circles to PolygonObjects
        /// </summary>
        /// <param name="CircleList"></param>
        /// <returns></returns>
        public List<List<PolygonObject>> GetInitialPolygonObjectForStableDorling(List<Dictionary<IFeature, double>> TimeSeriesData, double MinValue)
        {
            #region 获得最小值
            double CacheMinValue = 100000000;
            for (int i = 0; i < TimeSeriesData.Count; i++)
            {
                if (TimeSeriesData[i].Values.ToList().Min() < MinValue)
                {
                    CacheMinValue = TimeSeriesData[i].Values.ToList().Min();
                }
            }
            #endregion

            #region 比较获取圆赋值最小值
            if (CacheMinValue > MinValue)
            {
                MinValue = CacheMinValue;
            }
            #endregion

            #region 生成圆
            List<List<Circle>> CircleLists = new List<List<Circle>>();
            for (int i = 0; i < TimeSeriesData.Count; i++)
            {
                List<Circle> CircleList = this.GetInitialCircle(TimeSeriesData[i], 1, 1, 0, MinValue);
                CircleLists.Add(CircleList);
            }
            List<List<PolygonObject>> CircleObjectList = new List<List<PolygonObject>>();
            for (int i = 0; i < CircleLists.Count; i++)
            {
                List<PolygonObject> PoList = this.GetInitialPolygonObject2(CircleLists[i]);
                CircleObjectList.Add(PoList);
            }
            #endregion

            return CircleObjectList;
        }

        /// <summary>
        /// 深拷贝（通用拷贝）
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static object Clone(object obj)
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(memoryStream, obj);
            memoryStream.Position = 0;
            return formatter.Deserialize(memoryStream);
        }

        /// <summary>
        /// Beams Displace
        /// </summary>
        /// <param name="pg">邻近图</param>
        /// <param name="pMap">Dorling图层</param>
        /// <param name="scale">比例尺</param>
        /// <param name="E">弹性模量</param>
        /// <param name="I">惯性力矩</param>
        /// <param name="A">横切面积</param>
        /// MaxTd:吸引力作用的最大范围，若距离超过一定数值，就没有吸引力
        /// <param name="Iterations">迭代次数</param>
        public void DorlingBeams(ProxiGraph pg, SMap pMap, double scale, double E, double I, double A, int Iterations,int algType,double StopT,double MaxTd,int ForceType,bool WeightConsi,double InterDis)
        {
            AlgBeams algBeams = new AlgBeams(pg, pMap, E, I, A);
            //求吸引力-2014-3-20所用

            ProxiGraph CopyG = Clone((object)pg) as ProxiGraph;
            algBeams.OriginalGraph = CopyG;
            algBeams.Scale = scale;
            algBeams.AlgType = algType;

            if (Iterations < 20)
            {
                Iterations = 20;
            }
            if (Iterations > 100)
            {
                Iterations = 100;
            }

            for (int i = 0; i < Iterations; i++)//迭代计算
            {
                Console.WriteLine(i.ToString());//标识
                          
                algBeams.DoDisplacePgDorling(pMap,StopT,MaxTd,ForceType,WeightConsi,InterDis);// 调用Beams算法 
                //pg.OverlapDelete();
                pg.PgRefined(pMap.PolygonList);//每次处理完都需要更新Pg
                //pg.CreateMSTRevise(pg.NodeList, pg.EdgeList, pMap.PolygonList);//构建MST，保证群组是连接的
                //pg.DeleteLongerEdges(pg.EdgeList, pMap.PolygonList, 25);//删除长的边
                //pg.DeleteCrossEdge(pg.EdgeList, pMap.PolygonList);//删除穿过的边                
                //pg.PgRefined(pg.MSTEdgeList);//MSTrefine

                this.continueLable = algBeams.isContinue;
                if (algBeams.isContinue == false)
                {
                    break;
                }
            }
        }

        /// Beams Displace
        /// </summary>
        /// <param name="pg">邻近图</param>
        /// <param name="pMap">Dorling图层</param>
        /// <param name="scale">比例尺</param>
        /// <param name="E">弹性模量</param>
        /// <param name="I">惯性力矩</param>
        /// <param name="A">横切面积</param>
        /// <param name="Iterations">迭代次数</param>
        public void GroupDorlingBeams(ProxiGraph pg, SMap pMap, double scale, double E, double I, double A, int Iterations, int algType, double StopT, double MaxTd, int ForceType, bool WeightConsi,int j)
        {
            AlgBeams algBeams = new AlgBeams(pg, pMap, E, I, A);
            //求吸引力-2014-3-20所用

            ProxiGraph CopyG = Clone((object)pg) as ProxiGraph;
            algBeams.OriginalGraph = CopyG;
            algBeams.Scale = scale;
            algBeams.AlgType = algType;
            for (int i = 0; i < Iterations; i++)//迭代计算
            {
                Console.WriteLine(i.ToString());//标识

                algBeams.GroupDoDisplacePgDorling(pMap, StopT, MaxTd, ForceType, WeightConsi);// 调用Beams算法
                pg.OverlapDelete();
                pg.GroupPgRefined(pMap.PolygonList);//每次处理完都需要更新Pg
                //pg.CreateMSTRevise(pg.NodeList, pg.EdgeList, pMap.PolygonList);//构建MST，保证群组是连接的
                //pg.DeleteLongerEdges(pg.EdgeList, pMap.PolygonList, 25);//删除长的边
                //pg.DeleteCrossEdge(pg.EdgeList, pMap.PolygonList);//删除穿过的边                
                //pg.PgRefined(pg.MSTEdgeList);//MSTrefine

                pg.WriteProxiGraph2Shp("C:\\Users\\10988\\Desktop\\ex", "邻近图" + j.ToString()+i.ToString(), pMapControl.Map.SpatialReference);
                pMap.WriteResult2Shp("C:\\Users\\10988\\Desktop\\ex",j.ToString()+i.ToString(), pMapControl.Map.SpatialReference);    

                if (algBeams.isContinue == false)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Beams Displace
        /// </summary>
        /// <param name="pg">邻近图</param>
        /// <param name="pMap">Dorling图层</param>
        /// SubMaps 基于pMap进行更新的图层
        /// <param name="scale">比例尺</param>
        /// <param name="E">弹性模量</param>
        /// <param name="I">惯性力矩</param>
        /// <param name="A">横切面积</param>
        /// MaxTd:吸引力作用的最大范围，若距离超过一定数值，就没有吸引力
        /// InterDis=定义Touch的距离
        /// /// GroupForceType=0 平均力；GroupForceType=1最大力；GroupForceType=0 最小力；
        /// <param name="Iterations">迭代次数</param>
        public void StableDorlingBeams(ProxiGraph pg, List<SMap> SubMaps, double scale, double E, double I, double A, int Iterations, int algType, double StopT, double MaxTd, int ForceType, bool WeightConsi, double InterDis,int GroupForceType)
        {
            AlgBeams algBeams = new AlgBeams(pg, SubMaps, E, I, A);
            //求吸引力-2014-3-20所用

            ProxiGraph CopyG = Clone((object)pg) as ProxiGraph;
            algBeams.OriginalGraph = CopyG;
            algBeams.Scale = scale;
            algBeams.AlgType = algType;
            for (int i = 0; i < Iterations; i++)//迭代计算
            {
                Console.WriteLine(i.ToString());//标识

                algBeams.DoDisplacePgStableDorling(SubMaps, StopT, MaxTd, ForceType, WeightConsi, InterDis,GroupForceType);// 调用Beams算法 
                //pg.OverlapDelete();
                //pg.CreateMSTRevise(pg.NodeList, pg.EdgeList, pMap.PolygonList);//构建MST，保证群组是连接的
                //pg.DeleteLongerEdges(pg.EdgeList, pMap.PolygonList, 25);//删除长的边
                //pg.DeleteCrossEdge(pg.EdgeList, pMap.PolygonList);//删除穿过的边                
                //pg.PgRefined(pg.MSTEdgeList);//MSTrefine

                this.continueLable = algBeams.isContinue;
                if (algBeams.isContinue == false)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Beams Displace
        /// </summary>
        /// <param name="pg">邻近图</param>
        /// <param name="pMap">Dorling图层</param>
        /// SubMaps 基于pMap进行更新的图层
        /// <param name="scale">比例尺</param>
        /// <param name="E">弹性模量</param>
        /// <param name="I">惯性力矩</param>
        /// <param name="A">横切面积</param>
        /// MaxTd:吸引力作用的最大范围，若距离超过一定数值，就没有吸引力
        /// InterDis=定义Touch的距离
        /// <param name="Iterations">迭代次数</param>
        /// /// GroupForceType=0 平均力；GroupForceType=1最大力；GroupForceType=0 最小力；
        public void StableDorlingBeams(List<ProxiGraph> PgList, List<SMap> SubMaps, double scale, double E, double I, double A, int Iterations, int algType, double StopT, double MaxTd, int ForceType, bool WeightConsi, double InterDis,int GroupForceType)
        {
            AlgBeams algBeams = new AlgBeams(PgList, SubMaps, I, E, A);

            ProxiGraph CopyG = Clone((object)PgList[0]) as ProxiGraph;
            algBeams.OriginalGraph = CopyG;
            algBeams.Scale = scale;
            algBeams.AlgType = algType;
            for (int i = 0; i < Iterations; i++)//迭代计算
            {
                Console.WriteLine(i.ToString());//标识

                algBeams.DoDisplacePgStableDorling(SubMaps, StopT, MaxTd, ForceType, WeightConsi, InterDis, GroupForceType);// 调用Beams算法 
                //pg.OverlapDelete();
                //pg.CreateMSTRevise(pg.NodeList, pg.EdgeList, pMap.PolygonList);//构建MST，保证群组是连接的
                //pg.DeleteLongerEdges(pg.EdgeList, pMap.PolygonList, 25);//删除长的边
                //pg.DeleteCrossEdge(pg.EdgeList, pMap.PolygonList);//删除穿过的边                
                //pg.PgRefined(pg.MSTEdgeList);//MSTrefine
                PgList[0].PgRefined(SubMaps);

                this.continueLable = algBeams.isContinue;
                if (algBeams.isContinue == false)
                {
                    break;
                }
            }
        }
    }
}
