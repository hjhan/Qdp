using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Base.Implementations;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.Utilities;

namespace Qdp.Pricing.Library.Common.Engines
{
	public class BondEngineCn : CashflowProductEngine<Bond>
	{
		private readonly IBondYieldPricer _bondYieldPricer;
		private readonly BondEngine _bondEngine;

		public BondEngineCn(IBondYieldPricer bondYieldPricer = null)
		{
			_bondYieldPricer = bondYieldPricer ?? new BondYieldPricer();
			_bondEngine = new BondEngine(_bondYieldPricer);
		}

		public override IPricingResult Calculate(Bond bond, IMarketCondition market, PricingRequest request)
		{
			var beginValuation = DateTime.Now;
			var result = new PricingResult(market.ValuationDate, request);
			var isCleanPriceRound = bond.RoundCleanPrice;

			var bondQuote = market.MktQuote.Value.ContainsKey(bond.Id) ? market.MktQuote.Value[bond.Id] : null;
			var bMktQuote = new Dictionary<string, Tuple<PriceQuoteType, double>>();
			IPricingResult resultOptionBond = new PricingResult(market.ValuationDate, request);
			IPricingResult resultSimpleBond;
			if (bondQuote != null && (bondQuote.Item1 == PriceQuoteType.YtmExecution || bondQuote.Item1 == PriceQuoteType.YtmCallExecution || bondQuote.Item1 == PriceQuoteType.YtmPutExecution))
			{
				bMktQuote[bond.Id] = Tuple.Create(PriceQuoteType.Ytm, bondQuote.Item2);
				var ytmMarket = market.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, Tuple<PriceQuoteType, double>>>(x => x.MktQuote, bMktQuote));
				// Get Call or Put cleanprice
				var pricingRequest = PricingRequest.CleanPrice;
				if (result.IsRequested(PricingRequest.AiEod))
				{
					pricingRequest = pricingRequest | PricingRequest.AiEod;
				}
				resultOptionBond = CalcOptionBond(bond, ytmMarket, pricingRequest, bondQuote.Item1);
				// Parse market
				var cleanMarket = UpdateCleanPriceMarket(bond.Id, resultOptionBond.CleanPrice, isCleanPriceRound, market);
				resultSimpleBond = _bondEngine.Calculate(bond, cleanMarket, request);
			}
			else
			{
				if (isCleanPriceRound && bondQuote !=null)
				{
					if (bondQuote.Item1 == PriceQuoteType.Clean)
					{
						var cleanPriceMarket = UpdateCleanPriceMarket(bond.Id, bondQuote.Item2, isCleanPriceRound,
							market);
						resultSimpleBond = _bondEngine.Calculate(bond, cleanPriceMarket, request);
					}
					else
					{
						resultSimpleBond = _bondEngine.Calculate(bond, market, request);
						var cleanPriceMarket = UpdateCleanPriceMarket(bond.Id, resultSimpleBond.CleanPrice, isCleanPriceRound, market);
						resultSimpleBond = _bondEngine.Calculate(bond, cleanPriceMarket, request);
					}
				}
				else
				{
					resultSimpleBond = _bondEngine.Calculate(bond, market, request);
				}
				// Parse market
				bMktQuote[bond.Id] = Tuple.Create(PriceQuoteType.Clean, double.IsNaN(resultSimpleBond.CleanPrice) ? 0.0 : resultSimpleBond.CleanPrice);
				var executionYieldPricingRequest = PricingRequest.Ytm;
				if (result.IsRequested(PricingRequest.AiEod))
				{
					executionYieldPricingRequest = PricingRequest.Ytm | PricingRequest.AiEod;
				}
				var newMarket = market.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, Tuple<PriceQuoteType, double>>>(x => x.MktQuote, bMktQuote));
				if (result.IsRequested(PricingRequest.YtmExecution))
				{
					resultOptionBond = CalcOptionBond(bond, newMarket, executionYieldPricingRequest, PriceQuoteType.YtmExecution);
				}
			}
			result = (PricingResult)resultSimpleBond;
			result.YieldToCall = resultOptionBond.YieldToCall;
			result.YieldToPut = resultOptionBond.YieldToPut;
			result.CallDate = resultOptionBond.CallDate;
			result.PutDate = resultOptionBond.PutDate;

