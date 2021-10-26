using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using System.Windows.Forms;

namespace AuxStructureLib
{
   
    /// <summary>
    /// 轮廓线
    /// </summary>
    public class BorderPolygon
    {
        public List<TriNode> PointList = null;
        public float EdgeIndex=2f;//表示边为平均边长的多少倍时，将其删除
        private DelaunayTin Dt = null;
        /// <summary>
        /// 构造函数
        /// </summary>
        public BorderPolygon()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public BorderPolygon(float k)
        {
            EdgeIndex = k;
        }
        /// <summary>
        /// 从三角网和凸包创建外围轮廓线
        /// </summary>
        /// <param name="convexNull">轮廓线</param>
        /// <param name="dt">三角网</param>
        public bool CreateOutLine(ConvexNull convexNull,DelaunayTin dt)
        {
            if (convexNull == null || dt == null || convexNull.PointSet.Count < 3)
                return false;

            Dt = dt;

            this.PointList = convexNull.ConvexVertexSet;
          
            double aveLength = CalAveEdgeLength(dt);
            aveLength = aveLength * this.EdgeIndex;//判断阈值
            TriEdge lEdge = null;
            int index = -1;
            while (HasLargeEdge(aveLength, out lEdge, out index))
            {
                TriNode p = null;
                Triangle tri = Triangle.FindTriangbyNodes(dt.TriangleList, lEdge.startPoint, lEdge.endPoint, out p);
                this.PointList.Insert(index + 1, p);
                dt.DeleteTriangle(tri);
            }
            return true;
        }

        /// <summary>
        /// 是否存在长边
        /// </summary>
        /// <returns></returns>
        private bool HasLargeEdge(double l,out TriEdge edge,out int index)
        {
            edge = null;
            TriNode sp = null;
            TriNode ep = null;

            for(int i = 0; i < PointList.Count-1; i++)
            {
  
               sp = PointList[i];
               ep = PointList[i + 1];
          
                edge = new TriEdge(sp, ep);
                
                if (edge.Length > l)
                {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;

        }


        /// <summary>
        /// 计算平均边长
        /// </summary>
        /// <param name="dt">三角网</param>
        /// <returns>边长</returns>
        private double CalAveEdgeLength(DelaunayTin dt)
        {
            if(dt==null||dt.TriEdgeList==null||dt.TriEdgeList.Count==0)
                return -1;
            int EdgeN = dt.TriEdgeList.Count;
            double ave = 0;
            for (int i = 0; i < EdgeN; i++)
            {
                ave += dt.TriEdgeList[i].Length;
            }
            return ave = ave / EdgeN;
        }

        /// <summary>
        /// 将分布边界写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件夹名</param>
        /// <param name="fileName">文件名</param>
        /// <param name="PointList">点列表</param>
        /// <param name="prj">投影</param>
        public static void Create_WriteBorder2Shp(string filePath, string fileName, BorderPolygon  bp, esriSRProjCS4Type prj)
        {
            List<TriNode> PointList = bp.PointList;
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
                if (PointList == null || PointList.Count < 3)
                    return;
                int n = PointList.Count;
                IFeature feature = pFeatClass.CreateFeature();
                IGeometry shp = new PolygonClass();
                // shp.SpatialReference = mapControl.SpatialReference;
                IPointCollection pointSet = shp as IPointCollection;
                IPoint curResultPoint = null;
                TriNode curPoint = null;
                for (int i = 0; i < n; i++)
                {
                    curPoint = PointList[i];
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                }
                curPoint = PointList[0];
                curResultPoint = new PointClass();
                curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                feature.Shape = shp;
                feature.Store();//保存IFeature对象  
                fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
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
        /// 创建虚拟边界线
        /// </summary>
        /// <returns>虚拟边界线上的点序列</returns>
        public List<TriNode> CreateVirtualBorderLine()
        {
            List<TriNode> resVBorder = new List<TriNode>();
            TriNode Centrid = null;
            //计算出点群的重心
            Centrid = ComFunLib.CalpolygonCenterPoint(this.PointList);
            double extendDis = 0;        //外扩的距离
            double disfrmCentrid = 0;        //外扩的距离
            List<TriEdge>  curEdgeList=null;
            double x = 0;
            double y = 0;
            foreach (TriNode curNode in this.PointList)
            {
                curEdgeList = TriEdge.FindEdgesContsNode(this.Dt.TriEdgeList, curNode);
                if (curEdgeList != null && curEdgeList.Count > 0)
                {
                    foreach (TriEdge curEdge in curEdgeList)
                    {
                        extendDis = extendDis + curEdge.Length;
                    }
                    extendDis = extendDis / curEdgeList.Count;
                    disfrmCentrid = ComFunLib.CalLineLength(curNode, Centrid);
                    x = curNode.X + extendDis * (curNode.X - Centrid.X) / disfrmCentrid;
                    y = curNode.Y + extendDis * (curNode.Y - Centrid.Y) / disfrmCentrid;
                    resVBorder.Add(new TriNode(x,y,-1,-1));
                }
            }

            return resVBorder;
        }


        /// <summary>
        /// 将边界以ArcGIS，IPolygon对象返回
        /// </summary>
        public IPolygon ESRIPolygonObject
        {
            get
            {
                object missing1 = Type.Missing;
                object missing2 = Type.Missing;
                List<TriNode> PointList = this.PointList;
                if (PointList == null || PointList.Count < 3)
                    return null;
                int n = PointList.Count;

                IPolygon shp = new PolygonClass();
                // shp.SpatialReference = mapControl.SpatialReference;
                IPointCollection pointSet = shp as IPointCollection;
                IPoint curResultPoint = null;
                TriNode curPoint = null;
                for (int i = 0; i < n; i++)
                {
                    curPoint = PointList[i];
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                }
                curPoint = PointList[0];
                curResultPoint = new PointClass();
                curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                return shp;
            }
        }

    }
}
