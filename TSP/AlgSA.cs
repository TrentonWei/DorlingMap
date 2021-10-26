using System;
using System.Collections.Generic;
using System.Text;

namespace TSP
{
    public class AlgSA
    {
        //简单数据结构数据
        public FitnessFunction FitFunc = null;
        double[,] Map = null;
        public State State = null;
        public double T0 = 1000;
        public double X = 0.99;
        public int M = 100;
        public int N = 100;
        



        public AlgSA(
                   double[,] Map,
                   double t0,//初始温度
                   double x,//温度降温速率
                   int m,  //抽样终止条件
                   int n)  //迭代终止条件
        {
            this.Map = Map;
            this.FitFunc = new FitnessFunction(Map);
            this.State = new State(Map.GetLength(0));
            T0 = t0;
            X = x;
            M = m;
            N = n;
        }

        /// <summary>
        /// 抽样
        ///（1）令k=0时的初始当前状态为s’(0)=s(i)，q=0；
        ///（2）由状态s通过状态产生函数产生新状态s’，计算增量∆C’=C(s’)-C(s)；
        ///（3）若∆C’<0，则接受s’作为当前解，并判断C(s’)< C(s*’) ?若是，则令s*’=s’，q=0；否则，令q=q+1。若∆C’>0，则以概率exp(-∆C’/t)接受s’作为下一当前状态；
        ///（4）令k=k+1，判断q>m1? 若是，则转第(5)步；否则，返回第(2)步；
        ///（5）将当前最优解s*’和当前状态s’(k)返回改进退火过程。
        /// </summary>
        /// <param name="t">温度</param>
        /// <param name="curState">当前状态</param>
        /// <param name="bestState">当前最优状态</param>
        /// <param name="M">终止条件，当在curState的邻域下经M次移动无法得到更优的目标值时结束本次抽样</param>
        public void DoSampling(double t, ref State curState, out State bestState, int M)
        {
            int q = 0;
            double a = this.FitFunc.Evaluate(curState);
            curState.fitness = a;
            bestState = curState.Clone() as State;
            double b = a;
            double dE = 0;
            int p1,p2;
            while (q < M)
            {
                curState.ChangeState(out p1,out p2);
                b = this.FitFunc.Evaluate(curState);

                dE = b - a;
                if (dE < 0)
                {
                    a = b;
                    curState.fitness = a;
                    if (bestState.fitness > a)
                    {
                        bestState = curState.Clone() as State;
                        bestState.fitness = a;
                        q = 0;
                    }
                    q++;

                }
                else
                {
                    double P = Math.Exp((-1.0) * dE / t);
                    double R = State.rand.NextDouble();
                    if (R < P)
                    {
                        // curState = newState;
                        a = b;
                        curState.fitness = a;
                    }
                    else
                    {
                        curState.RecoverState(p1, p2);
                    }
                    q++;
                }
            }
        }

        public void DoSampling1(double t, ref State curState, out State bestState, int M)
        {
            int q = 0;
            double a = this.FitFunc.Evaluate(curState);
            curState.fitness = a;
            bestState = curState.Clone() as State;
            double b = a;
            double dE = 0;
            int p1, p2;

            while (q < M)
            {
                curState.ChangeState(out p1, out p2);
                b = this.FitFunc.Evaluate(curState);

                dE = b - a;
                if (dE < 0)
                {

                    a = b;
                    curState.fitness = a;
                    if (bestState.fitness > a)
                    {
                        bestState.fitness = a;
                        bestState = curState.Clone() as State;
                        q = 0;
                    }
                    else
                    {
                        q++;
                    }

                }
                else if (dE == 0)
                    continue;
                else
                {
                    double P = Math.Exp((-1.0) * dE / t);
                    double R = State.rand.NextDouble();

                    //Random r = new Random();
                     //double R=r.NextDouble();
                    if (R < P)
                    {
                        // curState = newState;
                        a = b;
                        curState.fitness = a;
                    }
                    else
                    {
                        curState.RecoverState(p1, p2);
                    }
                    q++;
                }
            }
        }


        /// <summary>
        ///改进后的模拟退火算法
        /// （1）给定初温t0，随机产生初始状态s，令初始最优解s*=s，当前状态为s(0)=s，i=p=0；
        /// （2）令t=ti，以t，s*和s(i)调用改进的抽样过程，返回其所得最优解s*’和当前状态s’(k)，令当前状态s(i)=s’(k)；
        /// （3）判断C(s*)<C(s*’)? 若是，则令p=p+1；否则，令s*=s*’，p=0；
        /// （4）退温ti+1=update(ti)，令i=i+1；
        /// （5）判断p>m2? 若是，则转第(6)步；否则，返回第(2)步；
        /// （6）以最优解s*作为最终解输出，停止算法。
        /// </summary>
        public void DoSA()
        {
            State curState = this.State;
            double a = this.FitFunc.Evaluate(curState);
            double A = a;
            State bestState = curState.Clone() as State;
            int p = 0;
            double t = 0;

            t = T0;
            while (p < N)
            {
                State bestState1 = null;
                this.DoSampling1(t, ref curState, out bestState1, M);
                if (bestState1.fitness < bestState.fitness)
                {
                    bestState = bestState1;
                    p = 0;
                }
                else
                {
                    p++;//当最优状态趋于稳定后可终止
                }
                t = t * X;
            }

            this.State = bestState;
        }
 
    }
}
