using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatrixOperation;
using AuxStructureLib;
using AuxStructureLib.IO;
using System.Data;
using AuxStructureLib.ConflictLib;

namespace AlgEMLib
{
    /// <summary>
    /// Snakes受力向量计算类
    /// </summary>
    public class SnakesForceVector : ForceVectorBase
    {
        private Matrix _Fx = null;                                      //X方向受力向量
        private Matrix _Fy = null;                                     //Y方向受力向量

        public Matrix Fx { get { return _Fx; } }
        public Matrix Fy { get { return _Fy; } }

        #region 单条线的移位
        /// <summary>
        /// 受力向量
        /// </summary>
        public SnakesForceVector(PolylineObject polyline, string forcefile)
        {
            ForceList = ReadForceListfrmFile(forcefile);
            MakeForceVectorfrmPolyline(polyline);
        }

        /// <summary>
        /// 受力向量
        /// </summary>
        public SnakesForceVector(PolylineObject polyline)
        {
            MakeForceVectorfrmPolyline0(polyline);
        }
        /// <summary>
        /// 创建受力向量，初始化为0
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public bool MakeForceVectorfrmPolyline0(PolylineObject polyline)
        {
            int n = polyline.PointList.Count;
            _Fx = new Matrix(2 * n, 1);
            _Fy = new Matrix(2 * n, 1);

            return true;
        }
        /// <summary>
        /// 根据线对象建立受力向量
        /// </summary>
        /// <param name="polyline">线对象</param>
        public bool MakeForceVectorfrmPolyline(PolylineObject polyline)
        {
            int n = polyline.PointList.Count;
            _Fx = new Matrix(2 * n, 1);
            _Fy = new Matrix(2 * n, 1);
            double h = 0.0;
            //forceList = GetForce();//求受力
            //调试==手动添加力
            //SetForceVectorEle(5, 1330 * 1000 / AlgSnakes.scale, -1590 * 1000 / AlgSnakes.scale);
            if (!IsHasForce(this.ForceList))
            {
                return false;
            }
            //WriteForce(@"E:\map\实验数据\network", "F.txt", forceList);
            Node fromPoint = null;
            Node nextPoint = null;
            int index0 = -1;
            int index1 = -1;
            for (int i = 0; i < n - 1; i++)
            {
                fromPoint = polyline.PointList[i];
                nextPoint = polyline.PointList[i + 1];

                index0 = fromPoint.ID;
                index1 = nextPoint.ID;
                //如如果仅有一条线，可值按点序列顺序
                //index0 =i;
                // index1 =i+1;
                h = ComFunLib.CalLineLength(fromPoint, nextPoint);

                //获得受力
                Force force0 = GetForcebyIndex(index0);
                Force force1 = GetForcebyIndex(index1);

                if (force0 != null)
                {
                    _Fx[2 * index0, 0] += 0.5 * h * force0.Fx;
                    _Fx[2 * index0 + 1, 0] += (1.0 / 12) * h * h * force0.Fx;
                    _Fy[2 * index0, 0] += 0.5 * h * force0.Fy;
                    _Fy[2 * index0 + 1, 0] += (1.0 / 12) * h * h * force0.Fy;
                }

                if (force1 != null)
                {
                    _Fx[2 * index1, 0] += 0.5 * h * force1.Fx;
                    _Fx[2 * index1 + 1, 0] += -(1.0 / 12) * h * h * force1.Fx;
                    _Fy[2 * index1, 0] += 0.5 * h * force1.Fy;
                    _Fy[2 * index1 + 1, 0] += -(1.0 / 12) * h * h * force1.Fy;

                }

            }
            return true;
        }
        #endregion

        #region ForRoadNetwork-从冲突计算外力
        /// <summary>
        /// 受力向量-线对象，读文件中的力
        /// </summary>
        public SnakesForceVector(SMap map, List<ConflictBase> conflictList)
        {
            this.Map = map;
            ForceList = CalForcefrmConflicts(conflictList);
            MakeForceVectorfrmPolylineList();
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="map"></param>
        public SnakesForceVector(SMap map)
        {
            this.Map = map;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="map">地图对象</param>
        /// <param name="forceList">受力向量列表</param>
        public SnakesForceVector(SMap map, List<Force> forceList)
        {
            this.Map = map;
            this.ForceList = forceList;
        }
        /// <summary>
        /// 根据线对象建立受力向量
        /// </summary>
        /// <param name="polyline">线对象</param>
        public bool MakeForceVectorfrmPolylineList()
        {
            int n = this.Map.TriNodeList.Count;
            _Fx = new Matrix(2 * n, 1);
            _Fy = new Matrix(2 * n, 1);
            double h = 0.0;

            if (!IsHasForce(this.ForceList))
            {
                return false;
            }

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
                    //如如果仅有一条线，可值按点序列顺序
                    //index0 =i;
                    // index1 =i+1;
                    h = ComFunLib.CalLineLength(fromPoint, nextPoint);

                    //获得受力
                    Force force0 = GetForcebyIndex(index0);
                    Force force1 = GetForcebyIndex(index1);

                    if (force0 != null)
                    {
                        _Fx[2 * index0, 0] += 0.5 * h * force0.Fx;
                        _Fx[2 * index0 + 1, 0] += (1.0 / 12) * h * h * force0.Fx;
                        _Fy[2 * index0, 0] += 0.5 * h * force0.Fy;
                        _Fy[2 * index0 + 1, 0] += (1.0 / 12) * h * h * force0.Fy;
                    }

                    if (force1 != null)
                    {
                        _Fx[2 * index1, 0] += 0.5 * h * force1.Fx;
                        _Fx[2 * index1 + 1, 0] += -(1.0 / 12) * h * h * force1.Fx;
                        _Fy[2 * index1, 0] += 0.5 * h * force1.Fy;
                        _Fy[2 * index1 + 1, 0] += -(1.0 / 12) * h * h * force1.Fy;
                    }
                }
               
            }
            return true;

        }

