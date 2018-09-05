using System;
using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.Pricing.Library.Common.MathMethods.Maths;
using Qdp.Pricing.Library.Base.Utilities;

namespace Qdp.Pricing.Library.Equity.Engines.Analytical
{
    public class AnalyticalRainbowOptionEngine : Engine<RainbowOption>
    {
        public override IPricingResult Calculate(RainbowOption trade, IMarketCondition market, PricingRequest request)
        {
            var result = new PricingResult(market.ValuationDate, request);

            var exerciseDate = trade.ExerciseDates.Last();
            var maturityDate = trade.UnderlyingMaturityDate;

            //TODO:  support timeIncrement
            var exerciseInYears = AnalyticalOptionPricerUtil.timeToMaturityFraction(market.ValuationDate, exerciseDate, trade);
            var maturityInYears = AnalyticalOptionPricerUtil.timeToMaturityFraction(market.ValuationDate, maturityDate, trade);

            var riskFreeRate = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, exerciseDate);
            var ticker1 = trade.UnderlyingTickers[0];
            var ticker2 = trade.UnderlyingTickers[1];

            double dividendRate1, dividendRate2;
            if(AnalyticalOptionPricerUtil.isForwardFuturesOption(trade.UnderlyingProductType))
            {
                dividendRate1= riskFreeRate;
                dividendRate2 = riskFreeRate;
            }
            else
            {
                dividendRate1 = market.DividendCurves.Value[ticker1].ZeroRate(market.ValuationDate, exerciseDate);
                dividendRate2 = market.DividendCurves.Value[ticker2].ZeroRate(market.ValuationDate, exerciseDate);
            }

            var spot1 = market.SpotPrices.Value[ticker1];
            var spot2 = market.SpotPrices.Value[ticker2];

            var sigma1 = market.VolSurfaces.Value[ticker1].GetValue(exerciseDate, trade.Strikes[0], spot1);
            var sigma2 = market.VolSurfaces.Value[ticker2].GetValue(exerciseDate, trade.Strikes[0], spot2);
            
            var strike1 = trade.Strikes[0];

            var strike2 = 0.0;
            if (trade.Strikes.Length > 1)
                strike2 = trade.Strikes[1];

            //Note: correlation here is a scala number.  can be a grid for multi-asset option
            var rho = market.Correlations.Value[ticker1].GetValue(exerciseDate, strike1);

            var calculator = new RainbowOptionCalculator(trade.OptionType,
                trade.RainbowType,
                strike1,strike2,trade.CashAmount,
                spot1,spot2,rho,
                sigma1,sigma2,
                exerciseInYears,
                riskFreeRate,
                dividendRate1,dividendRate2,
                trade.Notional);

            bool isExpired = trade.ExerciseDates.Last() < market.ValuationDate;
            bool isExpiredforTheta = trade.ExerciseDates.Last() <= market.ValuationDate;

            if (isExpired)
            {
                result.Pv = 0.0;
                result.Theta = 0.0;
                result.asset1Delta = 0.0;
                result.asset1DeltaCash = 0.0;
                result.asset2Delta = 0.0;
                result.asset2DeltaCash = 0.0;
                result.asset1PartialDelta = 0.0;
                result.asset2PartialDelta = 0.0;
                result.asset1Gamma = 0.0;
                result.asset1GammaCash = 0.0;
                result.asset2Gamma = 0.0;
                result.asset2GammaCash = 0.0;
                result.asset1Vega = 0.0;
                result.asset2Vega = 0.0;
                result.Rho = 0.0;
                result.Theta = 0.0;
                result.asset1DDeltaDt = 0.0;
                result.asset2DDeltaDt = 0.0;
                result.asset1DVegaDvol = 0.0;
                result.asset2DVegaDvol = 0.0;
                result.asset1DDeltaDvol = 0.0;
                result.asset2DDeltaDvol = 0.0;
                result.asset1DVegaDt = 0.0;
                result.asset2DVegaDt = 0.0;
                result.crossGamma = 0.0;
                result.crossVomma = 0.0;
                result.crossVanna1 = 0.0;
                result.crossVanna2 = 0.0;
                result.correlationVega = 0.0;
            }

