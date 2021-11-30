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
            pg.CreateProxiG(pFeatureClass, 0);

            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, pg, "POPULATION", 1, 50, 1, 1, 0.01, 0.1);
            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
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

            //ProxiGraph pg = new ProxiGraph();
            //pg.CreateProxiGByDT(pFeatureClass);

            //ProxiGraph npg = new ProxiGraph();
            //npg.CreateProxiG(pFeatureClass);
            //List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", 1, 50, 1, 1, 0.01,0.1);

            //SMap Map = new SMap();
            //List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            //Map.PolygonList = PoList;
            ////pg.DeleteLongerEdges(pg.EdgeList, Map.PolygonList, 25);//删除长的边
            ////pg.CreateMST(pg.NodeList, pg.EdgeList, PoList);
            //pg.CreateRNG(pg.NodeList, pg.EdgeList, PoList);
            //pg.PgRefined(Map.PolygonList);
            //pg.DeleteLongerEdges(pg.EdgeList, Map.PolygonList, 25);//删除长的边
            //if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "邻近图", pMap.SpatialReference); }

            #region 构建邻近关系
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(pFeatureClass, 0);
            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", 1, 50, 1, 1, 0.01, 0.1);
            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            npg.DeleteCrossEdge(npg.EdgeList, Map.PolygonList);
            //npg.CreateRNG(npg.NodeList, npg.EdgeList, PoList);
            npg.DeleteLongerEdges(npg.EdgeList, Map.PolygonList, 5);//删除长的边
            #endregion

            #region 构建MST
            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGByDT(pFeatureClass);
            pg.CreateMST(pg.NodeList, pg.EdgeList, PoList);
            //pg.DeleteLongerEdges(pg.EdgeList, Map.PolygonList, 25);//删除长的边
            #endregion

            #region Pg的refinement
            npg.PgRefined(pg.EdgeList);//MSTrefine
            //npg.PgRefined(Map.PolygonList);//重叠边refine

            //int TestCount = 0;
            //for (int i = 0; i < npg.EdgeList.Count; i++)
            //{
            //    if (npg.EdgeList[i].StepOverLap)
            //    {
            //        TestCount++;
            //    }
            //}
            //npg.DeleteLongerEdges(npg.EdgeList, Map.PolygonList, 25);//删除长边
            //npg.DeleteCrossEdge(npg.EdgeList, Map.PolygonList);//删除穿过边  
            #endregion

            //List<ProxiGraph> PgList = npg.GetGroupPg();
            //for (int i = 0; i < PgList.Count; i++)
            //{
            //    if (PgList[i].NodeList.Count > 1)
            //    {
            //        if (OutFilePath != null) { PgList[i].WriteProxiGraph2Shp(OutFilePath, "邻近图" + i.ToString(), pMap.SpatialReference); }
            //    }
            //}
            if (OutFilePath != null) {npg.WriteProxiGraph2Shp(OutFilePath, "邻近图", pMap.SpatialReference); }

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

            #region 构建邻近关系
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(pFeatureClass, 0);
            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", 1, 50, 1, 1, 0.01, 0.1);
            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            //npg.CreateRNG(npg.NodeList, npg.EdgeList, PoList);
            #endregion

            #region 构建MST+npg refine
            npg.DeleteLongerEdges(npg.EdgeList, Map.PolygonList, 4);//删除长边
            npg.DeleteCrossEdge(npg.EdgeList, Map.PolygonList);//删除穿过边                           
            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGByDT(pFeatureClass);
            pg.CreateMST(pg.NodeList, pg.EdgeList, PoList);
            npg.PgRefined(pg.EdgeList);//MSTrefine 
            npg.PgRefined(Map.PolygonList);//重叠边refine  

            npg.DeleteLongerEdges(npg.EdgeList, Map.PolygonList, 4);
            #endregion

            List<ProxiGraph> PgList = npg.GetGroupPg();
            #region 移位
            SMap OutMap = new SMap();
            for (int i = 0; i < PgList.Count; i++)
            {
                if (PgList[i].NodeList.Count > 1)
                {
                    SMap newMap = DMS.regulation(PgList[i], Map);
                    DM.DorlingBeams(PgList[i], newMap, 1, 10, 1, 1, 200, 0, 0.05, 30, 1, true, 0.2);
                    if (OutFilePath != null) { PgList[i].WriteProxiGraph2Shp(OutFilePath, "邻近图" + i.ToString()+1, pMap.SpatialReference); }
                    OutMap.WriteResult2Shp(OutFilePath, "1",pMap.SpatialReference);

                    DM.DorlingBeams(PgList[i], newMap, 1, 10, 1, 1, 300, 0, 0.05, 30, 0, true, 0.2);                    
                    if (OutFilePath != null) { PgList[i].WriteProxiGraph2Shp(OutFilePath, "邻近图" + i.ToString() + 2, pMap.SpatialReference); }
                    OutMap.WriteResult2Shp(OutFilePath, "2", pMap.SpatialReference);
                    
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

            #region 构建邻近关系
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(pFeatureClass, 0.2);
            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", 1, 50, 1, 1, 0.01, 0.1);
            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            //npg.CreateRNG(npg.NodeList, npg.EdgeList, PoList);
            #endregion

            #region 创建RNG并移位
            double E = 10;
            for (int i = 0; i < 100; i++)
            {
                ProxiGraph pg = new ProxiGraph();
                pg.CreateProxiGByDT(Map.PolygonList);
                pg.CreateRNG(pg.NodeList, pg.EdgeList, PoList);
                AlgBeams algBeams = new AlgBeams(pg, Map, 10, 1, 1);
                ProxiGraph CopyG = Clone((object)pg) as ProxiGraph;
                algBeams.OriginalGraph = CopyG;
                algBeams.Scale = 1;
                algBeams.AlgType = 0;
                Console.WriteLine(i.ToString());//标识
                algBeams.DoDisplacePgDorling(Map, 0.00001, 200, 1, true,0.2);// 调用Beams算法 

                E = algBeams.E;
                if (algBeams.isContinue == false)
                {
                    break;
                }
            }
            #endregion

            Map.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
        }

        /// <summary>
        /// 邻近图每次都重构
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            #region OutPutCheck
            if (OutFilePath == null)
            {
                MessageBox.Show("Please give the OutPut path");
                return;
            }
            #endregion

            #region 构建邻近关系
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(pFeatureClass, 0.2);
            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", 1, 50, 1, 1, 0.01, 0.1);
            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            //npg.CreateRNG(npg.NodeList, npg.EdgeList, PoList);
            #endregion

            #region 构建MST
            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGByDT(pFeatureClass);
            pg.CreateMST(pg.NodeList, pg.EdgeList, PoList);
            pg.DeleteLongerEdges(pg.EdgeList, Map.PolygonList, 25);//删除长的边
            #endregion

            #region Pg的refinement
            pg.PgRefined(Map.PolygonList);//依据重叠关系refined
            pg.PgRefined(npg.EdgeList);//依据邻接关系refined            
            pg.DeleteCrossEdge(pg.EdgeList, Map.PolygonList);//删除穿过的边
            #endregion

            #region 移位
            SMap OutMap = new SMap();
            List<ProxiGraph> PgList = pg.GetGroupPg();
            for (int i = 0; i < PgList.Count; i++)
            {
                if (PgList[i].NodeList.Count > 1)
                {
                    SMap newMap = DMS.regulation(PgList[i], Map);
                    DM.DorlingBeams(PgList[i], newMap, 1, 1000, 1, 1, 100, 0, 0.0001, 1000, 1, true,0.2);
                    OutMap.PolygonList.AddRange(newMap.PolygonList);
                    if (OutFilePath != null) { PgList[i].WriteProxiGraph2Shp(OutFilePath, "邻近图" + i.ToString(), pMap.SpatialReference); }
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
        /// SeparateDis
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button7_Click(object sender, EventArgs e)
        {
            #region OutPutCheck
            if (OutFilePath == null)
            {
                MessageBox.Show("Please give the OutPut path");
                return;
            }
            #endregion

            #region 构建邻近关系
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(pFeatureClass, 0);
            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", 1, 50, 1, 1, 0.01, 0.1);
            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            //npg.CreateRNG(npg.NodeList, npg.EdgeList, PoList);
            #endregion

            #region 构建MST+npg refine
            npg.DeleteLongerEdges(npg.EdgeList, Map.PolygonList, 20);//删除长边
            npg.DeleteCrossEdge(npg.EdgeList, Map.PolygonList);//删除穿过边                           
            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGByDT(pFeatureClass);
            pg.CreateMST(pg.NodeList, pg.EdgeList, PoList);
            npg.PgRefined(pg.EdgeList);//MSTrefine 
            npg.PgRefined(Map.PolygonList);//重叠边refine   
            #endregion

            #region 移位
            DM.DorlingBeams(npg, Map, 1, 1000, 1, 1, 50, 0, 0.05, 1000, 1, true,0.2);
            Map.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
            #endregion

            //SMap kOutMap = new SMap();
            //for (int i = 0; i < Circles.Count; i++)
            //{
            //    kOutMap.PolygonList.AddRange(Circles[i]);
            //}

            //kOutMap.WriteResult2Shp(OutFilePath, pMap.SpatialReference);

            //if (OutFilePath != null) { NewPg.WriteProxiGraph2Shp(OutFilePath, "邻近图", pMap.SpatialReference); }
        }

        /// <summary>
        /// Circle可以聚类
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button8_Click(object sender, EventArgs e)
        {
            #region OutPutCheck
            if (OutFilePath == null)
            {
                MessageBox.Show("Please give the OutPut path");
                return;
            }
            #endregion

            #region 构建邻近关系
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(pFeatureClass, 0);
            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", 1, 50, 1, 1, 0.01, 0.1);
            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            //npg.CreateRNG(npg.NodeList, npg.EdgeList, PoList);
            #endregion

            #region refine和重构MST
            npg.DeleteLongerEdges(npg.EdgeList, Map.PolygonList, 10);//删除长边
            npg.DeleteCrossEdge(npg.EdgeList, Map.PolygonList);//删除穿过边   

            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGByDT(pFeatureClass);
            pg.CreateMST(pg.NodeList, pg.EdgeList, PoList);

            npg.PgRefined(pg.EdgeList);//MSTrefine 
            npg.PgRefined(Map.PolygonList);//重叠边refine
            #endregion

            DM.pMapControl = pMapControl;
            DM.DorlingBeams(npg, Map, 1, 1000, 1, 1, 50, 0, 0.05, 1000, 0, true,0.2);

            for (int i = 1; i < 8; i++)
            {
                #region 构建MST+npg refine
                List<List<PolygonObject>> Circles = DM.CircleGroup(Map.PolygonList, 0.1, 0.05);
                ProxiGraph NewPg = new ProxiGraph();
                NewPg.PgReConstruction(Circles, npg);
                #endregion

                if (OutFilePath != null) { NewPg.WriteProxiGraph2Shp(OutFilePath, "邻近图" + i.ToString(), pMap.SpatialReference); }
                Map.WriteResult2Shp(OutFilePath, i.ToString(), pMap.SpatialReference);

                DM.GroupDorlingBeams(NewPg, Map, 1, 1000, 1, 1, 50, 0, 0.05, i * 5, 1, true, i);
            }

            //SMap kOutMap = new SMap();
            //for (int i = 0; i < Circles.Count; i++)
            //{
            //    kOutMap.PolygonList.AddRange(Circles[i]);
            //}

            Map.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
        }

        /// <summary>
        /// 渐进式聚合
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button9_Click(object sender, EventArgs e)
        {
            #region OutPutCheck
            if (OutFilePath == null)
            {
                MessageBox.Show("Please give the OutPut path");
                return;
            }
            #endregion

            #region 构建邻近关系
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(pFeatureClass, 0);
            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", 1, 50, 1, 1, 0.01, 0.1);
            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            //npg.CreateRNG(npg.NodeList, npg.EdgeList, PoList);
            #endregion

            #region refine和重构MST
            npg.DeleteLongerEdges(npg.EdgeList, Map.PolygonList, 10);//删除长边
            npg.DeleteCrossEdge(npg.EdgeList, Map.PolygonList);//删除穿过边   

            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGByDT(pFeatureClass);
            pg.CreateMST(pg.NodeList, pg.EdgeList, PoList);

            npg.PgRefined(pg.EdgeList);//MSTrefine 
            npg.PgRefined(Map.PolygonList);//重叠边refine
            #endregion

            for (int i = 0; i < 10; i++)
            {
                double E = 10;
                for (int j = 0; j < 50; j++)
                {
                    List<List<PolygonObject>> Circles = DM.CircleGroup(Map.PolygonList, 0.1, 0);
                    ProxiGraph NewPg = new ProxiGraph();
                    NewPg.PgReConstruction(Circles, npg);

                    AlgBeams algBeams = new AlgBeams(NewPg, Map, E, 1, 1);
                    ProxiGraph CopyG = Clone((object)NewPg) as ProxiGraph;
                    algBeams.OriginalGraph = CopyG;
                    algBeams.Scale = 1;
                    algBeams.AlgType = 0;

                    algBeams.GroupDoDisplacePgDorling(Map, 0.05, 1000, 1, true);// 调用Beams算法
                    E = algBeams.E;

                    //if (OutFilePath != null) { NewPg.WriteProxiGraph2Shp(OutFilePath, i.ToString() + j.ToString() + "邻近图", pMap.SpatialReference); }
                    //Map.WriteResult2Shp(OutFilePath, i.ToString() + j.ToString(), pMap.SpatialReference);
                    if (algBeams.isContinue == false)
                    {
                        break;
                    }

                    Console.WriteLine(i.ToString() + j.ToString());
                }
            }

            Map.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
        }

        /// <summary>
        /// 不考虑Group
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button10_Click(object sender, EventArgs e)
        {
            #region OutPutCheck
            if (OutFilePath == null)
            {
                MessageBox.Show("Please give the OutPut path");
                return;
            }
            #endregion

            #region 构建邻近关系
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(pFeatureClass, 0);
            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", 1, 50, 1, 1, 0.01, 0.1);
            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            //npg.CreateRNG(npg.NodeList, npg.EdgeList, PoList);
            #endregion

            #region refine和重构MST
            npg.DeleteLongerEdges(npg.EdgeList, Map.PolygonList, 20);//删除长边
            npg.DeleteCrossEdge(npg.EdgeList, Map.PolygonList);//删除穿过边   

            //ProxiGraph pg = new ProxiGraph();
            //pg.CreateProxiGByDT(pFeatureClass);
            //pg.CreateMST(pg.NodeList, pg.EdgeList, PoList);

            //npg.PgRefined(pg.EdgeList);//MSTrefine 
            //npg.PgRefined(Map.PolygonList);//重叠边refine
            #endregion

            List<ProxiGraph> PgList = npg.GetGroupPg();

            #region 移位
            SMap OutMap = new SMap();
            for (int i = 0; i < PgList.Count; i++)
            {
                if (PgList[i].NodeList.Count > 1)
                {
                    SMap newMap = DMS.regulation(PgList[i], Map);
                    DM.DorlingBeams(PgList[i], newMap, 1, 1000, 1, 1, 40, 0, 0.0001, 1000, 1, true,0.2);
                    OutMap.PolygonList.AddRange(newMap.PolygonList);
                    if (OutFilePath != null) { PgList[i].WriteProxiGraph2Shp(OutFilePath, "邻近图" + i.ToString(), pMap.SpatialReference); }
                }

                else
                {
                    SMap newMap = DMS.regulation(PgList[i], Map);
                    OutMap.PolygonList.AddRange(newMap.PolygonList);
                }
            }
            #endregion

            OutMap.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
        }
    }
}
