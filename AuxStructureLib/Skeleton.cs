using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;

namespace AuxStructureLib
{
    /// <summary>
    /// 骨架线
    /// </summary>
    public class Skeleton
    {
        //弧段列表
        public List<Skeleton_Arc> Skeleton_ArcList = null;

        //所有伪Voronoi图
        //public List<Pseu_VoronoiPolygon> Pseu_VoronoiPolygonList = null;
        //约束三角网

        public ConsDelaunayTin CDT = null;
        public List<ConNode> ConNodeList = null;
        public SMap Map = null;
       // private ConsDelaunayTin cdt;
        private List<ConNode> CNList;


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="cdt"></param>
        public Skeleton(ConsDelaunayTin cdt, SMap map)
        {
            this.CDT = cdt;
            Map = map;
        }

        public Skeleton(ConsDelaunayTin cdt, List<ConNode> CNList)
        {
            // TODO: Complete member initialization
            this.CDT = cdt;
            this.CNList = CNList;
        }

        /// <summary>
        /// 删除多边形内部三角形
        /// </summary>
        /// <param name="isDAobu">是否删除凹部</param>
        private void PreProcessCDTforPLP(bool isDAobu)
        {
            List<Triangle> delTriList = new List<Triangle>();
            for (int i = 0; i < CDT.TriangleList.Count; i++)
            {
                Triangle curTri = null;
                curTri = CDT.TriangleList[i];
                int tagID = curTri.point1.TagValue;
                if (tagID == curTri.point2.TagValue && tagID == curTri.point3.TagValue)
                {
                    if (curTri.point1.FeatureType != FeatureType.PolygonType
                        || curTri.point2.FeatureType != FeatureType.PolygonType 
                        || curTri.point3.FeatureType != FeatureType.PolygonType)
                    {
                        continue;
                    }

                    if (isDAobu == true)//如果删除凹部就直接删除
                    {
                        delTriList.Add(curTri);
                    }
                    else//否则判断是否位于多边形内部
                    {
                        PolygonObject curPolygon = null;
                        foreach (PolygonObject polygon in Map.PolygonList)
                        {
                            if (polygon.ID == tagID)
                            {
                                curPolygon = polygon;
                                break;
                            }
                        }
                        if (curPolygon == null || curPolygon.PointList.Count < 3)
                            continue;
                        else
                        {
                            TriNode p = ComFunLib.CalCenter(curTri);

                            if (ComFunLib.IsPointinPolygon(p, curPolygon.PointList))
                            {
                                delTriList.Add(curTri);
                            }
                        }

                    }
                }
            }

            #region 删除三角形及其相关的边
            foreach (Triangle delTri in delTriList)
            {

                TriEdge e1 = delTri.edge1;
                TriEdge e2 = delTri.edge2;
                TriEdge e3 = delTri.edge3;

                if (e1 != null)
                {
                    TriEdge de = e1.doulEdge;
                    if (de != null)
                    {
                        de.doulEdge = null;
                        de.rightTriangle = null;
                    }
                    CDT.TriEdgeList.Remove(e1);
                }
                if (e2 != null)
                {
                    TriEdge de = e2.doulEdge;
                    if (de != null)
                    {
                        de.doulEdge = null;
                        de.rightTriangle = null;
                    }
                    CDT.TriEdgeList.Remove(e2);
                }
                if (e3 != null)
                {
                    TriEdge de = e3.doulEdge;
                    if (de != null)
                    {
                        de.doulEdge = null;
                        de.rightTriangle = null;
                    }
                    CDT.TriEdgeList.Remove(e3);
                }
                CDT.TriangleList.Remove(delTri);
            }
            #endregion

            //重写ID
            Triangle.WriteID(CDT.TriangleList);
        }

        /// <summary>
        /// 删除同一路段内部三角形
        /// </summary>
        /// <param name="isDAobu"></param>
        private void PreProcessDeleRoadSeg()
        {
            List<int> indexList = new List<int>();
            for (int i = 0; i < CDT.TriangleList.Count; i++)
            {
                Triangle curTri = null;
                curTri = CDT.TriangleList[i];
                int tagID = curTri.point1.TagValue;
                if (tagID == curTri.point2.TagValue && tagID == curTri.point3.TagValue)
                {
                    if (curTri.point1.FeatureType != FeatureType.PolylineType
                        || curTri.point2.FeatureType != FeatureType.PolylineType
                        || curTri.point3.FeatureType != FeatureType.PolylineType)
                    {
                        continue;
                    }
                    indexList.Add(curTri.ID);//记录ID号
                }
            }
            #region 删除三角形
            foreach (int index in indexList)
            {
                foreach (Triangle curTri in CDT.TriangleList)
                {
                    if (curTri.ID == index)
                    {
                        TriEdge e1 = curTri.edge1;
                        TriEdge e2 = curTri.edge2;
                        TriEdge e3 = curTri.edge3;

                        if (e1 != null)
                        {
                            TriEdge de = e1.doulEdge;
                            if (de != null)
                            {
                                de.doulEdge = null;
                                de.rightTriangle = null;
                            }
                            CDT.TriEdgeList.Remove(e1);
                        }
                        if (e2 != null)
                        {
                            TriEdge de = e2.doulEdge;
                            if (de != null)
                            {
                                de.doulEdge = null;
                                de.rightTriangle = null;
                            }
                            CDT.TriEdgeList.Remove(e2);
                        }
                        if (e3 != null)
                        {
                            TriEdge de = e3.doulEdge;
                            if (de != null)
                            {
                                de.doulEdge = null;
                                de.rightTriangle = null;
                            }
                            CDT.TriEdgeList.Remove(e3);
                        }
                        CDT.TriangleList.Remove(curTri);
                        break;
                    }
                }
            }
            #endregion
            //重写ID
            Triangle.WriteID(CDT.TriangleList);
        }

