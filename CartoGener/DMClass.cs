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

        /// <summary>
        /// Compute the radius for each Circle
        /// </summary>
        /// <param name="ValueList"></param>
        /// <param name="MinR"></param>
        /// <param name="Rate"></param>a unit for 100
        /// <param name="RType">=0 linear R</param>=1 SinR
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
                    double R = (((ValueList[i] - MinValue) / (MaxValue - MinValue)) * (MaxValue - MinValue) + MinR) * scale;
                    RList.Add(R);
                }
            }
            #endregion

            #region SinR
            else if (RType == 1)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    double R = (Math.Sin((ValueList[i] - MinValue) / (MaxValue - MinValue)) * (MaxR - MinR) + MinR) * scale;
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
        /// <returns></returns>
        public double GetAveCR(ProxiGraph pg, List<PolygonObject> PoList, List<double> ValueList, double MinR, double MaxR, double scale, int RType)
        {
            double AveCR = 0;
            int CCount = 0;
            double CCSumDis = 0;

            #region GetTheRList
            List<double> RList = new List<double>();//R

            if (RType == 0)
            {
                RList = this.GetR(ValueList, MinR, MaxR, scale, RType);
            }
            else if (RType == 1)
            {
                RList = this.GetR(ValueList, MinR, MaxR, scale, RType);
            }
            #endregion

            #region Compute the AveCR
            foreach (ProxiEdge Pe in pg.EdgeList)
            {
                ProxiNode Node1 = Pe.Node1;
                ProxiNode Node2 = Pe.Node2;

                double EdgeDis = DMS.GetDis(Node1, Node2);
                double RSDis = DMS.GetObjectByID(PoList, Node1.ID).R + DMS.GetObjectByID(PoList, Node2.ID).R;

                if (EdgeDis < RSDis)
                {
                    CCount++;
                    CCSumDis = (RSDis - EdgeDis) + CCSumDis;
                }
            }
            #endregion

            AveCR = CCSumDis / CCount;
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
        public List<double> GetFinalListR(ProxiGraph pg, List<PolygonObject> PoList, List<double> ValueList, double MinR, double initialMaxR, double scale, int RType, double CCTd)
        {
            double AveCR = this.GetAveCR(pg, PoList, ValueList, MinR, initialMaxR, scale, RType);
            List<double> RList = this.GetR(ValueList, MinR, initialMaxR, scale, RType);//Return R

            #region 智能阻尼振荡过程
            while (Math.Abs(AveCR - CCTd) < 0.01)
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
                AveCR = this.GetAveCR(pg, PoList, ValueList, MinR, initialMaxR, scale, RType);

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
        /// <param name="Iterations">迭代次数</param>
        public void DorlingBeams(ProxiGraph pg, SMap pMap, double scale, double E, double I, double A, int Iterations,int algType)
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
                          
                algBeams.DoDisplacePgDorling(pMap);// 调用Beams算法 
                if (algBeams.isContinue == false)
                {
                    break;
                }
            }
        }
    }
}
