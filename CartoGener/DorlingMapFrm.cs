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
    //DorlingMap production 
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
                    this.comboBox3.Items.Add(strLayerName);
                    this.comboBox4.Items.Add(strLayerName);
                }
            }
            #endregion

            #region 默认显示第一个
            if (this.comboBox1.Items.Count > 0)
            {
                this.comboBox1.SelectedIndex = 0;
            }
            if (this.comboBox3.Items.Count > 0)
            {
                this.comboBox3.SelectedIndex = 0;
            }
            if (this.comboBox4.Items.Count > 0)
            {
                this.comboBox4.SelectedIndex = 0;
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

            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, pg, "AREA", 1, 30, 1, 2, 0.01, 0.1);
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
            //List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", 1, 50, 1, 1, 0.01, 0.1);
            //SMap Map = new SMap();
            //List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            //Map.PolygonList = PoList;
            //npg.DeleteCrossEdge(npg.EdgeList, Map.PolygonList);
            ////npg.CreateRNG(npg.NodeList, npg.EdgeList, PoList);
            //npg.DeleteLongerEdges(npg.EdgeList, Map.PolygonList, 5);//删除长的边
            #endregion

            #region 构建MST
            //ProxiGraph pg = new ProxiGraph();
            //pg.CreateProxiGByDT(pFeatureClass);
            //pg.CreateMST(pg.NodeList, pg.EdgeList, PoList);

            //pg.DeleteLongerEdges(pg.EdgeList, Map.PolygonList, 25);//删除长的边
            #endregion

            #region Pg的refinement
            //npg.PgRefined(npg.EdgeList);//MSTrefine
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
        /// BeamsDisplace   考虑了部分吸引力可删除
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
            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", "GMI_CNTRY", 1, 50, 1, 2, 0.01, 0.1);
            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            //npg.CreateRNG(npg.NodeList, npg.EdgeList, PoList);
            #endregion

            #region 构建MST+npg refine
            npg.DeleteLongerEdges(npg.EdgeList, Map.PolygonList, 4);//删除长边
            //npg.DeleteLongerEdges(npg.EdgeList, Map.PolygonList, 20);//删除长边
            npg.DeleteCrossEdge(npg.EdgeList, Map.PolygonList);//删除穿过边                           
            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGByDT(pFeatureClass);
            pg.CreateMST(pg.NodeList, pg.EdgeList, PoList);
            npg.PgRefined(pg.EdgeList);//MSTrefine 
            npg.PgRefined(Map.PolygonList);//重叠边refine  

            npg.DeleteLongerEdges(npg.EdgeList, Map.PolygonList, 4);
            //npg.DeleteLongerEdges(npg.EdgeList, Map.PolygonList, 20);//删除长边
            #endregion

            List<ProxiGraph> PgList = npg.GetGroupPg();

            #region 移位
            SMap OutMap = new SMap();
            for (int i = 0; i < PgList.Count; i++)
            {
                if (PgList[i].NodeList.Count > 1)
                {
                    SMap newMap = DMS.regulation(PgList[i], Map);
                    int PNum = Map.PolygonList.Count;
                    if (Map.PolygonList.Count < 10)
                    {
                        PNum = 10;
                    }

                    DM.DorlingBeams(PgList[i], newMap, 1, 10, 1, 1, 2 * PNum, 0, 0.05, 30, 1, true, 0.2);
                    if (OutFilePath != null) { PgList[i].WriteProxiGraph2Shp(OutFilePath, "邻近图" + i.ToString()+1, pMap.SpatialReference); }
                    OutMap.WriteResult2Shp(OutFilePath, "1",pMap.SpatialReference);

                    DM.DorlingBeams(PgList[i], newMap, 1, 10, 1, 1, 2 * PNum, 0, 0.05, 30, 0, true, 0.2);                  
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
            npg.CreateProxiG(pFeatureClass, 0);
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
        /// 渐进式聚合（MST）
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
        /// 不考虑Group 邻近图+MST
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

        /// <summary>
        /// DT+其它
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button11_Click(object sender, EventArgs e)
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
            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGByDTConsiTop(pFeatureClass);//依据DT构建的邻近图(考虑拓扑关系)

            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(pFeatureClass, 0.2);
            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", 1, 50, 1, 1, 0.01, 0.1);
            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            #endregion

            #region 删除长边和Cross的边
            pg.DeleteLongerEdges(pg.EdgeList, Map.PolygonList, 20);//删除长边
            pg.DeleteCrossEdge(pg.EdgeList, Map.PolygonList);//删除穿过边  
            #endregion

            List<ProxiGraph> PgList = pg.GetGroupPg();
            #region 移位
            SMap OutMap = new SMap();
            for (int i = 0; i < PgList.Count; i++)
            {
                if (PgList[i].NodeList.Count > 1)
                {
                    SMap newMap = DMS.regulation(PgList[i], Map);
                    DM.DorlingBeams(PgList[i], newMap, 1, 1000, 1, 1, 40, 0, 0.0001, 1000, 2, true, 0.2);
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

        /// <summary>
        /// Dorling Approach
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button12_Click(object sender, EventArgs e)
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
            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, "POPULATION", "STATE_ABBR", 0.5, 1, 2);//成比例 美国
            //List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", "GMI_CNTRY", 1, 50, 1, 1, 0.01, 0.1);//不成比例美洲
            //List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, "POPULATION", "GMI_CNTRY", 1, 1, 2);//成比例 美洲
            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            //npg.CreateRNG(npg.NodeList, npg.EdgeList, PoList);
            #endregion

            #region 构建MST+npg refine
            //npg.DeleteLongerEdges(npg.EdgeList, Map.PolygonList, 4);//删除长边
            ////npg.DeleteLongerEdges(npg.EdgeList, Map.PolygonList, 20);//删除长边
            //npg.DeleteCrossEdge(npg.EdgeList, Map.PolygonList);//删除穿过边                           
            ProxiGraph pg = new ProxiGraph();

            #region Create ProxiNodes
            for (int i = 0; i < Map.PolygonList.Count; i++)
            {
                ProxiNode CacheNode = new ProxiNode(Map.PolygonList[i].CalProxiNode().X, Map.PolygonList[i].CalProxiNode().Y, i, i);
                pg.NodeList.Add(CacheNode);
            }
            #endregion

            //pg.CreateProxiGByDT(pFeatureClass);
            //pg.CreateMST(pg.NodeList, pg.EdgeList, PoList);
            //npg.PgRefined(pg.EdgeList);//MSTrefine 
            pg.PgRefined(Map.PolygonList);//重叠边refine  

            //npg.DeleteLongerEdges(npg.EdgeList, Map.PolygonList, 4);
            //npg.DeleteLongerEdges(npg.EdgeList, Map.PolygonList, 20);//删除长边
            #endregion

            List<ProxiGraph> PgList = pg.GetGroupPg();
            #region 移位
            SMap OutMap = new SMap();
            for (int i = 0; i < PgList.Count; i++)
            {
                if (PgList[i].NodeList.Count > 1)
                {
                    SMap newMap = DMS.regulation(PgList[i], Map);
                    int PNum = Map.PolygonList.Count;
                    if (Map.PolygonList.Count < 10)
                    {
                        PNum = 10;
                    }

                    DM.DorlingBeams(PgList[i], newMap, 1, 10, 1, 1, 2 * PNum, 0, 0.05, 20, 1, true, 0.2);
                    if (OutFilePath != null) { PgList[i].WriteProxiGraph2Shp(OutFilePath, "邻近图" + i.ToString() + 1, pMap.SpatialReference); }
                    //OutMap.WriteResult2Shp(OutFilePath, "1", pMap.SpatialReference);

                    if (PgList[i].EdgeList.Count > 0)
                    {
                        DM.DorlingBeams(PgList[i], newMap, 1, 10, 1, 1, 2 * PNum, 0, 0.05, 20, 0, true, 0.2);
                        if (OutFilePath != null) { PgList[i].WriteProxiGraph2Shp(OutFilePath, "邻近图" + i.ToString() + 2, pMap.SpatialReference); }
                        //OutMap.WriteResult2Shp(OutFilePath, "2", pMap.SpatialReference);
                    }

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
        /// MSTBeams
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button11_Click_1(object sender, EventArgs e)
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
            //List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", "STATE_ABBR", 1, 50, 1, 2, 0.01, 0.1);//不成比例 美国
            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, "POPULATION", "STATE_ABBR", 0.5, 1, 2);//成比例，美国
            //List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", "GMI_CNTRY", 1, 50, 1, 2, 0.01, 0.1);//不成比例 美洲
            //List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, "POPULATION", "GMI_CNTRY", 1, 1, 2);//成比例，美洲
            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            //npg.CreateRNG(npg.NodeList, npg.EdgeList, PoList);
            #endregion

            #region 移位
            int Step = 0;

            while (Step < 300)
            {
                #region 构建MST+npg refine
                ProxiGraph pg = new ProxiGraph();
                pg.CreateProxiGByDT(Map.PolygonList);
                pg.CreateMST(pg.NodeList, pg.EdgeList, PoList);
                pg.PgRefined(Map.PolygonList);//依据重叠关系refined          
                #endregion

                DM.DorlingBeams(pg, Map, 1, 10, 1, 1, 1, 0, 0.05, 1000, 1, true, 0.2);
                Step++;
                Console.WriteLine(Step);
                if (!DM.continueLable)
                {
                    break;
                }
            }


            while (Step < 400)
            {
                #region 构建MST+npg refine
                ProxiGraph pg = new ProxiGraph();
                pg.CreateProxiGByDT(Map.PolygonList);
                pg.CreateMST(pg.NodeList, pg.EdgeList, PoList);
                pg.PgRefined(Map.PolygonList);//依据重叠关系refined          
                #endregion
                DM.DorlingBeams(pg, Map, 1, 10, 1, 1, 1, 0, 0.05, 1000, 0, true, 0.2);
                Step++;
                Console.WriteLine(Step);
                if (!DM.continueLable)
                {
                    break;
                }
            }
            Map.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
            #endregion
        }

        /// <summary>
        /// Version_Beams
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button13_Click(object sender, EventArgs e)
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
            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", "STATE_ABBR", 1, 50, 1, 2, 0.01, 0.1);
            //List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", "GMI_CNTRY", 1, 50, 1, 2, 0.01, 0.1);
            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            //npg.CreateRNG(npg.NodeList, npg.EdgeList, PoList);
            #endregion

            #region 构建MST+npg refine                        
            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGByDT(pFeatureClass);
            pg.CreateMST(pg.NodeList, pg.EdgeList, PoList);
            npg.PgRefined(pg.EdgeList);//MSTrefine （添加非邻近的边,将所有图形构成一个整体）
            npg.PgRefined(Map.PolygonList);//重叠边refine  （添加重叠的边）
            //npg.LabelLongerEdges(npg.EdgeList, Map.PolygonList, 20);
            npg.LabelLongerEdges(npg.EdgeList, Map.PolygonList, 0.2);
            #endregion

            #region 移位
            int PNum = Map.PolygonList.Count;
            if (Map.PolygonList.Count < 10)
            {
                PNum = 10;
            }
            //DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 2 * PNum, 0, 0.05, 20, 3, true, 0.2);
            //DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 2 * PNum, 0, 0.05, 20, 0, true, 0.2);
            DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 2 * PNum, 0, 0.05, 0.2, 3, true, 0.2);
            DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 2 * PNum, 0, 0.05, 0.2, 0, true, 0.2); 
            Map.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
            #endregion
        }

        /// <summary>
        /// A similar Dorling approach Version_2
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button14_Click(object sender, EventArgs e)
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
            //List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", "STATE_ABBR", 1, 50, 1, 2, 0.01, 0.1);//不成比例
            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, "POPULATION", "STATE_ABBR", 0.5, 1, 2);//成比例，美国
            //List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", "GMI_CNTRY", 1, 50, 1, 2, 0.01, 0.1);//不成比例
            //List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, "POPULATION", "GMI_CNTRY", 1, 1, 2);//成比例，美洲
            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            //npg.CreateRNG(npg.NodeList, npg.EdgeList, PoList);
            #endregion

            #region 构建MST+npg refine
            //npg.DeleteLongerEdges(npg.EdgeList, Map.PolygonList, 4);//删除长边
            ////npg.DeleteLongerEdges(npg.EdgeList, Map.PolygonList, 20);//删除长边
            //npg.DeleteCrossEdge(npg.EdgeList, Map.PolygonList);//删除穿过边                           
            ProxiGraph pg = new ProxiGraph();

            #region Create ProxiNodes
            for (int i = 0; i < Map.PolygonList.Count; i++)
            {
                ProxiNode CacheNode = new ProxiNode(Map.PolygonList[i].CalProxiNode().X, Map.PolygonList[i].CalProxiNode().Y, i, i);
                pg.NodeList.Add(CacheNode);
            }
            #endregion

            //pg.CreateProxiGByDT(pFeatureClass);
            //pg.CreateMST(pg.NodeList, pg.EdgeList, PoList);
            //npg.PgRefined(pg.EdgeList);//MSTrefine 
            pg.PgRefined(Map.PolygonList);//重叠边refine  

            //npg.DeleteLongerEdges(npg.EdgeList, Map.PolygonList, 4);
            //npg.DeleteLongerEdges(npg.EdgeList, Map.PolygonList, 20);//删除长边
            #endregion

            List<ProxiGraph> PgList = pg.GetGroupPg();
            //List<ProxiGraph> PgList = npg.GetGroupPg();
            #region 移位
            SMap OutMap = new SMap();
            for (int i = 0; i < PgList.Count; i++)
            {
                if (PgList[i].NodeList.Count > 2)
                {
                    SMap newMap = DMS.regulation(PgList[i], Map);
                    int PNum = Map.PolygonList.Count;
                    if (Map.PolygonList.Count < 10)
                    {
                        PNum = 10;
                    }

                    DM.DorlingBeams(PgList[i], newMap, 1, 10, 1, 1, 2*PNum, 0, 0.05, 20, 1, true, 0.2);//美国
                    //DM.DorlingBeams(PgList[i], newMap, 1, 10, 1, 1, 2 * PNum, 0, 0.05, 20, 1, true, 0.2);//美洲
                    //if (OutFilePath != null) { PgList[i].WriteProxiGraph2Shp(OutFilePath, "邻近图" + i.ToString() + 1, pMap.SpatialReference); }
                    //OutMap.WriteResult2Shp(OutFilePath, "1", pMap.SpatialReference);
                    DM.DorlingBeams(PgList[i], newMap, 1, 10, 1, 1, 2*PNum, 0, 0.05, 20, 0, true, 0.2);//美国
                    //DM.DorlingBeams(PgList[i], newMap, 1, 10, 1, 1, 2 * PNum, 0, 0.05, 50, 0, true, 0.2);//美洲
                    //if (OutFilePath != null) { PgList[i].WriteProxiGraph2Shp(OutFilePath, "邻近图" + i.ToString() + 2, pMap.SpatialReference); }
                    //OutMap.WriteResult2Shp(OutFilePath, "2", pMap.SpatialReference);

                    OutMap.PolygonList.AddRange(newMap.PolygonList);
                }

                else
                {
                    SMap newMap = DMS.regulation(PgList[i], Map);
                    OutMap.PolygonList.AddRange(newMap.PolygonList);
                }
            }
            #endregion

            #region 后处理
            OutMap.MapObjectRegulation();
            ProxiGraph anpg = new ProxiGraph();
            anpg.CreateProxiGByDT(OutMap.PolygonList);
            anpg.CreateMST(anpg.NodeList, anpg.EdgeList, OutMap.PolygonList);
            anpg.PgRefined(OutMap.PolygonList);//重叠边refine 
            int aPNum = OutMap.PolygonList.Count;
            if (OutMap.PolygonList.Count < 10)
            {
                aPNum = 10;
            }
            DM.DorlingBeams(anpg, OutMap, 1, 10, 1, 1, 2 * aPNum, 0, 0.05, 20, 0, true, 0.2);//美国
            //DM.DorlingBeams(anpg, OutMap, 1, 10, 1, 1, 2 * aPNum, 0, 0.05, 50, 0, true, 0.2);//美洲
            #endregion

            OutMap.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
            
        }

        /// <summary>
        /// Evaluation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button17_Click(object sender, EventArgs e)
        {
            #region 获取两个Circles图层
            IFeatureLayer sBuildingLayer = null;//获得基准Cricles
            if (this.comboBox3.Text != null)
            {
                sBuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox3.Text);
            }
            IFeatureLayer aBuildingLayer = null;//获得变化后Circles
            if (this.comboBox4.Text != null)
            {
                aBuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
            }
            #endregion

            #region 读取对应的Circles
            IFeatureClass sFeatureClass = sBuildingLayer.FeatureClass;
            IFeatureClass aFeatureClass = aBuildingLayer.FeatureClass;
            Dictionary<string, IPolygon> sDic = new Dictionary<string, IPolygon>();//基准
            Dictionary<string, IPolygon> aDic = new Dictionary<string, IPolygon>();//变化后

            #region 获取对应Target的Circles（基准）
            IFeatureCursor sFeatureCursor = sFeatureClass.Update(null, false);
            IFeature sFeature = sFeatureCursor.NextFeature();
            while (sFeature != null)
            {
                string Name = this.GetStringValue(sFeature, "STATE_ABBR");//美国
                //string Name = this.GetStringValue(sFeature, "GMI_CNTRY");//美洲
                //string Name = this.GetStringValue(sFeature, "COUNTRY");
                IPolygon pPolygon = sFeature.Shape as IPolygon;
                sDic.Add(Name, pPolygon);

                sFeature = sFeatureCursor.NextFeature();
            }
            #endregion

            #region 计算邻近关系（RNG）
            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGByDT(sFeatureClass);
            pg.CreateRNG(pg.NodeList, pg.EdgeList);
            #endregion

            #region 获取对应Matching的Circles
            IFeatureCursor aFeatureCursor = aFeatureClass.Update(null, false);
            IFeature aFeature = aFeatureCursor.NextFeature();
            while (aFeature != null)
            {
                string Name = this.GetStringValue(aFeature, "Name");
                IPolygon pPolygon = aFeature.Shape as IPolygon;
                aDic.Add(Name, pPolygon);
                aFeature = aFeatureCursor.NextFeature();
            }
            #endregion
            #endregion

            #region 计算总体移位量
            double DisSum = 0;
            foreach (KeyValuePair<string, IPolygon> kv in sDic)
            {
                IPolygon tPolygon = kv.Value;
                IPolygon mPolygon = aDic[kv.Key];

                IArea tArea = tPolygon as IArea;
                IArea mArea = mPolygon as IArea;

                IPoint tPoint = tArea.Centroid;
                IPoint mPoint = mArea.Centroid;

                double Dis = Math.Sqrt((tPoint.X - mPoint.X) * (tPoint.X - mPoint.X) + (tPoint.Y - mPoint.Y) * (tPoint.Y - mPoint.Y));
                DisSum = Dis + DisSum;
            }
            #endregion

            #region 计算邻近关系保持比
            #region 原始Touch关系
            List<Tuple<string, string>> TouchedList = new List<Tuple<string, string>>();
            for (int i = 0; i < sFeatureClass.FeatureCount(null) - 1; i++)
            {
                for (int j = i + 1; j < sFeatureClass.FeatureCount(null); j++)
                {
                    if (j != i)
                    {
                        try
                        {
                            IGeometry iGeo = sFeatureClass.GetFeature(i).Shape;
                            IGeometry jGeo = sFeatureClass.GetFeature(j).Shape;

                            IRelationalOperator iRo = iGeo as IRelationalOperator;
                            if (iRo.Touches(jGeo) || iRo.Overlaps(jGeo))
                            {
                                string NameI = this.GetStringValue(sFeatureClass.GetFeature(i), "STATE_ABBR");//美国
                                string NameJ = this.GetStringValue(sFeatureClass.GetFeature(j), "STATE_ABBR");//美国

                                //string NameI = this.GetStringValue(sFeatureClass.GetFeature(i), "COUNTRY");
                                //string NameJ = this.GetStringValue(sFeatureClass.GetFeature(j), "COUNTRY");

                                //string NameI = this.GetStringValue(sFeatureClass.GetFeature(i), "GMI_CNTRY");//美洲
                                //string NameJ = this.GetStringValue(sFeatureClass.GetFeature(j), "GMI_CNTRY");//美洲

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

            #region Touch关系保持比
            double Count = 0;
            foreach (Tuple<string, string> NameMatch in TouchedList)
            {
                IPolygon Po1 = aDic[NameMatch.Item1];
                IPolygon Po2 = aDic[NameMatch.Item2];

                IProximityOperator IPO = Po1 as IProximityOperator;
                double Dis = IPO.ReturnDistance(Po2);
                if (Dis <= 0.4)
                {
                    Count++;
                }
            }
            #endregion

            double Rate = Count / TouchedList.Count;
            #endregion

            #region 邻近关系对
            List<Tuple<string, string>> NearList = new List<Tuple<string, string>>();
            for (int i = 0; i < pg.RNGBuildingEdgesListShortestDistance.Count; i++)
            {
                try
                {
                    string NameI = this.GetStringValue(sFeatureClass.GetFeature(pg.RNGBuildingEdgesListShortestDistance[i].Node1.TagID), "STATE_ABBR");//美国
                    string NameJ = this.GetStringValue(sFeatureClass.GetFeature(pg.RNGBuildingEdgesListShortestDistance[i].Node2.TagID), "STATE_ABBR");//美国

                    //string NameI = this.GetStringValue(sFeatureClass.GetFeature(pg.RNGBuildingEdgesListShortestDistance[i].Node1.TagID), "COUNTRY");
                    //string NameJ = this.GetStringValue(sFeatureClass.GetFeature(pg.RNGBuildingEdgesListShortestDistance[i].Node2.TagID), "COUNTRY");

                    //string NameI = this.GetStringValue(sFeatureClass.GetFeature(pg.RNGBuildingEdgesListShortestDistance[i].Node1.TagID), "GMI_CNTRY");//美洲
                    //string NameJ = this.GetStringValue(sFeatureClass.GetFeature(pg.RNGBuildingEdgesListShortestDistance[i].Node2.TagID), "GMI_CNTRY");//美洲
                    Tuple<string, string> NameMatch = new Tuple<string, string>(NameI, NameJ);
                    NearList.Add(NameMatch);
                }
                catch
                {
                }
            }
            #endregion

            #region 计算方向变化
            List<double> ChangeAngleList = new List<double>();
            foreach (Tuple<string, string> NameMatch in NearList)
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

            #region 计算方向均方根（RMS）
            double Ave = ChangeAngleList.Average();//计算方向变化平均值(AVE)

            double Sum = 0;
            for (int i = 0; i < ChangeAngleList.Count; i++)
            {
                Sum = ChangeAngleList[i] * ChangeAngleList[i] + Sum;
            }
            double RMS = Math.Sqrt(Sum / ChangeAngleList.Count);
            #endregion

            #endregion

            #region 计算NumO
            double NumO = 0;
            List<IPolygon> PoList = aDic.Values.ToList();
            for (int i = 0; i < PoList.Count - 1; i++)
            {
                IRelationalOperator iRo = PoList[i] as IRelationalOperator;
                for (int j = i + 1; j < PoList.Count; j++)
                {
                    if(iRo.Overlaps(PoList[j] as IGeometry))
                    {
                        NumO++;
                    }
                }
            }
            #endregion

            MessageBox.Show("TDD=" + DisSum.ToString() + ";" + "RT=" + Count.ToString() + "/" + TouchedList.Count.ToString() + "=" + Rate.ToString() + ";" + "RMS=" + RMS.ToString() + ";" + "NumO=" + NumO.ToString() + ";");
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

        /// <summary>
        /// USA Data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button15_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Stopwatch oTime = new System.Diagnostics.Stopwatch();
            oTime.Start(); //记录开始时间

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
            //List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", "STATE_ABBR", 1, 50, 1, 2, 0.01, 0.1);//不沉比例
            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, "POPULATION", "STATE_ABBR", 0.5, 1, 2);//成比例

            for (int i = 0; i < CircleList.Count; i++) //输出半径
            {
                Console.WriteLine(CircleList[i].Radius);
            }

            //List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", "GMI_CNTRY", 1, 50, 1, 2, 0.01, 0.1);
            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            //npg.CreateRNG(npg.NodeList, npg.EdgeList, PoList);
            #endregion

            #region 构建MST+npg refine
            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGByDT(pFeatureClass);
            pg.CreateMST(pg.NodeList, pg.EdgeList, PoList);
            npg.PgRefined(pg.EdgeList);//MSTrefine （添加非邻近的边,将所有图形构成一个整体）
            npg.PgRefined(Map.PolygonList);//重叠边refine  （添加重叠的边）;
            npg.LabelLongerEdges(npg.EdgeList, Map.PolygonList, 20);
            #endregion

            #region 移位
            int PNum = Map.PolygonList.Count;
            if (Map.PolygonList.Count < 10)
            {
                PNum = 10;
            }

            DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 20, 0, 0.05, 20, 3, true, 0.2);
            DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 20, 0, 0.05, 20, 0, true, 0.2);//单用的话就是Tl=0

            //DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 20, 0, 0.05, 20, 3, true, 0.2);
            //DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 2 * PNum, 0, 0.05, 20, 0, true, 0.2);
            Map.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
            if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "邻近图", pMap.SpatialReference); }
            #endregion

            oTime.Stop(); //记录结束时间

            //输出运行时间
            Console.WriteLine("程序的运行时间：{0} 秒", oTime.Elapsed.Seconds);
            Console.WriteLine("程序的运行时间：{0} 毫秒", oTime.Elapsed.Milliseconds);
        }

        /// <summary>
        /// America Data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button16_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Stopwatch oTime = new System.Diagnostics.Stopwatch();
            oTime.Start(); //记录开始时间

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
            //List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", "STATE_ABBR", 1, 50, 1, 2, 0.01, 0.1);
            //List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", "GMI_CNTRY", 1, 50, 1, 2, 0.01, 0.1);//不成比例
            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, "POPULATION", "GMI_CNTRY", 1, 1, 2);//成比例

            for (int i = 0; i < CircleList.Count; i++) //输出半径
            {
                Console.WriteLine(CircleList[i].Radius);
            }

            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            //npg.CreateRNG(npg.NodeList, npg.EdgeList, PoList);
            #endregion

            #region 构建MST+npg refine
            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGByDT(pFeatureClass);
            pg.CreateMST(pg.NodeList, pg.EdgeList, PoList);
            npg.PgRefined(pg.EdgeList);//MSTrefine （添加非邻近的边,将所有图形构成一个整体）
            npg.PgRefined(Map.PolygonList);//重叠边refine  （添加重叠的边）
            //npg.LabelLongerEdges(npg.EdgeList, Map.PolygonList, 20);
            npg.LabelLongerEdges(npg.EdgeList, Map.PolygonList, 50);
            #endregion

            #region 移位
            int PNum = Map.PolygonList.Count;
            if (Map.PolygonList.Count < 10)
            {
                PNum = 10;
            }
            DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 2*PNum, 0, 0.05, 20, 3, true, 0.2);
            DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 2*PNum, 0, 0.05, 20, 0, true, 0.2);
            //DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 20, 0, 0.05, 50, 3, true, 0.2);
            //DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 2 * PNum, 0, 0.05, 50, 0, true, 0.2);
            Map.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
            #endregion

            oTime.Stop(); //记录结束时间

            //输出运行时间
            Console.WriteLine("程序的运行时间：{0} 秒", oTime.Elapsed.Seconds);

            Console.WriteLine("程序的运行时间：{0} 毫秒", oTime.Elapsed.Milliseconds);
        }

        /// <summary>
        /// Linear size for USA Data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button18_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Stopwatch oTime = new System.Diagnostics.Stopwatch();
            oTime.Start(); //记录开始时间

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
            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, "POPULATION", "STATE_ABBR", 0.5, 1, 2);
            //List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", "GMI_CNTRY", 1, 50, 1, 2, 0.01, 0.1);

            for (int i = 0; i < CircleList.Count; i++) //输出半径
            {
                Console.WriteLine(CircleList[i].Radius);
            }

            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            //npg.CreateRNG(npg.NodeList, npg.EdgeList, PoList);
            #endregion

            #region 构建MST+npg refine
            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGByDT(pFeatureClass);
            pg.CreateMST(pg.NodeList, pg.EdgeList, PoList);
            npg.PgRefined(pg.EdgeList);//MSTrefine （添加非邻近的边,将所有图形构成一个整体）
            npg.PgRefined(Map.PolygonList);//重叠边refine  （添加重叠的边）
            //npg.LabelLongerEdges(npg.EdgeList, Map.PolygonList, 20);
            npg.LabelLongerEdges(npg.EdgeList, Map.PolygonList, 20);
            #endregion

            #region 移位
            int PNum = Map.PolygonList.Count;
            if (Map.PolygonList.Count < 10)
            {
                PNum = 10;
            }

            DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 2 * PNum, 0, 0.05, 20, 3, true, 0.2);
            DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 2 * PNum, 0, 0.05, 20, 0, true, 0.2);

            //DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 20, 0, 0.05, 20, 3, true, 0.2);
            //DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 2 * PNum, 0, 0.05, 20, 0, true, 0.2);
            Map.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
            if (OutFilePath != null) { npg.WriteProxiGraph2Shp(OutFilePath, "邻近图", pMap.SpatialReference); }
            #endregion

            oTime.Stop(); //记录结束时间

            //输出运行时间
            Console.WriteLine("程序的运行时间：{0} 秒", oTime.Elapsed.Seconds);
            Console.WriteLine("程序的运行时间：{0} 毫秒", oTime.Elapsed.Milliseconds);
        }

        /// <summary>
        /// Linear Size for America Data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button19_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Stopwatch oTime = new System.Diagnostics.Stopwatch();
            oTime.Start(); //记录开始时间

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
            //List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", "STATE_ABBR", 1, 50, 1, 2, 0.01, 0.1);
            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, "POPULATION", "GMI_CNTRY", 1, 1, 2);

            for (int i = 0; i < CircleList.Count; i++) //输出半径
            {
                Console.WriteLine(CircleList[i].Radius);
            }

            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            //npg.CreateRNG(npg.NodeList, npg.EdgeList, PoList);
            #endregion

            #region 构建MST+npg refine
            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGByDT(pFeatureClass);
            pg.CreateMST(pg.NodeList, pg.EdgeList, PoList);
            npg.PgRefined(pg.EdgeList);//MSTrefine （添加非邻近的边,将所有图形构成一个整体）
            npg.PgRefined(Map.PolygonList);//重叠边refine  （添加重叠的边）
            //npg.LabelLongerEdges(npg.EdgeList, Map.PolygonList, 20);
            npg.LabelLongerEdges(npg.EdgeList, Map.PolygonList, 10);
            #endregion

            #region 移位
            int PNum = Map.PolygonList.Count;
            if (Map.PolygonList.Count < 10)
            {
                PNum = 10;
            }
            DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 2 * PNum, 0, 0.05, 20, 3, true, 0.2);
            DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 2 * PNum, 0, 0.05, 20, 0, true, 0.2);
            //DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 20, 0, 0.05, 10, 3, true, 0.2);
            //DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 2 * PNum, 0, 0.05, 10, 0, true, 0.2);
            Map.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
            #endregion

            oTime.Stop(); //记录结束时间

            //输出运行时间
            Console.WriteLine("程序的运行时间：{0} 秒", oTime.Elapsed.Seconds);

            Console.WriteLine("程序的运行时间：{0} 毫秒", oTime.Elapsed.Milliseconds);
        }

        /// <summary>
        /// efficiency analysis for US counties
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button20_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Stopwatch oTime = new System.Diagnostics.Stopwatch();
            oTime.Start(); //记录开始时间

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
            List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", "NAME", 0.1, 10, 1, 2, 0.01, 0.1);

            for (int i = 0; i < CircleList.Count; i++) //输出半径
            {
                Console.WriteLine(CircleList[i].Radius);
            }

            //List<Circle> CircleList = DM.GetInitialCircle(pFeatureClass, npg, "POPULATION", "NAME", 0.1, 10, 1, 2, 0.01, 0.1);
            SMap Map = new SMap();
            List<PolygonObject> PoList = DM.GetInitialPolygonObject2(CircleList);
            Map.PolygonList = PoList;
            //npg.CreateRNG(npg.NodeList, npg.EdgeList, PoList);
            #endregion

            #region 构建MST+npg refine
            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGByDT(pFeatureClass);
            pg.CreateMST(pg.NodeList, pg.EdgeList, PoList);
            npg.PgRefined(pg.EdgeList);//MSTrefine （添加非邻近的边,将所有图形构成一个整体）
            npg.PgRefined(Map.PolygonList);//重叠边refine  （添加重叠的边）
            //npg.LabelLongerEdges(npg.EdgeList, Map.PolygonList, 20);
            npg.LabelLongerEdges(npg.EdgeList, Map.PolygonList, 20);
            #endregion

            #region 移位
            int PNum = Map.PolygonList.Count;
            if (Map.PolygonList.Count < 10)
            {
                PNum = 10;
            }
            //DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 2 * PNum, 0, 0.05, 20, 3, true, 0.2);
            //DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 2 * PNum, 0, 0.05, 20, 0, true, 0.2);
            DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 2 * PNum, 0, 0.02, 20, 3, true, 0.05);
            DM.DorlingBeams(npg, Map, 1, 10, 1, 1, 2 * PNum, 0, 0.02, 20, 0, true, 0.05);
            Map.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
            if (OutFilePath != null) { npg.WriteProxiGraph2Shp(OutFilePath, "邻近图", pMap.SpatialReference); }
            #endregion

            oTime.Stop(); //记录结束时间

            //输出运行时间
            Console.WriteLine("程序的运行时间：{0} 时", oTime.Elapsed.Hours);
            Console.WriteLine("程序的运行时间：{0} 分", oTime.Elapsed.Minutes);
            Console.WriteLine("程序的运行时间：{0} 秒", oTime.Elapsed.Seconds);
            Console.WriteLine("程序的运行时间：{0} 毫秒", oTime.Elapsed.Milliseconds);
        }
    }
}
