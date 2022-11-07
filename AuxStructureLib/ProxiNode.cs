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
    /// 要素类型
    /// </summary>
    public enum FeatureType
    {
        PointType,
        PolylineType,
        PolygonType,
        ConnNode,
        Unknown,
        Group
    }

    [Serializable]
    public class ProxiNode:Node
    {
        /// <summary>
        /// 要素类型
        /// </summary>
        public FeatureType FeatureType=FeatureType.Unknown; 
        /// <summary>
        /// 要素ID
        /// </summary>
        public int TagID=-1;
        public List<int> TagIds = new List<int>();//表示代表的节点或建筑物列表
        public bool MaxForce = false;
        public bool NearFinal = false;//表示该点是否靠近其最终的位置

        public List<ProxiEdge> EdgeList = null;
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ProxiNode()
        {
            EdgeList = new List<ProxiEdge>();
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        public ProxiNode(double x,double y)
        {
            X = x;
            Y = y;
            EdgeList = new List<ProxiEdge>();
        }
      
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="id">ID</param>
        /// <param name="tagValue">要素ID</param>
        public ProxiNode(double x, double y, int id, int tagValue)
        {
            X = x;
            Y = y;
            ID = id;
            this.TagID = tagValue;
            EdgeList = new List<ProxiEdge>();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="id">ID</param>
        public ProxiNode(double x, double y,int id)
        {
            X = x;
            Y = y;
            ID = id;
            EdgeList = new List<ProxiEdge>();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="id">ID</param>
        /// <param name="tagValue">要素ID</param>
        public ProxiNode(double x, double y, int id, int tagValue, FeatureType ftype)
        {
            X = x;
            Y = y;
            ID = id;
            this.TagID = tagValue;
            FeatureType = ftype;
            EdgeList = new List<ProxiEdge>();
        }
        /// <summary>
        /// 根据TagID和要素类型获取邻近图结点
        /// </summary>
        /// <param name="nodeList">邻近图结点列表</param>
        /// <param name="tagID">对应的要素ID</param>
        /// <param name="type">要素类型</param>
        /// <returns></returns>
        public static ProxiNode GetProxiNodebyTagIDandFType(List<ProxiNode> nodeList, int tagID, FeatureType type)
        {
            if (nodeList == null || nodeList.Count == 0)
                return null;
            foreach (ProxiNode curNode in nodeList)
            {
                if (curNode.TagID == tagID && curNode.FeatureType == type)
                {
                    return curNode;
                }
            }
            return null;
        }

        /// <summary>
        /// 将点写入Shp
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <param name="TriEdgeList"></param>
        /// <param name="prj"></param>
        public static void Create_WriteProxiNodes2Shp(string filePath, string fileName, List<ProxiNode> nodeList, ISpatialReference prj)
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

            //ISpatialReferenceFactory ispfac = new SpatialReferenceEnvironmentClass();
            //IProjectedCoordinateSystem iprcoorsys = ispfac.CreateProjectedCoordinateSystem((int)prj);
            //ISpatialReference pSpatialReference = iprcoorsys as ISpatialReference;

            IGeometryDef pGeomDef = new GeometryDefClass();
            IGeometryDefEdit pGeomDefEdit = pGeomDef as IGeometryDefEdit;
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPoint;
            pGeomDefEdit.SpatialReference_2 = prj;
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

            //X
            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;
            pFieldEdit2.Name_2 = "X";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField2);

            //Y
            IField pField3;
            IFieldEdit pFieldEdit3;
            pField3 = new FieldClass();
            pFieldEdit3 = pField3 as IFieldEdit;
            pFieldEdit3.Length_2 = 30;
            pFieldEdit3.Name_2 = "Y";
            pFieldEdit3.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField3);

            //tagValue
            IField pField4;
            IFieldEdit pFieldEdit4;
            pField4 = new FieldClass();
            pFieldEdit4 = pField4 as IFieldEdit;
            pFieldEdit4.Length_2 = 30;
            pFieldEdit4.Name_2 = "tagValue";
            pFieldEdit4.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField4);

            IField pField5;
            IFieldEdit pFieldEdit5;
            pField5 = new FieldClass();
            pFieldEdit5 = pField5 as IFieldEdit;
            pFieldEdit5.Length_2 = 30;
            pFieldEdit5.Name_2 = "FeatureType";
            pFieldEdit5.Type_2 = esriFieldType.esriFieldTypeString;
            pFieldsEdit.AddField(pField5);
            #endregion

            #region 创建要素类
            IFeatureClass pFeatClass;
            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
            #endregion
            #endregion

            #region 添加要素

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

                int n = nodeList.Count;
                if (n == 0)
                    return;

                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PointClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    //IPoint curResultPoint = null;
                    ProxiNode curPoint = null;
                    if (nodeList[i] == null)
                        continue;

                    curPoint = nodeList[i]; ;
                    ((PointClass)shp).PutCoords(curPoint.X, curPoint.Y);

                    feature.Shape = shp;
                    feature.set_Value(2, nodeList[i].ID);
                    feature.set_Value(3, nodeList[i].X);
                    feature.set_Value(4, nodeList[i].Y);
                    feature.set_Value(5, nodeList[i].TagID);
                    feature.set_Value(6, nodeList[i].FeatureType.ToString());

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
