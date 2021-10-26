using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using AForge;
using AForge.Controls;

namespace TSP
{
    public partial class Form1 : Form
    {
        private int citiesCount = 20;
        private double[,] map = null;
        public double T0 = 1000;
        public double X = 0.99;
        public int M = 100;
        public int N = 100;

        public Form1()
        {
            InitializeComponent();

            // set up map control
            mapControl.RangeX = new Range(0, 1000);
            mapControl.RangeY = new Range(0, 1000);
            mapControl.AddDataSeries("map", Color.Red, Chart.SeriesType.Dots, 5, false);
            mapControl.AddDataSeries("path", Color.Blue, Chart.SeriesType.Line, 1, false);

            //
            //selectionBox.SelectedIndex = selectionMethod;
           // greedyCrossoverBox.Checked = greedyCrossover;
            UpdateSettings();
            GenerateMap();
        }

        private void generateMapButton_Click(object sender, EventArgs e)
        {
            // get cities count
            try
            {
                citiesCount = Math.Max(5, Math.Min(50, int.Parse(citiesCountBox.Text)));
            }
            catch
            {
                citiesCount = 20;
            }
            citiesCountBox.Text = citiesCount.ToString();

            // regenerate map
            GenerateMap();
        }


    	// Generate new map for the Traivaling Salesman problem
		private void GenerateMap( )
		{
			Random rand = new Random( (int) DateTime.Now.Ticks );

			// create coordinates array
			map = new double[citiesCount, 2];

			for ( int i = 0; i < citiesCount; i++ )
			{
				map[i, 0] = rand.Next( 1001 );
				map[i, 1] = rand.Next( 1001 );
			}

			// set the map
			mapControl.UpdateDataSeries( "map", map );
			// erase path if it is
			mapControl.UpdateDataSeries( "path", null );
		}

        // Update settings controls
        private void UpdateSettings()
        {
            citiesCountBox.Text = citiesCount.ToString();
           // populationSizeBox.Text = populationSize.ToString();
            this.InitTBox.Text = this.T0.ToString();
            this.vTBox.Text = this.X.ToString();
            this.iterationsBox.Text=N.ToString();
            this.SampleBox.Text=M.ToString();
        }

        private void startButton_Click(object sender, EventArgs e)
        {

            // get population size
            try
            {
                this.T0 = double.Parse(InitTBox.Text);
            }
            catch
            {
              //
            }
            // iterations
            try
            {
              this.X=   double.Parse(this.vTBox.Text);
            }
            catch
            {
                //iterations = 100;
            }

            try
            {
                this.N = int.Parse(this.iterationsBox.Text);
            }
            catch
            {
                //iterations = 100;
            }

            this.N = int.Parse(this.SampleBox.Text);
            // update settings controls
           // UpdateSettings();

            AlgSA alg = new AlgSA(this.map, this.T0, this.X, this.N, this.M);
            alg.DoSA();
            int[] bestValue = alg.State.traveralPath;
            // path
            double[,] path = new double[citiesCount + 1, 2];
            for (int j = 0; j < citiesCount; j++)
            {
                path[j, 0] = map[bestValue[j], 0];
                path[j, 1] = map[bestValue[j], 1];
            }
            path[citiesCount, 0] = map[bestValue[0], 0];
            path[citiesCount, 1] = map[bestValue[0], 1];

            mapControl.UpdateDataSeries("path", path);

            // set current iteration's info
           // SetText(currentIterationBox, i.ToString());
            SetText(pathLengthBox, alg.State.fitness.ToString());
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }

}
