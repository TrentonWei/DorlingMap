using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuxStructureLib.ConflictLib
{
    /// <summary>
    /// 冲突基础类
    /// </summary>
    public class ConflictBase
    {
        public Skeleton_Arc Skel_arc = null;
        public string Type;//LL, RR and RL
        public double DisThreshold;
    }
}
