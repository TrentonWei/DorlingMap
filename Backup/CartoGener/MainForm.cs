using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System.Collections.Generic;
using DisplaceAlgLib;

namespace CartoGener
{
    public sealed partial class MainForm : Form
    {
        #region class private members
        private IMapControl3 m_mapControl = null;
        private string m_mapDocumentName = string.Empty;
        #endregion

        #region class constructor
        public MainForm()
        {
            InitializeComponent();
        }
        #endregion

        private void MainForm_Load(object sender, EventArgs e)
        {
            //get the MapControl
            m_mapControl = (IMapControl3)axMapControl1.Object;

            //disable the Save menu (since there is no document yet)
            menuSaveDoc.Enabled = false;
        }

        #region Main Menu event handlers
        private void menuNewDoc_Click(object sender, EventArgs e)
        {
            //execute New Document command
            ICommand command = new CreateNewDocument();
            command.OnCreate(m_mapControl.Object);
            command.OnClick();
        }

        private void menuOpenDoc_Click(object sender, EventArgs e)
        {
            //execute Open Document command
            ICommand command = new ControlsOpenDocCommandClass();
            command.OnCreate(m_mapControl.Object);
            command.OnClick();
        }

        private void menuSaveDoc_Click(object sender, EventArgs e)
        {
            //execute Save Document command
            if (m_mapControl.CheckMxFile(m_mapDocumentName))
            {
                //create a new instance of a MapDocument
                IMapDocument mapDoc = new MapDocumentClass();
                mapDoc.Open(m_mapDocumentName, string.Empty);

                //Make sure that the MapDocument is not readonly
                if (mapDoc.get_IsReadOnly(m_mapDocumentName))
                {
                    MessageBox.Show("Map document is read only!");
                    mapDoc.Close();
                    return;
                }

                //Replace its contents with the current map
                mapDoc.ReplaceContents((IMxdContents)m_mapControl.Map);

                //save the MapDocument in order to persist it
                mapDoc.Save(mapDoc.UsesRelativePaths, false);

                //close the MapDocument
                mapDoc.Close();
            }
        }

        private void menuSaveAs_Click(object sender, EventArgs e)
        {
            //execute SaveAs Document command
            ICommand command = new ControlsSaveAsDocCommandClass();
            command.OnCreate(m_mapControl.Object);
            command.OnClick();
        }

        private void menuExitApp_Click(object sender, EventArgs e)
        {
            //exit the application
            Application.Exit();
        }
        #endregion

        //listen to MapReplaced evant in order to update the statusbar and the Save menu
        private void axMapControl1_OnMapReplaced(object sender, IMapControlEvents2_OnMapReplacedEvent e)
        {
            //get the current document name from the MapControl
            m_mapDocumentName = m_mapControl.DocumentFilename;

            //if there is no MapDocument, diable the Save menu and clear the statusbar
            if (m_mapDocumentName == string.Empty)
            {
                menuSaveDoc.Enabled = false;
                statusBarXY.Text = string.Empty;
            }
            else
            {
                //enable the Save manu and write the doc name to the statusbar
                menuSaveDoc.Enabled = true;
                statusBarXY.Text = System.IO.Path.GetFileName(m_mapDocumentName);
            }
        }

        private void axMapControl1_OnMouseMove(object sender, IMapControlEvents2_OnMouseMoveEvent e)
        {
            statusBarXY.Text = string.Format("{0}, {1}  {2}", e.mapX.ToString("#######.##"), e.mapY.ToString("#######.##"), axMapControl1.MapUnits.ToString().Substring(4));
        }

