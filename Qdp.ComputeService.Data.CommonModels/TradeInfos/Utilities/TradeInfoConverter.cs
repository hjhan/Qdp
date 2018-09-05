using System.Collections.Generic;
using System.Linq;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.Equity;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.Equity;
using Qdp.Foundation.Serializer;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.Utilities
{
    public class TradeInfoConverter
    {
        public static ConvertibleBondInfo ToConvertibleBondInfo(BondInfoBase bondInfo,
            string conversionStartDate,
            string conversionEndDate,
            string conversionStockcode,
            double conversionPrice,
            string dividenCurveName,
            bool treatAsCommonBond = false,
            string[] ebcStrikeQuoteTypes = null,
            string[] ebpStrikeQuoteTypes = null)
        {
            var json = DataContractJsonObjectSerializer.Serialize(bondInfo);
            var bondPart = DataContractJsonObjectSerializer.Deserialize<BondInfoBase>(json);
            bondPart.OptionToAssPut = null;
            bondPart.OptionToCall = null;
            bondPart.OptionToPut = null;

            var hasPut = !(bondInfo.OptionToPut == null || !bondInfo.OptionToPut.Any());
            var hasCall = !(bondInfo.OptionToCall == null || !bondInfo.OptionToCall.Any());
            if(!hasPut) bondInfo.OptionToPut = new Dictionary<string, double>();
            if(!hasCall) bondInfo.OptionToCall = new Dictionary<string, double>();
            
            var ebos = bondInfo.OptionToPut.Select(x => new VanillaOptionInfo
            (tradeId: "", 
            strike: x.Value, 
            underlyingTicker: bondInfo.BondId, 
            underlyingInstrumentType: "Bond", 
            valuationParameter: null,
            startDate: bondInfo.StartDate,
            underlyingMaturityDate: bondInfo.MaturityDate,
            exerciseDates: x.Key,
            calendar: bondInfo.Calendar,
            dayCount: bondInfo.DayCount,
            exercise: "European",
            notional: 1.0,
            optionType: "Call"
            )).Union(
                bondInfo.OptionToCall.Select(x => new VanillaOptionInfo
                ("", 
                strike: x.Value, 
                underlyingTicker: bondInfo.BondId, 
                underlyingInstrumentType: "Bond", 
                valuationParameter: null,
                startDate: bondInfo.StartDate,
                underlyingMaturityDate: bondInfo.MaturityDate,
                exerciseDates: x.Key,
                calendar: bondInfo.Calendar,
                optionType: "Put",
                dayCount: bondInfo.DayCount,
                notional: 1.0)
                )
            ).ToArray();

            var eboStrikQuoteTypes = ebcStrikeQuoteTypes ?? new string[0]
                .Union(ebpStrikeQuoteTypes ?? new string[0])
                .ToArray();

            var conversion = new VanillaOptionInfo(
                tradeId: "", 
                strike: conversionPrice, 
                underlyingTicker: bondInfo.BondId, 
                underlyingInstrumentType: "Stock", 
                valuationParameter: null,
                startDate: conversionStartDate,
                underlyingMaturityDate: conversionEndDate,
                calendar: bondInfo.Calendar,
                optionType: "Call",
                exercise: "American",
                dayCount: bondInfo.DayCount,
                notional: 1.0,
                exerciseDates: bondInfo.OptionToCall.Keys.First()
                );
            return new ConvertibleBondInfo(bondPart.BondId)
            {
                BondPart = bondPart,
                ConversionOption = conversion,
                EmbeddedOptions = ebos.Any() ? ebos : null,
                EboStrikeQuoteTypes = eboStrikQuoteTypes.Any() ? eboStrikQuoteTypes : null,
                ValuationParameters = new OptionValuationParameters(discountCurveName: bondInfo.ValuationParamters.DiscountCurveName, 
                dividendCurveName:dividenCurveName,
                volSurfName: conversionStockcode + "Vol",
                underlyingId: conversionStockcode),
                TreatAsCommonBond = treatAsCommonBond
            };
        } 
    }
}
