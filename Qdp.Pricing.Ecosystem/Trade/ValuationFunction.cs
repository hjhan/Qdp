using System;
using System.Reflection;
using log4net;
using Qdp.ComputeService.Data.CommonModels.TradeInfos;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Library.Base.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Foundation.Implementations;

namespace Qdp.Pricing.Ecosystem.Trade
{
	public abstract class ValuationFunction<TTradeInfo, TInstrument> : IValuationFunction
		where TTradeInfo : TradeInfoBase
		where TInstrument : IInstrument
	{
		protected static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		protected TTradeInfo TradeInfo { get; private set; }

		protected ValuationFunction(TTradeInfo tradeInfo)
		{
			TradeInfo = tradeInfo;
		} 

		public abstract TInstrument GenerateInstrument();
		public abstract IEngine<TInstrument> GenerateEngine();
		public abstract IMarketCondition GenerateMarketCondition(QdpMarket market);
        //希望这个放入tradebaseinfo对应的类

		public virtual IPricingResult ValueTrade(QdpMarket market, PricingRequest request)
		{
			try
			{
				var instrument = GenerateInstrument();
				Logger.InfoFormat("Valuing trade of type {0}", instrument.GetType().Name);
				var marketCondition = GenerateMarketCondition(market);
				var engine = GenerateEngine();
				var result = engine.Calculate(instrument, marketCondition, request);
				return result;
			}
			catch (Exception ex)
			{
				return new PricingResult(market.ReferenceDate, request)
				{
					Succeeded = false,
					ErrorMessage = ex.Message
				};
			}
		}

	}
}
