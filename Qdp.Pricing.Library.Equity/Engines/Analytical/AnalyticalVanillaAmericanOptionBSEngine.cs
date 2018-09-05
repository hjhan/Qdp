using System;
using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.Maths;
using Qdp.Pricing.Library.Equity.Interfaces;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.Pricing.Base.Utilities;

namespace Qdp.Pricing.Library.Equity.Engines.Analytical
{
    /// <summary>
    /// 美式期权的Bjerksund And Stensland 2002计算引擎
    /// </summary>
    /// <remarks>
    /// https://core.ac.uk/download/pdf/52069431.pdf
    /// </remarks>
    // Two calculations depending on the underlying type
    //Black Schole: The model is widely used for modeling European options on stocks or equity index
    //Black model: The model is widely used for modeling European options on physical commodities, forwards or futures. http://www.riskencyclopedia.com/articles/black_1976/
    // DiscountCurve => physical world risk free curve
    // DividendCurve => physical world dividend curve
    public class AnalyticalVanillaAmericanOptionBSEngine : BaseNumericalOptionEngine
    {

        private BjerksundAndStensland2002Calculator _calculator = null;

        private BjerksundAndStensland2002Calculator ConfigureCalculator(IOption option, IMarketCondition market, 
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

            var dividendRateInput = market.DividendCurves.Value.Values.First().ZeroRate(market.ValuationDate, exerciseDate);
            var dividendRate = AnalyticalOptionPricerUtil.dividendDf(trade.UnderlyingProductType, dividendRateInput, riskfreeRate);

            var spot = market.SpotPrices.Value.Values.First();
            double sigma = AnalyticalOptionPricerUtil.pricingVol(volSurf: market.VolSurfaces.Value.Values.First(),
                exerciseDate: exerciseDate, option: option, spot: spot);

            var calculator = new BjerksundAndStensland2002Calculator(trade.OptionType,
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

    }

    #region calculator
    /// Closed form Vanilla American option price and risk calculator
	/// Reference: Espen Haug - The Complete Guide To Option Pricing Formulas (2006 2nd ed) and its workbook.
    /// see also https://brage.bibsys.no/xmlui/bitstream/handle/11250/163705/bjerksund%20petter%200902.pdf?sequence=1&isAllowed=y
    /// 
    class BjerksundAndStensland2002Calculator
    {
        private readonly OptionType _optionType;
        private readonly double _S, _T, _X, _sigma, _r, _b, _dividend, _notional;
        private readonly bool _isOptionOnForward;
        private readonly double _riskfreeDfAtExercise, _riskfreeDfAtMaturity;
        private double _expiryDayRemainingLife;

        public BjerksundAndStensland2002Calculator(OptionType optionType, double strike, double spotPrice, 
            double maturityInYears, double standardDeviation, double riskFreeRate, double dividendRate, 
            double notional, InstrumentType underlyingInstrumentType, 
            double riskfreeDfAtExercise, double riskfreeDfAtMaturity,
            double expiryDayRemainingLife=double.NaN, double timeIncrement = 0.0)
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

        private double Phi(double S, double T, double gamma, double h, double i, double r, double b, double sigma)
        {
            double lambda = (-r + gamma * b + 0.5 * gamma * (gamma - 1) * sigma * sigma) * T;
            double d = -(Math.Log(S / h) + (b + (gamma - 0.5) * sigma * sigma) * T) / sigma / Math.Sqrt(T);
            double kappa = 2 * b / sigma / sigma + (2 * gamma - 1);
            double phi = Math.Exp(lambda) * Math.Pow(S, gamma) * (NormalCdf.NormalCdfHart(d) - Math.Pow(i / S, kappa) * NormalCdf.NormalCdfHart(d - 2 * Math.Log(i / S) / sigma / Math.Sqrt(T)));

            return phi;
        }

        private double Ksi(double S, double T, double gamma, double h, double i2, double i1, double t1, double r, double b, double sigma)
        {

            double e1 = (Math.Log(S / i1) + (b + (gamma - 0.5) * sigma * sigma) * t1) / sigma / Math.Sqrt(t1);
            double e2 = (Math.Log(i2 * i2 / S / i1) + (b + (gamma - 0.5) * sigma * sigma) * t1) / sigma / Math.Sqrt(t1);
            double e3 = (Math.Log(S / i1) - (b + (gamma - 0.5) * sigma * sigma) * t1) / sigma / Math.Sqrt(t1);
            double e4 = (Math.Log(i2 * i2 / S / i1) - (b + (gamma - 0.5) * sigma * sigma) * t1) / sigma / Math.Sqrt(t1);

            double f1 = (Math.Log(S / h) + (b + (gamma - 0.5) * sigma * sigma) * T) / sigma / Math.Sqrt(T);
            double f2 = (Math.Log(i2 * i2 / S / h) + (b + (gamma - 0.5) * sigma * sigma) * T) / sigma / Math.Sqrt(T);
            double f3 = (Math.Log(i1 * i1 / S / h) + (b + (gamma - 0.5) * sigma * sigma) * T) / sigma / Math.Sqrt(T);
            double f4 = (Math.Log(S * i1 * i1 / i2 / i2 / h) + (b + (gamma - 0.5) * sigma * sigma) * T) / sigma / Math.Sqrt(T);

            double rho = Math.Sqrt(t1 / T);
            double lambda = -r + gamma * b + 0.5 * gamma * (gamma - 1) * sigma * sigma;
            double kappa = 2 * b / sigma / sigma + (2 * gamma - 1);
            double ksi = Math.Exp(lambda * T) * Math.Pow(S, gamma) * (NormalCdf.NormalCdfGenz(-e1, -f1, rho)
                - Math.Pow(i2 / S, kappa) * NormalCdf.NormalCdfGenz(-e2, -f2, rho) - Math.Pow(i1 / S, kappa) * NormalCdf.NormalCdfGenz(-e3, -f3, -rho)
                + Math.Pow(i1 / i2, kappa) * NormalCdf.NormalCdfGenz(-e4, -f4, -rho));

            return ksi;
        }

        private double GBS(OptionType optionType, double S,double X, double T, double r, double b, double sigma)
        {
            double d1 = (Math.Log(S / X) + (b + 0.5 * Math.Pow(sigma, 2)) * T) / (sigma * Math.Sqrt(T));
            double d2 = d1 - sigma * Math.Sqrt(T);

            var Gbs = (optionType == OptionType.Call)?
                Math.Exp(-r * T) * (S * Math.Exp(b * T) * NormalCdf.NormalCdfHart(d1) - X * NormalCdf.NormalCdfHart(d2)):
                -1.0 * Math.Exp(-r * T) * (S * Math.Exp(b * T) * NormalCdf.NormalCdfHart(-d1) - X * NormalCdf.NormalCdfHart(-d2));
            return Gbs;
        }

        private double BSAmerican2002(double S, double X, double T, double r, double b, double sigma)
        {
            double t1 = 0.5 * (Math.Sqrt(5) - 1) * T;
            double BSAmerican2002;

            if (b >= r)
            {
                BSAmerican2002 = GBS(0, S, X, T, r, b, sigma);
            }
            else
            {
                double beta = (0.5 - b / sigma / sigma) + Math.Sqrt(Math.Pow(b / sigma / sigma - 0.5, 2) + 2 * r / sigma / sigma);
                double binf = beta * X / (beta - 1);
                double b0 = Math.Max(X, X * r / (r - b));

                double ht1 = -(b * t1 + 2 * sigma * Math.Sqrt(t1)) * X * X / b0 / (binf - b0);
                double ht2 = -(b * T + 2 * sigma * Math.Sqrt(T)) * X * X / b0 / (binf - b0);
                double i1 = b0 + (binf - b0) * (1 - Math.Exp(ht1));
                double i2 = b0 + (binf - b0) * (1 - Math.Exp(ht2));
                double alpha1 = (i1 - X) * Math.Pow(i1, -beta);
                double alpha2 = (i2 - X) * Math.Pow(i2, -beta);

                if (S >= i2)
                {
                    BSAmerican2002 = S - X;
                }
                else
                {
                    BSAmerican2002 = alpha2 * Math.Pow(S, beta) - alpha2 * Phi(S, t1, beta, i2, i2, r, b, sigma)
                        + Phi(S, t1, 1, i2, i2, r, b, sigma) - Phi(S, t1, 1, i1, i2, r, b, sigma)
                        - X * Phi(S, t1, 0, i2, i2, r, b, sigma) + X * Phi(S, t1, 0, i1, i2, r, b, sigma)
                        + alpha1 * Phi(S, t1, beta, i1, i2, r, b, sigma) - alpha1 * Ksi(S, T, beta, i1, i2, i1, t1, r, b, sigma)
                        + Ksi(S, T, 1, i1, i2, i1, t1, r, b, sigma) - Ksi(S, T, 1, X, i2, i1, t1, r, b, sigma)
                        - X * Ksi(S, T, 0, i1, i2, i1, t1, r, b, sigma) + X * Ksi(S, T, 0, X, i2, i1, t1, r, b, sigma);

                }
            }

            return BSAmerican2002;
        }

        private double CalcPV(double S, double T, double r, double b, double sigma)
        {
            var dfExerciseToMaturity = (_isOptionOnForward) ? _riskfreeDfAtMaturity / _riskfreeDfAtExercise : 1.0;
            var pv = _optionType == OptionType.Call ?
                _notional * dfExerciseToMaturity * BSAmerican2002(S, _X, T, r, b, sigma) :
                _notional * dfExerciseToMaturity * BSAmerican2002(_X, S, T, r - b, -b, sigma);

            return pv;
        }
        
        public double Pv => CalcPV(_S, _T, _r, _b, _sigma);

        //private const double riskBumpSize = 1e-4;
        //private const double vegaBumpSize = 0.01;
        //private const double timeIncrement = 1.0 / 365.0;

        //private double CalDelta(double spot, double T, double sigma) => ( CalcPV(spot + riskBumpSize, _T, _r, _b, _sigma) - Pv )/ riskBumpSize;

        //public double Delta => CalDelta(_S, _T, _sigma);

        //public double Gamma => CalDelta(_S + riskBumpSize, _T, _sigma) - CalDelta(_S, _T, _sigma);

        ////TODO: properly fix it, pass in actual day
        //public double Theta => CalcPV(_S, _T - timeIncrement , _r, _b, _sigma) - Pv;

        ////pv change per 0.01 absolute change in vol
        //public double Vega => CalcVega(_S, _T, _sigma); //CalcPV(_S, _T, _r, _b, _sigma + vegaBumpSize) - Pv;

        //private double CalcVega(double spot, double T, double sigma) => CalcPV(spot, T, _r, _b, sigma + vegaBumpSize) - CalcPV(spot, T, _r, _b, sigma);

        ////pv change per 1 bp absolute change in r
        //public double Rho => CalcPV(_S, _T, _r + riskBumpSize, _b, _sigma) - Pv;

        ////higher order, cross effect
        //public double DDeltaDt => CalDelta(_S, _T - timeIncrement, _sigma) - CalDelta(_S, _T, _sigma);

        //public double DDeltaDvol => CalDelta(_S, _T, _sigma + vegaBumpSize) - CalDelta(_S, _T, _sigma);

        //public double DVegaDt => CalcVega(_S, _T - timeIncrement, _sigma) - CalcVega(_S, _T, _sigma);

        //public double DVegaDvol => (CalcVega(_S, _T, _sigma + vegaBumpSize) - CalcVega(_S, _T, _sigma));

    }


    #endregion
}
