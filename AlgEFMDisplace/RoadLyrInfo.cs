using System;
using System.Collections.Generic;
using System.Text;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;

namespace AlgEFMDisplace
{
    public class RoadLyrInfo
    {
        public RoadGrade RoadGrade=null;            //道路等级
        public IFeatureLayer Lyr = null;            //图层对象
        public IGeometryCollection GeoSet = null;   //几何图形集合

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="roadGrade">道路等级</param>
        /// <param name="lyr">ArcGIS图层对象</param>
        public RoadLyrInfo(RoadGrade roadGrade, IFeatureLayer lyr)
        {
            RoadGrade = roadGrade;
            Lyr = lyr;
        }
        /// <summary>
        /// 创建几何图形对象集合
        /// </summary>
        public void CreateGeoSet()
        {
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;
            GeoSet = new GeometryBagClass();
            IFeatureCursor cursor = Lyr.Search(null, false);
            IFeature curFeature = null;
            IGeometry shp = null;
            while ((curFeature = cursor.NextFeature()) != null)
            {
                shp = curFeature.Shape;
                GeoSet.AddGeometry(shp, ref missing1, ref missing2);
            }
        }

    }
}
