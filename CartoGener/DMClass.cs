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

        /// <summary>
        /// Compute the radius for each Circle
        /// </summary>
        /// <param name="ValueList"></param>
        /// <param name="MinR"></param>
        /// <param name="Rate"></param>a unit for 100
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
                    double R = ((ValueList[i] - MinValue) / 100000 * Rate + MinR) * scale;
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
                pPo.R = CircleList[i].Radius;

                PoList.Add(pPo);
            }

            return PoList;
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
        /// <param name="Iterations">迭代次数</param>
        public void DorlingBeams(ProxiGraph pg, SMap pMap, double scale, double E, double I, double A, int Iterations,int algType)
        {
            for (int i = 0; i < Iterations; i++)//迭代计算
            {
                Console.WriteLine(i.ToString());//标识

                #region 调用Beams算法
                AlgBeams algBeams = new AlgBeams(pg, pMap, E, I, A);
                //求吸引力-2014-3-20所用
                algBeams.OriginalGraph = pg;
                algBeams.Scale = scale;
                algBeams.AlgType = algType;
                algBeams.DoDisplacePgDorling(pMap);
                #endregion

                if (algBeams.isContinue == false)
                {
                    break;
                }
            }
        }
    }
}
