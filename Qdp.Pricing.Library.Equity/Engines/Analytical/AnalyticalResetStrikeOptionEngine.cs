using System;
using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.Pricing.Library.Common.MathMethods.Maths;

namespace Qdp.Pricing.Library.Equity.Engines.Analytical
{

    public class AnalyticalResetStrikeOptionEngine : Engine<ResetStrikeOption>
    {
        public override IPricingResult Calculate(ResetStrikeOption trade, IMarketCondition market, PricingRequest request)
        {
            var result = new PricingResult(market.ValuationDate, request);

            var exerciseDate = trade.ExerciseDates.Last();
            var strikefixingDate = trade.StrikeFixingDate;
            var spot = market.SpotPrices.Value.Values.First();
            var sigma = AnalyticalOptionPricerUtil.pricingVol(volSurf: market.VolSurfaces.Value.Values.First(),
                exerciseDate: exerciseDate, option: trade, spot: spot);
            var T = AnalyticalOptionPricerUtil.timeToMaturityFraction(market.ValuationDate, exerciseDate, trade);
            var t = AnalyticalOptionPricerUtil.timeToMaturityFraction(market.ValuationDate, strikefixingDate, trade);

            var riskFreeRate = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, exerciseDate);
            var dividendRateInput = market.DividendCurves.Value.Values.First().ZeroRate(market.ValuationDate, exerciseDate);
            var dividendRate = AnalyticalOptionPricerUtil.dividenRate(trade.UnderlyingProductType, dividendRateInput, riskFreeRate);
            

            var calculator = new ResetStrikeOptionCalculator(trade.OptionType, trade.ResetStrikeType, trade.UnderlyingProductType,
                trade.Strike,
                market.SpotPrices.Value.Values.First(),
                T,
                t,
                sigma,
                riskFreeRate,
                dividendRate,
                trade.Notional);

            result.Pv = calculator.Pv;
            result.Delta = calculator.Delta;
            result.DeltaCash = result.Delta * market.SpotPrices.Value.Values.First();
            result.Gamma = calculator.Gamma;
            result.GammaCash = result.Gamma * market.SpotPrices.Value.Values.First() * market.SpotPrices.Value.Values.First() / 100;
            result.Vega = calculator.Vega;
            result.Rho = calculator.Rho;
            result.Theta = calculator.Theta;
            result.DDeltaDt = calculator.DDeltaDt;
            result.DDeltaDvol = calculator.DDeltaDvol;
            result.DVegaDvol = calculator.DDeltaDvol;
            result.DVegaDt = calculator.DVegaDt;

            return result;
        }

        internal class ResetStrikeOptionCalculator
        {
            private OptionType _optionType;
            private ResetStrikeType _resetstrikeType;
            private double _S, _T, _t, _X, _sigma, _r, _dividendRate, _b, _notional;

