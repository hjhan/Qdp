using System;
using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.Maths;
using Qdp.Pricing.Library.Equity.Interfaces;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.Pricing.Base.Utilities;

/// <summary>
/// Qdp.Pricing.Library.Equity.Engines.Analytical
/// </summary>
namespace Qdp.Pricing.Library.Equity.Engines.Analytical
{

    /// <summary>
    /// 美式期权的BAW计算引擎
    /// 
    /// Reference: http://finance.bi.no/~bernt/gcc_prog/algoritms_v1/algoritms/node24.html
    /// 
    /// Efficient analytic approximation of American option values: https://www.deriscope.com/docs/Barone_Adesi_Whaley_1987.pdf
    /// </summary>    
    public class AnalyticalVanillaAmericanOptionBAWEngine : BaseNumericalOptionEngine
    {

        private BAWCalculator _calculator = null;

        private BAWCalculator ConfigureCalculator(IOption option, IMarketCondition market,
            double expiryDayRemainingLife = double.NaN, double timeIncrement = 0.0)
        {
            var trade = (VanillaOption)option;

            var exerciseDate = trade.ExerciseDates.Last();
            var maturityDate = trade.UnderlyingMaturityDate;
            var exerciseInYears = trade.DayCount.CalcDayCountFraction(market.ValuationDate, exerciseDate);
            var maturityInYears = trade.DayCount.CalcDayCountFraction(market.ValuationDate, maturityDate);

            var riskfreeRate = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, exerciseDate);
            var riskfreeDfAtExercise = market.DiscountCurve.Value.GetDf(market.ValuationDate, exerciseDate);
            var riskfreeDfAtMaturity = market.DiscountCurve.Value.GetDf(market.ValuationDate, maturityDate);

            var dividendCurveInput = market.DividendCurves.Value.Values.First().ZeroRate(market.ValuationDate, exerciseDate);
            var dividendInput = AnalyticalOptionPricerUtil.dividendYieldOutput(dividendCurveInput, riskfreeRate,
               option.Dividends, market.SpotPrices.Value.Values.First(), market.ValuationDate, trade.ExerciseDates.Last(), option.DayCount);
            var dividendRate = AnalyticalOptionPricerUtil.dividendDf(trade.UnderlyingProductType, dividendInput, riskfreeRate);

            var spot = market.SpotPrices.Value.Values.First();
            double sigma = AnalyticalOptionPricerUtil.pricingVol(volSurf: market.VolSurfaces.Value.Values.First(),
                exerciseDate: exerciseDate, option: option, spot: spot);

