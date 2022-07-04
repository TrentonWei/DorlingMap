using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using System.IO;

namespace AuxStructureLib
{
    /// <summary>
    /// 构建Delaunay算法类型
    /// </summary>
    public enum AlgDelaunayType
    {
        Side_extent, 
        Point_insert, 
        Divide_conquer,
        ESRI_AE
    }
    /// <summary>
    /// Delaunay三角网
    /// </summary>
    public class DelaunayTin
    {
        public List<Triangle> TriangleList = null;  //三角形列表
        public List<TriEdge> TriEdgeList = null;    //边列表
        public List<TriNode> TriNodeList = null;    //节点列表

        public List<TriEdge> RNGEdgeList = new List<TriEdge>();
        public List<TriNode> RNGNodeList = new List<TriNode>();

        private SMap map = null;
        /// <summary>
        /// 三角形个数
        /// </summary>
        public int TriangleCount
        {
            get
            {
                return TriangleList.Count;
            }
        }

        /// <summary>
        /// 边数
        /// </summary>
        public int TriNodeCount
        {
            get
            {
                return TriNodeList.Count;
            }
        }

        /// <summary>
        /// 节点数
        /// </summary>
        public int TriEdgeCount
        {
            get
            {
                return TriEdgeList.Count;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="triNodeList"></param>
        public DelaunayTin(List<TriNode> triNodeList)
        {
            TriNodeList = triNodeList;
            TriangleList = new List<Triangle>();
            TriEdgeList = new List<TriEdge>();
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="triNodeList"></param>
        public DelaunayTin(SMap map)
        {
            this.map = map;
            this.TriNodeList = map.TriNodeList;
            TriangleList = new List<Triangle>();
            TriEdgeList = new List<TriEdge>();
        }

        //List<TriPoint> triPoint = new List<TriPoint>();
       // List<TriEdge> triEdgeTemp = new List<TriEdge>();
       // List<Triangle> listTriangle = new List<Triangle>();
       // List<PointF> circumPoints = new List<PointF>();
        /// <summary>
        /// 创建Delaunay三角网
        /// </summary>
        /// <param name="algType">算法类型</param>
        public void CreateDelaunayTin(AlgDelaunayType algType)
        {
            switch (algType)
            {
                case AlgDelaunayType.Side_extent:
                    CreateDelaunayTin_Side_extent();
                    break;
                case AlgDelaunayType.Point_insert:
                    break;
                case  AlgDelaunayType.Divide_conquer:
                    break;
                case AlgDelaunayType.ESRI_AE:
                    this.CreateDelaunayAE();
                    break;
            }
        }

        public void DeleteTriangle(Triangle tri)
        {
            TriEdge e1 = tri.edge1;
            TriEdge e2 = tri.edge2;
            TriEdge e3 = tri.edge3;

            if (e1 != null)
            {
                TriEdge de = e1.doulEdge;
                if (de != null)
                {
                    de.doulEdge = null;
                    de.rightTriangle = null;
                }
                TriEdgeList.Remove(e1);
            }
            if (e2 != null)
            {
                TriEdge de = e2.doulEdge;
                if (de != null)
                {
                    de.doulEdge = null;
                    de.rightTriangle = null;
                }
                TriEdgeList.Remove(e2);
            }
            if (e3 != null)
            {
                TriEdge de = e3.doulEdge;
                if (de != null)
                {
                    de.doulEdge = null;
                    de.rightTriangle = null;
                }
                TriEdgeList.Remove(e3);
            }
            TriangleList.Remove(tri);
        }


        #region 调用AE生成三角网
        /// <summary>
        /// 调用AE生成三角网
        /// </summary>
        private void CreateDelaunayAE()
        {
            // Instantiate a new empty TIN.
            ITinEdit TinEdit = new TinClass();

            // Initialize the TIN with an envelope. The envelope's extent should be set large enough to // encompass all the data that will be added to the TIN. The envelope's spatial reference, if// if has one, will be used as the TIN's spatial reference. If it is not set, as in this case,// the TIN's spatial reference will be unknown.
            IEnvelope Env = new EnvelopeClass();
            //获取点集的范围
            double minx = double.PositiveInfinity;
            double miny = double.PositiveInfinity;
            double maxx = double.NegativeInfinity;
            double maxy = double.NegativeInfinity;
            foreach (TriNode curPoint in map.TriNodeList)
            {
                if (curPoint.X < minx)
                    minx = curPoint.X;
                if (curPoint.Y < miny)
                    miny = curPoint.Y;
                if (curPoint.X > maxx)
                    maxx = curPoint.X;
                if (curPoint.Y > maxy)
                    maxy = curPoint.Y;

            }
            Env.PutCoords(minx, miny, maxx, maxy);

            TinEdit.InitNew(Env);
            IFeatureClass pointfcls, polylinefcls, polygonfcls;
            map.Export2FeatureClasses(out pointfcls, out polylinefcls, out polygonfcls);
            object o = Type.Missing;
            if (pointfcls != null)
            {
                TinEdit.AddFromFeatureClass(pointfcls, null, null, null, esriTinSurfaceType.esriTinMassPoint, ref o);
            }
            if (polylinefcls != null)
            {
                TinEdit.AddFromFeatureClass(polylinefcls, null, null, null, esriTinSurfaceType.esriTinSoftLine, ref o);
            }
            if (polygonfcls != null)
            {
                TinEdit.AddFromFeatureClass(polygonfcls, null, null, null, esriTinSurfaceType.esriTinSoftLine, ref o);
            }

            //#region AddfromFeatureClass
            //object o = Type.Missing;

            //foreach (IFeatureLayer curLyr in lyrList)
            //{
            //    IFeatureCursor cursor = null;
            //    switch (curLyr.FeatureClass.ShapeType)
            //    {

            //        case esriGeometryType.esriGeometryPoint:
            //            {
            //                #region 点要素

            //                IFields pfields = curLyr.FeatureClass.Fields;
            //                IField pHeightField = null;
            //                for (int i = 0; i < pfields.FieldCount; i++)
            //                {
            //                    IField curField = pfields.get_Field(i);
            //                    if (curField.Name == "OBJECTID")
            //                    {
            //                        pHeightField = curField;
            //                        break;
            //                    }
            //                }

            //                TinEdit.AddFromFeatureClass(curLyr.FeatureClass, null, pHeightField, pHeightField, esriTinSurfaceType.esriTinMassPoint, ref o);

            //                #endregion
            //                break;
            //            }
            //        case esriGeometryType.esriGeometryPolyline:
            //            {

            //                #region 线要素
            //                cursor = curLyr.Search(null, false);

            //                IFields pfields = curLyr.FeatureClass.Fields;
            //                IField pHeightField = null;
            //                for (int i = 0; i < pfields.FieldCount; i++)
            //                {
            //                    IField curField = pfields.get_Field(i);
            //                    if (curField.Name == "OBJECTID")
            //                    {
            //                        pHeightField = curField;
            //                        break;
            //                    }
            //                }
            //                TinEdit.AddFromFeatureClass(curLyr.FeatureClass, null, pHeightField, pHeightField, esriTinSurfaceType.esriTinSoftLine, ref o);

            //                #endregion
            //                break;
            //            }

            //        case esriGeometryType.esriGeometryPolygon:
            //            {

            //                #region 面要素
            //                IFields pfields = curLyr.FeatureClass.Fields;
            //                IField pHeightField = null;
            //                for (int i = 0; i < pfields.FieldCount; i++)
            //                {
            //                    IField curField = pfields.get_Field(i);
            //                    if (curField.Name == "OBJECTID")
            //                    {
            //                        pHeightField = curField;
            //                        break;
            //                    }
            //                }
            //                TinEdit.AddFromFeatureClass(curLyr.FeatureClass, null, pHeightField, pHeightField, esriTinSurfaceType.esriTinSoftLine, ref o);

            //                #endregion
            //                break;
            //            }
            //    }
            //}
            //#endregion

            o = true;
          //  TinEdit.StopEditing(true);
            object overwrite = true;
            TinEdit.SaveAs(@"E:\TinTest", ref overwrite); //写入文件

            ITinAdvanced itina = (TinEdit as ITinAdvanced);

            int NodeCount = itina.NodeCount;
            for (int i = 1; i <= NodeCount; i++)
            {
                ITinNode curNode = itina.GetNode(i);
                int tag = curNode.TagValue;
            }

            int EdgeCount = itina.EdgeCount;
            for (int i = 1; i <= EdgeCount; i++)
            {
                ITinEdge curEdge = itina.GetEdge(i);
                int tag = curEdge.TagValue;
            }

            int TriCount = itina.TriangleCount;
            for (int i = 1; i <= TriCount; i++)
            {
                ITinTriangle curTriangle = itina.GetTriangle(i);

                ITinNode p1 = curTriangle.get_Node(1);
                ITinNode p2 = curTriangle.get_Node(2);
                ITinNode p3 = curTriangle.get_Node(3);

                TriNode point1 = TriNode.GetNode(map.TriNodeList, p1);
                TriNode point2 = TriNode.GetNode(map.TriNodeList, p2);
                TriNode point3 = TriNode.GetNode(map.TriNodeList, p3);

                if (point1 != null && point2 != null && point3 != null)
                {
                    Triangle tri = new Triangle();
                    tri.point1 = point1;
                    tri.point2 = point2;
                    tri.point3 = point3;

                    //第一条边
                    TriEdge e1 = new TriEdge(point1, point2);
                    if (point1.TagValue == point2.TagValue && point1.FeatureType == point2.FeatureType&&point1.TagValue!=-1)
                    {
                        e1.tagID = point1.TagValue;
                        e1.FeatureType = point1.FeatureType;
                    }
                    else
                    {
                        FeatureType ftype = FeatureType.Unknown;
                        int tagvalue=map.GetConsEdge(point1, point2,out ftype);
                        if (tagvalue != -1)
                        {
                            e1.tagID = tagvalue;
                            e1.FeatureType = ftype;
                        }
                    }
                    TriEdgeList.Add(e1);

                    //第二条边
                    TriEdge e2 = new TriEdge(point2, point3);
                    if (point2.TagValue == point3.TagValue && point2.FeatureType == point3.FeatureType && point2.TagValue != -1)
                    {
                        e2.tagID = point2.TagValue;
                        e2.FeatureType = point2.FeatureType;
                    }
                    else
                    {
                        FeatureType ftype = FeatureType.Unknown;
                        int tagvalue = map.GetConsEdge(point2, point3, out ftype);
                        if (tagvalue != -1)
                        {
                            e2.tagID = tagvalue;
                            e2.FeatureType = ftype;
                        }
                    }
                    TriEdgeList.Add(e2);

                    //第三条边
                    TriEdge e3 = new TriEdge(point3, point1);
                    if (point3.TagValue == point1.TagValue && point3.FeatureType == point1.FeatureType && point3.TagValue != -1)
                    {
                        e3.tagID = point3.TagValue;
                        e3.FeatureType = point3.FeatureType;
                    }
                    else
                    {
                        FeatureType ftype = FeatureType.Unknown;
                        int tagvalue = map.GetConsEdge(point3, point1, out ftype);
                        if (tagvalue != -1)
                        {
                            e3.tagID = tagvalue;
                            e3.FeatureType = ftype;
                        }
                    }
                    TriEdgeList.Add(e3);

                    tri.edge1 = e1;
                    tri.edge2 = e2;
                    tri.edge3 = e3;
                    e1.leftTriangle = tri;
                    e2.leftTriangle = tri;
                    e3.leftTriangle = tri;
                    this.TriangleList.Add(tri);
                }
                //设置对偶边和右边三角形
                foreach (TriEdge edge in TriEdgeList)
                {
                    if (edge.doulEdge != null)
                        continue;
                    TriEdge doulEdge = TriEdge.FindOppsiteEdge(TriEdgeList, edge);
                    if (doulEdge == null)
                    {
                        edge.doulEdge = null;
                        edge.rightTriangle = null;
                    }
                    else
                    {
                        edge.doulEdge = doulEdge;
                        edge.rightTriangle = doulEdge.leftTriangle;
                    }
                }
            }

        }

        #endregion

        #region 扩边法
        /// <summary>
        /// 扩边法创建三角网
        /// </summary>
        private void CreateDelaunayTin_Side_extent()
        {
            if (this.TriNodeCount < 3)
                return;
            FirstTriangle();//创建第一个三角形
            BuildDelaunay();//扩边创建三角网
            TriEdgeList.Clear();
            TriEdgeList = Triangle.GetEdges(TriangleList);
            TriEdge.AmendEdgeLeftTriangle(TriEdgeList);
        }

        /// <summary>
        /// 构建第一个三角形
        /// </summary>
        private void FirstTriangle()
        {
            int index =-1;
            double length = double.MaxValue;
            //找到与第一个点最近的点，构成一条边
            foreach (TriNode p1 in TriNodeList)
            {
                double temp = TriEdge.LengthSquare(TriNodeList[0], p1);
                if (temp != 0 && temp < length)
                {
                    index = p1.ID;
                    length = temp;
                }
            }
            TriNode point1, point2, point3;
            point1 = TriNodeList[0];
            point3 = TriNodeList[index];
            if (point1 != null && point3 != null)
            {
                TriEdge edge = new TriEdge(point1, point3);
                point2 = TriEdge.GetBestPoint(edge, TriNodeList);

                //如果右边有点
                if (point2 != null)
                {
                    TriEdge triEdge1 = new TriEdge(point1, point2);
                    TriEdge triEdge2 = new TriEdge(point2, point3);
                    TriEdge triEdge3 = new TriEdge(point3, point1);
                    Triangle triangle = new Triangle(point1, point2, point3);
                    //将三条边的引用赋值给该三角形
                    triangle.edge1 = triEdge1;
                    triangle.edge2 = triEdge2;
                    triangle.edge3 = triEdge3;
                    //将该三角形作为每条边的左三角形（边的方向保持为逆时针方向）
                    triEdge1.leftTriangle = triangle;
                    triEdge2.leftTriangle = triangle;
                    triEdge3.leftTriangle = triangle;
                    //此时加入的边可能有重复
                    TriEdgeList.Add(triEdge1);
                    TriEdgeList.Add(triEdge2);
                    TriEdgeList.Add(triEdge3);
                    TriangleList.Add(triangle);
                }
                else
                {
                    //如果右边没有点
                    edge = new TriEdge(point3, point1);
                    point2 = TriEdge.GetBestPoint(edge, TriNodeList);
                    TriEdge triEdge1 = new TriEdge(point3, point2);
                    TriEdge triEdge2 = new TriEdge(point2, point1);
                    TriEdge triEdge3 = new TriEdge(point1, point3);
                    Triangle triangle = new Triangle(point3, point2, point1);
                    triangle.edge1 = triEdge1;
                    triangle.edge2 = triEdge2;
                    triangle.edge3 = triEdge3;
                    triEdge1.leftTriangle = triangle;
                    triEdge2.leftTriangle = triangle;
                    triEdge3.leftTriangle = triangle;
                    //此时加入的边可能有重复
                    TriEdgeList.Add(triEdge1);
                    TriEdgeList.Add(triEdge2);
                    TriEdgeList.Add(triEdge3);
                    TriangleList.Add(triangle);
                }
            }   
        }
       
        private void BuildDelaunay()
        {   
            while (TriEdgeList.Count != 0)
            {
                TriEdge edge = TriEdgeList[0];
                TriNode point2 = new TriNode();
                point2 = TriEdge.GetBestPoint(edge, TriNodeList);
                if (point2 != null)
                {
                    Triangle triangle = new Triangle(edge.startPoint, point2, edge.endPoint);
                    TriEdge edge1 = new TriEdge(edge.startPoint, point2);
                    TriEdge edge2 = new TriEdge(point2, edge.endPoint);
                    TriEdge edge3 = new TriEdge(edge.endPoint, edge.startPoint);
                    edge1.leftTriangle = triangle;
                    edge2.leftTriangle = triangle;
                    edge3.leftTriangle = triangle;
                    triangle.edge1 = edge1;
                    triangle.edge2 = edge2;
                    triangle.edge3 = edge3;
                    edge3.rightTriangle = edge.leftTriangle;
                    edge.rightTriangle = edge3.leftTriangle;
                    TriEdgeList.Remove(edge);
                    TriangleList.Add(triangle);
                    TriEdge edgeTemp = new TriEdge();
                    edgeTemp.startPoint = edge1.endPoint;
                    edgeTemp.endPoint = edge1.startPoint;
                    TriEdge sameEdge = TriEdge.FindSameEdge(TriEdgeList, edgeTemp);
                    if (sameEdge == null)
                    {
                        TriEdgeList.Add(edge1);
                    }
                    else
                    {
                        TriEdgeList.Remove(sameEdge);
                    }
                    edgeTemp.startPoint = edge2.endPoint;
                    edgeTemp.endPoint = edge2.startPoint;
                    sameEdge = TriEdge.FindSameEdge(TriEdgeList, edgeTemp);
                    if (sameEdge == null)
                    {
                        TriEdgeList.Add(edge2);
                    }
                    else
                    {
                        TriEdgeList.Remove(sameEdge);
                    }
                }
                else
                {
                    TriEdgeList.Remove(edge);
                }
            }
        }


        #endregion


        /// 创建点集的RNG图（最短距离）
        ///RNG计算(找到邻近图中每一个三角形，删除三角形中的最长边)
        ///说明：对于任意一条边，找到对应的三角形；如果是最长边，则删除（如果是一条边，保留；如果是两条边，若是最长边，删除）
        /// </summary>
        public void CreateRNG()
        {
            #region 找到潜在RNG对应的每条边，判断是否是邻近图中三角形对应的最长边，如果是，删除
            for (int i = 0; i < this.TriEdgeList.Count; i++)
            {
                TriEdge TinLine = this.TriEdgeList[i];

                Triangle LeftTri = TinLine.leftTriangle;
                Triangle RightTri = TinLine.rightTriangle;
                double MaxRight = 1000000000;
                double MaxLeft = 1000000000;

                if (LeftTri != null)
                {
                    List<double> LeftList = new List<double>();
                    LeftList.Add(LeftTri.edge1.Length); LeftList.Add(LeftTri.edge2.Length); LeftList.Add(LeftTri.edge3.Length);
                    MaxLeft = LeftList.Max();
                }

                if (RightTri != null)
                {
                    List<double> RightList = new List<double>();
                    RightList.Add(RightTri.edge1.Length); RightList.Add(RightTri.edge2.Length); RightList.Add(RightTri.edge3.Length);
                    MaxRight = RightList.Max();
                }

                if (TinLine.Length < MaxLeft && TinLine.Length < MaxRight)
                {
                    this.RNGEdgeList.Add(TinLine);
                }
            }
            #endregion

            this.RNGNodeList = this.TriNodeList;
        }

        /// <summary>
        /// 写ID
        /// </summary>
        public void WriteID()
        {
            Triangle.WriteID(this.TriangleList);
            TriEdge.WriteID(this.TriEdgeList);
        }
        /// <summary>
        /// 将结果写入Shape文件
        /// </summary>
        public void WriteShp(ISpatialReference pri)
        {
            TriNode.Create_WriteVetex2Shp(@"E:\DelaunayShape", @"Vextex", this.TriNodeList, pri);
            TriEdge.Create_WriteEdge2Shp(@"E:\DelaunayShape", @"Edge", this.TriEdgeList, pri);
            Triangle.Create_WriteTriange2Shp(@"E:\DelaunayShape", @"Triangle", this.TriangleList, pri);
        }

        /// <summary>
        /// 将结果写入Shape文件
        /// </summary>
        public void WriteShp(string filepath, ISpatialReference pri)
        {
            if (!Directory.Exists(filepath))
                Directory.CreateDirectory(filepath);
            TriNode.Create_WriteVetex2Shp(filepath, @"Vextex", this.TriNodeList, pri);
            TriEdge.Create_WriteEdge2Shp(filepath, @"Edge", this.TriEdgeList, pri);
            Triangle.Create_WriteTriange2Shp(filepath, @"Triangle", this.TriangleList, pri);
        }
    }
}
