using System;
using System.Collections.Generic;
using Qdp.Pricing.Library.Common.MathMethods.Utilities;

namespace Qdp.Pricing.Library.Common.MathMethods.FiniteDifference
{
	/// <summary>
	/// This class solves second order PDE equation for Black Schole option price using Crank Nicolson finite difference scheme
	/// The discretization on both Time and Space must be UNIFORM
	/// df/dt + alpha(t,x, dt)*df/dx + beta(t,x)*d^2{f}/dx^2 = r(t)*f
	/// This equation  
	/// </summary>
	public class SecondOrderPdeCrankNicolson
	{
		private readonly Func<double, double, double, double> _alpha;
		private readonly Func<double, double, double> _beta;
		private readonly Func<double, double> _r;
		private readonly bool _isTimeDependent;

		public SecondOrderPdeCrankNicolson(
			Func<double, double, double, double> alpha,
			Func<double, double, double> beta,
			Func<double, double> r,
			bool isTimeDependent = false)
		{
			_alpha = alpha;
			_beta = beta;
			_r = r;
			_isTimeDependent = isTimeDependent;
		}


		/// <summary>
		/// Solve the PDE backwards from the end.
		/// Each step t, solve Ax=b, where A is constructed from pu, pm, and pd, and b is calculated from t+dt
		/// </summary>
		public double[][] Solve(double[] t, double[] xGrid, double[] x, Func<double, double> payOff)
		{
			var value = new double[t.Length][];
			for (var i = 0; i < t.Length; ++i)
			{
				value[i] = new double[x.Length];
			}

			for (var j = 0; j < x.Length; ++j)
			{
				value[t.Length - 1][j] = payOff(x[j]);
			}

			var boundaryConditions = new NeumannBoundaryCondition[2];
			boundaryConditions[0] = new NeumannBoundaryCondition(Boundary.Upper,
				payOff(x[x.Length - 1]) - payOff(x[x.Length - 2]));
			boundaryConditions[1] = new NeumannBoundaryCondition(Boundary.Lower,
				payOff(x[1]) - payOff(x[0]));

			var dt = t[1] - t[0];
			var dx = xGrid[1] - xGrid[0];
			var nu = _alpha(0.0, 0.0, dt);
			var beta = _beta(0.0, 0.0);
			var rf = _r(0.0);
			var pu = -nu * dt / (4 * dx) - beta * dt / (2 * dx * dx);
			var pd = nu * dt / (4 * dx) - beta * dt / (2 * dx * dx);
			var pm = 1 + rf * dt / 2 + beta * dt / (dx * dx);

			var steps = t.Length;
			var gridPoints = xGrid.Length;

			var diagonal = new List<double>();
			var upperDiagonal = new List<double>();
			var lowerDiagonal = new List<double>();
			diagonal.Add(1);
			upperDiagonal.Add(-1);
			lowerDiagonal.Add(0);

			for (var i = 1; i < gridPoints - 1; ++i)
			{
				lowerDiagonal.Add(pd);
				diagonal.Add(pm);
				upperDiagonal.Add(pu);
			}

			diagonal.Add(-1);
			lowerDiagonal.Add(1);
			upperDiagonal.Add(0);

			var aTridiagonal = new TridiagonalMatrix(gridPoints, diagonal.ToArray(), upperDiagonal.ToArray(), lowerDiagonal.ToArray());
			var b = new double[gridPoints];

			for (var i = steps - 2; i >= 0; --i)
			{
				for (var j = 1; j < gridPoints - 1; ++j)
				{
					b[j] = -pu * value[i + 1][j + 1] - pd * value[i + 1][j - 1] - (pm - 2) * value[i + 1][j];
				}
				Array.ForEach(boundaryConditions, boundary =>
				{
					b = boundary.Apply(b);
				});

				var solution = aTridiagonal.SolveFor(b);
				for (var k = 0; k < solution.Length; ++k)
				{
					value[i][k] = Math.Max(solution[k], payOff(x[k]));
				}
			}

			return value;
		}
	}
}

