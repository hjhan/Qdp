using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Interfaces;

namespace Qdp.Pricing.Library.Common
{
	public class HoldingPeriod : IInstrument
	{
		public string Id { get; private set; }
        public string TypeName { get { return "HoldingPeriod"; } }
        public DayGap SettlmentGap { get; private set; }
		public double Notional { get; set; }
		public Date StartDate { get; private set; }
		public Date UnderlyingMaturityDate { get; private set; }
		public Bond UnderlyingBond { get; private set; }
		public Direction Direction { get; private set; }
		public double InterestTaxRate { get; private set; }
		public double BusinessTaxRate { get; private set; }
		public double HoldingCost { get; private set; }
		public double StartFrontCommission { get; private set; }
		public double StartBackCommission { get; private set; }
		public double EndFrontCommission { get; private set; }
		public double EndBackCommission { get; private set; }
		public IDayCount PaymentBusinessDayCounter { get; private set; }
		public double StartFixingRate { get; private set; }
		public double EndFixingRate { get; private set; }

		public HoldingPeriod(
			string id,
			double notional,
			Date startDate,
			Date maturityDate,
			Bond underlyingBond,
			Direction direction,
			double interestTaxRate,
			double businessTaxRate,
			double holdingCost,
			double startFrontCommission,
			double startBackCommission,
			double endFrontCommission,
			double endBackCommission,
			IDayCount paymentBusinessDayCounter,
			double startFixingRate,
			double endFixingRate
			)
		{
			Id = id;
			Notional = notional;
			StartDate = startDate;
			UnderlyingMaturityDate = maturityDate;
			UnderlyingBond = underlyingBond;
			Direction = direction;
			InterestTaxRate = interestTaxRate;
			BusinessTaxRate = businessTaxRate;
			HoldingCost = holdingCost;
			StartFrontCommission = startFrontCommission;
			StartBackCommission = startBackCommission;
			EndFrontCommission = endFrontCommission;
			EndBackCommission = endBackCommission;
			PaymentBusinessDayCounter = paymentBusinessDayCounter;
			StartFixingRate = startFixingRate;
			EndFixingRate = endFixingRate;
		}
	}

}
