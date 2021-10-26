using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;
using System.Threading;
using AForge.Genetic;
using System.IO;

namespace AlgIterative.AlgGA
{
    /// <summary>
    /// Genentic Algorithm
    /// </summary>
    public class AlgGA
    {
        private int PopulationSize = 100;//种群大小
        private int Iterations = 50;//迭代次数
        private SDS Map = null;//地图对象
        private DispVectorTemplate DisVTemp = null;//移位向量模版
        private ConflictDetection ConflictDetector = null;//冲突探测工具
        private double PLDtol;
        private double PPDtol;
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
        public AlgGA(SDS map,
                    int populationSize,
                    int iterations ,
                    int circleNum,
                    double PLDtol,
                    double PPDtol,
                    double dstancetol,
                    double pLCost ,
                    double pPCost ,
                    double dispCost
            )
        {
            PopulationSize = populationSize;
            Iterations = iterations;
            Map = map;
            this.PPDtol = PPDtol;
            this.PLDtol = PLDtol;
            Dstancetol = dstancetol;
            PLCost = pLCost;
            PPCost = pPCost;
            DispCost = dispCost;

            InitDispTemplate(circleNum);
            
            ConflictDetector = new ConflictDetection(Map);
        }


        /// <summary>
        /// 初始化移位向量模版-32方向
        /// </summary>
        private void InitDispTemplate()
        {
            List<TrialPosiDisGroup> TPDGList = new List<TrialPosiDisGroup>();
            TrialPosiDisGroup g1 = new TrialPosiDisGroup(Dstancetol / 4.0, 0, Math.PI / 2);
            TrialPosiDisGroup g2 = new TrialPosiDisGroup(Dstancetol / 2.0, Math.PI / 8, Math.PI / 4);
            TrialPosiDisGroup g3 = new TrialPosiDisGroup(3.0 * Dstancetol / 4.0, 0, Math.PI / 8);
            TrialPosiDisGroup g4 = new TrialPosiDisGroup(Dstancetol, 0, Math.PI / 16);

            TPDGList.Add(g1);
            TPDGList.Add(g2);
            TPDGList.Add(g3);
            TPDGList.Add(g4);
            DisVTemp = new DispVectorTemplate(TPDGList);
       
            DisVTemp.CalTriPosition();
        }



        /// <summary>
        /// 初始化移位向量模版-16方向
        /// </summary>
        private void InitDispTemplate(int n)
        {
            List<TrialPosiDisGroup> TPDGList = new List<TrialPosiDisGroup>();
            for (int i = 1; i <= n; i++)
            {
                TrialPosiDisGroup curG=new TrialPosiDisGroup(i*(Dstancetol /n),0,Math.PI / (Math.Pow(2.0,i)));
                TPDGList.Add(curG);
            }
            DisVTemp = new DispVectorTemplate(TPDGList);
            DisVTemp.CalTriPosition();
        }
        /// <summary>
        /// 执行算法
        /// </summary>
        public void DoGA(string filePath)
        {
            // create fitness function
            MapFitnessFunction fitnessFunction = new MapFitnessFunction(this.Map,
                this.DisVTemp,
                this.ConflictDetector,
                PLDtol,
                PPDtol,
                this.Dstancetol,
                this.PLCost,
                this.PPCost, this.DispCost);
            // create population
            Population population = new Population(PopulationSize,
                new MapChromosome(Map, DisVTemp),
                fitnessFunction,
                new RouletteWheelSelection()
                );

           #region 初始冲突检测
            List<Conflict> initCList=this.ConflictDetector.ConflictDetect(this.PLDtol,this.PPDtol);
            this.IntiConflictNo = initCList.Count;
            this.IntiConflictCost = 0;
            foreach (Conflict conflict in initCList)
            {
                if (conflict.ConflictType == @"PP")
                {
                    //cost += this.PPCost;
                    IntiConflictCost += (this.PPCost * (this.PPDtol - conflict.Distance));
                }
                else if (conflict.ConflictType == @"PL")
                {

                    //cost += this.PLCost;
                    IntiConflictCost += (this.PLCost * (this.PLDtol - conflict.Distance));

                }
            }
            #endregion


            if (IntiConflictNo == 0)
                return;

            StreamWriter streamw = File.CreateText(filePath + @"\\" + "fitness.txt");
            streamw.Write(population.Size.ToString() + "  " + Iterations.ToString());
            streamw.WriteLine();
            streamw.Write("Iter_No" + "  " + "Cost" + "  " + "Conf_No");
            streamw.WriteLine();

            DateTime Time1 = DateTime.Now;
            // iterations
            int i = 1;
            // loop
            while (true)
            {
                // run one epoch of genetic algorithm
                population.RunEpoch();
                streamw.Write(i.ToString() + "  " + ((MapChromosome)population.BestChromosome).ConflictsCost + " " + ((MapChromosome)population.BestChromosome).ConflictsNo);
                streamw.WriteLine();
                // increase current iteration
                i++;
                //
                if ((Iterations != 0) && (i > Iterations))
                    break;
            }

            DateTime Time2 = DateTime.Now;
            // Difference in days, hours, and minutes.
            TimeSpan ts = Time2 - Time1;
            // Difference in ticks.
            Timespan = (int)ts.TotalMilliseconds;

            MapChromosome bestChromesome = population.BestChromosome as MapChromosome;
            ushort[] bestState = bestChromesome.Value;
            fitnessFunction.MoveObjects(bestState);

            ConlictsNo = bestChromesome.ConflictsNo;
            ConflictCost = bestChromesome.ConflictsCost;

            streamw.Close();
        }

        // Worker thread
        private void SearchSolution()
        {
            // create fitness function
            MapFitnessFunction fitnessFunction = new MapFitnessFunction(this.Map,
                this.DisVTemp,
                this.ConflictDetector,
                PLDtol,
                PPDtol,
                this.Dstancetol,
                this.PLCost,
                this.PPCost,this.DispCost);
            // create population
            Population population = new Population(PopulationSize,
                new MapChromosome(Map, DisVTemp),
                fitnessFunction,
                new RouletteWheelSelection()
                );
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
        }

    }
}
