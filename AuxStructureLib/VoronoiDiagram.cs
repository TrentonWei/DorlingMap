using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using System.Windows.Forms;
using System.IO;
using System.Data;
using AuxStructureLib.IO;

namespace AuxStructureLib
{
    /// <summary>
    /// Voronoi
    /// </summary>
    public class VoronoiDiagram
    {
        public List<VoronoiPolygon> VorPolygonList = null; //Voronoi多边形列表
        public List<TriNode> PointList = null;             //点集
        DelaunayTin tin  = null;                    //三角网
        ConvexNull convexNull = null;               //点集的凸包
        public Skeleton Skeleton = null;
        public ProxiGraph ProxiGraph=null;
        public SMap Map=null;

        /// <summary>
        /// 从骨架线创建V图
        /// </summary>
        /// <param name="skeleton"></param>
        public VoronoiDiagram(Skeleton skeleton, ProxiGraph proxiGraph, SMap map)
        {
            ProxiGraph = proxiGraph;          
            Skeleton = skeleton;
            Map=map;
            VorPolygonList = new List<VoronoiPolygon>();
        }

        /// <summary>
        /// 根据ID和类型获取多边形对象
        /// </summary>
        /// <param name="id">ID号</param>
        /// <param name="Type">类型</param>
        /// <returns>返回类型</returns>
        public VoronoiPolygon GetVPbyIDandType(int id, FeatureType Type)
        {
            foreach (VoronoiPolygon vp in this.VorPolygonList)
            {
                if (vp.MapObj != null && vp.MapObj.ID == id && vp.MapObj.FeatureType == Type)
                    return vp;
            }
            return null;
        }
        /// <summary>
        /// 从骨架线创建建筑物和点群的的V图
        /// </summary>
        public void CreateVoronoiDiagramfrmSkeletonforBuildings()
        {
            //初始化多边形，根据邻近图
            foreach (ProxiNode node in ProxiGraph.NodeList)
            {
                if (node.FeatureType == FeatureType.PolygonType)
                {
                    int id = node.TagID;
                    TriNode point=new TriNode(node.X,node.Y);
                    MapObject mapObj=this.Map.GetObjectbyID(id,FeatureType.PolygonType);
                    VoronoiPolygon curPolygon = new VoronoiPolygon(mapObj, point);
                    this.VorPolygonList.Add(curPolygon);
                }
            }
            //将骨架线弧段分配到每个多边形的弧段列表中
            foreach (Skeleton_Arc curArc in this.Skeleton.Skeleton_ArcList)
            {
                if (curArc.LeftMapObj != null && curArc.LeftMapObj.FeatureType == FeatureType.PolygonType)
                {
                    int id = curArc.LeftMapObj.ID;
                    VoronoiPolygon curVP=this.GetVoronoiPolygonbyTagIDandType(id, curArc.LeftMapObj.FeatureType);
                   if(curVP!=null)
                    {curVP.ArcList.Add(curArc);}
                }
                if (curArc.RightMapObj != null && curArc.RightMapObj.FeatureType == FeatureType.PolygonType)
                {
                    int id = curArc.RightMapObj.ID;
                    VoronoiPolygon curVP=this.GetVoronoiPolygonbyTagIDandType(id, curArc.RightMapObj.FeatureType);
                    if (curVP != null)
                    { curVP.ArcList.Add(curArc); }
                }
            }
            //创建VP
            foreach(VoronoiPolygon vp in this.VorPolygonList)
            {
                //StreamWriter streamw = File.CreateText(@"K:\4-10\北京\Result" +@"\temp.TXT");
                //streamw.Write("No" + "  " + "sx" + "  " + "sy" + "  " + "sx" + "  " + "sy");
                //streamw.WriteLine();
                //foreach (Skeleton_Arc arc in vp.ArcList)
                //{
                //    streamw.Write(arc.PointList[0].X.ToString() + "  " + arc.PointList[0].Y.ToString() + "  " + arc.PointList[arc.PointList.Count - 1].X.ToString() + "  " + arc.PointList[arc.PointList.Count - 1].Y.ToString());
                //    streamw.WriteLine();
                //}
                //streamw.Close();
                //break;
                vp.CreateVoronoiPolygonfrmSkeletonArcList();
            }
        }

