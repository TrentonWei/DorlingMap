using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace UILib
{
    public partial class FeatureSelectPanel : UserControl
    {
       
        public FeatureSelectPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 初始化图层复选框
        /// </summary>
        /// <param name="axMapControl">地图控件</param>
        public void InitiFeatureLyrNames(ESRI.ArcGIS.Controls.AxMapControl axMapControl)
        {
            if (axMapControl == null || axMapControl.LayerCount == 0)
                return;
            string lyrName = "";
            for (int i = 0; i < axMapControl.LayerCount; i++)
            {
                lyrName = axMapControl.get_Layer(i).Name;
                this.cbxLyrs.Items.Add(lyrName);
            }
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }
        /// <summary>
        /// 打开文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "";
        }

        private void cbxLyrs_SelectedIndexChanged(object sender, EventArgs e)
        {
            object lyrName = this.cbxLyrs.SelectedItem;
            this.lbSelectedFeatures.Items.Add(lyrName);
          //  this.lbSelectedFeatures.
           //his.lbSelectedFeatures.Invalidate();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            object selItem=this.lbSelectedFeatures.SelectedItem;
            if (selItem!= null)
            {
                this.lbSelectedFeatures.Items.Remove(selItem);
            }
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            object selItem = this.lbSelectedFeatures.SelectedItem;
            if (selItem != null)
            {
                //向上
            }
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            object selItem = this.lbSelectedFeatures.SelectedItem;
            if (selItem != null)
            {
                //向下
            }
        }
        /// <summary>
        /// 图层名称
        /// </summary>
        public string[] LyrNames
        {
            get {
                int n = this.lbSelectedFeatures.Items.Count;
                if (n <= 0)
                    return null;
                string[] lyrNames = new string[n];
                int i=0;
                foreach (object curItem in this.lbSelectedFeatures.Items)
                {
                    lyrNames[i] = curItem.ToString();
                    i++;
                }
                return lyrNames;
            }
        }
    }
}
