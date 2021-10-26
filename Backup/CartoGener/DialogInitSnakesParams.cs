using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DisplaceAlgLib;

namespace CartoGener
{
    public partial class DialogInitSnakesParams : Form
    {
        public DialogInitSnakesParams()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            Dialog2Data();
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void DialogInitSnakesParams_Load(object sender, EventArgs e)
        {
            InitDialog();
        }

        private void InitDialog()
        {
            this.txtAA.Text = SnakesAlg.fDefaultA.ToString();
            this.txtBB.Text = SnakesAlg.fDefaultB.ToString();
        }

        private void Dialog2Data()
        {
            SnakesAlg.fDefaultA = double.Parse(this.txtAA.Text);
            SnakesAlg.fDefaultB = double.Parse(this.txtBB.Text);
        }
    }
}
