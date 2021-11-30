using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using MatrixOperation;
using System.Data;
using AuxStructureLib.IO;
using AuxStructureLib.ConflictLib;

namespace AlgEMLib
{
    /// <summary>
    /// 受力向量-for Beams
    /// </summary>
    public class BeamsForceVector
    {
        public List<Force> ForceList = null;//数组
        public ProxiGraph ProxiGraph = null;
        public ProxiGraph OrigialProxiGraph = null;

        public bool isDragForce=false;

        public double RMSE = 0.5;

        public SMap Map = null;

        private Matrix vector_F = null;

        public Matrix Vector_F { get { return vector_F; } }

        /// <summary>
        /// z判断是否还有冲突
        /// </summary>
        /// <param name="forceList"></param>
        /// <returns></returns>
        private bool IsHasForce(List<Force> forceList)
        {
            foreach (Force curF in forceList)
            {
                if (curF.F > 0.0001)//判断受力为0的条件
                {
                    return true;
                }
            }
            return false;
        }

        #region For单条线目标移位-从文件中读受力
        /// <summary>
        /// 受力向量-线对象，读文件中的力
        /// </summary>
        public BeamsForceVector(PolylineObject polyline,string forcefile)
        {
            ForceList = ReadForceListfrmFile(forcefile);
            MakeForceVectorfrmPolyline(polyline);
        }

        /// <summary>
        /// 受力向量-线对象，读文件中的力
        /// </summary>
        public BeamsForceVector()
        {
            
        }

        /// <summary>
        /// 受力向量-线对象，设置受力全为0
        /// </summary>
        public BeamsForceVector(PolylineObject polyline)
        {
            MakeForceVectorfrmPolyline0(polyline);
        }

        /// <summary>
        /// 创建受力向量初始化为0
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public bool MakeForceVectorfrmPolyline0(PolylineObject polyline)
        {
            int n = polyline.PointList.Count;
            vector_F = new Matrix(3 * n, 1);

            return true;
        }

        /// <summary>
        /// 根据线对象建立受力向量
        /// </summary>
        /// <param name="polyline">线对象</param>
        public bool MakeForceVectorfrmPolyline(PolylineObject polyline)
        {
            int n = polyline.PointList.Count;
            vector_F = new Matrix(3 * n, 1);

            double L = 0.0;
            double sin = 0.0;
            double cos = 0.0;

            //WriteForce(@"E:\map\实验数据\network", "F.txt", forceList);
            Node fromPoint = null;
            Node nextPoint = null;
            int index0 = -1;
            int index1 = -1;

            for(int i=0;i<n-1;i++) 
            {
                fromPoint = polyline.PointList[i];
                nextPoint = polyline.PointList[i+1];

                index0 = fromPoint.ID;
                index1 = nextPoint.ID;
                //获得受力
                Force force0 = GetForcebyIndex(index0);
                Force force1 = GetForcebyIndex(index1);

                L = ComFunLib.CalLineLength(fromPoint, nextPoint);
                sin = (nextPoint.Y - fromPoint.Y) / L;
                cos = (nextPoint.X - fromPoint.X) / L;

                if (force0 != null)
                {
                    //vector_F[3 * index0, 0] += force0.Fx;
                    //vector_F[3 * index0 + 1, 0] += force0.Fy;

                    //vector_F[3 * index0+ 2, 0] += -1.0 * L * (force0.Fx * sin + force0.Fy * cos);


                    vector_F[3 * index0, 0] +=0.5*L* force0.Fx;
                    vector_F[3 * index0 + 1, 0] += 0.5 * L * force0.Fy;
                    vector_F[3 * index0 + 2, 0] += 1.0 * L * L*(force0.Fx * sin + force0.Fy * cos)/12;

                }

                if (force1 != null)
                {
                   // vector_F[3 * index1, 0] += force1.Fx;
                   // vector_F[3 * index1 + 1, 0] += force1.Fy;
                   //vector_F[3 * index1 + 2, 0] += +1.0 * L * (force1.Fx * sin + force1.Fy * cos);

                    vector_F[3 * index1, 0] += 0.5 * L * force1.Fx;
                    vector_F[3 * index1 + 1, 0] += 0.5 * L * force1.Fy;
                    vector_F[3 * index1 + 2, 0] +=-1.0 * L * L * (force1.Fx * sin + force1.Fy * cos) / 12;

                }
            }
            return true;
        }



        /// <summary>
        /// 读取文件中的受力值
        /// </summary>
        /// <param name="forcefile"></param>
        /// <returns></returns>
        private List<Force> ReadForceListfrmFile(string forcefile)
        {
            List<Force> forceList = new List<Force>();
            //读文件========
            DataTable dt = TestIO.ReadData(forcefile);

            foreach (DataRow curR in dt.Rows)
            {
                int curID = Convert.ToInt32(curR[0]);
                double curFx = Convert.ToDouble(curR[1]);
                double curFy = Convert.ToDouble(curR[2]);
                Force curForce = new Force(curID);
                curForce.Fx = curFx;
                curForce.Fy = curFy;
                curForce.F = Math.Sqrt(curFx * curFx + curFy * curFy);
                forceList.Add(curForce);
            }
            return forceList;
        }

        #endregion

        #region for邻近图移位-最早的邻近图移位


        /// <summary>
        /// 受力向量-邻近图
        /// </summary>
        /// <param name="proxiGraph"></param>
        public BeamsForceVector(ProxiGraph proxiGraph)
        {
            ProxiGraph = proxiGraph;
            ForceList = new List<Force>();
        }

        /// <summary>
        /// 由邻近图计算外力
        /// </summary>
        /// <param name="proxiGraph">邻近图</param>
        /// <param name="disThreshold">阈值</param>
        /// <returns>是否成功</returns>
        public bool CreateForceVectorfrmGraph(double disThreshold)
        {
            if (ProxiGraph == null || ProxiGraph.NodeList == null || ProxiGraph.EdgeList == null)
                return false;

            InitForceListfrmGraph(ProxiGraph);//初始化受力向量

            foreach (ProxiEdge curEdge in ProxiGraph.EdgeList)
            {
                double distance = curEdge.NearestEdge.NearestDistance;
                double curForce = 0;
                NearestPoint point1 = null;
                NearestPoint point2 = null;
                int id1=-1;
                int id2=-1;
                if (distance < disThreshold)
                {
                    curForce = disThreshold - distance;
                    point1 = curEdge.NearestEdge.Point1;
                    point2 = curEdge.NearestEdge.Point2;
                    id1 = curEdge.Node1.TagID;
                    id2 = curEdge.Node2.TagID;
                    Cal_Accumulate_Force(curForce, point1, point2, id1, id2);
                }
            }
           //计算最终的受力大小
            foreach (Force curForce in this.ForceList)
            {
                curForce.F=Math.Sqrt(curForce.Fx*curForce.Fx+curForce.Fy*curForce.Fy);
                if (curForce.QanSumF != 0)//数量和不为零，说明参与冲突
                {
                    curForce.RF = curForce.F / curForce.QanSumF;//越大说明越该优先移位
                }
                else
                {
                    curForce.RF=-1;
                }
            }

            //按curForce.RF 排名
            for (int i = 0; i < ForceList.Count; i++)
            {
                if (ForceList[i].RF != 0 && ForceList[i].RF != -1)
                {
                    ForceList[i].SID = 0;
                    for (int j = 0; j < ForceList.Count; j++)
                    {
                        if (ForceList[j].RF != 0 && ForceList[j].RF != -1)
                        {
                            if (ForceList[j].RF < ForceList[i].RF)
                            {
                                ForceList[i].SID++;
                            }
                        }
                    }
                }
                else if (ForceList[i].RF == 0)
                {
                    ForceList[i].SID=-1;
                }
                else if (ForceList[i].RF == -1)
                {
                    ForceList[i].SID = -2;
                }
            }

            MakeForceVectorfrmGraph();
            return true;
        }

        /// <summary>
        /// 由邻近图计算外力
        /// </summary>
        /// <param name="proxiGraph">邻近图</param>
        /// <param name="disThreshold">阈值</param>
        /// <returns>是否成功</returns>
        public bool CreateForceVectorfrmGraph(double disThresholdLP, double disThresholdPP)
        {
            if (ProxiGraph == null || ProxiGraph.NodeList == null || ProxiGraph.EdgeList == null)
                return false;

            InitForceListfrmGraph(ProxiGraph);//初始化受力向量

            foreach (ProxiEdge curEdge in ProxiGraph.EdgeList)
            {
                ProxiNode lineNode = null;
                ProxiNode unLineNode = null;
                NearestPoint linePoint = null;
                NearestPoint unLinePoint = null;
                double distance = 0;
                double curForce = 0;

                int id1 = -1;
                int id2 = -1;

                distance = curEdge.NearestEdge.NearestDistance;
                //处理与边界线相邻的情况
                if (curEdge.Node1.FeatureType == FeatureType.PolylineType || curEdge.Node2.FeatureType == FeatureType.PolylineType)
                {

                    if (curEdge.Node1.FeatureType == FeatureType.PolylineType && (curEdge.Node2.FeatureType == FeatureType.PolygonType || curEdge.Node2.FeatureType == FeatureType.PointType))
                    {
                        lineNode = curEdge.Node1;
                        unLineNode = curEdge.Node2;
                        linePoint = curEdge.NearestEdge.Point1;
                        unLinePoint = curEdge.NearestEdge.Point2;

                        id1 = curEdge.Node1.ID; 
                        id2 = curEdge.Node2.ID;
          
                        //if (unLineNode.TagID == curEdge.NearestEdge.Point1.ID)
                        //{
                        //    linePoint = curEdge.NearestEdge.Point2;
                        //    unLinePoint = curEdge.NearestEdge.Point1;
                        //}
                        //else if (unLineNode.TagID == curEdge.NearestEdge.Point2.ID)
                        //{
                        //    linePoint = curEdge.NearestEdge.Point1;
                        //    unLinePoint = curEdge.NearestEdge.Point2;
                        //}
                    }
                    else if (curEdge.Node2.FeatureType == FeatureType.PolylineType && (curEdge.Node1.FeatureType == FeatureType.PolygonType || curEdge.Node1.FeatureType == FeatureType.PointType))
                    {
                        lineNode = curEdge.Node2;
                        unLineNode = curEdge.Node1;

                       id1 = curEdge.Node2.ID;
                       id2 = curEdge.Node1.ID;

                       linePoint = curEdge.NearestEdge.Point2;
                       unLinePoint = curEdge.NearestEdge.Point1;

                       //if (unLineNode.TagID == curEdge.NearestEdge.Point1.ID )
                       //{
                       //    if (curEdge.Node1.FeatureType != FeatureType.PolylineType)
                       //    {
                       //        linePoint = curEdge.NearestEdge.Point2;
                       //        unLinePoint = curEdge.NearestEdge.Point1;
                       //    }
                       //    else
                       //    {
                       //        linePoint = curEdge.NearestEdge.Point2;
                       //        unLinePoint = curEdge.NearestEdge.Point1;
                       //    }
                       //}
                       //else if (unLineNode.TagID == curEdge.NearestEdge.Point2.ID && curEdge.Node1.FeatureType != FeatureType.PolylineType)
                       //{
                       //    linePoint = curEdge.NearestEdge.Point1;
                       //    unLinePoint = curEdge.NearestEdge.Point2;
                       //}
                       //else
                       //{
                       //    linePoint = curEdge.NearestEdge.Point1;
                       //    unLinePoint = curEdge.NearestEdge.Point2;
                       //}
                    }
  

                    if (distance < disThresholdLP)
                    {
                        curForce = disThresholdLP - distance;
                        Cal_Accumulate_Force_LP(curForce, linePoint, unLinePoint, id1, id2);
               
                    }
                }
               //处理点-点或面-面的情况
                else
                {
                    NearestPoint point1 = null;
                    NearestPoint point2 = null;
                    id1 = -1;
                    id2 = -1;
                    if (distance < disThresholdPP)
                    {
                        curForce = disThresholdPP - distance;
                        point1 = curEdge.NearestEdge.Point1;
                        point2 = curEdge.NearestEdge.Point2;
                        id1 = curEdge.Node1.ID;
                        id2 = curEdge.Node2.ID;
                        Cal_Accumulate_Force(curForce, point1, point2, id1, id2);
                    }
                }


               
            }
            //计算最终的受力大小
            foreach (Force curForce in this.ForceList)
            {
                curForce.F = Math.Sqrt(curForce.Fx * curForce.Fx + curForce.Fy * curForce.Fy);
                if (curForce.QanSumF != 0)//数量和不为零，说明参与冲突
                {
                    curForce.RF = curForce.F / curForce.QanSumF;//越大说明越该优先移位
                }
                else
                {
                    curForce.RF = -1;
                }
            }

            //按curForce.RF 排名
            for (int i = 0; i < ForceList.Count; i++)
            {
                if (ForceList[i].RF != 0 && ForceList[i].RF != -1)
                {
                    ForceList[i].SID = 0;
                    for (int j = 0; j < ForceList.Count; j++)
                    {
                        if (ForceList[j].RF != 0 && ForceList[j].RF != -1)
                        {
                            if (ForceList[j].RF < ForceList[i].RF)
                            {
                                ForceList[i].SID++;
                            }
                        }
                    }
                }
                else if (ForceList[i].RF == 0)
                {
                    ForceList[i].SID = -1;
                }
                else if (ForceList[i].RF == -1)
                {
                    ForceList[i].SID = -2;
                }
            }

            MakeForceVectorfrmGraph();
            return true;
        }

