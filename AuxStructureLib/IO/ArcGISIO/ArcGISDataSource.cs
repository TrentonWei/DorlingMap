using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.esriSystem;

namespace AuxStructureLib.IO.ArcGISIO
{
    /// <summary>
    /// ArcGIS数据源枚举类型
    /// </summary>
    public enum enumArcGISDataSourceType
    {
        Shapefile,Coverage,
        Personal_Geodatabase,
        Enterprise_Geodatabase,
        Tin,
        Raster,
        CAD,
        RDBMS
    }

    /// <summary>
    /// ArcGIS数据源
    /// </summary>
    public class ArcGISDataSource
    {
        public enumArcGISDataSourceType DataSourceType;
        public string StrURL;
        public string FeatureName;
        public IPropertySet PropSet=null;

        //IPropertySet propSet = new PropertySetClass();
        //propSet.SetProperty("SERVER", "actc");propSet.SetProperty("INSTANCE", "5151");
        //propSet.SetProperty("USER", "apdm");propSet.SetProperty("PASSWORD", "apdm");
        //propSet.SetProperty("VERSION", "SDE.DEFAULT");
        //pPropset.SetProperty("CONNECTSTRING", @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=E:\Company.mdb;Persist Security Info=False");//创建一个新的OleDB工作空间并打开IWorkspaceFactory pWorkspaceFact;IFeatureWorkspace pFeatWorkspace;pWorkspaceFact = new OLEDBWorkspaceFactoryClass();pFeatWorkspace = pWorkspaceFact.Open(pPropset, 0) as IFeatureWorkspace;ITable pTTable = pFeatWorkspace.OpenTable("Custom");


        /// <summary>
        /// 构造函数-适合文件类数据源
        /// 包括：
        /// Shapefile
        /// Coverage
        /// Personal Geodatabase
        /// Tin
        /// Raster
        /// CAD
        /// </summary>
        /// <param name="dataSourceType">数据源类型</param>
        /// <param name="strURL">文件路径或Access数据库文件名</param>
        /// <param name="featureName">要素名称</param>
        public ArcGISDataSource(enumArcGISDataSourceType dataSourceType, string strURL, string featureName)
        {
            this.DataSourceType = dataSourceType;
            this.StrURL = strURL;
            this.FeatureName = featureName;
        }

        /// <summary>
        /// 构造函数-适合数据库数据源
        /// </summary>
        /// <param name="dataSourceType"></param>
        /// <param name="PropSet"></param>
        public ArcGISDataSource(enumArcGISDataSourceType dataSourceType, IPropertySet propSet)
        {
            this.DataSourceType = dataSourceType;
            this.PropSet = propSet;
        }
    }
}
