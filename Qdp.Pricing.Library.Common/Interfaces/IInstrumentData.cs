using Qdp.Pricing.Base.Enums;

namespace Qdp.Pricing.Library.Common.Interfaces
{
	public interface IInstrumentData
	{
		string Id { get; }
		InstrumentType UnderlyingInstrumentType { get; }
		double Price { get; }
	}
}
