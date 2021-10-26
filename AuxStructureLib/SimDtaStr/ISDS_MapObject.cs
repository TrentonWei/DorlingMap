using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuxStructureLib
{
    /// <summary>
    /// SDS结构中地图对象接口
    /// <summary>
   
    public interface ISDS_MapObject
    {
        string ObjType { get; }
        int AID { get; }
        double SomeAtriValue { get; set; }
        void Translate(double dx, double dy);//平移
    }
}
