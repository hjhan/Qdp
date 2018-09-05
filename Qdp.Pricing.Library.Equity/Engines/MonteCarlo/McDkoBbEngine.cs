using System;
using System.Linq;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Random;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Equity.Interfaces;
using Qdp.Pricing.Library.Equity.Options;

namespace Qdp.Pricing.Library.Equity.Engines.MonteCarlo
{
	public class McDkoBbEngine : BaseNumericalOptionEngine
	{
		public int Steps { get; private set; }
		public int NumSimulations { get; private set; }

		public McDkoBbEngine(int nsteps, int numSimulations, double spotPriceBump = 0.01)
			: base(spotPriceBump)
		{
			Steps = nsteps;
			NumSimulations = numSimulations;
		}

		protected override double CalcPv(IOption option, IMarketCondition market, double timeIncrement = 0.0)
		{
            //if (!(option is BarrierOption))
            //{
            //	throw new PricingLibraryException("Must be dko option in McDkoBbEngine!");
            //}
            //return markets.Select(m => CalcSinglePv(option as BarrierOption, m)).ToArray();
            return CalcSinglePv(option as BarrierOption, market);
        }

		private double CalcSinglePv(BarrierOption option, IMarketCondition market)
		{

			var strike = option.Strike;
			var lnK = Math.Log(strike);
			var spot = market.SpotPrices.Value.Values.First();
            var x0 = Math.Log(spot);
			var a = Math.Log(option.Barrier);
			var b = Math.Log(option.UpperBarrier);
			var exerciseDate = option.ExerciseDates.Last();
			var volatility = market.VolSurfaces.Value.Values.First().GetValue(exerciseDate, option.Strike);
			var dist = b - a;
			var tmp1 = Math.Max(a, lnK);

			var discFact = market.DiscountCurve.Value.GetDf(market.ValuationDate, exerciseDate);
			var yearFrac = option.DayCount.CalcDayCountFraction(market.ValuationDate, exerciseDate);
			var dt = yearFrac/Steps;
			var upBarrier = option.UpperBarrier;
			var lowBarrier = option.Barrier;
			var random = new MersenneTwister();


			var pvAvg = 0.0;
			var outProb = 0.0;
			for (int i = 0; i < NumSimulations; ++i)
			{
				var stockPrice = spot;
				bool isOut = false;
				var pv = 0.0;

				int j = 0;
				while (!isOut && j < Steps - 1)
				{
					// generate N(0,1) sample
					var drift = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, exerciseDate) - 0.5*volatility*volatility;
					var newStockPrice = stockPrice*Math.Exp(volatility*Math.Sqrt(dt)*Normal.Sample(random, 0, 1.0) + drift*dt);
					if (newStockPrice > option.UpperBarrier || newStockPrice < option.Barrier) // knock-out happened
					{
						isOut = true;
						outProb += 1.0/NumSimulations;
					}
					else // possible barrier crossing giving two end points
					{
						var pUp = Math.Exp(-2*Math.Log(upBarrier/stockPrice)*Math.Log(upBarrier/newStockPrice)/(dt*volatility*volatility));
						var pLow =
							Math.Exp(-2*Math.Log(lowBarrier/stockPrice)*Math.Log(lowBarrier/newStockPrice)/(dt*volatility*volatility));
						var pOut = pUp + pLow;
						if (random.NextDouble() < pOut)
						{
							isOut = true;
							outProb += 1.0/NumSimulations;
						}
					}
					stockPrice = newStockPrice;
					++j;
				}

				// payoff at exercise if not knocked out
				if (!isOut)
				{
					var drift = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, exerciseDate) - 0.5*volatility*volatility;
					stockPrice *= Math.Exp(volatility*Math.Sqrt(dt)*Normal.Sample(random, 0, 1.0) + drift*dt);
					pv = option.OptionType == OptionType.Call
						? Math.Max(stockPrice - strike, 0)
						: Math.Max(strike - stockPrice, 0);
					pvAvg += (pv + option.Coupon);
					//std::cout << "pv*discFact=" << pv*discFact << std::endl;
				}
				else
				{
					pvAvg += option.Rebate;
				}
			}
			pvAvg /= NumSimulations;
			return pvAvg * option.Notional * discFact;
		}
	}
}
