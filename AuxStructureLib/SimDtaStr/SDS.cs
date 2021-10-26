using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System.Windows.Forms;

namespace AuxStructureLib
{
    /// <summary>
    /// 简单数据结构
    /// </summary>
    public class SDS
    {
        public SMap Map = null;//地图数据
        public ConsDelaunayTin CDT = null;//约束三角网数据

        public List<SDS_Node> Nodes = null;       //所有顶点
        public List<SDS_Edge> Edges = null;       //所有边
        public List<SDS_Triangle> Triangles = null;        //所有三角形
        public List<SDS_PointObj> PointObjs = null;        //所有点对象
        public List<SDS_PolylineObj> PolylineObjs = null;  //所有线对象
        public List<SDS_PolygonO> PolygonOObjs = null;     //所有面对象
        public List<SDS_PolygonF> PolygonFObjs = null;     //所有空白对象
        /// <summary>
        /// 构造函数
        /// </summary>
        public SDS(SMap map, ConsDelaunayTin cdt)
        {
            Map = map;
            CDT = cdt;

            Nodes = new List<SDS_Node>();//所有顶点
            Edges = new List<SDS_Edge>();//所有边
            Triangles = new List<SDS_Triangle>();//所有三角形
            PointObjs = new List<SDS_PointObj>();//所有
            PolylineObjs = new List<SDS_PolylineObj>();
            PolygonOObjs = new List<SDS_PolygonO>();
            PolygonFObjs = new List<SDS_PolygonF>();
        }

