using System;
using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.Maths;
using Qdp.Pricing.Library.Equity.Interfaces;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Base.Utilities;


namespace Qdp.Pricing.Library.Equity.Engines.Analytical
{
    /// <summary>
    /// 欧式香草期权的解析计算引擎
    /// 
    /// http://www.riskencyclopedia.com/articles/black_1976/
    /// </summary>
    // Two calculations depending on the underlying type
    //Black Schole: The model is widely used for modeling European options on stocks or equity index
    //Black model: The model is widely used for modeling European options on physical commodities, forwards or futures. 
    // DiscountCurve => physical world risk free curve
    // DividendCurve => physical world dividend curve
    public class AnalyticalVanillaEuropeanOptionEngine : BaseNumericalOptionEngine
    {
        private BlackScholeCalculator _calculator = null;

        private BlackScholeCalculator ConfigureCalculator(IOption option, IMarketCondition market, 
            double expiryDayRemainingLife = double.NaN, double timeIncrement = 0.0)
        {
            var trade = (VanillaOption)option;
            var exerciseDate = trade.ExerciseDates.Last();
            var maturityDate = trade.UnderlyingMaturityDate;

            var spot = market.SpotPrices.Value.Values.First();
            double sigma = AnalyticalOptionPricerUtil.pricingVol(volSurf: market.VolSurfaces.Value.Values.First(),
                exerciseDate: exerciseDate, option: option, spot: spot);

            var riskfreeRateAtExercise = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, exerciseDate);
            var riskfreeRateAtMaturity = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, maturityDate);
            var riskFreeRate = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, exerciseDate);
            var dividendCurveInput = market.DividendCurves.Value.Values.First().ZeroRate(market.ValuationDate, exerciseDate);
            var dividendInput = AnalyticalOptionPricerUtil.dividendYieldOutput(dividendCurveInput, riskFreeRate,
               option.Dividends, spot, market.ValuationDate, trade.ExerciseDates.Last(), option.DayCount);

            var dividendRate = AnalyticalOptionPricerUtil.dividenRate(trade.UnderlyingProductType, dividendInput, riskFreeRate);

            var BSCalculator = new BlackScholeCalculator(trade.OptionType,
                strike: option.IsMoneynessOption? trade.Strike * trade.InitialSpotPrice: trade.Strike,
                spotPrice: spot,
                sigma: sigma,
                riskfreeRateAtExercise: riskfreeRateAtExercise,
                riskfreeRateAtMaturity: riskfreeRateAtMaturity,
                curveDayCount: market.DiscountCurve.Value.DayCount,
                dividendRate: dividendRate,
                maturityDate: trade.UnderlyingMaturityDate,     //maturity of underlying, i.e. option on futures
                exerciseDate: trade.ExerciseDates.Last(),
                valuationDate: market.ValuationDate,
                trade: trade,
                underlyingInstrumentType: trade.UnderlyingProductType,
                notional: trade.Notional,
                expiryDayRemainingLife: expiryDayRemainingLife,
                timeIncrement: timeIncrement);
            _calculator = BSCalculator;
            return BSCalculator;
        }

    
        protected override double CalcPv(IOption option, IMarketCondition market, double timeIncrement = 0.0)
        {
            if (!(option is VanillaOption))
            {
                throw new PricingBaseException("");
            }
            var trade = (VanillaOption)option;
            var BSCalculator = ConfigureCalculator(option, market, timeIncrement: timeIncrement);

            return BSCalculator.Pv;
        }

        //, double SpotPriceBump
        protected override double CalcExpiryDelta(IOption option, IMarketCondition[] markets, double T)
        {       
            var pvBase = ConfigureCalculator(option, markets[0], expiryDayRemainingLife: T).Pv;
            var pvUp = ConfigureCalculator(option, markets[1], expiryDayRemainingLife: T).Pv;

            return (pvUp -pvBase) / SpotPriceBump;
        }
        protected override double CalcExpiryPV(IOption option, IMarketCondition market, double T)
        {
            return ConfigureCalculator(option, market, expiryDayRemainingLife: T).Pv; 
             
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

        /// <summary>
        /// 计算隐含波动率
        /// </summary>
        /// <param name="option">期权</param>
        /// <param name="market">市场</param>
        /// <param name="targetPremium">权利金</param>
        /// <returns>隐含波动率</returns>
        //Note: calc implied vol & calc implied expiry date do not support tune-fined T-to-Maturity pricing mode
        public double ImpliedVol(IOption option, IMarketCondition market, double targetPremium)
        {
            var timeIncrement = AnalyticalOptionPricerUtil.optionTimeToMaturityIncrement(option);
            var calculator = ConfigureCalculator(option, market, timeIncrement: timeIncrement);
            return calculator.SolveVol(targetPremium);
        }

        public Date ImpliedExpiryDate(IOption option, IMarketCondition market, double targetPremium)
        {
            var timeIncrement = AnalyticalOptionPricerUtil.optionTimeToMaturityIncrement(option);
            var calculator = ConfigureCalculator(option, market, timeIncrement: timeIncrement);
            return calculator.SolveExpiryDate(targetPremium);
        }

        public double ImpliedVolFromPremium(double targetPremium,
            VanillaOption option, MarketCondition market)
        {
            var timeIncrement = AnalyticalOptionPricerUtil.optionTimeToMaturityIncrement(option);
            var calculator = ConfigureCalculator(option, market, timeIncrement: timeIncrement);
            return calculator.SolveVol(targetPremium);
        }
    }       
       
    #region calculator
    internal class BlackScholeCalculator
    {
        private readonly OptionType _optionType;
        internal readonly double _spotPrice;
        internal readonly double _strike;
        internal readonly double _sigma;

        private readonly double _notional;
        private readonly bool _isDelayedPay;
        private readonly bool _isOptionOnFutures;
        private readonly bool _isOptionOnForward;

        private readonly double _riskfreeRateAtExercise;
        private readonly double _riskfreeRateAtMaturity;
        private readonly double _dividendRate;
        private readonly double _riskfreeDfAtExercise;
        private readonly double _riskfreeDfAtMaturity;
        private readonly Date _maturityDate;
        internal readonly Date _exerciseDate;
        internal readonly Date _valuationDate;
        internal readonly IOption _trade;
        internal readonly double _expiryDayRemainingLife;
        internal readonly double _timeIncrement;
        private InstrumentType _underlyingInstrumentType;
        private readonly IDayCount _curveDayCount;

        public BlackScholeCalculator(OptionType optionType,
            double strike,
            double spotPrice,
            double sigma,
            double riskfreeRateAtExercise,
            double riskfreeRateAtMaturity,
            IDayCount curveDayCount,
            double dividendRate,
            Date maturityDate,
            Date exerciseDate,
            Date valuationDate,
            IOption trade,
            InstrumentType underlyingInstrumentType,
            double notional,
            double expiryDayRemainingLife,
            double timeIncrement = 0.0)
        {
            _underlyingInstrumentType = underlyingInstrumentType;
            _isOptionOnFutures = AnalyticalOptionPricerUtil.isFuturesOption(underlyingInstrumentType);
            _isOptionOnForward = AnalyticalOptionPricerUtil.isForwardOption(underlyingInstrumentType);

            _optionType = optionType;
            _strike = strike;
            _spotPrice = spotPrice;
            _notional = notional;
            _sigma = sigma;
            _riskfreeRateAtMaturity = riskfreeRateAtMaturity;
            _riskfreeRateAtExercise = riskfreeRateAtExercise;
            _dividendRate = dividendRate;

            _trade = trade;
            _maturityDate = maturityDate;
            _exerciseDate = exerciseDate;
            _valuationDate = valuationDate;

            _riskfreeDfAtExercise = CalcDfFromZeroRate(_riskfreeRateAtExercise, _exerciseDate, _valuationDate);
            _riskfreeDfAtMaturity = CalcDfFromZeroRate(_riskfreeRateAtMaturity, _maturityDate, _valuationDate);

            //just a internal indicator
            _isDelayedPay = maturityDate.CompareTo(_exerciseDate) > 0;

            _expiryDayRemainingLife = expiryDayRemainingLife;
            _timeIncrement = timeIncrement;
            _curveDayCount = curveDayCount;
        }


        private double CalcDfFromZeroRate(double zeroRate, Date date, Date valuationDate) {
            return Compound.Continuous.CalcDfFromZeroRate(zeroRate, _trade.DayCount.CalcDayCountFraction(valuationDate, date));
        }

        //Solve implied exerciseDay 
        public Date SolveExpiryDate(double targetPremium)
        {
            var impliedT = BrentZero.Solve(new SolveBlackScholesExpiry(this, targetPremium), left: 1.0 / 365.0, right: 10.0, tolerance: 1.0 / 365.0);
            //TODO:  do it properly with daycount convention
            var days = Convert.ToInt16(impliedT * 365.0);
            return _valuationDate.AddDays(days);
        }

        public double SolveVol(double targetPremium)
        {
            var impliedVol = BrentZero.Solve(new SolveBlackScholesVol(this, targetPremium), 1e-6, 100.0);
            return impliedVol;
        }

        //Note: for vol calibration
        internal double CalcPV(double S, double sigma, Date valuationDate, Date exerciseDate, 
            double riskFreeCurveShiftInBp = 0.0, double dividendCurveShiftInBp = 0.0)
        {
            AnalyticalOptionPricerUtil.prepareBlackScholesInputs(
                spotPrice: S,
                riskfreeRateAtExerciseInput: _riskfreeRateAtExercise,
                riskfreeRateAtMaturityInput: _riskfreeRateAtMaturity,
                dividendRateInput: _dividendRate,
                riskFreeCurveShiftInBp: riskFreeCurveShiftInBp,
                dividendCurveShiftInBp: dividendCurveShiftInBp,
                maturityDate: _maturityDate,
                exerciseDate: exerciseDate,
                valuationDate: valuationDate,
                curveDayCount: _curveDayCount,
                trade: _trade,
                isOptionOnForward: _isOptionOnForward,
                isForwardFuturesOption: _isOptionOnForward || _isOptionOnFutures,
                strike: _strike,
                sigma: sigma,
                nd1: out double nd1,
                nd2: out double nd2,
                riskfreeDfAtExercise: out double riskfreeDfAtExercise,
                dfExerciseToMaturity: out double dfExerciseToMaturity,
                forwardPrice: out double forwardPrice,
                expiryDayRemainingLife: _expiryDayRemainingLife,
                timeIncrement: _timeIncrement);

            var pv = _optionType == OptionType.Call
                    ? _notional * dfExerciseToMaturity * riskfreeDfAtExercise * (forwardPrice * nd1 - _strike * nd2)
                    : _notional * dfExerciseToMaturity * riskfreeDfAtExercise * (_strike * (1 - nd2) - forwardPrice * (1 - nd1));

            return pv;
        }

        private const double riskBumpSize = 1e-4;
        private const double vegaBumpSize = 0.01;

        public double Pv => CalcPV(S: _spotPrice, sigma: _sigma, valuationDate: _valuationDate, exerciseDate: _exerciseDate);

        //private double CalcDelta(double spot, Date valuationDate, double sigma) {
        //    var exerciseDate = _exerciseDate;
        //    return ( CalcPV(S: spot + riskBumpSize, sigma: sigma, valuationDate: valuationDate, exerciseDate: _exerciseDate) - 
        //        CalcPV(S: spot, sigma: sigma, valuationDate: valuationDate, exerciseDate: _exerciseDate) )/ riskBumpSize;
        //}

        ////1bp delta
        //public double Delta => CalcDelta(_spotPrice, _valuationDate, _sigma);
        ////public double DoDelta(double sigma) => CalcDelta(_spotPrice, _valuationDate, sigma);
        
        ////1bp gamma, not so useful
        //public double Gamma => (CalcDelta(_spotPrice+ riskBumpSize, _valuationDate, _sigma) - CalcDelta(_spotPrice, _valuationDate, _sigma))/riskBumpSize ;

        ////pv change per 0.01 absolute change in vol, unlike other greeks, this number is not standardized
        //public double Vega => CalcVega(_valuationDate, _spotPrice, _sigma);

        //public double CalcVega(Date valuationDate, double spotPrice, double sigma) {
        //    return CalcPV(S: spotPrice, sigma: sigma + vegaBumpSize, valuationDate: valuationDate, exerciseDate: _exerciseDate) -
        //        CalcPV(S: spotPrice, sigma: sigma, valuationDate: valuationDate, exerciseDate: _exerciseDate);
        //}
        
        ////pv change per 1 bp absolute change in r
        //public double Rho => CalcPV(S: _spotPrice, sigma: _sigma, valuationDate: _valuationDate, exerciseDate: _exerciseDate, riskFreeCurveShiftInBp: 1) - Pv;
        
        //public double RhoForeign => CalcPV(S: _spotPrice, sigma: _sigma, valuationDate: _valuationDate, exerciseDate: _exerciseDate, dividendCurveShiftInBp: 1) - Pv;
        
        ////pv change per 1 day move forward
        //public double Theta => CalcPV(S: _spotPrice, sigma: _sigma, valuationDate: _valuationDate.AddDays(1), exerciseDate: _exerciseDate) - Pv;

        ////higher order, cross effect

        ////1day move, delta change
        //public double DDeltaDt => CalcDelta(_spotPrice, _valuationDate.AddDays(1), _sigma) - CalcDelta(_spotPrice, _valuationDate, _sigma);

        ////1day move, vega change
        //public double DVegaDt => CalcVega(_valuationDate.AddDays(1), _spotPrice, _sigma) - CalcVega(_valuationDate, _spotPrice, _sigma);

        ////1%vol move, delta change
        //public double DDeltaDvol => CalcDelta(_spotPrice, _valuationDate, _sigma + vegaBumpSize) - CalcDelta(_spotPrice, _valuationDate, _sigma);
        
        //public double DVegaDvol => (CalcVega(_valuationDate, _spotPrice, _sigma + vegaBumpSize) - CalcVega(_valuationDate, _spotPrice, _sigma));

        //previous implementation without taking Black76 model into account
        //bs: Delta
        //_optionType == OptionType.Call
        //        ? _notional * _dfExerciseToMaturity * _dividendDfAtExercise * _nd1
        //        : _notional * _dfExerciseToMaturity * _dividendDfAtExercise * (_nd1 - 1);

        //bs: Gamma
        //_notional * _nPrimceD1 * _dfExerciseToMaturity * _dividendDfAtExercise / (_spotPrice * _sigma * Math.Sqrt(_exerciseInYears));

        //bs:Vega
        //_notional * _spotPrice * Math.Sqrt(_exerciseInYears) * _nPrimceD1 * _dfExerciseToMaturity * _dividendDfAtExercise * 0.01;

        //bs: Rho
        //_optionType == OptionType.Call
        //        ? _notional * _strike * _maturityInYears * _riskfreeDfAtExercise * _nd2 / 10000
        //        : -_notional * _strike * _maturityInYears * _riskfreeDfAtExercise * (1 - _nd2) / 10000;

        //bs: RhoForeign
        // _optionType == OptionType.Call
        //            ? -_notional* _spotPrice * _maturityInYears* _dividendDfAtExercise * _nd1 / 10000
        //            : _notional* _spotPrice * _maturityInYears* _dividendDfAtExercise * (1 - _nd1) / 10000;

        //bs: Theta
        //{
        //    get
        //    {
        //        var dividendRate = -Math.Log(_dividendDfAtExercise) / Math.Sqrt(_exerciseInYears);
        //        var riskfreeRate = -Math.Log(_riskfreeDfAtExercise) / Math.Sqrt(_exerciseInYears);
        //        var c = -_spotPrice * _nPrimceD1 * _sigma * _dfExerciseToMaturity * _dividendDfAtExercise / (2 * Math.Sqrt(_exerciseInYears));
        //        return _optionType == OptionType.Call
        //            ? _notional * (c + dividendRate * _spotPrice * _nd1 * _dfExerciseToMaturity * _dividendDfAtExercise - riskfreeRate * _strike * _riskfreeDfAtMaturity * _nd2) / 365
        //            : _notional * (c - dividendRate * _spotPrice * (1 - _nd1) * _dfExerciseToMaturity * _dividendDfAtExercise + riskfreeRate * _strike * _riskfreeDfAtMaturity * (1 - _nd2)) / 365;
        //    }
        //}
    }
    #endregion

    internal class SolveBlackScholesVol: IFunctionOfOneVarialbe
    {
        private readonly BlackScholeCalculator bs;
        private readonly double target;

        public SolveBlackScholesVol(BlackScholeCalculator calculator, double targetPremium) {
            this.bs = calculator;
            this.target = targetPremium;
        }

        public double F(double vol)
        {
            return bs.CalcPV(S: bs._spotPrice, sigma: vol, valuationDate: bs._valuationDate, exerciseDate: bs._exerciseDate) - this.target;
        }
    }

    internal class SolveBlackScholesExpiry : IFunctionOfOneVarialbe
    {
        private readonly BlackScholeCalculator bs;
        private readonly double target;
        public SolveBlackScholesExpiry(BlackScholeCalculator calculator, double targetPremium)
        {
            this.bs = calculator;
            this.target = targetPremium;
        }

        public double F(double t)
        {
            //bs._dayCount.CalcDayCountFraction(startDate: bs._valuationDate, endDate: bs)
            //TODO: properly backout exerciseDay from timeFraction
            var exerciseDate = bs._valuationDate.AddDays(Convert.ToInt32(t * 365.0)); 
            return bs.CalcPV(S: bs._spotPrice, sigma: bs._sigma, valuationDate: bs._valuationDate, exerciseDate: exerciseDate) - this.target;
        }
    }
}
