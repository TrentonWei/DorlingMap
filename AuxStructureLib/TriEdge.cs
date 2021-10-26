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
    /// 三角形边
    /// </summary>
    public class TriEdge
    {
        public int ID;
        public TriNode startPoint, endPoint;
        public Triangle leftTriangle, rightTriangle;
        public TriEdge doulEdge=null;
        public int tagID = -1;
        public FeatureType FeatureType = FeatureType.Unknown;
        // private double length;
        public double Length
        {
            get
            {
                return Math.Sqrt(EdgeLengthSquare());
            }
        }
        public TriEdge(TriNode StartPoint, TriNode EndPoint)
        {
            this.startPoint = StartPoint;
            this.endPoint = EndPoint;
        }

        public TriEdge()
        {

        }
        /// <summary>
        /// 判断是否在该边的右边
        /// </summary>
        /// <param name="p3"></param>
        /// <returns></returns>
        public bool RightOfLine(TriNode p3)
        {
            double temp = (p3.X - startPoint.X) * (endPoint.Y - startPoint.Y) - (p3.Y - startPoint.Y) * (endPoint.X - startPoint.X);
            if (temp > 0)
                return true;
            else
                return false;
        }

        public double EdgeLengthSquare()
        {
            double x1 = startPoint.X;
            double y1 = startPoint.Y;
            double x2 = endPoint.X;
            double y2 = endPoint.Y;
            double lengtSquare = (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2);
            return lengtSquare;
        }

        public static double LengthSquare(TriNode point1, TriNode point2) //(x1-x2)*(x1-x2)*(y1-y2)*(y1-y2)
        {
            double x1 = point1.X;
            double y1 = point1.Y;
            double x2 = point2.X;
            double y2 = point2.Y;
            return (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2);
        }
        /// <summary>
        /// 余弦定理
        /// </summary>
        /// <param name="point3"></param>
        /// <returns></returns>
        public double CosValue(TriNode point3)   //cosx=(a*a+b*b-c*c)/2*a*b
        {
            double a = LengthSquare(startPoint, point3);
            double b = LengthSquare(endPoint, point3);
            double c = LengthSquare(startPoint, endPoint);
            double cos = (a + b - c) / (2 * Math.Sqrt(a) * Math.Sqrt(b));
            return cos;
        }

        /// <summary>
        /// 获取第三个点的ID
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="triPoint"></param>
        /// <returns></returns>
        public static int GetPointID(TriEdge edge, List<TriNode> triPoint)
        {
            double temp = 1;
            int tempID = 0;
            foreach (TriNode p in triPoint)
            {
                if (edge.RightOfLine(p))
                {
                    if (edge.CosValue(p) < temp)
                    {
                        tempID = p.ID;
                        temp = edge.CosValue(p);
                    }
                }
            }
            return tempID;
        }
        /// <summary>
        /// 查找相同边
        /// </summary>
        /// <param name="listEdge"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        public static TriEdge FindSameEdge(List<TriEdge> listEdge, TriEdge edge)
        {
            foreach (TriEdge e in listEdge)
            {
                if (e.startPoint==edge.startPoint && e.endPoint == edge.endPoint)
                    return e;
            }
            return null;
        }

        /// <summary>
        /// 查找相同边
        /// </summary>
        /// <param name="listEdge"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        public static TriEdge FindSameEdge(List<TriEdge> listEdge, TriNode startP,TriNode endP)
        {
            foreach (TriEdge e in listEdge)
            {
                if (e.startPoint.ID == startP.ID && e.endPoint.ID == endP.ID)
                    return e;
            }
            return null;
        }

        public static TriNode GetBestPoint(TriEdge edge, List<TriNode> triPoint)
        {
            if (GetPointID(edge, triPoint) == 0)
                return null;
            return triPoint[GetPointID(edge, triPoint)];
        }
        /// <summary>
        /// 搜索相反边
        /// </summary>
        /// <param name="listEdge"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        public static TriEdge FindOppsiteEdge(List<TriEdge> listEdge, TriNode startP, TriNode endP)
        {
            foreach (TriEdge e in listEdge)
            {
                if (e.startPoint.ID == endP.ID && e.endPoint.ID == startP.ID)
                    return e;
            }
            return null;
        }
        /// <summary>
        /// 搜索相反边
        /// </summary>
        /// <param name="listEdge"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        public static TriEdge FindOppsiteEdge(List<TriEdge> listEdge, TriEdge edge)
        {
            foreach (TriEdge e in listEdge)
            {
                if (e.startPoint == edge.endPoint && e.endPoint == edge.startPoint)
                    return e;
            }
            return null;
        }


        /// <summary>
        /// 找出重复边，然后删除
        /// </summary>
        /// <param name="edges"></param>
        public static void GetUniqueEdges(List<TriEdge> edges)
        {
            List<TriEdge> edgesTemp = new List<TriEdge>();
            foreach (TriEdge edge in edges)
            {
                TriEdge edgeTemp = FindOppsiteEdge(edges, edge);
                if (edgeTemp != null && FindOppsiteEdge(edgesTemp, edgeTemp) == null)
                {
                    edgesTemp.Add(edgeTemp);
                }
            }
            foreach (TriEdge e in edgesTemp)
            {
                edges.Remove(e);
            }
        }

        /// </summary>
        /// 修整边的LeftTriangle属性
        /// </summary>
        /// <param name="edges"></param>
        public static void AmendEdgeLeftTriangle(List<TriEdge> edges)
        {

            foreach (TriEdge edge in edges)
            {
                TriEdge oe=FindOppsiteEdge(edges, edge);
                if (oe != null)
                {
                    edge.doulEdge = oe;
                    oe.doulEdge = edge;

                    edge.rightTriangle = oe.leftTriangle;
                    oe.rightTriangle = edge.leftTriangle;
                }
                else
                {
                    edge.doulEdge = null;
                }
            }
        }

        public TriNode EdgeMidPoint
        {
            get
            {
                if (this.startPoint == null || this.endPoint == null)
                    return null;
                else
                    return new TriNode((startPoint.X + endPoint.X) / 2.0, (startPoint.Y + endPoint.Y) / 2.0);
            }
        }

        /// <summary>
        /// 给每条边编号
        /// </summary>
        /// <param name="TriEdgeList"></param>
        public static void WriteID(List<TriEdge> TriEdgeList)
        {
            if (TriEdgeList == null || TriEdgeList.Count == 0)
                return;
            int n = TriEdgeList.Count;
            for (int i = 0; i < n; i++)
            {
                TriEdgeList[i].ID = i;
            }

        }

        /// <summary>
        /// 将三角形边写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="Skeleton_SegmentList">骨架线段列表</param>
        public static void Create_WriteEdge2Shp(string filePath, string fileName, List<TriEdge> TriEdgeList, esriSRProjCS4Type prj)
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

            //对偶边doulID
            IField pField7;
            IFieldEdit pFieldEdit7;
            pField7 = new FieldClass();
            pFieldEdit7 = pField7 as IFieldEdit;
            pFieldEdit7.Length_2 = 30;
            pFieldEdit7.Name_2 = "DoulID";
            pFieldEdit7.Type_2 = esriFieldType.esriFieldTypeInteger;
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
                    TriNode curPoint = null;
                    if (TriEdgeList[i] == null)
                        continue;

                    curPoint = TriEdgeList[i].startPoint; ;
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                    curPoint = TriEdgeList[i].endPoint;
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
             
                    feature.Shape = shp;
                    feature.set_Value(2, TriEdgeList[i].ID);
                    feature.set_Value(3, TriEdgeList[i].startPoint.ID);
                    feature.set_Value(4, TriEdgeList[i].endPoint.ID);
                    if(TriEdgeList[i].leftTriangle==null)
                    {
                        feature.set_Value(5, -1);
                    }
                    else
                    {
                        feature.set_Value(5, TriEdgeList[i].leftTriangle.ID);
                    }
                    if (TriEdgeList[i].rightTriangle == null)
                    {
                        feature.set_Value(6,-1);
                    }
                    else
                    {
                        feature.set_Value(6, TriEdgeList[i].rightTriangle.ID);
                    }

                    feature.set_Value(7, TriEdgeList[i].tagID);

                    if (TriEdgeList[i].doulEdge == null)
                    {
                        feature.set_Value(8, -1);
                    }
                    else
                    {
                        feature.set_Value(8, TriEdgeList[i].doulEdge.ID);
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

        /// <summary>
        /// 判断三角形边是否一条约束边
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="ID1"></param>
        /// <param name="ID2"></param>
        /// <returns></returns>
        public static bool IsConsEdge(TriEdge edge, int ID1, int ID2)
        {
            if ((edge.startPoint.ID == ID1 && edge.endPoint.ID == ID2) || (edge.endPoint.ID == ID1 && edge.startPoint.ID == ID2))
                return true;
            return false;
        }

        /// <summary>
        /// 判断是否端点相交
        /// </summary>
        /// <param name="edge">边</param>
        /// <param name="ID1">约束边的起点ID</param>
        /// <param name="ID2">约束边的终点ID</param>
        /// <param name="InNodeID">相交端点的ID</param>
        /// <returns>是否端点相交</returns>
        public static bool IsIntersectatNode(TriEdge edge, int ID1, int ID2, out int InNodeID)
        {
            InNodeID = -1;
            if ((edge.startPoint.ID == ID1 && edge.endPoint.ID != ID2))
            {
                InNodeID = ID1;
                return true;
            }
            else if ((edge.startPoint.ID != ID1 && edge.endPoint.ID == ID2))
            {
                InNodeID = ID2;
                return true;
            }
            return false;
        }
        /// <summary>
        /// 找到包含特定点的所有边
        /// </summary>
        /// <param name="node">点</param>
        /// <returns>边列表</returns>
        public static   List<TriEdge> FindEdgesContsNode( List<TriEdge> triEdgeList,TriNode node)
        {
            List<TriEdge> resEdges = new List<TriEdge>();
            foreach (TriEdge curEdge in triEdgeList)
            {
                if (curEdge.startPoint.TagValue == node.TagValue || curEdge.endPoint.TagValue == node.TagValue)
                {
                    if(FindOppsiteEdge(resEdges,curEdge)==null||FindSameEdge(resEdges,curEdge)==null)
                    {
                        resEdges.Add(curEdge);
                    }
                }
            }
            if (resEdges != null || resEdges.Count > 0)
                return resEdges;
            return null;
        }
        /// <summary>
        /// 是否包含某点
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public bool IsContainPoint(TriNode p)
        {
            if (startPoint.ID == p.ID || endPoint.ID == p.ID)
                return true;
            return false;
        }
    }
}
