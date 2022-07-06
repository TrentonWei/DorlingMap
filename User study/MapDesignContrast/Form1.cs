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
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesRasterUI;
using ESRI.ArcGIS.SystemUI;
using stdole;

namespace MapDesignContrast
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        #region 初始化
        private void Form1_Load(object sender, EventArgs e)
        {
            #region 添加工具
            try
            {
                //this.mMapControl.MousePointer = esriControlsMousePointer.esriPointerCrosshair;
                this.axMapControl1.MousePointer = esriControlsMousePointer.esriPointerArrow;
                this.axMapControl2.MousePointer = esriControlsMousePointer.esriPointerArrow;

                //AxMapcontrol1 Tool的定义和初始化 
                ITool tool1 = new ControlsSelectFeaturesToolClass();
                //查询接口获取ICommand 
                ICommand command1 = tool1 as ICommand;
                //Tool通过ICommand与MapControl的关联 
                command1.OnCreate(this.axMapControl1.Object);
                //command1.OnClick();
                //MapControl的当前工具设定为tool 
                this.axMapControl1.CurrentTool = tool1;


                //AxMapcontrol1 Tool的定义和初始化 
                ITool tool2 = new ControlsSelectFeaturesToolClass();
                //查询接口获取ICommand 
                ICommand command2 = tool2 as ICommand;
                //Tool通过ICommand与MapControl的关联 
                command2.OnCreate(this.axMapControl2.Object);
                //command2.OnClick();
                //MapControl的当前工具设定为tool 
                this.axMapControl2.CurrentTool = tool2;           
            }

            catch
            {
                MessageBox.Show("异常");
                return;
            }
            #endregion

            #region 注记要素属性
            IRgbColor pColor = new RgbColorClass()
            {
                Red = 255,
                Blue = 0,
                Green = 0
            };
            IFontDisp pFont = new StdFont()
            {
                Name = "宋体",
                Size = 5
            } as IFontDisp;
            
            ITextSymbol pTextSymbol = new TextSymbolClass()
            {
                Color = pColor,
                Font = pFont,
                Size = 11
            };
            #endregion

            #region 图层1添加注记
            IGraphicsContainer pGraContainer = axMapControl1.Map as IGraphicsContainer;

            //遍历要标注的要素
            IFeatureLayer pFeaLayer = axMapControl1.Map.get_Layer(0) as IFeatureLayer;
            IFeatureClass pFeaClass = pFeaLayer.FeatureClass;
            IFeatureCursor pFeatCur = pFeaClass.Search(null, false);
            IFeature pFeature = pFeatCur.NextFeature();
            int index = pFeature.Fields.FindField("Name");//要标注的字段的索引
            IEnvelope pEnv = null;
            ITextElement pTextElment = null;
            IElement pEle = null;
            while (pFeature != null)
            {
                //使用地理对象的中心作为标注的位置
                pEnv = pFeature.Extent;
                IPoint pPoint = new PointClass();
                pPoint.PutCoords(pEnv.XMin + pEnv.Width * 0.5, pEnv.YMin + pEnv.Height * 0.5);

                pTextElment = new TextElementClass()
                {
                    Symbol = pTextSymbol,
                    ScaleText = true,
                    Text = pFeature.get_Value(index).ToString()
                };
                pEle = pTextElment as IElement;
                pEle.Geometry = pPoint;
                //添加标注
                pGraContainer.AddElement(pEle, 0);
                pFeature = pFeatCur.NextFeature();
            }
            (axMapControl1.Map as IActiveView).PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, axMapControl1.Extent);
            #endregion

            #region 图层2添加注记
            IGraphicsContainer pGraContainer2 = axMapControl2.Map as IGraphicsContainer;

            //遍历要标注的要素
            IFeatureLayer pFeaLayer2 = axMapControl2.Map.get_Layer(0) as IFeatureLayer;
            IFeatureClass pFeaClass2 = pFeaLayer2.FeatureClass;
            IFeatureCursor pFeatCur2 = pFeaClass2.Search(null, false);
            IFeature pFeature2 = pFeatCur2.NextFeature();
            int index2 = pFeature2.Fields.FindField("Name");//要标注的字段的索引
            IEnvelope pEnv2 = null;
            ITextElement pTextElment2 = null;
            IElement pEle2 = null;
            while (pFeature2 != null)
            {
                //使用地理对象的中心作为标注的位置
                pEnv2 = pFeature2.Extent;
                IPoint pPoint = new PointClass();
                pPoint.PutCoords(pEnv2.XMin + pEnv2.Width * 0.5, pEnv2.YMin + pEnv2.Height * 0.5);

                pTextElment2 = new TextElementClass()
                {
                    Symbol = pTextSymbol,
                    ScaleText = true,
                    Text = pFeature2.get_Value(index).ToString()
                };
                pEle2 = pTextElment2 as IElement;
                pEle2.Geometry = pPoint;
                //添加标注
                pGraContainer2.AddElement(pEle2, 0);
                pFeature2 = pFeatCur2.NextFeature();
            }
            (axMapControl2.Map as IActiveView).PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, axMapControl2.Extent);
            #endregion
        }
        #endregion

        #region axMapControl1鼠标点击事件
        private void axMapControl1_OnMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
            try
            {
                #region 右键事件
                if (e.button == 2)
                {
                    this.contextMenuStrip1.Show(this.axMapControl1, e.x, e.y);
                }
                #endregion

                #region 左键事件（记录当前的时间）
                if (e.button == 1)
                {
                    DateTime dt = DateTime.Now;
                    Console.WriteLine("当前点击时间：" + dt.Hour + ":" + dt.Minute + ":" + dt.Second + ":" + dt.Millisecond);
                }
                #endregion
            }

            catch
            {
                MessageBox.Show("异常");
                return;
            }
        }
        #endregion

        #region axMapControl2鼠标点击事件
        private void axMapControl2_OnMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
            try
            {
                #region 右键事件
                if (e.button == 2)
                {
                    this.contextMenuStrip1.Show(this.axMapControl2, e.x, e.y);
                }
                #endregion
            }

            catch
            {
                MessageBox.Show("异常");
                return;
            }
        }
        #endregion

        #region 添加样图
        private void 添加样图ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                #region 若控件在axMapcontrol1中
                if (this.contextMenuStrip1.SourceControl.Name == "axMapControl1")
                {
                    if (this.axMapControl1.Map.LayerCount == 0)
                    {
                        OpenFileDialog OpenShpFile = new OpenFileDialog();
                        OpenShpFile.Multiselect = true;
                        OpenShpFile.Title = "打开样图文件";
                        OpenShpFile.InitialDirectory = "C:";
                        OpenShpFile.Filter = "样图文件(*.样图文件)|*.img;*.bmp;*.jpg;*.png;*.shp;.tif";

                        if (OpenShpFile.ShowDialog() == DialogResult.OK)
                        {
                            string[] ShapPath = OpenShpFile.FileNames;
                            for (int i = 0; i < ShapPath.Count(); i++)
                            {
                                string SingleShapPath = ShapPath[i];
                                int Position = SingleShapPath.LastIndexOf("\\"); //利用"\\"将文件路径分成两部分
                                string FilePath = SingleShapPath.Substring(0, Position);
                                string fileName = SingleShapPath.Substring(Position + 1);

                                IWorkspaceFactory pWSF;
                                pWSF = new RasterWorkspaceFactoryClass();
                                IWorkspace pWS;
                                pWS = pWSF.OpenFromFile(FilePath, 0);
                                IRasterWorkspace pRWS;
                                pRWS = pWS as IRasterWorkspace;
                                IRasterDataset pRasterDataset;
                                pRasterDataset = pRWS.OpenRasterDataset(fileName);

                                ////影像金字塔的判断与创建
                                //IRasterPyramid pRasPyrmid;
                                //pRasPyrmid = pRasterDataset as IRasterPyramid;

                                //if (pRasPyrmid != null)
                                //{
                                //    if (!(pRasPyrmid.Present))
                                //    {
                                //        pRasPyrmid.Create();
                                //    }
                                //}

                                IRaster pRaster;
                                pRaster = pRasterDataset.CreateDefaultRaster();
                                IRasterLayer pRasterLayer;
                                pRasterLayer = new RasterLayerClass();
                                pRasterLayer.CreateFromRaster(pRaster);
                                ILayer pLayer = pRasterLayer as ILayer;
                                this.axMapControl1.AddLayer(pLayer, 0);
                            }
                        }
                    }

                    else
                    {
                        MessageBox.Show("已存在样图");
                        return;
                    }
                }
                #endregion

                #region 若控件在axMapcontrol2中
                if (this.contextMenuStrip1.SourceControl.Name == "axMapControl2")
                {
                    if (this.axMapControl2.Map.LayerCount == 0)
                    {
                        OpenFileDialog OpenShpFile = new OpenFileDialog();
                        OpenShpFile.Multiselect = true;
                        OpenShpFile.Title = "打开样图文件";
                        OpenShpFile.InitialDirectory = "C:";
                        OpenShpFile.Filter = "样图文件(*.样图文件)|*.img;*.bmp;*.jpg;*.png;*.shp;.tif";

                        if (OpenShpFile.ShowDialog() == DialogResult.OK)
                        {
                            string[] ShapPath = OpenShpFile.FileNames;
                            for (int i = 0; i < ShapPath.Count(); i++)
                            {
                                string SingleShapPath = ShapPath[i];
                                int Position = SingleShapPath.LastIndexOf("\\"); //利用"\\"将文件路径分成两部分
                                string FilePath = SingleShapPath.Substring(0, Position);
                                string fileName = SingleShapPath.Substring(Position + 1);

                                IWorkspaceFactory pWSF;
                                pWSF = new RasterWorkspaceFactoryClass();
                                IWorkspace pWS;
                                pWS = pWSF.OpenFromFile(FilePath, 0);
                                IRasterWorkspace pRWS;
                                pRWS = pWS as IRasterWorkspace;
                                IRasterDataset pRasterDataset;
                                pRasterDataset = pRWS.OpenRasterDataset(fileName);

                                ////影像金字塔的判断与创建
                                //IRasterPyramid pRasPyrmid;
                                //pRasPyrmid = pRasterDataset as IRasterPyramid;

                                //if (pRasPyrmid != null)
                                //{
                                //    if (!(pRasPyrmid.Present))
                                //    {
                                //        pRasPyrmid.Create();
                                //    }
                                //}

                                IRaster pRaster;
                                pRaster = pRasterDataset.CreateDefaultRaster();
                                IRasterLayer pRasterLayer;
                                pRasterLayer = new RasterLayerClass();
                                pRasterLayer.CreateFromRaster(pRaster);
                                ILayer pLayer = pRasterLayer as ILayer;
                                this.axMapControl2.AddLayer(pLayer, 0);
                            }
                        }
                    }

                    else
                    {
                        MessageBox.Show("已存在样图");
                        return;
                    }
                }
                #endregion          
            }

            catch
            {
                MessageBox.Show("打开样图失败");
                return;
            }
        }
        #endregion

        #region 全部移除
        private void 全部移除ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.axMapControl1.Map.LayerCount > 0)
                {
                    this.axMapControl1.ClearLayers();
                }

                if (this.axMapControl2.Map.LayerCount > 0)
                {
                    this.axMapControl2.ClearLayers();
                }
            }

            catch
            {
                MessageBox.Show("移除全部图层失败");
                return;
            }
        }
        #endregion

        #region 全图显示
        private void 全图显示ToolStripMenuItem_Click(object sender, EventArgs e)
        {
             try
            {
                if (this.axMapControl1.Map.LayerCount > 0)
                {
                    this.axMapControl1.ActiveView.Extent = this.axMapControl1.ActiveView.FullExtent;
                    this.axMapControl1.Refresh();
                }

                if (this.axMapControl2.Map.LayerCount > 0)
                {
                    this.axMapControl2.ActiveView.Extent = this.axMapControl2.ActiveView.FullExtent;
                    this.axMapControl2.Refresh();
                }
            }

            catch
            {
                MessageBox.Show("全图显示失败");
                return;
            }
        }
        #endregion

        #region 移除样图
        private void 移除样图ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                #region 若控件在axMapcontrol1中
                if (this.contextMenuStrip1.SourceControl.Name == "axMapControl1")
                {
                    if (this.axMapControl1.Map.LayerCount > 0)
                    {
                        this.axMapControl1.ClearLayers();
                    }
                }
                #endregion

                #region 若控件在axMapcontrol2中
                if (this.contextMenuStrip1.SourceControl.Name == "axMapControl2")
                {
                    if (this.axMapControl2.Map.LayerCount > 0)
                    {
                        this.axMapControl2.ClearLayers();
                    }
                }
                #endregion
            }

            catch
            {
                MessageBox.Show("移除图层失败");
                return;
            }
        }
        #endregion

        #region 视图更新
        private void 视图更新ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                #region 若控件在axMapcontrol1中
                if (this.contextMenuStrip1.SourceControl.Name == "axMapControl1")
                {
                    this.axMapControl2.ActiveView.Extent = this.axMapControl1.ActiveView.Extent;
                    this.axMapControl2.ActiveView.Refresh();
                }
                #endregion

                #region 若控件在axMapcontrol2中
                if (this.contextMenuStrip1.SourceControl.Name == "axMapControl2")
                {
                    this.axMapControl1.ActiveView.Extent = this.axMapControl2.ActiveView.Extent;
                    this.axMapControl1.ActiveView.Refresh();
                }
                #endregion
            }

            catch
            {
                MessageBox.Show("视图更新失败");
                return;
            }
        }
        #endregion

    }
}
