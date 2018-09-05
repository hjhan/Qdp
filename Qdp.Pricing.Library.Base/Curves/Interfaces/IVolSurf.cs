using Qdp.Foundation.Implementations;

namespace Qdp.Pricing.Library.Base.Curves.Interfaces
{
	public interface IVolSurface : ICurve2D<Date, double>
	{
		IVolSurface BumpVolSurf(double dv);
		IVolSurface BumpMaturitySlice(int index, double dv);
		IVolSurface BumpMaturityStrikePoint(int indMaturity, int indStrike, double dv);
	}
}
