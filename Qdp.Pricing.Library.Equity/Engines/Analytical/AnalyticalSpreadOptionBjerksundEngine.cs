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
    public class AnalyticalSpreadOptionBjerksundEngine : Engine<SpreadOption>
    {
        private double _sigma1 = 0.0;
        private double _sigma2 = 0.0;
        private double _sigma3 = 0.0;
        private double _sigma4 = 0.0;

        private void SetPricingVol(PricingResult result,  bool notPricing = false) {
            if (notPricing)
            {
                result.asset1PricingVol = result.asset2PricingVol = result.asset3PricingVol = result.asset4PricingVol = 0.0;
            }
            else {
                result.asset1PricingVol = _sigma1;
                result.asset2PricingVol = _sigma2;
                result.asset3PricingVol = _sigma3;
                result.asset4PricingVol = _sigma4;
            }
        }

        public override IPricingResult Calculate(SpreadOption trade, IMarketCondition market, PricingRequest request)
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
            string ticker4 = "", ticker3 = "";
            if (trade.UnderlyingTickers.Length > 2) { ticker3 = trade.UnderlyingTickers[2]; }
            if (trade.UnderlyingTickers.Length > 3) { ticker4 = trade.UnderlyingTickers[3]; }


            double dividendRate1=0.0 , dividendRate2=0.0, dividendRate3=0.0, dividendRate4=0.0;
            if (AnalyticalOptionPricerUtil.isForwardFuturesOption(trade.UnderlyingProductType))
            {
                dividendRate1 = riskFreeRate;
                dividendRate2 = riskFreeRate;
                if (trade.UnderlyingTickers.Length > 2)
                {
                    dividendRate3 = riskFreeRate;
                }
                if (trade.UnderlyingTickers.Length > 3)
                {
                    dividendRate4 = riskFreeRate;
                }

            }
            else
            {
                dividendRate1 = market.DividendCurves.Value[ticker1].ZeroRate(market.ValuationDate, exerciseDate);
                dividendRate2 = market.DividendCurves.Value[ticker2].ZeroRate(market.ValuationDate, exerciseDate);
                if (trade.UnderlyingTickers.Length > 2)
                { dividendRate3 = market.DividendCurves.Value[ticker3].ZeroRate(market.ValuationDate, exerciseDate); }
                if (trade.UnderlyingTickers.Length > 3)
                { dividendRate4 = market.DividendCurves.Value[ticker4].ZeroRate(market.ValuationDate, exerciseDate); }
            }

            var spot1 = market.SpotPrices.Value[ticker1];
            var spot2 = market.SpotPrices.Value[ticker2];
            double spot3 = 0.0, spot4 = 0.0;
            if (trade.UnderlyingTickers.Length > 2)
               spot3 = market.SpotPrices.Value[ticker3];
            if (trade.UnderlyingTickers.Length > 3)
               spot4 = market.SpotPrices.Value[ticker4];
            var strike = trade.Strike;

            //Note:  spread option pricing doesn't support moneyness style option yet.
            //Need a few more InitialSpotPrices for all underliers
            _sigma1 = market.VolSurfaces.Value[ticker1].GetValue(exerciseDate, trade.Strike, spot1);
            _sigma2 = market.VolSurfaces.Value[ticker2].GetValue(exerciseDate, trade.Strike, spot2);
            //double _sigma3 = 0.0, _sigma4 = 0.0;
            if (trade.UnderlyingTickers.Length > 2)
                _sigma3 = market.VolSurfaces.Value[ticker3].GetValue(exerciseDate, trade.Strike, spot3);
            if (trade.UnderlyingTickers.Length > 3)
                _sigma4 = market.VolSurfaces.Value[ticker4].GetValue(exerciseDate, trade.Strike, spot4);

            double rho13 = 0.0, rho23 = 0.0, rho14 = 0.0, rho24 = 0.0, rho34 = 0.0;
            var rho12 = market.Correlations.Value[ticker1 + ticker2].GetValue(exerciseDate, strike);

            if (trade.UnderlyingTickers.Length > 2)
            {
                rho13 = market.Correlations.Value[ticker1 + ticker3].GetValue(exerciseDate, strike);
                rho23 = market.Correlations.Value[ticker2 + ticker3].GetValue(exerciseDate, strike);
            }

            if (trade.UnderlyingTickers.Length > 3)
            {
                rho14 = market.Correlations.Value[ticker1 + ticker4].GetValue(exerciseDate, strike);
                rho24 = market.Correlations.Value[ticker2 + ticker4].GetValue(exerciseDate, strike);
                rho34 = market.Correlations.Value[ticker3 + ticker4].GetValue(exerciseDate, strike);
            }             

            var calculator = new SpreadOptionCalculator(trade.OptionType,
                trade.SpreadType,
                strike,
                spot1 * trade.Weights[0], spot2 * trade.Weights[1], spot3 * trade.Weights[2], spot4 * trade.Weights[3],
                rho12, rho13, rho14, rho23, rho24, rho34,
                _sigma1, _sigma2, _sigma3, _sigma4,
                exerciseInYears,
                riskFreeRate,
                dividendRate1, dividendRate2, dividendRate3, dividendRate4,
                trade.Weights[0], trade.Weights[1], trade.Weights[2], trade.Weights[3],
                trade.Notional);

            bool isExpired = trade.ExerciseDates.Last() < market.ValuationDate;
            bool isExpiredforTheta = trade.ExerciseDates.Last() <= market.ValuationDate;

            if (isExpired)
            {
                result.Pv = 0.0;
                result.Theta = 0.0;
                result.Rho = 0.0;
                result.asset1Delta = result.asset1DeltaCash = 0.0;
                result.asset2Delta = result.asset2DeltaCash = 0.0;
                result.asset3Delta = result.asset3DeltaCash = 0.0;
                result.asset4Delta = result.asset4DeltaCash = 0.0;
                result.asset1PartialDelta = 0.0;
                result.asset2PartialDelta = 0.0;
                result.asset3PartialDelta = 0.0;
                result.asset4PartialDelta = 0.0;
                result.asset1Gamma = result.asset1GammaCash = 0.0;
                result.asset2Gamma = result.asset2GammaCash = 0.0;
                result.asset3Gamma = result.asset3GammaCash = 0.0;
                result.asset4Gamma = result.asset4GammaCash = 0.0;
                result.asset1Vega = 0.0;
                result.asset2Vega = 0.0;
                result.asset3Vega = 0.0;
                result.asset4Vega = 0.0;
               
                result.asset1DDeltaDt = result.asset2DDeltaDt = result.asset3DDeltaDt = result.asset4DDeltaDt = 0.0;
                result.asset1DDeltaDvol = result.asset2DDeltaDvol = result.asset3DDeltaDvol = result.asset4DDeltaDvol = 0.0;
                result.asset1DVegaDvol = result.asset2DVegaDvol = result.asset3DVegaDvol = result.asset4DVegaDvol = 0.0;
                result.asset1DVegaDt = result.asset2DVegaDt = result.asset3DVegaDt = result.asset4DVegaDt = 0.0;
                result.crossGamma12 = result.crossGamma13 = result.crossGamma14 = result.crossGamma23 = result.crossGamma24 = result.crossGamma34 = 0.0;
                result.crossVomma12 = result.crossVomma13 = result.crossVomma14 = result.crossVomma23 = result.crossVomma24 = result.crossVomma34 = 0.0;

                SetPricingVol(result, notPricing: true);
            }
            else
            {
                if (result.IsRequested(PricingRequest.Pv))
                {
                    result.Pv = calculator.Pv;
                }

                if (AnalyticalOptionPricerUtil.isBasicPricing(result))
                {                
                    result.Rho = calculator.Rho;
                    result.asset1Delta = calculator.asset1Delta;
                    result.asset2Delta = calculator.asset2Delta;
                    result.asset3Delta = calculator.asset3Delta;
                    result.asset4Delta = calculator.asset4Delta;
                    result.asset1DeltaCash = calculator.asset1Delta * spot1;
                    result.asset2DeltaCash = calculator.asset2Delta * spot2;
                    result.asset3DeltaCash = calculator.asset3Delta * spot3;
                    result.asset4DeltaCash = calculator.asset4Delta * spot4;
                    result.asset1PartialDelta = calculator.asset1PartialDelta;
                    result.asset2PartialDelta = calculator.asset2PartialDelta;
                    result.asset3PartialDelta = calculator.asset3PartialDelta;
                    result.asset4PartialDelta = calculator.asset4PartialDelta;
                    result.asset1Gamma = calculator.asset1Gamma;
                    result.asset2Gamma = calculator.asset2Gamma;
                    result.asset3Gamma = calculator.asset3Gamma;
                    result.asset4Gamma = calculator.asset4Gamma;
                    result.asset1GammaCash = calculator.asset1Gamma * spot1 * spot1 / 100;
                    result.asset2GammaCash = calculator.asset2Gamma * spot2 * spot2 / 100;
                    result.asset3GammaCash = calculator.asset3Gamma * spot3 * spot3 / 100;
                    result.asset4GammaCash = calculator.asset4Gamma * spot4 * spot4 / 100;
                    result.asset1Vega = calculator.asset1Vega;
                    result.asset2Vega = calculator.asset2Vega;
                    result.asset3Vega = calculator.asset3Vega;
                    result.asset4Vega = calculator.asset4Vega;
                    result.Theta = (isExpiredforTheta) ? 0.0 : calculator.Theta;

                }

                if (AnalyticalOptionPricerUtil.isHighOrderPricing(result))
                {
                    result.asset1DDeltaDvol = calculator.asset1DDeltaDvol;
                    result.asset2DDeltaDvol = calculator.asset2DDeltaDvol;
                    result.asset3DDeltaDvol = calculator.asset3DDeltaDvol;
                    result.asset4DDeltaDvol = calculator.asset4DDeltaDvol;
                    result.asset1DVegaDvol = calculator.asset1DVegaDvol;
                    result.asset2DVegaDvol = calculator.asset2DVegaDvol;
                    result.asset3DVegaDvol = calculator.asset3DVegaDvol;
                    result.asset4DVegaDvol = calculator.asset4DVegaDvol;

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

                    if (isExpiredforTheta)
                    {
                        result.asset1DDeltaDt = result.asset2DDeltaDt = result.asset3DDeltaDt = result.asset4DDeltaDt = 0.0;
                        result.asset1DVegaDt = result.asset2DVegaDt = result.asset3DVegaDt = result.asset4DVegaDt = 0.0;
                    }
                    else
                    {
                        result.asset1DDeltaDt = calculator.asset1DDeltaDt;
                        result.asset2DDeltaDt = calculator.asset2DDeltaDt;
                        result.asset3DDeltaDt = calculator.asset3DDeltaDt;
                        result.asset4DDeltaDt = calculator.asset4DDeltaDt;
                        result.asset1DVegaDt = calculator.asset1DVegaDt;
                        result.asset2DVegaDt = calculator.asset2DVegaDt;
                        result.asset3DVegaDt = calculator.asset3DVegaDt;
                        result.asset4DVegaDt = calculator.asset4DVegaDt;
                    }                                      
                }

                SetPricingVol(result);
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

            //Bjerksund and Stensland Approximation
            //See also http://www.opus-finance.com/sites/default/files/Fichier_Site_Opus/Article_recherche/Articles_externes/2013/Closed_form_spread_option_valuation/Closed_form_spread_option_valuation.pdf
            //Payoff: max(S1-S2-K,0)
            private double Bjerksund(double S1, double S2, double T, double r, double b1, double b2,
               double sigma1, double sigma2, double rho12)
            {
                double F1 = S1 * Math.Exp(b1 * T);
                double F2 = S2 * Math.Exp(b2 * T);
                double a = F2 + _X;
                double m = F2 / (F2 + _X);
                double sigma = Math.Sqrt(sigma1 * sigma1 - 2 * m * rho12 * sigma1 * sigma2 + m * m * sigma2 * sigma2);

                double d1 = (Math.Log(F1 / a) + (0.5 * sigma1 * sigma1 - m * rho12 * sigma1 * sigma2 + 0.5 * m * m * sigma2 * sigma2) * T) / sigma / Math.Sqrt(T);
                double d2 = (Math.Log(F1 / a) + (-0.5 * sigma1 * sigma1 + rho12 * sigma1 * sigma2 + 0.5 * m * m * sigma2 * sigma2 - m * sigma2 * sigma2) * T) / sigma / Math.Sqrt(T);
                double d3 = (Math.Log(F1 / a) + (-0.5 * sigma1 * sigma1 + 0.5 * m * m * sigma2 * sigma2) * T) / sigma / Math.Sqrt(T);

                double pv = Math.Exp(-r * T) * (F1 * NormalCdf.NormalCdfHart(d1) - F2 * NormalCdf.NormalCdfHart(d2) - _X * NormalCdf.NormalCdfHart(d3));
                switch (_optionType)
                {
                    case OptionType.Call:
                        return pv;
                    default:
                        return pv - Math.Exp(-r * T) * (F1 - F2 - _X);
                }
            }

            //RIKARD GREEN 2015: Closed Form Valuation of Three-Asset Spread Options With a view towards Clean Dark Spreads
            //Extended Bjerksund Approximation 
            //See also https://www.lusem.lu.se/media/kwc/working-papers/2015/wp-wp-2015-3.pdf
            //Payoff: Max(S1-S2-S3-X, 0)
            private double ExtendedBjerksund(double S1, double S2, double S3, double T, double r, double b1, double b2, double b3,
                double sigma1, double sigma2, double sigma3, double rho12, double rho23, double rho13)
            {
                double F1 = S1 * Math.Exp(b1 * T);
                double F2 = S2 * Math.Exp(b2 * T);
                double F3 = S3 * Math.Exp(b3 * T);

                double a = F2 + F3 + _X;
                double n2 = F2 / (F2 + F3 + _X);
                double n3 = F3 / (F2 + F3 + _X);
                double sigma = Math.Sqrt(sigma1 * sigma1 + n2 * n2 * sigma2 * sigma2 + n3 * n3 * sigma3 * sigma3
                    - 2 * n2 * sigma1 * sigma2 * rho12 - 2 * n3 * sigma1 * sigma3 * rho13 + 2 * n2 * n3 * sigma2 * sigma3 * rho23);

                double d1 = (Math.Log(F1 / a) + (0.5 * sigma1 * sigma1 + 0.5 * n2 * n2 * sigma2 * sigma2 + 0.5 * sigma3 * sigma3 * n3 * n3
                    - n2 * sigma1 * sigma2 * rho12 - n3 * sigma1 * sigma3 * rho13 + n2 * n3 * sigma2 * sigma3 * rho23) * T) / sigma / Math.Sqrt(T);
                double d2 = (Math.Log(F1 / a) + (-0.5 * sigma1 * sigma1 + 0.5 * n2 * n2 * sigma2 * sigma2 + 0.5 * sigma3 * sigma3 * n3 * n3
                   - n2 * sigma2 * sigma2 + n2 * n3 * sigma2 * sigma3 * rho23 - n3 * sigma2 * sigma3 * rho23 + sigma1 * sigma2 * rho12) * T) / sigma / Math.Sqrt(T);
                double d3 = (Math.Log(F1 / a) + (-0.5 * sigma1 * sigma1 + 0.5 * n2 * n2 * sigma2 * sigma2 + 0.5 * sigma3 * sigma3 * n3 * n3
                   - n3 * sigma3 * sigma3 + n2 * n3 * sigma2 * sigma3 * rho23 - n2 * sigma2 * sigma3 * rho23 + sigma1 * sigma3 * rho13) * T) / sigma / Math.Sqrt(T);
                double d4 = (Math.Log(F1 / a) + (-0.5 * sigma1 * sigma1 + 0.5 * n2 * n2 * sigma2 * sigma2 + 0.5 * sigma3 * sigma3 * n3 * n3
                   + n2 * n3 * sigma2 * sigma3 * rho23) * T) / sigma / Math.Sqrt(T);

                double pv = Math.Exp(-r * T) * (F1 * NormalCdf.NormalCdfHart(d1) - F2 * NormalCdf.NormalCdfHart(d2) - F3 * NormalCdf.NormalCdfHart(d3) - _X * NormalCdf.NormalCdfHart(d4));
                switch (_optionType)
                {
                    case OptionType.Call:
                        return pv;
                    default:
                        return pv - Math.Exp(-r * T) * (F1 - F2 - F3 - _X);
                }
            }

            //Tommaso Pellegrino 2016: A General Closed Form Approximation Pricing Formula for Basket and Multi-Asset Spread Options
            //Extended Basket Bjerksund Approximation 
            //See also http://file.scirp.org/pdf/JMF_2016113016245890.pdf
            //Payoff: Max(S1+S2-S3-X, 0)
            private double BasketExtendedBjerksund(double S1, double S2, double S3, double T, double r, double b1, double b2, double b3,
                double sigma1, double sigma2, double sigma3, double rho12, double rho23, double rho31)
            {
                double F1 = S1 * Math.Exp(b1 * T);
                double F2 = S2 * Math.Exp(b2 * T);
                double F3 = S3 * Math.Exp(b3 * T);
                double F = Math.Log(F1 + F2);
                double K = Math.Log(F3 + _X);
                double n1 = F1 / Math.Exp(F);
                double n2 = F2 / Math.Exp(F);
                double n3 = F3 / Math.Exp(K);

                double m1 = F1 / Math.Exp(F);
                double m2 = F2 / Math.Exp(F);
                double m3 = -F3 / Math.Exp(F);
                double sigma1New = m1 * sigma1;
                double sigma2New = m2 * sigma2;
                double sigma3New = m3 * sigma3;

                double sigma = Math.Sqrt(sigma1New * sigma1New + sigma2New * sigma2New + sigma3New * sigma3New
                    + 2 * rho12 * sigma1New * sigma2New + 2 * rho23 * sigma2New * sigma3New + 2 * rho31 * sigma3New * sigma1New);
                double d = (K - F - T * (n1 * (b1 - sigma1 * sigma1 * 0.5) - n3 * (b3 - sigma3 * sigma3 * 0.5) + n2 * (b2 - sigma2 * sigma2 * 0.5))) / sigma / Math.Sqrt(T);
                double c1 = (sigma1 * n1 - sigma3 * n3 * rho31 + rho12 * sigma2 * n2) / sigma;
                double c2 = (sigma2 * n2 - sigma3 * n3 * rho23 + rho12 * sigma1 * n1) / sigma;
                double c3 = (-sigma3 * n3 + sigma1 * n1 * rho31 + rho23 * sigma2 * n2) / sigma;
                switch (_optionType)
                {
                    case OptionType.Call:
                        return Math.Exp(-r * T) * (F1 * NormalCdf.NormalCdfHart(sigma1 * c1 * Math.Sqrt(T) - d) + F2 * NormalCdf.NormalCdfHart(sigma2 * c2 * Math.Sqrt(T) - d)
                    - F3 * NormalCdf.NormalCdfHart(sigma3 * c3 * Math.Sqrt(T) - d) - _X * NormalCdf.NormalCdfHart(-d));
                    default:
                        return -1.0 * (Math.Exp(-r * T) * (F1 * NormalCdf.NormalCdfHart(-sigma1 * c1 * Math.Sqrt(T) + d) + F2 * NormalCdf.NormalCdfHart(-sigma2 * c2 * Math.Sqrt(T) + d)
                    - F3 * NormalCdf.NormalCdfHart(-sigma3 * c3 * Math.Sqrt(T) + d) - _X * NormalCdf.NormalCdfHart(d)));
                }

            }

            //Payoff: Max(S1-S2-S3-S4-X, 0)
            private double FourExtendedBjerksund(double S1, double S2, double S3, double S4, double T, double r, double b1, double b2, double b3, double b4,
                double sigma1, double sigma2, double sigma3, double sigma4, double rho12, double rho13, double rho14, double rho23, double rho24, double rho34)
            {
                double F1 = S1 * Math.Exp(b1 * T);
                double F2 = S2 * Math.Exp(b2 * T);
                double F3 = S3 * Math.Exp(b3 * T);
                double F4 = S4 * Math.Exp(b4 * T);
                double F = Math.Log(F1);
                double K = Math.Log(F2 + F3 + F4 + _X);
                double n1 = F1 / Math.Exp(F);
                double n2 = F2 / Math.Exp(K);
                double n3 = F3 / Math.Exp(K);
                double n4 = F4 / Math.Exp(K);

                double m1 = F1 / Math.Exp(F);
                double m2 = -F2 / Math.Exp(F);
                double m3 = -F3 / Math.Exp(F);
                double m4 = -F4 / Math.Exp(F);
                double sigma1New = m1 * sigma1;
                double sigma2New = m2 * sigma2;
                double sigma3New = m3 * sigma3;
                double sigma4New = m4 * sigma4;

                double sigma = Math.Sqrt(sigma1New * sigma1New + sigma2New * sigma2New + sigma3New * sigma3New + sigma4New * sigma4New
                    + 2 * rho12 * sigma1New * sigma2New + 2 * rho13 * sigma3New * sigma1New + 2 * rho14 * sigma1New * sigma4New
                    + 2 * rho23 * sigma2New * sigma3New + 2 * rho24 * sigma2New * sigma4New + 2 * rho34 * sigma3New * sigma4New);
                double d = (K - F - T * (n1 * (b1 - sigma1 * sigma1 * 0.5) - n3 * (b3 - sigma3 * sigma3 * 0.5) - n2 * (b2 - sigma2 * sigma2 * 0.5) - n4 * (b4 - sigma4 * sigma4 * 0.5))) / sigma / Math.Sqrt(T);
                double c1 = (sigma1 * n1 - rho12 * sigma2 * n2 - rho13 * sigma3 * n3 - rho14 * sigma4 * n4) / sigma;
                double c2 = (sigma1 * n1 * rho12 - sigma2 * n2 - rho23 * sigma3 * n3 - rho24 * sigma4 * n4) / sigma;
                double c3 = (sigma1 * n1 * rho13 - rho23 * sigma2 * n2 - sigma3 * n3 - rho34 * sigma4 * n4) / sigma;
                double c4 = (sigma1 * n1 * rho14 - rho24 * sigma2 * n2 - sigma3 * n3 * rho34 - sigma4 * n4) / sigma;
                switch (_optionType)
                {
                    case OptionType.Call:
                        return Math.Exp(-r * T) * (F1 * NormalCdf.NormalCdfHart(sigma1 * c1 * Math.Sqrt(T) - d) - F2 * NormalCdf.NormalCdfHart(sigma2 * c2 * Math.Sqrt(T) - d)
                    - F3 * NormalCdf.NormalCdfHart(sigma3 * c3 * Math.Sqrt(T) - d) - F4 * NormalCdf.NormalCdfHart(sigma4 * c4 * Math.Sqrt(T) - d) - _X * NormalCdf.NormalCdfHart(-d));
                    default:
                        return -1.0 * (Math.Exp(-r * T) * (F1 * NormalCdf.NormalCdfHart(-sigma1 * c1 * Math.Sqrt(T) + d) - F2 * NormalCdf.NormalCdfHart(-sigma2 * c2 * Math.Sqrt(T) + d)
                    - F3 * NormalCdf.NormalCdfHart(-sigma3 * c3 * Math.Sqrt(T) + d) - F4 * NormalCdf.NormalCdfHart(-sigma4 * c4 * Math.Sqrt(T) + d) - _X * NormalCdf.NormalCdfHart(d)));
                }

            }

            //Payoff: Max(S1+S2-S3-S4-X, 0)
            private double FourBasketExtendedBjerksundType1(double S1, double S2, double S3, double S4, double T, double r, double b1, double b2, double b3, double b4,
                double sigma1, double sigma2, double sigma3, double sigma4, double rho12, double rho13, double rho14, double rho23, double rho24, double rho34)
            {
                double F1 = S1 * Math.Exp(b1 * T);
                double F2 = S2 * Math.Exp(b2 * T);
                double F3 = S3 * Math.Exp(b3 * T);
                double F4 = S4 * Math.Exp(b4 * T);
                double F = Math.Log(F1 + F2);
                double K = Math.Log(F3 + F4 + _X);
                double n1 = F1 / Math.Exp(F);
                double n2 = F2 / Math.Exp(F);
                double n3 = F3 / Math.Exp(K);
                double n4 = F4 / Math.Exp(K);

                double m1 = F1 / Math.Exp(F);
                double m2 = F2 / Math.Exp(F);
                double m3 = -F3 / Math.Exp(F);
                double m4 = -F4 / Math.Exp(F);
                double sigma1New = m1 * sigma1;
                double sigma2New = m2 * sigma2;
                double sigma3New = m3 * sigma3;
                double sigma4New = m4 * sigma4;

                double sigma = Math.Sqrt(sigma1New * sigma1New + sigma2New * sigma2New + sigma3New * sigma3New + sigma4New * sigma4New
                    + 2 * rho12 * sigma1New * sigma2New + 2 * rho13 * sigma3New * sigma1New + 2 * rho14 * sigma1New * sigma4New
                    + 2 * rho23 * sigma2New * sigma3New + 2 * rho24 * sigma2New * sigma4New + 2 * rho34 * sigma3New * sigma4New);
                double d = (K - F - T * (n1 * (b1 - sigma1 * sigma1 * 0.5) + n2 * (b2 - sigma2 * sigma2 * 0.5) - n3 * (b3 - sigma3 * sigma3 * 0.5) - n4 * (b4 - sigma4 * sigma4 * 0.5))) / sigma / Math.Sqrt(T);
                double c1 = (sigma1 * n1 + rho12 * sigma2 * n2 - rho13 * sigma3 * n3 - rho14 * sigma4 * n4) / sigma;
                double c2 = (sigma1 * n1 * rho12 + sigma2 * n2 - rho23 * sigma3 * n3 - rho24 * sigma4 * n4) / sigma;
                double c3 = (sigma1 * n1 * rho13 + rho23 * sigma2 * n2 - sigma3 * n3 - rho34 * sigma4 * n4) / sigma;
                double c4 = (sigma1 * n1 * rho14 + rho24 * sigma2 * n2 - sigma3 * n3 * rho34 - sigma4 * n4) / sigma;
                switch (_optionType)
                {
                    case OptionType.Call:
                        return Math.Exp(-r * T) * (F1 * NormalCdf.NormalCdfHart(sigma1 * c1 * Math.Sqrt(T) - d) + F2 * NormalCdf.NormalCdfHart(sigma2 * c2 * Math.Sqrt(T) - d)
                    - F3 * NormalCdf.NormalCdfHart(sigma3 * c3 * Math.Sqrt(T) - d) - F4 * NormalCdf.NormalCdfHart(sigma4 * c4 * Math.Sqrt(T) - d) - _X * NormalCdf.NormalCdfHart(-d));
                    default:
                        return -1.0 * (Math.Exp(-r * T) * (F1 * NormalCdf.NormalCdfHart(-sigma1 * c1 * Math.Sqrt(T) + d) + F2 * NormalCdf.NormalCdfHart(-sigma2 * c2 * Math.Sqrt(T) + d)
                    - F3 * NormalCdf.NormalCdfHart(-sigma3 * c3 * Math.Sqrt(T) + d) - F4 * NormalCdf.NormalCdfHart(-sigma4 * c4 * Math.Sqrt(T) + d) - _X * NormalCdf.NormalCdfHart(d)));
                }
            }
            //Payoff: Max(S1+S2+S3-S4-X, 0)
            private double FourBasketExtendedBjerksundType2(double S1, double S2, double S3, double S4, double T, double r, double b1, double b2, double b3, double b4,
                double sigma1, double sigma2, double sigma3, double sigma4, double rho12, double rho13, double rho14, double rho23, double rho24, double rho34)
            {
                double F1 = S1 * Math.Exp(b1 * T);
                double F2 = S2 * Math.Exp(b2 * T);
                double F3 = S3 * Math.Exp(b3 * T);
                double F4 = S4 * Math.Exp(b4 * T);
                double F = Math.Log(F1 + F2 + F3);
                double K = Math.Log(F4 + _X);
                double n1 = F1 / Math.Exp(F);
                double n2 = F2 / Math.Exp(F);
                double n3 = F3 / Math.Exp(F);
                double n4 = F4 / Math.Exp(K);

                double m1 = F1 / Math.Exp(F);
                double m2 = F2 / Math.Exp(F);
                double m3 = F3 / Math.Exp(F);
                double m4 = -F4 / Math.Exp(F);
                double sigma1New = m1 * sigma1;
                double sigma2New = m2 * sigma2;
                double sigma3New = m3 * sigma3;
                double sigma4New = m4 * sigma4;

                double sigma = Math.Sqrt(sigma1New * sigma1New + sigma2New * sigma2New + sigma3New * sigma3New + sigma4New * sigma4New
                    + 2 * rho12 * sigma1New * sigma2New + 2 * rho13 * sigma3New * sigma1New + 2 * rho14 * sigma1New * sigma4New
                    + 2 * rho23 * sigma2New * sigma3New + 2 * rho24 * sigma2New * sigma4New + 2 * rho34 * sigma3New * sigma4New);
                double d = (K - F - T * (n1 * (b1 - sigma1 * sigma1 * 0.5) + n3 * (b3 - sigma3 * sigma3 * 0.5) + n2 * (b2 - sigma2 * sigma2 * 0.5) - n4 * (b4 - sigma4 * sigma4 * 0.5))) / sigma / Math.Sqrt(T);
                double c1 = (sigma1 * n1 + rho12 * sigma2 * n2 + rho13 * sigma3 * n3 - rho14 * sigma4 * n4) / sigma;
                double c2 = (sigma1 * n1 * rho12 + sigma2 * n2 + rho23 * sigma3 * n3 - rho24 * sigma4 * n4) / sigma;
                double c3 = (sigma1 * n1 * rho13 + rho23 * sigma2 * n2 + sigma3 * n3 - rho34 * sigma4 * n4) / sigma;
                double c4 = (sigma1 * n1 * rho14 + rho24 * sigma2 * n2 + sigma3 * n3 * rho34 - sigma4 * n4) / sigma;
                switch (_optionType)
                {
                    case OptionType.Call:
                        return Math.Exp(-r * T) * (F1 * NormalCdf.NormalCdfHart(sigma1 * c1 * Math.Sqrt(T) - d) + F2 * NormalCdf.NormalCdfHart(sigma2 * c2 * Math.Sqrt(T) - d)
                    + F3 * NormalCdf.NormalCdfHart(sigma3 * c3 * Math.Sqrt(T) - d) - F4 * NormalCdf.NormalCdfHart(sigma4 * c4 * Math.Sqrt(T) - d) - _X * NormalCdf.NormalCdfHart(-d));
                    default:
                        return -1.0 * (Math.Exp(-r * T) * (F1 * NormalCdf.NormalCdfHart(-sigma1 * c1 * Math.Sqrt(T) + d) + F2 * NormalCdf.NormalCdfHart(-sigma2 * c2 * Math.Sqrt(T) + d)
                    + F3 * NormalCdf.NormalCdfHart(-sigma3 * c3 * Math.Sqrt(T) + d) - F4 * NormalCdf.NormalCdfHart(-sigma4 * c4 * Math.Sqrt(T) + d) - _X * NormalCdf.NormalCdfHart(d)));
                }

            }

            private double CalcPV(double S1, double S2, double S3, double S4, double T, double r, double b1, double b2, double b3, double b4,
                double sigma1, double sigma2, double sigma3, double sigma4, double rho12, double rho13, double rho14, double rho23, double rho24, double rho34)
            {
                switch (_spreadType)
                {
                    case SpreadType.TwoAssetsSpread:
                        return Bjerksund(S1, S2, T, r, b1, b2, sigma1, sigma2, rho12) * _notional;
                    case SpreadType.ThreeAssetsSpread:
                        return ExtendedBjerksund(S1, S2, S3, T, r, b1, b2, b3, sigma1, sigma2, sigma3, rho12, rho23, rho13) * _notional;
                    case SpreadType.ThreeAssetsSpreadBasket:
                        return BasketExtendedBjerksund(S1, S2, S3, T, r, b1, b2, b3, sigma1, sigma2, sigma3, rho12, rho23, rho13) * _notional;
                    case SpreadType.FourAssetsSpread:
                        return FourExtendedBjerksund(S1, S2, S3, S4, T, r, b1, b2, b3, b4, sigma1, sigma2, sigma3, sigma4, rho12, rho13, rho14, rho23, rho24, rho34) * _notional;
                    case SpreadType.FourAssetsSpreadBasketType1:
                        return FourBasketExtendedBjerksundType1(S1, S2, S3, S4, T, r, b1, b2, b3, b4, sigma1, sigma2, sigma3, sigma4, rho12, rho13, rho14, rho23, rho24, rho34) * _notional;
                    case SpreadType.FourAssetsSpreadBasketType2:
                        return FourBasketExtendedBjerksundType2(S1, S2, S3, S4, T, r, b1, b2, b3, b4, sigma1, sigma2, sigma3, sigma4, rho12, rho13, rho14, rho23, rho24, rho34) * _notional;
                    default:
                        throw new PricingLibraryException($"Do not support spread option with such payoff: {_spreadType}");
                }
            }

           public double Pv => CalcPV(_S1, _S2, _S3,_S4, _T, _r, _b1, _b2, _b3, _b4,_sigma1, _sigma2, _sigma3,_sigma4, _rho12, _rho13, _rho14, _rho23, _rho24,_rho34);

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
            
            public double crossGamma13 => (CalcPV(_S1 + _w1*riskBumpSize, _S2, _S3 + _w3*riskBumpSize, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                           - CalcPV(_S1 + _w1*riskBumpSize, _S2, _S3 - _w3*riskBumpSize, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                           - CalcPV(_S1 - _w1*riskBumpSize, _S2, _S3 + _w3*riskBumpSize, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)
                           + CalcPV(_S1 - _w1*riskBumpSize, _S2, _S3 - _w3*riskBumpSize, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23, _rho24, _rho34)) / 4.0 / riskBumpSize / riskBumpSize;
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
            public double correlationVega13 => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12 , _rho13 + vegaBumpSize, _rho14, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12 , _rho13 - vegaBumpSize, _rho14, _rho23, _rho24, _rho34)) / 2.0;
            public double correlationVega14 => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12 , _rho13, _rho14 + vegaBumpSize, _rho23, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12 , _rho13, _rho14 - vegaBumpSize, _rho23, _rho24, _rho34)) / 2.0;
            public double correlationVega23 => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12 , _rho13, _rho14, _rho23 + vegaBumpSize, _rho24, _rho34)
                - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12, _rho13, _rho14, _rho23 - vegaBumpSize, _rho24, _rho34)) / 2.0;
            public double correlationVega24 => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12 , _rho13, _rho14, _rho23, _rho24 + vegaBumpSize, _rho34)
                - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12 , _rho13, _rho14, _rho23, _rho24 - vegaBumpSize, _rho34)) / 2.0;
            public double correlationVega34 => (CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12 , _rho13, _rho14, _rho23, _rho24, _rho34 + vegaBumpSize)
                - CalcPV(_S1, _S2, _S3, _S4, _T, _r, _b1, _b2, _b3, _b4, _sigma1, _sigma2, _sigma3, _sigma4, _rho12 , _rho13, _rho14, _rho23, _rho24, _rho34 - vegaBumpSize)) / 2.0;
        }

    }
}