        /// <summary>
        /// 从骨架线创建建筑物的V图
        /// </summary>
        public void CreateVoronoiDiagramfrmSkeletonforBuildingsPoints()
        {
            //初始化多边形，根据邻近图
            foreach (ProxiNode node in ProxiGraph.NodeList)
            {
                if (node.FeatureType != FeatureType.PolylineType)
                {
                    int id = node.TagID;
                    TriNode point = new TriNode(node.X, node.Y);
                    MapObject mapObj = this.Map.GetObjectbyID(id, node.FeatureType);
                    VoronoiPolygon curPolygon = new VoronoiPolygon(mapObj, point);
                    this.VorPolygonList.Add(curPolygon);
                }
            }
            //将骨架线弧段分配到每个多边形的弧段列表中
            foreach (Skeleton_Arc curArc in this.Skeleton.Skeleton_ArcList)
            {
                if (curArc.LeftMapObj != null && curArc.LeftMapObj.FeatureType != FeatureType.PolylineType)
                {
                    int id = curArc.LeftMapObj.ID;
                    VoronoiPolygon curVP = this.GetVoronoiPolygonbyTagIDandType(id, curArc.LeftMapObj.FeatureType);
                    if (curVP != null)
                    { curVP.ArcList.Add(curArc); }
                }
                if (curArc.RightMapObj != null && curArc.RightMapObj.FeatureType != FeatureType.PolylineType)
                {
                    int id = curArc.RightMapObj.ID;
                    VoronoiPolygon curVP = this.GetVoronoiPolygonbyTagIDandType(id, curArc.RightMapObj.FeatureType);
                    if (curVP != null)
                    { curVP.ArcList.Add(curArc); }
                }
            }
            //创建VP
            foreach (VoronoiPolygon vp in this.VorPolygonList)
            {
                //StreamWriter streamw = File.CreateText(@"K:\4-10\北京\Result" +@"\temp.TXT");
                //streamw.Write("No" + "  " + "sx" + "  " + "sy" + "  " + "sx" + "  " + "sy");
                //streamw.WriteLine();
                //foreach (Skeleton_Arc arc in vp.ArcList)
                //{
                //    streamw.Write(arc.PointList[0].X.ToString() + "  " + arc.PointList[0].Y.ToString() + "  " + arc.PointList[arc.PointList.Count - 1].X.ToString() + "  " + arc.PointList[arc.PointList.Count - 1].Y.ToString());
                //    streamw.WriteLine();
                //}
                //streamw.Close();
                //break;
                vp.CreateVoronoiPolygonfrmSkeletonArcList();
            }
        }

