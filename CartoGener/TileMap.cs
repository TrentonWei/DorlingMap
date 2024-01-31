using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.GlobeCore;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Geoprocessing;
using AuxStructureLib;
using AuxStructureLib.IO;
using AlgEMLib;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;


namespace CartoGener
{
    public partial class TileMap : Form
    {
        public TileMap(AxESRI.ArcGIS.Controls.AxMapControl axMapControl)
        {
            InitializeComponent();
            this.pMap = axMapControl.Map;
            this.pMapControl = axMapControl;
        }

        #region Parameters
        AxESRI.ArcGIS.Controls.AxMapControl pMapControl;
        IMap pMap;
        string OutlocalFilePath, OutfileNameExt, OutFilePath;
        FeatureHandle pFeatureHandle = new FeatureHandle();
        #endregion

        /// <summary>
        /// Initialization
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TileMap_Load(object sender, EventArgs e)
        {
            if (this.pMap.LayerCount <= 0)
                return;

            #region 添加图层
            ILayer pLayer;
            string strLayerName;
            for (int i = 0; i < this.pMap.LayerCount; i++)
            {
                pLayer = this.pMap.get_Layer(i);
                strLayerName = pLayer.Name;
                IDataset LayerDataset = pLayer as IDataset;

                if (LayerDataset != null)
                {
                    IFeatureLayer pFeatureLayer = pLayer as IFeatureLayer;
                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        this.comboBox1.Items.Add(strLayerName);
                        this.comboBox2.Items.Add(strLayerName);
                        this.comboBox7.Items.Add(strLayerName);
                        this.comboBox8.Items.Add(strLayerName);
                    }

                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPoint)
                    {
                        this.comboBox4.Items.Add(strLayerName);
                        this.comboBox5.Items.Add(strLayerName);
                        this.comboBox6.Items.Add(strLayerName);
                    }
                }
            }
            #endregion

