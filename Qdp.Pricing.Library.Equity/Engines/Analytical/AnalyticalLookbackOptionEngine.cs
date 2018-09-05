using System;
using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.Pricing.Library.Common.MathMethods.Maths;
using Qdp.Pricing.Library.Equity.Interfaces;
using Qdp.Pricing.Base.Utilities;
using System.Linq;

namespace Qdp.Pricing.Library.Equity.Engines.Analytical
{

    public class AnalyticalLookbackOptionEngine : BaseNumericalOptionEngine
    {
        private LookbackOptionCalculator _calculator = null;
        private LookbackOptionCalculator ConfigureCalculator(IOption option, IMarketCondition market,
            double expiryDayRemainingLife = double.NaN, double timeIncrement = 0.0)
        {
            var trade = (LookbackOption)option;

            var exerciseDate = trade.ExerciseDates.Last();
            var observedMax = trade.Fixings.Any() ? trade.Fixings.Max(x => x.Value) : market.SpotPrices.Value.Values.First();
            var observedMin = trade.Fixings.Any() ? trade.Fixings.Min(x => x.Value) : market.SpotPrices.Value.Values.First();

            var spot = market.SpotPrices.Value.Values.First();
            double sigma = AnalyticalOptionPricerUtil.pricingVol(volSurf: market.VolSurfaces.Value.Values.First(),
                exerciseDate: exerciseDate, option: option, spot: spot);

            double t;
            if (!double.IsNaN(expiryDayRemainingLife))
                t = expiryDayRemainingLife;
            else
                t = trade.DayCount.CalcDayCountFraction(market.ValuationDate, exerciseDate) + timeIncrement;

            var riskFreeRate = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, exerciseDate);
            var dividendCurveInput = market.DividendCurves.Value.Values.First().ZeroRate(market.ValuationDate, exerciseDate);
            var dividendInput = AnalyticalOptionPricerUtil.dividendYieldOutput(dividendCurveInput, riskFreeRate,
                option.Dividends, spot, market.ValuationDate, trade.ExerciseDates.Last(), option.DayCount);
            var dividendRate = AnalyticalOptionPricerUtil.dividenRate(trade.UnderlyingProductType, dividendInput, riskFreeRate);

            var calculator = new LookbackOptionCalculator(trade.OptionType, trade.UnderlyingProductType,
                trade.StrikeStyle,
                strike: option.IsMoneynessOption ? trade.Strike * trade.InitialSpotPrice : trade.Strike,
                spotPrice: spot,               
                exerciseInYears: t,
                realizedMaxPrice: observedMax,
                realizedMinPrice: observedMin,
                sigma: sigma,
                riskFreeRate: riskFreeRate,
                dividendRate: dividendRate,
                notional: trade.Notional);
            this._calculator = calculator;
            return calculator;
        }

        protected override double CalcIntrinsicValue(IOption option, IMarketCondition market)
        {
            var asianOption = option as LookbackOption;
            var callPayOff = (asianOption.StrikeStyle == StrikeStyle.Fixed) ?
                    Math.Max(asianOption.Fixings.Values.Max() - option.Strike, 0) :
                    Math.Max(market.SpotPrices.Value.Values.First() - asianOption.Fixings.Values.Min(), 0);
            var putPayOff = (asianOption.StrikeStyle == StrikeStyle.Fixed) ?
                    Math.Max(option.Strike - asianOption.Fixings.Values.Min(), 0) :
                    Math.Max(asianOption.Fixings.Values.Max() - market.SpotPrices.Value.Values.First(), 0);

            if (option.OptionType == OptionType.Call)
                return callPayOff * option.Notional;
            else
                return putPayOff * option.Notional;
        }

        protected override double CalcExpiryDelta(IOption option, IMarketCondition[] markets, double T)
        {
            var trade = (LookbackOption)option;

            var pvBase = ConfigureCalculator(option, markets[0], expiryDayRemainingLife: T).Pv;
            var pvUp = ConfigureCalculator(option, markets[1], expiryDayRemainingLife: T).Pv;

            return (pvUp - pvBase) / SpotPriceBump;
        }

