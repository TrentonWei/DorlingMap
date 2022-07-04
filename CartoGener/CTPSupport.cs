using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public void CTPSnake(ProxiGraph pg, List<ProxiNode> FinalLocation, double scale, double a, double b, int Iterations, int algType, double MinDis, double StopT,double MaxForce)
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

                algSnakes.DoDispacePG_CTP(FinalLocation, MinDis, MaxForce);//执行邻近图Beams移位算法，代表混合型

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
        public void CTPSnake_PgReBuilt(ProxiGraph pg, List<ProxiNode> FinalLocation, double scale, double a, double b, int Iterations, int algType, double MinDis, double StopT, string OutFilePath, IMap pMap,double MaxForce)
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

                algSnakes.DoDispacePG_CTP(FinalLocation, MinDis,MaxForce);//执行邻近图Beams移位算法，代表混合型

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
        public void CTPSnake_PgReBuiltMapIn(ProxiGraph pg, List<ProxiNode> FinalLocation,SMap sMap, double scale, double a, double b, int Iterations, int algType, double MinDis, double StopT, string OutFilePath, IMap pMap,double MaxForce)
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

                algSnakes.DoDispacePG_CTP(FinalLocation,sMap, MinDis,MaxForce);//执行邻近图Beams移位算法，代表混合型

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

                ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
                cdt.CreateConsDTfromPolylineandPolygon(null, sMap.PolygonList);

                //dt.CreateRNG();
                ProxiGraph CachePg = new ProxiGraph(cdt.TriNodeList, cdt.TriEdgeList); //Pg中节点的ID和Map.PointList中节点的ID是一样的
                CachePg.WriteProxiGraph2Shp(OutFilePath, "pG_" + i.ToString(), pMap.SpatialReference);
                //pg.WriteProxiGraph2Shp(OutFilePath, "pG_" + i.ToString(), pMap.SpatialReference);
                sMap.WriteResult2Shp(OutFilePath, "Map_" + i.ToString(), pMap.SpatialReference);

                //algSnakes.ProxiGraph = CachePg;
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
        public void CTPSnake_PgReBuiltHierMapIn(ProxiGraph pg, List<ProxiNode> FinalLocation, SMap sMap, double scale, double a, double b, int Iterations, int algType, double MinDis, double StopT, string OutFilePath, IMap pMap,double MaxForce)
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
                algSnakes.DoDispacePG_HierCTP(FinalLocation, sMap, MinDis, MaxForce);//执行邻近图Beams移位算法，代表混合型

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

                    //algSnakes.ProxiGraph = CachePg;
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
    }
}
