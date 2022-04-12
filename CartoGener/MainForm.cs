using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.GlobeCore;
using ESRI.ArcGIS.Geodatabase;
using System.Collections.Generic;
using DisplaceAlgLib;
using AuxStructureLib;
using AlgEMLib;
using System.Diagnostics;
using AuxStructureLib.IO;

namespace CartoGener
{
    public sealed partial class MainForm : Form
    {

        /// <summary>
        /// class constructor
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Parameters
        /// </summary>
        ILayer pLayer;

        /// <summary>
        /// DorlingMap Create Form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dorlingMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DorlingMapFrm DMF = new DorlingMapFrm(this.axMapControl1);
            DMF.Show();
        }

        /// <summary>
        /// Remove the layers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void axTOCControl1_OnMouseDown(object sender, AxESRI.ArcGIS.Controls.ITOCControlEvents_OnMouseDownEvent e)
        {
            if (axMapControl1.LayerCount > 0)
            {
                esriTOCControlItem pItem = new esriTOCControlItem();
                //pLayer = new FeatureLayerClass();
                IBasicMap pBasicMap = new MapClass();
                object pOther = new object();
                object pIndex = new object();
                // Returns the item in the TOCControl at the specified coordinates.
                axTOCControl1.HitTest(e.x, e.y, ref pItem, ref pBasicMap, ref pLayer, ref pOther, ref pIndex);
            }

            if (e.button == 2)
            {
                this.contextMenuStrip1.Show(axTOCControl1, e.x, e.y);
            }
        }

        /// <summary>
        /// Remove the layer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void removeTheLayerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (axMapControl1.Map.LayerCount > 0)
                {
                    if (pLayer != null)
                    {
                        axMapControl1.Map.DeleteLayer(pLayer);
                    }
                }
            }

            catch
            {
                MessageBox.Show("Fail to remove");
                return;
            }
        }

        private void axTOCControl1_OnMouseDown_1(object sender, AxESRI.ArcGIS.Controls.ITOCControlEvents_OnMouseDownEvent e)
        {
            if (axMapControl1.LayerCount > 0)
            {
                esriTOCControlItem pItem = new esriTOCControlItem();
                //pLayer = new FeatureLayerClass();
                IBasicMap pBasicMap = new MapClass();
                object pOther = new object();
                object pIndex = new object();
                // Returns the item in the TOCControl at the specified coordinates.
                axTOCControl1.HitTest(e.x, e.y, ref pItem, ref pBasicMap, ref pLayer, ref pOther, ref pIndex);
            }

            if (e.button == 2)
            {
                this.contextMenuStrip1.Show(axTOCControl1, e.x, e.y);
            }
        }

        /// <summary>
        /// Stable DorlingMap Create Form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StableDorlingMapForToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StableDorlingMapFrm SDMF=new StableDorlingMapFrm(this.axMapControl1);
            SDMF.Show();
        }
    }
}

