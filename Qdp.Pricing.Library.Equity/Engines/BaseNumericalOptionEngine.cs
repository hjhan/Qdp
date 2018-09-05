using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Equity.Engines.Analytical;
using Qdp.Pricing.Library.Equity.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Qdp.Pricing.Library.Equity.Engines
/// </summary>
namespace Qdp.Pricing.Library.Equity.Engines
{
    /// <summary>
    /// 期权数值计算引擎基类
    /// </summary>
    public abstract class BaseNumericalOptionEngine : Engine<IOption>
	{
        /// <summary>
        /// 标的资产现价偏移量
        /// </summary>
		public double SpotPriceBump { get; private set; }
        int PayoffPreciseToDecimalPlace = 2;

		protected BaseNumericalOptionEngine(double spotPriceBump = 0.01)
		{
			SpotPriceBump = spotPriceBump;
		}

        
        /// <summary>
        /// 计算期权
        /// </summary>
        /// <param name="option">期权</param>
        /// <param name="market">市场</param>
        /// <param name="request">计算请求</param>
        /// <returns>计算结果</returns>
        public override IPricingResult Calculate(IOption option, IMarketCondition market, PricingRequest request)
		{

			var result = new PricingResult(market.ValuationDate, request);
            bool isExpired = option.ExerciseDates.Last() < market.ValuationDate;
            bool isExpiredforTheta= option.ExerciseDates.Last() <= market.ValuationDate;
            bool onMaturityDate = (option.ExerciseDates.Last() == market.ValuationDate);

            if (isExpired)
            {
                result.Pv = result.Delta = result.DeltaCash = result.Gamma = result.GammaCash =  result.Theta = 0.0;
                result.ThetaPnL = result.CalenderThetaPnL = result.Vega = result.Rho = result.StoppingTime = 0.0;
                result.DVegaDvol = result.DVegaDt = result.DDeltaDvol = result.DDeltaDt = 0.0;
                result.PricingVol = 0.0;
                return result;
            }
            
            else
            {
                var now = DateTime.Now;
                var expiryDayCutoffTime = new DateTime(now.Year, now.Month, now.Day, 15, 0, 0);
                var expiryDayStartTime = new DateTime(now.Year, now.Month, now.Day, 9, 30, 0);
                var T = (expiryDayCutoffTime - now).TotalSeconds / (expiryDayCutoffTime - expiryDayStartTime).TotalSeconds / 365.0;

                SetPricingVol(result, option, market);

                //Calc delta/pv differently on expiry day
                if (onMaturityDate && T > 0.0)
                {
                    if (result.IsRequested(PricingRequest.Pv) && double.IsNaN(result.Pv))
                        result.Pv = DoCalcExpiryPv(option, market, T);

                    if (result.IsRequested(PricingRequest.Delta) && double.IsNaN(result.Delta)) {
                        result.Delta = DoCalcExpiryDelta(option, market, T);
                        result.DeltaCash = result.Delta * market.SpotPrices.Value.Values.First();
                    }
                        
                    
                }
                else if (onMaturityDate && T <= 0.0)
                {
                    if (result.IsRequested(PricingRequest.Pv) && double.IsNaN(result.Pv))
                    {
                        var pv = CalcPvs(option, market.ToArrayT());
                        result.Pv = pv[0];
                        result.PricingVol = 0.0;
                    }

                    if (result.IsRequested(PricingRequest.Delta) && double.IsNaN(result.Delta)) {
                        var pv = CalcPvs(option, new[] { market })[0];
                        if (option.OptionType == OptionType.Call)
                        {
                            result.Delta = (pv > 0) ? 1 * option.Notional : 0;
                            result.DeltaCash = result.Delta * market.SpotPrices.Value.Values.First();
                        }
                        else
                        {
                            result.Delta = (pv > 0) ? -1 * option.Notional: 0;
                            result.DeltaCash = result.Delta * market.SpotPrices.Value.Values.First();
                        }
                    }
                    
                }

                //on other days

                if (result.IsRequested(PricingRequest.Pv) && double.IsNaN(result.Pv))
                {
                    var pv = CalcPvs(option, market.ToArrayT());
                    result.Pv = pv[0];
                }
                
                if (result.IsRequested(PricingRequest.Delta) && double.IsNaN(result.Delta))
                {
                    result.Delta = CalcDelta(option, market);
                    result.DeltaCash = result.Delta * market.SpotPrices.Value.Values.First();
                }

                if (result.IsRequested(PricingRequest.Gamma) && double.IsNaN(result.Gamma) )
                {
                    result.Gamma = CalcGamma(option, market);
                    result.GammaCash = result.Gamma * market.SpotPrices.Value.Values.First() * market.SpotPrices.Value.Values.First() / 100;
                }              

                if (result.IsRequested(PricingRequest.Vega) && double.IsNaN(result.Vega))
                {
                    result.Vega = CalcVega(option, market);
                    result.FwdDiffVega = CalcFwdDiffVega(option, market);
                }

                if (result.IsRequested(PricingRequest.Rho) && double.IsNaN(result.Rho))
                {
                    var markets = new[]
                    {
                    market,
                    market.UpdateCondition(new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve, market.DiscountCurve.Value.Shift(10))),
                };
                    var pvs = CalcPvs(option, markets);
                    result.Rho = (pvs[1] - pvs[0]) / 10.0;
                }

                if (result.IsRequested(PricingRequest.DVegaDvol) || result.IsRequested(PricingRequest.DVegaDt) ||
                    result.IsRequested(PricingRequest.DDeltaDvol) || result.IsRequested(PricingRequest.DDeltaDt))
                {
                    CalcHighOrder(option, market, result,isExpiredforTheta);
                }

                if (result.IsRequested(PricingRequest.StoppingTime) && double.IsNaN(result.StoppingTime))
                {
                    var stoppingTime = StoppingTime(option, market.ToArrayT());
                    result.StoppingTime = stoppingTime;
                }

                if (result.IsRequested(PricingRequest.Theta))
                {
                    if (isExpiredforTheta)
                    {
                        result.ThetaPnL = result.CalenderThetaPnL = result.Theta = 0.0;
                        return result;
                    }
                    else
                    {
                        var thetaResults = CalcTheta(option, market);
                        result.Theta = thetaResults[0]; // this theta is for time pnl
                        result.ThetaPnL = thetaResults[1];
                        result.CalenderThetaPnL = thetaResults[2];  //in trading day mode, we cannot calc calendar theta
                    }
                   
                }
            }

