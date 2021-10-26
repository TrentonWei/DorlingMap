using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;

namespace DisplaceAlgLib
{
    public class Snake:EnergyMinPolyLine
    {
        private float _a;                        //形状参数1
        private float _b;                        //形状参数2


        //private IPath _path;                      //线路径AE中Geometry对象，表示一条简单线

        //private IFeatureLayer bufferLyr;          //缓冲区的图层

      /*  public IFeatureLayer BufferLyr
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
        }*/
        
        public float a
        {
          get
          {
            return _a;
          }
          set
          {
            if (_a == value)
              return;
            _a = value;
          }
        }
        public float b
        {
          get
          {
            return _b;
          }
          set
          {
            if (_b == value)
              return;
            _b = value;
          }
        }
     /*   public float SymbolWidth
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
        }*/
        
     /*   public IPath Path
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
        } */

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="symbolWidth"></param>
        /// <param name="path"></param>
        public Snake(float a, float b, float symbolWidth, IPath path)
        {
            _a = a;
            _b = b;
            _fSymbolWidth = symbolWidth;
            _path = path;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="symbolWidth"></param>
        /// <param name="path"></param>
        public Snake(float a, float b, float symbolWidth, IPath path,IFeatureLayer bufferLayer)
        {
            _a = a;
            _b = b;
            _fSymbolWidth = symbolWidth;
            _path = path;
            this.bufferLyr = bufferLayer;
        }
    }
}
