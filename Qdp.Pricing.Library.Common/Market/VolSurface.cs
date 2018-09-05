using System;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Base.Curves;
using Qdp.Pricing.Library.Base.Curves.Interfaces;

namespace Qdp.Pricing.Library.Common.Market
{
	public class VolSurface : Curve2D<Date, double>, IVolSurface
	{
		public VolSurface(Date[] x, double[] y, double[,] valueOnGrids, Func<Date, double> x2DoubleFunc, Func<double, double> y2DoubleFunc, Interpolation2D interpolation2D) 
			: base(x, y, valueOnGrids, x2DoubleFunc, y2DoubleFunc, interpolation2D)
		{

		}

		public IVolSurface BumpVolSurf(double dv)
		{
			throw new NotImplementedException();
		}

		public IVolSurface BumpMaturitySlice(int index, double dv)
		{
			throw new NotImplementedException();
		}

		public IVolSurface BumpMaturityStrikePoint(int indMaturity, int indStrike, double dv)
		{
			throw new NotImplementedException();
		}
	}
}
