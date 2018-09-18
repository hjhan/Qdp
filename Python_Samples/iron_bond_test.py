# run it with IronPython
import clr
import sys
sys.path.append("D:\\SourceCode\\Git\\QdpGitHub_YieldChain\\build\\Release")
clr.AddReference("Qdp.Pricing.Library.Common")
clr.AddReference("Qdp.Foundation")
clr.AddReference("Qdp.Pricing.Base")

import System
from System.Collections.Generic import Dictionary
from System import String, Double, Tuple, Action
from Qdp.Pricing.Base.Interfaces import IPricingResult
from Qdp.Pricing.Library.Common import Bond
from Qdp.Pricing.Library.Common.Utilities.Coupons import FixedCoupon
from Qdp.Foundation.Implementations import Date
from Qdp.Pricing.Base.Enums import CurrencyCode, Frequency, Stub, TradingMarket, BusinessDayConvention, PriceQuoteType
from Qdp.Pricing.Base.Implementations import CalendarImpl, Act365, DayGap, PricingRequest
from Qdp.Pricing.Library.Common.Engines import BondEngine
from Qdp.Pricing.Library.Common.Utilities import BondYieldPricer
from Qdp.Pricing.Library.Common.Market import MarketCondition
from Qdp.Pricing.Library.Common.Interfaces import IMarketCondition

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

engine = BondEngine(BondYieldPricer())

# always provide a dummy action and do all the assignments later
market = MarketCondition(Action[MarketCondition](lambda x: x))
market.ValuationDate.Value = Date(2018, 8, 1)
prices = Dictionary[String, Tuple[PriceQuoteType, Double]]()
prices["bond"] = Tuple[PriceQuoteType, Double](PriceQuoteType.Clean, 100.0)
market.MktQuote.Value = prices

cashflows = bond.GetCashflows(market)
for cashflow in cashflows:
	print(cashflow.PaymentDate.ToString(), cashflow.PaymentAmount)

result = engine.Calculate(bond, market, PricingRequest.Convexity)
print("Ai:", result.Ai)
print("Ytm:", result.Ytm)
print("Pv01", result.Pv01)
print("ModifiedDuration:", result.ModifiedDuration)
print("MacDuration:", result.MacDuration)
print("Convexity:", result.Convexity)