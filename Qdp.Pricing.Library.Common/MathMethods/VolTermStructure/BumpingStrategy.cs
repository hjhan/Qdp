using System;

namespace Qdp.Pricing.Library.Common.MathMethods.VolTermStructure
{
	public class BumpingStrategy
	{
		public BumpingStrategy(double fbump, double bbump, bool relative)
		{
			_forwardBump = fbump;
			_backwardBump = bbump;
			_bumpsAreRelative = relative;
		}

		public double MinBump(double x0)
		{
			return _bumpsAreRelative ? x0 * Math.Min(_forwardBump, _backwardBump) : Math.Min(_forwardBump, _backwardBump);
		}

		public double BumpForward(double x0)
		{
			return _bumpsAreRelative ? x0 * (1 + _forwardBump) : x0 + _forwardBump;
		}

		public double BumpBackward(double x0)
		{
			return _bumpsAreRelative ? x0 * (1 - _backwardBump) : x0 - _backwardBump;
		}

		public double BumpForwardInverse(double x0)
		{
			return _bumpsAreRelative ? x0 / (1 + _forwardBump) : x0 - _forwardBump;
		}

		public double BumpBackwardInverse(double x0)
		{
			return _bumpsAreRelative ? x0 / (1 - _backwardBump) : x0 + _backwardBump;
		}

		private readonly double _forwardBump;
		private readonly double _backwardBump;
		private readonly bool _bumpsAreRelative;
	}
}