            var calculator = new BAWCalculator(trade.OptionType,
                option.IsMoneynessOption ? trade.Strike * trade.InitialSpotPrice : trade.Strike,
                spot,
                exerciseInYears,
                sigma,
                riskfreeRate,
                dividendRate,
                trade.Notional,
                trade.UnderlyingProductType,
                riskfreeDfAtExercise: riskfreeDfAtExercise,
                riskfreeDfAtMaturity: riskfreeDfAtMaturity,
                expiryDayRemainingLife: expiryDayRemainingLife,
                timeIncrement: timeIncrement);
            this._calculator = calculator;
            return calculator;
        }
        protected override double CalcPv(IOption option, IMarketCondition market, double timeIncrement = 0.0)
        {
            if (!(option is VanillaOption))
            {
                throw new PricingBaseException("");
            }
            var trade = (VanillaOption)option;
            var Calculator = ConfigureCalculator(option, market, timeIncrement: timeIncrement);
            var pv = Calculator.Pv;
            return pv;
        }
        protected override double CalcExpiryDelta(IOption option, IMarketCondition[] markets, double T)
        {
            var pvBase = ConfigureCalculator(option, markets[0], expiryDayRemainingLife: T).Pv;
            var pvUp = ConfigureCalculator(option, markets[1], expiryDayRemainingLife: T).Pv;

            return (pvUp - pvBase) / SpotPriceBump;
        }


    }

    #region calculator
    /// Closed form Vanilla American option price and risk calculator
	/// Reference: Espen Haug - The Complete Guide To Option Pricing Formulas (2006 2nd ed) and its workbook.
    /// see also https://brage.bibsys.no/xmlui/bitstream/handle/11250/163705/bjerksund%20petter%200902.pdf?sequence=1&isAllowed=y
    /// 
    class BAWCalculator
    {
        private readonly OptionType _optionType;
        private readonly double _S, _T, _X, _sigma, _r, _b, _dividend, _notional;
        private readonly bool _isOptionOnForward;
        private readonly double _riskfreeDfAtExercise, _riskfreeDfAtMaturity;
        private double _expiryDayRemainingLife;

        public BAWCalculator(OptionType optionType, double strike, double spotPrice,
            double maturityInYears, double standardDeviation, double riskFreeRate, double dividendRate,
            double notional, InstrumentType underlyingInstrumentType,
            double riskfreeDfAtExercise, double riskfreeDfAtMaturity,
            double expiryDayRemainingLife = double.NaN, double timeIncrement = 0.0)
        {
            _isOptionOnForward = AnalyticalOptionPricerUtil.isForwardOption(underlyingInstrumentType);
            _optionType = optionType;
            _X = strike;
            _S = spotPrice;
            _T = maturityInYears + timeIncrement;
            if (!double.IsNaN(expiryDayRemainingLife))
            {
                _T = expiryDayRemainingLife;
            }
            _sigma = standardDeviation;
            _r = riskFreeRate;
            _dividend = dividendRate;
            _b = riskFreeRate - dividendRate;
            _notional = notional;

            _riskfreeDfAtExercise = riskfreeDfAtExercise;
            _riskfreeDfAtMaturity = riskfreeDfAtMaturity;
            _expiryDayRemainingLife = expiryDayRemainingLife;

        }  

        private double GBS(OptionType optionType, double S, double X, double T, double r, double b, double sigma)
        {
            double d1 = (Math.Log(S / X) + (b + 0.5 * Math.Pow(sigma, 2)) * T) / (sigma * Math.Sqrt(T));
            double d2 = d1 - sigma * Math.Sqrt(T);

            var Gbs = (optionType == OptionType.Call) ?
                Math.Exp(-r * T) * (S * Math.Exp(b * T) * NormalCdf.NormalCdfHart(d1) - X * NormalCdf.NormalCdfHart(d2)) :
                -1.0 * Math.Exp(-r * T) * (S * Math.Exp(b * T) * NormalCdf.NormalCdfHart(-d1) - X * NormalCdf.NormalCdfHart(-d2));
            return Gbs;
        }

        private double BAWCall(double S, double X, double T, double r, double b, double sigma, double ACCURACY = 1.0e-6)
        {
            double sigma_sqr = sigma * sigma;
            double time_sqrt = Math.Sqrt(T);
            double nn = 2.0 * b / sigma_sqr;
            double m = 2.0 * r / sigma_sqr;
            double K = 1.0 - Math.Exp(-r * T);
            double q2 = (-(nn - 1) + Math.Sqrt(Math.Pow((nn - 1), 2.0) + 4.0 * m / K)) * 0.5;

            double q2_inf = 0.5 * (-(nn - 1) + Math.Sqrt(Math.Pow((nn - 1), 2.0) + 4.0 * m));
            double S_star_inf = X / (1.0 - 1.0 / q2_inf);
            double h2 = -(b * T + 2.0 * sigma * time_sqrt) * (X / (S_star_inf - X));
            double S_seed = X + (S_star_inf - X) * (1.0 - Math.Exp(h2));

            int no_iterations = 0; // iterate on S to find S_star, using Newton steps
            double Si = S_seed;
            double g = 1;
            double gprime = 1.0;
            while ((Math.Abs(g) > ACCURACY)
               && (Math.Abs(gprime) > ACCURACY) // to avoid exploding Newton's  
               && (no_iterations++ < 500)
               && (Si > 0.0))
            {
                double bs = GBS(_optionType, Si, X, T, r, b, sigma);
                double d1 = (Math.Log(Si / X) + (b + 0.5 * sigma_sqr) * T) / (sigma * time_sqrt);
                g = (1.0 - 1.0 / q2) * Si - X - bs + (1.0 / q2) * Si * Math.Exp((b - r) * T) * NormalCdf.NormalCdfHart(d1);
                gprime = (1.0 - 1.0 / q2) * (1.0 - Math.Exp((b - r) * T) * NormalCdf.NormalCdfHart(d1))
                    + (1.0 / q2) * Math.Exp((b - r) * T) * NormalCdf.NormalCdfHart(d1) * (1.0 / (sigma * time_sqrt));
                Si = Si - (g / gprime);
            };

            double S_star = 0;
            if (Math.Abs(g) > ACCURACY) { S_star = S_seed; } // did not converge
            else { S_star = Si; };
            double C = 0;
            double c = GBS(_optionType, S, X, T, r, b, sigma);
            if (S >= S_star)
            {
                C = S - X;
            }
            else
            {
                double d1 = (Math.Log(S_star / X) + (b + 0.5 * sigma_sqr) * T) / (sigma * time_sqrt);
                double A2 = (1.0 - Math.Exp((b - r) * T) * NormalCdf.NormalCdfHart(d1)) * (S_star / q2);
                C = c + A2 * Math.Pow((S / S_star), q2);
            };
            if (double.IsNaN(C)) return c;
            else
            {
                return Math.Max(C, c);
            }
            
        }


        private double BAWPut(double S, double X, double T, double r, double b, double sigma, double ACCURACY = 1.0e-6)
        {
            double sigma_sqr = sigma * sigma;
            double time_sqrt = Math.Sqrt(T);
            double nn = 2.0 * b / sigma_sqr;
            double m = 2.0 * r / sigma_sqr;
            double K = 1.0 - Math.Exp(-r * T);
            double q1 = (-(nn - 1) - Math.Sqrt(Math.Pow((nn - 1), 2.0) + 4.0 * m / K)) * 0.5;

            double q1_inf = 0.5 * (-(nn - 1) - Math.Sqrt(Math.Pow((nn - 1), 2.0) + 4.0 * m));
            double S_star_inf = X / (1.0 - 1.0 / q1_inf);
            double h1 = -(b * T - 2.0 * sigma * time_sqrt) * (X / (S_star_inf - X));
            double S_seed = S_star_inf + (X - S_star_inf) * Math.Exp(h1);
   
            int no_iterations = 0; // iterate on S to find S_star, using Newton steps
            double Si = S_seed;
            double g = 1;
            double gprime = 1.0;
            while ((Math.Abs(g) > ACCURACY)
               && (Math.Abs(gprime) > ACCURACY) // to avoid exploding Newton's  
               && (no_iterations++ < 500)
               && (Si > 0.0))
            {
                double bs = GBS(_optionType, Si, X, T, r, b, sigma);
                double d1 = (Math.Log(Si / X) + (b + 0.5 * sigma_sqr) * T) / (sigma * time_sqrt);
                g = X - Si - bs + Si / q1 * (1 - Math.Exp((b - r) * T) * NormalCdf.NormalCdfHart(-d1));
                gprime = (1.0 / q1 - 1.0) * (1.0 - Math.Exp((b - r) * T) * NormalCdf.NormalCdfHart(-d1))
                    + (1.0 / q1) * Math.Exp((b - r) * T) * NormalCdf.NormalCdfHart(-d1) * (1.0 / (sigma * time_sqrt));
                Si = Si - (g / gprime);
            };

            double S_star = 0;
            if (Math.Abs(g) > ACCURACY) { S_star = S_seed; } // did not converge
            else { S_star = Si; };
            double P = 0;
            double p = GBS(_optionType, S, X, T, r, b, sigma);
            if (S <= S_star)
            {
                P = X - S;
            }
            else
            {
                double d1 = (Math.Log(S_star / X) + (b + 0.5 * sigma_sqr) * T) / (sigma * time_sqrt);
                double A1 = -(1.0 - Math.Exp((b - r) * T) * NormalCdf.NormalCdfHart(-d1)) * (S_star / q1);
                P = p + A1 * Math.Pow((S / S_star), q1);
            };
            if (double.IsNaN(P)) return p;
            else return Math.Max(P, p);
        }

        private double CalcPV(double S, double T, double r, double b, double sigma)
        {
            var dfExerciseToMaturity = (_isOptionOnForward) ? _riskfreeDfAtMaturity / _riskfreeDfAtExercise : 1.0;
            var pv = _optionType == OptionType.Call ?
                _notional * dfExerciseToMaturity * BAWCall(S, _X, T, r, b, sigma) :
                _notional * dfExerciseToMaturity * BAWPut(S, _X, T, r, b, sigma);

            return pv;
        }

        public double Pv => CalcPV(_S, _T, _r, _b, _sigma);


    }


    #endregion
}

