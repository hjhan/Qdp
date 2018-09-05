using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.VolTermStructure;

namespace Qdp.Pricing.Library.Common.MathMethods.Interfaces
{
	public interface ILocalVolSurface : IVolSurface
	{
		InterpolatedImpliedVolSurface ImpliedVol { get; }
	}
}
