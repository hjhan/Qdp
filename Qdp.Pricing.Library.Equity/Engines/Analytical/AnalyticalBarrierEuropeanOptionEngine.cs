using System;
using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.Maths;
using Qdp.Pricing.Library.Equity.Interfaces;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Interfaces;

namespace Qdp.Pricing.Library.Equity.Engines.Analytical
{
    /// <summary>
    /// 障碍期权的计算引擎
    /// 
    /// 参考 Espen Haug - The Complete Guide To Option Pricing Formulas (2006 2nd ed) and its workbook.
    /// 
    /// 将black76模型扩展到以期货为标的资产的欧式障碍期权
    /// </summary>
	public class AnalyticalBarrierEuropeanOptionEngine : BaseNumericalOptionEngine
	{   
        private BarrierOptionCalculator _calculator = null;

        private BarrierOptionCalculator configureCalculator(IOption option, IMarketCondition market, 
            double expiryDayRemainingLife = double.NaN, double timeIncrement = 0.0) {
            var trade = (BarrierOption)option;
            var exerciseDate = trade.ExerciseDates.Last();
            var maturityDate = trade.UnderlyingMaturityDate;
            var spot = market.SpotPrices.Value.Values.First();

            double exerciseInYears;
            if (!double.IsNaN(expiryDayRemainingLife))
                exerciseInYears = expiryDayRemainingLife;
            else
                exerciseInYears = AnalyticalOptionPricerUtil.timeToMaturityFraction(market.ValuationDate, exerciseDate, trade) + timeIncrement;

            //barrier adjust
            var dt = trade.DayCount.CalcDayCountFraction(trade.ObservationDates.First(), trade.ObservationDates.Last()) /
                     trade.ObservationDates.Length;

            var riskFreeRate = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, exerciseDate);
            var riskfreeRateAtMaturity = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, maturityDate);

            var dividendCurveInput = market.DividendCurves.Value.Values.First().ZeroRate(market.ValuationDate, exerciseDate);
            var dividendInput = AnalyticalOptionPricerUtil.dividendYieldOutput(dividendCurveInput, riskFreeRate,
               option.Dividends, spot, market.ValuationDate, trade.ExerciseDates.Last(), option.DayCount);
            var dividendRate = AnalyticalOptionPricerUtil.dividenRate(trade.UnderlyingProductType, dividendInput, riskFreeRate);
      
            var vol = AnalyticalOptionPricerUtil.pricingVol(volSurf: market.VolSurfaces.Value.Values.First(),
                exerciseDate: exerciseDate, option: option, spot: spot);

