using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSTC_Solver.SolverMethods.NewtonRaphsonMethod.LeadingAlgorithms
{
    using SSTC_Solver_CommonInterfaces;
    internal class FactorizationLU_Doolittle : I_LinearEquationSystemSolvingAlgorithm
    {
        // INTERFACE IMPLEMENTATION
        
        public string Description { get { return "Linear equations system solving algorithm. It's faster than others, but less reliable. Calculations may fail if 0 value on main diagonal would be encountered."; } }
        public string LabelName { get { return "LU Decomposition: Doolittle Algorithm"; } }
        public bool Solve_LinearEquationSystem(double[,] A, double[] B, ref double[] X)
        {
            int i, k;
            double SUM;

            int n = A.GetLength(0);

            if (!IsSquare(A) || B.GetLength(0) != n)
            {
                X = null;
                return false;
            }

            double[,] LU = A; // only for better readability purpose

            if (LUdecDoolitle(ref LU))
            {
                X = null;
                return false;
            }

            for (i = 0; i < n; i++)
            {
                SUM = 0;
                for (k = 0; k <= i - 1; k++) SUM += LU[i, k] * X[k];
                X[i] = B[i] - SUM;
            }

            for (i = n - 1; i >= 0; i--)
            {
                SUM = 0;
                for (k = i + 1; k <= n - 1; k++) SUM += LU[i, k] * X[k];
                X[i] = (1 / LU[i, i]) * (X[i] - SUM);
            }
            return true;
        }

        // AUXILIARIES

        /// <summary>
        /// Decomposes given square matrix to L and U triangular matrices according Doolitle Algorithm. Returns false if something will go wrong.
        /// </summary>
        private bool LUdecDoolitle(ref double[,] entryArray)
        {

            int i, j, k;
            double SUM = 0;

            if (IsSquare(entryArray)) return false;
            if (HasZeroesOnDiagonal(entryArray)) return false;

            int n = entryArray.GetLength(0);

            for (i = 0; i < n; i++)
            {
                for (j = 0; j < n; j++)
                {
                    if (i <= j)
                    {
                        for (k = 0; k <= i - 1; k++) SUM += entryArray[i, k] * entryArray[k, j];
                        entryArray[i, j] = entryArray[i, j] - SUM;
                        SUM = 0;
                    }
                }

                for (j = 0; j < n; j++)
                {
                    if (j > i)
                    {
                        for (k = 0; k <= i - 1; k++) SUM += entryArray[j, k] * entryArray[k, i];
                        entryArray[j, i] = (1 / entryArray[i, i]) * (entryArray[j, i] - SUM);
                        SUM = 0;
                    }
                }
            }
            return true;
        }
        // Checks if given double two dimmensional array is square.
        private  bool IsSquare(double[,] arrayToCheck)
        {
            if (arrayToCheck.GetLength(0) == arrayToCheck.GetLength(1)) return true;
            else return false;
        }
        // Looking for zeroes on diagonal of given square array. Returns true if would find one. 
        private bool HasZeroesOnDiagonal(double[,] arrayToCheck, double eps = 1e-12)
        {
            for (int i = 0; i < arrayToCheck.GetLength(0); i++)
            {
                if (Math.Abs(arrayToCheck[i, i]) <= eps) return true;
            }
            return false;
        }
    }
}
