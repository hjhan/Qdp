using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.Market.Spread;
using Qdp.Pricing.Library.Common.Utilities;

namespace Qdp.Pricing.Library.Common.Engines
{
    /// <summary>
    /// 债券计算引擎类
    /// </summary>
	public class BondEngine : CashflowProductEngine<Bond>
	{
		private readonly IBondYieldPricer _bondYieldPricer;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="bondYieldPricer">债券收益率计算器</param>
		public BondEngine(IBondYieldPricer bondYieldPricer = null)
		{
			_bondYieldPricer = bondYieldPricer ?? new BondYieldPricerCn();
		}

        /// <summary>
        /// 计算一个债券的估值和基本风险指标
        /// </summary>
        /// <param name="bond">债券</param>
        /// <param name="market">市场数据对象</param>
        /// <param name="request">计算请求类型</param>
        /// <returns>计算结果</returns>
        public override IPricingResult Calculate(Bond bond, IMarketCondition market, PricingRequest request)
        {
            var result = new PricingResult(market.ValuationDate, request);

            var cfs = bond.GetCashflows(market, true);
            var cfAis = bond.GetAiCashflows(market, false);

            //Ai and AiEod are mutually exclusive requests
            var isEod = result.IsRequested(PricingRequest.AiEod);
            if (result.IsRequested(PricingRequest.Ai) || result.IsRequested(PricingRequest.AiEod))
            {
                result.Ai = bond.GetAccruedInterest(market.ValuationDate, cfAis, isEod);
                result.AiDays = bond.GetAccruedInterestDays(market.ValuationDate, cfAis, isEod);
            }

            if (result.IsRequested(PricingRequest.Ytm) || result.IsRequested(PricingRequest.DirtyPrice) || result.IsRequested(PricingRequest.CleanPrice) 
                || result.IsRequested(PricingRequest.ModifiedDuration) || result.IsRequested(PricingRequest.Convexity)
                || (result.IsRequested(PricingRequest.DollarDuration)) || result.IsRequested(PricingRequest.DollarConvexity)
                || result.IsRequested(PricingRequest.MacDuration) || result.IsRequested(PricingRequest.Pv01) 
                || result.IsRequested(PricingRequest.ZeroSpread) || result.IsRequested(PricingRequest.Dv01) 
                || result.IsRequested(PricingRequest.KeyRateDv01))
            {
                double unitDirtyPrice = 0;
                double unitCleanPrice = 0;
                if (double.IsNaN(result.Ai))
                {
                    result.Ai = bond.GetAccruedInterest(market.ValuationDate, cfAis, isEod);
                    result.AiDays = bond.GetAccruedInterestDays(market.ValuationDate, cfAis, isEod);
                }
                var bondQuote = market.MktQuote.Value[bond.Id];
                if (bondQuote.Item1 == PriceQuoteType.Dirty)
                {
                    result.DirtyPrice = bondQuote.Item2 * bond.Notional / 100.0;
                    result.Ytm = _bondYieldPricer.YieldFromFullPrice(cfs, bond.PaymentDayCount, bond.PaymentFreq, bond.StartDate, market.ValuationDate, result.DirtyPrice, bond.BondTradeingMarket, bond.IrregularPayment);
                    result.CleanPrice = result.DirtyPrice - result.Ai;
                    unitDirtyPrice = bondQuote.Item2;
                    unitCleanPrice = result.CleanPrice / bond.Notional * 100.0;
                }
                else if (bondQuote.Item1 == PriceQuoteType.Clean)
                {
                    result.CleanPrice = bondQuote.Item2 * bond.Notional / 100.0;
                    result.DirtyPrice = result.CleanPrice + result.Ai;

                    unitCleanPrice = bondQuote.Item2;
                    unitDirtyPrice = result.DirtyPrice / bond.Notional * 100;
                    result.Ytm = _bondYieldPricer.YieldFromFullPrice(cfs, bond.PaymentDayCount, bond.PaymentFreq, bond.StartDate, market.ValuationDate, result.DirtyPrice, bond.BondTradeingMarket, bond.IrregularPayment);

                }
                else
                {
                    result.Ytm = bondQuote.Item2;
                    result.DirtyPrice = _bondYieldPricer.FullPriceFromYield(cfs, bond.PaymentDayCount, bond.PaymentFreq, bond.StartDate, market.ValuationDate, result.Ytm, bond.BondTradeingMarket, bond.IrregularPayment);
                    result.CleanPrice = result.DirtyPrice - result.Ai;
                    unitCleanPrice = result.CleanPrice / 100.0;
                    unitDirtyPrice = result.DirtyPrice / 100.0;
                }

                if (result.IsRequested(PricingRequest.Convexity) || result.IsRequested(PricingRequest.ModifiedDuration)
                    ||result.IsRequested(PricingRequest.DollarConvexity) || result.IsRequested(PricingRequest.DollarDuration)
                    ||result.IsRequested(PricingRequest.MacDuration) || result.IsRequested(PricingRequest.Pv01))
                {
                    result.MacDuration = _bondYieldPricer.GetMacDuration(cfs, bond.PaymentDayCount, bond.PaymentFreq, bond.StartDate, market.ValuationDate, result.Ytm, bond.BondTradeingMarket);
                    
                    //modified duration here = 1% move, price change, per 100 notional dollar bond
                    result.ModifiedDuration = BondPricingFunctions.GetModifiedDuration(cfs, bond.PaymentDayCount, bond.PaymentFreq, bond.StartDate, market.ValuationDate, result.Ytm, bond.BondTradeingMarket, bond.IrregularPayment, _bondYieldPricer);

                    //1% convexity here, modified duration move, further multiplied by 10000
                    result.Convexity = BondPricingFunctions.GetConvexity(cfs, bond.PaymentDayCount, bond.PaymentFreq, bond.StartDate, market.ValuationDate, result.Ytm,
                        bond.BondTradeingMarket, bond.IrregularPayment, _bondYieldPricer);

                    //1% impact, dollar duration is modified duration in dollar term, but scales with notional, for calculating bond book avg portfolio duration, and display purpose
                    result.DollarModifiedDuration = result.ModifiedDuration * unitDirtyPrice * 0.0001 * bond.Notional;

                    //1% impact, dollar convexity is for calculating both pnl and avg book convexity
                    result.DollarConvexity = result.Convexity / 100 * unitDirtyPrice * bond.Notional;

                    //1bp dollar impact, scales with Notional
                    result.Pv01 = (-result.DollarModifiedDuration + 0.5 * result.DollarConvexity * 0.0001 * 0.0001) / 100.0;
                }
            }

            if (result.IsRequested(PricingRequest.Pv))
            {
                result.Pv = cfs.Where(x => x.PaymentDate > market.ValuationDate)
                .Sum(x => x.PaymentAmount * market.DiscountCurve.Value.GetDf(market.ValuationDate, x.PaymentDate));

            }

            if (result.IsRequested(PricingRequest.ZeroSpread) || result.IsRequested(PricingRequest.Dv01) || result.IsRequested(PricingRequest.KeyRateDv01) || result.IsRequested(PricingRequest.ZeroSpreadDelta))
			{
				result.ZeroSpread = BondPricingFunctions.ZeroSpread(cfs, market.DiscountCurve.Value, market.ValuationDate,result.DirtyPrice);
                result.ZeroSpreadDelta = BondPricingFunctions.ZeroSpreadRisk(cfs, market.DiscountCurve.Value, market.ValuationDate, result.ZeroSpread);
                if (market.CreditSpread.HasValue)
				{
					market =
						market.UpdateCondition(new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve,
							market.DiscountCurve.Value.GetSpreadedCurve(market.CreditSpread.Value)));
				}
				else
				{
					market =
						market.UpdateCondition(new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve,
							market.DiscountCurve.Value.GetSpreadedCurve(new ZeroSpread(result.ZeroSpread))));
				}
			}

