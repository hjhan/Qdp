using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.MktCalibrationInstruments;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.DependencyTree;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Library.Base.Implementations;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Fx;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;

namespace Qdp.Pricing.Ecosystem.Market.YieldCurveDependentObjects
{
	public sealed class InstrumentCurveObject : BaseCurveObject<InstrumentCurveDefinition>
	{
		public InstrumentCurveObject()
		{
			BuildFromDependents = CurveBuiltFromDependents;
		}

		private GuidObject CurveBuiltFromDependents(DependentObject fatherNode, DependentObject[] childrenNodes)
		{
			IMarketCondition baseMarket;


			if (childrenNodes.Any(x => x is InstrumentCurveObject))
			{
				var baseCurve = (CurveData)childrenNodes.First().Value;
				baseMarket = new MarketCondition(x => x.ValuationDate.Value = Market.ReferenceDate,
					x => x.HistoricalIndexRates.Value = Market.HistoricalIndexRates,
					x => x.DiscountCurve.Value = baseCurve.YieldCurve,
					x => x.FixingCurve.Value = baseCurve.YieldCurve,
					x => x.FgnDiscountCurve.Value = baseCurve.YieldCurve);
			}
			else
			{
				baseMarket = new MarketCondition(x => x.ValuationDate.Value = Market.ReferenceDate,
					x => x.HistoricalIndexRates.Value = Market.HistoricalIndexRates);
			}

			var rateDefinitions = childrenNodes.Where(x => x.Value is RateMktData).Select(x => (RateMktData)x.Value).ToArray();
			var fxSpotDefinition = rateDefinitions.Where(x => x.InstrumentType.ToInstrumentType() == InstrumentType.FxSpot).ToArray();
			if (fxSpotDefinition.Count() > 1)
			{
				throw new PricingBaseException("A yield curve cannot have more than 2 fx rates against its base curve's currency");
			}
			if (fxSpotDefinition.Any())
			{
				baseMarket = baseMarket.UpdateCondition(
					new UpdateMktConditionPack<FxSpot[]>(x => x.FxSpots, new[] { CreateFxSpot(fxSpotDefinition[0]) }));
			}

			var curveConvention = childrenNodes.Last().Value as CurveConvention;
			if (curveConvention == null)
			{
				Logger.ErrorFormat("Curve conventions are missing!");
				return null;
			}

			var isRegridded = rateDefinitions.All(x => x.IndexType != null && x.IndexType.ToIndexType() == IndexType.Regridded);
			if (isRegridded)
			{
				var yieldCurve = baseMarket.DiscountCurve.Value;
				var tempRateDefinitions = new List<RateMktData>();
				foreach (var tenor in Definition.RegriddedTenors)
				{
					var date = new Term(tenor).Next(yieldCurve.ReferenceDate);
					if (date > yieldCurve.KeyPoints.Last().Item1)
					{
						date = yieldCurve.KeyPoints.Last().Item1;
					}
					tempRateDefinitions.Add(new RateMktData(date.ToString(), GetParRate(date, yieldCurve), "SwapParRate", "InterestRateSwap", Definition.Name));
				}
				rateDefinitions = tempRateDefinitions.ToArray();
			}

			YieldCurve instrumentCurve;
            //This branch is for bond pnl calculation
			if (rateDefinitions.All(
					x => x.InstrumentType.ToInstrumentType() == InstrumentType.Dummy || x.InstrumentType.ToInstrumentType() == InstrumentType.None))
			{
				if (rateDefinitions.All(x => x.IsTerm()))
				{
					instrumentCurve = new YieldCurve(
					Definition.Name,
					Market.ReferenceDate,
					rateDefinitions.Select(x => Tuple.Create((ITerm)new Term(x.Tenor) , x.Rate)).ToArray(),
					curveConvention.BusinessDayConvention.ToBda(),
					curveConvention.DayCount.ToDayCountImpl(),
					curveConvention.Calendar.ToCalendarImpl(),
					curveConvention.Currency.ToCurrencyCode(),
					curveConvention.Compound.ToCompound(),
					curveConvention.Interpolation.ToInterpolation(),
					Definition.Trait.ToYieldCurveTrait(),
					baseMarket
					);
				}
				else
				{
					instrumentCurve = new YieldCurve(
					Definition.Name,
					Market.ReferenceDate,
					rateDefinitions.Select(x => Tuple.Create(new Date(DateTime.Parse(x.Tenor)), x.Rate)).ToArray(),
					curveConvention.BusinessDayConvention.ToBda(),
					curveConvention.DayCount.ToDayCountImpl(),
					curveConvention.Calendar.ToCalendarImpl(),
					curveConvention.Currency.ToCurrencyCode(),
					curveConvention.Compound.ToCompound(),
					curveConvention.Interpolation.ToInterpolation(),
					Definition.Trait.ToYieldCurveTrait(),
					baseMarket
					);
				}
				
			}
			else
			{
				var mktInstruments = new List<MarketInstrument>();
				foreach (var rateDefinition in rateDefinitions)
				{
					MktInstrumentCalibMethod calibrationMethod;
                    var instType = rateDefinition.InstrumentType.ToInstrumentType();

                    switch (instType)
                    {
                        case InstrumentType.InterestRateSwap:
                            var swap = CreateIrsInstrument(rateDefinition, out calibrationMethod);
                            mktInstruments.Add(new MarketInstrument(swap, rateDefinition.Rate, calibrationMethod));
                            break;
                        case InstrumentType.Deposit:
                        case InstrumentType.Repo:
                        case InstrumentType.Ibor:
                            var deposit = CreateDepositInstrument(rateDefinition, out calibrationMethod);
                            mktInstruments.Add(new MarketInstrument(deposit, rateDefinition.Rate, calibrationMethod));
                            break;

                        case InstrumentType.Dummy:
                        case InstrumentType.None:
                            var dummy = CreateDummyInstrument(rateDefinition);
                            mktInstruments.Add(new MarketInstrument(dummy, rateDefinition.Rate, MktInstrumentCalibMethod.Default));
                            break;

                        case InstrumentType.CreditDefaultSwap:
                            var cds = CreateCreditDefaultSwap(rateDefinition);
                            mktInstruments.Add(new MarketInstrument(cds, rateDefinition.Rate, MktInstrumentCalibMethod.Default));
                            break;

                        case InstrumentType.CommodityForward:
                            mktInstruments.Add(new MarketInstrument(CreateCommodityForward(rateDefinition), rateDefinition.Rate, MktInstrumentCalibMethod.Default));
                            break;

                        case InstrumentType.CommoditySpot:
                            //baseMarket = baseMarket.UpdateCondition(new UpdateMktConditionPack<double>(x => x.SpotPrices, rateDefinition.Rate));
                            baseMarket = baseMarket.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, double>>(x => x.SpotPrices, new Dictionary<string, double> { { "", rateDefinition.Rate } }));
                            break;

                        case InstrumentType.FxSpot:
                            break;

                        default:
                            throw new PricingLibraryException("Unrecognized product type in calibrating curve.");


                    }
                    #region legacy code

                    //               if (rateDefinition.InstrumentType.ToInstrumentType() == InstrumentType.InterestRateSwap)
                    //{
                    //	var swap = CreateIrsInstrument(rateDefinition, out calibrationMethod);
                    //	mktInstruments.Add(new MarketInstrument(swap, rateDefinition.Rate, calibrationMethod));
                    //}
                    //else if ( instType == InstrumentType.Deposit 
                    //	|| rateDefinition.InstrumentType.ToInstrumentType() == InstrumentType.Repo
                    //	|| rateDefinition.InstrumentType.ToInstrumentType() == InstrumentType.Ibor)
                    //{
                    //	var deposit = CreateDepositInstrument(rateDefinition, out calibrationMethod);
                    //	mktInstruments.Add(new MarketInstrument(deposit, rateDefinition.Rate, calibrationMethod));
                    //}
                    //else if (rateDefinition.InstrumentType.ToInstrumentType() == InstrumentType.Dummy ||
                    //				 rateDefinition.InstrumentType.ToInstrumentType() == InstrumentType.None)
                    //{
                    //	var dummy = CreateDummyInstrument(rateDefinition);
                    //	mktInstruments.Add(new MarketInstrument(dummy, rateDefinition.Rate, MktInstrumentCalibMethod.Default));
                    //}
                    //else if (rateDefinition.InstrumentType.ToInstrumentType() == InstrumentType.CreditDefaultSwap)
                    //{
                    //	var cds = CreateCreditDefaultSwap(rateDefinition);
                    //	mktInstruments.Add(new MarketInstrument(cds, rateDefinition.Rate, MktInstrumentCalibMethod.Default));
                    //}
                    //else if (rateDefinition.InstrumentType.ToInstrumentType() == InstrumentType.CommodityForward)
                    //{
                    //	mktInstruments.Add(new MarketInstrument(CreateCommodityForward(rateDefinition), rateDefinition.Rate, MktInstrumentCalibMethod.Default));
                    //}
                    //else if (rateDefinition.InstrumentType.ToInstrumentType() == InstrumentType.CommoditySpot)
                    //{
                    //                   //baseMarket = baseMarket.UpdateCondition(new UpdateMktConditionPack<double>(x => x.SpotPrices, rateDefinition.Rate));
                    //                   baseMarket = baseMarket.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, double>>(x => x.SpotPrices, new Dictionary<string, double> { {"", rateDefinition.Rate } }));
                    //               }
                    //else if (rateDefinition.InstrumentType.ToInstrumentType() == InstrumentType.FxSpot)
                    //{

                    //}
                    //else
                    //{
                    //	throw new PricingLibraryException("Unrecognized product type in calibrating curve.");
                    //}
                    #endregion legacy code
                }

                var isSpcCurve = rateDefinitions.All(x => x.IndexType != null && x.IndexType.ToIndexType() == IndexType.Spc);
				var isConvenicenYield = rateDefinitions.All(x => x.IndexType != null && x.IndexType.ToIndexType() == IndexType.ConvenienceYield);
				

				Expression<Func<IMarketCondition, object>>[] expression = null;
				if (isSpcCurve) expression = new Expression<Func<IMarketCondition, object>>[] {x => x.SurvivalProbabilityCurve};
				if (isConvenicenYield) expression = new Expression<Func<IMarketCondition, object>>[] { x => x.DividendCurves };

				instrumentCurve = new YieldCurve(
					Definition.Name,
					Market.ReferenceDate,
					mktInstruments.ToArray(),
					curveConvention.BusinessDayConvention.ToBda(),
					curveConvention.DayCount.ToDayCountImpl(),
					curveConvention.Calendar.ToCalendarImpl(),
					curveConvention.Currency.ToCurrencyCode(),
					curveConvention.Compound.ToCompound(),
					curveConvention.Interpolation.ToInterpolation(),
					Definition.Trait.ToYieldCurveTrait(),
					baseMarket,
					expression,
					null
					);
			}
			return new CurveData(instrumentCurve);

		}