        /// <summary>
        /// 创建SDS数据结构
        /// </summary>
        public void CreateSDS()
        {
            if (this.Map == null || this.CDT == null)
                return;

            if (this.CDT.TriNodeList != null && this.CDT.TriNodeList.Count > 0)
            {
                //顶点和点对象
                foreach (TriNode curNode in this.CDT.TriNodeList)
                {
                    SDS_Node curSDSNode = new SDS_Node(curNode.ID, curNode.TagValue, curNode.X, curNode.Y);
                    this.Nodes.Add(curSDSNode);
                }
            }

            if (this.CDT.TriEdgeList != null && this.CDT.TriEdgeList.Count > 0)
            {
                //三角形边对象
                int n = this.CDT.TriEdgeList.Count;
                bool[] isVisited = new bool[n];
                for (int i = 0; i < n; i++)
                {
                    TriEdge curEdge = this.CDT.TriEdgeList[i];

                    TriEdge curDEdge = curEdge.doulEdge;
                    if ((curDEdge != null && isVisited[curDEdge.ID] == true && isVisited[curEdge.ID] == true) || (curDEdge == null && isVisited[curEdge.ID] == true))//存在对偶边且已经被访问
                    {
                        continue;
                    }
                    else
                    {
                        if (curDEdge != null)
                        {
                            isVisited[curDEdge.ID] = true;
                            isVisited[curEdge.ID] = true;
                        }
                        else
                        {
                            isVisited[curEdge.ID] = true;
                        }
                    }

                    int id1 = curEdge.startPoint.ID;
                    int id2 = curEdge.endPoint.ID;
                    SDS_Node node1 = this.GetNode(id1);
                    SDS_Node node2 = this.GetNode(id2);

                    SDS_Edge curSDSEdge = new SDS_Edge(curEdge.ID, node1, node2);

                    node1.Edges.Add(curSDSEdge);
                    node1.EdgesD.Add(true);
                    node2.Edges.Add(curSDSEdge);
                    node2.EdgesD.Add(false);

                    this.Edges.Add(curSDSEdge);
                }
            }

            //点对象
            if (Map.PointList != null && Map.PointList.Count > 0)
            {
                foreach (PointObject curPointObject in Map.PointList)
                {

                    SDS_Node node = this.GetNode(curPointObject.Point);
                    SDS_PointObj curSDSPointObj = new SDS_PointObj(curPointObject.ID, node);
                    node.PointObj = curSDSPointObj;
                    this.PointObjs.Add(curSDSPointObj);
                }
            }
            //线 
            if (Map.PolylineList != null && Map.PolylineList.Count > 0)
            {
                foreach (PolylineObject curPolylineObject in Map.PolylineList)
                {
                    SDS_PolylineObj curSDSPolylineObj = new SDS_PolylineObj(curPolylineObject.ID, curPolylineObject.PointList);
                    int count = curPolylineObject.PointList.Count;
                    SDS_Node curNode = null;
                    SDS_Edge curE = null;
                    for (int i = 0; i < count - 1; i++)
                    {
                        curNode = GetNode(curPolylineObject.PointList[i].ID);
                        curSDSPolylineObj.Points.Add(curNode);
                        int id1 = curPolylineObject.PointList[i].ID;
                        int id2 = curPolylineObject.PointList[i + 1].ID;
                        bool wise;
                        curE = this.GetEdge(id1, id2, out wise);
                        curSDSPolylineObj.Edges.Add(curE);
                        curSDSPolylineObj.Wises.Add(wise);
                        curE.MapObject = curSDSPolylineObj;
                    }
                    curNode = new SDS_Node(curPolylineObject.PointList[count - 1].ID, curPolylineObject.PointList[count - 1].TagValue, curPolylineObject.PointList[count - 1].X, curPolylineObject.PointList[count - 1].Y);
                    curSDSPolylineObj.Points.Add(curNode);
                    this.PolylineObjs.Add(curSDSPolylineObj);
                }
            }
            //面
            if (Map.PolygonList != null && Map.PolygonList.Count > 0)
            {
                foreach (PolygonObject curPolygonObject in Map.PolygonList)
                {
                    SDS_PolygonO curSDS_PolygonO = new SDS_PolygonO(curPolygonObject.ID, curPolygonObject.PointList);

                    curSDS_PolygonO.TrialPosList = curPolygonObject.TriableList;

                    int count = curPolygonObject.PointList.Count;
                    int id1 = -1;
                    int id2 = -1;
                    bool wise;
                    SDS_Node curNode = null;
                    SDS_Edge curE = null;
                    for (int i = 0; i < count - 1; i++)
                    {
                        curNode = curNode = GetNode(curPolygonObject.PointList[i].ID);
                        curSDS_PolygonO.Points.Add(curNode);
                        id1 = curPolygonObject.PointList[i].ID;
                        id2 = curPolygonObject.PointList[i + 1].ID;
                        curE = this.GetEdge(id1, id2, out wise);
                        curSDS_PolygonO.Edges.Add(curE);
                        curSDS_PolygonO.Wises.Add(wise);
                        curE.MapObject = curSDS_PolygonO;
                    }

                    curNode = curNode = this.GetNode(curPolygonObject.PointList[count - 1].ID);
                    curSDS_PolygonO.Points.Add(curNode);

                    //最后一条线段
                    id1 = curPolygonObject.PointList[count - 1].ID;
                    id2 = curPolygonObject.PointList[0].ID;
                    curE = this.GetEdge(id1, id2, out wise);
                    curSDS_PolygonO.Edges.Add(curE);
                    curSDS_PolygonO.Wises.Add(wise);
                    curE.MapObject = curSDS_PolygonO;
                    this.PolygonOObjs.Add(curSDS_PolygonO);
                }
            }
            //三角形对象
            if (CDT.TriangleList != null && CDT.TriangleList.Count > 0)
            {
                //三角形对象
                foreach (Triangle curTri in this.CDT.TriangleList)
                {
                    SDS_Triangle curSDSTri = new SDS_Triangle();

                    curSDSTri.ID = curTri.ID;
                    curSDSTri.TriType = curTri.TriType;

                    SDS_Node p1 = this.GetNode(curTri.point1);
                    SDS_Node p2 = this.GetNode(curTri.point2);
                    SDS_Node p3 = this.GetNode(curTri.point3);
                    curSDSTri.Points[0] = p1;
                    curSDSTri.Points[1] = p2;
                    curSDSTri.Points[2] = p3;
                    p1.Triangles.Add(curSDSTri);
                    p2.Triangles.Add(curSDSTri);
                    p3.Triangles.Add(curSDSTri);

                    bool wise1;
                    bool wise2;
                    bool wise3;
                    SDS_Edge e1 = this.GetEdge(curTri.edge1, out wise1);
                    SDS_Edge e2 = this.GetEdge(curTri.edge2, out wise2);
                    SDS_Edge e3 = this.GetEdge(curTri.edge3, out wise3);
                    curSDSTri.Edges[0] = e1;
                    curSDSTri.Edges[1] = e2;
                    curSDSTri.Edges[2] = e3;
                    curSDSTri.Wises[0] = wise1;
                    curSDSTri.Wises[1] = wise2;
                    curSDSTri.Wises[2] = wise3;
                    if (wise1) { e1.LeftTriangle = curSDSTri; }
                    else { e1.RightTriangle = curSDSTri; }
                    if (wise2) { e2.LeftTriangle = curSDSTri; }
                    else { e2.RightTriangle = curSDSTri; }
                    if (wise3) { e3.LeftTriangle = curSDSTri; }
                    else { e3.RightTriangle = curSDSTri; }


                    if (curTri.point1.FeatureType == FeatureType.PolygonType
                        && curTri.point2.FeatureType == FeatureType.PolygonType
                        && curTri.point3.FeatureType == FeatureType.PolygonType)
                    {
                        if (curTri.point1.TagValue == curTri.point2.TagValue && curTri.point2.TagValue == curTri.point3.TagValue)
                        {
                            TriNode centerPoint = ComFunLib.CalCenter(curTri);
                            SDS_PolygonO polygon = this.GetPolygonO(curSDSTri.Points[0].TagID);
                            if (polygon != null)
                            {
                                if (ComFunLib.IsPointinPolygon(centerPoint, polygon.PointList))
                                {
                                    curSDSTri.Polygon = polygon;
                                    polygon.Triangles.Add(curSDSTri);
                                }
                            }
                        }
                    }


                    this.Triangles.Add(curSDSTri);
                }
            }
        }

