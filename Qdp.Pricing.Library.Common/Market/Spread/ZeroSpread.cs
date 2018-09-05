using Qdp.Foundation.Implementations;
using Qdp.Pricing.Library.Base.Curves.Interfaces;

namespace Qdp.Pricing.Library.Common.Market.Spread
{
	public class ZeroSpread : ISpread
	{
		public double Spread { get; private set; }

		public ZeroSpread(double spread)
		{
			Spread = spread;
		}

		public ISpread BumpSpread(double ds)
		{
			return new ZeroSpread(Spread+ds);
		}

		public ISpread BumpSpread(int bp)
		{
			return new ZeroSpread(Spread + bp*0.0001);
		}

		public double GetValue(double t)
		{
			return Spread;
		}

		public double GetValue(Date date)
		{
			return Spread;
		}
	}
}
