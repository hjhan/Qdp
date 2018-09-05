using System;
using System.Collections.Generic;

namespace Qdp.Pricing.Library.Common.MathMethods.Interfaces
{
	public interface IStochasticProcessNd
	{
		Boolean UseSameRandomSource { get; }
		int Size { get;  }
		List<double> Evolve(double t0, List<double> x0, double dt, List<double> dw);
	}
}
