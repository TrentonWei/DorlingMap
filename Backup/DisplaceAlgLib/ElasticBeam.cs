using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;

namespace DisplaceAlgLib
{
    public  class ElasticBeam:EnergyMinPolyLine
    {
        private float _A;                        //形状参数1,横截面积
        private float _E;                        //形状参数2,弹性模量
        private float _I;                        //形状参数2,惯性力
        /// <summary>
        /// 横截面积及
        /// </summary>
        public float A
        {
            get
            {
                return _A;
            }
            set
            {
                if (_A == value)
                    return;
                _A = value;
            }
        }
        /// <summary>
        /// 弹性模量
        /// </summary>
        public float E
        {
            get
            {
                return _E;
            }
            set
            {
                if (_E == value)
                    return;
                _E = value;
            }
        }
        /// <summary>
        /// 惯性力
        /// </summary>
        public float I
        {
            get
            {
                return _I;
            }
            set
            {
                if (_I == value)
                    return;
                _I = value;
            }
        }
        /// <summary>
        /// 弹性梁构造函数
        /// </summary>
        /// <param name="A">横截面积</param>
        /// <param name="E">弹性模量</param>
        /// <param name="I">惯性力</param>
        /// <param name="symbolWidth">符号宽度</param>
        /// <param name="path">线对象</param>
        public ElasticBeam(float A, float E, float I, float symbolWidth, IPath path)
        {
            _A = A;
            _E = E;
            _I = I;
            _fSymbolWidth = symbolWidth;
            _path = path;
        }

        /// <summary>
        /// 弹性梁构造函数
        /// </summary>
        /// <param name="A">横截面积</param>
        /// <param name="E">弹性模量</param>
        /// <param name="I">惯性力</param>
        /// <param name="symbolWidth">符号宽度</param>
        /// <param name="path">线对象</param>
        /// <param name="bufferLayer">用于存储缓冲区的图层</param>
        public ElasticBeam(float A, float E, float I, float symbolWidth, IPath path, IFeatureLayer bufferLayer)
        {
            _A = A;
            _E = E;
            _I = I;
            _fSymbolWidth = symbolWidth;
            _path = path;
            this.bufferLyr = bufferLayer;
        }

    }
}
