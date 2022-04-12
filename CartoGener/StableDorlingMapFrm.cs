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
    /// DorlingMap production for Time series Data
    /// </summary>
    public partial class StableDorlingMapFrm : Form
    {
        public StableDorlingMapFrm(AxESRI.ArcGIS.Controls.AxMapControl axMapControl)
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
        DMClass DM = new DMClass();
        DMSupport DMS = new DMSupport();
        SDMSupport SDMS = new SDMSupport();
        #endregion

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StableDorlingMapFrm_Load(object sender, EventArgs e)
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
                    this.comboBox1.Items.Add(strLayerName);
                }
            }
            #endregion

            #region 默认显示第一个
            if (this.comboBox1.Items.Count > 0)
            {
                this.comboBox1.SelectedIndex = 0;
            }
            #endregion
        }

        /// <summary>
        /// 输出路径
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
            this.comboBox2.Text = OutFilePath;
        }

        /// <summary>
        /// GetTimeSeriesData
        /// </summary>
        /// TimeSeriesLabel=Time_
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            List<Dictionary<IPolygon, double>> TimeSeriesData = DMS.GetTimeSeriesData_2(pMap, this.comboBox1.Text);
        }

        /// <summary>
        /// Hierarchy construction 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            List<Dictionary<IPolygon, double>> TimeSeriesData = DMS.GetTimeSeriesData_2(pMap, this.comboBox1.Text);//GetTimeSeriesData
            Dictionary<int, Dictionary<int, List<int>>> Hierarchy = SDMS.GetHierarchy_2(TimeSeriesData, 0);//获得分组
        }

        /// <summary>
        /// PgConstruction for TimeSeriesData
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            #region 构建基础邻近关系
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiGByDT(pFeatureClass);//依据三角网构建邻近图
            #endregion

            npg.CreateRNG(npg.NodeList, npg.EdgeList);//考虑要素之间的重心距离构建RNG
            npg.LabelAdjEdges(npg.EdgeList, pFeatureClass,0);//表示邻近的边

            #region 输出
            if (OutFilePath != null) { npg.WriteProxiGraph2Shp(OutFilePath, "邻近图", pMap.SpatialReference); }
            #endregion
        }

        /// <summary>
        /// Circle generalization
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            List<Dictionary<IPolygon, double>> TimeSeriesData = DMS.GetTimeSeriesData_2(pMap, this.comboBox1.Text);//GetTimeSeriesData
            List<List<PolygonObject>> CircleLists = DM.GetInitialPolygonObjectForStableDorling(TimeSeriesData, 0.5, 1, 2, 10);//Circle generalization
            for (int i = 0; i < CircleLists.Count; i++)
            {
                SMap Map = new SMap();
                Map.PolygonList = CircleLists[i];
                Map.WriteResult2Shp(OutFilePath, i.ToString(), pMap.SpatialReference);
            }
        }

        /// <summary>
        /// StableDorlingMap
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            #region 获得数据并分组
            List<Dictionary<IPolygon, double>> TimeSeriesData = DMS.GetTimeSeriesData_2(pMap, this.comboBox1.Text);//GetTimeSeriesData
            Dictionary<int, Dictionary<int, List<int>>> Hierarchy = SDMS.GetHierarchy_2(TimeSeriesData, 0);//获得分组
            #endregion

            #region 获得圆形
            List<List<PolygonObject>> CircleLists = DM.GetInitialPolygonObjectForStableDorling(TimeSeriesData, 0.5, 1, 2, 10);//Circle generalization
            List<SMap> MapList = new List<SMap>();
            for (int i = 0; i < CircleLists.Count; i++)
            {
                SMap Map = new SMap();
                Map.PolygonList = CircleLists[i];
                MapList.Add(Map);
            }
            #endregion

            #region 构建基础邻近关系
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiGByDT(pFeatureClass);//依据三角网构建邻近图
            npg.CreateRNG(npg.NodeList, npg.EdgeList);//考虑要素之间的重心距离构建RNG
            npg.LabelAdjEdges(npg.EdgeList, pFeatureClass, 0);//表示邻近的边
            #endregion

            #region 层次移位操作
            List<int> LevelLabel = Hierarchy.Keys.ToList();
            for (int i = LevelLabel.Count - 1; i >= 0; i--)
            {
                #region 获取每一层的Maps
                Dictionary<int, List<int>> LevelMap = Hierarchy[i];
                List<List<SMap>> MapLists = new List<List<SMap>>();
                foreach (KeyValuePair<int, List<int>> kv in LevelMap)
                {
                    List<SMap> CacheMapList = new List<SMap>();
                    for (int j = 0; j < kv.Value.Count; j++)
                    {
                        CacheMapList.Add(MapList[kv.Value[j]]);
                    }
                    MapLists.Add(CacheMapList);
                }
                #endregion

                #region
                for (int j = 0; j < MapLists.Count; j++)
                {
                    int Testk = Convert.ToInt16(2 * pFeatureClass.FeatureCount(null) / TimeSeriesData.Count);
                    DM.StableDorlingBeams(npg, MapLists[j], 1, 10, 1, 1, Convert.ToInt16(2 * pFeatureClass.FeatureCount(null)/TimeSeriesData.Count), 0, 0.05, 20, 3, true, 0.2);
                }
                #endregion
            }
            #endregion

            #region 输出
            for (int i = 0; i < MapList.Count; i++)
            {
                MapList[i].WriteResult2Shp(OutFilePath,i.ToString(), pMap.SpatialReference);
            }
            if (OutFilePath != null) { npg.WriteProxiGraph2Shp(OutFilePath, "邻近图", pMap.SpatialReference); }
            #endregion
        }
    }
}
