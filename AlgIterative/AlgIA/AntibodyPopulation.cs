using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AForge;

namespace AlgIterative.AlgIA
{
    /// <summary>
    /// 抗体群
    /// </summary>
    public class AntibodyPopulation
    {
        private IAffinityFunction affinityFunction;
        public List<IAntibody> population = new List<IAntibody>( );
        private int			size;
        // random number generator
        
        //private static ThreadSafeRandom rand = new ThreadSafeRandom( );

        //
        private double		affinitysMax = 0;
        private double		affinitySum = 0;
        private double		affinityAvg = 0;
        private double      affinitysMin = 0;

        IAntibody bestAntibody = null;

        /// <summary>
        /// Fitness function to apply to the population.
        /// </summary>
        /// 
        /// <remarks><para>The property sets fitness function, which is used to evaluate
        /// usefulness of population's chromosomes. Setting new fitness function causes recalculation
        /// of fitness values for all population's members and new best member will be found.</para>
        /// </remarks>
        /// 
        public IAffinityFunction AffinityFunction
        {
            get { return affinityFunction; }
            set
            {
                affinityFunction = value;

                foreach ( IAntibody member in population )
                {
                    member.Evaluate( affinityFunction );
                }

                this.EvluateAntibodyPopulation( );

                //Sort
            }
        }

        /// <summary>
        /// Maximum  affinity of the population.
        /// </summary>
        /// 
        /// <remarks><para>The property keeps maximum  affinity of Antibody currently existing
        /// in the population.</para>
        /// 
        /// <para><note>The property is recalculate only after <see cref="Selection">selection</see>
        /// or <see cref="Migrate">migration</see> was done.</note></para>
        /// </remarks>
        /// 
        public double AffinityMax
        {
            get { return affinitysMax; }
        }

        /// <summary>
        /// Summary  Affinity of the population.
        /// </summary>
        ///
        /// <remarks><para>The property keeps summary  Affinity of all chromosome existing in the
        /// population.</para>
        /// 
        /// <para><note>The property is recalculate only after <see cref="Selection">selection</see>
        /// or <see cref="Migrate">migration</see> was done.</note></para>
        /// </remarks>
        ///
        public double AffinitySum
        {
            get { return affinitySum; }
        }

        /// <summary>
        /// Average  Affinity of the population.
        /// </summary>
        /// 
        /// <remarks><para>The property keeps average  Affinity of all chromosome existing in the
        /// population.</para>
        /// 
        /// <para><note>The property is recalculate only after <see cref="Selection">selection</see>
        /// or <see cref="Migrate">migration</see> was done.</note></para>
        /// </remarks>
        ///
        public double  AffinityAvg
        {
            get { return  affinityAvg; }
        }

        /// <summary>
        /// Best Antibody of the population.
        /// </summary>
        /// 
        /// <remarks><para>The property keeps the best Antibody existing in the population
        /// or <see langword="null"/> if all Antibody have 0 fitness.</para>
        /// 
        /// <para><note>The property is recalculate only after <see cref="Selection">selection</see>
        /// or <see cref="Migrate">migration</see> was done.</note></para>
        /// </remarks>
        /// 
        public IAntibody BestAntibody
        {
            get { return bestAntibody; }
        }

        /// <summary>
        /// Size of the population.
        /// </summary>
        /// 
        /// <remarks>The property keeps initial (minimal) size of population.
        /// Population always returns to this size after selection operator was applied,
        /// which happens after <see cref="Selection"/> or <see cref="RunEpoch"/> methods
        /// call.</remarks>
        /// 
        public int Size
        {
            get { return size; }
            set { size = value; }
        }

