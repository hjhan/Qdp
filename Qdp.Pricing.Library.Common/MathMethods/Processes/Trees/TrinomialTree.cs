using System;
using System.Collections.Generic;

namespace Qdp.Pricing.Library.Common.MathMethods.Processes.Trees
{
	/// <summary>
	/// Generates a Trinomial tree for interest rate process.
	/// </summary>
	public class TrinomialTree
	{
		public List<Branching> Branchings { get; private set; }
		public double X0 { get; private set; }
		public double[] Dx { get; private set; }
		public double[] Times { get; private set; }
		public bool IsPositive { get; private set; }
		public int Size { get; private set; }
		public List<BranchDirection> AllBranchDirections { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TrinomialTree"/> class.
		/// The process is defined by dX = -aXdt + \sigma*dw, Stage 1 Hull White tree construction follows Damino and Fabio edition 2, Page 78.
		/// </summary>
		/// <param name="process">The stochastic process which is used to calculate drift and variance in each time step.</param>
		/// <param name="time">The time grid in unit of years.</param>
		/// <param name="x0">The starting value of stochastic variable.</param>
		/// <param name="isPositive">Bool indicator where rate on tree required to be positive or not.</param>
		public TrinomialTree(StochasticProcess1D process, double[] time, double x0, bool isPositive = false)
		{
			X0 = x0;
			Times = time;
			IsPositive = isPositive;
			Size = Times.Length;
			AllBranchDirections = new List<BranchDirection> { BranchDirection.Down, BranchDirection.Up, BranchDirection.Middle };
			Dx = new Double[Size];
			Branchings = new List<Branching>();
			int jMin = 0;
			int jMax = 0;
			Dx[0] = 0.0;

			var sqrt3 = Math.Sqrt(3.0);
			for (var i = 0; i < Times.Length - 1; ++i)
			{
				var t = Times[i];
				var dt = Times[i + 1] - Times[i];
				var variance = process.Variance(t, 0.0, dt);
				var stdev = Math.Sqrt(variance);
				Dx[i + 1] = sqrt3 * stdev; // step size of next step

				var branching = new Branching();
				for (var j = jMin; j <= jMax; j++)
				{
					var x = X0 + j * Dx[i];
					var m = process.Expectation(t, x, dt);
					var k = (int)Math.Round((m - X0) / Dx[i + 1]);

					if (isPositive)
					{
						while (X0 + (k - 1) * Dx[i + 1] <= 0)
						{
							k++;
						}
					}

					var eta = m - (X0 + k * Dx[i + 1]);

					var pUp = 1.0 / 6.0 + eta * eta / (6 * variance) + eta / (2.0 * sqrt3 * stdev); // pu
					var pMiddle = (2.0 - eta * eta / variance) / 3.0; //pm
					var pDown = 1.0 / 6.0 + eta * eta / (6 * variance) - eta / (2.0 * sqrt3 * stdev); //pd
					branching.Add(k, pUp, pMiddle, pDown);
				}

				Branchings.Add(branching);
				jMin = branching.JMin;
				jMax = branching.JMax;
			}
		}

		public double Dt(int i)
		{
			return Times[i + 1] - Times[i];
		}
		/// <summary>
		/// Return the number of nodes at i^{th} point on time grid, starting from 0.
		/// </summary>
		/// <param name="i">The i.</param>
		/// <returns></returns>
		public int NumberOfNodes(int i)
		{
			if (i == Size - 1)
				return Branchings[i - 1].JMax - Branchings[i - 1].JMin + 1;

			return Branchings[i].Size;
		}

		/// <summary>
		/// Return the state value r_{i,j}
		/// </summary>
		/// <param name="i">The i^{th} point on time grid.</param>
		/// <param name="j">The j^{th} node at i^{th} time, starting from the bottom as 0.</param>
		/// <returns></returns>
		public double StateValue(int i, int j)
		{
			if (i == 0)
			{
				return X0;
			}
			else
			{
				return X0 + (Branchings[i - 1].JMin + j) * Dx[i];
			}
		}

		/// <summary>
		/// The branching probability from node (i, j).
		/// </summary>
		/// <param name="i">The i^{th} point on time grid.</param>
		/// <param name="j">The j^{th} node at i^{th} time, starting from the bottom as 0.</param>
		/// <param name="bDirection">The b direction.</param>
		/// <returns></returns>
		public double Probability(int i, int j, BranchDirection bDirection)
		{
			return Branchings[i].Probability[Convert.ToInt32(bDirection)][j];
		}

		/// <summary>
		/// The index of a descendant of node (i,j).
		/// The three descendants of one node in x[i,j] are: x[i, Descendant(i+1, j, BranchDirection.Up)], x[i, Descendant(i+1, j, BranchDirection.Middle)] and x[i+1, Descendant(i, j, BranchDirection.Down)]
		/// </summary>
		/// <param name="i">The i^{th} point on time grid.</param>
		/// <param name="j">The j^{th} node at i^{th} time, starting from the bottom as 0.</param>
		/// <param name="branchDirection">The b direction.</param>
		/// <returns></returns>
		public int Descendant(int i, int j, BranchDirection branchDirection)
		{
			var branching = Branchings[i];
			switch (branchDirection)
			{
				case BranchDirection.Down:
					return branching.K[j] - branching.JMin - 1;
				case BranchDirection.Up:
					return branching.K[j] - branching.JMin + 1;
				case BranchDirection.Middle:
					return branching.K[j] - branching.JMin;
				default:
					throw new ArgumentOutOfRangeException("branchDirection");
			}
		}
	}
}
