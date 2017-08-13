using System;
using Gurobi;
using MilpManager.Abstraction;

namespace GurobiMilpManager.Implementation
{
	[Serializable]
	public class GurobiVariable : IGurobiVariable
	{
		[NonSerialized]
		private GRBVar _grbVar;
		[NonSerialized]
		private IMilpManager _milpManager;

		public IMilpManager MilpManager
		{
			get { return _milpManager; }
			set { _milpManager = value; }
		}

		public Domain Domain { get; set; }
		public string Name { get; set; }
		public double? ConstantValue { get; set; }
		public string Expression { get; set; }

		public GRBVar GRBVar
		{
			get { return _grbVar; }
			set { _grbVar = value; }
		}
	}

	public static class GurobiVariableExtensions
	{
		public static GurobiVariable Typed(this IVariable variable)
		{
			return variable as GurobiVariable;
		}

		public static GRBVar Var(this IVariable variable)
		{
			return variable.Typed().GRBVar;
		}
	}
}