        /// <summary>
        /// 由邻近图计算外力
        /// </summary>
        /// <param name="proxiGraph">邻近图</param>
        /// <param name="disThreshold">阈值</param>
        /// <returns>是否成功</returns>
        public List<Force> CreateForceVectorfrmGraph(List<PolygonObject> PoList,double MaxTd,int ForceType,bool WeigthConsi,double InterDis)
        {
            #region 计算受力
            if (ProxiGraph == null || ProxiGraph.NodeList == null || ProxiGraph.EdgeList == null)
                return null;

            InitForceListfrmGraph(ProxiGraph);//初始化受力向量
            List<VertexForce> vForceList = new List<VertexForce>();

            foreach (ProxiEdge curEdge in ProxiGraph.EdgeList)
            {
                ProxiNode sNode = curEdge.Node1; ProxiNode eNode = curEdge.Node2;
                PolygonObject Po1 = this.GetPoByID(sNode.TagID, PoList);
                PolygonObject Po2 = this.GetPoByID(eNode.TagID, PoList);

                List<Force> ForceList = this.GetForce(sNode, eNode, Po1, Po2, ForceType,curEdge.adajactLable,MaxTd,WeigthConsi,curEdge.MSTLable,InterDis);//考虑引力

                if (ForceList.Count > 0)
                {
                    #region 添加Force
                    VertexForce svForce = this.GetvForcebyIndex(sNode.ID, vForceList);
                    if (svForce == null)
                    {
                        svForce = new VertexForce(sNode.ID);
                        vForceList.Add(svForce);
                    }
                    svForce.forceList.Add(ForceList[0]);//将当前的受力加入VertexForce数组

                    VertexForce evForce = this.GetvForcebyIndex(eNode.ID, vForceList);
                    if (evForce == null)
                    {
                        evForce = new VertexForce(eNode.ID);
                        vForceList.Add(evForce);
                    }
                    evForce.forceList.Add(ForceList[1]);//将当前的受力加入VertexForce数组
                    #endregion
                }
            }
            #endregion

            #region 吸引力
            //if (this.isDragForce == true || this.isDragForce == false)
            //{
            //    int n = this.ProxiGraph.NodeList.Count;
            //    for (int i = 0; i < n; i++)
            //    {

            //        ProxiNode curNode = this.ProxiGraph.NodeList[i];

            //        int id = curNode.ID;
            //        //  int tagID = curNode.TagID;
            //        FeatureType type = curNode.FeatureType;
            //        ProxiNode originalNode = this.OrigialProxiGraph.GetNodebyID(id);
            //        if (originalNode == null)
            //        {
            //            continue;
            //        }
            //        double distance = ComFunLib.CalLineLength(curNode, originalNode);
            //        if (distance > RMSE && (type != FeatureType.PolylineType))
            //        {
            //            //右边
            //            double f = distance - RMSE;
            //            double s = (originalNode.Y - curNode.Y) / distance;
            //            double c = (originalNode.X - curNode.X) / distance;
            //            //这里将力平分给两个对象
            //            double fx = f * c;
            //            double fy = f * s;
            //            Force force = new Force(id, fx, fy, s, c, f);
            //            VertexForce vForce = this.GetvForcebyIndex(id, vForceList);
            //            if (vForce == null)
            //            {
            //                vForce = new VertexForce(id);
            //                vForceList.Add(vForce);
            //            }
            //            vForce.forceList.Add(force);
            //        }
            //    }
            //    //foreach(Node cur Node )
            //}
            #endregion

            #region 求合力
            //先求最大力，以该力的方向为X轴方向建立局部坐标系，求四个主方向上的最大力，最后就合力
            List<Force> rforceList = new List<Force>();
            foreach (VertexForce vForce in vForceList)
            {
                if (vForce.forceList.Count == 1)//当只受一个力作用时
                {
                    rforceList.Add(vForce.forceList[0]);

                }
                else if (vForce.forceList.Count > 1)
                {
                    int index = 0;
                    double maxFx = 0;
                    double minFx = 0;
                    double maxFy = 0;
                    double minFy = 0;
                    Force maxF = GetMaxForce(out index, vForce.forceList);
                    maxFx = maxF.F;
                    double s = maxF.Sin;
                    double c = maxF.Cos;

                    for (int i = 0; i < vForce.forceList.Count; i++)
                    {

                        if (i != index)
                        {
                            Force F = vForce.forceList[i];
                            double fx = F.Fx * c + F.Fy * s;
                            double fy = F.Fy * c - F.Fx * s;

                            if (minFx > fx) minFx = fx;
                            if (maxFy < fy) maxFy = fy;
                            if (minFy > fy) minFy = fy;
                        }
                    }
                    double FFx = maxFx + minFx;
                    double FFy = maxFy + minFy;
                    double Fx = FFx * c - FFy * s;
                    double Fy = FFx * s + FFy * c;
                    double f = Math.Sqrt(Fx * Fx + Fy * Fy);
                    Force rForce = new Force(vForce.ID, Fx, Fy, f);
                    rforceList.Add(rForce);
                }
            }

            #endregion

            return rforceList;
        }

        /// <summary>
        /// 由邻近图计算外力
        /// </summary>
        /// <param name="proxiGraph">邻近图</param>
        /// <param name="disThreshold">阈值</param>
        /// <returns>是否成功</returns>
        public List<Force> GroupCreateForceVectorfrmGraph(List<PolygonObject> PoList, double MaxTd, int ForceType, bool WeigthConsi)
        {
            #region 计算受力
            if (ProxiGraph == null || ProxiGraph.NodeList == null || ProxiGraph.EdgeList == null)
                return null;

            InitForceListfrmGraph(ProxiGraph);//初始化受力向量
            List<VertexForce> vForceList = new List<VertexForce>();

            foreach (ProxiEdge curEdge in ProxiGraph.EdgeList)
            {
                ProxiNode sNode = curEdge.Node1; ProxiNode eNode = curEdge.Node2;
                List<PolygonObject> PoList1 = this.GetPoList(sNode, PoList);
                List<PolygonObject> PoList2 = this.GetPoList(eNode, PoList);

                List<Force> ForceList = this.GetForceGroup(sNode, eNode, PoList1, PoList2, ForceType, MaxTd, WeigthConsi);//考虑引力

                if (ForceList.Count > 0)
                {
                    #region 添加Force
                    VertexForce svForce = this.GetvForcebyIndex(sNode.ID, vForceList);
                    if (svForce == null)
                    {
                        svForce = new VertexForce(sNode.ID);
                        vForceList.Add(svForce);
                    }
                    svForce.forceList.Add(ForceList[0]);//将当前的受力加入VertexForce数组

                    VertexForce evForce = this.GetvForcebyIndex(eNode.ID, vForceList);
                    if (evForce == null)
                    {
                        evForce = new VertexForce(eNode.ID);
                        vForceList.Add(evForce);
                    }
                    evForce.forceList.Add(ForceList[1]);//将当前的受力加入VertexForce数组
                    #endregion
                }
            }
            #endregion

            #region 吸引力
            //if (this.isDragForce == true || this.isDragForce == false)
            //{
            //    int n = this.ProxiGraph.NodeList.Count;
            //    for (int i = 0; i < n; i++)
            //    {

            //        ProxiNode curNode = this.ProxiGraph.NodeList[i];

            //        int id = curNode.ID;
            //        //  int tagID = curNode.TagID;
            //        FeatureType type = curNode.FeatureType;
            //        ProxiNode originalNode = this.OrigialProxiGraph.GetNodebyID(id);
            //        if (originalNode == null)
            //        {
            //            continue;
            //        }
            //        double distance = ComFunLib.CalLineLength(curNode, originalNode);
            //        if (distance > RMSE && (type != FeatureType.PolylineType))
            //        {
            //            //右边
            //            double f = distance - RMSE;
            //            double s = (originalNode.Y - curNode.Y) / distance;
            //            double c = (originalNode.X - curNode.X) / distance;
            //            //这里将力平分给两个对象
            //            double fx = f * c;
            //            double fy = f * s;
            //            Force force = new Force(id, fx, fy, s, c, f);
            //            VertexForce vForce = this.GetvForcebyIndex(id, vForceList);
            //            if (vForce == null)
            //            {
            //                vForce = new VertexForce(id);
            //                vForceList.Add(vForce);
            //            }
            //            vForce.forceList.Add(force);
            //        }
            //    }
            //    //foreach(Node cur Node )
            //}
            #endregion

            #region 求合力
            //先求最大力，以该力的方向为X轴方向建立局部坐标系，求四个主方向上的最大力，最后就合力
            List<Force> rforceList = new List<Force>();
            foreach (VertexForce vForce in vForceList)
            {
                if (vForce.forceList.Count == 1)//当只受一个力作用时
                {
                    rforceList.Add(vForce.forceList[0]);

                }
                else if (vForce.forceList.Count > 1)
                {
                    int index = 0;
                    double maxFx = 0;
                    double minFx = 0;
                    double maxFy = 0;
                    double minFy = 0;
                    Force maxF = GetMaxForce(out index, vForce.forceList);
                    maxFx = maxF.F;
                    double s = maxF.Sin;
                    double c = maxF.Cos;

                    for (int i = 0; i < vForce.forceList.Count; i++)
                    {

                        if (i != index)
                        {
                            Force F = vForce.forceList[i];
                            double fx = F.Fx * c + F.Fy * s;
                            double fy = F.Fy * c - F.Fx * s;

                            if (minFx > fx) minFx = fx;
                            if (maxFy < fy) maxFy = fy;
                            if (minFy > fy) minFy = fy;
                        }
                    }
                    double FFx = maxFx + minFx;
                    double FFy = maxFy + minFy;
                    double Fx = FFx * c - FFy * s;
                    double Fy = FFx * s + FFy * c;
                    double f = Math.Sqrt(Fx * Fx + Fy * Fy);
                    Force rForce = new Force(vForce.ID, Fx, Fy, f);
                    rforceList.Add(rForce);
                }
            }
            #endregion

            return rforceList;
        }

        /// <summary>
        /// GetPoByID
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        public PolygonObject GetPoByID(int ID,List<PolygonObject> PoList)
        {
            PolygonObject Po = null;
            foreach (PolygonObject CachePo in PoList)
            {
                if (CachePo.ID == ID)
                {
                    Po=CachePo;
                    break;
                }
            }

            return Po;
        }

        /// <summary>
        /// 获取PoList
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        public List<PolygonObject> GetPoList(ProxiNode Node,List<PolygonObject> PoList)
        {
            List<PolygonObject> OutPoList = new List<PolygonObject>();
            for (int i = 0; i < Node.TagIds.Count; i++)
            {
                PolygonObject CachePo = this.GetPoByID(Node.TagIds[i], PoList);
                OutPoList.Add(CachePo);
            }

            return OutPoList;
        }

        /// <summary>
        /// Distance between two trinode
        /// </summary>
        /// <param name="sNode"></param>
        /// <param name="eNode"></param>
        /// <returns></returns>
        public double GetDis(ProxiNode sNode, ProxiNode eNode)
        {
            double Dis = Math.Sqrt((sNode.X - eNode.X) * (sNode.X - eNode.X) + (sNode.Y - eNode.Y) * (sNode.Y - eNode.Y));
            return Dis;
        }

