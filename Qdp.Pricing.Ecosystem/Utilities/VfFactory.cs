using Qdp.ComputeService.Data.CommonModels.TradeInfos;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.Equity;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.Pricing.Ecosystem.Trade;
using Qdp.Pricing.Ecosystem.Trade.Equity;
using Qdp.Pricing.Ecosystem.Trade.Exotic;
using Qdp.Pricing.Ecosystem.Trade.FixedIncome;
using Qdp.Pricing.Library.Base.Utilities;

namespace Qdp.Pricing.Ecosystem.Utilities
{
	public static class VfFactory
	{
		public static IValuationFunction ToVf(TradeInfoBase tradeInfo)
		{
			IValuationFunction vf;
            // sean todo // tradeInfo.GetValuationFunction
			if (tradeInfo is BondInfoBase)
			{
				vf = new BondVf((BondInfoBase) tradeInfo);
			}
			else if (tradeInfo is VanillaOptionInfo)
			{
				vf = new VanillaOptionVf((VanillaOptionInfo) tradeInfo);
			}
            else if (tradeInfo is BarrierOptionInfo)
            {
                vf = new BarrierOptionVf((BarrierOptionInfo)tradeInfo);
            }
            else if (tradeInfo is BinaryOptionInfo)
            {
                vf = new BinaryOptionVf((BinaryOptionInfo)tradeInfo);
            }
            else if (tradeInfo is AsianOptionInfo)
            {
                vf = new AsianOptionVf((AsianOptionInfo)tradeInfo);
            }
            else if (tradeInfo is RainbowOptionInfo)
            {
                vf = new RainbowOptionVf((RainbowOptionInfo)tradeInfo);
            }
            else if (tradeInfo is SpreadOptionInfo)
            {
                vf = new SpreadOptionVf((SpreadOptionInfo)tradeInfo);
            }
            else if (tradeInfo is InterestRateSwapInfo)
			{
				vf = new InterestRateSwapVf((InterestRateSwapInfo) tradeInfo);
			}
			else if (tradeInfo is FixedLegInfo)
			{
				vf = new FixedLegVf((FixedLegInfo) tradeInfo);
			}
			else if (tradeInfo is FloatingLegInfo)
			{
				vf = new FloatingLegVf((FloatingLegInfo) tradeInfo);
			}
			else if (tradeInfo is BondFuturesInfo)
			{
				vf = new BondFuturesVf((BondFuturesInfo)tradeInfo);
			}
			else if (tradeInfo is LoanInfo)
			{
				vf = new LoanVf((LoanInfo)tradeInfo);
			}
			else if (tradeInfo is HoldingPeriodInfo)
			{
				vf = new HoldingPeriodVf((HoldingPeriodInfo)tradeInfo);
			}
			else if (tradeInfo is AbsWithRepurchaseInfo)
			{
				vf = new AbsWithRepurchaseVf((AbsWithRepurchaseInfo)tradeInfo);
			}
            else if (tradeInfo is ConvertibleBondInfo)
			{
			    vf = new ConvertibleBondVf((ConvertibleBondInfo)tradeInfo);
			}
			else
			{
				throw new PricingLibraryException("Unknowy trade info type");
			}

			return vf;
		}
	}
}
