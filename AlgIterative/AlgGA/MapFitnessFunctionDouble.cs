using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AuxStructureLib;
using AForge.Genetic;
using AForge.Math.Random;
using AForge;

namespace AlgIterative.AlgGA
{
    public class MapFitnessFunctionDouble:IFitnessFunction
    {
        public SDS Map = null;
        public ContinuousDVT DisVTemp = null;
        ConflictDetection ConflictDetector = null;
        public double Dstancetol = 0;
        public double PLCost = 10;
        public double PPCost = 5;
        public double DispCost = 1;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="map">地图对象</param>
        public MapFitnessFunctionDouble(SDS map,
            ContinuousDVT disVTemp,
            ConflictDetection conflictDetector,
            double dstancetol,
            double pLCost,
            double pPCost,
            double dispCost)
        {
            this.Map = map;
            DisVTemp = disVTemp;
            ConflictDetector = conflictDetector;
            Dstancetol = dstancetol;
            PLCost = pLCost;
            PPCost = pPCost;
            DispCost = dispCost;
        }

        #region IFitnessFunction 成员
        /// <summary>
        /// 计算chromosome的适应值
        /// </summary>
        /// <param name="chromosome">1个个体</param>
        /// <returns>适应值</returns>
        public double Evaluate(IChromosome chromosome)
        {
            return 1 / (ConflictCost(chromosome) + 1);
        }
        #endregion

        /// <summary>
        /// 计算冲突评价值
        /// </summary>
        /// <param name="chromosome">1个个体</param>
        /// <returns>适应值</returns>
        public double ConflictCost(IChromosome chromosome)
        {
            double conflictCost=0;
            double[] diaplaceV = ((MapChromosomeDouble)chromosome).Value;
            double[] CalDeltaX_Y = null;
            double DCost = 0;
            // check path size
            if ((diaplaceV.Length/2) != this.Map.PolygonOObjs.Count)
            {
                throw new ArgumentException("错误的地图配置，目标个数不匹配！");
            }
            CalDeltaX_Y=this.CalDeltaX_Y(diaplaceV,out DCost);
            MoveObjects(CalDeltaX_Y);
            //if (HasTriangleInverted(state))//如果存在拓扑冲突则冲突值设为正无穷。
               // return double.PositiveInfinity;
            List<Conflict> clist = ConflictDetector.ConflictDetect(Dstancetol);
            ((MapChromosomeDouble)chromosome).ConflictsNo = clist.Count;
            conflictCost = CostFunction(clist, DCost);
            ((MapChromosomeDouble)chromosome).ConflictsCost = conflictCost;

            ReturnObject(CalDeltaX_Y);

            return conflictCost;
        }
        /// <summary>
        /// 计算DX与DY
        /// </summary>
        /// <param name="diaplaceV">移位向量（直角坐标和极坐标两种）</param>
        /// <returns></returns>
        public double[] CalDeltaX_Y(double[] diaplaceV,out double DCost)
        {
            DCost = 0;
            int n = diaplaceV.Length / 2;
            double[] calDeltaX_Y = new double[2 * n];
            if (this.DisVTemp.IsPolarCoors == false)
            {
                for (int i = 0; i < n; i++)
                {
                    calDeltaX_Y[2 * i] = diaplaceV[2 * i];
                    calDeltaX_Y[2 * i+1] = diaplaceV[2 * i + 1];
                    DCost += Math.Sqrt(calDeltaX_Y[2 * i] * calDeltaX_Y[2 * i] + calDeltaX_Y[2 * i + 1] * calDeltaX_Y[2 * i + 1]);

                }
            }
            else
            {
                double r = 0;
                double a = 0;
                for (int i = 0; i < n; i++)
                {
                    r = diaplaceV[2 * i];
                    a = diaplaceV[2 * i + 1];
                    if (r != 0)
                    {
                        calDeltaX_Y[2 * i] = r * Math.Cos(a);
                        calDeltaX_Y[2 * i + 1] = r * Math.Sin(a);
                        DCost += r;
                    }
                    else
                    {
                        calDeltaX_Y[2 * i] =0;
                        calDeltaX_Y[2 * i + 1] = 0;
                    }
                }
            }
            return calDeltaX_Y;
        }
        /// <summary>
        /// 移动到一个新的状态
        /// </summary>
        /// <param name="state"></param>
        public void MoveObjects(double[] CalDeltaX_Y)
        {
            int n = CalDeltaX_Y.Length / 2;
            double curX = -1;
            double curY = -1;
            for (int i = 0; i < n; i++)
            {
                curX = CalDeltaX_Y[2 * i];
                curY = CalDeltaX_Y[2 * i + 1];
                if (curX != 0 && curY != 0)
                {
                    this.Map.PolygonOObjs[i].Translate(curX, curY);
                }
            }
        }
        /// <summary>
        /// 返回原来的位置
        /// </summary>
        /// <param name="state"></param>
        private void ReturnObject(double[] CalDeltaX_Y)
        {
            int n = CalDeltaX_Y.Length/2;
            double curX = -1;
            double curY = -1;
            for (int i = 0; i < n; i++)
            {
                curX = (-1) * CalDeltaX_Y[2 * i];
                curY = (-1) * CalDeltaX_Y[2 * i + 1];
                if (curX != 0 && curY != 0)
                {
                    this.Map.PolygonOObjs[i].Translate(curX, curY);
                }
            }
        }
        /// <summary>
        /// 检查是否有三角形穿越现象(拓扑检查)
        /// </summary>
        /// <returns>是否有拓扑错误</returns>
        private bool HasTriangleInverted(ushort[] state)
        {
            int n=state.Length;
            for (int i = 0; i < n; i++)
            {
                if (state[i] !=0)
                {
                    ISDS_MapObject Obj = this.Map.PolygonOObjs[i];
                    if (ConflictDetector.HasTriangleInverted(Obj as SDS_PolygonO))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 计算总状态值
        /// </summary>
        /// <returns>状态值</returns>
        private double CostFunction(List<Conflict> Conflicts, double DCost)
        {
            double cost = 0;

            foreach (Conflict conflict in Conflicts)
            {
                if (conflict.ConflictType == @"PP")
                {
                    //cost += this.PPCost;
                    cost += (this.PPCost * (this.Dstancetol - conflict.Distance));
                }
                else if (conflict.ConflictType == @"PL")
                {

                    //cost += this.PLCost;
                    cost += (this.PLCost * (this.Dstancetol - conflict.Distance));

                }
            }
            cost += DCost*this.DispCost;
            return cost;
        }

    }
}
