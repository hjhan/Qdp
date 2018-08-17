![image](http://yieldchain.com/static/img/logo_md_new.png)
# Qdp（Quantatitive Derivative Pricing)
[Introduction In English](https://github.com/YieldChain/Qdp/blob/master/README_EN.md)
## 简介
Qdp是一套基于C#开发的金融衍生品估值定价库，可以对金融衍生品进行模型定价，计算衍生品风险和盈亏归因等。

### 为什么用C#?
+ 比C++开发更容易，比Python运行效率高
+ 可以很容易封装成Excel Addin
+ 利用.NET生态，容易扩展成分布式集群计算
+ 在兼容.NET Core后，跨平台也不是问题

## 运行时要求
+ .NET Framework 4.0+ 或者
+ .NET Core 2.1+

## 如何开始
+ 获得源码
+ 如果需要.NET Framework版本, 请用Visual Studio 2017打开PricingLibrary.sln；如果需要.NET Core版本，请用Visual Studio 2017打开PricingLibrary_DotNetCore.sln
+ 编译整个解决方案

## 使用说明
Qdp中计算的三个基本概念：Instrument/MarketCondition/Engine。

+ Instrument： 定义一个金融产品或衍生品
+ MarketCondition： 定义一个市场条件
+ Engine： 定义一个计算引擎

在Qdp中，每一个定价计算都是某一个Instrument在给定的MarketCondition下，使用特定的Engine所得到的计算结果，这是Qdp中最基础的计算方式。

以下是示例代码：
### 债券计算
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

### 期权计算
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

## 参考文档
建设中...

## 技术支持
+ Qdp由镒链科技技术团队维护. www.yieldchain.com
+ 有任何问题请联系<support@yieldchain.com>.