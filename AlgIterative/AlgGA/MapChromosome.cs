using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AForge.Genetic;
using AuxStructureLib;

namespace AlgIterative.AlgGA
{
    /// <summary>
    /// 染色体
    /// </summary>
    public class MapChromosome : ShortArrayChromosome
    {
        private SDS Map = null;
        private DispVectorTemplate DisVTemp = null;
        public int ConflictsNo=0;
        public double ConflictsCost = 0;


		/// <summary>
		/// Constructor
		/// </summary>
        public MapChromosome(SDS map, DispVectorTemplate disVTemp)
            : base(map.PolygonOObjs.Count, disVTemp.TrialPosiList.Count-1)
		{
			this.Map = map;
            this.DisVTemp = disVTemp;
		}

		/// <summary>
		/// Copy Constructor
		/// </summary>
        protected MapChromosome(MapChromosome source)
            : base(source)
		{
            this.Map = source.Map;
            this.DisVTemp = source.DisVTemp;
            this.ConflictsNo = source.ConflictsNo;
            this.ConflictsCost = source.ConflictsCost;
		}

		/// <summary>
		/// Create new random chromosome (factory method)
		/// </summary>
		public override IChromosome CreateNew( )
		{
            return new MapChromosome(Map, DisVTemp);
		}

		/// <summary>
		/// Clone the chromosome
		/// </summary>
		public override IChromosome Clone( )
		{
            return new MapChromosome(this);
		}
    }
}