            else
            {
                if (result.IsRequested(PricingRequest.Pv)) {
                    result.Pv = calculator.Pv;
                }

                if (AnalyticalOptionPricerUtil.isBasicPricing(result))
                {
                    result.asset1Delta = calculator.asset1Delta;
                    result.asset2Delta = calculator.asset2Delta;
                    result.asset1DeltaCash = calculator.asset1Delta * spot1;
                    result.asset2DeltaCash = calculator.asset2Delta * spot2;
                    result.asset1PartialDelta = calculator.asset1PartialDelta;
                    result.asset2PartialDelta = calculator.asset2PartialDelta;
                    result.asset1Gamma = calculator.asset1Gamma;
                    result.asset2Gamma = calculator.asset2Gamma;
                    result.asset1GammaCash = calculator.asset1Gamma * spot1 * spot1 / 100;
                    result.asset2GammaCash = calculator.asset2Gamma * spot2 * spot2 / 100;
                    result.asset1Vega = calculator.asset1Vega;
                    result.asset2Vega = calculator.asset2Vega;
                    result.Rho = calculator.Rho;

                    result.Theta = (isExpiredforTheta) ? 0.0 : calculator.Theta;
                }

                if (AnalyticalOptionPricerUtil.isHighOrderPricing(result))
                {
                    result.asset1DVegaDvol = calculator.asset1DVegaDvol;
                    result.asset2DVegaDvol = calculator.asset2DVegaDvol;
                    result.asset1DDeltaDvol = calculator.asset1DDeltaDvol;
                    result.asset2DDeltaDvol = calculator.asset2DDeltaDvol;
                    result.crossGamma = calculator.crossGamma;
                    result.crossVomma = calculator.crossVomma;
                    result.crossVanna1 = calculator.crossVanna1;
                    result.crossVanna2 = calculator.crossVanna2;
                    result.correlationVega = calculator.correlationVega;
                }


                if (isExpiredforTheta)
                {
                    result.asset1DDeltaDt = result.asset2DDeltaDt = 0.0;
                    result.asset1DVegaDt = result.asset2DVegaDt = 0.0;
                }
                else
                {
                    result.asset1DDeltaDt = calculator.asset1DDeltaDt;
                    result.asset2DDeltaDt = calculator.asset2DDeltaDt;
                    result.asset1DVegaDt = calculator.asset1DVegaDt;
                    result.asset2DVegaDt = calculator.asset2DVegaDt;
                }
            }
            return result;
        }

        internal class RainbowOptionCalculator
        {
            private OptionType _optionType;
            private RainbowType _rainbowType;
            private double _X1,_X2, _K, _S1, _S2, _sigma1, _sigma2, _r, _b1, _b2, _notional, _T, _rho;
        
           
             public RainbowOptionCalculator(
                OptionType optionType, RainbowType rainbowType, 
                double strike1,double strike2, double cashAmount, double spotPrice1, double spotPrice2, double rho, double sigma1,double sigma2,
                double exerciseInYears,double riskFreeRate, double dividendRate1, double dividendRate2,
                double notional)
            {
                _optionType = optionType;
                _rainbowType = rainbowType;
                _X1 = strike1;
                _X2 = strike2;
                _K = cashAmount;
                _S1 = spotPrice1;
                _S2 = spotPrice2;
                _sigma1 = sigma1;
                _sigma2 = sigma2;
                _r = riskFreeRate;
                _b1 = riskFreeRate - dividendRate1;
                _b2 = riskFreeRate - dividendRate2;
                _notional = notional;
                _T = exerciseInYears;
                _rho = rho;
            }


