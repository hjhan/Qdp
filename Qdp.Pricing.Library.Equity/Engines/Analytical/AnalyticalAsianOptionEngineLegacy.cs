using System;
using System.Linq;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Equity.Options;
using System.Collections.Generic;

namespace Qdp.Pricing.Library.Equity.Engines.Analytical
{
	//Haug P231 HHM
	//Fixings must be correct, and contains no useless inputs
	public class AnalyticalAsianOptionEngineLegacy : Engine<AsianOption>
	{
		public override IPricingResult Calculate(AsianOption trade, IMarketCondition market, PricingRequest request)
		{
			var result = new PricingResult(market.ValuationDate, request);

			var exerciseDate = trade.ExerciseDates.Last();
			var remainingObsDates = trade.ObservationDates.Where(x => x > market.ValuationDate).ToArray();
			var n = trade.ObservationDates.Count();
			var n2 = remainingObsDates.Count();
			var m = n - n2;
			if (trade.Fixings.Count != m)
			{
				throw	new PricingLibraryException("AsianOption: number of fixings does not match!");
			}

			var fixingAverage = trade.Fixings.Any() ? trade.Fixings.Average(x => x.Value) : 0.0;


			if (!remainingObsDates.Any())
			{
				//already fix on all observation days
				var cfs = trade.GetPayoff(new[] { fixingAverage });
				result.Pv = cfs.Sum(x => x.PaymentAmount*market.DiscountCurve.Value.GetDf(market.ValuationDate, x.PaymentDate));
				result.Delta = 0.0;
                result.DeltaCash = 0.0;
                result.Gamma = 0.0;
                result.GammaCash = 0.0;
				result.Vega = 0.0;
				result.Theta = 0.0;
				result.Rho = 0.0;
				return result;
			}

            //TODO: do it properly
            var newSpot = market.SpotPrices.Value.Values.First();
            
            var sigma = market.VolSurfaces.Value.Values.First().GetValue(exerciseDate, trade.Strike, newSpot);

            var newSigma = market.VolSurfaces.Value.Values.First().GetValue(exerciseDate, trade.Strike, newSpot);
			double newStrike;
			var factor = 1.0;

			if (remainingObsDates.Count() == 1)
			{
				newStrike = trade.ObservationDates.Count()*trade.Strike - (n - 1)*trade.Fixings.Average(x => x.Value);
				factor = 1.0/n;
			}
			else
			{
                var T = AnalyticalOptionPricerUtil.timeToMaturityFraction(market.ValuationDate, exerciseDate, trade);
                var T2 = AnalyticalOptionPricerUtil.timeToMaturityFraction(remainingObsDates[0], remainingObsDates.Last(), trade);
                var t1 = AnalyticalOptionPricerUtil.timeToMaturityFraction(market.ValuationDate, remainingObsDates[0], trade);

                var h = T2/(n2-1);
				var S = market.SpotPrices.Value.Values.First();
				var riskFreeRate = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, exerciseDate);
				var dividendRate = market.DividendCurves.Value.Values.First().ZeroRate(market.ValuationDate, exerciseDate);
				var b = riskFreeRate - dividendRate;
				var sigma2 = sigma * sigma;

				double eat, eat2;
				if (riskFreeRate.AlmostEqual(dividendRate))
				{
					eat = market.SpotPrices.Value.Values.First();
					
					eat2 = S * S * Math.Exp(sigma2 * t1) / n / n *
								 (
									 (1 - Math.Exp(sigma2 * h * n)) / (1 - Math.Exp(sigma2 * h)) +
									 2.0 / (1 - Math.Exp(sigma2*h)) *
									 (n - (1 - Math.Exp(sigma2 * h * n)) / (1 - Math.Exp(sigma2 * h)))
									);                                                                                                                                                            
				}
				else
				{
					
					var o = 2 * b + sigma2;
					eat = S/n2*Math.Exp(b*t1)*(1-Math.Exp(b*h*n2))/(1-Math.Exp(b*h));
					eat2 = S*S*Math.Exp(o*t1)/n/n*
					       (
						       (1 - Math.Exp(o*h*n))/(1 - Math.Exp(o*h)) +
						       2.0/(1 - Math.Exp((b + sigma2)*h))*
						       ((1 - Math.Exp(b*h*n))/(1 - Math.Exp(b*h)) - (1 - Math.Exp(o*h*n))/(1 - Math.Exp(o*h)))
						      );
				}

				newSpot = eat;
				newSigma = Math.Sqrt(Math.Log(eat2/eat/eat)/T);
                newStrike = (n * trade.Strike - m * fixingAverage) / (n - m) - m / (n - m);

				factor = 1.0*(n - m)/n;
			}

			var newTrade = new VanillaOption(trade.StartDate,
				trade.UnderlyingMaturityDate,
				trade.Exercise,
				trade.OptionType,
				newStrike,
                trade.UnderlyingProductType,
				trade.Calendar,
				trade.DayCount,
				trade.PayoffCcy,
				trade.SettlementCcy,
				trade.ExerciseDates,
				trade.ObservationDates,
				trade.Notional,
				trade.SettlmentGap,
				trade.OptionPremiumPaymentDate,
				trade.OptionPremium);

            var newVol = market.VolSurfaces.Value.Values.First().BumpVolSurf(newSigma - sigma);
            var newMarket = market.UpdateCondition(
				new UpdateMktConditionPack<Dictionary<string, double>>(x => x.SpotPrices, new Dictionary<string, double> { {"", newSpot } }),
				new UpdateMktConditionPack<Dictionary<string, IVolSurface>>(x => x.VolSurfaces, new Dictionary<string, IVolSurface> { {"", newVol } } )
			);

			var bsEngine = new AnalyticalVanillaEuropeanOptionEngine();
			result = (PricingResult)bsEngine.Calculate(newTrade, newMarket, request);
			result.Pv *= factor;
			result.Delta *= factor;
			result.Gamma *= factor;
			result.Vega *= factor;
			result.Theta *= factor;
			result.Rho *= factor;

			return result;
		}
	}
}
