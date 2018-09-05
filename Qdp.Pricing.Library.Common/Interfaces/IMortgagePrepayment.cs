namespace Qdp.Pricing.Library.Common.Interfaces
{
	public interface IMortgagePrepayment
	{
		//single month mortatility rate
		double Smm(int n);
		bool NeedRecalc();
	}
}
