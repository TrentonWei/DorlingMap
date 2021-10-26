using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;
using AForge;

namespace AlgIterative.AlgIA
{
    /// <summary>
    /// 地图解决方案抗体
    /// </summary>
    public class ImprMapShortAntibody : AntibodyBase
    {
        private SDS Map = null;
        private DispVectorTemplate DisVTemp = null;
        public int ConflictsNo=0;
        public double ConflictsCost = 0;

        /// <summary>
        /// Antibody's length in number of elements.
        /// </summary>
        protected int length;

        /// <summary>
        /// Antibody's value.
        /// </summary>
        protected ushort[] val = null;
		/// <summary>
		/// Constructor
		/// </summary>
        /// 

        /// <summary>
        /// Random number generator for Antibody generation, crossover, mutation, etc.
        /// </summary>
        protected static ThreadSafeRandom rand = new ThreadSafeRandom();

        /// <summary>
        /// Antibody's length.
        /// </summary>
        /// 
        /// <remarks><para>Length of the Antibody in array elements.</para></remarks>
        ///
        public int Length
        {
            get { return length; }
        }

        /// <summary>
        /// Antibody's value.
        /// </summary>
        /// 
        /// <remarks><para>Current value of the Antibody.</para></remarks>
        ///
        public ushort[] Value
        {
            get { return val; }
        }

        /// <summary>
        /// Antibody's maximum length.
        /// </summary>
        /// 
        /// <remarks><para>Maxim Antibody's length in array elements.</para></remarks>
        /// 
        public const int MaxLength = 65536;


        public ImprMapShortAntibody(SDS map, DispVectorTemplate disVTemp)
		{
			this.Map = map;
            this.DisVTemp = disVTemp;
            // save parameters
            this.length = Math.Max(2, Math.Min(MaxLength,  map.PolygonOObjs.Count));
            // allocate array
            val = new ushort[this.length];

            // generate random Antibody
            Generate();
		}

		/// <summary>
		/// Copy Constructor
		/// </summary>
        protected ImprMapShortAntibody(ImprMapShortAntibody source)
		{
            this.Map = source.Map;
            this.DisVTemp = source.DisVTemp;
            this.ConflictsNo = source.ConflictsNo;
            this.ConflictsCost = source.ConflictsCost;
            this.length = source.length;
            val = (ushort[])source.val.Clone();
            affinity = source.affinity;
		}

		/// <summary>
        /// Create new random Antibody (factory method)
		/// </summary>
        public override IAntibody CreateNew()
		{
            return new ImprMapShortAntibody(Map, DisVTemp);
		}

		/// <summary>
        /// Clone the Antibody
		/// </summary>
        public override IAntibody Clone()
		{
            return new ImprMapShortAntibody(this);
		}

        #region IComparable<double> 成员

        public int CompareTo(double other)
        {

            throw new NotImplementedException();
        }

        #endregion
        /// <summary>
        /// 生成抗体
        /// </summary>
        public override void Generate()
        {
  
            for (int i = 0; i < length; i++)
            {
                int max = this.Map.PolygonOObjs[i].TrialPosList.Count;
                // generate next value
                val[i] = (ushort)rand.Next(max);
            }
        }
        /// <summary>
        /// 变异
        /// </summary>
        public override void Mutate()
        {
            // get random index
            int i = rand.Next(length);
            // randomize the gene
            int max = this.Map.PolygonOObjs[i].TrialPosList.Count;
            val[i] = (ushort)rand.Next(max);
        }
    }
}
