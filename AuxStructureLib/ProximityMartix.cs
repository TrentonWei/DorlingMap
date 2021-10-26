using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AuxStructureLib
{
    /// <summary>
    /// 邻近矩阵，用于表达地图对象两两之间的邻近图，直接相邻为1级，相切或相同为0级
    /// Date：2011-7-22
    /// Author：Liuygis
    /// </summary>
    public class ProximityMatrix
    {
        /// <summary>
        /// 地图对象字段
        /// </summary>
        private SMap map = null;
        /// <summary>
        /// 邻近图字段
        /// </summary>
        private ProxiGraph proximityGraph = null;        //最小生成树
        /// <summary>
        ///邻近图属性
        /// </summary>
        public ProxiGraph ProximityGraph
        {
            get
            {
                return proximityGraph;
            }
            set
            {
                if (value != null)
                {
                    proximityGraph = value;
                }
            }
        }
        /// <summary>
        ///邻近矩阵字段
        /// </summary>
        private double[,] adjMatrix = null;  
        /// <summary>
        /// 邻近矩阵属性
        /// </summary>
        public double[,] AdjMatrix
        {
            get
            {
                return adjMatrix;
            }
            set
            {
                if (value != null)
                {
                    adjMatrix = value;
                }
            }
        }

        /// <summary>
        /// 邻近图尺寸
        /// </summary>
        public int Size
        {
            get
            {
                if (this.adjMatrix != null)
                    return this.adjMatrix.Length;
                else
                    return 0;
            }
              
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="map">地图对象</param>
        /// <param name="proximityGraph">邻近图</param>
        public ProximityMatrix(SMap map, ProxiGraph proximityGraph,bool isBoundary)
        {
            this.map = map;
            this.proximityGraph = proximityGraph;
            this.CreateAdjMartixForPartition_Polylines();
            if (isBoundary)
            {
                this.CreateAdjMartixForPartition_Boundery();
            }
        }
        /// <summary>
        /// 创建邻近图-线目标单独计算
        /// </summary>
        private void CreateAdjMartixForPartition_Polylines()
        {
            if (map == null || proximityGraph == null || map.NumberofMapObject == 0)
            {
                return;
            }

            List<MapObject> mapOList = map.MapObjectList;
            int size = mapOList.Count;
            this.adjMatrix = new double[size, size];

            //初始化矩阵值，对角线上为0，其他地方为无穷大
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                {
                    if (i == j)
                        AdjMatrix[i, j] = 0;
                    else
                    {
                        AdjMatrix[i, j] = double.PositiveInfinity;
                    }
                }

            for (int i = 0; i < size; i++)
            {
                bool[] flags = new bool[size];
                flags[i] = true;

                MapObject curMapO = mapOList[i];
                List<MapObject> curOs = new List<MapObject>();
                curOs.Add(curMapO);
                int order = 1;

                List<MapObject> neighborOs = null;

                while ((neighborOs = GetNeighbors(curOs, flags, mapOList)) != null)
                {
                    foreach (MapObject curNeighbor in neighborOs)
                    {
                        int j = curNeighbor.SomeValue;
                        AdjMatrix[i, j] = order;
                        AdjMatrix[j, i] = order;
                        flags[j] = true;

                    }
                    curOs = neighborOs;
                    order++;
                }
            }
        }

        /// <summary>
        /// 创建邻近图-线目标整体作为边界
        /// </summary>
        private void CreateAdjMartixForPartition_Boundery()
        {
            if (map == null || proximityGraph == null || map.NumberofMapObject == 0)
            {
                return;
            }

            List<MapObject> mapOList = map.MapObjectList;
            int countofPolyline = 0;
            foreach (MapObject mo in mapOList)
            {
                if (mo.FeatureType == FeatureType.PolylineType)
                    countofPolyline++;
            }

            int size = mapOList.Count - countofPolyline + 1;
            double[,] NewMatrix = new double[size, size];
            int size1 = mapOList.Count;
            for (int i = 0; i < size; i++)
            {
                double[] curRow = new double[size1 - size];
                for (int j = 0; j < size-1; j++)
                {
                    NewMatrix[i, j] = this.adjMatrix[i, j];
                }
                NewMatrix[i, size-1] = this.adjMatrix[i, size-1];
                for (int k = size-1 ; k < size1; k++)
                {
                    if (NewMatrix[i, size-1] > adjMatrix[i, k])
                    {
                        NewMatrix[i, size-1] = this.adjMatrix[i, k];
                    }
                }
            }
            NewMatrix[size-1, size-1] = 0;
            for (int i = 0; i < size; i++)
            {
                NewMatrix[size-1, i] = NewMatrix[i, size-1];
            }
            this.adjMatrix = NewMatrix;
        }
 

        /// <summary>
        ///获取mapObjects对象的1阶邻近对象，且排除flags标记为true的对象
        /// </summary>
        /// <param name="mapObjects">源对象</param>
        /// <param name="flags">标记数组</param>
       /// <returns>返回尚未搜索到的1阶邻近对象</returns>
        private List<MapObject> GetNeighbors(List<MapObject> mapObjects, bool[] flags, List<MapObject> mapOList)
        {
            List<MapObject> resNeighbors = new List<MapObject>();
            foreach (MapObject curObj in mapObjects)
            {
                foreach (ProxiEdge curEdge in this.proximityGraph.EdgeList)
                {
                    MapObject curNeighbor = null;
                    int index = -1;
                    if (curObj.FeatureType == curEdge.Node1.FeatureType && curObj.ID == curEdge.Node1.TagID)
                    {
                        curNeighbor = GetMapObject_Index(curEdge.Node2.TagID, curEdge.Node2.FeatureType, mapOList, out index);
                        curNeighbor.SomeValue = index;
                    }
                    else if (curObj.FeatureType == curEdge.Node2.FeatureType && curObj.ID == curEdge.Node2.TagID)
                    {
                        curNeighbor = GetMapObject_Index(curEdge.Node1.TagID, curEdge.Node1.FeatureType, mapOList, out index);
                        curNeighbor.SomeValue = index;
                    }
                    if (curNeighbor != null)
                    {
                        if (flags[index] == false)
                        {
                            resNeighbors.Add(curNeighbor);
                        }

                    }
                }
            }

            if (resNeighbors.Count == 0)
                return null;
            else
                return resNeighbors;

        }

        /// <summary>
        /// 根据得到地图对象ID和类获取其在矩阵中的索引号
        /// </summary>
        /// <param name="id">地图对象ID</param>
        /// <param name="type">地图对象类型</param>
        /// <param name="mapOList">地图对象列表</param>
        /// <returns>返回地图对象在矩阵中的索引号</returns>
        private int GetIndexofMapObject(int id, FeatureType type,List<MapObject> mapOList )
        {
            for (int i=0;i<mapOList.Count;i++)
            {
                MapObject curO=mapOList[i];
                if (curO.ID == id && curO.FeatureType == type)
                {
                    return i;
                }
            }
            return -1;
        }
        /// <summary>
        /// 根据地图对象ID和类获取其在矩阵中的索引号
        /// </summary>
        /// <param name="id">地图对象ID</param>
        /// <param name="type">地图对象类型</param>
        /// <param name="mapOList">地图对象列表</param>
        /// <returns>地图对象</returns>
        private MapObject GetMapObject_Index(int id, FeatureType type, List<MapObject> mapOList,out int index)
        {
            for (int i = 0; i < mapOList.Count; i++)
            {
                MapObject curO = mapOList[i];
                if (curO.ID == id && curO.FeatureType == type)
                {
                    index=i;
                    return curO;
                }
            }
            index=-1;
            return null;
        }

        /// <summary>
        /// 输出结果到文本文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        public void Write2TXT(string fileName)
        {
            if(this.adjMatrix==null||this.adjMatrix.Length==0)
            {
                return;
            }
            StreamWriter streamw = File.CreateText(fileName);
            int size=(int)(Math.Sqrt(this.adjMatrix.Length));
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    streamw.Write(this.adjMatrix[i, j].ToString() + "\t");
                }
                streamw.WriteLine();
            }
            streamw.Close();
        }

        /// <summary>
        /// 两个邻近矩阵对应元素差的绝对值的平均值
        /// </summary>
        /// <param name="anotherMa">另一个</param>
        /// <returns>返回两个矩阵对应元素差的绝对值的平均值</returns>
        public double DeltaMatrix(ProximityMatrix anotherMa)
        {
            if(anotherMa.adjMatrix.Length !=this.adjMatrix.Length)
            {
                return -1;
            }
            double sum = 0;
            int n=(int)Math.Sqrt(anotherMa.adjMatrix.Length);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    sum += Math.Abs(anotherMa.adjMatrix[i, j] - this.adjMatrix[i, j]);
                }
            }
            return sum / this.adjMatrix.Length;
        }
    }
}