            if (option.IsMoneynessOption && result.IsRequested(PricingRequest.Pv) && (option.InitialSpotPrice!= 0.0) ) {
                result.PctPv = result.Pv / option.InitialSpotPrice;
            }
            
            return result;
		}

        //Precise TimeValue Optiong Pricing Mode:
        // if nightMarket,
        // T0,  <12:00, T = original T + 2/3
        // T0,  >12:00  && <15:00,  T = original T + 1/3
        // T0,  >=15:00 && <21:00,  T = original T
        // T0,  >=21:00,  T = original T (nightMarket)
        // if noNightMarket
        // T0,  <12:00,  T = orignal T + 1
        // T0,  >12:00 && <15:00,  T = original T +  1/2
        // T0,  >= 15:00,  T = original T

        /// <summary>
        /// 计算期权现值
        /// </summary>
        /// <param name="option">期权</param>
        /// <param name="markets">市场</param>
        /// <returns>计算结果</returns>
        public virtual double[] CalcPvs(IOption option, IMarketCondition[] markets) {
            var timeIncrement = AnalyticalOptionPricerUtil.optionTimeToMaturityIncrement(option);

            return markets.Select(x => {
                if (x.ValuationDate == option.ExerciseDates.Last())
                    return CalcIntrinsicValue(option, x);
                else
                    return CalcPv(option, x, timeIncrement);
            }).ToArray();
        }
        protected virtual double CalcIntrinsicValue(IOption option, IMarketCondition market) {
            double strike = option.IsMoneynessOption ? option.Strike * option.InitialSpotPrice : option.Strike;
            if (option.OptionType == OptionType.Call)
                return Math.Round(Math.Max(market.SpotPrices.Value.Values.First() - strike, 0), PayoffPreciseToDecimalPlace) * option.Notional;
            else
                return Math.Round(Math.Max(strike - market.SpotPrices.Value.Values.First(), 0), PayoffPreciseToDecimalPlace) * option.Notional;
        }

        protected abstract double CalcPv(IOption option, IMarketCondition market, double timeIncrement = 0.0);

        protected virtual void SetPricingVol(PricingResult result, IOption option, IMarketCondition market) {
            var vol= AnalyticalOptionPricerUtil.pricingVol(volSurf: market.VolSurfaces.Value.Values.First(),
                exerciseDate: option.ExerciseDates.Last(), option: option, spot: market.SpotPrices.Value.Values.First());
            result.PricingVol = vol;
        }

