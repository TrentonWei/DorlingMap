using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;

namespace ConflictDetection
{
    /// <summary>
    /// 骨架线段：
    /// </summary>
    public class Skeleton_Segment
    {
        //邻近的两条道路的ID号
        public int LeftRoadSegID = -1;
        public int RightRoadSegID = -1;
        public int FrontRoadSegID = -1;  //？可能并不需要
        public int BackRoadSegID = -1;   //? 可能并不需要
        public List<Triangle> TriangleList = null; //三角形路径
        public float Threshold;                     //距离阈值
        // public List<Conflict> ConflictList = null;//冲突列表，一段骨架线上可能有若干段冲突
        public List<TriNode> Axis_Points = null;//轴线上的点

        public Skeleton_Segment()
        {
            TriangleList = new List<Triangle>();
            //ConflictList = new List<Conflict>();
            Axis_Points = new List<TriNode>();
        }

    }

    /// <summary>
    /// 骨架线
    /// </summary>
    public class Skeleton
    {
        //所有骨架线段
        public List<Skeleton_Segment> Skeleton_SegmentList = null;
        public List<Skeleton_Segment> ProcessedSkeleton_SegmentList = null;
        //所有伪Voronoi图
        //public List<Pseu_VoronoiPolygon> Pseu_VoronoiPolygonList = null;
        //约束三角网
        public ConsDelaunayTin CDT = null;
        public List<ConNode> CNList = null;

        public List<PolylineObject> PLList = null;
        public List<PolygonObject> PPList = null;


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="cdt"></param>
        public Skeleton(ConsDelaunayTin cdt, List<ConNode> cNList)
        {
            this.CDT = cdt;
            CNList = cNList;
        }

        /// <summary>
        /// 是否单通道1类三角形
        /// </summary>
        /// <param name="tri"></param>
        /// <param name="isWise"></param>
        /// <returns></returns>
        private bool IsSinglePathT1(Triangle tri)
        {
            int count = 0;
            Triangle nextTri = null;
            if (tri.edge1.tagID == -1)
            {
                nextTri = tri.edge1.rightTriangle;
                if (nextTri != null)
                    count++;
            }
            if (tri.edge2.tagID == -1)
            {

                nextTri = tri.edge2.rightTriangle;
                if (nextTri != null)
                    count++;
            }

            if (tri.edge3.tagID == -1)
            {
                nextTri = tri.edge3.rightTriangle;
                if (nextTri != null)
                    count++;
            }
            if (count == 1)
                return true;
            else
                return false;
        }

        /// <summary>
        /// 是否单通道1类三角形
        /// </summary>
        /// <param name="tri"></param>
        /// <param name="isWise"></param>
        /// <returns></returns>
        private bool IsNoPathT1(Triangle tri)
        {
            int count = 0;
            Triangle nextTri = null;
            if (tri.edge1.tagID == -1)
            {
                nextTri = tri.edge1.rightTriangle;
                if (nextTri != null)
                    count++;
            }
            if (tri.edge2.tagID == -1)
            {

                nextTri = tri.edge2.rightTriangle;
                if (nextTri != null)
                    count++;
            }

            if (tri.edge3.tagID == -1)
            {
                nextTri = tri.edge3.rightTriangle;
                if (nextTri != null)
                    count++;
            }
            if (count == 0)
                return true;
            else
                return false;
        }
        /// <summary>
        /// 获取其他的另外两条边
        /// </summary>
        /// <param name="tri">三角形</param>
        /// <param name="ve">边</param>
        /// <param name="edge1">1</param>
        /// <param name="edge2">2</param>
        private void GetAnother2EdgeofT0(Triangle tri, TriEdge ve, out TriEdge edge1, out TriEdge edge2)
        {
            edge1 = null;
            edge2 = null;
            if (ve.doulEdge != null)
            {

                if (tri.edge1.ID == ve.doulEdge.ID)
                {
                    edge1 = tri.edge2;
                    edge2 = tri.edge3;
                }
                else if (tri.edge2.ID == ve.doulEdge.ID)
                {
                    edge1 = tri.edge1;
                    edge2 = tri.edge3;
                }

                else if (tri.edge3.ID == ve.doulEdge.ID)
                {
                    edge1 = tri.edge1;
                    edge2 = tri.edge2;
                }
            }
        }
        /// <summary>
        /// 获取1类三角形的两条虚边和一条实际边
        /// </summary>
        /// <param name="tri">三角形</param>
        /// <param name="vEdge1">第一条虚边</param>
        /// <param name="vEdge2">第二条虚边</param>
        ///  <param name="vVex">实边相对的顶点</param>
        private TriEdge GetVEdgeofT1(Triangle tri, out TriEdge vEdge1, out TriEdge vEdge2, out TriNode vVex)
        {
            TriEdge re = null;
            vEdge1 = null;
            vEdge2 = null;
            vVex = null;
            if (tri.edge1.tagID != -1)
            {
                vEdge1 = tri.edge2;
                vEdge2 = tri.edge3;
                re = tri.edge1;

            }
            else if (tri.edge2.tagID != -1)
            {
                vEdge1 = tri.edge3;
                vEdge2 = tri.edge1;
                re = tri.edge2;

            }
            else if (tri.edge3.tagID != -1)
            {
                vEdge1 = tri.edge1;
                vEdge2 = tri.edge2;
                re = tri.edge3;
            }
            //实边相对的顶点
            if (tri.point1.ID != re.startPoint.ID && tri.point1.ID != re.endPoint.ID)
            {
                vVex = tri.point1;
            }
            else if (tri.point2.ID != re.startPoint.ID && tri.point2.ID != re.endPoint.ID)
            {
                vVex = tri.point2;
            }
            else if (tri.point3.ID != re.startPoint.ID && tri.point3.ID != re.endPoint.ID)
            {
                vVex = tri.point3;
            }
            return re;
        }

