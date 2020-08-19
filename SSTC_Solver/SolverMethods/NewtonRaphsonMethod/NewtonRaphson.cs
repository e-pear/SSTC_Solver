using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SSTC_Solver.SolverMethods.NewtonRaphsonMethod
{
    using SSTC_Solver.SolverMethods.NewtonRaphsonMethod.LeadingAlgorithms;
    using SSTC_Solver_CommonInterfaces;
    using System.Collections.ObjectModel;

    internal class NewtonRaphson : SolverMethod
    {
        // INTERNAL FIELDS

        protected I_NewtonRaphson_SolveableModel _mathModel;
        protected int _baseVectorSize;

        protected double[] _vector_mF;
        protected double[,] _jacobian;

        protected double[] _vector_X0;

        protected double[] _vector_X1;
        protected double[] _vector_D;

        // LEADING ALGORITHMS

        protected I_LinearEquationSystemSolvingAlgorithm _selected_LEQS_SolvingAlgorithm;

        public ObservableCollection<I_LinearEquationSystemSolvingAlgorithm> Available_LEQS_SolvingAlgorithms { get; protected set; }
        public I_LinearEquationSystemSolvingAlgorithm Selected_LEQS_SolvingAlgorithm 
        { 
            get { return _selected_LEQS_SolvingAlgorithm; }
            set 
            { 
                _selected_LEQS_SolvingAlgorithm = value; 
                RaisePropertyChanged("Selected_LEQS_SolvingAlgorithm");
            } 
        }


        // EXTERNAL (LOGIC) PROPERTIES

        private double? _methodEps;
        private int? _maxStep;
        private double? _expectedInitialSolutionVectorLeadingMember;

        public double? EPS
        {
            get { return _methodEps; }
            set
            {
                if (_methodEps != value)
                {
                    _methodEps = value;
                    RaisePropertyChanged("EPS");
                    RaisePropertyChanged("IsReady");
                }
            }
        }
        public int? MaxStep
        {
            get { return _maxStep; }
            set
            {
                if (_maxStep != value)
                {
                    _maxStep = value;
                    RaisePropertyChanged("MaxStep");
                    RaisePropertyChanged("IsReady");
                }
            }
        }
        public double? ExpectedInitialSolutionVectorLeadingMember
        {
            get { return _expectedInitialSolutionVectorLeadingMember; }
            set
            {
                if (_expectedInitialSolutionVectorLeadingMember != value)
                {
                    _expectedInitialSolutionVectorLeadingMember = value;
                    RaisePropertyChanged("ExpectedInitialSolutionVectorLeadingMember");
                    RaisePropertyChanged("IsReady");
                }
            }
        }
        // DEFAULT (RE)SETTER
        public override void ResetToDefaultSettings()
        {
            _methodEps = 1e-12;
            _maxStep = 10000;
            _expectedInitialSolutionVectorLeadingMember = 1;
            RaisePropertyChanged("EPS");
            RaisePropertyChanged("MaxStep");
            RaisePropertyChanged("ExpectedInitialSolutionVectorLeadingMember");
            RaisePropertyChanged("IsReady");
        }

        // MATHMODEL SETTER
        public override bool SetMathModel(object correspondingMathModel)
        {
            if (correspondingMathModel is I_NewtonRaphson_SolveableModel)
            {
                _mathModel = correspondingMathModel as I_NewtonRaphson_SolveableModel;
                _baseVectorSize = _mathModel.VectorBaseSize;

                _vector_D = new double[_baseVectorSize];
                _vector_X1 = new double[_baseVectorSize];
                _vector_X0 = new double[_baseVectorSize];

                return true;
            }
            else return false;
        }

        // CONSTRUCTOR

        public NewtonRaphson()
        {
            LabelName = "Newton-Raphson";
            Description = "Base solving method.";

            Available_LEQS_SolvingAlgorithms = new ObservableCollection<I_LinearEquationSystemSolvingAlgorithm>() { new FactorizationLU_Doolittle(), new FactorizationLU_GaussCrout(_methodEps) };
            Selected_LEQS_SolvingAlgorithm = Available_LEQS_SolvingAlgorithms[0];

            ResetToDefaultSettings();
        }

        // BASE METHOD
        public override bool Solve(ref double[] results)
        {
            int currentStep = 0;

            do
            {
                currentStep++;
                OnCalculationsProgressReached(currentStep);

                if(currentStep == 1)
                {
                    for (int i = 0; i < _baseVectorSize; i++)
                    {
                        _vector_X0[i] = _expectedInitialSolutionVectorLeadingMember.Value;
                    }
                }
                else
                {
                    for (int i = 0; i < _baseVectorSize; i++)
                    {
                        _vector_X0[i] = _vector_X1[i];
                    }
                }

                _jacobian = _mathModel.Get_Jacobian_J(_vector_X0);
                _vector_mF = ReverseVectorSign(_mathModel.Get_Vector_F(_vector_X0));

                if (!_selected_LEQS_SolvingAlgorithm.Solve_LinearEquationSystem(_jacobian,_vector_mF,ref _vector_D))
                {
                    OnCalculationsProgressReached(null);
                    return false;
                }

                for (int i = 0; i < _baseVectorSize; i++)
                {
                    _vector_X1[i] = _vector_X0[i] + _vector_D[i];
                }

            } while (CalculationCondition(currentStep));

            results = _vector_X1;
            return true;
        }

        // AUX IN-SOLVING METHODS
        protected virtual double[] ReverseVectorSign(double[] vector)
        {
            int vectorLength = vector.Length;
            double[] result = new double[vectorLength];

            for (int i = 0; i < vectorLength; i++)
            {
                result[i] = vector[i] * (-1);
            }

            return result;
        }
        protected virtual bool CalculationCondition(int currentStep)
        {
            bool option1 = (_methodEps != null);
            bool option2 = (_maxStep != null);

            int counter = 0;

            if (option1 && option2)
            {
                for (int i = 0; i < _baseVectorSize; i++)
                {
                    if (Math.Abs(_vector_X1[i] - _vector_X0[i]) > _methodEps) counter++;
                }

                if (counter == 0) return false;             //stop calculations
                if (currentStep >= _maxStep) return false;   //stop calculations
                return true;                                //continue calculations
            }
            else if (option1 && !option2)
            {
                for (int i = 0; i < _baseVectorSize; i++)
                {
                    if (Math.Abs(_vector_X1[i] - _vector_X0[i]) > _methodEps) counter++;
                }

                if (counter == 0) return false;             //stop calculations
                else return true;                           //continue calculations
            }
            else
            {
                if (currentStep >= _maxStep) return false;   //stop calculations
                else return true;                           //continue calculations
            }
        }

        // AUX METHODS

        protected virtual bool CheckAnyZeroesOrNullsInExpectedInitialSolutionVector()
        {
            if (!(_expectedInitialSolutionVectorLeadingMember.HasValue) || _expectedInitialSolutionVectorLeadingMember == 0) return true;
            else return false;
        }
        protected override bool CheckReadiness()
        {
            if (EPS == null || MaxStep == null) return false;
            if (CheckAnyZeroesOrNullsInExpectedInitialSolutionVector()) return false;

            return true;
        }
    }
}
