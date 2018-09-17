import clr
# replace with your own DLL path
clr.AddReference("D:\\SourceCode\\Git\\QdpGitHub_YieldChain\\build\\Release\\Qdp.Pricing.Library.Common.dll")

import System
from System.Collections.Generic import Dictionary
from System import String, Double
Tuple = getattr(System, "Tuple`1")
from Qdp.Pricing.Base.Interfaces import IPricingResult
from Qdp.Pricing.Library.Common import Bond
from Qdp.Pricing.Library.Common.Utilities.Coupons import FixedCoupon
from Qdp.Foundation.Implementations import Date
from Qdp.Pricing.Base.Enums import CurrencyCode, Frequency, Stub, TradingMarket, BusinessDayConvention, PriceQuoteType
from Qdp.Pricing.Base.Implementations import CalendarImpl, Act365, DayGap, PricingRequest
from Qdp.Pricing.Library.Common.Engines import BondEngineCn
from Qdp.Pricing.Library.Common.Utilities import BondYieldPricerCn
from Qdp.Pricing.Library.Common.Market import MarketCondition

bond = Bond(
    "bond",
	Date(2016, 3, 15),
	Date(2019, 3, 15),
	100.0,
	CurrencyCode.CNY,
	FixedCoupon(0.05),
	CalendarImpl.Get("chn"),
	Frequency.SemiAnnual,
	Stub.ShortEnd,
	Act365(),
	Act365(),
	BusinessDayConvention.ModifiedFollowing,
	BusinessDayConvention.ModifiedFollowing,
	DayGap("+0D"),
	TradingMarket.ChinaInterBank)

engine = BondEngineCn(BondYieldPricerCn())

market = MarketCondition([])
market.ValuationDate.Value = Date(2018, 8, 1)
prices = Dictionary[String, Tuple[PriceQuoteType, Double]]()
prices["bond"] = Tuple[PriceQuoteType, Double](PriceQuoteType.Clean, 100.0)
market.MktQuote.Value = prices

cashflows = bond.GetCashflows(market)
for cashflow in cashflows:
	print(cashflow.PaymentDate, cashflow.PaymentAmount)
	
result = engine.Calculate(bond, market, PricingRequest.Ytm)
print(result.ytm)
