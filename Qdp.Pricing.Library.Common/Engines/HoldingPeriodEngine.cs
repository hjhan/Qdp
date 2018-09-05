using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.Utilities;

namespace Qdp.Pricing.Library.Common.Engines
{
	public class HoldingPeriodEngine : Engine<HoldingPeriod>
	{
		private readonly BondEngineCn _bondEngine;
		private bool _roundCleanPrice;

		private readonly List<string> _negativeList = new List<string>{"startFrontCommission"
			,"startBackCommission"
			,"endFrontCommission"
			,"endBackCommission"
			,"frontCommissionPreTax"
			,"backCommissionPreTax"
			,"frontCommissionAT"
			,"backCommissionAT"
			,"businessTax"
			,"interestTax"
			,"totalTax"
			,"holdingCost"};

		public HoldingPeriodEngine()
		{
			_bondEngine = new BondEngineCn(new BondYieldPricerCn());
			_roundCleanPrice = true;

		}

		public override IPricingResult Calculate(HoldingPeriod holdingPeriod, IMarketCondition market, PricingRequest request)
		{
			var beginValuation = DateTime.Now;
			var result = new PricingResult(market.ValuationDate, request);
			var psDict = new Dictionary<string, Dictionary<string, RateRecord>>();

			var bondId = holdingPeriod.Id;
			var startDate = holdingPeriod.StartDate;
			var endDate = holdingPeriod.UnderlyingMaturityDate;
			var bond = holdingPeriod.UnderlyingBond;
			_roundCleanPrice = holdingPeriod.UnderlyingBond.RoundCleanPrice;

			var startBondId = bondId + "_Start";
			var endBondId = bondId + "_End";

			// Create start and end bond market
			var startMarket = CreateMarketCondition(bondId, startBondId, startDate, market, holdingPeriod.StartFixingRate);
			var endMarket = CreateMarketCondition(bondId, endBondId, endDate, market, holdingPeriod.EndFixingRate);

			// Calc Ai
			var startAiCashflow = CalcBond(bond, startMarket);
			var endAiCashflow = CalcBond(bond, endMarket);
			var principalBetweenTemp = startAiCashflow.Cashflows.Where(x => x.CashflowType == CashflowType.Principal && x.AccrualEndDate > startDate && x.AccrualEndDate <= endDate).Sum(cashflow => cashflow.PaymentAmount);
			var interestBetweenTemp = startAiCashflow.Cashflows.Where(x => x.CashflowType == CashflowType.Coupon && x.AccrualEndDate > startDate && x.AccrualEndDate <= endDate).Sum(cashflow => cashflow.PaymentAmount);
			var yieldPricer = new HoldingPeriodYieldPricer(holdingPeriod, startAiCashflow.Ai, endAiCashflow.Ai, principalBetweenTemp, interestBetweenTemp);

			var holdingPeriodResult = new Dictionary<string, double>();

			holdingPeriodResult["holdingCost"] = double.IsNaN(holdingPeriod.HoldingCost) ? 0.0 : holdingPeriod.HoldingCost;
			holdingPeriodResult["startFrontCommission"] = double.IsNaN(holdingPeriod.StartFrontCommission) ? 0.0 : holdingPeriod.StartFrontCommission;
			holdingPeriodResult["startBackCommission"] = double.IsNaN(holdingPeriod.StartBackCommission) ? 0.0 : holdingPeriod.StartBackCommission;
			holdingPeriodResult["endFrontCommission"] = double.IsNaN(holdingPeriod.EndFrontCommission) ? 0.0 : holdingPeriod.EndFrontCommission;
			holdingPeriodResult["endBackCommission"] = double.IsNaN(holdingPeriod.EndBackCommission) ? 0.0 : holdingPeriod.EndBackCommission;
			// option date
			holdingPeriodResult["hasStartOption"] = ExecutionOptionDate(startDate, holdingPeriod.UnderlyingBond, startAiCashflow.Cashflows) == null ? 1.0 : 0.0;
			holdingPeriodResult["hasEndOption"] = ExecutionOptionDate(endDate, holdingPeriod.UnderlyingBond, endAiCashflow.Cashflows) == null ? 1.0 : 0.0;

			var isCalcStartCleanPrice = false;
			var isCalcEndCleanPrice = false;
			string functionType = "";
			double inputPar1 = 0.0;
			double inputPar2 = 0.0;
			if (result.IsRequested(PricingRequest.NetAnnualizedYield))
			{
				var startResult = CalcStartBond(startMarket, bondId, bond, holdingPeriodResult);
				var endResult = CalcEndBond(endMarket, bondId, bond, holdingPeriodResult);

				var startCleanPrice = _roundCleanPrice ? Math.Round(startResult.CleanPrice, 4, MidpointRounding.AwayFromZero) : startResult.CleanPrice;
				var endCleanPrice = _roundCleanPrice ? Math.Round(endResult.CleanPrice, 4, MidpointRounding.AwayFromZero) : endResult.CleanPrice;
				// Calc
				functionType = "AnnualYieldFromCleanPrice";
				inputPar1 = startCleanPrice;
				inputPar2 = endCleanPrice;
			}
			else if (result.IsRequested(PricingRequest.CleanPrice) && market.MktQuote.Value[bondId].Item1 == PriceQuoteType.None)
			{
				if (market.MktQuote.Value.ContainsKey(startBondId))
				{
					var startResult = CalcStartBond(startMarket, bondId, bond, holdingPeriodResult);
					var startCleanPrice = _roundCleanPrice ? Math.Round(startResult.CleanPrice, 4, MidpointRounding.AwayFromZero) : startResult.CleanPrice;
					var annualizedYield = market.MktQuote.Value[bondId].Item2;
					// Calc
					functionType = "EndCleanPriceFromAnnual";
					inputPar1 = startCleanPrice;
					inputPar2 = annualizedYield;
					isCalcEndCleanPrice = true;
				}
				else if (market.MktQuote.Value.ContainsKey(endBondId))
				{
					var endResult = CalcEndBond(endMarket, bondId, bond, holdingPeriodResult);
					var annualizedYield = market.MktQuote.Value[bondId].Item2;
					var endCleanPrice = _roundCleanPrice ? Math.Round(endResult.CleanPrice, 4, MidpointRounding.AwayFromZero) : endResult.CleanPrice;
					// Calc
					functionType = "StartCleanPriceFromAnnual";
					inputPar1 = endCleanPrice;
					inputPar2 = annualizedYield;
					isCalcStartCleanPrice = true;
				}
			}
			else if (result.IsRequested(PricingRequest.CleanPrice) && market.MktQuote.Value.ContainsKey(bondId) &&
			         market.MktQuote.Value[bondId].Item1 == PriceQuoteType.NetPnl)
			{
				if (market.MktQuote.Value.ContainsKey(startBondId))
				{
					var startResult = CalcStartBond(startMarket, bondId, bond, holdingPeriodResult);
					var netPnl = market.MktQuote.Value[bondId].Item2;
					var startCleanPrice = _roundCleanPrice ? Math.Round(startResult.CleanPrice, 4, MidpointRounding.AwayFromZero) : startResult.CleanPrice;
					// Calc
					functionType = "EndCleanPriceFromPnl";
					inputPar1 = startCleanPrice;
					inputPar2 = netPnl;
					isCalcEndCleanPrice = true;
				}
				else if (market.MktQuote.Value.ContainsKey(endBondId))
				{
					var endResult = CalcEndBond(endMarket, bondId, bond, holdingPeriodResult);
					var netPnl = market.MktQuote.Value[bondId].Item2;
					var endCleanPrice = _roundCleanPrice ? Math.Round(endResult.CleanPrice, 4, MidpointRounding.AwayFromZero) : endResult.CleanPrice;
					// Calc
					functionType = "StartCleanPriceFromPnl";
					inputPar1 = endCleanPrice;
					inputPar2 = netPnl;
					isCalcStartCleanPrice = true;
				}
			}
			else
			{
				CalcStartBond(startMarket, bondId, bond, holdingPeriodResult);
				CalcEndBond(endMarket, bondId, bond, holdingPeriodResult);
			}

			// Calc
			yieldPricer.CalcAnnualizedYieldCleanPrice(functionType, inputPar1, inputPar2, holdingPeriodResult);
			if (isCalcStartCleanPrice)
			{
				// Calc startDate bond
				var mktQuote = new Dictionary<string, Tuple<PriceQuoteType, double>> { { bondId, Tuple.Create(PriceQuoteType.Clean, holdingPeriodResult["startCleanPrice"]) } };
				var newMarket = startMarket.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, Tuple<PriceQuoteType, double>>>(x => x.MktQuote, mktQuote));
				CalcStartBond(newMarket, bondId, bond, holdingPeriodResult);
			}
			if (isCalcEndCleanPrice)
			{
				// Calc endDate bond
				var mktQuote = new Dictionary<string, Tuple<PriceQuoteType, double>> { { bondId, Tuple.Create(PriceQuoteType.Clean, holdingPeriodResult["endCleanPrice"]) } };
				var newMarket = endMarket.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, Tuple<PriceQuoteType, double>>>(x => x.MktQuote, mktQuote));
				CalcEndBond(newMarket, bondId, bond, holdingPeriodResult);
			}

