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

namespace CartoGener
{
    class FeatureHandle
    {
        #region 创建指定形状的面文件
        public IFeatureClass createPolygonshapefile(ISpatialReference pSpatialReference, string filepath, string filename)
        {
            IFields pFields = new FieldsClass();
            IFieldsEdit pFieldsEdit = (IFieldsEdit)pFields;

            IField pField = new FieldClass();
            IFieldEdit pFieldEdit = (IFieldEdit)pField;
            pFieldEdit.Name_2 = "SHAPE";
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;
            IGeometryDefEdit pGeoDef = new GeometryDefClass();
            IGeometryDefEdit pGeoDefEdit = (IGeometryDefEdit)pGeoDef;
            pGeoDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolygon;
            pGeoDefEdit.HasZ_2 = true;
            pGeoDefEdit.SpatialReference_2 = pSpatialReference;
            pFieldEdit.GeometryDef_2 = pGeoDef;
            pFieldsEdit.AddField(pField);

            IWorkspaceFactory factory = new ShapefileWorkspaceFactory();
            IFeatureWorkspace wspace = factory.OpenFromFile(filepath, 0) as IFeatureWorkspace;

            IFeatureClass out_shpfileclass = wspace.CreateFeatureClass(filename, pFields, null, null, esriFeatureType.esriFTSimple, "shape", "");
            return out_shpfileclass;
        }
        #endregion

        #region 创建指定形状的点文件
        public IFeatureClass createPointshapefile(ISpatialReference pSpatialReference, string filepath, string filename)
        {
            IFields pFields = new FieldsClass();
            IFieldsEdit pFieldsEdit = (IFieldsEdit)pFields;

            IField pField = new FieldClass();
            IFieldEdit pFieldEdit = (IFieldEdit)pField;
            pFieldEdit.Name_2 = "SHAPE";
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;
            IGeometryDefEdit pGeoDef = new GeometryDefClass();
            IGeometryDefEdit pGeoDefEdit = (IGeometryDefEdit)pGeoDef;
            pGeoDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPoint;
            pGeoDefEdit.SpatialReference_2 = pSpatialReference;
            pFieldEdit.GeometryDef_2 = pGeoDef;
            pFieldsEdit.AddField(pField);

            IWorkspaceFactory factory = new ShapefileWorkspaceFactory();
            IFeatureWorkspace wspace = factory.OpenFromFile(filepath, 0) as IFeatureWorkspace;

            IFeatureClass out_shpfileclass = wspace.CreateFeatureClass(filename, pFields, null, null, esriFeatureType.esriFTSimple, "shape", "");
            return out_shpfileclass;
        }
        #endregion

        #region 创建指定形状的线文件
        public IFeatureClass createLineshapefile(ISpatialReference pSpatialReference, string filepath, string filename)
        {
            IFields pFields = new FieldsClass();
            IFieldsEdit pFieldsEdit = (IFieldsEdit)pFields;

            IField pField = new FieldClass();
            IFieldEdit pFieldEdit = (IFieldEdit)pField;
            pFieldEdit.Name_2 = "SHAPE";
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;
            IGeometryDefEdit pGeoDef = new GeometryDefClass();
            IGeometryDefEdit pGeoDefEdit = (IGeometryDefEdit)pGeoDef;
            pGeoDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolyline;
            pGeoDefEdit.SpatialReference_2 = pSpatialReference;
            pFieldEdit.GeometryDef_2 = pGeoDef;
            pFieldsEdit.AddField(pField);

            IWorkspaceFactory factory = new ShapefileWorkspaceFactory();
            IFeatureWorkspace wspace = factory.OpenFromFile(filepath, 0) as IFeatureWorkspace;

            IFeatureClass out_shpfileclass = wspace.CreateFeatureClass(filename, pFields, null, null, esriFeatureType.esriFTSimple, "shape", "");
            return out_shpfileclass;
        }
        #endregion

        #region 获取FeatureClass
        public IFeatureClass GetFeatureClass(IMap Map, string s)
        {
            int ILayerCount;

            ILayerCount = Map.LayerCount;
            IFeatureClass pFeatureClass1 = null;

            if (ILayerCount <= 0)
            {
                return null;
            }

            else
            {
                for (int LayerIndex1 = 0; LayerIndex1 < ILayerCount; LayerIndex1++)
                {
                    ILayer Shapelayer1 = Map.get_Layer(LayerIndex1);
                    if (Shapelayer1.Name == s)
                    {
                        IFeatureLayer FeatureLayer1;
                        FeatureLayer1 = (IFeatureLayer)Shapelayer1;

                        pFeatureClass1 = FeatureLayer1.FeatureClass;
                    }
                }
            }

            return pFeatureClass1;
        }
        #endregion

