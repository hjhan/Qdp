using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.Maths;
using Qdp.Pricing.Library.Equity.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Qdp.Pricing.Library.Equity.Engines.Analytical
{
    static class AnalyticalOptionPricerUtil
    {
        public static bool isBasicPricing(PricingResult result) {
            if (result.IsRequested(PricingRequest.Delta) ||
                result.IsRequested(PricingRequest.Gamma) ||
                result.IsRequested(PricingRequest.Vega) ||
                result.IsRequested(PricingRequest.Theta) ||
                result.IsRequested(PricingRequest.Rho))
                return true;
            else
                return false;
        }

        public static bool isHighOrderPricing(PricingResult result) {
            if (result.IsRequested(PricingRequest.DDeltaDt) ||
                result.IsRequested(PricingRequest.DVegaDt) ||
                result.IsRequested(PricingRequest.DDeltaDvol) ||
                result.IsRequested(PricingRequest.DVegaDvol))
                return true;
            else
                return false;
        }

        public static bool isFuturesOption(InstrumentType underlyingType) {
            if (underlyingType == InstrumentType.Futures ||
                underlyingType == InstrumentType.CommodityFutures ||
                underlyingType == InstrumentType.BondFutures)
                return true;
            else
                return false;
        }

        public static bool isForwardOption(InstrumentType underlyingType) {
            if (underlyingType == InstrumentType.Forward ||
                underlyingType == InstrumentType.CommodityForward ||
                underlyingType == InstrumentType.BondForward ||
                underlyingType == InstrumentType.FxForward)
                return true;
            else
                return false;
        }

        public static bool isForwardFuturesOption(InstrumentType underlyingType) {
            return isFuturesOption(underlyingType) || isForwardOption(underlyingType);
        }

        //Note:  dividend yield curve from mkt is useless
        //construct proper dividend yiled from discrete stock cash dividend schedule
        public static double dividendYieldOutput(double dividendRateInput, double riskFreeRate,
            Dictionary<Date, double> dividends, double spot, Date startDate, Date endDate,IDayCount dayCount)
        {
            if (dividends == null)
                return dividendRateInput;
            else
            {
                Tuple<Date, double>[] cashDividends = dividends.Select(x => new Tuple<Date, double>(x.Key, x.Value)).Where(x => x.Item1 >= startDate & x.Item1 <= endDate).ToArray();
                Array.Sort(cashDividends, (o1, o2) => o1.Item1.CompareTo(o2.Item1));

                //if cash dividends, then annualize it to equivalent dividend yield
                var DividendsAtMaturity = cashDividends.Select(x => Math.Exp(riskFreeRate * dayCount.CalcDayCountFraction(x.Item1, endDate)) * x.Item2).Sum();
                var T = dayCount.CalcDayCountFraction(startDate, endDate);
                var adjustedForwardPrice = Math.Exp(riskFreeRate * T) * spot - DividendsAtMaturity;
                var eqvDividendYield = -(Math.Log(adjustedForwardPrice / spot) / T - riskFreeRate);
                return eqvDividendYield;

            }         
        }

        public static double optionTimeToMaturityIncrement(IOption option) {
            var timeIncrement = 0.0;
            if (option.CommodityFuturesPreciseTimeMode)
            {
                var now = DateTime.Now;

                //China trading days = 244/year,  fixed number as per requested by GuoTaiJunAn
                var tradingDays = 244.0;
                //option.Calendar.NumberBizDaysBetweenDate(new Date(now.Year,now.Month,now.Day), new Date(now.Year+1, now.Month, now.Day));
                if (option.HasNightMarket)
                {
                    if (now.Hour >= 21)
                        timeIncrement = 0.0;
                    else if (now.Hour >= 0 && now.Hour < 12)
                        timeIncrement = 2.0 / 3.0;
                    else if (now.Hour >= 12 && now.Hour < 15)
                        timeIncrement = 1.0 / 3.0;
                }
                else
                {
                    if (now.Hour >= 0 && now.Hour < 12)
                        timeIncrement = 1.0;
                    else if (now.Hour >= 12 && now.Hour < 15)
                        timeIncrement = 0.5;
                }
                timeIncrement = timeIncrement / tradingDays;
            }
            return timeIncrement;
        }

        public static double pricingVol(IVolSurface volSurf, Date exerciseDate, IOption option, double spot) {
            return option.IsMoneynessOption ?
                //moneyness option , strike i.e. 120% of initialSpot
                volSurf.GetValue(exerciseDate, option.Strike * option.InitialSpotPrice, spot):
                volSurf.GetValue(exerciseDate, option.Strike, spot);
        }

        public static double dividenRate(InstrumentType underlyingType, double dividendRateInput, double riskFreeRate) {
            if (isFuturesOption(underlyingType) || isForwardOption(underlyingType))
                return riskFreeRate;
            else
                return dividendRateInput;
        }

        public static double costOfCarry(bool isFwdFuturesOption, double dividendRate, double riskFreeRate)
        {
            if (isFwdFuturesOption)
                return 0;
            else
                return riskFreeRate - dividendRate;
        }

        public static double dividendDf(InstrumentType underlyingType, double dividendDfInput, double riskFreeDf)
        {
            if(isForwardFuturesOption(underlyingType))
                return riskFreeDf;
            else
                return dividendDfInput;
        }

        public static double forwardPrice(bool isFwdFuturesOption, double spotPrice, 
            double riskfreeDfAtExercise,
            double dividendRate, 
            Date exerciseDate, Date valuationDate, IDayCount dayCount)
        {
            if (isFwdFuturesOption)
            {
                return spotPrice;
            }
            else {
                return spotPrice *
                    Compound.Continuous.CalcDfFromZeroRate(dividendRate, dayCount.CalcDayCountFraction(valuationDate, exerciseDate))
                    / riskfreeDfAtExercise;
            }
        }

        public static double CalcDfFromZeroRate(double zeroRate, Date date, Date valuationDate, IDayCount dayCount)
        {
            return Compound.Continuous.CalcDfFromZeroRate(zeroRate, dayCount.CalcDayCountFraction(valuationDate, date));
        }


        public static void prepareBlackScholesInputs(
            double spotPrice,
            double riskfreeRateAtExerciseInput, double riskfreeRateAtMaturityInput, double dividendRateInput,
            double riskFreeCurveShiftInBp, double dividendCurveShiftInBp,
            Date maturityDate, Date exerciseDate, Date valuationDate,
            IOption trade,
            IDayCount curveDayCount,
            bool isOptionOnForward,
            bool isForwardFuturesOption,
            double strike,
            double sigma,
            out double nd1,  // P(x < d1)
            out double nd2,   // P(x < d2),
            out double riskfreeDfAtExercise,
            out double dfExerciseToMaturity,
            out double forwardPrice,
            double expiryDayRemainingLife = double.NaN,
            double timeIncrement = 0.0
            ) {

            double T = 0.0;
            if (!double.IsNaN(expiryDayRemainingLife))
                T = expiryDayRemainingLife;
            else
                T = timeToMaturityFraction(valuationDate, exerciseDate, trade) + timeIncrement;
            double riskfreeRateAtExercise, riskfreeRateAtMaturity;
            if (riskFreeCurveShiftInBp != 0.0)
            {
                riskfreeRateAtExercise = riskfreeRateAtExerciseInput + riskFreeCurveShiftInBp * 1e-4;
                riskfreeRateAtMaturity = riskfreeRateAtMaturityInput + riskFreeCurveShiftInBp * 1e-4;
            }
            else
            {
                riskfreeRateAtExercise = riskfreeRateAtExerciseInput;
                riskfreeRateAtMaturity = riskfreeRateAtMaturityInput;
            }
            var riskfreeDfAtMaturity = CalcDfFromZeroRate(riskfreeRateAtMaturity, maturityDate, valuationDate, curveDayCount);
            riskfreeDfAtExercise = CalcDfFromZeroRate(riskfreeRateAtExercise, exerciseDate, valuationDate, curveDayCount);

            double dividendRate;
            if (dividendCurveShiftInBp != 0.0)
                dividendRate = dividendRateInput + dividendCurveShiftInBp / 1e4;
            else
                dividendRate = dividendRateInput;

            //https://en.wikipedia.org/wiki/Black_model
            //if option on forward, discount to forward maturity day, and make sure maturity here is forward maturity, exercise is option exercise day
            //for other contracts,  pass maturity day = expiry day,  therefore _dfExerciseToMaturity = 1.0;
            dfExerciseToMaturity = (isOptionOnForward) ? riskfreeDfAtMaturity / riskfreeDfAtExercise : 1.0;

            var b = AnalyticalOptionPricerUtil.costOfCarry(isForwardFuturesOption, dividendRate, riskfreeRateAtExercise);

            forwardPrice = AnalyticalOptionPricerUtil.forwardPrice(
                isForwardFuturesOption,
                spotPrice,
                riskfreeDfAtExercise: riskfreeDfAtExercise,
                dividendRate: dividendRate,
                exerciseDate: exerciseDate,
                valuationDate: valuationDate,
                dayCount: trade.DayCount);

            var d1 = (Math.Log(spotPrice / strike) + (b + sigma * sigma / 2.0) * T) / (sigma * Math.Sqrt(T));
            var d2 = d1 - sigma * Math.Sqrt(T);
            nd1 = NormalCdf.NormalCdfHart(d1);  // P(x < d1)
            nd2 = NormalCdf.NormalCdfHart(d2);  // P(x < d2)

            //var nPrimceD1 = 1.0 / Math.Sqrt(2.0 * Math.PI) * Math.Exp(-d1 * d1 / 2.0); // derivative of N(d1)
            //var nPrimceD2 = 1.0 / Math.Sqrt(2.0 * Math.PI) * Math.Exp(-d2 * d2 / 2.0); // derivative of N(d2)
        }

        public static double timeToMaturityFraction(Date startDate, Date endDate, IOption option)
        {
            return option.DayCount.CalcDayCountFraction(startDate, endDate);
        }

    }
}
