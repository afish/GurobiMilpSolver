using System;
using Gurobi;
using MilpManager.Abstraction;
using MilpManager.Implementation;

namespace GurobiMilpManager.Implementation
{
    public class GurobiMilpSolver : BaseMilpSolver, IDisposable
    {
        public GRBEnv Environment { get; protected set; }
        public GRBModel Model { get; protected set; }
        public int ConstraintIndex { get; protected set; }
        protected GRBVar One { get; set; }
        private bool _disposed;
        private bool _hasGoal;

        public GurobiMilpSolver(int integerWidth, double epsilon = 0.000000001) : base(integerWidth, epsilon)
        {
            Environment = new GRBEnv();
            Model = new GRBModel(Environment);
            AddOne();
        }

        public GurobiMilpSolver(GRBEnv environment, GRBModel model, int integerWidth, double epsilon) : base(integerWidth, epsilon)
        {
            Environment = environment;
            Model = model;
            AddOne();
        }

        private void AddOne()
        {
            One = Model.AddVar(1, 1, 0, GRB.INTEGER, "_v_predefinedOne");
            Model.Update();
        }

        public override void SetLessOrEqual(IVariable variable, IVariable bound)
        {
            Model.AddConstr((variable.Var() ?? variable.ConstantValue.Value*One) <= (bound.Var() ?? bound.ConstantValue.Value * One), NewConstraintName());
        }

        public override void SetGreaterOrEqual(IVariable variable, IVariable bound)
        {
            Model.AddConstr((variable.Var() ?? variable.ConstantValue.Value * One) >= (bound.Var() ?? bound.ConstantValue.Value * One), NewConstraintName());
        }

        public override void SetEqual(IVariable variable, IVariable bound)
        {
            Model.AddConstr((variable.Var() ?? variable.ConstantValue.Value * One) == (bound.Var() ?? bound.ConstantValue.Value * One), NewConstraintName());
        }

        public override void SaveModelToFile(string modelPath)
        {
            if (!_hasGoal)
            {
                AddGoal("__dummy", FromConstant(0).Create());
                _hasGoal = true;
            }
            Model.Write(modelPath);
        }

        public override void Solve()
        {
            Model.Optimize();
        }

        public override double GetValue(IVariable variable)
        {
            return variable.Var().Get(GRB.DoubleAttr.X);
        }

        public override SolutionStatus GetStatus()
        {
            int optimstatus = Model.Get(GRB.IntAttr.Status);
            switch (optimstatus)
            {
                case GRB.Status.INFEASIBLE: return SolutionStatus.Infeasible;
                case GRB.Status.INF_OR_UNBD: return SolutionStatus.Unbounded;
                case GRB.Status.OPTIMAL: return SolutionStatus.Optimal;
                case GRB.Status.UNBOUNDED: return SolutionStatus.Unbounded;
                default: return SolutionStatus.Unknown;
            }
        }

        protected override IVariable InternalCreate(string name, Domain domain)
        {
            GRBVar variable;
            if (domain == Domain.AnyConstantInteger || domain == Domain.AnyInteger)
            {
                variable = Model.AddVar(double.MinValue, double.MaxValue, 0, GRB.INTEGER, name);
            }
            else if (domain == Domain.PositiveOrZeroConstantInteger || domain == Domain.PositiveOrZeroInteger)
            {
                variable = Model.AddVar(0, double.MaxValue, 0, GRB.INTEGER, name);
            }
            else if (domain == Domain.BinaryConstantInteger || domain == Domain.BinaryInteger)
            {
                variable = Model.AddVar(0, 1, 0, GRB.BINARY, name);
            }
            else if (domain == Domain.PositiveOrZeroConstantReal || domain == Domain.PositiveOrZeroReal)
            {
                variable = Model.AddVar(0, double.MaxValue, 0, GRB.CONTINUOUS, name);
            }
            else
            {
                variable = Model.AddVar(double.MinValue, double.MaxValue, 0, GRB.CONTINUOUS, name);
            }

            Model.Update();
            return new GurobiVariable
            {
                GRBVar = variable
            };
        }

        protected override IVariable InternalFromConstant(string name, int value, Domain domain)
        {
            return InternalFromConstant(name, (double) value, domain);
        }

        protected override IVariable InternalFromConstant(string name, double value, Domain domain)
        {
            return new GurobiVariable();
        }

        protected override IVariable InternalSumVariables(IVariable first, IVariable second, Domain domain)
        {
            var result = CreateAnonymous(domain);
            Model.AddConstr((first.Var() ?? first.ConstantValue.Value * One) + (second.Var() ?? second.ConstantValue.Value * One) <= result.Var(), NewConstraintName());
            Model.AddConstr((first.Var() ?? first.ConstantValue.Value * One) + (second.Var() ?? second.ConstantValue.Value * One) >= result.Var(), NewConstraintName());
            return result;
        }

        protected override IVariable InternalNegateVariable(IVariable variable, Domain domain)
        {
            var result = CreateAnonymous(domain);
            Model.AddConstr((variable.Var() ?? variable.ConstantValue.Value * One) <= -result.Var(), NewConstraintName());
            Model.AddConstr((variable.Var() ?? variable.ConstantValue.Value * One) >= -result.Var(), NewConstraintName());
            return result;
        }

        protected override IVariable InternalMultiplyVariableByConstant(IVariable variable, IVariable constant, Domain domain)
        {
            var result = CreateAnonymous(domain);
            Model.AddConstr((variable.Var() ?? variable.ConstantValue.Value * One) * (constant.ConstantValue.Value) <= result.Var(), NewConstraintName());
            Model.AddConstr((variable.Var() ?? variable.ConstantValue.Value * One) * (constant.ConstantValue.Value) >= result.Var(), NewConstraintName());
            return result;
        }

        protected override IVariable InternalDivideVariableByConstant(IVariable variable, IVariable constant, Domain domain)
        {
            var result = CreateAnonymous(domain);
            Model.AddConstr((variable.Var() ?? variable.ConstantValue.Value * One) <= (constant.ConstantValue.Value) * result.Var(), NewConstraintName());
            Model.AddConstr((variable.Var() ?? variable.ConstantValue.Value * One) >= (constant.ConstantValue.Value) * result.Var(), NewConstraintName());
            return result;
        }

        protected override object GetObjectsToSerialize()
        {
            return ConstraintIndex;
        }

        protected override void InternalDeserialize(object data)
        {
            ConstraintIndex = (int) data;
            foreach (var variable in Variables)
            {
                var typed = variable.Value as GurobiVariable;
                typed.GRBVar = Model.GetVarByName(typed.Name);
            }
        }

        protected override void InternalLoadModelFromFile(string modelPath)
        {
            Model.Dispose();
            Model = new GRBModel(Environment, modelPath);
            _hasGoal = true;
            AddOne();
        }

        protected override void InternalAddGoal(string name, IVariable operation)
        {
            Model.SetObjective(operation.Var() * One, GRB.MAXIMIZE);
        }

        protected virtual string NewConstraintName()
        {
            return $"_c_{ConstraintIndex++}";
        }

        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                Model.Dispose();
                Environment.Dispose();
            }
            
            _disposed = true;
        }
    }
}