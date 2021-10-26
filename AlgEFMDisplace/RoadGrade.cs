using System;
using System.Collections.Generic;
using System.Text;

namespace AlgEFMDisplace
{
    /// <summary>
    /// 道路等级及形状参数信息
    /// </summary>
    public  class RoadGrade
    {
        public string  LyrName;   //道路图层名称
        public int Grade;         //道路等级
        public double SylWidth;   //道路符号宽度
        //形状参数
        public double a;
        public double b;
        /// <summary>
        /// 道路等级
        /// </summary>
        /// <param name="_lyrName">图层名称</param>
        /// <param name="_Grade">道路等级</param>
        public RoadGrade(string _lyrName, int _Grade,double sylWidth)
        {
            LyrName = _lyrName;
            Grade = _Grade;
            SylWidth = sylWidth;
            ComParams();
        }

        /// <summary>
        /// 计算形状参数
        /// </summary>
        private  void  ComParams()
        {
            switch (AlgSnakes.GradeModelType)
            {
                case GradeModelType.Ratio:
                    {

                        this.a = AlgSnakes.a * Math.Pow(AlgSnakes.g1, (double)(Grade - 1));
                        this.b = AlgSnakes.b * Math.Pow(AlgSnakes.g2, (double)(Grade - 1));
                    }
                    break;
                case GradeModelType.Squence:
                    {
                        this.a = AlgSnakes.a + (Grade - 1)*AlgSnakes.d1;
                        this.b = AlgSnakes.b + (Grade - 1)*AlgSnakes.d2;
                    }
                    break;
                case GradeModelType.Interactive:
                    break;

            }
        }
    }
}
