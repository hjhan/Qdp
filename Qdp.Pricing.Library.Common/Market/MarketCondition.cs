using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Qdp.Foundation.Implementations;
using Qdp.Foundation.Utilities;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Base.Interfaces;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Fx;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Market
{

	public class MarketCondition : IMarketCondition
	{
        public SetOnce<Date> ValuationDate { get; private set; }
		public SetOnce<Dictionary<string, IYieldCurve>> YieldCurves { get; private set; }
		public SetOnce<Dictionary<string, Tuple<PriceQuoteType, double>>> MktQuote { get; private set; }
		public SetOnce<ISpread> CreditSpread { get; private set; }
		public SetOnce<Dictionary<string, double>> SpotPrices { get; private set; }
        //Fx fixing:
        public SetOnce<Dictionary<string, double>> FxSpot { get; private set; }
        //public SetOnce<double> SpotPriceNew { get; private set; }
        public SetOnce<Dictionary<string, IVolSurface>> Correlations { get; private set; }

        public SetOnce<IYieldCurve> DiscountCurve { get; private set; }
		public SetOnce<IYieldCurve> FixingCurve { get; private set; }
		public SetOnce<IYieldCurve> RiskfreeCurve { get; private set; }
		public SetOnce<Dictionary<string, IYieldCurve>> DividendCurves { get; private set; }

        public SetOnce<IYieldCurve> UnderlyingDiscountCurve { get; private set; }
		public SetOnce<Dictionary<IndexType, SortedDictionary<Date, double>>> HistoricalIndexRates { get; private set; }
		public SetOnce<Dictionary<string, IVolSurface>> VolSurfaces { get; private set; }
        //public SetOnce<IVolSurface> VolSurfaceNew { get; private set; }
        public SetOnce<IYieldCurve> FgnDiscountCurve { get; private set; }
		public SetOnce<IYieldCurve> FgnFixingCurve { get; private set; }
		public SetOnce<FxSpot[]> FxSpots { get; private set; }
		public SetOnce<IYieldCurve> SettlementCcyDiscountCurve { get; private set; }
		public SetOnce<IYieldCurve> SettlementCcyFixingCurve { get; private set; }
		public SetOnce<IYieldCurve> SurvivalProbabilityCurve { get; private set; }

		public MarketCondition(params Action<MarketCondition>[] actions)
		{
			ValuationDate = new SetOnce<Date>("ValuationDate");
			MktQuote = new SetOnce<Dictionary<string, Tuple<PriceQuoteType, double>>>("MktQuote");
			CreditSpread = new SetOnce<ISpread>("CreditSpread");
			SpotPrices = new SetOnce<Dictionary<string, double>>("SpotPrice");
            FxSpot = new SetOnce<Dictionary<string, double>>("FxSpot");
            Correlations = new SetOnce<Dictionary<string, IVolSurface>>("Correlations");
            DiscountCurve = new SetOnce<IYieldCurve>("DiscountCurve");
			FixingCurve = new SetOnce<IYieldCurve>("FixingCurve");
			RiskfreeCurve = new SetOnce<IYieldCurve>("RiskfreeCurve");
			UnderlyingDiscountCurve = new SetOnce<IYieldCurve>("UnderlyingDiscountCurve");
			DividendCurves = new SetOnce<Dictionary<string, IYieldCurve>>("DividendCurve");
            VolSurfaces = new SetOnce<Dictionary<string, IVolSurface >>("VolSurface");
            FgnDiscountCurve = new SetOnce<IYieldCurve>("FgnDiscountCurve");
			FgnFixingCurve = new SetOnce<IYieldCurve>("FgnFixingCurve");
			FxSpots = new SetOnce<FxSpot[]>("FxSpots");

			SettlementCcyDiscountCurve = new SetOnce<IYieldCurve>("SettlementCcyDiscountCurve");
			SettlementCcyFixingCurve = new SetOnce<IYieldCurve>("SettlementCcyFixingCurve");

			SurvivalProbabilityCurve = new SetOnce<IYieldCurve>("SurvialProbabilityCurve");

			HistoricalIndexRates = new SetOnce<Dictionary<IndexType, SortedDictionary<Date, double>>>("HistoricalIndexRates");
			YieldCurves = new SetOnce<Dictionary<string, IYieldCurve>>("YieldCurves");
			foreach (var action in actions)
			{
				action(this);
			}
		}

        public IMarketCondition UpdateDividendCurve( IYieldCurve diviCurve, string curveName ) {
            return new MarketCondition(
                x => x.ValuationDate.Value = this.ValuationDate.Value,
                x => x.DiscountCurve.Value = this.DiscountCurve.Value,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { curveName, diviCurve } },
                x => x.SpotPrices.Value = this.SpotPrices.Value,
                x => x.VolSurfaces.Value = this.VolSurfaces.Value
                );
        }

        public IMarketCondition UpdateCondition(params IUpdateMktConditionPack[] mktConditionPacks)
		{
			var items = mktConditionPacks.ToDictionary(x => ExpressionHelper.GetMemberName(x.ConditionExpression), x => x.NewCondition);
			return UpdateCondition(this, items);
		}

		public static IMarketCondition UpdateCondition(MarketCondition market, Dictionary<string, object> updateItems)
		{
			var newMarket = new MarketCondition();
			foreach (var property in typeof (MarketCondition).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty))
			{
				var targetSetObject =
					(ISetOnce)property.GetValue(newMarket, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty, null, null, null);
				object newCondtion;

				if (updateItems.TryGetValue(property.Name, out newCondtion))
				{
					targetSetObject.SetValue(newCondtion);
				}
				else
				{
					var sourceObject =
					(ISetOnce)property.GetValue(market, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty, null, null, null);
					if (sourceObject.HasValue)
					{
						targetSetObject.SetValue(sourceObject.GetValue());
					}
				}
			}
			return newMarket;
		}
	}
}
