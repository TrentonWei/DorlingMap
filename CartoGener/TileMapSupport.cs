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
            #region 删除NodeBoundaryPg中直接关联两个中心点的边
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
                for (int j = 0; j < NodePg.NodeList.Count; j++)
                {
                    if (NodePg.EdgeList[i].Node1.TagID == NodePg.NodeList[j].TagID)
                    {
                        Node1 = NodePg.NodeList[j];
                    }


                    if (NodePg.EdgeList[i].Node2.TagID == NodePg.NodeList[j].TagID)
                    {
                        Node2 = NodePg.NodeList[j];
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
    }
}
