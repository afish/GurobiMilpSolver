using System;
using System.Collections.Generic;
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
	    public ICollection<string> Constraints { get; } = new List<string>();

        public GRBVar GRBVar
		{
			get => _grbVar;
            set => _grbVar = value;
        }
	}

	public static class GurobiVariableExtensions
	{
		public static IGurobiVariable Typed(this IVariable variable)
		{
			return variable as IGurobiVariable;
		}

		public static GRBVar Var(this IVariable variable)
		{
			return variable.Typed().GRBVar;
		}
	}
}