        /// <summary>
        /// 根据ID和对象类型获取其对应的V图多边形
        /// </summary>
        /// <param name="id">对象ID</param>
        /// <param name="type">FeatureType</param>
        public VoronoiPolygon GetVoronoiPolygonbyTagIDandType(int id, FeatureType type)
        {
            foreach(VoronoiPolygon curVP in this.VorPolygonList)
            {
                if(curVP.MapObj.ID==id&&curVP.MapObj.FeatureType==type)
                    return curVP;
            }
            return null;
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="PointList"></param>
        public VoronoiDiagram(List<TriNode> pointList)
        {
            PointList = pointList;
            VorPolygonList = new List<VoronoiPolygon>();
        }

        /// <summary>
        /// 创建Voronoi图
        /// </summary>
        public void CreateVoronoiDiagram()
        {
            //创建凸壳
            this.convexNull = new ConvexNull(this.PointList);
            convexNull.CreateConvexNull();
            //创建TIN
            this.tin = new DelaunayTin(this.PointList);
            tin.CreateDelaunayTin(AlgDelaunayType.Side_extent);
            foreach (TriNode point in this.PointList)
            {
                VoronoiPolygon vp = new VoronoiPolygon(point);
                //判断是否凸壳上的点
                int index = 0;
                if(convexNull.ContainPoint(point,out index))
                {
                    vp.IsPolygon = false;
                    int n=PointList.Count;
                    TriEdge edge1 = new TriEdge(point, PointList[(index - 1 + n) % n]);
                    TriEdge edge2 = new TriEdge(point, PointList[index + 1 % n]);

                    foreach (Triangle curTri in tin.TriangleList)
                    {

                        if (curTri.ContainEdge(edge1))
                        {
                            vp.PointSet.Add(edge1.EdgeMidPoint);
                        }
                        else if (curTri.ContainEdge(edge2))
                        {
                            vp.PointSet.Add(edge2.EdgeMidPoint);
                        }
                        else if (curTri.ContainPoint(point))
                        {
                            vp.PointSet.Add(curTri.CircumCenter);
                        }
                    }
                }

                else
                {
                    foreach (Triangle curTri in tin.TriangleList)
                    {
                        if (curTri.ContainPoint(point))
                        {
                            vp.PointSet.Add(curTri.CircumCenter);
                        }
                    }
                }
                vp.CreateVoronoiPolygon();
                if (vp.IsPolygon)
                {
                    this.VorPolygonList.Add(vp);
                }
            }
        }


        /// <summary>
        /// 将面写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="Skeleton_SegmentList">线列表</param>
        public void Create_WritePolygonObject2Shp(string filePath, string fileName, esriSRProjCS4Type prj)
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

            //IField pField2;
            //IFieldEdit pFieldEdit2;
            //pField2 = new FieldClass();
            //pFieldEdit2 = pField1 as IFieldEdit;
            //pFieldEdit2.Length_2 = 30;//对象类型
            //pFieldEdit2.Name_2 = "Type";
            //pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeString;
            //pFieldsEdit.AddField(pField2);
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

                int n = this.VorPolygonList.Count;
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
                    if (this.VorPolygonList[i] == null)
                        continue;
                    int m = this.VorPolygonList[i].PointSet.Count;

                    for (int k = 0; k < m; k++)
                    {
                        curPoint = this.VorPolygonList[i].PointSet[k];
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    }
                    //curPoint = this.VorPolygonList[i].PointSet[0];
                    //curResultPoint = new PointClass();
                    //curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    //pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    feature.Shape = shp;
                    feature.set_Value(2, this.VorPolygonList[i].MapObj.ID);//编号 
                  //  feature.set_Value(3, this.VorPolygonList[i].MapObj.FeatureType.ToString());//编号 

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

        public void CalandOutputDensity(ProxiGraph pg,string strPath,int iterID)
        {
            if (this.VorPolygonList == null || VorPolygonList.Count == 0)
                return;
            DataSet ds = new DataSet();
            //创建一个表
            DataTable tableforce = new DataTable();
            tableforce.TableName = "Desity" + iterID.ToString();
            tableforce.Columns.Add("ID", typeof(int));
            tableforce.Columns.Add("Density", typeof(double));
            foreach (ProxiNode curNode in pg.NodeList)
            {
                int index = curNode.ID;
                int tagID = curNode.TagID;
                FeatureType fType = curNode.FeatureType;
                VoronoiPolygon vp = null;
                PolygonObject po=null;
                if (fType == FeatureType.PolygonType)
                {

                    po = this.Map.GetObjectbyID(tagID, fType) as PolygonObject;
                    vp = this.GetVPbyIDandType(tagID, fType);
                    DataRow dr = tableforce.NewRow();
                    dr[0] = tagID;
                    dr[1] = po.Area / vp.Area;
                    tableforce.Rows.Add(dr);
                }
            }
            TXTHelper.ExportToTxt(tableforce, strPath + @"\Density" + iterID.ToString() + @".txt");
        }
    }
}
