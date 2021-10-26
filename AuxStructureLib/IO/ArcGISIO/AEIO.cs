using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Geometry;
using System.Windows.Forms;
using ESRI.ArcGIS.Carto;
using System.Data;

namespace AuxStructureLib.IO
{
    /// <summary>
    /// ArcGIS Engine数据输入输出方法类
    /// </summary>
    public class AEIO
    {

        /// <summary>
        /// 写要素类
        /// </summary>
        /// <param name="filePath">存储路径-数据库名</param>
        /// <param name="PolylineObjList">线对象列表</param>
        /// <param name="fileName">文件名-图层名</param>
        /// <param name="prj">投影</param>
        /// <returns>返回是否成功</returns>
        public static bool Create_Write_FeatureClass(string filePath,List<PolylineObject> PolylineObjList,string fileName,esriSRProjCS4Type prj)
        {
            #region 创建一个线的要素类文件
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
            pFeatClass = pFWS.CreateFeatureClass(fileName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
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
                    return false;
                //获取顶点图层的数据集，并创建工作空间
                IDataset dataset = (IDataset)pFeatClass;
                IWorkspace workspace = dataset.Workspace;
                IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
                //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
                IFeatureClassWrite fr = (IFeatureClassWrite)pFeatClass;
                //注意：此时，所编辑数据不能被其他程序打开
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();

                int n = PolylineObjList.Count;
                // List<Skeleton_Segment> Skeleton_SegmentList = ske.Skeleton_SegmentList;
                if (n == 0)
                    return false;

                for (int i = 0; i < n; i++)
                {


                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PolylineClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = null;
                    Node curPoint = null;
                    if (PolylineObjList == null)
                        continue;
                    int m = PolylineObjList[i].PointList.Count;

                    for (int k = 0; k < m; k++)
                    {
                        curPoint = PolylineObjList[i].PointList[k];
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    }
                    feature.Shape = shp;
                    //feature.set_Value(2, PolylineObjList[i].ID);//编号 
      
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
                return false;

            }
            #endregion

            return true;
        }
        /// <summary>
        /// 打开要素类写入线对象
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="featureclassName"></param>
        /// <param name="mapControlMain"></param>
        public static void Open_WriteFeatures(string filePath, List<PolylineObject> PolylineObjList, string featureclassName, esriSRProjCS4Type prj)
        {
            
            string Folderpathstr = filePath;
            string LyrName = featureclassName;

            IFeatureWorkspace pFWS;
            //载入图层
            IWorkspaceFactory pWorkspaceFactory = null;//ESRI.ArcGIS.DataSourcesFile           pFWS = pWorkspaceFactory.OpenFromFile(strFolder, 0) as IFeatureWorkspace;
            IWorkspace pWorkspace = null;
            pFWS = pWorkspace as IFeatureWorkspace;

            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            try//打开工作空间
            {
                pWorkspaceFactory = new AccessWorkspaceFactoryClass();//ESRI.ArcGIS.DataSourcesFile           pFWS = pWorkspaceFactory.OpenFromFile(strFolder, 0) as IFeatureWorkspace;
                pWorkspace = pWorkspaceFactory.OpenFromFile(filePath, 0);
                pFWS = pWorkspace as IFeatureWorkspace;
                //加载矢量图层
                IFeatureClass pFeatClass = pFWS.OpenFeatureClass(featureclassName);



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

                int n = PolylineObjList.Count;
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
                    Node curPoint = null;
                    if (PolylineObjList == null)
                        continue;
                    int m = PolylineObjList[i].PointList.Count;

                    for (int k = 0; k < m; k++)
                    {
                        curPoint = PolylineObjList[i].PointList[k];
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    }
                    feature.Shape = shp;
                    //feature.set_Value(2, PolylineObjList[i].ID);//编号 

                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }

                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }

            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }


