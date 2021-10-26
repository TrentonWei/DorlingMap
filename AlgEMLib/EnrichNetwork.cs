using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;
using AuxStructureLib.ConflictLib;
using ESRI.ArcGIS.Geometry;

namespace AlgEMLib
{
    /// <summary>
    /// 增强网络：
    /// 基于能量最小的道路网移位在传播上存在无法向直线距离邻近而网络路程较远的路段上传播，
    /// 也无法向邻近其他目标的传播，比如街边建筑物
    /// 因此需要基于邻近关系构建增强型道路网，使移位得到充分传播。
    /// </summary>
    public class EnrichNetwork
    {
        /// <summary>
        /// 地图对象字段
        /// </summary>
        private SMap map = null;

        /// <summary>
        /// 地图对象字段
        /// </summary>
        private SMap map1 = null;
        /// <summary>
        /// 骨架线对象
        /// </summary>
        private  AuxStructureLib.Skeleton skeleton=null;

        /// <summary>
        /// 骨架线对象1
        /// </summary>
        private AuxStructureLib.Skeleton skeleton1 = null;
        /// <summary>
        /// 冲突探测对象字段
        /// </summary>
        private ConflictDetector conflictDetector=null;
        /// <summary>
        /// 地图对象属性
        /// </summary>
        public SMap Map
        {
            get
            {
                return map;
            }
            set
            {
                map = value;
            }
        }
        /// <summary>
        /// 冲突探测对象属性
        /// </summary>
        public ConflictDetector ConflictDetector
        {
            get
            {
                return conflictDetector;
            }
            set
            {
                conflictDetector = value;
            }
        }
        /// <summary>
        /// 骨架线属性
        /// </summary>
        public Skeleton Skeleton
        {
            get
            {
                return skeleton;
            }
            set
            {
                skeleton = value;
            }
        }

