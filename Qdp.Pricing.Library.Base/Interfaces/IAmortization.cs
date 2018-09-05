using System.Collections.Generic;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Implementations;

namespace Qdp.Pricing.Library.Base.Interfaces
{
	public interface IAmortization
	{
		Dictionary<Date, double> AmortizationSchedule { get; }

		AmortizationType AmortizationType { get; }

		bool RenormalizeAfterAmoritzation { get; }

		IAmortization Adjust(Schedule paymentSchedule, ICalendar calendar = null, BusinessDayConvention bda = BusinessDayConvention.None, Frequency frequency = Frequency.None);

		double GetRemainingPrincipal(Date valueDate);

		IAmortization ResetAmortization(Date valueDate);
	}
}
