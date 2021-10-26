using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlgIterative.AlgIA
{
    /// <summary>
    /// 抗体
    /// </summary>
    public abstract class AntibodyBase:IAntibody
    {
        /// <summary>
        /// Antibody's affinity value.
        /// </summary>
        protected double affinity = 0;

        /// <summary>
        /// Antibody's affinity value.
        /// </summary>
        /// 
        /// <remarks><para>affinity value (usefulness) of the Antibody calculate by calling
        /// <see cref="Evaluate"/> method. The greater the value, the more useful the Antibody.
        /// </para></remarks>
        /// 
        public double Affinity
        {
            get { return affinity; }
        }

        /// <summary>
        /// Generate random Antibody value.
        /// </summary>
        /// 
        /// <remarks><para>Regenerates Antibody's value using random number generator.</para>
        /// </remarks>
        /// 
        public abstract void Generate();

        /// <summary>
        /// Create new random Antibody with same parameters (factory method).
        /// </summary>
        /// 
        /// <remarks><para>The method creates new Antibody of the same type, but randomly
        /// initialized. The method is useful as factory method for those classes, which work
        /// with Antibody's interface, but not with particular Antibody class.</para></remarks>
        /// 
        public abstract IAntibody CreateNew();

        /// <summary>
        /// Clone the Antibody.
        /// </summary>
        /// 
        /// <remarks><para>The method clones the Antibody returning the exact copy of it.</para>
        /// </remarks>
        /// 
        public abstract IAntibody Clone();

        /// <summary>
        /// Mutation operator.
        /// </summary>
        /// 
        /// <remarks><para>The method performs Antibody's mutation, changing its part randomly.</para></remarks>
        /// 
        public abstract void Mutate();

        ///// <summary>
        ///// Crossover operator.
        ///// </summary>
        ///// 
        ///// <param name="pair">Pair chromosome to crossover with.</param>
        ///// 
        ///// <remarks><para>The method performs crossover between two chromosomes – interchanging some parts of chromosomes.</para></remarks>
        ///// 
        //public abstract void Crossover(IAntibody pair);

        /// <summary>
        /// Evaluate chromosome with specified fitness function.
        /// </summary>
        /// 
        /// <param name="function">Affinity function to use for evaluation of the Antibody.</param>
        /// 
        /// <remarks><para>Calculates Antibody's Affinity using the specifed Affinity function.</para></remarks>
        ///
        public void Evaluate(IAffinityFunction function)
        {
            affinity = function.Evaluate(this);
        }

        /// <summary>
        /// Compare two Antibody.
        /// </summary>
        /// 
        /// <param name="o">Binary Antibody to compare to.</param>
        /// 
        /// <returns>Returns comparison result, which equals to 0 if Affinity values
        /// of both Antibodies are equal, 1 if Affinity value of this Antibody
        /// is less than Affinity value of the specified Affinity, -1 otherwise.</returns>
        /// 
        public int CompareTo(object o)
        {
            double f = ((AntibodyBase)o).affinity;

            return (affinity == f) ? 0 : (affinity < f) ? 1 : -1;
        }
    }
}