			ParseResult(psDict, bondId, holdingPeriodResult);

			result.ProductSpecific = psDict;
			var endValuation = DateTime.Now;
			result.CalcTimeInMilliSecond = (endValuation - beginValuation).TotalMilliseconds;
			return result;
		}

		private static string ExecutionOptionDate(Date tradeDate, Bond bondInfo, Cashflow[] cashflows)
		{
			var dateDic = new KeyValuePair<string, double>();
			if (bondInfo.OptionToCall != null)
			{
				dateDic = bondInfo.OptionToCall.FirstOrDefault(x => x.Key.ToDate() > tradeDate);
			}
			else if (bondInfo.OptionToPut != null)
			{
				dateDic = bondInfo.OptionToPut.FirstOrDefault(x => x.Key.ToDate() > tradeDate);
			}
			else if (bondInfo.OptionToAssPut != null)
			{
				dateDic = bondInfo.OptionToAssPut.FirstOrDefault(x => cashflows[Convert.ToInt16(x.Key)].AccrualStartDate > tradeDate);
			}

			if (string.IsNullOrEmpty(dateDic.Key))
			{
				return null;
			}
			return dateDic.Key;
		}

		private void ParseResult(Dictionary<string, Dictionary<string, RateRecord>> psDict, string bondId, Dictionary<string, double> result)
		{
			foreach (var key in result.Keys)
			{
				psDict[key] = result.Where(x => x.Key == key).Select(x => Tuple.Create(bondId, new RateRecord { Rate = x.Value })).ToDictionary(x => x.Item1, x => x.Item2);
			}
			NegativeCommissionAndCost(psDict);
		}

		private void NegativeCommissionAndCost(Dictionary<string, Dictionary<string, RateRecord>> psDict)
		{
			foreach (var negativeKey in _negativeList)
			{
				if (psDict.ContainsKey(negativeKey))
				{
					var dic = psDict[negativeKey];
					foreach (var key in dic.Keys)
					{
						dic[key].Rate = dic[key].Rate < 0 ? dic[key].Rate : -dic[key].Rate;
					}
				}
			}
		}

		private IPricingResult CalcStartBond(IMarketCondition market, string bondId, Bond bond, Dictionary<string, double> result)
		{
			var startResult = BondCalculate(bondId, bond, market);
			if (startResult != null)
			{
				result["startCleanPrice"] = _roundCleanPrice ? Math.Round(startResult.CleanPrice, 4, MidpointRounding.AwayFromZero) : startResult.CleanPrice;
				result["startFullPrice"] = startResult.DirtyPrice;
				result["startAccruedAmount"] = startResult.Ai;
				result["startYield"] = startResult.Ytm;
				if (!double.IsNaN(startResult.YieldToCall) || !double.IsNaN(startResult.YieldToPut))
				{
					result["startYieldToExecution"] = double.IsNaN(startResult.YieldToCall)
						? startResult.YieldToPut
						: startResult.YieldToCall;
				}
			}
			return startResult;
		}

		private IPricingResult CalcEndBond(IMarketCondition market, string bondId, Bond bond, Dictionary<string, double> result)
		{
			var endResult = BondCalculate(bondId, bond, market);
			if (endResult != null)
			{
				result["endCleanPrice"] = _roundCleanPrice ? Math.Round(endResult.CleanPrice, 4, MidpointRounding.AwayFromZero) : endResult.CleanPrice;
				result["endFullPrice"] = endResult.DirtyPrice;
				result["endAccruedAmount"] = endResult.Ai;
				result["endYield"] = endResult.Ytm;
				if (!double.IsNaN(endResult.YieldToCall) || !double.IsNaN(endResult.YieldToPut))
				{
					result["endYieldToExecution"] = double.IsNaN(endResult.YieldToCall) ? endResult.YieldToPut : endResult.YieldToCall;
				}
			}
			return endResult;
		}

		private IPricingResult BondCalculate(string quoteBondId, Bond bond, IMarketCondition market)
		{
			var bondQuote = market.MktQuote.Value.ContainsKey(quoteBondId) ? market.MktQuote.Value[quoteBondId] : null;
			var executionYieldPricingRequest = PricingRequest.None;
			IPricingResult bondResult = null;
			if (bondQuote != null && bondQuote.Item1 != PriceQuoteType.None)
			{
				if (bondQuote.Item1 == PriceQuoteType.Dirty || bondQuote.Item1 == PriceQuoteType.Clean)
				{
					executionYieldPricingRequest = PricingRequest.Ytm;
				}
				else if (bondQuote.Item1 == PriceQuoteType.Ytm || bondQuote.Item1 == PriceQuoteType.YtmExecution)
				{
					executionYieldPricingRequest = PricingRequest.CleanPrice;
				}

				bondResult = CalcBond(bond, market, executionYieldPricingRequest | PricingRequest.YtmExecution);
			}

			return bondResult;
		}

		private IPricingResult CalcBond(Bond bond, IMarketCondition market, PricingRequest pricingRequest = PricingRequest.None)
		{
			var request = bond.BondTradeingMarket == TradingMarket.ChinaInterBank ? PricingRequest.Ai : PricingRequest.AiEod;
			var bondResult = _bondEngine.Calculate(bond, market, request | PricingRequest.Cashflow | pricingRequest);
			return bondResult;
		}

		private IMarketCondition CreateMarketCondition(string bondId, string quoteBondId, Date valueDate, IMarketCondition market, double fixingRate)
		{
			var bondQuote = market.MktQuote.Value.ContainsKey(quoteBondId) ? market.MktQuote.Value[quoteBondId] : null;
			var mktQuote = new Dictionary<string, Tuple<PriceQuoteType, double>> { { bondId, bondQuote } };
			var newMarket = market.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, Tuple<PriceQuoteType, double>>>(x => x.MktQuote, mktQuote));
			newMarket = newMarket.UpdateCondition(new UpdateMktConditionPack<Date>(x => x.ValuationDate, valueDate));

			if (market.FixingCurve.HasValue && market.FixingCurve.Value != null)
			{
				// fixing rate yield curve
				newMarket = newMarket.UpdateCondition(new UpdateMktConditionPack<IYieldCurve>(x => x.FixingCurve, newMarket.FixingCurve.Value.UpdateReferenceDate(valueDate)));
				var keyTenors = new string[newMarket.FixingCurve.Value.GetKeyTenors().Length];
				newMarket.FixingCurve.Value.GetKeyTenors().CopyTo(keyTenors, 0);
				for (var i = 0; i < keyTenors.Length; ++i)
				{
					newMarket = newMarket.UpdateCondition(new UpdateMktConditionPack<IYieldCurve>(x => x.FixingCurve, newMarket.FixingCurve.Value.BumpKeyRate(i, fixingRate)));
				}
			}

			return newMarket;
		}
	}
}