        /// <summary>
        /// 预处理，去除端点处的右边通道
        /// </summary>
        private void PreProcessCDTforRNT()
        {
            if (Map.ConNodeList == null || Map.ConNodeList.Count == 0)
                return;
            //先处理1类三角形中，两个端点的虚边
            foreach (Triangle curTri in CDT.TriangleList)
            {

                if (curTri.TriType == 1)
                {

                    TriEdge vEdge1 = null;
                    TriEdge vEdge2 = null;
                    TriNode vV = null;
                    TriEdge re = curTri.GetVEdgeofT1(out vEdge1, out vEdge2, out vV);
                    //端点在实边上
                    ConNode cNode1 = ConNode.GetPLbyID(Map.ConNodeList, re.startPoint.ID);
                    ConNode cNode2 = ConNode.GetPLbyID(Map.ConNodeList, re.endPoint.ID);
                    ConNode cNode3 = ConNode.GetPLbyID(Map.ConNodeList, vV.ID);
                    //无端点
                    if (cNode1 == null && cNode2 == null && cNode3 == null)
                    {
                        continue;
                    }
                    //三个全是端点
                    else if (cNode1 != null && cNode2 != null && cNode3 != null)
                    {
                        if (vEdge1.rightTriangle != null)
                        {
                            vEdge1.rightTriangle = null;
                            TriEdge dEdge = vEdge1.doulEdge;
                            vEdge1.doulEdge = null;
                            dEdge.rightTriangle = null;
                            dEdge.doulEdge = null;
                        }
                        if (vEdge2.rightTriangle != null)
                        {

                            vEdge2.rightTriangle = null;
                            TriEdge dEdge1 = vEdge2.doulEdge;
                            vEdge2.doulEdge = null;
                            dEdge1.rightTriangle = null;
                            dEdge1.doulEdge = null;
                        }
                    }
                    //有一条虚边的端点均为端点
                    else if (cNode1 != null && cNode2 == null && cNode3 != null)
                    {
                        if (vEdge1.startPoint.ID == cNode1.ID || vEdge1.endPoint.ID == cNode1.ID)
                        {
                            if (vEdge1.rightTriangle != null)
                            {
                                vEdge1.rightTriangle = null;
                                TriEdge dEdge = vEdge1.doulEdge;
                                vEdge1.doulEdge = null;
                                dEdge.rightTriangle = null;
                                dEdge.doulEdge = null;
                            }
                        }
                        else if (vEdge2.startPoint.ID == cNode1.ID || vEdge2.endPoint.ID == cNode1.ID)
                        {
                            if (vEdge2.rightTriangle != null)
                            {
                                vEdge2.rightTriangle = null;
                                TriEdge dEdge1 = vEdge2.doulEdge;
                                vEdge2.doulEdge = null;
                                dEdge1.rightTriangle = null;
                                dEdge1.doulEdge = null;
                            }
                        }
                    }
                    //有一条虚边的端点均为端点
                    else if (cNode1 == null && cNode2 != null && cNode3 != null)
                    {
                        if (vEdge1.startPoint.ID == cNode2.ID || vEdge1.endPoint.ID == cNode2.ID)
                        {
                            if (vEdge1.rightTriangle != null)
                            {
                                vEdge1.rightTriangle = null;
                                TriEdge dEdge = vEdge1.doulEdge;
                                vEdge1.doulEdge = null;
                                dEdge.rightTriangle = null;
                                dEdge.doulEdge = null;
                            }
                        }
                        else if (vEdge2.startPoint.ID == cNode2.ID || vEdge2.endPoint.ID == cNode2.ID)
                        {
                            if (vEdge2.rightTriangle != null)
                            {
                                vEdge2.rightTriangle = null;
                                TriEdge dEdge1 = vEdge2.doulEdge;
                                vEdge2.doulEdge = null;
                                dEdge1.rightTriangle = null;
                                dEdge1.doulEdge = null;
                            }
                        }
                    }

                                        //一条虚边上仅有一个端点，且在实边上
                    else if ((cNode1 != null || cNode2 != null) && cNode3 == null)
                    {
                        if (cNode1 != null && cNode2 == null)
                        {
                            if (vEdge1.startPoint.ID == cNode1.ID || vEdge1.endPoint.ID == cNode1.ID)
                            {
                                Triangle nextTri = vEdge1.rightTriangle;
                                TriEdge nextEdge = vEdge1.doulEdge;
                                TriEdge vNextEdge1 = null;
                                TriEdge vNextEdge2 = null;
                                TriNode vNextV = null;
                                TriEdge nextRe = null;

                                while (nextTri != null && nextTri.TriType == 1)
                                {
                                    nextRe = nextTri.GetVEdgeofT1(out vNextEdge1, out vNextEdge2, out vNextV);
                                    if (vNextV.ID == cNode1.ID)
                                    {
                                        if (nextEdge.ID == vNextEdge1.ID)
                                        {
                                            nextEdge = vNextEdge2;
                                        }
                                        else
                                        {
                                            nextEdge = vNextEdge1;
                                        }
                                        nextTri = nextEdge.rightTriangle;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                if (nextTri != null)
                                {
                                    nextEdge.rightTriangle = null;
                                    TriEdge dEdge = nextEdge.doulEdge;
                                    nextEdge.doulEdge = null;
                                    dEdge.rightTriangle = null;
                                    dEdge.doulEdge = null;
                                }
                            }
                            else if (vEdge2.startPoint.ID == cNode1.ID || vEdge2.endPoint.ID == cNode1.ID)
                            {
                                Triangle nextTri = vEdge2.rightTriangle;
                                TriEdge nextEdge = vEdge2.doulEdge;
                                TriEdge vNextEdge1 = null;
                                TriEdge vNextEdge2 = null;
                                TriNode vNextV = null;
                                TriEdge nextRe = null;

                                while (nextTri != null && nextTri.TriType == 1)
                                {
                                    nextRe = nextTri.GetVEdgeofT1(out vNextEdge1, out vNextEdge2, out vNextV);
                                    if (vNextV.ID == cNode1.ID)
                                    {
                                        if (nextEdge.ID == vNextEdge1.ID)
                                        {
                                            nextEdge = vNextEdge2;
                                        }
                                        else
                                        {
                                            nextEdge = vNextEdge1;
                                        }
                                        nextTri = nextEdge.rightTriangle;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                if (nextTri != null)
                                {
                                    nextEdge.rightTriangle = null;
                                    TriEdge dEdge = nextEdge.doulEdge;
                                    nextEdge.doulEdge = null;
                                    dEdge.rightTriangle = null;
                                    dEdge.doulEdge = null;
                                }
                            }

                        }
                        else if (cNode1 == null && cNode2 != null)
                        {
                            if (vEdge1.startPoint.ID == cNode2.ID || vEdge1.endPoint.ID == cNode2.ID)
                            {
                                Triangle nextTri = vEdge1.rightTriangle;
                                TriEdge nextEdge = vEdge1.doulEdge;
                                TriEdge vNextEdge1 = null;
                                TriEdge vNextEdge2 = null;
                                TriNode vNextV = null;
                                TriEdge nextRe = null;

                                while (nextTri != null && nextTri.TriType == 1)
                                {
                                    nextRe = nextTri.GetVEdgeofT1(out vNextEdge1, out vNextEdge2, out vNextV);
                                    if (vNextV.ID == cNode2.ID)
                                    {
                                        if (nextEdge.ID == vNextEdge1.ID)
                                        {
                                            nextEdge = vNextEdge2;
                                        }
                                        else
                                        {
                                            nextEdge = vNextEdge1;
                                        }
                                        nextTri = nextEdge.rightTriangle;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                if (nextTri != null)
                                {
                                    nextEdge.rightTriangle = null;
                                    TriEdge dEdge = nextEdge.doulEdge;
                                    nextEdge.doulEdge = null;
                                    dEdge.rightTriangle = null;
                                    dEdge.doulEdge = null;
                                }
                            }
                            else if (vEdge2.startPoint.ID == cNode2.ID || vEdge2.endPoint.ID == cNode2.ID)
                            {

                                Triangle nextTri = vEdge2.rightTriangle;
                                TriEdge nextEdge = vEdge2.doulEdge;
                                TriEdge vNextEdge1 = null;
                                TriEdge vNextEdge2 = null;
                                TriNode vNextV = null;
                                TriEdge nextRe = null;

                                while (nextTri != null && nextTri.TriType == 1)
                                {
                                    nextRe = nextTri.GetVEdgeofT1(out vNextEdge1, out vNextEdge2, out vNextV);
                                    if (vNextV.ID == cNode2.ID)
                                    {
                                        if (nextEdge.ID == vNextEdge1.ID)
                                        {
                                            nextEdge = vNextEdge2;
                                        }
                                        else
                                        {
                                            nextEdge = vNextEdge1;
                                        }
                                        nextTri = nextEdge.rightTriangle;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                if (nextTri != null)
                                {
                                    nextEdge.rightTriangle = null;
                                    TriEdge dEdge = nextEdge.doulEdge;
                                    nextEdge.doulEdge = null;
                                    dEdge.rightTriangle = null;
                                    dEdge.doulEdge = null;
                                }
                            }
                        }
                        else if (cNode1 != null && cNode2 != null)
                        {
                            if (vEdge1.startPoint.ID == cNode1.ID || vEdge1.endPoint.ID == cNode1.ID)
                            {
                                Triangle nextTri = vEdge1.rightTriangle;
                                TriEdge nextEdge = vEdge1.doulEdge;
                                TriEdge vNextEdge1 = null;
                                TriEdge vNextEdge2 = null;
                                TriNode vNextV = null;
                                TriEdge nextRe = null;

                                while (nextTri != null && nextTri.TriType == 1)
                                {

                                    nextRe = nextTri.GetVEdgeofT1(out vNextEdge1, out vNextEdge2, out vNextV);
                                    if (vNextV.ID == cNode1.ID)
                                    {
                                        if (nextEdge.ID == vNextEdge1.ID)
                                        {
                                            nextEdge = vNextEdge2;
                                        }
                                        else
                                        {
                                            nextEdge = vNextEdge1;
                                        }
                                        nextTri = nextEdge.rightTriangle;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                if (nextTri != null)
                                {
                                    nextEdge.rightTriangle = null;
                                    TriEdge dEdge = nextEdge.doulEdge;
                                    nextEdge.doulEdge = null;
                                    dEdge.rightTriangle = null;
                                    dEdge.doulEdge = null;
                                }
                            }
                            else if (vEdge2.startPoint.ID == cNode1.ID || vEdge2.endPoint.ID == cNode1.ID)
                            {
                                Triangle nextTri = vEdge2.rightTriangle;
                                TriEdge nextEdge = vEdge2.doulEdge;
                                TriEdge vNextEdge1 = null;
                                TriEdge vNextEdge2 = null;
                                TriNode vNextV = null;
                                TriEdge nextRe = null;

                                while (nextTri != null && nextTri.TriType == 1)
                                {
                                    nextRe = nextTri.GetVEdgeofT1(out vNextEdge1, out vNextEdge2, out vNextV);
                                    if (vNextV.ID == cNode1.ID)
                                    {
                                        if (nextEdge.ID == vNextEdge1.ID)
                                        {
                                            nextEdge = vNextEdge2;
                                        }
                                        else
                                        {
                                            nextEdge = vNextEdge1;
                                        }
                                        nextTri = nextEdge.rightTriangle;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                if (nextTri != null)
                                {
                                    nextEdge.rightTriangle = null;
                                    TriEdge dEdge = nextEdge.doulEdge;
                                    nextEdge.doulEdge = null;
                                    dEdge.rightTriangle = null;
                                    dEdge.doulEdge = null;
                                }
                            }

                            if (vEdge1.startPoint.ID == cNode2.ID || vEdge1.endPoint.ID == cNode2.ID)
                            {
                                Triangle nextTri = vEdge1.rightTriangle;
                                TriEdge nextEdge = vEdge1.doulEdge;
                                TriEdge vNextEdge1 = null;
                                TriEdge vNextEdge2 = null;
                                TriNode vNextV = null;
                                TriEdge nextRe = null;

                                while (nextTri != null && nextTri.TriType == 1)
                                {
                                    nextRe = nextTri.GetVEdgeofT1(out vNextEdge1, out vNextEdge2, out vNextV);
                                    if (vNextV.ID == cNode2.ID)
                                    {
                                        if (nextEdge.ID == vNextEdge1.ID)
                                        {
                                            nextEdge = vNextEdge2;
                                        }
                                        else
                                        {
                                            nextEdge = vNextEdge1;
                                        }
                                        nextTri = nextEdge.rightTriangle;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                if (nextTri != null)
                                {
                                    nextEdge.rightTriangle = null;
                                    TriEdge dEdge = nextEdge.doulEdge;
                                    nextEdge.doulEdge = null;
                                    dEdge.rightTriangle = null;
                                    dEdge.doulEdge = null;
                                }
                            }
                            else if (vEdge2.startPoint.ID == cNode2.ID || vEdge2.endPoint.ID == cNode2.ID)
                            {

                                Triangle nextTri = vEdge2.rightTriangle;
                                TriEdge nextEdge = vEdge2.doulEdge;
                                TriEdge vNextEdge1 = null;
                                TriEdge vNextEdge2 = null;
                                TriNode vNextV = null;
                                TriEdge nextRe = null;

                                while (nextTri != null && nextTri.TriType == 1)
                                {
                                    nextRe = nextTri.GetVEdgeofT1(out vNextEdge1, out vNextEdge2, out vNextV);
                                    if (vNextV.ID == cNode2.ID)
                                    {
                                        if (nextEdge.ID == vNextEdge1.ID)
                                        {
                                            nextEdge = vNextEdge2;
                                        }
                                        else
                                        {
                                            nextEdge = vNextEdge1;
                                        }
                                        nextTri = nextEdge.rightTriangle;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                if (nextTri != null)
                                {
                                    nextEdge.rightTriangle = null;
                                    TriEdge dEdge = nextEdge.doulEdge;
                                    nextEdge.doulEdge = null;
                                    dEdge.rightTriangle = null;
                                    dEdge.doulEdge = null;
                                }
                            }
                        }
                    }
                }
            }


            //foreach (Triangle curTri in CDT.TriangleList)
            //{

            //    if (curTri.TriType == 1)
            //    {

            //        TriEdge vEdge1 = null;
            //        TriEdge vEdge2 = null;
            //        TriNode vV = null;
            //        TriEdge re = curTri.GetVEdgeofT1( out vEdge1, out vEdge2, out vV);
            //        //端点在实边上
            //        ConNode cNode1 = ConNode.GetPLbyID(Map.ConNodeList, re.startPoint.ID);
            //        ConNode cNode2 = ConNode.GetPLbyID(Map.ConNodeList, re.endPoint.ID);
            //        ConNode cNode3 = ConNode.GetPLbyID(Map.ConNodeList, vV.ID);
            //        //无端点
            //        if (cNode1 == null && cNode2 == null && cNode3 == null)
            //        {
            //            continue;
            //        }
            //        //一条虚边上仅有一个端点，且在实边上
            //        else if ((cNode1 != null || cNode2 != null) && cNode3 == null)
            //        {
            //            if (cNode1 != null && cNode2 == null)
            //            {
            //                if (vEdge1.startPoint.ID == cNode1.ID || vEdge1.endPoint.ID == cNode1.ID)
            //                {
            //                    Triangle nextTri = vEdge1.rightTriangle;
            //                    TriEdge nextEdge = vEdge1.doulEdge;
            //                    TriEdge vNextEdge1 = null;
            //                    TriEdge vNextEdge2 = null;
            //                    TriNode vNextV = null;
            //                    TriEdge nextRe = null;

            //                    while (nextTri != null && nextTri.TriType == 1)
            //                    {
            //                        nextRe =nextTri.GetVEdgeofT1(out vNextEdge1, out vNextEdge2, out vNextV);
            //                        if (vNextV.ID == cNode1.ID)
            //                        {
            //                            if (nextEdge.ID == vNextEdge1.ID)
            //                            {
            //                                nextEdge = vNextEdge2;
            //                            }
            //                            else
            //                            {
            //                                nextEdge = vNextEdge1;
            //                            }
            //                            nextTri = nextEdge.rightTriangle;
            //                        }
            //                        else
            //                        {
            //                            break;
            //                        }
            //                    }
            //                    if (nextTri != null)
            //                    {
            //                        nextEdge.rightTriangle = null;
            //                        TriEdge dEdge = nextEdge.doulEdge;
            //                        nextEdge.doulEdge = null;
            //                        dEdge.rightTriangle = null;
            //                        dEdge.doulEdge = null;
            //                    }
            //                }
            //                else if (vEdge2.startPoint.ID == cNode1.ID || vEdge2.endPoint.ID == cNode1.ID)
            //                {
            //                    Triangle nextTri = vEdge2.rightTriangle;
            //                    TriEdge nextEdge = vEdge2.doulEdge;
            //                    TriEdge vNextEdge1 = null;
            //                    TriEdge vNextEdge2 = null;
            //                    TriNode vNextV = null;
            //                    TriEdge nextRe = null;

            //                    while (nextTri != null && nextTri.TriType == 1)
            //                    {
            //                        nextRe = nextTri.GetVEdgeofT1(out vNextEdge1, out vNextEdge2, out vNextV);
            //                        if (vNextV.ID == cNode1.ID)
            //                        {
            //                            if (nextEdge.ID == vNextEdge1.ID)
            //                            {
            //                                nextEdge = vNextEdge2;
            //                            }
            //                            else
            //                            {
            //                                nextEdge = vNextEdge1;
            //                            }
            //                            nextTri = nextEdge.rightTriangle;
            //                        }
            //                        else
            //                        {
            //                            break;
            //                        }
            //                    }
            //                    if (nextTri != null)
            //                    {
            //                        nextEdge.rightTriangle = null;
            //                        TriEdge dEdge = nextEdge.doulEdge;
            //                        nextEdge.doulEdge = null;
            //                        dEdge.rightTriangle = null;
            //                        dEdge.doulEdge = null;
            //                    }
            //                }

            //            }
            //            else if (cNode1 == null && cNode2 != null)
            //            {
            //                if (vEdge1.startPoint.ID == cNode2.ID || vEdge1.endPoint.ID == cNode2.ID)
            //                {
            //                    Triangle nextTri = vEdge1.rightTriangle;
            //                    TriEdge nextEdge = vEdge1.doulEdge;
            //                    TriEdge vNextEdge1 = null;
            //                    TriEdge vNextEdge2 = null;
            //                    TriNode vNextV = null;
            //                    TriEdge nextRe = null;

            //                    while (nextTri != null && nextTri.TriType == 1)
            //                    {
            //                        nextRe = nextTri.GetVEdgeofT1( out vNextEdge1, out vNextEdge2, out vNextV);
            //                        if (vNextV.ID == cNode2.ID)
            //                        {
            //                            if (nextEdge.ID == vNextEdge1.ID)
            //                            {
            //                                nextEdge = vNextEdge2;
            //                            }
            //                            else
            //                            {
            //                                nextEdge = vNextEdge1;
            //                            }
            //                            nextTri = nextEdge.rightTriangle;
            //                        }
            //                        else
            //                        {
            //                            break;
            //                        }
            //                    }
            //                    if (nextTri != null)
            //                    {
            //                        nextEdge.rightTriangle = null;
            //                        TriEdge dEdge = nextEdge.doulEdge;
            //                        nextEdge.doulEdge = null;
            //                        dEdge.rightTriangle = null;
            //                        dEdge.doulEdge = null;
            //                    }
            //                }
            //                else if (vEdge2.startPoint.ID == cNode2.ID || vEdge2.endPoint.ID == cNode2.ID)
            //                {

            //                    Triangle nextTri = vEdge2.rightTriangle;
            //                    TriEdge nextEdge = vEdge2.doulEdge;
            //                    TriEdge vNextEdge1 = null;
            //                    TriEdge vNextEdge2 = null;
            //                    TriNode vNextV = null;
            //                    TriEdge nextRe = null;

            //                    while (nextTri != null && nextTri.TriType == 1)
            //                    {
            //                        nextRe = nextTri.GetVEdgeofT1( out vNextEdge1, out vNextEdge2, out vNextV);
            //                        if (vNextV.ID == cNode2.ID)
            //                        {
            //                            if (nextEdge.ID == vNextEdge1.ID)
            //                            {
            //                                nextEdge = vNextEdge2;
            //                            }
            //                            else
            //                            {
            //                                nextEdge = vNextEdge1;
            //                            }
            //                            nextTri = nextEdge.rightTriangle;
            //                        }
            //                        else
            //                        {
            //                            break;
            //                        }
            //                    }
            //                    if (nextTri != null)
            //                    {
            //                        nextEdge.rightTriangle = null;
            //                        TriEdge dEdge = nextEdge.doulEdge;
            //                        nextEdge.doulEdge = null;
            //                        dEdge.rightTriangle = null;
            //                        dEdge.doulEdge = null;
            //                    }
            //                }
            //            }
            //            else if (cNode1 != null && cNode2 != null)
            //            {
            //                if (vEdge1.startPoint.ID == cNode1.ID || vEdge1.endPoint.ID == cNode1.ID)
            //                {
            //                    Triangle nextTri = vEdge1.rightTriangle;
            //                    TriEdge nextEdge = vEdge1.doulEdge;
            //                    TriEdge vNextEdge1 = null;
            //                    TriEdge vNextEdge2 = null;
            //                    TriNode vNextV = null;
            //                    TriEdge nextRe = null;

            //                    while (nextTri != null && nextTri.TriType == 1)
            //                    {

            //                        nextRe = nextTri.GetVEdgeofT1(out vNextEdge1, out vNextEdge2, out vNextV);
            //                        if (vNextV.ID == cNode1.ID)
            //                        {
            //                            if (nextEdge.ID == vNextEdge1.ID)
            //                            {
            //                                nextEdge = vNextEdge2;
            //                            }
            //                            else
            //                            {
            //                                nextEdge = vNextEdge1;
            //                            }
            //                            nextTri = nextEdge.rightTriangle;
            //                        }
            //                        else
            //                        {
            //                            break;
            //                        }
            //                    }
            //                    if (nextTri != null)
            //                    {
            //                        nextEdge.rightTriangle = null;
            //                        TriEdge dEdge = nextEdge.doulEdge;
            //                        nextEdge.doulEdge = null;
            //                        dEdge.rightTriangle = null;
            //                        dEdge.doulEdge = null;
            //                    }
            //                }
            //                else if (vEdge2.startPoint.ID == cNode1.ID || vEdge2.endPoint.ID == cNode1.ID)
            //                {
            //                    Triangle nextTri = vEdge2.rightTriangle;
            //                    TriEdge nextEdge = vEdge2.doulEdge;
            //                    TriEdge vNextEdge1 = null;
            //                    TriEdge vNextEdge2 = null;
            //                    TriNode vNextV = null;
            //                    TriEdge nextRe = null;

            //                    while (nextTri != null && nextTri.TriType == 1)
            //                    {
            //                        nextRe =nextTri.GetVEdgeofT1( out vNextEdge1, out vNextEdge2, out vNextV);
            //                        if (vNextV.ID == cNode1.ID)
            //                        {
            //                            if (nextEdge.ID == vNextEdge1.ID)
            //                            {
            //                                nextEdge = vNextEdge2;
            //                            }
            //                            else
            //                            {
            //                                nextEdge = vNextEdge1;
            //                            }
            //                            nextTri = nextEdge.rightTriangle;
            //                        }
            //                        else
            //                        {
            //                            break;
            //                        }
            //                    }
            //                    if (nextTri != null)
            //                    {
            //                        nextEdge.rightTriangle = null;
            //                        TriEdge dEdge = nextEdge.doulEdge;
            //                        nextEdge.doulEdge = null;
            //                        dEdge.rightTriangle = null;
            //                        dEdge.doulEdge = null;
            //                    }
            //                }

            //                if (vEdge1.startPoint.ID == cNode2.ID || vEdge1.endPoint.ID == cNode2.ID)
            //                {
            //                    Triangle nextTri = vEdge1.rightTriangle;
            //                    TriEdge nextEdge = vEdge1.doulEdge;
            //                    TriEdge vNextEdge1 = null;
            //                    TriEdge vNextEdge2 = null;
            //                    TriNode vNextV = null;
            //                    TriEdge nextRe = null;

            //                    while (nextTri != null && nextTri.TriType == 1)
            //                    {
            //                        nextRe = nextTri.GetVEdgeofT1( out vNextEdge1, out vNextEdge2, out vNextV);
            //                        if (vNextV.ID == cNode2.ID)
            //                        {
            //                            if (nextEdge.ID == vNextEdge1.ID)
            //                            {
            //                                nextEdge = vNextEdge2;
            //                            }
            //                            else
            //                            {
            //                                nextEdge = vNextEdge1;
            //                            }
            //                            nextTri = nextEdge.rightTriangle;
            //                        }
            //                        else
            //                        {
            //                            break;
            //                        }
            //                    }
            //                    if (nextTri != null)
            //                    {
            //                        nextEdge.rightTriangle = null;
            //                        TriEdge dEdge = nextEdge.doulEdge;
            //                        nextEdge.doulEdge = null;
            //                        dEdge.rightTriangle = null;
            //                        dEdge.doulEdge = null;
            //                    }
            //                }
            //                else if (vEdge2.startPoint.ID == cNode2.ID || vEdge2.endPoint.ID == cNode2.ID)
            //                {

            //                    Triangle nextTri = vEdge2.rightTriangle;
            //                    TriEdge nextEdge = vEdge2.doulEdge;
            //                    TriEdge vNextEdge1 = null;
            //                    TriEdge vNextEdge2 = null;
            //                    TriNode vNextV = null;
            //                    TriEdge nextRe = null;

            //                    while (nextTri != null && nextTri.TriType == 1)
            //                    {
            //                        nextRe = nextTri.GetVEdgeofT1(out vNextEdge1, out vNextEdge2, out vNextV);
            //                        if (vNextV.ID == cNode2.ID)
            //                        {
            //                            if (nextEdge.ID == vNextEdge1.ID)
            //                            {
            //                                nextEdge = vNextEdge2;
            //                            }
            //                            else
            //                            {
            //                                nextEdge = vNextEdge1;
            //                            }
            //                            nextTri = nextEdge.rightTriangle;
            //                        }
            //                        else
            //                        {
            //                            break;
            //                        }
            //                    }
            //                    if (nextTri != null)
            //                    {
            //                        nextEdge.rightTriangle = null;
            //                        TriEdge dEdge = nextEdge.doulEdge;
            //                        nextEdge.doulEdge = null;
            //                        dEdge.rightTriangle = null;
            //                        dEdge.doulEdge = null;
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}
        }

        /// <summary>
        /// 遍历三角形构建骨架线段For道路网
        /// </summary>
        public void TranverseSkeleton_Segment_NT()
        {
            PreProcessCDTforRNT();
            TranverseSkeleton_Arc();
        }

        /// <summary>
        /// 遍历三角形构建骨架线段For道路网
        /// </summary>
        public void TranverseSkeleton_Segment_NT_DeleteIntraSkel()
        {
            PreProcessCDTforRNT();
            TranverseSkeleton_Arc();
            //删除相同路段内部的骨架线-如果保持内部骨架线就出错
            PostNTDeleteIntraSeleton();
            //保持内部的骨架线
        }

        /// <summary>
        /// 删除相同路段内部的骨架线
        /// </summary>
        private void PostNTDeleteIntraSeleton()
        {
            List<Skeleton_Arc> skeletonarc = new List<Skeleton_Arc>();
            foreach (Skeleton_Arc arc in this.Skeleton_ArcList)
            {
                if (arc.RightMapObj == arc.LeftMapObj)
                    skeletonarc.Add(arc);

            }

            foreach (Skeleton_Arc arc in skeletonarc)
            {
                this.Skeleton_ArcList.Remove(arc);
            }
        }

        /// <summary>
        /// 遍历三角形构建骨架线段For道路网(删除路段内部三角形)
        /// </summary>
        public void TranverseSkeleton_Segment_NT_RoadSeg()
        {
            PreProcessDeleRoadSeg();//删除同一路段内部三角形
            PreProcessCDTforRNT();//将不同路段相同骨架线弧段的三角形列表隔离开
            TranverseSkeleton_Arc();
        }

        /// <summary>
        /// 遍历三角形构建骨架线段For街区
        /// </summary>
        public void TranverseSkeleton_Segment_PLP()
        {
            PreProcessCDTforPLP(true);//删除凹部
            PreProcessCDTforRNT();//路段端点处理
            TranverseSkeleton_Arc();
            PostDeleteRepeatedArcs();
            PostProcessDelSkeArcInSameObject();//删除同一对象内部或凹部的的骨架线
        }

        /// <summary>
        /// 遍历三角形构建骨架线段For街区
        /// </summary>
        public void TranverseSkeleton_Segment_PLP_NONull()
        {
            PreProcessCDTforPLP(true);//删除凹部
            PreProcessCDTforRNT();//路段端点处理

            TranverseSkeleton_Arc();
            PostDeleteRepeatedArcs();
            PostProcessDelSkeArcInSameObject();//删除同一对象内部或凹部的的骨架线
            this.PostDeleteNullObjectsArcs();                                                                                                                  
        }

        /// <summary>
        /// 湖泊
        /// </summary>
        public void TranverseSkeleton_Lakers()
        {
            PreProcessCDTforPLP(true);//删除凹部
            TranverseSkeleton_Arc();
            this.PostProcessDelSkeArcInSameObject();//删除同一对象内部或凹部的的骨架线
        }

        /// <summary>
        /// 街区
        /// </summary>
        public void TranverseSkeleton_Building()
        {
            PreProcessCDTforPLP(true);//删除凹部
            TranverseSkeleton_Arc();
          // this.PostProcessSkeforLakers();
        }

        /// <summary>
        /// 删除同一对象内部或凹部的的骨架线
        /// 条件：（1）ID相同；（2）FeatureType相同
        /// </summary>
        private void PostProcessDelSkeArcInSameObject()
        {
            List<Skeleton_Arc> deleteArcs = new List<Skeleton_Arc>();
            foreach (Skeleton_Arc curArc in this.Skeleton_ArcList)
            {
                if (curArc.LeftMapObj != null && curArc.RightMapObj != null)
                {
                    if ((curArc.LeftMapObj.ID == curArc.RightMapObj.ID) && (curArc.LeftMapObj.FeatureType == curArc.RightMapObj.FeatureType))
                    {
                        deleteArcs.Add(curArc);
                    }
                }
            }

            foreach (Skeleton_Arc dArc in deleteArcs)
            {
                this.Skeleton_ArcList.Remove(dArc);
            }
        }

        /// <summary>
        /// 判断三角形是否全部遍历过
        /// </summary>
        /// <param name="tranversed">是否遍历标示数组</param>
        /// <param name="index">当前没有遍历的三角形下标</param>
        /// <returns></returns>
        private bool IsAllTranversed(int[] tranversed, out int index)
        {
            index = -1;
            for (int i = 0; i < tranversed.Length; i++)
            {
               /* if (tranversed[i] == 0 && CDT.TriangleList[i].TriType!=0)
                {
                    index = i;
                    return false;
                }
                else if (tranversed[i] < 3 && CDT.TriangleList[i].TriType == 0)
                {
                        index = i;
                        return false;
                }*/

                if (tranversed[i] == 0 && CDT.TriangleList[i].TriType == 1)
                {
                    index = i;
                    return false;
                }

            }
            return true;
        }

        /// <summary>
        /// 通过遍历构建骨架线
        /// </summary>
        public void TranverseSkeleton_Arc()
        {
            this.Skeleton_ArcList = new List<Skeleton_Arc>();//骨架线弧段列表
            //以下实现遍历三角形形成骨架线段的算法

            //定义栈
            Stack<Triangle> StaTri = new Stack<Triangle>();
            Stack<TriEdge> StaEdge = new Stack<TriEdge>();
            Stack<TriNode> StaCP = new Stack<TriNode>();

            if (this.CDT.TriangleList == null || this.CDT.TriangleList.Count == 0)
                return;

            int TriCount = this.CDT.TriangleList.Count;
            int[] TriTranversed = new int[TriCount];
            for (int j = 0; j < TriCount; j++)
            {
                TriTranversed[j] = 0;
            }

            Triangle startTri = null;    //起始三角形，可以是2类0类或1类中的单连通者
            Triangle nextTri = null;     //骨架线段中间的三角形
            Skeleton_Arc newArc = null;  //新的一条骨架线段
            double curL = 0;

            TriEdge vedge = null;
            TriEdge ovedge = null;

            int i = 0;
            TriNode cp = null;

            int ArcID = 0;

            //栈不为空或所有的三角形还没有遍历完
            while (StaTri.Count != 0 || i < TriCount)
            {
                if (ArcID == 43 || ArcID == 100)
                {
                    int error = 0;
                }
                #region 骨架线上第一个三角形
                #region 栈中无0类三角形
                if (StaTri.Count == 0)
                {
                    startTri = CDT.TriangleList[i]; 
                    #region 如果是3类
                    if (startTri.TriType == 3)
                    {
                        TriTranversed[i] = 1;
                        TriNode centerV = ComFunLib.CalCenter(startTri);
                        TriEdge FE = null;
                        TriEdge LE = null;
                        TriEdge RE = null;

                        MapObject FO = null;
                        MapObject LO = null;
                        MapObject RO = null;

                        startTri.GetEdgesofT3(out LE, out RE, out FE);
                        int curtagID = -1;
                        FeatureType curType = FeatureType.Unknown;

                        curtagID = FE.tagID;
                        curType = FE.FeatureType;
                        LO = Map.GetObjectbyID(curtagID, curType);

                        curtagID = RE.tagID;
                        curType = RE.FeatureType;
                        RO = Map.GetObjectbyID(curtagID, curType);

                        curtagID = LE.tagID;
                        curType = LE.FeatureType;
                        FO = Map.GetObjectbyID(curtagID, curType);

                        newArc = new Skeleton_Arc(ArcID);
                        newArc.TriangleList.Add(startTri);//加入起始三角形
                        newArc.PointList.Add(startTri.point1);
                        newArc.PointList.Add(centerV);
                        newArc.LeftMapObj = LO;
                        newArc.RightMapObj = RO;
                        newArc.FrontMapObj = FO;
                        newArc.NearestEdge = new NearestEdge(ArcID, new NearestPoint(-1, startTri.point1.X, startTri.point1.Y), new NearestPoint(-1, startTri.point1.X, startTri.point1.Y), 0);
                        newArc.AveDistance = startTri.CalAveDisforT2_3(startTri.point1);
                        newArc.WAD = newArc.AveDistance;
                        newArc.Length = ComFunLib.CalLineLength(startTri.point1, centerV);
                        this.Skeleton_ArcList.Add(newArc); ArcID++;

                        newArc = new Skeleton_Arc(ArcID);
                        newArc.TriangleList.Add(startTri);//加入起始三角形
                        newArc.PointList.Add(startTri.point2);
                        newArc.PointList.Add(centerV);
                        newArc.LeftMapObj = RO;
                        newArc.RightMapObj = FO;
                        newArc.FrontMapObj = LO;
                        newArc.NearestEdge = new NearestEdge(ArcID, new NearestPoint(-1, startTri.point2.X, startTri.point2.Y), new NearestPoint(-1, startTri.point2.X, startTri.point2.Y), 0);
                        newArc.AveDistance = startTri.CalAveDisforT2_3(startTri.point2);
                        newArc.WAD = newArc.AveDistance;
                        newArc.Length = ComFunLib.CalLineLength(startTri.point2, centerV);
                        this.Skeleton_ArcList.Add(newArc); ArcID++;

                        newArc = new Skeleton_Arc(ArcID);
                        newArc.TriangleList.Add(startTri);//加入起始三角形
                        newArc.PointList.Add(startTri.point3);
                        newArc.PointList.Add(centerV);
                        newArc.LeftMapObj = FO;
                        newArc.RightMapObj = LO;
                        newArc.FrontMapObj = RO;
                        newArc.NearestEdge = new NearestEdge(ArcID, new NearestPoint(-1, startTri.point3.X, startTri.point3.Y), new NearestPoint(-1, startTri.point3.X, startTri.point3.Y), 0);
                        newArc.AveDistance = startTri.CalAveDisforT2_3(startTri.point3);
                        newArc.WAD = newArc.AveDistance;
                        newArc.Length = ComFunLib.CalLineLength(startTri.point3, centerV);
                        this.Skeleton_ArcList.Add(newArc); ArcID++;
                        i++;
                        continue;
                    }
                    #endregion

                    #region 不联通1类
                    else if (startTri.TriType == 1 && TriTranversed[i] == 0 && startTri.IsNoPathT1())
                    {
                        TriTranversed[i] = 1;
                        TriEdge vE1 = null;//顶点
                        TriEdge vE2 = null;//实边顶点
                        TriEdge rE = null;//实边
                        TriNode vVex = null;//实边相对的边

                        rE = startTri.GetVEdgeofT1(out vE1, out vE2, out vVex);
                        NearestPoint nearestPoint;
                        double minDis = startTri.CalMinDisforT1(vVex, rE.startPoint, rE.endPoint, out nearestPoint);
                        startTri.W = minDis;
                        if (vE1 == null || vE2 == null)
                        {
                            int error = 0;

                        }

                        TriNode n1 = ComFunLib.CalLineCenterPoint(vE1);
                        TriNode n2 = ComFunLib.CalLineCenterPoint(vE2);
                        string res = ComFunLib.funReturnRightOrLeft(n1, n2, vVex);
                        int curtagID = -1;
                        FeatureType curType = FeatureType.Unknown;

                        newArc = new Skeleton_Arc(ArcID);
                        newArc.TriangleList.Add(startTri);//加入起始三角形
                        newArc.PointList.Add(n1);
                        newArc.PointList.Add(n2);
                        newArc.Length = ComFunLib.CalLineLength(n1, n2);
                        if (res == "LEFT")
                        {
                            curtagID = vVex.TagValue;
                            curType = vVex.FeatureType;
                            newArc.LeftMapObj = Map.GetObjectbyID(curtagID, curType);

                            curtagID = rE.tagID;
                            curType = rE.FeatureType;
                            newArc.RightMapObj = Map.GetObjectbyID(curtagID, curType);

                            newArc.NearestEdge = new NearestEdge(ArcID, new NearestPoint(-1, vVex.X, vVex.Y), nearestPoint, minDis);
                            newArc.AveDistance = minDis;
                            newArc.WAD = newArc.AveDistance;
                        }
                        else
                        {
                            curtagID = vVex.TagValue;
                            curType = vVex.FeatureType;
                            newArc.RightMapObj = Map.GetObjectbyID(curtagID, curType);

                            curtagID = rE.tagID;
                            curType = rE.FeatureType;
                            newArc.LeftMapObj = Map.GetObjectbyID(curtagID, curType);

                            newArc.NearestEdge = new NearestEdge(ArcID, nearestPoint, new NearestPoint(-1, vVex.X, vVex.Y), minDis);
                            newArc.AveDistance = minDis;
                            newArc.WAD = newArc.AveDistance;
                           
                        }
                        this.Skeleton_ArcList.Add(newArc); ArcID++;
                        i++;
                        continue;
                    }
                    #endregion

                    #region 2类或1类
                    else if ((startTri.TriType == 2 && TriTranversed[i] == 0 ||
                    (startTri.TriType == 1 && TriTranversed[i] == 0 && startTri.IsSinglePathT1())))
                    {
                        TriTranversed[i] = 1;
                        newArc = new Skeleton_Arc(ArcID);
                        newArc.TriangleList.Add(startTri);//加入起始三角形

                        if (startTri.TriType == 2)
                        {
                            TriEdge LE = null;
                            TriEdge RE = null;

                            //计算并加入起点,并得到下一虚拟边
                            TriNode startP = startTri.GetStartPointofT2(out vedge, out LE, out RE);
                            newArc.PointList.Add(startP);

                            int curtagID = -1;
                            FeatureType curType = FeatureType.Unknown;

                            curtagID = LE.tagID;
                            curType = LE.FeatureType;
                            newArc.LeftMapObj = Map.GetObjectbyID(curtagID, curType);

                            curtagID = RE.tagID;
                            curType = RE.FeatureType;
                            newArc.RightMapObj = Map.GetObjectbyID(curtagID, curType);

                            if (vedge == null)
                            {
                                int error = 0;
                            }

                            newArc.PointList.Add(ComFunLib.CalLineCenterPoint(vedge));

                            TriNode p1 = newArc.PointList[newArc.PointList.Count - 2];
                            TriNode p2=newArc.PointList[newArc.PointList.Count - 1];
                            curL = ComFunLib.CalLineLength(p1, p2);
                            
                            newArc.NearestEdge = new NearestEdge(ArcID, new NearestPoint(-1, startP.X, startP.Y), new NearestPoint(-1, startP.X, startP.Y), 0);
                            newArc.AveDistance += 0.5 * vedge.Length;
                            newArc.WAD += 0.5 * vedge.Length*curL;
                            newArc.Length+=curL;
                            //2014-2-26
                            startTri.W = vedge.Length;
                        }

                        else if (startTri.TriType == 1) //单连通1类
                        {
                            TriEdge e = startTri.GetStartEdgeofT1();
                            vedge = startTri.GetOtherVEdgeofT1(e);
                            if (vedge == null || e == null)
                            {
                                int error = 0;
                            }
                            TriNode n1 = ComFunLib.CalLineCenterPoint(e);
                            TriNode n2 = ComFunLib.CalLineCenterPoint(vedge);
                            //计算并加入起点和第二个点
                            newArc.PointList.Add(n1);
                            newArc.PointList.Add(n2);
                            //curL = ComFunLib.CalLineLength(n1, n2);
                            SetMapObjsandDistforT1(ref  newArc, startTri, n1, n2);
                          //  newArc.WAD += newArc.NearestEdge.NearestDistance * curL;
                          //  newArc.Length += curL;
                        }
                        //计算并加入第二点
                        nextTri = vedge.rightTriangle;
                        i++;
                    }
                    #endregion

                    #region 0类
                    else if (startTri.TriType == 0 && TriTranversed[i] == 0)
                    {
                        TriTranversed[i]=1;

                        TriNode centerV = ComFunLib.CalCenter(startTri);
                        newArc = new Skeleton_Arc(ArcID);
                        newArc.TriangleList.Add(startTri);//加入起始三角形
                        newArc.PointList.Add(centerV);

                        TriNode n = ComFunLib.CalLineCenterPoint(startTri.edge1);
                        newArc.PointList.Add(n);

                        curL = ComFunLib.CalLineLength(centerV, n);
  
                        this.SetMapObjectandDistforT0(ref newArc, centerV, n, startTri.edge1);
                        vedge = startTri.edge1;
                        nextTri = vedge.rightTriangle;

                        StaTri.Push(startTri);
                        StaEdge.Push(startTri.edge2);
                        StaCP.Push(centerV);

                        StaTri.Push(startTri);
                        StaEdge.Push(startTri.edge3);
                        StaCP.Push(centerV);
                        i++;
                    }
                    #endregion
                    else
                    {
                        i++;
                        continue;
                    }
                }
                #endregion

                #region 栈中存在0类三角形
                else//栈中存在0类三角形
                {
                    //出栈
                    startTri = StaTri.Pop();
                    vedge = StaEdge.Pop(); //虚边
                    cp = StaCP.Pop();
                    //如果是下一个三角形不为空，但已经被访问过，则说明是已经遍历的环路跳过(vedge.leftTriangle!=null&&TriTranversed[vedge.rightTriangle.ID] == true)
                    if (vedge == null || (vedge.rightTriangle != null && TriTranversed[vedge.rightTriangle.ID] >0 && vedge.rightTriangle.ID == startTri.ID)||TriTranversed[startTri.ID]==3)
                    {
                        continue;
                    }
                    TriTranversed[startTri.ID]=1;
                    newArc = new Skeleton_Arc(ArcID);

                    TriNode n = ComFunLib.CalLineCenterPoint(vedge);
                    newArc.TriangleList.Add(startTri);//加入起始三角形
                    newArc.PointList.Add(cp);//加入起点
                    newArc.PointList.Add(n);//加入第二点
                    this.SetMapObjectandDistforT0(ref newArc, cp, n, vedge);
                    nextTri = vedge.rightTriangle;
                }
                #endregion

                #endregion

                //循环遍历路径上所有的1类连通三角形，
                //直到遇到2类，0类或半封闭1类三角形为止,或循环链路为止
                #region 遍历中间三角形
                while (nextTri != null && nextTri.TriType == 1 && TriTranversed[nextTri.ID] != 1)
                {
                    TriTranversed[nextTri.ID] = 1;
                    newArc.TriangleList.Add(nextTri);
                    ovedge = nextTri.GetOtherVEdgeofT1(vedge.doulEdge);
                    if (vedge == null || ovedge == null)
                    {
                        int error = 0;
                    }
                    TriNode n1 = ComFunLib.CalLineCenterPoint(vedge);
                    TriNode n2 = ComFunLib.CalLineCenterPoint(ovedge);
                    newArc.PointList.Add(n2);

                    SetMapObjsandDistforT1(ref newArc, nextTri, n1, n2);
       
                    vedge = ovedge;
                    nextTri = vedge.rightTriangle;
                }
                #endregion

                #region 骨架线段的最后一个三角形

                #region 如果没有邻接三角形，结束遍历
                if (nextTri == null && newArc != null)
                {

                    newArc.AveDistance = newArc.AveDistance / newArc.TriangleList.Count;
                    newArc.WAD = newArc.WAD / newArc.Length;
                    this.Skeleton_ArcList.Add(newArc); ArcID++;

                    startTri = null;
                    nextTri = null;
                    newArc = null;
                }
                #endregion

                #region  如果是2类三角形
                else if (nextTri != null && nextTri.TriType == 2)
                {
                    TriEdge re = null;
                    TriEdge le = null;
                    TriEdge oe = null;
                    TriTranversed[nextTri.ID] = 1;
                    newArc.TriangleList.Add(nextTri);
                    TriNode startP = nextTri.GetStartPointofT2(out oe, out re, out le);
                    newArc.PointList.Add(startP);

                    if (newArc.LeftMapObj == null || newArc.RightMapObj == null)
                    {
                        int curtagID = -1;
                        FeatureType curType = FeatureType.Unknown;
                        curtagID = le.tagID;
                        curType = le.FeatureType;
                        newArc.LeftMapObj = Map.GetObjectbyID(curtagID, curType);

                        curtagID = re.tagID;
                        curType = re.FeatureType;
                        newArc.RightMapObj = Map.GetObjectbyID(curtagID, curType);

                        if (vedge == null)
                        {
                            int error = 0;
                        }

                        newArc.PointList.Add(ComFunLib.CalLineCenterPoint(vedge));
                    }

                    newArc.NearestEdge = new NearestEdge(ArcID, new NearestPoint(-1, startP.X, startP.Y), new NearestPoint(-1, startP.X, startP.Y), 0);
                    newArc.AveDistance += 0.5 * oe.Length;
                    newArc.AveDistance = newArc.AveDistance / newArc.TriangleList.Count;
                    TriNode p1 = newArc.PointList[newArc.PointList.Count - 2];
                    TriNode p2 = newArc.PointList[newArc.PointList.Count - 1];
                    curL = ComFunLib.CalLineLength(p1, p2);
                    //newArc.NearestEdge = new NearestEdge(ArcID, new NearestPoint(-1, startP.X, startP.Y), new NearestPoint(-1, startP.X, startP.Y), 0);
                   // newArc.AveDistance += 0.5 * vedge.Length;
                    newArc.WAD += 0.5 * oe.Length * curL;
                    newArc.Length += curL;
                    newArc.WAD = newArc.WAD / newArc.Length;
                    this.Skeleton_ArcList.Add(newArc); ArcID++;
                    //2014-2-26
                    nextTri.W = oe.Length;

                    startTri = null;
                    nextTri = null;
                    newArc = null;
                }
                #endregion

                #region 如果是0类三角形,进入二叉树模式
                else if (nextTri != null && nextTri.TriType == 0)
                {
                    //结束遍历
                    TriNode centerV = ComFunLib.CalCenter(nextTri);
                    newArc.TriangleList.Add(nextTri);
                    newArc.PointList.Add(centerV);
                    this.Skeleton_ArcList.Add(newArc);
     
                    SetMapObjectandDistforT0(ref  newArc, newArc.PointList[newArc.PointList.Count - 2], centerV, vedge);
                    if (TriTranversed[nextTri.ID] ==0)
                    {
                        TriTranversed[nextTri.ID]=1;

                        //加入两个分支的起点到栈中
                        TriEdge edge1 = null;
                        TriEdge edge2 = null;

                        nextTri.GetAnother2EdgeofT0(vedge, out edge1, out edge2);

                        StaTri.Push(nextTri);
                        StaEdge.Push(edge1);
                        StaCP.Push(centerV);

                        StaTri.Push(nextTri);
                        StaEdge.Push(edge2);
                        StaCP.Push(centerV);
                    }
                   // else if(TriTranversed[nextTri.ID] <3)
                  //  {

                    newArc.AveDistance = newArc.AveDistance / newArc.TriangleList.Count;
                    newArc.WAD = newArc.WAD / newArc.Length;
                    ArcID++;
                    startTri = null;
                    nextTri = null;
                    newArc = null;
                }
                #endregion
                #endregion
            }
            #region 如果还存在没有遍历的三角形则说明存在环路需要特别处理之
            int index = -1;
            while (!IsAllTranversed(TriTranversed, out index))
            {
                startTri = CDT.TriangleList[index];
                if (startTri.TriType == 1 && TriTranversed[index] == 0)
                {
                    TriTranversed[index] = 1;
                    newArc = new Skeleton_Arc(ArcID);
                    newArc.TriangleList.Add(startTri);//加入起始三角形

                    TriEdge e = null;

                    if (startTri.edge1.tagID == -1)
                    {
                        e = startTri.edge1;
                    }
                    else if (startTri.edge2.tagID == -1)
                    {
                        e = startTri.edge2;
                    }

                    else if (startTri.edge3.tagID == -1)
                    {
                        e = startTri.edge3;
                    }
                    vedge = startTri.GetOtherVEdgeofT1(e);

                    if (e == null || vedge == null)
                    {
                        int error = 0;
                    }

                    TriNode n1 = ComFunLib.CalLineCenterPoint(e);
                    TriNode n2 = ComFunLib.CalLineCenterPoint(vedge);
                    //计算并加入起点
                    newArc.PointList.Add(n1);
                    //计算并加入第二点
                    newArc.PointList.Add(n2);
                    SetMapObjsandDistforT1(ref newArc, startTri, n1, n2);
                    nextTri = vedge.rightTriangle;
                }

                //循环遍历路径上所有的1类连通三角形，
                //直到遇到2类，0类或半封闭1类三角形为止,或循环链路为止
                #region 遍历中间三角形
                while (nextTri != null && nextTri.TriType == 1 && TriTranversed[nextTri.ID] == 0)
                {
                    TriTranversed[nextTri.ID] = 1;
                    newArc.TriangleList.Add(nextTri);
                    ovedge = nextTri.GetOtherVEdgeofT1(vedge.doulEdge);

                    if (ovedge == null || vedge == null)
                    {
                        int error = 0;
                    }

                    TriNode n1 = ComFunLib.CalLineCenterPoint(vedge);
                    TriNode n2 = ComFunLib.CalLineCenterPoint(ovedge);
                    newArc.PointList.Add(n2);

                    SetMapObjsandDistforT1(ref newArc, nextTri, n1, n2);

                    vedge = ovedge;
                    nextTri = vedge.rightTriangle;
                }
                #endregion


                #region 如果是环路
                if (nextTri != null && nextTri.TriType == 1 && TriTranversed[nextTri.ID] == 1)
                {
                    TriNode endp = newArc.PointList[0];
                    newArc.PointList.Add(endp);//将起点作为终点添加到链表的最后
                    TriNode p1 = newArc.PointList[newArc.PointList.Count - 2];
                    TriNode p2 = newArc.PointList[newArc.PointList.Count - 1];
                    curL = ComFunLib.CalLineLength(p1, p2);
                    //newArc.NearestEdge = new NearestEdge(ArcID, new NearestPoint(-1, startP.X, startP.Y), new NearestPoint(-1, startP.X, startP.Y), 0);
                    // newArc.AveDistance += 0.5 * vedge.Length;
                    newArc.Length += curL;
                    newArc.WAD = newArc.WAD / newArc.Length;
                    newArc.AveDistance = newArc.AveDistance / newArc.TriangleList.Count;
                    this.Skeleton_ArcList.Add(newArc); ArcID++;
                    startTri = null;
                    nextTri = null;
                    newArc = null;
                }
                #endregion
            }
            #endregion
            //删除重复的Skeleton_Arc
            PostDeleteRepeatedArcs();
        }
        /// <summary>
        /// 删除有一边的对象为NULL的骨架线弧段
        /// </summary>
        private void PostDeleteNullObjectsArcs()
        {
            List<Skeleton_Arc> deleteArcs = new List<Skeleton_Arc>();
            foreach (Skeleton_Arc curArc in this.Skeleton_ArcList)
            {
                if (curArc.LeftMapObj == null || curArc.RightMapObj == null)
                {
                   
                        deleteArcs.Add(curArc);
                 
                }
            }

            foreach (Skeleton_Arc dArc in deleteArcs)
            {
                this.Skeleton_ArcList.Remove(dArc);
            }
        }
        /// <summary>
        /// 删除重复的Skeleton_Arc,由于骨架线遍历的算法的Bug
        /// 当起点与终点均为T0时，可能重复，需要剔除重复的弧段
        /// </summary>
        private void PostDeleteRepeatedArcs()
        {
            List<Skeleton_Arc> tempSkeletonArcList=new List<Skeleton_Arc>();
            foreach(Skeleton_Arc curArc in this.Skeleton_ArcList)
            {
                //记录以T0为起点，以T0为终点的弧段
                if(curArc.TriangleList.Count==2&&curArc.TriangleList[0].TriType==0&&curArc.TriangleList[1].TriType==0)
                {
                    tempSkeletonArcList.Add(curArc);
                }
            }
            List<Skeleton_Arc> tempSkeletonArcList1 = new List<Skeleton_Arc>();
            if(tempSkeletonArcList.Count==0)
                return;
            int n=tempSkeletonArcList.Count;
            //挑出所有重复的以T0为起点和终点的弧段
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    if (tempSkeletonArcList[i].TriangleList[0] == tempSkeletonArcList[j].TriangleList[1] && tempSkeletonArcList[i].TriangleList[1] == tempSkeletonArcList[j].TriangleList[0])
                    {
                        tempSkeletonArcList1.Add(tempSkeletonArcList[j]);
                    }
                }
            }
            foreach (Skeleton_Arc curArc in tempSkeletonArcList1)
            {
                this.Skeleton_ArcList.Remove(curArc);
            }

        }

        ///// <summary>
        ///// 设置0类三角形距离
        ///// </summary>
        ///// <param name="curArc"></param>
        ///// <param name="n1"></param>
        ///// <param name="n2"></param>
        ///// <param name="edge"></param>
        //private void SetDistanceforT0(ref Skeleton_Arc curArc, TriNode n1, TriNode n2, TriEdge edge)
        //{
        //    double minDis = edge.Length;
        //    string res = ComFunLib.funReturnRightOrLeft(n1, n2, edge.startPoint);
        //    if (curArc.NearestEdge.NearestDistance > minDis)
        //    {
        //        if (res == "LEFT")
        //        {
        //            curArc.NearestEdge = new NearestEdge(curArc.ID, new NearestPoint(-1, edge.startPoint.X, edge.startPoint.Y), new NearestPoint(-1, edge.endPoint.X, edge.endPoint.Y), minDis);
        //        }
        //        else
        //        {
        //            curArc.NearestEdge = new NearestEdge(curArc.ID, new NearestPoint(-1, edge.endPoint.X, edge.endPoint.Y), new NearestPoint(-1, edge.startPoint.X, edge.startPoint.Y), minDis);
        //        }
        //    }
        //    curArc.AveDistance += minDis;
        //}

        /// <summary>
        /// 设置0类三角形左右对象和距离
        /// </summary>
        /// <param name="curArc"></param>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <param name="edge"></param>
        private void SetMapObjectandDistforT0(ref Skeleton_Arc curArc, TriNode n1, TriNode n2, TriEdge edge)
        {
            double minDis = edge.Length;
            double curL = 0;

            TriNode p1 = curArc.PointList[curArc.PointList.Count - 2];
            TriNode p2 = curArc.PointList[curArc.PointList.Count - 1];
            curL = ComFunLib.CalLineLength(p1, p2);

            if (curArc.LeftMapObj == null || curArc.RightMapObj == null)
            {
                string res = ComFunLib.funReturnRightOrLeft(n1, n2, edge.startPoint);
                int curtagID = -1;
                FeatureType curType = FeatureType.Unknown;
                if (res == "LEFT")
                {
                    if (curArc.LeftMapObj == null)
                    {
                        curtagID = edge.startPoint.TagValue;
                        curType = edge.startPoint.FeatureType;
                        curArc.LeftMapObj = Map.GetObjectbyID(curtagID, curType);
                    }
                    if (curArc.RightMapObj == null)
                    {
                        curtagID = edge.endPoint.TagValue;
                        curType = edge.endPoint.FeatureType;
                        curArc.RightMapObj = Map.GetObjectbyID(curtagID, curType);
                    }
                    if (curArc.NearestEdge.NearestDistance > minDis)
                    {
      
                        curArc.NearestEdge = new NearestEdge(curArc.ID,
                          new NearestPoint(edge.startPoint.TagValue, edge.startPoint.X, edge.startPoint.Y),
                          new NearestPoint(edge.endPoint.TagValue, edge.endPoint.X, edge.endPoint.Y),
                          minDis);
                    }
                }
                else
                {
                    if (curArc.RightMapObj == null)
                    {
                        curtagID = edge.startPoint.TagValue;
                        curType = edge.startPoint.FeatureType;
                        curArc.RightMapObj = Map.GetObjectbyID(curtagID, curType);
                    }
                    if (curArc.LeftMapObj == null)
                    {
                        curtagID = edge.endPoint.TagValue;
                        curType = edge.endPoint.FeatureType;
                        curArc.LeftMapObj = Map.GetObjectbyID(curtagID, curType);
                    }
                    if (curArc.NearestEdge.NearestDistance > minDis)
                    {
                        curArc.NearestEdge = new NearestEdge(curArc.ID,
             new NearestPoint(edge.endPoint.TagValue, edge.endPoint.X, edge.endPoint.Y),
             new NearestPoint(edge.startPoint.TagValue, edge.startPoint.X, edge.startPoint.Y),
               minDis);
                    }

                }
               
            }
            else
            {
                string res = ComFunLib.funReturnRightOrLeft(n1, n2, edge.startPoint);
                if (curArc.NearestEdge.NearestDistance > minDis)
                {
                    if (res == "LEFT")
                    {
                        curArc.NearestEdge = new NearestEdge(curArc.ID,
                            new NearestPoint(edge.endPoint.TagValue, edge.endPoint.X, edge.endPoint.Y),
                            new NearestPoint(edge.startPoint.TagValue, edge.startPoint.X, edge.startPoint.Y),
                            minDis);
                    }
                    else
                    {
                        curArc.NearestEdge = new NearestEdge(curArc.ID, 
                            new NearestPoint(edge.startPoint.TagValue, edge.startPoint.X, edge.startPoint.Y),
                            new NearestPoint(edge.endPoint.TagValue, edge.endPoint.X, edge.endPoint.Y), 
                            minDis);
                    }
                }
            }
            curArc.AveDistance += minDis;
            curArc.WAD += minDis * curL;
            curArc.Length += curL;
        }

        ///// <summary>
        ///// 设置1类三角形的左右对象
        ///// </summary>
        ///// <param name="curArc">骨架线弧段</param>
        ///// <param name="tri">三角形</param>
        ///// <param name="n1">第一点</param>
        ///// <param name="n2">第二点</param>
        //private void SetDistanceforT1(ref Skeleton_Arc curArc, Triangle tri, TriNode n1, TriNode n2)
        //{
        //    TriEdge vE1 = null;
        //    TriEdge vE2 = null;
        //    TriEdge rE = null;
        //    TriNode vVex = null;

        //    rE = tri.GetVEdgeofT1(out vE1, out vE2, out vVex);
        //    NearestPoint nearestPoint = null;
        //    double minDis = tri.CalMinDisforT1(vVex, rE.startPoint, rE.endPoint, out nearestPoint);

        //    if (curArc.NearestEdge.NearestDistance > minDis)
        //    {
        //        string res = ComFunLib.funReturnRightOrLeft(n1, n2, vVex);
        //        if (res == "LEFT")
        //            curArc.NearestEdge = new NearestEdge(curArc.ID, new NearestPoint(-1, vVex.X, vVex.Y), nearestPoint, minDis);
        //        else

        //            curArc.NearestEdge = new NearestEdge(curArc.ID, nearestPoint, new NearestPoint(-1, vVex.X, vVex.Y), minDis);
        //    }
        //    curArc.AveDistance += minDis;
        //}
        /// <summary>
        /// 设置1类三角形的左右对象
        /// </summary>
        /// <param name="curArc"></param>
        /// <param name="tri"></param>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        private void SetMapObjsandDistforT1(ref Skeleton_Arc curArc, Triangle tri, TriNode n1, TriNode n2)
        {
            TriEdge vE1 = null;
            TriEdge vE2 = null;
            TriEdge rE = null;
            TriNode vVex = null;
            double curL = 0;

            TriNode p1 = curArc.PointList[curArc.PointList.Count - 2];
            TriNode p2 = curArc.PointList[curArc.PointList.Count - 1];
            curL = ComFunLib.CalLineLength(p1, p2);
                            
            rE = tri.GetVEdgeofT1(out vE1, out vE2, out vVex);
            NearestPoint nearestPoint = null;
            double minDis = tri.CalMinDisforT1(vVex, rE.startPoint, rE.endPoint, out nearestPoint);
            //2014-2-26
            tri.W = minDis;
            if (curArc.LeftMapObj == null || curArc.RightMapObj == null)
            {
             
                string res = ComFunLib.funReturnRightOrLeft(n1, n2, vVex);
                int curtagID = -1;
                FeatureType curType = FeatureType.Unknown;
                MapObject mo = null;
                if (res == "LEFT")
                {
                    curtagID = vVex.TagValue;
                    curType = vVex.FeatureType;

                    mo = Map.GetObjectbyID(curtagID, curType);
                    if (mo != null)
                        curArc.LeftMapObj = mo;

                    curtagID = rE.tagID;
                    curType = rE.FeatureType;
                    mo = Map.GetObjectbyID(curtagID, curType);
                    if (mo != null)
                        curArc.RightMapObj = mo;
                    if (curArc.NearestEdge.NearestDistance > minDis)
                    {
                        nearestPoint.ID = rE.tagID;
                        curArc.NearestEdge = new NearestEdge(curArc.ID, new NearestPoint(vVex.TagValue, vVex.X, vVex.Y), nearestPoint, minDis);
                        //curArc.NearestEdge = new NearestEdge(curArc.ID,
                        //    nearestPoint,
                        //    new NearestPoint(vVex.TagValue, vVex.X, vVex.Y), 
                        //    minDis);
                    }

                }
                else
                {
                    curtagID = vVex.TagValue;
                    curType = vVex.FeatureType;

                    mo = Map.GetObjectbyID(curtagID, curType);
                    if (mo != null)
                        curArc.RightMapObj = mo;

                    curtagID = rE.tagID;
                    curType = rE.FeatureType;
                    mo = Map.GetObjectbyID(curtagID, curType);
                    if (mo != null)
                        curArc.LeftMapObj = mo;

                    if (curArc.NearestEdge.NearestDistance > minDis)
                    {
                        nearestPoint.ID = rE.tagID;
                       curArc.NearestEdge = new NearestEdge(curArc.ID, nearestPoint, new NearestPoint(vVex.TagValue, vVex.X, vVex.Y), minDis);
         
                    }

                }
               
            }
            else
            {
                if (curArc.NearestEdge.NearestDistance > minDis)
                {
                    string res = ComFunLib.funReturnRightOrLeft(n1, n2, vVex);
                    if (res == "LEFT")
                       curArc.NearestEdge = new NearestEdge(curArc.ID, new NearestPoint(vVex.TagValue, vVex.X, vVex.Y), nearestPoint, minDis);
                        //curArc.NearestEdge = new NearestEdge(curArc.ID, 
                        //    nearestPoint,
                        //    new NearestPoint(vVex.TagValue, vVex.X, vVex.Y), 
                        //    minDis);
                    else

                       curArc.NearestEdge = new NearestEdge(curArc.ID, nearestPoint, new NearestPoint(vVex.TagValue, vVex.X, vVex.Y), minDis);
         //               curArc.NearestEdge = new NearestEdge(curArc.ID,
         //new NearestPoint(vVex.TagValue, vVex.X, vVex.Y),
         //nearestPoint,
         //minDis);
                }
           
            }
            curArc.AveDistance += minDis;
            curArc.WAD += minDis * curL;
            curArc.Length += curL;
        }

        /// <summary>
        /// 计算所有的间隔面积
        /// </summary>
        public void CalGapArea()
        {
            if (this.Skeleton_ArcList == null || this.Skeleton_ArcList.Count == 0)
                return;
            foreach (Skeleton_Arc curarc in this.Skeleton_ArcList)
            {
                curarc.CalGapArea();
            }
        }

        /// <summary>
        /// 计算所有的间隔面积
        /// </summary>
        public void CalDVD()
        {
            if (this.Skeleton_ArcList == null || this.Skeleton_ArcList.Count == 0)
                return;
            foreach (Skeleton_Arc curarc in this.Skeleton_ArcList)
            {
                curarc.CalDVD();
            }
        }

        /// <summary>
        /// 构建Pseu_VoronoiPolygon
        /// </summary>
        public void CreatePseu_VoronoiPolygon()
        {
            //  Pseu_VoronoiPolygonList = new List<Pseu_VoronoiPolygon>();
            //通过多边形生成算法实现Pseu_VoronoiPolygon
            // ...........\
        }

        /// <summary>
        /// 将骨架线写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="Skeleton_SegmentList">骨架线段列表</param>
        public  void Create_WriteSkeleton_Segment2Shp(string filePath, string fileName, esriSRProjCS4Type prj)
        {
            #region 创建一个线的shape文件
            string Folderpathstr = filePath;
            string LyrName = fileName;
            string strFolder = Folderpathstr;
            string strName = LyrName;
            string strShapeFieldName = "Shape";

            IFeatureWorkspace pFWS;//ESRI.ArcGIS.Geodatabase;
            IWorkspaceFactory pWorkspaceFactory = new ESRI.ArcGIS.DataSourcesFile.ShapefileWorkspaceFactoryClass();//ESRI.ArcGIS.DataSourcesFile
            pFWS = pWorkspaceFactory.OpenFromFile(strFolder, 0) as IFeatureWorkspace;

            //创建一个字段集
            IFields pFields = new ESRI.ArcGIS.Geodatabase.FieldsClass();
            IFieldsEdit pFieldsEdit;
            pFieldsEdit = pFields as IFieldsEdit;

            #region 创建图形字段
            IField pField;
            IFieldEdit pFieldEdit;
            //创建图形字段
            pField = new FieldClass();
            pFieldEdit = pField as IFieldEdit;
            pFieldEdit.Name_2 = strShapeFieldName;
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;

            ISpatialReferenceFactory ispfac = new SpatialReferenceEnvironmentClass();
            IProjectedCoordinateSystem iprcoorsys = ispfac.CreateProjectedCoordinateSystem((int)prj);
            ISpatialReference pSpatialReference = iprcoorsys as ISpatialReference;

            IGeometryDef pGeomDef = new GeometryDefClass();
            IGeometryDefEdit pGeomDefEdit = pGeomDef as IGeometryDefEdit;
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolyline;
            pGeomDefEdit.SpatialReference_2 = pSpatialReference;
            pFieldEdit.GeometryDef_2 = pGeomDef;
            pFieldsEdit.AddField(pField);
            #endregion

            #region 创建属性字段
            //属性字段1
            IField pField1;
            IFieldEdit pFieldEdit1;
            pField1 = new FieldClass();
            pFieldEdit1 = pField1 as IFieldEdit;
            pFieldEdit1.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit1.Name_2 = "ID";
            pFieldEdit1.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField1);

            //属性字段2
            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit2.Name_2 = "LeftOID";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField2);

            //属性字段3
            IField pField3;
            IFieldEdit pFieldEdit3;
            pField3 = new FieldClass();
            pFieldEdit3 = pField3 as IFieldEdit;
            pFieldEdit3.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit3.Name_2 = "LeftOType";
            pFieldEdit3.Type_2 = esriFieldType.esriFieldTypeString;
            pFieldsEdit.AddField(pField3);

            //属性字段4
            IField pField4;
            IFieldEdit pFieldEdit4;
            pField4 = new FieldClass();
            pFieldEdit4 = pField4 as IFieldEdit;
            pFieldEdit4.Length_2 = 30;
            pFieldEdit4.Name_2 = "RightOID";
            pFieldEdit4.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField4);

            //属性字段5
            IField pField5;
            IFieldEdit pFieldEdit5;
            pField5 = new FieldClass();
            pFieldEdit5 = pField5 as IFieldEdit;
            pFieldEdit5.Length_2 = 30;
            pFieldEdit5.Name_2 = "RightOType";
            pFieldEdit5.Type_2 = esriFieldType.esriFieldTypeString;
            pFieldsEdit.AddField(pField5);


            //属性字段6
            IField pField6;
            IFieldEdit pFieldEdit6;
            pField6 = new FieldClass();
            pFieldEdit6 = pField6 as IFieldEdit;
            pFieldEdit6.Length_2 = 30;
            pFieldEdit6.Name_2 = "FrontOID";
            pFieldEdit6.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField6);

            //属性字段7
            IField pField7;
            IFieldEdit pFieldEdit7;
            pField7 = new FieldClass();
            pFieldEdit7 = pField7 as IFieldEdit;
            pFieldEdit7.Length_2 = 30;
            pFieldEdit7.Name_2 = "FrontOType";
            pFieldEdit7.Type_2 = esriFieldType.esriFieldTypeString;
            pFieldsEdit.AddField(pField7);

            //属性字段8
            IField pField8;
            IFieldEdit pFieldEdit8;
            pField8 = new FieldClass();
            pFieldEdit8 = pField8 as IFieldEdit;
            pFieldEdit8.Length_2 = 30;
            pFieldEdit8.Name_2 = "BackOID";
            pFieldEdit8.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField8);

            //属性字段9
            IField pField9;
            IFieldEdit pFieldEdit9;
            pField9 = new FieldClass();
            pFieldEdit9 = pField9 as IFieldEdit;
            pFieldEdit9.Length_2 = 30;
            pFieldEdit9.Name_2 = "BackOType";
            pFieldEdit9.Type_2 = esriFieldType.esriFieldTypeString;
            pFieldsEdit.AddField(pField9);

            //属性字段10
            IField pField10;
            IFieldEdit pFieldEdit10;
            pField10 = new FieldClass();
            pFieldEdit10 = pField10 as IFieldEdit;
            pFieldEdit10.Length_2 = 30;
            pFieldEdit10.Name_2 = "MinDis";
            pFieldEdit10.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField10);

            //属性字段11
            IField pField11;
            IFieldEdit pFieldEdit11;
            pField11 = new FieldClass();
            pFieldEdit11 = pField11 as IFieldEdit;
            pFieldEdit11.Length_2 = 30;
            pFieldEdit11.Name_2 = "AveDis";
            pFieldEdit11.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField11);

            //属性字段12
            IField pField12;
            IFieldEdit pFieldEdit12;
            pField12 = new FieldClass();
            pFieldEdit12 = pField12 as IFieldEdit;
            pFieldEdit12.Length_2 = 30;
            pFieldEdit12.Name_2 = "WAveDis";
            pFieldEdit12.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField12);

            //属性字段13
            IField pField13;
            IFieldEdit pFieldEdit13;
            pField13 = new FieldClass();
            pFieldEdit13 = pField13 as IFieldEdit;
            pFieldEdit13.Length_2 = 30;
            pFieldEdit13.Name_2 = "Length";
            pFieldEdit13.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField13);

            //属性字段14
            IField pField14;
            IFieldEdit pFieldEdit14;
            pField14 = new FieldClass();
            pFieldEdit14 = pField14 as IFieldEdit;
            pFieldEdit14.Length_2 = 30;
            pFieldEdit14.Name_2 = "GapArea";
            pFieldEdit14.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField14);

            //属性字段15
            IField pField15;
            IFieldEdit pFieldEdit15;
            pField15 = new FieldClass();
            pFieldEdit15 = pField15 as IFieldEdit;
            pFieldEdit15.Length_2 = 30;
            pFieldEdit15.Name_2 = "DVD-N";
            pFieldEdit15.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField15);

            //属性字段16
            IField pField16;
            IFieldEdit pFieldEdit16;
            pField16 = new FieldClass();
            pFieldEdit16 = pField16 as IFieldEdit;
            pFieldEdit16.Length_2 = 30;
            pFieldEdit16.Name_2 = "DVD-NE";
            pFieldEdit16.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField16);

            //属性字段17
            IField pField17;
            IFieldEdit pFieldEdit17;
            pField17 = new FieldClass();
            pFieldEdit17 = pField17 as IFieldEdit;
            pFieldEdit17.Length_2 = 30;
            pFieldEdit17.Name_2 = "DVD-E";
            pFieldEdit17.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField17);

            //属性字段18
            IField pField18;
            IFieldEdit pFieldEdit18;
            pField18 = new FieldClass();
            pFieldEdit18 = pField18 as IFieldEdit;
            pFieldEdit18.Length_2 = 30;
            pFieldEdit18.Name_2 = "DVD-NW";
            pFieldEdit18.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField18);

