using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Common.MathMethods.Maths;


namespace Qdp.Pricing.Library.Common.Utilities
{
    static class DeltaVolStrikeConverter
    {
        public static double DeltaToStrike(OptionType optionType, double delta, double spotPrice,
            double maturityInYears, double standardDeviation, double riskFreeRate, double dividendRate)
        {
            double S = spotPrice;
            double T = maturityInYears;
            double sigma = standardDeviation;
            double r = riskFreeRate;
            double q = dividendRate;
            double b = riskFreeRate - dividendRate;

            double phi = optionType.Equals(OptionType.Call) ? 1.0 : -1.0;
            double theta = (r - q) / sigma + sigma / 2;

            double K = S * Math.Exp(-phi * NormalCdf.NormSInv(phi * delta * Math.Exp(q * T)) * sigma * Math.Sqrt(T) + sigma * theta * T);
            return K;
            
        }
    }
}
