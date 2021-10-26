using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;

namespace AuxStructureLib
{
    /// <summary>
    /// ArcGIS不规则三角网
    /// </summary>
    public class ESRITin
    {
        /// <summary>
        /// 由点图层创建Tin
        /// </summary>
        /// <param name="featurelyr">点图层</param>
        /// <param name="tagFieldIndex">TagValue</param>
        /// <param name="heightFieldIndex">高程字段</param>
        /// <returns>ITin对象</returns>
        public static ITinEdit CreateTinfrmPointFeatureClass(IFeatureLayer featurelyr,
            int tagFieldIndex,
            int heightFieldIndex,
            string savePath)
        {
            IFeatureClass freCls = null;
            IEnvelope evp = featurelyr.AreaOfInterest.Envelope;
            ITinEdit TinEdit = new TinClass();
            TinEdit.InitNew(evp);
            // pTinEdit.SaveAs "d:\TinFolder", False    '创建放的路径
            object overwrite = true;
            TinEdit.SaveAs(@"d\TinFolder", overwrite);
            //开始编辑Tin
            TinEdit.StartEditing();
            freCls = featurelyr.FeatureClass;
            IField tagField = freCls.Fields.Field[tagFieldIndex];
            IField hightFeild = freCls.Fields.Field[heightFieldIndex];
            //创建基于高程字段的Tin数据表面
            object missing = Type.Missing;
            TinEdit.AddFromFeatureClass(
                freCls, 
                null, 
                hightFeild, 
                tagField, 
                esriTinSurfaceType.esriTinMassPoint,
                ref missing);
            TinEdit.StopEditing(true);
            return TinEdit;
            
        }
        /// <summary>
        /// 创建Voronoi Polygons
        /// </summary>
        /// <param name="inPointLyr">点图层</param>
        /// <param name="tagFieldIndex">TagValue字段序号</param>
        /// <param name="heightFieldIndex">高程字段序号</param>
        /// <param name="pBorder">边界多边形对象</param>
        /// <param name="prj">投影系统</param>
        /// <param name="TinFilePath">Tin的存储目录</param>
        /// <param name="VoronoiPolygonFilePath">Voronoi多边形的存储目录</param>
        /// <param name="fileName">Voronoi多边形的存储文件</param>
        public static void CreateVoronoiPolygons(IFeatureLayer inPointLyr, 
            int tagFieldIndex, 
            int heightFieldIndex,
            IPolygon pBorder,
            esriSRProjCS4Type prj,
            string TinFilePath,
            string VoronoiPolygonFilePath,
            string fileName)
        {
            ITinNodeCollection TinNodes =
                CreateTinfrmPointFeatureClass(inPointLyr, tagFieldIndex, heightFieldIndex, TinFilePath)
                as ITinNodeCollection;
            IFeatureClass outPolygons=null;

            #region 建一个多边形的shape文件
            string Folderpathstr = VoronoiPolygonFilePath;
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
            //属性字段2
            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit2.Name_2 = "TagValue";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField2);
            #endregion

            #region 创建要素类
            outPolygons = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
            #endregion

            #endregion
            //创建多边形要素类
            TinNodes.ConvertToVoronoiRegions(outPolygons, null, pBorder, "ID", "TagValue");

            //#region 向线层添加线要素

            //object missing1 = Type.Missing;
            //object missing2 = Type.Missing;

            //IWorkspaceEdit pIWorkspaceEdit = null;
            //IDataset pIDataset = (IDataset)outPolygons;

            //if (pIDataset != null)
            //{
            //    pIWorkspaceEdit = (IWorkspaceEdit)pIDataset.Workspace;
            //}
            //try
            //{
            //    if (outPolygons == null)
            //        return;
            //    //获取顶点图层的数据集，并创建工作空间
            //    IDataset dataset = (IDataset)outPolygons;
            //    IWorkspace workspace = dataset.Workspace;
            //    IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
            //    //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
            //    IFeatureClassWrite fr = (IFeatureClassWrite)outPolygons;
            //    //注意：此时，所编辑数据不能被其他程序打开
            //    workspaceEdit.StartEditing(true);
            //    workspaceEdit.StartEditOperation();
            //    //关闭编辑
            //    workspaceEdit.StopEditOperation();
            //    workspaceEdit.StopEditing(true);
            //}
            //catch (Exception ex)
            //{

            //}
            //#endregion
        }