            var barrierCalculator = new BarrierOptionCalculator(
                    trade.OptionType,
                    trade.BarrierType,
                    trade.Rebate,
                    trade.IsDiscreteMonitored ? DiscreteAdjustedBarrier(trade.BarrierType, trade.Position, trade.Barrier, vol, dt, trade.BarrierShift) : trade.Barrier,
                    trade.IsDiscreteMonitored ? DiscreteAdjustedBarrier(trade.BarrierType, trade.Position, trade.UpperBarrier, vol, dt, trade.BarrierShift) : trade.UpperBarrier,
                    option.IsMoneynessOption ? trade.Strike * trade.InitialSpotPrice : trade.Strike,
                    spot,
                    exerciseInYears,
                    vol,
                    riskFreeRate,
                    dividendRate,
                    valuationDate: market.ValuationDate,
                    exerciseDate: exerciseDate,
                    underlyingMaturityDate: trade.UnderlyingMaturityDate,
                    dayCount: trade.DayCount,
                    underlyingInstrumentType: trade.UnderlyingProductType,
                    notional:trade.Notional
                    );
            this._calculator = barrierCalculator;
            return barrierCalculator;
        }

        private BlackScholeCalculator configureBsCalculator(IOption option, IMarketCondition market, 
            double expiryDayRemainingLife = double.NaN, double timeIncrement = 0.0)
        {
            var trade = (BarrierOption)option;
            var spot = market.SpotPrices.Value.Values.First();
            var exerciseDate = trade.ExerciseDates.Last();
            var maturityDate = trade.UnderlyingMaturityDate;

            var vol = option.IsMoneynessOption ?
                //moneyness option , strike i.e. 120% of initialSpot
                market.VolSurfaces.Value.Values.First().GetValue(exerciseDate, trade.Strike * trade.InitialSpotPrice, spot) :
                market.VolSurfaces.Value.Values.First().GetValue(exerciseDate, trade.Strike, spot);

            var riskFreeRate = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, exerciseDate);
            var riskfreeRateAtMaturity = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, maturityDate);

            var dividendRateInput = market.DividendCurves.Value.Values.First().ZeroRate(market.ValuationDate, exerciseDate);
            var dividendRate = AnalyticalOptionPricerUtil.dividenRate(trade.UnderlyingProductType, dividendRateInput, riskFreeRate);

            return new BlackScholeCalculator(
                trade.OptionType,
                strike: option.IsMoneynessOption ? trade.Strike * trade.InitialSpotPrice : trade.Strike,
                spotPrice: spot,
                sigma: vol,
                riskfreeRateAtExercise: riskFreeRate,
                riskfreeRateAtMaturity: riskfreeRateAtMaturity,
                curveDayCount: market.DiscountCurve.Value.DayCount,
                dividendRate: dividendRate,
                maturityDate: trade.UnderlyingMaturityDate,
                exerciseDate: trade.ExerciseDates.Last(),
                valuationDate: market.ValuationDate,
                trade: trade,
                underlyingInstrumentType: trade.UnderlyingProductType,
                notional: trade.Notional,
                expiryDayRemainingLife: expiryDayRemainingLife);
         }

        protected override double CalcExpiryDelta(IOption option, IMarketCondition[] markets, double T)
        {
            var pvBase = DoCalcPv(option, markets[0], T);
            var pvUp = DoCalcPv(option, markets[1], T);
            return (pvUp - pvBase) / SpotPriceBump;
        }

        protected override double CalcPv(IOption option, IMarketCondition market, double timeIncrement = 0.0) {
            var trade = (BarrierOption)option;
            var exerciseDate = trade.ExerciseDates.Last();
            var maturityDate = trade.UnderlyingMaturityDate;
            var unitPv = DoCalcPv(option, market, timeIncrement: timeIncrement);
            return unitPv * trade.ParticipationRate;
            //+ trade.Notional * trade.Coupon * market.DiscountCurve.Value.GetDf(exerciseDate, maturityDate);
        }

        private double DoCalcPv(IOption option, IMarketCondition market, double expiryDayRemainingLife = double.NaN, double timeIncrement = 0.0)
        {
            if (!(option is BarrierOption))
            {
                throw new PricingBaseException("");
            }
            var trade = (BarrierOption)option;
            if (trade.BarrierStatus == BarrierStatus.KnockedIn)
            {
                var calculator = configureBsCalculator(option, market, timeIncrement: timeIncrement);
                //Notional has already been considered within the BScalculator
                return calculator.Pv  ;

            }
            else if (trade.BarrierStatus == BarrierStatus.KnockedOut)
            {
                return trade.Rebate * trade.Notional;
                //For KnockOut day;
            }
            else {
                var barrierCalculator = configureCalculator(option, market, timeIncrement: timeIncrement);
                return barrierCalculator.Pv;
            }
            
		}
        protected override double CalcIntrinsicValue(IOption option, IMarketCondition market)
        {
            var barrier = option as BarrierOption;
            double S = market.SpotPrices.Value.Values.First();
            double _H = barrier.Barrier;
            double _X = barrier.Strike;
            double _rebate = barrier.Rebate;
            //price here
            if (barrier.BarrierType.Equals(BarrierType.DownAndIn))
            {
                if (S <= _H)
                    return base.CalcIntrinsicValue(option, market);
                else
                    return _rebate * barrier.Notional;
            }
            else if (barrier.BarrierType.Equals(BarrierType.UpAndIn))
            {
                if (S >= _H)
                    return base.CalcIntrinsicValue(option, market);
                else
                    return _rebate * barrier.Notional;
            }               
            else if (barrier.BarrierType.Equals(BarrierType.DownAndOut) )
            {
                if (S <= _H) return _rebate * barrier.Notional; // already knocked out
                else return base.CalcIntrinsicValue(option, market);
            }
            else 
            //if (barrier.BarrierType.Equals(BarrierType.UpAndOut) )
            {
                if (S >= _H) return _rebate * barrier.Notional; // already knocked out
                else return base.CalcIntrinsicValue(option, market);
            }
          

        }
        private double DiscreteAdjustedBarrier(BarrierType barrierType, Position position, double barrier, double sigma, double dt, double barrierShift)
		{
            double longBarrier = (barrier + barrierShift) * Math.Exp(0.5826 * sigma * Math.Sqrt(dt));
            double shortBarrier = (barrier + barrierShift) * Math.Exp(-0.5826 * sigma * Math.Sqrt(dt));

            if (position == Position.Sell)
            {
                if (barrierType == BarrierType.DownAndOut || barrierType == BarrierType.UpAndIn)
                    return longBarrier;
                else return shortBarrier;
            }
            else 
            {
                if (barrierType == BarrierType.DownAndIn || barrierType == BarrierType.UpAndOut)
                    return longBarrier;
                else return shortBarrier;
            }				
		}

        //Note: stopping time does not support time-tuned option-time-value pricing mode
        protected override double StoppingTime(IOption option, IMarketCondition[] markets) {
            var calculator = configureCalculator(option, markets[0]);
            return calculator.StoppingTime();
        }

        //protected override void CalcHighOrder(IOption option, IMarketCondition market, PricingResult result )
        //{
        //    if (this._calculator == null)
        //    {
        //        configureCalculator(option, market);
        //    }
        //    var calc = (this._calculator == null) ?
        //        configureCalculator(option, market) : this._calculator;

        //    result.DDeltaDt = calc.DDeltaDt;
        //    result.DDeltaDvol = calc.DDeltaDvol;
        //    result.DVegaDvol = calc.DVegaDvol;
        //    result.DVegaDt = calc.DVegaDt;
        //}
    }

	#region
	/// <summary>
	/// Closed form barrier option price and risk calculator
	/// Reference: Espen Haug - The Complete Guide To Option Pricing Formulas (2006 2nd ed) and its workbook.
    /// Note:  model is extended to support barrier option on futures,  key assumption being futures has zero cost of carry,  based on black76 model
	/// </summary>
	internal class BarrierOptionCalculator
	{
		private double _A, _B, _C, _D, _E, _F, _Gcall, _Gput;
		private double _x1, _x2, _y1, _y2, _z, _mu, _lambda;
		private double _yita, _phi;
        private double _d1, _d2;  // to value vanilla option

        private double _S;	// spot price
		private double _X;	// strike
		private double _H;	// barrier, lower barrier in case of double barrier option
		private double _H2; // upper barrier in case of double barrier option
		private double _T;	// maturity in years
		private double _K;	// rebate, cash paid if the barrier is not hit.

		private double _r; // insterest rate
		private double _sigma; // vol
		private double _b; // cost of carray, equals interest rate - dividend

        private double _dividend;

		private double _rebate;
		private OptionType _optionType;
		private BarrierType _barrierType;
        private readonly bool _isOptionOnFutures;
        private readonly bool _isOptionOnForward;

        private readonly double _dfExerciseToMaturity;
        private readonly IDayCount _dayCount;
        private double _notional;

        /// <summary>
        /// Initializes a new instance of the <see cref="BarrierOptionCalculator"/> class.
        /// </summary>
        /// <param name="optionType">Type of the option, Call or Put.</param>
        /// <param name="barrierType">UpAndIn, DownAndOut etc.</param>
        /// <param name="payoff">Option payoff, coupon/rebate/paticipationrate included.</param>
        /// <param name="barrier">Barrier level</param>
        /// <param name="secondarybarrier">Secondary barrier, only effective for double barrier options</param>
        /// <param name="strike">The strike.</param>
        /// <param name="spotPrice">The spot price.</param>
        /// <param name="exerciseInYears">The maturity in years.</param>
        /// <param name="standardDeviation">Volatility, measured by the standard deviation of the underlying.</param>
        /// <param name="riskFreeRate">Risk free rate</param>
        /// <param name="dividendRate">Continuous dividend rate</param>
        public BarrierOptionCalculator(OptionType optionType, 
            BarrierType barrierType, 
            double rebate, 
            double barrier, 
            double secondarybarrier, 
			double strike, 
            double spotPrice,  
            double exerciseInYears, 
            double standardDeviation, 
            double riskFreeRate, 
            double dividendRate,
            Date valuationDate,
            Date exerciseDate,
            Date underlyingMaturityDate,
            IDayCount dayCount,
            InstrumentType underlyingInstrumentType,
            double notional)
		{
            _isOptionOnFutures = AnalyticalOptionPricerUtil.isFuturesOption(underlyingInstrumentType);
            _isOptionOnForward = AnalyticalOptionPricerUtil.isForwardOption(underlyingInstrumentType);

            _optionType = optionType;
			_barrierType = barrierType;
			_rebate = rebate;
			_H = barrier;
			_H2 = secondarybarrier;
			_X = strike;
			_S = spotPrice;
			_T = exerciseInYears;
			_K = rebate;
			_sigma = standardDeviation;
			_r = riskFreeRate;
            _dividend = dividendRate;
            _b = AnalyticalOptionPricerUtil.costOfCarry(_isOptionOnFutures|| _isOptionOnForward, dividendRate, riskFreeRate);
            _dayCount = dayCount;
            _notional = notional;

            var riskfreeDfAtMaturity = CalcDfFromZeroRate(riskFreeRate, underlyingMaturityDate, valuationDate);
            var riskfreeDfAtExercise = CalcDfFromZeroRate(riskFreeRate, exerciseDate, valuationDate);
            _dfExerciseToMaturity = (_isOptionOnForward) ? riskfreeDfAtMaturity / riskfreeDfAtExercise : 1.0;

            // factors calculation
            switch (barrierType)
			{
				case BarrierType.DownAndIn:
				case BarrierType.DownAndOut:
					_yita = 1.0;
					break;
				case BarrierType.UpAndIn:
				case BarrierType.UpAndOut:
					_yita = -1.0;
					break;
				case BarrierType.DoubleTouchOut:
				case BarrierType.DoubleTouchIn:
					throw new PricingLibraryException("Double barrier shall use AnalyticalDoubleBarrierOptionEngine to calculate!");
			}

			_phi = optionType.Equals(OptionType.Call) ? 1.0 : -1.0;

        }

        private double CalcDfFromZeroRate(double zeroRate, Date date, Date valuationDate)
        {
            return Compound.Continuous.CalcDfFromZeroRate(zeroRate, _dayCount.CalcDayCountFraction(valuationDate, date));
        }

        private double CalcPV(Double S, Double sigma, Double T, Double r)
        {
            //TODO:  get risk free rate from curve,  here
            var b = _isOptionOnFutures ? 0.0 : r - _dividend;
            _mu = (b - sigma * sigma / 2.0) / Math.Pow(sigma, 2.0);
            _lambda = _isOptionOnFutures ?
                Math.Sqrt(_mu * _mu) :
                Math.Sqrt(_mu * _mu + 2.0 * r / sigma / sigma);

            _z = Math.Log(_H / S) / sigma / Math.Sqrt(T) + _lambda * sigma * Math.Sqrt(T);

            _x1 = Math.Log(S / _X) / sigma / Math.Sqrt(T) + (1 + _mu) * sigma * Math.Sqrt(T);
            _x2 = Math.Log(S / _H) / sigma / Math.Sqrt(T) + (1 + _mu) * sigma * Math.Sqrt(T);
            _y1 = Math.Log(_H * _H / S / _X) / sigma / Math.Sqrt(T) + (1 + _mu) * sigma * Math.Sqrt(T);
            _y2 = Math.Log(_H / S) / sigma / Math.Sqrt(T) + (1 + _mu) * sigma * Math.Sqrt(T);

            _d1 = _isOptionOnFutures ?
                (Math.Log(S / _X) + 0.5 * Math.Pow(sigma, 2.0) * T) / (sigma * Math.Sqrt(T)) :
                (Math.Log(S / _X) + (b + 0.5 * Math.Pow(sigma, 2.0)) * T) / (sigma * Math.Sqrt(T));  //black scholes

            _d2 = _d1 - sigma * Math.Sqrt(T);

            //Note: given yield curve infra, we could
            //1. replace Math.Exp((b - r) * T)  with  market.DividendCurve.Value.GetDf(T)
            //2. replace Math.Exp(( -r) * T)  with  market.DiscountCurve.Value.GetDf(T)
            _A = _phi * S * Math.Exp((b - r) * T) * NormalCdf.NormalCdfHart(_phi * _x1) -
                     _phi * _X * Math.Exp(-r * T) * NormalCdf.NormalCdfHart(_phi * _x1 - _phi * sigma * Math.Sqrt(T));
            _B = _phi * S * Math.Exp((b - r) * T) * NormalCdf.NormalCdfHart(_phi * _x2) -
                     _phi * _X * Math.Exp(-r * T) * NormalCdf.NormalCdfHart(_phi * _x2 - _phi * sigma * Math.Sqrt(T));
            _C = _phi * S * Math.Exp((b - r) * T) * Math.Pow(_H / S, 2 * (_mu + 1)) * NormalCdf.NormalCdfHart(_yita * _y1) -
                     _phi * _X * Math.Exp(-r * T) * Math.Pow(_H / S, 2 * _mu) * NormalCdf.NormalCdfHart(_yita * _y1 - _yita * sigma * Math.Sqrt(T));
            _D = _phi * S * Math.Exp((b - r) * T) * Math.Pow(_H / S, 2 * (_mu + 1)) * NormalCdf.NormalCdfHart(_yita * _y2) -
                     _phi * _X * Math.Exp(-r * T) * Math.Pow(_H / S, 2 * _mu) * NormalCdf.NormalCdfHart(_yita * _y2 - _yita * sigma * Math.Sqrt(T));
            _E = _K * Math.Exp(-r * T) *
                 (NormalCdf.NormalCdfHart(_yita * _x2 - _yita * sigma * Math.Sqrt(T)) -
                  Math.Pow(_H / S, 2 * _mu) * NormalCdf.NormalCdfHart(_yita * _y2 - _yita * sigma * Math.Sqrt(T)));
            _F = _K *
                 (Math.Pow(_H / S, _mu + _lambda) * NormalCdf.NormalCdfHart(_yita * _z) +
                  Math.Pow(_H / S, _mu - _lambda) * NormalCdf.NormalCdfHart(_yita * _z - 2 * _yita * _lambda * sigma * Math.Sqrt(T)));

            _Gcall = Math.Exp(-r * T) * (S * Math.Exp(b * T) * NormalCdf.NormalCdfHart(_d1) - _X * NormalCdf.NormalCdfHart(_d2));
            _Gput = -1.0 * Math.Exp(-r * T) * (S * Math.Exp(b * T) * NormalCdf.NormalCdfHart(-_d1) - _X * NormalCdf.NormalCdfHart(-_d2));

            //price here
            if (_barrierType.Equals(BarrierType.DownAndIn) && _optionType.Equals(OptionType.Call))
            {
                if (S <= _H)
                    return _notional* _Gcall;
                if (_X >= _H)
                    return _notional * (_C + _E);
                if (_X < _H)
                    return _notional *( _A - _B + _D + _E);
            }
            if (_barrierType.Equals(BarrierType.UpAndIn) && _optionType.Equals(OptionType.Call))
            {
                if (S >= _H)
                    return _notional * _Gcall;
                if (_X > _H)
                    return _notional *( _A + _E);
                if (_X <= _H)
                    return _notional *( _B - _C + _D + _E);
            }
            if (_barrierType.Equals(BarrierType.DownAndIn) && _optionType.Equals(OptionType.Put))
            {
                if (S <= _H)
                    return _notional * _Gput;
                if (_X >= _H)
                    return _notional *( _B - _C + _D + _E);
                if (_X < _H)
                    return _notional *( _A + _E);
            }
            if (_barrierType.Equals(BarrierType.UpAndIn) && _optionType.Equals(OptionType.Put))
            {
                if (S >= _H)
                    return _notional * _Gput;
                if (_X > _H)
                    return _notional *( _A - _B + _D + _E);
                if (_X <= _H)
                    return _notional *( _C + _E);
            }
            if (_barrierType.Equals(BarrierType.DownAndOut) && _optionType.Equals(OptionType.Call))
            {
                if (S <= _H) return _notional * _rebate; // already knocked out
                if (_X >= _H)
                    return _notional * (_A - _C + _F);
                if (_X < _H)
                    return _notional *( _B - _D + _F);
            }
            if (_barrierType.Equals(BarrierType.UpAndOut) && _optionType.Equals(OptionType.Call))
            {
                if (S >= _H) return _notional * _rebate; // already knocked out
                if (_X > _H)
                    return _notional * _F;
                if (_X <= _H)
                    return _notional *( _A - _B + _C - _D + _F);
            }
            if (_barrierType.Equals(BarrierType.DownAndOut) && _optionType.Equals(OptionType.Put))
            {
                if (S <= _H) return _notional * _rebate; // already knocked out
                if (_X >= _H)
                    return _notional * (_A - _B + _C - _D + _F);
                if (_X < _H)
                    return _notional * _F;
            }
            if (_barrierType.Equals(BarrierType.UpAndOut) && _optionType.Equals(OptionType.Put))
            {
                if (S >= _H)  return _notional * _rebate; // already knocked out
                if (_X > _H)
                    return _notional *( _B - _D + _F);
                if (_X <= _H)
                    return _notional *( _A - _C + _F);
            }

            throw new PricingBaseException("Code should not reach here. Something is wrong.");

        }

        /// <summary>
        /// Values
        /// </summary>
        /// <returns></returns>
        public double Pv => CalcPV(_S, _sigma, _T, _r);

        //private const double riskBumpSize = 1e-4;
        //private const double vegaBumpSize = 0.01;
        ////TODO: properly fix it,  increment 1 day, using daycount convention
        //private const double timeIncrement = 1.0 / 365.0;

        //private double CalDelta(double spot, double T, double sigma) => (CalcPV( spot + riskBumpSize, sigma, T, _r) - CalcPV(spot, sigma, T, _r))/ riskBumpSize;

        ////To avoid confusion, current barrier greek calc is handled in BaseNumericalOptionEngine
        ////public double Delta => CalDelta(_S, _T, _sigma);

        ////public double Gamma => CalDelta(_S + riskBumpSize, _T, _sigma) - CalDelta(_S, _T, _sigma);

        ////pv change per 0.01 absolute change in vol,  unlike other greeks, this number is not standardized
        ////public double Vega => CalcVega(_S, _T, _sigma); //CalcPV(_S, _sigma + vegaBumpSize, _T, _r) - CalcPV(_S, _sigma, _T, _r);

        //public double CalcVega(double spot, double T, double sigma) => CalcPV(spot, sigma + vegaBumpSize, T, _r) - CalcPV(spot, sigma, T, _r);
        
        //public double Theta => CalcPV(_S, _sigma, _T - timeIncrement, _r) - CalcPV(_S, _sigma, _T, _r);

        ////pv change per 1 bp absolute change in r
        //public double Rho => CalcPV(_S, _sigma, _T, _r + riskBumpSize) - CalcPV(_S, _sigma, _T, _r);

        ////higher order, cross effect
        //public double DDeltaDt => CalDelta(_S, _T- timeIncrement, _sigma) - CalDelta(_S, _T, _sigma);

        //public double DDeltaDvol => CalDelta(_S, _T, _sigma + 0.01) - CalDelta(_S, _T, _sigma);

        //public double DVegaDt => CalcVega(_S, _T - timeIncrement, _sigma) - CalcVega(_S, _T, _sigma);

        //public double DVegaDvol => (CalcVega(_S, _T, _sigma+vegaBumpSize) - CalcVega(_S, _T, _sigma));

        //expected barrier first exit time/barrier cross time
        public double StoppingTime()
        {   //From Dynamic hedging,  module G
            double lambda = _b / _sigma - _sigma / 2.0;
            double h = 1.0 / _sigma * Math.Log(_H / _S);

            Func<bool, double> calcStoppingTimeDistribution = barrierHigherThanSpot => 
            {
                var sign = barrierHigherThanSpot ? 1.0 : -1.0;
                return (_T - h / lambda) * NormalCdf.NormalCdfHart( (h / Math.Sqrt(_T) - lambda * Math.Sqrt(_T))* sign )
                - Math.Exp(2.0 * lambda * h) * (_T + h / lambda) * NormalCdf.NormalCdfHart(  (-h / Math.Sqrt(_T) - lambda * Math.Sqrt(_T))* sign);
            };
            var tEffectivBarrier = h / lambda + calcStoppingTimeDistribution(_H > _S);
            var tVanilla = _T;
            var tKnockedOut = 0;

            if (_barrierType.Equals(BarrierType.DownAndIn) && _S <= _H)
                return tVanilla;
            else if (_barrierType.Equals(BarrierType.UpAndIn) && _S >= _H)
                return tVanilla;
            else if (_barrierType.Equals(BarrierType.DownAndOut) && _S <= _H)
                return tKnockedOut;
            else if (_barrierType.Equals(BarrierType.UpAndOut) && _S >= _H)
                return tKnockedOut;
            else 
                return tEffectivBarrier;
        }
    #endregion
    }

}
