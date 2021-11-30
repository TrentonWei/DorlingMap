using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using AuxStructureLib;
using System.IO;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace AuxStructureLib
{
    /// <summary>
    /// 邻近图
    /// </summary>
    [Serializable]
    public class ProxiGraph
    {
        /// <summary>
        /// 点列表
        /// </summary>
        public List<ProxiNode> NodeList = null;
        /// <summary>
        /// 边列表
        /// </summary>
        public List<ProxiEdge> EdgeList = null;
        /// <summary>
        /// 父亲
        /// </summary>
        public ProxiGraph ParentGraph = null;
        /// <summary>
        /// 孩子
        /// </summary>
        public List<ProxiGraph> SubGraphs = null;
        /// <summary>
        /// 多变形的个数字段
        /// </summary>
        private int polygonCount = -1;
        /// <summary>
        /// 边列表
        /// </summary>
        public List<ProxiEdge> MSTEdgeList = null;

        /// <summary>
        /// 多边形个数属性
        /// </summary>
        public int PolygonCount
        {
            get
            {
                if (this.polygonCount != -1)
                {
                    return this.polygonCount;
                }
                else
                {
                    int count = 0;
                    if (this.NodeList == null || this.NodeList.Count == 0)
                        return -1;
                    foreach (ProxiNode node in this.NodeList)
                    {
                        if (node.FeatureType == FeatureType.PolygonType)
                            count++;
                    }
                    this.polygonCount = count;
                    return this.polygonCount;
                }
            }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        public ProxiGraph()
        {
            NodeList = new List<ProxiNode>();
            EdgeList = new List<ProxiEdge>();
        }
        /// <summary>
        /// 创建结点列表
        /// </summary>
        /// <param name="map">地图</param>
        private void CreateNodes(SMap map)
        {
            int nID = 0;
            //点
            if (map.PointList != null)
            {
                foreach (PointObject point in map.PointList)
                {
                    ProxiNode curNode = point.CalProxiNode();
                    curNode.ID = nID;
                    this.NodeList.Add(curNode);
                    nID++;
                }
            }
            //线
            if (map.PolylineList != null)
            {
                foreach (PolylineObject pline in map.PolylineList)
                {
                    ProxiNode curNode = pline.CalProxiNode();
                    curNode.ID = nID;
                    this.NodeList.Add(curNode);
                    nID++;
                }
            }
            //面
            if (map.PolygonList != null)
            {
                foreach (PolygonObject polygon in map.PolygonList)
                {
                    ProxiNode curNode = polygon.CalProxiNode();
                    curNode.ID = nID;
                    this.NodeList.Add(curNode);
                    nID++;
                }
            }
        }


        /// <summary>
        /// 创建边
        /// </summary>
        /// <param name="skeleton">骨架线</param>
        private void CreateEdges(Skeleton skeleton)
        {
            if (skeleton == null || skeleton.Skeleton_ArcList == null || skeleton.Skeleton_ArcList.Count == 0)
            {
                return;
            }
            int curTagID = -1;
            FeatureType curType = FeatureType.Unknown;

            ProxiNode node1 = null;
            ProxiNode node2 = null;

            ProxiEdge curEdge = null;

            // int eID = 0;

            foreach (Skeleton_Arc curArc in skeleton.Skeleton_ArcList)
            {
                if (curArc.LeftMapObj != null && curArc.RightMapObj != null)
                {

                    curTagID = curArc.LeftMapObj.ID;
                    curType = curArc.LeftMapObj.FeatureType;
                    node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                    curTagID = curArc.RightMapObj.ID;
                    curType = curArc.RightMapObj.FeatureType;
                    node2 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                    curEdge = new ProxiEdge(curArc.ID, node1, node2);
                    this.EdgeList.Add(curEdge);
                    node1.EdgeList.Add(curEdge);
                    node2.EdgeList.Add(curEdge);
                    curEdge.NearestEdge = curArc.NearestEdge;
                    curEdge.Weight = curArc.AveDistance;
                    curEdge.Ske_Arc = curArc;
                }
            }

        }


        /// <summary>
        /// 创建结点列表
        /// </summary>
        /// <param name="map">地图</param>
        private void CreateNodesforPointandPolygon(SMap map)
        {
            int nID = 0;
            //点
            if (map.PointList != null)
            {
                foreach (PointObject point in map.PointList)
                {
                    ProxiNode curNode = point.CalProxiNode();
                    curNode.ID = nID;
                    this.NodeList.Add(curNode);
                    nID++;
                }
            }
            ////线
            //if (map.PolylineList != null)
            //{
            //    foreach (PolylineObject pline in map.PolylineList)
            //    {
            //        ProxiNode curNode = pline.CalProxiNode();
            //        curNode.ID = nID;
            //        this.NodeList.Add(curNode);
            //        nID++;
            //    }
            //}
            //面
            if (map.PolygonList != null)
            {
                foreach (PolygonObject polygon in map.PolygonList)
                {
                    ProxiNode curNode = polygon.CalProxiNode();
                    curNode.ID = nID;
                    this.NodeList.Add(curNode);
                    nID++;
                }
            }
        }

        /// <summary>
        /// 创建边
        /// </summary>
        /// <param name="skeleton">骨架线</param>
        private void CreateEdgesforPointandPolygon(Skeleton skeleton)
        {
            if (skeleton == null || skeleton.Skeleton_ArcList == null || skeleton.Skeleton_ArcList.Count == 0)
            {
                return;
            }
            int curTagID = -1;
            FeatureType curType = FeatureType.Unknown;

            ProxiNode node1 = null;
            ProxiNode node2 = null;

            ProxiEdge curEdge = null;

            // int eID = 0;

            foreach (Skeleton_Arc curArc in skeleton.Skeleton_ArcList)
            {
                if (curArc.LeftMapObj != null && curArc.RightMapObj != null)
                {
                    if (curArc.LeftMapObj.FeatureType != FeatureType.PolylineType && curArc.RightMapObj.FeatureType != FeatureType.PolylineType)
                    {
                        curTagID = curArc.LeftMapObj.ID;
                        curType = curArc.LeftMapObj.FeatureType;
                        node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        curTagID = curArc.RightMapObj.ID;
                        curType = curArc.RightMapObj.FeatureType;
                        node2 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        curEdge = new ProxiEdge(curArc.ID, node1, node2);
                        this.EdgeList.Add(curEdge);
                        node1.EdgeList.Add(curEdge);
                        node2.EdgeList.Add(curEdge);
                        curEdge.NearestEdge = curArc.NearestEdge;
                        curEdge.Weight = curArc.AveDistance;
                        curEdge.Ske_Arc = curArc;
                    }
                    //eID++;
                }
            }

        }

        /// <summary>
        /// 创建边
        /// </summary>
        /// <param name="skeleton">冲突</param>
        private void CreateEdges(List<Conflict> conflicts)
        {
            if (conflicts == null || conflicts.Count == 0)
            {
                return;
            }
            int curTagID = -1;
            FeatureType curType = FeatureType.Unknown;
            ProxiNode node1 = null;
            ProxiNode node2 = null;

            ProxiEdge curEdge = null;

            // int eID = 0;

            foreach (Conflict curConflict in conflicts)
            {
                if (curConflict.Obj1 != null && curConflict.Obj2 != null)
                {
                    if (curConflict.Obj1.ToString() == @"AuxStructureLib.SDS_PolylineObj")
                    {
                        SDS_PolylineObj curl = curConflict.Obj1 as SDS_PolylineObj;
                        curTagID = curl.ID;
                        curType = FeatureType.PolylineType;
                        node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                    }
                    else if (curConflict.Obj1.ToString() == @"AuxStructureLib.SDS_PolygonO")
                    {
                        SDS_PolygonO curO = curConflict.Obj1 as SDS_PolygonO;
                        curTagID = curO.ID;
                        curType = FeatureType.PolygonType;
                        node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                    }
                    if (curConflict.Obj2.ToString() == @"AuxStructureLib.SDS_PolylineObj")
                    {
                        SDS_PolylineObj curl = curConflict.Obj2 as SDS_PolylineObj;
                        curTagID = curl.ID;
                        curType = FeatureType.PolylineType;
                        node2 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                    }
                    else if (curConflict.Obj2.ToString() == @"AuxStructureLib.SDS_PolygonO")
                    {
                        SDS_PolygonO curO = curConflict.Obj2 as SDS_PolygonO;
                        curTagID = curO.ID;
                        curType = FeatureType.PolygonType;
                        node2 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                    }

                    curEdge = new ProxiEdge(-1, node1, node2);
                    this.EdgeList.Add(curEdge);
                    node1.EdgeList.Add(curEdge);
                    node2.EdgeList.Add(curEdge);
                    //eID++;
                }
            }

        }

        /// <summary>
        /// 从骨架线构造邻近图
        /// </summary>
        public void CreateProxiGraphfrmSkeleton(SMap map, Skeleton skeleton)
        {
            CreateNodes(map);
            CreateEdges(skeleton);
        }

        /// <summary>
        /// 从骨架线构造邻近图
        /// </summary>
        public void CreateProxiGraphfrmSkeletonBuildings_Perpendicular(SMap map, Skeleton skeleton)
        {
            this.CreateNodesforPointandPolygon(map);
            this.CreateEdgesforPointandPolygon(skeleton);
            CreateNodesandPerpendicular_EdgesforPolyline_(map, skeleton);
        }

        /// <summary>
        /// 创建点集的MST（最短距离）
        /// </summary>
        public void CreateMST(List<ProxiNode> PnList, List<ProxiEdge> PeList,List<PolygonObject> PoList)
        {
            #region 矩阵初始化
            double[,] matrixGraph = new double[PnList.Count, PnList.Count];

            for (int i = 0; i < PnList.Count; i++)
            {
                for (int j = 0; j < PnList.Count; j++)
                {
                    matrixGraph[i, j] = -1;
                }
            }
            #endregion

            #region 矩阵赋值
            //double MinV = 1000000;//矩阵最小值
            for (int i = 0; i < PnList.Count; i++)
            {
                ProxiNode Point1 = PnList[i];

                for (int j = 0; j < PeList.Count; j++)
                {
                    ProxiEdge Edge1 = PeList[j];

                    ProxiNode pPoint1 = Edge1.Node1;
                    ProxiNode pPoint2 = Edge1.Node2;
                    if (Point1.X == pPoint1.X && Point1.Y == pPoint1.Y)
                    {
                        for (int m = 0; m < PnList.Count; m++)
                        {
                            ProxiNode Point2 = PnList[m];

                            if (Point2.X == pPoint2.X && Point2.Y == pPoint2.Y)
                            {
                                #region 计算两个圆之间的距离
                                PolygonObject Po1 = this.GetObjectByID(PoList, pPoint1.TagID);
                                PolygonObject Po2 = this.GetObjectByID(PoList, pPoint2.TagID);

                                double EdgeDis = this.GetDis(pPoint1, pPoint2);
                                double RSDis = Po1.R + Po2.R;
                                #endregion

                                matrixGraph[i, m] = matrixGraph[m, i] = EdgeDis - RSDis;
                                //if (EdgeDis - RSDis < MinV)
                                //{
                                //    MinV = EdgeDis - RSDis;//获取矩阵最小值
                                //}
                            }
                        }
                    }
                }
            }

            
            for (int i = 0; i < PnList.Count; i++)
            {
                for (int j = 0; j < PnList.Count; j++)
                {
                    if (matrixGraph[i, j] == -1 || matrixGraph[j, i] == -1)
                    {
                        matrixGraph[i, j] = matrixGraph[j, i] = 100000;
                    }             
                }
            }
            #endregion

            #region MST计算
            IArray LabelArray = new ArrayClass();//MST点集
            IArray fLabelArray = new ArrayClass();
            List<List<int>> EdgesGroup = new List<List<int>>();//MST边集

            for (int F = 0; F < PnList.Count; F++)
            {
                fLabelArray.Add(F);
            }

            int LabelFirst = 0;//任意添加一个节点
            LabelArray.Add(LabelFirst);
            //int x = 0;
            int LabelArrayNum;
            do
            {
                LabelArrayNum = LabelArray.Count;
                int fLabelArrayNum = fLabelArray.Count;
                double MinDist = 100001;
                List<int> Edge = new List<int>();

                int EdgeLabel2 = -1;
                int EdgeLabel1 = -1;
                int Label = -1;

                for (int i = 0; i < LabelArrayNum; i++)
                {
                    int p1 = (int)LabelArray.get_Element(i);

                    for (int j = 0; j < fLabelArrayNum; j++)
                    {
                        int p2 = (int)fLabelArray.get_Element(j);

                        if (matrixGraph[p1, p2] < MinDist)
                        {
                            MinDist = matrixGraph[p1, p2];
                            EdgeLabel2 = p2;
                            EdgeLabel1 = p1;
                            Label = j;
                        }
                    }
                }


                //x++;
                Edge.Add(EdgeLabel1);
                Edge.Add(EdgeLabel2);
                EdgesGroup.Add(Edge);

                fLabelArray.Remove(Label);
                LabelArray.Add(EdgeLabel2);

            } while (LabelArrayNum < PnList.Count);
            #endregion

            #region 生成MST的nodes和Edges
            int EdgesGroupNum = EdgesGroup.Count;
            List<ProxiEdge> MSTEdgeList = new List<ProxiEdge>();

            for (int i = 0; i < EdgesGroupNum; i++)
            {
                int m, n;
                m = EdgesGroup[i][0];
                n = EdgesGroup[i][1];

                ProxiNode Pn1 = PnList[m];
                ProxiNode Pn2 = PnList[n];

                foreach (ProxiEdge Pe in PeList)
                {
                    if ((Pe.Node1.X == Pn1.X && Pe.Node2.X == Pn2.X) || (Pe.Node1.X == Pn2.X && Pe.Node2.X == Pn1.X))
                    {
                        if (!MSTEdgeList.Contains(Pe))
                        {
                            MSTEdgeList.Add(Pe);
                            break;
                        }
                    }
                }

            }
            #endregion

            this.EdgeList = MSTEdgeList;
        }

        /// <summary>
        /// 创建点集的MST（最短距离）
        /// </summary>
        public void CreateMSTRevise(List<ProxiNode> PnList, List<ProxiEdge> PeList, List<PolygonObject> PoList)
        {
            #region 矩阵初始化
            double[,] matrixGraph = new double[PnList.Count, PnList.Count];

            for (int i = 0; i < PnList.Count; i++)
            {
                for (int j = 0; j < PnList.Count; j++)
                {
                    matrixGraph[i, j] = -1;
                }
            }
            #endregion

            #region 矩阵赋值
            //double MinV = 1000000;//矩阵最小值
            for (int i = 0; i < PnList.Count; i++)
            {
                ProxiNode Point1 = PnList[i];

                for (int j = 0; j < PeList.Count; j++)
                {
                    ProxiEdge Edge1 = PeList[j];

                    ProxiNode pPoint1 = Edge1.Node1;
                    ProxiNode pPoint2 = Edge1.Node2;
                    if (Point1.X == pPoint1.X && Point1.Y == pPoint1.Y)
                    {
                        for (int m = 0; m < PnList.Count; m++)
                        {
                            ProxiNode Point2 = PnList[m];

                            if (Point2.X == pPoint2.X && Point2.Y == pPoint2.Y)
                            {
                                #region 计算两个圆之间的距离
                                PolygonObject Po1 = this.GetObjectByID(PoList, pPoint1.TagID);
                                PolygonObject Po2 = this.GetObjectByID(PoList, pPoint2.TagID);

                                double EdgeDis = this.GetDis(pPoint1, pPoint2);
                                double RSDis = Po1.R + Po2.R;
                                #endregion

                                matrixGraph[i, m] = matrixGraph[m, i] = EdgeDis - RSDis;
                                //if (EdgeDis - RSDis < MinV)
                                //{
                                //    MinV = EdgeDis - RSDis;//获取矩阵最小值
                                //}
                            }
                        }
                    }
                }
            }


            for (int i = 0; i < PnList.Count; i++)
            {
                for (int j = 0; j < PnList.Count; j++)
                {
                    if (matrixGraph[i, j] == -1 || matrixGraph[j, i] == -1)
                    {
                        matrixGraph[i, j] = matrixGraph[j, i] = 100000;
                    }
                }
            }
            #endregion

            #region MST计算
            IArray LabelArray = new ArrayClass();//MST点集
            IArray fLabelArray = new ArrayClass();
            List<List<int>> EdgesGroup = new List<List<int>>();//MST边集

            for (int F = 0; F < PnList.Count; F++)
            {
                fLabelArray.Add(F);
            }

            int LabelFirst = 0;//任意添加一个节点
            LabelArray.Add(LabelFirst);
            //int x = 0;
            int LabelArrayNum;
            do
            {
                LabelArrayNum = LabelArray.Count;
                int fLabelArrayNum = fLabelArray.Count;
                double MinDist = 100001;
                List<int> Edge = new List<int>();

                int EdgeLabel2 = -1;
                int EdgeLabel1 = -1;
                int Label = -1;

                for (int i = 0; i < LabelArrayNum; i++)
                {
                    int p1 = (int)LabelArray.get_Element(i);

                    for (int j = 0; j < fLabelArrayNum; j++)
                    {
                        int p2 = (int)fLabelArray.get_Element(j);

                        if (matrixGraph[p1, p2] < MinDist)
                        {
                            MinDist = matrixGraph[p1, p2];
                            EdgeLabel2 = p2;
                            EdgeLabel1 = p1;
                            Label = j;
                        }
                    }
                }


                //x++;
                Edge.Add(EdgeLabel1);
                Edge.Add(EdgeLabel2);
                EdgesGroup.Add(Edge);

                fLabelArray.Remove(Label);
                LabelArray.Add(EdgeLabel2);

            } while (LabelArrayNum < PnList.Count);
            #endregion

            #region 生成MST的nodes和Edges
            int EdgesGroupNum = EdgesGroup.Count;
            List<ProxiEdge> MSTEdgeList = new List<ProxiEdge>();

            for (int i = 0; i < EdgesGroupNum; i++)
            {
                int m, n;
                m = EdgesGroup[i][0];
                n = EdgesGroup[i][1];

                ProxiNode Pn1 = PnList[m];
                ProxiNode Pn2 = PnList[n];

                foreach (ProxiEdge Pe in PeList)
                {
                    if ((Pe.Node1.X == Pn1.X && Pe.Node2.X == Pn2.X) || (Pe.Node1.X == Pn2.X && Pe.Node2.X == Pn1.X))
                    {
                        if (!MSTEdgeList.Contains(Pe))
                        {
                            MSTEdgeList.Add(Pe);
                            break;
                        }
                    }
                }

            }
            #endregion

            this.MSTEdgeList = MSTEdgeList;
        }

        /// <summary>
        /// 创建点集的RNG图（最短距离）
        ///
        ///RNG计算(找到邻近图中每一个三角形，删除三角形中的最长边)
        ///说明：对于任意一条边，找到对应的三角形；如果是最长边，则删除（如果是一条边，保留；如果是两条边，若是最长边，删除）
        /// </summary>
        public void CreateRNG(List<ProxiNode> PnList, List<ProxiEdge> PeList, List<PolygonObject> PoList)
        {
            #region 找到潜在RNG对应的每条边，判断是否是邻近图中三角形对应的最长边，如果是，删除
            for (int i = this.EdgeList.Count - 1; i >= 0; i--)
            {
                #region Dis For EdgeList[i]
                ProxiEdge Edge1 = this.EdgeList[i];

                ProxiNode pPoint1 = Edge1.Node1;
                ProxiNode pPoint2 = Edge1.Node2;

                PolygonObject Po1 = this.GetObjectByID(PoList, pPoint1.TagID);
                PolygonObject Po2 = this.GetObjectByID(PoList, pPoint2.TagID);

                double EdgeDis = this.GetDis(pPoint1, pPoint2);
                double RSDis = Po1.R + Po2.R;

                double CacheDis = EdgeDis - RSDis;
                #endregion

                #region 判断边是否为三角形对应的最长边
                for (int j = 0; j < PnList.Count; j++)
                {
                    bool Label1 = false; bool Label2 = false;
                    double Distance1 = 0; double Distance2 = 0;
                    ProxiNode mPn = PnList[j];
                    if (mPn.X != pPoint1.X && mPn.X != pPoint2.X)
                    {
                        for (int m = 0; m < PeList.Count; m++)
                        {
                            #region mPn与pPoint1是否为边
                            if ((mPn.TagID == PeList[m].Node1.TagID && pPoint1.TagID == PeList[m].Node2.TagID) || mPn.TagID == PeList[m].Node2.TagID && pPoint1.TagID == PeList[m].Node1.TagID)
                            {
                                Label1 = true;

                                ProxiEdge CacheEdge1 = this.EdgeList[m];
                                ProxiNode CachepPoint1 = CacheEdge1.Node1;
                                ProxiNode CachepPoint2 = CacheEdge1.Node2;
                                PolygonObject CachePo1 = this.GetObjectByID(PoList, CacheEdge1.Node1.TagID);
                                PolygonObject CachePo2 = this.GetObjectByID(PoList, CacheEdge1.Node2.TagID);

                                double CacheEdgeDis = this.GetDis(CachepPoint1, CachepPoint2);
                                double CacheRSDis = CachePo1.R + CachePo2.R;
                                Distance1 = CacheEdgeDis - CacheRSDis;
                            }
                            #endregion

                            #region mPn与Pn2是否为边
                            if ((mPn.TagID == PeList[m].Node1.TagID && pPoint2.TagID == PeList[m].Node2.TagID) || mPn.TagID == PeList[m].Node2.TagID && pPoint2.TagID == PeList[m].Node1.TagID)
                            {
                                Label2 = true;

                                ProxiEdge CacheEdge1 = this.EdgeList[m];
                                ProxiNode CachepPoint1 = CacheEdge1.Node1;
                                ProxiNode CachepPoint2 = CacheEdge1.Node2;
                                PolygonObject CachePo1 = this.GetObjectByID(PoList, CacheEdge1.Node1.TagID);
                                PolygonObject CachePo2 = this.GetObjectByID(PoList, CacheEdge1.Node2.TagID);

                                double CacheEdgeDis = this.GetDis(CachepPoint1, CachepPoint2);
                                double CacheRSDis = CachePo1.R + CachePo2.R;
                                Distance2 = CacheEdgeDis - CacheRSDis;
                            }
                            #endregion
                        }
                    }

                    if (Label1 && Label2)
                    {
                        if (CacheDis > Distance1 && CacheDis > Distance2)
                        {
                            this.EdgeList.Remove(Edge1);
                            break;
                        }
                    }
                }
                #endregion
            }
            #endregion
        }

        /// <summary>
        /// 从骨架线构造邻近图
        /// </summary>
        public void CreateProxiGraphfrmSkeletonBuildings(SMap map, Skeleton skeleton)
        {
            this.CreateNodesforPointandPolygon(map);
            this.CreateEdgesforPointandPolygon(skeleton);
            CreateNodesandEdgesforPolyline_LP(map, skeleton);
        }

        /// <summary>
        /// 从骨架线构造邻近图
        /// </summary>
        public void CreateProxiGraphfrmSkeletonForEnrichNetwork(SMap map, Skeleton skeleton)
        {
            this.CreateNodesforPointandPolygon(map);
            this.CreateEdgesforPointandPolygon(skeleton);
            this.CreateNodesandNearestLine2PolylineVertices(map, skeleton);
            this.RemoveSuperfluousEdges();
        }

        /// <summary>
        /// 删除邻近图中多余的边
        /// </summary>
        private void RemoveSuperfluousEdges()
        {
            List<ProxiEdge> edgeList = new List<ProxiEdge>();
            foreach (ProxiEdge curEdge in this.EdgeList)
            {
                if (!this.IsContainEdge(edgeList, curEdge))
                {
                    edgeList.Add(curEdge);
                }
            }
            this.EdgeList = edgeList;
        }

        /// <summary>
        /// 是否包含该边
        /// </summary>
        /// <returns></returns>
        private bool IsContainEdge(List<ProxiEdge> edgeList,ProxiEdge edge)
        {
            if (edgeList == null || edgeList.Count == 0)
                return false;
            foreach (ProxiEdge curEdge in edgeList)
            {
                if ((edge.Node1.ID == curEdge.Node1.ID && edge.Node2.ID == curEdge.Node2.ID) || (edge.Node2.ID == curEdge.Node1.ID && edge.Node1.ID == curEdge.Node2.ID))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 添加点与线、面与线的邻近边和线上的邻近点（仅仅加入与街道垂直的邻近边）
        /// </summary>
        /// <param name="map"></param>
        /// <param name="skeleton"></param>
        private void CreateNodesandEdgesforPolyline_LP(SMap map, Skeleton skeleton)
        {
            if (skeleton == null || skeleton.Skeleton_ArcList == null || skeleton.Skeleton_ArcList.Count == 0)
            {
                return;
            }
            int curTagID = -1;
            FeatureType curType = FeatureType.Unknown;

            ProxiNode node1 = null;
            ProxiNode node2 = null;
            bool isPerpendicular = false;

            ProxiEdge curEdge = null;

            int id = this.NodeList.Count;

            foreach (Skeleton_Arc curArc in skeleton.Skeleton_ArcList)
            {
                if (curArc.LeftMapObj != null && curArc.RightMapObj != null)
                {
                    if (curArc.LeftMapObj.FeatureType == FeatureType.PolylineType &&
                        (curArc.RightMapObj.FeatureType == FeatureType.PointType || curArc.RightMapObj.FeatureType == FeatureType.PolygonType))
                    {
                        curTagID = curArc.LeftMapObj.ID;
                        curType = curArc.LeftMapObj.FeatureType;
                        // node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        PolylineObject curline = map.GetObjectbyID(curTagID, FeatureType.PolylineType) as PolylineObject;

                        curTagID = curArc.RightMapObj.ID;
                        curType = curArc.RightMapObj.FeatureType;
                        node2 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        Node node = ComFunLib.MinDisPoint2Polyline(node2, curline, out isPerpendicular);


                        node1 = new ProxiNode(node.X, node.Y, id, curArc.LeftMapObj.ID, FeatureType.PolylineType);
                        this.NodeList.Add(node1);
                        id++;


                        curEdge = new ProxiEdge(curArc.ID, node1, node2);
                        this.EdgeList.Add(curEdge);
                        node1.EdgeList.Add(curEdge);
                        node2.EdgeList.Add(curEdge);

                        curEdge.NearestEdge = curArc.NearestEdge;
                        curEdge.Weight = curArc.AveDistance;
                        curEdge.Ske_Arc = curArc;

                    }

                    else if (curArc.RightMapObj.FeatureType == FeatureType.PolylineType &&
                        (curArc.LeftMapObj.FeatureType == FeatureType.PointType || curArc.LeftMapObj.FeatureType == FeatureType.PolygonType))
                    {
                        curTagID = curArc.RightMapObj.ID;
                        curType = curArc.RightMapObj.FeatureType;
                        // node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        PolylineObject curline = map.GetObjectbyID(curTagID, FeatureType.PolylineType) as PolylineObject;

                        curTagID = curArc.LeftMapObj.ID;
                        curType = curArc.LeftMapObj.FeatureType;
                        node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);

                        Node node = ComFunLib.MinDisPoint2Polyline(node1, curline, out isPerpendicular);

                        node2 = new ProxiNode(node.X, node.Y, id, curArc.RightMapObj.ID, FeatureType.PolylineType);
                        this.NodeList.Add(node2);
                        id++;


                        curEdge = new ProxiEdge(curArc.ID, node1, node2);
                        this.EdgeList.Add(curEdge);
                        node1.EdgeList.Add(curEdge);
                        node2.EdgeList.Add(curEdge);

                        curEdge.NearestEdge = curArc.NearestEdge;
                        curEdge.Weight = curArc.AveDistance;
                        curEdge.Ske_Arc = curArc;

                    }
                    //eID++;
                }
            }
        }

        /// <summary>
        /// 添加点与线、面与线的邻近边和线上的邻近点（仅仅加入与街道垂直的邻近边）
        /// </summary>
        /// <param name="map"></param>
        /// <param name="skeleton"></param>
        private void CreateNodesandPerpendicular_EdgesforPolyline_(SMap map, Skeleton skeleton)
        {
            if (skeleton == null || skeleton.Skeleton_ArcList == null || skeleton.Skeleton_ArcList.Count == 0)
            {
                return;
            }
            int curTagID = -1;
            FeatureType curType = FeatureType.Unknown;

            ProxiNode node1 = null;
            ProxiNode node2 = null;
            bool isPerpendicular = false;

            ProxiEdge curEdge = null;

            int id = this.NodeList.Count;

            foreach (Skeleton_Arc curArc in skeleton.Skeleton_ArcList)
            {
                if (curArc.LeftMapObj != null && curArc.RightMapObj != null)
                {
                    if (curArc.LeftMapObj.FeatureType == FeatureType.PolylineType &&
                        (curArc.RightMapObj.FeatureType == FeatureType.PointType || curArc.RightMapObj.FeatureType == FeatureType.PolygonType))
                    {
                        curTagID = curArc.LeftMapObj.ID;
                        curType = curArc.LeftMapObj.FeatureType;
                        // node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        PolylineObject curline = map.GetObjectbyID(curTagID, FeatureType.PolylineType) as PolylineObject;

                        curTagID = curArc.RightMapObj.ID;
                        curType = curArc.RightMapObj.FeatureType;
                        node2 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        Node node = ComFunLib.MinDisPoint2Polyline(node2, curline, out isPerpendicular);

                        if (isPerpendicular)//仅仅加入与街道垂直的邻近边
                        {

                            node1 = new ProxiNode(node.X, node.Y, id, curArc.LeftMapObj.ID, FeatureType.PolylineType);
                            this.NodeList.Add(node1);
                            id++;


                            curEdge = new ProxiEdge(curArc.ID, node1, node2);
                            this.EdgeList.Add(curEdge);
                            node1.EdgeList.Add(curEdge);
                            node2.EdgeList.Add(curEdge);

                            curEdge.NearestEdge = curArc.NearestEdge;
                            curEdge.Weight = curArc.AveDistance;
                            curEdge.Ske_Arc = curArc;
                        }
                    }

                    else if (curArc.RightMapObj.FeatureType == FeatureType.PolylineType &&
                        (curArc.LeftMapObj.FeatureType == FeatureType.PointType || curArc.LeftMapObj.FeatureType == FeatureType.PolygonType))
                    {
                        curTagID = curArc.RightMapObj.ID;
                        curType = curArc.RightMapObj.FeatureType;
                        // node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        PolylineObject curline = map.GetObjectbyID(curTagID, FeatureType.PolylineType) as PolylineObject;

                        curTagID = curArc.LeftMapObj.ID;
                        curType = curArc.LeftMapObj.FeatureType;
                        node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);

                        Node node = ComFunLib.MinDisPoint2Polyline(node1, curline, out isPerpendicular);
                        if (isPerpendicular)//仅仅加入与街道垂直的邻近边
                        {
                            node2 = new ProxiNode(node.X, node.Y, id, curArc.RightMapObj.ID, FeatureType.PolylineType);
                            this.NodeList.Add(node2);
                            id++;


                            curEdge = new ProxiEdge(curArc.ID, node1, node2);
                            this.EdgeList.Add(curEdge);
                            node1.EdgeList.Add(curEdge);
                            node2.EdgeList.Add(curEdge);

                            curEdge.NearestEdge = curArc.NearestEdge;
                            curEdge.Weight = curArc.AveDistance;
                            curEdge.Ske_Arc = curArc;
                        }
                    }
                    //eID++;
                }
            }
        }

        /// <summary>
        /// 添加点与线、面与线的邻近边和线上的邻近点（仅仅加入与街道垂直的邻近边）
        /// </summary>
        /// <param name="map"></param>
        /// <param name="skeleton"></param>
        private void CreateNodesandNearestLine2PolylineVertices(SMap map, Skeleton skeleton)
        {
            if (skeleton == null || skeleton.Skeleton_ArcList == null || skeleton.Skeleton_ArcList.Count == 0)
            {
                return;
            }
            int curTagID = -1;
            FeatureType curType = FeatureType.Unknown;

            ProxiNode node1 = null;
            ProxiNode node2 = null;
            ProxiEdge curEdge = null;

            int id = this.NodeList.Count;

            foreach (Skeleton_Arc curArc in skeleton.Skeleton_ArcList)
            {
                if (curArc.LeftMapObj != null && curArc.RightMapObj != null)
                {
                    if (curArc.LeftMapObj.FeatureType == FeatureType.PolylineType &&
                        (curArc.RightMapObj.FeatureType == FeatureType.PointType || curArc.RightMapObj.FeatureType == FeatureType.PolygonType))
                    {
                        curTagID = curArc.LeftMapObj.ID;
                        curType = curArc.LeftMapObj.FeatureType;
                        // node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        PolylineObject curline = map.GetObjectbyID(curTagID, FeatureType.PolylineType) as PolylineObject;

                        curTagID = curArc.RightMapObj.ID;
                        curType = curArc.RightMapObj.FeatureType;

                        node2 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        Node node = ComFunLib.MinDisPoint2PolylineVertices(node2, curline);
                        ProxiNode exitNode = GetContainNode(this.NodeList, node.X, node.Y);
                        if (exitNode == null)
                        {

                            node1 = new ProxiNode(node.X, node.Y, id, curArc.LeftMapObj.ID, FeatureType.PolylineType);
                            node1.SomeValue = node.ID;
                            this.NodeList.Add(node1);
                            id++;
                        }
                        else
                        {
                            node1 = exitNode;
                        }
                        curEdge = new ProxiEdge(curArc.ID, node1, node2);
                        this.EdgeList.Add(curEdge);
                        node1.EdgeList.Add(curEdge);
                        node2.EdgeList.Add(curEdge);

                        curEdge.NearestEdge = curArc.NearestEdge;
                        curEdge.Weight = curArc.AveDistance;
                        curEdge.Ske_Arc = curArc;
            
                    }

                    else if (curArc.RightMapObj.FeatureType == FeatureType.PolylineType &&
                        (curArc.LeftMapObj.FeatureType == FeatureType.PointType || curArc.LeftMapObj.FeatureType == FeatureType.PolygonType))
                    {
                        curTagID = curArc.RightMapObj.ID;
                        curType = curArc.RightMapObj.FeatureType;
                        // node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        PolylineObject curline = map.GetObjectbyID(curTagID, FeatureType.PolylineType) as PolylineObject;

                        curTagID = curArc.LeftMapObj.ID;

                        curType = curArc.LeftMapObj.FeatureType;
                        node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        Node node = ComFunLib.MinDisPoint2PolylineVertices(node1, curline);
                        ProxiNode exitNode = GetContainNode(this.NodeList, node.X, node.Y);
                        if (exitNode == null)
                        {

                            node2 = new ProxiNode(node.X, node.Y, id, curArc.RightMapObj.ID, FeatureType.PolylineType);
                            node2.SomeValue = node.ID;
                            this.NodeList.Add(node2);
                            id++;
                        }
                        else
                        {
                            node2 = exitNode;
                        }
                        curEdge = new ProxiEdge(curArc.ID, node1, node2);
                        this.EdgeList.Add(curEdge);
                        node1.EdgeList.Add(curEdge);
                        node2.EdgeList.Add(curEdge);

                        curEdge.NearestEdge = curArc.NearestEdge;
                        curEdge.Weight = curArc.AveDistance;
                        curEdge.Ske_Arc = curArc;
                    }
                    //eID++;
                }
            }
        }

        /// <summary>
        /// 判断当期的顶点是否已经在关联点集合中
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns></returns>
        private ProxiNode GetContainNode(List<ProxiNode> nodeList, double x, double y)
        {

            if (nodeList == null || nodeList.Count == 0)
            {
                return null;
            }
            foreach (ProxiNode curNode in nodeList)
            {
                // int id = curNode.ID;
                ProxiNode curV = curNode;

                if (Math.Abs((1 - curV.X / x)) <= 0.000001f && Math.Abs((1 - curV.Y / y)) <= 0.000001f)
                {
                    return curV;
                }
            }
            return null;
        }


        /// <summary>
        /// 从骨架线构造邻近图
        /// </summary>
        public void CreateProxiGraphfrmConflicts(SMap map, List<Conflict> conflicts)
        {
            CreateNodes(map);
            CreateEdges(conflicts);
        }

        /// <summary>
        /// 根据索引获取结点
        /// </summary>
        /// <param name="tagID"></param>
        /// <returns></returns>
        public ProxiNode GetNodebyTagID(int tagID)
        {
            foreach (ProxiNode curNode in this.NodeList)
            {
                if (curNode.TagID == tagID)
                {
                    return curNode;
                }
            }
            return null;
        }

        /// <summary>
        /// 根据索引获取结点
        /// </summary>
        /// <param name="tagID"></param>
        /// <returns></returns>
        public ProxiNode GetNodebyTagIDandType(int tagID, FeatureType type)
        {
            foreach (ProxiNode curNode in this.NodeList)
            {
                if (curNode.TagID == tagID && type == curNode.FeatureType)
                {
                    return curNode;
                }
            }
            return null;
        }

        /// <summary>
        /// 根据索引获取结点
        /// </summary>
        /// <param name="tagID"></param>
        /// <returns></returns>
        public ProxiNode GetNodebyID(int ID)
        {
            foreach (ProxiNode curNode in this.NodeList)
            {
                if (curNode.ID == ID)
                {
                    return curNode;
                }
            }
            return null;
        }

        /// <summary>
        /// 根据两端点的索引号获取边
        /// </summary>
        /// <param name="index1"></param>
        /// <param name="index2"></param>
        /// <returns></returns>
        public ProxiEdge GetEdgebyNodeIndexs(int index1, int index2)
        {
            foreach (ProxiEdge edge in this.EdgeList)
            {
                if ((edge.Node1.ID == index1 && edge.Node2.ID == index2) || (edge.Node1.ID == index2 && edge.Node2.ID == index1))
                    return edge;
            }
            return null;
        }

        /// <summary>
        /// 获取所有与node相关联的边
        /// </summary>
        /// <param name="node"></param>
        /// <returns>边序列</returns>
        public List<ProxiEdge> GetEdgesbyNode(ProxiNode node)
        {
            int index = node.ID;
            List<ProxiEdge> resEdgeList = new List<ProxiEdge>();
            foreach (ProxiEdge edge in this.EdgeList)
            {
                if (edge.Node1.ID == index || edge.Node2.ID == index)
                    resEdgeList.Add(edge);
            }
            if (resEdgeList.Count > 0) return resEdgeList;
            else return null;
        }

        /// <summary>
        /// 写入SHP文件
        /// </summary>
        public void WriteProxiGraph2Shp(string filePath, string fileName, ISpatialReference pri)
        {
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);

            ProxiNode.Create_WriteProxiNodes2Shp(filePath, @"Node_" + fileName, this.NodeList, pri);
            ProxiEdge.Create_WriteEdge2Shp(filePath, @"Edges_" + fileName, this.EdgeList, pri);
            if (EdgeList != null && EdgeList.Count > 0)
            {
                if (this.EdgeList[0].NearestEdge != null)
                {
                    ProxiEdge.Create_WriteNearestDis2Shp(filePath, @"Nearest_" + fileName, this.EdgeList, pri);
                }
            }
        }
        /// <summary>
        /// 拷贝邻近图-   //求吸引力-2014-3-20所用
        /// </summary>
        /// <returns></returns>
        public ProxiGraph Copy()
        {
            ProxiGraph pg = new ProxiGraph();
            foreach (ProxiNode node in this.NodeList)
            {
                ProxiNode newNode = new ProxiNode(node.X, node.Y, node.ID, node.TagID, node.FeatureType);
                pg.NodeList.Add(newNode);

            }

            foreach (ProxiEdge edge in this.EdgeList)
            {
                ProxiEdge newedge = new ProxiEdge(edge.ID, this.GetNodebyID(edge.Node1.ID), this.GetNodebyID(edge.Node1.ID));
                pg.EdgeList.Add(newedge);
            }
            return pg;

        }

        /// <summary>
        /// 就算边的权重
        /// </summary>
        public void CalWeightbyNearestDistance()
        {
            foreach (ProxiEdge edge in this.EdgeList)
            {
                edge.Weight = edge.NearestEdge.NearestDistance;
            }
        }

        /// <summary>
        /// 从最小外接矩形中获取相似性信息
        /// </summary>
        public void GetSimilarityInfofrmSMBR(List<SMBR> SMBRList, SMap map)
        {
            foreach (ProxiEdge edge in this.EdgeList)
            {
                int tagID1 = edge.Node1.TagID;
                int tagID2 = edge.Node2.TagID;
                FeatureType type1 = edge.Node1.FeatureType;
                FeatureType type2 = edge.Node2.FeatureType;
                SMBR smbr1 = SMBR.GetSMBR(tagID1, type1, SMBRList);
                SMBR smbr2 = SMBR.GetSMBR(tagID2, type2, SMBRList);


                if (smbr1 == null || smbr2 == null)
                    continue;

                if (type1 == FeatureType.PolygonType && type2 == FeatureType.PolygonType)
                {
                    PolygonObject obj1 = PolygonObject.GetPPbyID(map.PolygonList, tagID1);
                    PolygonObject obj2 = PolygonObject.GetPPbyID(map.PolygonList, tagID2);
                    double A1 = smbr1.Direct1;
                    double A2 = smbr2.Direct1;
                    int EN1 = obj1.PointList.Count;
                    int EN2 = obj2.PointList.Count;
                    double Area1 = obj1.Area;
                    double Area2 = obj2.Area;
                    double Peri1 = obj1.Perimeter;
                    double Peri2 = obj2.Perimeter;

                    if (EN1 > EN2)
                    {
                        int temp;
                        temp = EN1;
                        EN1 = EN2;
                        EN2 = temp;
                    }
                    if (Area1 > Area2)
                    {
                        double temp;
                        temp = Area1;
                        Area1 = Area2;
                        Area2 = temp;
                    }
                    if (Peri1 > Peri2)
                    {
                        double temp;
                        temp = Peri1;
                        Peri1 = Peri2;
                        Peri2 = temp;
                    }

                    double a = Math.Abs(A1 - A2);
                    if (a > Math.PI / 2)
                    {
                        a = Math.PI - a;
                    }
                    edge.W_A_Simi = 2 * a / Math.PI;

                    edge.W_Area_Simi = Area1 / Area2;
                    edge.W_EdgeN_Simi = EN1 * 1.0 / EN2;
                    edge.W_Peri_Simi = Peri1 / Peri2;

                    edge.CalWeight();//重新计算全重
                }

                else if (type1 == FeatureType.PolylineType && type2 == FeatureType.PolylineType)
                {
                    //待续
                }
                //线线之间相似性，讨论线面之间，
                else if (type1 == FeatureType.PolygonType && type2 == FeatureType.PolylineType)
                {
                    //待续
                }

                else if (type1 == FeatureType.PolylineType && type2 == FeatureType.PolygonType)
                {
                    //待续
                }
            }
        }

        /// <summary>
        /// 用分组信息对邻近图进行优化-04-19
        /// </summary>
        /// <param name="groups"></param>
        public void OptimizeGraphbyBuildingGroups(List<GroupofMapObject> groups,SMap map)
        {
            if (groups == null || groups.Count == 0)
                return;
            foreach (GroupofMapObject curGroup in groups)
            {
                if (curGroup.ListofObjects == null || curGroup.ListofObjects.Count == 0)
                    continue;
                //获取图中对应的结点
                List<ProxiNode> curNodeList = new List<ProxiNode>();
                int tagID = curGroup.ID;
                foreach (MapObject curO in curGroup.ListofObjects)
                {
                    PolygonObject curB = curO as PolygonObject;
                    int curTagId = curB.ID;
                    FeatureType curType = curB.FeatureType;

                    ProxiNode curNode = this.GetNodebyTagIDandType(curTagId, curType);
                    curNodeList.Add(curNode);
                }

                List<ProxiEdge> curIntraEdgeList = new List<ProxiEdge>();
                List<ProxiEdge> curInterEdgeList = new List<ProxiEdge>();
                List<ProxiNode> curNeighbourNodeList = new List<ProxiNode>();//与组内邻近但在组外的结点
                List<ProxiNode> curNeighbourBoundaryNodeList = new List<ProxiNode>();
                foreach (ProxiEdge curEdge in this.EdgeList)
                {
                    ProxiNode sN = curEdge.Node1;
                    ProxiNode eN = curEdge.Node2;
                    bool f1 = this.IsContainNode(curNodeList, sN);
                    bool f2 = this.IsContainNode(curNodeList, eN);
                    if (f1 == true && f2 == true)
                    {
                        curIntraEdgeList.Add(curEdge);
                    }
                    else if (f1 == false && f2 == false)
                    {

                    }
                    else
                    {
                        curInterEdgeList.Add(curEdge);
                        if (f1 == true && f2 == false)
                        {
                            if (!this.IsContainNode(curNeighbourNodeList, eN))
                            {
                                curNeighbourNodeList.Add(eN);
                            }
                            else
                            {
                                if (eN.FeatureType == FeatureType.PolylineType)
                                    curNeighbourBoundaryNodeList.Add(eN);
                            }
                        }
                        else
                        {
                            if (!this.IsContainNode(curNeighbourNodeList, sN))
                            {
                                curNeighbourNodeList.Add(sN);
                            }
                            else
                            {
                                if (sN.FeatureType == FeatureType.PolylineType)
                                    curNeighbourBoundaryNodeList.Add(sN);
                            }
                        }
                    }

                }


                ProxiNode groupNode = AuxStructureLib.ComFunLib.CalGroupCenterPoint(curNodeList);
                groupNode.TagID = tagID;
                groupNode.FeatureType = FeatureType.Group;
                this.NodeList.Add(groupNode);//加入结点

                foreach (ProxiNode curNeighbouringNode in curNeighbourNodeList)
                {
                    if (curNeighbouringNode.FeatureType == FeatureType.PolygonType||curNeighbouringNode.FeatureType == FeatureType.PointType||curNeighbouringNode.FeatureType==FeatureType.Group)
                    {
                        ProxiEdge newEdge = new ProxiEdge(-1, groupNode, curNeighbouringNode);
                        this.EdgeList.Add(newEdge);
                    }
                    else if (curNeighbouringNode.FeatureType == FeatureType.PolylineType)
                    {
                        int curTagID = curNeighbouringNode.TagID;
                        FeatureType curType = FeatureType.PolylineType;
                        // node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        PolylineObject curline = map.GetObjectbyID(curTagID, FeatureType.PolylineType) as PolylineObject;
                        bool isPerpendicular = true;
                        Node newNode = ComFunLib.MinDisPoint2Polyline(groupNode, curline, out isPerpendicular);
                        ProxiNode nodeonLine = new ProxiNode(newNode.X, newNode.Y, -1, curTagID, FeatureType.PolylineType);
                        this.NodeList.Add(nodeonLine);
                        ProxiEdge newEdge = new ProxiEdge(-1, groupNode, nodeonLine);
                        this.EdgeList.Add(newEdge);

                        this.NodeList.Remove(curNeighbouringNode);
                    }
                }

                foreach (ProxiEdge edge in curIntraEdgeList)
                {
                    this.EdgeList.Remove(edge);

                }
                foreach (ProxiEdge edge in curInterEdgeList)
                {
                    this.EdgeList.Remove(edge);

                }
                foreach (ProxiNode node in curNodeList)
                {
                    this.NodeList.Remove(node);
                }
                foreach (ProxiNode node in curNeighbourBoundaryNodeList)
                {
                    this.NodeList.Remove(node);
                }
                int nodeID=0;
                int edgeID=0;

                foreach (ProxiNode node in this.NodeList)
                {
                    node.ID = nodeID;
                    nodeID++;
                }
                
                 foreach (ProxiEdge edge in this.EdgeList)
                {
                    edge.ID = edgeID;
                    edgeID++;

                }
            }
        }

        /// <summary>
        /// 化简边
        /// </summary>
        /// <param name="MaxDistance"></param>
        private void SimplifyPG(double MaxDistance)
        {
            List<ProxiEdge> delEdgeList = new List<ProxiEdge>();
            foreach(ProxiEdge curEdge in this.EdgeList)
            {
                delEdgeList.Add(curEdge);
            }
            foreach (ProxiEdge delcurEdge in delEdgeList)
            {
                this.EdgeList.Remove(delcurEdge);
            }
        }
        
        /// <summary>
        /// 判断结点集合中是否含有结点-用于分组优化函数;OptimizeGraphbyBuildingGroups
        /// </summary>
        /// <param name="nodeList">结点集合</param>
        /// <param name="node">结点</param>
        /// <returns></returns>
        private bool IsContainNode(List<ProxiNode> nodeList, ProxiNode node)
        {
            if (nodeList == null || nodeList.Count == 0)
                return false;

            foreach (ProxiNode curNode in nodeList)
            {
                if (curNode.TagID==node.TagID&&curNode.FeatureType==node.FeatureType)//线上的结点
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 创建ProxiG （依据拓扑关系创建邻近图）
        /// Td表示邻接的参数
        /// </summary>
        /// <param name="pFeatureClass">原始图层</param>
        public void CreateProxiG(IFeatureClass pFeatureClass,double Td)
        {
            #region Create ProxiNodes
            for (int i = 0; i < pFeatureClass.FeatureCount(null); i++)
            {
                IArea pArea = pFeatureClass.GetFeature(i).Shape as IArea;
                ProxiNode CacheNode = new ProxiNode(pArea.Centroid.X, pArea.Centroid.Y, i, i);
                this.NodeList.Add(CacheNode);
            }
            #endregion

            #region Create ProxiEdges
            int edgeID = 0;
            for (int i = 0; i < pFeatureClass.FeatureCount(null)-1; i++)
            {
                for (int j = i+1; j < pFeatureClass.FeatureCount(null); j++)
                {
                    if (j != i)
                    {
                        IGeometry iGeo = pFeatureClass.GetFeature(i).Shape;
                        IGeometry jGeo = pFeatureClass.GetFeature(j).Shape;

                        IRelationalOperator iRo = iGeo as IRelationalOperator;
                        if (iRo.Touches(jGeo) || iRo.Overlaps(jGeo))
                        {
                            ITopologicalOperator iTo = iGeo as ITopologicalOperator;
                            IGeometry pGeo = iTo.Intersect(jGeo, esriGeometryDimension.esriGeometry1Dimension) as IGeometry;
                            IPolyline pPolyline = pGeo as IPolyline;

                            IPolygon iPo=iGeo as IPolygon;IPolygon jPo=jGeo as IPolygon;
                            if (pPolyline.Length / iPo.Length > Td || pPolyline.Length / jPo.Length > Td)
                            {
                                ProxiEdge CacheEdge = new ProxiEdge(edgeID, this.NodeList[i], this.NodeList[j]);
                                this.EdgeList.Add(CacheEdge);
                            }
                        }
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// 创建ProxiG
        /// </summary>
        /// <param name="pFeatureClass">原始图层</param>
        public void CreateProxiGByDT(IFeatureClass pFeatureClass)
        {
            #region Create ProxiNodes
            List<TriNode> TriNodeList = new List<TriNode>();
            for (int i = 0; i < pFeatureClass.FeatureCount(null); i++)
            {
                IArea pArea = pFeatureClass.GetFeature(i).Shape as IArea;
                ProxiNode CacheNode = new ProxiNode(pArea.Centroid.X, pArea.Centroid.Y, i, i);
                TriNode CacheTriNode = new TriNode(pArea.Centroid.X, pArea.Centroid.Y, i, i);
                this.NodeList.Add(CacheNode);
                TriNodeList.Add(CacheTriNode);
            }
            #endregion

            #region Create ProxiEdges
            DelaunayTin dt = new DelaunayTin(TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);
                     
            int edgeID = 0;
            for (int i = 0; i < dt.TriEdgeList.Count; i++)
            {
                TriEdge tE = dt.TriEdgeList[i];
                
                if (!this.repeatEdge(tE, this.EdgeList))
                {                    
                    ProxiEdge CacheEdge = new ProxiEdge(edgeID, this.NodeList[tE.startPoint.TagValue], this.NodeList[tE.endPoint.TagValue]);
                    //CacheEdge.adajactLable = true;
                    this.EdgeList.Add(CacheEdge);
                    edgeID++;
                }
            }
            #endregion
        }

        /// <summary>
        /// 创建ProxiG
        /// </summary>
        /// <param name="pFeatureClass">原始图层</param>
        public void CreateProxiGByDT(List<PolygonObject> PoList)
        {
            #region Create ProxiNodes
            List<TriNode> TriNodeList = new List<TriNode>();
            for (int i = 0; i < PoList.Count; i++)
            {
                ProxiNode CacheNode = new ProxiNode(PoList[i].CalProxiNode().X, PoList[i].CalProxiNode().Y, i, i);
                TriNode CacheTriNode = new TriNode(PoList[i].CalProxiNode().X, PoList[i].CalProxiNode().Y, i, i);
                this.NodeList.Add(CacheNode);
                TriNodeList.Add(CacheTriNode);
            }
            #endregion

            #region Create ProxiEdges
            DelaunayTin dt = new DelaunayTin(TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);

            int edgeID = 0;
            for (int i = 0; i < dt.TriEdgeList.Count; i++)
            {
                TriEdge tE = dt.TriEdgeList[i];

                if (!this.repeatEdge(tE, this.EdgeList))
                {
                    ProxiEdge CacheEdge = new ProxiEdge(edgeID, this.NodeList[tE.startPoint.TagValue], this.NodeList[tE.endPoint.TagValue]);
                    //CacheEdge.adajactLable = true;
                    this.EdgeList.Add(CacheEdge);
                    edgeID++;
                }
            }
            #endregion
        }

        /// <summary>
        /// 创建ProxiG
        /// </summary>
        /// <param name="pFeatureClass">原始图层</param>
        public void CreateProxiGByDTConsiTop(IFeatureClass pFeatureClass)
        {
            #region Create ProxiNodes
            List<TriNode> TriNodeList = new List<TriNode>();
            for (int i = 0; i < pFeatureClass.FeatureCount(null); i++)
            {
                IArea pArea = pFeatureClass.GetFeature(i).Shape as IArea;
                ProxiNode CacheNode = new ProxiNode(pArea.Centroid.X, pArea.Centroid.Y, i, i);
                TriNode CacheTriNode = new TriNode(pArea.Centroid.X, pArea.Centroid.Y, i, i);
                this.NodeList.Add(CacheNode);
                TriNodeList.Add(CacheTriNode);
            }
            #endregion

            #region Create ProxiEdges
            DelaunayTin dt = new DelaunayTin(TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);

            int edgeID = 0;
            for (int i = 0; i < dt.TriEdgeList.Count; i++)
            {
                TriEdge tE = dt.TriEdgeList[i];
                if (!this.repeatEdge(tE, this.EdgeList))
                {
                   
                    IGeometry iGeo = pFeatureClass.GetFeature(tE.startPoint.TagValue).Shape;
                    IGeometry jGeo = pFeatureClass.GetFeature(tE.endPoint.TagValue).Shape;

                    IRelationalOperator iRo = iGeo as IRelationalOperator;
                    if (iRo.Touches(jGeo) || iRo.Overlaps(jGeo))
                    {
                        ProxiEdge CacheEdge = new ProxiEdge(edgeID, this.NodeList[tE.startPoint.TagValue], this.NodeList[tE.endPoint.TagValue]);
                        CacheEdge.adajactLable = true;
                        this.EdgeList.Add(CacheEdge);
                        edgeID++;
                    }

                    else
                    {
                        ProxiEdge CacheEdge = new ProxiEdge(edgeID, this.NodeList[tE.startPoint.TagValue], this.NodeList[tE.endPoint.TagValue]);
                        CacheEdge.adajactLable = false;
                        this.EdgeList.Add(CacheEdge);
                        edgeID++;
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// 判断给定的边是否是当前邻近图中的重复边
        /// </summary>
        /// <param name="Pe"></param>
        /// <param name="EdgeList"></param>
        public bool repeatEdge(ProxiEdge Pe,List<ProxiEdge> EdgeList)
        {
            bool repeatLabel = false;
            foreach(ProxiEdge CachePe in EdgeList)
            {
                if((CachePe.Node1.TagID==Pe.Node1.TagID && CachePe.Node2.TagID==Pe.Node2.TagID)||
                    (CachePe.Node1.TagID==Pe.Node2.TagID && CachePe.Node2.TagID==Pe.Node1.TagID))
                {
                    repeatLabel = true;
                    break;
                }
            }

            return repeatLabel;
        }

        /// <summary>
        /// 判断给定的边是否是当前邻近图中的重复边
        /// </summary>
        /// <param name="Pe"></param>
        /// <param name="EdgeList"></param>
        /// true 重复；False不重复
        public bool repeatEdge(TriEdge tP, List<ProxiEdge> EdgeList)
        {
            bool repeatLabel = false;
            foreach (ProxiEdge CachePe in EdgeList)
            {
                if ((CachePe.Node1.TagID == tP.startPoint.TagValue && CachePe.Node2.TagID == tP.endPoint.TagValue) ||
                    (CachePe.Node1.TagID == tP.endPoint.TagValue && CachePe.Node2.TagID == tP.startPoint.TagValue))
                {
                    repeatLabel = true;
                    break;
                }
            }

            return repeatLabel;
        }

        /// <summary>
        /// 计算两个点的距离
        /// </summary>
        /// <param name="Node1"></param>
        /// <param name="Node2"></param>
        /// <returns></returns>
        double GetDis(ProxiNode Node1, ProxiNode Node2)
        {
            double Dis = Math.Sqrt((Node1.X - Node2.X) * (Node1.X - Node2.X) + (Node1.Y - Node2.Y) * (Node1.Y - Node2.Y));
            return Dis;
        }

        /// <summary>
        /// 依据ID获取对应的Circle
        /// </summary>
        /// <param name="PoList"></param>
        /// <param name="ID"></param>
        /// <returns></returns>
        PolygonObject GetObjectByID(List<PolygonObject> PoList, int ID)
        {
            bool NullLabel = false; int TID = 0;
            for (int i = 0; i < PoList.Count; i++)
            {
                if (PoList[i].ID == ID)
                {
                    NullLabel = true;
                    TID = ID;
                    break;
                }
            }

            if (!NullLabel)
            {
                return null;
            }
            else
            {
                return PoList[TID];
            }
        }

        /// <summary>
        /// /// <summary>
        /// 删除较长的边(距离计算考虑了圆的半径)
        /// </summary>EdgeList=邻近图的边
        /// </summary>PoList=circles
        /// <param name="Td">边的阈值条件</param>
        public void DeleteLongerEdges(List<ProxiEdge> EdgeList,List<PolygonObject> PoList, double Td)
        {
            for (int i = EdgeList.Count - 1; i >= 0; i--)
            {
                ProxiNode Node1 = EdgeList[i].Node1;
                ProxiNode Node2 = EdgeList[i].Node2;

                double EdgeDis = this.GetDis(Node1, Node2);
                double RSDis = this.GetObjectByID(PoList,Node1.ID).R + this.GetObjectByID(PoList,Node2.ID).R;

                if ((EdgeDis-RSDis) > Td)
                {
                    EdgeList.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 删除穿过给定圆的边
        /// </summary>
        /// <param name="EdgeList"></param>
        /// <param name="PoList"></param>
        public void DeleteCrossEdge(List<ProxiEdge> EdgeList, List<PolygonObject> PoList)
        {
            ComFunLib CFL=new ComFunLib();
            for (int i = EdgeList.Count - 1; i >= 0; i--)
            {
                ProxiNode Node1 = EdgeList[i].Node1;
                ProxiNode Node2 = EdgeList[i].Node2;
                double R1 = this.GetObjectByID(PoList, Node1.ID).R;
                double R2 = this.GetObjectByID(PoList, Node2.ID).R; 

                foreach(PolygonObject Po in PoList)
                {
                    ProxiNode PoNode = Po.CalProxiNode();

                    double EdgeDis1 = this.GetDis(Node1, PoNode);
                    double RSDis1 = this.GetObjectByID(PoList, Node1.ID).R + Po.R;
                    double EdgeDis2 = this.GetDis(Node2, PoNode);
                    double RSDis2 = this.GetObjectByID(PoList, Node2.ID).R + Po.R;

                    #region 给定圆与特定圆不重合
                    if (EdgeDis1 > RSDis1 && EdgeDis2 > RSDis2)
                    {
                        double KDis = CFL.pCalMinDisPoint2Line(PoNode, Node1, Node2);
                        if (KDis < Po.R)
                        {
                            EdgeList.RemoveAt(i);
                        }
                    }
                    #endregion
                }
            }
        }

        /// <summary>
        /// 依据冲突refine proximity graph
        /// 虽然不邻接，但是冲突的Circles
        /// </summary>
        /// <param name="PoList"></param>
        public void PgRefined(List<PolygonObject> PoList)
        {
            for (int i = 0; i < PoList.Count-1; i++)
            {
                for (int j = i + 1; j < PoList.Count; j++)
                {
                    ProxiNode Node1 = this.NodeList[PoList[i].ID];
                    ProxiNode Node2 = this.NodeList[PoList[j].ID];

                    double EdgeDis = this.GetDis(Node1, Node2);
                    double RSDis =PoList[i].R + PoList[j].R;

                    if (EdgeDis < RSDis)
                    {
                        ProxiEdge rPe = new ProxiEdge(this.EdgeList.Count, Node1, Node2);
                        if (!this.repeatEdge(rPe, this.EdgeList))
                        {
                            rPe.StepOverLap = true;
                            this.EdgeList.Add(rPe);
                        }                     
                    }
                }
            }
        }

        /// <summary>
        /// 依据冲突refine proximity graph
        /// </summary>
        /// <param name="PoList"></param>
        public void GroupPgRefined(List<PolygonObject> PoList)
        {
            for (int i = 0; i < PoList.Count - 1; i++)
            {
                for (int j = i + 1; j < PoList.Count; j++)
                {
                    if (!this.GroupPos(PoList[i], PoList[j]))
                    {
                        ProxiNode Node1 = this.GetTarNode(PoList[i].ID);
                        ProxiNode Node2 = this.GetTarNode(PoList[j].ID);

                        double EdgeDis = this.GetDis(Node1, Node2);
                        double RSDis = PoList[i].R + PoList[j].R;

                        if (EdgeDis < RSDis + 0.05)
                        {
                            ProxiEdge rPe = new ProxiEdge(this.EdgeList.Count, Node1, Node2);
                            if (!this.repeatEdge(rPe, this.EdgeList))
                            {
                                rPe.StepOverLap = true;
                                this.EdgeList.Add(rPe);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 判断两个建筑物是否是同一个Group中的建筑物
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <returns></returns>
        public bool GroupPos(PolygonObject Po1, PolygonObject Po2)
        {
            bool GroupLable = false;

            for (int i = 0; i < this.NodeList.Count; i++)
            {
                if (this.NodeList[i].TagIds.Contains(Po1.ID) && this.NodeList[i].TagIds.Contains(Po2.ID))
                {
                    GroupLable = true;
                    break;
                }
            }

            return GroupLable;
        }

        /// <summary>
        /// 返回建筑物对应Node
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public ProxiNode GetTarNode(int Id)
        {
            foreach (ProxiNode Pn in this.NodeList)
            {
                if (Pn.TagIds.Contains(Id))
                {
                    return Pn;
                }
            }

            return null;
        }

        /// <summary>
        /// 删除上一步因重叠新添加的边
        /// </summary>
        public void OverlapDelete()
        {
            for (int i = this.EdgeList.Count - 1; i >= 0; i--)
            {
                ProxiEdge Pe = this.EdgeList[i];
                if (Pe.StepOverLap)
                {
                    this.EdgeList.Remove(Pe);
                }
            }
        }

        /// <summary>
        /// 依据邻近关系refine proximity graph
        /// </summary>
        /// <param name="EdgeList"></param>
        public void PgRefined(List<ProxiEdge> pEdgeList)
        {
            foreach (ProxiEdge rPe in pEdgeList)
            {
                if (!this.repeatEdge(rPe, this.EdgeList))
                {
                    ProxiEdge cachePe = new ProxiEdge(this.EdgeList.Count, this.NodeList[rPe.Node1.ID], this.NodeList[rPe.Node2.ID]);
                    this.EdgeList.Add(cachePe);
                }
            }
        }

        /// <summary>
        /// 依据MST边标识属性
        /// </summary>
        /// <param name="EdgeList"></param>
        public void MSTPgAttri(List<ProxiEdge> pEdgeList)
        {
            foreach (ProxiEdge rPe in this.EdgeList)
            {
                if (this.repeatEdge(rPe, pEdgeList))
                {
                    rPe.MSTLable = true;
                }
            }
        }

        /// <summary>
        /// 依据邻近边标识属性
        /// </summary>
        /// <param name="EdgeList"></param>
        public void PrPgAttri(List<ProxiEdge> pEdgeList)
        {
            foreach (ProxiEdge rPe in this.EdgeList)
            {
                if (this.repeatEdge(rPe, pEdgeList))
                {
                    rPe.adajactLable = true;
                }
            }
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
        ///将邻近图中邻接的邻近图分组 
        /// </summary>
        /// <returns></returns>
        public List<ProxiGraph> GetGroupPg()
        {
            ProxiGraph CopyPg = Clone((object)this) as ProxiGraph;
            List<ProxiGraph> subPgList = new List<ProxiGraph>();

            #region 求解过程
            while (CopyPg.NodeList.Count > 0)
            {
                List<ProxiNode> NodeToContinue = new List<ProxiNode>();
                NodeToContinue.Add(CopyPg.NodeList[0]);
                ProxiGraph CachePg = new ProxiGraph();
                while (NodeToContinue.Count > 0)
                {
                    if (!CachePg.NodeList.Contains(NodeToContinue[0]))
                    {
                        CachePg.NodeList.Add(NodeToContinue[0]);
                    }

                    for (int i = CopyPg.EdgeList.Count - 1; i >= 0;i-- )
                    {
                        ProxiEdge Pe = CopyPg.EdgeList[i];
                        if (Pe.Node1.ID == NodeToContinue[0].ID)
                        {
                            if (!CachePg.EdgeList.Contains(Pe))
                            {
                                CachePg.EdgeList.Add(Pe);
                                CopyPg.EdgeList.Remove(Pe);
                            }

                            if (!CachePg.NodeList.Contains(Pe.Node2))
                            {
                                CachePg.NodeList.Add(Pe.Node2);
                                NodeToContinue.Add(Pe.Node2);                               
                            }

                        }
                        else if (Pe.Node2.ID == NodeToContinue[0].ID)
                        {
                            if (!CachePg.EdgeList.Contains(Pe))
                            {
                                CachePg.EdgeList.Add(Pe);
                                CopyPg.EdgeList.Remove(Pe);
                            }

                            if (!CachePg.NodeList.Contains(Pe.Node1))
                            {
                                CachePg.NodeList.Add(Pe.Node1);                                
                                NodeToContinue.Add(Pe.Node1);                              
                            }
                        }
                    }

                    CopyPg.NodeList.Remove(NodeToContinue[0]);
                    NodeToContinue.Remove(NodeToContinue[0]);                    
                }

                subPgList.Add(CachePg);
            }
            #endregion

            return subPgList;
        }

        /// <summary>
        /// 依据
        /// </summary>
        public void PgReConstruction(List<List<PolygonObject>> Circles,ProxiGraph OldPg)
        {
            #region Create ProxiNodes
            List<TriNode> TriNodeList = new List<TriNode>();
            for (int i = 0; i <Circles.Count; i++)
            {
                ProxiNode CacheNode = this.GetCenter(Circles[i]); CacheNode.ID = i; CacheNode.TagID = i;
                TriNode CacheTriNode = new TriNode(CacheNode.X, CacheNode.Y, i, i);
                this.NodeList.Add(CacheNode);
                TriNodeList.Add(CacheTriNode);
            }
            #endregion

            #region Create ProxiEdges
 
            for (int i = 0; i < OldPg.EdgeList.Count; i++)
            {
                ProxiNode Node1 = OldPg.EdgeList[i].Node1;
                ProxiNode Node2 = OldPg.EdgeList[i].Node2;

                #region 确定对应的Node
                int TagID1=-1;int TagID2=-1;
                bool bID1 = false; bool bID2 = false;
                for (int j = 0; j < this.NodeList.Count; j++)
                {
                    if (this.NodeList[j].TagIds.Contains(Node1.ID))
                    {
                        TagID1 = j;
                        bID1 = true;
                    }
                    if (this.NodeList[j].TagIds.Contains(Node2.ID))
                    {
                        TagID2 = j;
                        bID2 = true;
                    }

                    if (bID1 && bID2)
                    {
                        break;
                    }
                }
                #endregion
                if (TagID1 != TagID2)//同一个群中邻近边需删除
                {
                    ProxiEdge rPe = new ProxiEdge(this.EdgeList.Count, this.NodeList[TagID1], this.NodeList[TagID2]);
                    if (!this.repeatEdge(rPe, this.EdgeList))
                    {
                        this.EdgeList.Add(rPe);
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// 获取给定的PoList的中心
        /// </summary>
        /// <param name="PoList"></param>
        /// <returns></returns>
        public ProxiNode GetCenter(List<PolygonObject> PoList)
        {
            double RSum = 0;
            for (int i = 0; i < PoList.Count; i++)
            {
                RSum = RSum + PoList[i].R;
            }

            double X = 0; double Y = 0; List<int> PoIds = new List<int>();
            for (int i = 0; i < PoList.Count; i++)
            {
                X = PoList[i].CalProxiNode().X * (PoList[i].R / RSum) + X;
                Y = PoList[i].CalProxiNode().Y * (PoList[i].R / RSum) + Y;
                PoIds.Add(PoList[i].ID);
            }

            ProxiNode CacheNode = new ProxiNode(X, Y);
            CacheNode.TagIds = PoIds;
            return CacheNode;
        }
    }
}
