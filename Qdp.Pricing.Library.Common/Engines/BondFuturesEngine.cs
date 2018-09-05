using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.MathMethods.Interfaces;
using Qdp.Pricing.Library.Common.Utilities;

namespace Qdp.Pricing.Library.Common.Engines
{
	public class BondFuturesEngine<TInterestRateModel> : Engine<BondFutures>
		where TInterestRateModel : class, IOneFactorModel
	{
		private readonly TInterestRateModel _interestRateModel;
		private readonly int _steps;
		private readonly BondEngine _bondEngine;

		public BondFuturesEngine(TInterestRateModel interestRateModel, int steps)
		{
			_interestRateModel = interestRateModel;
			_steps = steps;
			_bondEngine = new BondEngine();
		}

		public override IPricingResult Calculate(BondFutures bondFuture, IMarketCondition market, PricingRequest request)
		{
			var beginValuation = DateTime.Now;
			var pricingRequest = CheckParameterCondition(bondFuture, market, request);

            //if (result.IsRequested(PricingRequest.Dv01))
            //{
            //	result.Dv01 = CalcDv01(bondFuture, market);
            //}

            var result = new PricingResult(market.ValuationDate, pricingRequest);

            //if (result.IsRequested(PricingRequest.Pv))
            //{
            //    result.Pv = CalcPv(bondFuture, market);
            //}

            if (result.IsRequested(PricingRequest.DirtyPrice))
            {
                result.DirtyPrice = market.MktQuote.Value.Where(x => x.Key == bondFuture.Id).Select(x => x.Value.Item2).First() * bondFuture.Notional / 100;
            }

            if (result.IsRequested(PricingRequest.ConvertFactors))
			{
				result.ConvertFactors = CalcConvertFactors(bondFuture, market);
			}

			if (result.IsRequested(PricingRequest.Irr) || result.IsRequested(PricingRequest.Pv01) 
                || result.IsRequested(PricingRequest.KeyRateDv01) 
                || result.IsRequested(PricingRequest.ZeroSpread) || result.IsRequested(PricingRequest.ZeroSpreadDelta) 
                || result.IsRequested(PricingRequest.UnderlyingPv) || result.IsRequested(PricingRequest.Basis)
                || result.IsRequested(PricingRequest.Convexity) || result.IsRequested(PricingRequest.Ytm) 
                || result.IsRequested(PricingRequest.CheapestToDeliver) 
                || result.IsRequested(PricingRequest.ModifiedDuration) || result.IsRequested(PricingRequest.MacDuration)
                )
            {
                //TODO:  wierd update logic, why bother?
                var mktQuote = market.MktQuote.Value.Keys.Where(quoteKey => !quoteKey.Contains(bondFuture.Id + "_")).ToDictionary(quoteKey => quoteKey, quoteKey => market.MktQuote.Value[quoteKey]);
				var updateMarket = market.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, Tuple<PriceQuoteType, double>>>(x => x.MktQuote, mktQuote));
				var yieldPricer = new BondFuturesYieldPricer(bondFuture, updateMarket);

                //calculate convert factors
                if (!result.ConvertFactors.Any())
                {
                    result.ConvertFactors = CalcConvertFactors(bondFuture, market);
                }
                var convertFactors = result.ConvertFactors;

                //calculate IRR
                result.ProductSpecific = yieldPricer.CalcEquation("FromFuturesPriceAndBondPrice");

				//calculate pv01
				var maxIrr = result.ProductSpecific["Irr"].Values.Select(x => x.Rate).Max();
				var ctdBondId = result.ProductSpecific["Irr"].First(x => x.Value.Rate == maxIrr).Key;
                var ctdBond = bondFuture.Deliverables.First(x => x.Id == ctdBondId);
                var cf = convertFactors[ctdBondId];
                var scaling = bondFuture.Notional / (100.0 * cf);

                var engine = new BondEngineCn();

                result.CheapestToDeliver = ctdBondId;

                //two risks here are CTD risk, not bond futures risk
                var  resultCTD = engine.Calculate(ctdBond, market, PricingRequest.All);
                result.ZeroSpread = resultCTD.ZeroSpread;
                result.UnderlyingPv = resultCTD.Pv * scaling;
                result.Basis = yieldPricer.CalcFutureCtdBasis(ctdBond, cf);
                result.Ytm = resultCTD.Ytm;
                result.MacDuration = resultCTD.MacDuration;

                result.ModifiedDuration = resultCTD.ModifiedDuration;
                result.Convexity = resultCTD.Convexity;
                result.DollarConvexity = resultCTD.DollarConvexity * scaling;  // 1% price impact
                result.DollarModifiedDuration = resultCTD.DollarModifiedDuration * scaling;  // same order of magnitutude of CTD dollar modifiedDuration, good for pnl attribution

                //convert to bond futures risk
                result.Pv01 = resultCTD.Pv01 * scaling;   // underlying pv01 is
                foreach (var kvp in resultCTD.KeyRateDv01)
                {
                    foreach (var risk in kvp.Value)
                    {
                        risk.Risk *= scaling;
                    }
                }
                result.KeyRateDv01 = resultCTD.KeyRateDv01;
                result.ZeroSpreadDelta = resultCTD.ZeroSpreadDelta * scaling;

            }
			if (result.IsRequested(PricingRequest.FairQuote))
			{
				var mktQuote = market.MktQuote.Value.Keys.Where(quoteKey => !quoteKey.Equals(bondFuture.Id)).ToDictionary(quoteKey => quoteKey, quoteKey => market.MktQuote.Value[quoteKey]);
				var updateMarket = market.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, Tuple<PriceQuoteType, double>>>(x => x.MktQuote, mktQuote));
				var yieldPricer = new BondFuturesYieldPricer(bondFuture, updateMarket);
				result.ProductSpecific = yieldPricer.CalcEquation("FromBondPriceAndIrr");
			}
			if (result.IsRequested(PricingRequest.MktQuote))
			{
				result.ProductSpecific = CalcMktFuturePrice(bondFuture, market);
			}
			if (result.IsRequested(PricingRequest.UnderlyingFairQuote))
			{
				var mktQuote = market.MktQuote.Value.Keys.Where(quoteKey => quoteKey.Contains(bondFuture.Id)).ToDictionary(quoteKey => quoteKey, quoteKey => market.MktQuote.Value[quoteKey]);
				var updateMarket = market.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, Tuple<PriceQuoteType, double>>>(x => x.MktQuote, mktQuote));
				var yieldPricer = new BondFuturesYieldPricer(bondFuture, updateMarket);
				result.ProductSpecific = yieldPricer.CalcEquation("FromFuturesPriceAndIrr");
			}


			var endValuation = DateTime.Now;
			result.CalcTimeInMilliSecond = (endValuation - beginValuation).TotalMilliseconds;
			return result;
		}

		private PricingRequest CheckParameterCondition(BondFutures bondFuture, IMarketCondition market, PricingRequest request)
		{
			var pricingRequest = request;
			var tfId = bondFuture.Id;
			var bondIds = bondFuture.Deliverables.Select(x => x.Id).ToArray();
			var treasuryIds = bondIds.Select(x => tfId + "_" + x).ToArray();
			var mktQuotes = market.MktQuote.Value;

			if (mktQuotes != null && mktQuotes.Count > 0)
			{
				var tfHasValue = mktQuotes.ContainsKey(tfId);
				var bondHasValue = bondIds.Select(x => mktQuotes.ContainsKey(x)).Count(y => y) > 0;
				var treasuryHasValue = treasuryIds.Select(x => mktQuotes.ContainsKey(x)).Count(y => y) > 0;
				if (request == PricingRequest.All)
				{
					if (tfHasValue && bondHasValue)
					{
						pricingRequest = PricingRequest.Irr;
					}
					else if (bondHasValue && treasuryHasValue)
					{
						pricingRequest = PricingRequest.FairQuote;
					}
					else if (tfHasValue && treasuryHasValue)
					{
						pricingRequest = PricingRequest.UnderlyingFairQuote;
					}
				}
			}
			return pricingRequest;
		}

        //only used if we switch from CTD model to basket model
		private double CalcDv01(BondFutures bondFuture, IMarketCondition market)
		{
			var discountCurve = market.DiscountCurve.Value;
			var dayCount = bondFuture.DayCount;

			var tree = _interestRateModel.Tree(GetGridTimes(bondFuture, market), 0.0, discountCurve);

			var deliveryTime = dayCount.CalcDayCountFraction(market.ValuationDate, bondFuture.UnderlyingMaturityDate);

			var bonds = bondFuture.Deliverables;
			var ais = bonds.Select(x => x.GetAccruedInterest(bondFuture.UnderlyingMaturityDate, market, true)).ToArray();
			var conversionFactors = bonds.Select(x => bondFuture.GetConversionFactor(x, market)).ToArray();

			var zeroSpreads =
				bonds.Select((bond, i) =>
					BondPricingFunctions.ZeroSpread(bond, market)
					).ToArray();

			var simulationRate = tree.ShortRate(0, 0);
			var bondPrices = bonds
				.Select((x, i) =>
				{
					var cfs = x.GetCashflows(market).Where(cf => cf.PaymentDate > bondFuture.UnderlyingMaturityDate).ToArray();
					var bPrice = 0.0;
					foreach (var cf in cfs)
					{
						var T = dayCount.CalcDayCountFraction(market.ValuationDate, cf.PaymentDate);
						var P = tree.DiscountBond(deliveryTime, T, simulationRate);
						var rate = discountCurve.Compound.CalcRateFromDf(P, T - deliveryTime);
						var df = 1.0 / discountCurve.Compound.CalcCompoundRate(T - deliveryTime, rate + zeroSpreads[i]);
						bPrice += cf.PaymentAmount * df;
					}
					return bPrice;
				}).ToArray();

			var futurePrices = bondPrices
				.Select((price, k) => price - ais[k])
				.Select((cleanPrice, k) => cleanPrice / conversionFactors[k]).ToList();

			var futurePrice = futurePrices.Min();
			var index = futurePrices.FirstIndexOf(x => (x - futurePrice).IsAlmostZero());
			var ctdBond = bonds[index];
			var ctdBondPrice = market.MktQuote.Value[ctdBond.Id];

			return
				new BondEngine().Calculate(ctdBond, market, PricingRequest.Dv01).Dv01;

		}

		private double CalcPv(BondFutures bondFuture, IMarketCondition market)
		{
			var discountCurve = market.DiscountCurve.Value;
			var dayCount = bondFuture.DayCount;

			var tree = _interestRateModel.Tree(GetGridTimes(bondFuture, market), 0.0, discountCurve);

			var deliveryTime = dayCount.CalcDayCountFraction(market.ValuationDate, bondFuture.UnderlyingMaturityDate);
			var indexAtDelivery = tree.IndexAtT(deliveryTime);

			var bonds = bondFuture.Deliverables;
			var ais = bonds.Select(x => x.GetAccruedInterest(bondFuture.UnderlyingMaturityDate, market, true)).ToArray();
			var conversionFactors = bonds.Select(x => bondFuture.GetConversionFactor(x, market)).ToArray();

			var zeroSpreads =
				bonds.Select((bond, i) => BondPricingFunctions.ZeroSpread(bond, market)
					).ToArray();

			var nNodesAtDelivery = tree.TrinomialTree.Branchings[indexAtDelivery].Size;

			var futurePricesAtDelivery = new double[nNodesAtDelivery];
			for (var j = 0; j < nNodesAtDelivery; ++j)
			{
				var simulationRate = tree.ShortRate(indexAtDelivery, j);
				var bondPrices = bonds
					.Select((x, i) =>
					{
						var cfs = x.GetCashflows(market).Where(cf => cf.PaymentDate > bondFuture.UnderlyingMaturityDate).ToArray();
						var bPrice = 0.0;
						foreach (var cf in cfs)
						{
							var T = dayCount.CalcDayCountFraction(market.ValuationDate, cf.PaymentDate);
							var P = tree.DiscountBond(deliveryTime, T, simulationRate);
							var rate = discountCurve.Compound.CalcRateFromDf(P, T - deliveryTime);
							var df = 1.0 / discountCurve.Compound.CalcCompoundRate(T - deliveryTime, rate + zeroSpreads[i]);
							bPrice += cf.PaymentAmount * df;
						}
						return bPrice;
					}).ToArray();

				var futurePricesAtNodes = bondPrices
					.Select((price, k) => price - ais[k])
					.Select((cleanPrice, k) => cleanPrice / conversionFactors[k]).ToList();
				futurePricesAtDelivery[j] = futurePricesAtNodes.Min();
			}

			return tree.ReverseInduction(null, null, indexAtDelivery, futurePricesAtDelivery, false)[0][0];

		}

		private double[] GetGridTimes(BondFutures bondFuture, IMarketCondition market)
		{
			var begin = 0.0;
			var end = bondFuture.DayCount.CalcDayCountFraction(market.ValuationDate, bondFuture.UnderlyingMaturityDate);

			var grids = new[] { begin, end, end + (end - begin) / _steps };
			if (_steps == 0)
			{
				return grids;
			}
			else
			{
				var newGrids = new List<double>();
				var dtMax = (grids.Last() - grids[0]) / (_steps + 1);
				newGrids.Add(grids[0]);
				for (var i = 1; i < grids.Length; ++i)
				{
					begin = grids[i - 1];
					end = grids[i];
					var n = (int)((end - begin) / dtMax + 0.5);
					if (n == 0) n = 1;
					var dt = (end - begin) / n;
					for (var j = 1; j <= n; ++j)
					{
						newGrids.Add(begin + j * dt);
					}
				}
				return newGrids.ToArray();
			}
		}

		private Dictionary<string, Dictionary<string, RateRecord>> CalcMktFuturePrice(BondFutures bondFuture, IMarketCondition market)
		{
			var psDict = new Dictionary<string, Dictionary<string, RateRecord>>();
			var fundingCurve = market.RiskfreeCurve.HasValue
				? market.RiskfreeCurve.Value
				: YieldCurve.GetConstRateCurve(market.DiscountCurve.Value, 0.0);
			var reinvestmentCurve = market.DiscountCurve.HasValue
				? market.DiscountCurve.Value
				: YieldCurve.GetConstRateCurve(market.RiskfreeCurve.Value, 0.0);

			var bonds = bondFuture.Deliverables;
			var length = bonds.Length;

			var aiAtStart = bonds.Select(x => x.GetAccruedInterest(market.ValuationDate, market, false)).ToArray();
			var aiAtEnd = bonds.Select(x => x.GetAccruedInterest(bondFuture.UnderlyingMaturityDate, market, false)).ToArray();
			var cf = bonds.Select(x => bondFuture.GetConversionFactor(x, market)).ToArray();

			var coupons = new double[length];
			var timeWeightedCoupon = new double[length];
			var couponsAccruedByReinvestment = new double[length];

			for (var i = 0; i < length; ++i)
			{
				var bond = bonds[i];
				var cashflows = bond.GetCashflows(market, false).Where(x => x.PaymentDate > market.ValuationDate && x.PaymentDate <= bondFuture.UnderlyingMaturityDate).ToArray();
				if (cashflows.Any())
				{
					coupons[i] = cashflows.Sum(x => x.PaymentAmount);
					timeWeightedCoupon[i] = cashflows.Sum(x => x.PaymentAmount * bondFuture.DayCount.CalcDayCountFraction(x.PaymentDate, bondFuture.UnderlyingMaturityDate));
					couponsAccruedByReinvestment[i] = cashflows.Sum(x => x.PaymentAmount * (reinvestmentCurve.GetCompoundedRate2(x.PaymentDate, bondFuture.UnderlyingMaturityDate) - 1.0));
				}
				else
				{
					coupons[i] = 0.0;
					timeWeightedCoupon[i] = 0.0;
					couponsAccruedByReinvestment[i] = 0.0;
				}
			}

			var interestIncome = bonds.Select((x, i) => aiAtEnd[i] - aiAtStart[i] + coupons[i] + couponsAccruedByReinvestment[i]).ToArray();
			//var interestIncome = bonds.Select((x, i) => aiAtEnd[i] - aiAtStart[i] + coupons[i]).ToArray();

			var dirtyPrice = new double[length];
			var cleanPrice = new double[length];
			var ytm = new double[length];
			var modifiedDuration = new double[length];

			var bondEngine = new BondEngine();
			for (var i = 0; i < length; ++i)
			{
				var bResult = bondEngine.Calculate(bonds[i], market, PricingRequest.Ytm | PricingRequest.ModifiedDuration);
				dirtyPrice[i] = bResult.DirtyPrice;
				cleanPrice[i] = bResult.CleanPrice;
				ytm[i] = bResult.Ytm;
			}

			var interestCostRate = fundingCurve.GetCompoundedRate2(market.ValuationDate, bondFuture.UnderlyingMaturityDate) - 1.0;
			var interestCost = dirtyPrice.Select(x => x * interestCostRate).ToArray();

			var futurePrice = new double[length];
			var irr = new double[length];
			var basis = new double[length];
			var pnl = new double[length];
			var netBasis = new double[length];
			var invoicePrice = new double[length];
			var margin = new double[length];
			var spread = new double[length];

			var fundingRate = fundingCurve.GetSpotRate(0.0);

			var compoundedRate = fundingCurve.GetCompoundedRate2(market.ValuationDate, bondFuture.UnderlyingMaturityDate);
			var bondPricesCompounedByFunding = bonds.Select((x, i) => dirtyPrice[i] * compoundedRate).ToArray();

			for (var i = 0; i < bonds.Length; ++i)
			{
				futurePrice[i] = (bondPricesCompounedByFunding[i] - aiAtEnd[i] - coupons[i]) / cf[i];

				var newMarket =
					market.UpdateCondition(
						new UpdateMktConditionPack<Dictionary<string, Tuple<PriceQuoteType, double>>>(x => x.MktQuote,
							market.MktQuote.Value.UpdateKey(bondFuture.Id, Tuple.Create(PriceQuoteType.Dirty, futurePrice[i]))));

				var yieldPricer = new BondFuturesYieldPricer(bondFuture, newMarket);
				var tmpResult = yieldPricer.CalcEquation("FromFuturesPriceAndBondPrice");

				invoicePrice[i] = tmpResult["InvoicePrice"][bonds[i].Id].Rate;
				margin[i] = tmpResult["Margin"][bonds[i].Id].Rate;

				basis[i] = tmpResult["Basis"][bonds[i].Id].Rate;
				pnl[i] = tmpResult["PnL"][bonds[i].Id].Rate;
				netBasis[i] = tmpResult["NetBasis"][bonds[i].Id].Rate;

				irr[i] = tmpResult["Irr"][bonds[i].Id].Rate;
				spread[i] = irr[i] - fundingRate;
			}

			psDict["FuturesPrice"] = futurePrice.Select((x, i) => Tuple.Create(bonds[i].Id, new RateRecord { Rate = x })).ToDictionary(x => x.Item1, x => x.Item2);
			psDict["BondDirtyPrice"] = dirtyPrice.Select((x, i) => Tuple.Create(bonds[i].Id, new RateRecord { Rate = x })).ToDictionary(x => x.Item1, x => x.Item2);
			psDict["BondCleanPrice"] = cleanPrice.Select((x, i) => Tuple.Create(bonds[i].Id, new RateRecord { Rate = x })).ToDictionary(x => x.Item1, x => x.Item2);
			psDict["BondYieldToMaturity"] = ytm.Select((x, i) => Tuple.Create(bonds[i].Id, new RateRecord { Rate = x })).ToDictionary(x => x.Item1, x => x.Item2);
			psDict["ConversionFactor"] = cf.Select((x, i) => Tuple.Create(bonds[i].Id, new RateRecord { Rate = x })).ToDictionary(x => x.Item1, x => x.Item2);
			psDict["AiStart"] = aiAtStart.Select((x, i) => Tuple.Create(bonds[i].Id, new RateRecord { Rate = x })).ToDictionary(x => x.Item1, x => x.Item2);
			psDict["AiEnd"] = aiAtEnd.Select((x, i) => Tuple.Create(bonds[i].Id, new RateRecord { Rate = x })).ToDictionary(x => x.Item1, x => x.Item2);
			psDict["Coupon"] = coupons.Select((x, i) => Tuple.Create(bonds[i].Id, new RateRecord { Rate = x })).ToDictionary(x => x.Item1, x => x.Item2);
			psDict["InterestIncome"] = interestIncome.Select((x, i) => Tuple.Create(bonds[i].Id, new RateRecord { Rate = x })).ToDictionary(x => x.Item1, x => x.Item2);
			psDict["InterestCost"] = interestCost.Select((x, i) => Tuple.Create(bonds[i].Id, new RateRecord { Rate = x })).ToDictionary(x => x.Item1, x => x.Item2);
			psDict["PnL"] = pnl.Select((x, i) => Tuple.Create(bonds[i].Id, new RateRecord { Rate = x })).ToDictionary(x => x.Item1, x => x.Item2);
			psDict["InvoicePrice"] = invoicePrice.Select((x, i) => Tuple.Create(bonds[i].Id, new RateRecord { Rate = x })).ToDictionary(x => x.Item1, x => x.Item2);
			psDict["Margin"] = margin.Select((x, i) => Tuple.Create(bonds[i].Id, new RateRecord { Rate = x })).ToDictionary(x => x.Item1, x => x.Item2);
			psDict["Spread"] = spread.Select((x, i) => Tuple.Create(bonds[i].Id, new RateRecord { Rate = x })).ToDictionary(x => x.Item1, x => x.Item2);
			psDict["Irr"] = irr.Select((x, i) => Tuple.Create(bonds[i].Id, new RateRecord { Rate = x })).ToDictionary(x => x.Item1, x => x.Item2);
			psDict["Basis"] = basis.Select((x, i) => Tuple.Create(bonds[i].Id, new RateRecord { Rate = x })).ToDictionary(x => x.Item1, x => x.Item2);
			psDict["NetBasis"] = netBasis.Select((x, i) => Tuple.Create(bonds[i].Id, new RateRecord { Rate = x })).ToDictionary(x => x.Item1, x => x.Item2);
			psDict["ModifiedDuration"] = modifiedDuration.Select((x, i) => Tuple.Create(bonds[i].Id, new RateRecord { Rate = x })).ToDictionary(x => x.Item1, x => x.Item2);
			psDict["TimeWeightedCoupon"] = timeWeightedCoupon.Select((x, i) => Tuple.Create(bonds[i].Id, new RateRecord { Rate = x })).ToDictionary(x => x.Item1, x => x.Item2);

			return psDict;
		}
		private Dictionary<string, double> CalcConvertFactors(BondFutures bondFuture, IMarketCondition market)
		{
			var bonds = bondFuture.Deliverables;
			var cfs = new Dictionary<string, double>();
			foreach (var bond in bonds)
			{
				var cf = bondFuture.GetConversionFactor(bond, market);
				cfs.Add(bond.Id, cf);
			}
			return cfs;
		}
	}
}