        /// <summary>
        /// 更具图层名称获取图层
        /// </summary>
        /// <param name="lyrName"></param>
        /// <returns></returns>
        private IFeatureLayer GetLayer(string lyrName)
        {
            if (axMapControl1.LayerCount > 0)
            {
                for (int i = 0; i < axMapControl1.LayerCount; i++)
                {
                    if (lyrName == this.axMapControl1.get_Layer(i).Name)
                    {
                        return this.axMapControl1.get_Layer(i) as IFeatureLayer;
                       // axMapControl1.getse
                    }
                }

            }
            return null;
        }
        /// <summary>
        /// 对测试数据层中的线对象求刚度矩阵
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void calcuPathMaxtrixToolStripMenuItem_Click(object sender, EventArgs e)
        {
           /* IFeatureLayer lineLayer = GetLayer("测试数据");
            if(lineLayer==null)
                return;
            IGeometry shp;
            IFeatureCursor cursor = lineLayer.Search(null, false);
            IPolyline polyline = null;
            IGeometryCollection pathSet;
            IFeature curFeature = null;
            DisplaceAlgLib.Matrix  result= null;
            while ((curFeature = cursor.NextFeature()) != null)
            {
                shp = curFeature.Shape;
                if (shp.GeometryType != esriGeometryType.esriGeometryPolyline)
                    return;
                polyline = shp as IPolyline;
                pathSet = polyline as IGeometryCollection;
                int count=pathSet.GeometryCount;
                IPath curPath;
                for(int i=0;i<count;i++)
                {
                    curPath=pathSet.get_Geometry(i) as IPath;
                   // DisplaceAlgLib.ElasticBeamsAlg elasticbeams = new DisplaceAlgLib.ElasticBeamsAlg();
                    result = ElasticBeamsAlg.CalcuPolyLineMatrix(curPath);
                }
            }*/
           
        }
        /// <summary>
        /// 对所选的线对象就刚度矩阵
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void calcuSelectedPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }
        /// <summary>
        /// 求所选线的受力向量
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void calcuSelectedPathForceToolStripMenuItem_Click(object sender, EventArgs e)
        {
          
        }
        /// <summary>
        /// ElasticBeam移位操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void displaceToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            IGeometryCollection geoSet = null;   //冲突几何对象      
            IGeometryCollection pathSet = null;   //线几何对象
            IGeometry shp = null;
            IPolyline polyline = null;
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            DisplaceAlgLib.Matrix displaceVector = null;
            List<BoundPointDisplaceParams> boundPoints = null;

            IFeatureLayer lineLayer = GetLayer("测试数据");
            //获取要素图层对象
            IFeatureLayer lyrResult = GetLayer("结果");
            DeleteAllFeature("结果");
            IFeatureLayer lyr = GetLayer("邻近点");
            IFeatureLayer bufferLyr = GetLayer("移位线缓冲区");
            if (lineLayer == null || lyrResult == null)
                return;

            geoSet = new GeometryBagClass();
            IFeatureCursor cursor = lineLayer.Search(null, false);
            IFeature curFeature = null;
            while ((curFeature = cursor.NextFeature()) != null)
            {
                shp = curFeature.Shape;
                geoSet.AddGeometry(shp, ref missing1, ref missing2);
            }

