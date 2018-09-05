using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Library.Base.Utilities;

namespace Qdp.Pricing.Library.Common.MathMethods.Dividend
{
	public class DiscreteDividends
	{

		public DiscreteDividends(IEnumerable<Tuple<Date, double>> absDiv =null, IEnumerable<Tuple<Date, double>> relDiv=null)
		{
			if (absDiv != null)
			{
				var temp = absDiv.ToList();
				temp.Sort(new DateComparer());
				AbsoluteDividends = temp;
			}
			else
			{
				AbsoluteDividends = new List<Tuple<Date, double>>();
			}
			
			if (relDiv != null) 
			{
				var temp = relDiv.ToList();
				temp.Sort(new DateComparer());
				RelativeDividends = temp;
				Validate();
			}
			else
			{
				RelativeDividends = new List<Tuple<Date, double>>();
			}

			HasAbsoluteDividends = AbsoluteDividends.Any();
			HasRelativeDividends = RelativeDividends.Any();
		}

		public IEnumerable<Tuple<Date, double>> AbsoluteDividends { get; private set; }
		public IEnumerable<Tuple<Date, double>> RelativeDividends { get; private set; }
		public bool HasAbsoluteDividends { get; private set; }
		public bool HasRelativeDividends { get; private set; }

		private void Validate()
		{
			foreach (var item in RelativeDividends)
			{
				if (item.Item2<0 || item.Item2 > 1)
					throw new PricingLibraryException(string.Format("Relative dividend {0} is not within the proper range!", item.Item2 ));
			}
		}

	}

	internal class DateComparer:IComparer<Tuple<Date,double>>
	{
		public int Compare(Tuple<Date, double> x, Tuple<Date, double> y)
		{
			return (int)(x.Item1.ToOADate() - y.Item1.ToOADate());
		}
	}
}