            //Haug 2007
            private double BestOrWorst(double S1, double S2,double T, double r, double b1, double b2, double sigma1, double sigma2,double rho)
            {
                double vol = Math.Sqrt(sigma1 * sigma1 + sigma2 * sigma2 - 2.0 * rho * sigma1 * sigma2);
                double y = 1.0 / vol / Math.Sqrt(T) * (Math.Log(S1 / S2) + T * (b1 - b2 + vol * vol / 2.0));
                double z1 = 1.0 / sigma1 / Math.Sqrt(T) * (Math.Log(S1 / _X1) + T * (b1 + sigma1 * sigma1 / 2.0));
                double z2 = 1.0 / sigma2 / Math.Sqrt(T) * (Math.Log(S2 / _X1) + T * (b2 + sigma2 * sigma2 / 2.0));
                double rho1 = (sigma1 - sigma2 * rho) / vol;
                double rho2 = (sigma2 - rho * sigma1) / vol;
                double pv = 0;
                if (_rainbowType == RainbowType.BestCashOrNothing)
                {
                    pv = (_optionType == OptionType.Call) ? 
                        _K * _notional * Math.Exp(-r * T) * (NormalCdf.NormalCdfGenz(y, z1, -rho1) + NormalCdf.NormalCdfGenz(-y, z2, -rho2)) :
                        _K * _notional * Math.Exp(-r * T) * (1 - NormalCdf.NormalCdfGenz(y, z1, -rho1) - NormalCdf.NormalCdfGenz(-y, z2, -rho2));
                }
                if (_rainbowType == RainbowType.WorstCashOrNothing)
                {
                    pv = (_optionType == OptionType.Call) ? 
                        _K * _notional * Math.Exp(-r * T) * (NormalCdf.NormalCdfGenz(-y, z1, rho1) + NormalCdf.NormalCdfGenz(y, z2, rho2)) :
                        _K * _notional * Math.Exp(-r * T) * (1 - NormalCdf.NormalCdfGenz(-y, z1, rho1) + NormalCdf.NormalCdfGenz(y, z2, rho2));
                }
                return pv;
            }

            //West on wilmott
            private double WestRainbowCall(double S1, double S2, double T, double r, double b1, double b2, double sigma1, double sigma2,double rho)
            {
                WestModel(S1, S2, T, r, b1, b2, sigma1, sigma2, rho,
                    out double d1, out double d1_, out double d2, out double d2_, out double rho1, out double rho2,
                    out double d21, out double d12, out double q1, out double q2);

                return (_rainbowType == RainbowType.Max) ?
                          S1 * Math.Exp(-q1 * T) * NormalCdf.NormalCdfGenz(-d21, d1, rho1) 
                        + S2 * Math.Exp(-q2 * T) * NormalCdf.NormalCdfGenz(-d12, d2, rho2)
                        - _X1 * Math.Exp(-r * T) * (1 - NormalCdf.NormalCdfGenz(-d1_, -d2_, rho)) :

                          S1 * Math.Exp(-q1 * T) * NormalCdf.NormalCdfGenz(d21, d1, -rho1) 
                        + S2 * Math.Exp(-q2 * T) * NormalCdf.NormalCdfGenz(d12, d2, -rho2)
                        - _X1 * Math.Exp(-r * T) * NormalCdf.NormalCdfGenz(d1_, d2_, rho);

            }

            //West on wilmott
            private double WestBestOfAssetsOrCash(double S1, double S2, double T, double r, double b1, double b2, double sigma1, double sigma2, double rho)
            {
                WestModel(S1, S2, T, r, b1, b2, sigma1, sigma2, rho,
                    out double d1, out double d1_, out double d2, out double d2_, out double rho1, out double rho2,
                    out double d21, out double d12, out double q1, out double q2);

                return S1 * Math.Exp(-q1 * T) * NormalCdf.NormalCdfGenz(-d21, d1, rho1)
                    + S2 * Math.Exp(-q2 * T) * NormalCdf.NormalCdfGenz(-d12, d2, rho2)
                    + _X1 * Math.Exp(-r * T) * NormalCdf.NormalCdfGenz(-d1_, -d2_, rho);
            }