            MapSelection selectSet = this.axMapControl1.ActiveView.Selection as MapSelection;
            IEnumFeature ennuFeature = selectSet as IEnumFeature;
            if (selectSet == null)
                return;
            selectSet.Reset();
            while ((curFeature = selectSet.Next()) != null)
            {
                shp = curFeature.Shape;
                if (shp.GeometryType == esriGeometryType.esriGeometryPolyline)
                {
                    polyline = shp as IPolyline;
                    pathSet = polyline as IGeometryCollection;
                    int count = pathSet.GeometryCount;
                    IPath curPath;
                    for (int i = 0; i < count; i++)
                    {
                        curPath = pathSet.get_Geometry(i) as IPath;
                        DisplaceAlgLib.ElasticBeam beam = new ElasticBeam((float)ElasticBeamsAlg.fDefaultParaA, (float)ElasticBeamsAlg.fDefaultParaE, (float)ElasticBeamsAlg.fDefaultParaI, (float)SnakesAlg.fDefaultSymWidth, curPath, bufferLyr);
                      
                        //边界条件
                        IPointCollection curPathPointSet = curPath as IPointCollection;
                        int n = curPathPointSet.PointCount;

                        boundPoints = new List<BoundPointDisplaceParams>();

                        BoundPointDisplaceParams boundp1 = new BoundPointDisplaceParams(0, 0, 0, 0);
                        BoundPointDisplaceParams boundp2 = new BoundPointDisplaceParams(n - 1, 0, 0, 0);

                        boundPoints.Add(boundp1);
                        boundPoints.Add(boundp2);

                        ElasticBeam newBeam = ElasticBeamsAlg.CalcuDisplaceVector(beam, geoSet, boundPoints, 10, 0.1, 5);

                        WriteResultLineBeam(lyrResult, newBeam.Path, displaceVector);

                        this.axMapControl1.ActiveView.Refresh();
                    }
                }
            }
        }
        /// <summary>
        /// 将移位结果写入结果图层
        /// </summary>
        /// <param name="resultLyr">结果图层对象</param>
        /// <param name="path">原来的线对象</param>
        /// <param name="dispaceVector">移位向量</param>
        private void WriteResultLineBeam(IFeatureLayer resultLyr, IPath path, Matrix displaceVector)
        {
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            if(resultLyr==null)
                return;
            if(path==null)
                return;
            
            IFeatureClass featureClass = resultLyr.FeatureClass;
            if(featureClass==null)
                return;
           //获取凸壳图层的数据集，并创建工作空间
            IDataset dataset = (IDataset)resultLyr;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;

            //注意：此时，所编辑数据不能被其他程序打开
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();
            
            IPointCollection pointSet = path as IPointCollection;
            int n = pointSet.PointCount;
            if (n == 0)
                return;

            IFeature feature = featureClass.CreateFeature();
            IGeometry shp = new PolylineClass();
            IPointCollection pointSet2 = shp as IPointCollection;
            IPoint curResultPoint=null;
            IPoint curPoint=null;
            for (int i = 0; i < n; i++)
            {
                curPoint = pointSet.get_Point(i); 
                curResultPoint=new PointClass();
                curResultPoint.PutCoords(curPoint.X/* + displaceVector[i * 3, 0]*/, curPoint.Y /*+ displaceVector[i * 3 + 1, 0]*/);
                pointSet2.AddPoint(curResultPoint,ref missing1,ref missing2);
            }
            feature.Shape = shp;
            feature.Store();
            //关闭编辑
            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
        }

        /// <summary>
        /// 将移位结果写入一个Path
        /// </summary>
        /// <param name="path">原来的线对象</param>
        /// <param name="dispaceVector">移位向量</param>
        private void UpdatePath(ref IPath path, Matrix displaceVector)
        {
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

         
            if (path == null)
                return;

            IPointCollection pointSet = path as IPointCollection;
            int n = pointSet.PointCount;
            if (n == 0)
                return;

            IPoint curPoint = null;
            for (int i = 0; i < n; i++)
            {
                curPoint = pointSet.get_Point(i); 
                curPoint = pointSet.get_Point(i);
                curPoint.PutCoords(curPoint.X + displaceVector[i * 3, 0], curPoint.Y + displaceVector[i * 3 + 1, 0]);   
            }
        }

        /// <summary>
        ///清空一个图层中所有的要素
        /// </summary>
        /// <param name="strLyrName">图层名</param>
        private void DeleteAllFeature(string strLyrName)
        {
            //获取要素图层对象
            IFeatureLayer lyr = GetLayer(strLyrName);
            if (lyr == null)
            {
                return;
            }

            //获取凸壳图层的数据集，并创建工作空间
            IDataset dataset = (IDataset)lyr;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;

            // Start an edit session and edit operation.打开编辑状态
            //注意：此时，所编辑数据不能被其他程序打开
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();
            //获取要素集
            // IFeatureClass srcFeatureCls = lyrConvexHull.FeatureClass;
            IFeatureClass featureCls = lyr.FeatureClass;

            IFeature curFeature = null;
            IFeatureCursor cursor = lyr.Search(null, false);

            while ((curFeature = cursor.NextFeature()) != null)
            {
                curFeature.Delete();
            }

            //关闭编辑
            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);

        }
        /// <summary>
        /// Snake移位操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void snakeDisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IGeometryCollection geoSet = null;   //冲突几何对象      
            IGeometryCollection pathSet = null;   //线几何对象
            IGeometry shp = null;
            IPolyline polyline = null;
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            //DisplaceAlgLib.Matrix forceVector = null;
            //DisplaceAlgLib.Matrix stiffMatrix = null;
            //DisplaceAlgLib.Matrix InversstiffMatrix = null;
            DisplaceAlgLib.Matrix displaceVector = null;
            List<BoundPointDisplaceParams> boundPoints = null;

            //bool isSingle = false; //刚度矩阵是否奇异矩阵

            
            IFeatureLayer lineLayer = GetLayer("测试数据");
            //获取要素图层对象
            IFeatureLayer lyrResult = GetLayer("结果");
           // DeleteAllFeature("结果");
            IFeatureLayer lyr = GetLayer("邻近点");
            IFeatureLayer bufferLyr = GetLayer("移位线缓冲区");
            if (lineLayer == null || lyrResult == null)
                return;

            geoSet = new GeometryBagClass();
            IFeatureCursor cursor = lineLayer.Search(null, false);
            IFeature curFeature = null;
            while ((curFeature = cursor.NextFeature()) != null)
            {
                shp = curFeature.Shape;
                geoSet.AddGeometry(shp, ref missing1, ref missing2);
            }

            MapSelection selectSet = this.axMapControl1.ActiveView.Selection as MapSelection;
            IEnumFeature ennuFeature = selectSet as IEnumFeature;
            if (selectSet == null)
                return;
            selectSet.Reset();
            while ((curFeature = selectSet.Next()) != null)
            {
                shp = curFeature.Shape;
                if (shp.GeometryType == esriGeometryType.esriGeometryPolyline)
                {
                    polyline = shp as IPolyline;
                    pathSet = polyline as IGeometryCollection;
                    int count = pathSet.GeometryCount;
                    IPath curPath;
                    for (int i = 0; i < count; i++)
                    {
                        curPath = pathSet.get_Geometry(i) as IPath;
                        Snake snake = new Snake((float)SnakesAlg.fDefaultA, (float)SnakesAlg.fDefaultB, (float)SnakesAlg.fDefaultSymWidth, curPath, bufferLyr);
                        // DisplaceAlgLib.ElasticBeamsAlg elasticbeams = new DisplaceAlgLib.ElasticBeamsAlg();
                        // stiffMatrix = ElasticBeamsAlg.CalcuPolyLineMatrix(curPath);                 //计算刚度矩阵

                        // for (int d = 0; d < 500; d++)
                        //{
                        // forceVector = ElasticBeamsAlg.CalcuVetexModelForceVector(curPath, geoSet);//计算力
                        // forceVector = ElasticBeamsAlg.CalcuMoment(curPath, ref forceVector);       //计算力矩
                        //边界条件
                        IPointCollection curPathPointSet = curPath as IPointCollection;
                        int n = curPathPointSet.PointCount;

                        boundPoints = new List<BoundPointDisplaceParams>();

                        BoundPointDisplaceParams boundp1 = new BoundPointDisplaceParams(0, 0, 0, 0);
                        BoundPointDisplaceParams boundp2 = new BoundPointDisplaceParams(n - 1, 0, 0, 0);

                        boundPoints.Add(boundp1);
                        boundPoints.Add(boundp2);

                        Matrix displaceVector_X;
                        Matrix displaceVector_Y;

                        Snake newSnake = SnakesAlg.CalcuDisplaceVector(out  displaceVector_X, out displaceVector_Y, snake, geoSet, boundPoints,1, 0.1, 1);
                        //Snake newSnake = SnakesAlg.CalcuDisplaceVectorXY(out  displaceVector_X, out displaceVector_Y, snake, geoSet, boundPoints, 0, 0.1, 1);
                        //UpdatePath(ref curPath, displaceVector);
                  
                        WriteResultLineSnake(lyrResult, newSnake.Path, displaceVector_X, displaceVector_Y);

                        this.axMapControl1.ActiveView.Refresh();
                    }
                }
            }
        }

        /// <summary>
        /// 将移位结果写入结果图层
        /// </summary>
        /// <param name="resultLyr">结果图层对象</param>
        /// <param name="path">原来的线对象</param>
        /// <param name="dispaceVector">移位向量</param>
        private void WriteResultLineSnake(IFeatureLayer resultLyr, IPath path, Matrix displaceVector_X, Matrix displaceVector_Y)
        {
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            if (resultLyr == null)
                return;
            if (path == null)
                return;

            IFeatureClass featureClass = resultLyr.FeatureClass;
            if (featureClass == null)
                return;
            //获取凸壳图层的数据集，并创建工作空间
            IDataset dataset = (IDataset)resultLyr;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;

            //注意：此时，所编辑数据不能被其他程序打开
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            IPointCollection pointSet = path as IPointCollection;
            int n = pointSet.PointCount;
            if (n == 0)
                return;

            IFeature feature = featureClass.CreateFeature();
            IGeometry shp = new PolylineClass();
            IPointCollection pointSet2 = shp as IPointCollection;
            IPoint curResultPoint = null;
            IPoint curPoint = null;
            for (int i = 0; i < n; i++)
            {
                curPoint = pointSet.get_Point(i);
                curResultPoint = new PointClass();
                curResultPoint.PutCoords(curPoint.X/* +displaceVector_X[i *2, 0]*/, curPoint.Y/* + displaceVector_Y[i * 2 , 0]*/);
                pointSet2.AddPoint(curResultPoint, ref missing1, ref missing2);
            }
            feature.Shape = shp;
            feature.Store();
            //关闭编辑
            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void mapSettingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogMapSetting dialogMapSetting = new DialogMapSetting();
            dialogMapSetting.ShowDialog();
        }

        private void clearResultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteAllFeature("结果");
            this.axMapControl1.ActiveView.Refresh();
        }

        private void snakeSettingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogInitSnakesParams dialogInitSnakesParams = new DialogInitSnakesParams();
            dialogInitSnakesParams.ShowDialog();
        }

        private void tESTToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogSnakeSetting dialog = new DialogSnakeSetting();
            dialog.ShowDialog();

        }
    }
}