        /// <summary>
        /// 计算两个建筑物的受力
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <param name="?"></param>
        /// <returns></returns>List<Force>=[sForce,eForce]
        /// ForceType=0,只考虑斥力；ForceType=1，既考虑引力，也考虑斥力;ForceType=2，只考虑邻近和MST的引力和斥力;ForceType=3 只考虑引力
        /// WeightConsi=false 不考虑权重；WeightConsi 考虑权重
        /// Adj=true 邻接；Adj=false 不邻接
        /// MaxTd 考虑的邻近条件
        public List<Force> GetForce(ProxiNode sNode, ProxiNode eNode, PolygonObject sPo1, PolygonObject ePo2, int ForceType, bool Adj, double MaxTd, bool WeigthConsi, bool MSTLable, double InterDis)
        {
            //ProxiNode tNode1 = sPo1.CalProxiNode();
            //ProxiNode tNode2 = ePo2.CalProxiNode();

            double EdgeDis = this.GetDis(sNode, eNode);
            double RSDis = sPo1.R + ePo2.R;
            List<Force> ForceList = new List<Force>();


            if (EdgeDis < RSDis)
            {
                double curForce = RSDis + 0.5 * InterDis - EdgeDis;
                double r = Math.Sqrt((eNode.Y - sNode.Y) * (eNode.Y - sNode.Y) + (eNode.X - sNode.X) * (eNode.X - sNode.X));
                double s = (eNode.Y - sNode.Y) / r;
                double c = (eNode.X - sNode.X) / r;

                if (!WeigthConsi)
                {
                    //这里将力平分给两个对象
                    double fx = 0.5 * curForce * c;
                    double fy = 0.5 * curForce * s;

                    Force eForce = new Force(eNode.ID, fx, fy, s, c, curForce * 0.5);
                    Force sForce = new Force(sNode.ID, fx * (-1), fy * (-1), s * (-1), c * (-1), curForce * 0.5);

                    ForceList.Add(sForce);
                    ForceList.Add(eForce);
                }
                else
                {
                    double w1 = ePo2.R / RSDis; double w2 = sPo1.R / RSDis;

                    Force eForce = new Force(eNode.ID, curForce * c * w1, curForce * s * w1, s, c, curForce * w1);
                    Force sForce = new Force(sNode.ID, curForce * c * w2 * (-1), curForce * s * w2 * (-1), s * (-1), c * (-1), curForce * w2);

                    ForceList.Add(sForce);
                    ForceList.Add(eForce);
                }
            }
            else if ((RSDis + InterDis) < EdgeDis && (EdgeDis - RSDis) < MaxTd)//这里需要考虑比例尺的问题
            {
                double curForce = EdgeDis - RSDis;///这里需要考虑比例尺的问题

                double r = Math.Sqrt((eNode.Y - sNode.Y) * (eNode.Y - sNode.Y) + (eNode.X - sNode.X) * (eNode.X - sNode.X));
                double s = (eNode.Y - sNode.Y) / r;
                double c = (eNode.X - sNode.X) / r;
                //这里将力平分给两个对象
                double fx = 0.5 * curForce * c;
                double fy = 0.5 * curForce * s;

                //if (ForceType == 1 && Adj && !WeigthConsi)
                if (ForceType == 1 && !WeigthConsi)
                {
                    Force eForce = new Force(eNode.ID, fx * (-1), fy * (-1), s * (-1), c * (-1), curForce * 0.5);
                    Force sForce = new Force(sNode.ID, fx, fy, s, c, curForce * 0.5);
                    ForceList.Add(sForce);
                    ForceList.Add(eForce);
                }
                //else if (ForceType == 1 && Adj && WeigthConsi)
                else if (ForceType == 1 && WeigthConsi)
                {
                    double w1 = ePo2.R / RSDis; double w2 = sPo1.R / RSDis;

                    Force eForce = new Force(eNode.ID, curForce * c * w1 * (-1), curForce * s * w1 * (-1), s * (-1), c * (-1), curForce * w1);
                    Force sForce = new Force(sNode.ID, curForce * c * w2, curForce * s * w2, s, c, curForce * w2);

                    ForceList.Add(sForce);
                    ForceList.Add(eForce);
                }
                else if (ForceType == 2 && !WeigthConsi)
                {
                    if (Adj || MSTLable)
                    {
                        Force eForce = new Force(eNode.ID, fx * (-1), fy * (-1), s * (-1), c * (-1), curForce * 0.5);
                        Force sForce = new Force(sNode.ID, fx, fy, s, c, curForce * 0.5);
                        ForceList.Add(sForce);
                        ForceList.Add(eForce);
                    }
                }
                else if (ForceType == 2 && WeigthConsi)
                {
                    if (Adj || MSTLable)
                    {
                        double w1 = ePo2.R / RSDis; double w2 = sPo1.R / RSDis;

                        Force eForce = new Force(eNode.ID, curForce * c * w1 * (-1), curForce * s * w1 * (-1), s * (-1), c * (-1), curForce * w1);
                        Force sForce = new Force(sNode.ID, curForce * c * w2, curForce * s * w2, s, c, curForce * w2);

                        ForceList.Add(sForce);
                        ForceList.Add(eForce);
                    }
                }
            }

            return ForceList;
        }

        /// <summary>
        /// 计算两个建筑物的受力
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <param name="?"></param>
        /// <returns></returns>List<Force>=[sForce,eForce]
        /// ForceType=0,只考虑斥力；ForceType=1，既考虑引力，也考虑斥力;ForceType=2，只考虑邻近和MST的引力和斥力
        /// WeightConsi=false 不考虑权重；WeightConsi 考虑权重
        /// Adj=true 邻接；Adj=false 不邻接
        /// MaxTd 考虑的邻近条件
        public List<Force> GetForceGroup(ProxiNode sNodeP, ProxiNode eNodeP, List<PolygonObject> sPoList1, List<PolygonObject> ePoList2, int ForceType, double MaxTd, bool WeigthConsi)
        {
            #region 获取最小力
            double MinForce = 100000; ProxiNode eNode = null; ProxiNode sNode = null ;
            for (int i = 0; i < sPoList1.Count; i++)
            {
                for (int j = 0; j < ePoList2.Count; j++)
                {
                    ProxiNode sNode1 = sPoList1[i].CalProxiNode();
                    ProxiNode eNode2 = ePoList2[j].CalProxiNode();

                    double EdgeDis = this.GetDis(sNode1, eNode2);
                    double RSDis = sPoList1[i].R + ePoList2[j].R;

                    if ((EdgeDis - RSDis) < MinForce)
                    {
                        MinForce = EdgeDis - RSDis;
                        eNode = eNode2;
                        sNode = sNode1;
                    }
                }
            }
            #endregion

            List<Force> ForceList = new List<Force>();
            if (MinForce<0)
            {
                double curForce = -MinForce;
                double r = Math.Sqrt((eNode.Y - sNode.Y) * (eNode.Y - sNode.Y) + (eNode.X - sNode.X) * (eNode.X - sNode.X));
                double s = (eNode.Y - sNode.Y) / r;
                double c = (eNode.X - sNode.X) / r;

                if (!WeigthConsi)
                {
                    //这里将力平分给两个对象
                    double fx = 0.5 * curForce * c;
                    double fy = 0.5 * curForce * s;

                    Force eForce = new Force(eNodeP.ID, fx, fy, s, c, curForce * 0.5);
                    Force sForce = new Force(sNodeP.ID, fx * (-1), fy * (-1), s * (-1), c * (-1), curForce * 0.5);

                    ForceList.Add(sForce);
                    ForceList.Add(eForce);
                }
                else
                {

                    double sRSum = 0; double eRSum = 0;
                    for (int i = 0; i < sPoList1.Count; i++)
                    {
                        sRSum = sRSum + sPoList1[i].R;
                    }

                    for (int j = 0; j < ePoList2.Count; j++)
                    {
                        eRSum = eRSum + ePoList2[j].R;
                    }
                    double w1 = eRSum / (sRSum + eRSum); double w2 = sRSum / (sRSum + eRSum);

                    Force eForce = new Force(eNodeP.ID, curForce * c * w1, curForce * s * w1, s, c, curForce * w1);
                    Force sForce = new Force(sNodeP.ID, curForce * c * w2 * (-1), curForce * s * w2 * (-1), s * (-1), c * (-1), curForce * w2);

                    ForceList.Add(sForce);
                    ForceList.Add(eForce);
                }

            }
            else if (MinForce > 0.05 && MinForce < MaxTd)//这里需要考虑比例尺的问题
            {
                double curForce = MinForce - 0.05;///这里需要考虑比例尺的问题

                double r = Math.Sqrt((eNode.Y - sNode.Y) * (eNode.Y - sNode.Y) + (eNode.X - sNode.X) * (eNode.X - sNode.X));
                double s = (eNode.Y - sNode.Y) / r;
                double c = (eNode.X - sNode.X) / r;
                //这里将力平分给两个对象
                double fx = 0.5 * curForce * c;
                double fy = 0.5 * curForce * s;

                //if (ForceType == 1 && Adj && !WeigthConsi)
                if (ForceType == 1 && !WeigthConsi)
                {
                    Force eForce = new Force(eNodeP.ID, fx * (-1), fy * (-1), s * (-1), c * (-1), curForce * 0.5);
                    Force sForce = new Force(sNodeP.ID, fx, fy, s, c, curForce * 0.5);
                    ForceList.Add(sForce);
                    ForceList.Add(eForce);
                }
                //else if (ForceType == 1 && Adj && WeigthConsi)
                else if (ForceType == 1 && WeigthConsi)
                {
                    double sRSum = 0; double eRSum = 0;
                    for (int i = 0; i < sPoList1.Count; i++)
                    {
                        sRSum = sRSum + sPoList1[i].R;
                    }

                    for (int j = 0; j < ePoList2.Count; j++)
                    {
                        eRSum = eRSum + ePoList2[j].R;
                    }
                    double w1 = eRSum / (sRSum + eRSum); double w2 = sRSum / (sRSum + eRSum);

                    Force eForce = new Force(eNodeP.ID, curForce * c * w1 * (-1), curForce * s * w1 * (-1), s * (-1), c * (-1), curForce * w1);
                    Force sForce = new Force(sNodeP.ID, curForce * c * w2, curForce * s * w2, s, c, curForce * w2);

                    ForceList.Add(sForce);
                    ForceList.Add(eForce);
                }
            }

            return ForceList;
        }

        /// <summary>
        /// 计算受力并累加到受力向量上去(对于线与点，线与线的情况)
        /// </summary>
        /// <param name="force">受力</param>
        /// <param name="forceValue">受力向量</param>
        /// <param name="p1">点1</param>
        /// <param name="p2">点2</param>
        private void Cal_Accumulate_Force_LP(double forceValue, NearestPoint p1, NearestPoint p2, int ID1, int ID2)
        {
            double r = Math.Sqrt((p2.Y - p1.Y) * (p2.Y - p1.Y) + (p2.X - p1.X) * (p2.X - p1.X));
            double s = (p2.Y - p1.Y) / r;
            double c = (p2.X - p1.X) / r;
            //这里将力仅给面对象
            //这里将力平分给两个对象
            double fx =  forceValue * c;
            double fy =  forceValue * s;
          //  Force force1 = this.GetForce(tagID1);
            Force force = this.GetForce(ID2);
            //受力数量和

            force.QanSumF +=  forceValue;


            if (force != null)
            {
                force.Fx += fx;
                force.Fy += fy;
            }
        }


        private void Cal_Accumulate_Force_LP(int ID,double forceValue, NearestPoint p1, NearestPoint p2, int ID1, int ID2)
        {
            double r = Math.Sqrt((p2.Y - p1.Y) * (p2.Y - p1.Y) + (p2.X - p1.X) * (p2.X - p1.X));
            double s = (p2.Y - p1.Y) / r;
            double c = (p2.X - p1.X) / r;
            //这里将力仅给面对象
            //这里将力平分给两个对象
            double fx = forceValue * c;
            double fy = forceValue * s;
            //  Force force1 = this.GetForce(tagID1);
            Force force = this.GetForce(ID2);
            //受力数量和

            force.QanSumF += forceValue;


            if (force != null)
            {
                force.Fx += fx;
                force.Fy+= fy;
            }
        }

        /// <summary>
        /// 计算受力并累加到受力向量上去(对于点-点或面-面的情况)
        /// 注意：此处，受力被冲突对象平分，
        /// 但如果冲突双方有等级高低或面积差别较大时，
        /// 需要考虑受力按照不同权重分配问题
        /// </summary>
        /// <param name="force">受力</param>
        /// <param name="forceValue">受力向量</param>
        /// <param name="p1">点1</param>
        /// <param name="p2">点2</param>
        private void Cal_Accumulate_Force(double forceValue, NearestPoint p1, NearestPoint p2, int ID1, int ID2)
        {
            double r =Math.Sqrt((p2.Y-p1.Y)*(p2.Y-p1.Y)+(p2.X-p1.X)*(p2.X-p1.X));
            double s = (p2.Y - p1.Y) / r;
            double c = (p2.X - p1.X) / r;
            //这里将力平分给两个对象
            double fx = 0.5 * forceValue * c;
            double fy = 0.5 * forceValue * s;
            Force force1 = this.GetForce(ID1);
            Force force2 = this.GetForce(ID2);
            //受力数量和
            force1.QanSumF += 0.5 * forceValue;
            force2.QanSumF += 0.5 * forceValue;

            if (force1 != null)
            {
                force1.Fx -=  fx;
                force1.Fy -=  fy;
            }
            if (force2 != null)
            {
                force2.Fx +=  fx;
                force2.Fy +=  fy;
            }
        }

        /// <summary>
        /// 以ID号获取对应的力
        /// </summary>
        /// <returns></returns>
        private Force GetForce(int tagID)
        {
            foreach (Force curForce in this.ForceList)
            {
                if (curForce.ID == tagID)
                {
                    return curForce;
                }
            }
            return null;
        }

        /// <summary>
        /// 初始化受力向量
        /// </summary>
        /// <param name="proxiGraph">邻近图</param>
        private void InitForceListfrmGraph(ProxiGraph proxiGraph)
        {
            Force curForce = null;
            foreach (ProxiNode curNode in proxiGraph.NodeList)
            {
                curForce = new Force(curNode.ID);
                if (curNode.FeatureType == FeatureType.PolylineType)
                {
                    curForce.IsBouldPoint = true;
                }
                this.ForceList.Add(curForce);
            }
        }