            private void WestModel(double S1, double S2, double T, double r, double b1, double b2, double sigma1, double sigma2, double rho,
                out double d1, out double d1_, out double d2, out double d2_, out double rho1, out double rho2, 
                out double d21, out double d12, out double q1, out double q2 )
            {
                q1 = r - b1;
                q2 = r - b2;

                d1 = (Math.Log(S1 / _X1) + (r - q1 + 0.5 * sigma1 * sigma1) * T) / sigma1 / Math.Sqrt(T);
                d1_ = (Math.Log(S1 / _X1) + (r - q1 - 0.5 * sigma1 * sigma1) * T) / sigma1 / Math.Sqrt(T);
                d2 = (Math.Log(S2 / _X1) + (r - q2 + 0.5 * sigma2 * sigma2) * T) / sigma2 / Math.Sqrt(T);
                d2_ = (Math.Log(S2 / _X1) + (r - q2 - 0.5 * sigma2 * sigma2) * T) / sigma2 / Math.Sqrt(T);

                var vol = Math.Sqrt(sigma1 * sigma1 + sigma2 * sigma2 - 2.0 * rho * sigma1 * sigma2);
                d21 = (Math.Log(S2 / S1) + (q1 - q2 - 0.5 * vol * vol) * T) / vol / Math.Sqrt(T);
                d12 = (Math.Log(S1 / S2) + (q2 - q1 - 0.5 * vol * vol) * T) / vol / Math.Sqrt(T);
                rho1 = (sigma1 - sigma2 * rho) / vol;
                rho2 = (sigma2 - rho * sigma1) / vol;
            }

            private double CalcPV(double S1, double S2, double T, double r, double b1, double b2, double sigma1, double sigma2, double rho)
            {
                //for two asset cash or nothing option
                var d11 = (Math.Log(S1 / _X1) + (b1 - sigma1 * sigma1 * 0.5) * T) / sigma1 / Math.Sqrt(T);
                var d22 = (Math.Log(S2 / _X2) + (b2 - sigma2 * sigma2 * 0.5) * T) / sigma2 / Math.Sqrt(T);

                switch (_rainbowType) {
                    //Haug
                    case RainbowType.TwoAssetsCashOrNothing:
                        return (_optionType == OptionType.Call) ?
                            _K * Math.Exp(-r * T) * NormalCdf.NormalCdfGenz(d11, d22, rho) :
                            _K * Math.Exp(-r * T) * NormalCdf.NormalCdfGenz(-d11, -d22, rho);

                    case RainbowType.TwoAssetsCashOrNothingUpDown:
                        return _K * Math.Exp(-r * T) * NormalCdf.NormalCdfGenz(d11, -d22, -rho);

                    case RainbowType.TwoAssetsCashOrNothingDownUp:
                        return _K * Math.Exp(-r * T) * NormalCdf.NormalCdfGenz(-d11, d22, -rho);

                    case RainbowType.BestCashOrNothing:
                    case RainbowType.WorstCashOrNothing:
                        return BestOrWorst(S1, S2, T, r, b1, b2, sigma1, sigma2, rho);

                    //west
                    case RainbowType.BestOfAssetsOrCash:
                        return WestBestOfAssetsOrCash(S1, S2, T, r, b1, b2, sigma1, sigma2, rho);

                    case RainbowType.Min:
                    case RainbowType.Max:
                        if (_optionType == OptionType.Call)
                        {
                            return WestRainbowCall(S1, S2, T, r, b1, b2, sigma1, sigma2, rho);
                        }
                        else
                        {   //put,  use put/call parity to price
                            double q1 = r - b1;
                            double q2 = r - b2;
                            var vol = Math.Sqrt(sigma1 * sigma1 + sigma2 * sigma2 - 2.0 * rho * sigma1 * sigma2);
                            var d21 = (Math.Log(S2 / S1) + (q1 - q2 - 0.5 * vol * vol) * T) / vol / Math.Sqrt(T);
                            var d12 = (Math.Log(S1 / S2) + (q2 - q1 - 0.5 * vol * vol) * T) / vol / Math.Sqrt(T);

                            var VC0max = S1 * Math.Exp(-q1 * T) * NormalCdf.NormalCdfHart(-d21) + S2 * Math.Exp(-q2 * T) * NormalCdf.NormalCdfHart(-d12);
                            var VC0min = S1 * Math.Exp(-q1 * T) * NormalCdf.NormalCdfHart(d21) + S2 * Math.Exp(-q2 * T) * NormalCdf.NormalCdfHart(d12);

                            return (_rainbowType == RainbowType.Max) ? 
                                WestRainbowCall(S1, S2, T, r, b1, b2, sigma1, sigma2, rho) - VC0max + _X1 * Math.Exp(-r * T) : 
                                WestRainbowCall(S1, S2, T, r, b1, b2, sigma1, sigma2, rho) - VC0min + _X1 * Math.Exp(-r * T);
                        }

                    default:
                        throw new PricingLibraryException("Unsupported rainbow option type");
                }

            }

