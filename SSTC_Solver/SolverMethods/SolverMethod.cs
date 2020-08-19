
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSTC_Solver.SolverMethods
{
    using SSTC_Solver_CommonInterfaces;
    using System.Windows.Controls;

    public abstract class SolverMethod : INotifyPropertyChanged
    {
        public string LabelName { get; protected set; }
        public string Description { get; protected set; }
        public bool IsReady { get { return CheckReadiness(); } }

        // CONTROL HANDLED EVENTS

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        // SOLVER HANDLED EVENTS

        /// <summary>
        /// Fired up when certain calculation progress level has been reached.
        /// </summary>
        public event ProgressChangedEventHandler IterationProgressReached;

        protected virtual void OnCalculationsProgressReached(int? currentStep)
        {
            if (!currentStep.HasValue)
            {
                IterationProgressReached?.Invoke(this, new ProgressChangedEventArgs(-1, "LEQS solving internal error."));
            }

            IterationProgressReached?.Invoke(this, new ProgressChangedEventArgs(currentStep.Value, "Iterating..."));
        }

        // METHODS
        public abstract bool SetMathModel(object correspondingMathModel);
        public abstract bool Solve(ref double[] results);
        public abstract void ResetToDefaultSettings();
        protected abstract bool CheckReadiness();
    }
}
