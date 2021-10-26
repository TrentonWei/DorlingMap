using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesGDB;

namespace AuxStructureLib
{
    /// <summary>
    /// 最小外包矩形
    /// </summary>
    public class SMBR
    {
        /// <summary>
        /// 四点坐标
        /// </summary>
        public Node Point1 = null;
        public Node Point2 = null;
        public Node Point3 = null;
        public Node Point4 = null;

        /// <summary>
        /// ID号
        /// </summary>
        public int ID=-1;
        public FeatureType FeatureType = FeatureType.Unknown;
        public int Tag_ID=-1;

        public double L1 = 0;//长轴长
        public double L2 = 0;//短轴长
        public double Direct1 = -1;//主方向
        public double Direct2 = -1;//副方向
        public double Area = 0;
        public double Perimeter = 0;

        /// <summary>
        /// 读取多边形的最小外接矩形的坐标及相关信息
        /// </summary>
        /// <param name="fFile"></param>
        /// <returns></returns>
        public static List<SMBR> ReadSMBRfrmESRI(string path,string featureName,string type)
        {
            IWorkspaceFactory pWorkspaceFactory=null;
            int ppID=0;
            if (type == "ShapeFile")
            {
                pWorkspaceFactory = new ShapefileWorkspaceFactoryClass();
            }
            else if (type == "Access")
            {
                pWorkspaceFactory = new AccessWorkspaceFactoryClass();
            }
            IFeatureWorkspace pFeatWS;
            if (pWorkspaceFactory == null)
                return null;
            pFeatWS = pWorkspaceFactory.OpenFromFile(path, 0) as IFeatureWorkspace;
            IFeatureClass pFeatureClass = pFeatWS.OpenFeatureClass(featureName);//打开一个要素类
           
            if (pFeatureClass == null)
                return null;

            List<SMBR> sMBRList = new List<SMBR>();
            IFeatureCursor cursor = null;
            IFeature curFeature = null;
            IGeometry shp = null;

            #region 面要素
            cursor = pFeatureClass.Search(null, false);
            curFeature = null;
            shp = null;
            while ((curFeature = cursor.NextFeature()) != null)
            {
                shp = curFeature.Shape;
                IPolygon polygon = null;
                IGeometryCollection pathSet = null;
                //几何图形
                if (shp.GeometryType == esriGeometryType.esriGeometryPolygon)
                {
                    ppID = curFeature.OID;
                    polygon = shp as IPolygon;
                    pathSet = polygon as IGeometryCollection;
                    int count = pathSet.GeometryCount;
                    //Path对象
                    IPath curPath = null;
                    for (int i = 0; i < count; i++)
                    {
                        SMBR curSMBR = null;                      //当前道路
                        TriNode curPoint = null;                  //当前关联点
                      //  List<TriNode> curPointList = new List<TriNode>();
                        double curX;
                        double curY;
                        curPath = pathSet.get_Geometry(i) as IPath;
                        IPointCollection pointSet = curPath as IPointCollection;
                        int pointCount = pointSet.PointCount;
                        if (pointCount >= 3)
                        {
                            //ArcGIS中将起点和终点重复存储
                            curSMBR = new SMBR();

                            //添加点
                            curX = pointSet.get_Point(0).X;
                            curY = pointSet.get_Point(0).Y;
                            curPoint = new TriNode(curX, curY, -1, ppID, FeatureType.PolygonType);
                            curSMBR.Point1 = curPoint;

                            //添加点
                            curX = pointSet.get_Point(1).X;
                            curY = pointSet.get_Point(1).Y;
                            curPoint = new TriNode(curX, curY, -1, ppID, FeatureType.PolygonType);
                            curSMBR.Point2 = curPoint;

                            //添加点
                            curX = pointSet.get_Point(2).X;
                            curY = pointSet.get_Point(2).Y;
                            curPoint = new TriNode(curX, curY, -1, ppID, FeatureType.PolygonType);
                            curSMBR.Point3 = curPoint;

                            //添加点
                            curX = pointSet.get_Point(3).X;
                            curY = pointSet.get_Point(3).Y;
                            curPoint = new TriNode(curX, curY, -1, ppID, FeatureType.PolygonType);
                            curSMBR.Point4 = curPoint;
                            sMBRList.Add(curSMBR);

                            //获取属性数据
                            curSMBR.FeatureType = FeatureType.PolygonType;
                            curSMBR.ID = ppID;
                            curSMBR.Tag_ID =(int)(curFeature.get_Value(2));
                            curSMBR.Perimeter = (double)(curFeature.get_Value(3));
                            curSMBR.Area = (double)(curFeature.get_Value(4));
                            curSMBR.CalAttributes();
                        }
                    }
                }
            }
            #endregion

            return sMBRList;
        }

        /// <summary>
        /// 计算长轴长、短轴长、主方向和副方向
        /// </summary>
        public void CalAttributes()
        {
            this.L1 = ComFunLib.CalLineLength(this.Point1, this.Point2);
            this.L2 = ComFunLib.CalLineLength(this.Point2, this.Point3);
            this.Direct1 = ComFunLib.CalDirect(this.Point1, this.Point2);
            this.Direct2 = ComFunLib.CalDirect(this.Point2, this.Point3);
            //如果反了就交换
            if (L1 < L2)
            {
                double temp;
                temp = this.L1;
                this.L1 = this.L2;
                this.L2=temp;

                temp = Direct1;
                Direct1 = Direct2;
                Direct2 = temp;
            }
        }
        /// <summary>
        /// 获取指定的SSMBR对象
        /// </summary>
        /// <param name="tagID">源对象ID</param>
        /// <param name="type">源对象类型</param>
        /// <param name="SMBRList">SMBR列表</param>
        /// <returns>SMBR对象</returns>
        public static SMBR  GetSMBR(int tagID,FeatureType type,List<SMBR> SMBRList)
        {
            foreach(SMBR smbr in SMBRList)
            {
                if (smbr.Tag_ID == tagID && smbr.FeatureType == type)
                    return smbr;
            }
            return null;         
        }


    }
}