        #region 获取所有的Features
        public List<IFeature> GetFeatures(IMap Map, string s)
        {
            List<IFeature> FeatureList = new List<IFeature>();

            IFeatureClass sFeatureClass = this.GetFeatureClass(Map, s);
            IFeatureCursor sFeatureCursor = sFeatureClass.Update(null, false);
            IFeature sFeature = sFeatureCursor.NextFeature();
            while (sFeature != null)
            {
                FeatureList.Add(sFeature);
                sFeature = sFeatureCursor.NextFeature();
            }

            return FeatureList;
        }
        #endregion

        #region 获取指定名字的图层IFeatureLayer
        public IFeatureLayer GetLayer(IMap pMap, string s)
        {
            int ILayerCount;

            ILayerCount = pMap.LayerCount;
            IFeatureLayer FeatureLayer1 = null;

            if (ILayerCount <= 0)
            {
                return null;
            }

            else
            {
                for (int LayerIndex1 = 0; LayerIndex1 < ILayerCount; LayerIndex1++)
                {
                    ILayer Shapelayer1 = pMap.get_Layer(LayerIndex1);
                    if (Shapelayer1.Name == s)
                    {
                        FeatureLayer1 = (IFeatureLayer)Shapelayer1;
                    }
                }
            }

            return FeatureLayer1;
        }
        #endregion

        #region 获取指定名字的图层IFeatureLayer
        public ILayer GetiLayer(IMap pMap, string s)
        {
            int ILayerCount;

            ILayerCount = pMap.LayerCount;
            ILayer Layer1 = null;

            if (ILayerCount <= 0)
            {
                return null;
            }

            else
            {
                for (int LayerIndex1 = 0; LayerIndex1 < ILayerCount; LayerIndex1++)
                {
                    Layer1 = pMap.get_Layer(LayerIndex1);
                }
            }

            return Layer1;
        }
        #endregion

        #region 获取指定名字的图层IFeatureLayer
        public IRasterLayer GetRasterLayer(IMap pMap, string s)
        {
            int ILayerCount;

            ILayerCount = pMap.LayerCount;
            IRasterLayer rLayer = null;

            if (ILayerCount <= 0)
            {
                return null;
            }

            else
            {
                for (int LayerIndex1 = 0; LayerIndex1 < ILayerCount; LayerIndex1++)
                {
                    rLayer = pMap.get_Layer(LayerIndex1) as IRasterLayer;
                }
            }

            return rLayer;
        }
        #endregion

        #region 添加字段 //字段名字添加不能过长
        public void AddField(IFeatureClass pFeatureClass, string name, esriFieldType FieldType)
        {
            if (pFeatureClass.Fields.FindField(name) < 0)
            {
                IFeatureClass pFc = (IFeatureClass)pFeatureClass;
                IClass pClass = pFc as IClass;

                IFieldsEdit fldsE = pFc.Fields as IFieldsEdit;
                IField fld = new FieldClass();
                IFieldEdit2 fldE = fld as IFieldEdit2;
                fldE.Type_2 = FieldType;
                fldE.Name_2 = name;
                pClass.AddField(fld);
            }
        }
        #endregion

        #region 将数据存储到字段下
        public void DataStore(IFeatureClass pFeatureClass, IFeature pFeature, string s, int t)
        {
            IDataset dataset = pFeatureClass as IDataset;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit wse = workspace as IWorkspaceEdit;

            IFields pFields = pFeature.Fields;

            wse.StartEditing(false);
            wse.StartEditOperation();

            int fnum;
            fnum = pFields.FieldCount;

            for (int m = 0; m < fnum; m++)
            {
                if (pFields.get_Field(m).Name == s)
                {
                    int field1 = pFields.FindField(s);
                    pFeature.set_Value(field1, t);
                    pFeature.Store();
                }
            }

            wse.StopEditOperation();
            wse.StopEditing(true);
        }
        #endregion

        #region  根据线要素生成多边形
        public IPolygon RoundLineToPolygon(IFeatureClass roadfeatureClass)
        {
            IFeature pfeature;
            IPolyline pPolyline;
            IPolygon pPolygon;
            ISegmentCollection pPath = new PathClass();
            ISegmentCollection pPolylineSegments;
            int count = roadfeatureClass.FeatureCount(null);
            for (int i = 0; i < count; i++)
            {
                pfeature = roadfeatureClass.GetFeature(i);
                pPolyline = pfeature.Shape as IPolyline;
                pPolylineSegments = pPolyline as ISegmentCollection;
                pPath.AddSegmentCollection(pPolylineSegments);
            }
            ISegmentCollection pSegCollRing = new RingClass();
            pSegCollRing.AddSegmentCollection(pPath);
            IRing Ring = pSegCollRing as IRing;
            Ring.Close();//闭合

            object missing = Type.Missing;
            IGeometryCollection GeoColl = new PolygonClass();
            GeoColl.AddGeometry(Ring, ref missing, ref missing);
            pPolygon = GeoColl as IPolygon;//道路围成多边形
            pPolygon.SimplifyPreserveFromTo();
            return pPolygon;
        }
        #endregion
    }
}
