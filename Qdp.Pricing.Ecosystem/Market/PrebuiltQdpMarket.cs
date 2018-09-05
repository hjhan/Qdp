using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Ecosystem.Trade;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.VolTermStructure;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Ecosystem.Market.YieldCurveDependentObjects;

namespace Qdp.Pricing.Ecosystem.Market
{
    /// <summary>
    /// PrebuiltQdpMarket builds all market objects when adding them,
    /// to accelerate computation in Excel
    /// </summary>
    public class PrebuiltQdpMarket : QdpMarket
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public PrebuiltQdpMarket(string marketName, Date referenceDate)
        {
            MarketName = marketName;
            ReferenceDate = referenceDate;
            YieldCurves = new Dictionary<string, IYieldCurve>();
            VolRawData = new Dictionary<string, VolSurfMktData>();
            VolSurfaces = new Dictionary<string, ImpliedVolSurface>();
            CorrRawData = new Dictionary<string, CorrSurfMktData>();
            CorrSurfaces = new Dictionary<string, ImpliedVolSurface>();
            BondPrices = new Dictionary<string, BondMktData>();
            CommodityPrices = new Dictionary<string, double>();
            StockPrices = new Dictionary<string, double>();
            FxSpots = new Dictionary<string, double>();
            BondFuturePrices = new Dictionary<string, double>();
        }

        public PrebuiltQdpMarket Copy(string marketName, Date referenceDate,
            Dictionary<string, IYieldCurve> YieldCurvesNew = null,
            Dictionary<string, VolSurfMktData> VolRawDataNew = null,
            Dictionary<string, ImpliedVolSurface> VolSurfacesNew = null,
            Dictionary<string, CorrSurfMktData> CorrRawDataNew = null,
            Dictionary<string, ImpliedVolSurface> CorrSurfacesNew = null,
            Dictionary<string, BondMktData> BondPricesNew = null,
            Dictionary<string, double> CommodityPricesNew = null,
            Dictionary<string, double> StockPricesNew = null,
            Dictionary<string, double> FxSpotsNew = null,
            Dictionary<string, double> BondFuturePricesNew = null
            ) {
            return new PrebuiltQdpMarket(marketName, referenceDate)
            {
                YieldCurves = YieldCurvesNew ?? this.YieldCurves,
                VolRawData = VolRawDataNew ?? this.VolRawData,
                VolSurfaces = VolSurfacesNew ?? this.VolSurfaces,
                CorrRawData = CorrRawDataNew ?? this.CorrRawData,
                CorrSurfaces = CorrSurfacesNew ?? this.CorrSurfaces,
                BondPrices = BondPricesNew ?? this.BondPrices,
                CommodityPrices = CommodityPricesNew ?? this.CommodityPrices,
                StockPrices = StockPricesNew ?? this.StockPrices,
                FxSpots = FxSpotsNew ?? this.FxSpots,
                BondFuturePrices = BondFuturePricesNew ?? this.BondFuturePrices
            };
        }

        //public new Date ReferenceDate { get; private set; }
        //public new string MarketName { get; private set; }
        public Dictionary<string, IYieldCurve> YieldCurves { get; set; }
        //TODO: can be redundant
        public Dictionary<string, VolSurfMktData> VolRawData { get; set; }
        
        public Dictionary<string, ImpliedVolSurface> VolSurfaces { get; set; }
        //TODO: can be redundant
        public Dictionary<string, CorrSurfMktData> CorrRawData { get; set; }
        public Dictionary<string, ImpliedVolSurface> CorrSurfaces { get; set; }

        public Dictionary<string, BondMktData> BondPrices { get; set; }
        public Dictionary<string, double> CommodityPrices { get; set; }
        public Dictionary<string, double> StockPrices { get; set; }
        public Dictionary<string, double> FxSpots { get; set; }
        public Dictionary<string, double> BondFuturePrices { get; set; }
        public new Dictionary<IndexType, SortedDictionary<Date, double>> HistoricalIndexRates { get; set; }

        public void Dispose()
        {
            
        }

        //not used
        public IPricingResult[] ValueTrades(IEnumerable<IValuationFunction> trades, IEnumerable<PricingRequest> requests)
        {
            throw new NotImplementedException();
        }

        public PrebuiltQdpMarket ReCalibratePrebuiltMarketToDate(string newMarketName, string newDate)
        {
            var date = newDate.ToDate();
            var newMarket = new PrebuiltQdpMarket(newMarketName, date)
            {
                BondPrices = BondPrices,
                CommodityPrices = CommodityPrices,
                StockPrices = StockPrices,
                FxSpots = FxSpots,
                BondFuturePrices = BondFuturePrices,
                VolSurfaces = VolSurfaces,
                CorrSurfaces = CorrSurfaces,
                HistoricalIndexRates = HistoricalIndexRates,
            };

            newMarket.YieldCurves = this.YieldCurves.ToDictionary(x => x.Key, x => (IYieldCurve)CurveBuildHelper.BuildYieldCurve(newMarket, date, (x.Value as YieldCurve).RawDefinition));
            
            return newMarket;
        }
    }
}
