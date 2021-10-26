using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuxStructureLib
{
    /// <summary>
    /// 结点基础类
    /// </summary>
        [Serializable]
    public abstract class Node
    {
        public double X;        //坐标
        public double Y;        //坐标
        public int ID;          //编号
        public int SomeValue=-1;
        public int SomeValue1 = -1;//临时存储类型
        public bool IsBoundaryPoint = false;//是否边界点

        public double dx = 0;        //坐标
        public double dy = 0;        //坐标
    }
}