            private InstrumentType[] FuturesProducts = { InstrumentType.Futures, InstrumentType.BondFutures, InstrumentType.CommodityFutures };
            public ResetStrikeOptionCalculator(
                OptionType optionType, ResetStrikeType resetstrikeType, InstrumentType underlyingProductType,
                double strike, double spotPrice,
                double exerciseInYears, double strikefixingInYears, 
                double sigma, double riskFreeRate, double dividendRate, double notional)
            {
                _optionType = optionType;
                _resetstrikeType = resetstrikeType;
                _X = strike;
                _S = spotPrice;
                _T = exerciseInYears;
                _t = strikefixingInYears;
                _sigma = sigma;
                _r = riskFreeRate;
                _dividendRate = dividendRate;

                if (FuturesProducts.Contains(underlyingProductType))
                    _b = 0.0;
                else
                    _b = riskFreeRate - dividendRate;

                _notional = notional;

            }
            //Haug 2007: Chapter 4.9 & 4.10
            //Payoff: max((S-X)/X,0) or max(S-X,0)
            private double CalcPV(double S, double T, double r, double b, double sigma, double t)
            {
                double rho = Math.Sqrt(t / T);
                double a1 = (Math.Log(S / _X) + (b + sigma * sigma * 0.5) * t) / sigma / Math.Sqrt(t);
                double a2 = a1 - sigma * Math.Sqrt(t);
                double z1 = (b + sigma * sigma * 0.5) * (T - t) / sigma / Math.Sqrt(T - t);
                double z2 = z1 - sigma * Math.Sqrt(T - t);
                double y1 = (Math.Log(S / _X) + (b + sigma * sigma * 0.5) * T) / sigma / Math.Sqrt(T);
                double y2 = y1 - sigma * Math.Sqrt(T);

                double C1 = Math.Exp((b - r) * (T - t)) * NormalCdf.NormalCdfHart(-a2) * NormalCdf.NormalCdfHart(z1) * Math.Exp(-r * t)
                    - Math.Exp(-r * T) * NormalCdf.NormalCdfHart(-a2) * NormalCdf.NormalCdfHart(z2) - Math.Exp(-r * T) * NormalCdf.NormalCdfGenz(a2, y2, rho)
                    + (S / _X) * Math.Exp((b - r) * T) * NormalCdf.NormalCdfGenz(a1, y1, rho);

                double P1 = -Math.Exp((b - r) * (T - t)) * NormalCdf.NormalCdfHart(a2) * NormalCdf.NormalCdfHart(-z1) * Math.Exp(-r * t)
                    + Math.Exp(-r * T) * NormalCdf.NormalCdfHart(a2) * NormalCdf.NormalCdfHart(-z2) + Math.Exp(-r * T) * NormalCdf.NormalCdfGenz(-a2, -y2, rho)
                    - (S / _X) * Math.Exp((b - r) * T) * NormalCdf.NormalCdfGenz(-a1, -y1, rho);

                double C2 = S * Math.Exp((b - r) * T) * NormalCdf.NormalCdfGenz(a1, y1, rho) - _X * Math.Exp(-r * T) * NormalCdf.NormalCdfGenz(a2, y2, rho)
                    - S * Math.Exp((b - r) * t) * NormalCdf.NormalCdfHart(-a1) * NormalCdf.NormalCdfHart(z2) * Math.Exp(-r * (T - t)) + S * Math.Exp((b - r) * T) * NormalCdf.NormalCdfHart(-a1) * NormalCdf.NormalCdfHart(z1);

                double P2 = -S * Math.Exp((b - r) * T) * NormalCdf.NormalCdfGenz(-a1, -y1, rho) + _X * Math.Exp(-r * T) * NormalCdf.NormalCdfGenz(-a2, -y2, rho)
                    + S * Math.Exp((b - r) * t) * NormalCdf.NormalCdfHart(a1) * NormalCdf.NormalCdfHart(-z2) * Math.Exp(-r * (T - t)) - S * Math.Exp((b - r) * T) * NormalCdf.NormalCdfHart(a1) * NormalCdf.NormalCdfHart(-z1);

                switch (_resetstrikeType)
                {

                    case ResetStrikeType.PercentagePayoff:
                        return (_optionType == OptionType.Call) ? _notional * C1 : _notional * P1;
                    case ResetStrikeType.NormalPayoff:
                        return (_optionType == OptionType.Call) ? _notional * C2 : _notional * P2;
                    default:
                        throw new PricingLibraryException("Unsupported reset strike option type");
                }
            }



        
            public double Pv => CalcPV(_S, _T, _r, _b, _sigma,_t);

            private const double riskBumpSize = 1e-4;
            private const double vegaBumpSize = 0.01;
            private const double timeIncrement = 1.0 / 365.0;

            private double CalDelta(double spot, double T, double sigma, double t) => (CalcPV(spot + riskBumpSize, _T, _r, _b, _sigma, _t) - Pv) / riskBumpSize;

            public double Delta => CalDelta(_S, _T, _sigma, _t);

            public double Gamma => (CalcPV(_S + riskBumpSize, _T, _r, _b, _sigma, _t) - 2 * Pv + CalcPV(_S - riskBumpSize, _T, _r, _b, _sigma, _t)) / riskBumpSize / riskBumpSize;

            //TODO: properly fix it, pass in actual day
            public double Theta => CalcPV(_S, _T - timeIncrement, _r, _b, _sigma, _t - timeIncrement) - Pv;

            //pv change per 0.01 absolute change in vol
            public double Vega => CalcVega(_S, _T, _sigma, _t);

            private double CalcVega(double spot, double T, double sigma, double t) => CalcPV(spot, T, _r, _b, sigma + vegaBumpSize, t) - CalcPV(spot, T, _r, _b, sigma, t);

            //pv change per 1 bp absolute change in r
            public double Rho => CalcPV(_S, _T, _r + riskBumpSize, _b, _sigma, _t) - Pv;

            //higher order, cross effect
            public double DDeltaDt => CalDelta(_S, _T - timeIncrement, _sigma, _t - timeIncrement) - CalDelta(_S, _T, _sigma, _t);

            public double DDeltaDvol => CalDelta(_S, _T, _sigma + vegaBumpSize, _t) - CalDelta(_S, _T, _sigma, _t);

            public double DVegaDt => CalcVega(_S, _T - timeIncrement, _sigma, _t - timeIncrement) - CalcVega(_S, _T, _sigma, _t);

            public double DVegaDvol => (CalcVega(_S, _T, _sigma + vegaBumpSize, _t) - CalcVega(_S, _T, _sigma, _t));

        }

    }
}
