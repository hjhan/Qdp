namespace Qdp.Pricing.Library.Common.MathMethods.VolTermStructure
{
	public struct SabrCoeffOptimizerResult
	{
		private readonly double _maturity;
		private readonly double _bestAlpha;
		private readonly double _bestBeta;
		private readonly double _bestRho;
		private readonly double _bestNu;

		public SabrCoeffOptimizerResult(double maturity, double bestAlpha, double bestBeta, double bestRho, double bestNu)
		{
			_maturity = maturity;
			_bestAlpha = bestAlpha;
			_bestBeta = bestBeta;
			_bestRho = bestRho;
			_bestNu = bestNu;
		}

		public double Maturity
		{
			get { return _maturity; }
		}

		public double BestAlpha
		{
			get { return _bestAlpha; }
		}
		public double BestBeta
		{
			get { return _bestBeta; }
		}
		public double BestRho
		{
			get { return _bestRho; }
		}
		public double BestNu
		{
			get { return _bestNu; }
		}
	}
}