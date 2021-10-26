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
    public partial class DialogMapSetting : Form
    {
        public DialogMapSetting()
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

        private void DialogMapSetting_Load(object sender, EventArgs e)
        {
            InitDialog();
        }

        private void  InitDialog()
        {
            this.txtMinDis.Text = SnakesAlg.fminDistance.ToString();
            this.txtScale.Text = SnakesAlg.fDenominatorofMapScale.ToString();
            this.txtDefaultSymWidth.Text = SnakesAlg.fDefaultSymWidth.ToString();
        }

        private void Dialog2Data()
        {
            SnakesAlg.fminDistance = double.Parse(this.txtMinDis.Text);
            SnakesAlg.fDenominatorofMapScale = double.Parse(this.txtScale.Text);
            SnakesAlg.fDefaultSymWidth = double.Parse(this.txtDefaultSymWidth.Text); ;
        }

        private void txtMinDis_TextChanged(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void txtScale_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
