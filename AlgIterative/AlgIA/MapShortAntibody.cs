using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;

namespace AlgIterative.AlgIA
{
    /// <summary>
    /// 地图解决方案抗体
    /// </summary>
    public class MapShortAntibody : ShortArrayAntibody
    {
        private SDS Map = null;
        private DispVectorTemplate DisVTemp = null;
        public int ConflictsNo=0;
        public double ConflictsCost = 0;


		/// <summary>
		/// Constructor
		/// </summary>
        public MapShortAntibody(SDS map, DispVectorTemplate disVTemp)
            : base(map.PolygonOObjs.Count, disVTemp.TrialPosiList.Count-1)
		{
			this.Map = map;
            this.DisVTemp = disVTemp;
		}

		/// <summary>
		/// Copy Constructor
		/// </summary>
        protected MapShortAntibody(MapShortAntibody source)
            : base(source)
		{
            this.Map = source.Map;
            this.DisVTemp = source.DisVTemp;
            this.ConflictsNo = source.ConflictsNo;
            this.ConflictsCost = source.ConflictsCost;
		}

		/// <summary>
        /// Create new random Antibody (factory method)
		/// </summary>
        public override IAntibody CreateNew()
		{
            return new MapShortAntibody(Map, DisVTemp);
		}

		/// <summary>
        /// Clone the Antibody
		/// </summary>
        public override IAntibody Clone()
		{
            return new MapShortAntibody(this);
		}

        #region IComparable<double> 成员

        public int CompareTo(double other)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
