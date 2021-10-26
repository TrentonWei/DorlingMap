using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Geometry;
using AuxStructureLib;

namespace ConflictDetection
{
    public class WriteSHP
    {
        /// <summary>
        /// 写冲突三角形到SHP
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <param name="TriangleList"></param>
        /// <param name="prj"></param>
        public static void Create_WriteConflictTri2Shp(string filePath, string fileName, ConflictDetection cd, esriSRProjCS4Type prj)
        {
            #region 创建一个线的shape文件
            string Folderpathstr = filePath;
            string LyrName = fileName;
            string strFolder = Folderpathstr;
            string strName = LyrName;
            string strShapeFieldName = "Shape";

            IFeatureWorkspace pFWS;//ESRI.ArcGIS.Geodatabase;
            IWorkspaceFactory pWorkspaceFactory = new ShapefileWorkspaceFactoryClass();//ESRI.ArcGIS.DataSourcesFile
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

            ISpatialReferenceFactory ispfac = new SpatialReferenceEnvironmentClass();
            IProjectedCoordinateSystem iprcoorsys = ispfac.CreateProjectedCoordinateSystem((int)prj);
            ISpatialReference pSpatialReference = iprcoorsys as ISpatialReference;

            IGeometryDef pGeomDef = new GeometryDefClass();
            IGeometryDefEdit pGeomDefEdit = pGeomDef as IGeometryDefEdit;
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolygon;
            pGeomDefEdit.SpatialReference_2 = pSpatialReference;
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

            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;
            pFieldEdit2.Name_2 = "P1";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField2);

            IField pField3;
            IFieldEdit pFieldEdit3;
            pField3 = new FieldClass();
            pFieldEdit3 = pField3 as IFieldEdit;
            pFieldEdit3.Length_2 = 30;
            pFieldEdit3.Name_2 = "P2";
            pFieldEdit3.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField3);

