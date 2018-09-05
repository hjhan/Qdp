using System;
using System.Collections.Generic;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Fx;

namespace Qdp.Pricing.Library.Common.Interfaces
{
	public interface IMarketCondition
	{
		SetOnce<Date> ValuationDate { get; }
		SetOnce<ISpread> CreditSpread { get; }
		SetOnce<Dictionary<string, double>> SpotPrices { get; }
        SetOnce<Dictionary<string, double>> FxSpot { get; }
        SetOnce<Dictionary<string, IVolSurface>> Correlations { get; }
        SetOnce<IYieldCurve> DiscountCurve { get; }
		SetOnce<IYieldCurve> FixingCurve { get; }
		SetOnce<IYieldCurve> RiskfreeCurve { get; }
		SetOnce<Dictionary<string, IYieldCurve>> DividendCurves { get; }

        SetOnce<IYieldCurve> UnderlyingDiscountCurve { get; }
        SetOnce<Dictionary<string, IVolSurface>> VolSurfaces { get; }
        //SetOnce<IVolSurface> VolSurfaceNew { get; }
        SetOnce<IYieldCurve> FgnDiscountCurve { get; }
		SetOnce<IYieldCurve> FgnFixingCurve { get; }
		SetOnce<FxSpot[]> FxSpots { get; }
		SetOnce<IYieldCurve> SettlementCcyDiscountCurve { get; }
		SetOnce<IYieldCurve> SettlementCcyFixingCurve { get; }
		SetOnce<IYieldCurve> SurvivalProbabilityCurve { get; }
		SetOnce<Dictionary<string, IYieldCurve>> YieldCurves { get; }
		SetOnce<Dictionary<string, Tuple<PriceQuoteType, double>>> MktQuote { get; }
		SetOnce<Dictionary<IndexType, SortedDictionary<Date, double>>> HistoricalIndexRates { get; }
		IMarketCondition UpdateCondition(params IUpdateMktConditionPack[] mktConditionPacks);
        IMarketCondition UpdateDividendCurve(IYieldCurve diviCurve, string curveName);


    }
}
