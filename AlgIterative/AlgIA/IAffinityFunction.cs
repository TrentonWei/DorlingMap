using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlgIterative.AlgIA
{

     /// <summary>
    /// IAffinity function interface.
    /// </summary>
    /// 
    /// <remarks>The interface should be implemented by all Affinity function
    /// classes, which are supposed to be used for calculation of Antibodies
    /// IAffinity values. All IAffinity functions should return positive (<b>greater
    /// then zero</b>) value, which indicates how good is the evaluated Antibody - 
    /// the greater the value, the better the Antibody.
    /// </remarks>
    public interface IAffinityFunction
    {
        /// <summary>
        /// Evaluates Antibody.
        /// </summary>
        /// 
        /// <param name="Antibody">Antibody to evaluate.</param>
        /// 
        /// <returns>Returns Antibody's Affinity value.</returns>
        ///
        /// <remarks>The method calculates Affinity value of the specified
        /// Antibody.</remarks>
        ///
        double Evaluate(IAntibody Antibody);
    }
}
