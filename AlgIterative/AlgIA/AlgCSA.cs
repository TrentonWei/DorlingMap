using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;
using System.IO;

namespace AlgIterative.AlgIA
{

    public enum MutationType
    {
        ConstMutationRatio,                    //常数变异概率
        Gens_Linear_ActiveMutationRatio,       //迭代数线性相关变异概率
        Gens_Affi_Linear_ActiveMutationRatio,  //迭代数和亲和度线性相关变异概率
        Gens_Exp_ActiveMutationRatio,          //迭代数指数相关变异概率
        Gens_Affi_Exp_ActiveMutationRatio      //迭代数和亲和度指数相关变异概率
    }
    /// <summary>
    /// 克隆选择算法
    /// Clonal Selection Algorithm
    /// (1)  Generate a set (P) of candidate solutions, composed
    ///of the subset of memory cells (M) added to the remaining (Pr) population (P = Pr+ M);
    ///(2)  Determine (Select) the n best individuals of the
    ///population (Pn), based on an affinity measure;
    /// (3)  Reproduce (Clone) these n best individuals of the
    ///population, giving rise to a temporary population of
    ///clones (C). The clone size is an increasing function
    ///of the affinity with the antigen;
    ///(4)  Submit the population of clones to a hypermutation
    ///scheme, where the hypermutation is proportional to
    ///the affinity of the antibody with the antigen. A
    ///maturated antibody population is generated ( C*);
    ///(5)  Re-select the improved individuals from  C* to
    ///compose the memory set M. Some members of P can
    ///be replaced by other improved members of  C*;
    ///(6)  Replace d antibodies by novel ones (diversity
    ///introduction). The lower affinity cells have higher
    ///probabilities of being replaced
    /// </summary>
    public class AlgCSA
    {


        private int PopulationSize = 100;//种群大小
        private int Iterations = 50;//迭代次数
        private int SelectSize = 0;//克隆选择数目
        private double DiversityRatio = 0;//抗体补充比例
        private double MutationFactor = 0.2;//变异概率
        private MutationType MutationType = MutationType.ConstMutationRatio;
        private SDS Map = null;//地图对象
        private DispVectorTemplate DisVTemp = null;//移位向量模版
        private ConflictDetection ConflictDetector = null;//冲突探测工具
        private double Dstancetol = 0;//距离阈值

        private double PLDtol = 0;//距离阈值
        private double PPDtol = 0;//距离阈值

        private double PLCost = 10;//线-面冲突权值
        private double PPCost = 5;//面-面冲突权值
        private double DispCost = 1;//移动距离权值

        public int ConlictsNo = 0;
        public double ConflictCost = 0;
        public int IntiConflictNo = 0;
        public double IntiConflictCost = 0;

         //private                MutationType MutationType= MutationType.ConstMutationRatio;
        private double MutationFactor1 = 1;

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
        public AlgCSA(SDS map,
                    int populationSize,
                    int iterations ,
                    int selectSize,//克隆选择数目
                    double diversityRatio,//抗体补充比例
                    int circleNum,
                    double dstancetol,
            double PLDtol,
            double PPCost,
                    double pLCost ,
                    double pPCost ,
                    double dispCost,
                    double MutationFactor,
                    MutationType MutationType, 
                    double MutationFactor1
            )
        {
            this.SelectSize = selectSize;
            this.DiversityRatio = diversityRatio;
            this.MutationFactor = MutationFactor;
            PopulationSize = populationSize;
            Iterations = iterations;
            Map = map;
            Dstancetol = dstancetol;
            this.PLDtol = PLDtol;
            this.PPCost = PPCost;
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
        public void DoCSA(string filePath)
        {
            // create fitness function
            MapAffinityFunction affinityFunction = new MapAffinityFunction(this.Map,
                this.DisVTemp,
                this.ConflictDetector,
                this.PLDtol,
                this.PPDtol,
                this.Dstancetol,
                this.PLCost,
                this.PPCost, this.DispCost);
            // create population
            AntibodyPopulation population = new AntibodyPopulation(PopulationSize,
                new MapShortAntibody(Map, DisVTemp),
                affinityFunction);

            #region 初始冲突检测
            List<Conflict> initCList = this.ConflictDetector.ConflictDetect(this.PLDtol,this.PPDtol);
            this.IntiConflictNo = initCList.Count;
            this.IntiConflictCost = 0;
            foreach (Conflict conflict in initCList)
            {
                if (conflict.ConflictType == @"PP")
                {
                    IntiConflictCost += (this.PPCost * (this.PPDtol - conflict.Distance));
                }
                else if (conflict.ConflictType == @"PL")
                {
                    IntiConflictCost += (this.PLCost * (this.PLDtol - conflict.Distance));
                }
            }
            #endregion

            if (IntiConflictNo == 0)
                return;

            StreamWriter streamw = File.CreateText(filePath + @"\\" + "fitness.txt");
            streamw.WriteLine(population.Size.ToString() + "  " + Iterations.ToString());
            streamw.WriteLine("Iter_No" + "  " + "Cost" + "  " + "Conf_No");
            DateTime Time1 = DateTime.Now;
            // iterations
            int i = 1;
            // loop
            while (true)
            {
                // run one epoch of genetic algorithm
                AntibodyPopulation newpopulation = population.Select_Clone(this.SelectSize,0.2);
                // newpopulation.HyperMutationAdaptive1(i, 2);
                if (this.MutationType == MutationType.ConstMutationRatio)
                {
                    newpopulation.HyperMutation(MutationFactor);
                }
                else if (this.MutationType == MutationType.Gens_Linear_ActiveMutationRatio)
                {
                    //与迭代数线性相关
                    newpopulation.Gens_Linear_ActiveMutationRatio(MutationFactor,
                        i, 
                        Iterations, 
                        this.MutationFactor1);
                }
                else if (this.MutationType == MutationType.Gens_Affi_Linear_ActiveMutationRatio)
                {
                    //与迭代数+亲和度线性相关
                    //newpopulation.Gens_Linear_ActiveMutationRatio(
                    
                }

                else if (this.MutationType == MutationType.Gens_Exp_ActiveMutationRatio)
                {
                    // //与迭代数指数相关
                    newpopulation.Gens_Exp_ActiveMutationRatio(MutationFactor, i, Iterations, this.MutationFactor1);
                }

                else if (this.MutationType == MutationType.Gens_Affi_Exp_ActiveMutationRatio)
                {
                    // //与迭代数+亲和度指数相关
                    newpopulation.Gens_Affi_Exp_ActiveMutationRatio(Iterations, this.MutationFactor1);
                }
                newpopulation = newpopulation.SelectAfterClone(newpopulation, population, this.SelectSize);
                newpopulation.Diversity(this.DiversityRatio);
                population = newpopulation;
                population.Size = newpopulation.population.Count; ;
                streamw.Write(i.ToString() + "  " + ((MapShortAntibody)population.BestAntibody).ConflictsCost + " " + ((MapShortAntibody)population.BestAntibody).ConflictsNo);
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

            MapShortAntibody bestChromesome = population.BestAntibody as MapShortAntibody;
            ushort[] bestState = bestChromesome.Value;
            affinityFunction.MoveObjects(bestState);

            ConlictsNo = bestChromesome.ConflictsNo;
            ConflictCost = bestChromesome.ConflictsCost;

            streamw.Close();
        }
    }
}
