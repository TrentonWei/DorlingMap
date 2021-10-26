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

namespace CartoGener
{
    /// <summary>
    /// Class for the generation of Dorling Map
    /// </summary>
    class DMClass
    {
        ///Parameters
        DMSupport DMS = new DMSupport();

        /// <summary>
        /// Compute the radius for each Circle
        /// </summary>
        /// <param name="ValueList"></param>
        /// <param name="MinR"></param>
        /// <param name="Rate"></param>
        /// <param name="RType">=0 linear R</param>
        /// <returns></returns>
        public List<Double> GetR(List<double> ValueList,double MinR,double Rate, double scale,int RType)
        {
            double MinValue = ValueList.Min();//Get the minValue
            List<double> RList = new List<double>();//Return R

            #region Linear R
            if (RType == 0)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    double R = ((ValueList[i] - MinValue) * Rate + MinR) * scale;
                    RList.Add(R);
                }
            }
            #endregion

            return RList;
        }

        /// <summary>
        /// Get the initial Circles with R
        /// </summary>
        /// <param name="pFeatureClass"></param>
        /// <returns></returns>
        public List<Circle> GetInitialCircle(IFeatureClass pFeatureClass,string ValueField,double MinR,double Rate,double scale, int RType)
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

                IArea pArea = pFeature.Shape as IArea;
                CacheCircle.CenterX = pArea.Centroid.X;
                CacheCircle.CenterY = pArea.Centroid.Y;

                i++;
                pFeature = pFeatureCursor.NextFeature();
            }
            #endregion

            #region assign R for circles
            List<double> RList=this.GetR(ValueList, MinR, Rate,scale, RType);
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
                IPolygon pPolygon = DMS.CircleToPolygon(CircleList[i]);
                PolygonObject pPo = DMS.PolygonConvert(pPolygon);
                pPo.ID = i;
                pPo.R = CircleList[i].Radius;

                PoList.Add(pPo);
            }

            return PoList;
        }
    }
}