        /// <summary>
        /// 创建SDS数据结构-
        /// -需要将加密后的VD图的位置信息赋予新的地图
        /// </summary>
        public void CreateSDS1(SMap map1)
        {
            if (this.Map == null || this.CDT == null)
                return;

            if (this.CDT.TriNodeList != null && this.CDT.TriNodeList.Count > 0)
            {
                //顶点和点对象
                foreach (TriNode curNode in this.CDT.TriNodeList)
                {
                    SDS_Node curSDSNode = new SDS_Node(curNode.ID, curNode.TagValue, curNode.X, curNode.Y);
                    this.Nodes.Add(curSDSNode);
                }
            }

            if (this.CDT.TriEdgeList != null && this.CDT.TriEdgeList.Count > 0)
            {
                //三角形边对象
                int n = this.CDT.TriEdgeList.Count;
                bool[] isVisited = new bool[n];
                for (int i = 0; i < n; i++)
                {
                    TriEdge curEdge = this.CDT.TriEdgeList[i];

                    TriEdge curDEdge = curEdge.doulEdge;
                    if ((curDEdge != null && isVisited[curDEdge.ID] == true && isVisited[curEdge.ID] == true) || (curDEdge == null && isVisited[curEdge.ID] == true))//存在对偶边且已经被访问
                    {
                        continue;
                    }
                    else
                    {
                        if (curDEdge != null)
                        {
                            isVisited[curDEdge.ID] = true;
                            isVisited[curEdge.ID] = true;
                        }
                        else
                        {
                            isVisited[curEdge.ID] = true;
                        }
                    }

                    int id1 = curEdge.startPoint.ID;
                    int id2 = curEdge.endPoint.ID;
                    SDS_Node node1 = this.GetNode(id1);
                    SDS_Node node2 = this.GetNode(id2);

                    SDS_Edge curSDSEdge = new SDS_Edge(curEdge.ID, node1, node2);

                    node1.Edges.Add(curSDSEdge);
                    node1.EdgesD.Add(true);
                    node2.Edges.Add(curSDSEdge);
                    node2.EdgesD.Add(false);

                    this.Edges.Add(curSDSEdge);
                }
            }

            //点对象
            if (Map.PointList != null && Map.PointList.Count > 0)
            {
                foreach (PointObject curPointObject in Map.PointList)
                {

                    SDS_Node node = this.GetNode(curPointObject.Point);
                    SDS_PointObj curSDSPointObj = new SDS_PointObj(curPointObject.ID, node);
                    node.PointObj = curSDSPointObj;
                    this.PointObjs.Add(curSDSPointObj);
                }
            }
            //线 
            if (Map.PolylineList != null && Map.PolylineList.Count > 0)
            {
                foreach (PolylineObject curPolylineObject in Map.PolylineList)
                {
                    SDS_PolylineObj curSDSPolylineObj = new SDS_PolylineObj(curPolylineObject.ID, curPolylineObject.PointList);
                    int count = curPolylineObject.PointList.Count;
                    SDS_Node curNode = null;
                    SDS_Edge curE = null;
                    for (int i = 0; i < count - 1; i++)
                    {
                        curNode = GetNode(curPolylineObject.PointList[i].ID);
                        curSDSPolylineObj.Points.Add(curNode);
                        int id1 = curPolylineObject.PointList[i].ID;
                        int id2 = curPolylineObject.PointList[i + 1].ID;
                        bool wise;
                        curE = this.GetEdge(id1, id2, out wise);
                        curSDSPolylineObj.Edges.Add(curE);
                        curSDSPolylineObj.Wises.Add(wise);
                        curE.MapObject = curSDSPolylineObj;
                    }
                    curNode = new SDS_Node(curPolylineObject.PointList[count - 1].ID, curPolylineObject.PointList[count - 1].TagValue, curPolylineObject.PointList[count - 1].X, curPolylineObject.PointList[count - 1].Y);
                    curSDSPolylineObj.Points.Add(curNode);
                    this.PolylineObjs.Add(curSDSPolylineObj);
                }
            }
            //面
            if (Map.PolygonList != null && Map.PolygonList.Count > 0)
            {
                int cP = map1.PolygonList.Count;
                for(int i=0;i<cP;i++) 
                {
                    PolygonObject curPolygonObject=Map.PolygonList[i];
                    PolygonObject curPolygonObject1=map1.PolygonList[i];
                    SDS_PolygonO curSDS_PolygonO = new SDS_PolygonO(curPolygonObject.ID, curPolygonObject.PointList);

                    curSDS_PolygonO.TrialPosList = curPolygonObject1.TriableList;

                    int count = curPolygonObject.PointList.Count;
                    int id1 = -1;
                    int id2 = -1;
                    bool wise;
                    SDS_Node curNode = null;
                    SDS_Edge curE = null;
                    for (int j = 0; j < count - 1; j++)
                    {
                        curNode = curNode = GetNode(curPolygonObject.PointList[j].ID);
                        curSDS_PolygonO.Points.Add(curNode);
                        id1 = curPolygonObject.PointList[j].ID;
                        id2 = curPolygonObject.PointList[j + 1].ID;
                        curE = this.GetEdge(id1, id2, out wise);
                        curSDS_PolygonO.Edges.Add(curE);
                        curSDS_PolygonO.Wises.Add(wise);
                        curE.MapObject = curSDS_PolygonO;
                    }

                    curNode = curNode = this.GetNode(curPolygonObject.PointList[count - 1].ID);
                    curSDS_PolygonO.Points.Add(curNode);

                    //最后一条线段
                    id1 = curPolygonObject.PointList[count - 1].ID;
                    id2 = curPolygonObject.PointList[0].ID;
                    curE = this.GetEdge(id1, id2, out wise);
                    curSDS_PolygonO.Edges.Add(curE);
                    curSDS_PolygonO.Wises.Add(wise);
                    curE.MapObject = curSDS_PolygonO;
                    this.PolygonOObjs.Add(curSDS_PolygonO);
                }
            }
            //三角形对象
            if (CDT.TriangleList != null && CDT.TriangleList.Count > 0)
            {
                //三角形对象
                foreach (Triangle curTri in this.CDT.TriangleList)
                {
                    SDS_Triangle curSDSTri = new SDS_Triangle();

                    curSDSTri.ID = curTri.ID;
                    curSDSTri.TriType = curTri.TriType;

                    SDS_Node p1 = this.GetNode(curTri.point1);
                    SDS_Node p2 = this.GetNode(curTri.point2);
                    SDS_Node p3 = this.GetNode(curTri.point3);
                    curSDSTri.Points[0] = p1;
                    curSDSTri.Points[1] = p2;
                    curSDSTri.Points[2] = p3;
                    p1.Triangles.Add(curSDSTri);
                    p2.Triangles.Add(curSDSTri);
                    p3.Triangles.Add(curSDSTri);

                    bool wise1;
                    bool wise2;
                    bool wise3;
                    SDS_Edge e1 = this.GetEdge(curTri.edge1, out wise1);
                    SDS_Edge e2 = this.GetEdge(curTri.edge2, out wise2);
                    SDS_Edge e3 = this.GetEdge(curTri.edge3, out wise3);
                    curSDSTri.Edges[0] = e1;
                    curSDSTri.Edges[1] = e2;
                    curSDSTri.Edges[2] = e3;
                    curSDSTri.Wises[0] = wise1;
                    curSDSTri.Wises[1] = wise2;
                    curSDSTri.Wises[2] = wise3;
                    if (wise1) { e1.LeftTriangle = curSDSTri; }
                    else { e1.RightTriangle = curSDSTri; }
                    if (wise2) { e2.LeftTriangle = curSDSTri; }
                    else { e2.RightTriangle = curSDSTri; }
                    if (wise3) { e3.LeftTriangle = curSDSTri; }
                    else { e3.RightTriangle = curSDSTri; }


                    if (curTri.point1.FeatureType == FeatureType.PolygonType
                        && curTri.point2.FeatureType == FeatureType.PolygonType
                        && curTri.point3.FeatureType == FeatureType.PolygonType)
                    {
                        if (curTri.point1.TagValue == curTri.point2.TagValue && curTri.point2.TagValue == curTri.point3.TagValue)
                        {
                            TriNode centerPoint = ComFunLib.CalCenter(curTri);
                            SDS_PolygonO polygon = this.GetPolygonO(curSDSTri.Points[0].TagID);
                            if (polygon != null)
                            {
                                if (ComFunLib.IsPointinPolygon(centerPoint, polygon.PointList))
                                {
                                    curSDSTri.Polygon = polygon;
                                    polygon.Triangles.Add(curSDSTri);
                                }
                            }
                        }
                    }


                    this.Triangles.Add(curSDSTri);
                }
            }
        }

