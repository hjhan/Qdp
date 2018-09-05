using Qdp.Foundation.Implementations;

namespace Qdp.Pricing.Library.Base.Curves.Interfaces
{

	public interface ISpread
	{
		double Spread { get; }
		ISpread BumpSpread(double ds);
		ISpread BumpSpread(int bp);
		double GetValue(double t);
		double GetValue(Date date);
	}
}
