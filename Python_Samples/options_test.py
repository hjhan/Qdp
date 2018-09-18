# run it with pythonnet
import clr
import sys
# replace with your own DLL path
sys.path.append("D:\\SourceCode\\Git\\QdpGitHub_YieldChain\\build\\Release")
clr.AddReference("D:\\SourceCode\\Git\\QdpGitHub_YieldChain\\build\\Release\\Qdp.Pricing.Library.Equity.dll")
clr.AddReference("D:\\SourceCode\\Git\\QdpGitHub_YieldChain\\build\\Release\\Qdp.ComputeService.Data.CommonModels.dll")
clr.AddReference("D:\\SourceCode\\Git\\QdpGitHub_YieldChain\\build\\Release\\Qdp.Pricing.Ecosystem.dll")

import System
Tuple = getattr(System, "Tuple`1")
from System.Collections.Generic import Dictionary, List
from System import String, Double
from Qdp.Pricing.Base.Interfaces import IPricingResult, ITerm
from Qdp.Foundation.Implementations import Date
from Qdp.Pricing.Base.Enums import CurrencyCode, TradingMarket, BusinessDayConvention, Compound, Interpolation, YieldCurveTrait, OptionExercise, OptionType, InstrumentType
from Qdp.Pricing.Base.Implementations import CalendarImpl, Act365, PricingRequest, Term, DayGap
from Qdp.Pricing.Library.Common.Market import MarketCondition
from Qdp.ComputeService.Data.CommonModels.MarketInfos import RateMktData, VolSurfMktData
from Qdp.Pricing.Library.Common.Market import YieldCurve
from Qdp.Pricing.Library.Common.Interfaces import IYieldCurve
from Qdp.Pricing.Library.Base.Curves.Interfaces import IVolSurface
from Qdp.Pricing.Library.Equity.Engines.Analytical import AnalyticalVanillaEuropeanOptionEngine
from Qdp.Pricing.Ecosystem.Utilities import ExtensionFunctions
from Qdp.Pricing.Library.Equity.Options import VanillaOption
from Qdp.Pricing.Library.Common.Interfaces import IMarketCondition


startDate = Date(2014, 3, 18)
maturityDate = Date(2015, 3, 18)
valueDate = Date(2014, 3, 18)

fr007CurveName = "Fr007"
fr007RateDefinition = []
fr007RateDefinition.append(Tuple[ITerm, Double](Term("1D"), 0.06))
fr007RateDefinition.append(Tuple[ITerm, Double](Term("5Y"), 0.06))

dividendCurveName = "Dividend"
dividendRateDefinition = []
dividendRateDefinition.append(Tuple[ITerm, Double](Term("1D"), 0.03))
dividendRateDefinition.append(Tuple[ITerm, Double](Term("5Y"), 0.03))

discountCurve = YieldCurve(
    fr007CurveName,
    valueDate,
    fr007RateDefinition,
    BusinessDayConvention.ModifiedFollowing,
    Act365(),
    CalendarImpl.Get("chn"),
    CurrencyCode.CNY,
    Compound.Continuous,
    Interpolation.CubicHermiteMonotic,
    YieldCurveTrait.SpotCurve)

dividendCurve = YieldCurve(
    dividendCurveName,
    valueDate,
    dividendRateDefinition,
    BusinessDayConvention.ModifiedFollowing,
    Act365(),
    CalendarImpl.Get("chn"),
    CurrencyCode.CNY,
    Compound.Continuous,
    Interpolation.CubicHermiteMonotic,
    YieldCurveTrait.SpotCurve)

volSurfData = VolSurfMktData("VolSurf", 0.25)
volSurface = ExtensionFunctions.ToImpliedVolSurface(volSurfData, valueDate, "Act365")

market = MarketCondition([])
market.ValuationDate.Value = valueDate
market.DiscountCurve.Value = discountCurve

curves = Dictionary[String, IYieldCurve]()
curves[""] = dividendCurve
market.DividendCurves.Value = curves

volSurfs = Dictionary[String, IVolSurface]()
volSurfs[""] = volSurface
market.VolSurfaces.Value = volSurfs

prices = Dictionary[String, Double]()
prices[""] = 1.0
market.SpotPrices.Value = prices

exerciseDates = []
exerciseDates.append(maturityDate)
observationDates = []
observationDates.append(maturityDate)

put = VanillaOption(
    startDate,
    maturityDate,
    OptionExercise.European,
    OptionType.Put,
    1.0,
    InstrumentType.Stock,
    CalendarImpl.Get("chn"),
    Act365(),
    CurrencyCode.CNY,
    CurrencyCode.CNY,
    exerciseDates,
    observationDates)

engine = AnalyticalVanillaEuropeanOptionEngine()
calculate = engine.Calculate.__overloads__[System.Object, IMarketCondition, PricingRequest]
result = calculate(put, market, PricingRequest.All)
print("Pv:", result.Pv)
print("Delta:", result.Delta)
print("Gamma:", result.Gamma)
print("Vega:", result.Vega)
print("Theta:", result.Theta)
print("Rho:", result.Rho)