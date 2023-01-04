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
    /// TileMap support
    /// </summary>
    class TileMapSupport
    {
        public bool continueLable = true;//参数

        /// <summary>
        /// Node Transform
        /// </summary>
        /// <param name="NodeList"></param>
        /// <param name="EdgeList"></param>
        /// <param name="BoundaryPo"></param>
        /// <param name="TileCount"></param>
        public void NodesTransform(List<ProxiNode> NodeList, List<ProxiEdge> EdgeList, PolygonObject BoundaryPo, int TileCount)
        {
            #region Get New XY
            double Size = this.GetTileSize(BoundaryPo, TileCount);//Get Tile size
            List<Tuple<double, double>> NewNodesXY = new List<Tuple<double, double>>();
            for (int i = 0; i < NodeList.Count; i++)
            {
                Tuple<double, double> NodeXY = this.NodeShift(EdgeList, NodeList[i], Size);
                NewNodesXY.Add(NodeXY);

                //NodeList[i].X = NodeXY.Item1;
                //NodeList[i].Y = NodeXY.Item2;
            }
            #endregion

            #region Update XY
            for (int i = 0; i < NewNodesXY.Count; i++)
            {
                NodeList[i].X = NewNodesXY[i].Item1;
                NodeList[i].Y = NewNodesXY[i].Item2;
            }
            #endregion
        }

        /// <summary>
        /// Node Transform
        /// </summary>
        /// <param name="NodeList"></param>
        /// <param name="EdgeList"></param>
        /// <param name="BoundaryPo"></param>
        /// <param name="TileCount"></param>
        public void NodesTransform(List<ProxiNode> NodeList, List<ProxiEdge> EdgeList, double FullArea, int TileCount)
        {
            #region Get New XY
            double Size = this.GetTileSize(FullArea, TileCount);//Get Tile size
            List<Tuple<double, double>> NewNodesXY = new List<Tuple<double, double>>();
            for (int i = 0; i < NodeList.Count; i++)
            {
                Tuple<double, double> NodeXY = this.NodeShift(EdgeList, NodeList[i], Size);
                NewNodesXY.Add(NodeXY);

                //NodeList[i].X = NodeXY.Item1;
                //NodeList[i].Y = NodeXY.Item2;
            }
            #endregion

            #region Update XY
            for (int i = 0; i < NewNodesXY.Count; i++)
            {
                NodeList[i].X = NewNodesXY[i].Item1;
                NodeList[i].Y = NewNodesXY[i].Item2;
            }
            #endregion
        }

        /// <summary>
        /// 依据NodePg更新NodeBoundaryPg
        /// </summary>
        /// <param name="NodePg">中心节点间的邻近关系</param>
        /// <param name="NodeBoundaryPg">中心节点、边缘节点间的邻近关系</param>
        public void PgRefresh(ProxiGraph NodePg, ProxiGraph NodeBoundaryPg)
        {
            #region 删除NodeBoundaryPg中直接关联两个重心点的边
            for (int i = NodeBoundaryPg.EdgeList.Count - 1; i >= 0; i--)
            {

                if (NodeBoundaryPg.EdgeList[i].Node1.FeatureType == FeatureType.PointType && NodeBoundaryPg.EdgeList[i].Node2.FeatureType == FeatureType.PointType)
                {
                    NodeBoundaryPg.EdgeList.RemoveAt(i);
                }
            }

            #region 更新EdgeList中Edge的序号
            for (int i = 0; i < NodeBoundaryPg.EdgeList.Count; i++)
            {
                NodeBoundaryPg.EdgeList[i].ID = i;
            }
            #endregion
            #endregion

            #region 依据NodePg中节点间邻近关系更新NodeBoundaryPg
            for (int i = 0; i < NodePg.EdgeList.Count; i++)
            {
                ProxiNode Node1 = null; ProxiNode Node2 = null;
                for (int j = 0; j < NodeBoundaryPg.NodeList.Count; j++)
                {
                    if (NodePg.EdgeList[i].Node1.TagID == NodeBoundaryPg.NodeList[j].TagID && NodeBoundaryPg.NodeList[j].FeatureType==FeatureType.PointType)
                    {
                        Node1 = NodeBoundaryPg.NodeList[j];
                    }

                    if (NodePg.EdgeList[i].Node2.TagID == NodeBoundaryPg.NodeList[j].TagID && NodeBoundaryPg.NodeList[j].FeatureType == FeatureType.PointType)
                    {
                        Node2 = NodeBoundaryPg.NodeList[j];
                    }
                }

                if (Node1 != null && Node2 != null)
                {
                    ProxiEdge CacheEdge = new ProxiEdge(NodeBoundaryPg.EdgeList.Count, Node1, Node2);
                    NodeBoundaryPg.EdgeList.Add(CacheEdge);
                }
            }
            #endregion

            #region 依据NodeBoundaryPg更新NodePg
            //for (int i = 0; i < NodeBoundaryPg.NodeList.Count; i++)
            //{
            //    if (NodeBoundaryPg.NodeList[i].FeatureType == FeatureType.PolygonType)///如果是边界上的点
            //    {
            //        int CacheID = NodeBoundaryPg.NodeList[i].ID;//节点的ID
            //        NodeBoundaryPg.NodeList[i].ID = NodePg.NodeList.Count;
            //        NodePg.NodeList.Add(NodeBoundaryPg.NodeList[i]);

            //        for (int j = 0; j < NodeBoundaryPg.EdgeList.Count; j++)
            //        {
            //            if (!NodeBoundaryPg.EdgeList[j].Visited)//如果该边未被访问
            //            {
            //                if (NodeBoundaryPg.EdgeList[j].Node1.ID == CacheID)//对应节点关联的边
            //                {
            //                    ProxiNode TarNode = NodePg.GetNodebyTagIDandType(NodeBoundaryPg.EdgeList[j].Node2.TagID, NodeBoundaryPg.EdgeList[j].Node2.FeatureType);//获得了对应的中心点
            //                    if (TarNode != null)
            //                    {
            //                        ProxiEdge CacheEdge = new ProxiEdge(NodePg.EdgeList.Count, NodeBoundaryPg.NodeList[i], TarNode);
            //                        NodePg.EdgeList.Add(CacheEdge);
            //                        NodeBoundaryPg.EdgeList[j].Visited = true;
            //                    }
            //                }

            //                else if (NodeBoundaryPg.EdgeList[j].Node2.ID == CacheID)//对应节点关联的边
            //                {
            //                    ProxiNode TarNode = NodePg.GetNodebyTagIDandType(NodeBoundaryPg.EdgeList[j].Node1.TagID, NodeBoundaryPg.EdgeList[j].Node1.FeatureType);//获得了对应的中心点
            //                    if (TarNode != null)
            //                    {
            //                        ProxiEdge CacheEdge = new ProxiEdge(NodePg.EdgeList.Count, NodeBoundaryPg.NodeList[i], TarNode);
            //                        NodePg.EdgeList.Add(CacheEdge);
            //                        NodeBoundaryPg.EdgeList[j].Visited = true;
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}
            #endregion
        }

        /// <summary>
        /// 获得给定点偏移后的位置[x,y]
        /// </summary>
        /// <param name="EdgeList"></param>
        /// <param name="Pn"></param>
        /// <param name="BoundaryPo"></param>
        /// <param name="TileCount"></param>
        /// <returns></returns>
        public Tuple<double, double> NodeShift(List<ProxiEdge> EdgeList, ProxiNode Pn,double Size)
        {
            #region MainProcess
            List<ProxiNode> NeiborNodes = this.GetNeibors(EdgeList, Pn);//Get NeinorNodes
            
            double SumX = 0; double SumY = 0;
            for (int i = 0; i < NeiborNodes.Count; i++)
            {
                Tuple<double, double> NodePairVector = this.GetNodePairVector(Pn, NeiborNodes[i]);
                SumX = SumX + (NeiborNodes[i].X + Size * NodePairVector.Item1);
                SumY = SumY + (NeiborNodes[i].Y + Size * NodePairVector.Item2);
            }
            #endregion

            double NewX = SumX / NeiborNodes.Count;
            double NewY = SumY / NeiborNodes.Count;
            Tuple<double, double> ShiftXY = new Tuple<double, double>(NewX,NewY);

            return ShiftXY;
        }

        /// <summary>
        /// 获得给定节点的1阶邻近
        /// </summary>
        /// <param name="EdgeList"></param>
        /// <param name="Pn"></param>
        /// <returns></returns>
        public List<ProxiNode> GetNeibors(List<ProxiEdge> EdgeList, ProxiNode Pn)
        {
            List<ProxiNode> NeiborNodes = new List<ProxiNode>();
            foreach (ProxiEdge Pe in EdgeList)
            {
                if (Pe.Node1.TagID == Pn.TagID)
                {
                    NeiborNodes.Add(Pe.Node2);
                }
                if (Pe.Node2.TagID == Pn.TagID)
                {
                    NeiborNodes.Add(Pe.Node1);
                }
            }
            return NeiborNodes;
        }

        /// <summary>
        /// GetTileSize
        /// </summary>
        /// <param name="Po"></param>
        /// <param name="TileCount"></param>
        /// <returns></returns>
        public double GetTileSize(PolygonObject Po, int TileCount)
        {
            ///Get Area
            PublicUtil PU = new PublicUtil();
            IPolygon pPo = PU.PolygonObjectConvert(Po);
            IArea pArea = pPo as IArea;
            double Area = pArea.Area;

            ///GetSize
            double Size = this.GetTileSize(Area, TileCount);
            return Size;
        }

        /// <summary>
        /// GetTileSize
        /// </summary>
        /// <param name="Po"></param>
        /// <param name="TileCount"></param>
        /// <returns></returns>
        public double GetTileSize(double FullArea, int TileCount)
        {
            double Size = Math.Sqrt(FullArea / TileCount);
            return Size;
        }

        /// <summary>
        /// Get unit displacement vector betweeo two nodes (Pn1 to Pn2)
        /// </summary>
        /// <param name="Pn1"></param>
        /// <param name="Pn2"></param>
        /// <returns></returns>
        public Tuple<double, double> GetNodePairVector(ProxiNode Pn1, ProxiNode Pn2)
        {
            double Dis = this.GetDis(Pn1, Pn2);
            double CosX = (Pn1.X - Pn2.X) / Dis;
            double SinY = (Pn1.Y - Pn2.Y) / Dis;
            Tuple<double, double> VectorXY = new Tuple<double, double>(CosX,SinY);
            return VectorXY;
        }

        /// <summary>
        /// Get distance between two nodes
        /// </summary>
        /// <param name="Pn1"></param>
        /// <param name="Pn2"></param>
        /// <returns></returns>
        public double GetDis(ProxiNode Pn1, ProxiNode Pn2)
        {
            if (Pn1 == null || Pn2 == null)
            {
                return 0;
            }
            else
            {
                return Math.Sqrt((Pn1.X - Pn2.X) * (Pn1.X - Pn2.X) + (Pn1.Y - Pn2.Y) * (Pn1.Y - Pn2.Y));
            }
        }

        /// <summary>
        /// Get the Center of group nodes
        /// </summary>
        /// <param name="NodeList"></param>
        /// <returns></returns>
        public ProxiNode GetGroupNodeCenter(List<ProxiNode> NodeList)
        {
            if (NodeList.Count > 0)
            {
                double SumX = 0; double SumY = 0;
                for (int i = 0; i < NodeList.Count; i++)
                {
                    SumX = SumX + NodeList[i].X;
                    SumY = SumY + NodeList[i].Y;
                }

                ProxiNode CacheNode = new ProxiNode(SumX / NodeList.Count, SumY / NodeList.Count);
                return CacheNode;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 将移动后的邻近图移动到原始的重心
        /// </summary>
        /// <param name="pg"></param>
        /// <param name="OriginalNode"></param>
        public void CenterRecover(ProxiGraph pg,ProxiNode OriginalNode)
        {
            ProxiNode TransformeredCenter = this.GetGroupNodeCenter(pg.NodeList);//计算移动后的新重心
            double Dx = OriginalNode.X - TransformeredCenter.X;
            double Dy = OriginalNode.Y - TransformeredCenter.Y;

            #region 移动过程
            for (int i = 0; i < pg.NodeList.Count; i++)
            {
                pg.NodeList[i].X = pg.NodeList[i].X + Dx;
                pg.NodeList[i].Y = pg.NodeList[i].Y + Dy;
            }
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
        public void TileMapSnake_PgReBuiltMapIn(ProxiGraph pg, SMap sMap, double scale, double a, double b, int Iterations, int algType, double Size, double StopT, string OutFilePath, IMap pMap, double MaxForce, double MaxForce_2)
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

                algSnakes.DoDisplaceTileMap(sMap, MaxForce, MaxForce_2, Size);//执行邻近图Beams移位算法，代表混合型

                #region 邻近图重构
                pg.WriteProxiGraph2Shp(OutFilePath, "pG_" + i.ToString(), pMap.SpatialReference);
                //pg.WriteProxiGraph2Shp(OutFilePath, "pG_" + i.ToString(), pMap.SpatialReference);
                sMap.WriteResult2Shp(OutFilePath, "Map_" + i.ToString(), pMap.SpatialReference);
                #endregion

                this.continueLable = algSnakes.isContinue;
                if (algSnakes.isContinue == false)
                {
                    break;
                }
            }
        }
    }
}
