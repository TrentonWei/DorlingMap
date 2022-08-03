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
        /// StableDorlingMap_RNG USA Data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            #region 时间与Steps记录
            System.Diagnostics.Stopwatch oTime = new System.Diagnostics.Stopwatch();
            oTime.Start(); //记录开始时间
            #endregion

            #region 获得数据并分组
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            List<Dictionary<IPolygon, double>> TimeSeriesData = DMS.GetTimeSeriesData_2(pMap, this.comboBox1.Text);//GetTimeSeriesData
            Dictionary<int, Dictionary<int, List<int>>> Hierarchy = SDMS.GetHierarchy_2(TimeSeriesData, 0);//获得分组
            List<String> NameList = DMS.GetNames(pFeatureClass, "STATE_ABBR");//获取数据名字（评价用，非必要）
            #endregion

            #region 获得圆形
            List<List<PolygonObject>> CircleLists = DM.GetInitialPolygonObjectForStableDorling(TimeSeriesData, 0.5, 1, 2, 10);//Circle generalization

            #region 圆名字赋值（评价用，非必要）
            for (int i = 0; i < CircleLists.Count; i++)
            {
                for (int j = 0; j < NameList.Count; j++)
                {
                    CircleLists[i][j].Name = NameList[j];
                }
            }
            #endregion

            List<SMap> MapList = new List<SMap>();
            for (int i = 0; i < CircleLists.Count; i++)
            {
                SMap Map = new SMap();
                Map.PolygonList = CircleLists[i];
                MapList.Add(Map);
            }
            #endregion

            #region 构建基础邻近关系
            List<ProxiGraph> PgList = new List<ProxiGraph>();
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(pFeatureClass, 0);

            for (int i = 0; i < CircleLists.Count; i++)
            {
                ProxiGraph CopyG = Clone((object)npg) as ProxiGraph;
                PgList.Add(CopyG);
            }
            #endregion

            #region 层次移位操作
            List<int> LevelLabel = Hierarchy.Keys.ToList();
            int CircleCount = TimeSeriesData[0].Keys.Count;
            int TimeSeriesCount = TimeSeriesData.Count;

            for (int i = LevelLabel.Count - 1; i >= 0; i--)
            {
                #region 获取每一层的Maps
                Dictionary<int, List<int>> LevelMap = Hierarchy[i];
                List<List<SMap>> MapLists = new List<List<SMap>>();
                List<List<ProxiGraph>> PgLists = new List<List<ProxiGraph>>();
                foreach (KeyValuePair<int, List<int>> kv in LevelMap)
                {
                    List<SMap> CacheMapList = new List<SMap>();
                    List<ProxiGraph> CachePgList = new List<ProxiGraph>();
                    for (int j = 0; j < kv.Value.Count; j++)
                    {
                        CacheMapList.Add(MapList[kv.Value[j]]);
                        CachePgList.Add(PgList[kv.Value[j]]);
                    }
                    MapLists.Add(CacheMapList);
                    PgLists.Add(CachePgList);
                }
                #endregion

                #region 依据圆之间的重叠程度更新
                for (int j = 0; j < PgLists.Count; j++)
                {
                    PgLists[j][0].PgRefined(MapLists[j]);
                }
                #endregion

                #region Dorling Displacement
                if (i >= LevelLabel.Count / 2)
                {
                    for (int j = 0; j < MapLists.Count; j++)
                    {
                        DM.StableDorlingBeams(PgLists[j], MapLists[j], 1, 10, 1, 1, Convert.ToInt16(4 * CircleCount / TimeSeriesCount), 0, 0.05, 20, 3, true, 0.1, 0);
                    }
                }
                else
                {
                    for (int j = 0; j < MapLists.Count; j++)
                    {
                        DM.StableDorlingBeams(PgLists[j], MapLists[j], 1, 10, 1, 1, Convert.ToInt16(4 * CircleCount / TimeSeriesCount), 0, 0.05, 20, 0, true, 0.1, 0);
                    }
                }
                #endregion
            }
            #endregion

            #region 输出
            for (int i = 0; i < MapList.Count; i++)
            {
                MapList[i].WriteResult2Shp(OutFilePath, i.ToString(), pMap.SpatialReference);
                if (OutFilePath != null) { PgList[i].WriteProxiGraph2Shp(OutFilePath, "邻近图" + i.ToString(), pMap.SpatialReference); }
            }
            #endregion

            #region 时间与Steps记录
            oTime.Stop(); //记录结束时间

            //输出运行时间
            Console.WriteLine("程序的运行时间：{0} 分", oTime.Elapsed.Minutes);
            Console.WriteLine("程序的运行时间：{0} 秒", oTime.Elapsed.Seconds);
            Console.WriteLine("程序的运行时间：{0} 毫秒", oTime.Elapsed.Milliseconds);
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
        /// StableDorlingMap_Adjacent (考虑了重叠和吸引力删除的方法)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button7_Click(object sender, EventArgs e)
        {
            #region 时间与Steps记录
            System.Diagnostics.Stopwatch oTime = new System.Diagnostics.Stopwatch();
            oTime.Start(); //记录开始时间
            #endregion

            #region 获得数据并分组
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            List<Dictionary<IPolygon, double>> TimeSeriesData = DMS.GetTimeSeriesData_2(pMap, this.comboBox1.Text);//GetTimeSeriesData
            Dictionary<int, Dictionary<int, List<int>>> Hierarchy = SDMS.GetHierarchy_2(TimeSeriesData, 0);//获得分组
            List<String> NameList = DMS.GetNames(pFeatureClass, "STATE_ABBR");//获取数据名字（评价用，非必要）
            #endregion

            #region 获得圆形
            List<List<PolygonObject>> CircleLists = DM.GetInitialPolygonObjectForStableDorling(TimeSeriesData, 0.5, 1, 2, 10);//Circle generalization

            #region 圆名字赋值（评价用，非必要）
            for (int i = 0; i < CircleLists.Count; i++)
            {
                for (int j = 0; j < NameList.Count; j++)
                {
                    CircleLists[i][j].Name = NameList[j];
                }
            }
            #endregion

            List<SMap> MapList = new List<SMap>();
            for (int i = 0; i < CircleLists.Count; i++)
            {
                SMap Map = new SMap();
                Map.PolygonList = CircleLists[i];
                MapList.Add(Map);
            }
            #endregion

            #region 构建基础邻近关系
            List<ProxiGraph> PgList = new List<ProxiGraph>();
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(pFeatureClass, 0);

            for (int i = 0; i < CircleLists.Count; i++)
            {
                ProxiGraph CopyG = Clone((object)npg) as ProxiGraph;
                PgList.Add(CopyG);
            }
            #endregion

            #region 层次移位操作
            List<int> LevelLabel = Hierarchy.Keys.ToList();
            int CircleCount = TimeSeriesData[0].Keys.Count;
            int TimeSeriesCount = TimeSeriesData.Count;

            for (int i = LevelLabel.Count - 1; i >= 0; i--)
            {
                #region 获取每一层的Maps
                Dictionary<int, List<int>> LevelMap = Hierarchy[i];
                List<List<SMap>> MapLists = new List<List<SMap>>();
                List<List<ProxiGraph>> PgLists = new List<List<ProxiGraph>>();
                foreach (KeyValuePair<int, List<int>> kv in LevelMap)
                {
                    List<SMap> CacheMapList = new List<SMap>();
                    List<ProxiGraph> CachePgList = new List<ProxiGraph>();
                    for (int j = 0; j < kv.Value.Count; j++)
                    {
                        CacheMapList.Add(MapList[kv.Value[j]]);
                        CachePgList.Add(PgList[kv.Value[j]]);
                    }
                    MapLists.Add(CacheMapList);
                    PgLists.Add(CachePgList);
                }
                #endregion

                #region 依据圆之间的重叠程度更新
                for (int j = 0; j < PgLists.Count; j++)
                {
                    PgLists[j][0].PgRefined(MapLists[j]);
                }
                #endregion

                #region Dorling Displacement
                if (i >= LevelLabel.Count / 2)
                {
                    for (int j = 0; j < MapLists.Count; j++)
                    {
                        DM.StableDorlingBeams(PgLists[j], MapLists[j], 1, 10, 1, 1, Convert.ToInt16(4 * CircleCount / TimeSeriesCount), 0, 0.05, 20, 3, true, 0.1, 0);
                    }
                }
                else
                {
                    for (int j = 0; j < MapLists.Count; j++)
                    {
                        DM.StableDorlingBeams(PgLists[j], MapLists[j], 1, 10, 1, 1, Convert.ToInt16(4 * CircleCount / TimeSeriesCount), 0, 0.05, 20, 0, true, 0.1, 0);
                    }
                }
                #endregion
            }
            #endregion

            #region 输出
            for (int i = 0; i < MapList.Count; i++)
            {
                MapList[i].WriteResult2Shp(OutFilePath, i.ToString(), pMap.SpatialReference);
                if (OutFilePath != null) { PgList[i].WriteProxiGraph2Shp(OutFilePath, "邻近图" + i.ToString(), pMap.SpatialReference); }
            }
            #endregion

            #region 时间与Steps记录
            oTime.Stop(); //记录结束时间

            //输出运行时间
            Console.WriteLine("程序的运行时间：{0} 分", oTime.Elapsed.Minutes);
            Console.WriteLine("程序的运行时间：{0} 秒", oTime.Elapsed.Seconds);
            Console.WriteLine("程序的运行时间：{0} 毫秒", oTime.Elapsed.Milliseconds);
            #endregion
        }

        /// <summary>
        /// separate DorlingMap
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button8_Click(object sender, EventArgs e)
        {
            #region 时间与Steps记录
            System.Diagnostics.Stopwatch oTime = new System.Diagnostics.Stopwatch();
            oTime.Start(); //记录开始时间
            #endregion

            #region 获得数据  
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            List<Dictionary<IPolygon, double>> TimeSeriesData = DMS.GetTimeSeriesData_2(pMap, this.comboBox1.Text);//GetTimeSeriesData
            List<String> NameList = DMS.GetNames(pFeatureClass, "STATE_ABBR");//获取数据名字（评价用，非必要）
            #endregion

            #region 获得圆形
            List<List<PolygonObject>> CircleLists = DM.GetInitialPolygonObjectForStableDorling(TimeSeriesData, 0.5, 1, 2, 10);//Circle generalization

            #region 圆名字赋值（评价用，非必要）
            for (int i = 0; i < CircleLists.Count; i++)
            {
                for (int j = 0; j < NameList.Count; j++)
                {
                    CircleLists[i][j].Name = NameList[j];
                }
            }
            #endregion

            List<SMap> MapList = new List<SMap>();
            for (int i = 0; i < CircleLists.Count; i++)
            {
                SMap Map = new SMap();
                Map.PolygonList = CircleLists[i];
                MapList.Add(Map);
            }
            #endregion

            #region 构建基础邻近关系
            List<ProxiGraph> PgList = new List<ProxiGraph>();
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(pFeatureClass, 0);

            for (int i = 0; i < CircleLists.Count; i++)
            {
                ProxiGraph CopyG = Clone((object)npg) as ProxiGraph;
                PgList.Add(CopyG);
            }
            #endregion

            #region 分步移位操作
            int CircleCount = TimeSeriesData[0].Keys.Count;
            int TimeSeriesCount = TimeSeriesData.Count;

            for (int i = 0; i < TimeSeriesCount;i++ )
            {
                SMap TimeMap= MapList[i];//获取每一时刻的Maps
                PgList[i].PgRefined(TimeMap.PolygonList);//依据重叠关系更新Map

                #region Dorling Displacement
                DM.DorlingBeams(PgList[i], TimeMap, 1, 10, 1, 1, 2 * CircleCount, 0, 0.05, 20, 3, true, 0.1);
                DM.DorlingBeams(PgList[i], TimeMap, 1, 10, 1, 1, 2 * CircleCount, 0, 0.05, 20, 0, true, 0.1);
                #endregion
            }
            #endregion

            #region 输出
            for (int i = 0; i < MapList.Count; i++)
            {
                MapList[i].WriteResult2Shp(OutFilePath, i.ToString(), pMap.SpatialReference);
                if (OutFilePath != null) { PgList[i].WriteProxiGraph2Shp(OutFilePath, "邻近图" + i.ToString(), pMap.SpatialReference); }
            }
            #endregion

            #region 时间与Steps记录
            oTime.Stop(); //记录结束时间

            //输出运行时间
            Console.WriteLine("程序的运行时间：{0} 分", oTime.Elapsed.Minutes);
            Console.WriteLine("程序的运行时间：{0} 秒", oTime.Elapsed.Seconds);
            Console.WriteLine("程序的运行时间：{0} 毫秒", oTime.Elapsed.Milliseconds);
            #endregion
        }

        /// <summary>
        /// DD evaluation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button9_Click(object sender, EventArgs e)
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
                string Name = this.GetStringValue(sFeature, "Name");
                //string Name = this.GetStringValue(sFeature, "GMI_CNTRY");
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

            MessageBox.Show("DD=" + DisSum.ToString() + ";" );
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
        /// StableDorling_MST
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button10_Click(object sender, EventArgs e)
        {
            #region 时间与Steps记录
            System.Diagnostics.Stopwatch oTime = new System.Diagnostics.Stopwatch();
            oTime.Start(); //记录开始时间
            #endregion

            #region 获得数据并分组
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            List<Dictionary<IPolygon, double>> TimeSeriesData = DMS.GetTimeSeriesData_2(pMap, this.comboBox1.Text);//GetTimeSeriesData
            Dictionary<int, Dictionary<int, List<int>>> Hierarchy = SDMS.GetHierarchy_2(TimeSeriesData, 0);//获得分组
            List<String> NameList = DMS.GetNames(pFeatureClass, "STATE_ABBR");//获取数据名字（评价用，非必要）
            #endregion

            #region 获得圆形
            List<List<PolygonObject>> CircleLists = DM.GetInitialPolygonObjectForStableDorling(TimeSeriesData, 0.5, 1, 2, 10);//Circle generalization

            #region 圆名字赋值（评价用，非必要）
            for (int i = 0; i < CircleLists.Count; i++)
            {
                for (int j = 0; j < NameList.Count; j++)
                {
                    CircleLists[i][j].Name = NameList[j];
                }
            }
            #endregion

            List<SMap> MapList = new List<SMap>();
            for (int i = 0; i < CircleLists.Count; i++)
            {
                SMap Map = new SMap();
                Map.PolygonList = CircleLists[i];
                MapList.Add(Map);
            }
            #endregion

            #region 构建基础邻近关系 
            List<ProxiGraph> PgList = new List<ProxiGraph>();
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiGByDT(CircleLists[0]);
            npg.CreateMST(npg.NodeList, npg.EdgeList, CircleLists[0]);

            for (int i = 0; i < CircleLists.Count; i++)
            {
                ProxiGraph CopyG = Clone((object)npg) as ProxiGraph;
                PgList.Add(CopyG);
            }
            #endregion

            #region 层次移位操作
            List<int> LevelLabel = Hierarchy.Keys.ToList();
            int CircleCount = TimeSeriesData[0].Keys.Count;
            int TimeSeriesCount = TimeSeriesData.Count;

            for (int i = LevelLabel.Count - 1; i >= 0; i--)
            {
                #region 获取每一层的Maps
                Dictionary<int, List<int>> LevelMap = Hierarchy[i];
                List<List<SMap>> MapLists = new List<List<SMap>>();
                List<List<ProxiGraph>> PgLists = new List<List<ProxiGraph>>();
                foreach (KeyValuePair<int, List<int>> kv in LevelMap)
                {
                    List<SMap> CacheMapList = new List<SMap>();
                    List<ProxiGraph> CachePgList = new List<ProxiGraph>();
                    for (int j = 0; j < kv.Value.Count; j++)
                    {
                        CacheMapList.Add(MapList[kv.Value[j]]);
                        CachePgList.Add(PgList[kv.Value[j]]);
                    }
                    MapLists.Add(CacheMapList);
                    PgLists.Add(CachePgList);
                }
                #endregion

                #region 依据圆之间的重叠程度更新
                for (int j = 0; j < PgLists.Count; j++)
                {
                    PgLists[j][0].PgRefined(MapLists[j]);
                }
                #endregion

                #region Dorling Displacement
                if (i >= LevelLabel.Count / 2 - 1)
                {
                    for (int j = 0; j < MapLists.Count; j++)
                    {
                        //DM.StableDorlingBeams(PgLists[j], MapLists[j], 1, 10, 1, 1, Convert.ToInt16(4 * CircleCount / TimeSeriesCount), 0, 0.05, 20, 3, true, 0.1); //Proposed approach
                        DM.StableDorlingBeams(PgLists[j], MapLists[j], 1, 10, 1, 1, Convert.ToInt16(4 * CircleCount / TimeSeriesCount), 0, 0.05, 20, 1, true, 0.1, 0);
                    }
                }
                else
                {
                    for (int j = 0; j < MapLists.Count; j++)
                    {
                        DM.StableDorlingBeams(PgLists[j], MapLists[j], 1, 10, 1, 1, Convert.ToInt16(4 * CircleCount / TimeSeriesCount), 0, 0.05, 20, 0, true, 0.1, 0);
                    }
                }
                #endregion
            }
            #endregion

            #region 输出
            for (int i = 0; i < MapList.Count; i++)
            {
                MapList[i].WriteResult2Shp(OutFilePath, i.ToString(), pMap.SpatialReference);
                if (OutFilePath != null) { PgList[i].WriteProxiGraph2Shp(OutFilePath, "邻近图" + i.ToString(), pMap.SpatialReference); }
            }
            #endregion

            #region 时间与Steps记录
            oTime.Stop(); //记录结束时间

            //输出运行时间
            Console.WriteLine("程序的运行时间：{0} 分", oTime.Elapsed.Minutes);
            Console.WriteLine("程序的运行时间：{0} 秒", oTime.Elapsed.Seconds);
            Console.WriteLine("程序的运行时间：{0} 毫秒", oTime.Elapsed.Milliseconds);
            #endregion
        }

        /// <summary>
        /// 输出中间结果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button11_Click(object sender, EventArgs e)
        {

            #region 时间与Steps记录
            System.Diagnostics.Stopwatch oTime = new System.Diagnostics.Stopwatch();
            oTime.Start(); //记录开始时间
            #endregion

            #region 获得数据并分组
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            List<Dictionary<IPolygon, double>> TimeSeriesData = DMS.GetTimeSeriesData_2(pMap, this.comboBox1.Text);//GetTimeSeriesData
            Dictionary<int, Dictionary<int, List<int>>> Hierarchy = SDMS.GetHierarchy_2(TimeSeriesData, 0);//获得分组
            List<String> NameList = DMS.GetNames(pFeatureClass, "STATE_ABBR");//获取数据名字（评价用，非必要）
            #endregion

            #region 获得圆形
            List<List<PolygonObject>> CircleLists = DM.GetInitialPolygonObjectForStableDorling(TimeSeriesData, 0.5, 1, 2, 10);//Circle generalization

            #region 圆名字赋值（评价用，非必要）
            for (int i = 0; i < CircleLists.Count; i++)
            {
                for (int j = 0; j < NameList.Count; j++)
                {
                    CircleLists[i][j].Name = NameList[j];
                }
            }
            #endregion

            List<SMap> MapList = new List<SMap>();
            for (int i = 0; i < CircleLists.Count; i++)
            {
                SMap Map = new SMap();
                Map.PolygonList = CircleLists[i];
                MapList.Add(Map);
            }
            #endregion

            #region 构建基础邻近关系
            List<ProxiGraph> PgList = new List<ProxiGraph>();
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(pFeatureClass, 0);

            for (int i = 0; i < CircleLists.Count; i++)
            {
                ProxiGraph CopyG = Clone((object)npg) as ProxiGraph;
                PgList.Add(CopyG);
            }
            #endregion

            #region 层次移位操作
            List<int> LevelLabel = Hierarchy.Keys.ToList();
            int CircleCount = TimeSeriesData[0].Keys.Count;
            int TimeSeriesCount = TimeSeriesData.Count;

            for (int i = LevelLabel.Count - 1; i >= 0; i--)
            {
                #region 中间结果输出
                for (int k = 0; k < MapList.Count; k++)
                {
                    MapList[k].WriteResult2Shp(OutFilePath, (i + 1 - LevelLabel.Count).ToString() + "_" + k.ToString(), pMap.SpatialReference);
                    if (OutFilePath != null) { PgList[i].WriteProxiGraph2Shp(OutFilePath, (i + 1 - LevelLabel.Count).ToString() + "_" + k.ToString() + "邻近图", pMap.SpatialReference); }
                }
                #endregion

                #region 获取每一层的Maps
                Dictionary<int, List<int>> LevelMap = Hierarchy[i];
                List<List<SMap>> MapLists = new List<List<SMap>>();
                List<List<ProxiGraph>> PgLists = new List<List<ProxiGraph>>();
                foreach (KeyValuePair<int, List<int>> kv in LevelMap)
                {
                    List<SMap> CacheMapList = new List<SMap>();
                    List<ProxiGraph> CachePgList = new List<ProxiGraph>();
                    for (int j = 0; j < kv.Value.Count; j++)
                    {
                        CacheMapList.Add(MapList[kv.Value[j]]);
                        CachePgList.Add(PgList[kv.Value[j]]);
                    }
                    MapLists.Add(CacheMapList);
                    PgLists.Add(CachePgList);
                }
                #endregion

                #region 依据圆之间的重叠程度更新
                for (int j = 0; j < PgLists.Count; j++)
                {
                    PgLists[j][0].PgRefined(MapLists[j]);
                }
                #endregion

                #region Dorling Displacement
                if (i >= LevelLabel.Count / 2 )
                {
                    for (int j = 0; j < MapLists.Count; j++)
                    {
                        DM.StableDorlingBeams(PgLists[j], MapLists[j], 1, 10, 1, 1, Convert.ToInt16(4 * CircleCount / TimeSeriesCount), 0, 0.05, 20, 3, true, 0.1, 0);
                    }
                }
                else
                {
                    for (int j = 0; j < MapLists.Count; j++)
                    {
                        DM.StableDorlingBeams(PgLists[j], MapLists[j], 1, 10, 1, 1, Convert.ToInt16(4 * CircleCount / TimeSeriesCount), 0, 0.05, 20, 0, true, 0.1, 0);
                    }
                }
                #endregion
            }
            #endregion

            #region 输出
            for (int i = 0; i < MapList.Count; i++)
            {
                MapList[i].WriteResult2Shp(OutFilePath, i.ToString(), pMap.SpatialReference);
                if (OutFilePath != null) { PgList[i].WriteProxiGraph2Shp(OutFilePath, i.ToString()+"邻近图" , pMap.SpatialReference); }
            }
            #endregion

            #region 时间与Steps记录
            oTime.Stop(); //记录结束时间

            //输出运行时间
            Console.WriteLine("程序的运行时间：{0} 分", oTime.Elapsed.Minutes);
            Console.WriteLine("程序的运行时间：{0} 秒", oTime.Elapsed.Seconds);
            Console.WriteLine("程序的运行时间：{0} 毫秒", oTime.Elapsed.Milliseconds);
            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button12_Click(object sender, EventArgs e)
        {
            #region 时间与Steps记录
            System.Diagnostics.Stopwatch oTime = new System.Diagnostics.Stopwatch();
            oTime.Start(); //记录开始时间
            #endregion

            #region 获得数据并分组
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            List<Dictionary<IPolygon, double>> TimeSeriesData = DMS.GetTimeSeriesData_2(pMap, this.comboBox1.Text);//GetTimeSeriesData
            Dictionary<int, Dictionary<int, List<int>>> Hierarchy = SDMS.GetHierarchy_2(TimeSeriesData, 0);//获得分组
            List<String> NameList = DMS.GetNames(pFeatureClass, "COUNTRY");//获取数据名字（评价用，非必要）
            #endregion

            #region 获得圆形
            List<List<PolygonObject>> CircleLists = DM.GetInitialPolygonObjectForStableDorling_2(TimeSeriesData, 0.5, 5, 1, 2, 10);//Circle generalization

            #region 圆名字赋值（评价用，非必要）
            for (int i = 0; i < CircleLists.Count; i++)
            {
                for (int j = 0; j < NameList.Count; j++)
                {
                    CircleLists[i][j].Name = NameList[j];
                }
            }
            #endregion

            List<SMap> MapList = new List<SMap>();
            for (int i = 0; i < CircleLists.Count; i++)
            {
                SMap Map = new SMap();
                Map.PolygonList = CircleLists[i];
                MapList.Add(Map);
            }

            //for (int i = 0; i < MapList.Count; i++)
            //{
            //    MapList[i].WriteResult2Shp(OutFilePath, i.ToString(), pMap.SpatialReference);
            //}
            #endregion

            #region 构建基础邻近关系（RNG构建）
            //List<ProxiGraph> PgList = new List<ProxiGraph>();
            //ProxiGraph npg = new ProxiGraph();
            ////npg.CreateProxiG(pFeatureClass, 0);
            //npg.CreateProxiGByDT(CircleLists[0]);
            //npg.CreateRNG(npg.NodeList, npg.EdgeList);//考虑要素之间的重心距离构建RNG

            //for (int i = 0; i < npg.EdgeList.Count; i++)//表示邻近关系
            //{
            //    npg.EdgeList[i].adajactLable = true;
            //}
            #endregion

            #region 构建基础邻近关系（邻接关系构建+MSTRefine）
            List<ProxiGraph> PgList = new List<ProxiGraph>();
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(pFeatureClass, 0);

            //if (OutFilePath != null) { npg.WriteProxiGraph2Shp(OutFilePath, "npg1", pMap.SpatialReference); }

            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGByDT(MapList[0].PolygonList);
            pg.CreateMST(pg.NodeList, pg.EdgeList, MapList[0].PolygonList);
            npg.PgRefined_3(pg.EdgeList);//MSTrefine （添加非邻近的边,将所有图形构成一个整体）

            //if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pg", pMap.SpatialReference); }
            //if (OutFilePath != null) { npg.WriteProxiGraph2Shp(OutFilePath, "npg2", pMap.SpatialReference); }         
            #endregion

            #region 由于ArcEngine对复杂图形关系计算存在问题，需对复杂图形手动添加相应的关系（俄罗斯周边、芬兰周边需要添加边）
            ProxiGraph rpg = new ProxiGraph();
            rpg.CreateProxiGByDT(MapList[0].PolygonList);
            rpg.CreateRNG(rpg.NodeList, rpg.EdgeList);
            npg.PgRefined_special(rpg.EdgeList);//MSTrefine （添加非邻近的边,将所有图形构成一个整体）

            //if (OutFilePath != null) { npg.WriteProxiGraph2Shp(OutFilePath, "npg3", pMap.SpatialReference); }
            for (int i = 0; i < npg.EdgeList.Count; i++)//邻接关系全部赋值为true
            {
                npg.EdgeList[i].adajactLable = true;
            }
            #endregion

            #region 给每个Map邻近图赋值
            for (int i = 0; i < CircleLists.Count; i++)
            {
                ProxiGraph CopyG = Clone((object)npg) as ProxiGraph;
                PgList.Add(CopyG);
            }
            #endregion

            #region 层次移位操作
            List<int> LevelLabel = Hierarchy.Keys.ToList();
            int CircleCount = TimeSeriesData[0].Keys.Count;
            int TimeSeriesCount = TimeSeriesData.Count;

            for (int i = LevelLabel.Count - 1; i >= 0; i--)
            {
                #region 获取每一层的Maps
                Dictionary<int, List<int>> LevelMap = Hierarchy[i];
                List<List<SMap>> MapLists = new List<List<SMap>>();
                List<List<ProxiGraph>> PgLists = new List<List<ProxiGraph>>();
                foreach (KeyValuePair<int, List<int>> kv in LevelMap)
                {
                    List<SMap> CacheMapList = new List<SMap>();
                    List<ProxiGraph> CachePgList = new List<ProxiGraph>();
                    for (int j = 0; j < kv.Value.Count; j++)
                    {
                        CacheMapList.Add(MapList[kv.Value[j]]);
                        CachePgList.Add(PgList[kv.Value[j]]);
                    }
                    MapLists.Add(CacheMapList);
                    PgLists.Add(CachePgList);
                }
                #endregion

                #region 依据圆之间的重叠程度更新
                for (int j = 0; j < PgLists.Count; j++)
                {
                    PgLists[j][0].PgRefined(MapLists[j]);
                }
                #endregion

                #region Dorling Displacement
                if (i >= LevelLabel.Count / 2)
                {
                    for (int j = 0; j < MapLists.Count; j++)
                    {
                        DM.StableDorlingBeams(PgLists[j], MapLists[j], 1, 10, 1, 1, Convert.ToInt16(4 * CircleCount / TimeSeriesCount), 0, 0.05, 100, 3, true, 0.1, 0);
                    }
                }
                else
                {
                    for (int j = 0; j < MapLists.Count; j++)
                    {
                        DM.StableDorlingBeams(PgLists[j], MapLists[j], 1, 10, 1, 1, Convert.ToInt16(4 * CircleCount / TimeSeriesCount), 0, 0.05, 100, 0, true, 0.1, 0);
                    }
                }
                #endregion
            }
            #endregion

            #region 输出
            for (int i = 0; i < MapList.Count; i++)
            {
                MapList[i].WriteResult2Shp(OutFilePath, i.ToString(), pMap.SpatialReference);
                if (OutFilePath != null) { PgList[i].WriteProxiGraph2Shp(OutFilePath, "邻近图" + i.ToString(), pMap.SpatialReference); }
            }
            #endregion

            #region 时间与Steps记录
            oTime.Stop(); //记录结束时间

            //输出运行时间
            Console.WriteLine("程序的运行时间：{0} 分", oTime.Elapsed.Minutes);
            Console.WriteLine("程序的运行时间：{0} 秒", oTime.Elapsed.Seconds);
            Console.WriteLine("程序的运行时间：{0} 毫秒", oTime.Elapsed.Milliseconds);
            #endregion
        }

        /// <summary>
        /// Separated Dorling For EU Data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button13_Click(object sender, EventArgs e)
        {

            #region 时间与Steps记录
            System.Diagnostics.Stopwatch oTime = new System.Diagnostics.Stopwatch();
            oTime.Start(); //记录开始时间
            #endregion

            #region 获得数据
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            List<Dictionary<IPolygon, double>> TimeSeriesData = DMS.GetTimeSeriesData_2(pMap, this.comboBox1.Text);//GetTimeSeriesData
            List<String> NameList = DMS.GetNames(pFeatureClass, "COUNTRY");//获取数据名字（评价用，非必要）
            #endregion

            #region 获得圆形
            List<List<PolygonObject>> CircleLists = DM.GetInitialPolygonObjectForStableDorling_2(TimeSeriesData, 0.5, 5, 1, 2, 10);//Circle generalization

            #region 圆名字赋值（评价用，非必要）
            for (int i = 0; i < CircleLists.Count; i++)
            {
                for (int j = 0; j < NameList.Count; j++)
                {
                    CircleLists[i][j].Name = NameList[j];
                }
            }
            #endregion

            List<SMap> MapList = new List<SMap>();
            for (int i = 0; i < CircleLists.Count; i++)
            {
                SMap Map = new SMap();
                Map.PolygonList = CircleLists[i];
                MapList.Add(Map);
            }
            #endregion

            #region 构建基础邻近关系
            //List<ProxiGraph> PgList = new List<ProxiGraph>();
            //ProxiGraph npg = new ProxiGraph();
            ////npg.CreateProxiG(pFeatureClass, 0);
            //npg.CreateProxiGByDT(CircleLists[0]);
            //npg.CreateRNG(npg.NodeList, npg.EdgeList);//考虑要素之间的重心距离构建RNG

            //for (int i = 0; i < npg.EdgeList.Count; i++)//表示邻近关系
            //{
            //    npg.EdgeList[i].adajactLable = true;
            //}

            #endregion

            #region 构建基础邻近关系（邻接关系构建+MSTRefine）
            List<ProxiGraph> PgList = new List<ProxiGraph>();
            ProxiGraph npg = new ProxiGraph();
            npg.CreateProxiG(pFeatureClass, 0);

            //if (OutFilePath != null) { npg.WriteProxiGraph2Shp(OutFilePath, "npg1", pMap.SpatialReference); }

            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGByDT(MapList[0].PolygonList);
            pg.CreateMST(pg.NodeList, pg.EdgeList, MapList[0].PolygonList);
            npg.PgRefined_3(pg.EdgeList);//MSTrefine （添加非邻近的边,将所有图形构成一个整体）

            //if (OutFilePath != null) { pg.WriteProxiGraph2Shp(OutFilePath, "pg", pMap.SpatialReference); }
            //if (OutFilePath != null) { npg.WriteProxiGraph2Shp(OutFilePath, "npg2", pMap.SpatialReference); }         
            #endregion

            #region 由于ArcEngine对复杂图形关系计算存在问题，需对复杂图形手动添加相应的关系（俄罗斯周边、芬兰周边需要添加边）
            ProxiGraph rpg = new ProxiGraph();
            rpg.CreateProxiGByDT(MapList[0].PolygonList);
            rpg.CreateRNG(rpg.NodeList, rpg.EdgeList);
            npg.PgRefined_special(rpg.EdgeList);//MSTrefine （添加非邻近的边,将所有图形构成一个整体）

            //if (OutFilePath != null) { npg.WriteProxiGraph2Shp(OutFilePath, "npg3", pMap.SpatialReference); }
            for (int i = 0; i < npg.EdgeList.Count; i++)//邻接关系全部赋值为true
            {
                npg.EdgeList[i].adajactLable = true;
            }
            #endregion

            #region 给每个Map的邻近图赋值
            for (int i = 0; i < CircleLists.Count; i++)
            {
                ProxiGraph CopyG = Clone((object)npg) as ProxiGraph;
                PgList.Add(CopyG);
            }
            #endregion

            #region 分步移位操作
            int CircleCount = TimeSeriesData[0].Keys.Count;
            int TimeSeriesCount = TimeSeriesData.Count;

            for (int i = 0; i < TimeSeriesCount; i++)
            {
                SMap TimeMap = MapList[i];//获取每一时刻的Maps
                PgList[i].PgRefined(TimeMap.PolygonList);//依据重叠关系更新Map

                #region Dorling Displacement
                DM.DorlingBeams(PgList[i], TimeMap, 1, 10, 1, 1, 2 * CircleCount, 0, 0.05, 100, 3, true, 0.1);
                DM.DorlingBeams(PgList[i], TimeMap, 1, 10, 1, 1, 2 * CircleCount, 0, 0.05, 100, 0, true, 0.1);
                #endregion
            }
            #endregion

            #region 输出
            for (int i = 0; i < MapList.Count; i++)
            {
                MapList[i].WriteResult2Shp(OutFilePath, i.ToString(), pMap.SpatialReference);
                if (OutFilePath != null) { PgList[i].WriteProxiGraph2Shp(OutFilePath, "邻近图" + i.ToString(), pMap.SpatialReference); }
            }
            #endregion

            #region 时间与Steps记录
            oTime.Stop(); //记录结束时间

            //输出运行时间
            Console.WriteLine("程序的运行时间：{0} 分", oTime.Elapsed.Minutes);
            Console.WriteLine("程序的运行时间：{0} 秒", oTime.Elapsed.Seconds);
            Console.WriteLine("程序的运行时间：{0} 毫秒", oTime.Elapsed.Milliseconds);
            #endregion
        }
    }
}
