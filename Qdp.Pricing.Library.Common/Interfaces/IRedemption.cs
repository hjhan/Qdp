using Qdp.Pricing.Base.Enums;

namespace Qdp.Pricing.Library.Common.Interfaces
{
	public interface IRedemption
	{
        //PriceQuoteType BondPriceQuoteType { get; }
		RedemptionType RedemptionType { get; }
		double RedemptionRate { get; }
		double GetRedemptionPayment(double coupon, double principal);
	}
}
