using System;
using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.Pricing.Library.Common.MathMethods.Maths;
using Qdp.Pricing.Library.Equity.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Foundation.Implementations;

namespace Qdp.Pricing.Library.Equity.Engines.Analytical
{

    public class AnalyticalAsianOptionEngine : BaseNumericalOptionEngine
    {
        private AsianOptionCalculator _calculator = null;
        private AsianOptionCalculator ConfigureCalculator(IOption option, IMarketCondition market, 
            double expiryDayRemainingLife = double.NaN, double timeIncrement = 0.0)
        {
            var trade = (AsianOption)option;

            var exerciseDate = trade.ExerciseDates.Last();
            var remainingObsDates = trade.ObservationDates.Where(x => x >= market.ValuationDate).ToArray();
            var numOfObsDates = trade.ObservationDates.Count();
            var numOfObservedDates = numOfObsDates - remainingObsDates.Count();
            var observedAverage = trade.Fixings.Any() ? trade.Fixings.Average(x => x.Value) : market.SpotPrices.Value.Values.First();
            //if (trade.Fixings.Count != numOfObservedDates)
            //{
            //    throw new PricingLibraryException("AsianOption: number of fixings does not match!");
            //}

            var spot = market.SpotPrices.Value.Values.First();
            double sigma = AnalyticalOptionPricerUtil.pricingVol(volSurf: market.VolSurfaces.Value.Values.First(),
                exerciseDate: exerciseDate, option: option, spot: spot);

            double t;
            if (!double.IsNaN(expiryDayRemainingLife))
                t = expiryDayRemainingLife;
            else
                t = AnalyticalOptionPricerUtil.timeToMaturityFraction(market.ValuationDate, exerciseDate, trade) + timeIncrement;

            var t2 = AnalyticalOptionPricerUtil.timeToMaturityFraction(trade.ObservationDates[0], remainingObsDates.Last(), trade);
            var t1 = AnalyticalOptionPricerUtil.timeToMaturityFraction(market.ValuationDate, remainingObsDates[0], trade) + timeIncrement;

            var riskFreeRate = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, exerciseDate);
            var dividendCurveInput = market.DividendCurves.Value.Values.First().ZeroRate(market.ValuationDate, exerciseDate);
            var dividendInput = AnalyticalOptionPricerUtil.dividendYieldOutput(dividendCurveInput, riskFreeRate,
               option.Dividends, spot, market.ValuationDate, trade.ExerciseDates.Last(), option.DayCount);
            var dividendRate = AnalyticalOptionPricerUtil.dividenRate(trade.UnderlyingProductType, dividendInput, riskFreeRate);

            var calculator = new AsianOptionCalculator(trade.OptionType, trade.AsianType, trade.UnderlyingProductType,
                trade.StrikeStyle,
                strike: option.IsMoneynessOption ? trade.Strike * trade.InitialSpotPrice : trade.Strike,
                spotPrice: spot,
                realizedAveragePrice: observedAverage,
                exerciseInYears: t,
                originalAveragePeriod: t2,
                timetoNextAveragePoint: t1,
                sigma: sigma,
                riskFreeRate: riskFreeRate,
                dividendRate: dividendRate,
                notional: trade.Notional,
                numOfObsDates: numOfObsDates,
                numOfObservedDates: numOfObservedDates);
            this._calculator = calculator;
            return calculator;
        }

        protected override double CalcIntrinsicValue(IOption option, IMarketCondition market)
        {
            var asianOption = option as AsianOption;
            var callPayOff = (asianOption.StrikeStyle == StrikeStyle.Fixed) ?
                    (asianOption.Fixings.Values.Average() - option.Strike) :
                    (market.SpotPrices.Value.Values.First() - asianOption.Fixings.Values.Average());

            if (option.OptionType == OptionType.Call)
                return callPayOff * option.Notional;
            else
                return -1.0 * callPayOff * option.Notional;
        }

        protected override double CalcExpiryDelta(IOption option, IMarketCondition[] markets, double T)
        {
            var trade = (AsianOption)option;

            var pvBase = ConfigureCalculator(option, markets[0], expiryDayRemainingLife: T).Pv;
            var pvUp = ConfigureCalculator(option, markets[1], expiryDayRemainingLife: T).Pv;

            return (pvUp - pvBase) / SpotPriceBump;
        }

