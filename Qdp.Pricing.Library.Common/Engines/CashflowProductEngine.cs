using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.Utilities;

/// <summary>
/// Qdp.Pricing.Library.Common.Engines
/// </summary>
namespace Qdp.Pricing.Library.Common.Engines
{
    /// <summary>
    /// 现金流产品的估值引擎
    /// </summary>
    /// <typeparam name="TTrade"></typeparam>
	public class CashflowProductEngine<TTrade> : Engine<TTrade>
		where TTrade : class , ICashflowInstrument
	{
        /// <summary>
        /// 计算一个金融衍生品交易的定价和风险指标
        /// </summary>
        /// <param name="trade">交易</param>
        /// <param name="market">市场数据对象</param>
        /// <param name="request">计算请求类型</param>
        /// <returns>计算结果</returns>
		public override IPricingResult Calculate(TTrade trade, IMarketCondition market, PricingRequest request)
		{
			var result = new PricingResult(market.ValuationDate, request);

			if (result.IsRequested(PricingRequest.Pv))
			{
				result.Pv = CalcPv(trade, market);
			}

            if (result.IsRequested(PricingRequest.Carry)) {
                result.Carry = CalcCarry(trade, market);
            }

			if (result.IsRequested(PricingRequest.Dv01))
			{
				if (double.IsNaN(result.Pv))
				{
					result.Pv = CalcPv(trade, market);
				}
				var mktDown = market.FixingCurve.HasValue
					? market.UpdateCondition(
						new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve, market.DiscountCurve.Value.Shift(1)),
						new UpdateMktConditionPack<IYieldCurve>(x => x.FixingCurve, market.FixingCurve.Value.Shift(1)))
					: market.UpdateCondition(
						new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve, market.DiscountCurve.Value.Shift(1)));
				result.Dv01 = CalcPv(trade, mktDown) - result.Pv;
			}

			if (result.IsRequested(PricingRequest.Cashflow))
			{
				result.Cashflows = trade.GetCashflows(market, false);
			}

			//Ai and AiEod are mutually exclusive requests
			var isEod = result.IsRequested(PricingRequest.AiEod);
			if (result.IsRequested(PricingRequest.Ai) || result.IsRequested(PricingRequest.AiEod))
			{
				if (result.Cashflows == null || result.Cashflows.Length == 0)
				{
					result.Cashflows = trade.GetCashflows(market, false);
				}

				result.Ai = trade.GetAccruedInterest(market.ValuationDate, market, isEod);
			}

			if (result.IsRequested(PricingRequest.KeyRateDv01))
			{
				if (double.IsNaN(result.Pv))
				{
					result.Pv = CalcPv(trade, market);
				}
				var dc = new Dictionary<string, CurveRisk[]>();
				var fc = new Dictionary<string, CurveRisk[]>();

				Parallel.Invoke(
					() => CalcDiscountDv01(trade, market, result.Pv, ref dc),
					() => CalcResetDv01(trade, market, result.Pv, ref fc)
				);


				result.KeyRateDv01 = PricingResultExtension.Aggregate(dc, fc);
			}

			if (result.IsRequested(PricingRequest.FairQuote))
			{
				result.FairQuote = GetFairQuote(trade, market);
			}

			if (result.IsRequested(PricingRequest.MacDuration))
			{
				if (result.Cashflows == null || result.Cashflows.Length == 0)
				{
					result.Cashflows = trade.GetCashflows(market, false);
				}
				var weightedCf = 0.0;
				var totalCf = 0.0;
				foreach (var cashflow in result.Cashflows)
				{
					if (cashflow.PaymentDate > market.ValuationDate)
					{
						var t = market.DiscountCurve.Value.DayCount.CalcDayCountFraction(market.ValuationDate, cashflow.PaymentDate);
						var df = market.DiscountCurve.Value.GetDf(market.ValuationDate, cashflow.PaymentDate);

						weightedCf += cashflow.PaymentAmount * df * t;
						totalCf += cashflow.PaymentAmount * df;
					}
				}
				result.MacDuration = weightedCf / totalCf;
			}

			if (result.IsRequested(PricingRequest.Pv01))
			{
				result.Pv01 = CalcPv01(trade, market, result.Pv);
			}

