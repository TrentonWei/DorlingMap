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
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesRaster;

using AuxStructureLib;
using AuxStructureLib.IO;

namespace CartoGener
{
    /// <summary>
    /// Support for stable DorlingMap
    /// </summary>
    class SDMSupport
    {
        ///约束条件1：邻近的两个TimeData or TimeDataGroup才能聚为一类（邻近的两个TimeData or TimeDataGourp差异为欧式距离；不邻近的两个TimeData or TimeDataGroup无穷大）
        ///约束条件2：

        /// <summary>
        /// Get Sim between two Time series Data（越小越好）
        /// </summary>
        /// <param name="TimeData_1"></param>
        /// <param name="TimeData_2"></param>
        /// <returns></returns>
        public double GetSim(Dictionary<IPolygon, double> TimeData_1, Dictionary<IPolygon, double> TimeData_2)
        {
            double Sim = 0;

            #region 欧式距离
            foreach (KeyValuePair<IPolygon, double> Kv in TimeData_1)
            {
                Sim = Math.Abs(Kv.Value - TimeData_2[Kv.Key]) + Sim;
            }
            Sim = Sim / TimeData_1.Count;
            #endregion

            return Sim;
        }

        /// <summary>
        /// Get Sim between two Time series Group Data（越小越好）
        /// </summary>
        /// <param name="TimeDataGroup_1"></param>
        /// <param name="TimeDataGroup_2"></param>
        /// GroupSimType=0 类间最短距离；GroupSimType=1 类间最大距离；GroupSimType=2 类间平均距离
        /// <returns></returns>
        public double GetGroupSim(List<Dictionary<IPolygon, double>> TimeDataGroup_1, List<Dictionary<IPolygon, double>> TimeDataGroup_2,int GroupSimType)
        {
            double GroupSim = 0; List<double> SimList = new List<double>();

            #region 类间所有距离计算
            for (int i = 0; i < TimeDataGroup_1.Count; i++)
            {
                for (int j = 0; j < TimeDataGroup_2.Count; j++)
                {
                    double Sim = this.GetSim(TimeDataGroup_1[i], TimeDataGroup_2[j]);
                    SimList.Add(Sim);
                }
            }
            #endregion

            #region 类间距离计算
            if (SimList.Count > 0)
            {
                if (GroupSimType == 0)//返回类间最小距离
                {
                }
                if (GroupSimType == 1)//返回类间最大距离
                {
                    GroupSim = SimList.Max();
                }
                if (GroupSimType == 2)//返回类间最大距离
                {
                    GroupSim = SimList.Average(); ;
                }
            }
            else
            {
                GroupSim = -1;
            }
            #endregion

            return GroupSim;
        }

        /// <summary>
        /// 系统聚类结果
        /// </summary>
        /// <param name="TimeSeriesData"></param>Dictionary<IPolygon, double>表示specific_Time的Data
        ///  GroupSimType=0 类间最短距离；GroupSimType=1 类间最大距离；GroupSimType=2 类间平均距离
        /// <returns></returns>
        public Dictionary<int, Dictionary<int, List<int>>> GetHierarchy(List<Dictionary<IPolygon, double>> TimeSeriesData,int GroupSimType)
        {
            Dictionary<int, Dictionary<int, List<int>>> Hierarchy = new Dictionary<int, Dictionary<int, List<int>>>();//int 表示层level层级；Dictionary<int, List<int>>表示对应层上的聚类结果

            #region Level_0
            Dictionary<int, List<int>> Level_0Dic = new Dictionary<int, List<int>>();
            for (int i = 0; i < TimeSeriesData.Count; i++)
            {
                List<int> Level_0 = new List<int>();
                Level_0.Add(i);
                Level_0Dic.Add(i, Level_0);
            }
            Hierarchy.Add(0, Level_0Dic);
            #endregion

            #region 聚类结果
            int Level=0;
            while (Level < TimeSeriesData.Count)
            {
                Dictionary<int, List<int>> LevelValue = Hierarchy[Level];//获取上一层级分类结果

                #region 计算相似程度，获取最相似的两类
                Dictionary<Tuple<int, int>, double> SimDic = new Dictionary<Tuple<int, int>, double>();
                for (int i = 1; i < LevelValue.Count; i++)
                {
                    #region 获取当前对象
                    List<int> TargetLevel = LevelValue[i];
                    List<Dictionary<IPolygon, double>> TargetLevelTimeSeriesData = new List<Dictionary<IPolygon, double>>();
                    for (int j = 0; j < TargetLevel.Count; j++)
                    {
                        TargetLevelTimeSeriesData.Add(TimeSeriesData[TargetLevel[j]]);
                    }
                    #endregion

                    #region 获取用于计算的对象
                    List<int> ComparableLevel = LevelValue[i - 1];
                    List<Dictionary<IPolygon, double>> ComparableLevelTimeSeriesData = new List<Dictionary<IPolygon, double>>();
                    for (int j = 0; j < ComparableLevel.Count; j++)
                    {
                        ComparableLevelTimeSeriesData.Add(TimeSeriesData[ComparableLevel[j]]);
                    }
                    #endregion

                    #region 存储相似度
                    double CacheGroupSim = this.GetGroupSim(TargetLevelTimeSeriesData, ComparableLevelTimeSeriesData, GroupSimType);
                    Tuple<int, int> CacheGroups = new Tuple<int, int>(i - 1, i);
                    SimDic.Add(CacheGroups, CacheGroupSim);
                    #endregion
                }

                #region 获取相似程度最大的Pair
                double MinSim = 1000000000000;
                Tuple<int, int> MaxPair = null;
                foreach (KeyValuePair<Tuple<int, int>, double> kv in SimDic)
                {
                    if (kv.Value < MinSim)
                    {
                        MinSim = kv.Value;
                        MaxPair = kv.Key;
                    }
                }
                #endregion
                #endregion

                #region 层次更新
                Dictionary<int, List<int>> CacheLevel_Dic = new Dictionary<int, List<int>>();
                foreach (KeyValuePair<int, List<int>> kv in LevelValue)
                {
                    if (kv.Key < MaxPair.Item1)
                    {
                        CacheLevel_Dic.Add(kv.Key, kv.Value);
                    }
                    else if (kv.Key == MaxPair.Item1)
                    {
                        List<int> NewList = kv.Value.Concat(LevelValue[MaxPair.Item2]).ToList();
                        CacheLevel_Dic.Add(kv.Key, NewList);
                    }
                    else if (kv.Key > MaxPair.Item2)
                    {
                        CacheLevel_Dic.Add(kv.Key - 1, kv.Value);
                    }
                }
                #endregion
            }

            #endregion

            return Hierarchy;
        }
    }
}