            public double Pv => CalcPV(_S1, _S2, _T, _r, _b1, _b2, _sigma1, _sigma2, _rho);

            private const double riskBumpSize = 1e-4;
            private const double vegaBumpSize = 0.01;
            private const double timeIncrement = 1.0 / 365.0;

            //First Order Greeks, single asset

            public double asset1Delta => (CalcPV(_S1 + riskBumpSize, _S2, _T, _r, _b1, _b2, _sigma1, _sigma2, _rho) - CalcPV(_S1 - riskBumpSize, _S2, _T, _r, _b1, _b2, _sigma1, _sigma2, _rho)) / 2.0 / riskBumpSize;
            public double asset2Delta => (CalcPV(_S1, _S2 + riskBumpSize, _T, _r, _b1, _b2, _sigma1, _sigma2, _rho) - CalcPV(_S1, _S2 - riskBumpSize, _T, _r, _b1, _b2, _sigma1, _sigma2, _rho)) / 2.0 / riskBumpSize;

            public double asset1Gamma => (CalcPV(_S1 + riskBumpSize, _S2, _T, _r, _b1, _b2, _sigma1, _sigma2, _rho) - 2.0 * Pv
                + CalcPV(_S1 - riskBumpSize, _S2, _T, _r, _b1, _b2, _sigma1, _sigma2, _rho)) / riskBumpSize / riskBumpSize;

            public double asset2Gamma => (CalcPV(_S1, _S2 + riskBumpSize, _T, _r, _b1, _b2, _sigma1, _sigma2, _rho) - 2.0 * Pv
                + CalcPV(_S1, _S2 - riskBumpSize, _T, _r, _b1, _b2, _sigma1, _sigma2, _rho)) / riskBumpSize / riskBumpSize;

            public double Theta => CalcPV(_S1, _S2, _T - timeIncrement, _r, _b1, _b2, _sigma1, _sigma2, _rho) - Pv;

            public double asset1Vega => (CalcPV(_S1, _S2, _T, _r, _b1, _b2, _sigma1 + vegaBumpSize, _sigma2, _rho) - CalcPV(_S1, _S2, _T, _r, _b1, _b2, _sigma1 - vegaBumpSize, _sigma2, _rho)) / 2.0;
            public double asset2Vega => (CalcPV(_S1, _S2, _T, _r, _b1, _b2, _sigma1, _sigma2 + vegaBumpSize, _rho) - CalcPV(_S1, _S2, _T, _r, _b1, _b2, _sigma1, _sigma2 - vegaBumpSize, _rho)) / 2.0;

            public double Rho => CalcPV(_S1, _S2, _T, _r + riskBumpSize, _b1, _b2, _sigma1, _sigma2, _rho) - Pv;
            public double asset1PartialDelta => (CalcPV(_S1 + riskBumpSize, _S2 + riskBumpSize * _rho * _sigma2 / _sigma1, _T, _r, _b1, _b2, _sigma1, _sigma2, _rho) - CalcPV(_S1 - riskBumpSize, _S2 - riskBumpSize * _rho * _sigma2 / _sigma1, _T, _r, _b1, _b2, _sigma1, _sigma2, _rho)) / 2.0 / riskBumpSize;
            public double asset2PartialDelta => (CalcPV(_S1 + riskBumpSize * _rho * _sigma1 / _sigma2, _S2 + riskBumpSize, _T, _r, _b1, _b2, _sigma1, _sigma2, _rho) - CalcPV(_S1 - riskBumpSize * _rho * _sigma1 / _sigma2, _S2 - riskBumpSize, _T, _r, _b1, _b2, _sigma1, _sigma2, _rho)) / 2.0 / riskBumpSize;


