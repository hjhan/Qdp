using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Pricing.Library.Common.MathMethods.Interfaces;

namespace Qdp.Pricing.Library.Common.MathMethods.Utilities
{
	public enum Boundary
	{
		Upper,
		Lower,
		Any
	}

	public class NeumannBoundaryCondition : NumericCondition
	{
		private readonly Boundary _boundary;
		private readonly double _value;

		/// <summary>
		/// Initializes a new instance of the <see cref="NeumannBoundaryCondition"/> class.
		/// Neumann boundary condition specifies the derivative on the boundary
		/// 
		/// Call: when StockPrice is high, delta = 1, when stockPrice is small, delta = 0
		/// Put: : when StockPrice is high, delta = 0, when stockPrice is small, delta = -1
		/// 
		/// </summary>
		/// <param name="boundary">The boundary.</param>
		/// <param name="value">The value.</param>
		public NeumannBoundaryCondition(Boundary boundary, double value)
		{
			_boundary = boundary;
			_value = value;
		}

		/// <summary>
		/// Applies the before solving.
		/// 
		/// In solving Ax = B, apply these boundary conditions to B. 
		/// Where A[0][0] = 1, A[0][1] = -1, A[end][end-1] = 1, A[end][end] = -1. These will then produce the equations at the boundary.
		/// 
		/// </summary>
		/// <param name="arrayValues">The array values.</param>
		/// <exception cref="PricingLibraryException">Unknown boundary</exception>
		public double[] Apply(IEnumerable<double> arrayValues)
		{
			var ret = arrayValues.Select(x => x).ToArray();
			switch (_boundary)
			{
				case Boundary.Lower:
					ret[0] = _value;
					break;
				case Boundary.Upper:
					ret[ret.Length - 1] = _value;
					break;
				default:
					throw new ArgumentOutOfRangeException("arrayValues");
			}

			return ret;
		}
	}
}
