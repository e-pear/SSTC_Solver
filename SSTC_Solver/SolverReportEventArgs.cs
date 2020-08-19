using SSTC_BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSTC_Solver
{
    public class SolverReportEventArgs : EventArgs
    {
        public List<string> AdditionalInformation { get; }
        public List<ResultantSpan> Results { get; }
        public double[] SolutionsVector { get; }
        public bool? AreCalculationsSucceed { get; }

        public SolverReportEventArgs(bool? calculationsResultIndicator, double[] solutionsVector, List<ResultantSpan> calculationsResult, List<string> additionalInfo)
        {
            AdditionalInformation= additionalInfo;
            SolutionsVector = solutionsVector;
            Results = calculationsResult;
            AreCalculationsSucceed = calculationsResultIndicator;
        }
    }
}