        /// <summary>
        /// 从点集创建TIN
        /// </summary>
        /// <param name="points">点集合</param>
        /// <param name="savePath">保存路径</param>
        /// <returns>ITinEdit对象</returns>
        public static ITinEdit CreateTinfrmPoints(
            List<TriNode> points, 
            string savePath)
        {
            // Instantiate a new empty TIN.
            ITinEdit TinEdit = new TinClass();

            // Initialize the TIN with an envelope. The envelope's extent should be set large enough to // encompass all the data that will be added to the TIN. The envelope's spatial reference, if// if has one, will be used as the TIN's spatial reference. If it is not set, as in this case,// the TIN's spatial reference will be unknown.
            IEnvelope Env = new EnvelopeClass();
            //获取点集的范围
            double minx = double.PositiveInfinity;
            double miny = double.PositiveInfinity;
            double maxx = double.NegativeInfinity;
            double maxy = double.NegativeInfinity;
            foreach (TriNode curPoint in points)
            {
                if (curPoint.X < minx)
                    minx = curPoint.X;
                if (curPoint.Y < miny)
                    miny = curPoint.Y;
                if(curPoint.X >maxx)
                    maxx = curPoint.X;
                if (curPoint.Y > maxy)
                    maxy = curPoint.Y;

            }
            Env.PutCoords(minx, miny, maxx, maxy);

            TinEdit.InitNew(Env);
            // Add points to the TIN. These will become triangle nodes.
            foreach (TriNode curPoint in points)
            {
                IPoint Point = new PointClass();
                Point.X = curPoint.X;
                Point.Y = curPoint.Y;
                Point.Z = 0;
                Point.ID = curPoint.TagValue;
                TinEdit.AddPointZ(Point, 0);
            
            }
            object overwrite = true;
            TinEdit.SaveAs(savePath, ref overwrite);
            return TinEdit;
        }

        /// <summary>
        /// 创建Voronoi Polygons
        /// </summary>
        /// <param name="inPointLyr">点图层</param>
        /// <param name="tagFieldIndex">TagValue字段序号</param>
        /// <param name="heightFieldIndex">高程字段序号</param>
        /// <param name="pBorder">边界多边形对象</param>
        /// <param name="prj">投影系统</param>
        /// <param name="TinFilePath">Tin的存储目录</param>
        /// <param name="VoronoiPolygonFilePath">Voronoi多边形的存储目录</param>
        /// <param name="fileName">Voronoi多边形的存储文件</param>
        public static void CreateVoronoiPolygons(
            List<TriNode> points, 
            IPolygon pBorder,
            esriSRProjCS4Type prj,
            string TinFilePath,
            string VoronoiPolygonFilePath,
            string fileName)
        {
            ITinNodeCollection TinNodes =
                CreateTinfrmPoints(points,TinFilePath)
                as ITinNodeCollection;
            IFeatureClass outPolygons = null;

            #region 建一个多边形的shape文件
            string Folderpathstr = VoronoiPolygonFilePath;
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
            //属性字段2
            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit2.Name_2 = "TagValue";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField2);
            #endregion

            #region 创建要素类
            outPolygons = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
            #endregion

            #endregion
            //创建多边形要素类
            TinNodes.ConvertToVoronoiRegions(outPolygons, null, pBorder, "ID", "TagValue");

            //#region 向线层添加线要素

            //object missing1 = Type.Missing;
            //object missing2 = Type.Missing;

            //IWorkspaceEdit pIWorkspaceEdit = null;
            //IDataset pIDataset = (IDataset)outPolygons;

            //if (pIDataset != null)
            //{
            //    pIWorkspaceEdit = (IWorkspaceEdit)pIDataset.Workspace;
            //}
            //try
            //{
            //    if (outPolygons == null)
            //        return;
            //    //获取顶点图层的数据集，并创建工作空间
            //    IDataset dataset = (IDataset)outPolygons;
            //    IWorkspace workspace = dataset.Workspace;
            //    IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
            //    //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
            //    IFeatureClassWrite fr = (IFeatureClassWrite)outPolygons;
            //    //注意：此时，所编辑数据不能被其他程序打开
            //    workspaceEdit.StartEditing(true);
            //    workspaceEdit.StartEditOperation();
            //    //关闭编辑
            //    workspaceEdit.StopEditOperation();
            //    workspaceEdit.StopEditing(true);
            //}
            //catch (Exception ex)
            //{

