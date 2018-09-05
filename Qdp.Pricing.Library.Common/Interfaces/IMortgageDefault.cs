namespace Qdp.Pricing.Library.Common.Interfaces
{
	public interface IMortgageDefault
	{
		//Monthly default rate
		double Mdr(int n);
		bool NeedRecalc();
	}
}
