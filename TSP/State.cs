using System;
using System.Collections.Generic;
using System.Text;
using AForge;

namespace TSP
{
    public class State
    {
        public int citiesNum = 0;
        public int[] traveralPath = null;
        public double fitness =double.PositiveInfinity;
        public static ThreadSafeRandom rand = new ThreadSafeRandom();

        public State(int n)
        {
            this.citiesNum = n;
            this.traveralPath = new int[citiesNum];
            GenerateState();
        }

        public State(State source)
        {
            // copy all properties
            citiesNum = source.citiesNum;
            traveralPath = (int[])source.traveralPath.Clone();
            fitness = source.fitness;
        }


        public State Clone()
        {
            return new State(this);
        }

        /// <summary>
        /// 生成一个状态
        /// </summary>
        private void GenerateState()
        {
            int length = citiesNum;
			// create ascending permutation initially
			for ( int i = 0; i < length; i++ )
			{
                traveralPath[i] = (ushort)i;
			}

			// shufle the permutation
            for (int i = 0, n = length >> 1; i < n; i++)
			{
				int t;
				int j1 = rand.Next( length );
				int j2 = rand.Next( length );

				// swap values
                t = traveralPath[j1];
                traveralPath[j1] = traveralPath[j2];
                traveralPath[j2] = t;
			}
        }

        public  void ChangeState(out int j1,out int j2 )
        {
            int t;
            j1 = rand.Next(this.citiesNum);
            j2 = rand.Next(this.citiesNum);

            // swap values
            t = traveralPath[j1];
            traveralPath[j1] = traveralPath[j2];
            traveralPath[j2] = t;
        }

        public void RecoverState(int j1, int j2)
        {
            int t;
            // swap values
            t = traveralPath[j1];
            traveralPath[j1] = traveralPath[j2];
            traveralPath[j2] = t;
        }
    }
}
