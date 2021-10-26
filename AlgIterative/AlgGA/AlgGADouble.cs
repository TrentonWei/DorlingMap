using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;
using System.Threading;
using AForge.Genetic;
using AForge.Math;
using AForge.Math.Random;
using AForge;
using System.IO;
namespace AlgIterative.AlgGA
{
    public class AlgGADouble
    {
       /* public static void TestRandom()
        {
            StreamWriter streamw = File.CreateText(@"E:\test.txt");
            
            UniformGenerator ug = new UniformGenerator(new Range(0,100));
            for(int i=0;i<100;i++)
            {
                float curx=ug.Next();
                streamw.Write(curx.ToString());
                streamw.WriteLine();
            }

            streamw.Close();
        }*/

        private int PopulationSize = 100;//种群大小
        private int Iterations = 50;//迭代次数
        private SDS Map = null;//地图对象
        private ContinuousDVT DisVTemp = null;//移位向量模版
        private ConflictDetection ConflictDetector = null;//冲突探测工具
        private double Dstancetol = 0;//距离阈值
        private double PLCost = 10;//线-面冲突权值
        private double PPCost = 5;//面-面冲突权值
        private double DispCost = 1;//移动距离权值

        private Thread workerThread = null;

        public int ConlictsNo = 0;
        public double ConflictCost = 0;
        public int IntiConflictNo = 0;
        public double IntiConflictCost = 0;
        public double Timespan = 0;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="map">地图对象</param>
        /// <param name="populationSize">种群大小</param>
        /// <param name="iterations">迭代次数</param>

        /// <param name="dstancetol">距离阈值</param>
        /// <param name="pLCost">线-面冲突权值</param>
        /// <param name="pPCost">面-面冲突权值</param>
        /// <param name="dispCost">移动距离权值</param>
        public AlgGADouble(SDS map,
                    int populationSize,
                    int iterations ,
                    //DispVectorTemplate disVTemp ,
                    //ConflictDetection conflictDetector,
                    double dstancetol,
                    double pLCost ,
                    double pPCost ,
                    double dispCost ,
            bool IsPolarCoors)
        {
            PopulationSize = populationSize;
            Iterations = iterations;
            Map = map;
            Dstancetol = dstancetol;
            PLCost = pLCost;
            PPCost = pPCost;
            DispCost = dispCost;
            if (IsPolarCoors == true)
            {
                this.DisVTemp = new ContinuousDVT(this.Dstancetol, 2 * Math.PI, true);
            }
            else
            {
                this.DisVTemp = new ContinuousDVT(this.Dstancetol, this.Dstancetol, false);
            }
            ConflictDetector = new ConflictDetection(Map);
        }