        /// <summary>
        /// 获取2类三角形的起点和虚边
        /// </summary>
        /// <param name="tri"></param>
        /// <returns></returns>
        private TriNode GetStartPointofT2(Triangle tri, out TriEdge vEdge)
        {
            TriEdge redge1 = null;
            TriEdge redge2 = null;
            vEdge = null;

            if (tri.edge1.tagID == -1)
            {
                redge1 = tri.edge2;
                redge2 = tri.edge3;
                vEdge = tri.edge1;

            }
            else if (tri.edge2.tagID == -1)
            {
                redge1 = tri.edge1;
                redge2 = tri.edge3;
                vEdge = tri.edge2;
            }
            else if (tri.edge3.tagID == -1)
            {
                redge1 = tri.edge1;
                redge2 = tri.edge2;
                vEdge = tri.edge3;
            }

            if ((redge1.startPoint == tri.point1 || redge1.endPoint == tri.point1)
                && (redge2.startPoint == tri.point1 || redge2.endPoint == tri.point1))
                return tri.point1;
            else if ((redge1.startPoint == tri.point2 || redge1.endPoint == tri.point2)
                && (redge2.startPoint == tri.point2 || redge2.endPoint == tri.point2))
                return tri.point2;
            else if ((redge1.startPoint == tri.point3 || redge1.endPoint == tri.point3)
                && (redge2.startPoint == tri.point3 || redge2.endPoint == tri.point3))
                return tri.point3;
            return null;
        }

        /// <summary>
        /// 获取1类三角形的起点
        /// </summary>
        /// <param name="tri"></param>
        /// <returns></returns>
        private TriEdge GetStartEdgeofT1(Triangle tri)
        {
            Triangle nextTri = null;
            if (tri.edge1.tagID == -1)
            {
                nextTri = tri.edge1.rightTriangle;
                if (nextTri == null)
                    return tri.edge1;
            }

            if (tri.edge2.tagID == -1)
            {
                nextTri = tri.edge2.rightTriangle;
                if (nextTri == null)
                    return tri.edge2;
            }

            if (tri.edge3.tagID == -1)
            {
                nextTri = tri.edge3.rightTriangle;
                if (nextTri == null)
                    return tri.edge3;
            }
            return null;
        }

        /// <summary>
        /// 求三角形的重心
        /// </summary>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <param name="pt3"></param>
        /// <returns></returns>
        private TriNode CalTriCenterPoint(TriNode pt1, TriNode pt2, TriNode pt3)
        {
            double x = pt1.X + pt2.X + pt3.X;
            x /= 3;
            double y = pt1.Y + pt2.Y + pt3.Y;
            y /= 3;
            return new TriNode((float)x, (float)y);
        }

        /// <summary>
        /// 求线段的中点
        /// </summary>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <returns></returns>
        private TriNode CalLineCenterPoint(TriNode pt1, TriNode pt2)
        {
            double x = pt1.X + pt2.X;
            x /= 2;
            double y = pt1.Y + pt2.Y;
            y /= 2;
            return new TriNode((float)x, (float)y);
        }

        /// <summary>
        /// 获取一类三角形中另外一条虚边
        /// </summary>
        /// <param name="tri"></param>
        /// <param name="isWise"></param>
        /// <returns></returns>
        private TriEdge GetOtherVEdgeofT1(Triangle tri, TriEdge vedge)
        {
            //=========调试出现空错误9-6
            if (vedge == null)
                return null;
            TriEdge ovedge = null; //虚拟边
            if (tri.edge1.tagID == -1 && tri.edge1.ID != vedge.ID)
            {
                ovedge = tri.edge1;
            }
            else if (tri.edge2.tagID == -1 && tri.edge2.ID != vedge.ID)
            {
                ovedge = tri.edge2;
            }
            else if (tri.edge3.tagID == -1 && tri.edge3.ID != vedge.ID)
            {
                ovedge = tri.edge3;
            }
            return ovedge;
        }

        /// <summary>
        /// 删除多边形内部三角形
        /// </summary>
        /// <param name="isDAobu">是否删除凹部</param>
        private void PostProcessCDTforPLP(bool isDAobu)
        {
            List<int> indexList = new List<int>();
            for (int i=0;i< CDT.TriangleList.Count;i++)
            {
                Triangle curTri = null;
                curTri = CDT.TriangleList[i];
                int tagID = curTri.point1.TagValue;
                if (tagID == curTri.point2.TagValue && tagID == curTri.point3.TagValue)
                {
                    if (curTri.point1.FeatureType != FeatureType.PolygonType)
                    {
                        continue;
                    }

                    if (isDAobu == true)
                    {
                        indexList.Add(curTri.ID);
                    }
                    else
                    {
                        PolygonObject curPolygon = null;
                        foreach (PolygonObject polygon in PPList)
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
                                indexList.Add(curTri.ID);
                            }
                        }

                    }
                }
            }

