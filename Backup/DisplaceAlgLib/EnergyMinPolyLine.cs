using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;

namespace DisplaceAlgLib
{
    public abstract class EnergyMinPolyLine
    {

        protected IPath _path;                      //线路径AE中Geometry对象，表示一条简单线
        protected IFeatureLayer bufferLyr;          //缓冲区的图层
        protected float _fSymbolWidth;             //符号宽度，单位为m

        /// <summary>
        /// 缓冲区的图层
        /// </summary>
        public IFeatureLayer BufferLyr
        {
            get
            {
                return bufferLyr;
            }
            set
            {
                if (bufferLyr == value)
                    return;
                bufferLyr = value;
            }
        }

        /// <summary>
        /// 线路径AE中Geometry对象
        /// </summary>
        public IPath Path
        {
            get
            {
                return _path;
            }
            set
            {
                if (_path == value)
                    return;
                _path = value;
            }
        }
        /// <summary>
        /// 线符号宽度
        /// </summary>
        public float SymbolWidth
        {
            get
            {
                return _fSymbolWidth;
            }
            set
            {
                if (_fSymbolWidth == value)
                    return;
                _fSymbolWidth = value;
            }
        }

        /// <summary>
        /// 判断是否存在冲突
        /// </summary>
        /// <param name="conflictShape">邻近的Geometry对象</param>
        /// <returns>是否冲突</returns>
        public bool IsBufferConflict(IGeometry conflictShape)
        {
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;
            IPolyline poly = new PolylineClass();
            IGeometryCollection polySet = poly as IGeometryCollection;
            polySet.AddGeometry(this.Path, ref missing1, ref missing2);

            IRelationalOperator pRelop = poly as IRelationalOperator;
            if (pRelop.Equals(conflictShape))
                return false;
            //生成缓冲区
            ITopologicalOperator pTopop = poly as ITopologicalOperator;
            IPolygon bufferPolygon = pTopop.Buffer(SnakesAlg.fDenominatorofMapScale * (this.SymbolWidth + SnakesAlg.fminDistance)) as IPolygon;
            //把缓冲区画出来
            UtilFunc.AddPolygon2Layer(this.bufferLyr, bufferPolygon);

            //空间关系运算
            pRelop = bufferPolygon as IRelationalOperator;
            if (pRelop.Disjoint(conflictShape))
            {
                return false;
            }
            return true;
        }

    }
}
