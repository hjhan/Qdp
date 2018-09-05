using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Equity.Options;
using System.Linq;

namespace Qdp.Pricing.Library.Equity.Engines.Analytical
{
	public class AnalyticalBinaryEuropeanOptionReplicationEngine : Engine<BinaryOption>
	{
		public double Offset { get; private set; }

		public BinaryOptionReplicationStrategy BinaryOptionReplicationStrategy { get; private set; }


		public AnalyticalBinaryEuropeanOptionReplicationEngine(double offset, BinaryOptionReplicationStrategy binaryOptionReplicationStrategy)
		{
			Offset = offset;
			BinaryOptionReplicationStrategy = binaryOptionReplicationStrategy;
		}
		public override IPricingResult Calculate(BinaryOption trade, IMarketCondition market, PricingRequest request)
		{
			
			var result =  new PricingResult(market.ValuationDate, request);
             
            if (trade.BinaryOptionPayoffType == BinaryOptionPayoffType.CashOrNothing)
			{
				var factor = (double)BinaryOptionReplicationStrategy;
				var lowStrike = trade.Strike + Offset * (factor - 1.0) / 2.0;
				var highStrike = trade.Strike + Offset * (factor + 1.0) / 2.0;

                //if call, replicate by call spreads,
                //if put, replicate by put spreads

                var lowStrikeOption = new VanillaOption(trade.StartDate, trade.UnderlyingMaturityDate, trade.Exercise, trade.OptionType, lowStrike, trade.UnderlyingProductType, trade.Calendar, trade.DayCount, trade.PayoffCcy, trade.SettlementCcy, trade.ExerciseDates, trade.ObservationDates, trade.Notional);
				var highStrikeOption = new VanillaOption(trade.StartDate, trade.UnderlyingMaturityDate, trade.Exercise, trade.OptionType, highStrike, trade.UnderlyingProductType, trade.Calendar, trade.DayCount, trade.PayoffCcy, trade.SettlementCcy, trade.ExerciseDates, trade.ObservationDates, trade.Notional);
				var engine = new AnalyticalVanillaEuropeanOptionEngine();
				var lowResult = engine.Calculate(lowStrikeOption, market, request);
				var highResult = engine.Calculate(highStrikeOption, market, request);

				var sign = trade.OptionType == OptionType.Call ? 1.0 : -1.0;
				factor = sign*trade.CashOrNothingAmount/Offset;

                //calc basic stuff
                if (result.IsRequested(PricingRequest.Pv))
                {
                    result.Pv = (lowResult.Pv - highResult.Pv) * factor;
                }
                if (AnalyticalOptionPricerUtil.isBasicPricing(result))
                {
                    result.Delta = (lowResult.Delta - highResult.Delta) * factor;
                    result.DeltaCash = result.Delta * market.SpotPrices.Value.Values.First();
                    result.Gamma = (lowResult.Gamma - highResult.Gamma) * factor;
                    result.GammaCash = result.Gamma * market.SpotPrices.Value.Values.First() * market.SpotPrices.Value.Values.First() / 100;
                    result.Vega = (lowResult.Vega - highResult.Vega) * factor;
                    result.Rho = (lowResult.Rho - highResult.Rho) * factor;
                    result.Theta = (lowResult.Theta - highResult.Theta) * factor;
                }
                if (AnalyticalOptionPricerUtil.isHighOrderPricing(result)) {
                    result.DDeltaDvol = (lowResult.DDeltaDvol - highResult.DDeltaDvol) * factor;
                    result.DVegaDvol = (lowResult.DVegaDvol - highResult.DVegaDvol) * factor;
                    result.DVegaDt = (lowResult.DVegaDt - highResult.DVegaDt) * factor;
                    result.DDeltaDt = (lowResult.DDeltaDt - highResult.DDeltaDt) * factor;
                }

            }
			else if (trade.BinaryOptionPayoffType == BinaryOptionPayoffType.AssetOrNothing)
			{
				var binaryCfOption = new BinaryOption(trade.StartDate, trade.UnderlyingMaturityDate, trade.Exercise, trade.OptionType,
					trade.Strike, trade.UnderlyingProductType, BinaryOptionPayoffType.CashOrNothing, 1.0, trade.Calendar,
					trade.DayCount, trade.PayoffCcy,
					trade.SettlementCcy, trade.ExerciseDates, trade.ObservationDates);
				var binaryResult = Calculate(binaryCfOption, market, request);
				var vanillaOption = new VanillaOption(trade.StartDate, trade.UnderlyingMaturityDate, trade.Exercise, trade.OptionType,
					trade.Strike, trade.UnderlyingProductType, trade.Calendar, trade.DayCount, trade.PayoffCcy,
					trade.SettlementCcy, trade.ExerciseDates, trade.ObservationDates, trade.Notional);
				var engine = new AnalyticalVanillaEuropeanOptionEngine();
				var vanillaResult = engine.Calculate(vanillaOption, market, request);
				var sign = trade.OptionType == OptionType.Call ? 1.0 : -1.0;

                if (result.IsRequested(PricingRequest.Pv))
                {
                    result.Pv = sign * vanillaResult.Pv + trade.Strike * binaryResult.Pv;
                }

                if (AnalyticalOptionPricerUtil.isBasicPricing(result))
                {   
                    result.Delta = sign * vanillaResult.Delta + trade.Strike * binaryResult.Delta;
                    result.DeltaCash = result.Delta * market.SpotPrices.Value.Values.First();
                    result.Gamma = sign * vanillaResult.Gamma + trade.Strike * binaryResult.Gamma;
                    result.GammaCash = result.Gamma * market.SpotPrices.Value.Values.First() * market.SpotPrices.Value.Values.First() / 100;
                    result.Vega = sign * vanillaResult.Vega + trade.Strike * binaryResult.Vega;
                    result.Rho = sign * vanillaResult.Rho + trade.Strike * binaryResult.Rho;
                    result.Theta = sign * vanillaResult.Theta + trade.Strike * binaryResult.Theta;
                }

                if (AnalyticalOptionPricerUtil.isHighOrderPricing(result))
                {
                    result.DDeltaDvol = sign * vanillaResult.DDeltaDvol + trade.Strike * binaryResult.DDeltaDvol;
                    result.DVegaDvol = sign * vanillaResult.DVegaDvol + trade.Strike * binaryResult.DVegaDvol;
                    result.DVegaDt = sign * vanillaResult.DVegaDt + trade.Strike * binaryResult.DVegaDt;
                    result.DDeltaDt = sign * vanillaResult.DDeltaDt + trade.Strike * binaryResult.DDeltaDt;
                }
                
            }
			return result;
		}
	}
}