			var endValuation = DateTime.Now;
			result.CalcTimeInMilliSecond = (endValuation - beginValuation).TotalMilliseconds;
			return result;
		}

		private IMarketCondition UpdateCleanPriceMarket(string bondId, double cleanPrice, bool isCleanPriceRound, IMarketCondition oldMarket)
		{
			var bMktQuote = new Dictionary<string, Tuple<PriceQuoteType, double>>();

			var cleanPriceRound = double.IsNaN(cleanPrice) ? 0.0 : (isCleanPriceRound ? cleanPrice.Round(4) : cleanPrice);
			bMktQuote[bondId] = Tuple.Create(PriceQuoteType.Clean, cleanPriceRound);
			var cleanMarket = oldMarket.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, Tuple<PriceQuoteType, double>>>(x => x.MktQuote, bMktQuote));
			return cleanMarket;
		}

		private IPricingResult CalcOptionBond(Bond bond, IMarketCondition market, PricingRequest request, PriceQuoteType? optionRequestType)
		{
			var result = new PricingResult(market.ValuationDate, request);
			var optionFlg = optionRequestType == PriceQuoteType.YtmExecution
				|| optionRequestType == PriceQuoteType.YtmCallExecution
				|| optionRequestType == PriceQuoteType.YtmPutExecution;
			// Get Call Execution
			if (optionFlg)
			{
				if (bond.OptionToCall != null)
				{
					var optionCallResult = CalcOption(bond.OptionToCall, bond, market, request, bond.PaymentFreq);
					result.CallDate = double.IsNaN(optionCallResult.Item2) ? null : optionCallResult.Item1;
					result.YieldToCall = optionCallResult.Item2;
					result.CleanPrice = optionCallResult.Item3;
				}
				if (bond.OptionToPut != null)
				{
					var optionPutResult = CalcOption(bond.OptionToPut, bond, market, request, bond.PaymentFreq);
					result.PutDate = double.IsNaN(optionPutResult.Item2) ? null : optionPutResult.Item1;
					result.YieldToPut = optionPutResult.Item2;
					result.CleanPrice = double.IsNaN(result.CleanPrice) ? optionPutResult.Item3 : result.CleanPrice;
				}
				else if (bond.OptionToAssPut != null)
				{
					// Get Put Execution for ass type
					Frequency frequency;
					var optionPut = ParseAssPut(bond, out frequency);
					var optionAssPutResult = CalcOption(optionPut, bond, market, request, frequency);
					result.PutDate = double.IsNaN(optionAssPutResult.Item2) ? null : optionAssPutResult.Item1;
					result.YieldToPut = optionAssPutResult.Item2;
					result.CleanPrice = double.IsNaN(result.CleanPrice) ? optionAssPutResult.Item3 : result.CleanPrice;
				}
			}

			return result;
		}

		private Dictionary<string, double> ParseAssPut(Bond bond, out Frequency freq)
		{
			freq = bond.PaymentFreq;
			if (bond.OptionToAssPut != null)
			{
				var isHalf =
					bond.OptionToAssPut.Keys.Any(
						termKey => (Convert.ToDouble(termKey) - (int) Convert.ToDouble(termKey)).CompareTo(0.5) == 0);
				var termNew = bond.OptionToAssPut.Select(x => Convert.ToDouble(x.Key)).ToArray();
				if (isHalf)
				{
					termNew = termNew.Select(x => x*2).ToArray();
					switch (freq)
					{
						case Frequency.Annual:
							freq = Frequency.SemiAnnual;
							break;
						case Frequency.SemiAnnual:
							freq = Frequency.Quarterly;
							break;
					}
				}

				var tmpDate = new Schedule(bond.StartDate, bond.UnderlyingMaturityDate, freq.GetTerm(), "ShortStart".ToStub()).ToList();
				return termNew.Select(x => tmpDate[Convert.ToInt16(x)])
					.ToDictionary(x => x.ToString(), y => bond.OptionToAssPut.Values.FirstOrDefault());
			}
			return null;
		}

		private Tuple<Date, double, double> CalcOption(Dictionary<string, double> optionInfo, Bond bond, IMarketCondition market, PricingRequest request, Frequency frequency = Frequency.None)
		{
			var paymentDaycount = bond.BondTradeingMarket == TradingMarket.ChinaInterBank
				? (IDayCount)new ActActIsma()
				: new ModifiedAfb();
			if (optionInfo != null && optionInfo.Count > 0)
			{
				var execution = optionInfo.Where(x => x.Key.ToDate() > market.ValuationDate).OrderBy(x => x.Key.ToDate()).FirstOrDefault();
				var maturityDate = execution.Key.ToDate();
				if (maturityDate != null)
				{
					var executionBond = new Bond(bond.Id,
						bond.StartDate,
						maturityDate,
						bond.Notional,
						bond.Currency,
						bond.Coupon,
						bond.Calendar,
						frequency,
						bond.Stub,
						bond.AccrualDayCount,
						paymentDaycount,
						bond.AccrualBizDayRule,
						bond.PaymentBizDayRule,
						bond.SettlmentGap,
						bond.BondTradeingMarket,
						bond.StickToEom,
						bond.Redemption,
						bond.FirstPaymentDate,
						bond.IsZeroCouponBond,
						bond.IssuePrice,
						bond.IssueRate,
						AmortizationType.None,
						bond.AmortizationInDates,
						bond.AmortizationInIndex,
						bond.RenormalizeAfterAmoritzation,
						bond.StepWiseCompensationRate);
					var executionResult = _bondEngine.Calculate(executionBond, market, request);

					return Tuple.Create(maturityDate, executionResult.Ytm, executionResult.CleanPrice);
				}
			}
			return Tuple.Create(Date.MaxValue, double.NaN, double.NaN);
		}
	}
}