        /// <summary>
        /// 建立受力向量
        /// </summary>
        private bool MakeForceVectorfrmGraph()
        {
            int n = this.ProxiGraph.NodeList.Count;
            vector_F = new Matrix(3 * n, 1);

            double L = 0.0;
            double sin = 0.0;
            double cos = 0.0;

            //WriteForce(@"E:\map\实验数据\network", "F.txt", forceList);
            ProxiNode fromPoint = null;
            ProxiNode nextPoint = null;
            int index0 = -1;
            int index1 = -1;

            foreach (ProxiEdge curEdge in this.ProxiGraph.EdgeList)
            {

                fromPoint = curEdge.Node1;
                nextPoint = curEdge.Node2;
                index0 = fromPoint.ID;
                index1 = nextPoint.ID;

                L = ComFunLib.CalLineLength(fromPoint, nextPoint);
                sin = (nextPoint.Y - fromPoint.Y) / L;
                cos = (nextPoint.X - fromPoint.X) / L;

                vector_F[3 * index0, 0] += this.ForceList[index0].Fx;
                vector_F[3 * index0 + 1, 0] += ForceList[index0].Fy;
                vector_F[3 * index0 + 2, 0] += -1.0 * L * (ForceList[index0].Fx * sin + ForceList[index0].Fy * cos);
                vector_F[3 * index1, 0] += ForceList[index1].Fx;
                vector_F[3 * index1 + 1, 0] += ForceList[index1].Fy;
                vector_F[3 * index1 + 2, 0] += L * (ForceList[index1].Fx * sin + ForceList[index1].Fy * cos);

            }
            return true;
        }

        /// <summary>
        /// 将受力向量写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="">受力向量列表</param>
        public void Create_WriteForceVector2Shp(string filePath, string fileName, esriSRProjCS4Type prj)
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
            //ID
            IField pField1;
            IFieldEdit pFieldEdit1;
            pField1 = new FieldClass();
            pFieldEdit1 = pField1 as IFieldEdit;
            pFieldEdit1.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit1.Name_2 = "ID";
            pFieldEdit1.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField1);