        protected override double CalcPv(IOption option, IMarketCondition market, double timeIncrement = 0.0)
        {
            double pv = 0.0;
            if (!(option is AsianOption))
            {
                throw new PricingBaseException("");
            }
            var trade = (AsianOption)option;
            var Calculator = ConfigureCalculator(option, market, timeIncrement: timeIncrement);
            var remainingObsDates = trade.ObservationDates.Where(x => x >= market.ValuationDate).ToArray();
            var observedAverage = trade.Fixings.Any() ? trade.Fixings.Average(x => x.Value) : market.SpotPrices.Value.Values.First();
            if (!remainingObsDates.Any())
            {
                //already fix on all observation days
                var cfs = trade.GetPayoff(new[] { observedAverage });
                pv = cfs.Sum(x => x.PaymentAmount* market.DiscountCurve.Value.GetDf(market.ValuationDate, x.PaymentDate));
            }
            pv = Calculator.Pv;
            return pv;
        }

        //protected override void CalcHighOrder(IOption option, IMarketCondition market, PricingResult result)
        //{
        //    if (this._calculator == null)
        //    {
        //        ConfigureCalculator(option, market);
        //    }
        //    var calc = (this._calculator == null) ?
        //        ConfigureCalculator(option, market) : this._calculator;

        //    result.DDeltaDt = calc.DDeltaDt;
        //    result.DDeltaDvol = calc.DDeltaDvol;
        //    result.DVegaDvol = calc.DVegaDvol;
        //    result.DVegaDt = calc.DVegaDt;

        //}

