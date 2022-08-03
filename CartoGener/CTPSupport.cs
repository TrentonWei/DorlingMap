using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatrixOperation;

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
    /// CTPSupport Class
    /// </summary>
    class CTPSupport
    {
        public bool continueLable = true;//参数
        public AxESRI.ArcGIS.Controls.AxMapControl mCon = null;
        Symbolization Sb = new Symbolization();//符号化工具，测试用

        /// <summary>
        /// 计算每个位置的最终位置
        /// </summary>
        /// <param name="InitialObject">第一个点是起点；其它点是终点</param>
        /// <returns></returns>
        public List<PointObject> FinalLocation(List<PointObject> InitialObject)
        {
            List<PointObject> FinalPoints = new List<PointObject>();
            PointObject StartPoint = InitialObject[0];  //start_Point
            double StartX=StartPoint.Point.X;
            double StartY=StartPoint.Point.Y;

            #region Get distance and travel time
            List<double> DisList = new List<double>();//存储目的地到起点的距离列表
            List<double> TimeList = new List<double>();//存储目的地到起点的时间列表

            for (int i = 1; i < InitialObject.Count; i++)//第一个点是起点，不纳入考虑！！
            {
                double Dis = Math.Sqrt((InitialObject[i].Point.X - StartX) * (InitialObject[i].Point.X - StartX) + (InitialObject[i].Point.Y - StartY) * (InitialObject[i].Point.Y - StartY));
                DisList.Add(Dis);
                TimeList.Add(InitialObject[i].TT);
            }
            #endregion

            #region GetFinalLocations
            double SumDis=DisList.Sum();
            double SumTime=TimeList.Sum();

            for (int i = 1; i < InitialObject.Count; i++)
            {
                double FinalDis = TimeList[i - 1] * SumDis / SumTime ;

                double X = StartX + (InitialObject[i].Point.X - StartX) * FinalDis / DisList[i - 1];
                double Y = StartY + (InitialObject[i].Point.Y - StartY) * FinalDis / DisList[i - 1];

                PointObject curPoint = null;
                TriNode curVextex = null;
                curVextex = new TriNode((float)X, (float)Y, InitialObject[i].Point.TagValue, InitialObject[i].Point.TagValue, FeatureType.PointType);
                curPoint = new PointObject(InitialObject[i].ID, curVextex);

                FinalPoints.Add(curPoint);
            }

            #endregion

            return FinalPoints;
        }

        /// <summary>
        /// 计算每个位置的最终位置
        /// </summary>
        /// <param name="InitialObject">第一个点是起点；其它点是终点</param>
        /// <returns></returns>
        public List<ProxiNode> FinalLocation_2(List<PointObject> InitialObject)
        {
            List<ProxiNode> FinalPoints = new List<ProxiNode>();
            PointObject StartPoint = InitialObject[0];  //start_Point
            double StartX = StartPoint.Point.X;
            double StartY = StartPoint.Point.Y;

            #region Get distance and travel time
            List<double> DisList = new List<double>();//存储目的地到起点的距离列表
            List<double> TimeList = new List<double>();//存储目的地到起点的时间列表

            for (int i = 1; i < InitialObject.Count; i++)//第一个点是起点，不纳入考虑！！
            {
                double Dis = Math.Sqrt((InitialObject[i].Point.X - StartX) * (InitialObject[i].Point.X - StartX) + (InitialObject[i].Point.Y - StartY) * (InitialObject[i].Point.Y - StartY));
                DisList.Add(Dis);
                TimeList.Add(InitialObject[i].TT);
            }
            #endregion

            #region GetFinalLocations
            double SumDis = DisList.Sum();
            double SumTime = TimeList.Sum();

            for (int i = 1; i < InitialObject.Count; i++)
            {

                double FinalDis = TimeList[i - 1] * SumDis / SumTime ;

                double X = StartX + (InitialObject[i].Point.X - StartX) * FinalDis / DisList[i - 1];
                double Y = StartY + (InitialObject[i].Point.Y - StartY) * FinalDis / DisList[i - 1];
                ProxiNode curVextex = new ProxiNode(X, Y, InitialObject[i].Point.ID, InitialObject[i].Point.TagValue);

                FinalPoints.Add(curVextex);
            }

            #endregion

            return FinalPoints;
        }

        /// 计算每个位置的最终位置
        /// </summary>
        /// <param name="InitialObject">第一个点是起点；其它点是终点</param>
        /// <returns></returns>
        public List<TriNode> FinalLocation_4(List<PointObject> InitialObject)
        {
            List<TriNode> FinalPoints = new List<TriNode>();
            PointObject StartPoint = InitialObject[0];  //start_Point
            double StartX = StartPoint.Point.X;
            double StartY = StartPoint.Point.Y;

            #region Get distance and travel time
            List<double> DisList = new List<double>();//存储目的地到起点的距离列表
            List<double> TimeList = new List<double>();//存储目的地到起点的时间列表

            for (int i = 1; i < InitialObject.Count; i++)//第一个点是起点，不纳入考虑！！
            {
                double Dis = Math.Sqrt((InitialObject[i].Point.X - StartX) * (InitialObject[i].Point.X - StartX) + (InitialObject[i].Point.Y - StartY) * (InitialObject[i].Point.Y - StartY));
                DisList.Add(Dis);
                TimeList.Add(InitialObject[i].TT);
            }
            #endregion

            #region GetFinalLocations
            double SumDis = DisList.Sum();
            double SumTime = TimeList.Sum();

            for (int i = 1; i < InitialObject.Count; i++)
            {

                double FinalDis = TimeList[i - 1] * SumDis / SumTime;

                double X = StartX + (InitialObject[i].Point.X - StartX) * FinalDis / DisList[i - 1];
                double Y = StartY + (InitialObject[i].Point.Y - StartY) * FinalDis / DisList[i - 1];
                TriNode curVextex = new TriNode(X, Y, InitialObject[i].Point.ID, InitialObject[i].Point.TagValue);

                double MoveDis = Math.Sqrt((X - InitialObject[i].Point.X) * (X - InitialObject[i].Point.X) + (Y - InitialObject[i].Point.Y) * (Y - InitialObject[i].Point.Y));
                curVextex.MoveDis = MoveDis;

                FinalPoints.Add(curVextex);
            }

            #endregion

            return FinalPoints;
        }


        /// 计算每个位置的最终位置
        /// </summary>
        /// <param name="InitialObject">第一个点是起点；其它点是终点</param>
        /// <returns></returns>
        public List<TriNode> FinalLocation_5(List<PointObject> InitialObject)
        {
            List<TriNode> FinalPoints = new List<TriNode>();
            PointObject StartPoint = InitialObject[0];  //start_Point
            double StartX = StartPoint.Point.X;
            double StartY = StartPoint.Point.Y;

            #region Get distance and travel time
            List<double> DisList = new List<double>();//存储目的地到起点的距离列表
            List<double> TimeList = new List<double>();//存储目的地到起点的时间列表

            for (int i = 1; i < InitialObject.Count; i++)//第一个点是起点，不纳入考虑！！
            {
                double Dis = Math.Sqrt((InitialObject[i].Point.X - StartX) * (InitialObject[i].Point.X - StartX) + (InitialObject[i].Point.Y - StartY) * (InitialObject[i].Point.Y - StartY));
                DisList.Add(Dis);
                TimeList.Add(InitialObject[i].TT);
            }
            #endregion

            #region GetFinalLocations
            double SumDis = DisList.Sum();
            double SumTime = TimeList.Sum();

            TriNode cacheStartPoint = new TriNode(StartPoint.Point.X, StartPoint.Point.Y, StartPoint.Point.ID, StartPoint.Point.TagValue);
            FinalPoints.Add(cacheStartPoint);
            for (int i = 1; i < InitialObject.Count; i++)
            {

                double FinalDis = TimeList[i - 1] * SumDis / SumTime;

                double X = StartX + (InitialObject[i].Point.X - StartX) * FinalDis / DisList[i - 1];
                double Y = StartY + (InitialObject[i].Point.Y - StartY) * FinalDis / DisList[i - 1];
                TriNode curVextex = new TriNode(X, Y, InitialObject[i].Point.ID, InitialObject[i].Point.TagValue);

                double MoveDis = Math.Sqrt((X - InitialObject[i].Point.X) * (X - InitialObject[i].Point.X) + (Y - InitialObject[i].Point.Y) * (Y - InitialObject[i].Point.Y));
                curVextex.MoveDis = MoveDis;

                FinalPoints.Add(curVextex);
            }

            #endregion

            return FinalPoints;
        }

        /// <summary>
        /// 计算每个位置的最终位置
        /// </summary>
        /// <param name="InitialObject">第一个点是起点；其它点是终点</param>
        /// <returns></returns>
        public List<ProxiNode> FinalLocation_3(List<PointObject> InitialObject)
        {
            List<ProxiNode> FinalPoints = new List<ProxiNode>();

            for (int i = 0; i < InitialObject.Count; i++)
            {
                ProxiNode curVextex = new ProxiNode(InitialObject[i].Point.X, InitialObject[i].Point.Y, i + 40, i + 1);
                FinalPoints.Add(curVextex);
            }

            return FinalPoints;
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

        /// Beams Displace
        /// </summary>
        /// <param name="pg">邻近图</param>
        /// <param name="pMap">Dorling图层</param>
        /// <param name="scale">比例尺</param>
        /// <param name="E">弹性模量</param>
        /// <param name="I">惯性力矩</param>
        /// <param name="A">横切面积</param>
        /// MinDis 起点和终点之间的距离误差
        /// StopT 迭代终止的最大力大小
        /// <param name="Iterations">迭代次数</param>
        public void CTPBeams(ProxiGraph pg, List<ProxiNode> FinalLocation, double scale, double E, double I, double A, int Iterations, int algType,double MinDis, double StopT)
        {
            AlgBeams algBeams = new AlgBeams(pg, E, I, A);
            //求吸引力-2014-3-20所用

            ProxiGraph CopyG = Clone((object)pg) as ProxiGraph;
            algBeams.OriginalGraph = CopyG;
            algBeams.Scale = scale;
            algBeams.AlgType = algType;
            for (int i = 0; i < Iterations; i++)//迭代计算
            {
                Console.WriteLine(i.ToString());//标识

                algBeams.DoDisplacePgCTP(pg.NodeList, FinalLocation,MinDis,StopT);// 调用Beams算法 

                this.continueLable = algBeams.isContinue;
                if (algBeams.isContinue == false)
                {
                    break;
                }
            }
        }
        
        /// Beams Displace
        /// </summary>
        /// <param name="pg">邻近图</param>
        /// <param name="pMap">Dorling图层</param>
        /// <param name="scale">比例尺</param>
        /// <param name="E">弹性模量</param>
        /// <param name="I">惯性力矩</param>
        /// <param name="A">横切面积</param>
        /// MinDis 起点和终点之间的距离误差
        /// StopT 迭代终止的最大力大小
        /// <param name="Iterations">迭代次数</param>
        /// <param name="MaxForce">用于限制计算两个力时的最大力</param>
        /// <param name="MaxForce_2">用于限制全力力时的最大力</param> 
        public void CTPSnake(ProxiGraph pg, List<ProxiNode> FinalLocation, double scale, double a, double b, int Iterations, int algType, double MinDis, double StopT,double MaxForce,double MaxForce_2)
        {
            AlgSnake algSnakes = new AlgSnake(pg, a, b);
            //求吸引力-2014-3-20所用
            ProxiGraph CopyG = Clone((object)pg) as ProxiGraph;
            algSnakes.OriginalGraph = CopyG;
            algSnakes.Scale = scale;//目标比例尺
            algSnakes.AlgType = algType;//0-Snakes, 1-Sequence, 2-Combined
            //1- Sequence：几何算法。
            //2-Combined：混合算法，当目标个数大于3用Snakes，否则用几何算法
            for (int i = 0; i < Iterations; i++)//迭代计算
            {
                Console.WriteLine(i.ToString());//标识

                algSnakes.DoDispacePG_CTP(FinalLocation, MinDis, MaxForce, MaxForce_2);//执行邻近图Beams移位算法，代表混合型

                this.continueLable = algSnakes.isContinue;
                if (algSnakes.isContinue == false)
                {
                    break;
                }
            }        
        }

        /// Beams Displace
        /// </summary>
        /// <param name="pg">邻近图</param>
        /// <param name="pMap">Dorling图层</param>
        /// <param name="scale">比例尺</param>
        /// <param name="E">弹性模量</param>
        /// <param name="I">惯性力矩</param>
        /// <param name="A">横切面积</param>
        /// MinDis 起点和终点之间的距离误差
        /// StopT 迭代终止的最大力大小
        /// <param name="Iterations">迭代次数</param>
        /// <param name="MaxForce">用于限制计算两个力时的最大力</param>
        /// <param name="MaxForce_2">用于限制全力力时的最大力</param>
        public void CTPSnake_PgReBuilt(ProxiGraph pg, List<ProxiNode> FinalLocation, double scale, double a, double b, int Iterations, int algType, double MinDis, double StopT, string OutFilePath, IMap pMap,double MaxForce,double MaxForce_2)
        {
            AlgSnake algSnakes = new AlgSnake(pg, a, b);
            //求吸引力-2014-3-20所用
            ProxiGraph CopyG = Clone((object)pg) as ProxiGraph;
            algSnakes.OriginalGraph = CopyG;
            algSnakes.Scale = scale;//目标比例尺
            algSnakes.AlgType = algType;//0-Snakes, 1-Sequence, 2-Combined
            //1- Sequence：几何算法。
            //2-Combined：混合算法，当目标个数大于3用Snakes，否则用几何算法

            for (int i = 0; i < Iterations; i++)//迭代计算
            {
                Console.WriteLine(i.ToString());//标识

                algSnakes.DoDispacePG_CTP(FinalLocation, MinDis,MaxForce,MaxForce_2);//执行邻近图Beams移位算法，代表混合型

                #region 邻近图重构
                List<TriNode> CacheNodeList = new List<TriNode>();
                for (int j = 0; j < algSnakes.ProxiGraph.NodeList.Count; j++)
                {
                    TriNode CacheNode = new TriNode(algSnakes.ProxiGraph.NodeList[j].X, algSnakes.ProxiGraph.NodeList[j].Y, algSnakes.ProxiGraph.NodeList[j].ID, algSnakes.ProxiGraph.NodeList[j].TagID);
                    CacheNode.FeatureType = algSnakes.ProxiGraph.NodeList[j].FeatureType;
                    CacheNodeList.Add(CacheNode);
                }

                DelaunayTin dt = new DelaunayTin(CacheNodeList); ///Dt中节点的ID和Map.PointList中节点的ID是一样的
                dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);
                dt.CreateRNG();
                ProxiGraph CachePg = new ProxiGraph(dt.RNGNodeList, dt.RNGEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的
                CachePg.WriteProxiGraph2Shp(OutFilePath, "pG_" + i.ToString(), pMap.SpatialReference);

                algSnakes.ProxiGraph = CachePg;
                #endregion

                this.continueLable = algSnakes.isContinue;
                if (algSnakes.isContinue == false)
                {
                    break;
                }
            }
        }

        /// Beams Displace
        /// </summary>
        /// <param name="pg">邻近图</param>
        /// <param name="pMap">Dorling图层</param>
        /// <param name="scale">比例尺</param>
        /// <param name="E">弹性模量</param>
        /// <param name="I">惯性力矩</param>
        /// <param name="A">横切面积</param>
        /// MinDis 起点和终点之间的距离误差
        /// StopT 迭代终止的最大力大小
        /// <param name="Iterations">迭代次数</param>
        /// <param name="MaxForce">用于限制计算两个力时的最大力</param>
        /// <param name="MaxForce_2">用于限制全力力时的最大力</param>
        public void CTPSnake_PgReBuiltMapIn(ProxiGraph pg, List<ProxiNode> FinalLocation,SMap sMap, double scale, double a, double b, int Iterations, int algType, double MinDis, double StopT, string OutFilePath, IMap pMap,double MaxForce,double MaxForce_2)
        {
            AlgSnake algSnakes = new AlgSnake(pg, sMap, a, b);
            //求吸引力-2014-3-20所用
            ProxiGraph CopyG = Clone((object)pg) as ProxiGraph;
            algSnakes.OriginalGraph = CopyG;
            algSnakes.Scale = scale;//目标比例尺
            algSnakes.AlgType = algType;//0-Snakes, 1-Sequence, 2-Combined
            //1- Sequence：几何算法。
            //2-Combined：混合算法，当目标个数大于3用Snakes，否则用几何算法

            for (int i = 0; i < Iterations; i++)//迭代计算
            {
                Console.WriteLine(i.ToString());//标识

                algSnakes.DoDispacePG_CTP(FinalLocation, sMap, MinDis, MaxForce, MaxForce_2);//执行邻近图Beams移位算法，代表混合型

                #region 邻近图重构
                List<TriNode> CacheNodeList = new List<TriNode>();
                for (int j = 0; j < algSnakes.ProxiGraph.NodeList.Count; j++)
                {
                    TriNode CacheNode = new TriNode(algSnakes.ProxiGraph.NodeList[j].X, algSnakes.ProxiGraph.NodeList[j].Y, algSnakes.ProxiGraph.NodeList[j].ID, algSnakes.ProxiGraph.NodeList[j].TagID);
                    CacheNode.FeatureType = algSnakes.ProxiGraph.NodeList[j].FeatureType;
                    CacheNodeList.Add(CacheNode);
                }

                DelaunayTin dt = new DelaunayTin(CacheNodeList); ///Dt中节点的ID和Map.PointList中节点的ID是一样的
                dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);

                //ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
                //cdt.CreateConsDTfromPolylineandPolygon(null, sMap.PolygonList);
                //ProxiGraph CachePg = new ProxiGraph(cdt.TriNodeList, cdt.TriEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的

                dt.CreateRNG();
                ProxiGraph CachePg = new ProxiGraph(dt.RNGNodeList, dt.RNGEdgeList); 
                
                CachePg.WriteProxiGraph2Shp(OutFilePath, "pG_" + i.ToString(), pMap.SpatialReference);
                //pg.WriteProxiGraph2Shp(OutFilePath, "pG_" + i.ToString(), pMap.SpatialReference);
                sMap.WriteResult2Shp(OutFilePath, "Map_" + i.ToString(), pMap.SpatialReference);

                algSnakes.ProxiGraph = CachePg;
                #endregion

                this.continueLable = algSnakes.isContinue;
                if (algSnakes.isContinue == false)
                {
                    break;
                }
            }
        }

        /// Beams Displace
        /// </summary>
        /// <param name="pg">邻近图</param>
        /// <param name="pMap">Dorling图层</param>
        /// <param name="scale">比例尺</param>
        /// <param name="E">弹性模量</param>
        /// <param name="I">惯性力矩</param>
        /// <param name="A">横切面积</param>
        /// MinDis 起点和终点之间的距离误差
        /// StopT 迭代终止的最大力大小
        /// <param name="Iterations">迭代次数</param>
        /// <param name="MaxForce">用于限制计算两个力时的最大力</param>
        /// <param name="MaxForce_2">用于限制全力力时的最大力</param>
        public void CTPSnake_PgReBuiltMapIn_BdMain(ProxiGraph pg, List<ProxiNode> FinalLocation, SMap sMap, double scale, double a, double b, int Iterations, int algType, double MinDis, double StopT, string OutFilePath, IMap pMap, double MaxForce,bool PgRefine,double MaxForce_2)
        {
            AlgSnake algSnakes = new AlgSnake(pg, sMap, a, b);
            //求吸引力-2014-3-20所用
            ProxiGraph CopyG = Clone((object)pg) as ProxiGraph;
            algSnakes.OriginalGraph = CopyG;
            algSnakes.Scale = scale;//目标比例尺
            algSnakes.AlgType = algType;//0-Snakes, 1-Sequence, 2-Combined
            //1- Sequence：几何算法。
            //2-Combined：混合算法，当目标个数大于3用Snakes，否则用几何算法

            for (int i = 0; i < Iterations; i++)//迭代计算
            {
                Console.WriteLine(i.ToString());

                if (!PgRefine)
                {
                    algSnakes.DoDispacePG_CTP(FinalLocation, sMap, MinDis, MaxForce,MaxForce_2);//执行邻近图Beams移位算法，代表混合型
                }

                else
                {
                    algSnakes.DoDispacePG_CTP_AdpPg(FinalLocation, sMap, MinDis, MaxForce, MaxForce_2);
                }

                algSnakes.ProxiGraph.WriteProxiGraph2Shp(OutFilePath, "pG_" + i.ToString(), pMap.SpatialReference);

                #region 依据拓扑关系更新Map和邻近图重构
                this.BoundaryMaintain(algSnakes.ProxiGraph, sMap,1);
                sMap.WriteResult2Shp(OutFilePath, "Map_" + i.ToString(), pMap.SpatialReference);
                DelaunayTin dt = new DelaunayTin(sMap.TriNodeList); ///Dt中节点的ID和Map.PointList中节点的ID是一样的
                dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);
                //dt.CreateMST();
                //ProxiGraph CachePg = new ProxiGraph(dt.MSTNodeList, dt.MSTEdgeList);

                dt.CreateRNG();
                ProxiGraph CachePg = new ProxiGraph(dt.RNGNodeList, dt.RNGEdgeList); 

                //ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
                //cdt.CreateConsDTfromPolylineandPolygon(null, sMap.PolygonList);
                //ProxiGraph CachePg = new ProxiGraph(cdt.TriNodeList, cdt.TriEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的
                //CachePg.WriteProxiGraph2Shp(OutFilePath, "pG_" + i.ToString(), pMap.SpatialReference);
                //pg.WriteProxiGraph2Shp(OutFilePath, "pG_" + i.ToString(), pMap.SpatialReference);
                //sMap.WriteResult2Shp(OutFilePath, "Map_" + i.ToString(), pMap.SpatialReference);
                #endregion

                algSnakes.ProxiGraph = CachePg;
                this.continueLable = algSnakes.isContinue;
                if (algSnakes.isContinue == false)
                {
                    break;
                }
            }
        }

        /// Beams Displace
        /// </summary>
        /// <param name="pg">邻近图</param>
        /// <param name="pMap">Dorling图层</param>
        /// <param name="scale">比例尺</param>
        /// <param name="E">弹性模量</param>
        /// <param name="I">惯性力矩</param>
        /// <param name="A">横切面积</param>
        /// MinDis 起点和终点之间的距离误差
        /// StopT 迭代终止的最大力大小
        /// <param name="Iterations">迭代次数</param>
        public void CTPSnake_PgReBuiltHierMapIn(ProxiGraph pg, List<ProxiNode> FinalLocation, SMap sMap, double scale, double a, double b, int Iterations, int algType, double MinDis, double StopT, string OutFilePath, IMap pMap,double MaxForce,double ForceRate)
        {
            AlgSnake algSnakes = new AlgSnake(pg, sMap, a, b);
            //求吸引力-2014-3-20所用
            ProxiGraph CopyG = Clone((object)pg) as ProxiGraph;
            algSnakes.OriginalGraph = CopyG;
            algSnakes.Scale = scale;//目标比例尺
            algSnakes.AlgType = algType;//0-Snakes, 1-Sequence, 2-Combined
            //1- Sequence：几何算法。
            //2-Combined：混合算法，当目标个数大于3用Snakes，否则用几何算法

            for (int i = 0; i < Iterations; i++)//迭代计算
            {
                Console.WriteLine(i.ToString());//标识

                //if (i < 50)
                //{
                //    algSnakes.DoDispacePG_CTP(FinalLocation, sMap, MinDis);//执行邻近图Beams移位算法，代表混合型

                //    #region 邻近图重构
                //    List<TriNode> CacheNodeList = new List<TriNode>();
                //    for (int j = 0; j < algSnakes.ProxiGraph.NodeList.Count; j++)
                //    {
                //        TriNode CacheNode = new TriNode(algSnakes.ProxiGraph.NodeList[j].X, algSnakes.ProxiGraph.NodeList[j].Y, algSnakes.ProxiGraph.NodeList[j].ID, algSnakes.ProxiGraph.NodeList[j].TagID);
                //        CacheNode.FeatureType = algSnakes.ProxiGraph.NodeList[j].FeatureType;
                //        CacheNodeList.Add(CacheNode);
                //    }

                //    DelaunayTin dt = new DelaunayTin(CacheNodeList); ///Dt中节点的ID和Map.PointList中节点的ID是一样的
                //    dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);
                //    //dt.CreateRNG();
                //    ProxiGraph CachePg = new ProxiGraph(dt.TriNodeList, dt.TriEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的
                //    CachePg.WriteProxiGraph2Shp(OutFilePath, "pG_" + i.ToString(), pMap.SpatialReference);
                //    //pg.WriteProxiGraph2Shp(OutFilePath, "pG_" + i.ToString(), pMap.SpatialReference);
                //    sMap.WriteResult2Shp(OutFilePath, "Map_" + i.ToString(), pMap.SpatialReference);

                //    //algSnakes.ProxiGraph = CachePg;
                //    #endregion
                //}

                //else
                //{
                algSnakes.DoDispacePG_HierCTP(FinalLocation, sMap, MinDis, MaxForce,ForceRate);//执行邻近图Beams移位算法，代表混合型

                    #region 邻近图重构
                    List<TriNode> CacheNodeList = new List<TriNode>();
                    for (int j = 0; j < algSnakes.ProxiGraph.NodeList.Count; j++)
                    {
                        TriNode CacheNode = new TriNode(algSnakes.ProxiGraph.NodeList[j].X, algSnakes.ProxiGraph.NodeList[j].Y, algSnakes.ProxiGraph.NodeList[j].ID, algSnakes.ProxiGraph.NodeList[j].TagID);
                        CacheNode.FeatureType = algSnakes.ProxiGraph.NodeList[j].FeatureType;
                        CacheNodeList.Add(CacheNode);
                    }

                    DelaunayTin dt = new DelaunayTin(CacheNodeList); ///Dt中节点的ID和Map.PointList中节点的ID是一样的
                    dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);
                    //dt.CreateRNG();
                    ProxiGraph CachePg = new ProxiGraph(dt.TriNodeList, dt.TriEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的
                    CachePg.WriteProxiGraph2Shp(OutFilePath, "pG_" + i.ToString(), pMap.SpatialReference);
                    //pg.WriteProxiGraph2Shp(OutFilePath, "pG_" + i.ToString(), pMap.SpatialReference);
                    sMap.WriteResult2Shp(OutFilePath, "Map_" + i.ToString(), pMap.SpatialReference);

                    algSnakes.ProxiGraph = CachePg;
                    #endregion
                //}

                this.continueLable = algSnakes.isContinue;
                if (algSnakes.isContinue == false)
                {
                    break;
                }
            }
        }

        /// Beams Displace
        /// </summary>
        /// <param name="pg">邻近图</param>
        /// <param name="pMap">Dorling图层</param>
        /// <param name="scale">比例尺</param>
        /// <param name="E">弹性模量</param>
        /// <param name="I">惯性力矩</param>
        /// <param name="A">横切面积</param>
        /// MinDis 起点和终点之间的距离误差
        /// StopT 迭代终止的最大力大小
        /// <param name="Iterations">迭代次数</param>
        public void CTPBeams_2(ProxiGraph pg, List<ProxiNode> FinalLocation, double scale, double E, double I, double A, int Iterations, int algType, double MinDis, double StopT)
        {
            AlgBeams algBeams = new AlgBeams(pg, E, I, A);
            //求吸引力-2014-3-20所用

            ProxiGraph CopyG = Clone((object)pg) as ProxiGraph;
            algBeams.OriginalGraph = CopyG;
            algBeams.Scale = scale;
            algBeams.AlgType = algType;
            for (int i = 0; i < Iterations; i++)//迭代计算
            {
                Console.WriteLine(i.ToString());//标识

                algBeams.DoDisplacePgCTP_2(pg.NodeList, FinalLocation, MinDis, StopT);// 调用Beams算法 

                this.continueLable = algBeams.isContinue;
                if (algBeams.isContinue == false)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 将点都保持在边界中
        /// 依据该约束条件更新最新的Map(依据延长线更新)
        /// </summary>
        /// <param name="pg"></param>
        /// <param name="Map"></param>
        ///  ExtendLabel=1单边延长； ExtendLabel=2双边延长； ExtendLabel=3不延长
        public void BoundaryMaintain(ProxiGraph pg, SMap Map,int ExtendLabel)
        {
            AlgSnake AS = new AlgSnake();

            #region 获取pg中在Map中PolygonObject外的点
            List<ProxiNode> InPolygonNodeList = new List<ProxiNode>();
            for (int i = 0; i < pg.NodeList.Count; i++)
            {
                if (pg.NodeList[i].FeatureType == FeatureType.PointType)//判断是否是非边界点（不是边缘上的点）
                {
                    TriNode CacheTriNode = new TriNode(pg.NodeList[i].X, pg.NodeList[i].Y, pg.NodeList[i].ID, pg.NodeList[i].TagID);
                    if (!ComFunLib.IsPointinPolygon(CacheTriNode, Map.PolygonList[0].PointList))//IsPointinPolygon静态函数
                    {
                        InPolygonNodeList.Add(pg.NodeList[i]);
                    }
                }
            }
            #endregion

            Console.WriteLine(InPolygonNodeList.Count.ToString());
            #region 依据在PolygonObject中外的点更新Map(更新点、插入点)
            for (int i = 0; i < InPolygonNodeList.Count; i++)
            {
                ProxiNode curNode = InPolygonNodeList[i];
                TriNode CacheTriNode = AS.GetPointByID(curNode.ID, Map.TriNodeList);           

                List<List<TriNode>> CrossEdges = this.CrossEdges(CacheTriNode, Map.PolygonList[0].PointList,ExtendLabel);
                if (CrossEdges != null)
                {
                    Tuple<List<TriNode>, TriNode> NearCrossEdge = this.NearCrossEdge(CacheTriNode, CrossEdges, ExtendLabel);

                    if (NearCrossEdge != null)
                    {
                        #region 更新边界上的点
                        TriNode NewMapBDNode = new TriNode(CacheTriNode.X, CacheTriNode.Y, -1);//对原有的边界外点做备份
                        NewMapBDNode.TagValue = Map.PolygonList[0].PointList[0].TagValue;

                        int Test1 = Map.PolygonList[0].PointList.IndexOf(NearCrossEdge.Item1[0]);
                        int Test2 = Map.PolygonList[0].PointList.IndexOf(NearCrossEdge.Item1[1]);
                        int Test3 = Map.TriNodeList.IndexOf(NearCrossEdge.Item1[0]);
                        int Test4 = Map.TriNodeList.IndexOf(NearCrossEdge.Item1[1]);

                        Map.PolygonList[0].PointList.Insert(Map.PolygonList[0].PointList.IndexOf(NearCrossEdge.Item1[0]) + 1, NewMapBDNode);//更新边界上的点（Polygon中的PointList）
                        Map.TriNodeList.Insert(Map.TriNodeList.IndexOf(NearCrossEdge.Item1[0]) + 1, NewMapBDNode);//更新边界上的点（TriNodes）
                        #endregion

                        #region 更新非边界上的点
                        CacheTriNode.X = NearCrossEdge.Item2.X;//更新非边界上点(TriNodeList)
                        CacheTriNode.Y = NearCrossEdge.Item2.Y;

                        PointObject CachePointObject = AS.GetPointObjectByID(curNode.TagID, Map.PointList);////更新非边界上点(Map中的PointList)
                        CachePointObject.Point = CacheTriNode;
                        #endregion                 
                    }
                }
            }
            #endregion

            #region 更新Map中目标的IDs，更新PointObjects
            int VertexID = 0;
            for (int j = 0; j < Map.PolygonList[0].PointList.Count; j++)//更新Polygon中PointList的ID
            {
                Map.PolygonList[0].PointList[j].ID = VertexID;
                VertexID++;
            }

            for (int j = 0; j < Map.TriNodeList.Count; j++)//更新Map TriNodeList的ID
            {
                Map.TriNodeList[j].ID = j;
            }
            #endregion
        }

        /// <summary>
        /// 将点都保持在边界中
        /// 依据该约束条件更新最新的Map（依据最邻近点更新）
        /// </summary>
        /// <param name="pg"></param>
        /// <param name="Map"></param>
        public void BoundaryMaintain(ProxiGraph pg, SMap Map)
        {
            AlgSnake AS = new AlgSnake();

            #region 获取pg中在Map中PolygonObject外的点
            List<ProxiNode> InPolygonNodeList = new List<ProxiNode>();
            for (int i = 0; i < pg.NodeList.Count; i++)
            {
                if (pg.NodeList[i].FeatureType == FeatureType.PointType)//判断是否是非边界点（不是边缘上的点）
                {
                    TriNode CacheTriNode = new TriNode(pg.NodeList[i].X, pg.NodeList[i].Y, pg.NodeList[i].ID, pg.NodeList[i].TagID);
                    if (!ComFunLib.IsPointinPolygon(CacheTriNode, Map.PolygonList[0].PointList))//IsPointinPolygon静态函数
                    {
                        InPolygonNodeList.Add(pg.NodeList[i]);
                    }
                }
            }
            #endregion

            Console.WriteLine(InPolygonNodeList.Count.ToString());
            #region 依据在PolygonObject中外的点更新Map(更新点、插入点)
            for (int i = 0; i < InPolygonNodeList.Count; i++)
            {
                ProxiNode curNode = InPolygonNodeList[i];
                TriNode CacheTriNode = AS.GetPointByID(curNode.ID, Map.TriNodeList);

                TriNode NearNode = this.GetNearestNode(CacheTriNode, Map.PolygonList[0]);

                if (NearNode != null)
                {
                    List<TriNode> OnLine = this.GetOnLineEdge(NearNode, Map.PolygonList[0]);
                    if (OnLine.Count > 0)
                    {

                        #region 更新边界上的点
                        TriNode NewMapBDNode = new TriNode(CacheTriNode.X, CacheTriNode.Y, -1);//对原有的边界外点做备份
                        NewMapBDNode.TagValue = Map.PolygonList[0].PointList[0].TagValue;

                        //int Test1 = Map.PolygonList[0].PointList.IndexOf(NearCrossEdge.Item1[0]);
                        //int Test2 = Map.PolygonList[0].PointList.IndexOf(NearCrossEdge.Item1[1]);
                        //int Test3 = Map.TriNodeList.IndexOf(NearCrossEdge.Item1[0]);
                        //int Test4 = Map.TriNodeList.IndexOf(NearCrossEdge.Item1[1]);

                        Map.PolygonList[0].PointList.Insert(Map.PolygonList[0].PointList.IndexOf(OnLine[0]) + 1, NewMapBDNode);//更新边界上的点（Polygon中的PointList）
                        Map.TriNodeList.Insert(Map.TriNodeList.IndexOf(OnLine[0]) + 1, NewMapBDNode);//更新边界上的点（TriNodes）
                        #endregion

                        #region 更新非边界上的点
                        CacheTriNode.X = NearNode.X;//更新非边界上点(TriNodeList)
                        CacheTriNode.Y = NearNode.Y;

                        PointObject CachePointObject = AS.GetPointObjectByID(curNode.TagID, Map.PointList);////更新非边界上点(Map中的PointList)
                        CachePointObject.Point = CacheTriNode;
                        #endregion
                    }
                }
            }
            #endregion

            #region 更新Map中目标的IDs，更新PointObjects
            int VertexID = 0;
            for (int j = 0; j < Map.PolygonList[0].PointList.Count; j++)//更新Polygon中PointList的ID
            {
                Map.PolygonList[0].PointList[j].ID = VertexID;
                VertexID++;
            }

            for (int j = 0; j < Map.TriNodeList.Count; j++)//更新Map TriNodeList的ID
            {
                Map.TriNodeList[j].ID = j;
            }
            #endregion
        }

        /// <summary>
        /// 计算给定的偏移点（在制定方向上）与建筑物的交点（仅延长一边的交点）
        /// 方向=偏移方向（偏移后位置相对于初始位置）
        /// </summary>
        /// <param name="OutNode"></param>
        /// <param name="PolygonNodes"></param>
        /// ExtendLabel=1 单边延长；EdtendLabel=2 双边延长； ExtendLabel=3不延长
        /// <returns></returns>
        public List<List<TriNode>> CrossEdges(TriNode OutNode, List<TriNode> PolygonNodes,int ExtendLabel)
        {

            if (PolygonNodes.Count > 0)
            {
                //TriNode InitialNode = new TriNode(OutNode.Initial_X, OutNode.Initial_Y);//可能无交点，故取延长线
                #region 不延长
                if (ExtendLabel == 3)
                {
                    double Extend_X = OutNode.Initial_X ;
                    double Extend_Y = OutNode.Initial_Y ;
                    TriNode InitialNode = new TriNode(Extend_X, Extend_Y);//可能无交点，故取延长线

                    #region 可视化显示（测试用）
                    IPoint StartPoint = new PointClass(); IPoint EndPoint = new PointClass();
                    StartPoint.X = InitialNode.X; StartPoint.Y = InitialNode.Y;
                    EndPoint.X = OutNode.X; EndPoint.Y = OutNode.Y;
                    IPolyline ParaLine = new PolylineClass();
                    ParaLine.FromPoint = StartPoint; ParaLine.ToPoint = EndPoint;

                    object PolygonSymbol = Sb.LineSymbolization(1, 100, 100, 100, 0);
                    mCon.DrawShape(ParaLine, ref PolygonSymbol);
                    #endregion

                    List<List<TriNode>> CrossEdges = new List<List<TriNode>>();
                    for (int i = 0; i < PolygonNodes.Count; i++)
                    {
                        #region 是多边形的最后一个点
                        if (i == PolygonNodes.Count - 1)
                        {
                            if (ComFunLib.IsLineSegCross(InitialNode, OutNode, PolygonNodes[i], PolygonNodes[0]))
                            {
                                List<TriNode> CacheNodes = new List<TriNode>();
                                CacheNodes.Add(PolygonNodes[i]);
                                CacheNodes.Add(PolygonNodes[0]);
                                CrossEdges.Add(CacheNodes);
                            }
                        }
                        #endregion

                        #region 是多边形的最后一个点
                        else
                        {
                            if (ComFunLib.IsLineSegCross(InitialNode, OutNode, PolygonNodes[i], PolygonNodes[i + 1]))
                            {
                                List<TriNode> CacheNodes = new List<TriNode>();
                                CacheNodes.Add(PolygonNodes[i]);
                                CacheNodes.Add(PolygonNodes[i + 1]);
                                CrossEdges.Add(CacheNodes);
                            }
                        }
                        #endregion
                    }

                    return CrossEdges;
                }
                #endregion

                #region 单边延长线
                else if (ExtendLabel == 1)
                {
                    double Extend_X = OutNode.Initial_X - 100 * (OutNode.X - OutNode.Initial_X);
                    double Extend_Y = OutNode.Initial_Y - 100 * (OutNode.Y - OutNode.Initial_Y);
                    TriNode InitialNode = new TriNode(Extend_X, Extend_Y);//可能无交点，故取延长线

                    #region 可视化显示（测试用）
                    IPoint StartPoint = new PointClass(); IPoint EndPoint = new PointClass();
                    StartPoint.X = InitialNode.X; StartPoint.Y = InitialNode.Y;
                    EndPoint.X = OutNode.X; EndPoint.Y = OutNode.Y;
                    IPolyline ParaLine = new PolylineClass();
                    ParaLine.FromPoint = StartPoint; ParaLine.ToPoint = EndPoint;

                    object PolygonSymbol = Sb.LineSymbolization(1, 100, 100, 100, 0);
                    mCon.DrawShape(ParaLine, ref PolygonSymbol);
                    #endregion

                    List<List<TriNode>> CrossEdges = new List<List<TriNode>>();
                    for (int i = 0; i < PolygonNodes.Count; i++)
                    {
                        #region 是多边形的最后一个点
                        if (i == PolygonNodes.Count - 1)
                        {
                            if (ComFunLib.IsLineSegCross(InitialNode, OutNode, PolygonNodes[i], PolygonNodes[0]))
                            {
                                List<TriNode> CacheNodes = new List<TriNode>();
                                CacheNodes.Add(PolygonNodes[i]);
                                CacheNodes.Add(PolygonNodes[0]);
                                CrossEdges.Add(CacheNodes);
                            }
                        }
                        #endregion

                        #region 是多边形的最后一个点
                        else
                        {
                            if (ComFunLib.IsLineSegCross(InitialNode, OutNode, PolygonNodes[i], PolygonNodes[i + 1]))
                            {
                                List<TriNode> CacheNodes = new List<TriNode>();
                                CacheNodes.Add(PolygonNodes[i]);
                                CacheNodes.Add(PolygonNodes[i + 1]);
                                CrossEdges.Add(CacheNodes);
                            }
                        }
                        #endregion
                    }

                    return CrossEdges;
                }
                #endregion

                #region 双边延长线
                else if (ExtendLabel == 2)
                {
                    List<TriNode> ExtendLine = ComFunLib.GetExtendingLine(OutNode);

                    #region 可视化显示（测试用）
                    IPoint StartPoint = new PointClass(); IPoint EndPoint = new PointClass();
                    StartPoint.X = ExtendLine[0].X; StartPoint.Y = ExtendLine[0].Y;
                    EndPoint.X = ExtendLine[1].X; EndPoint.Y = ExtendLine[1].Y;
                    IPolyline ParaLine = new PolylineClass();
                    ParaLine.FromPoint = StartPoint; ParaLine.ToPoint = EndPoint;

                    object PolygonSymbol = Sb.LineSymbolization(1, 100, 100, 100, 0);
                    mCon.DrawShape(ParaLine, ref PolygonSymbol);
                    #endregion

                    List<List<TriNode>> CrossEdges = new List<List<TriNode>>();
                    for (int i = 0; i < PolygonNodes.Count; i++)
                    {
                        #region 是多边形的最后一个点
                        if (i == PolygonNodes.Count - 1)
                        {
                            if (ComFunLib.IsLineSegCross(ExtendLine[0], ExtendLine[1], PolygonNodes[i], PolygonNodes[0]))
                            {
                                List<TriNode> CacheNodes = new List<TriNode>();
                                CacheNodes.Add(PolygonNodes[i]);
                                CacheNodes.Add(PolygonNodes[0]);
                                CrossEdges.Add(CacheNodes);
                            }
                        }
                        #endregion

                        #region 是多边形的最后一个点
                        else
                        {
                            if (ComFunLib.IsLineSegCross(ExtendLine[0], ExtendLine[1], PolygonNodes[i], PolygonNodes[i + 1]))
                            {
                                List<TriNode> CacheNodes = new List<TriNode>();
                                CacheNodes.Add(PolygonNodes[i]);
                                CacheNodes.Add(PolygonNodes[i + 1]);
                                CrossEdges.Add(CacheNodes);
                            }
                        }
                        #endregion
                    }

                    return CrossEdges;
                }
                #endregion

                else
                {
                    return null;
                }
            }

            else
            {
                return null;
            }
        }

        /// <summary>
        /// 获得给定相交边中与节点最邻近的点和该点所在的边（仅延长一边的交点）
        /// </summary>
        /// <param name="OutNode"></param>
        /// <param name="CrossEdges"></param>
        /// <returns></returns>
        public Tuple<List<TriNode>,TriNode> NearCrossEdge(TriNode OutNode, List<List<TriNode>> CrossEdges,int ExtendLabel)
        {
            if (CrossEdges.Count > 0)
            {
                List<Tuple<List<TriNode>, TriNode>> CrossNodeEdges = new List<Tuple<List<TriNode>, TriNode>>();

                #region 求交点
                //TriNode InitialNode = new TriNode(OutNode.Initial_X, OutNode.Initial_Y);
                #region 不延长
                if (ExtendLabel == 3)
                {
                    #region 获得延长线顶点
                    double Extend_X = OutNode.Initial_X ;
                    double Extend_Y = OutNode.Initial_Y ;
                    TriNode InitialNode = new TriNode(Extend_X, Extend_Y);//可能无交点，故取延长线
                    #endregion

                    for (int i = 0; i < CrossEdges.Count; i++)
                    {
                        TriNode CrossNode = ComFunLib.CrossNode(InitialNode, OutNode, CrossEdges[i][0], CrossEdges[i][1]);

                        if (CrossNode != null)
                        {
                            CrossNode.ID = -1;//ID=-1表示交叉后添加的点
                            Tuple<List<TriNode>, TriNode> CrossNodeEdge = new Tuple<List<TriNode>, TriNode>(CrossEdges[i], CrossNode);
                            CrossNodeEdges.Add(CrossNodeEdge);
                        }
                    }
                }
                #endregion

                #region 双边延长线
                if (ExtendLabel == 2)
                {
                     List<TriNode> ExtendLine = ComFunLib.GetExtendingLine(OutNode);//获得延长线顶点

                    for (int i = 0; i < CrossEdges.Count; i++)
                    {
                        TriNode CrossNode = ComFunLib.CrossNode(ExtendLine[0], ExtendLine[1], CrossEdges[i][0], CrossEdges[i][1]);

                        if (CrossNode != null)
                        {
                            CrossNode.ID = -1;//ID=-1表示交叉后添加的点
                            Tuple<List<TriNode>, TriNode> CrossNodeEdge = new Tuple<List<TriNode>, TriNode>(CrossEdges[i], CrossNode);
                            CrossNodeEdges.Add(CrossNodeEdge);
                        }
                    }

                }
                #endregion

                #region 单边延长线
                if (ExtendLabel == 1)
                {
                    #region 获得延长线顶点
                    double Extend_X = OutNode.Initial_X - 100 * (OutNode.X - OutNode.Initial_X);
                    double Extend_Y = OutNode.Initial_Y - 100 * (OutNode.Y - OutNode.Initial_Y);
                    TriNode InitialNode = new TriNode(Extend_X, Extend_Y);//可能无交点，故取延长线
                    #endregion

                    for (int i = 0; i < CrossEdges.Count; i++)
                    {
                        TriNode CrossNode = ComFunLib.CrossNode(InitialNode, OutNode, CrossEdges[i][0], CrossEdges[i][1]);

                        if (CrossNode != null)
                        {
                            CrossNode.ID = -1;//ID=-1表示交叉后添加的点
                            Tuple<List<TriNode>, TriNode> CrossNodeEdge = new Tuple<List<TriNode>, TriNode>(CrossEdges[i], CrossNode);
                            CrossNodeEdges.Add(CrossNodeEdge);
                        }
                    }
                }
                #endregion

                #endregion

                #region 获得最近的交点
                List<double> DisList = new List<double>();
                for (int i = 0; i < CrossNodeEdges.Count; i++)
                {
                    double Dis = ComFunLib.CalLineLength(OutNode, CrossNodeEdges[i].Item2);
                    DisList.Add(Dis);
                }
                #endregion

                double MinDis = DisList.Min();
                return CrossNodeEdges[DisList.IndexOf(MinDis)];
            }

            else
            {
                return null;
            }
        }

        /// <summary>
        /// 获得给定点距离多边形最近的点
        /// </summary>
        /// <param name="OutNode"></param>
        /// <param name="Po"></param>
        /// <returns></returns>
        public TriNode GetNearestNode(TriNode OutNode, PolygonObject Po)
        {
            IPoint pPoint = new PointClass();
            pPoint.X = OutNode.X; pPoint.Y = OutNode.Y;
            IPolygon pPolygon = this.PolygonObjectConvert(Po);
            IProximityOperator IPo = pPolygon as IProximityOperator;

            IPoint NearestPoint = IPo.ReturnNearestPoint(pPoint, 0);
            if (NearestPoint != null)
            {
                TriNode ReturnNode=new TriNode(NearestPoint.X, NearestPoint.Y);
                ReturnNode.ID = -1;

                #region 测试用
                object PointSymbol = Sb.PointSymbolization(100, 100, 100);
                mCon.DrawShape(NearestPoint,ref PointSymbol);
                #endregion

                return ReturnNode;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 获得点所在的多边形边
        /// </summary>
        /// <param name="CrossNode"></param>
        /// <param name="Po"></param>
        /// <returns></returns>
        public List<TriNode> GetOnLineEdge(TriNode CrossNode, PolygonObject Po)
        {
            List<TriNode> OutLine = new List<TriNode>();

            for (int i = 0; i < Po.PointList.Count; i++)
            {
                if (i == Po.PointList.Count - 1)
                {
                    if (CrossNode.X > Math.Min(Po.PointList[i].X, Po.PointList[0].X) &&
                       CrossNode.X < Math.Max(Po.PointList[i].X, Po.PointList[0].X) &&
                       CrossNode.Y > Math.Min(Po.PointList[i].Y, Po.PointList[0].Y) &&
                       CrossNode.Y < Math.Max(Po.PointList[i].Y, Po.PointList[0].Y))
                    {
                        if ((Po.PointList[i].Y - CrossNode.Y) / (Po.PointList[i].X - CrossNode.X) ==
                            (CrossNode.Y - Po.PointList[0].Y) / (CrossNode.X - Po.PointList[0].X))
                        {
                            OutLine.Add(Po.PointList[i]);
                            OutLine.Add(Po.PointList[0]);
                            break;
                        }
                    }
                }

                else
                {
                    if (CrossNode.X > Math.Min(Po.PointList[i].X, Po.PointList[i + 1].X) &&
                       CrossNode.X < Math.Max(Po.PointList[i].X, Po.PointList[i + 1].X) &&
                       CrossNode.Y > Math.Min(Po.PointList[i].Y, Po.PointList[i + 1].Y) &&
                       CrossNode.Y< Math.Max(Po.PointList[i].Y, Po.PointList[i + 1].Y))
                    {
                        if ((Po.PointList[i].Y - CrossNode.Y) / (Po.PointList[i].X - CrossNode.X) ==
                           (CrossNode.Y - Po.PointList[i + 1].Y) / (CrossNode.X - Po.PointList[i + 1].X))
                        {
                            OutLine.Add(Po.PointList[i]);
                            OutLine.Add(Po.PointList[i + 1]);
                            break;
                        }
                    }
                }
            }

            return OutLine;
        }

        /// <summary>
        /// 将建筑物转化为IPolygon
        /// </summary>
        /// <param name="pPolygonObject"></param>
        /// <returns></returns>
        public IPolygon PolygonObjectConvert(PolygonObject pPolygonObject)
        {
            Ring ring1 = new RingClass();
            object missing = Type.Missing;

            IPoint curResultPoint = new PointClass();
            TriNode curPoint = null;
            if (pPolygonObject != null)
            {
                for (int i = 0; i < pPolygonObject.PointList.Count; i++)
                {
                    curPoint = pPolygonObject.PointList[i];
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    ring1.AddPoint(curResultPoint, ref missing, ref missing);
                }
            }

            curPoint = pPolygonObject.PointList[0];
            curResultPoint.PutCoords(curPoint.X, curPoint.Y);
            ring1.AddPoint(curResultPoint, ref missing, ref missing);

            IGeometryCollection pointPolygon = new PolygonClass();
            pointPolygon.AddGeometry(ring1 as IGeometry, ref missing, ref missing);
            IPolygon pPolygon = pointPolygon as IPolygon;

            //PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
            //object PolygonSymbol = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 0, 20, 20);

            //pMapControl.DrawShape(pPolygon, ref PolygonSymbol);
            //pMapControl.Map.RecalcFullExtent();

            pPolygon.SimplifyPreserveFromTo();
            return pPolygon;
        }

        /// <summary>
        /// 最小二乘计算新的边界点的坐标
        /// </summary>
        /// <param name="BoundNode"></param>
        /// <param name="OriginalControlNode"></param>
        /// <param name="NewControlNodes"></param>
        /// <param name="Count"></param>
        /// <param name="Apl"></param>
        /// <returns></returns>
        public List<Tuple<double,double>> LeastSquareAdj(List<TriNode> BoundNode,List<TriNode> OriginalControlNode,List<TriNode> NewControlNodes,int Count,double Apl)
        {
            List<Tuple<double, double>> NewBound_XY = new List<Tuple<double, double>>();

            for (int i = 0; i < BoundNode.Count; i++)
            {
                List<TriNode> NearControlNodes = this.GetNearNodes(BoundNode[i], OriginalControlNode, Count);
                List<TriNode> NewNearControlNodes = this.NewControlNearNodes(BoundNode[i], OriginalControlNode, NewControlNodes, Count);
                Tuple<double, double> Original_X_Y = this.GetNewWeightCenter(BoundNode[i],NearControlNodes, Apl);
                Tuple<double, double> New_X_Y = this.GetNewWeightCenter(BoundNode[i], NewNearControlNodes, Apl);

                Matrix OriginalV_XY=new Matrix(1,2);OriginalV_XY[0,0]=BoundNode[i].X;OriginalV_XY[0,1]=BoundNode[i].Y;//边界点的矩阵表示
                Matrix OriginalCenter_XY=new Matrix(1,2);OriginalCenter_XY[0,0]=Original_X_Y.Item1;OriginalCenter_XY[0,1]=Original_X_Y.Item2;//控制点变换前的权重中心的矩阵表示
                Matrix NewCenter_XY=new Matrix(1,2);NewCenter_XY[0,0]=New_X_Y.Item1;NewCenter_XY[0,1]=New_X_Y.Item2;//控制点变换前的权重中心的矩阵表示

                Matrix CacheOriginal_J = new Matrix(2, 2);
                Matrix CacheNew_J = new Matrix(2, 2);

                for (int j = 0; j < NearControlNodes.Count; j++)
                {
                    double Weigth = this.GetWeigth(NearControlNodes[j], BoundNode[i], Apl);

                    double Original_j_X = NearControlNodes[j].X - Original_X_Y.Item1;
                    double Original_j_Y = NearControlNodes[j].Y - Original_X_Y.Item2;

                    double New_j_X = NewNearControlNodes[j].X - New_X_Y.Item1;
                    double New_j_Y = NewNearControlNodes[j].Y - New_X_Y.Item2;

                    Matrix WeightM = new Matrix(1, 1); WeightM[0, 0] = Weigth;
                    Matrix Original_J = new Matrix(1, 2); Original_J[0, 0] = Original_j_X; Original_J[0, 1] = Original_j_Y;
                    Matrix New_J = new Matrix(1, 2); New_J[0, 0] = New_j_X; New_J[0, 1] = New_j_Y;

                    Matrix resOriginal = Original_J.Transpose() * WeightM * Original_J;
                    Matrix resNew = Original_J.Transpose() * WeightM * New_J;

                    CacheOriginal_J = CacheOriginal_J + resOriginal;
                    CacheNew_J = CacheNew_J + resNew;
                }

                Matrix V_X_Y = (OriginalV_XY - OriginalCenter_XY) * CacheOriginal_J.Inverse() * CacheNew_J + NewCenter_XY;
                Tuple<double, double> Bound_XY = new Tuple<double, double>(V_X_Y[0, 0], V_X_Y[0, 1]);
                NewBound_XY.Add(Bound_XY);
            }

            return NewBound_XY;
        }

        /// <summary>
        /// 计算给定点相对于目标点的权重
        /// </summary>
        /// <param name="CurNode">给定点</param>
        /// <param name="TarNode">目标点</param>
        /// <param name="Apl">权重</param>
        /// <returns></returns>
        public double GetWeigth(TriNode CurNode,TriNode TarNode,double Apl)
        {
            double Weigth=1/Math.Pow(Math.Sqrt((CurNode.X-TarNode.X)*(CurNode.X-TarNode.X)+(CurNode.Y-TarNode.Y)*(CurNode.Y-TarNode.Y)),2*Apl);
            return Weigth;
        }

        /// <summary>
        /// 返回距离目标点最近的Count个点
        /// </summary>
        /// <param name="TarNode">目标点</param>
        /// <param name="AllNodes">所有点</param>
        /// <param name="Count">前Count个</param>
        public List<TriNode> GetNearNodes(TriNode TarNode,List<TriNode> AllNodes,int Count)
        {
            if (Count >= AllNodes.Count)
            {
                return AllNodes;
            }

            else
            {
                List<TriNode> resNodes = new List<TriNode>();

                List<double> DisDistance = new List<double>();
                List<double> SortDisDistance = new List<double>();
                for (int i = 0; i < AllNodes.Count; i++)
                {
                    double Dis = Math.Sqrt((TarNode.X - AllNodes[i].X) * (TarNode.X - AllNodes[i].X) + (TarNode.Y - AllNodes[i].Y) * (TarNode.Y - AllNodes[i].Y));
                    DisDistance.Add(Dis);
                    SortDisDistance.Add(Dis);
                }

                SortDisDistance.Sort();//升序排列
                double CountValue = SortDisDistance[Count - 1];


                for (int i = 0; i < DisDistance.Count; i++)
                {
                    if (DisDistance[i] <= CountValue)
                    {
                        resNodes.Add(AllNodes[i]);
                    }
                }

                return resNodes;
            }
        }

        /// <summary>
        /// 返回权重质心
        /// </summary>
        /// <param name="TarNode">目标点</param>
        /// <param name="AllNodes">所有点</param>
        /// <param name="Count">前Count个</param>
        /// <param name="Apl">权重参数</param>
        /// <returns></returns>
        public Tuple<double,double> GetWeightCenter(TriNode TarNode,List<TriNode> AllNodes,int Count,double Apl)
        {
            List<TriNode> NearNodes=this.GetNearNodes(TarNode,AllNodes,Count);//返回距离目标点的前Count的点
            double WeigthSum=0;double PointXSum=0;double PointYSum=0;
            for(int i=0;i<NearNodes.Count;i++)
            {
                double Weigth=this.GetWeigth(NearNodes[i],TarNode,Apl);
                WeigthSum=WeigthSum+Weigth;
                PointXSum=PointXSum+NearNodes[i].X*Weigth;
                PointYSum=PointYSum+NearNodes[i].Y*Weigth;
            }

            double X=PointXSum/WeigthSum;
            double Y=PointYSum/WeigthSum;
            Tuple<double,double> X_Y=new Tuple<double,double>(X,Y);
            return X_Y;
        }

        /// <summary>
        /// 返回权重质心
        /// </summary>
        /// <param name="TarNode">目标点</param>
        /// <param name="NearNodes">所有点</param>
        /// <param name="Count">前Count个</param>
        /// <param name="Apl">权重参数</param>
        /// <returns></returns>
        public Tuple<double, double> GetNewWeightCenter(TriNode TarNode, List<TriNode> NearNodes,double Apl)
        {
            double WeigthSum = 0; double PointXSum = 0; double PointYSum = 0;
            for (int i = 0; i < NearNodes.Count; i++)
            {
                double Weigth = this.GetWeigth(NearNodes[i], TarNode, Apl);
                WeigthSum = WeigthSum + Weigth;
                PointXSum = PointXSum + NearNodes[i].X * Weigth;
                PointYSum = PointYSum + NearNodes[i].Y * Weigth;
            }

            double X = PointXSum / WeigthSum;
            double Y = PointYSum / WeigthSum;
            Tuple<double, double> X_Y = new Tuple<double, double>(X, Y);
            return X_Y;
        }

        /// <summary>
        /// 返回相对于原始ControlNode最近的Count的个在变换后点集中的对应点
        /// </summary>
        /// <param name="TarNode"></param>
        /// <param name="OriginalNodes"></param>
        /// <param name="NewNodes"></param>
        /// <param name="Count"></param>
        /// <returns></returns>
        public List<TriNode> NewControlNearNodes(TriNode TarNode, List<TriNode> OriginalNodes, List<TriNode> NewNodes, int Count)
        {
            List<TriNode> resNodes = new List<TriNode>();
            List<TriNode> NearNodes = this.GetNearNodes(TarNode, OriginalNodes, Count);//返回距离目标点的前Count的点

            for(int i=0;i<NearNodes.Count;i++)
            {
                for (int j = 0; j < NewNodes.Count;j++ )
                {
                    if (NewNodes[j].TagValue == NearNodes[i].TagValue)
                    {
                        resNodes.Add(NewNodes[j]);
                        break;
                    }
                }
            }

            return resNodes;
        }
    }
}