            //起点
            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;
            pFieldEdit2.Name_2 = "F";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField2);

            //终点
            IField pField3;
            IFieldEdit pFieldEdit3;
            pField3 = new FieldClass();
            pFieldEdit3 = pField3 as IFieldEdit;
            pFieldEdit3.Length_2 = 30;
            pFieldEdit3.Name_2 = "Fx";
            pFieldEdit3.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField3);

  
            IField pField4;
            IFieldEdit pFieldEdit4;
            pField4 = new FieldClass();
            pFieldEdit4 = pField4 as IFieldEdit;
            pFieldEdit4.Length_2 = 30;
            pFieldEdit4.Name_2 = "Fy";
            pFieldEdit4.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField4);
    
            IField pField5;
            IFieldEdit pFieldEdit5;
            pField5 = new FieldClass();
            pFieldEdit5 = pField5 as IFieldEdit;
            pFieldEdit5.Length_2 = 30;
            pFieldEdit5.Name_2 = "SID";
            pFieldEdit5.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField5);


            #endregion

            #region 创建要素类
            IFeatureClass pFeatClass;
            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
            #endregion
            #endregion

            #region 向线层添加线要素

            //IFeatureClass featureClass = null;
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

                int n = this.ForceList.Count;
                if (n == 0)
                    return;

                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PolylineClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = null;
                    if (this.ForceList[i] == null)
                        continue;
                    Node node = null;
                    //邻近图
                    if (this.ProxiGraph != null)
                    {
                        node = this.ProxiGraph.GetNodebyID(this.ForceList[i].ID);
                    }
                    //线目标
                    else
                    {
                       node= this.Map.TriNodeList[this.ForceList[i].ID];
                    }

                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(node.X, node.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(node.X + 5*this.ForceList[i].Fx, node.Y + 5*this.ForceList[i].Fy);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                    feature.Shape = shp;
                    feature.set_Value(2, this.ForceList[i].ID);
                    feature.set_Value(3, this.ForceList[i].F);
                    feature.set_Value(4, this.ForceList[i].Fx);
                    feature.set_Value(5, this.ForceList[i].Fy);
                    feature.set_Value(6, this.ForceList[i].SID);
                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }

                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                //MessageBox.Show("异常信息" + ex.Message);
            }
            #endregion
        }

        /// <summary>
        /// 将受力向量写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="">受力向量列表</param>
        public void Create_WriteForceVector2Shp(string filePath, string fileName, esriSRProjCS4Type prj,double  k)
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
            //ID
            IField pField1;
            IFieldEdit pFieldEdit1;
            pField1 = new FieldClass();
            pFieldEdit1 = pField1 as IFieldEdit;
            pFieldEdit1.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit1.Name_2 = "ID";
            pFieldEdit1.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField1);

            //起点
            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;
            pFieldEdit2.Name_2 = "F";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField2);

            //终点
            IField pField3;
            IFieldEdit pFieldEdit3;
            pField3 = new FieldClass();
            pFieldEdit3 = pField3 as IFieldEdit;
            pFieldEdit3.Length_2 = 30;
            pFieldEdit3.Name_2 = "Fx";
            pFieldEdit3.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField3);


            IField pField4;
            IFieldEdit pFieldEdit4;
            pField4 = new FieldClass();
            pFieldEdit4 = pField4 as IFieldEdit;
            pFieldEdit4.Length_2 = 30;
            pFieldEdit4.Name_2 = "Fy";
            pFieldEdit4.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField4);

            IField pField5;
            IFieldEdit pFieldEdit5;
            pField5 = new FieldClass();
            pFieldEdit5 = pField5 as IFieldEdit;
            pFieldEdit5.Length_2 = 30;
            pFieldEdit5.Name_2 = "SID";
            pFieldEdit5.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField5);


            #endregion

            #region 创建要素类
            IFeatureClass pFeatClass;
            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
            #endregion
            #endregion

            #region 向线层添加线要素

            //IFeatureClass featureClass = null;
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

                int n = this.ForceList.Count;
                if (n == 0)
                    return;

                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PolylineClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = null;
                    if (this.ForceList[i] == null)
                        continue;
                    Node node = null;
                    //邻近图
                    if (this.ProxiGraph != null)
                    {
                        node = this.ProxiGraph.GetNodebyID(this.ForceList[i].ID);
                    }
                    //线目标
                    else
                    {
                        node = this.Map.TriNodeList[this.ForceList[i].ID];
                    }

                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(node.X, node.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(node.X + k * this.ForceList[i].Fx, node.Y + k * this.ForceList[i].Fy);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                    feature.Shape = shp;
                    feature.set_Value(2, this.ForceList[i].ID);
                    feature.set_Value(3, this.ForceList[i].F);
                    feature.set_Value(4, this.ForceList[i].Fx);
                    feature.set_Value(5, this.ForceList[i].Fy);
                    feature.set_Value(6, this.ForceList[i].SID);
                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }

                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                //MessageBox.Show("异常信息" + ex.Message);
            }
            #endregion
        }
        /// <summary>
        /// 将受力向量写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="">受力向量列表</param>
        public void Create_WriteForceVector2Shp(int times, string filePath, string fileName, esriSRProjCS4Type prj)
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
            //ID
            IField pField1;
            IFieldEdit pFieldEdit1;
            pField1 = new FieldClass();
            pFieldEdit1 = pField1 as IFieldEdit;
            pFieldEdit1.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit1.Name_2 = "ID";
            pFieldEdit1.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField1);

            //起点
            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;
            pFieldEdit2.Name_2 = "F";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField2);

            //终点
            IField pField3;
            IFieldEdit pFieldEdit3;
            pField3 = new FieldClass();
            pFieldEdit3 = pField3 as IFieldEdit;
            pFieldEdit3.Length_2 = 30;
            pFieldEdit3.Name_2 = "Fx";
            pFieldEdit3.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField3);


            IField pField4;
            IFieldEdit pFieldEdit4;
            pField4 = new FieldClass();
            pFieldEdit4 = pField4 as IFieldEdit;
            pFieldEdit4.Length_2 = 30;
            pFieldEdit4.Name_2 = "Fy";
            pFieldEdit4.Type_2 = esriFieldType.esriFieldTypeSingle;
            pFieldsEdit.AddField(pField4);

            IField pField5;
            IFieldEdit pFieldEdit5;
            pField5 = new FieldClass();
            pFieldEdit5 = pField5 as IFieldEdit;
            pFieldEdit5.Length_2 = 30;
            pFieldEdit5.Name_2 = "SID";
            pFieldEdit5.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField5);


            #endregion

            #region 创建要素类
            IFeatureClass pFeatClass;
            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
            #endregion
            #endregion

            #region 向线层添加线要素

            //IFeatureClass featureClass = null;
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

                int n = this.ForceList.Count;
                if (n == 0)
                    return;

                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PolylineClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = null;
                    if (this.ForceList[i] == null)
                        continue;
                    Node node = null;
                    //邻近图
                    if (this.ProxiGraph != null)
                    {
                        node = this.ProxiGraph.GetNodebyID(this.ForceList[i].ID);
                    }
                    //线目标
                    else
                    {
                        node = this.Map.TriNodeList[this.ForceList[i].ID];
                    }

                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(node.X, node.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(node.X + times * this.ForceList[i].Fx, node.Y + times*this.ForceList[i].Fy);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                    feature.Shape = shp;
                    feature.set_Value(2, this.ForceList[i].ID);
                    feature.set_Value(3, this.ForceList[i].F);
                    feature.set_Value(4, this.ForceList[i].Fx);
                    feature.set_Value(5, this.ForceList[i].Fy);
                    feature.set_Value(6, this.ForceList[i].SID);
                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }

                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                //MessageBox.Show("异常信息" + ex.Message);
            }
            #endregion
        }


        /// <summary>
        /// 将面写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="Skeleton_SegmentList">线列表</param>
        public void Create_WriteLakesObject2Shp(string filePath, List<PolygonObject> PolygonList, BeamsForceVector fv, string fileName, esriSRProjCS4Type prj)
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
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolygon;
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

                int n = PolygonList.Count;
                // List<Skeleton_Segment> Skeleton_SegmentList = ske.Skeleton_SegmentList;
                if (n == 0)
                    return;

                for (int i = 0; i < n; i++)
                {


                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PolygonClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = null;
                    TriNode curPoint = null;
                    if (PolygonList[i] == null)
                        continue;
                    int m = PolygonList[i].PointList.Count;

                    for (int k = 0; k < m; k++)
                    {
                        curPoint = PolygonList[i].PointList[k];
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    }
                    curPoint = PolygonList[i].PointList[0];
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    feature.Shape = shp;
                    feature.set_Value(2, PolygonList[i].ID);//编号 


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
        #endregion

        #region ForRoadNetwork-从冲突计算外力
        /// <summary>
        /// 受力向量-线对象，读文件中的力
        /// </summary>
        public BeamsForceVector(SMap map,List <ConflictBase> conflictList)
        {
               this.Map = map;
               ForceList = CalForcefrmConflicts(conflictList);
               MakeForceVectorfrmPolylineList();
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="map"></param>
        public BeamsForceVector(SMap map)
        {
            this.Map = map;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="map">地图对象</param>
        /// <param name="forceList">受力向量列表</param>
        public BeamsForceVector(SMap map,List<Force> forceList)
        {
            this.Map = map;
            this.ForceList = forceList;
        }

        /// <summary>
        /// 根据冲突计算受力
        /// </summary>
        /// <param name="conflictList">冲突列表</param>
        /// <returns>受力列表</returns>
        public List<Force> CalForcefrmConflicts(List<ConflictBase> conflictList)
        {
            bool isp=true;
            Node nearestPoint=null;
            List<VertexForce> vForceList = null;
            vForceList = new List<VertexForce>();
            double d = -1;
            double lw = -1;
            double rw = -1;
            //计算所有的VertexForce并存入数组vForceList
            foreach (ConflictBase conflict in conflictList)
            {
                Conflict_L curConflict = conflict as Conflict_L;
                if (curConflict != null)
                {
                    //以地图符号的宽度作为受力的权值
                    double w2=curConflict.Skel_arc.RightMapObj.SylWidth;                
                    double w1=curConflict.Skel_arc.LeftMapObj.SylWidth;

                    double g1 = ComFunLib.getGrade(w1);
                    double g2 =ComFunLib.getGrade(w2);

                    double w = w1 + w2;
                    double g = g1 + g2;
                    //lw = w2 / w; //左权值
                    //rw = w1 / w; //右权值 

                    lw = g1 / g; //左权值
                    rw = g2 / g; //右权值 

                    d = curConflict.DisThreshold;
                    //左边
                    if (curConflict.LeftPointList != null && curConflict.LeftPointList.Count > 0)
                    {
                        foreach (TriNode point in curConflict.LeftPointList)
                        {
                            nearestPoint = AuxStructureLib.ComFunLib.MinDisPoint2Polyline(point, curConflict.Skel_arc.PointList, out isp);//isp是否垂直
                            double dis = AuxStructureLib.ComFunLib.CalLineLength(nearestPoint, point);
                            if (dis < 0.5 * d)
                            {
                                double f = lw * (d - 2 * dis);
                                double r = Math.Sqrt((nearestPoint.Y - point.Y) * (nearestPoint.Y - point.Y) + (nearestPoint.X - point.X) * (nearestPoint.X - point.X));
                                double s = (point.Y - nearestPoint.Y) / r;
                                double c = (point.X - nearestPoint.X) / r;
                                //这里将力平分给两个对象
                                double fx = f * c;
                                double fy = f * s;
                                int ID = point.ID;
                                Force force = new Force(ID, fx, fy, s, c, f);
                                VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);
                                if (vForce == null)
                                {
                                    vForce = new VertexForce(ID);
                                    vForceList.Add(vForce);

                                }
                                vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                            }
                        }
                    }
                         //右边
                    if (curConflict.RightPointList != null&&curConflict.RightPointList.Count>0)
                    {
                        foreach (TriNode point in curConflict.RightPointList)
                        {
                            nearestPoint = AuxStructureLib.ComFunLib.MinDisPoint2Polyline(point, curConflict.Skel_arc.PointList, out isp);

                            double dis = AuxStructureLib.ComFunLib.CalLineLength(nearestPoint, point);
                            if (dis < 0.5 * d)
                            {
                                double f = rw * (d - 2 * dis);
                                double r = Math.Sqrt((nearestPoint.Y - point.Y) * (nearestPoint.Y - point.Y) + (nearestPoint.X - point.X) * (nearestPoint.X - point.X));
                                double s = (point.Y - nearestPoint.Y) / r;
                                double c = (point.X - nearestPoint.X) / r;
                                //这里将力平分给两个对象
                                double fx = f * c;
                                double fy = f * s;
                                int ID = point.ID;
                                Force force = new Force(ID, fx, fy,s,c ,f);
                               VertexForce  vForce =this.GetvForcebyIndex(ID, vForceList);
                               if (vForce == null)
                               {
                                   vForce = new VertexForce(ID);
                                   vForceList.Add(vForce);
                               }
                               vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                            }
                        }
                    }

                }
            }

            #region 求合力
            //先求最大力，以该力的方向为X轴方向建立局部坐标系，求四个主方向上的最大力，最后就合力
            List<Force> rforceList = new List<Force>();
            foreach (VertexForce vForce in vForceList)
            {
                if (vForce.forceList.Count == 1)//当只受一个力作用时
                {
                    rforceList.Add(vForce.forceList[0]);
                }
                else if (vForce.forceList.Count > 1)
                {
                    int index=0;
                    double maxFx = 0;
                    double minFx = 0;
                    double maxFy = 0;
                    double minFy = 0;
                    Force maxF = GetMaxForce(out  index, vForce.forceList);
                    maxFx = maxF.F;
                    double s = maxF.Sin;
                    double c = maxF.Cos;

                    for (int i = 0; i < vForce.forceList.Count; i++)
                    {
                        
                        if (i == index)
                        {  
                            Force F=vForce.forceList[i];
                            double fx = F.Fx * c + F.Fy * s;
                            double fy = F.Fy * c - F.Fx * s;

                            if (minFx > fx) minFx = fx;
                            if (maxFy < fy) maxFy = fy;
                            if (minFy > fy) minFy = fy;
                        }
                    }
                    double FFx=maxFx+minFx;
                    double FFy=maxFy+minFy;
                    double Fx=FFx*c-FFy*s;
                    double Fy=FFx*s+FFy*c;
                    double f=Math.Sqrt(Fx*Fx+Fy*Fy);
                    Force rForce = new Force(vForce.ID, Fx, Fy, f);
                    rforceList.Add(rForce);
                }
            }

            #endregion 
            return rforceList;
        }

        /// <summary>
        /// 根据冲突计算受力
        /// </summary>
        /// <param name="conflictList">冲突列表</param>
        /// <returns>受力列表</returns>
        public List<Force> CalInitDisVectorsfrmConflicts(List<ConflictBase> conflictList)
        {
            bool isp = true;
            Node nearestPoint = null;
            List<VertexForce> vForceList = null;
            vForceList = new List<VertexForce>();
            double d = -1;
            double lw = -1;
            double rw = -1;
            //计算所有的VertexForce并存入数组vForceList
            foreach (ConflictBase conflict in conflictList)
            {
                Conflict_L curConflict = conflict as Conflict_L;

                if (curConflict != null)
                {
                    //以地图符号的宽度作为受力的权值
                    double w1 = curConflict.Skel_arc.LeftMapObj.SylWidth;
                    double w2 = curConflict.Skel_arc.RightMapObj.SylWidth;
                    double w = w1 + w2;
                    lw = w2 / w; //左权值
                    rw = w1 / w; //右权值
                    d = curConflict.DisThreshold;

                    //瓶颈三角形
                    List<Triangle> triList = this.FindBottle_NeckTriangle(curConflict.TriangleList);
                    //三角形序列中包含的顶点
                    List<TriNode> pointList = new List<TriNode>();
                    foreach (Triangle t in triList)
                    {
                        if (!pointList.Contains(t.point1)) pointList.Add(t.point1);
                        if (!pointList.Contains(t.point2)) pointList.Add(t.point2);
                        if (!pointList.Contains(t.point3)) pointList.Add(t.point3);
                    }
                    List<TriNode> removeRange = null;          
                    //左边
                    if (curConflict.LeftPointList != null && curConflict.LeftPointList.Count > 0)
                    {

                        removeRange = new List<TriNode>();

                        foreach (TriNode p in curConflict.LeftPointList)
                        {
                            if (!pointList.Contains(p))
                                removeRange.Add(p);

                        }
                        foreach (TriNode p in removeRange)
                        {
                            curConflict.LeftPointList.Remove(p);
                        }

                        foreach (TriNode point in curConflict.LeftPointList)
                        {
                            nearestPoint = AuxStructureLib.ComFunLib.MinDisPoint2Polyline(point, curConflict.Skel_arc.PointList, out isp);//isp是否垂直
                            double dis = AuxStructureLib.ComFunLib.CalLineLength(nearestPoint, point);
                            if (dis < 0.5 * d)
                            {
                                double f = lw * (d - 2 * dis);
                                double r = Math.Sqrt((nearestPoint.Y - point.Y) * (nearestPoint.Y - point.Y) + (nearestPoint.X - point.X) * (nearestPoint.X - point.X));
                                double s = (point.Y - nearestPoint.Y) / r;
                                double c = (point.X - nearestPoint.X) / r;
                                //这里将力平分给两个对象
                                double fx = f * c;
                                double fy = f * s;
                                int ID = point.ID;
                                Force force = new Force(ID, fx, fy, s, c, f);
                                VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);
                                if (vForce == null)
                                {
                                    vForce = new VertexForce(ID);
                                    vForceList.Add(vForce);

                                }
                                vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                            }
                        }
                    }
                    //右边
                    if (curConflict.RightPointList != null && curConflict.RightPointList.Count > 0)
                    {
                        //右边冲突点
                        removeRange = new List<TriNode>();
                        foreach (TriNode p in curConflict.RightPointList)
                        {
                            if (!pointList.Contains(p))
                                removeRange.Add(p);
                        }
                        foreach (TriNode p in removeRange)
                        {
                            curConflict.RightPointList.Remove(p);
                        }
                        foreach (TriNode point in curConflict.RightPointList)
                        {
                            nearestPoint = AuxStructureLib.ComFunLib.MinDisPoint2Polyline(point, curConflict.Skel_arc.PointList, out isp);

                            double dis = AuxStructureLib.ComFunLib.CalLineLength(nearestPoint, point);
                            if (dis < 0.5 * d)
                            {
                                double f = rw * (d - 2 * dis);
                                double r = Math.Sqrt((nearestPoint.Y - point.Y) * (nearestPoint.Y - point.Y) + (nearestPoint.X - point.X) * (nearestPoint.X - point.X));
                                double s = (point.Y - nearestPoint.Y) / r;
                                double c = (point.X - nearestPoint.X) / r;
                                //这里将力平分给两个对象
                                double fx = f * c;
                                double fy = f * s;
                                int ID = point.ID;
                                Force force = new Force(ID, fx, fy, s, c, f);
                                VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);
                                if (vForce == null)
                                {
                                    vForce = new VertexForce(ID);
                                    vForceList.Add(vForce);
                                }
                                vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                            }
                        }
                    }
                }
            }

            #region 求合力
            //先求最大力，以该力的方向为X轴方向建立局部坐标系，求四个主方向上的最大力，最后就合力
            List<Force> rforceList = new List<Force>();
            foreach (VertexForce vForce in vForceList)
            {
                if (vForce.forceList.Count == 1)//当只受一个力作用时
                {
                    rforceList.Add(vForce.forceList[0]);
                }
                else if (vForce.forceList.Count > 1)
                {
                    int index = 0;
                    double maxFx = 0;
                    double minFx = 0;
                    double maxFy = 0;
                    double minFy = 0;
                    Force maxF = GetMaxForce(out  index, vForce.forceList);
                    maxFx = maxF.F;
                    double s = maxF.Sin;
                    double c = maxF.Cos;

                    for (int i = 0; i < vForce.forceList.Count; i++)
                    {

                        if (i == index)
                        {
                            Force F = vForce.forceList[i];
                            double fx = F.Fx * c + F.Fy * s;
                            double fy = F.Fy * c - F.Fx * s;

                            if (minFx > fx) minFx = fx;
                            if (maxFy < fy) maxFy = fy;
                            if (minFy > fy) minFy = fy;
                        }
                    }
                    double FFx = maxFx + minFx;
                    double FFy = maxFy + minFy;
                    double Fx = FFx * c - FFy * s;
                    double Fy = FFx * s + FFy * c;
                    double f = Math.Sqrt(Fx * Fx + Fy * Fy);
                    Force rForce = new Force(vForce.ID, Fx, Fy, f);
                    rforceList.Add(rForce);
                }
            }

            #endregion
            return rforceList;
        }

        /// <summary>
        /// 根据冲突计算受力
        /// </summary>
        /// <param name="conflictList">冲突列表</param>
        /// <param name="isLog">是否采用对数衰减</param>
        /// <returns>受力列表</returns>
        public List<Force> CalInitDisVectorsfrmConflicts_LinearorLog(List<ConflictBase> conflictList,bool isLog)
        {
            bool isp = true;
            Node nearestPoint = null;
            List<VertexForce> vForceList = null;
            vForceList = new List<VertexForce>();
            double d = -1;
            double lw = -1;
            double rw = -1;
            //计算所有的VertexForce并存入数组vForceList
            foreach (ConflictBase conflict in conflictList)
            {
                Conflict_L curConflict = conflict as Conflict_L;

                if (curConflict != null)
                {
                    //以地图符号的宽度作为受力的权值
                    double w1 = curConflict.Skel_arc.LeftMapObj.SylWidth;
                    double w2 = curConflict.Skel_arc.RightMapObj.SylWidth;
                    double w = w1 + w2;
                    lw = w2 / w; //左权值
                    rw = w1 / w; //右权值
                    d = curConflict.DisThreshold;

                    //瓶颈三角形
                    List<Triangle> triList = this.FindBottle_NeckTriangle(curConflict.TriangleList);
                    //三角形序列中包含的顶点
                    List<TriNode> pointList = new List<TriNode>();
                    foreach (Triangle t in triList)
                    {
                        if (!pointList.Contains(t.point1)) pointList.Add(t.point1);
                        if (!pointList.Contains(t.point2)) pointList.Add(t.point2);
                        if (!pointList.Contains(t.point3)) pointList.Add(t.point3);
                    }
                    List<TriNode> removeRange = null;

                    List<Force> forceList = new List<Force>();
                    double minDis = double.PositiveInfinity;

                    //左边
                    if (curConflict.LeftPointList != null && curConflict.LeftPointList.Count > 0)
                    {

                        removeRange = new List<TriNode>();

                        foreach (TriNode p in curConflict.LeftPointList)
                        {
                            if (!pointList.Contains(p))
                                removeRange.Add(p);

                        }
                        foreach (TriNode p in removeRange)
                        {
                            curConflict.LeftPointList.Remove(p);
                        }
                       
                        foreach (TriNode point in curConflict.LeftPointList)
                        {
                            nearestPoint = AuxStructureLib.ComFunLib.MinDisPoint2Polyline(point, (curConflict.Skel_arc.RightMapObj as PolylineObject).PointList, out isp);//isp是否垂直
                            double distance = AuxStructureLib.ComFunLib.CalLineLength(nearestPoint, point);
                            if (distance < d)
                            {
                                if (minDis > distance && distance > 0)
                                {
                                    minDis = distance;
                                }
                               // double f = lw * (d - distance); 
                                double r = Math.Sqrt((nearestPoint.Y - point.Y) * (nearestPoint.Y - point.Y) + (nearestPoint.X - point.X) * (nearestPoint.X - point.X));
                                double s = (point.Y - nearestPoint.Y) / r;
                                double c = (point.X - nearestPoint.X) / r;
                                //这里将力平分给两个对象
                                //double fx = f * c;
                                //double fy = f * s;
                                int ID = point.ID;
                                Force force = new Force(ID, 0, 0, s, c, 0);
                                force.distance = distance;
                                force.w = lw;
                                forceList.Add(force);
                                //VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);
                                //if (vForce == null)
                                //{
                                //    vForce = new VertexForce(ID);
                                //    vForceList.Add(vForce);

                                //}
                                //vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                            }
                        }
                    }
                    //右边
                    if (curConflict.RightPointList != null && curConflict.RightPointList.Count > 0)
                    {
                        //右边冲突点
                        removeRange = new List<TriNode>();
                        foreach (TriNode p in curConflict.RightPointList)
                        {
                            if (!pointList.Contains(p))
                                removeRange.Add(p);
                        }
                        foreach (TriNode p in removeRange)
                        {
                            curConflict.RightPointList.Remove(p);
                        }
                        foreach (TriNode point in curConflict.RightPointList)
                        {
                            nearestPoint = AuxStructureLib.ComFunLib.MinDisPoint2Polyline(point,(curConflict.Skel_arc.LeftMapObj as PolylineObject).PointList, out isp);

                            double distance = AuxStructureLib.ComFunLib.CalLineLength(nearestPoint, point);
                            if (distance < d)
                            {
                                if (minDis > distance && distance > 0)
                                {
                                    minDis = distance;
                                }
                                //double f = rw * (d - distance);
                                double r = Math.Sqrt((nearestPoint.Y - point.Y) * (nearestPoint.Y - point.Y) + (nearestPoint.X - point.X) * (nearestPoint.X - point.X));
                                double s = (point.Y - nearestPoint.Y) / r;
                                double c = (point.X - nearestPoint.X) / r;
                                //这里将力平分给两个对象
                                //double fx = f * c;
                                //double fy = f * s;
                                int ID = point.ID;
                                Force force = new Force(ID, 0, 0, s, c, 0);
                                force.distance = distance;
                                force.w = rw;
                                forceList.Add(force);
                                //VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);
                                //if (vForce == null)
                                //{
                                //    vForce = new VertexForce(ID);
                                //    vForceList.Add(vForce);
                                //}
                                //vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                            }
                        }
                    }

                    foreach (Force curForce in forceList)
                    {
                        if (isLog == true)
                        {
                            curForce.F = curForce.w * ((Math.Log((curForce.distance / minDis))+1) * d - curForce.distance);//线性比例函数取对数
                        }
                        else
                        {
                            curForce.F = curForce.w * ((curForce.distance / minDis) * d - curForce.distance);//线性比例函数
                        }
                        curForce.Fx = curForce.F * curForce.Cos;
                        curForce.Fy = curForce.F * curForce.Sin;
                        VertexForce vForce = this.GetvForcebyIndex(curForce.ID, vForceList);
                        if (vForce == null)
                        {
                            vForce = new VertexForce(curForce.ID);
                            vForceList.Add(vForce);
                        }
                        vForce.forceList.Add(curForce);//将当前的受力加入VertexForce数组
                    }
                }
            }

            #region 求合力
            //先求最大力，以该力的方向为X轴方向建立局部坐标系，求四个主方向上的最大力，最后就合力
            List<Force> rforceList = new List<Force>();
            foreach (VertexForce vForce in vForceList)
            {
                if (vForce.forceList.Count == 1)//当只受一个力作用时
                {
                    rforceList.Add(vForce.forceList[0]);
                }
                else if (vForce.forceList.Count > 1)
                {
                    int index = 0;
                    double maxFx = 0;
                    double minFx = 0;
                    double maxFy = 0;
                    double minFy = 0;
                    Force maxF = GetMaxForce(out  index, vForce.forceList);
                    maxFx = maxF.F;
                    double s = maxF.Sin;
                    double c = maxF.Cos;

                    for (int i = 0; i < vForce.forceList.Count; i++)
                    {

                        if (i == index)
                        {
                            Force F = vForce.forceList[i];
                            double fx = F.Fx * c + F.Fy * s;
                            double fy = F.Fy * c - F.Fx * s;

                            if (minFx > fx) minFx = fx;
                            if (maxFy < fy) maxFy = fy;
                            if (minFy > fy) minFy = fy;
                        }
                    }
                    double FFx = maxFx + minFx;
                    double FFy = maxFy + minFy;
                    double Fx = FFx * c - FFy * s;
                    double Fy = FFx * s + FFy * c;
                    double f = Math.Sqrt(Fx * Fx + Fy * Fy);
                    Force rForce = new Force(vForce.ID, Fx, Fy, f);
                    rforceList.Add(rForce);
                }
            }

            #endregion
            return rforceList;
        }
        /// <summary>
        /// 通过索引号获取受力值
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private Force GetMaxForce(out int index, List<Force> ForceList)
        {
            double MaxF = 0;
            index = 0;
            for (int i = 0; i < ForceList.Count; i++)
            {
                if (Math.Abs(ForceList[i].F) > Math.Abs(MaxF))
                {
                    index = i;
                    MaxF = ForceList[i].F;
                }
            }
            return ForceList[index];
        }
        /// <summary>
        /// 根据线对象建立受力向量
        /// </summary>
        /// <param name="polyline">线对象</param>
        public bool MakeForceVectorfrmPolylineList()
        {
            vector_F = new Matrix(3 * this.Map.TriNodeList.Count, 1);

            double L = 0.0;
            double sin = 0.0;
            double cos = 0.0;

            //WriteForce(@"E:\map\实验数据\network", "F.txt", forceList);
            Node fromPoint = null;
            Node nextPoint = null;
            int index0 = -1;
            int index1 = -1;
            foreach (PolylineObject polyline in this.Map.PolylineList)
            {
                for (int i = 0; i < polyline.PointList.Count - 1; i++)
                {
                    fromPoint = polyline.PointList[i];
                    nextPoint = polyline.PointList[i + 1];

                    index0 = fromPoint.ID;
                    index1 = nextPoint.ID;
                    //获得受力
                    Force force0 = GetForcebyIndex(index0);
                    Force force1 = GetForcebyIndex(index1);

                    L = ComFunLib.CalLineLength(fromPoint, nextPoint);
                    sin = (nextPoint.Y - fromPoint.Y) / L;
                    cos = (nextPoint.X - fromPoint.X) / L;

                    if (force0 != null)
                    {
                        //vector_F[3 * index0, 0] += force0.Fx;
                        //vector_F[3 * index0 + 1, 0] += force0.Fy;

                        //vector_F[3 * index0+ 2, 0] += -1.0 * L * (force0.Fx * sin + force0.Fy * cos);


                        vector_F[3 * index0, 0] += 0.5 * L * force0.Fx;
                        vector_F[3 * index0 + 1, 0] += 0.5 * L * force0.Fy;
                        vector_F[3 * index0 + 2, 0] += 1.0 * L * L * (force0.Fx * sin + force0.Fy * cos) / 12;

                    }

                    if (force1 != null)
                    {
                        // vector_F[3 * index1, 0] += force1.Fx;
                        // vector_F[3 * index1 + 1, 0] += force1.Fy;
                        //vector_F[3 * index1 + 2, 0] += +1.0 * L * (force1.Fx * sin + force1.Fy * cos);

                        vector_F[3 * index1, 0] += 0.5 * L * force1.Fx;
                        vector_F[3 * index1 + 1, 0] += 0.5 * L * force1.Fy;
                        vector_F[3 * index1 + 2, 0] += -1.0 * L * L * (force1.Fx * sin + force1.Fy * cos) / 12;

                    }
                }
            }
            return true;
        }

        #endregion

        #region For邻近图移位-2014-2-28
        /// <summary>
        /// 由冲突计算外力向量
        /// </summary>
        /// <param name="conflictList">冲突</param>
        /// <returns>是否成功</returns>
        public bool CreateForceVectorfrmConflict(List<ConflictBase> conflictList)
        {
            if (ProxiGraph == null || ProxiGraph.NodeList == null || ProxiGraph.EdgeList == null)
                return false;

            // InitForceListfrmGraph(ProxiGraph);//初始化受力向量
            this.ForceList= CalForceforProxiGraph(conflictList);

            if (MakeForceVectorfrmGraphNew())
                return true;
            return false;

        }

        /// <summary>
        /// 由邻近图计算外力
        /// </summary>
        /// <param name="conflictList">冲突</param>
        /// <returns>是否成功</returns>
        public bool CreateForceVectorfrmConflict_Group(List<ConflictBase> conflictList, List<GroupofMapObject> groups)
        {
            if (ProxiGraph == null || ProxiGraph.NodeList == null || ProxiGraph.EdgeList == null)
                return false;

            // InitForceListfrmGraph(ProxiGraph);//初始化受力向量
            this.ForceList = CalForceforProxiGraph_Group(conflictList, groups);

            if (MakeForceVectorfrmGraphNew())
                return true;
            return false;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool CreateForceVectorForDorling(List<PolygonObject> PoList,double MaxTd,int ForceType,bool WeigthConsi,double InterDis)
        {
            if (ProxiGraph == null || ProxiGraph.NodeList == null || ProxiGraph.EdgeList == null)
                return false;

            // InitForceListfrmGraph(ProxiGraph);//初始化受力向量
            this.ForceList = CreateForceVectorfrmGraph(PoList,MaxTd,ForceType,WeigthConsi,InterDis);//ForceList

            if (MakeForceVectorfrmGraphNew())
                return true;
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool GroupCreateForceVectorForDorling(List<PolygonObject> PoList, double MaxTd, int ForceType, bool WeigthConsi)
        {
            if (ProxiGraph == null || ProxiGraph.NodeList == null || ProxiGraph.EdgeList == null)
                return false;

            // InitForceListfrmGraph(ProxiGraph);//初始化受力向量
            this.ForceList = GroupCreateForceVectorfrmGraph(PoList, MaxTd, ForceType, WeigthConsi);//ForceList

            if (MakeForceVectorfrmGraphNew())
                return true;
            return false;
        }

        /// <summary>
        /// 计算力向量
        /// </summary>
        /// <returns></returns>
     public bool MakeForceVectorfrmGraphNew()
    {
        if (ProxiGraph == null || ProxiGraph.NodeList == null || ProxiGraph.EdgeList == null)
            return false;
        int n = ProxiGraph.NodeList.Count;
        vector_F = new Matrix(3 * n, 1);

        double L = 0.0;
        double sin = 0.0;
        double cos = 0.0;

        //WriteForce(@"E:\map\实验数据\network", "F.txt", forceList);
        Node fromPoint = null;
        Node nextPoint = null;
        int index0 = -1;
        int index1 = -1;
        foreach (ProxiEdge edge in ProxiGraph.EdgeList)
        {
            for (int i = 0; i < n - 1; i++)
            {
                fromPoint = edge.Node1;
                nextPoint = edge.Node2;

                index0 = fromPoint.ID;
                index1 = nextPoint.ID;
                //获得受力
                Force force0 = GetForcebyIndex(index0);
                Force force1 = GetForcebyIndex(index1);

                L = ComFunLib.CalLineLength(fromPoint, nextPoint);
                sin = (nextPoint.Y - fromPoint.Y) / L;
                cos = (nextPoint.X - fromPoint.X) / L;

                if (force0 != null)
                {
                    //vector_F[3 * index0, 0] += force0.Fx;
                    //vector_F[3 * index0 + 1, 0] += force0.Fy;

                    //vector_F[3 * index0+ 2, 0] += -1.0 * L * (force0.Fx * sin + force0.Fy * cos);


                    vector_F[3 * index0, 0] += 0.5 * L * force0.Fx;
                    vector_F[3 * index0 + 1, 0] += 0.5 * L * force0.Fy;
                    vector_F[3 * index0 + 2, 0] += 1.0 * L * L * (force0.Fx * sin + force0.Fy * cos) / 12;

                }

                if (force1 != null)
                {
                    // vector_F[3 * index1, 0] += force1.Fx;
                    // vector_F[3 * index1 + 1, 0] += force1.Fy;
                    //vector_F[3 * index1 + 2, 0] += +1.0 * L * (force1.Fx * sin + force1.Fy * cos);

                    vector_F[3 * index1, 0] += 0.5 * L * force1.Fx;
                    vector_F[3 * index1 + 1, 0] += 0.5 * L * force1.Fy;
                    vector_F[3 * index1 + 2, 0] += -1.0 * L * L * (force1.Fx * sin + force1.Fy * cos) / 12;

                }
            }
        }
        return true;
    }
        /// <summary>
        /// 计算邻近图上个点的最终受力-最大力做主方向的局部最大值法
        /// </summary>
        /// <returns></returns>
        private List<Force> CalForceforProxiGraph(List<ConflictBase> conflictList)
        {
            #region 计算每个点的各受力分量
            List<VertexForce> vForceList = new List<VertexForce>();
            foreach (ConflictBase conflict in conflictList)
            {
                Conflict_R curConflict = conflict as Conflict_R;
                double d = curConflict.DisThreshold;
                if (curConflict.Type == "RR")
                {
                    #region 暂时仅考虑面与面
                    PolygonObject leftObject = curConflict.Skel_arc.LeftMapObj as PolygonObject;
                    PolygonObject RightObject = curConflict.Skel_arc.RightMapObj as PolygonObject;
                    double larea = leftObject.Area;
                    double rarea = RightObject.Area;
                    double area = larea + rarea;
                    double lw = rarea / area;
                    double rw = larea / area;
                    //左边受力
                    double f = lw * (d - curConflict.Distance);
                    double r = Math.Sqrt((curConflict.LeftPoint.Y - curConflict.RightPoint.Y) * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) + (curConflict.LeftPoint.X - curConflict.RightPoint.X) * (curConflict.LeftPoint.X - curConflict.RightPoint.X));
                    double s = (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) / r;
                    double c = (curConflict.LeftPoint.X - curConflict.RightPoint.X) / r;
                    //这里将力平分给两个对象
                    double fx = f * c;
                    double fy = f * s;
                    int ID = curConflict.LeftPoint.ID;
                    Force force = new Force(ID, fx, fy,s,c,f);
                    VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);
                    if (vForce == null)
                    {
                        vForce = new VertexForce(ID);
                        vForceList.Add(vForce);
                    }
                    vForce.forceList.Add(force);//将当前的受力加入VertexForce数组

                    //右边
                    f = rw * (d - curConflict.Distance);
                    s = -1 * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) / r;
                    c = -1 * (curConflict.LeftPoint.X - curConflict.RightPoint.X) / r;
                    //这里将力平分给两个对象
                    fx = f * c;
                    fy = f * s;
                    ID = curConflict.RightPoint.ID;
                    force = new Force(ID, fx, fy, s, c, f);
                    vForce = this.GetvForcebyIndex(ID, vForceList);
                    if (vForce == null)
                    {
                        vForce = new VertexForce(ID);
                        vForceList.Add(vForce);
                    }
                    vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                    #endregion
                }
                else if (curConflict.Type == "RL")
                {
                    if (curConflict.Skel_arc.LeftMapObj.FeatureType == FeatureType.PolylineType)//线在左边
                    {
                        double f = d - curConflict.Distance;
                        double r = Math.Sqrt((curConflict.LeftPoint.Y - curConflict.RightPoint.Y) * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) + (curConflict.LeftPoint.X - curConflict.RightPoint.X) * (curConflict.LeftPoint.X - curConflict.RightPoint.X));
                        double s = -1 * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) / r;
                        double c = -1 * (curConflict.LeftPoint.X - curConflict.RightPoint.X) / r;
                        //这里将力平分给两个对象
                        double fx = f * c;
                        double fy = f * s;
                        int ID = curConflict.RightPoint.ID;
                        Force force = new Force(ID, fx, fy, s, c, f);
                        VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);

                        if (vForce == null)
                        {
                            vForce = new VertexForce(ID);
                            vForceList.Add(vForce);
                        }
                        vForce.forceList.Add(force);//将当前的受力加入VertexForce数组


                        //线上的边界点
                        f = 0;
                        s = 0;
                        c = 0;
                        //这里将力平分给两个对象
                        fx = 0;
                        fy = 0;
                        ID = curConflict.LeftPoint.ID;
                        force = new Force(ID, fx, fy, f);
                        force.IsBouldPoint = true;
                        vForce = this.GetvForcebyIndex(ID, vForceList);
                        if (vForce == null)
                        {
                            vForce = new VertexForce(ID);
                            vForceList.Add(vForce);
                        }
                        vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                    }
                    else if (curConflict.Skel_arc.RightMapObj.FeatureType == FeatureType.PolylineType)
                    {
                        double f = d - curConflict.Distance;
                        double r = Math.Sqrt((curConflict.LeftPoint.Y - curConflict.RightPoint.Y) * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) + (curConflict.LeftPoint.X - curConflict.RightPoint.X) * (curConflict.LeftPoint.X - curConflict.RightPoint.X));
                        double s = (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) / r;
                        double c = (curConflict.LeftPoint.X - curConflict.RightPoint.X) / r;
                        //这里将力平分给两个对象
                        double fx = f * c;
                        double fy = f * s;
                        int ID = curConflict.LeftPoint.ID;
                        Force force = new Force(ID, fx, fy, s, c, f);
                        VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);
                        if (vForce == null)
                        {
                            vForce = new VertexForce(ID);
                            vForceList.Add(vForce);
                        }
                        vForce.forceList.Add(force);//将当前的受力加入VertexForce数组


                        //线上的边界点
                        f = 0;
                        s = 0;
                        c = 0;
                        //这里将力平分给两个对象
                        fx = 0;
                        fy = 0;
                        ID = curConflict.RightPoint.ID;
                        force = new Force(ID, fx, fy,f);
                        force.IsBouldPoint = true;
                        vForce = this.GetvForcebyIndex(ID, vForceList);
                        if (vForce == null)
                        {
                            vForce = new VertexForce(ID);
                            vForceList.Add(vForce);
                        }
                        vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                    }
                }
            }
            #endregion

            #region  求吸引力-2014-3-20
            if (this.isDragForce == true)
            {
                int n = this.ProxiGraph.NodeList.Count;
                for (int i = 0; i < n; i++)
                {

                    ProxiNode curNode = this.ProxiGraph.NodeList[i];

                    int id = curNode.ID;
                    //  int tagID = curNode.TagID;
                    FeatureType type = curNode.FeatureType;
                    ProxiNode originalNode = this.OrigialProxiGraph.GetNodebyID(id);
                    if (originalNode == null)
                    {
                        continue;
                    }
                    double distance = ComFunLib.CalLineLength(curNode, originalNode);
                    if (distance > RMSE && (type != FeatureType.PolylineType))
                    {
                        //右边
                        double f = distance - RMSE;
                        double s = (originalNode.Y - curNode.Y) / distance;
                        double c = (originalNode.X - curNode.X) / distance;
                        //这里将力平分给两个对象
                        double fx = f * c;
                        double fy = f * s;
                        Force force = new Force(id, fx, fy, s, c, f);
                        VertexForce vForce = this.GetvForcebyIndex(id, vForceList);
                        if (vForce == null)
                        {
                            vForce = new VertexForce(id);
                            vForceList.Add(vForce);
                        }
                        vForce.forceList.Add(force);
                    }
                }
                //foreach(Node cur Node )
            }
            #endregion

            #region 求合力
            //先求最大力，以该力的方向为X轴方向建立局部坐标系，求四个主方向上的最大力，最后就合力

            List<Force> rforceList = new List<Force>();
            foreach (VertexForce vForce in vForceList)
            {
                if (vForce.forceList.Count == 1)//当只受一个力作用时
                {
                    rforceList.Add(vForce.forceList[0]);

                }
                else if (vForce.forceList.Count > 1)
                {
                    int index = 0;
                    double maxFx = 0;
                    double minFx = 0;
                    double maxFy = 0;
                    double minFy = 0;
                    Force maxF = GetMaxForce(out  index, vForce.forceList);
                    maxFx = maxF.F;
                    double s = maxF.Sin;
                    double c = maxF.Cos;

                    for (int i = 0; i < vForce.forceList.Count; i++)
                    {

                        if (i != index)
                        {
                            Force F = vForce.forceList[i];
                            double fx = F.Fx * c + F.Fy * s;
                            double fy = F.Fy * c - F.Fx * s;

                            if (minFx > fx) minFx = fx;
                            if (maxFy < fy) maxFy = fy;
                            if (minFy > fy) minFy = fy;
                        }
                    }
                    double FFx = maxFx + minFx;
                    double FFy = maxFy + minFy;
                    double Fx = FFx * c - FFy * s;
                    double Fy = FFx * s + FFy * c;
                    double f = Math.Sqrt(Fx * Fx + Fy * Fy);
                    Force rForce = new Force(vForce.ID, Fx, Fy, f);
                    rforceList.Add(rForce);
                }
            }

            #endregion
            return rforceList;
        }

        /// <summary>
        /// 考虑分组
        /// </summary>
        /// <param name="conflictList">冲突</param>
        /// <param name="groups">分组</param>
        /// <returns></returns>
        private List<Force> CalForceforProxiGraph_Group(List<ConflictBase> conflictList,List<GroupofMapObject> groups)
        {
            #region 计算每个点的各受力分量
            List<VertexForce> vForceList = new List<VertexForce>();
            foreach (ConflictBase conflict in conflictList)
            {
                Conflict_R curConflict = conflict as Conflict_R;
                double d = curConflict.DisThreshold;
                if (curConflict.Type == "RR")
                {
                    #region 暂时仅考虑面与面
                    PolygonObject leftObject = curConflict.Skel_arc.LeftMapObj as PolygonObject;
                    PolygonObject RightObject = curConflict.Skel_arc.RightMapObj as PolygonObject;

                    double larea = 0;
                    double rarea = 0;
                    double area = 0;
                    double lw = 0;
                    double rw = 0;

                    int leftTagID = -1;
                    FeatureType lefttype = FeatureType.Unknown;

                    int rightTagID = -1;
                    FeatureType righttype = FeatureType.Unknown;

                    GroupofMapObject leftGroup = GroupofMapObject.GetGroup(leftObject, groups);
                    GroupofMapObject rightGroup = GroupofMapObject.GetGroup(RightObject, groups);
                    if (leftGroup != null)
                    {
                        larea = leftGroup.Area;
                        leftTagID = leftGroup.ID;
                        lefttype = FeatureType.Group; ;
                    }
                    else
                    {
                        larea = leftObject.Area;
                        leftTagID = leftObject.ID;
                        lefttype = FeatureType.PolygonType; ;
                    }

                    if (rightGroup != null)
                    {
                        rarea = rightGroup.Area;
                        rightTagID = rightGroup.ID;
                        righttype = FeatureType.Group;
                    }
                    else
                    {
                        rarea = RightObject.Area;
                        rightTagID = RightObject.ID;
                        righttype = FeatureType.PolygonType; ;
                    }
                    area = larea + rarea;
                    lw = rarea / area;
                    rw = larea / area;

                    //左边受力
                    double f = lw * (d - curConflict.Distance);
                    double r = Math.Sqrt((curConflict.LeftPoint.Y - curConflict.RightPoint.Y) * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) + (curConflict.LeftPoint.X - curConflict.RightPoint.X) * (curConflict.LeftPoint.X - curConflict.RightPoint.X));
                    double s = (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) / r;
                    double c = (curConflict.LeftPoint.X - curConflict.RightPoint.X) / r;
                    //这里将力按面积大小的反比分给两个对象
                    double fx = f * c;
                    double fy = f * s;

                    int ID = ProxiGraph.GetNodebyTagIDandType(leftTagID, lefttype).ID;

                    Force force = new Force(ID, fx, fy, s, c, f);
                    VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);
                    if (vForce == null)
                    {
                        vForce = new VertexForce(ID);
                        vForceList.Add(vForce);
                    }
                    vForce.forceList.Add(force);//将当前的受力加入VertexForce数组


                    //右边
                    f = rw * (d - curConflict.Distance);
                    s = -1 * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) / r;
                    c = -1 * (curConflict.LeftPoint.X - curConflict.RightPoint.X) / r;
                    //这里将力平分给两个对象
                    fx = f * c;
                    fy = f * s;
                    ID = ProxiGraph.GetNodebyTagIDandType(rightTagID, righttype).ID;
                    force = new Force(ID, fx, fy, s, c, f);
                    vForce = this.GetvForcebyIndex(ID, vForceList);
                    if (vForce == null)
                    {
                        vForce = new VertexForce(ID);
                        vForceList.Add(vForce);
                    }
                    vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                    #endregion
                }
                else if (curConflict.Type == "RL")
                {
                    if (curConflict.Skel_arc.LeftMapObj.FeatureType == FeatureType.PolylineType)//线在左边
                    {
                        double f = d - curConflict.Distance;
                        double r = Math.Sqrt((curConflict.LeftPoint.Y - curConflict.RightPoint.Y) * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) + (curConflict.LeftPoint.X - curConflict.RightPoint.X) * (curConflict.LeftPoint.X - curConflict.RightPoint.X));
                        double s = -1 * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) / r;
                        double c = -1 * (curConflict.LeftPoint.X - curConflict.RightPoint.X) / r;
                        //这里将力平分给两个对象
                        double fx = f * c;
                        double fy = f * s;
                        GroupofMapObject rightGroup = GroupofMapObject.GetGroup(curConflict.Skel_arc.RightMapObj, groups);
                        int rightTagID = 0;
                        FeatureType type = FeatureType.Unknown;
                        if (rightGroup != null)
                        {
                            rightTagID = rightGroup.ID;
                            type = FeatureType.Group; ;
                        }
                        else
                        {
                            rightTagID = curConflict.Skel_arc.RightMapObj.ID;
                            type = FeatureType.PolygonType;
                        }
                        int ID = ProxiGraph.GetNodebyTagIDandType(rightTagID, type).ID;
                        Force force = new Force(ID, fx, fy, s, c, f);
                        VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);

                        if (vForce == null)
                        {
                            vForce = new VertexForce(ID);
                            vForceList.Add(vForce);
                        }
                        vForce.forceList.Add(force);//将当前的受力加入VertexForce数组


                        ////线上的边界点
                        //f = 0;
                        //s = 0;
                        //c = 0;
                        ////这里将力平分给两个对象
                        //fx = 0;
                        //fy = 0;
                        //ID = curConflict.LeftPoint.TagValue;
                        //force = new Force(ID, fx, fy, f);
                        //force.IsBouldPoint = true;
                        //vForce = this.GetvForcebyIndex(ID, vForceList);
                        //if (vForce == null)
                        //{
                        //    vForce = new VertexForce(ID);
                        //    vForceList.Add(vForce);
                        //}
                        //vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                    }
                    else if (curConflict.Skel_arc.RightMapObj.FeatureType == FeatureType.PolylineType)
                    {


                        double f = d - curConflict.Distance;
                        double r = Math.Sqrt((curConflict.LeftPoint.Y - curConflict.RightPoint.Y) * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) + (curConflict.LeftPoint.X - curConflict.RightPoint.X) * (curConflict.LeftPoint.X - curConflict.RightPoint.X));
                        double s = (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) / r;
                        double c = (curConflict.LeftPoint.X - curConflict.RightPoint.X) / r;
                        //这里将力平分给两个对象
                        double fx = f * c;
                        double fy = f * s;

                        GroupofMapObject leftGroup = GroupofMapObject.GetGroup(curConflict.Skel_arc.LeftMapObj, groups);
                        int leftTagID = 0;
                        FeatureType type = FeatureType.Unknown;
                        if (leftGroup != null)
                        {
                            leftTagID = leftGroup.ID;
                            type = FeatureType.Group; ;
                        }
                        else
                        {
                            leftTagID = curConflict.Skel_arc.LeftMapObj.ID;
                            type = FeatureType.PolygonType;
                        }
  
                        int ID = ProxiGraph.GetNodebyTagIDandType(leftTagID, type).ID;
                        Force force = new Force(ID, fx, fy, s, c, f);
                        VertexForce vForce = this.GetvForcebyIndex(ID, vForceList);
                        if (vForce == null)
                        {
                            vForce = new VertexForce(ID);
                            vForceList.Add(vForce);
                        }
                        vForce.forceList.Add(force);//将当前的受力加入VertexForce数组


                        ////线上的边界点
                        //f = 0;
                        //s = 0;
                        //c = 0;
                        ////这里将力平分给两个对象
                        //fx = 0;
                        //fy = 0;
                        //ID = curConflict.RightPoint.ID;
                        //force = new Force(ID, fx, fy, f);
                        //force.IsBouldPoint = true;
                        //vForce = this.GetvForcebyIndex(ID, vForceList);
                        //if (vForce == null)
                        //{
                        //    vForce = new VertexForce(ID);
                        //    vForceList.Add(vForce);
                        //}
                        //vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                    }
                }
            }



            #region 边界上的点
            foreach (ProxiNode curNode in this.ProxiGraph.NodeList)
            {
                if (curNode.FeatureType == FeatureType.PolylineType)
                {
                    Force force = new Force(curNode.ID, 0, 0, 0);
                    force.IsBouldPoint = true;
                    VertexForce vForce = this.GetvForcebyIndex(curNode.ID, vForceList);
                    if (vForce == null)
                    {
                        vForce = new VertexForce(curNode.ID);
                        vForceList.Add(vForce);
                    }
                    vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                }
            }
            #endregion

            #endregion

            #region  求吸引力-2014-3-20
            if (this.isDragForce == true)
            {
                int n = this.ProxiGraph.NodeList.Count;
                for (int i = 0; i < n; i++)
                {

                    ProxiNode curNode = this.ProxiGraph.NodeList[i];

                    int id = curNode.ID;
                    //  int tagID = curNode.TagID;
                    FeatureType type = curNode.FeatureType;
                    ProxiNode originalNode = this.OrigialProxiGraph.GetNodebyID(id);
                    if (originalNode == null)
                    {
                        continue;
                    }
                    double distance = ComFunLib.CalLineLength(curNode, originalNode);
                    if (distance > RMSE && (type != FeatureType.PolylineType))
                    {
                        //右边
                        double f = distance - RMSE;
                        double s = (originalNode.Y - curNode.Y) / distance;
                        double c = (originalNode.X - curNode.X) / distance;
                        //这里将力平分给两个对象
                        double fx = f * c;
                        double fy = f * s;
                        Force force = new Force(id, fx, fy, s, c, f);
                        VertexForce vForce = this.GetvForcebyIndex(id, vForceList);
                        if (vForce == null)
                        {
                            vForce = new VertexForce(id);
                            vForceList.Add(vForce);
                        }
                        vForce.forceList.Add(force);
                    }
                }
                //foreach(Node cur Node )
            }
            #endregion

            #region 求合力
            //先求最大力，以该力的方向为X轴方向建立局部坐标系，求四个主方向上的最大力，最后就合力

            List<Force> rforceList = new List<Force>();
            foreach (VertexForce vForce in vForceList)
            {
                if (vForce.forceList.Count == 1)//当只受一个力作用时
                {
                    rforceList.Add(vForce.forceList[0]);

                }
                else if (vForce.forceList.Count > 1)
                {
                    int index = 0;
                    double maxFx = 0;
                    double minFx = 0;
                    double maxFy = 0;
                    double minFy = 0;
                    Force maxF = GetMaxForce(out  index, vForce.forceList);
                    maxFx = maxF.F;
                    double s = maxF.Sin;
                    double c = maxF.Cos;

                    for (int i = 0; i < vForce.forceList.Count; i++)
                    {

                        if (i != index)
                        {
                            Force F = vForce.forceList[i];
                            double fx = F.Fx * c + F.Fy * s;
                            double fy = F.Fy * c - F.Fx * s;

                            if (minFx > fx) minFx = fx;
                            if (maxFy < fy) maxFy = fy;
                            if (minFy > fy) minFy = fy;
                        }
                    }
                    double FFx = maxFx + minFx;
                    double FFy = maxFy + minFy;
                    double Fx = FFx * c - FFy * s;
                    double Fy = FFx * s + FFy * c;
                    double f = Math.Sqrt(Fx * Fx + Fy * Fy);
                    Force rForce = new Force(vForce.ID, Fx, Fy, f);
                    rforceList.Add(rForce);
                }
            }

            #endregion
            return rforceList;
        }
        #endregion

        /// <summary>
        /// 通过索引号获取受力值
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Force GetForcebyIndex(int index)
        {
            foreach (Force curF in this.ForceList)
            {
                if (curF.ID == index)
                    return curF;
            }
            return null;
        }

        /// <summary>
        /// 通过索引号获取受力值
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private VertexForce GetvForcebyIndex(int index, List<VertexForce> vForceList)
        {
            foreach (VertexForce curvF in vForceList)
            {
                if (curvF.ID == index)
                    return curvF;
            }
            return null;
        }

        /// <summary>
        /// 找到瓶颈三角形
        /// </summary>
        /// <param name="TriList">三角形列表</param>
        /// <returns>返回一个新的三角形列表</returns>
        public  List<Triangle> FindBottle_NeckTriangle(List<Triangle> TriList)
        {
            if(TriList==null||TriList.Count==0)
                return null;
            List<Triangle> triList = new List<Triangle>();
            if (TriList.Count == 1)
            {
                return TriList;
            }
              
            else if (TriList.Count == 2)
            {
               // List<Triangle> triList = new List<Triangle>();
                if (TriList[0].W > TriList[1].W)
                {
                    //List<Triangle> triList = new List<Triangle>();
                    triList.Add(TriList[1]);
                    return triList;
                }
                else
                {
                    //List<Triangle> triList = new List<Triangle>();
                    triList.Add(TriList[0]);
                    return triList;
                }
            }
            else if(TriList.Count >=3)
            {
                
                int n = TriList.Count;
                if (TriList[0].W < TriList[1].W)
                {
                    triList.Add(TriList[0]);
                }
                for (int i = 1; i < n - 1; i++)
                {
                    if (TriList[i - 1].W >= TriList[i].W && TriList[i + 1].W >= TriList[i].W)
                    {
                        triList.Add(TriList[i]);
                    }
                }
                if (TriList[n-1].W < TriList[n-2].W)
                {
                    triList.Add(TriList[n - 1]);
                }
            }
            return triList;
        }

        /// <summary>
        /// 获得子图网络的受力向量
        /// </summary>
        /// <param name="subNetwork">子图网络</param>
        /// <returns>受力向量</returns>
        public BeamsForceVector GetSubNetForceVector(SMap subNetwork)
        {
            if (this.ForceList == null || this.ForceList.Count == 0)
                return null;
            List<Force> forceList = new List<Force>();
            foreach (Force cuf in this.ForceList)
            {
                int indexInMap = cuf.ID;
                TriNode pInMap = this.Map.TriNodeList[indexInMap];
                
                int indexInSubNetwork = subNetwork.GetIndexofVertexbyX_Y(pInMap, 0.000001f);
                if (indexInSubNetwork == -1)//没找到的情况
                {
                    continue;
                }
                Force forceInSubNetwork =new Force(cuf);
                forceInSubNetwork.ID=indexInSubNetwork;
                forceList.Add(forceInSubNetwork);
            }
            BeamsForceVector subNetForceVector=new BeamsForceVector(subNetwork,forceList);
            return subNetForceVector;
        }

        #region 对比试验Bader
        /// <summary>
        /// 由冲突计算外力向量
        /// </summary>
        /// <param name="conflictList">冲突</param>
        /// <returns>是否成功</returns>
        public bool CreateForceVectorfrmConflictforMST(List<ConflictBase> conflictList,ProxiGraph MstPg)
        {
            if (ProxiGraph == null || ProxiGraph.NodeList == null || ProxiGraph.EdgeList == null)
                return false;

            // InitForceListfrmGraph(ProxiGraph);//初始化受力向量
            this.ForceList = CalForceforProxiGraph(conflictList);

            if (MakeForceVectorfrmGraphforMST(MstPg))
                return true;
            return false;

        }


        /// <summary>
        /// 计算力向量
        /// </summary>
        /// <returns></returns>
        public bool MakeForceVectorfrmGraphforMST(ProxiGraph MstPg)
        {
            if (ProxiGraph == null || ProxiGraph.NodeList == null || ProxiGraph.EdgeList == null)
                return false;
            int n = MstPg.NodeList.Count;
            vector_F = new Matrix(3 * n, 1);

            double L = 0.0;
            double sin = 0.0;
            double cos = 0.0;

            //WriteForce(@"E:\map\实验数据\network", "F.txt", forceList);
            ProxiNode fromPoint = null;
            ProxiNode nextPoint = null;
            int index0 = -1;
            int index1 = -1;
            int tagID0 = -1;
            int tagID1 = -1;
            FeatureType type0=FeatureType.Unknown;
            FeatureType type1 = FeatureType.Unknown; 
            foreach (ProxiEdge edge in MstPg.EdgeList)
            {
                for (int i = 0; i < n - 1; i++)
                {
                    fromPoint = edge.Node1;
                    nextPoint = edge.Node2;

                    index0 = fromPoint.ID;
                    index1 = nextPoint.ID;
                    tagID0 = fromPoint.TagID;
                    tagID1 = nextPoint.TagID;
                    type0 = fromPoint.FeatureType;
                    type1 = nextPoint.FeatureType;
                    //获得受力
                    Force force0 = GetForcebyTagIDandType(tagID0, type0,this.ProxiGraph);
                    Force force1 = GetForcebyTagIDandType(tagID1, type1, this.ProxiGraph);

                    L = ComFunLib.CalLineLength(fromPoint, nextPoint);
                    sin = (nextPoint.Y - fromPoint.Y) / L;
                    cos = (nextPoint.X - fromPoint.X) / L;

                    if (force0 != null)
                    {
                        //vector_F[3 * index0, 0] += force0.Fx;
                        //vector_F[3 * index0 + 1, 0] += force0.Fy;

                        //vector_F[3 * index0+ 2, 0] += -1.0 * L * (force0.Fx * sin + force0.Fy * cos);


                        vector_F[3 * index0, 0] += 0.5 * L * force0.Fx;
                        vector_F[3 * index0 + 1, 0] += 0.5 * L * force0.Fy;
                        vector_F[3 * index0 + 2, 0] += 1.0 * L * L * (force0.Fx * sin + force0.Fy * cos) / 12;

                    }

                    if (force1 != null)
                    {
                        // vector_F[3 * index1, 0] += force1.Fx;
                        // vector_F[3 * index1 + 1, 0] += force1.Fy;
                        //vector_F[3 * index1 + 2, 0] += +1.0 * L * (force1.Fx * sin + force1.Fy * cos);

                        vector_F[3 * index1, 0] += 0.5 * L * force1.Fx;
                        vector_F[3 * index1 + 1, 0] += 0.5 * L * force1.Fy;
                        vector_F[3 * index1 + 2, 0] += -1.0 * L * L * (force1.Fx * sin + force1.Fy * cos) / 12;

                    }
                }
            }
            return true;
        }
        /// <summary>
        /// 通过TagID和Type获取Force值
        /// </summary>
        /// <param name="tagID"></param>
        /// <param name="type"></param>
        /// <param name="pg"></param>
        /// <returns></returns>
        public Force GetForcebyTagIDandType(int tagID,FeatureType type,ProxiGraph pg)
        {

            ProxiNode node = pg.GetNodebyTagIDandType(tagID, type);
            if (node == null || node.ID == -1)
                return null;

            return this.GetForcebyIndex(node.ID);
        }

        /// <summary>
        /// 由冲突计算外力向量
        /// </summary>
        /// <param name="conflictList">冲突</param>
        /// <returns>是否成功</returns>
        public bool CreateForceVectorfrmConflictforBader(List<ConflictBase> conflictList,ProxiGraph mstPg)
        {
            if (ProxiGraph == null || ProxiGraph.NodeList == null || ProxiGraph.EdgeList == null)
                return false;

            // InitForceListfrmGraph(ProxiGraph);//初始化受力向量
            this.ForceList = CalForceforProxiGraph(conflictList);

            if (this.MakeForceVectorfrmGraphforMST(mstPg))
                return true;
            return false;

        }
        #endregion
    }
}
