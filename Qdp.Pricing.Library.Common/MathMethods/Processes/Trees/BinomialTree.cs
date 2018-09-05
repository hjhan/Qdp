using System.Collections.Generic;

namespace Qdp.Pricing.Library.Common.MathMethods.Processes.Trees
{
	/// <summary>
	/// Simulate stock movement using Binomial Tree.
	/// </summary>
	public abstract class BinomialTree
	{
		public double X0 { get; private set; }
		public double DriftPerStep { get; private set; }
		public double Dt { get; private set; }
		public int NumberOfSteps { get; private set; }

		//public double Dx { get; private set; }
		//public double PUp { get; private set; }
		//public double PDown { get; private set; }
		public BlackScholesProcess Process { get; private set; }
		public List<BranchDirection> AllBranchDirections { get; private set; }

		protected BinomialTree()
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="BinomialTree"/> class.
		/// The underlying process shall be a Black-Schole process with constant r, sigma
		/// </summary>
		/// <param name="process">The process.</param>
		/// <param name="x0">The x0.</param>
		/// <param name="tend">The end time in years.</param>
		/// <param name="steps">The number of time steps.</param>
		//protected BinomialTree(BlackScholesProcess process, double x0, double tend, int steps = 200)
		protected BinomialTree(BlackScholesProcess process, double x0, double tend, int steps)
		{
			Process = process;
			X0 = x0;
			Dt = tend / steps;
			NumberOfSteps = steps;
			DriftPerStep = process.Drift(0, x0, Dt) * Dt;

			AllBranchDirections = new List<BranchDirection> { BranchDirection.Down, BranchDirection.Up };
		}

		/// <summary>
		/// Returns the descendant of node [i,j].
		/// The three descendants of one node in x[i,j] are: x[i+1, Descendant(i, j, BranchDirection.Up)], and x[i+1, Descendant(i, j, BranchDirection.Down)]
		/// </summary>
		/// <param name="i">The i^{th} time step, starts from 0.</param>
		/// <param name="j">The j^{th} state.</param>
		/// <param name="branch">The branching direction, <see cref="BranchDirection"/>. In binomial tree case, it takes value as either Up or Down.</param>
		/// <returns></returns>
		public int Descendant(int i, int j, BranchDirection branch)
		{
			return branch == BranchDirection.Down ? j : j + 1;
		}

		public abstract double StateValue(int i, int j);
		public abstract double Probability(int i, int j, BranchDirection branch);
	}
}
