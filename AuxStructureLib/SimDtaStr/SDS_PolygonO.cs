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
    /// 面对象
    /// </summary>
         [Serializable]
    public class SDS_PolygonO : SDS_Polygon
    {
             //可移位点的列表
             public List<TrialPosition> TrialPosList = null;

        /// <summary>
        /// 面对象
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pointList"></param>
        public SDS_PolygonO(int id, List<TriNode> pointList)
        {
            this.ID = id;
            this.PointList = pointList;
            Points = new List<SDS_Node>();
            Edges = new List<SDS_Edge>();
            Wises = new List<bool>();
            Triangles = new List<SDS_Triangle>();
        }

        /// <summary>
        /// 获得周围的三角形
        /// </summary>
        /// <returns></returns>
        public List<SDS_Triangle> GetSurroundingTris()
        {
            List<SDS_Triangle> triList = new List<SDS_Triangle>();
            List<SDS_Triangle> conTriList = this.Triangles;
            foreach (SDS_Node curNode in this.Points)
            {
                List<SDS_Triangle> curTriList = curNode.Triangles;
                if (curTriList != null && curTriList.Count > 0)
                {
                    foreach (SDS_Triangle curTri in curTriList)
                    {
                        if (!SDS.IsContainTri(conTriList, curTri) && !SDS.IsContainTri(triList, curTri))
                        {
                            triList.Add(curTri);
                        }
                    }
                }
            }
            return triList;
        }






        /// <summary>
        /// 将面写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="Skeleton_SegmentList">线列表</param>
        public static void Create_WritePolygonObject2Shp(string filePath, string fileName, List<SDS_PolygonO> PolygonObjList, esriSRProjCS4Type prj)
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
            //属性字段2
            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit2.Name_2 = "EdgesCount";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField2);

            //属性字段3
            IField pField3;
            IFieldEdit pFieldEdit3;
            pField3 = new FieldClass();
            pFieldEdit3 = pField3 as IFieldEdit;
            pFieldEdit3.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit3.Name_2 = "Triangles";
            pFieldEdit3.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField3);
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

                int n = PolygonObjList.Count;
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
                    SDS_Node curPoint = null;
                    if (PolygonObjList[i] == null)
                        continue;
                    int m = PolygonObjList[i].PointList.Count;

                    for (int k = 0; k < m; k++)
                    {
                        curPoint = PolygonObjList[i].Points[k];
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    }
                    curPoint = PolygonObjList[i].Points[0];
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    feature.Shape = shp;
                    feature.set_Value(2, PolygonObjList[i].ID);//编号 
                    if (PolygonObjList[i].Edges != null)
                        feature.set_Value(3, PolygonObjList[i].Edges.Count);//
                    else
                        feature.set_Value(3, 0);//

                    if (PolygonObjList[i].Triangles != null)
                        feature.set_Value(4, PolygonObjList[i].Triangles.Count);//
                    else
                        feature.set_Value(4, 0);//
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