        private double DoCalcExpiryDelta(IOption option, IMarketCondition market, double T) {
            var markets = new[]
                 {
                    market,
                    market.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, double>>(x => x.SpotPrices, new Dictionary<string, double> { { "", market.SpotPrices.Value.Values.First() + SpotPriceBump } })),
                };
            return CalcExpiryDelta(option, markets, T);
        }

        private double DoCalcExpiryPv(IOption option, IMarketCondition market, double T)
        {
            return CalcExpiryPV(option, market, T);
        }

        protected virtual double CalcExpiryPV(IOption option, IMarketCondition market, double T)
        {
            return CalcPv(option, market);
        }

        protected virtual double CalcExpiryDelta(IOption option, IMarketCondition[] markets, double T) {
            return CalcDelta(option, markets[0]);
        }

        /// <summary>
        /// 计算Delta
        /// </summary>
        /// <param name="option">期权</param>
        /// <param name="market">市场</param>
        /// <returns>计算结果</returns>
        public virtual double CalcDelta(IOption option, IMarketCondition market)
        {
            var markets = new[]
                 {
                    market,
                    market.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, double>>(x => x.SpotPrices, new Dictionary<string, double> { { "", market.SpotPrices.Value.Values.First() + SpotPriceBump } })),
                };
            var pvs = CalcPvs(option, markets);
            return (pvs[1] - pvs[0]) / SpotPriceBump;
        }

        /// <summary>
        /// 计算Gamma
        /// </summary>
        /// <param name="option">期权</param>
        /// <param name="market">市场</param>
        /// <returns>计算结果</returns>
        public virtual double CalcGamma(IOption option, IMarketCondition market)
        {
            var priceUp = new Dictionary<string, double> { { "", market.SpotPrices.Value.Values.First() + SpotPriceBump } };
            var priceDn = new Dictionary<string, double> { { "", market.SpotPrices.Value.Values.First() - SpotPriceBump } };
            var markets = new[]
            {
                    market,
                    market.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, double>>(x => x.SpotPrices, priceUp)),
                    market.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, double>>(x => x.SpotPrices, priceDn)),
            };
            var pvs = CalcPvs(option, markets);
            return (pvs[1] + pvs[2] - 2 * pvs[0]) / (SpotPriceBump * SpotPriceBump);

        }

        /// <summary>
        /// 计算Theta
        /// </summary>
        /// <param name="option">期权</param>
        /// <param name="market">市场</param>
        /// <returns>计算结果</returns>
        public virtual double[] CalcTheta(IOption option, IMarketCondition market)
        {
            var markets = new[]
               {
                    market,
                    market.UpdateCondition(new UpdateMktConditionPack<Date>(x => x.ValuationDate, option.Calendar.NextBizDay(market.ValuationDate))),
                    market.UpdateCondition(new UpdateMktConditionPack<Date>(x => x.ValuationDate, market.ValuationDate.Value.AddDays(1))),
                };
            var pvs = CalcPvs(option, markets);

            var numberOfDays = markets[1].ValuationDate.Value - markets[0].ValuationDate.Value;

            double[] theta =
            { (pvs[1] - pvs[0]) / numberOfDays,
               pvs[1] - pvs[0],  //trading 1day theta pnl
               pvs[2] - pvs[0]   //calendar 1day theta pnl
            };
            return theta;
        }

        /// <summary>
        /// 计算Vega，向上向下分别偏移波动率计算Vega
        /// </summary>
        /// <param name="option">期权</param>
        /// <param name="market">市场</param>
        /// <returns>计算结果</returns>
        //by default, center difference
        public virtual double CalcVega(IOption option, IMarketCondition market)
        {
            var bumpedUpVol = market.VolSurfaces.Value.Values.First().BumpVolSurf(0.01);
            var bumpedDownVol = market.VolSurfaces.Value.Values.First().BumpVolSurf(-0.01);

            var markets = new[]
            {
                    market.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, IVolSurface>>(x => x.VolSurfaces, new Dictionary<string, IVolSurface>{ { "", bumpedUpVol } } )),
                    market.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, IVolSurface>>(x => x.VolSurfaces, new Dictionary<string, IVolSurface>{ { "", bumpedDownVol } } )),
                };
            var pvs = CalcPvs(option, markets);
            return  (pvs[0] - pvs[1])/ 2.0;
        }

        /// <summary>
        /// 计算Vega，只向上偏移波动率计算
        /// </summary>
        /// <param name="option">期权</param>
        /// <param name="market">市场</param>
        /// <returns>计算结果</returns>
        //forward difference vega
        public virtual double CalcFwdDiffVega(IOption option, IMarketCondition market)
        {
            var bumpedVol = market.VolSurfaces.Value.Values.First().BumpVolSurf(0.01);

            var markets = new[]
            {
                    market,
                    market.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, IVolSurface>>(x => x.VolSurfaces, new Dictionary<string, IVolSurface>{ { "", bumpedVol } } )),
                };
            var pvs = CalcPvs(option, markets);
            return pvs[1] - pvs[0];
        }

        protected virtual void CalcHighOrder(IOption option, IMarketCondition market, PricingResult result, bool isExpiredforTheta)
        {
            var bumpedVol = market.VolSurfaces.Value.Values.First().BumpVolSurf(0.01);
            var marketTheta = market.UpdateCondition(new UpdateMktConditionPack<Date>(x => x.ValuationDate, market.ValuationDate.Value.AddDays(1)));
            var marketVega = market.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, IVolSurface>>(x => x.VolSurfaces, new Dictionary<string, IVolSurface> { { "", bumpedVol } }));

            result.DDeltaDvol = CalcDelta(option, marketVega) - CalcDelta(option, market);
            result.DVegaDvol = CalcVega(option, marketVega) - CalcVega(option, market);

            if (isExpiredforTheta)
            {
                result.DVegaDt = result.DDeltaDt = 0.0;
            }
            else
            {
                result.DDeltaDt = CalcDelta(option, marketTheta) - CalcDelta(option, market);
                result.DVegaDt = CalcVega(option, marketTheta) - CalcVega(option, market);
            }
                    
        }

        protected virtual double StoppingTime(IOption option, IMarketCondition[] markets) {
            return option.DayCount.CalcDayCountFraction(markets[0].ValuationDate, option.UnderlyingMaturityDate);
        } 
    }
}
