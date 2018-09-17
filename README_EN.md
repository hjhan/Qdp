![image](http://yieldchain.com/static/img/logo_md_new.png)
# Qdp£¨Quantatitive Derivative Pricing)
## Introduction
Qpd is a pricing library for financial derivatives developed in C#. It can calculate model price, risks and pnl explain for derivatives.

### Why in C#?
+ Easier than C++£¬more efficient than Python
+ Easy to build Excel Addin
+ Based on .NET£¬easy to extend to distributed computing cluster
+ Cross-platform is not a problem after supporting .NET Core

## Runtime Requirements
+ .NET Framework 4.0+ or
+ .NET Core 2.1+

## How to start
+ Get the source code
+ If you need .NET Framework version, open PricingLibrary.sln in Visual Studio 2017£»if you need .NET Core version£¬open PricingLibrary_DotNetCore.sln in Visual Studio 2017
+ Build whole solution

## How to use
There're three basic concepts in Qdp£ºInstrument/MarketCondition/Engine¡£

+ Instrument£º Define an instrument in financial market
+ MarketCondition£º Define a 
+ Engine£º Define an engine to implement the pricing model

In Qpd, each "calculation" is an Instrument being calculated by a particular Engine under certain MarketCondition. This is basicly how Qpd works.

Some sample code£º
### Bond
```CSharp
    var bond = new Bond(
        id: "bond",
        startDate: new Date(2016, 3, 15),
        maturityDate: new Date(2019, 3, 15),
        notional: 100.0,
        currency: CurrencyCode.CNY,
        coupon: new FixedCoupon(0.05),
        calendar: CalendarImpl.Get("chn"),
        paymentFreq: Frequency.SemiAnnual,
        stub: Stub.ShortEnd,
        accrualDayCount: new Act365(),
        paymentDayCount: new Act365(),
        accrualBizDayRule: BusinessDayConvention.ModifiedFollowing,
        paymentBizDayRule: BusinessDayConvention.ModifiedFollowing,
        settlementGap: new DayGap("+0D"),
        bondTradingMarket: TradingMarket.ChinaInterBank);

    var engine = new BondEngineCn(new BondYieldPricerCn());
    var market = new MarketCondition(
                x => x.ValuationDate.Value = new Date(2018, 8, 1),
                x => x.MktQuote.Value = 
                    new Dictionary<string, Tuple<PriceQuoteType, double>>
                    {
                        { "bond", Tuple.Create(PriceQuoteType.Clean, 100.0) }
                    }
                );
    var result = engine.Calculate(bond, market, PricingRequest.Ytm);
```

### Options
```CSharp
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
        referenceDate: new Date(2014, 3, 8),
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
        referenceDate: new Date(2014, 3, 8),
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
```

### Python Samples
[Python Samples](https://github.com/YieldChain/Qdp/Python_Samples)

## Document
http://api.yieldchain.com/qdponlinehelp

## Support
+ Qdp is actively maintained by YieldChain. www.yieldchain.com
+ Please contact <support@yieldchain.com>.