            //}
            //#endregion
        }

        /// <summary>
        /// 图层列表
        /// </summary>
        /// <param name="lyrList">图层</param>
        public static void CreateCDTfrmLyrsbyEsri(List<IFeatureLayer> lyrList)
        {


            SMap map = new SMap(lyrList);
            map.ReadDateFrmEsriLyrs();

            // Instantiate a new empty TIN.
            ITinEdit TinEdit = new TinClass();

            // Initialize the TIN with an envelope. The envelope's extent should be set large enough to // encompass all the data that will be added to the TIN. The envelope's spatial reference, if// if has one, will be used as the TIN's spatial reference. If it is not set, as in this case,// the TIN's spatial reference will be unknown.
            IEnvelope Env = new EnvelopeClass();
            //获取点集的范围
            double minx = double.PositiveInfinity;
            double miny = double.PositiveInfinity;
            double maxx = double.NegativeInfinity;
            double maxy = double.NegativeInfinity;
            foreach (TriNode curPoint in map.TriNodeList)
            {
                if (curPoint.X < minx)
                    minx = curPoint.X;
                if (curPoint.Y < miny)
                    miny = curPoint.Y;
                if (curPoint.X > maxx)
                    maxx = curPoint.X;
                if (curPoint.Y > maxy)
                    maxy = curPoint.Y;

            }
            Env.PutCoords(minx, miny, maxx, maxy);

            TinEdit.InitNew(Env);

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
            #endregion

            //#region 线段
            //object o = Type.Missing;
            //if (map.PolylineList != null && map.PolylineList.Count > 0)
            //{
            //    foreach (PolylineObject line in map.PolylineList)
            //    {
            //        for (int i = 0; i < line.PointList.Count - 1; i++)
            //        {
            //            IPolyline polyline = new PolylineClass();
            //            IPoint fromp = new PointClass();
            //            fromp.ID = line.PointList[i].ID;
            //            fromp.X = line.PointList[i].X;
            //            fromp.Y = line.PointList[i].Y;
            //            fromp.Z = 8;
            //            fromp.M = 8;
            //            polyline.FromPoint = fromp;
            //            IPoint top = new PointClass();
            //            top.ID = line.PointList[i + 1].ID;
            //            top.X = line.PointList[i + 1].X;
            //            top.Y = line.PointList[i + 1].Y;
            //            top.Z = 8;
            //            top.M = 8;
            //            polyline.ToPoint = top;
            //            TinEdit.AddShape(polyline, esriTinSurfaceType.esriTinSoftLine, line.ID, ref o);
            //        }
            //    }
            //}
            //if (map.PolygonList != null && map.PolygonList.Count > 0)
            //{
            //    foreach (PolygonObject polygon in map.PolygonList)
            //    {
            //        for (int i = 0; i < polygon.PointList.Count - 1; i++)
            //        {
            //            IPolyline polyline = new PolylineClass();
            //            IPoint fromp = new PointClass();
            //            fromp.ID = polygon.PointList[i].ID;
            //            fromp.X = polygon.PointList[i].X;
            //            fromp.Y = polygon.PointList[i].Y;
            //            fromp.Z = 10;
            //            fromp.M = 10;
            //            polyline.FromPoint = fromp;
            //            IPoint top = new PointClass();
            //            top.ID = polygon.PointList[i + 1].ID;
            //            top.X = polygon.PointList[i + 1].X;
            //            top.Y = polygon.PointList[i + 1].Y;
            //            top.Z = 10;
            //            top.M = 10;
            //            polyline.ToPoint = top;
            //            TinEdit.AddShape(polyline, esriTinSurfaceType.esriTinSoftLine, polygon.ID, ref o);
            //        }

            //        IPolyline polyline1 = new PolylineClass();
            //        IPoint fromp1 = new PointClass();
            //        fromp1.ID = polygon.PointList[polygon.PointList.Count - 1].ID;
            //        fromp1.X = polygon.PointList[polygon.PointList.Count - 1].X;
            //        fromp1.Y = polygon.PointList[polygon.PointList.Count - 1].Y;
            //        fromp1.Z = 10;
            //        fromp1.M = 10;
            //        polyline1.FromPoint = fromp1;
            //        IPoint top1 = new PointClass();
            //        top1.ID = polygon.PointList[0].ID;
            //        top1.X = polygon.PointList[0].X;
            //        top1.Y = polygon.PointList[0].Y;
            //        top1.Z = 10;
            //        top1.M = 10;
            //        polyline1.ToPoint = top1;
            //        TinEdit.AddShape(polyline1, esriTinSurfaceType.esriTinSoftLine, polygon.ID, ref o);
            //    }
            //}
            //#endregion







            //#region AddShape
            //object o = Type.Missing;
            //int vextexID = 0;
            //int pID = 1;
            //int plID = 1;
            //int ppID = 1;

            //foreach (IFeatureLayer curLyr in lyrList)
            //{
            //    IFeatureCursor cursor = null;
            //    IFeature curFeature = null;
            //    IGeometry shp = null;

            //    switch (curLyr.FeatureClass.ShapeType)
            //    {

            //        case esriGeometryType.esriGeometryPoint:
            //            {
            //                #region 点要素
            //                cursor = curLyr.Search(null, false);
            //                while ((curFeature = cursor.NextFeature()) != null)
            //                {
            //                    shp = curFeature.Shape;
            //                    pID = curFeature.OID;
            //                    IPoint point = null;
            //                    //几何图形
            //                    if (shp.GeometryType == esriGeometryType.esriGeometryPoint)
            //                    {
            //                        point = shp as IPoint;
            //                        TinEdit.AddShape(point, esriTinSurfaceType.esriTinMassPoint, 1000000 + pID, ref o);
            //                    }

            //                }
            //                #endregion
            //                break;


            //            }
            //        case esriGeometryType.esriGeometryPolyline:
            //            {
            //                #region 线要素
            //                cursor = curLyr.Search(null, false);
            //                while ((curFeature = cursor.NextFeature()) != null)
            //                {
            //                    shp = curFeature.Shape;
            //                    plID = curFeature.OID;
            //                    IPolyline polyline = null;
            //                    //几何图形
            //                    if (shp.GeometryType == esriGeometryType.esriGeometryLine)
            //                    {
            //                        polyline = shp as IPolyline;
            //                        TinEdit.AddShape(polyline, esriTinSurfaceType.esriTinSoftLine, 2000000 + plID, ref o);
            //                    }

            //                }
            //                #endregion
            //                break;
            //            }

            //        case esriGeometryType.esriGeometryPolygon:
            //            {

            //                #region 面要素
            //                cursor = curLyr.Search(null, false);
            //                while ((curFeature = cursor.NextFeature()) != null)
            //                {
            //                    shp = curFeature.Shape;
            //                    ppID = curFeature.OID;
            //                    IPolygon polygon = null;
            //                    //几何图形
            //                    if (shp.GeometryType == esriGeometryType.esriGeometryPolygon)
            //                    {
            //                        polygon = shp as IPolygon;
            //                        TinEdit.AddShape(polygon, esriTinSurfaceType.esriTinSoftLine, 3000000 + ppID, ref o);
            //                    }

            //                }
            //                #endregion
            //                break;
            //            }
            //    }
            //}
            //#endregion

            #region AddfromFeatureClass
            object o = Type.Missing;

            foreach (IFeatureLayer curLyr in lyrList)
            {
                IFeatureCursor cursor = null;
                switch (curLyr.FeatureClass.ShapeType)
                {

                    case esriGeometryType.esriGeometryPoint:
                        {
                            #region 点要素

                            IFields pfields = curLyr.FeatureClass.Fields;
                            IField pHeightField = null;
                            for (int i = 0; i < pfields.FieldCount; i++)
                            {
                                IField curField = pfields.get_Field(i);
                                if (curField.Name == "OBJECTID")
                                {
                                    pHeightField = curField;
                                    break;
                                }
                            }

                            TinEdit.AddFromFeatureClass(curLyr.FeatureClass, null, pHeightField, pHeightField, esriTinSurfaceType.esriTinMassPoint, ref o);

                            #endregion
                            break;
                        }
                    case esriGeometryType.esriGeometryPolyline:
                        {

                            #region 线要素
                            cursor = curLyr.Search(null, false);

                            IFields pfields = curLyr.FeatureClass.Fields;
                            IField pHeightField = null;
                            for (int i = 0; i < pfields.FieldCount; i++)
                            {
                                IField curField = pfields.get_Field(i);
                                if (curField.Name == "OBJECTID")
                                {
                                    pHeightField = curField;
                                    break;
                                }
                            }
                            TinEdit.AddFromFeatureClass(curLyr.FeatureClass, null, pHeightField, pHeightField, esriTinSurfaceType.esriTinSoftLine, ref o);

                            #endregion
                            break;
                        }

                    case esriGeometryType.esriGeometryPolygon:
                        {

                            #region 面要素
                            IFields pfields = curLyr.FeatureClass.Fields;
                            IField pHeightField = null;
                            for (int i = 0; i < pfields.FieldCount; i++)
                            {
                                IField curField = pfields.get_Field(i);
                                if (curField.Name == "OBJECTID")
                                {
                                    pHeightField = curField;
                                    break;
                                }
                            }
                            TinEdit.AddFromFeatureClass(curLyr.FeatureClass, null, pHeightField, pHeightField, esriTinSurfaceType.esriTinSoftLine, ref o);

                            #endregion
                            break;
                        }
                }
            }
            #endregion

            o = true;
          //  TinEdit.StopEditing(true);
            object overwrite = true;
            TinEdit.SaveAs(@"E:\TinTest", ref overwrite); //写入文件

            List<Triangle> TriList = new List<Triangle>();
            List<TriEdge> TriEdgeList = new List<TriEdge>();

            ITinAdvanced itina = (TinEdit as ITinAdvanced);

            int NodeCount = itina.NodeCount;
            for (int i = 1; i <= NodeCount; i++)
            {
                ITinNode curNode = itina.GetNode(i);
                int tag = curNode.TagValue;
            }

            int EdgeCount = itina.EdgeCount;
            for (int i = 1; i <= EdgeCount; i++)
            {
                ITinEdge curEdge = itina.GetEdge(i);
                int tag = curEdge.TagValue;
            }

            int TriCount = itina.TriangleCount;
            for (int i = 1; i <= TriCount; i++)
            {
                ITinTriangle curTriangle = itina.GetTriangle(i);

                ITinNode p1 = curTriangle.get_Node(1);
                ITinNode p2 = curTriangle.get_Node(2);
                ITinNode p3 = curTriangle.get_Node(3);

                TriNode point1 = TriNode.GetNode(map.TriNodeList, p1);
                TriNode point2 = TriNode.GetNode(map.TriNodeList, p2);
                TriNode point3 = TriNode.GetNode(map.TriNodeList, p3);

                if (point1 != null && point2 != null && point3 != null)
                {
                    Triangle tri = new Triangle();
                    tri.point1 = point1;
                    tri.point2 = point2;
                    tri.point3 = point3;

                    //第一条边
                    TriEdge e1 = new TriEdge(point1, point2);
                    if (point1.TagValue == point2.TagValue && point1.FeatureType == point2.FeatureType&&point1.TagValue!=-1)
                    {
                        e1.tagID = point1.TagValue;
                        e1.FeatureType = point1.FeatureType;
                    }
                    else
                    {
                        FeatureType ftype = FeatureType.Unknown;
                        int tagvalue=map.GetConsEdge(point1, point2,out ftype);
                        if (tagvalue != -1)
                        {
                            e1.tagID = tagvalue;
                            e1.FeatureType = ftype;
                        }
                    }
                    TriEdgeList.Add(e1);

                    //第二条边
                    TriEdge e2 = new TriEdge(point2, point3);
                    if (point2.TagValue == point3.TagValue && point2.FeatureType == point3.FeatureType && point2.TagValue != -1)
                    {
                        e2.tagID = point2.TagValue;
                        e2.FeatureType = point2.FeatureType;
                    }
                    else
                    {
                        FeatureType ftype = FeatureType.Unknown;
                        int tagvalue = map.GetConsEdge(point2, point3, out ftype);
                        if (tagvalue != -1)
                        {
                            e2.tagID = tagvalue;
                            e2.FeatureType = ftype;
                        }
                    }
                    TriEdgeList.Add(e2);

                    //第三条边
                    TriEdge e3 = new TriEdge(point3, point1);
                    if (point3.TagValue == point1.TagValue && point3.FeatureType == point1.FeatureType && point3.TagValue != -1)
                    {
                        e3.tagID = point3.TagValue;
                        e3.FeatureType = point3.FeatureType;
                    }
                    else
                    {
                        FeatureType ftype = FeatureType.Unknown;
                        int tagvalue = map.GetConsEdge(point3, point1, out ftype);
                        if (tagvalue != -1)
                        {
                            e3.tagID = tagvalue;
                            e3.FeatureType = ftype;
                        }
                    }
                    TriEdgeList.Add(e3);

                    tri.edge1 = e1;
                    tri.edge2 = e2;
                    tri.edge3 = e3;
                    e1.leftTriangle = tri;
                    e2.leftTriangle = tri;
                    e3.leftTriangle = tri;
                    TriList.Add(tri);
                }
                //设置对偶边和右边三角形
                foreach (TriEdge edge in TriEdgeList)
                {
                    if (edge.doulEdge != null)
                        continue;
                    TriEdge doulEdge = TriEdge.FindOppsiteEdge(TriEdgeList, edge);
                    if (doulEdge == null)
                    {
                        edge.doulEdge = null;
                        edge.rightTriangle = null;
                    }
                    else
                    {
                        edge.doulEdge = doulEdge;
                        edge.rightTriangle = doulEdge.leftTriangle;
                    }
                }
            }
        }
    }


}