        /// <summary>
        /// Get Antibody with specified index.
        /// </summary>
        /// 
        /// <param name="index">Chromosome's index to retrieve.</param>
        /// 
        /// <remarks>Allows to access individuals of the population.</remarks>
        /// 
        public IAntibody this[int index]
        {
            get { return population[index]; }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="Population"/> class.
        /// </summary>
        /// 
        /// <param name="size">Initial size of population.</param>
        /// <param name="ancestor">Ancestor Antibody to use for population creatioin.</param>
        /// <param name="AffinityFunction">"Affinity function to use for calculating
        /// Antibody's fitness values.</param>
        /// <param name="CloneMethod">Selection algorithm to use for selection
        /// Antibody's to new generation.</param>
        /// 
        /// <remarks>Creates new population of specified size. The specified ancestor
        /// becomes first member of the population and is used to create other members
        /// with same parameters, which were used for ancestor's creation.</remarks>
        /// 
        /// <exception cref="ArgumentException">Too small population's size was specified. The
        /// exception is thrown in the case if <paramref name="size"/> is smaller than 2.</exception>
        ///
        public AntibodyPopulation(int size,
                           IAntibody ancestor,
                           IAffinityFunction affinityFunction)
        {
            if ( size < 2 )
                throw new ArgumentException( "Too small population's size was specified." );

            this.affinityFunction = affinityFunction;
          
            this.size = size;

            // add ancestor to the population
            ancestor.Evaluate(affinityFunction);
            population.Add( ancestor.Clone( ));
            // add more Antibody to the population
            for ( int i = 1; i < size; i++ )
            {
                // create new Antibody
                IAntibody c= ancestor.CreateNew();
                // calculate it's affinity
                c.Evaluate(affinityFunction);
                // add it to population
                population.Add( c );
            }
            this.EvluateAntibodyPopulation();
        }

        /// <summary>
        /// Regenerate population.
        /// </summary>
        /// 
        /// <remarks>The method regenerates population filling it with random chromosomes.</remarks>
        /// 
        public void Regenerate( )
        {
            IAntibody ancestor = population[0];
            // clear population
            population.Clear( );
            // add Antibody to the population
            for ( int i = 0; i < size; i++ )
            {
                // create new Antibody
                IAntibody c = ancestor.CreateNew();
                // calculate it's affinity
                c.Evaluate(affinityFunction);
                // add it to population
                population.Add( c );
            }

            this.EvluateAntibodyPopulation();
        }

        public AntibodyPopulation(List<IAntibody> population,
                          IAffinityFunction affinityFunction)
        {
            this.affinityFunction = affinityFunction;
            this.population = population;
            this.size = population.Count;
            // add more Antibody to the population
            for (int i = 0; i < size; i++)
            {
                population[i].Evaluate(affinityFunction);
            }
            this.EvluateAntibodyPopulation();
        }


        /// <summary>
        /// 按照亲和度排序，并计算各统计信息（最大、最小、平均、求和、最优抗体）
        /// </summary>
        private void EvluateAntibodyPopulation()
        {
            //this.size = this.population.Count;
            this.population.Sort();
            bestAntibody = population[0];
            affinitysMax = bestAntibody.Affinity;
            affinitysMin = population[population.Count - 1].Affinity;
            affinitySum = affinitysMax;
            affinityAvg=affinitysMax;

            for (int i = 0; i < population.Count; i++)
            {
                double affinity = population[i].Affinity;

                // accumulate summary value
                affinitySum += affinity;

            }
            affinityAvg = affinitySum / population.Count;
        }

        #region Two kinds of clone methods
        /// <summary>
        /// 选择+克隆扩增
        /// </summary>
        /// <param name="n">选择的数目</param>
        /// <returns>克隆后的抗体群</returns>
        public AntibodyPopulation Select_Clone(int n,double beta)
        {
            //AntibodyPopulation clonePopu = new AntibodyPopulation();
            List<IAntibody> newpopulation = new List<IAntibody>();
            int N_i = 0;
            for (int i = 0; i < n; i++)
            {
                N_i = (int)Math.Round(beta * this.size / (i + 1));
                IAntibody[] clones = new IAntibody[N_i];
                for (int j = 0; j < N_i; j++)
                {
                    clones[j] = this.population[i].Clone();
                }
                newpopulation.AddRange(clones);
            }
            AntibodyPopulation newAntibodyPopu = new AntibodyPopulation(newpopulation, this.affinityFunction);
            return newAntibodyPopu;
        }
   

        /// <summary>
        /// 根据亲和度确定克隆规模
        /// </summary>
        /// <param name="n">被克隆的抗体数目</param>
        /// <returns>克隆后的抗体群</returns>
        public AntibodyPopulation Select_Clone(int n)
        {
            //AntibodyPopulation clonePopu = new AntibodyPopulation();
            List<IAntibody> newpopulation = new List<IAntibody>();
            int N_i = 0;
            for (int i = 0; i < n; i++)
            {
                N_i = (int)Math.Round( this.population[i].Affinity*this.size /this.affinitySum);
                IAntibody[] clones = new IAntibody[N_i];
                for (int j = 0; j < N_i; j++)
                {
                    clones[j] = this.population[i].Clone();
                }
                newpopulation.AddRange(clones);
            }
            AntibodyPopulation newAntibodyPopu = new AntibodyPopulation(newpopulation, this.affinityFunction);
            return newAntibodyPopu;
        }
        #endregion

        #region Tree Kinds of heperMutation methods
        /// <summary>
        /// 高频变异
        /// </summary>
        /// <param name="genNum">迭代次数</param>
        /// <returns>变异后的抗体群</returns>
        public void Gens_Affi_Exp_ActiveMutationRatio(int genNum, double a)
        {
            Random rand=new Random();
            double normalisedAffinity=0;
            double deta=this.affinitysMax - this.affinitysMin;
            foreach(IAntibody curAb in this.population)
            {
                normalisedAffinity = (curAb.Affinity - this.affinitysMin) / deta;
                double Pm=Math.Exp((-1) * a * normalisedAffinity) / genNum;
                if (rand.NextDouble() <= Pm)
                {
                    // mutate it
                    curAb.Mutate();
                }
            }
        }

        /// <summary>
        /// 高频变异
        /// </summary>
        /// <param name="genNum">迭代次数</param>
        /// <returns>变异后的抗体群</returns>
        public void Affi_Linear_ActiveMutationRatio()
        {
            Random rand = new Random();
            double normalisedAffinity = 0;
            double deta = this.affinitysMax - this.affinitysMin;
            foreach (IAntibody curAb in this.population)
            {
                normalisedAffinity = (curAb.Affinity - this.affinitysMin) / deta;
                double Pm = 1 - normalisedAffinity;
                if (rand.NextDouble() <= Pm)
                {
                    // mutate it
                    curAb.Mutate();
                }
            }
        }

        /// <summary>
        /// 高频变异
        /// </summary>
        /// <param name="genNum">迭代次数</param>
        /// <returns>变异后的抗体群</returns>
        public void Gens_Linear_ActiveMutationRatio(double Pm,int t,int T,double a)
        {
            Random rand = new Random();;
           
            foreach (IAntibody curAb in this.population)
            {
              
                Pm =Pm*(1 - a*t/T);
                if (rand.NextDouble() <= Pm)
                {
                    // mutate it
                    curAb.Mutate();
                }
            }
        }

        /// <summary>
        /// 高频变异
        /// </summary>
        /// <param name="genNum">迭代次数</param>
        /// <returns>变异后的抗体群</returns>
        public void Gens_Exp_ActiveMutationRatio(double Pm, int t, int T, double a)
        {
            Random rand = new Random(); 

            foreach (IAntibody curAb in this.population)
            {

                Pm = Pm * Math.Exp( (-1)* a * t / T);
                if (rand.NextDouble() <= Pm)
                {
                    // mutate it
                    curAb.Mutate();
                }
            }
        }

        /// <summary>
        /// 高频变异
        /// </summary>
        /// <param name="genNum">迭代次数</param>
        /// <returns>变异后的抗体群</returns>
        public void HyperMutation(double mutationFactor)
        {
            Random rand = new Random();
            foreach (IAntibody curAb in this.population)
            {
                if (rand.NextDouble() <= mutationFactor)
                {
                    // mutate it
                    curAb.Mutate();
                }
            }
        }
        #endregion

        //克隆死亡操作


        /// <summary>
        /// 从克隆变异后的抗体群中选择n个最优的
        /// </summary>
        /// <param name="CloneP"></param>
        /// <param name="OldP"></param>
        public AntibodyPopulation SelectAfterClone(AntibodyPopulation CloneP, AntibodyPopulation OldP, int n)
        {
            //int lengthNew=CloneP.population.Count;

            //for (int i = lengthNew - 1; i >= n; i--)
            //{
            //    CloneP.population.RemoveAt(i);
            //}

            //for (int i = n; i < OldP.Size; i++)
            //{
            //    CloneP.population.Add(OldP.population[n]);
            //}
            //CloneP.EvluateAntibodyPopulation();
            OldP.population.AddRange(CloneP.population);
            OldP.EvluateAntibodyPopulation();

            int count = OldP.population.Count;
            for (int i = OldP.Size; i < count; i++)
            {
                OldP.population.RemoveAt(OldP.population.Count-1);
            }
            return OldP;
        }

        /// <summary>
        /// 抗体增补
        /// </summary>
        /// <param name="diversityRate">增补率</param>
        public void Diversity(double diversityRate)
        {
            IAntibody ancestor=this.population[0];
            int n=(int)(diversityRate*this.size);
            if(n>this.size)
                  throw new ArgumentException("增补数量太多！"); 
            for(int i=this.size-n;i<this.size;i++)
            {
                this.population.Insert(i,ancestor.CreateNew());
                this.population.RemoveAt(i+1);
            }
            this.EvluateAntibodyPopulation();
        }

    }
}
