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
    /// 用V图改进后的遗传算法
    /// </summary>
    public class AlgImprovedGA
    {
        private int PopulationSize = 100;//种群大小
        private int Iterations = 50;//迭代次数
        private SDS Map = null;//地图对象
        private SMap Map1 = null;//加密后的地图用于生成V图
        private SMap nMap = null;//SMap地图对象
        private SMap oMap = null;//原始地图数据，用于计算移位代价
        private VoronoiDiagram vd = null;
        private DispVectorTemplate DisVTemp = null;//移位向量模版
        private ConflictDetection ConflictDetector = null;//冲突探测工具
        private double PLDtol = 0;//距离阈值
        private double PPDtol = 0;//距离阈值
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
        /// <param name="map">SDS地图对象</param>
        /// <param name="map1">SMap加密地图对象</param>
        /// <param name="oMap">SMap原始地图对象</param>
        /// <param name="nmap">SMap地图对象</param>
        /// <param name="populationSize">种群大小</param>
        /// <param name="iterations">迭代次数</param>
        /// <param name="circleNum">模版圆环数</param>
        /// <param name="PLDtol">线-面冲突阈值</param>
        /// <param name="PPDtol">面-面冲突阈值</param>
        /// <param name="dstancetol">移位距离阈值</param>
        /// <param name="pLCost">线-面冲突代价值</param>
        /// <param name="pPCost">面-面冲突代价值</param>
        /// <param name="dispCost">移位距离代价值</param>
        /// <param name="vd">V图用于精化模版</param>
        public AlgImprovedGA(SDS map,
                        SMap map1, //加密顶点的地图对象
            SMap oMap,//原始地图
            SMap nmap,//SMap地图
                    int populationSize,
                    int iterations,
                    int circleNum,
                    double PLDtol,
                    double PPDtol,
                    double dstancetol,
                    double pLCost,
                    double pPCost,
                    double dispCost,
                    VoronoiDiagram vd//V图
            )
        {
            this.Map1 = map1;
            this.oMap = oMap;
            this.nMap = nmap;
            PopulationSize = populationSize;
            Iterations = iterations;
            Map = map;
            this.PLDtol = PLDtol;
            this.PPDtol = PPDtol;
            Dstancetol = dstancetol;
            PLCost = pLCost;
            PPCost = pPCost;
            DispCost = dispCost;
            InitDispTemplate(circleNum);
            this.DisVTemp.CreateIsTriableList1(vd, oMap, dstancetol);
            ConflictDetector = new ConflictDetection(Map);
            this.vd = vd;
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
            ImprMapFitnessFunction fitnessFunction = new ImprMapFitnessFunction(this.Map,
                this.DisVTemp,
                this.ConflictDetector,
                this.PLDtol,
                this.PPDtol,
                this.Dstancetol,
                this.PLCost,
                this.PPCost, this.DispCost);
            // create population
            Population population = new Population(PopulationSize,
                new ImprMapChromosome(Map),
                fitnessFunction,
                new RouletteWheelSelection()
                );

            #region 初始冲突检测
            List<Conflict> initCList = this.ConflictDetector.ConflictDetect(PLDtol, PPDtol);
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
            int nO = this.oMap.PolygonList.Count;
            double InitDcost = 0;
            for (int k = 0; k < nO; k++)
            {
                PolygonObject oo = this.oMap.PolygonList[k];
                PolygonObject no = this.nMap.PolygonList[k];
                Node op = oo.CalProxiNode();
                Node np = no.CalProxiNode();
                InitDcost += Math.Sqrt((op.X - np.X) * (op.X - np.X) + (op.Y - np.Y) * (op.Y - np.Y));
            }
            IntiConflictCost += InitDcost * this.DispCost;
            #endregion


            if (IntiConflictNo == 0)
                return;

            StreamWriter streamw = File.CreateText(filePath + @"\\" + "fitness.txt");
            streamw.WriteLine(population.Size.ToString() + "  " + Iterations.ToString());
            streamw.WriteLine("Iter_No" + "  " + "Cost" + "  " + "Conf_No");
            streamw.WriteLine("-1" + "  " + IntiConflictCost.ToString() + " " + IntiConflictNo.ToString());
           // streamw.WriteLine("0" + "  " + ((ImprMapChromosome)population.BestChromosome).ConflictsCost + " " + ((ImprMapChromosome)population.BestChromosome).ConflictsNo);
            DateTime Time1 = DateTime.Now;
            // iterations
            int i = 1;
            // loop
            while (true)
            {
                // run one epoch of genetic algorithm
                population.RunEpoch();


                streamw.WriteLine(i.ToString() + "  " + ((ImprMapChromosome)population.BestChromosome).ConflictsCost + " " + ((ImprMapChromosome)population.BestChromosome).ConflictsNo);
                //streamw.WriteLine();
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

            ImprMapChromosome bestChromesome = population.BestChromosome as ImprMapChromosome;
            ushort[] bestState = bestChromesome.Value;
            fitnessFunction.MoveObjects(bestState, Map1, this.nMap); ;

            ConlictsNo = bestChromesome.ConflictsNo;
            ConflictCost = bestChromesome.ConflictsCost;

            streamw.Close();

        }

        // Worker thread
        private void SearchSolution()
        {
            // create fitness function
            ImprMapFitnessFunction fitnessFunction = new ImprMapFitnessFunction(this.Map,
                this.DisVTemp,
                this.ConflictDetector,
                this.PLDtol,
                this.PPDtol,
                this.Dstancetol,
                this.PLCost,
                this.PPCost,this.DispCost);
            // create population
            Population population = new Population(PopulationSize,
                new ImprMapChromosome(Map),
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
