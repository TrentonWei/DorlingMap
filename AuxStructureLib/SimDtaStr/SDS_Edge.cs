using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System.Windows.Forms;

namespace AuxStructureLib
{

    /// <summary>
    /// 边
    /// </summary>
         [Serializable]
    public class SDS_Edge
    {
        public int ID = -1;
        public SDS_Node StartPoint = null, EndPoint = null;
        public SDS_Triangle LeftTriangle = null, RightTriangle = null;
        public ISDS_MapObject MapObject = null;

        /// <summary>
        /// 构造函数
        /// </summary>
        ///<param name="id">ID</param>
        /// <param name="startPoint">起点</param>
        /// <param name="endPoint">终点</param>
        /// <param name="leftTriangle">左三角形</param>
        /// <param name="rightTriangle">右三角形</param>
        /// <param name="mapObject">所属对象</param>
        public SDS_Edge(int id, SDS_Node startPoint, SDS_Node endPoint,
            SDS_Triangle leftTriangle, SDS_Triangle rightTriangle,
            ISDS_MapObject mapObject)
        {
            ID = id;
            this.StartPoint = startPoint;
            this.EndPoint = endPoint;
            this.LeftTriangle = leftTriangle;
            this.RightTriangle = rightTriangle;
            this.MapObject = mapObject;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        ///<param name="id">ID</param>
        /// <param name="startPoint">起点</param>
        /// <param name="endPoint">终点</param>
        public SDS_Edge(int id, SDS_Node startPoint, SDS_Node endPoint)
        {
            this.ID = id;
            this.StartPoint = startPoint;
            this.EndPoint = endPoint;
        }

        public SDS_Edge()
        {

        }

        /// <summary>
        /// 将三角形边写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="Skeleton_SegmentList">骨架线段列表</param>
        public static void Create_WriteEdge2Shp(string filePath, string fileName, List<SDS_Edge> TriEdgeList, esriSRProjCS4Type prj)
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
            //ID
            IField pField1;
            IFieldEdit pFieldEdit1;
            pField1 = new FieldClass();
            pFieldEdit1 = pField1 as IFieldEdit;
            pFieldEdit1.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit1.Name_2 = "ID";
            pFieldEdit1.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField1);

            //起点
            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;
            pFieldEdit2.Name_2 = "PID1";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField2);

            //终点
            IField pField3;
            IFieldEdit pFieldEdit3;
            pField3 = new FieldClass();
            pFieldEdit3 = pField3 as IFieldEdit;
            pFieldEdit3.Length_2 = 30;
            pFieldEdit3.Name_2 = "PID2";
            pFieldEdit3.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField3);

            //左三角形ID
            IField pField4;
            IFieldEdit pFieldEdit4;
            pField4 = new FieldClass();
            pFieldEdit4 = pField4 as IFieldEdit;
            pFieldEdit4.Length_2 = 30;
            pFieldEdit4.Name_2 = "LTID";
            pFieldEdit4.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField4);

            //右三角形ID
            IField pField5;
            IFieldEdit pFieldEdit5;
            pField5 = new FieldClass();
            pFieldEdit5 = pField5 as IFieldEdit;
            pFieldEdit5.Length_2 = 30;
            pFieldEdit5.Name_2 = "RTID";
            pFieldEdit5.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField5);

            //属性ID
            IField pField6;
            IFieldEdit pFieldEdit6;
            pField6 = new FieldClass();
            pFieldEdit6 = pField6 as IFieldEdit;
            pFieldEdit6.Length_2 = 30;
            pFieldEdit6.Name_2 = "TagID";
            pFieldEdit6.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField6);


            //属性ObjType
            IField pField7;
            IFieldEdit pFieldEdit7;
            pField7 = new FieldClass();
            pFieldEdit7 = pField7 as IFieldEdit;
            pFieldEdit7.Length_2 = 30;
            pFieldEdit7.Name_2 = "ObjType";
            pFieldEdit7.Type_2 = esriFieldType.esriFieldTypeString;
            pFieldsEdit.AddField(pField7);

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

                int n = TriEdgeList.Count;
                if (n == 0)
                    return;

                for (int i = 0; i < n; i++)
                {


                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PolylineClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = null;
                    SDS_Node curPoint = null;
                    if (TriEdgeList[i] == null)
                        continue;

                    curPoint = TriEdgeList[i].StartPoint; ;
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                    curPoint = TriEdgeList[i].EndPoint;
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    try
                    {
                        feature.Shape = shp;
                        feature.set_Value(2, TriEdgeList[i].ID);
                        feature.set_Value(3, TriEdgeList[i].StartPoint.ID);
                        feature.set_Value(4, TriEdgeList[i].EndPoint.ID);
                        if (TriEdgeList[i].LeftTriangle == null)
                        {
                            feature.set_Value(5, -1);
                        }
                        else
                        {
                            feature.set_Value(5, TriEdgeList[i].LeftTriangle.ID);
                        }
                        if (TriEdgeList[i].RightTriangle == null)
                        {
                            feature.set_Value(6, -1);
                        }
                        else
                        {
                            feature.set_Value(6, TriEdgeList[i].RightTriangle.ID);
                        }
                        if (TriEdgeList[i].MapObject != null)
                        {
                            string curType = TriEdgeList[i].MapObject.ToString();
                            if (curType == @"AuxStructureLib.SDS_PolylineObj")
                            {
                                feature.set_Value(7, (TriEdgeList[i].MapObject as SDS_PolylineObj).ID);
                                feature.set_Value(8, @"线");
                            }
                            else if (curType == @"AuxStructureLib.SDS_PolygonO")
                            {
                                feature.set_Value(7, (TriEdgeList[i].MapObject as SDS_PolygonO).ID);
                                feature.set_Value(8, @"面");
                            }
                        }
                        else
                        {
                            feature.set_Value(7, -1);
                            feature.set_Value(8, @"-");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("异常信息" + ex.Message);
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
                MessageBox.Show("异常信息" + ex.Message);
            }
            #endregion
        }
    }
}