			Date valueDate = result.ValuationDate;
			if (result.IsRequested(PricingRequest.ProductSpecific))
			{
				var yieldCurve = market.DiscountCurve.Value;

				#region

				var psDict = new Dictionary<string, Dictionary<string, RateRecord>>();
				var dayGap = new DayGap("+0BD");
				var T = (trade is InterestRateSwap) ? (trade as InterestRateSwap).FloatingLeg : trade as SwapLeg;

				//forward rate points
				var tenors = new[] { "1D", "7D", "3M", "1Y" };
				var fwdStartInTenors = new List<string> { "1D", "1W", "2W", "1M", "2M", "3M", "4M", "5M", "6M", "7M", "8M", "9M", "10M", "11M", "1Y" };
				var totalMonths = Convert.ToInt16((yieldCurve.KeyPoints.Last().Item1 - yieldCurve.KeyPoints.First().Item1) / 30.0) + 1;
				for (var i = 15; i <= totalMonths; i += 3)
				{
					fwdStartInTenors.Add(i + "M");
				}
				foreach (var tenor in tenors)
				{
					var fwdRates = new Dictionary<string, RateRecord>();
					var fwdTerm = new Term(tenor);
					foreach (var fwdStartInTenor in fwdStartInTenors)
					{
						var fwdStartDate = dayGap.Get(T.Calendar, new Term(fwdStartInTenor).Next(valueDate));
						var fwdEndDate = dayGap.Get(T.Calendar, fwdTerm.Next(fwdStartDate));
						if (fwdEndDate < yieldCurve.KeyPoints.Last().Item1)
						{
							fwdRates[fwdStartInTenor] = new RateRecord()
							{
								Date = fwdStartDate.ToString(),
								Rate = yieldCurve.GetForwardRate(fwdStartDate, fwdTerm)
							};
						}
					}

					psDict["forwardrates" + tenor] = fwdRates;
				}

				//spot rate
				var spotRates = new Dictionary<string, RateRecord>();
				var spotInTenors = fwdStartInTenors;

				foreach (var spotInTenor in spotInTenors)
				{
					var spotDate = dayGap.Get(T.Calendar, new Term(spotInTenor).Next(valueDate));
					if (spotDate <= yieldCurve.KeyPoints.Last().Item1)
					{
						spotRates[spotInTenor] = new RateRecord
						{
							Date = spotDate.ToString(),
							Rate = yieldCurve.ZeroRate(valueDate, spotDate, Compound.Simple)
						};
					}
				}
				psDict["spotRates"] = spotRates;

				//key rates
				var rates = new Dictionary<string, RateRecord>();
				var ccTenors = yieldCurve.GetKeyTenors().ToArray();
				var mktInstruments = yieldCurve.MarketInstruments;
				if (mktInstruments != null)
				{
					if (mktInstruments.Length != ccTenors.Length)
					{
						throw new PricingBaseException("Number of calibration instruments mismatches number of calibrated points!");
					}
				}
				for (var i = 0; i < ccTenors.Count(); ++i)
				{
					//var spotDate = mktInstruments != null ? mktInstruments[i].Instrument.GetClibrationDate() : dayGap.Get(T.Calendar, new Term(ccTenors[i]).Next(valueDate));
					var spotDate = dayGap.Get(T.Calendar, new Term(ccTenors[i]).Next(valueDate));
					rates[ccTenors[i]] = new RateRecord()
					{
						ContinuousRate = yieldCurve.ZeroRate(valueDate, spotDate),
						Date = spotDate.ToString(),
						DiscountFactor = yieldCurve.GetDf(valueDate, spotDate),
						Rate = mktInstruments == null ? yieldCurve.GetSpotRate(spotDate) : mktInstruments[i].TargetValue,
						ProductType = mktInstruments == null ? "None" : (mktInstruments[i].Instrument is Deposit) ? "Index" : "Swap",
						ZeroRate = yieldCurve.ZeroRate(valueDate, spotDate, Compound.Simple),
						Term = ccTenors[i]
					};
				}
				psDict["rates"] = rates;

				//discount at cash flow dates
				var dfs = new Dictionary<string, RateRecord>();
				var dates = result.Cashflows.Select(x => x.PaymentDate);
				foreach (var date in dates)
				{
					dfs[date.ToString()] = new RateRecord
					{
						DiscountFactor = yieldCurve.GetDf(date)
					};
				}

				psDict["discountfactor"] = dfs;
				//qb rate return
				result.ProductSpecific = psDict;

				#endregion
			}