            #region 默认显示第一个
            if (this.comboBox1.Items.Count > 0)
            {
                this.comboBox1.SelectedIndex = 0;
            }
            if (this.comboBox2.Items.Count > 0)
            {
                this.comboBox2.SelectedIndex = 0;
            }
            if (this.comboBox4.Items.Count > 0)
            {
                this.comboBox4.SelectedIndex = 0;
            }
            if (this.comboBox5.Items.Count > 0)
            {
                this.comboBox5.SelectedIndex = 0;
            }
            if (this.comboBox6.Items.Count > 0)
            {
                this.comboBox6.SelectedIndex = 0;
            }
            if (this.comboBox7.Items.Count > 0)
            {
                this.comboBox7.SelectedIndex = 0;
            }
            if (this.comboBox8.Items.Count > 0)
            {
                this.comboBox8.SelectedIndex = 0;
            }
            #endregion
        }

        /// <summary>
        /// GridMap_V1_20221027
        /// 1.依据面之间的邻接关系构建邻近图
        /// 2.依据面重心和边缘节点构建约束三角网后建立RNG
        /// 3.依据邻近关系结构重构RNG图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            #region 数据读取与邻近关系构建
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            IFeatureLayer AreaFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);//Area是面状要素
            IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);///Boundary是面状要素
            list.Add(BoundaryLayer);
            //list.Add(AreaFeatureLayer);

            IFeatureClass AreaFeatureClass = AreaFeatureLayer.FeatureClass;
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(AreaFeatureClass, 0);//构建的邻近图是对于AreaFeature的重心
            //if (OutFilePath != null) { npg.WriteProxiGraph2Shp(OutFilePath, "pG_1", pMap.SpatialReference); }
            ProxiGraph Copynpg = Clone((object)npg) as ProxiGraph;
            if (OutFilePath != null) { Copynpg.WriteProxiGraph2Shp(OutFilePath, "pG_5", pMap.SpatialReference); }

            //SMap tMap = new SMap(list);
            //tMap.ReadDateFrmEsriLyrs();
            IFeature pFeaure = BoundaryLayer.FeatureClass.GetFeature(0);
            IArea pArea = pFeaure.Shape as IArea;
            double FullArea = pArea.Area;
            #endregion

            #region Node Transform
            TileMapSupport TMS = new TileMapSupport();
            double Size = TMS.GetTileSize(FullArea, AreaFeatureClass.FeatureCount(null));//Get Tile size
            for (int i = 0; i < 30; i++)
            {
                Console.WriteLine(i.ToString());
                //TMS.NodesTransform(npg.NodeList, npg.EdgeList,tMap.PolygonList[0],AreaFeatureClass.FeatureCount(null));
                npg.PgRefinedShort(npg.NodeList, Size, true);
                TMS.NodesTransform(npg.NodeList, npg.EdgeList, FullArea, AreaFeatureClass.FeatureCount(null));
            }
            #endregion

            //if (OutFilePath != null) { npg.WriteProxiGraph2Shp(OutFilePath, "pG_2", pMap.SpatialReference); }

            #region Boundary Transform
            List<ProxiNode> FinalPoint = npg.NodeList;//新位置
            SMap map = new SMap(list);//数据的读取，边缘+节点
            map.ReadDateFrmEsriLyrs();

            #region 将AreaFeature中要素的重心添加至map中
            for (int i = 0; i < AreaFeatureClass.FeatureCount(null); i++)
            {
                IArea aArea = AreaFeatureClass.GetFeature(i).Shape as IArea;
                TriNode CacheNode = new TriNode(aArea.Centroid.X, aArea.Centroid.Y, map.TriNodeList.Count, i);//Id是PointCount的序号；TagId是对应元素的编号
                CacheNode.FeatureType = FeatureType.PointType;
                PointObject CachePn = new PointObject(i, CacheNode);
                map.PointList.Add(CachePn);//添加点要素
                map.TriNodeList.Add(CacheNode);//添加地图中所有节点
            }
            #endregion

            #region 关联边缘和中心点的邻近图构建（RNG）
            DelaunayTin dt = new DelaunayTin(map.TriNodeList); ///Dt中节点的ID和Map.PointList中节点的ID是一样的
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);
            dt.CreateRNG();
            ProxiGraph pg = new ProxiGraph(dt.RNGNodeList, dt.RNGEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的
            //if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG_3", pMap.SpatialReference); }
            ///依据邻近图关联的节点，添加至新的邻近图中，并进行重构

            #endregion

            TMS.PgRefresh(Copynpg, pg); //更新中心点构成的邻近图
            if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG_4", pMap.SpatialReference); }
            #endregion
        }

        /// <summary>
        /// Node transformer 20221027
        /// 邻接关系构建时依赖邻接关系创建！
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            #region 数据读取与邻近关系构建
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            IFeatureLayer AreaFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
            list.Add(BoundaryLayer);
            list.Add(AreaFeatureLayer);

            IFeatureClass AreaFeatureClass = AreaFeatureLayer.FeatureClass;
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(AreaFeatureClass, 0);
            if (OutFilePath != null) { npg.WriteProxiGraph2Shp(OutFilePath, "pG_1", pMap.SpatialReference); }

            //SMap tMap = new SMap(list);
            //tMap.ReadDateFrmEsriLyrs();
            IFeature pFeaure = BoundaryLayer.FeatureClass.GetFeature(0);
            IArea pArea = pFeaure.Shape as IArea;
            double FullArea = pArea.Area;
            #endregion

            #region Node Transform
            TileMapSupport TMS = new TileMapSupport();
            for (int i = 0; i < 20; i++)
            {
                Console.WriteLine(i.ToString());
                //TMS.NodesTransform(npg.NodeList, npg.EdgeList,tMap.PolygonList[0],AreaFeatureClass.FeatureCount(null));
                TMS.NodesTransform(npg.NodeList, npg.EdgeList, FullArea, AreaFeatureClass.FeatureCount(null));
            }
            #endregion

            if (OutFilePath != null) { npg.WriteProxiGraph2Shp(OutFilePath, "pG_2", pMap.SpatialReference); }
        }

        /// <summary>
        /// OutPut
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fdialog = new FolderBrowserDialog();
            string outfilepath = null;

            if (fdialog.ShowDialog() == DialogResult.OK)
            {
                string Path = fdialog.SelectedPath;
                outfilepath = Path;
            }

            OutFilePath = outfilepath;
            this.comboBox3.Text = OutFilePath;
        }

        /// <summary>
        /// Node transformer 20221027
        /// 邻接关系构建时依赖邻接关系创建！(对于小于一定距离的边进行添加后更新)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            #region 数据读取与邻近关系构建
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            IFeatureLayer AreaFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
            list.Add(BoundaryLayer);
            //list.Add(AreaFeatureLayer);

            IFeatureClass AreaFeatureClass = AreaFeatureLayer.FeatureClass;
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(AreaFeatureClass, 0);
            if (OutFilePath != null) { npg.WriteProxiGraph2Shp(OutFilePath, "pG_1", pMap.SpatialReference); }

            //SMap tMap = new SMap(list);
            //tMap.ReadDateFrmEsriLyrs();
            IFeature pFeaure = BoundaryLayer.FeatureClass.GetFeature(0);
            IArea pArea = pFeaure.Shape as IArea;
            double FullArea = pArea.Area;
            #endregion

            #region Node Transform
            TileMapSupport TMS = new TileMapSupport();
            double Size = TMS.GetTileSize(FullArea, AreaFeatureClass.FeatureCount(null));//Get Tile size
            for (int i = 0; i < 30; i++)
            {
                Console.WriteLine(i.ToString());
                //TMS.NodesTransform(npg.NodeList, npg.EdgeList,tMap.PolygonList[0],AreaFeatureClass.FeatureCount(null));
                npg.PgRefinedShort(npg.NodeList, Size, true);
                TMS.NodesTransform(npg.NodeList, npg.EdgeList, FullArea, AreaFeatureClass.FeatureCount(null));
            }
            #endregion

            if (OutFilePath != null) { npg.WriteProxiGraph2Shp(OutFilePath, "pG_2", pMap.SpatialReference); }
        }

        /// <summary>
        /// 深拷贝（通用拷贝）
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static object Clone(object obj)
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(memoryStream, obj);
            memoryStream.Position = 0;
            return formatter.Deserialize(memoryStream);
        }

        /// <summary>
        /// 保持节点移动后的重心与原始各点重心一致
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            TileMapSupport TMS = new TileMapSupport();

            #region 数据读取与邻近关系构建
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            IFeatureLayer AreaFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
            list.Add(BoundaryLayer);
            //list.Add(AreaFeatureLayer);

            IFeatureClass AreaFeatureClass = AreaFeatureLayer.FeatureClass;
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(AreaFeatureClass, 0);
            if (OutFilePath != null) { npg.WriteProxiGraph2Shp(OutFilePath, "pG_1", pMap.SpatialReference); }
            //ProxiNode OriginalCenter = TMS.GetGroupNodeCenter(npg.NodeList);

            //SMap tMap = new SMap(list);
            //tMap.ReadDateFrmEsriLyrs();
            IFeature pFeaure = BoundaryLayer.FeatureClass.GetFeature(0);
            IArea pArea = pFeaure.Shape as IArea;
            double FullArea = pArea.Area;
            ProxiNode OriginalCenter = new ProxiNode(pArea.Centroid.X, pArea.Centroid.Y);
            #endregion

            #region Node Transform

            double Size = TMS.GetTileSize(FullArea, AreaFeatureClass.FeatureCount(null));//Get Tile size
            for (int i = 0; i < 30; i++)
            {
                Console.WriteLine(i.ToString());
                //TMS.NodesTransform(npg.NodeList, npg.EdgeList,tMap.PolygonList[0],AreaFeatureClass.FeatureCount(null));
                npg.PgRefinedShort(npg.NodeList, Size, true);
                TMS.NodesTransform(npg.NodeList, npg.EdgeList, FullArea, AreaFeatureClass.FeatureCount(null));
            }
            #endregion

            #region Center recover
            TMS.CenterRecover(npg, OriginalCenter);
            #endregion

            if (OutFilePath != null) { npg.WriteProxiGraph2Shp(OutFilePath, "pG_2", pMap.SpatialReference); }
        }

        /// <summary>
        /// GraphConstruction_V2_20221111
        /// 1.依据边界图层和区域的重心图层构建DT/RNG/MST
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            #region 数据读取
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox2.Text != null)
            {
                IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BoundaryLayer);
            }

            IFeatureLayer RegionLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);

            if (this.comboBox4.Text != null)
            {
                IFeatureLayer PointLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
                list.Add(PointLayer);
            }

            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrs();
            #endregion

            #region 依据邻接关系构建
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(RegionLayer.FeatureClass, 0);//构建的邻近图是对于AreaFeature的重心
            #endregion

            #region 邻近图构建
            //DelaunayTin dt = new DelaunayTin(map.TriNodeList); ///Dt中节点的ID和Map.PointList中节点的ID是一样的                                                       
            //dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);

            #region DT
            //ProxiGraph pg = new ProxiGraph(dt.TriNodeList, dt.TriEdgeList); //DT
            #endregion

            #region DT MST
            //dt.CreateMST();
            //ProxiGraph pg = new ProxiGraph(dt.MSTNodeList, dt.MSTEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的 （MST）
            #endregion

            #region DT RNG
            //dt.CreateRNG();
            //ProxiGraph pg = new ProxiGraph(dt.RNGNodeList, dt.RNGEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的
            #endregion

            #region CDT
            //ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            //cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);
            //ProxiGraph pg = new ProxiGraph(cdt.TriNodeList, cdt.TriEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的 (DT)
            #endregion

            #region CDT MST
            //ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            //cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);
            //cdt.CreateMST();
            //ProxiGraph pg = new ProxiGraph(cdt.MSTNodeList, cdt.MSTEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的 （MST）
            #endregion

            #region CDT RNG
            //ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            //cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);
            //cdt.CreateRNG();
            //ProxiGraph pg = new ProxiGraph(cdt.RNGNodeList, cdt.RNGEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的 （RNG）
            #endregion

            #region CDT RefinedRNG(构建CDT时若两区域邻接，则不删除该边)
            //ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            //cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);
            //cdt.LabelAdj(RegionLayer.FeatureClass, 0);
            //cdt.CreateRefinedRNG();
            //ProxiGraph pg = new ProxiGraph(cdt.RNGNodeList, cdt.RNGEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的 （RNG）
            #endregion

            #region 构建RNG；同时，用邻接图的边来refine(即不包含在邻接图中的边都需要添加到RNG)
            //ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            //cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);
            //cdt.CreateRefinedRNG();
            //ProxiGraph pg = new ProxiGraph(cdt.RNGNodeList, cdt.RNGEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的 （RNG）
            //pg.PgRefined_4(npg.EdgeList);
            #endregion

            //if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG", pMap.SpatialReference); }
            if (OutFilePath != null) { npg.WriteProxiGraph2Shp(OutFilePath, "npG", pMap.SpatialReference); }
            //cdt.DeleteEdgeOutPolygon(map.PolygonList[0]);
            //ProxiGraph cpg = new ProxiGraph(cdt.TriNodeList, cdt.TriEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的 (DT)
            //if (OutFilePath != null) { cpg.WriteProxiGraph2Shp(OutFilePath, "cpG", pMap.SpatialReference); }
            #endregion
        }

        /// <summary>
        /// Snake移位方法处理边界（依据区域之间的邻接关系构建邻近图）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button7_Click(object sender, EventArgs e)
        {
            TileMapSupport TMS = new TileMapSupport();

            #region 数据读取
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            IFeatureLayer AreaFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);//Area是面状要素
            IFeatureLayer CenterLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
            IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);///Boundary是面状要素
            list.Add(BoundaryLayer);
            list.Add(CenterLayer);
            //list.Add(AreaFeatureLayer);
            SMap map = new SMap(list);//数据的读取，边缘+节点
            map.ReadDateFrmEsriLyrs();
            #endregion

            #region 将AreaFeature中要素的重心添加至map中
            //IFeatureClass AreaFeatureClass = AreaFeatureLayer.FeatureClass;
            //for (int i = 0; i < AreaFeatureClass.FeatureCount(null); i++)
            //{
            //    IArea aArea = AreaFeatureClass.GetFeature(i).Shape as IArea;
            //    TriNode CacheNode = new TriNode(aArea.Centroid.X, aArea.Centroid.Y, map.TriNodeList.Count, i);//Id是PointCount的序号；TagId是对应元素的编号
            //    CacheNode.FeatureType = FeatureType.PointType;
            //    PointObject CachePn = new PointObject(i, CacheNode);
            //    map.PointList.Add(CachePn);//添加点要素
            //    map.TriNodeList.Add(CacheNode);//添加地图中所有节点
            //}
            #endregion

            #region 依据区域的邻接关系构建邻近图
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(AreaFeatureLayer.FeatureClass, 0);//构建的邻近图是对于AreaFeature的重心
            //if (OutFilePath != null) { npg.WriteProxiGraph2Shp(OutFilePath, "npG_Out", pMap.SpatialReference); }
            #endregion

            #region 关联边缘和中心点的邻近图构建（RNG）
            DelaunayTin dt = new DelaunayTin(map.TriNodeList); ///Dt中节点的ID和Map.PointList中节点的ID是一样的                                                       
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);
            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);
            cdt.CreateRefinedRNG();
            ProxiGraph pg = new ProxiGraph(cdt.RNGNodeList, cdt.RNGEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的 （RNG）
            pg.PgRefined_4(npg.EdgeList);//
            //if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG_Out", pMap.SpatialReference); }
            #endregion

            //TMS.PgRefresh(npg, pg); //更新中心点构成的邻近图
            //if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG_Out2", pMap.SpatialReference); }

            #region 计算Size
            IFeature pFeaure = BoundaryLayer.FeatureClass.GetFeature(0);
            IArea pArea = pFeaure.Shape as IArea;
            double FullArea = pArea.Area;
            double Size = TMS.GetTileSize(FullArea, AreaFeatureLayer.FeatureClass.FeatureCount(null));//Get Tile size
            #endregion

            if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG", pMap.SpatialReference); }

            TMS.TileMapSnake_PgReBuiltMapIn(pg, map, 1, 10000000, 10000000, 50, 0, Size, 0.01, OutFilePath, pMap, 0.5, 0.5);
            map.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
            //终止条件：1.移位距离最小；2.迭代次数达到最终次数；3.移位的距离不会变的更小（连续两次）【或平均受力变大】；
        }

        /// <summary>
        /// Beams
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button8_Click(object sender, EventArgs e)
        {
            TileMapSupport TMS = new TileMapSupport();

            #region 数据读取
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            IFeatureLayer AreaFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);//Area是面状要素
            IFeatureLayer CenterLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
            IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);///Boundary是面状要素
            list.Add(BoundaryLayer);
            list.Add(CenterLayer);
            //list.Add(AreaFeatureLayer);
            SMap map = new SMap(list);//数据的读取，边缘+节点
            map.ReadDateFrmEsriLyrs();
            #endregion

            #region 将AreaFeature中要素的重心添加至map中
            //IFeatureClass AreaFeatureClass = AreaFeatureLayer.FeatureClass;
            //for (int i = 0; i < AreaFeatureClass.FeatureCount(null); i++)
            //{
            //    IArea aArea = AreaFeatureClass.GetFeature(i).Shape as IArea;
            //    TriNode CacheNode = new TriNode(aArea.Centroid.X, aArea.Centroid.Y, map.TriNodeList.Count, i);//Id是PointCount的序号；TagId是对应元素的编号
            //    CacheNode.FeatureType = FeatureType.PointType;
            //    PointObject CachePn = new PointObject(i, CacheNode);
            //    map.PointList.Add(CachePn);//添加点要素
            //    map.TriNodeList.Add(CacheNode);//添加地图中所有节点
            //}
            #endregion

            #region 依据区域的邻接关系构建邻近图
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(AreaFeatureLayer.FeatureClass, 0);//构建的邻近图是对于AreaFeature的重心
            //if (OutFilePath != null) { npg.WriteProxiGraph2Shp(OutFilePath, "npG_Out", pMap.SpatialReference); }
            #endregion

            #region 关联边缘和中心点的邻近图构建（RNG）
            DelaunayTin dt = new DelaunayTin(map.TriNodeList); ///Dt中节点的ID和Map.PointList中节点的ID是一样的                                                       
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);
            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);
            cdt.CreateRefinedRNG();
            ProxiGraph pg = new ProxiGraph(cdt.RNGNodeList, cdt.RNGEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的 （RNG）
            pg.PgRefined_4(npg.EdgeList);
            //if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG_Out", pMap.SpatialReference); }
            #endregion

            //TMS.PgRefresh(npg, pg); //更新中心点构成的邻近图
            //if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG_Out2", pMap.SpatialReference); }

            #region 计算Size
            IFeature pFeaure = BoundaryLayer.FeatureClass.GetFeature(0);
            IArea pArea = pFeaure.Shape as IArea;
            double FullArea = pArea.Area;
            double Size = TMS.GetTileSize(FullArea, AreaFeatureLayer.FeatureClass.FeatureCount(null));//Get Tile size
            #endregion

            TMS.TileMapBeams_PgReBuiltMapIn(pg, map, 1, 10, 1, 1, 50, 0, Size, 0.01, OutFilePath, pMap, 0.5, 0.5);
            map.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
            //终止条件：1.移位距离最小；2.迭代次数达到最终次数；3.移位的距离不会变的更小（连续两次）【或平均受力变大】；
        }

        /// <summary>
        /// 不考虑区域的邻接关系
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button9_Click(object sender, EventArgs e)
        {
            #region 数据读取
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox2.Text != null)
            {
                IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BoundaryLayer);
            }

            IFeatureLayer RegionLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);

            IFeatureLayer PointLayer = null;
            if (this.comboBox4.Text != null)
            {
                PointLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
                list.Add(PointLayer);
            }

            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrs();
            #endregion

            #region 邻近图构建
            DelaunayTin dt = new DelaunayTin(map.TriNodeList); ///Dt中节点的ID和Map.PointList中节点的ID是一样的                                                       
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);

            #region DT
            //ProxiGraph pg = new ProxiGraph(dt.TriNodeList, dt.TriEdgeList); //DT
            #endregion

            #region DT MST
            //dt.CreateMST();
            //ProxiGraph pg = new ProxiGraph(dt.MSTNodeList, dt.MSTEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的 （MST）
            #endregion

            #region DT RNG
            //dt.CreateRNG();
            //ProxiGraph pg = new ProxiGraph(dt.RNGNodeList, dt.RNGEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的
            #endregion

            #region CDT
            //ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            //cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);
            //cdt.DeleteEdgeOutPolygon(map.PolygonList[0]);//删除区域外的边
            //ProxiGraph pg = new ProxiGraph(cdt.TriNodeList, cdt.TriEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的 (DT)
            #endregion

            #region CDT MST
            //ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            //cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);
            //cdt.DeleteEdgeOutPolygon(map.PolygonList[0]);//删除区域外的边
            //cdt.CreateMST();
            //ProxiGraph pg = new ProxiGraph(cdt.MSTNodeList, cdt.MSTEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的 （MST）
            #endregion

            #region CDT RNG
            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);
            cdt.DeleteEdgeOutPolygon(map.PolygonList[0]);//删除区域外的边
            cdt.CreateRNG();
            ProxiGraph pg = new ProxiGraph(cdt.RNGNodeList, cdt.RNGEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的 （RNG）
            #endregion

            #region 构建点集的MST
            //ProxiGraph mpg = new ProxiGraph();
            //mpg.CreateProxiGByDT_Point(PointLayer.FeatureClass);
            //mpg.CreateGravityMST(mpg.NodeList, mpg.EdgeList);
            //mpg.EdgeList = mpg.MSTEdgeList;
            #endregion

            #region 判断离群点重构CDT
            //pg.PgRefined_5(mpg.EdgeList);
            #endregion

            if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG", pMap.SpatialReference); }
            //if (OutFilePath != null) { mpg.WriteProxiGraph2Shp(OutFilePath, "mpG", pMap.SpatialReference); }
            #endregion
        }

        /// <summary>
        /// Snake生成（直接构建DT和CDT，可能有离群的点—得添加对应的点）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button10_Click(object sender, EventArgs e)
        {
            TileMapSupport TMS = new TileMapSupport();

            #region 数据读取
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            IFeatureLayer BoundaryLayer = null;
            if (this.comboBox2.Text != null)
            {
                BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BoundaryLayer);
            }

            IFeatureLayer RegionLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);

            IFeatureLayer PointLayer = null;
            if (this.comboBox4.Text != null)
            {
                PointLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
                list.Add(PointLayer);
            }

            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrs();
            #endregion

            #region 邻近图（CDT）构建
            DelaunayTin dt = new DelaunayTin(map.TriNodeList); ///Dt中节点的ID和Map.PointList中节点的ID是一样的                                                       
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);
            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);
            cdt.DeleteEdgeOutPolygon(map.PolygonList[0]);//删除区域外的边
            ProxiGraph pg = new ProxiGraph(cdt.TriNodeList, cdt.TriEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的 (DT)
            pg.DeleteRepeatedEdge2(pg.EdgeList);
            #endregion

            #region 构建点集的MST，并判断离群点重构CDT
            //ProxiGraph mpg = new ProxiGraph();
            //mpg.CreateProxiGByDT_Point(PointLayer.FeatureClass);
            //mpg.CreateGravityMST(mpg.NodeList, mpg.EdgeList);
            //mpg.EdgeList = mpg.MSTEdgeList;
            //pg.PgRefined_5(mpg.EdgeList);
            #endregion

            if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG", pMap.SpatialReference); }
            //if (OutFilePath != null) { mpg.WriteProxiGraph2Shp(OutFilePath, "mpG", pMap.SpatialReference); }

            #region 计算Size
            IFeature pFeaure = BoundaryLayer.FeatureClass.GetFeature(0);
            IArea pArea = pFeaure.Shape as IArea;
            double FullArea = pArea.Area;
            double Size = TMS.GetTileSize(FullArea, RegionLayer.FeatureClass.FeatureCount(null));//Get Tile size
            #endregion

            //TMS.TileMapSnake_PgReBuiltMapIn(pg, map, 1, 10000000, 10000000, 30, 0, Size, 0.01, OutFilePath, pMap, 0.5, 0.5);//Chinese Data
            TMS.TileMapSnake_PgReBuiltMapIn(pg, map, 1, 10000000, 10000000, 30, 0, Size, 0.01, OutFilePath, pMap, 100, 100);//Chinese Data
            map.WriteResult2Shp(OutFilePath, pMap.SpatialReference);

        }

        /// <summary>
        /// Create Tile and Fit Center to Tiles
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button11_Click(object sender, EventArgs e)
        {
            #region 数据读取
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            IFeatureLayer BoundaryLayer = null;
            if (this.comboBox2.Text != null)
            {
                BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BoundaryLayer);
            }

            //IFeatureLayer RegionLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);

            IFeatureLayer PointLayer = null;
            if (this.comboBox4.Text != null)
            {
                PointLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
                list.Add(PointLayer);
            }
            #endregion

            #region 计算Size
            TileMapSupport TMS = new TileMapSupport();
            IFeature pFeaure = BoundaryLayer.FeatureClass.GetFeature(0);
            IArea pArea = pFeaure.Shape as IArea;
            double FullArea = pArea.Area;
            double SizeR = TMS.GetTileSize(FullArea, PointLayer.FeatureClass.FeatureCount(null));//Get Tile size
            #endregion

            #region 格网生成
            ITopologicalOperator iTop = pFeaure.Shape as ITopologicalOperator;
            double MinX = pFeaure.Extent.XMin; double MinY = pFeaure.Extent.YMin; double MaxX = pFeaure.Extent.XMax; double MaxY = pFeaure.Extent.YMax;
            Dictionary<Envelope, double> GridDic = new Dictionary<Envelope, double>();

            //MinX = MinX + 0.5 * SizeR;
            MaxY = MaxY - 0.5 * SizeR;

            //获取相交的Grid
            for (int i = 0; MinX + i * SizeR < MaxX + SizeR; i++)
            {
                for (int j = 0; MaxY - j * SizeR > MinY - SizeR; j++)
                {
                    double CacheMinX = MinX + SizeR * i;
                    double CacheMaxY = MaxY - SizeR * j;
                    double CacheMaxX = MinX + SizeR * (i + 1);
                    double CacheMinY = MaxY - SizeR * (j + 1);

                    Envelope CacheEnvelope = new Envelope();
                    CacheEnvelope.PutCoords(CacheMinX, CacheMinY, CacheMaxX, CacheMaxY);

                    IGeometry pGeo = iTop.Intersect(CacheEnvelope as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                    double IntersectS = 0;
                    if (!pGeo.IsEmpty)
                    {
                        IArea CachepGeo = pGeo as IArea;
                        double CacheArea = CachepGeo.Area;
                        IntersectS = CacheArea / FullArea;
                    }

                    GridDic.Add(CacheEnvelope, IntersectS);
                }
            }

            //获取权重的前n个Grid
            var sortedDic = GridDic.OrderByDescending(x => x.Value);//按照Value排序(Lamda语句)
            var Keys = sortedDic.Take(PointLayer.FeatureClass.FeatureCount(null)).Select(x => x.Key);//获取前n个要素
            #endregion

            #region 输出所有格网
            //SMap CacheEnveMap = new SMap();
            //List<PolygonObject> CachePoList = new List<PolygonObject>();
            //int Index = 0;
            //foreach (KeyValuePair<Envelope, double> kv in GridDic)
            //{
            //    List<TriNode> trilist = new List<TriNode>();

            //    TriNode tPoint_1 = new TriNode(kv.Key.XMin, kv.Key.YMin, 0, 1);
            //    trilist.Add(tPoint_1);
            //    TriNode tPoint_2 = new TriNode(kv.Key.XMax, kv.Key.YMin, 1, 1);
            //    trilist.Add(tPoint_2);
            //    TriNode tPoint_3 = new TriNode(kv.Key.XMax, kv.Key.YMax, 2, 1);
            //    trilist.Add(tPoint_3);
            //    TriNode tPoint_4 = new TriNode(kv.Key.XMin, kv.Key.YMax, 3, 1);
            //    trilist.Add(tPoint_4);
            //    TriNode tPoint_5 = new TriNode(kv.Key.XMin, kv.Key.YMin, 4, 1);
            //    trilist.Add(tPoint_5);

            //    PolygonObject mPolygonObject = new PolygonObject(Index, trilist);
            //    CachePoList.Add(mPolygonObject);
            //    Index++;
            //}
            //CacheEnveMap.PolygonList = CachePoList;
            //CacheEnveMap.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
            #endregion

            #region 匈牙利匹配 Keys=Grids PointLayer=Points
            //Use scipy linear_sum_assignment to solve this problem via PyCharm
            //See our Code Named HungarianMethod in CartoGener file
            //Input Matrix[Center Distance between GridCenter and Points]
            double[,] MatrixDis = new double[PointLayer.FeatureClass.FeatureCount(null), PointLayer.FeatureClass.FeatureCount(null)];
            for (int i = 0; i < Keys.Count(); i++)
            {
                double CacheMinX = Keys.ElementAt(i).XMin; double CacheMaxX = Keys.ElementAt(i).XMax; double CacheMinY = Keys.ElementAt(i).YMin; double CacheMaxY = Keys.ElementAt(i).YMax;
                double CenterX = (CacheMinX + CacheMaxX) / 2; double CenterY = (CacheMinY + CacheMaxY) / 2;
                for (int j = 0; j < PointLayer.FeatureClass.FeatureCount(null); j++)
                {
                    IPoint CachePoint = PointLayer.FeatureClass.GetFeature(j).Shape as IPoint;
                    double CacheDis = Math.Sqrt((CachePoint.Y - CenterY) * (CachePoint.Y - CenterY) + (CachePoint.X - CenterX) * (CachePoint.X - CenterX));
                    MatrixDis[i, j] = CacheDis;
                }
            }
            #endregion

            #region 输出保留的格网
            #region 输出数组值
            for (int i = 0; i < PointLayer.FeatureClass.FeatureCount(null); i++)
            {
                for (int j = 0; j < PointLayer.FeatureClass.FeatureCount(null); j++)
                {
                    Console.Write(MatrixDis[i, j]);//输出矩阵值
                    Console.Write(" ");
                }
                Console.Write("\n");
            }
            #endregion

            #region 输出格网
            SMap EnveMap = new SMap();
            List<PolygonObject> PoList = new List<PolygonObject>();
            for (int i = 0; i < Keys.Count(); i++)
            {
                List<TriNode> trilist = new List<TriNode>();

                TriNode tPoint_1 = new TriNode(Keys.ElementAt(i).XMin, Keys.ElementAt(i).YMin, 0, 1);
                trilist.Add(tPoint_1);
                TriNode tPoint_2 = new TriNode(Keys.ElementAt(i).XMax, Keys.ElementAt(i).YMin, 1, 1);
                trilist.Add(tPoint_2);
                TriNode tPoint_3 = new TriNode(Keys.ElementAt(i).XMax, Keys.ElementAt(i).YMax, 2, 1);
                trilist.Add(tPoint_3);
                TriNode tPoint_4 = new TriNode(Keys.ElementAt(i).XMin, Keys.ElementAt(i).YMax, 3, 1);
                trilist.Add(tPoint_4);
                TriNode tPoint_5 = new TriNode(Keys.ElementAt(i).XMin, Keys.ElementAt(i).YMin, 4, 1);
                trilist.Add(tPoint_5);

                PolygonObject mPolygonObject = new PolygonObject(i, trilist);
                PoList.Add(mPolygonObject);
            }
            EnveMap.PolygonList = PoList;
            EnveMap.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
            #endregion
            #endregion

        }

        /// <summary>
        /// 最小二乘方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button12_Click(object sender, EventArgs e)
        {
            CTPSupport CTPS = new CTPSupport();

            #region 数据读取（原始数据）
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox2.Text != null)
            {
                IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BoundaryLayer);
            }

            if (this.comboBox4.Text != null)
            {
                IFeatureLayer PointDistanceLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
                list.Add(PointDistanceLayer);
            }
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrs();
            #endregion

            #region 数据读取(终点数据)
            List<IFeatureLayer> Finallist = new List<IFeatureLayer>();
            if (this.comboBox5.Text != null)
            {
                IFeatureLayer FinalPointLayer = pFeatureHandle.GetLayer(pMap, this.comboBox5.Text);
                Finallist.Add(FinalPointLayer);
            }
            SMap Pointmap = new SMap(Finallist);
            Pointmap.ReadDateFrmEsriLyrs();
            List<TriNode> FinalPoint = new List<TriNode>();
            for (int i = 0; i < Pointmap.PointList.Count; i++)
            {
                FinalPoint.Add(Pointmap.PointList[i].Point);
            }
            //FinalPoint.RemoveRange(20, FinalPoint.Count - 21);
            #endregion

            //SMap CacheMap = new SMap();
            //CacheMap.PointList = CacheFinalPoint;
            //CacheMap.WriteResult2Shp(OutFilePath, pMap.SpatialReference);

            #region 最小二乘计算边界点
            List<TriNode> BoundNodes = map.PolygonList[0].PointList;
            List<TriNode> OriginalNodes = new List<TriNode>();
            for (int i = 0; i < map.PointList.Count; i++)
            {
                OriginalNodes.Add(map.PointList[i].Point);
            }

            List<Tuple<double, double>> XYs = CTPS.LeastSquareAdj(BoundNodes, OriginalNodes, FinalPoint, 50, 1);

            for (int i = 0; i < map.PolygonList[0].PointList.Count; i++)
            {
                map.PolygonList[0].PointList[i].X = XYs[i].Item1;
                map.PolygonList[0].PointList[i].Y = XYs[i].Item2;
            }
            #endregion

            map.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
        }

        #region Add Gaussian noise for Points
        private void button13_Click(object sender, EventArgs e)
        {
            #region 数据读取（原始数据）
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox4.Text != null)
            {
                IFeatureLayer PointDistanceLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
                list.Add(PointDistanceLayer);
            }
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrs();
            #endregion

            #region 添加随机噪声
            // 设置高斯分布参数
            double mean = 0.01;   // 平均值
            double stdDev = 1; // 标准差

            Random random = new Random();

            for (int i = 0; i < map.PointList.Count; i++)
            {
                // 生成随机高斯噪声
                double noiseX = GaussianNoise(mean, stdDev);
                double noiseY = GaussianNoise(mean, stdDev);

                // 更新每个点的位置
                map.PointList[i].Point.X += noiseX;
                map.PointList[i].Point.Y += noiseY;
            }
            #endregion

            #region 输出
            map.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
            #endregion
        }
        #endregion


        private static double GaussianNoise(double mean, double stdDev)
        {
            //基本原理：https://zhuanlan.zhihu.com/p/73841698
            // Box-Muller转换算法生成符合正态分布（高斯）的随机变量
            Random randomGenerator = new Random();
            double u1 = 1.0 - randomGenerator.NextDouble();
            double u2 = 1.0 - randomGenerator.NextDouble();
            double z0 = Math.Sqrt(-2 * Math.Log(u1)) * Math.Sin(2 * Math.PI * u2);

            return mean + z0 * stdDev;
        }

        /// <summary>
        /// Location Cost
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button14_Click(object sender, EventArgs e)
        {
            #region 获取Points图层和Grids图层
            IFeatureLayer PointLayer = null;//获得基准Point
            if (this.comboBox6.Text != null)
            {
                PointLayer = pFeatureHandle.GetLayer(pMap, this.comboBox6.Text);
            }
            IFeatureLayer GridLayer = null;//获得变化后Grids
            if (this.comboBox7.Text != null)
            {
                GridLayer = pFeatureHandle.GetLayer(pMap, this.comboBox7.Text);
            }
            #endregion

            #region 读取基准的Point
            IFeatureClass PointFeatureClass = PointLayer.FeatureClass;
            Dictionary<string, IPoint> sDic = new Dictionary<string, IPoint>();//基准
            IFeatureCursor sFeatureCursor = PointFeatureClass.Update(null, false);
            IFeature sFeature = sFeatureCursor.NextFeature();
            while (sFeature != null)
            {
                string Name = this.GetStringValue(sFeature, "FID");//For注记配置评价
                IPoint pPoint = sFeature.Shape as IPoint;
                sDic.Add(Name, pPoint);

                sFeature = sFeatureCursor.NextFeature();
            }
            #endregion

            #region 获取对应Matching的Grid
            IFeatureClass GridFeatureClass = GridLayer.FeatureClass;
            Dictionary<string, IPolygon> aDic = new Dictionary<string, IPolygon>();//变化后
            IFeatureCursor aFeatureCursor = GridFeatureClass.Update(null, false);
            IFeature aFeature = aFeatureCursor.NextFeature();
            while (aFeature != null)
            {
                string Name = this.GetStringValue(aFeature, "ID");
                IPolygon pPolygon = aFeature.Shape as IPolygon;
                aDic.Add(Name, pPolygon);
                aFeature = aFeatureCursor.NextFeature();
            }
            #endregion

            #region 计算Location Cost
            double DisSum = 0;
            foreach (KeyValuePair<string, IPoint> kv in sDic)
            {
                IPoint tPoint = kv.Value;
                IPolygon mPolygon = aDic[kv.Key];

                IArea mArea = mPolygon as IArea;
                IPoint mPoint = mArea.Centroid;

                double Dis = Math.Sqrt((tPoint.X - mPoint.X) * (tPoint.X - mPoint.X) + (tPoint.Y - mPoint.Y) * (tPoint.Y - mPoint.Y));
                DisSum = Dis + DisSum;
            }
            #endregion

            MessageBox.Show(DisSum.ToString());
        }

        /// <summary>
        /// 获取给定Feature的属性
        /// </summary>
        /// <param name="CurFeature"></param>
        /// <param name="FieldString"></param>
        /// <returns></returns>
        public string GetStringValue(IFeature curFeature, string FieldString)
        {
            string Value = null;

            IFields pFields = curFeature.Fields;
            int field1 = pFields.FindField(FieldString);
            Value = Convert.ToString(curFeature.get_Value(field1));

            return Value;
        }

        //Adjacent cost
        private void button15_Click(object sender, EventArgs e)
        {
            #region 获取Points图层和Grids图层
            IFeatureLayer AreaLayer = null;//获得基准Area
            if (this.comboBox1.Text != null)
            {
                AreaLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            }
            IFeatureLayer GridLayer = null;//获得变化后Grids
            if (this.comboBox7.Text != null)
            {
                GridLayer = pFeatureHandle.GetLayer(pMap, this.comboBox7.Text);
            }
            #endregion

            #region 读取基准的Area
            IFeatureClass AreaFeatureClass = AreaLayer.FeatureClass;
            Dictionary<string, IPolygon> sDic = new Dictionary<string, IPolygon>();//基准
            IFeatureCursor sFeatureCursor = AreaFeatureClass.Update(null, false);
            IFeature sFeature = sFeatureCursor.NextFeature();
            while (sFeature != null)
            {
                string Name = this.GetStringValue(sFeature, "FID");//For注记配置评价
                IPolygon pPolygon = sFeature.Shape as IPolygon;
                sDic.Add(Name, pPolygon);

                sFeature = sFeatureCursor.NextFeature();
            }
            #endregion

            #region 获取对应Matching的Grid
            IFeatureClass GridFeatureClass = GridLayer.FeatureClass;
            Dictionary<string, IPolygon> aDic = new Dictionary<string, IPolygon>();//变化后
            IFeatureCursor aFeatureCursor = GridFeatureClass.Update(null, false);
            IFeature aFeature = aFeatureCursor.NextFeature();
            while (aFeature != null)
            {
                string Name = this.GetStringValue(aFeature, "ID");
                IPolygon pPolygon = aFeature.Shape as IPolygon;
                aDic.Add(Name, pPolygon);
                aFeature = aFeatureCursor.NextFeature();
            }
            #endregion

            #region 计算Area的邻接关系
            List<Tuple<string, string>> TouchedList = new List<Tuple<string, string>>();
            for (int i = 0; i < AreaFeatureClass.FeatureCount(null) - 1; i++)
            {
                for (int j = i + 1; j < AreaFeatureClass.FeatureCount(null); j++)
                {
                    if (j != i)
                    {
                        try
                        {
                            IGeometry iGeo = AreaFeatureClass.GetFeature(i).Shape;
                            IGeometry jGeo = AreaFeatureClass.GetFeature(j).Shape;

                            IRelationalOperator iRo = iGeo as IRelationalOperator;
                            if (iRo.Touches(jGeo) || iRo.Overlaps(jGeo))
                            {
                                string NameI = this.GetStringValue(AreaFeatureClass.GetFeature(i), "FID");
                                string NameJ = this.GetStringValue(AreaFeatureClass.GetFeature(j), "FID");

                                Tuple<string, string> NameMatch = new Tuple<string, string>(NameI, NameJ);
                                TouchedList.Add(NameMatch);
                            }
                        }

                        catch
                        {

                        }
                    }
                }
            }
            #endregion

            #region 判断NameMatch的匹配对对应的格网是否也存在邻近关系
            double Count = 0;
            foreach (Tuple<string, string> NameMatch in TouchedList)
            {
                IPolygon Po1 = aDic[NameMatch.Item1];
                IPolygon Po2 = aDic[NameMatch.Item2];

                IRelationalOperator IPO = Po1 as IRelationalOperator;
                 if (IPO.Touches(Po2 as IGeometry) || IPO.Overlaps(Po2 as IGeometry))
                 {
                    Count++;
                 }
            }
            #endregion

            double Rate = Count / TouchedList.Count;
            MessageBox.Show(Rate.ToString());
        }

        /// <summary>
        /// Oritation Cost
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button16_Click(object sender, EventArgs e)
        {
            #region 获取Points图层和Grids图层
            IFeatureLayer AreaLayer = null;//获得基准Area
            if (this.comboBox1.Text != null)
            {
                AreaLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            }
            IFeatureLayer GridLayer = null;//获得变化后Grids
            if (this.comboBox7.Text != null)
            {
                GridLayer = pFeatureHandle.GetLayer(pMap, this.comboBox7.Text);
            }
            #endregion

            #region 读取基准的Area
            IFeatureClass AreaFeatureClass = AreaLayer.FeatureClass;
            Dictionary<string, IPolygon> sDic = new Dictionary<string, IPolygon>();//基准
            IFeatureCursor sFeatureCursor = AreaFeatureClass.Update(null, false);
            IFeature sFeature = sFeatureCursor.NextFeature();
            while (sFeature != null)
            {
                string Name = this.GetStringValue(sFeature, "FID");//For注记配置评价
                IPolygon pPolygon = sFeature.Shape as IPolygon;
                sDic.Add(Name, pPolygon);

                sFeature = sFeatureCursor.NextFeature();
            }
            #endregion

            #region 获取对应Matching的Grid
            IFeatureClass GridFeatureClass = GridLayer.FeatureClass;
            Dictionary<string, IPolygon> aDic = new Dictionary<string, IPolygon>();//变化后
            IFeatureCursor aFeatureCursor = GridFeatureClass.Update(null, false);
            IFeature aFeature = aFeatureCursor.NextFeature();
            while (aFeature != null)
            {
                string Name = this.GetStringValue(aFeature, "ID");
                IPolygon pPolygon = aFeature.Shape as IPolygon;
                aDic.Add(Name, pPolygon);
                aFeature = aFeatureCursor.NextFeature();
            }
            #endregion

            #region 计算Area的邻接关系
            List<Tuple<string, string>> TouchedList = new List<Tuple<string, string>>();
            for (int i = 0; i < AreaFeatureClass.FeatureCount(null) - 1; i++)
            {
                for (int j = i + 1; j < AreaFeatureClass.FeatureCount(null); j++)
                {
                    if (j != i)
                    {
                        try
                        {
                            IGeometry iGeo = AreaFeatureClass.GetFeature(i).Shape;
                            IGeometry jGeo = AreaFeatureClass.GetFeature(j).Shape;

                            IRelationalOperator iRo = iGeo as IRelationalOperator;
                            if (iRo.Touches(jGeo) || iRo.Overlaps(jGeo))
                            {
                                string NameI = this.GetStringValue(AreaFeatureClass.GetFeature(i), "FID");
                                string NameJ = this.GetStringValue(AreaFeatureClass.GetFeature(j), "FID");

                                Tuple<string, string> NameMatch = new Tuple<string, string>(NameI, NameJ);
                                TouchedList.Add(NameMatch);
                            }
                        }

                        catch
                        {

                        }
                    }
                }
            }
            #endregion

            #region 计算方向变化
            List<double> ChangeAngleList = new List<double>();
            foreach (Tuple<string, string> NameMatch in TouchedList)
            {
                #region 方向1
                IPolygon sPo1 = sDic[NameMatch.Item1];
                IPolygon sPo2 = sDic[NameMatch.Item2];
                IArea sArea1 = sPo1 as IArea;
                IArea sArea2 = sPo2 as IArea;
                IPoint sPoint1 = sArea1.Centroid;
                IPoint sPoint2 = sArea2.Centroid;

                ILine sLine = new LineClass();
                sLine.FromPoint = sPoint1;
                sLine.ToPoint = sPoint2;
                double Ori1 = sLine.Angle;
                #endregion

                #region 方向2
                IPolygon aPo1 = aDic[NameMatch.Item1];
                IPolygon aPo2 = aDic[NameMatch.Item2];
                IArea aArea1 = aPo1 as IArea;
                IArea aArea2 = aPo2 as IArea;
                IPoint aPoint1 = aArea1.Centroid;
                IPoint aPoint2 = aArea2.Centroid;
                ILine aLine = new LineClass();
                aLine.FromPoint = aPoint1;
                aLine.ToPoint = aPoint2;
                double Ori2 = aLine.Angle;
                #endregion

                #region 计算变化的绝对值
                if (Ori1 < 0)
                {
                    Ori1 = Ori1 + 3.1415926;
                }

                Ori1 = Ori1 / 3.1415926 * 180;

                if (Ori2 < 0)
                {
                    Ori2 = Ori2 + 3.1415926;
                }

                Ori2 = Ori2 / 3.1415926 * 180;

                double ChangeOri = Math.Abs(Ori1 - Ori2);
                if (ChangeOri > 90)
                {
                    ChangeOri = 180 - ChangeOri;
                }
                ChangeAngleList.Add(ChangeOri);
                #endregion
            }
            #endregion

            #region 计算方向均方根（RMS）
            double Ave = ChangeAngleList.Average();//计算方向变化平均值(AVE)

            double Sum = 0;
            for (int i = 0; i < ChangeAngleList.Count; i++)
            {
                Sum = ChangeAngleList[i] * ChangeAngleList[i] + Sum;
            }
            double RMS = Math.Sqrt(Sum / ChangeAngleList.Count);
            #endregion

            MessageBox.Show(Ave.ToString());
        }

        /// <summary>
        /// Other Tiles
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button17_Click(object sender, EventArgs e)
        {
            #region 数据读取
            IFeatureLayer TilesLayer = null;
            if (this.comboBox2.Text != null)
            {
                TilesLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
            }

            IFeatureLayer PointLayer = null;
            if (this.comboBox4.Text != null)
            {
                PointLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
            }
            #endregion

            #region 匈牙利匹配 Keys=Grids PointLayer=Points
            //Use scipy linear_sum_assignment to solve this problem via PyCharm
            //See our Code Named HungarianMethod in CartoGener file
            //Input Matrix[Center Distance between GridCenter and Points]
            double[,] MatrixDis = new double[PointLayer.FeatureClass.FeatureCount(null), PointLayer.FeatureClass.FeatureCount(null)];
            for (int i = 0; i < TilesLayer.FeatureClass.FeatureCount(null); i++)
            {
                IArea pArea = TilesLayer.FeatureClass.GetFeature(i).Shape as IArea;
                double CenterX = pArea.Centroid.X; ; double CenterY = pArea.Centroid.Y;
                for (int j = 0; j < PointLayer.FeatureClass.FeatureCount(null); j++)
                {
                    IPoint CachePoint = PointLayer.FeatureClass.GetFeature(j).Shape as IPoint;
                    double CacheDis = Math.Sqrt((CachePoint.Y - CenterY) * (CachePoint.Y - CenterY) + (CachePoint.X - CenterX) * (CachePoint.X - CenterX));
                    MatrixDis[i, j] = CacheDis;
                }
            }
            #endregion

            #region 输出数组值
            for (int i = 0; i < PointLayer.FeatureClass.FeatureCount(null); i++)
            {
                for (int j = 0; j < PointLayer.FeatureClass.FeatureCount(null); j++)
                {
                    Console.Write(MatrixDis[i, j]);//输出矩阵值
                    Console.Write(" ");
                }
                Console.Write("\n");
            }
            #endregion
        }
    }
}
