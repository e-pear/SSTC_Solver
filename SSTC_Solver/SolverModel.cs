using SSTC_Solver.SolverMethods;
using SSTC_BaseModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSTC_MathModel;
using SSTC_Solver.SolverMethods.NewtonRaphsonMethod;
using System.Windows.Input;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace SSTC_Solver
{
    internal class SolverModel
    {
        private bool _H0_HasBeenSet;
        private bool _T1_HasBeenSet;
        private bool _T2_HasBeenSet;

        private double[] _conductorParams;
        private double[] _conditionParams;
        private double[,] _sectionParams;

        private double _modelEps;
        private double _modelGravAccel;

        internal SolverMethod _selectedSolverMethod;
        internal List<SolverMethod> _availableSolverMethods;

        // EVENTS
        public event EventHandler CalculationsReportSent;
        public event ProgressChangedEventHandler CalculationProgressReached;
        // RAISERS
        protected virtual void SendCalculationsReport(bool? resultIndicator, double[] solutionsVector, List<ResultantSpan> result, List<string> additionalInfos)
        {
            CalculationsReportSent?.Invoke(this, new SolverReportEventArgs(resultIndicator, solutionsVector, result, additionalInfos));
        }
        protected virtual void NotifyAboutProgressUpdate(int progress, string desc)
        {
            CalculationProgressReached?.Invoke(this, new ProgressChangedEventArgs(progress, desc));
        }
        // CONSTRUCTOR
        public SolverModel(double modelEPS, double modelGravAcceleration)
        {
            _H0_HasBeenSet = false;
            _T1_HasBeenSet = false;
            _T2_HasBeenSet = false;

            _conditionParams = new double[3];

            _modelEps = modelEPS;
            _modelGravAccel = modelGravAcceleration;

            _availableSolverMethods = new List<SolverMethod> { new NewtonRaphson() };
            _selectedSolverMethod = _availableSolverMethods[0];

        }
        // SETTERS
        internal void SetInitialTension_H0_Param(double param_H0)
        {
            _conditionParams[0] = param_H0;
            _H0_HasBeenSet = true;
        }
        internal void SetInitialTemperature_T1_Param(double param_T1)
        {
            _conditionParams[1] = param_T1;
            _T1_HasBeenSet = true;
        }
        internal void SetTargetTemperature_T2_Param(double param_T2)
        {
            _conditionParams[2] = param_T2;
            _T2_HasBeenSet = true;
        }
        internal void SetSectionParams(IEnumerable<ISpanModel> sectionModel)
        {
            int count = sectionModel.Count();
            int index = 0;
            double[,] buffer = new double[count, 8];

            count = 0;
            foreach (ISpanModel span in sectionModel)
            {
                buffer[index, 0] = span.TowerAbscissa;
                buffer[index, 1] = span.TowerOrdinate;
                buffer[index, 2] = span.AttachmentPointHeight;
                buffer[index, 3] = span.InsulatorArmLength;
                buffer[index, 4] = span.InsulatorArmWeight;
                buffer[index, 5] = span.InsulatorIceLoad;
                buffer[index, 6] = span.SpanIceLoad;
                buffer[index, 7] = span.SpanWindLoad;

                index++;
            }

            _sectionParams = buffer;
        }
        internal void SetConductorParams(Conductor conductor)
        {
            _conductorParams = new double[4];

            _conductorParams[0] = conductor.CrossSection;
            _conductorParams[1] = conductor.WeightPerLength;
            _conductorParams[2] = conductor.ThermalExpansionCoefficient;
            _conductorParams[3] = conductor.ModulusOfElasticity;
        }
        // METHODS
        internal void Calculate()
        {
            IEnumerable<double[]> rawResults = new List<double[]>();
            double[] solutionsVector = new double[_sectionParams.Length - 1];
            List<ResultantSpan> results = new List<ResultantSpan>();
            List<string> report;

            NotifyAboutProgressUpdate(0, "Initializing Math Model...");

            object mathModel = new MathModel_Mk10(_conditionParams, _conductorParams, _sectionParams, 1e-03);
            ResultsPrinter printer = new ResultsPrinter((MathModel_Mk10)mathModel);

            NotifyAboutProgressUpdate(20, "Initializing Math Model...");
            _selectedSolverMethod.SetMathModel(mathModel);
            if(_selectedSolverMethod.Solve(ref solutionsVector))
            {
                report = AnalyzeProblemsInResults(solutionsVector);
                if(report.Count == 0)
                {
                    NotifyAboutProgressUpdate(90, "Calculating target values...");
                    rawResults = printer.GetResultantArray(solutionsVector);
                    foreach (var item in rawResults)
                    {
                        results.Add(new ResultantSpan(item));
                    
                    }
                    NotifyAboutProgressUpdate(100, "Calculations successful.");
                    report.Add("Calculations successful.");
                    SendCalculationsReport(true, solutionsVector, results, report);
                }
                else
                {
                    NotifyAboutProgressUpdate(-1, "Calculation failure...");
                    report.Add("Calculations failed.");
                    SendCalculationsReport(false, solutionsVector, null, report);
                }
            }
            else
            {
                SendCalculationsReport(null, null, null, new List<string>() { "Math model unknown internal error has been encountered." });
            }
        }
        internal void ResetSelectedSolverMethodParameters()
        {
            _selectedSolverMethod.ResetToDefaultSettings();
        }
        internal bool CanPerformCalculations()
        {
            if (!_H0_HasBeenSet || !_T1_HasBeenSet || !_T2_HasBeenSet) return false;
            if (_conductorParams == null) return false;
            if (_sectionParams == null) return false;
            if (!_selectedSolverMethod.IsReady) return false;
            return true;  
        }
        // AUX
        protected virtual object CreateMathModel()
        {
            return new MathModel_Mk10(_conditionParams, _conductorParams, _sectionParams, _modelEps, _modelGravAccel);
        }
        protected virtual List<string> AnalyzeProblemsInResults(double[] solutionsVector)
        {
            List<string> report = new List<string>();
            
            string zeroes_found = "Solution vector contains zero values.";
            string NaNs_found = "Solution vector contains \"not a number\" elements.";
            string negatives_found = "Solution vector contains negative values.";
            string infinities_found = "Solution vector contains infinities.";
            
            int zeroes_count = 0;
            int NaNs_count = 0;
            int negatives_count = 0;
            int infinities_count = 0;

            foreach (double result in solutionsVector)
            {
                if (double.IsNaN(result)) NaNs_count++;
                else if (double.IsInfinity(result)) infinities_count++;
                else if (result == 0) zeroes_count++;
                else if (result < 0) negatives_count++;
            }

            if (zeroes_count > 0) report.Add(zeroes_found);
            if (NaNs_count > 0) report.Add(NaNs_found);
            if (infinities_count > 0) report.Add(infinities_found);
            if (negatives_count > 0) report.Add(negatives_found);

            return report;
        }
        // SUBSCRIBER
        public void SubscribeToCalculationAndIterationProgressUpdate(ProgressChangedEventHandler handler)
        {
            foreach (var method in _availableSolverMethods)
            {
                method.IterationProgressReached += handler;
            }

            this.CalculationProgressReached += handler;
        }

    }
}
