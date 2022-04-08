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
            List<Dictionary<IPolygon, double>> TimeSeriesData = DMS.GetTimeSeriesData(pMap, this.comboBox1.Text);
        }

        /// <summary>
        /// Hierarchy construction 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            List<Dictionary<IPolygon, double>> TimeSeriesData = DMS.GetTimeSeriesData(pMap, this.comboBox1.Text);//GetTimeSeriesData
            Dictionary<int, Dictionary<int, List<int>>> Hierarchy = SDMS.GetHierarchy(TimeSeriesData, 0);//获得分组
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
        }

        /// <summary>
        /// Circle generalization
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            List<Dictionary<IPolygon, double>> TimeSeriesData = DMS.GetTimeSeriesData(pMap, this.comboBox1.Text);//GetTimeSeriesData
            List<List<PolygonObject>> CircleLists = DM.GetInitialPolygonObjectForStableDorling(TimeSeriesData, 10);//Circle generalization
        }

        /// <summary>
        /// StableDorlingMap
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            #region 获得数据并分组
            List<Dictionary<IPolygon, double>> TimeSeriesData = DMS.GetTimeSeriesData(pMap, this.comboBox1.Text);//GetTimeSeriesData
            Dictionary<int, Dictionary<int, List<int>>> Hierarchy = SDMS.GetHierarchy(TimeSeriesData, 0);//获得分组
            #endregion

            #region 获得圆形
            List<List<PolygonObject>> CircleLists = DM.GetInitialPolygonObjectForStableDorling(TimeSeriesData, 10);//Circle generalization
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
             
            #endregion
        }
    }
}
