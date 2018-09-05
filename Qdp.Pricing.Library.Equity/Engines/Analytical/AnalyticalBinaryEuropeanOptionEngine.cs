using System;
using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Library.Equity.Interfaces;
using Qdp.Pricing.Base.Interfaces;

namespace Qdp.Pricing.Library.Equity.Engines.Analytical
{
	public class AnalyticalBinaryEuropeanOptionEngine : BaseNumericalOptionEngine
    {
        private BlackScholeCalculator _calculator = null;

        private BlackScholeCalculator ConfigureCalculator(IOption option, IMarketCondition market, 
            double expiryDayRemainingLife = double.NaN, double timeIncrement = 0.0)
        {
            var trade = (BinaryOption)option;

            var exerciseDate = trade.ExerciseDates.Last();
            var maturityDate = trade.UnderlyingMaturityDate;

            var riskFreeRate = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, exerciseDate);
            var riskfreeDfAtMaturity = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, maturityDate);
            var riskfreeDfAtExercise = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, exerciseDate);
            var spot = market.SpotPrices.Value.Values.First();
            double sigma = AnalyticalOptionPricerUtil.pricingVol(volSurf: market.VolSurfaces.Value.Values.First(),
                exerciseDate: exerciseDate, option: option, spot: spot);

            var dividendCurveInput = market.DividendCurves.Value.Values.First().ZeroRate(market.ValuationDate, exerciseDate);
            var dividendInput = AnalyticalOptionPricerUtil.dividendYieldOutput(dividendCurveInput, riskFreeRate,
                option.Dividends, spot, market.ValuationDate, trade.ExerciseDates.Last(), option.DayCount);

            var dividendRate = AnalyticalOptionPricerUtil.dividenRate(trade.UnderlyingProductType, dividendInput, riskfreeDfAtExercise);

            var exerciseInYears = AnalyticalOptionPricerUtil.timeToMaturityFraction(market.ValuationDate, exerciseDate, trade) + timeIncrement;

            var maturityInYears = AnalyticalOptionPricerUtil.timeToMaturityFraction(market.ValuationDate, maturityDate, trade) + timeIncrement;

            var _isDelayedPay = exerciseInYears.AlmostEqual(maturityInYears);

            var calculator = new BlackScholeCalculator(trade.OptionType,
                trade.BinaryOptionPayoffType,
                strike: option.IsMoneynessOption ? trade.Strike * trade.InitialSpotPrice : trade.Strike,
                spotPrice: spot,
                sigma: sigma,
                dividendRate: dividendRate,
                riskfreeRateAtExercise: riskfreeDfAtExercise,
                riskfreeRateAtMaturity: riskfreeDfAtMaturity,
                curveDayCount: market.DiscountCurve.Value.DayCount,
                cashOrNothingAmount: trade.CashOrNothingAmount,
                exerciseDate: exerciseDate,
                maturityDate: maturityDate,
                valuationDate: market.ValuationDate,
                trade: trade,
                underlyingInstrumentType: trade.UnderlyingProductType,
                notional: trade.Notional,
                isDelayedPay: _isDelayedPay,
                expiryDayRemainingLife: expiryDayRemainingLife,
                timeIncrement: timeIncrement);

            this._calculator = calculator;
            return calculator;
        }

        protected override double CalcIntrinsicValue(IOption option, IMarketCondition market)
        {
            var binary = option as BinaryOption;
            double spot = market.SpotPrices.Value.Values.First();
            if (binary.BinaryOptionPayoffType == BinaryOptionPayoffType.AssetOrNothing)
            {
                if (option.OptionType == OptionType.Call)
                    return Math.Max(market.SpotPrices.Value.Values.First() - option.Strike,0) * option.Notional;
                else
                    return Math.Max(option.Strike - market.SpotPrices.Value.Values.First(),0) * option.Notional;
            }
            else
            {
                if (option.OptionType == OptionType.Call)
                    if (spot > binary.Strike)
                        return binary.CashOrNothingAmount * option.Notional;
                    else return 0.0;
                else
                    if (spot < binary.Strike)
                    return binary.CashOrNothingAmount * option.Notional;
                     else return 0.0; ;

            }
           
        }

        protected override double CalcPv(IOption option, IMarketCondition market, double timeIncrement = 0.0)
        {
            if (!(option is BinaryOption))
            {
                throw new PricingBaseException("");
            }
            var trade = (BinaryOption)option;
            var Calculator = ConfigureCalculator(option, market, timeIncrement: timeIncrement);
            var pv = Calculator.Pv;
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
		#region calculator
		class BlackScholeCalculator
		{
            #region internal fileds
            private readonly OptionType _optionType;
			private readonly BinaryOptionPayoffType _binaryOptionPayoffType;
			private double _spotPrice;
			private double _strike;
			private double _sigma;
			private double _rfDfExerciseToMaturity;
			private double _cashOrNothingAmount;
			private double _notional;
			private bool _isDelayedPay;

            private readonly bool _isOptionOnForward;
            private readonly bool _isOptionOnFutures;

            private readonly Date _valuationDate, _exerciseDate, _maturityDate;
            private readonly IOption _trade;
            private readonly InstrumentType _underlyingInstrumentType;
            private double _riskfreeRateAtExercise, _riskfreeRateAtMaturity;
            private double _dividendRate; //dividendRate
            #endregion internal fileds

            private readonly double _expiryDayRemainingLife;
            private readonly double _timeIncrement;
            private readonly IDayCount _curveDayCount;

            public BlackScholeCalculator(OptionType optionType,
				BinaryOptionPayoffType binaryOptionPayoffType,
				double strike,
				double spotPrice,
				double sigma,
				double dividendRate,
                double riskfreeRateAtExercise,
                double riskfreeRateAtMaturity,
                IDayCount curveDayCount,
                double cashOrNothingAmount,
                Date exerciseDate,
                Date maturityDate,
                Date valuationDate,
                IOption trade,
                InstrumentType underlyingInstrumentType,
				double notional = 1.0,
                bool isDelayedPay = false,
                double expiryDayRemainingLife = double.NaN,
                double timeIncrement = 0.0)
			{
                _underlyingInstrumentType = underlyingInstrumentType;
                _isOptionOnFutures = AnalyticalOptionPricerUtil.isFuturesOption(underlyingInstrumentType);
                _isOptionOnForward = AnalyticalOptionPricerUtil.isForwardOption(underlyingInstrumentType);

                _optionType = optionType;
				_binaryOptionPayoffType = binaryOptionPayoffType;
				_strike = strike;
				_spotPrice = spotPrice;
				_notional = notional;
				_cashOrNothingAmount = cashOrNothingAmount;
                _sigma = sigma;

                _dividendRate = dividendRate;
                _riskfreeRateAtExercise = riskfreeRateAtExercise;
                _riskfreeRateAtMaturity = riskfreeRateAtMaturity;

                _exerciseDate = exerciseDate;
                _maturityDate = maturityDate;
                _valuationDate = valuationDate;

                _trade = trade;
                
                _isDelayedPay = isDelayedPay;
                _rfDfExerciseToMaturity = riskfreeRateAtMaturity / riskfreeRateAtExercise;

                _expiryDayRemainingLife = expiryDayRemainingLife;
                _timeIncrement = timeIncrement;
                _curveDayCount = curveDayCount;
            }

            //public double CalcPV(Double spotPrice, Double sigma, Double T, Double r)
            public double CalcPV(Double spotPrice, Double sigma, Date valuationDate, double riskFreeCurveShiftInBp = 0.0, double dividendCurveShiftInBp = 0.0)
            {
                AnalyticalOptionPricerUtil.prepareBlackScholesInputs(
                spotPrice: spotPrice,
                riskfreeRateAtExerciseInput: _riskfreeRateAtExercise,
                riskfreeRateAtMaturityInput: _riskfreeRateAtMaturity,
                dividendRateInput: _dividendRate,
                riskFreeCurveShiftInBp: riskFreeCurveShiftInBp,
                dividendCurveShiftInBp: dividendCurveShiftInBp,
                curveDayCount: _curveDayCount,
                maturityDate: _maturityDate,
                exerciseDate: _exerciseDate,
                valuationDate: valuationDate,
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
                timeIncrement : _timeIncrement);

                if (_binaryOptionPayoffType == BinaryOptionPayoffType.AssetOrNothing)
					{
						return _optionType == OptionType.Call
							? _notional * dfExerciseToMaturity * riskfreeDfAtExercise * forwardPrice * nd1
							: _notional * dfExerciseToMaturity * riskfreeDfAtExercise * (1 - nd1) * forwardPrice;
					}
				else
					{
						return _optionType == OptionType.Call
							? _notional * dfExerciseToMaturity * riskfreeDfAtExercise * nd2 * _cashOrNothingAmount
							: _notional * dfExerciseToMaturity * riskfreeDfAtExercise * (1 - nd2) * _cashOrNothingAmount;
					}
				
			}

            public double Pv => CalcPV(_spotPrice, _sigma, _valuationDate); //, _r);

            //private const double riskBumpSize = 1e-4;
            //private const double vegaBumpSize = 0.01;
            //private const double timeIncrement = 1.0 / 365.0;

            //private double CalDelta(double spot, Date valuationDate, double sigma) => (CalcPV(spot + riskBumpSize, sigma, valuationDate) - CalcPV(spot, sigma, valuationDate)) / riskBumpSize;

            //public double Delta => CalDelta(_spotPrice, _valuationDate, _sigma);

            //public double Gamma => (CalDelta(_spotPrice + riskBumpSize, _valuationDate, _sigma) - CalDelta(_spotPrice, _valuationDate, _sigma)) / riskBumpSize;

            ////pv change per 0.01 absolute change in vol,  unlike other greeks, this number is not standardized
            //public double CalcVega(double spot, Date valuationDate, double sigma) => CalcPV(spot, sigma + vegaBumpSize, valuationDate) - CalcPV(spot, sigma, valuationDate);
            //public double Vega => CalcVega(_spotPrice, _valuationDate, _sigma);

            //public double Theta => CalcPV(_spotPrice, _sigma, _valuationDate.AddDays(1)) - CalcPV(_spotPrice, _sigma, _valuationDate);

            ////pv change per 1 bp absolute change in r
            //public double Rho => CalcPV(_spotPrice, _sigma, _valuationDate, riskFreeCurveShiftInBp: 1) - CalcPV(_spotPrice, _sigma, _valuationDate);

            ////higher order, cross effect
            //public double DDeltaDt => CalDelta(_spotPrice, _valuationDate.AddDays(1), _sigma) - CalDelta(_spotPrice, _valuationDate, _sigma);

            //public double DDeltaDvol => CalDelta(_spotPrice, _valuationDate, _sigma + vegaBumpSize) - CalDelta(_spotPrice, _valuationDate, _sigma);

            //public double DVegaDt => CalcVega(_spotPrice, _valuationDate.AddDays(1), _sigma) - CalcVega(_spotPrice, _valuationDate, _sigma);

            //public double DVegaDvol => (CalcVega(_spotPrice, _valuationDate, _sigma + vegaBumpSize) - CalcVega(_spotPrice, _valuationDate, _sigma));
 

            
		#endregion
	}

}
}
