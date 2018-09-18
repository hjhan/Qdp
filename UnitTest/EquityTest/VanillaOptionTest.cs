using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Library.Base.Implementations;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Equity.Engines.Analytical;
using Qdp.Pricing.Library.Equity.Engines.MonteCarlo;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Ecosystem.Utilities;
using UnitTest.Utilities;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using System.Collections.Generic;
using Qdp.Pricing.Library.Common.MathMethods.VolTermStructure;
using System.Linq;
using Qdp.Pricing.Base.Interfaces;

namespace UnitTest.EquityTest
{
	[TestClass]
	public class VanillaOptionTest
	{
		[TestMethod]
		public void TestVanillaEuropeanCallMc()
		{
			var startDate = new Date(2014, 03, 18);
			var maturityDate = new Date(2015, 03, 18);
			var market = TestMarket();

			var call = new VanillaOption(startDate,
				maturityDate,
				OptionExercise.European,
				OptionType.Call,
				1.0,
				InstrumentType.Stock,
				CalendarImpl.Get("chn"),
				new Act365(),
				CurrencyCode.CNY, 
				CurrencyCode.CNY, 
				new[] { maturityDate },
				new[] { maturityDate });

			var mcengine = new GenericMonteCarloEngine(2, 200000);
			var result = mcengine.Calculate(call, market, PricingRequest.All);

			var analyticalResult = new AnalyticalVanillaEuropeanOptionEngine().Calculate(call, market, PricingRequest.All);
			Console.WriteLine("{0},{1}", analyticalResult.Pv, result.Pv);
			Console.WriteLine("{0},{1}", analyticalResult.Delta, result.Delta);
			Console.WriteLine("{0},{1}", analyticalResult.Gamma, result.Gamma);
			Console.WriteLine("{0},{1}", analyticalResult.Vega, result.Vega);
			Console.WriteLine("{0},{1}", analyticalResult.Rho, result.Rho);
			Console.WriteLine("{0},{1}", analyticalResult.Theta, result.Theta);
			Assert.AreEqual(analyticalResult.Pv, result.Pv, 0.002);
			Assert.AreEqual(analyticalResult.Delta, result.Delta, 0.02);
			Assert.AreEqual(analyticalResult.Gamma, result.Gamma, 0.05);
			Assert.AreEqual(analyticalResult.Theta, result.Theta, 1e-5);
			Assert.AreEqual(analyticalResult.Rho, result.Rho, 1e-6);
			Assert.AreEqual(analyticalResult.Vega, result.Vega, 1e-4);
		}

        [TestMethod]
        public void BasicVanillaOptionTest()
        {
            var startDate = new Date(2014, 03, 18);
            var maturityDate = new Date(2015, 03, 18);
            var valueDate = new Date(2014, 03, 18);

            #region Prepare Market
            // build discount curve and dividend curve
            var fr007CurveName = "Fr007";
            var fr007RateDefinition = new[]
            {
                new RateMktData("1D", 0.06, "Spot", "None", fr007CurveName),
                new RateMktData("5Y", 0.06, "Spot", "None", fr007CurveName),
            };

            var dividendCurveName = "Dividend";
            var dividendRateDefinition = new[]
            {
                new RateMktData("1D", 0.03, "Spot", "None", dividendCurveName),
                new RateMktData("5Y", 0.03, "Spot", "None", dividendCurveName),
            };

            var discountCurve = new YieldCurve(
                name: fr007CurveName,
                referenceDate: valueDate,
                keyPoints: fr007RateDefinition.Select(x => Tuple.Create((ITerm)new Term(x.Tenor), x.Rate)).ToArray(),
                bda: BusinessDayConvention.ModifiedFollowing,
                dayCount: new Act365(),
                calendar: CalendarImpl.Get("Chn"),
                currency: CurrencyCode.CNY,
                compound: Compound.Continuous,
                interpolation: Interpolation.CubicHermiteMonotic,
                trait: YieldCurveTrait.SpotCurve);

            var dividendCurve = new YieldCurve(
                name: dividendCurveName,
                referenceDate: valueDate,
                keyPoints: dividendRateDefinition.Select(x => Tuple.Create((ITerm)new Term(x.Tenor), x.Rate)).ToArray(),
                bda: BusinessDayConvention.ModifiedFollowing,
                dayCount: new Act365(),
                calendar: CalendarImpl.Get("Chn"),
                currency: CurrencyCode.CNY,
                compound: Compound.Continuous,
                interpolation: Interpolation.CubicHermiteMonotic,
                trait: YieldCurveTrait.SpotCurve);


            // build vol surface
            var volSurfData = new VolSurfMktData("VolSurf", 0.25);
            var volSurface = volSurfData.ToImpliedVolSurface(
                valuationDate: valueDate,
                dc: "Act365");

            // construct market
            var market = new MarketCondition(
                x => x.ValuationDate.Value = valueDate,
                x => x.DiscountCurve.Value = discountCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", dividendCurve } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volSurface } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", 1.0 } }
                );