        protected override double CalcPv(IOption option, IMarketCondition market, double timeIncrement = 0.0)
        {
            double pv = 0.0;
            if (!(option is LookbackOption))
            {
                throw new PricingBaseException("");
            }
            var trade = (LookbackOption)option;
            var Calculator = ConfigureCalculator(option, market, timeIncrement: timeIncrement);
            pv = Calculator.Pv;
            return pv;
        }
    }


    internal class LookbackOptionCalculator
    {
        private OptionType _optionType;
        private StrikeStyle _strikeStyle;
        private double _S, _Smin, _Smax, _T, _X, _sigma, _r, _dividendRate, _b, _notional;

        private InstrumentType[] FuturesProducts = { InstrumentType.Futures, InstrumentType.BondFutures, InstrumentType.CommodityFutures };
        public LookbackOptionCalculator(
            OptionType optionType, InstrumentType underlyingProductType, StrikeStyle strikeStyle,
            double strike, double spotPrice, 
            double exerciseInYears, double realizedMaxPrice, double realizedMinPrice,
            double sigma, double riskFreeRate, double dividendRate, double notional)
        {
            _optionType = optionType;
            _strikeStyle = strikeStyle;
            _X = strike;
            _S = spotPrice;
            _Smin = realizedMinPrice;
            _Smax = realizedMaxPrice;
            _T = exerciseInYears;

            _sigma = sigma;
            _r = riskFreeRate;
            _dividendRate = dividendRate;

            if (FuturesProducts.Contains(underlyingProductType))
                _b = 0.0;
            else
                _b = riskFreeRate - dividendRate;

            _notional = notional;
        }

        //Payoff: 
        //FloatingCall: S - S_min
        //FloatingPut: S_max - S
        private double FloatingCall(double S, double X, double T, double r, double b, double sigma, double Smin)
        {
            double a1 = (Math.Log(S / Smin) + (b + sigma * sigma / 2.0) * T) / sigma / Math.Sqrt(T);
            double a2 = a1 - sigma * Math.Sqrt(T);
            double call = 0.0;
            if (b == 0.0)
            {
                call = S * Math.Exp(-r * T) * NormalCdf.NormalCdfHart(a1) - Smin * Math.Exp(-r * T) * NormalCdf.NormalCdfHart(a2)
                    + S * Math.Exp(-r * T) * sigma * Math.Sqrt(T) * (NormalCdf.NormalPdf(a1) + a1 * (NormalCdf.NormalCdfHart(a1) - 1));
            }
            if (b != 0.0)
            {
                call = S * Math.Exp((b - r) * T) * NormalCdf.NormalCdfHart(a1) - Smin * Math.Exp(-r * T) * NormalCdf.NormalCdfHart(a2)
                    + S * Math.Exp(-r * T) * sigma * sigma * 0.5 / b * (Math.Pow(S / Smin, -2.0 * b / sigma / sigma) * NormalCdf.NormalCdfHart(-a1 + 2.0 * b / sigma * Math.Sqrt(T)) - Math.Exp(b * T) * NormalCdf.NormalCdfHart(-a1));
            }
            return call;

        }

        private double FloatingPut(double S, double X, double T, double r, double b, double sigma, double Smax)
        {
            double b1 = (Math.Log(S / Smax) + (b + sigma * sigma / 2.0) * T) / sigma / Math.Sqrt(T);
            double b2 = b1 - sigma * Math.Sqrt(T);
            double put = 0.0;
            if (b == 0.0)
            {
                put = -S * Math.Exp(-r * T) * NormalCdf.NormalCdfHart(-b1) + Smax * Math.Exp(-r * T) * NormalCdf.NormalCdfHart(-b2)
                    + S * Math.Exp(-r * T) * sigma * Math.Sqrt(T) * (NormalCdf.NormalPdf(b1) + b1 * NormalCdf.NormalCdfHart(b1));
            }
            if (b != 0.0)
            {
                put = -S * Math.Exp((b - r) * T) * NormalCdf.NormalCdfHart(-b1) + Smax * Math.Exp(-r * T) * NormalCdf.NormalCdfHart(-b2)
                    + S * Math.Exp(-r * T) * sigma * sigma * 0.5 / b * (-Math.Pow(S / Smax, -2.0 * b / sigma / sigma) * NormalCdf.NormalCdfHart(b1 - 2.0 * b / sigma * Math.Sqrt(T)) + Math.Exp(b * T) * NormalCdf.NormalCdfHart(b1));
            }
            return put;
        }


        private double CalcPV(double S, double T, double r, double b, double sigma, double Smin, double Smax)
        {
            double pv = 0.0;

            if (_strikeStyle == StrikeStyle.Floating)
            {
                pv = (_optionType == OptionType.Call) ? FloatingCall(S, _X, T, r, b, sigma, Smin) : FloatingPut(S, _X, T, r, b, sigma, Smax);
            }
            return pv * _notional;
        }

        public double Pv => CalcPV(_S, _T, _r, _b, _sigma, _Smin, _Smax);

    }

}