        /// <summary>
        /// 判断三角形数组中是否包含给定的三角形
        /// </summary>
        /// <param name="TestedTriangles">三角形数组</param>
        /// <param name="T">三角形</param>
        /// <returns>BOOL</returns>
        public static bool IsContainTri(List<SDS_Triangle> Triangles, SDS_Triangle T)
        {
            foreach (SDS_Triangle curT in Triangles)
            {
                if (curT == T)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 获取Node
        /// </summary>
        /// <param name="id">ID号</param>
        /// <returns>Node</returns>
        private SDS_Node GetNode(int id)
        {
            foreach (SDS_Node curSDSNode in this.Nodes)
            {
                if (curSDSNode.ID == id)
                {
                    return curSDSNode;
                }
            }
            return null;
        }


        /// <summary>
        /// 获取Node
        /// </summary>
        /// <param name="Node">TriNode对象</param>
        /// <returns>SDS_Node对象</returns>
        private SDS_Node GetNode(TriNode Node)
        {
            int id = Node.ID;
            return GetNode(id);
        }

        /// <summary>
        /// 获取边对象，并返回方向
        /// </summary>
        /// <param name="id1">第一个点</param>
        /// <param name="id2">第二个点</param>
        /// <param name="wise">方向</param>
        /// <returns>边</returns>
        private SDS_Edge GetEdge(int id1, int id2, out bool wise)
        {
            wise = true;
            foreach (SDS_Edge curSDSEdge in this.Edges)
            {
                if (curSDSEdge.StartPoint.ID == id1 && curSDSEdge.EndPoint.ID == id2)
                {
                    wise = true;
                    return curSDSEdge;
                }
                else if (curSDSEdge.StartPoint.ID == id2 && curSDSEdge.EndPoint.ID == id1)
                {
                    wise = false;
                    return curSDSEdge;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取边对象，并返回方向
        /// </summary>
        /// <param name="id1">第一个点</param>
        /// <param name="id2">第二个点</param>
        /// <param name="wise">方向</param>
        /// <returns>边</returns>
        private SDS_Edge GetEdge(TriEdge Edge, out bool wise)
        {
            wise = true;
            int id1 = Edge.startPoint.ID;
            int id2 = Edge.endPoint.ID;
            return GetEdge(id1, id2, out wise);
        }
        /// <summary>
        /// 根据ID号获取多边形
        /// </summary>
        /// <param name="id">ID号</param>
        /// <returns>多边形对象</returns>
        public SDS_PolygonO GetPolygonO(int id)
        {
            foreach (SDS_PolygonO curPolygon in this.PolygonOObjs)
            {
                if (curPolygon.ID == id)
                {
                    return curPolygon;
                }
            }
            return null;
        }
        /// <summary>
        /// 写入shp
        /// </summary>
        /// <param name="filePath">路径</param>
        /// <param name="fileName">文件名</param>
        /// <param name="prj"></param>
        public void Create_WriteShp(string filePath, esriSRProjCS4Type prj)
        {
            SDS_Node.Create_WriteVetex2Shp(filePath, @"SDSNodes", this.Nodes, esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
            SDS_Edge.Create_WriteEdge2Shp(filePath, @"SDSEdges", this.Edges, esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
            SDS_Triangle.Create_WriteTriange2Shp(filePath, @"SDSTriangles", this.Triangles, esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
            SDS_PointObj.Create_WritePoint2Shp(filePath, @"SDSPointObjs", this.PointObjs, esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
            SDS_PolylineObj.Create_WritePolylineObject2Shp(filePath, @"SDSPolylineObjs", this.PolylineObjs, esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
            SDS_PolygonO.Create_WritePolygonObject2Shp(filePath, @"SDSPolygonOs", this.PolygonOObjs, esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E);
        }
    }
}