		private double GetParRate(Date date, IYieldCurve yieldCurve)
		{
			if (date < yieldCurve.ReferenceDate)
			{
				return 0.0;
			}
			var irsJson = MktInstrumentIrsRule.MktIrsRule[IndexType.SwapParRate];
			var irsInfo = irsJson.InterestRateSwapInfo;
			var calendar = irsInfo.Calendar.ToCalendarImpl();
			var frequency = irsInfo.Currency.ToFrequency();
			var stub = irsInfo.FixedLegFreq.ToStub();
			var dayCount = irsInfo.FixedLegDC.ToDayCountImpl();
			var bda = date <= new Term("1M").Next(yieldCurve.ReferenceDate) ? BusinessDayConvention.Following : irsInfo.FixedLegBD.ToBda();
			var endDate = bda.Adjust(calendar, date);
			var schedule = new Schedule(
				yieldCurve.ReferenceDate,
				endDate,
				frequency.GetTerm(),
				stub,
				calendar,
				bda
				).ToArray();

			var fixedPv = 0.0;
			for (var i = 1; i < schedule.Length; ++i)
			{
				fixedPv += dayCount.CalcDayCountFraction(schedule[i - 1], schedule[i]) * yieldCurve.GetDf(yieldCurve.ReferenceDate, schedule[i]);
			}
			return ((1 - yieldCurve.GetDf(yieldCurve.ReferenceDate, schedule.Last())) / fixedPv);

		}
	}
}