			if (result.IsRequested(PricingRequest.Dv01))
			{
				result.Dv01 = base.Calculate(bond, market, PricingRequest.Dv01).Dv01;
			}

			if(result.IsRequested(PricingRequest.KeyRateDv01))
			{
				result.KeyRateDv01 = base.Calculate(bond, market, PricingRequest.KeyRateDv01).KeyRateDv01;
			}

			if (result.IsRequested(PricingRequest.Cashflow))
			{
				result.Cashflows = bond.GetCashflows(market, false);
				result.CashflowDict = result.Cashflows.ToDictionary(x => x.ToCfKey(), x => x.PaymentAmount);
			}

			//if (bond.Coupon is FloatingCoupon)
			//{
			//	var primeRateDate = market.ValuationDate;
			//	//var primeRateCashflow = cfAis.FirstOrDefault(x => x.AccrualEndDate > market.ValuationDate && x.AccrualStartDate <= market.ValuationDate);
			//	//if (primeRateCashflow != null)
			//	//{
			//	//	primeRateDate = primeRateCashflow.AccrualStartDate;
			//	//}
			//	var fixingCurve = market.FixingCurve ?? market.DiscountCurve ?? market.RiskfreeCurve;
			//	var fixingTuple = bond.Coupon.GetPrimeCoupon(market.HistoricalIndexRates, fixingCurve.Value, primeRateDate);
			//	if (fixingTuple != null && fixingTuple.Item1 != null)
			//		result.ProductSpecific = new Dictionary<string, Dictionary<string, RateRecord>>
			//		{
			//			{
			//				"currentPrimeRate",
			//				new Dictionary<string, RateRecord>
			//				{
			//					{
			//						bond.Id,
			//						new RateRecord
			//						{
			//							Rate = fixingTuple.Item2,
			//							Date = fixingTuple.Item1.ToString()
			//						}
			//					}
			//				}
			//			}
			//		};
			//}

			return result;
		}
	}
}
