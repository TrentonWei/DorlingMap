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
    /// <summary>
    ///  Central time-space Map create form
    /// </summary>
    public partial class Central_TP_MapFrm : Form
    {
        public Central_TP_MapFrm(AxESRI.ArcGIS.Controls.AxMapControl axMapControl)
        {
            InitializeComponent();
            this.pMap = axMapControl.Map;
            this.pMapControl = axMapControl;
        }

        #region parameters
        AxESRI.ArcGIS.Controls.AxMapControl pMapControl;
        IMap pMap;
        string OutlocalFilePath, OutfileNameExt, OutFilePath;
        FeatureHandle pFeatureHandle = new FeatureHandle();
        #endregion

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Central_TP_MapFrm_Load(object sender, EventArgs e)
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
                    }

                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPoint)
                    {
                        this.comboBox2.Items.Add(strLayerName);
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
        /// OutPut Path
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
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
        /// Beams
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            CTPSupport CTPS=new CTPSupport();

            #region 数据读取
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BoundaryLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer PointDistanceLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(PointDistanceLayer);
            }
            #endregion 

            #region 数据读取(终点计算)
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrs();

            List<PointObject> CacheFinalPoint = CTPS.FinalLocation(map.PointList);//获得每个点的最终位置(LocationPoint的ID和map.PointList中点ID是一样的)
            List<ProxiNode> FinalPoint = CTPS.FinalLocation_2(map.PointList);//获得每个点的最终位置(LocationPoint的ID和map.PointList中点ID是一样的)
            #endregion

            SMap CacheMap = new SMap();
            CacheMap.PointList = CacheFinalPoint;
            CacheMap.WriteResult2Shp(OutFilePath, pMap.SpatialReference);

            #region 邻近图构建
            DelaunayTin dt = new DelaunayTin(map.TriNodeList); ///Dt中节点的ID和Map.PointList中节点的ID是一样的
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);
            //dt.CreateRNG();
            ProxiGraph pg = new ProxiGraph(dt.TriNodeList,dt.TriEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的
            #endregion

            #region 生成过程
            CTPS.CTPBeams(pg, FinalPoint, 1, 10, 1, 1, pg.NodeList.Count, 0, 0, 0.01);
            if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "邻近图", pMap.SpatialReference); }//输出邻近图
            #endregion

            #region 依据邻近图结果更新Map

            #endregion

            #region 输出
            if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG", pMap.SpatialReference); }
            #endregion
        }

        /// <summary>
        /// Snake
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            CTPSupport CTPS = new CTPSupport();

            #region 数据读取
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BoundaryLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer PointDistanceLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(PointDistanceLayer);
            }
            #endregion

            #region 数据读取(终点计算)
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrs();

            List<PointObject> CacheFinalPoint = CTPS.FinalLocation(map.PointList);//获得每个点的最终位置(LocationPoint的ID和map.PointList中点ID是一样的)[两个函数是一样的，只是一个生成的PointObject；一个生成的是ProxiNode]
            List<ProxiNode> FinalPoint = CTPS.FinalLocation_2(map.PointList);//获得每个点的最终位置(LocationPoint的ID和map.PointList中点ID是一样的)
            #endregion

            SMap CacheMap = new SMap();
            CacheMap.PointList = CacheFinalPoint;
            CacheMap.WriteResult2Shp(OutFilePath, pMap.SpatialReference);

            #region 邻近图构建
            DelaunayTin dt = new DelaunayTin(map.TriNodeList); ///Dt中节点的ID和Map.PointList中节点的ID是一样的
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);
            dt.CreateRNG();
            ProxiGraph pg = new ProxiGraph(dt.RNGNodeList, dt.RNGEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的
            #endregion

            #region 生成过程
            //CTPS.CTPSnake(pg, FinalPoint, 1, 1000, 10000, pg.NodeList.Count, 0, 0, 0.01);
            CTPS.CTPSnake(pg, FinalPoint, 1, 1000, 10000, 15, 0, 0, 0.01, 100000,0.5);
            //if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "邻近图", pMap.SpatialReference); }//输出邻近图
            #endregion

            #region 依据邻近图结果更新Map

            #endregion

            #region 输出
            if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG", pMap.SpatialReference); }
            #endregion
        }

        /// <summary>
        /// 确定（邻近图重构）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            CTPSupport CTPS = new CTPSupport();

            #region 数据读取
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BoundaryLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer PointDistanceLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(PointDistanceLayer);
            }
            #endregion

            #region 数据读取(终点计算)
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrs();

            List<PointObject> CacheFinalPoint = CTPS.FinalLocation(map.PointList);//获得每个点的最终位置(LocationPoint的ID和map.PointList中点ID是一样的)[两个函数是一样的，只是一个生成的PointObject；一个生成的是ProxiNode]
            List<ProxiNode> FinalPoint = CTPS.FinalLocation_2(map.PointList);//获得每个点的最终位置(LocationPoint的ID和map.PointList中点ID是一样的)
            #endregion

            SMap CacheMap = new SMap();
            CacheMap.PointList = CacheFinalPoint;
            CacheMap.WriteResult2Shp(OutFilePath, pMap.SpatialReference);

            #region 邻近图构建
            DelaunayTin dt = new DelaunayTin(map.TriNodeList); ///Dt中节点的ID和Map.PointList中节点的ID是一样的
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);
            dt.CreateRNG();
            ProxiGraph pg = new ProxiGraph(dt.RNGNodeList, dt.RNGEdgeList); ; //Pg中节点的ID和Map.PointList中节点的ID是一样的
            #endregion

            #region 生成过程
            //CTPS.CTPSnake_PgReBuilt(pg, FinalPoint, 1, 1000, 10000, pg.NodeList.Count, 0, 0, 0.01, OutFilePath, pMap);
            CTPS.CTPSnake_PgReBuilt(pg, FinalPoint, 1, 1000, 10000, 15, 0, 0, 0.01, OutFilePath, pMap, 0.5, 0.5);
            //if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "邻近图", pMap.SpatialReference); }//输出邻近图
            #endregion

            #region 依据邻近图结果更新Map

            #endregion

            #region 输出
            if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG", pMap.SpatialReference); }
            #endregion
        }

        /// <summary>
        /// Map需要输出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            CTPSupport CTPS = new CTPSupport();

            #region 数据读取
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BoundaryLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer PointDistanceLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(PointDistanceLayer);
            }
            #endregion

            #region 数据读取(终点计算)
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrs();

            List<PointObject> CacheFinalPoint = CTPS.FinalLocation(map.PointList);//获得每个点的最终位置(LocationPoint的ID和map.PointList中点ID是一样的)[两个函数是一样的，只是一个生成的PointObject；一个生成的是ProxiNode]
            List<ProxiNode> FinalPoint = CTPS.FinalLocation_2(map.PointList);//获得每个点的最终位置(LocationPoint的ID和map.PointList中点ID是一样的)

            //FinalPoint.RemoveRange(20, FinalPoint.Count - 21);
            #endregion

            SMap CacheMap = new SMap();
            CacheMap.PointList = CacheFinalPoint;
            CacheMap.WriteResult2Shp(OutFilePath, pMap.SpatialReference);

            #region 邻近图构建
            DelaunayTin dt = new DelaunayTin(map.TriNodeList); ///Dt中节点的ID和Map.PointList中节点的ID是一样的
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);
            dt.CreateRNG();
            ProxiGraph pg = new ProxiGraph(dt.RNGNodeList, dt.RNGEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的
            //ProxiGraph pg = new ProxiGraph(dt.TriNodeList, dt.TriEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的
            #endregion

            #region 生成过程
            //CTPS.CTPSnake_PgReBuilt(pg, FinalPoint, 1, 1000, 10000, pg.NodeList.Count, 0, 0, 0.01, OutFilePath, pMap);
            CTPS.CTPSnake_PgReBuiltMapIn(pg, FinalPoint, map, 1, 1000, 10000, 300, 0, 0, 0.01, OutFilePath, pMap, 0.5, 0.5);
            //if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "邻近图", pMap.SpatialReference); }//输出邻近图
            #endregion

            #region 输出
            if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG", pMap.SpatialReference); }
            #endregion
        }

        /// <summary>
        /// StepByStep
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            CTPSupport CTPS = new CTPSupport();

            #region 数据读取
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BoundaryLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer PointDistanceLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(PointDistanceLayer);
            }
            
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrs();
            #endregion

            #region 数据读取(终点计算)  
            List<IFeatureLayer> FinalLocationlist = new List<IFeatureLayer>();
            if (this.comboBox4.Text != null)
            {
                IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
                FinalLocationlist.Add(BoundaryLayer);
            }
            SMap FinalLocationMap = new SMap(FinalLocationlist);
            List<ProxiNode> FinalPoint = CTPS.FinalLocation_3(map.PointList);//获得每个点的最终位置(LocationPoint的ID和map.PointList中点ID是一样的)
            #endregion

            #region 邻近图构建
            DelaunayTin dt = new DelaunayTin(map.TriNodeList); ///Dt中节点的ID和Map.PointList中节点的ID是一样的
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);
            //dt.CreateRNG();
            ProxiGraph pg = new ProxiGraph(dt.TriNodeList, dt.TriEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的
            #endregion

            #region 生成过程
            //CTPS.CTPSnake_PgReBuilt(pg, FinalPoint, 1, 1000, 10000, pg.NodeList.Count, 0, 0, 0.01, OutFilePath, pMap);
            CTPS.CTPSnake_PgReBuiltMapIn(pg, FinalPoint, map, 1, 1000, 10000, 1, 0, 0, 0.01, OutFilePath, pMap,100000,100000);
            //if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "邻近图", pMap.SpatialReference); }//输出邻近图
            #endregion

            #region 依据邻近图结果更新Map

            #endregion

            #region 输出
            if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG", pMap.SpatialReference); }
            #endregion
        }
        
        /// <summary>
        /// 层次Snake（不控制最大移位量）【层次的意思是指：每次只选择力最大的n个力量进行移位】
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button7_Click(object sender, EventArgs e)
        {
            CTPSupport CTPS = new CTPSupport();

            #region 数据读取
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BoundaryLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer PointDistanceLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(PointDistanceLayer);
            }

            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrs();
            #endregion

            #region 数据读取(终点计算)
            List<IFeatureLayer> FinalLocationlist = new List<IFeatureLayer>();
            if (this.comboBox4.Text != null)
            {
                IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
                FinalLocationlist.Add(BoundaryLayer);
            }
            SMap FinalLocationMap = new SMap(FinalLocationlist);
            List<ProxiNode> FinalPoint = CTPS.FinalLocation_3(map.PointList);//获得每个点的最终位置(LocationPoint的ID和map.PointList中点ID是一样的)
            #endregion

            #region 邻近图构建
            DelaunayTin dt = new DelaunayTin(map.TriNodeList); ///Dt中节点的ID和Map.PointList中节点的ID是一样的
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);
            dt.CreateRNG();
            ProxiGraph pg = new ProxiGraph(dt.RNGNodeList, dt.RNGEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的
            //ProxiGraph pg = new ProxiGraph(dt.TriNodeList, dt.TriEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的
            #endregion

            #region 生成过程
            //CTPS.CTPSnake_PgReBuilt(pg, FinalPoint, 1, 1000, 10000, pg.NodeList.Count, 0, 0, 0.01, OutFilePath, pMap);
            CTPS.CTPSnake_PgReBuiltHierMapIn(pg, FinalPoint, map, 1, 1000, 10000, 200, 0, 0, 0.01, OutFilePath, pMap, 0.5, 0.5);
            //if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "邻近图", pMap.SpatialReference); }//输出邻近图
            #endregion

            #region 输出
            if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG", pMap.SpatialReference); }
            #endregion
        }

        /// <summary>
        /// 控制最大移位量的Snake（每一次移动的最大移位量被控制）【层次的意思是指：每次只选择力最大的%个力量进行移位；同时，最大的移位量被控制】
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button8_Click(object sender, EventArgs e)
        {
            CTPSupport CTPS = new CTPSupport();

            #region 数据读取
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BoundaryLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer PointDistanceLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(PointDistanceLayer);
            }
            #endregion

            #region 数据读取(终点计算)
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrs();

            List<PointObject> CacheFinalPoint = CTPS.FinalLocation(map.PointList);//获得每个点的最终位置(LocationPoint的ID和map.PointList中点ID是一样的)[两个函数是一样的，只是一个生成的PointObject；一个生成的是ProxiNode]
            List<ProxiNode> FinalPoint = CTPS.FinalLocation_2(map.PointList);//获得每个点的最终位置(LocationPoint的ID和map.PointList中点ID是一样的)

            //FinalPoint.RemoveRange(20, FinalPoint.Count - 21);
            #endregion

            SMap CacheMap = new SMap();
            CacheMap.PointList = CacheFinalPoint;
            CacheMap.WriteResult2Shp(OutFilePath, pMap.SpatialReference);

            #region 邻近图构建
            DelaunayTin dt = new DelaunayTin(map.TriNodeList); ///Dt中节点的ID和Map.PointList中节点的ID是一样的
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);
            //dt.CreateRNG();

            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);
            ProxiGraph pg = new ProxiGraph(cdt.TriNodeList, cdt.TriEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的
            if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG", pMap.SpatialReference); }
            #endregion

            #region 生成过程
            //CTPS.CTPSnake_PgReBuilt(pg, FinalPoint, 1, 1000, 10000, pg.NodeList.Count, 0, 0, 0.01, OutFilePath, pMap);
            CTPS.CTPSnake_PgReBuiltMapIn(pg, FinalPoint, map, 1, 1000, 10000, 300, 0, 0, 0.01, OutFilePath, pMap, 0.5, 0.5);
            //if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "邻近图", pMap.SpatialReference); }//输出邻近图
            #endregion

            #region 依据邻近图结果更新Map

            #endregion

            #region 输出
            if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG", pMap.SpatialReference); }
            #endregion
        }

       
        /// <summary>
        /// 计算终点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button10_Click(object sender, EventArgs e)
        {
            CTPSupport CTPS = new CTPSupport();

            #region 数据读取
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BoundaryLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer PointDistanceLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(PointDistanceLayer);
            }
            #endregion

            #region 终点计算
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrs();

            List<PointObject> CacheFinalPoint = CTPS.FinalLocation(map.PointList);//获得每个点的最终位置(LocationPoint的ID和map.PointList中点ID是一样的)[两个函数是一样的，只是一个生成的PointObject；一个生成的是ProxiNode]
            List<TriNode> FinalPoint = CTPS.FinalLocation_4(map.PointList);//获得每个点的最终位置(LocationPoint的ID和map.PointList中点ID是一样的)
            #endregion

            #region
            SMap sMap = new SMap();
            sMap.TriNodeList = FinalPoint;
            sMap.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
            #endregion
        }

        /// <summary>
        /// 确定-边界关系维护的Snake（RNG）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button9_Click(object sender, EventArgs e)
        {
            CTPSupport CTPS = new CTPSupport();
            CTPS.mCon = this.pMapControl;

            #region 数据读取
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BoundaryLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer PointDistanceLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(PointDistanceLayer);
            }
            #endregion

            #region 数据读取(终点计算)
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrs();

            List<PointObject> CacheFinalPoint = CTPS.FinalLocation(map.PointList);//获得每个点的最终位置(LocationPoint的ID和map.PointList中点ID是一样的)[两个函数是一样的，只是一个生成的PointObject；一个生成的是ProxiNode]
            List<ProxiNode> FinalPoint = CTPS.FinalLocation_2(map.PointList);//获得每个点的最终位置(LocationPoint的ID和map.PointList中点ID是一样的)

            //FinalPoint.RemoveRange(20, FinalPoint.Count - 21);
            #endregion

            SMap CacheMap = new SMap();
            CacheMap.PointList = CacheFinalPoint;
            CacheMap.WriteResult2Shp(OutFilePath, pMap.SpatialReference);

            #region 邻近图构建
            DelaunayTin dt = new DelaunayTin(map.TriNodeList); ///Dt中节点的ID和Map.PointList中节点的ID是一样的
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);
            dt.CreateRNG();
            ProxiGraph pg = new ProxiGraph(dt.RNGNodeList, dt.RNGEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的
            //ProxiGraph pg = new ProxiGraph(dt.TriNodeList, dt.TriEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的
            #endregion

            #region 生成过程
            //CTPS.CTPSnake_PgReBuilt(pg, FinalPoint, 1, 1000, 10000, pg.NodeList.Count, 0, 0, 0.01, OutFilePath, pMap);
            CTPS.CTPSnake_PgReBuiltMapIn_BdMain(pg, FinalPoint, map, 1, 1000, 10000, 300, 0, 0, 0.01, OutFilePath, pMap, 50, false, 0.5);
            //if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "邻近图", pMap.SpatialReference); }//输出邻近图
            #endregion

            #region 输出
            if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG", pMap.SpatialReference); }
            #endregion
        }

        /// <summary>
        /// 确定-边界关系维护-邻近更新的Snake（DT+MST）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button11_Click(object sender, EventArgs e)
        {
            CTPSupport CTPS = new CTPSupport();

            #region 数据读取
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BoundaryLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer PointDistanceLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(PointDistanceLayer);
            }
            #endregion

            #region 数据读取(终点计算)
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrs();

            List<PointObject> CacheFinalPoint = CTPS.FinalLocation(map.PointList);//获得每个点的最终位置(LocationPoint的ID和map.PointList中点ID是一样的)[两个函数是一样的，只是一个生成的PointObject；一个生成的是ProxiNode]
            List<ProxiNode> FinalPoint = CTPS.FinalLocation_2(map.PointList);//获得每个点的最终位置(LocationPoint的ID和map.PointList中点ID是一样的)

            //FinalPoint.RemoveRange(20, FinalPoint.Count - 21);
            #endregion

            SMap CacheMap = new SMap();
            CacheMap.PointList = CacheFinalPoint;
            CacheMap.WriteResult2Shp(OutFilePath, pMap.SpatialReference);

            #region 邻近图构建
            DelaunayTin dt = new DelaunayTin(map.TriNodeList); ///Dt中节点的ID和Map.PointList中节点的ID是一样的
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);
            dt.CreateMST();
            ProxiGraph pg = new ProxiGraph(dt.MSTNodeList, dt.MSTEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的

            //ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            //cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);
            //ProxiGraph pg = new ProxiGraph(cdt.TriNodeList, cdt.TriEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的
            #endregion

            #region 生成过程
            //CTPS.CTPSnake_PgReBuilt(pg, FinalPoint, 1, 1000, 10000, pg.NodeList.Count, 0, 0, 0.01, OutFilePath, pMap);
            CTPS.CTPSnake_PgReBuiltMapIn_BdMain(pg, FinalPoint, map, 1, 1000, 10000, 110, 0, 0, 0.01, OutFilePath, pMap, 50, false, 0.5);
            //if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "邻近图", pMap.SpatialReference); }//输出邻近图
            #endregion

            #region 输出
            if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pG", pMap.SpatialReference); }
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

            #region 数据读取
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BoundaryLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BoundaryLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer PointDistanceLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(PointDistanceLayer);
            }
            #endregion

            #region 数据读取(终点计算)
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrs();

            List<PointObject> CacheFinalPoint = CTPS.FinalLocation(map.PointList);//获得每个点的最终位置(LocationPoint的ID和map.PointList中点ID是一样的)[两个函数是一样的，只是一个生成的PointObject；一个生成的是ProxiNode]
            List<TriNode> FinalPoint = CTPS.FinalLocation_5(map.PointList);//获得每个点的最终位置(LocationPoint的ID和map.PointList中点ID是一样的)
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

            List<Tuple<double,double>> XYs=CTPS.LeastSquareAdj(BoundNodes, OriginalNodes, FinalPoint, 50, 1);

            for (int i = 0; i < map.PolygonList[0].PointList.Count;i++ )
            {
                map.PolygonList[0].PointList[i].X = XYs[i].Item1;
                map.PolygonList[0].PointList[i].Y = XYs[i].Item2;
            }
            #endregion

            map.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
        }
    }
}
