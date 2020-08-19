using SSTC_BaseModel;
using SSTC_Solver.SolverMethods;
using SSTC_ViewResources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SSTC_Solver
{
    public class SolverViewModel : INotifyPropertyChanged
    {
        private SolverModel solverModel;
        private BackgroundWorker worker;

        private int _progress;
        private string _label;

        private bool _isProgressVisible;

        public SolverViewModel(double modelEPS = 1e-09, double modelGravAcceleration = 9.80665)
        {
            solverModel = new SolverModel(modelEPS, modelGravAcceleration);
            solverModel.SubscribeToCalculationAndIterationProgressUpdate(OnSolverProgressUpdateNotification);

            worker = new BackgroundWorker();

            CalculateCommand = new CommandRelay(Calculate, CanExecute_CalculateCommand);
            ResetMethodParametersToDefaultCommand = new CommandRelay(solverModel.ResetSelectedSolverMethodParameters, () => true);

            CalculationsInProgress = false;

            ResetLabels();
        }
        // STATE PROPERTIES
        private bool _calculationsInProgress;
        public bool CalculationsInProgress 
        {
            get { return _calculationsInProgress; } 
            private set
            {
                _calculationsInProgress = value;
                RaisePropertyChanged("CalculationsInProgress");
            }
        }
        
        // PROPERTIES
        public ObservableCollection<SolverMethod> AvailableSolverMethods { get { return new ObservableCollection<SolverMethod>(solverModel._availableSolverMethods); } }
        public SolverMethod SelectedSolverMethod
        {
            get { return solverModel._selectedSolverMethod; }
            set { solverModel._selectedSolverMethod = value; RaisePropertyChanged("SelectedSolverMethod"); }
        }
        
        public int ProgressLabel
        {
            get { return _progress; }
            private set { _progress = value; RaisePropertyChanged("ProgressLabel"); }
        }
        public string TextLabel
        {
            get { return _label; }
            private set { _label = value; RaisePropertyChanged("TextLabel"); }
        }
        public bool IsProgressVisible
        {
            get { return _isProgressVisible; }
            private set { _isProgressVisible = value; RaisePropertyChanged("IsProgressVisible"); }
        }

        // COMMANDS
        public ICommand CalculateCommand { get; }
        public ICommand ResetMethodParametersToDefaultCommand { get; }
        // EVENTS
        public event PropertyChangedEventHandler PropertyChanged;
        // RAISERS
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        // HANDLERS
        protected virtual void OnSolverProgressUpdateNotification(object sender, ProgressChangedEventArgs args)
        {
            double p;
            int n, progress;
            
            if(sender is SolverMethod)
            {
                n = args.ProgressPercentage;
                if (n < 0)
                {
                    worker.CancelAsync();
                }
                else
                {
                    p = (double)n;
                    progress = (int)((p + p) / (p + (100 + p)) * 70 + 20);
                    worker.ReportProgress(progress, args.UserState);
                }
            }
            else
            {
                n = args.ProgressPercentage;
                if (n < 0) worker.ReportProgress(0, args.UserState);
                worker.ReportProgress(args.ProgressPercentage, args.UserState);
                Thread.Sleep(500);
            }
        }
        // SUBSCRIBERS
        public void SubscribeOn_SolverCalculationsReportSent_Event(EventHandler handler)
        {
            solverModel.CalculationsReportSent += handler;
        }
        public void UnSubscribeOn_SolverCalculationsReportSent_Event(EventHandler handler)
        {
            solverModel.CalculationsReportSent -= handler;
        }
        // SETTERS
        public void SetConductor(Conductor conductor)
        {
            solverModel.SetConductorParams(conductor);
            ResetLabels();
        }
        public void SetConditionParams(double? H0, double? T1, double? T2)
        {
            if (H0.HasValue && H0 > 0) solverModel.SetInitialTension_H0_Param(H0.Value);
            if (T1.HasValue && T1.Value > (-273.15)) solverModel.SetInitialTemperature_T1_Param(T1.Value);
            if (T2.HasValue && T2.Value > (-273.15)) solverModel.SetTargetTemperature_T2_Param(T2.Value);

            ResetLabels();
        }
        public void SetSectionParams(IEnumerable<ISpanModel> sectionModel)
        {
            solverModel.SetSectionParams(sectionModel);
            ResetLabels();
        }
        // METHODS
        private void Calculate()
        {
            IsProgressVisible = true;
            
            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;

            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            
            worker.RunWorkerAsync();
        }
        private bool CanExecute_CalculateCommand()
        {
            return solverModel.CanPerformCalculations();
        }
        // AUX
        private void ResetLabels()
        {
            TextLabel = "Calculate";
            ProgressLabel = 0;
            IsProgressVisible = false;
        }
        // BACKGROUND WORKER KINGDOM
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            CalculationsInProgress = true;
            solverModel.Calculate();
        }
        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            TextLabel = (string)e.UserState;
            ProgressLabel = e.ProgressPercentage;
        }
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ResetLabels();
            TextLabel = "Recalculate";
            if (e.Error != null)
            {
                MessageBox.Show("Solver Error:", "There was an error running the process. The thread aborted.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            CalculationsInProgress = false;
        }
    }
}
