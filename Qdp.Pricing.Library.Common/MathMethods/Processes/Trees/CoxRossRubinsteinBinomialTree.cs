using System;

namespace Qdp.Pricing.Library.Common.MathMethods.Processes.Trees
{
	public class CoxRossRubinsteinBinomialTree : BinomialTree
	{
		private readonly double[][] _stateValues;

		public double Dx { get; private set; }
		public double PUp { get; private set; }
		public double PDown { get; private set; }

		public CoxRossRubinsteinBinomialTree()
		{ }

		public CoxRossRubinsteinBinomialTree(BlackScholesProcess process, double x0, double tend, int steps = 200)
			: base(process, x0, tend, steps)
		{
			Dx = process.StdDeviation(0, X0, Dt); // sigma * sqrt(dt)
			// (exp(r*dt) - exp(-sigma*sqrt(dt)) / (exp(sigma*sqrt(dt)) - exp(-sigma*sqrt(dt)))
			PUp = 0.5 + 0.5 * DriftPerStep / Dx; //(Math.Exp((Process.GetDiscountRate(0.0) - Process.GetDividendRate(0.0)) * Dt) - Math.Exp(-Dx)) / (Math.Exp(Dx) - Math.Exp(-Dx));
			PDown = 1 - PUp;

			_stateValues = new double[steps + 1][];
			for (int i = 0; i < steps + 1; ++i)
			{
				_stateValues[i] = new double[i + 1];
				for (int j = 0; j < i + 1; ++j)
				{
					_stateValues[i][j] = X0 * Math.Exp((2 * j - i) * Dx);
				}
			}
		}

		/// <summary>
		/// Returns the state value, i.e. stock price, at node [i,j].
		/// </summary>
		/// <param name="i">The i^{th} time step, starts from 0.</param>
		/// <param name="j">The j^{th} state, 0 is the bottom node.</param>
		/// <returns></returns>
		public override double StateValue(int i, int j)
		{
			//return X0 * Math.Exp((2*j - i) * Dx);
			return _stateValues[i][j];
		}

		/// <summary>
		/// Probabilities of braching up/down from node [i,j].
		/// </summary>
		/// <param name="i">The i^{th} time step, starts from 0.</param>
		/// <param name="j">The j^{th} state.</param>
		/// <param name="branch">The branching direction, <see cref="BranchDirection"/>. In binomial tree case, it takes value as either Up or Down.</param>
		/// <returns></returns>
		public override double Probability(int i, int j, BranchDirection branch)
		{
			return branch == BranchDirection.Up ? PUp : PDown;
		}
	}

}
