using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System.Windows.Forms;
using System.IO;

namespace AuxStructureLib
{
    /// <summary>
    /// 三角形顶点
    /// </summary>
         [Serializable]
    public class TriNode:Node
    {
       // public double X;        //坐标
       // public double Y;        //坐标
       // public int ID;         //编号
        public int TagValue;   //标识值,1、道路网：当为端点是为-1，否则为所属线目标的ID
        public double Initial_X = 0;//点可能偏移，这里记录点偏移的初始位置X
        public double Initial_Y = 0;//点可能偏移，这里记录点偏移的初始位置Y

        public double MoveDis = 0;
        /// <summary>
        /// 要素类型
        /// </summary>
        public FeatureType FeatureType = FeatureType.Unknown; 
        /// <summary>
        /// 构造函数
        /// </summary>
        public TriNode()
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="x">坐标X</param>
        /// <param name="y">坐标Y</param>

        public TriNode(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="x">坐标X</param>
        /// <param name="y">坐标Y</param>
        /// <param name="id">编号id</param>
        public TriNode(double x, double y, int id)
        {
            X = x;
            Y = y;
            ID = id;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="x">坐标X</param>
        /// <param name="y">坐标Y</param>
        /// <param name="id">编号id</param>
        public TriNode(double x, double y, int id, int tagValue)
        {
            X = x;
            Y = y;
            ID = id;
            this.TagValue = tagValue;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="x">坐标X</param>
        /// <param name="y">坐标Y</param>
        /// <param name="id">编号id</param>
        /// <param name="ftype">要素类型</param>
        public TriNode(double x, double y, int id, int tagValue, FeatureType ftype)
        {
            X = x;
            Y = y;
            ID = id;
            this.TagValue = tagValue;
            FeatureType = ftype;
        }

        /// <summary>
        /// 最小Y值的Id号
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static int GetYMinPoint(List<TriNode> points)
        {
            int id = 0;
            double yMin = points[0].Y;
            for (int i = 0; i < points.Count; i++)
            {
                if (yMin > points[i].Y || (yMin == points[i].Y) && (points[0].X > points[i].X))
                {
                    id = i;
                    yMin = points[i].Y;
                }
            }
            return id;
        }
        /// <summary>
        /// 点顺序排列，不知有什么作用难道是从往左排序？
        /// </summary>
        /// <param name="points"></param>
        /* public static void GetAssortedPoints(List<TriNode> points)
         {
             List<TriNode> pointsTemp = new List<TriNode>();
            
             int index = GetYMinPoint(points);
             TriNode point = points[index];
             points.Remove(point);
             for (int i = 0; i < points.Count-1; i++)
             {
                 for (int j = i + 1; j < points.Count; j++)
                 {
                     if (GetCos(point, points[i]) > GetCos(point, points[j]))
                     {
                         TriNode p = points[i];
                         points[i] = points[j];
                         points[j] = p;
                     }
                 }
             }
             points.Insert(0, point);
         }*/

        /// <summary>
        /// 点顺序排列，不知有什么作用难道是从往左排序？
        /// </summary>
        /// <param name="points"></param>
        public static void GetAssortedPoints(List<TriNode> points)
        {
            List<TriNode> pointsTemp = new List<TriNode>();

            int index = GetYMinPoint(points);
            TriNode point = points[index];
            points.Remove(point);

            int k = 0;
            for (int i = 0; i < points.Count - 1; i++)
            {
                k = i;
                for (int j = i + 1; j < points.Count; j++)
                {
                    if ((multiply(points[j], points[k], point) > 0) || ((multiply(points[j], points[k], point) == 0) && (dis(points[0], points[j]) < dis(points[0], points[k]))))
                    {
                        k = j;
                    }
                }
                TriNode p = points[i];
                points[i] = points[k];
                points[k] = p;
            }
            points.Insert(0, point);
        }


        //小于0,说明向量p0p1的极角大于p0p2的极角   
        public static float multiply(TriNode p1, TriNode p2, TriNode p0)
        {
            return (float)((p1.X - p0.X) * (p2.Y - p0.Y) - (p2.X - p0.X) * (p1.Y - p0.Y));
        }

        public static float dis(TriNode p1, TriNode p2)
        {
            return (float)(Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y)));
        }



        /// <summary>
        /// 计算方位角余弦
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double GetCos(TriNode p1, TriNode p2)
        {
            double length = Math.Sqrt(TriEdge.LengthSquare(p1, p2));
            return (p2.X - p1.X) / length;
        }

        /// <summary>
        /// 计算方位角余弦
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double GetSin(TriNode p1, TriNode p2)
        {
            double length = Math.Sqrt(TriEdge.LengthSquare(p1, p2));
            return (p2.Y - p1.Y) / length;
        }
        /// <summary>
        /// 判断两点是否相等，不靠谱
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public static bool PointsEqual(TriNode point1, TriNode point2)
        {
            if ((point1.X - point2.X) < 0.0000001 && (point1.Y - point2.Y) < 0.0000001)
                return true;
            return
                false;
        }

        /// <summary>
        /// 判断点是否在多边形内部
        ///  by Philippe Reverdy is to compute the sum of the angles made between t
        ///  he test point and each pair of points making up the polygon. 
        ///  If this sum is 2pi then the point is an interior point, 
        ///  if 0 then the point is an exterior point. This also works for polygons with holes
        ///  given the polygon is defined with a path made up of coincident edges into and out 
        ///  of the hole as is common practice in many CAD packages.
        /// </summary>
        /// <param name="polygon">多边形点序列</param>
        /// <returns>是否在内部</returns>
        public bool InsidePolygon(List<TriNode> polygon)
        {
            int i;
            double angle = 0;
            TriNode p1 = new TriNode();
            TriNode p2 = new TriNode();

            int n = polygon.Count;
            for (i = 0; i < n; i++)
            {
                p1.X = polygon[i].X - this.X;
                p1.Y = polygon[i].Y - this.Y;
                p2.X = polygon[(i + 1) % n].X - this.X;
                p2.Y = polygon[(i + 1) % n].Y - this.Y;
                angle += Angle2D(p1.X, p1.Y, p2.X, p2.Y);
            }

            if (Math.Abs(angle) < Math.PI)
                return false;
            else
                return true;
        }

        /*
           Return the angle between two vectors on a plane
           The angle is from vector 1 to vector 2, positive anticlockwise
           The result is between -pi -> pi
        */
        double Angle2D(double x1, double y1, double x2, double y2)
        {
            double dtheta, theta1, theta2;

            theta1 = Math.Atan2(y1, x1);
            theta2 = Math.Atan2(y2, x2);
            dtheta = theta2 - theta1;
            while (dtheta > Math.PI)
                dtheta -= 2.0 * Math.PI;
            while (dtheta < -1 * Math.PI)
                dtheta += 2.0 * Math.PI;
            return (dtheta);
        }

             /// <summary>
             /// 
             /// </summary>
             /// <param name="triNodeList"></param>
             /// <param name="p"></param>
             /// <returns></returns>
        public static TriNode GetNode(List<TriNode> triNodeList, ITinNode p)
        {
            foreach (TriNode curNode in triNodeList)
            {
                if (curNode.X - p.X < 0.000001 && curNode.Y - p.Y < 0.000001)
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
        public static void Create_WriteVetex2Shp(string filePath, string fileName, List<TriNode> TriNodeList, ISpatialReference prj)
        {
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);
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
            pFieldEdit5.Name_2 = "MoveDis";
            pFieldEdit5.Type_2 = esriFieldType.esriFieldTypeDouble;
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

                int n = TriNodeList.Count;
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
                    if (TriNodeList[i] == null)
                        continue;

                    curPoint = TriNodeList[i]; ;
                    ((PointClass)shp).PutCoords(curPoint.X, curPoint.Y);

                    feature.Shape = shp;
                    feature.set_Value(2, TriNodeList[i].ID);
                    feature.set_Value(3, TriNodeList[i].X);
                    feature.set_Value(4, TriNodeList[i].Y);
                    feature.set_Value(5, TriNodeList[i].TagValue);
                    feature.set_Value(6, TriNodeList[i].MoveDis);

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
