namespace UILib
{
    partial class FeatureSelectPanel
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FeatureSelectPanel));
            this.TopPanel = new System.Windows.Forms.Panel();
            this.cbxLyrs = new System.Windows.Forms.ComboBox();
            this.btnOpen = new System.Windows.Forms.Button();
            this.MainPanel = new System.Windows.Forms.Panel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnDelete = new System.Windows.Forms.Button();
            this.lbSelectedFeatures = new System.Windows.Forms.ListBox();
            this.TopPanel.SuspendLayout();
            this.MainPanel.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // TopPanel
            // 
            this.TopPanel.Controls.Add(this.cbxLyrs);
            this.TopPanel.Controls.Add(this.btnOpen);
            this.TopPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.TopPanel.Location = new System.Drawing.Point(0, 0);
            this.TopPanel.Name = "TopPanel";
            this.TopPanel.Size = new System.Drawing.Size(315, 26);
            this.TopPanel.TabIndex = 0;
            // 
            // cbxLyrs
            // 
            this.cbxLyrs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbxLyrs.FormattingEnabled = true;
            this.cbxLyrs.Location = new System.Drawing.Point(0, 0);
            this.cbxLyrs.Name = "cbxLyrs";
            this.cbxLyrs.Size = new System.Drawing.Size(283, 20);
            this.cbxLyrs.TabIndex = 1;
            this.cbxLyrs.SelectedIndexChanged += new System.EventHandler(this.cbxLyrs_SelectedIndexChanged);
            // 
            // btnOpen
            // 
            this.btnOpen.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnOpen.Image = ((System.Drawing.Image)(resources.GetObject("btnOpen.Image")));
            this.btnOpen.Location = new System.Drawing.Point(283, 0);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(32, 26);
            this.btnOpen.TabIndex = 0;
            this.btnOpen.UseVisualStyleBackColor = true;
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // MainPanel
            // 
            this.MainPanel.Controls.Add(this.panel1);
            this.MainPanel.Controls.Add(this.lbSelectedFeatures);
            this.MainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainPanel.Location = new System.Drawing.Point(0, 26);
            this.MainPanel.Name = "MainPanel";
            this.MainPanel.Size = new System.Drawing.Size(315, 131);
            this.MainPanel.TabIndex = 1;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnDelete);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel1.Location = new System.Drawing.Point(283, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(32, 131);
            this.panel1.TabIndex = 1;
            // 
            // btnDelete
            // 
            this.btnDelete.Image = ((System.Drawing.Image)(resources.GetObject("btnDelete.Image")));
            this.btnDelete.Location = new System.Drawing.Point(1, 3);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(29, 23);
            this.btnDelete.TabIndex = 0;
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // lbSelectedFeatures
            // 
            this.lbSelectedFeatures.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbSelectedFeatures.FormattingEnabled = true;
            this.lbSelectedFeatures.ItemHeight = 12;
            this.lbSelectedFeatures.Location = new System.Drawing.Point(0, 0);
            this.lbSelectedFeatures.Name = "lbSelectedFeatures";
            this.lbSelectedFeatures.Size = new System.Drawing.Size(315, 131);
            this.lbSelectedFeatures.TabIndex = 0;
            // 
            // FeatureSelectPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.MainPanel);
            this.Controls.Add(this.TopPanel);
            this.Name = "FeatureSelectPanel";
            this.Size = new System.Drawing.Size(315, 157);
            this.TopPanel.ResumeLayout(false);
            this.MainPanel.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel TopPanel;
        private System.Windows.Forms.ComboBox cbxLyrs;
        private System.Windows.Forms.Button btnOpen;
        private System.Windows.Forms.Panel MainPanel;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.ListBox lbSelectedFeatures;

    }
}
