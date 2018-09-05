using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra.Double;
using Qdp.Pricing.Library.Common.MathMethods.Interfaces;

namespace Qdp.Pricing.Library.Common.MathMethods.Processes
{
	public class StochasticProcessNd : IStochasticProcessNd
	{
		public Boolean UseSameRandomSource { get; private set; }

		public int Size { get { return Processes.Count; } }

		public List<IStochasticProcess1D> Processes { get; private set; }
		public double[,] CorrelationMatrix { get; private set; }
		private readonly double[,] _sqrtCorrelationMatrix;

		public StochasticProcessNd(IEnumerable<IStochasticProcess1D> processes, double[,] correlationMatrix, bool useSameRandomSource = true)
		{
			Processes = processes.ToList();
			CorrelationMatrix = correlationMatrix;
			UseSameRandomSource = useSameRandomSource;
			_sqrtCorrelationMatrix = useSameRandomSource ? null : (DenseMatrix.OfArray(correlationMatrix).Cholesky().Factor).ToArray();
		}

		public List<double> Evolve(double t0, List<double> x0, double dt, List<double> dw)
		{
			List<double> results;
			if (UseSameRandomSource)
			{
				results = Processes.Select((t, i) => t.Evolve(t0, x0[i], dt, dw[0])).ToList();
			}
			else
			{
				var dz = (DenseMatrix.OfArray(_sqrtCorrelationMatrix) * DenseVector.OfArray(dw.ToArray())).ToArray();
				results = Processes.Select((t, i) => t.Evolve(t0, x0[i], dt, dz[i])).ToList();
			}
			return results;
		}
	}
}
