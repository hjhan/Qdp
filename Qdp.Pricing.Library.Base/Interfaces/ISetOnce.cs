namespace Qdp.Pricing.Library.Base.Interfaces
{
	public interface ISetOnce
	{
		bool HasValue { get; }
		void SetValue(object value);
		object GetValue();
	}
}
