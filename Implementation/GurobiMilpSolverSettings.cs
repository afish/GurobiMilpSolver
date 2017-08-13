using Gurobi;
using MilpManager.Abstraction;

namespace GurobiMilpManager.Implementation
{
	public class GurobiMilpSolverSettings : MilpManagerSettings
	{
		public GurobiMilpSolverSettings()
		{
			Environment = new GRBEnv();
			Model = new GRBModel(Environment);
		}

		public GRBEnv Environment { get; }
		public GRBModel Model { get; set; }
	}
}