        /// <summary>
        /// 获得子图网络的受力向量
        /// </summary>
        /// <param name="subNetwork">子图网络</param>
        /// <returns>受力向量</returns>
        public SnakesForceVector GetSubNetForceVector(SMap subNetwork)
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
                Force forceInSubNetwork = new Force(cuf);
                forceInSubNetwork.ID = indexInSubNetwork;
                forceList.Add(forceInSubNetwork);
            }
            SnakesForceVector subNetForceVector = new SnakesForceVector(subNetwork, forceList);
            return subNetForceVector;
        }

        #endregion

        #region For邻近图移位-2014-2-28
        
        /// <summary>
        /// 受力向量-邻近图
        /// </summary>
        /// <param name="proxiGraph"></param>
        public SnakesForceVector(ProxiGraph proxiGraph)
        {
            ProxiGraph = proxiGraph;
            ForceList = new List<Force>();
        }

        /// <summary>
        /// 由冲突计算外力向量-不分组
        /// </summary>
        /// <param name="conflictList">冲突</param>
        /// <returns>是否成功</returns>
        public bool CreateForceVectorfrmConflict(List<ConflictBase> conflictList)
        {
            if (ProxiGraph == null || ProxiGraph.NodeList == null || ProxiGraph.EdgeList == null)
                return false;

            // InitForceListfrmGraph(ProxiGraph);//初始化受力向量
            this.ForceList = CalForceforProxiGraph(conflictList);

            if (MakeForceVectorfrmGraphNew())
                return true;
            return false;

        }

        /// <summary>
        /// 由冲突计算外力向量-不分组
        /// </summary>
        /// <param name="conflictList">冲突</param>
        /// <returns>是否成功</returns>
        public bool CreateForceVectorfrm_CTP(List<ProxiNode> NodeList, List<ProxiNode> FinalLocation, double MinDis,double MaxForce)
        {
            if (ProxiGraph == null || ProxiGraph.NodeList == null || ProxiGraph.EdgeList == null)
                return false;

            // InitForceListfrmGraph(ProxiGraph);//初始化受力向量
            this.ForceList = CalForceforProxiGraph_CTP(NodeList, FinalLocation, MinDis, MaxForce);

            if (MakeForceVectorfrmGraphNew())
                return true;
            return false;

        }

        /// <summary>
        /// 由冲突计算外力向量-不分组
        /// </summary>
        /// <param name="conflictList">冲突</param>
        /// <returns>是否成功</returns>
        public bool CreateForceVectorfrm_HierCTP(List<ProxiNode> NodeList, List<ProxiNode> FinalLocation, double MinDis,out List<int> BoundingPoint,double MaxForce)
        {
            BoundingPoint = new List<int>();

            if (ProxiGraph == null || ProxiGraph.NodeList == null || ProxiGraph.EdgeList == null)
                return false;

            // InitForceListfrmGraph(ProxiGraph);//初始化受力向量
            this.ForceList = CalForceforProxiGraph_HierCTP(NodeList, FinalLocation, MinDis, out BoundingPoint, MaxForce);

            if (MakeForceVectorfrmGraphNew())
                return true;
            return false;

        }

        /// <summary>
        /// 由邻近图计算外力-带分组
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
        /// 计算力向量
        /// </summary>
        /// <returns></returns>
        public bool MakeForceVectorfrmGraphNew()
        {
            if (ProxiGraph == null || ProxiGraph.NodeList == null || ProxiGraph.EdgeList == null)
                return false;
            int n = ProxiGraph.NodeList.Count;
            _Fx = new Matrix(2 * n, 1);
            _Fy = new Matrix(2 * n, 1);
            double h = 0.0;
            if (!IsHasForce(this.ForceList))
            {
                return false;
            }
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
                    h = ComFunLib.CalLineLength(fromPoint, nextPoint);

                    //获得受力
                    Force force0 = GetForcebyIndex(index0);
                    Force force1 = GetForcebyIndex(index1);

                    if (force0 != null)
                    {
                        _Fx[2 * index0, 0] += 0.5 * h * force0.Fx;
                        _Fx[2 * index0 + 1, 0] += (1.0 / 12) * h * h * force0.Fx;
                        _Fy[2 * index0, 0] += 0.5 * h * force0.Fy;
                        _Fy[2 * index0 + 1, 0] += (1.0 / 12) * h * h * force0.Fy;

                    }

                    if (force1 != null)
                    {
                        _Fx[2 * index1, 0] += 0.5 * h * force1.Fx;
                        _Fx[2 * index1 + 1, 0] += -(1.0 / 12) * h * h * force1.Fx;
                        _Fy[2 * index1, 0] += 0.5 * h * force1.Fy;
                        _Fy[2 * index1 + 1, 0] += -(1.0 / 12) * h * h * force1.Fy;

                    }
                }
            }
            return true;
        }
        #endregion
    }
}
