using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Pricing.Library.Base.Utilities;

namespace Qdp.Pricing.Library.Common.MathMethods.Dividend
{
	public class DividendPolicy
	{
		/// <summary>
		/// Percentage of payout dividend to stock price if stock price is less than announced/forcast dividend
		/// 0 for survivor
		/// 1 for liquidator
		/// </summary>
		/// <param name="payOutRatio">payout percentage</param>
		public DividendPolicy(double payOutRatio=0.0)
		{
			PayOutRatio = payOutRatio;
			Validate();
		}

		public double PayOutRatio { get; private set; }

		/// <summary>
		/// Apply the Dividend Policy to stock price
		/// </summary>
		/// <param name="currentT">t</param>
		/// <param name="currentX">X</param>
		/// <param name="stepSize">steo size</param>
		/// <param name="absDivSchedule">absolute dividend schedule</param>
		/// <param name="relDivSchedule">relative dividend schedule</param>
		/// <returns>stock price after dividend paied out with the proper policy.</returns>
		public double ApplyDividendPolicy(double currentT, double currentX, double stepSize,
			IList<Tuple<double, double>> absDivSchedule, IList<Tuple<double, double>> relDivSchedule)
		{
			if (absDivSchedule!=null)
				foreach (var div in absDivSchedule.Where(div => IsRightBeforeExDate(currentT,div.Item1,stepSize)))
				{
					return (currentX > div.Item2)
						? currentX - div.Item2
						: currentX*(1 - PayOutRatio);
				}

			if (relDivSchedule == null) return currentX;

			foreach (var div in relDivSchedule.Where(div => IsRightBeforeExDate(currentT, div.Item1, stepSize)))
			{
				return currentX*(1 - div.Item2);
			}

			return currentX;
		}

		private bool IsRightBeforeExDate(double currentT, double exDate, double stepSize)
		{
			return (currentT <= exDate) && (exDate - currentT < stepSize);
		}

		private void Validate()
		{
			if (PayOutRatio <0 || PayOutRatio >1)
				throw new PricingLibraryException(string.Format("Payout amount {0} is not within the proper range!",PayOutRatio));
		}

	}
}
