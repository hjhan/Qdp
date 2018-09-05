using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Library.Common;
using Qdp.Pricing.Library.Common.Engines;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.Utilities.Mortgage;

namespace Qdp.Pricing.Ecosystem.Trade.FixedIncome
{
	public class LoanVf : ValuationFunction<LoanInfo, Loan>
	{
		public LoanVf(LoanInfo tradeInfo)
			: base(tradeInfo)
		{
		}

		public override Loan GenerateInstrument()
		{
			var numPaymentsPerYear = (int)TradeInfo.Frequency.ToFrequency().CountPerYear();
			var ppm = TradeInfo.AbsPrepaymentModel.ToEnumType<AbsPrepaymentModel>();
			var dm = TradeInfo.AbsDefaultModel.ToEnumType<AbsDefaultModel>();

			IMortgagePrepayment prepaymentModel;
			if (ppm == AbsPrepaymentModel.Psa)
			{
				prepaymentModel = new Psa(TradeInfo.PsaMultiplier, 0.06, 30, numPaymentsPerYear);
			}
			else if (ppm == AbsPrepaymentModel.Cpr)
			{
				prepaymentModel = new Cpr(TradeInfo.AnnualCprRate, numPaymentsPerYear);
			}
			else
			{
				prepaymentModel = new Psa();
			}

			IMortgageDefault defaultModel;
			if (dm == AbsDefaultModel.Sda)
			{
				defaultModel = new Sda(TradeInfo.SdaMultiplier, TradeInfo.RecoveryRate, numPaymentsPerYear);
			}
			else if (dm == AbsDefaultModel.Cdr)
			{
				defaultModel = new Cdr(TradeInfo.AnnualCdrRate, numPaymentsPerYear, TradeInfo.RecoveryRate);
			}
			else
			{
				defaultModel = new Sda();
			}

			var mortgageCalcMethod = TradeInfo.MortgageCalcMethod.ToEnumType<MortgageCalcMethod>();

			return new Loan(TradeInfo.StartDate.ToDate(),
					TradeInfo.MaturityDate.ToDate(),
					TradeInfo.FirstPaymentDate.ToDate(),
					TradeInfo.Notional,
					TradeInfo.NumOfPayment,
					TradeInfo.DayCount.ToDayCountImpl(),
					TradeInfo.Frequency.ToFrequency(),
					TradeInfo.Coupon,
					TradeInfo.ResetDate.ToDate(),
					TradeInfo.IsFloatingRate,
					string.IsNullOrEmpty(TradeInfo.IndexType) ? IndexType.None : TradeInfo.IndexType.ToIndexType(),
					TradeInfo.FloatingRateMultiplier,
					TradeInfo.Amortization.ToAmortizationType(),
					TradeInfo.Currency.ToCurrencyCode(),
					mortgageCalcMethod == MortgageCalcMethod.Simple ? new SimpleMortgageCalculator(prepaymentModel, defaultModel) : (IMortgageCalculator)new MortgageCalculator(prepaymentModel, defaultModel),
					TradeInfo.TaxRate
					);
		}

		public override IEngine<Loan> GenerateEngine()
		{
			return new LoanEngine();
		}

		public override IMarketCondition GenerateMarketCondition(QdpMarket market)
		{
			return new MarketCondition(
					x => x.ValuationDate.Value = market.ReferenceDate,
					x => x.HistoricalIndexRates.Value = market.HistoricalIndexRates
			);
		}
	}
}
