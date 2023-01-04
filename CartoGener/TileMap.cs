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
                    }

                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPoint)
                    {
                        this.comboBox4.Items.Add(strLayerName);
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

            if (this.comboBox4.Text != null)
            {
                IFeatureLayer PointLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
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
            //ProxiGraph pg = new ProxiGraph(cdt.TriNodeList, cdt.TriEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的 (DT)
            #endregion

            #region CDT MST
            //ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            //cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);
            //cdt.CreateMST();
            //ProxiGraph pg = new ProxiGraph(cdt.MSTNodeList, cdt.MSTEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的 （MST）
            #endregion

            #region CDT RNG
            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);
            cdt.CreateRNG();
            ProxiGraph pg = new ProxiGraph(cdt.RNGNodeList, cdt.RNGEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的 （RNG）
            #endregion


            if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG", pMap.SpatialReference); }
            #endregion
        }

        /// <summary>
        /// Snake移位方法处理边界
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button7_Click(object sender, EventArgs e)
        {
            #region 数据读取
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            IFeatureLayer AreaFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);//Area是面状要素
            IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);///Boundary是面状要素
            list.Add(BoundaryLayer);
            //list.Add(AreaFeatureLayer);
            SMap map = new SMap(list);//数据的读取，边缘+节点
            map.ReadDateFrmEsriLyrs();
            #endregion

            #region 将AreaFeature中要素的重心添加至map中
            IFeatureClass AreaFeatureClass = AreaFeatureLayer.FeatureClass;
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

            #region 依据区域的邻接关系构建邻近图
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(AreaFeatureClass, 0);//构建的邻近图是对于AreaFeature的重心
            if (OutFilePath != null) { npg.WriteProxiGraph2Shp(OutFilePath, "npG_Out", pMap.SpatialReference); }
            #endregion

            #region 关联边缘和中心点的邻近图构建（RNG）
            DelaunayTin dt = new DelaunayTin(map.TriNodeList); ///Dt中节点的ID和Map.PointList中节点的ID是一样的
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);
            dt.CreateRNG();
            ProxiGraph pg = new ProxiGraph(dt.RNGNodeList, dt.RNGEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的
            if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG_Out", pMap.SpatialReference); }
            #endregion

            TileMapSupport TMS = new TileMapSupport();
            TMS.PgRefresh(npg, pg); //更新中心点构成的邻近图
            if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG_Out2", pMap.SpatialReference); }

            #region 计算Size
            IFeature pFeaure = BoundaryLayer.FeatureClass.GetFeature(0);
            IArea pArea = pFeaure.Shape as IArea;
            double FullArea = pArea.Area;
            double Size = TMS.GetTileSize(FullArea, AreaFeatureClass.FeatureCount(null));//Get Tile size
            #endregion

            TMS.TileMapSnake_PgReBuiltMapIn(pg, map, 1, 1000, 10000, 200, 0, Size, 0.01, OutFilePath, pMap, 0.5, 0.5);

            //终止条件：1.移位距离最小；2.迭代次数达到最终次数；3.移位的距离不会变的更小（连续两次）【或平均受力变大】；
        }
    }
}