        /// <summary>
        /// 骨架线属性
        /// </summary>
        public Skeleton Skeleton1
        {
            get
            {
                return skeleton1;
            }
            set
            {
                skeleton1 = value;
            }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="map"></param>
        /// <param name="cd"></param>
        public EnrichNetwork(SMap map, ConflictDetector cd)
        {
            this.map = map;
            this.conflictDetector = cd;
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="map"></param>
        /// <param name="cd"></param>
        public EnrichNetwork(SMap map, SMap map1, ConflictDetector cd, Skeleton ske, Skeleton ske1)
        {
            this.map = map;
            this.map1 = map1;
            this.conflictDetector = cd;
            this.skeleton = ske;
            this.skeleton1 = ske1;
        }
        /// <summary>
        /// 找出三角网潜在冲突区中的瓶颈三角形并形成桥接边加入道路网中
        /// </summary>
        /// <param name="k">相对于间距的倍数</param>
        public void AddBottle_Neck_Lines(int k)
        {
              //潜在冲突
            List<ConflictBase> potentialConflicts=this.conflictDetector.DetectPotentialConflict(k);
            //int ct = 0;
            //foreach (AuxStructureLib.ConflictLib.Conflict_L cl in conflictDetector.ConflictList)
            //{
            //    (cl as AuxStructureLib.ConflictLib.Conflict_L).WriteConflict2Shp(strPath + @"\PotentialConflict", ct);
            //    ct++;
            //}
            #region 找出瓶颈三角形
            List<Triangle> Bottle_NeckTList = new List<AuxStructureLib.Triangle>();
            foreach (AuxStructureLib.ConflictLib.ConflictBase cl in potentialConflicts)
            {
                if (cl is Conflict_L)
                {
                    List<Triangle> triList = (cl as AuxStructureLib.ConflictLib.Conflict_L).TriangleList;
                    triList = ComFunLib.FindBottle_NeckTriangle(triList);
                    Bottle_NeckTList.AddRange(triList);
                }
            }
            #endregion
            //Triangle.Create_WriteTriange2Shp(strPath, @"Bottle_NeckTriangle", Bottle_NeckTList, esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);

            #region 找到瓶颈边
            List<TriEdge> Bottle_NeckEdgeList = new List<TriEdge>();
            foreach (Triangle curTri in Bottle_NeckTList)
            {
                if (curTri.TriType == 1)//如果是Tunk三角形
                {
                    //找到其中较短的一条非约束边
                    double minEdgeLength=double.PositiveInfinity;
                    TriEdge curBottle_Neck = null;
                    double curEdgeLength=-1;
                    if (curTri.edge1.tagID == -1)
                    {
                           curEdgeLength=curTri.edge1.Length;
                        if (curEdgeLength < minEdgeLength)
                        {
                            minEdgeLength=curEdgeLength;
                            curBottle_Neck = curTri.edge1;
                        }
                    }
                    if (curTri.edge2.tagID == -1)
                    {
                        curEdgeLength = curTri.edge2.Length;
                        if (curEdgeLength < minEdgeLength)
                        {
                            minEdgeLength = curEdgeLength;
                            curBottle_Neck = curTri.edge2;
                        }
                    }
                    if (curTri.edge3.tagID == -1)
                    {
                        curEdgeLength = curTri.edge3.Length;
                        if (curEdgeLength < minEdgeLength)
                        {
                            minEdgeLength = curEdgeLength;
                            curBottle_Neck = curTri.edge3;
                        }
                       
                    }
                    if (curBottle_Neck!=null&&!this.IsContain(curBottle_Neck, Bottle_NeckEdgeList))
                    {
                        Bottle_NeckEdgeList.Add(curBottle_Neck);
                    }
                }
                else if (curTri.TriType == 0)//如果是Branch三角形
                {
                    if (curTri.edge1.startPoint.TagValue!=curTri.edge1.endPoint.TagValue)
                    {
                        if (!IsContain(curTri.edge1, Bottle_NeckEdgeList))
                        {
                            Bottle_NeckEdgeList.Add(curTri.edge1);
                        }
                    }
                    if (curTri.edge2.startPoint.TagValue!=curTri.edge2.endPoint.TagValue)
                    {
                        if (!IsContain(curTri.edge2, Bottle_NeckEdgeList))
                        {
                            Bottle_NeckEdgeList.Add(curTri.edge2);
                        }
                    }
                    if (curTri.edge3.startPoint.TagValue!=curTri.edge3.endPoint.TagValue)
                    {
                        if (!IsContain(curTri.edge3, Bottle_NeckEdgeList))
                        {
                            Bottle_NeckEdgeList.Add(curTri.edge3);
                        }
                    }
                }
            }
            #endregion

            #region 将瓶颈边加入地图对象的线目标集合中
            foreach (TriEdge curEdge in Bottle_NeckEdgeList)
            {
                PolylineObject line1=null;
                PolylineObject line2=null;
                PolylineObject line = this.CutPolyline22(map, out line1, out line2, curEdge.startPoint);
                if (line != null)
                {
                    map.PolylineList.Remove(line);
                    map.PolylineList.Add(line1);
                    map.PolylineList.Add(line2);
                }

                 line1 = null;
                 line2 = null;
                 line = this.CutPolyline22(map, out line1, out line2, curEdge.endPoint);
                if (line != null)
                {
                    map.PolylineList.Remove(line);
                    map.PolylineList.Add(line1);
                    map.PolylineList.Add(line2);
                }

                PolylineObject bottle_NeckLine = new PolylineObject(-100);//桥接线段
                bottle_NeckLine.SomeValue = 1;//1代表瓶颈边；2代表与其他要素的连接边
                bottle_NeckLine.PointList = new List<TriNode>();
                bottle_NeckLine.PointList.Add(curEdge.startPoint);
                bottle_NeckLine.PointList.Add(curEdge.endPoint);
                map.PolylineList.Add(bottle_NeckLine);
            }
            #endregion
        }

        public void AddAdjancentBuilding_ContersandProximityEdge()
        {

            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGraphfrmSkeletonForEnrichNetwork(this.map1, this.skeleton1);
            //pg.WriteProxiGraph2Shp(@"E:\Displacement\7-18\Roads\Completed\PrixmityGraph", @"ProximityGraphforEnrichNetwork", esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);

            #region 将邻近图的建筑物节点和边加入网络中
            int idofNode=map.TriNodeList.Count;
            List<TriNode> buildingNodeList = new List<TriNode>();

            foreach (ProxiNode node in pg.NodeList)
            {
                if (node.FeatureType != FeatureType.PolylineType)
                {
                    TriNode curNode = new TriNode(node.X, node.Y,
                        idofNode,
                        node.TagID,
                        node.FeatureType);
                    idofNode++;
                    buildingNodeList.Add(curNode);
                } 
            }
            map.TriNodeList.AddRange(buildingNodeList);
            foreach (ProxiEdge curEdge in pg.EdgeList)
            {
                if (curEdge.Node1.FeatureType == FeatureType.PolygonType
                    && curEdge.Node2.FeatureType == FeatureType.PolygonType)
                {
                    //来源于邻近图的桥接线段TagValue=-200
                    PolylineObject proxi_Line = new PolylineObject(-200);
                    proxi_Line.SomeValue = 3;//1代表瓶颈边；2代表与其他要素的连接边
                    proxi_Line.PointList = new List<TriNode>();
                    TriNode curBNode1 = GetNodebyIDandType(curEdge.Node1.TagID,
                        curEdge.Node1.FeatureType,
                        buildingNodeList);
                    TriNode curBNode2 = GetNodebyIDandType(curEdge.Node2.TagID,
                        curEdge.Node2.FeatureType,
                        buildingNodeList);
                    if (curBNode1 != null && curBNode2 != null)
                    {
                        proxi_Line.PointList.Add(curBNode1);
                        proxi_Line.PointList.Add(curBNode2);
                        map.PolylineList.Add(proxi_Line);
                    }
                }
                else if (curEdge.Node1.FeatureType == FeatureType.PolylineType
                    && curEdge.Node2.FeatureType == FeatureType.PolygonType)
                {

                    PolylineObject proxi_Line = new PolylineObject(-200);
                    proxi_Line.SomeValue = 2;//1代表瓶颈边；2代表与其他要素的连接边
                    proxi_Line.PointList = new List<TriNode>();
                    TriNode oNode=GetNodebyX_Y(curEdge.Node1,map.TriNodeList,0.00000f);
                    TriNode curBNode2 = GetNodebyIDandType(curEdge.Node2.TagID,
                        curEdge.Node2.FeatureType,
                        buildingNodeList);
                    if (oNode != null && curBNode2!=null)
                    {

                        PolylineObject line1 = null;
                        PolylineObject line2 = null;
                        PolylineObject line = this.CutPolyline22(map, out line1, out line2, oNode);
                        if (line != null)
                        {
                            map.PolylineList.Remove(line);
                            map.PolylineList.Add(line1);
                            map.PolylineList.Add(line2);
                        }

                        proxi_Line.PointList.Add(oNode);
                        proxi_Line.PointList.Add(curBNode2);
                        map.PolylineList.Add(proxi_Line);
                    }
                }
                else if (curEdge.Node2.FeatureType == FeatureType.PolylineType
    && curEdge.Node1.FeatureType == FeatureType.PolygonType)
                {

                    PolylineObject proxi_Line = new PolylineObject(-200);
                    proxi_Line.SomeValue = 2;//1代表瓶颈边；2代表与其他要素的连接边
                    proxi_Line.PointList = new List<TriNode>();
                    TriNode oNode = GetNodebyX_Y(curEdge.Node2, map.TriNodeList, 0.00000f);
                    TriNode curBNode1 = GetNodebyIDandType(curEdge.Node1.TagID,
                        curEdge.Node1.FeatureType,
                        buildingNodeList);
                    if (oNode != null&& curBNode1!= null)
                    {
                        PolylineObject line1 = null;
                        PolylineObject line2 = null;
                        PolylineObject line = this.CutPolyline22(map, out line1, out line2, oNode);
                        if (line != null)
                        {
                            map.PolylineList.Remove(line);
                            map.PolylineList.Add(line1);
                            map.PolylineList.Add(line2);
                        }

                        proxi_Line.PointList.Add(oNode);
                        proxi_Line.PointList.Add(curBNode1);
                        map.PolylineList.Add(proxi_Line);
                    }
                }
            }
            #endregion
        }
        /// <summary>
        /// 判断集合中是否已经包含该边
        /// </summary>
        /// <param name="Bottle_NeckEdgeList">边集合</param>
        /// <returns></returns>
        private bool IsContain(TriEdge triEdge,List<TriEdge>  Bottle_NeckEdgeList)
        {
            TriNode startP = triEdge.startPoint;
            TriNode endP = triEdge.endPoint;
            foreach (TriEdge curEdge in Bottle_NeckEdgeList)
            {
                if ((curEdge.endPoint == endP && curEdge.startPoint == startP) || (curEdge.endPoint == startP && curEdge.startPoint == endP))
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 从数组中找到指定TagId和Type的节点
        /// </summary>
        /// <param name="id">TagId</param>
        /// <param name="type">FeatureType</param>
        /// <param name="nodeList">TirNode数组</param>
        /// <returns>指定TagId和Type的节点</returns>
        private TriNode GetNodebyIDandType(int id, FeatureType type,List<TriNode> nodeList)
        {
            foreach (TriNode curNode in nodeList)
            {
                if (curNode.TagValue == id && curNode.FeatureType == type)
                    return curNode;
            }
            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private TriNode GetNodebyX_Y(Node node, List<TriNode> nodeList, double delta)
        {
            int n = nodeList.Count;
            double x = node.X;
            double y = node.Y;
            for (int i = 0; i < n; i++)
            {
                TriNode curV = nodeList[i];
                if (Math.Abs((1 - curV.X / x)) <= delta && Math.Abs((1 - curV.Y / y)) <= delta)
                {
                    return curV;
                }
            }
            return null;
        }
        /// <summary>
        /// 从Node处将线截成两半
        /// </summary>
        /// <param name="line1">结果1</param>
        /// <param name="line2">结果1</param>
        /// <param name="node">截点</param>
        /// <returns>原对象</returns>
        private PolylineObject CutPolyline22(SMap map,out PolylineObject line1, out PolylineObject line2, TriNode node)
        {
            line1 = null;
            line2 = null;
            foreach (ConNode connode in map.ConNodeList)
            {
                if (node == connode.Point)
                {
                    return null;
                }
            }
            int count=map.PolylineList.Count;

            foreach (PolylineObject line in map.PolylineList)
            {
                
                int n=line.PointList.Count;
                for (int i = 0; i < n;i++ )
                {
                    TriNode curNode = line.PointList[i];
                    if (node == curNode)
                    {
                        ConNode curCoonNode = new ConNode(curNode.ID, 0.2f, curNode);
                        map.ConNodeList.Add(curCoonNode);
                        List<TriNode> curPointList1=new List<TriNode>();
                        List<TriNode> curPointList2 = new List<TriNode>();
                        for(int j=0;j<=i;j++)
                        {
                          // line.PointList[j].TagValue = count-1;
                           curPointList1.Add(line.PointList[j]);
                        }
                        for (int k = i; k < n; k++)
                        {
                            line.PointList[k].TagValue = count;
                            curPointList2.Add(line.PointList[k]);
                        }

                        line1 = new PolylineObject(line.ID, curPointList1, line.SylWidth);
                        line1.TypeID = line.TypeID;
                        line1.SomeValue = line.SomeValue;

                        line2 = new PolylineObject(line.ID, curPointList2, line.SylWidth);
                        line2.TypeID = line.TypeID;
                        line2.SomeValue = line.SomeValue;
                        count++;
                        return line;
                    }
                }
            }
            return null;
        }
    }
}
