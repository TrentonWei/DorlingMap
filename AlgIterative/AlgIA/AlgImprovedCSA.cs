using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;
using System.IO;

//public enum MutationType
//{
//    ConstMutationRatio,                    //常数变异概率
//    Gens_Linear_ActiveMutationRatio,       //迭代数线性相关变异概率
//    Gens_Affi_Linear_ActiveMutationRatio,  //迭代数和亲和度线性相关变异概率
//    Gens_Exp_ActiveMutationRatio,          //迭代数指数相关变异概率
//    Gens_Affi_Exp_ActiveMutationRatio      //迭代数和亲和度指数相关变异概率
//}

namespace AlgIterative.AlgIA
{
    /// <summary>
    /// 利用V图改进后的克隆选择算法
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
    public class AlgImprovedCSA
    {
        private int PopulationSize = 100;//种群大小
        private int Iterations = 50;//迭代次数
        private int SelectSize = 0;//克隆选择数目
        private double DiversityRatio = 0;//抗体补充比例
        private double MutationFactor = 0.2;//变异概率
        private MutationType MutationType = MutationType.ConstMutationRatio;
        private SDS Map = null;//SDS地图对象
        private SMap Map1 = null;//加密后的地图用于生成V图
        private SMap nMap = null;//SMap地图对象
        private SMap oMap = null;//原始地图数据，用于计算移位代价
        private DispVectorTemplate DisVTemp = null;//移位向量模版
        private ConflictDetection ConflictDetector = null;//冲突探测工具
        private double Dstancetol = 0;//距离阈值
        private double PLDtol = 0;//距离阈值
        private double PPDtol = 0;//距离阈值
        private double PLCost = 10;//线-面冲突权值
        private double PPCost = 5;//面-面冲突权值
        private double DispCost = 1;//移动距离权值
        private VoronoiDiagram vd = null;
        public int ConlictsNo = 0;
        public double ConflictCost = 0;
        public int IntiConflictNo = 0;
        public double IntiConflictCost = 0;
        private double MutationFactor1 = 1;
        public double Timespan = 0;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="map">SDS地图对象</param>
        /// <param name="map1">加密后的SMap地图对象用于计算移位值</param>
        /// <param name="oMap">SMap原始地图对象</param>
        /// <param name="nmap">SMap地图</param>
        /// <param name="populationSize">种群大小</param>
        /// <param name="iterations">迭代次数</param>
        /// <param name="selectSize">克隆选择数目</param>
        /// <param name="diversityRatio">抗体补充比例</param>
        /// <param name="circleNum">圆环数</param>
        /// <param name="dstancetol">距离阈值</param>
        /// <param name="PLDtol">线-面冲突权值</param>
        /// <param name="PPDtol">面-面冲突权值</param>
        /// <param name="pLCost">线-面冲突代价值</param>
        /// <param name="pPCost">面-面冲突代价值</param>
        /// <param name="dispCost">移动距离代价值</param>
        /// <param name="MutationFactor">变异系数</param>
        /// <param name="MutationType">变异类型</param>
        /// <param name="MutationFactor1">??</param>
        /// <param name="vd">//V图</param>
        public AlgImprovedCSA(SDS map,
            SMap map1, //加密顶点的地图对象
            SMap oMap,//原始地图
            SMap nmap,//SMap地图
            // DispVectorTemplate DisVTemp,//移位向量模版
                    int populationSize,
                    int iterations,
                    int selectSize,//克隆选择数目
                    double diversityRatio,//抗体补充比例
                    int circleNum,
                    double dstancetol,
                    double PLDtol,
                    double PPDtol,
                    double pLCost,
                    double pPCost,
                    double dispCost,
                    double MutationFactor,
                    MutationType MutationType,
                    double MutationFactor1,
                    VoronoiDiagram vd//V图
            )
        {
            this.Map1 = map1;
            this.oMap = oMap;
            this.nMap = nmap;
            this.SelectSize = selectSize;
            this.DiversityRatio = diversityRatio;
            this.MutationFactor = MutationFactor;
            PopulationSize = populationSize;
            Iterations = iterations;
            Map = map;
            Dstancetol = dstancetol;
            this.PLDtol = PLDtol;//距离阈值
            this.PPDtol = PPDtol;//距离阈值
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
                TrialPosiDisGroup curG = new TrialPosiDisGroup(i * (Dstancetol / n), 0, Math.PI / (Math.Pow(2.0, i)));
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
            ImprMapAffinityFunction affinityFunction = new ImprMapAffinityFunction(
                this.Map,
                this.DisVTemp,
                this.ConflictDetector,
                this.Dstancetol,
                 this.PLDtol,
                  this.PPDtol,
                this.PLCost,
                this.PPCost,
                this.DispCost);


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

            // create population
            AntibodyPopulation population = new AntibodyPopulation(
                PopulationSize,
                new ImprMapShortAntibody(Map, DisVTemp),
                affinityFunction);

            StreamWriter streamw = File.CreateText(filePath + @"\\" + "fitness.txt");
            streamw.WriteLine(population.Size.ToString() + "  " + Iterations.ToString());
            streamw.WriteLine("Iter_No" + "  " + "Cost" + "  " + "Conf_No");
            streamw.WriteLine("-1" + "  " + IntiConflictCost.ToString() + " " + IntiConflictNo.ToString());
            streamw.WriteLine("0" + "  " + ((ImprMapShortAntibody)population.BestAntibody).ConflictsCost + " " + ((ImprMapShortAntibody)population.BestAntibody).ConflictsNo);
            DateTime Time1 = DateTime.Now;
            // iterations
            int i = 1;
            // loop
            while (true)
            {
                // run one epoch of genetic algorithm
                AntibodyPopulation newpopulation = population.Select_Clone(this.SelectSize, 0.2);
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
                population.Size = newpopulation.population.Count;

                streamw.WriteLine(i.ToString() + "\t" +
                    ((ImprMapShortAntibody)population.BestAntibody).ConflictsCost
                    + "\t"
                    + ((ImprMapShortAntibody)population.BestAntibody).ConflictsNo);
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

            ImprMapShortAntibody bestAntibody = population.BestAntibody as ImprMapShortAntibody;
            ushort[] bestState = bestAntibody.Value;
            affinityFunction.MoveObjects(bestState, Map1, this.nMap);
            ConlictsNo = bestAntibody.ConflictsNo;
            ConflictCost = bestAntibody.ConflictsCost;
            streamw.Close();
        }
    }
}
