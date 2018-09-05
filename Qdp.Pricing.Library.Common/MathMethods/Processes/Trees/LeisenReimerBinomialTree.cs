using System;

namespace Qdp.Pricing.Library.Common.MathMethods.Processes.Trees
{
	public class LeisenReimerBinomialTree : BinomialTree
	{
		private readonly int _steps;
		private readonly double _strike;
		private readonly double[][] _stateValues;

		public double Dx { get; private set; }
		public double PUp { get; private set; }
		public double PDown { get; private set; }

		public LeisenReimerBinomialTree()
		{ }

		public LeisenReimerBinomialTree(BlackScholesProcess process, double x0, double strike, double tend, int steps = 200, int peizerPrattMethod=1)
			: base(process, x0, tend, (steps%2==0 ? steps+1 : steps))
		{ 
			_strike = strike;
			Dx = process.StdDeviation(0, X0, Dt); // sigma * sqrt(dt)
			var drift = process.Drift(0, X0, Dt);  // r - q - 0.5 * sigma * sigma
			var sigma = process.Diffusion(0, X0);  // sigma
			var d1 = ( Math.Log(X0 / _strike) + (drift + sigma * sigma) * tend ) / (sigma * Math.Sqrt(tend));  // d+
			var d2 = ( Math.Log(X0 / _strike) + drift * tend ) / (sigma * Math.Sqrt(tend));   // d-
			var p1 = ProbabilityHelper(d1, NumberOfSteps, peizerPrattMethod);
			var p2 = ProbabilityHelper(d2, NumberOfSteps, peizerPrattMethod);
			var tmp = Math.Exp((drift + 0.5 * sigma * sigma) * Dt);
			var u = tmp * p1 / p2;
			var d = (tmp - p2 * u) / (1 - p2);
			PUp = p2;
			PDown = 1 - PUp;

			_steps = steps % 2 == 0 ? steps + 1 : steps; // only use odd number
			_stateValues = new double[_steps + 1][];
			for (int i = 0; i < _steps + 1; ++i)
			{
				_stateValues[i] = new double[i + 1];
				for (int j = 0; j < i + 1; ++j)
				{
					_stateValues[i][j] = X0 * Math.Pow(u, j) * Math.Pow(d, i - j);
				}
			}
		}

		private static double ProbabilityHelper(double z, int n, int peizerPrattMethod=1)
		{
			var z1 = n + 1.0 / 3 + (peizerPrattMethod-1) / (10.0 * (n+1));
			var p = 0.5 + 0.5 * Math.Sign(z) * Math.Sqrt(1 - Math.Exp(-(n + 1 / 6) * z * z / (z1 * z1)));
			return p;
		}

		public override double StateValue(int i, int j)
		{
			return _stateValues[i][j];
		}

		public override double Probability(int i, int j, BranchDirection branch)
		{
			return branch == BranchDirection.Up ? PUp : PDown;
		}

	}

}