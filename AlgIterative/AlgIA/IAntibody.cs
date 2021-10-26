using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlgIterative.AlgIA
{
    /// <summary>
    /// 抗体
    /// </summary>
    public interface IAntibody : IComparable
    {
            /// <summary>
            /// Antibody's Affinity value.
            /// </summary>
            /// 
            /// <remarks><para>The Affinity value represents Antibody's usefulness - the greater the
            /// value, the more useful it.</para></remarks>
            /// 
            double Affinity { get; }

            /// <summary>
            /// Generate Antibody value.
            /// </summary>
            /// 
            /// <remarks><para>Regenerates Antibody's value using random number generator or other methods.</para>
            /// </remarks>
            /// 
            void Generate();

            /// <summary>
            /// Create new random chromosome with same parameters (factory method).
            /// </summary>
            /// 
            /// <remarks><para>The method creates new chromosome of the same type, but randomly
            /// initialized. The method is useful as factory method for those classes, which work
            /// with chromosome's interface, but not with particular chromosome class.</para></remarks>
            /// 
            IAntibody CreateNew();

            /// <summary>
            /// Clone the IAntibody.
            /// </summary>
            /// 
            /// <remarks><para>The method clones the chromosome returning the exact copy of it.</para>
            /// </remarks>
            /// 
            IAntibody Clone();

            /// <summary>
            /// Mutation operator.
            /// </summary>
            /// 
            /// <remarks><para>The method performs chromosome's mutation, changing its part randomly.</para></remarks>
            /// 
            void Mutate();

            ///// <summary>
            ///// Crossover operator.
            ///// </summary>
            ///// 
            ///// <param name="pair">Pair chromosome to crossover with.</param>
            ///// 
            ///// <remarks><para>The method performs crossover between two chromosomes ?interchanging some parts of chromosomes.</para></remarks>
            ///// 
            //void Crossover(IChromosome pair);

            /// <summary>
            /// Evaluate Affinity with specified IAntibody function.
            /// </summary>
            /// 
            /// <param name="function">Affinity function to use for evaluation of the IAntibody.</param>
            /// 
            /// <remarks><para>Calculates IAntibody's Affinity using the specifed Affinity function.</para></remarks>
            /// 
            void Evaluate(IAffinityFunction function);
        }
    }

