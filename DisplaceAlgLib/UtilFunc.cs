using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;

namespace DisplaceAlgLib
{
    public class UtilFunc
    {

        /// <summary>
        /// 将移位结果写入结果图层
        /// </summary>
        /// <param name="resultLyr">结果图层对象</param>
        /// <param name="path">原来的线对象</param>
        /// <param name="dispaceVector">移位向量</param>
        public static void AddPolygon2Layer(IFeatureLayer Lyr, IPolygon poly)
        {
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            if (Lyr == null)
                return;
            if (poly == null)
                return;

            IFeatureClass featureClass = Lyr.FeatureClass;
            if (featureClass.ShapeType!= esriGeometryType.esriGeometryPolygon)
                return;
            if (featureClass == null)
                return;
            
            IDataset dataset = (IDataset)Lyr;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;

            //注意：此时，所编辑数据不能被其他程序打开
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

         
            IFeature feature = featureClass.CreateFeature();

            feature.Shape = poly;
            feature.Store();
            //关闭编辑
            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
        }
    }
}