        public override double[] CalcTheta(IOption option, IMarketCondition market)
        {
            var markets = new[]
               {
                    market,
                    market.UpdateCondition(new UpdateMktConditionPack<Date>(x => x.ValuationDate, option.Calendar.PrevBizDay(market.ValuationDate))),
                    market.UpdateCondition(new UpdateMktConditionPack<Date>(x => x.ValuationDate, market.ValuationDate.Value.AddDays(-1))),
                };
            var pvs = CalcPvs(option, markets);
            double[] theta = 
                {(pvs[1] - pvs[0]) / (markets[1].ValuationDate.Value - markets[0].ValuationDate.Value),
                pvs[0] - pvs[1],
                pvs[0] - pvs[2]
            };
            return theta;
        }
    }


    internal class AsianOptionCalculator
    {
        private OptionType _optionType;
        private AsianType _asianType;
        private StrikeStyle _strikeStyle;
        private double _S, _SA, _T, _T2, _t1, _X, _sigma, _r, _dividendRate, _b, _notional, _numOfObsDates, _numOfObservedDates;

        private InstrumentType[] FuturesProducts = { InstrumentType.Futures, InstrumentType.BondFutures, InstrumentType.CommodityFutures };
        public AsianOptionCalculator(
            OptionType optionType, AsianType asianType, InstrumentType underlyingProductType, StrikeStyle strikeStyle,
            double strike, double spotPrice, double realizedAveragePrice,
            double exerciseInYears, double originalAveragePeriod, double timetoNextAveragePoint,
            double sigma, double riskFreeRate, double dividendRate, double notional, double numOfObsDates, double numOfObservedDates)
        {
            _optionType = optionType;
            _asianType = asianType;
            _strikeStyle = strikeStyle;
            _X = strike;
            _S = spotPrice;
            _SA = realizedAveragePrice;
            _T = exerciseInYears;
            _T2 = originalAveragePeriod;
            _t1 = timetoNextAveragePoint;
            _sigma = sigma;
            _r = riskFreeRate;
            _dividendRate = dividendRate;

            if (FuturesProducts.Contains(underlyingProductType))
                _b = 0.0;
            else
                _b = riskFreeRate - dividendRate;

            _notional = notional;
            _numOfObsDates = numOfObsDates;
            _numOfObservedDates = numOfObservedDates;

        }

        private double BS(OptionType optionType, double S, double X, double T, double r, double b, double sigma)
        {
            double d1 = (Math.Log(S / X) + (b + 0.5 * Math.Pow(sigma, 2)) * T) / (sigma * Math.Sqrt(T));
            double d2 = d1 - sigma * Math.Sqrt(T);

            var pv = (optionType == OptionType.Call) ?
                Math.Exp(-r * T) * (S * Math.Exp(b * T) * NormalCdf.NormalCdfHart(d1) - X * NormalCdf.NormalCdfHart(d2)) :
                -1.0 * Math.Exp(-r * T) * (S * Math.Exp(b * T) * NormalCdf.NormalCdfHart(-d1) - X * NormalCdf.NormalCdfHart(-d2));
            return pv;
        }

        //References: Kemna and Vorst(1990); Haug Chapter 4.20.1
        private double GeometricAverage(OptionType optionType, double S, double X, double T, double r, double b, double sigma)
        {
            double bNew = 0.5 * (b - sigma * sigma / 6.0);
            double sigmaNew = sigma / Math.Sqrt(3);

            double GA = BS(optionType, S, X, T, r, bNew, sigmaNew);
            return GA;
        }

        //References: Turnbull and Wakeman(1991); Haug Chapter 4.20.2
        private double ArithmeticAverage(OptionType optionType, double S, double SA, double X, double T, double T2, double r,
            double b, double sigma)
        //SA: realized average so far;
        //T2: original time in average period in years;
        {
            double t1 = Math.Max(0, T - T2);
            //t1: time to start of average period in years;
            double tau = T2 - T;

            double M1 = (b == 0) ? 1 : (Math.Exp(b * T) - Math.Exp(b * t1)) / (b * (T - t1));
            double M2 = (b == 0) ?
                2 * Math.Exp(sigma * sigma * T)/(Math.Pow(sigma,4) * Math.Pow(T- t1,2) ) -
                2 * Math.Exp(sigma * sigma * t1) * (1 + sigma * sigma * (T - t1))/ (Math.Pow(sigma, 4) * (T - t1) * (T - t1)) :

                2 * Math.Exp((2 * b + sigma * sigma) * T) / (b + sigma * sigma) / (2 * b + sigma * sigma) / Math.Pow((T - t1), 2) +
                2 * Math.Exp((2 * b + sigma * sigma) * t1) / b / Math.Pow((T - t1), 2) * (1 / (2 * b + sigma * sigma) - Math.Exp(b * (T - t1)) / (b + sigma * sigma));
            double bNew = Math.Log(M1) / T;
            double sigmaNew = Math.Sqrt(Math.Log(M2) / T - 2 * bNew);

            //exercise decision
            if (tau > 0)
            {
                if (T2 / T * X - tau / T * SA < 0)
                {
                    if (optionType == OptionType.Call)
                    {
                        //expected average at maturity
                        SA = SA * (T2 - T) / T2 + S * M1 * T / T2;
                        return Math.Max(0, SA - X) * Math.Exp(-r * T);
                    }
                    else
                    {
                        return 0;
                    }
                }
            }

            if (tau > 0)
            {
                X = T2 / T * X - tau / T * SA;
                return BS(optionType, S, X, T, r, bNew, sigmaNew) * T / T2;
            }
            else
            {
                return BS(optionType, S, X, T, r, bNew, sigmaNew);
            }
        }


        //References: Levy(1997), Haug,Haug,and Margrabe(2003); Haug Chapter 4.20.3
        private double DiscreteArithmeticAverage(OptionType optionType, double S, double SA, double X, double t1, double T, double n, double m, double r, double b, double sigma)
        //t1: time to the next average point;
        //n: total number of averaging points;
        //m: observed number of averaging points;
        {
            double h = (T - t1) / (n - 1);  //TODO:  n-1 or n -m-1?
            double sigma2 = sigma * sigma;
            double o = 2 * b + sigma2;
            double EA = (b == 0) ? S : S / n * Math.Exp(b * t1) * (1 - Math.Exp(b * h * n)) / (1 - Math.Exp(b * h));
            //into average period
            if (m > 0)
            {
                if (SA > n / m * X)
                {
                    if (optionType == OptionType.Call)
                    {
                        SA = SA * m / n + EA * (n - m) / n;
                        return (SA - X) * Math.Exp(-r * T);
                    }
                    else
                    {
                        return 0;
                    }
                }
            }

            //when only one fixing left to maturity
            if (m == n - 1)
            {
                X = n * X - (n - 1) * SA;
                return BS(optionType, S, X, T, r, b, sigma) * 1 / n;
            }

            double EA2 = (b == 0) ? S * S * Math.Exp(sigma2 * t1) / n / n *
                             (
                                 (1 - Math.Exp(sigma2 * h * n)) / (1 - Math.Exp(sigma2 * h)) +
                                 2.0 / (1 - Math.Exp(sigma2 * h)) *
                                 (n - (1 - Math.Exp(sigma2 * h * n)) / (1 - Math.Exp(sigma2 * h)))
                                ) :
               S * S * Math.Exp(o * t1) / n / n *
                       (
                           (1 - Math.Exp(o * h * n)) / (1 - Math.Exp(o * h)) +
                           2.0 / (1 - Math.Exp((b + sigma2) * h)) *
                           ((1 - Math.Exp(b * h * n)) / (1 - Math.Exp(b * h)) - (1 - Math.Exp(o * h * n)) / (1 - Math.Exp(o * h)))
                          );
            double sigmaNew = Math.Sqrt((Math.Log(EA2) - 2 * Math.Log(EA)) / T);
            

            if (m > 0)
            {
                if  (n == m)
                {
                    return 0;
                }
                else
                {
                    X = n / (n - m) * X - m / (n - m) * SA;
                }        
            }

            double d1 = (Math.Log(EA / X) + T * Math.Pow(sigmaNew, 2) / 2.0) / (sigmaNew * Math.Sqrt(T));
            double d2 = d1 - sigmaNew * Math.Sqrt(T);

            var OptionValue = (optionType == OptionType.Call) ?
                Math.Exp(-r * T) * (EA * NormalCdf.NormalCdfHart(d1) - X * NormalCdf.NormalCdfHart(d2)) :
                Math.Exp(-r * T) * (-EA * NormalCdf.NormalCdfHart(-d1) + X * NormalCdf.NormalCdfHart(-d2));

            return OptionValue * (n - m) / n;
        }

        private double CalcPV(double S, double T, double r, double b, double sigma,double t1)
            {
            double pv = 0.0;
            double floatingcall = 0.0;
            double floatingput = 0.0;
            if (_strikeStyle == StrikeStyle.Fixed)
            {
                if (_asianType == AsianType.GeometricAverage)
                {
                    pv = GeometricAverage(_optionType, S: S, X: _X, T: T, r: r, b: b, sigma: sigma) * _notional;
                }
                if (_asianType == AsianType.ArithmeticAverage)
                {
                    pv = ArithmeticAverage(_optionType, S: S, SA: _SA, X: _X, T: T, T2: _T2, r: r, b: b, sigma: sigma) * _notional;
                }
                if (_asianType == AsianType.DiscreteArithmeticAverage)
                {
                    pv = DiscreteArithmeticAverage(_optionType, S: S, SA: _SA, X: _X, t1: t1, T: T, n: _numOfObsDates, m: _numOfObservedDates, r: r, b: b, sigma: sigma) * _notional;
                }
            }

            if (_strikeStyle == StrikeStyle.Floating)
            {

                if (_asianType == AsianType.GeometricAverage)
                {
                    floatingcall = GeometricAverage(OptionType.Put, S: S, X: S, T: T, r: r - b, b: -b, sigma: sigma) * _notional;
                }
                if (_asianType == AsianType.ArithmeticAverage)
                {
                    floatingcall = ArithmeticAverage(OptionType.Put, S: S, SA: _SA, X: S, T: T, T2: _T2, r: r - b, b: -b, sigma: sigma) * _notional;
                }
                if (_asianType == AsianType.DiscreteArithmeticAverage)
                {
                    floatingcall = DiscreteArithmeticAverage(OptionType.Put, S: S, SA: _SA, X: S, t1: t1, T: T, n: _numOfObsDates, m: _numOfObservedDates, r: r - b, b: -b, sigma: sigma) * _notional;                   
                }
                floatingput = (b == 0) ? (floatingcall + Math.Exp(-r * T) * S - S) : (floatingcall + 1 / b / T * (Math.Exp((b - r) * T) - Math.Exp(-r * T)) * S - S);
                pv = (_optionType == OptionType.Call) ? floatingcall : floatingput;
            }
            return pv;
        }

            public double Pv => CalcPV(_S, _T, _r, _b, _sigma,_t1);

            //private const double riskBumpSize = 1e-4;
            //private const double vegaBumpSize = 0.01;
            //private const double timeIncrement = 1.0/365.0;

            //private double CalDelta(double spot, double T, double sigma,double t1) => (CalcPV(spot+ riskBumpSize, _T, _r, _b, _sigma,_t1) - Pv) / riskBumpSize;

            //public double Delta => CalDelta(_S, _T, _sigma,_t1);

            //public double Gamma => (CalcPV(_S + riskBumpSize, _T, _r, _b, _sigma, _t1) - 2*Pv+ CalcPV(_S - riskBumpSize, _T, _r, _b, _sigma, _t1) ) / riskBumpSize / riskBumpSize;

            ////TODO: properly fix it, pass in actual day
            //public double Theta => Pv - CalcPV(_S, _T + timeIncrement, _r, _b, _sigma, _t1 + timeIncrement);

            ////pv change per 0.01 absolute change in vol
            //public double Vega => CalcVega(_S, _T, _sigma, _t1);

            //private double CalcVega(double spot, double T, double sigma, double t1) => CalcPV(spot, T, _r, _b, sigma + vegaBumpSize, t1) - CalcPV(spot, T, _r, _b, sigma, t1);

            ////pv change per 1 bp absolute change in r
            //public double Rho => CalcPV(_S, _T, _r + riskBumpSize, _b, _sigma,_t1) - Pv;

            ////higher order, cross effect
            //public double DDeltaDt => CalDelta(_S, _T - timeIncrement, _sigma, _t1 - timeIncrement) - CalDelta(_S, _T, _sigma, _t1);

            //public double DDeltaDvol => CalDelta(_S, _T, _sigma + 0.01, _t1) - CalDelta(_S, _T, _sigma, _t1);

            //public double DVegaDt => CalcVega(_S, _T - timeIncrement, _sigma, _t1 - timeIncrement) - CalcVega(_S, _T, _sigma, _t1);

            //public double DVegaDvol => (CalcVega(_S, _T, _sigma + vegaBumpSize, _t1) - CalcVega(_S, _T, _sigma, _t1));

        }

}

