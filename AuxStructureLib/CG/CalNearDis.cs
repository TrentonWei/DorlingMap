
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using System.Windows.Forms;

namespace AuxStructureLib.CG
{
    public  class CalNearDis
    {
        SDS_PolygonO O1 = null;
        SDS_PolygonO O2 = null;
        public CalNearDis(SDS_PolygonO o1, SDS_PolygonO o2)
        {
            O1 = o1;
            O2 = o2;
        }

        public NearestEdge CalNearestEdge(int i)
        {
            ConvexNull co1 = new ConvexNull(O1.PointList);
            co1.CreateConvexNull();
            co1.ConvexVertexSet.RemoveAt(co1.ConvexVertexSet.Count - 1);
            ConvexNull co2= new ConvexNull(O2.PointList);
            co2.CreateConvexNull();
            co2.ConvexVertexSet.RemoveAt(co2.ConvexVertexSet.Count - 1);

            NearestDisBtwConvexNull ndisc=new NearestDisBtwConvexNull(co1.ConvexVertexSet,co2.ConvexVertexSet);
            double d = ndisc.CalNearestDistance() ;
            NearestEdge edge = null;
            edge =ndisc.CalNearestEdge();
            //test
            PolygonObject po1 = new PolygonObject(-1, ndisc.ConvexNull1);
            PolygonObject po2 = new PolygonObject(-1, ndisc.ConvexNull1);
            List<PolygonObject> pList = new List<PolygonObject>();
            pList.Add(po1);
            pList.Add(po2);
            Create_WritePolygonObject2Shp(@"E:\DelaunayShape\TestRotateCliper", pList, @"pp" + i.ToString(), esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);


            return edge;
           // List<NearestEdge> elist = new List<NearestEdge>();
          //  elist.Add(edge);
          //  NearestEdge.Create_WriteEdge2Shp(@"E:\DelaunayShape", @"fileNearest",elist, esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
           

        }

        /// <summary>
        /// 将面写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="Skeleton_SegmentList">线列表</param>
        public void Create_WritePolygonObject2Shp(string filePath,List<PolygonObject> PolygonList, string fileName, esriSRProjCS4Type prj)
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

                int n = PolygonList.Count;
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
                    if (PolygonList[i] == null)
                        continue;
                    int m = PolygonList[i].PointList.Count;

                    for (int k = 0; k < m; k++)
                    {
                        curPoint = PolygonList[i].PointList[k];
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    }
                    curPoint = PolygonList[i].PointList[0];
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    feature.Shape = shp;
                    feature.set_Value(2, PolygonList[i].ID);//编号 


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
