using System;
using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.Pricing.Library.Equity.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.Maths;

namespace Qdp.Pricing.Library.Equity.Engines.Analytical
{
    public class AnalyticalBinaryAmericanOptionEngine : BaseNumericalOptionEngine
    {
        private OneTouchCalculator _calculator = null;

        private OneTouchCalculator ConfigureCalculator(IOption option, IMarketCondition market, double timeIncrement = 0.0)
        {
            var trade = (BinaryOption)option;

            var exerciseDate = trade.ExerciseDates.Last();
            var maturityDate = trade.UnderlyingMaturityDate;

            var riskFreeRate = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, exerciseDate);
            var riskfreeDfAtExercise = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, exerciseDate);
            var spot = market.SpotPrices.Value.Values.First();
            double sigma = AnalyticalOptionPricerUtil.pricingVol(volSurf: market.VolSurfaces.Value.Values.First(),
                exerciseDate: exerciseDate, option: option, spot: spot);

            var dividendCurveInput = market.DividendCurves.Value.Values.First().ZeroRate(market.ValuationDate, exerciseDate);
            var dividendInput = AnalyticalOptionPricerUtil.dividendYieldOutput(dividendCurveInput, riskFreeRate,
               option.Dividends, spot, market.ValuationDate, trade.ExerciseDates.Last(), option.DayCount);
            var dividendRate = AnalyticalOptionPricerUtil.dividenRate(trade.UnderlyingProductType, dividendInput, riskfreeDfAtExercise);

            var exerciseInYears = AnalyticalOptionPricerUtil.timeToMaturityFraction(market.ValuationDate, exerciseDate, trade) + timeIncrement;

            var calculator = new OneTouchCalculator(
                trade.BinaryRebateType,
                strike: option.IsMoneynessOption ? trade.Strike * trade.InitialSpotPrice : trade.Strike,
                spotPrice: spot,
                sigma: sigma,
                dividendRate: dividendRate,
                riskFreeRate: riskfreeDfAtExercise,
                cashOrNothingAmount: trade.CashOrNothingAmount,
                exerciseInYears: exerciseInYears,
                underlyingInstrumentType: trade.UnderlyingProductType,
                notional: trade.Notional);

            this._calculator = calculator;
            return calculator;
        }

        protected override double CalcIntrinsicValue(IOption option, IMarketCondition market)
        {
            var binary = option as BinaryOption;
            double spot = market.SpotPrices.Value.Values.First();
            double strike = option.IsMoneynessOption ? binary.Strike * binary.InitialSpotPrice : binary.Strike;
            if (spot == strike) return binary.CashOrNothingAmount * option.Notional;
            else return 0.0;
        }

        protected override double CalcPv(IOption option, IMarketCondition market, double timeIncrement = 0.0)
        {
            if (!(option is BinaryOption))
            {
                throw new PricingBaseException("");
            }
            var trade = (BinaryOption)option;

            if (trade.BinaryOptionPayoffType == BinaryOptionPayoffType.AssetOrNothing)
            {
                throw new PricingBaseException("");
            }

            var Calculator = ConfigureCalculator(option, market, timeIncrement: timeIncrement);
            var pv = Calculator.Pv;
            return pv;
        }
        #region calculator
        class OneTouchCalculator
        {
            private double _S, _T, _sigma, _r, _notional, _dividendRate, _cashOrNothingAmount, _X, _b;
            private BinaryRebateType _binaryRebateType;
            private readonly bool _isOptionOnFutures;
            private readonly bool _isOptionOnForward;
            public OneTouchCalculator(
                BinaryRebateType binaryRebateType,
                double strike,
                double spotPrice,
                double sigma,
                double dividendRate,
                double riskFreeRate,
                double cashOrNothingAmount,
                double exerciseInYears,
                InstrumentType underlyingInstrumentType,
                double notional = 1.0)
            {
                _X = strike;
                _S = spotPrice;
                _notional = notional;
                _cashOrNothingAmount = cashOrNothingAmount;
                _sigma = sigma;
                _T = exerciseInYears;
                _binaryRebateType = binaryRebateType;

                _r = riskFreeRate;
                _dividendRate = dividendRate;
                _isOptionOnFutures = AnalyticalOptionPricerUtil.isFuturesOption(underlyingInstrumentType);
                _isOptionOnForward = AnalyticalOptionPricerUtil.isForwardOption(underlyingInstrumentType);
                _b = AnalyticalOptionPricerUtil.costOfCarry(_isOptionOnFutures || _isOptionOnForward, dividendRate, riskFreeRate);

            }

            //public double CalcPV(Double spotPrice, Double sigma, Double T, Double r)
            public double CalcPV(double S, double T, double sigma, double r, double b)
            {
                double omega = _binaryRebateType== BinaryRebateType.AtHit ? 0 : 1;
                double yita = S < _X ? -1 : 1;

                double theta = b / sigma - sigma / 2;
                double nu = Math.Sqrt(theta * theta + 2 * (1 - omega) * r);
                double epsilon1 = (Math.Log(S / _X) - sigma * nu * T) / sigma / Math.Sqrt(T);
                double epsilon2 = (-Math.Log(S / _X) - sigma * nu * T) / sigma / Math.Sqrt(T);

                double pv = _cashOrNothingAmount * Math.Exp(-omega * r * T) * (Math.Pow((_X / S), (theta + nu) / sigma) * NormalCdf.NormalCdfHart(-yita * epsilon1)
                    + Math.Pow((_X / S), (theta - nu) / sigma) * NormalCdf.NormalCdfHart(yita * epsilon2));
                return pv * _notional;

            }

            public double Pv => CalcPV(_S, _T, _sigma, _r, _b);

           
            #endregion
        }

    }
}

