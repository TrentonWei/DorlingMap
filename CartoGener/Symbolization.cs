using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Geodatabase;


namespace CartoGener
{
    class Symbolization
    {
        #region polygon符号化
        public object PolygonSymbolization(double width, int LineRgbRed, int LineRgbGreen, int LineRgbBlue, esriSimpleLineStyle symbolstyle, int FillRgbRed, int FillRgbGreen, int FillRgbBlue)
        {
            ESRI.ArcGIS.Display.ISimpleLineSymbol simpleLineSymbol = new ESRI.ArcGIS.Display.SimpleLineSymbolClass();
            simpleLineSymbol.Width = width;

            IRgbColor rgbColor1 = new RgbColorClass();
            rgbColor1.Red = LineRgbRed;
            rgbColor1.Green = LineRgbGreen;
            rgbColor1.Blue = LineRgbBlue;
            simpleLineSymbol.Color = rgbColor1;

            simpleLineSymbol.Style = symbolstyle;

            ISimpleFillSymbol SimpleFillSymbol = new ESRI.ArcGIS.Display.SimpleFillSymbolClass();
            SimpleFillSymbol.Outline = simpleLineSymbol;
            SimpleFillSymbol.Style = (esriSimpleFillStyle)1;

            IRgbColor rgbColor2 = new RgbColorClass();
            rgbColor2.Red = FillRgbRed;
            rgbColor2.Green = FillRgbGreen;
            rgbColor2.Blue = FillRgbBlue;
            SimpleFillSymbol.Color = rgbColor2;

            object oSimpleFillSymbol = SimpleFillSymbol;

            return oSimpleFillSymbol;
        }
        #endregion

        #region Line符号化
        public object LineSymbolization(double width, int LineRgbRed, int LineRgbGreen, int LineRgbBlue, esriSimpleLineStyle symbolstyle)
        {
            ESRI.ArcGIS.Display.ISimpleLineSymbol simpleLineSymbol = new ESRI.ArcGIS.Display.SimpleLineSymbolClass();
            simpleLineSymbol.Width = width;

            IRgbColor rgbColor1 = new RgbColorClass();
            rgbColor1.Red = LineRgbRed;
            rgbColor1.Green = LineRgbGreen;
            rgbColor1.Blue = LineRgbBlue;
            simpleLineSymbol.Color = rgbColor1;

            simpleLineSymbol.Style = symbolstyle;

            object oSimpleFillSymbol = simpleLineSymbol;

            return oSimpleFillSymbol;
        }
        #endregion

        #region Point符号化
        public object PointSymbolization(int PointRgbRed, int PointRgbGreen, int PointRgbBlue)
        {
            ISimpleMarkerSymbol pMarkerSymbol = new SimpleMarkerSymbolClass();
            esriSimpleMarkerStyle eSMS = (esriSimpleMarkerStyle)0;
            pMarkerSymbol.Style = eSMS;

            IRgbColor rgbColor = new RgbColorClass();
            rgbColor.Red = PointRgbRed;
            rgbColor.Green = PointRgbGreen;
            rgbColor.Blue = PointRgbBlue;

            pMarkerSymbol.Color = rgbColor;

            object oMarkerSymbol = pMarkerSymbol;

            return oMarkerSymbol;
        }
        #endregion
    }
}
