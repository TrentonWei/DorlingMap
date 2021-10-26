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
    public partial class DialogSnakeSetting : Form
    {
        public DialogSnakeSetting()
        {
            InitializeComponent();
        }

        private void DialogSnakeSetting_Load(object sender, EventArgs e)
        {
            this.txtTest.Text = SnakesAlg.fDefaultA.ToString();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            SnakesAlg.fDefaultA = double.Parse(this.txtTest.Text);
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        } 
    }
}