			return result;
		}

        /// <summary>
        /// 计算Carry
        /// </summary>
        /// <param name="trade">交易</param>
        /// <param name="market">市场数据对象</param>
        /// <returns>计算结果</returns>
        public virtual double CalcCarry(TTrade trade, IMarketCondition market) {
            return 0.0;
        }

        /// <summary>
        /// 计算FairQuote
        /// </summary>
        /// <param name="trade">交易</param>
        /// <param name="market">市场数据对象</param>
        /// <returns>计算结果</returns>
        public virtual double GetFairQuote(TTrade trade, IMarketCondition market)
		{
			return double.NaN;
		}

        /// <summary>
        /// 计算Dv01
        /// </summary>
        /// <param name="trade">交易</param>
        /// <param name="market">市场数据对象</param>
        /// <returns>计算结果</returns>
		public virtual double CalcDv01(TTrade trade, IMarketCondition market)
		{
            //market rally, rates go down
			if (market.FixingCurve.HasValue)
			{
				var mktUp =
					market.UpdateCondition(new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve, market.DiscountCurve.Value.Shift(-1)),
					new UpdateMktConditionPack<IYieldCurve>(x => x.FixingCurve, market.FixingCurve.Value.Shift(-1)));
				var mktDown =
					market.UpdateCondition(new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve, market.DiscountCurve.Value.Shift(1)),
					new UpdateMktConditionPack<IYieldCurve>(x => x.FixingCurve, market.FixingCurve.Value.Shift(1)));
				return (CalcPv(trade, mktUp) - CalcPv(trade, mktDown)) / 2.0;
			}
			else
			{
				var mktUp = market.UpdateCondition(new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve, market.DiscountCurve.Value.Shift(-1)));
				var mktDown = market.UpdateCondition(new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve, market.DiscountCurve.Value.Shift(1)));
				return (CalcPv(trade, mktUp) - CalcPv(trade, mktDown)) / 2.0;
			}
		}

        /// <summary>
        /// 计算DiscountDv01
        /// </summary>
        /// <param name="trade">交易</param>
        /// <param name="market">市场数据对象</param>
        /// <param name="pv">Pv</param>
        /// <param name="keyDv01">结果。曲线关键点Dv01</param>
		private void CalcDiscountDv01(TTrade trade, IMarketCondition market, double pv, ref Dictionary<string, CurveRisk[]> keyDv01)
		{
			var keyTenors = market.DiscountCurve.Value.GetKeyTenors();

			var lptr = new List<CurveRisk>();
			for (var i = 0; i < keyTenors.Length; ++i)
			{
				var mktDown = market.UpdateCondition(new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve, market.DiscountCurve.Value.BumpKeyRate(i, 1)));
				lptr.Add(new CurveRisk(keyTenors[i], CalcPv(trade, mktDown) - pv));
			}

			keyDv01[market.DiscountCurve.Value.Name] = lptr.ToArray();
		}

        /// <summary>
        /// 计算ResetDv01
        /// </summary>
        /// <param name="trade">交易</param>
        /// <param name="market">市场数据对象</param>
        /// <param name="pv">Pv</param>
        /// <param name="keyDv01">结果。曲线关键点Dv01</param>
		private void CalcResetDv01(TTrade trade, IMarketCondition market, double pv, ref Dictionary<string, CurveRisk[]> keyDv01)
		{
			if (market.FixingCurve.HasValue)
			{
				var keyTenors = market.FixingCurve.Value.GetKeyTenors();
				var lcr = new List<CurveRisk>();
				for (var i = 0; i < keyTenors.Length; ++i)
				{
					var mktDown = market.UpdateCondition(new UpdateMktConditionPack<IYieldCurve>(x => x.FixingCurve, market.FixingCurve.Value.BumpKeyRate(i, 1)));
					lcr.Add(new CurveRisk(keyTenors[i], CalcPv(trade, mktDown) - pv));
				}
				keyDv01[market.FixingCurve.Value.Name] = lcr.ToArray();
			}
		}

        /// <summary>
        /// 计算Pv
        /// </summary>
        /// <param name="trade">交易</param>
        /// <param name="market">市场数据对象</param>
        /// <returns>计算结果</returns>
		public virtual double CalcPv(TTrade trade, IMarketCondition market)
		{
			if (market.ValuationDate >= trade.UnderlyingMaturityDate)
			{
				return 0.0;
			}
			var cfs = trade.GetCashflows(market, true);
			return cfs.Where(cf => cf.PaymentDate > market.ValuationDate)
				.Sum(cf => cf.PaymentAmount * market.DiscountCurve.Value.GetDf(market.ValuationDate, cf.PaymentDate));
		}

        /// <summary>
        /// 计算Pv01
        /// </summary>
        /// <param name="trade">交易</param>
        /// <param name="market">市场数据对象</param>
        /// <param name="pv">Pv</param>
        /// <returns>计算结果</returns>
		public virtual double CalcPv01(TTrade trade, IMarketCondition market, double pv = double.NaN)
		{
			return this.CalcDv01(trade, market);
		}

	}
}
