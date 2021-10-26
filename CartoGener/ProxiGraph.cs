using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;

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

namespace CartoGener
{
    class ProxiGraph
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
        /// 构造函数
        /// </summary>
        public ProxiGraph()
        {
            NodeList = new List<ProxiNode>();
            EdgeList = new List<ProxiEdge>();
        }

        /// <summary>
        /// 创建ProxiG
        /// </summary>
        /// <param name="pFeatureClass">原始图层</param>
        private void CreateProxiG(IFeatureClass pFeatureClass)
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
            for (int i = 0; i < pFeatureClass.FeatureCount(null); i++)
            {
                for (int j = 0; j < pFeatureClass.FeatureCount(null); j++)
                {
                    if (j != i)
                    {
                        IGeometry iGeo = pFeatureClass.GetFeature(i).Shape;
                        IGeometry jGeo = pFeatureClass.GetFeature(j).Shape;

                        IRelationalOperator iRo = iGeo as IRelationalOperator;
                        if (iRo.Disjoint(jGeo) || iRo.Touches(jGeo) || iRo.Overlaps(jGeo))
                        {
                            ProxiEdge CacheEdge = new ProxiEdge(edgeID, this.NodeList[i], this.NodeList[j]);
                        }
                    }
                }
            }
            #endregion
        }
    }
}
