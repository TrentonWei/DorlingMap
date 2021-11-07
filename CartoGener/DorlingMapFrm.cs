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

namespace CartoGener
{
    public partial class DorlingMapFrm : Form
    {
        public DorlingMapFrm(AxESRI.ArcGIS.Controls.AxMapControl axMapControl)
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
        #endregion

        /// <summary>
        /// initialize
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DorlingMapFrm_Load(object sender, EventArgs e)
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
        /// Set the OutPutFile
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
        /// generate the initial circle of Dorling Map
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            #region OutPutCheck
            if (OutFilePath == null)
            {
                MessageBox.Show("Please give the OutPut path");
                return;
            }
            #endregion

            #region Get the initial Circles
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiG(pFeatureClass);

            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, pg, "POPULATION", 1.0, 50, 1, 2, 0.01,0.05);
            SMap Map = new SMap();
            List<PolygonObject> PoList=DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            #endregion

            Map.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
        }

        /// <summary>
        /// Proxigraph generation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {

            #region OutPutCheck
            if (OutFilePath == null)
            {
                MessageBox.Show("Please give the OutPut path");
                return;
            }
            #endregion

            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiG(pFeatureClass);
            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, pg, "POPULATION", 1, 50, 1, 1, 0.01,0.1);
            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            //pg.DeleteLongerEdges(pg.EdgeList, Map.PolygonList, 25);//删除长的边
            //pg.CreateMST(pg.NodeList, pg.EdgeList, PoList);
            pg.CreateRNG(pg.NodeList, pg.EdgeList, PoList);
            pg.PgRefined(Map.PolygonList);
            if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "邻近图", pMap.SpatialReference); }
        }

        /// <summary>
        /// BeamsDisplace
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            #region OutPutCheck
            if (OutFilePath == null)
            {
                MessageBox.Show("Please give the OutPut path");
                return;
            }
            #endregion

            #region Get the initial Circles and Pg
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            DM.pMapControl = pMapControl;
            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiG(pFeatureClass);

            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, pg, "POPULATION", 1.0, 50, 1, 1, 0.1, 0.05);
            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            pg.CreateRNG(pg.NodeList, pg.EdgeList, PoList);
            pg.PgRefined(Map.PolygonList);
            //pg.DeleteLongerEdges(pg.EdgeList, Map.PolygonList, 25);//删除长的边
            List<ProxiGraph> PgList = pg.GetGroupPg();
            #endregion

            #region 移位
            SMap OutMap = new SMap();
            for (int i = 0; i < PgList.Count; i++)
            {
                if (PgList[i].NodeList.Count > 1)
                {
                    SMap newMap = DMS.regulation(PgList[i], Map);
                    DM.DorlingBeams(PgList[i], newMap, 1, 1000, 1, 1, 200, 0, 0.000001, 20, 1, true);
                    OutMap.PolygonList.AddRange(newMap.PolygonList);
                }

                else
                {
                    SMap newMap = DMS.regulation(PgList[i], Map);
                    OutMap.PolygonList.AddRange(newMap.PolygonList);
                }
            }
            OutMap.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
            #endregion
        }

        /// <summary>
        /// 不分组移位
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            #region OutPutCheck
            if (OutFilePath == null)
            {
                MessageBox.Show("Please give the OutPut path");
                return;
            }
            #endregion

            #region Get the initial Circles and Pg
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            DM.pMapControl = pMapControl;
            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiG(pFeatureClass);

            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, pg, "POPULATION", 0.1, 10, 1, 0, 0.4,0.1);
            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            pg.PgRefined(PoList);

            SMap newMap = DMS.regulation(pg, Map);
            DM.DorlingBeams(pg, newMap, 1, 1000, 1, 1, 100, 0, 0.000001, 20, 1, true);

            Map.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
            #endregion
        }
    }
}
