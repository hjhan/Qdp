using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Utilities
{
	public class Redemption : IRedemption
	{
        //public PriceQuoteType BondPriceQuoteType { get; private set; }
        //public double RedemptionRate { get; private set; }

        //public Redemption(double redemptionRate, PriceQuoteType PriceQuoteType)
        //{
        //	RedemptionRate = redemptionRate;
        //          BondPriceQuoteType = PriceQuoteType;
        //}

        //      public double GetRedemptionPayment(double coupon, double principal)
        //      {
        //          if (BondPriceQuoteType == PriceQuoteType.Clean)
        //          {
        //              return coupon + principal;
        //          }
        //          return RedemptionRate * principal;
        //      }

        public RedemptionType RedemptionType { get; private set; }
        public double RedemptionRate { get; private set; }

        public Redemption(double redemptionRate, RedemptionType redemptionType)
        {
            RedemptionRate = redemptionRate;
            RedemptionType = redemptionType;
        }

        public double GetRedemptionPayment(double coupon, double principal)
        {
            if (RedemptionType == RedemptionType.None)
            {
                return principal + coupon;
            }
            else if (RedemptionType == RedemptionType.SeparatePrincipal)
            {
                //return RedemptionRate * principal + coupon;

                //(jira: http://139.196.190.223:8888/browse/QDP-264) handle this convention: redemption with last coupon included
                //Wind China convention
                return RedemptionRate * (principal + coupon);
            }
            else //RedemptionType.SeparatePrincipalWithLastCoupon
            {
                return RedemptionRate * principal;
            }
        }
    }
}
