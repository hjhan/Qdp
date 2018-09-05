using System.Linq;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Library.Common;
using Qdp.Pricing.Library.Common.Engines;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;

namespace Qdp.Pricing.Ecosystem.Trade.FixedIncome
{
	public class AbsWithRepurchaseVf : ValuationFunction<AbsWithRepurchaseInfo, AbsWithRepurchase>
	{
		public AbsWithRepurchaseVf(AbsWithRepurchaseInfo tradeInfo)
			: base(tradeInfo)
		{
		}

		public override AbsWithRepurchase GenerateInstrument()
		{
			var loan = new LoanVf(TradeInfo.LoanInfo).GenerateInstrument();
			var bonds = TradeInfo.Tranches.Select(x => new BondVf(x).GenerateInstrument()).ToArray();
			return new AbsWithRepurchase(loan, TradeInfo.RepurchaseRatio, bonds);
		}

		public override IEngine<AbsWithRepurchase> GenerateEngine()
		{
			return new AbsWithRepurchaseEngine();
		}

		public override IMarketCondition GenerateMarketCondition(QdpMarket market)
		{
			var discountCurve = TradeInfo.Tranches.Select(x => x.ValuationParamters.DiscountCurveName).First();
			var fixingCurve = TradeInfo.Tranches.Select(x => x.ValuationParamters.FixingCurveName).First();
			return new MarketCondition(
					x => x.ValuationDate.Value = market.ReferenceDate,
					x => x.HistoricalIndexRates.Value = market.HistoricalIndexRates,
					x => x.DiscountCurve.Value = null,
					x => x.FixingCurve.Value = null,
					x => x.RiskfreeCurve.Value = null
			);
		}
	}
}
