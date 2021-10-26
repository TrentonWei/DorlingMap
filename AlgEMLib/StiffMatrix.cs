using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatrixOperation;
using AuxStructureLib;

namespace AlgEMLib
{
    /// <summary>
    /// 刚度矩阵
    /// </summary>
    public abstract class StiffMatrix
    {
        protected  Matrix _K = null;                                            //刚度矩阵
    }

}
