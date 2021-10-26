namespace TSP
{
    partial class Form1
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

        // Delegates to enable async calls for setting controls properties
        private delegate void SetTextCallback(System.Windows.Forms.Control control, string text);

        // Thread safe updating of control's text property
        private void SetText(System.Windows.Forms.Control control, string text)
        {
            if (control.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                Invoke(d, new object[] { control, text });
            }
            else
            {
                control.Text = text;
            }
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.stopButton = new System.Windows.Forms.Button();
            this.startButton = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.pathLengthBox = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.currentIterationBox = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.iterationsBox = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.InitTBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.generateMapButton = new System.Windows.Forms.Button();
            this.citiesCountBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.mapControl = new AForge.Controls.Chart();
            this.vTBox = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.SampleBox = new System.Windows.Forms.TextBox();
            this.groupBox3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // stopButton
            // 
            this.stopButton.Enabled = false;
            this.stopButton.Location = new System.Drawing.Point(524, 358);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(90, 25);
            this.stopButton.TabIndex = 9;
            this.stopButton.Text = "S&top";
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(416, 358);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(90, 25);
            this.startButton.TabIndex = 8;
            this.startButton.Text = "&Start";
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.pathLengthBox);
            this.groupBox3.Controls.Add(this.label7);
            this.groupBox3.Controls.Add(this.currentIterationBox);
            this.groupBox3.Controls.Add(this.label6);
            this.groupBox3.Location = new System.Drawing.Point(392, 266);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(222, 81);
            this.groupBox3.TabIndex = 7;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Current iteration";
            // 
            // pathLengthBox
            // 
            this.pathLengthBox.Location = new System.Drawing.Point(150, 48);
            this.pathLengthBox.Name = "pathLengthBox";
            this.pathLengthBox.ReadOnly = true;
            this.pathLengthBox.Size = new System.Drawing.Size(60, 21);
            this.pathLengthBox.TabIndex = 3;
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(12, 51);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(96, 17);
            this.label7.TabIndex = 2;
            this.label7.Text = "Path length:";
            // 
            // currentIterationBox
            // 
            this.currentIterationBox.Location = new System.Drawing.Point(150, 22);
            this.currentIterationBox.Name = "currentIterationBox";
            this.currentIterationBox.ReadOnly = true;
            this.currentIterationBox.Size = new System.Drawing.Size(60, 21);
            this.currentIterationBox.TabIndex = 1;
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(12, 24);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(60, 17);
            this.label6.TabIndex = 0;
            this.label6.Text = "Iteration:";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.SampleBox);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.vTBox);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.iterationsBox);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.InitTBox);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Location = new System.Drawing.Point(392, 19);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(222, 242);
            this.groupBox2.TabIndex = 6;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Settings";
            // 
            // label5
            // 
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(150, 215);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(70, 17);
            this.label5.TabIndex = 6;
            this.label5.Text = "( 0 - inifinity )";
            // 
            // iterationsBox
            // 
            this.iterationsBox.Location = new System.Drawing.Point(152, 170);
            this.iterationsBox.Name = "iterationsBox";
            this.iterationsBox.Size = new System.Drawing.Size(60, 21);
            this.iterationsBox.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(22, 174);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(72, 17);
            this.label4.TabIndex = 4;
            this.label4.Text = "迭代次数";
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(12, 51);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(120, 17);
            this.label3.TabIndex = 2;
            this.label3.Text = "降温速率：";
            // 
            // InitTBox
            // 
            this.InitTBox.Location = new System.Drawing.Point(150, 22);
            this.InitTBox.Name = "InitTBox";
            this.InitTBox.Size = new System.Drawing.Size(60, 21);
            this.InitTBox.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(12, 24);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(108, 17);
            this.label2.TabIndex = 0;
            this.label2.Text = "初始温度:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.generateMapButton);
            this.groupBox1.Controls.Add(this.citiesCountBox);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.mapControl);
            this.groupBox1.Location = new System.Drawing.Point(20, 19);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(360, 366);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Map";
            // 
            // generateMapButton
            // 
            this.generateMapButton.Location = new System.Drawing.Point(132, 333);
            this.generateMapButton.Name = "generateMapButton";
            this.generateMapButton.Size = new System.Drawing.Size(90, 23);
            this.generateMapButton.TabIndex = 3;
            this.generateMapButton.Text = "&Generate";
            this.generateMapButton.Click += new System.EventHandler(this.generateMapButton_Click);
            // 
            // citiesCountBox
            // 
            this.citiesCountBox.Location = new System.Drawing.Point(60, 334);
            this.citiesCountBox.Name = "citiesCountBox";
            this.citiesCountBox.Size = new System.Drawing.Size(60, 21);
            this.citiesCountBox.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(12, 336);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(48, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "Cities:";
            // 
            // mapControl
            // 
            this.mapControl.Location = new System.Drawing.Point(12, 22);
            this.mapControl.Name = "mapControl";
            this.mapControl.RangeX = ((AForge.Range)(resources.GetObject("mapControl.RangeX")));
            this.mapControl.RangeY = ((AForge.Range)(resources.GetObject("mapControl.RangeY")));
            this.mapControl.Size = new System.Drawing.Size(336, 301);
            this.mapControl.TabIndex = 0;
            // 
            // vTBox
            // 
            this.vTBox.Location = new System.Drawing.Point(150, 51);
            this.vTBox.Name = "vTBox";
            this.vTBox.Size = new System.Drawing.Size(60, 21);
            this.vTBox.TabIndex = 8;
            this.vTBox.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // label8
            // 
            this.label8.Location = new System.Drawing.Point(22, 143);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(72, 17);
            this.label8.TabIndex = 9;
            this.label8.Text = "抽样次数";
            // 
            // SampleBox
            // 
            this.SampleBox.Location = new System.Drawing.Point(152, 139);
            this.SampleBox.Name = "SampleBox";
            this.SampleBox.Size = new System.Drawing.Size(60, 21);
            this.SampleBox.TabIndex = 10;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(634, 404);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button stopButton;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.TextBox pathLengthBox;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox currentIterationBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox iterationsBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox InitTBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button generateMapButton;
        private System.Windows.Forms.TextBox citiesCountBox;
        private System.Windows.Forms.Label label1;
        private AForge.Controls.Chart mapControl;
        private System.Windows.Forms.TextBox SampleBox;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox vTBox;


      
    }
}