        /// <summary>
        /// 将三角形写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="Skeleton_SegmentList">骨架线段列表</param>
        /// 
         public static void Open_WriteTriangles(string filePath, List<Triangle> TriangleList,  string featureclassName, esriSRProjCS4Type prj)
        {

            string Folderpathstr = filePath;
            string LyrName = featureclassName;
            IFeatureWorkspace pFWS;
            //载入图层
            IWorkspaceFactory pWorkspaceFactory = null;//ESRI.ArcGIS.DataSourcesFile           pFWS = pWorkspaceFactory.OpenFromFile(strFolder, 0) as IFeatureWorkspace;
            IWorkspace pWorkspace = null;
            pFWS = pWorkspace as IFeatureWorkspace;

            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            try//打开工作空间
            {
                pWorkspaceFactory = new AccessWorkspaceFactoryClass();//ESRI.ArcGIS.DataSourcesFile           pFWS = pWorkspaceFactory.OpenFromFile(strFolder, 0) as IFeatureWorkspace;
                pWorkspace = pWorkspaceFactory.OpenFromFile(filePath, 0);
                pFWS = pWorkspace as IFeatureWorkspace;
                //加载矢量图层
                IFeatureClass pFeatClass = pFWS.OpenFeatureClass(featureclassName);



                if (pFeatClass == null)
                    return;
                //获取顶点图层的数据集，并创建工作空间
                IDataset dataset = (IDataset)pFeatClass;
                IWorkspace workspace = dataset.Workspace;
                IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
                //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
                IFeatureClassWrite fr = (IFeatureClassWrite)pFeatClass;

            #region 向线层添加线要素


 
                //注意：此时，所编辑数据不能被其他程序打开
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();

                int n = TriangleList.Count;
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
         /// 写要素类-有错误2014-4-13
         /// </summary>
         /// <param name="filePath">存储路径-数据库名</param>
         /// <param name="PolylineObjList">线对象列表</param>
         /// <param name="fileName">文件名-图层名</param>
         /// <param name="prj">投影</param>
         /// <returns>返回是否成功</returns>
         public static bool Create_Write_TableData(string filePath, DataTable dataTable, string fileName)
         {
            // #region 创建一个表格
             string Folderpathstr = filePath;
             string LyrName = fileName;
             IFeatureWorkspace pFWS;//ESRI.ArcGIS.Geodatabase;
             IWorkspaceFactory pWorkspaceFactory = new AccessWorkspaceFactoryClass();//ESRI.ArcGIS.DataSourcesFile           pFWS = pWorkspaceFactory.OpenFromFile(strFolder, 0) as IFeatureWorkspace;
             IWorkspace pWorkspace = pWorkspaceFactory.OpenFromFile(filePath, 0);
             pFWS = pWorkspace as IFeatureWorkspace;
             ////创建一个字段集
             //IFields pFields = new ESRI.ArcGIS.Geodatabase.FieldsClass();
             //IFieldsEdit pFieldsEdit;
             //pFieldsEdit = pFields as IFieldsEdit;

             ////#region 创建字段
             ////foreach (DataColumn dc in dataTable.Columns)
             ////{
             //string fieldName = "ssd";
             //// string type = dc.DataType.ToString();

             //IField pField;
             //IFieldEdit pFieldEdit;
             //pField = new FieldClass();
             //pFieldEdit = pField as IFieldEdit;
             //pFieldEdit.Length_2 = 30;
             //pFieldEdit.Name_2 = fieldName;
             ////if()
             //pFieldEdit.Type_2 = esriFieldType.esriFieldTypeInteger;
             //pFieldsEdit.AddField(pField);
             ////}
             //#endregion

             #region 新建表字段

             IField pField = null;

             IFields fields = new FieldsClass();

             IFieldsEdit fieldsEdit = (IFieldsEdit)fields;

             fieldsEdit.FieldCount_2 = 3;



             pField = new FieldClass();

             IFieldEdit fieldEdit = (IFieldEdit)pField;

             fieldEdit.Name_2 = "FromField";

             fieldEdit.AliasName_2 = "开始字段值";

             fieldEdit.Type_2 = esriFieldType.esriFieldTypeDouble;

             fieldEdit.Editable_2 = true;



             //添加开始字段

             fieldsEdit.set_Field(0, pField);



             IField pField1 = new FieldClass();

             IFieldEdit fieldEdit1 = (IFieldEdit)pField1;

             fieldEdit1.Name_2 = "ToField";

             fieldEdit1.AliasName_2 = "结束字段值";

             fieldEdit1.Type_2 = esriFieldType.esriFieldTypeDouble;

             fieldEdit1.Editable_2 = true;



             //添加结束字段

             fieldsEdit.set_Field(1, pField1);



             IField pField2 = new FieldClass();

             IFieldEdit fieldEdit2 = (IFieldEdit)pField2;

             fieldEdit2.Name_2 = "outField";

             fieldEdit2.AliasName_2 = "分类字段值";

             fieldEdit2.Type_2 = esriFieldType.esriFieldTypeDouble;

             fieldEdit2.Editable_2 = true;

             //添加重分类字段

             fieldsEdit.set_Field(2, pField2);



             #endregion



             #region 创建要素类
             ITable pTable;
             pTable = pFWS.CreateTable(fileName, fieldsEdit, null, null, "");
             #endregion


             #region 向线层添加线要素

             object missing1 = Type.Missing;
             object missing2 = Type.Missing;

             IWorkspaceEdit pIWorkspaceEdit = null;
             IDataset pIDataset = (IDataset)pTable;

             if (pIDataset != null)
             {
                 pIWorkspaceEdit = (IWorkspaceEdit)pIDataset.Workspace;
             }
             try
             {
                 if (pTable == null)
                     return false;
                 //获取顶点图层的数据集，并创建工作空间
                 IDataset dataset = (IDataset)pTable;
                 IWorkspace workspace = dataset.Workspace;
                 IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
                 //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
                 IFeatureClassWrite fr = (IFeatureClassWrite)pTable;
                 //注意：此时，所编辑数据不能被其他程序打开
                 workspaceEdit.StartEditing(true);
                 workspaceEdit.StartEditOperation();

                 int n = dataTable.Rows.Count;
                 int m = dataTable.Columns.Count;
                 // List<Skeleton_Segment> Skeleton_SegmentList = ske.Skeleton_SegmentList;
                 if (n == 0)
                     return false;

                 for (int i = 0; i < n; i++)
                 {
                     IRow row = pTable.CreateRow(); ;
                     for (int j = 0; j < m; j++)
                     {
                         row.set_Value(j, dataTable.Rows[i][j]);
                     }
                 }

                 //关闭编辑
                 workspaceEdit.StopEditOperation();
                 workspaceEdit.StopEditing(true);
             }
             catch (Exception ex)
             {
                 MessageBox.Show("异常信息" + ex.Message);
                 return false;

             }
             #endregion

             return true;
         }


         /// <summary>
         /// 将面写入Shp文件+
         /// </summary>
         /// <param name="filePath">文件名</param>
         /// <param name="Skeleton_SegmentList">线列表</param>
         public static void Create_WritePolygonObject2Shp(string filePath, List<PolygonObject> polygonList,string fileName, esriSRProjCS4Type prj)
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
             pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolygon;
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

                 int n = polygonList.Count;
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
                     if (polygonList[i] == null)
                         continue;
                     int m = polygonList[i].PointList.Count;

                     for (int k = 0; k < m; k++)
                     {
                         curPoint = polygonList[i].PointList[k];
                         curResultPoint = new PointClass();
                         curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                         pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                     }
                     curPoint = polygonList[i].PointList[0];
                     curResultPoint = new PointClass();
                     curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                     pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                     feature.Shape = shp;
                     feature.set_Value(2, polygonList[i].ID);//编号 


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
         /// 将面写入Shp文件+
         /// </summary>
         /// <param name="filePath">文件名</param>
         /// <param name="Skeleton_SegmentList">线列表</param>
         public static void Create_WriteESRIPolygon2Shp(string filePath, List<IPolygon> polygonList, string fileName, esriSRProjCS4Type prj)
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
             pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolygon;
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

                 int n = polygonList.Count;
                 // List<Skeleton_Segment> Skeleton_SegmentList = ske.Skeleton_SegmentList;
                 if (n == 0)
                     return;

                 for (int i = 0; i < n; i++)
                 {


                     IFeature feature = pFeatClass.CreateFeature();
                     IGeometry shp = polygonList[i];
                    
                     feature.Shape = shp;
                     feature.set_Value(2, i);//编号 


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
         public static void Create_WritePolylineObject2Shp(string filePath, List<PolylineObject> polylineList, string fileName, esriSRProjCS4Type prj)
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

                 int n = polylineList.Count;
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
                     if (polylineList[i] == null)
                         continue;
                     int m = polylineList[i].PointList.Count;

                     for (int k = 0; k < m; k++)
                     {
                         curPoint = polylineList[i].PointList[k];
                         curResultPoint = new PointClass();
                         curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                         pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                     }
                     feature.Shape = shp;
                     feature.set_Value(2, polylineList[i].ID);//编号 


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
         public static void Create_WritePointObject2Shp(string filePath, List<TriNode> pointList,string fileName, esriSRProjCS4Type prj)
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
             pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPoint;
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
             #endregion

             #region 创建要素类
             IFeatureClass pFeatClass;
             pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
             #endregion
             #endregion

             #region 添加要素
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

                 int n = pointList.Count;
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
                     if (pointList[i] == null)
                         continue;

                     curPoint = pointList[i]; ;
                     ((PointClass)shp).PutCoords(curPoint.X, curPoint.Y);

                     feature.Shape = shp;
                     feature.set_Value(2, curPoint.ID);
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
    }
}