            //Higher Order Greeks, single asset
            public double asset1DDeltaDt => (CalcPV(_S1 + riskBumpSize, _S2, _T - timeIncrement, _r, _b1, _b2, _sigma1, _sigma2, _rho) - CalcPV(_S1 + riskBumpSize, _S2, _T, _r, _b1, _b2, _sigma1, _sigma2, _rho)
                - CalcPV(_S1 - riskBumpSize, _S2, _T - timeIncrement, _r, _b1, _b2, _sigma1, _sigma2, _rho) + CalcPV(_S1 - riskBumpSize, _S2, _T, _r, _b1, _b2, _sigma1, _sigma2, _rho)) / 2.0 / riskBumpSize;

            public double asset2DDeltaDt => (CalcPV(_S1, _S2 + riskBumpSize, _T - timeIncrement, _r, _b1, _b2, _sigma1, _sigma2, _rho) - CalcPV(_S1, _S2 + riskBumpSize, _T, _r, _b1, _b2, _sigma1, _sigma2, _rho)
                - CalcPV(_S1, _S2 - riskBumpSize, _T - timeIncrement, _r, _b1, _b2, _sigma1, _sigma2, _rho) + CalcPV(_S1, _S2 - riskBumpSize, _T, _r, _b1, _b2, _sigma1, _sigma2, _rho)) / 2.0 / riskBumpSize;

            public double asset1DVegaDt => (CalcPV(_S1, _S2, _T - timeIncrement, _r, _b1, _b2, _sigma1 + vegaBumpSize, _sigma2, _rho) - CalcPV(_S1, _S2, _T, _r, _b1, _b2, _sigma1 + vegaBumpSize, _sigma2, _rho)
                - CalcPV(_S1, _S2, _T - timeIncrement, _r, _b1, _b2, _sigma1 - vegaBumpSize, _sigma2, _rho) + CalcPV(_S1, _S2, _T, _r, _b1, _b2, _sigma1 - vegaBumpSize, _sigma2, _rho)) / 2.0;

            public double asset2DVegaDt => (CalcPV(_S1, _S2, _T - timeIncrement, _r, _b1, _b2, _sigma1, _sigma2 + vegaBumpSize, _rho) - CalcPV(_S1, _S2, _T, _r, _b1, _b2, _sigma1, _sigma2 + vegaBumpSize, _rho)
                - CalcPV(_S1, _S2, _T - timeIncrement, _r, _b1, _b2, _sigma1, _sigma2 - vegaBumpSize, _rho) + CalcPV(_S1, _S2, _T, _r, _b1, _b2, _sigma1, _sigma2 - vegaBumpSize, _rho)) / 2.0;

            public double asset1DVegaDvol => (CalcPV(_S1, _S2, _T, _r, _b1, _b2, _sigma1 + vegaBumpSize, _sigma2, _rho) - 2 * Pv
                + CalcPV(_S1, _S2, _T, _r, _b1, _b2, _sigma1 - vegaBumpSize, _sigma2, _rho)) /2.0 ;

            public double asset2DVegaDvol => (CalcPV(_S1, _S2, _T, _r, _b1, _b2, _sigma1, _sigma2 + vegaBumpSize, _rho) - 2 * Pv
                + CalcPV(_S1, _S2, _T, _r, _b1, _b2, _sigma1, _sigma2 - vegaBumpSize, _rho)) /2.0 ;

            public double asset1DDeltaDvol => (CalcPV(_S1 + riskBumpSize, _S2, _T, _r, _b1, _b2, _sigma1 + vegaBumpSize, _sigma2, _rho) - CalcPV(_S1 + riskBumpSize, _S2, _T, _r, _b1, _b2, _sigma1 - vegaBumpSize, _sigma2, _rho)
                - CalcPV(_S1 - riskBumpSize, _S2, _T, _r, _b1, _b2, _sigma1 + vegaBumpSize, _sigma2, _rho) + CalcPV(_S1 - riskBumpSize, _S2, _T, _r, _b1, _b2, _sigma1 - vegaBumpSize, _sigma2, _rho)) / 4.0 / riskBumpSize;

