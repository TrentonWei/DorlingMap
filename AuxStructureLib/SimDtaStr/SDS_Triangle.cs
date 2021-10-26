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
    /// 三角形
    /// </summary>
         [Serializable]
    public class SDS_Triangle
    {
        public int ID = -1;
        public SDS_Node[] Points;
        public SDS_Edge[] Edges;
        public bool[] Wises;
        public SDS_Polygon Polygon = null;
        //三角形类型-1,0,1,2,3
        public int TriType = -1;
        // public ISDS_MapObject MapObject = null;

        //private TriPoint circumCenter;
        /// <summary>
        /// 构造函数
        /// </summary>
        public SDS_Triangle()
        {
            Points = new SDS_Node[3];
            Edges = new SDS_Edge[3];
            Wises = new bool[3];
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        public SDS_Triangle(SDS_Node p1, SDS_Node p2, SDS_Node p3,
            SDS_Edge edge1, SDS_Edge edge2, SDS_Edge edge3,
            bool wise1, bool wise2, bool wise3,
            int type, SDS_Polygon poly)
        {
            Points = new SDS_Node[3];
            Edges = new SDS_Edge[3];
            Wises = new bool[3];
            Points[0] = p1;
            Points[1] = p2;
            Points[2] = p3;

            Edges[0] = edge1;
            Edges[1] = edge2;
            Edges[2] = edge3;

            Wises[0] = wise1;
            Wises[1] = wise2;
            Wises[2] = wise3;

            TriType = type;
            Polygon = poly;
        }

        /// <summary>
        /// 判断是否逆时针
        /// </summary>
        /// <returns>BOOL </returns>
        public bool IsAnticlockwise()
        {
            double s = (Points[1].X - Points[0].X) * (Points[2].Y - Points[1].Y) - (Points[2].X - Points[1].X) * (Points[1].Y - Points[0].Y);
            if (s > 0)
                return true;
            return false;
        }
        /// <summary>
        /// 将三角形写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="Skeleton_SegmentList">骨架线段列表</param>
        public static void Create_WriteTriange2Shp(string filePath, string fileName, List<SDS_Triangle> TriangleList, esriSRProjCS4Type prj)
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
            pFieldEdit6.Name_2 = "W1";
            pFieldEdit6.Type_2 = esriFieldType.esriFieldTypeString;
            pFieldsEdit.AddField(pField6);

            IField pField7;
            IFieldEdit pFieldEdit7;
            pField7 = new FieldClass();
            pFieldEdit7 = pField7 as IFieldEdit;
            pFieldEdit7.Length_2 = 30;
            pFieldEdit7.Name_2 = "E2";
            pFieldEdit7.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField7);

            IField pField8;
            IFieldEdit pFieldEdit8;
            pField8 = new FieldClass();
            pFieldEdit8 = pField8 as IFieldEdit;
            pFieldEdit8.Length_2 = 30;
            pFieldEdit8.Name_2 = "W2";
            pFieldEdit8.Type_2 = esriFieldType.esriFieldTypeString;
            pFieldsEdit.AddField(pField8);



            IField pField9;
            IFieldEdit pFieldEdit9;
            pField9 = new FieldClass();
            pFieldEdit9 = pField9 as IFieldEdit;
            pFieldEdit9.Length_2 = 30;
            pFieldEdit9.Name_2 = "E3";
            pFieldEdit9.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField9);

            IField pField10;
            IFieldEdit pFieldEdit10;
            pField10 = new FieldClass();
            pFieldEdit10 = pField10 as IFieldEdit;
            pFieldEdit10.Length_2 = 30;
            pFieldEdit10.Name_2 = "W3";
            pFieldEdit10.Type_2 = esriFieldType.esriFieldTypeString;
            pFieldsEdit.AddField(pField10);

            IField pField11;
            IFieldEdit pFieldEdit11;
            pField11 = new FieldClass();
            pFieldEdit11 = pField11 as IFieldEdit;
            pFieldEdit11.Length_2 = 30;
            pFieldEdit11.Name_2 = "Type";
            pFieldEdit11.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField11);

            IField pField12;
            IFieldEdit pFieldEdit12;
            pField12 = new FieldClass();
            pFieldEdit12 = pField12 as IFieldEdit;
            pFieldEdit12.Length_2 = 30;
            pFieldEdit12.Name_2 = "MapObject";
            pFieldEdit12.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField12);
            #endregion

            #region 创建要素类
            IFeatureClass pFeatClass;
            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
            #endregion
            #endregion

            #region 向面层添加面要素

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
                    SDS_Node curPoint = null;
                    if (TriangleList[i] == null)
                        continue;

                    curPoint = TriangleList[i].Points[0];
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                    curPoint = TriangleList[i].Points[1];
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                    curPoint = TriangleList[i].Points[2];
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                    curPoint = TriangleList[i].Points[0];
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);


                    feature.Shape = shp;
                    feature.set_Value(2, TriangleList[i].ID);
                    feature.set_Value(3, TriangleList[i].Points[0].ID);
                    feature.set_Value(4, TriangleList[i].Points[1].ID);
                    feature.set_Value(5, TriangleList[i].Points[2].ID);
                    feature.set_Value(6, TriangleList[i].Edges[0].ID);
                    feature.set_Value(7, TriangleList[i].Wises[0].ToString());
                    feature.set_Value(8, TriangleList[i].Edges[1].ID);
                    feature.set_Value(9, TriangleList[i].Wises[1].ToString());
                    feature.set_Value(10, TriangleList[i].Edges[2].ID);
                    feature.set_Value(11, TriangleList[i].Wises[2].ToString());
                    feature.set_Value(12, TriangleList[i].TriType);
                    if (TriangleList[i].Polygon != null)
                    {
                        feature.set_Value(13, TriangleList[i].Polygon.ID);
                    }
                    else
                    {
                        feature.set_Value(13, -1);
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