            #endregion

            // construct a put option
            var put = new VanillaOption(startDate,
                maturityDate,
                OptionExercise.European,
                OptionType.Put,
                1.0,
                InstrumentType.Stock,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { maturityDate },
                new[] { maturityDate });

            var engine = new AnalyticalVanillaEuropeanOptionEngine();
            
            var result = engine.Calculate(put, market, PricingRequest.All);
        }

		[TestMethod]
		public void TestVanillaEuropeanOption()
		{
			var startDate=new Date(2014, 03, 18);
 			var maturityDate = new Date(2015, 03, 18);
			var market = TestMarket();

			var call = new VanillaOption(startDate,
				maturityDate,
				OptionExercise.European,
				OptionType.Call,
				1.0,
				InstrumentType.Stock,
				CalendarImpl.Get("chn"),
				new Act365(),
				CurrencyCode.CNY, 
				CurrencyCode.CNY, 
				new[] {maturityDate},
				new[] {maturityDate});

			var callResults = new[]
			{
                0.11013078647539261, //0.1101307865, //pv
				0.58659840197749225, //0.579209648680634, //delta
				1.5026934848982876, //1.50252233849235, //gamma
				-0.000158271113220379, //-0.000158271113220421, //theta
				4.69520215692007E-05, //0.00004690554, //rho
				0.00375699094019481, //0.00375690062860851 //vega
                0.25, // vol
			};

			var put = new VanillaOption(startDate,
				maturityDate,
				OptionExercise.European,
				OptionType.Put,
				1.0,
				InstrumentType.Stock,
				CalendarImpl.Get("chn"),
				new Act365(),
				CurrencyCode.CNY, 
				CurrencyCode.CNY, 
				new[] { maturityDate },
				new[] { maturityDate });
			var putResults = new[]
			{
                0.081449786511133465, //0.0814497865, //pv
				-0.38384713157104261, //-0.391235884868724, //delta
				1.5026934848963447, //1.50252235237014, //gamma
				-8.3213704703308244E-05, //-0.000083213704702975, //theta
				-4.7177359254735321E-05, //-0.000047266234126, //rho
				0.00375699094019481, //0.00375690062860852 //vega
                0.25, // vol
			};

			var engine = new AnalyticalVanillaEuropeanOptionEngine();
			var result = engine.Calculate(call, market, PricingRequest.All);
			var callCalcResults = new[]
			{
				result.Pv,
				result.Delta,
				result.Gamma,
				result.Theta,
				result.Rho,
				result.Vega,
                result.PricingVol
			};
			for (var i = 0; i < callCalcResults.Length; ++i)
			{
				Assert.AreEqual(callResults[i], callCalcResults[i], 1e-10);
			}

			result = engine.Calculate(put, market, PricingRequest.All);
			var putCalcResults = new[]
			{
				result.Pv,
				result.Delta,
				result.Gamma,
				result.Theta,
				result.Rho,
				result.Vega,
                result.PricingVol
            };
			for (var i = 0; i < callCalcResults.Length; ++i)
			{
				Assert.AreEqual(putResults[i], putCalcResults[i],  1e-10);
			}
		}

        /// <summary>
        /// to verify OTC-926
        /// </summary>
        [TestMethod]
        public void MoneynessDeltaTest()
        {
            var startDate = new Date(2018, 03, 02);
            var maturityDate = new Date(2018, 04, 27);            

            var call = new VanillaOption(startDate,
                maturityDate,
                OptionExercise.American,
                OptionType.Call,
                1.05,
                InstrumentType.Stock,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { maturityDate },
                new[] { maturityDate },
                12,
                null,
                null,
                0,
                true,
                8000);

            #region construct market
            var historiclIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates;
            var curveConvention = new CurveConvention("fr007CurveConvention",
                "CNY",
                "ModifiedFollowing",
                "Chn",
                "Act365",
                "Continuous",
                "CubicHermiteMonotic");

            var fr007CurveName = "Fr007";
            var fr007RateDefinition = new[]
            {
                new RateMktData("1D", 0.05, "Spot", "None", fr007CurveName),
                new RateMktData("5Y", 0.05, "Spot", "None", fr007CurveName),
            };

            var dividendCurveName = "Dividend";
            var dividendRateDefinition = new[]
            {
                new RateMktData("1D", 0, "Spot", "None", dividendCurveName),
                new RateMktData("5Y", 0, "Spot", "None", dividendCurveName),
            };

            var curveDefinition = new[]
            {
                new InstrumentCurveDefinition(fr007CurveName, curveConvention, fr007RateDefinition, "SpotCurve"),
                new InstrumentCurveDefinition(dividendCurveName, curveConvention, dividendRateDefinition, "SpotCurve"),
            };

            var volSurf = new[] { new VolSurfMktData("VolSurf", 0.3), };
            var marketInfo = new MarketInfo("tmpMarket", "2018-03-02", curveDefinition, historiclIndexRates, null, null, volSurf);
            QdpMarket qdpMarket;
            MarketFunctions.BuildMarket(marketInfo, out qdpMarket);

            var valuationDate = new Date(2018, 3, 2);

            //flat vol surface
            
            var surface1 = qdpMarket.GetData<VolSurfMktData>("VolSurf").ToImpliedVolSurface(valuationDate);
            // market with flat vol surface
            var market = new MarketCondition(
                x => x.ValuationDate.Value = valuationDate,
                x => x.DiscountCurve.Value = qdpMarket.GetData<CurveData>("Fr007").YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", qdpMarket.GetData<CurveData>("Dividend").YieldCurve } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", surface1 } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", 8000 } }
                );

            //a real vol surface, same as stock 600000 of 2018-03-02 in OTC system
            var maturities = new Date[]
            {
                (new Term("1W")).Next(startDate),
                (new Term("2W")).Next(startDate),
                (new Term("1M")).Next(startDate)
            };

            var strikes = new double[]
            {
                0.9,
                0.95,
                1.0,
                1.05,
                1.1                
            };

            var vols = new double[3, 5];
            for (var i = 0; i < vols.GetLength(0); ++i)
            {
                for (var j = 0; j < vols.GetLength(1); ++j)
                {
                    vols[i, j] = 0.3;
                }
            }

            vols[2, 2] = 0.4;


            var surface2 = new ImpliedVolSurface(valuationDate, maturities, strikes, vols, Interpolation2D.BiLinear);
            // market with flat vol surface
            var market2 = new MarketCondition(
                x => x.ValuationDate.Value = valuationDate,
                x => x.DiscountCurve.Value = qdpMarket.GetData<CurveData>("Fr007").YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", qdpMarket.GetData<CurveData>("Dividend").YieldCurve } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", surface2 } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", 8000 } }
                );
            #endregion

            var engine = new AnalyticalVanillaEuropeanOptionEngine();

            //result of a flat vol surface of 0.3, which is the same as pricing result in OTC-926
            var result = engine.Calculate(call, market, PricingRequest.All);

            //result of real vol surface, which is the same as scenario analysis in OTC-926
            var result2 = engine.Calculate(call, market2, PricingRequest.All);
        }

		private IMarketCondition TestMarket()
		{
			var historiclIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates;

			
			var curveConvention = new CurveConvention("fr007CurveConvention",
				"CNY",
				"ModifiedFollowing",
				"Chn",
				"Act365",
				"Continuous",
				"CubicHermiteMonotic");

			var fr007CurveName = "Fr007";
			var fr007RateDefinition = new[]
			{
				new RateMktData("1D", 0.06, "Spot", "None", fr007CurveName),
				new RateMktData("5Y", 0.06, "Spot", "None", fr007CurveName),
			};

			var dividendCurveName = "Dividend";
			var dividendRateDefinition = new[]
			{
				new RateMktData("1D", 0.03, "Spot", "None", dividendCurveName),
				new RateMktData("5Y", 0.03, "Spot", "None", dividendCurveName),
			};

			var curveDefinition = new[]
			{
				new InstrumentCurveDefinition(fr007CurveName, curveConvention, fr007RateDefinition, "SpotCurve"),
				new InstrumentCurveDefinition(dividendCurveName, curveConvention, dividendRateDefinition, "SpotCurve"),
			};

			var volSurf = new[] {new VolSurfMktData("VolSurf", 0.25), };
			var marketInfo = new MarketInfo("tmpMarket","2014-02-10", curveDefinition, historiclIndexRates, null, null, volSurf);
			QdpMarket market;
			var result = MarketFunctions.BuildMarket(marketInfo, out market);

			var valuationDate = new Date(2014, 3, 18);
            var volsurf = market.GetData<VolSurfMktData>("VolSurf").ToImpliedVolSurface(valuationDate);

            return new MarketCondition(
				x => x.ValuationDate.Value = valuationDate,
				x => x.DiscountCurve.Value = market.GetData<CurveData>("Fr007").YieldCurve,
				x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>("Dividend").YieldCurve } },
				x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", 1.0 } }
				);
		}
	}
}
