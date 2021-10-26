using System;
using System.Collections.Generic;
using System.Text;

namespace TSP
{
    /// <summary>
    /// Fitness function for TSP task (Travaling Salasman Problem)
    /// </summary>
    public class FitnessFunction 
    {
        // map
        private double[,] map = null;

        // Constructor
        public FitnessFunction(double[,] map)
        {
            this.map = map;
        }

        /// <summary>
        /// Evaluate chromosome - calculates its fitness value
        /// </summary>
        public double Evaluate(State state)
        {
            return PathLength(state);
        }

        /// <summary>
        /// Translate genotype to phenotype 
        /// </summary>
        public object Translate(State state)
        {
            return state.ToString();
        }

        /// <summary>
        /// Calculate path length represented by the specified chromosome 
        /// </summary>
        public double PathLength(State state)
        {
            // salesman path
            int[] path = state.traveralPath;

            // check path size
            if (path.Length != map.GetLength(0))
            {
                throw new ArgumentException("Invalid path specified - not all cities are visited");
            }

            // path length
            int prev = path[0];
            int curr = path[path.Length - 1];

            // calculate distance between the last and the first city
            double dx = map[curr, 0] - map[prev, 0];
            double dy = map[curr, 1] - map[prev, 1];
            double pathLength = Math.Sqrt(dx * dx + dy * dy);

            // calculate the path length from the first city to the last
            for (int i = 1, n = path.Length; i < n; i++)
            {
                // get current city
                curr = path[i];

                // calculate distance
                dx = map[curr, 0] - map[prev, 0];
                dy = map[curr, 1] - map[prev, 1];
                pathLength += Math.Sqrt(dx * dx + dy * dy);

                // put current city as previous
                prev = curr;
            }

            return pathLength;
        }
    }
}
