using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System.IO;

namespace RoadDisAlg
{
    /// <summary>
    /// 道路网
    /// </summary>
    public class RoadNetWork
    {
        public List<RoadLyrInfo> RoadLyrInfoList = null;          //图层信息列表
        public List<Road> RoadList = null;                 //道路列表
        public List<ConnNode> ConnNodeList = null;         //关联结点列表
        public List<PointCoord> PointList = null;          //顶点坐标及编号

        /// <summary>
        /// 构造函数
        /// </summary>
        public RoadNetWork(List<RoadLyrInfo> roadLyrInfoList)
        {
            RoadLyrInfoList = roadLyrInfoList;
            RoadList = new List<Road>();
            ConnNodeList = new List<ConnNode>();
            PointList = new List<PointCoord>();
        }
        /// <summary>
        /// 判断当期的顶点是否已经在关联点集合中
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns></returns>
        public ConnNode GetContainNode(double x, double y)
        {
            if (this.ConnNodeList == null || this.ConnNodeList.Count == 0)
            {
                return null;
            }
            foreach (ConnNode curNode in this.ConnNodeList)
            {
                if(Math.Abs(1-this.PointList[curNode.PointID].X/x)<=0.0001f
                    &&(1-Math.Abs(this.PointList[curNode.PointID].Y/y))<=0.0001f)
                {
                    return curNode;
                }
            }
            return null;
        }

        /// <summary>
        /// 创建网络结构
        /// </summary>
        public void CreateNetWork()
        {
            int pointID = 0;
            int roadID = 0;
            double curSylWidth = 0.0;
            //图层
            foreach (RoadLyrInfo curLyrInfo in RoadLyrInfoList)
            {
                curSylWidth = curLyrInfo.RoadGrade.SylWidth;
                if (curLyrInfo.RoadGrade.Grade == 999)//等级为-1的代表不参与移位的要素
                {
                    continue;
                }

                IFeatureLayer curLyr = curLyrInfo.Lyr;
                IFeatureCursor cursor = curLyr.Search(null, false);
                IFeature curFeature = null;
                IGeometry shp = null;
               
                //要素
                while ((curFeature = cursor.NextFeature()) != null)
                {
                    shp = curFeature.Shape;
                    IPolyline polyline=null;
                    IGeometryCollection pathSet=null;
                    //几何图形
                    if (shp.GeometryType == esriGeometryType.esriGeometryPolyline)
                    {
                        polyline = shp as IPolyline;
                        pathSet = polyline as IGeometryCollection;
                        int count = pathSet.GeometryCount;
                        //Path对象
                        IPath curPath=null;
                        for (int i = 0; i < count; i++)
                        {
                            Road curRoad = null;                      //当前道路
                            ConnNode curNode = null;                  //当前关联点
                            PointCoord curPoint = null;               //当期顶点
                            double curX;
                            double curY;

                            curPath = pathSet.get_Geometry(i) as IPath;
                            IPointCollection pointSet = curPath as IPointCollection;
                            int pointCount = pointSet.PointCount;
                            if (pointCount >= 2)
                            {
                        
                                //添加起点
                                curRoad = new Road(curLyrInfo.RoadGrade,this);
                                //设置道路编号
                                curRoad.RID = roadID;
                                roadID++;

                                curX = pointSet.get_Point(0).X;
                                curY = pointSet.get_Point(0).Y;
                                ConnNode cNode = this.GetContainNode(curX, curY);
                                if (cNode == null)   //该关联点还未加入的情况
                                {
  

                                    curPoint = new PointCoord(curX, curY, pointID,curSylWidth);
                                    this.PointList.Add(curPoint);
                                    curRoad.PointList.Add(pointID);
                                    curNode = new ConnNode(pointID);
                                    curNode.ConRoadList.Add(curRoad.RID);
                                    this.ConnNodeList.Add(curNode);
                                    curRoad.FNode = pointID;
                                    pointID++;
                                }
                                else //该关联点已经加入的情况
                                {
                                    curRoad.PointList.Add(cNode.PointID);
                                    curRoad.FNode = cNode.PointID;
                                    curRoad.FNode = cNode.PointID;
                                    cNode.ConRoadList.Add(curRoad.RID);
                                }
                                //加入中间顶点
                                for(int j=1;j<pointCount-1;j++)
                                {
                   
                                    //添加起点
                                    curX = pointSet.get_Point(j).X;
                                    curY = pointSet.get_Point(j).Y;
                                    curPoint = new PointCoord(curX, curY, pointID, curSylWidth);
                                    curPoint.tagID = curRoad.RID;
                                    this.PointList.Add(curPoint);
                                    curRoad.PointList.Add(pointID);
                                    pointID++;
                                }


                                //加入终点
                                curX = pointSet.get_Point(pointCount - 1).X;
                                curY = pointSet.get_Point(pointCount - 1).Y;
                                cNode = this.GetContainNode(curX, curY);
                                if (cNode == null)   //该关联点还未加入的情况
                                {
                                    curPoint = new PointCoord(curX, curY, pointID, curSylWidth);
                                    this.PointList.Add(curPoint);
                                    curRoad.PointList.Add(pointID);
                                    curNode = new ConnNode(pointID);
                                    curNode.ConRoadList.Add(curRoad.RID);
                                    this.ConnNodeList.Add(curNode);
                                    curRoad.TNode = pointID;
                                    pointID++;
                                }
                                else //该关联点已经加入的情况
                                {
                                    curRoad.PointList.Add(cNode.PointID);
                                    curRoad.TNode = cNode.PointID;
                                    cNode.ConRoadList.Add(curRoad.RID);
                                }
                            }
                            //创建道路上的各段弯曲
                            curRoad.CreateCurveList();
                            //将创建好的道路对象加入列表
                            this.RoadList.Add(curRoad);
                        }
                    }
                }
            }
         }

