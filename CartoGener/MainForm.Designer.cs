namespace CartoGener
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            //Ensures that any ESRI libraries that have been used are unloaded in the correct order. 
            //Failure to do this may result in random crashes on exit due to the operating system unloading 
            //the libraries in the incorrect order. 
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.dorlingMapToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.removeTheLayerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.axLicenseControl2 = new AxESRI.ArcGIS.Controls.AxLicenseControl();
            this.axMapControl1 = new AxESRI.ArcGIS.Controls.AxMapControl();
            this.axTOCControl1 = new AxESRI.ArcGIS.Controls.AxTOCControl();
            this.axToolbarControl1 = new AxESRI.ArcGIS.Controls.AxToolbarControl();
            this.StableDorlingMapForToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.axLicenseControl2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.axMapControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.axTOCControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.axToolbarControl1)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.dorlingMapToolStripMenuItem,
            this.StableDorlingMapForToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(8, 2, 0, 2);
            this.menuStrip1.Size = new System.Drawing.Size(1145, 28);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // dorlingMapToolStripMenuItem
            // 
            this.dorlingMapToolStripMenuItem.Name = "dorlingMapToolStripMenuItem";
            this.dorlingMapToolStripMenuItem.Size = new System.Drawing.Size(108, 24);
            this.dorlingMapToolStripMenuItem.Text = "DorlingMap";
            this.dorlingMapToolStripMenuItem.Click += new System.EventHandler(this.dorlingMapToolStripMenuItem_Click);
            // 
            // splitter1
            // 
            this.splitter1.Location = new System.Drawing.Point(0, 28);
            this.splitter1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(4, 648);
            this.splitter1.TabIndex = 6;
            this.splitter1.TabStop = false;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Location = new System.Drawing.Point(4, 654);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(2, 0, 19, 0);
            this.statusStrip1.Size = new System.Drawing.Size(1141, 22);
            this.statusStrip1.Stretch = false;
            this.statusStrip1.TabIndex = 7;
            this.statusStrip1.Text = "statusBar1";
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.removeTheLayerToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(206, 28);
            // 
            // removeTheLayerToolStripMenuItem
            // 
            this.removeTheLayerToolStripMenuItem.Name = "removeTheLayerToolStripMenuItem";
            this.removeTheLayerToolStripMenuItem.Size = new System.Drawing.Size(205, 24);
            this.removeTheLayerToolStripMenuItem.Text = "Remove the layer";
            this.removeTheLayerToolStripMenuItem.Click += new System.EventHandler(this.removeTheLayerToolStripMenuItem_Click);
            // 
            // axLicenseControl2
            // 
            this.axLicenseControl2.Enabled = true;
            this.axLicenseControl2.Location = new System.Drawing.Point(853, 240);
            this.axLicenseControl2.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.axLicenseControl2.Name = "axLicenseControl2";
            this.axLicenseControl2.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axLicenseControl2.OcxState")));
            this.axLicenseControl2.Size = new System.Drawing.Size(32, 32);
            this.axLicenseControl2.TabIndex = 15;
            // 
            // axMapControl1
            // 
            this.axMapControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axMapControl1.Location = new System.Drawing.Point(401, 56);
            this.axMapControl1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.axMapControl1.Name = "axMapControl1";
            this.axMapControl1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axMapControl1.OcxState")));
            this.axMapControl1.Size = new System.Drawing.Size(744, 598);
            this.axMapControl1.TabIndex = 14;
            // 
            // axTOCControl1
            // 
            this.axTOCControl1.Dock = System.Windows.Forms.DockStyle.Left;
            this.axTOCControl1.Location = new System.Drawing.Point(4, 56);
            this.axTOCControl1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.axTOCControl1.Name = "axTOCControl1";
            this.axTOCControl1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axTOCControl1.OcxState")));
            this.axTOCControl1.Size = new System.Drawing.Size(397, 598);
            this.axTOCControl1.TabIndex = 13;
            this.axTOCControl1.OnMouseDown += new AxESRI.ArcGIS.Controls.ITOCControlEvents_OnMouseDownEventHandler(this.axTOCControl1_OnMouseDown_1);
            // 
            // axToolbarControl1
            // 
            this.axToolbarControl1.Dock = System.Windows.Forms.DockStyle.Top;
            this.axToolbarControl1.Location = new System.Drawing.Point(4, 28);
            this.axToolbarControl1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.axToolbarControl1.Name = "axToolbarControl1";
            this.axToolbarControl1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axToolbarControl1.OcxState")));
            this.axToolbarControl1.Size = new System.Drawing.Size(1141, 28);
            this.axToolbarControl1.TabIndex = 12;
            // 
            // StableDorlingMapForToolStripMenuItem
            // 
            this.StableDorlingMapForToolStripMenuItem.Name = "StableDorlingMapForToolStripMenuItem";
            this.StableDorlingMapForToolStripMenuItem.Size = new System.Drawing.Size(158, 24);
            this.StableDorlingMapForToolStripMenuItem.Text = "Stable DorlingMap";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1145, 676);
            this.Controls.Add(this.axLicenseControl2);
            this.Controls.Add(this.axMapControl1);
            this.Controls.Add(this.axTOCControl1);
            this.Controls.Add(this.axToolbarControl1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "MainForm";
            this.Text = "CartoGener";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.axLicenseControl2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.axMapControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.axTOCControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.axToolbarControl1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.Splitter splitter1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripMenuItem dorlingMapToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem removeTheLayerToolStripMenuItem;
        private AxESRI.ArcGIS.Controls.AxToolbarControl axToolbarControl1;
        private AxESRI.ArcGIS.Controls.AxTOCControl axTOCControl1;
        private AxESRI.ArcGIS.Controls.AxMapControl axMapControl1;
        private AxESRI.ArcGIS.Controls.AxLicenseControl axLicenseControl2;
        private System.Windows.Forms.ToolStripMenuItem StableDorlingMapForToolStripMenuItem;
    }
}

