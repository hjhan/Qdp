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
    public class AnalyticalSpreadOptionEngine : Engine<SpreadOption>
    {
        public override IPricingResult Calculate(SpreadOption trade, IMarketCondition market, PricingRequest request)
        {
            var result = new PricingResult(market.ValuationDate, request);

            var exerciseDate = trade.ExerciseDates.Last();
            var maturityDate = trade.UnderlyingMaturityDate;
            var exerciseInYears = AnalyticalOptionPricerUtil.timeToMaturityFraction(market.ValuationDate, exerciseDate, trade);
            var maturityInYears = AnalyticalOptionPricerUtil.timeToMaturityFraction(market.ValuationDate, maturityDate, trade);

            var riskFreeRate = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, exerciseDate);
            var ticker1 = trade.UnderlyingTickers[0];
            var ticker2 = trade.UnderlyingTickers[1];
            var ticker3 = trade.UnderlyingTickers[2];
            var ticker4 = trade.UnderlyingTickers[3];


            double dividendRate1, dividendRate2, dividendRate3, dividendRate4;
            if (AnalyticalOptionPricerUtil.isForwardFuturesOption(trade.UnderlyingProductType))
            {
                dividendRate1 = riskFreeRate;
                dividendRate2 = riskFreeRate;
                dividendRate3 = riskFreeRate;
                dividendRate4 = riskFreeRate;
            }
            else
            {
                dividendRate1 = market.DividendCurves.Value[ticker1].ZeroRate(market.ValuationDate, exerciseDate);
                dividendRate2 = market.DividendCurves.Value[ticker2].ZeroRate(market.ValuationDate, exerciseDate);
                dividendRate3 = market.DividendCurves.Value[ticker3].ZeroRate(market.ValuationDate, exerciseDate);
                dividendRate4 = market.DividendCurves.Value[ticker4].ZeroRate(market.ValuationDate, exerciseDate);
            }

            var spot1 = market.SpotPrices.Value[ticker1];
            var spot2 = market.SpotPrices.Value[ticker2];
            var spot3 = market.SpotPrices.Value[ticker3];
            var spot4 = market.SpotPrices.Value[ticker4];
            var strike = trade.Strike;

            var sigma1 = market.VolSurfaces.Value[ticker1].GetValue(exerciseDate, trade.Strike, spot1);
            var sigma2 = market.VolSurfaces.Value[ticker2].GetValue(exerciseDate, trade.Strike, spot2);
            var sigma3 = market.VolSurfaces.Value[ticker3].GetValue(exerciseDate, trade.Strike, spot3);
            var sigma4 = market.VolSurfaces.Value[ticker4].GetValue(exerciseDate, trade.Strike, spot4);

            var rho12 = market.Correlations.Value[ticker1 + ticker2].GetValue(exerciseDate, strike);
            var rho13 = market.Correlations.Value[ticker1 + ticker3].GetValue(exerciseDate, strike);
            var rho14 = market.Correlations.Value[ticker1 + ticker4].GetValue(exerciseDate, strike);
            var rho23 = market.Correlations.Value[ticker2 + ticker3].GetValue(exerciseDate, strike);
            var rho24 = market.Correlations.Value[ticker2 + ticker4].GetValue(exerciseDate, strike);
            var rho34 = market.Correlations.Value[ticker3 + ticker4].GetValue(exerciseDate, strike);


            var calculator = new SpreadOptionCalculator(trade.OptionType,
                trade.SpreadType,
                strike,
                spot1 * trade.Weights[0], spot2 * trade.Weights[1], spot3 * trade.Weights[2], spot4 * trade.Weights[4],
                rho12, rho13, rho14, rho23, rho24, rho34,
                sigma1, sigma2, sigma3, sigma4,
                exerciseInYears,
                riskFreeRate,
                dividendRate1, dividendRate2, dividendRate3, dividendRate4,
                trade.Weights[0], trade.Weights[1], trade.Weights[2], trade.Weights[3],
                trade.Notional);

            if (result.IsRequested(PricingRequest.Pv))
            {
                result.Pv = calculator.Pv;
            }

            if (AnalyticalOptionPricerUtil.isBasicPricing(result))
            {
                result.Theta = calculator.Theta;
                result.Rho = calculator.Rho;
                result.asset1Delta = calculator.asset1Delta;
                result.asset2Delta = calculator.asset2Delta;
                result.asset3Delta = calculator.asset3Delta;
                result.asset4Delta = calculator.asset4Delta;
                result.asset1PartialDelta = calculator.asset1PartialDelta;
                result.asset2PartialDelta = calculator.asset2PartialDelta;
                result.asset3PartialDelta = calculator.asset3PartialDelta;
                result.asset4PartialDelta = calculator.asset4PartialDelta;
                result.asset1Gamma = calculator.asset1Gamma;
                result.asset2Gamma = calculator.asset2Gamma;
                result.asset3Gamma = calculator.asset3Gamma;
                result.asset4Gamma = calculator.asset4Gamma;
                result.asset1Vega = calculator.asset1Vega;
                result.asset2Vega = calculator.asset2Vega;
                result.asset3Vega = calculator.asset3Vega;
                result.asset4Vega = calculator.asset4Vega;
            }

            if (AnalyticalOptionPricerUtil.isHighOrderPricing(result))
            {
                result.asset1DDeltaDt = calculator.asset1DDeltaDt;
                result.asset2DDeltaDt = calculator.asset2DDeltaDt;
                result.asset3DDeltaDt = calculator.asset3DDeltaDt;
                result.asset4DDeltaDt = calculator.asset4DDeltaDt;
                result.asset1DDeltaDvol = calculator.asset1DDeltaDvol;
                result.asset2DDeltaDvol = calculator.asset2DDeltaDvol;
                result.asset3DDeltaDvol = calculator.asset3DDeltaDvol;
                result.asset4DDeltaDvol = calculator.asset4DDeltaDvol;
                result.asset1DVegaDvol = calculator.asset1DVegaDvol;
                result.asset2DVegaDvol = calculator.asset2DVegaDvol;
                result.asset3DVegaDvol = calculator.asset3DVegaDvol;
                result.asset4DVegaDvol = calculator.asset4DVegaDvol;
                result.asset1DVegaDt = calculator.asset1DVegaDt;
                result.asset2DVegaDt = calculator.asset2DVegaDt;
                result.asset3DVegaDt = calculator.asset3DVegaDt;
                result.asset4DVegaDt = calculator.asset4DVegaDt;
                result.crossGamma12 = calculator.crossGamma12;
                result.crossGamma13 = calculator.crossGamma13;
                result.crossGamma14 = calculator.crossGamma14;
                result.crossGamma23 = calculator.crossGamma23;
                result.crossGamma24 = calculator.crossGamma24;
                result.crossGamma34 = calculator.crossGamma34;
                result.crossVomma12 = calculator.crossVomma12;
                result.crossVomma13 = calculator.crossVomma13;
                result.crossVomma14 = calculator.crossVomma14;
                result.crossVomma23 = calculator.crossVomma23;
                result.crossVomma24 = calculator.crossVomma24;
                result.crossVomma34 = calculator.crossVomma34;
                result.correlationVega12 = calculator.correlationVega12;
                result.correlationVega13 = calculator.correlationVega13;
                result.correlationVega14 = calculator.correlationVega14;
                result.correlationVega23 = calculator.correlationVega23;
                result.correlationVega24 = calculator.correlationVega24;
                result.correlationVega34 = calculator.correlationVega34;
            }

            return result;
        }



        internal class SpreadOptionCalculator
        {
            private OptionType _optionType;
            private SpreadType _spreadType;
            private double _X, _S1, _S2, _S3, _S4, _sigma1, _sigma2, _sigma3, _sigma4,
                _r, _b1, _b2, _b3, _b4, _notional, _T,
                _rho12, _rho13, _rho14, _rho23, _rho24, _rho34, _w1, _w2, _w3, _w4;


            public SpreadOptionCalculator(
               OptionType optionType, SpreadType spreadType,
               double strike, double spotPrice1, double spotPrice2, double spotPrice3, double spotPrice4, double rho12, double rho13, double rho14,
               double rho23, double rho24, double rho34, double sigma1, double sigma2, double sigma3, double sigma4, double exerciseInYears, double riskFreeRate,
               double dividendRate1, double dividendRate2, double dividendRate3, double dividendRate4, double weights1, double weights2, double weights3, double weights4,
               double notional)
            {
                _optionType = optionType;
                _spreadType = spreadType;
                _X = strike;
                _S1 = spotPrice1;
                _S2 = spotPrice2;
                _S3 = spotPrice3;
                _S4 = spotPrice4;
                _sigma1 = sigma1;
                _sigma2 = sigma2;
                _sigma3 = sigma3;
                _sigma4 = sigma4;
                _r = riskFreeRate;
                _b1 = riskFreeRate - dividendRate1;
                _b2 = riskFreeRate - dividendRate2;
                _b3 = riskFreeRate - dividendRate3;
                _b4 = riskFreeRate - dividendRate4;
                _notional = notional;
                _T = exerciseInYears;
                _rho12 = rho12;
                _rho13 = rho13;
                _rho14 = rho14;
                _rho23 = rho23;
                _rho24 = rho24;
                _rho34 = rho34;
                _w1 = weights1;
                _w2 = weights2;
                _w3 = weights3;
                _w4 = weights4;
            }
            //Kirk Approximation
            //See also http://www.opus-finance.com/sites/default/files/Fichier_Site_Opus/Article_recherche/Articles_externes/2013/Closed_form_spread_option_valuation/Closed_form_spread_option_valuation.pdf
            //Payoff: max(S1-S2-K,0)
            private double Kirk(double S1, double S2, double T, double r, double b1, double b2, 
               double sigma1, double sigma2, double rho12)
            {
                double F = S2 / (S2 + _X * Math.Exp(-b2 * T) );
                double sigmak = Math.Sqrt(sigma1 * sigma1 - 2.0 * rho12 * sigma1 * sigma2 * F + Math.Pow( F* sigma2, 2));
                double S = S1 * Math.Exp(b1 * T) / (S2 * Math.Exp(b2 * T) + _X);
                double d1 = (Math.Log(S) + 0.5 * sigmak * sigmak * T) / sigmak / Math.Sqrt(T);
                double d2 = d1 - sigmak * Math.Sqrt(T);
                double K = S2 * Math.Exp((b2 - r) * T) + _X * Math.Exp(-r * T);
                switch (_optionType) {
                    case OptionType.Call:
                        return K * (S * NormalCdf.NormalCdfHart(d1) - NormalCdf.NormalCdfHart(d2));
                    default:
                        return K * (NormalCdf.NormalCdfHart(-d2) - S * NormalCdf.NormalCdfHart(-d1));
                }
            }

            //Deng, Li and Zhou 2007: Multi-asset Spread Option Pricing and Hedging
            //Extended Kirk Approximation 
            //See also https://mpra.ub.uni-muenchen.de/8259/1/MPRA_paper_8259.pdf
            //Payoff: Max(S1-S2-S3-X, 0)
            private double ExtendedKirk(double S1, double S2, double S3, double T, double r, double b1, double b2, double b3, 
                double sigma1, double sigma2, double sigma3, double rho12, double rho23, double rho31)
            {
                double u1 = Math.Log(S1) + (b1 - sigma1 * sigma1 * 0.5) * T;
                double u2 = Math.Log(S2) + (b2 - sigma2 * sigma2 * 0.5) * T;
                double u3 = Math.Log(S3) + (b3 - sigma3 * sigma3 * 0.5) * T;
                double v1 = sigma1 * Math.Sqrt(T);
                double v2 = sigma2 * Math.Sqrt(T);
                double v3 = sigma3 * Math.Sqrt(T);

                double N = (S3 == 0 | S2 == 0) ? 1.0 : 2.0;
                double vA = 1.0 / N * Math.Sqrt(v2 * v2 + v3 * v3 + 2.0 * v2 * v3 * rho23);  // consider A be the process of S2+S3
                double rhoA = (v2 * rho12 + v3 * rho31) / N / vA;   // correlation between S1 and A

                double m0 = Math.Exp(u1 + 0.5 * v1 * v1) / (_X + Math.Exp(u2 + 0.5 * v2 * v2) + Math.Exp(u3 + 0.5 * v3 * v3));
                double m = (Math.Exp(u2 + 0.5 * v2 * v2) + Math.Exp(u3 + 0.5 * v3 * v3)) / (_X + Math.Exp(u2 + 0.5 * v2 * v2) + Math.Exp(u3 + 0.5 * v3 * v3));

                double vk = Math.Sqrt(v1 * v1 - 2 * rhoA * v1 * vA * m + vA * vA * m * m);
                double dk = Math.Log(m0) / vk;

                double d1 = dk + vk * 0.5;
                double d2 = dk - vk * 0.5;

                switch (_optionType)
                {
                    case OptionType.Call:
                        return  S1 * Math.Exp((b1 - r) * T) * NormalCdf.NormalCdfHart(d1) -
                                NormalCdf.NormalCdfHart(d2) * (S2 * Math.Exp((b2 - r) * T) + S3 * Math.Exp((b3 - r) * T) + _X * Math.Exp(-r * T));
                    default:
                        return -1.0 *( 
                                S1 * Math.Exp((b1 - r) * T) * NormalCdf.NormalCdfHart(-d1) -
                                NormalCdf.NormalCdfHart(-d2) * (S2 * Math.Exp((b2 - r) * T) + S3 * Math.Exp((b3 - r) * T) + _X * Math.Exp(-r * T)) 
                                );
                }
            }

            //Payoff: Max(S1+S2-S3-X, 0)
            private double BasketExtendedKirk(double S1, double S2, double S3, double T, double r, double b1, double b2, double b3,
                double sigma1, double sigma2, double sigma3, double rho12, double rho23, double rho31)
            {
                double H0 = S1 + S2;
                double u1 = Math.Log(S1) + (b1 - sigma1 * sigma1 * 0.5) * T;
                double u2 = Math.Log(S2) + (b2 - sigma2 * sigma2 * 0.5) * T;
                double u3 = Math.Log(S3) + (b3 - sigma3 * sigma3 * 0.5) * T;
                double v1 = sigma1 * Math.Sqrt(T);
                double v2 = sigma2 * Math.Sqrt(T);
                double v3 = sigma3 * Math.Sqrt(T);

                double M = (S1 == 0 | S2 == 0) ? 1.0 : 2.0;

                double u0H = Math.Log(H0) + (r * T - 1 / M * (r - b1 + r - b2) * T - 1 / 2.0 / M * (v1 * v1 + v2 * v2));
                double v0H = 1.0 / M * Math.Sqrt(v1 * v1 + v2 * v2 + 2.0 * v1 * v2 * rho12);
                double rhoH = (v1 * rho31 + v2 * rho23) / M / v0H;

                double m0 = Math.Exp(u0H + 0.5 * v0H * v0H) / (_X + Math.Exp(u3 + 0.5 * v3 * v3));
                double m = Math.Exp(u3 + 0.5 * v3 * v3) / (_X + Math.Exp(u3 + 0.5 * v3 * v3));

                double vk = Math.Sqrt(v0H * v0H - 2 * rhoH * v0H * v3 * m + v3 * v3 * m * m);
                double dk = Math.Log(m0) / vk;

                double d1 = dk + vk * 0.5;
                double d2 = dk - vk * 0.5;

                switch (_optionType)
                {
                    case OptionType.Call:
                        return  Math.Exp(-r * T + u0H + 0.5 * v0H * v0H) * NormalCdf.NormalCdfHart(d1) - NormalCdf.NormalCdfHart(d2) *
                                (S3 * Math.Exp((b3 - r) * T) + _X * Math.Exp(-r * T));
                    default:
                        return -1.0 * ( 
                                Math.Exp(-r * T + u0H + 0.5 * v0H * v0H) * NormalCdf.NormalCdfHart(-d1) - NormalCdf.NormalCdfHart(-d2) *
                                (S3 * Math.Exp((b3 - r) * T) + _X * Math.Exp(-r * T))
                                );
                }
            }

            private double CalcPV(double S1, double S2, double S3, double S4, double T, double r, double b1, double b2, double b3, double b4,
                double sigma1, double sigma2, double sigma3, double sigma4, double rho12, double rho13, double rho14, double rho23, double rho24, double rho34)
            {
                switch (_spreadType) {
                    case SpreadType.TwoAssetsSpread:
                        return Kirk(S1, S2, T, r, b1, b2, sigma1, sigma2, rho12) * _notional;
                     case SpreadType.ThreeAssetsSpread:
                        return ExtendedKirk(S1, S2, S3, T, r, b1, b2, b3, sigma1, sigma2, sigma3, rho12, rho23, rho13) * _notional;
                    case SpreadType.ThreeAssetsSpreadBasket:
                        return BasketExtendedKirk(S1, S2, S3, T, r, b1, b2, b3, sigma1, sigma2, sigma3, rho12, rho23, rho13) * _notional;
                    default:
                        throw new PricingLibraryException($"Do not support spread option with such payoff: {_spreadType}");
                }
            }

            public double Pv => CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34);

            private const double riskBumpSize = 1e-4;
            private const double vegaBumpSize = 0.01;
            private const double timeIncrement = 1.0 / 365.0;

            //First Order Greeks, single asset

            public double asset1Delta => (CalcPV(_S1 + _w1 * riskBumpSize, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34) - CalcPV(_S1 - _w1 * riskBumpSize, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0 / riskBumpSize;
            public double asset2Delta => (CalcPV(_S1, _S2 + _w2 * riskBumpSize, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34) - CalcPV(_S1, _S2 - _w2 * riskBumpSize, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0 / riskBumpSize;
            public double asset3Delta => (CalcPV(_S1, _S2, _S3 + _w3 * riskBumpSize, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34) - CalcPV(_S1, _S2, _S3 - _w3 * riskBumpSize, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0 / riskBumpSize;
            public double asset4Delta => (CalcPV(_S1, _S2, _S3, _S4 + _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34) - CalcPV(_S1, _S2, _S3, _S4 - _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0 / riskBumpSize;
            public double asset1Gamma => (CalcPV(_S1 + _w1 * riskBumpSize, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34) - 2.0 * Pv
                + CalcPV(_S1 - _w1 * riskBumpSize, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / riskBumpSize / riskBumpSize;
            public double asset2Gamma => (CalcPV(_S1, _S2 + _w2 * riskBumpSize, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34) - 2.0 * Pv
                + CalcPV(_S1, _S2 - _w2 * riskBumpSize, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / riskBumpSize / riskBumpSize;
            public double asset3Gamma => (CalcPV(_S1, _S2, _S3 + _w3 * riskBumpSize, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34) - 2.0 * Pv
                + CalcPV(_S1, _S2, _S3 - _w3 * riskBumpSize, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / riskBumpSize / riskBumpSize;
            public double asset4Gamma => (CalcPV(_S1, _S2, _S3, _S4 + _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34) - 2.0 * Pv
              + CalcPV(_S1, _S2, _S3, _S4 - _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / riskBumpSize / riskBumpSize;
            public double Theta => CalcPV(_S1, _S2, _S3, _S4, _T - timeIncrement, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34) - Pv;
            public double asset1Vega => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1 + vegaBumpSize, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1 - vegaBumpSize, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0;
            public double asset2Vega => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2 + vegaBumpSize, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2 - vegaBumpSize, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0;
            public double asset3Vega => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3 + vegaBumpSize, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3 - vegaBumpSize, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0;
            public double asset4Vega => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4 + vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4 - vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0;
            public double Rho => CalcPV(_S1, _S2, _S3, _S4, _T, _r + riskBumpSize, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34) - Pv;
            public double asset1PartialDelta => (CalcPV(_S1 + _w1 * riskBumpSize, _S2 + _w2 * riskBumpSize * _rho12 * _sigma2 / _sigma1, _S3 + _w3 * riskBumpSize * _rho13 * _sigma3 / _sigma1, _S4 + _w4 * riskBumpSize * _rho14 * _sigma4 / _sigma1, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1 - _w1 * riskBumpSize, _S2 - _w2 * riskBumpSize * _rho12 * _sigma2 / _sigma1, _S3 - _w3 * riskBumpSize * _rho13 * _sigma3 / _sigma1, _S4 - _w4 * riskBumpSize * _rho14 * _sigma4 / _sigma1, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0 / riskBumpSize;
            public double asset2PartialDelta => (CalcPV(_S1 + _w1 * riskBumpSize * _rho12 * _sigma1 / _sigma2, _S2 + _w2 * riskBumpSize, _S3 + _w3 * riskBumpSize * _rho23 * _sigma3 / _sigma2, _S4 + _w4 * riskBumpSize * _rho24 * _sigma4 / _sigma2, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1 - _w1 * riskBumpSize * _rho12 * _sigma1 / _sigma2, _S2 - _w2 * riskBumpSize, _S3 - _w3 * riskBumpSize * _rho23 * _sigma3 / _sigma2, _S4 - _w4 * riskBumpSize * _rho24 * _sigma4 / _sigma2, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0 / riskBumpSize;
            public double asset3PartialDelta => (CalcPV(_S1 + _w1 * riskBumpSize * _rho13 * _sigma1 / _sigma3, _S2 + _w2 * riskBumpSize * _rho23 * _sigma2 / _sigma3, _S3 + _w3 * riskBumpSize, _S4 + _w4 * riskBumpSize * _rho34 * _sigma4 / _sigma3, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1 - _w1 * riskBumpSize * _rho13 * _sigma1 / _sigma3, _S2 - _w2 * riskBumpSize * _rho23 * _sigma2 / _sigma3, _S3 - _w3 * riskBumpSize, _S4 - _w4 * riskBumpSize * _rho34 * _sigma4 / _sigma3, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0 / riskBumpSize;
            public double asset4PartialDelta => (CalcPV(_S1 + _w1 * riskBumpSize * _rho14 * _sigma1 / _sigma4, _S2 + _w2 * riskBumpSize * _rho24 * _sigma2 / _sigma4, _S3 + _w3 * riskBumpSize * _rho34 * _sigma3 / _sigma4, _S4 + _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1 - _w1 * riskBumpSize * _rho14 * _sigma1 / _sigma4, _S2 - _w2 * riskBumpSize * _rho24 * _sigma2 / _sigma4, _S3 - _w3 * riskBumpSize * _rho34 * _sigma3 / _sigma4, _S4 - _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0 / riskBumpSize;

            //Higher Order Greeks, single asset
            //DDeltaDt
            public double asset1DDeltaDt => (CalcPV(_S1 + _w1 * riskBumpSize, _S2, _S3, _S4, _T - timeIncrement, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1 + _w1 * riskBumpSize, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1 - _w1 * riskBumpSize, _S2, _S3, _S4, _T - timeIncrement, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                + CalcPV(_S1 - _w1 * riskBumpSize, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0 / riskBumpSize;
            public double asset2DDeltaDt => (CalcPV(_S1, _S2 + _w2 * riskBumpSize, _S3, _S4, _T - timeIncrement, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2 + _w2 * riskBumpSize, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2 - _w2 * riskBumpSize, _S3, _S4, _T - timeIncrement, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                + CalcPV(_S1, _S2 - _w2 * riskBumpSize, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0 / riskBumpSize;
            public double asset3DDeltaDt => (CalcPV(_S1, _S2, _S3 + _w3 * riskBumpSize, _S4, _T - timeIncrement, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3 + _w3 * riskBumpSize, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3 - _w3 * riskBumpSize, _S4, _T - timeIncrement, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                + CalcPV(_S1, _S2, _S3 - _w3 * riskBumpSize, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0 / riskBumpSize;
            public double asset4DDeltaDt => (CalcPV(_S1, _S2, _S3, _S4 + _w4 * riskBumpSize, _T - timeIncrement, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3, _S4 + _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3, _S4 - _w4 * riskBumpSize, _T - timeIncrement, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                + CalcPV(_S1, _S2, _S3, _S4 - _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0 / riskBumpSize;
            //DVegaDt

            public double asset1DVegaDt => (CalcPV(_S1, _S2, _S3, _S4, _T - timeIncrement, _r, _b1, _b2, _b3, _b4, _sigma1 + vegaBumpSize, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
            - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1 + vegaBumpSize, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
            - CalcPV(_S1, _S2, _S3, _S4, _T - timeIncrement, _r, _b1, _b2, _b3, _b4, _sigma1 - vegaBumpSize, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
            + CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1 - vegaBumpSize, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0;

            public double asset2DVegaDt => (CalcPV(_S1, _S2, _S3, _S4, _T - timeIncrement, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2 + vegaBumpSize, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
            - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2 + vegaBumpSize, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
            - CalcPV(_S1, _S2, _S3, _S4, _T - timeIncrement, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2 - vegaBumpSize, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
            + CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2 - vegaBumpSize, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0;
            public double asset3DVegaDt => (CalcPV(_S1, _S2, _S3, _S4, _T - timeIncrement, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3 + vegaBumpSize, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
            - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3 + vegaBumpSize, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
            - CalcPV(_S1, _S2, _S3, _S4, _T - timeIncrement, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3 - vegaBumpSize, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
            + CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3 - vegaBumpSize, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0;
            public double asset4DVegaDt => (CalcPV(_S1, _S2, _S3, _S4, _T - timeIncrement, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4 + vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
            - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4 + vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
            - CalcPV(_S1, _S2, _S3, _S4, _T - timeIncrement, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4 - vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
            + CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4 - vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0;

            //DVegaDvol
            public double asset1DVegaDvol => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1 + vegaBumpSize, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34) - 2 * Pv
                + CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1 - vegaBumpSize, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0;
            public double asset2DVegaDvol => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2 + vegaBumpSize, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34) - 2 * Pv
                + CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2 - vegaBumpSize, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0;
            public double asset3DVegaDvol => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3 + vegaBumpSize, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34) - 2 * Pv
                + CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3 - vegaBumpSize, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0;
            public double asset4DVegaDvol => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4 + vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34) - 2 * Pv
                + CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4 - vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0;

            //DDeltaDvol
            public double asset1DDeltaDvol => (CalcPV(_S1 + _w1 * riskBumpSize, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1 + vegaBumpSize, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1 - _w1 * riskBumpSize, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1 + vegaBumpSize, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1 + _w1 * riskBumpSize, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1 - vegaBumpSize, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                + CalcPV(_S1 - _w1 * riskBumpSize, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1 - vegaBumpSize, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 4.0 / riskBumpSize;
            public double asset2DDeltaDvol => (CalcPV(_S1, _S2 + _w2 * riskBumpSize, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2 + vegaBumpSize, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2 + _w2 * riskBumpSize, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2 - vegaBumpSize, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2 - _w2 * riskBumpSize, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2 + vegaBumpSize, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                + CalcPV(_S1, _S2 - _w2 * riskBumpSize, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2 - vegaBumpSize, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 4.0 / riskBumpSize;
            public double asset3DDeltaDvol => (CalcPV(_S1, _S2, _S3 + _w3 * riskBumpSize, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3 + vegaBumpSize, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3 + _w3 * riskBumpSize, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3 - vegaBumpSize, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3 - _w3 * riskBumpSize, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3 + vegaBumpSize, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                + CalcPV(_S1, _S2, _S3 - _w3 * riskBumpSize, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3 - vegaBumpSize, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 4.0 / riskBumpSize;
            public double asset4DDeltaDvol => (CalcPV(_S1, _S2, _S3, _S4 + _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4 + vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3, _S4 + _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4 - vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3, _S4 - _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4 + vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                + CalcPV(_S1, _S2, _S3, _S4 - _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4 - vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 4.0 / riskBumpSize;
            //Higher Order Greeks, cross effect
            //crossGamma
            public double crossGamma12 => (CalcPV(_S1 + _w1 * riskBumpSize, _S2 + _w2 * riskBumpSize, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1 + _w1 * riskBumpSize, _S2 - _w2 * riskBumpSize, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1 - _w1 * riskBumpSize, _S2 + _w2 * riskBumpSize, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                + CalcPV(_S1 - _w1 * riskBumpSize, _S2 - _w2 * riskBumpSize, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 4.0 / riskBumpSize / riskBumpSize;

            public double crossGamma13 => (CalcPV(_S1 + _w1 * riskBumpSize, _S2, _S3 + _w3 * riskBumpSize, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                           - CalcPV(_S1 + _w1 * riskBumpSize, _S2, _S3 - _w3 * riskBumpSize, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                           - CalcPV(_S1 - _w1 * riskBumpSize, _S2, _S3 + _w3 * riskBumpSize, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                           + CalcPV(_S1 - _w1 * riskBumpSize, _S2, _S3 - _w3 * riskBumpSize, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 4.0 / riskBumpSize / riskBumpSize;
            public double crossGamma14 => (CalcPV(_S1 + _w1 * riskBumpSize, _S2, _S3, _S4 + _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                           - CalcPV(_S1 + _w1 * riskBumpSize, _S2, _S3, _S4 - _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                           - CalcPV(_S1 - _w1 * riskBumpSize, _S2, _S3, _S4 + _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                           + CalcPV(_S1 - _w1 * riskBumpSize, _S2, _S3, _S4 - _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 4.0 / riskBumpSize / riskBumpSize;
            public double crossGamma23 => (CalcPV(_S1, _S2 + _w2 * riskBumpSize, _S3 + _w3 * riskBumpSize, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
               - CalcPV(_S1, _S2 - _w2 * riskBumpSize, _S3 + _w3 * riskBumpSize, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
               - CalcPV(_S1, _S2 + _w2 * riskBumpSize, _S3 - _w3 * riskBumpSize, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
               + CalcPV(_S1, _S2 - _w2 * riskBumpSize, _S3 - _w3 * riskBumpSize, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 4.0 / riskBumpSize / riskBumpSize;
            public double crossGamma24 => (CalcPV(_S1, _S2 + _w2 * riskBumpSize, _S3, _S4 + _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
              - CalcPV(_S1, _S2 - _w2 * riskBumpSize, _S3, _S4 + _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
              - CalcPV(_S1, _S2 + _w2 * riskBumpSize, _S3, _S4 - _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
              + CalcPV(_S1, _S2 - _w2 * riskBumpSize, _S3, _S4 - _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 4.0 / riskBumpSize / riskBumpSize;
            public double crossGamma34 => (CalcPV(_S1, _S2, _S3 + _w3 * riskBumpSize, _S4 + _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
              - CalcPV(_S1, _S2, _S3 + _w3 * riskBumpSize, _S4 - _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
              - CalcPV(_S1, _S2, _S3 - _w3 * riskBumpSize, _S4 + _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
              + CalcPV(_S1, _S2, _S3 - _w3 * riskBumpSize, _S4 - _w4 * riskBumpSize, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 4.0 / riskBumpSize / riskBumpSize;
            //vomma = vol gamma
            public double crossVomma12 => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1 + vegaBumpSize, _sigma2 + vegaBumpSize, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1 + vegaBumpSize, _sigma2 - vegaBumpSize, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1 - vegaBumpSize, _sigma2 + vegaBumpSize, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                + CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1 - vegaBumpSize, _sigma2 - vegaBumpSize, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 4.0;
            public double crossVomma13 => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1 + vegaBumpSize, _sigma2, _sigma3 + vegaBumpSize, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1 + vegaBumpSize, _sigma2, _sigma3 - vegaBumpSize, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1 - vegaBumpSize, _sigma2, _sigma3 + vegaBumpSize, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                + CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1 - vegaBumpSize, _sigma2, _sigma3 - vegaBumpSize, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 4.0;
            public double crossVomma14 => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1 + vegaBumpSize, _sigma2, _sigma3, _sigma4 + vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
               - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1 + vegaBumpSize, _sigma2, _sigma3, _sigma4 - vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
               - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1 - vegaBumpSize, _sigma2, _sigma3, _sigma4 + vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
               + CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1 - vegaBumpSize, _sigma2, _sigma3, _sigma4 - vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 4.0;
            public double crossVomma23 => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2 + vegaBumpSize, _sigma3 + vegaBumpSize, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
               - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2 + vegaBumpSize, _sigma3 - vegaBumpSize, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
               - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2 - vegaBumpSize, _sigma3 + vegaBumpSize, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
               + CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2 - vegaBumpSize, _sigma3 - vegaBumpSize, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 4.0;
            public double crossVomma24 => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2 + vegaBumpSize, _sigma3, _sigma4 + vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
               - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2 + vegaBumpSize, _sigma3, _sigma4 - vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
               - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2 - vegaBumpSize, _sigma3, _sigma4 + vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
               + CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2 - vegaBumpSize, _sigma3, _sigma4 - vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 4.0;
            public double crossVomma34 => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3 + vegaBumpSize, _sigma4 + vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
               - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3 - vegaBumpSize, _sigma4 + vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
               - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3 + vegaBumpSize, _sigma4 - vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
               + CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3 - vegaBumpSize, _sigma4 - vegaBumpSize, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 4.0;
            //correlationVega
            public double correlationVega12 => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12 + vegaBumpSize, _rho13, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12 - vegaBumpSize, _rho13, _rho14, _rho23, _rho24, _rho34)) / 2.0;
            public double correlationVega13 => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13 + vegaBumpSize, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13 - vegaBumpSize, _rho14, _rho23, _rho24, _rho34)) / 2.0;
            public double correlationVega14 => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14 + vegaBumpSize, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14 - vegaBumpSize, _rho23, _rho24, _rho34)) / 2.0;
            public double correlationVega23 => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23 + vegaBumpSize, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23 - vegaBumpSize, _rho24, _rho34)) / 2.0;
            public double correlationVega24 => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24 + vegaBumpSize, _rho34)
                - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24 - vegaBumpSize, _rho34)) / 2.0;
            public double correlationVega34 => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34 + vegaBumpSize)
                - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34 - vegaBumpSize)) / 2.0;
        }
    }
}
