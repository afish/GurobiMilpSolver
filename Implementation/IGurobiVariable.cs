using Gurobi;
using MilpManager.Abstraction;

namespace GurobiMilpManager.Implementation
{
	public interface IGurobiVariable : IVariable
	{
		GRBVar GRBVar { get; set; }
	}
}