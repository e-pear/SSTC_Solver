using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSTC_Solver.SolverMethods.NewtonRaphsonMethod.LeadingAlgorithms
{
    using SSTC_Solver_CommonInterfaces;
    using System.Diagnostics.Eventing.Reader;

    class FactorizationLU_GaussCrout : I_LinearEquationSystemSolvingAlgorithm
    {
        private double eps;
        
        public FactorizationLU_GaussCrout(double? eps)
        {
            if (eps.HasValue) this.eps = eps.Value;
            else eps = 1e-15;
        }
        // INTERFACE IMPLEMENTATION
        public string Description { get { return "Linear equations system solving algorithm. Slower, but immune on main diagonal 0 value possible occurances."; } }
        public string LabelName { get { return "LU Decomposition: Crout Algorithm"; } }

        public bool Solve_LinearEquationSystem(double[,] A, double[] B, ref double[] X)
        {

            int i, j, k, n, p;  // indexes
            double s, m;        // auxillary variables
            double[,] AB;       // combined matrix
            int[] P;            // permutation vector

            int nA0 = A.GetLength(0);
            int nA1 = A.GetLength(1);
            int nB = B.GetLength(0);

            if (nA0 != nA1 || nA0 != nB || nA1 != nB) return false;

            n = nA0;
            X = new double[n];
            P = new int[n + 1];
            AB = new double[n, n + 1];

            for (p = 0; p <= n; p++) P[p] = p;
            for (i = 0; i < n; i++)
            {
                for (j = 0; j <= n; j++)
                {
                    if (j < n) AB[i, j] = A[i, j];
                    if (j == n) AB[i, j] = B[i];
                }
            }

            for (i = 0; i < n - 1; i++)
            {
                k = i;
                for (j = i + 1; j < n; j++)
                {
                    if (Math.Abs(AB[i, P[k]]) < Math.Abs(AB[i, P[j]])) k = j;
                    if (!Swap(P, k, i)) return false;
                }
                for (j = i + 1; j < n; j++)
                {
                    if (Math.Abs(AB[i, P[i]]) < eps) return false;

                    m = (-1) * (AB[j, P[i]] / AB[i, P[i]]);

                    for (k = i + 1; k <= n; k++) AB[j, P[k]] += m * AB[i, P[k]];
                }
            }

            for (i = n - 1; i >= 0; i--)
            {
                if (Math.Abs(AB[i, P[i]]) < eps) return false;

                s = AB[i, n];

                for (j = n - 1; j >= 0; j--) s -= AB[i, P[j]] * X[P[j]];

                X[P[i]] = s / AB[i, P[i]];
            }
            return true;
        }
        // AUXILIARIES
        private bool Swap<T>(T[] vector, int initialIndex, int targetIndex)
        {
            T buffer;
            if (initialIndex >= vector.Length || targetIndex >= vector.Length) return false;

            buffer = vector[initialIndex];

            vector[initialIndex] = vector[targetIndex];
            vector[targetIndex] = buffer;

            return true;
        }
    }
}