        /// <summary>
        /// 执行算法
        /// </summary>
        public void DoGA(string filePath,int K)
        {

          

            // create fitness function
            MapFitnessFunctionDouble fitnessFunction = new MapFitnessFunctionDouble(this.Map,
                this.DisVTemp,
                this.ConflictDetector,
                this.Dstancetol,
                this.PLCost,
                this.PPCost, 
                this.DispCost);
            // create population
            Population population = new Population(PopulationSize,
                new MapChromosomeDouble(Map, DisVTemp),
                fitnessFunction,
                new RouletteWheelSelection()
                );

            population.MutationRate = 0.02;

            StreamWriter streamw = File.CreateText(filePath + @"\\" + "fitness"+K.ToString()+".txt");
            streamw.Write(population.Size.ToString() + "  " + Iterations.ToString());
            streamw.WriteLine();
            streamw.Write("Iter_No" + "  " + "Cost" + "  " + "Conf_No");
            streamw.WriteLine();

           #region 初始冲突检测
            List<Conflict> initCList=this.ConflictDetector.ConflictDetect(this.Dstancetol);
            this.IntiConflictNo = initCList.Count;
            this.IntiConflictCost = 0;
            foreach (Conflict conflict in initCList)
            {
                if (conflict.ConflictType == @"PP")
                {
                    //cost += this.PPCost;
                    IntiConflictCost += (this.PPCost * (this.Dstancetol - conflict.Distance));
                }
                else if (conflict.ConflictType == @"PL")
                {

                    //cost += this.PLCost;
                    IntiConflictCost += (this.PLCost * (this.Dstancetol - conflict.Distance));

                }
            }
            #endregion

            DateTime Time1 = DateTime.Now;
            // iterations
            int i = 1;
            // loop
            while (true)
            {
                // run one epoch of genetic algorithm
                population.RunEpoch();
                streamw.Write(i.ToString() + "  " + ((MapChromosomeDouble)population.BestChromosome).ConflictsCost + " " + ((MapChromosomeDouble)population.BestChromosome).ConflictsNo);
                streamw.WriteLine();
                // increase current iteration
                i++;
              

                if ((Iterations != 0) && (i > Iterations))
                    break;
            }


            DateTime Time2 = DateTime.Now;
            // Difference in days, hours, and minutes.
            TimeSpan ts = Time2 - Time1;
            // Difference in ticks.
            Timespan = (int)ts.TotalMilliseconds;

            MapChromosomeDouble bestChromesome = population.BestChromosome as MapChromosomeDouble;
            double[] DispV = bestChromesome.Value;
            double DCost=0;
            double[] CalDeltaX_Y= fitnessFunction.CalDeltaX_Y(DispV,out DCost);
            fitnessFunction.MoveObjects(CalDeltaX_Y);

            ConlictsNo = bestChromesome.ConflictsNo;
            ConflictCost = bestChromesome.ConflictsCost;

            streamw.Close();

         /*   // get population size
            int populationSize = 0;
            int iterations = 0;
            try
            {
                PopulationSize = Math.Max(10, Math.Min(100, this.PopulationSize));
            }
            catch
            {
                populationSize = 40;
            }
            // iterations
            try
            {
                Iterations = Math.Max(0, this.Iterations);
            }
            catch
            {
                iterations = 100;
            }

            workerThread = new Thread(new ThreadStart(SearchSolution));
            workerThread.Start();*/
        }

        // Worker thread
        private void SearchSolution()
        {  // create fitness function
            MapFitnessFunctionDouble fitnessFunction = new MapFitnessFunctionDouble(this.Map,
                this.DisVTemp,
                this.ConflictDetector,
                this.Dstancetol,
                this.PLCost,
                this.PPCost,
                this.DispCost);
            // create population
            Population population = new Population(PopulationSize,
                new MapChromosomeDouble(Map, DisVTemp),
                fitnessFunction,
                new RouletteWheelSelection()
                );

            #region 初始冲突检测
            List<Conflict> initCList = this.ConflictDetector.ConflictDetect(this.Dstancetol);
            this.IntiConflictNo = initCList.Count;
            this.IntiConflictCost = 0;
            foreach (Conflict conflict in initCList)
            {
                if (conflict.ConflictType == @"PP")
                {
                    //cost += this.PPCost;
                    IntiConflictCost += (this.PPCost * (this.Dstancetol - conflict.Distance));
                }
                else if (conflict.ConflictType == @"PL")
                {

                    //cost += this.PLCost;
                    IntiConflictCost += (this.PLCost * (this.Dstancetol - conflict.Distance));

                }
            }
            #endregion

            // iterations
            int i = 1;
            // loop
            while (true)
            {
                // run one epoch of genetic algorithm
                population.RunEpoch();
                // increase current iteration
                i++;
                //
                if ((Iterations != 0) && (i > Iterations))
                    break;
            }
            MapChromosomeDouble bestChromesome = population.BestChromosome as MapChromosomeDouble;
            double[] DispV = bestChromesome.Value;
            double DCost = 0;
            double[] CalDeltaX_Y = fitnessFunction.CalDeltaX_Y(DispV, out DCost);
            fitnessFunction.MoveObjects(CalDeltaX_Y);

            ConlictsNo = bestChromesome.ConflictsNo;
            ConflictCost = bestChromesome.ConflictsCost;
    }
    }
}