            //属性字段14
            IField pField19;
            IFieldEdit pFieldEdit19;
            pField19 = new FieldClass();
            pFieldEdit19 = pField19 as IFieldEdit;
            pFieldEdit19.Length_2 = 30;
            pFieldEdit19.Name_2 = "DVD-W";
            pFieldEdit19.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField19);

            //属性字段14
            IField pField20;
            IFieldEdit pFieldEdit20;
            pField20 = new FieldClass();
            pFieldEdit20 = pField20 as IFieldEdit;
            pFieldEdit20.Length_2 = 30;
            pFieldEdit20.Name_2 = "DVD-S";
            pFieldEdit20.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField20);

            //属性字段14
            IField pField21;
            IFieldEdit pFieldEdit21;
            pField21 = new FieldClass();
            pFieldEdit21 = pField21 as IFieldEdit;
            pFieldEdit21.Length_2 = 30;
            pFieldEdit21.Name_2 = "DVD-SE";
            pFieldEdit21.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField21);

            //属性字段14
            IField pField22;
            IFieldEdit pFieldEdit22;
            pField22 = new FieldClass();
            pFieldEdit22 = pField22 as IFieldEdit;
            pFieldEdit22.Length_2 = 30;
            pFieldEdit22.Name_2 = "DVD-SW";
            pFieldEdit22.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField22);

            #endregion

            #region 创建要素类
            IFeatureClass pFeatClass;
            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
            #endregion
            #endregion

            #region 向线层添加线要素

            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            IWorkspaceEdit pIWorkspaceEdit = null;
            IDataset pIDataset = (IDataset)pFeatClass;

            if (pIDataset != null)
            {
                pIWorkspaceEdit = (IWorkspaceEdit)pIDataset.Workspace;
            }
            try
            {
                if (pFeatClass == null)
                    return;
                //获取顶点图层的数据集，并创建工作空间
                IDataset dataset = (IDataset)pFeatClass;
                IWorkspace workspace = dataset.Workspace;
                IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
                //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
                IFeatureClassWrite fr = (IFeatureClassWrite)pFeatClass;
                //注意：此时，所编辑数据不能被其他程序打开
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();

                int n = this.Skeleton_ArcList.Count;
                List<Skeleton_Arc> Skeleton_SegmentList = this.Skeleton_ArcList;
                if (n == 0)
                    return;

                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PolylineClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = null;
                    TriNode curPoint = null;
                    if (Skeleton_SegmentList[i] == null)
                        continue;
                    int m = Skeleton_SegmentList[i].PointList.Count;

                    for (int k = 0; k < m; k++)
                    {
                        curPoint = Skeleton_SegmentList[i].PointList[k];
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    }
                    feature.Shape = shp;

                    feature.set_Value(2, Skeleton_SegmentList[i].ID);

                    int id = -1;
                    string type = "";
                    if (Skeleton_SegmentList[i].LeftMapObj != null)
                    {
                        id = Skeleton_SegmentList[i].LeftMapObj.ID;
                        type = Skeleton_SegmentList[i].LeftMapObj.GetType().ToString();
                    }
                    else
                    {
                        id = -1; type = "";
                    }
                    feature.set_Value(3, id);
                    feature.set_Value(4, type);
                    if (Skeleton_SegmentList[i].RightMapObj != null)
                    {
                        id = Skeleton_SegmentList[i].RightMapObj.ID;
                        type = Skeleton_SegmentList[i].RightMapObj.GetType().ToString();
                    }
                    else
                    {
                        id = -1; type = "";
                    }
                    feature.set_Value(5, id);
                    feature.set_Value(6, type);
                    if (Skeleton_SegmentList[i].FrontMapObj != null)
                    {
                        id = Skeleton_SegmentList[i].FrontMapObj.ID;
                        type = Skeleton_SegmentList[i].FrontMapObj.GetType().ToString();
                    }
                    else
                    {
                        id = -1; type = "";
                    }
                    feature.set_Value(7, id);
                    feature.set_Value(8, type);
                    if (Skeleton_SegmentList[i].BackMapObj != null)
                    {
                        id = Skeleton_SegmentList[i].BackMapObj.ID;
                        type = Skeleton_SegmentList[i].BackMapObj.GetType().ToString();
                    }
                    else
                    {
                        id = -1;
                        type = "";
                    }
                    feature.set_Value(9, id);
                    feature.set_Value(10, type);
                    feature.set_Value(11, Skeleton_SegmentList[i].NearestEdge.NearestDistance);
                    feature.set_Value(12, Skeleton_SegmentList[i].AveDistance);
                    feature.set_Value(13, Skeleton_SegmentList[i].WAD);
                    feature.set_Value(14, Skeleton_SegmentList[i].Length);
                    feature.set_Value(15, Skeleton_SegmentList[i].GapArea);

                    feature.set_Value(16, Skeleton_SegmentList[i].DVD[0]);
                    feature.set_Value(17, Skeleton_SegmentList[i].DVD[1]);
                    feature.set_Value(18, Skeleton_SegmentList[i].DVD[2]);
                    feature.set_Value(19, Skeleton_SegmentList[i].DVD[3]);
                    feature.set_Value(20, Skeleton_SegmentList[i].DVD[4]);
                    feature.set_Value(21, Skeleton_SegmentList[i].DVD[5]);
                    feature.set_Value(22, Skeleton_SegmentList[i].DVD[6]);
                    feature.set_Value(23, Skeleton_SegmentList[i].DVD[7]);

                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }

                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                // MessageBox.Show("异常信息" + ex.Message);
            }
            #endregion
        }

    }
}