            public double asset2DDeltaDvol => (CalcPV(_S1, _S2 + riskBumpSize, _T, _r, _b1, _b2, _sigma1, _sigma2 + vegaBumpSize, _rho) - CalcPV(_S1, _S2 + riskBumpSize, _T, _r, _b1, _b2, _sigma1, _sigma2 - vegaBumpSize, _rho)
                - CalcPV(_S1, _S2 - riskBumpSize, _T, _r, _b1, _b2, _sigma1, _sigma2 + vegaBumpSize, _rho) + CalcPV(_S1, _S2 - riskBumpSize, _T, _r, _b1, _b2, _sigma1, _sigma2 - vegaBumpSize, _rho)) / 4.0 / riskBumpSize;

            //Higher Order Greeks, cross effect
            public double crossGamma => (CalcPV(_S1 + riskBumpSize, _S2 + riskBumpSize, _T, _r, _b1, _b2, _sigma1, _sigma2, _rho)
                - CalcPV(_S1 - riskBumpSize, _S2 + riskBumpSize, _T, _r, _b1, _b2, _sigma1, _sigma2, _rho)
                - CalcPV(_S1 + riskBumpSize, _S2 - riskBumpSize, _T, _r, _b1, _b2, _sigma1, _sigma2, _rho)
                + CalcPV(_S1 - riskBumpSize, _S2 - riskBumpSize, _T, _r, _b1, _b2, _sigma1, _sigma2, _rho)) / 4.0 / riskBumpSize / riskBumpSize;

            //vomma = vol gamma
            public double crossVomma => (CalcPV(_S1, _S2, _T, _r, _b1, _b2, _sigma1 + vegaBumpSize, _sigma2 + vegaBumpSize, _rho) 
                - CalcPV(_S1, _S2, _T, _r, _b1, _b2, _sigma1 - vegaBumpSize, _sigma2 + vegaBumpSize, _rho)
                - CalcPV(_S1, _S2, _T, _r, _b1, _b2, _sigma1 + vegaBumpSize, _sigma2 - vegaBumpSize, _rho) 
                + CalcPV(_S1, _S2, _T, _r, _b1, _b2, _sigma1 - vegaBumpSize, _sigma2 - vegaBumpSize, _rho)) / 4.0;

            //dVegadSpot
            //dPrice2/dspot1/dsigma2
            public double crossVanna2 => (CalcPV(_S1 + riskBumpSize, _S2, _T, _r, _b1, _b2, _sigma1, _sigma2 + vegaBumpSize, _rho)
                - CalcPV(_S1 + riskBumpSize, _S2, _T, _r, _b1, _b2, _sigma1, _sigma2 - vegaBumpSize, _rho)
                - CalcPV(_S1 - riskBumpSize, _S2, _T, _r, _b1, _b2, _sigma1, _sigma2 + vegaBumpSize, _rho)
                + CalcPV(_S1 - riskBumpSize, _S2, _T, _r, _b1, _b2, _sigma1, _sigma2 - vegaBumpSize, _rho)) / 4.0 / riskBumpSize;

            public double crossVanna1 => (CalcPV(_S1, _S2 + riskBumpSize, _T, _r, _b1, _b2, _sigma1 + vegaBumpSize, _sigma2, _rho)
                - CalcPV(_S1, _S2 + riskBumpSize, _T, _r, _b1, _b2, _sigma1 - vegaBumpSize, _sigma2, _rho)
                - CalcPV(_S1, _S2 - riskBumpSize, _T, _r, _b1, _b2, _sigma1 + vegaBumpSize, _sigma2, _rho)
                + CalcPV(_S1, _S2 - riskBumpSize, _T, _r, _b1, _b2, _sigma1 - vegaBumpSize, _sigma2, _rho)) / 4.0 / riskBumpSize;

            public double correlationVega => (CalcPV(_S1, _S2, _T, _r, _b1, _b2, _sigma1 , _sigma2, _rho + vegaBumpSize) - CalcPV(_S1, _S2, _T, _r, _b1, _b2, _sigma1 , _sigma2, _rho - vegaBumpSize)) / 2.0;
        }

    }
}