            IField pField4;
            IFieldEdit pFieldEdit4;
            pField4 = new FieldClass();
            pFieldEdit4 = pField4 as IFieldEdit;
            pFieldEdit4.Length_2 = 30;
            pFieldEdit4.Name_2 = "P3";
            pFieldEdit4.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField4);

            IField pField5;
            IFieldEdit pFieldEdit5;
            pField5 = new FieldClass();
            pFieldEdit5 = pField5 as IFieldEdit;
            pFieldEdit5.Length_2 = 30;
            pFieldEdit5.Name_2 = "E1";
            pFieldEdit5.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField5);

            IField pField6;
            IFieldEdit pFieldEdit6;
            pField6 = new FieldClass();
            pFieldEdit6 = pField6 as IFieldEdit;
            pFieldEdit6.Length_2 = 30;
            pFieldEdit6.Name_2 = "E2";
            pFieldEdit6.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField6);

            IField pField7;
            IFieldEdit pFieldEdit7;
            pField7 = new FieldClass();
            pFieldEdit7 = pField7 as IFieldEdit;
            pFieldEdit7.Length_2 = 30;
            pFieldEdit7.Name_2 = "E3";
            pFieldEdit7.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField7);

            IField pField8;
            IFieldEdit pFieldEdit8;
            pField8 = new FieldClass();
            pFieldEdit8 = pField8 as IFieldEdit;
            pFieldEdit8.Length_2 = 30;
            pFieldEdit8.Name_2 = "Type";
            pFieldEdit8.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pFieldEdit8);

            IField pField9;
            IFieldEdit pFieldEdit9;
            pField9 = new FieldClass();
            pFieldEdit9 = pField9 as IFieldEdit;
            pFieldEdit9.Length_2 = 30;
            pFieldEdit9.Name_2 = "MinSep";
            pFieldEdit9.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pFieldEdit9);

            IField pField10;
            IFieldEdit pFieldEdit10;
            pField10 = new FieldClass();
            pFieldEdit10 = pField10 as IFieldEdit;
            pFieldEdit10.Length_2 = 30;
            pFieldEdit10.Name_2 = "MinTriID";
            pFieldEdit10.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pFieldEdit10);

           #endregion

            #region 创建要素类
            IFeatureClass pFeatClass;
            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
            #endregion
            #endregion

            #region 向线层添加线要素

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

                int n = cd.ConflictList.Count;
                if (n == 0)
                    return;

                for (int j = 0; j < n; j++)
                {
                    List<Triangle> TriangleList = cd.ConflictList[j].TriangleList;
                    int type = cd.ConflictList[j].ConflictType;
                    int MinTriID = cd.ConflictList[j].MinSeparateTri.ID;
                    float Minsep = cd.ConflictList[j].MinSeparate;
                    if (TriangleList == null)
                        continue;
                    for (int i = 0; i < cd.ConflictList[j].TriangleList.Count; i++)
                    {

                        IFeature feature = pFeatClass.CreateFeature();
                        IGeometry shp = new PolygonClass();
                        // shp.SpatialReference = mapControl.SpatialReference;
                        IPointCollection pointSet = shp as IPointCollection;
                        IPoint curResultPoint = null;
                        TriNode curPoint = null;
                        if (TriangleList[i] == null)
                            continue;

                        curPoint = TriangleList[i].point1; ;
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                        curPoint = TriangleList[i].point2;
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                        curPoint = TriangleList[i].point3;
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                        curPoint = TriangleList[i].point1;
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                        feature.Shape = shp;
                        feature.set_Value(2, TriangleList[i].ID);
                        feature.set_Value(3, TriangleList[i].point1.ID);
                        feature.set_Value(4, TriangleList[i].point2.ID);
                        feature.set_Value(5, TriangleList[i].point3.ID);
                        feature.set_Value(6, TriangleList[i].edge1.ID);
                        feature.set_Value(7, TriangleList[i].edge2.ID);
                        feature.set_Value(8, TriangleList[i].edge3.ID);
                        feature.set_Value(9, type);
                        feature.set_Value(10, Minsep);
                        feature.set_Value(11, MinTriID);

                        feature.Store();//保存IFeature对象  
                        fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                    }
                }

                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                // MessageBox.Show("异常信息" + ex.Message);
            }
            #endregion
        }

        /// <summary>
        /// 将骨架线写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="Skeleton_SegmentList">骨架线段列表</param>
        public static void Create_WriteSkeleton_Segment2Shp(string filePath, string fileName, Skeleton ske, esriSRProjCS4Type prj)
        {
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

            ISpatialReferenceFactory ispfac = new SpatialReferenceEnvironmentClass();
            IProjectedCoordinateSystem iprcoorsys = ispfac.CreateProjectedCoordinateSystem((int)prj);
            ISpatialReference pSpatialReference = iprcoorsys as ISpatialReference;

            IGeometryDef pGeomDef = new GeometryDefClass();
            IGeometryDefEdit pGeomDefEdit = pGeomDef as IGeometryDefEdit;
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolyline;
            pGeomDefEdit.SpatialReference_2 = pSpatialReference;
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
            pFieldEdit1.Name_2 = "LeftRoadSegID";
            pFieldEdit1.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField1);

            //属性字段2
            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;
            pFieldEdit2.Name_2 = "RightRoadSegID";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField2);

            //属性字段3
            IField pField3;
            IFieldEdit pFieldEdit3;
            pField3 = new FieldClass();
            pFieldEdit3 = pField3 as IFieldEdit;
            pFieldEdit3.Length_2 = 30;
            pFieldEdit3.Name_2 = "FrontRoadSegID";
            pFieldEdit3.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField3);

            //属性字段4
            IField pField4;
            IFieldEdit pFieldEdit4;
            pField4 = new FieldClass();
            pFieldEdit4 = pField4 as IFieldEdit;
            pFieldEdit4.Length_2 = 30;
            pFieldEdit4.Name_2 = "BackRoadSegID";
            pFieldEdit4.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField4);
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

                int n = ske.Skeleton_SegmentList.Count;
                List<Skeleton_Segment> Skeleton_SegmentList = ske.Skeleton_SegmentList;
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
                    if (Skeleton_SegmentList[i] == null)
                        continue;
                    int m = Skeleton_SegmentList[i].Axis_Points.Count;

                    for (int k = 0; k < m; k++)
                    {
                        curPoint = Skeleton_SegmentList[i].Axis_Points[k];
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    }
                    feature.Shape = shp;
                    feature.set_Value(2, Skeleton_SegmentList[i].LeftRoadSegID);//左路段  
                    feature.set_Value(3, Skeleton_SegmentList[i].RightRoadSegID);//右路段
                    feature.set_Value(4, Skeleton_SegmentList[i].FrontRoadSegID);//前路段
                    feature.set_Value(5, Skeleton_SegmentList[i].BackRoadSegID);//后路段

                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }

                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                // MessageBox.Show("异常信息" + ex.Message);
            }
            #endregion
        }

        /// <summary>
        /// 将骨架线写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="Skeleton_SegmentList">骨架线段列表</param>
        public static void Create_WriteSkeleton_Segment2Shp(string filePath, string fileName, AuxStructureLib.Skeleton ske, esriSRProjCS4Type prj)
        {
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

            ISpatialReferenceFactory ispfac = new SpatialReferenceEnvironmentClass();
            IProjectedCoordinateSystem iprcoorsys = ispfac.CreateProjectedCoordinateSystem((int)prj);
            ISpatialReference pSpatialReference = iprcoorsys as ISpatialReference;

            IGeometryDef pGeomDef = new GeometryDefClass();
            IGeometryDefEdit pGeomDefEdit = pGeomDef as IGeometryDefEdit;
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolyline;
            pGeomDefEdit.SpatialReference_2 = pSpatialReference;
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

            //属性字段2
            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit2.Name_2 = "LeftOID";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField2);

            //属性字段3
            IField pField3;
            IFieldEdit pFieldEdit3;
            pField3 = new FieldClass();
            pFieldEdit3 = pField3 as IFieldEdit;
            pFieldEdit3.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit3.Name_2 = "LeftOType";
            pFieldEdit3.Type_2 = esriFieldType.esriFieldTypeString;
            pFieldsEdit.AddField(pField3);

            //属性字段4
            IField pField4;
            IFieldEdit pFieldEdit4;
            pField4 = new FieldClass();
            pFieldEdit4 = pField4 as IFieldEdit;
            pFieldEdit4.Length_2 = 30;
            pFieldEdit4.Name_2 = "RightOID";
            pFieldEdit4.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField4);

            //属性字段5
            IField pField5;
            IFieldEdit pFieldEdit5;
            pField5 = new FieldClass();
            pFieldEdit5 = pField5 as IFieldEdit;
            pFieldEdit5.Length_2 = 30;
            pFieldEdit5.Name_2 = "RightOType";
            pFieldEdit5.Type_2 = esriFieldType.esriFieldTypeString;
            pFieldsEdit.AddField(pField5);


            //属性字段6
            IField pField6;
            IFieldEdit pFieldEdit6;
            pField6 = new FieldClass();
            pFieldEdit6 = pField6 as IFieldEdit;
            pFieldEdit6.Length_2 = 30;
            pFieldEdit6.Name_2 = "FrontOID";
            pFieldEdit6.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField6);

            //属性字段7
            IField pField7;
            IFieldEdit pFieldEdit7;
            pField7 = new FieldClass();
            pFieldEdit7 = pField7 as IFieldEdit;
            pFieldEdit7.Length_2 = 30;
            pFieldEdit7.Name_2 = "FrontOType";
            pFieldEdit7.Type_2 = esriFieldType.esriFieldTypeString;
            pFieldsEdit.AddField(pField7);

            //属性字段8
            IField pField8;
            IFieldEdit pFieldEdit8;
            pField8 = new FieldClass();
            pFieldEdit8 = pField8 as IFieldEdit;
            pFieldEdit8.Length_2 = 30;
            pFieldEdit8.Name_2 = "BackOID";
            pFieldEdit8.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField8);

            //属性字段9
            IField pField9;
            IFieldEdit pFieldEdit9;
            pField9 = new FieldClass();
            pFieldEdit9 = pField9 as IFieldEdit;
            pFieldEdit9.Length_2 = 30;
            pFieldEdit9.Name_2 = "BackOType";
            pFieldEdit9.Type_2 = esriFieldType.esriFieldTypeString;
            pFieldsEdit.AddField(pField9);

            //属性字段10
            IField pField10;
            IFieldEdit pFieldEdit10;
            pField10 = new FieldClass();
            pFieldEdit10 = pField10 as IFieldEdit;
            pFieldEdit10.Length_2 = 30;
            pFieldEdit10.Name_2 = "MinDis";
            pFieldEdit10.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField10);

            //属性字段11
            IField pField11;
            IFieldEdit pFieldEdit11;
            pField11 = new FieldClass();
            pFieldEdit11 = pField11 as IFieldEdit;
            pFieldEdit11.Length_2 = 30;
            pFieldEdit11.Name_2 = "AveDis";
            pFieldEdit11.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField11);

            //属性字段12
            IField pField12;
            IFieldEdit pFieldEdit12;
            pField12 = new FieldClass();
            pFieldEdit12 = pField12 as IFieldEdit;
            pFieldEdit12.Length_2 = 30;
            pFieldEdit12.Name_2 = "WAveDis";
            pFieldEdit12.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField12);

            //属性字段13
            IField pField13;
            IFieldEdit pFieldEdit13;
            pField13 = new FieldClass();
            pFieldEdit13 = pField13 as IFieldEdit;
            pFieldEdit13.Length_2 = 30;
            pFieldEdit13.Name_2 = "Length";
            pFieldEdit13.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField13);

            //属性字段14
            IField pField14;
            IFieldEdit pFieldEdit14;
            pField14 = new FieldClass();
            pFieldEdit14 = pField14 as IFieldEdit;
            pFieldEdit14.Length_2 = 30;
            pFieldEdit14.Name_2 = "GapArea";
            pFieldEdit14.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField14);

            //属性字段15
            IField pField15;
            IFieldEdit pFieldEdit15;
            pField15 = new FieldClass();
            pFieldEdit15 = pField15 as IFieldEdit;
            pFieldEdit15.Length_2 = 30;
            pFieldEdit15.Name_2 = "DVD-N";
            pFieldEdit15.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField15);

           //属性字段16
            IField pField16;
            IFieldEdit pFieldEdit16;
            pField16 = new FieldClass();
            pFieldEdit16 = pField16 as IFieldEdit;
            pFieldEdit16.Length_2 = 30;
            pFieldEdit16.Name_2 = "DVD-NE";
            pFieldEdit16.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField16);

            //属性字段17
            IField pField17;
            IFieldEdit pFieldEdit17;
            pField17 = new FieldClass();
            pFieldEdit17 = pField17 as IFieldEdit;
            pFieldEdit17.Length_2 = 30;
            pFieldEdit17.Name_2 = "DVD-E";
            pFieldEdit17.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField17);

            //属性字段18
            IField pField18;
            IFieldEdit pFieldEdit18;
            pField18 = new FieldClass();
            pFieldEdit18 = pField18 as IFieldEdit;
            pFieldEdit18.Length_2 = 30;
            pFieldEdit18.Name_2 = "DVD-NW";
            pFieldEdit18.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField18);

            //属性字段14
            IField pField19;
            IFieldEdit pFieldEdit19;
            pField19 = new FieldClass();
            pFieldEdit19 = pField19 as IFieldEdit;
            pFieldEdit19.Length_2 = 30;
            pFieldEdit19.Name_2 = "DVD-W";
            pFieldEdit19.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField19);

            //属性字段14
            IField pField20;
            IFieldEdit pFieldEdit20;
            pField20 = new FieldClass();
            pFieldEdit20 = pField20 as IFieldEdit;
            pFieldEdit20.Length_2 = 30;
            pFieldEdit20.Name_2 = "DVD-S";
            pFieldEdit20.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField20);

            //属性字段14
            IField pField21;
            IFieldEdit pFieldEdit21;
            pField21 = new FieldClass();
            pFieldEdit21 = pField21 as IFieldEdit;
            pFieldEdit21.Length_2 = 30;
            pFieldEdit21.Name_2 = "DVD-SE";
            pFieldEdit21.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField21);

            //属性字段14
            IField pField22;
            IFieldEdit pFieldEdit22;
            pField22 = new FieldClass();
            pFieldEdit22 = pField22 as IFieldEdit;
            pFieldEdit22.Length_2 = 30;
            pFieldEdit22.Name_2 = "DVD-SW";
            pFieldEdit22.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField22);


            //属性字段14
            IField pField23;
            IFieldEdit pFieldEdit23;
            pField23 = new FieldClass();
            pFieldEdit23 = pField23 as IFieldEdit;
            pFieldEdit23.Length_2 = 30;
            pFieldEdit23.Name_2 = "IsIntra";
            pFieldEdit23.Type_2 = esriFieldType.esriFieldTypeString;
            pFieldsEdit.AddField(pField23);
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

                int n = ske.Skeleton_ArcList.Count;
                List<Skeleton_Arc> Skeleton_SegmentList = ske.Skeleton_ArcList;
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
                    if (Skeleton_SegmentList[i] == null)
                        continue;
                    int m = Skeleton_SegmentList[i].PointList.Count;

                    for (int k = 0; k < m; k++)
                    {
                        curPoint = Skeleton_SegmentList[i].PointList[k];
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    }
                    feature.Shape = shp;

                    feature.set_Value(2, Skeleton_SegmentList[i].ID);

                    int id = -1;
                    string type = "";
                    if (Skeleton_SegmentList[i].LeftMapObj != null)
                    {
                        id = Skeleton_SegmentList[i].LeftMapObj.ID;
                        type = Skeleton_SegmentList[i].LeftMapObj.GetType().ToString();
                    }
                    else
                    {
                        id = -1; type = "";
                    }
                    feature.set_Value(3, id);
                    feature.set_Value(4, type);
                    if (Skeleton_SegmentList[i].RightMapObj != null)
                    {
                        id = Skeleton_SegmentList[i].RightMapObj.ID;
                        type = Skeleton_SegmentList[i].RightMapObj.GetType().ToString();
                    }
                    else
                    {
                        id = -1; type = "";
                    }
                    feature.set_Value(5, id);
                    feature.set_Value(6, type);
                    if (Skeleton_SegmentList[i].FrontMapObj != null)
                    {
                        id = Skeleton_SegmentList[i].FrontMapObj.ID;
                        type = Skeleton_SegmentList[i].FrontMapObj.GetType().ToString();
                    }
                    else
                    {
                        id = -1; type = "";
                    }
                    feature.set_Value(7, id);
                    feature.set_Value(8, type);
                    if (Skeleton_SegmentList[i].BackMapObj != null)
                    {
                        id = Skeleton_SegmentList[i].BackMapObj.ID;
                        type = Skeleton_SegmentList[i].BackMapObj.GetType().ToString();
                    }
                    else
                    {
                        id = -1;
                        type = "";
                    }
                    feature.set_Value(9, id);
                    feature.set_Value(10, type);
                    feature.set_Value(11, Skeleton_SegmentList[i].NearestEdge.NearestDistance);
                    feature.set_Value(12, Skeleton_SegmentList[i].AveDistance);
                    feature.set_Value(13, Skeleton_SegmentList[i].WAD);
                    feature.set_Value(14, Skeleton_SegmentList[i].Length);
                    feature.set_Value(15, Skeleton_SegmentList[i].GapArea);

                    feature.set_Value(16, Skeleton_SegmentList[i].DVD[0]);
                    feature.set_Value(17, Skeleton_SegmentList[i].DVD[1]);
                    feature.set_Value(18, Skeleton_SegmentList[i].DVD[2]);
                    feature.set_Value(19, Skeleton_SegmentList[i].DVD[3]);
                    feature.set_Value(20, Skeleton_SegmentList[i].DVD[4]);
                    feature.set_Value(21, Skeleton_SegmentList[i].DVD[5]);
                    feature.set_Value(22, Skeleton_SegmentList[i].DVD[6]);
                    feature.set_Value(23, Skeleton_SegmentList[i].DVD[7]);
                    if (Skeleton_SegmentList[i].LeftMapObj == Skeleton_SegmentList[i].RightMapObj)
                    {
                        feature.set_Value(24, "Intra");
                    }
                    else
                    {
                        feature.set_Value(24, "Inter");
                    }

                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }

                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                // MessageBox.Show("异常信息" + ex.Message);
            }
            #endregion
        }

        /// <summary>
        /// 将线写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="Skeleton_SegmentList">线列表</param>
        public static void Create_WritePolylineObject2Shp(string filePath, string fileName, List<PolylineObject> plist, esriSRProjCS4Type prj)
        {
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

            ISpatialReferenceFactory ispfac = new SpatialReferenceEnvironmentClass();
            IProjectedCoordinateSystem iprcoorsys = ispfac.CreateProjectedCoordinateSystem((int)prj);
            ISpatialReference pSpatialReference = iprcoorsys as ISpatialReference;

            IGeometryDef pGeomDef = new GeometryDefClass();
            IGeometryDefEdit pGeomDefEdit = pGeomDef as IGeometryDefEdit;
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolyline;
            pGeomDefEdit.SpatialReference_2 = pSpatialReference;
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

                int n = plist.Count;
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
                    if (plist[i] == null)
                        continue;
                    int m = plist[i].PointList.Count;

                    for (int k = 0; k < m; k++)
                    {
                        curPoint = plist[i].PointList[k];
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    }
                    feature.Shape = shp;
                    feature.set_Value(2, plist[i].ID);//编号 
                 

                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }

                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                // MessageBox.Show("异常信息" + ex.Message);
            }
            #endregion
        }




        
    }
}