            foreach(int index in indexList)
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
            //重写ID
            Triangle.WriteID( CDT.TriangleList);
        }
        /// <summary>
        /// 后处理，去除端点出的右边通道
        /// </summary>
        private void PostProcessCDTforRNT()
        {
            if (CNList == null || CNList.Count == 0)
                return;
            //先处理1类三角形中，两个端点的虚边
            foreach (Triangle curTri in CDT.TriangleList)
            {
   
                if (curTri.TriType == 1)
                {
                    if (curTri.ID == 115)
                    {
                        int error = 0;
                    }
                    TriEdge vEdge1 = null;
                    TriEdge vEdge2 = null;
                    TriNode vV = null;
                    TriEdge re = GetVEdgeofT1(curTri, out vEdge1, out vEdge2, out vV);
                    //端点在实边上
                    ConNode cNode1 = ConNode.GetPLbyID(this.CNList, re.startPoint.ID);
                    ConNode cNode2 = ConNode.GetPLbyID(this.CNList, re.endPoint.ID);
                    ConNode cNode3 = ConNode.GetPLbyID(this.CNList, vV.ID);

                    if (cNode1 == null && cNode2 == null && cNode3 == null)
                    {
                        continue;
                    }
                    else if (cNode1 != null && cNode2 != null && cNode3 != null)
                    {
                        if (vEdge1.rightTriangle != null&&vEdge1.rightTriangle.TriType==1)
                        {
                            vEdge1.rightTriangle = null;
                            TriEdge dEdge = vEdge1.doulEdge;
                            vEdge1.doulEdge = null;
                            dEdge.rightTriangle = null;
                            dEdge.doulEdge = null;
                        }
                        if (vEdge2.rightTriangle != null && vEdge2.rightTriangle.TriType == 1)
                        {

                            vEdge2.rightTriangle = null;
                            TriEdge dEdge1 = vEdge2.doulEdge;
                            vEdge2.doulEdge = null;
                            dEdge1.rightTriangle = null;
                            dEdge1.doulEdge = null;
                        }
                    }
                    else if (cNode1 != null && cNode2 == null && cNode3 != null)
                    {
                        if (vEdge1.startPoint.ID == cNode1.ID || vEdge1.endPoint.ID == cNode1.ID)
                        {
                            if (vEdge1.rightTriangle != null && vEdge1.rightTriangle.TriType == 1)
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
                            if (vEdge2.rightTriangle != null && vEdge2.rightTriangle.TriType == 1)
                            {
                                vEdge2.rightTriangle = null;
                                TriEdge dEdge1 = vEdge2.doulEdge;
                                vEdge2.doulEdge = null;
                                dEdge1.rightTriangle = null;
                                dEdge1.doulEdge = null;
                            }
                        }
                    }

                    else if (cNode1 == null && cNode2 != null && cNode3 != null)
                    {
                        if (vEdge1.startPoint.ID == cNode2.ID || vEdge1.endPoint.ID == cNode2.ID)
                        {
                            if (vEdge1.rightTriangle != null && vEdge1.rightTriangle.TriType == 1)
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
                            if (vEdge2.rightTriangle != null && vEdge1.rightTriangle.TriType == 1)
                            {
                                vEdge2.rightTriangle = null;
                                TriEdge dEdge1 = vEdge2.doulEdge;
                                vEdge2.doulEdge = null;
                                dEdge1.rightTriangle = null;
                                dEdge1.doulEdge = null;
                            }
                        }
                    }
                }
            }


            foreach (Triangle curTri in CDT.TriangleList)
            {

                if (curTri.TriType == 1)
                {

                    if (curTri.ID == 115)
                    {
                        int error = 0;
                    }

                    TriEdge vEdge1 = null;
                    TriEdge vEdge2 = null;
                    TriNode vV = null;
                    TriEdge re = GetVEdgeofT1(curTri, out vEdge1, out vEdge2, out vV);
                    //端点在实边上
                    ConNode cNode1 = ConNode.GetPLbyID(this.CNList, re.startPoint.ID);
                    ConNode cNode2 = ConNode.GetPLbyID(this.CNList, re.endPoint.ID);
                    ConNode cNode3 = ConNode.GetPLbyID(this.CNList, vV.ID);

                    if (cNode1 == null && cNode2 == null && cNode3 == null)
                    {
                        continue;
                    }
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

                                    nextRe = GetVEdgeofT1(nextTri, out vNextEdge1, out vNextEdge2, out vNextV);
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
                                if (nextTri != null && nextTri.TriType == 1)
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
                                    nextRe = GetVEdgeofT1(nextTri, out vNextEdge1, out vNextEdge2, out vNextV);
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
                                if (nextTri != null && nextTri.TriType == 1)
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
                                    nextRe = GetVEdgeofT1(nextTri, out vNextEdge1, out vNextEdge2, out vNextV);
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
                                if (nextTri != null && nextTri.TriType == 1)
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
                                    nextRe = GetVEdgeofT1(nextTri, out vNextEdge1, out vNextEdge2, out vNextV);
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
                                if (nextTri != null && nextTri.TriType == 1)
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

                                    nextRe = GetVEdgeofT1(nextTri, out vNextEdge1, out vNextEdge2, out vNextV);
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
                                if (nextTri != null && nextTri.TriType == 1)
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
                                    nextRe = GetVEdgeofT1(nextTri, out vNextEdge1, out vNextEdge2, out vNextV);
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
                                if (nextTri != null && nextTri.TriType == 1)
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
                                    nextRe = GetVEdgeofT1(nextTri, out vNextEdge1, out vNextEdge2, out vNextV);
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
                                if (nextTri != null && nextTri.TriType == 1)
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
                                    nextRe = GetVEdgeofT1(nextTri, out vNextEdge1, out vNextEdge2, out vNextV);
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
                                if (nextTri != null && nextTri.TriType == 1)
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
        }

        /// <summary>
        /// 遍历三角形构建骨架线段For道路网
        /// </summary>
        public void TranverseSkeleton_Segment_NT()
        {
            Skeleton_SegmentList = new List<Skeleton_Segment>();
            //以下实现遍历三角形形成骨架线段的算法

            //定义栈
            Stack<Triangle> StaTri = new Stack<Triangle>();
            Stack<TriEdge> StaEdge = new Stack<TriEdge>();
            Stack<TriNode> StaCP = new Stack<TriNode>();

            if (this.CDT.TriangleList == null || this.CDT.TriangleList.Count == 0)
                return;

            PostProcessCDTforRNT();
            int TriCount = this.CDT.TriangleList.Count;
            bool[] TriTranversed = new bool[TriCount];
            for (int j = 0; j < TriCount; j++)
            {
                TriTranversed[j] = false;
            }

            Triangle startTri = null; //起始三角形，可以是2类0类或1类中的单连通者
            Triangle nextTri = null;  //骨架线段中间的三角形
            Skeleton_Segment newSS = null;//新的一条骨架线段

            TriEdge vedge = null;
            TriEdge ovedge = null;

            int i = 0;
            TriNode cp = null;


            //栈不为空或所有的三角形还没有遍历完
            while (StaTri.Count != 0 || i < TriCount)
            {


                #region 骨架线上第一个三角形

                #region 栈中无0类三角形
                if (StaTri.Count == 0)
                {
                    startTri = CDT.TriangleList[i];

                    //如果是2类或1类中的单通道三角形，则可作为起点
                    if (startTri.TriType == 3)
                    {
                        TriTranversed[i] = true;
                        TriNode centerV = CalTriCenterPoint(startTri.point1, startTri.point2, startTri.point3);
                       
                        newSS = new Skeleton_Segment();
                        newSS.TriangleList.Add(startTri);//加入起始三角形
                        newSS.Axis_Points.Add(startTri.point1);
                        newSS.Axis_Points.Add(centerV);
                        this.Skeleton_SegmentList.Add(newSS);
                        newSS = new Skeleton_Segment();
                        newSS.TriangleList.Add(startTri);//加入起始三角形
                        newSS.Axis_Points.Add(startTri.point2);
                        newSS.Axis_Points.Add(centerV);
                        this.Skeleton_SegmentList.Add(newSS);
                        newSS = new Skeleton_Segment();
                        newSS.TriangleList.Add(startTri);//加入起始三角形
                        newSS.Axis_Points.Add(startTri.point3);
                        newSS.Axis_Points.Add(centerV);
                        this.Skeleton_SegmentList.Add(newSS);
                        i++;
                        continue;
                    }

                    else if (startTri.TriType == 1 && TriTranversed[i] == false && IsNoPathT1(startTri))
                    {

                        if (startTri.ID == 115)
                        {
                            int error = 0;
                        }

                        TriEdge vE1=null;
                        TriEdge vE2=null;
                        TriNode vVex=null;
                        this.GetVEdgeofT1(startTri,out vE1,out vE2,out vVex);
                        TriNode n1 = this.CalLineCenterPoint(vE1.startPoint, vE1.endPoint);
                        TriNode n2 = this.CalLineCenterPoint(vE2.startPoint, vE2.endPoint);
                        TriTranversed[i] = true;
                        newSS = new Skeleton_Segment();

                        newSS.TriangleList.Add(startTri);//加入起始三角形
                        newSS.Axis_Points.Add(n1);
                        newSS.Axis_Points.Add(n2);
                        this.Skeleton_SegmentList.Add(newSS);
                        i++;
                        continue;
                    }

                    else if ((startTri.TriType == 2 && TriTranversed[i] == false) ||
                    (startTri.TriType == 1 && TriTranversed[i] == false && IsSinglePathT1(startTri)))
                    {
                        if (startTri.ID == 115)
                        {
                            int error = 0;
                        }

                        TriTranversed[i] = true;

                        newSS = new Skeleton_Segment();

                        newSS.TriangleList.Add(startTri);//加入起始三角形

                        if (startTri.TriType == 2)
                        {
                            //计算并加入起点,并得到下一虚拟边
                            newSS.Axis_Points.Add(GetStartPointofT2(startTri, out vedge));
                        }

                        else if (startTri.TriType == 1)
                        {
                            TriEdge e = GetStartEdgeofT1(startTri);
                            //计算并加入起点
                            newSS.Axis_Points.Add(this.CalLineCenterPoint(e.startPoint, e.endPoint));
                            vedge = GetOtherVEdgeofT1(startTri, e);
                        }
                        //计算并加入第二点
                        newSS.Axis_Points.Add(this.CalLineCenterPoint(vedge.startPoint, vedge.endPoint));

                        nextTri = vedge.rightTriangle;
                        i++;
                    }
                    else
                    {
                        i++;
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
                    if (vedge == null)
                    {
                        continue;
                    }

                    newSS = new Skeleton_Segment();

                    newSS.TriangleList.Add(startTri);//加入起始三角形
                    newSS.Axis_Points.Add(cp);//加入起点
                    newSS.Axis_Points.Add(this.CalLineCenterPoint(vedge.startPoint, vedge.endPoint));//加入第二点

                    nextTri = vedge.rightTriangle;
                }
                #endregion

                #endregion

                //循环遍历路径上所有的1类连通三角形，
                //直到遇到2类，0类或半封闭1类三角形为止,或循环链路为止
                #region 遍历中间三角形
                while (nextTri != null && nextTri.TriType == 1 && TriTranversed[nextTri.ID] != true)
                {
                    if (startTri.ID == 115)
                    {
                        int error = 0;
                    }

                    TriTranversed[nextTri.ID] = true;
                    newSS.TriangleList.Add(nextTri);
                    ovedge = GetOtherVEdgeofT1(nextTri, vedge.doulEdge);
                    newSS.Axis_Points.Add(this.CalLineCenterPoint(ovedge.startPoint, ovedge.endPoint));
                    vedge = ovedge;

                    nextTri = vedge.rightTriangle;

                }
                #endregion

                #region 骨架线段的最后一个三角形

                #region 如果没有邻接三角形，结束遍历
                if (nextTri == null && newSS != null)
                {
                    this.Skeleton_SegmentList.Add(newSS);


                    startTri = null;
                    nextTri = null;
                    newSS = null;

                }
                #endregion

                #region 如果是环路
                else if (nextTri != null && nextTri.TriType == 1 && TriTranversed[nextTri.ID] == true)
                {
                    if (nextTri.ID == startTri.ID)
                    {
                        TriNode endp = newSS.Axis_Points[0];
                        newSS.Axis_Points.Add(endp);//将起点作为终点添加到链表的最后
                        newSS.TriangleList.Add(nextTri);
                        this.Skeleton_SegmentList.Add(newSS);
                    }
                    else
                    {
                        this.Skeleton_SegmentList.Add(newSS);
                    }

                    startTri = null;
                    nextTri = null;
                    newSS = null;
                }
                #endregion

                #region  如果是2类三角形
                else if (nextTri != null && nextTri.TriType == 2)
                {
                    TriEdge oe = null;
                    TriTranversed[nextTri.ID] = true;
                    newSS.TriangleList.Add(nextTri);
                    newSS.Axis_Points.Add(GetStartPointofT2(nextTri, out oe));
                    this.Skeleton_SegmentList.Add(newSS);

                    startTri = null;
                    nextTri = null;
                    newSS = null;
                }
                #endregion

                #region 如果是0类三角形,进入二叉树模式
                else if (nextTri != null && nextTri.TriType == 0)
                {
                    //结束遍历
                    TriNode centerV = CalTriCenterPoint(nextTri.point1, nextTri.point2, nextTri.point3);

                    newSS.TriangleList.Add(nextTri);
                    newSS.Axis_Points.Add(centerV);
                    this.Skeleton_SegmentList.Add(newSS);

                    if (TriTranversed[nextTri.ID] != true)
                    {
                        TriTranversed[nextTri.ID] = true;

                        //加入两个分支的起点到栈中
                        TriEdge edge1 = null;
                        TriEdge edge2 = null;

                        GetAnother2EdgeofT0(nextTri, vedge, out edge1, out edge2);

                        StaTri.Push(nextTri);
                        StaEdge.Push(edge1);
                        StaCP.Push(centerV);

                        StaTri.Push(nextTri);
                        StaEdge.Push(edge2);
                        StaCP.Push(centerV);
                    }

                    startTri = null;
                    nextTri = null;
                    newSS = null;
                }
                #endregion
                #endregion

            }

         /*   #region 如果还存在没有遍历的三角形则说明存在环路需要特别处理之
            int index = -1;
            while (!IsAllTranversed(TriTranversed, out index))
            {
                startTri = CDT.TriangleList[index];
                //如果是2类或1类中的单通道三角形，则可作为起点
                if (startTri.TriType == 1 && TriTranversed[index] == false)
                {
                    TriTranversed[index] = true;
                    newSS = new Skeleton_Segment();
                    newSS.TriangleList.Add(startTri);//加入起始三角形

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
                    //计算并加入起点
                    newSS.Axis_Points.Add(this.CalLineCenterPoint(e.startPoint, e.endPoint));
                    vedge = GetOtherVEdgeofT1(startTri, e);

                    //计算并加入第二点
                    newSS.Axis_Points.Add(this.CalLineCenterPoint(vedge.startPoint, vedge.endPoint));
                    nextTri = vedge.rightTriangle;
                }

                //循环遍历路径上所有的1类连通三角形，
                //直到遇到2类，0类或半封闭1类三角形为止,或循环链路为止
                #region 遍历中间三角形
                while (nextTri != null && nextTri.TriType == 1 && TriTranversed[nextTri.ID] != true)
                {
                    TriTranversed[nextTri.ID] = true;
                    newSS.TriangleList.Add(nextTri);
                    ovedge = GetOtherVEdgeofT1(nextTri, vedge.doulEdge);
                    newSS.Axis_Points.Add(this.CalLineCenterPoint(ovedge.startPoint, ovedge.endPoint));
                    vedge = ovedge;
                    nextTri = vedge.rightTriangle;
                }
                #endregion

                //  #region 如果没有邻接三角形，结束遍历
                //if (nextTri == null && newSS != null)
                //{
                //    this.Skeleton_SegmentList.Add(newSS);

                //    startTri = null;
                //    nextTri = null;
                //    newSS = null;

                //}
                //#endregion

                #region 如果是环路
                if (nextTri != null && nextTri.TriType == 1 && TriTranversed[nextTri.ID] == true)
                {
                    TriNode endp = newSS.Axis_Points[0];
                    newSS.Axis_Points.Add(endp);//将起点作为终点添加到链表的最后
                    newSS.TriangleList.Add(nextTri);
                    this.Skeleton_SegmentList.Add(newSS);

                    startTri = null;
                    nextTri = null;
                    newSS = null;

                }
                #endregion

            }
            #endregion */

        }

        /// <summary>
        /// 遍历三角形构建骨架线段For街区
        /// </summary>
        public void TranverseSkeleton_Segment_PLP()
        {
            Skeleton_SegmentList = new List<Skeleton_Segment>();
            //以下实现遍历三角形形成骨架线段的算法

            //定义栈
            Stack<Triangle> StaTri = new Stack<Triangle>();
            Stack<TriEdge> StaEdge = new Stack<TriEdge>();
            Stack<TriNode> StaCP = new Stack<TriNode>();

            if (this.CDT.TriangleList == null || this.CDT.TriangleList.Count == 0)
                return;

            PostProcessCDTforPLP(true);//不删除凹部
            int TriCount = this.CDT.TriangleList.Count;
            bool[] TriTranversed = new bool[TriCount];
            for (int j = 0; j < TriCount; j++)
            {
                TriTranversed[j] = false;
            }

            Triangle startTri = null; //起始三角形，可以是2类0类或1类中的单连通者
            Triangle nextTri = null;  //骨架线段中间的三角形
            Skeleton_Segment newSS = null;//新的一条骨架线段

            TriEdge vedge = null;
            TriEdge ovedge = null;

            int i = 0;
            TriNode cp = null;


            //栈不为空或所有的三角形还没有遍历完
            while (StaTri.Count != 0 || i < TriCount)
            {


                #region 骨架线上第一个三角形

                #region 栈中无0类三角形
                if (StaTri.Count == 0)
                {
                    startTri = CDT.TriangleList[i];

                    //如果是三类
                    if (startTri.TriType == 3)
                    {
                        TriTranversed[i] = true;
                        TriNode centerV = CalTriCenterPoint(startTri.point1, startTri.point2, startTri.point3);

                        newSS = new Skeleton_Segment();
                        newSS.TriangleList.Add(startTri);//加入起始三角形
                        newSS.Axis_Points.Add(startTri.point1);
                        newSS.Axis_Points.Add(centerV);
                        this.Skeleton_SegmentList.Add(newSS);
                        newSS = new Skeleton_Segment();
                        newSS.TriangleList.Add(startTri);//加入起始三角形
                        newSS.Axis_Points.Add(startTri.point2);
                        newSS.Axis_Points.Add(centerV);
                        this.Skeleton_SegmentList.Add(newSS);
                        newSS = new Skeleton_Segment();
                        newSS.TriangleList.Add(startTri);//加入起始三角形
                        newSS.Axis_Points.Add(startTri.point3);
                        newSS.Axis_Points.Add(centerV);
                        this.Skeleton_SegmentList.Add(newSS);
                        i++;
                        continue;
                    }
                    //如果是不连通1类
                    else if (startTri.TriType == 1 && TriTranversed[i] == false && IsNoPathT1(startTri))
                    {

                        if (startTri.ID == 115)
                        {
                            int error = 0;
                        }

                        TriEdge vE1 = null;
                        TriEdge vE2 = null;
                        TriNode vVex = null;
                        this.GetVEdgeofT1(startTri, out vE1, out vE2, out vVex);
                        TriNode n1 = this.CalLineCenterPoint(vE1.startPoint, vE1.endPoint);
                        TriNode n2 = this.CalLineCenterPoint(vE2.startPoint, vE2.endPoint);
                        TriTranversed[i] = true;
                        newSS = new Skeleton_Segment();

                        newSS.TriangleList.Add(startTri);//加入起始三角形
                        newSS.Axis_Points.Add(n1);
                        newSS.Axis_Points.Add(n2);
                        this.Skeleton_SegmentList.Add(newSS);
                        i++;
                        continue;
                    }
                    else if ((startTri.TriType == 2 && TriTranversed[i] == false) ||
                    (startTri.TriType == 1 && TriTranversed[i] == false && IsSinglePathT1(startTri)))
                    {
                        if (startTri.ID == 115)
                        {
                            int error = 0;
                        }

                        TriTranversed[i] = true;

                        newSS = new Skeleton_Segment();

                        newSS.TriangleList.Add(startTri);//加入起始三角形

                        if (startTri.TriType == 2)
                        {
                            //计算并加入起点,并得到下一虚拟边
                            newSS.Axis_Points.Add(GetStartPointofT2(startTri, out vedge));
                        }

                        else if (startTri.TriType == 1)
                        {
                            TriEdge e = GetStartEdgeofT1(startTri);
                            //计算并加入起点
                            newSS.Axis_Points.Add(this.CalLineCenterPoint(e.startPoint, e.endPoint));
                            vedge = GetOtherVEdgeofT1(startTri, e);
                        }
                        //计算并加入第二点
                        newSS.Axis_Points.Add(this.CalLineCenterPoint(vedge.startPoint, vedge.endPoint));

                        nextTri = vedge.rightTriangle;
                        i++;
                    }
                    else
                    {
                        i++;
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

                    if (startTri.ID == 127)
                    {
                        int error = 0;
                    }
                    if (vedge == null)
                    {
                        continue;
                    }

                    newSS = new Skeleton_Segment();

                    newSS.TriangleList.Add(startTri);//加入起始三角形
                    newSS.Axis_Points.Add(cp);//加入起点
                    newSS.Axis_Points.Add(this.CalLineCenterPoint(vedge.startPoint, vedge.endPoint));//加入第二点

                    nextTri = vedge.rightTriangle;
                }
                #endregion

                #endregion

                //循环遍历路径上所有的1类连通三角形，
                //直到遇到2类，0类或半封闭1类三角形为止,或循环链路为止
                #region 遍历中间三角形
                while (nextTri != null && nextTri.TriType == 1 && TriTranversed[nextTri.ID] != true)
                {
                    if (startTri.ID == 115)
                    {
                        int error = 0;
                    }

                    TriTranversed[nextTri.ID] = true;
                    newSS.TriangleList.Add(nextTri);
                    ovedge = GetOtherVEdgeofT1(nextTri, vedge.doulEdge);
                    newSS.Axis_Points.Add(this.CalLineCenterPoint(ovedge.startPoint, ovedge.endPoint));
                    vedge = ovedge;

                    nextTri = vedge.rightTriangle;

                }
                #endregion

                #region 骨架线段的最后一个三角形

                #region 如果没有邻接三角形，结束遍历
                if (nextTri == null && newSS != null)
                {
                    this.Skeleton_SegmentList.Add(newSS);


                    startTri = null;
                    nextTri = null;
                    newSS = null;

                }
                #endregion

                #region 如果是环路
                else if (nextTri != null && nextTri.TriType == 1 && TriTranversed[nextTri.ID] == true)
                {
                    if (nextTri.ID == startTri.ID)
                    {
                        TriNode endp = newSS.Axis_Points[0];
                        newSS.Axis_Points.Add(endp);//将起点作为终点添加到链表的最后
                        newSS.TriangleList.Add(nextTri);
                        this.Skeleton_SegmentList.Add(newSS);
                    }
                    else
                    {
                        this.Skeleton_SegmentList.Add(newSS);
                    }

                    startTri = null;
                    nextTri = null;
                    newSS = null;

                }
                #endregion

                #region  如果是2类三角形
                else if (nextTri != null && nextTri.TriType == 2)
                {
                    TriEdge oe = null;
                    TriTranversed[nextTri.ID] = true;
                    newSS.TriangleList.Add(nextTri);
                    newSS.Axis_Points.Add(GetStartPointofT2(nextTri, out oe));
                    this.Skeleton_SegmentList.Add(newSS);

                    startTri = null;
                    nextTri = null;
                    newSS = null;
                }
                #endregion

                #region 如果是0类三角形,进入二叉树模式
                else if (nextTri != null && nextTri.TriType == 0)
                {
                    //结束遍历
                    TriNode centerV = CalTriCenterPoint(nextTri.point1, nextTri.point2, nextTri.point3);

                    newSS.TriangleList.Add(nextTri);
                    newSS.Axis_Points.Add(centerV);
                    this.Skeleton_SegmentList.Add(newSS);

                    if (TriTranversed[nextTri.ID] != true)
                    {
                        TriTranversed[nextTri.ID] = true;

                        //加入两个分支的起点到栈中
                        TriEdge edge1 = null;
                        TriEdge edge2 = null;

                        GetAnother2EdgeofT0(nextTri, vedge, out edge1, out edge2);
                        if (edge1.rightTriangle != null)
                        {

                            StaTri.Push(nextTri);
                            StaEdge.Push(edge1);
                            StaCP.Push(centerV);
                        }
                        if (edge2.rightTriangle != null)
                        {
                            StaTri.Push(nextTri);
                            StaEdge.Push(edge2);
                            StaCP.Push(centerV);
                        }
                    }

                    startTri = null;
                    nextTri = null;
                    newSS = null;
                }
                #endregion
                #endregion

            }

              #region 如果还存在没有遍历的三角形则说明存在环路需要特别处理之
               int index = -1;
               while (StaTri.Count != 0 ||!IsAllTranversed(TriTranversed, out index))
               {
                   if (index == 127)
                   {
                       int error = 0;
                   }

                   if (StaTri.Count != 0)
                   {
                   
                       //出栈
                       startTri = StaTri.Pop();
                       vedge = StaEdge.Pop(); //虚边
                       cp = StaCP.Pop();
 

                       if (vedge == null)
                       {
                           continue;
                       }

                       newSS = new Skeleton_Segment();

                       newSS.TriangleList.Add(startTri);//加入起始三角形
                       newSS.Axis_Points.Add(cp);//加入起点
                       newSS.Axis_Points.Add(this.CalLineCenterPoint(vedge.startPoint, vedge.endPoint));//加入第二点

                       nextTri = vedge.rightTriangle;
                   }
                   else
                   {
                       startTri = CDT.TriangleList[index];
                       //如果是2类或1类中的单通道三角形，则可作为起点
                       if (startTri.TriType == 1 && TriTranversed[index] == false)
                       {
                           TriTranversed[index] = true;
                           newSS = new Skeleton_Segment();
                           newSS.TriangleList.Add(startTri);//加入起始三角形

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
                           //计算并加入起点
                           newSS.Axis_Points.Add(this.CalLineCenterPoint(e.startPoint, e.endPoint));
                           vedge = GetOtherVEdgeofT1(startTri, e);

                           //计算并加入第二点
                           newSS.Axis_Points.Add(this.CalLineCenterPoint(vedge.startPoint, vedge.endPoint));
                           nextTri = vedge.rightTriangle;
                       }

                       else if (startTri.TriType == 0 && TriTranversed[index] == false)
                       {
                           if (startTri.ID == 127)
                           {
                               int error = 0;
                           }

                           TriTranversed[index] = true;
                           newSS = new Skeleton_Segment();
                           newSS.TriangleList.Add(startTri);//加入起始三角形
                           //结束遍历
                           TriNode centerV = CalTriCenterPoint(startTri.point1, startTri.point2, startTri.point3);
                           newSS.Axis_Points.Add(centerV);
                           vedge = startTri.edge1;
                           newSS.Axis_Points.Add(this.CalLineCenterPoint(startTri.edge1.startPoint, startTri.edge1.endPoint));
                           nextTri = startTri.edge1.rightTriangle;

                           //加入两个分支的起点到栈中
                           StaTri.Push(nextTri);
                           StaEdge.Push(startTri.edge3);
                           StaCP.Push(centerV);

                           StaTri.Push(nextTri);
                           StaEdge.Push(startTri.edge2);
                           StaCP.Push(centerV);
                           i++;

                       }
                   }

                   //循环遍历路径上所有的1类连通三角形，
                   //直到遇到2类，0类或半封闭1类三角形为止,或循环链路为止
                   #region 遍历中间三角形
                   while (nextTri != null && nextTri.TriType == 1 && TriTranversed[nextTri.ID] != true)
                   {
                       TriTranversed[nextTri.ID] = true;
                       newSS.TriangleList.Add(nextTri);
                       if (nextTri.ID == 89)
                       {
                           int error = 0;
                       }
                       ovedge = GetOtherVEdgeofT1(nextTri, vedge.doulEdge);
                       //========================
                       if (ovedge == null)
                       {
                           nextTri = null;
                           break;
                       }
                       //=============================

                       newSS.Axis_Points.Add(this.CalLineCenterPoint(ovedge.startPoint, ovedge.endPoint));
                       vedge = ovedge;
                       nextTri = vedge.rightTriangle;
                   }
                   #endregion

                 #region 如果没有邻接三角形，结束遍历
                 if (nextTri == null && newSS != null)
                 {
                     this.Skeleton_SegmentList.Add(newSS);

                     startTri = null;
                     nextTri = null;
                     newSS = null;

                 }
                 #endregion

                 #region 如果是0类三角形,进入二叉树模式
                 else if (nextTri != null && nextTri.TriType == 0)
                 {
                     //结束遍历
                     TriNode centerV = CalTriCenterPoint(nextTri.point1, nextTri.point2, nextTri.point3);

                     newSS.TriangleList.Add(nextTri);
                     newSS.Axis_Points.Add(centerV);
                     this.Skeleton_SegmentList.Add(newSS);

                     if (TriTranversed[nextTri.ID] != true)
                     {
                         TriTranversed[nextTri.ID] = true;

                         //加入两个分支的起点到栈中
                         TriEdge edge1 = null;
                         TriEdge edge2 = null;

                         GetAnother2EdgeofT0(nextTri, vedge, out edge1, out edge2);

                         StaTri.Push(nextTri);
                         StaEdge.Push(edge1);
                         StaCP.Push(centerV);

                         StaTri.Push(nextTri);
                         StaEdge.Push(edge2);
                         StaCP.Push(centerV);
                     }

                     startTri = null;
                     nextTri = null;
                     newSS = null;
                 }
                 #endregion

                   #region 如果是环路
                   if (nextTri != null && nextTri.TriType == 1 && TriTranversed[nextTri.ID] == true)
                   {
                       if (nextTri.ID == startTri.ID)
                       {
                           TriNode endp = newSS.Axis_Points[0];
                           newSS.Axis_Points.Add(endp);//将起点作为终点添加到链表的最后
                           newSS.TriangleList.Add(nextTri);
                           this.Skeleton_SegmentList.Add(newSS);
                       }
                       else
                       {
                           this.Skeleton_SegmentList.Add(newSS);
                       }

                       startTri = null;
                       nextTri = null;
                       newSS = null;
                   }
                   #endregion

               }
               #endregion 

        }
        /// <summary>
        /// 判断三角形是否全部遍历过
        /// </summary>
        /// <param name="tranversed">是否遍历标示数组</param>
        /// <param name="index">当前没有遍历的三角形下标</param>
        /// <returns></returns>
        private bool IsAllTranversed(bool[] tranversed, out int index)
        {
            index = -1;
            for (int i = 0; i < tranversed.Length; i++)
            {
                if (tranversed[i] == false)
                {
                    index = i;
                    return false;
                }
            }
            return true;
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


    }
}