        /// <summary>
        /// 将线要素的顶点和端点写入图层中
        /// </summary>
        /// <param name="ptLyr">顶点图层</param>
        /// <param name="ntLyr">端点图层</param>
        /// <param name="mapControl">地图控件</param>
        public void WritePointtoFeatureClass(ref IFeatureLayer ptLyr, ref IFeatureLayer ntLyr, ESRI.ArcGIS.Controls.AxMapControl mapControl)
        {
            IFeatureClass featureClass = null;
            #region 写入顶点
            if (ptLyr != null&&this.PointList!=null&&this.PointList.Count != 0)
            {
                featureClass = ptLyr.FeatureClass;
                if (featureClass == null)
                    return;
                //获取顶点图层的数据集，并创建工作空间
                IDataset dataset = (IDataset)ptLyr;
                IWorkspace workspace = dataset.Workspace;
                IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
                IFeatureClassWrite fr = (IFeatureClassWrite)featureClass;//定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  

                //注意：此时，所编辑数据不能被其他程序打开
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();

                int n = this.PointList.Count;
                for (int i = 0; i < n; i++)
                {
                    IFeature feature = featureClass.CreateFeature();

                    IPoint p;//定义一个点，用来作为IFeature实例的形状属性，即shape属性  
                    //下面是设置点的坐标和参考系  
                    p = new PointClass();
                    p.SpatialReference = mapControl.SpatialReference;
                    p.X = PointList[i].X;
                    p.Y = PointList[i].Y;
                    //将IPoint设置为IFeature的shape属性时，需要通过中间接口IGeometry转换  
                    IGeometry peo;
                    peo = p;

                    feature.Shape = peo;//设置IFeature对象的形状属性  
                    feature.set_Value(2, PointList[i].ID);//设置IFeature对象的索引是2的字段值  
                    feature.set_Value(3, PointList[i].X);//设置IFeature对象的索引是3的字段值 
                    feature.set_Value(4, PointList[i].Y);//设置IFeature对象的索引是4的字段值
 
                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上  
                }

                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            #endregion
            #region 写入端点
            if (ntLyr != null&&this.ConnNodeList!=null&&this.ConnNodeList.Count != 0)
            {
                featureClass = ntLyr.FeatureClass;
                if (featureClass == null)
                    return;
                //获取端点图层的数据集，并创建工作空间
                IDataset dataset = (IDataset)ptLyr;
                IWorkspace workspace = dataset.Workspace;
                IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
                IFeatureClassWrite fr = (IFeatureClassWrite)featureClass;//定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  

                //注意：此时，所编辑数据不能被其他程序打开
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();

                int n = this.ConnNodeList.Count;
                for (int i = 0; i < n; i++)
                {
                    IFeature feature = featureClass.CreateFeature();

                    IPoint p;//定义一个点，用来作为IFeature实例的形状属性，即shape属性  
                    //下面是设置点的坐标和参考系  
                    p = new PointClass();
                    p.SpatialReference = mapControl.SpatialReference;
                    p.X = this.PointList[ConnNodeList[i].PointID].X;
                    p.Y = this.PointList[ConnNodeList[i].PointID].Y;
                    //将IPoint设置为IFeature的shape属性时，需要通过中间接口IGeometry转换  
                    IGeometry peo;
                    peo = p;

                    feature.Shape = peo;//设置IFeature对象的形状属性  
                    feature.set_Value(2, this.PointList[ConnNodeList[i].PointID].ID);//设置IFeature对象的索引是3的字段值  
                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上  
                }

                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            #endregion
            mapControl.Refresh();//刷新地图  
        }
        
        /// <summary>
        /// 将弯曲写入图层中
        /// </summary>
        /// <param name="cuvLyr">道路弯曲段图层</param>
        /// <param name="mapControl">地图控件</param>
        public void  WriteCurveFeatureClass(ref IFeatureLayer cuvLyr,ESRI.ArcGIS.Controls.AxMapControl mapControl) 
        {
            IFeatureClass featureClass = null;
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            #region 道路弯曲段
            if (cuvLyr != null && this.RoadList!= null && this.RoadList.Count != 0)
            {
                featureClass = cuvLyr.FeatureClass;
                if (featureClass == null)
                    return;
                //获取顶点图层的数据集，并创建工作空间
                IDataset dataset = (IDataset)cuvLyr;
                IWorkspace workspace = dataset.Workspace;
                IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
                //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
                IFeatureClassWrite fr = (IFeatureClassWrite)featureClass;
                //注意：此时，所编辑数据不能被其他程序打开
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();
                int n = this.RoadList.Count;
                for (int i = 0; i < n; i++)
                {
                    int m=this.RoadList[i].RoadCurveList.Count;
                    for (int j = 0; j < m; j++)
                    {
                        IFeature feature = featureClass.CreateFeature();
                        IGeometry shp = new PolylineClass();
                        shp.SpatialReference = mapControl.SpatialReference; 
                        IPointCollection pointSet= shp as IPointCollection;
                        IPoint curResultPoint = null;
                        PointCoord curPoint = null;
                        int h=this.RoadList[i].RoadCurveList[j].PointList.Count;
                        for (int k = 0; k < h; k++)
                        {
                            curPoint = this.RoadList[i].RoadCurveList[j].GetCoord(k);
                            curResultPoint = new PointClass();
                            curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                            pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                        }
                        feature.Shape = shp;
                        feature.set_Value(2, this.RoadList[i].RoadCurveList[j].k);//设置IFeature对象的索引是2的字段值  
                        feature.set_Value(3, this.RoadList[i].RoadCurveList[j].a);//设置IFeature对象的索引是3的字段值 
                        feature.set_Value(4, this.RoadList[i].RoadCurveList[j].b);//设置IFeature对象的索引是4的字段值

                        feature.Store();//保存IFeature对象  
                        fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                    }   
                }
               //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);

            }
            #endregion

            mapControl.Refresh();//刷新地图 
        }

        /// <summary>
        /// 获取指定名称的要素图层
        /// </summary>
        /// <param name="lyrName">图层名称</param>
        /// <returns></returns>
        private IFeatureLayer  GetLyrbyName(string lyrName,List<RoadLyrInfo> roadLyrs)
        {
            foreach(RoadLyrInfo curlyrinfo in  roadLyrs)
            {
                if(curlyrinfo.RoadGrade.LyrName==lyrName)
                {
                    return curlyrinfo.Lyr;
                }
            }
            return null;
        }
        /// <summary>
        /// 将移位后的道路写入图层中
        /// </summary>
        /// <param name="cuvLyr">道路弯曲段图层</param>
        /// <param name="mapControl">地图控件</param>
        public void WriteRoadFeaturesClass(List<RoadLyrInfo> roadLyrs, ESRI.ArcGIS.Controls.AxMapControl mapControl)
        {
            IFeatureClass featureClass = null;
            IFeatureLayer curLyr;
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;
            string curLyrName="";
            #region 道路弯曲段
            if (roadLyrs != null && this.RoadList != null && this.RoadList.Count != 0)
            {
                foreach (Road curRoad in this.RoadList)
                {
                    curLyrName = curRoad.RoadGrade.LyrName + "_D";
                    curLyr = GetLyrbyName(curLyrName, roadLyrs);
                    featureClass = curLyr.FeatureClass;
                    //获取顶点图层的数据集，并创建工作空间
                    IDataset dataset = (IDataset)curLyr;
                    IWorkspace workspace = dataset.Workspace;
                    IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
                    //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
                    IFeatureClassWrite fr = (IFeatureClassWrite)featureClass;
                    //注意：此时，所编辑数据不能被其他程序打开
                    workspaceEdit.StartEditing(true);
                    workspaceEdit.StartEditOperation();



                    IFeature feature = featureClass.CreateFeature();
                    IGeometry shp = new PolylineClass();
                    shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = null;
                    PointCoord curPoint = null;
                    int h = curRoad.PointList.Count;
                    for (int k = 0; k < h; k++)
                    {
                        curPoint = curRoad.GetCoord(k);
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    }
                    feature.Shape = shp;

                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     

                    //关闭编辑
                    workspaceEdit.StopEditOperation();
                    workspaceEdit.StopEditing(true);
                }
            }
            #endregion
            mapControl.Refresh();//刷新地图 
        }

        /// <summary>
        /// 输出网络结构到TXT文件
        /// </summary>
        /// <param name="filepath"></param>
        public void WritetoTxt(string filepath)
        {
            //输出道路
            StreamWriter streamw = File.CreateText(filepath+"\\Road.txt");
            int count = this.RoadList.Count;
            for (int i = 0; i < count; i++)
            {
                streamw.Write(RoadList[i].RID.ToString() + "  ");
                int n = this.RoadList[i].PointList.Count;
                for (int j = 0; j < n-1; j++)
                {
                    streamw.Write(this.RoadList[i].PointList[j].ToString() + ",");
                }
                streamw.Write(this.RoadList[i].PointList[n-1].ToString());
         
                streamw.WriteLine();
            }
            streamw.WriteLine();
            streamw.Close();
            //输出点
            streamw = File.CreateText(filepath + "\\Point.txt");
            count = this.PointList.Count;
            for (int i = 0; i < count; i++)
            {
                streamw.Write(PointList[i].ID.ToString() + "  " + PointList[i].X.ToString()+"  "+PointList[i].Y.ToString());
                streamw.WriteLine();
            }
            streamw.WriteLine();
            streamw.Close();
            //输出端点
            streamw = File.CreateText(filepath + "\\Node.txt");
            count = this.ConnNodeList.Count;
            for (int i = 0; i < count; i++)
            {
                streamw.Write(ConnNodeList[i].PointID.ToString() + "  ");
                int n = this.ConnNodeList[i].ConRoadList.Count;
                for (int j = 0; j < n - 1; j++)
                {
                    streamw.Write(this.ConnNodeList[i].ConRoadList[j].ToString() + ",");
                }
                streamw.Write(this.ConnNodeList[i].ConRoadList[n-1].ToString());

                streamw.WriteLine();
            }
            streamw.WriteLine();
            streamw.Close();

            //输出弯曲段
            streamw = File.CreateText(filepath + "\\Curve.txt");
            count = this.RoadList.Count;
            for (int i = 0; i < count; i++)
            {
                streamw.Write("道路"+this.RoadList[i].RID.ToString() + "包含弯曲段：");
                streamw.WriteLine();
                int n = this.RoadList[i].RoadCurveList.Count;
                for (int j = 0; j < n ; j++)
                {
                    int m=this.RoadList[i].RoadCurveList[j].PointList.Count;
                    streamw.Write(j.ToString()+"  ");
                    streamw.Write(this.RoadList[i].RoadCurveList[j].k.ToString()+"   ");
                    streamw.Write(this.RoadList[i].RoadCurveList[j].a.ToString() + "   ");
                    streamw.Write(this.RoadList[i].RoadCurveList[j].b.ToString() + "   ");
                    for (int k = 0; k < m - 1; k++)
                    {
                        streamw.Write(this.RoadList[i].RoadCurveList[j].PointList[k].ToString() + ",");
                    }
                    streamw.Write(this.RoadList[i].RoadCurveList[j].PointList[m-1].ToString());
                    streamw.WriteLine();
                }
                streamw.WriteLine();
            }
            streamw.WriteLine();
            streamw.Close();
        }
    }
}
