using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Base.Utilities;

namespace Qdp.Pricing.Library.Base.Curves.Interpolators
{

	public class ConvexMonoticInterpolator : IInterpolator
	{
		private readonly Tuple<double, double>[] _keyPoints;
		private readonly double[] _xArr;
		private readonly double[] _yArr;
		private readonly ISectionHelper _extrapolationHelper;
		private readonly double _quadraticity;
		private readonly double _monotonicity;
		private readonly bool _forcePositive;
		private readonly bool _constFinalPeriod;
		private readonly ISectionHelper[] _helpers;
		private readonly bool _allowExtrapolation;

		public ConvexMonoticInterpolator(
			IEnumerable<Tuple<double, double>> points,
			double quadraticity = 0.3,
			double monotonicity = 0.7,
			bool forcePositive = true,
			bool constFinalPeriod = false,
			bool allowExtrapolation = true)
		{
			_keyPoints = points.ToArray();
			_xArr = _keyPoints.Select(x => x.Item1).ToArray();
			_yArr = _keyPoints.Select(x => x.Item2).ToArray();
			_quadraticity = quadraticity;
			_monotonicity = monotonicity;
			_forcePositive = forcePositive;
			_constFinalPeriod = constFinalPeriod;
			_allowExtrapolation = allowExtrapolation;
			
			var length = _keyPoints.Length;
			_helpers = new ISectionHelper[length];
			if (length < 2)
			{
				throw new PricingLibraryException("Convex monotonic interpolator must have at least 2 points");
			}
			if (_keyPoints.Length == 2)
			{
				var helper = new EverywhereConstantHelper(_keyPoints.Last().Item2, 0.0, _keyPoints.First().Item1);
				_helpers[1] = helper;
				_extrapolationHelper = helper;
			}

			var f = new double[length];
			for (var i = 1; i < length - 1; ++i)
			{
				var dxPrev = _xArr[i] - _xArr[i - 1];
				var dx = _xArr[i + 1] - _xArr[i];
				f[i] = dxPrev / (dx + dxPrev) * _yArr[i] + dx / (dx + dxPrev) * _yArr[i + 1];
			}

			f[0] = 1.5 * _yArr[1] - 0.5 * f[1];
			f[length - 1] = 1.5 * _yArr.Last() - 0.5 * f[length - 2];
			if (_forcePositive)
			{
				if (f[0] < 0.0) f[0] = 0;
				if (f[length - 1] < 0.0) f[length - 1] = 0.0;
			}

			var integral = 0.0;
			var end = constFinalPeriod ? length - 1 : length;
			for (var i = 1; i < end; ++i)
			{
				var gPrev = f[i - 1] - _yArr[i];
				var gNext = f[i] - _yArr[i];
				if (gPrev.IsAlmostZero() && gNext.IsAlmostZero())
				{
					_helpers[i] = new ConstantGradHelper(f[i - 1], integral, _xArr[i - 1], _xArr[i], f[i]);
				}
				else
				{
					quadraticity = _quadraticity;
					ISectionHelper quadraticHelper = null;
					ISectionHelper convMonotoneHelper = null;
					if (_quadraticity > 0.0)
					{
						if (gPrev >= -2.0 * gNext && gPrev > -0.5 * gNext && _forcePositive)
						{
							quadraticHelper = new QuadraticMinHelper(_xArr[i - 1], _xArr[i], f[i - 1], f[i], _yArr[i], integral);
						}
						else
						{
							quadraticHelper = new QuadraticHelper(_xArr[i - 1], _xArr[i], f[i - 1], f[i], _yArr[i], integral);
						}
					}
					if (_quadraticity < 1.0)
					{
						if ((gPrev > 0.0 && -0.5 * gPrev >= gNext && gNext >= -2.0 * gPrev) ||
							 (gPrev < 0.0 && -0.5 * gPrev <= gNext && gNext <= -2.0 * gPrev))
						{
							quadraticity = 1.0;
							if (_quadraticity.IsAlmostZero())
							{
								quadraticHelper = _forcePositive
									? (ISectionHelper)new QuadraticMinHelper(_xArr[i - 1], _xArr[i], f[i - 1], f[i], _yArr[i], integral)
									: new QuadraticHelper(_xArr[i - 1], _xArr[i], f[i - 1], f[i], _yArr[i], integral);
							}
						}
						else if ((gPrev < 0.0 && gNext > -2.0 * gPrev) || (gPrev > 0.0 && gNext < -2.0 * gPrev))
						{

							var eta = (gNext + 2.0 * gPrev) / (gNext - gPrev);
							var b2 = (1.0 + _monotonicity) / 2.0;
							if (eta < b2)
							{
								convMonotoneHelper = new ConvexMonotone2Helper(_xArr[i - 1], _xArr[i], gPrev, gNext, _yArr[i], eta, integral);
							}
							else
							{
								convMonotoneHelper = _forcePositive
									? new ConvexMonotone4MinHelper(_xArr[i - 1], _xArr[i], gPrev, gNext, _yArr[i], b2, integral)
									: new ConvexMonotone4Helper(_xArr[i - 1], _xArr[i], gPrev, gNext, _yArr[i], b2, integral);
							}
						}
						else if ((gPrev > 0.0 && gNext < 0.0 && gNext > -0.5 * gPrev) || (gPrev < 0.0 && gNext > 0.0 && gNext < -0.5 * gPrev))
						{
							var eta = gNext / (gNext - gPrev) * 3.0;
							var b3 = (1.0 - _monotonicity) / 2.0;
							if (eta > b3)
							{
								convMonotoneHelper = new ConvexMonotone3Helper(_xArr[i - 1], _xArr[i], gPrev, gNext, _yArr[i], eta, integral);
							}
							else
							{
								convMonotoneHelper = _forcePositive ? new ConvexMonotone4MinHelper(_xArr[i - 1], _xArr[i], gPrev, gNext, _yArr[i], b3, integral) : new ConvexMonotone4Helper(_xArr[i - 1], _xArr[i], gPrev, gNext, _yArr[i], b3, integral);
							}
						}
						else
						{
							var eta = gNext / (gPrev + gNext);
							var b2 = (1.0 + _monotonicity) / 2.0;
							var b3 = (1.0 - _monotonicity) / 2.0;
							if (eta > b2) eta = b2;
							if (eta < b3) eta = b3;
							convMonotoneHelper = _forcePositive
								? new ConvexMonotone4MinHelper(_xArr[i - 1], _xArr[i], gPrev, gNext, _yArr[i], eta, integral)
								: new ConvexMonotone4Helper(_xArr[i - 1], _xArr[i], gPrev, gNext, _yArr[i], eta, integral);
						}
					}

					if (quadraticity.AlmostEqual(1.0))
					{
						_helpers[i] = quadraticHelper;
					}
					else if (quadraticity.IsAlmostZero())
					{
						_helpers[i] = convMonotoneHelper;
					}
					else
					{
						_helpers[i] = new ComboHelper(quadraticHelper, convMonotoneHelper, quadraticity);
					}
				}
				integral += _yArr[i] * (_xArr[i] - _xArr[i - 1]);
			}

			if (_constFinalPeriod)
			{
				_helpers[length - 1] = new EverywhereConstantHelper(_yArr[length - 1], integral, _xArr[length - 2]);
				_extrapolationHelper = _helpers[length - 1];
			}
			else
			{
				_extrapolationHelper = new EverywhereConstantHelper(_helpers[length - 1].GetValue(_xArr.Last()), integral, _xArr.Last());
			}
		}

		public double GetValue(double x)
		{
			if (x >= _keyPoints.Last().Item1)
			{
				if (!_allowExtrapolation)
				{
					throw new PricingBaseException("Interpolation is not allowed!");
				}
				return _extrapolationHelper.GetValue(x);
			}

			return _helpers[_xArr.UpperBound(x)].GetValue(x);
		}

		public double GetIntegral(double x)
		{
			if (x >= _keyPoints.Last().Item1)
			{
				if (!_allowExtrapolation)
				{
					throw new PricingBaseException("Interpolation is not allowed!");
				}
				return _extrapolationHelper.GetIntegral(x);
			}

			return _helpers[_xArr.UpperBound(x)].GetIntegral(x);
		}
	}

	internal enum SectionType
	{
		EverywhereConstant,
		ConstantGradient,
		QuadraticMinimum,
		QuadraticMaximum
	};

	internal interface ISectionHelper
	{
		double GetValue(double x);
		double GetIntegral(double x);
		double FNext();
	};

	internal class EverywhereConstantHelper : ISectionHelper
	{
		public EverywhereConstantHelper(double value, double prevIntegral, double xPrev)
		{
			_value = value;
			_prevIntegral = prevIntegral;
			_xPrev = xPrev;
		}

		public double GetValue(double x)
		{
			return _value;
		}

		public double GetIntegral(double x)
		{
			return _prevIntegral + (x - _xPrev) * _value;
		}

		public double FNext()
		{
			return _value;
		}

		private readonly double _value;
		private readonly double _prevIntegral;
		private readonly double _xPrev;
	}

	internal class ConvexMonotone2Helper : ISectionHelper
	{
		public ConvexMonotone2Helper(double xPrev, double xNext,
			double gPrev, double gNext,
			double fAverage, double eta2,

			double prevPrimitive)
		{
			_xPrev = xPrev;
			_xScaling = xNext - xPrev;
			_gPrev = gPrev;
			_gNext = gNext;
			_fAverage = fAverage;
			_eta2 = eta2;
			_prevPrimitive = prevPrimitive;
		}

		public double GetValue(double x)
		{
			var xVal = (x - _xPrev) / _xScaling;
			if (xVal <= _eta2)
			{
				return (_fAverage + _gPrev);
			}
			else
			{
				return (_fAverage + _gPrev + (_gNext - _gPrev) / ((1 - _eta2) * (1 - _eta2)) * (xVal - _eta2) * (xVal - _eta2));
			}
		}

		public double GetIntegral(double x)
		{
			var xVal = (x - _xPrev) / _xScaling;
			if (xVal <= _eta2)
			{
				return (_prevPrimitive + _xScaling * (_fAverage * xVal + _gPrev * xVal));
			}
			else
			{
				return (_prevPrimitive + _xScaling * (_fAverage * xVal + _gPrev * xVal + (_gNext - _gPrev) / ((1 - _eta2) * (1 - _eta2)) *
																(1.0 / 3.0 * (xVal * xVal * xVal - _eta2 * _eta2 * _eta2) - _eta2 * xVal * xVal +
																 _eta2 * _eta2 * xVal)));
			}
		}

		public double FNext()
		{
			return (_fAverage + _gNext);
		}

		private readonly double _xPrev;
		private readonly double _xScaling;
		private readonly double _gPrev;
		private readonly double _gNext;
		private readonly double _fAverage;
		private readonly double _eta2;
		private readonly double _prevPrimitive;
	}

	internal class ComboHelper : ISectionHelper
	{
		public ComboHelper(ISectionHelper quadraticHelper,
						ISectionHelper convMonoHelper,
						double quadraticity)
		{
			quadraticity_ = quadraticity;
			_quadraticHelper = quadraticHelper;
			_convMonoHelper = convMonoHelper;

		}

		public double GetValue(double x)
		{
			return (quadraticity_ * _quadraticHelper.GetValue(x) + (1.0 - quadraticity_) * _convMonoHelper.GetValue(x));
		}

		public double GetIntegral(double x)
		{
			return (quadraticity_ * _quadraticHelper.GetIntegral(x) + (1.0 - quadraticity_) * _convMonoHelper.GetIntegral(x));
		}
		public double FNext()
		{
			return (quadraticity_ * _quadraticHelper.FNext() + (1.0 - quadraticity_) * _convMonoHelper.FNext());
		}

		private readonly double quadraticity_;
		private readonly ISectionHelper _quadraticHelper;
		private readonly ISectionHelper _convMonoHelper;
	};

	public class ConvexMonotone3Helper : ISectionHelper
	{
		public ConvexMonotone3Helper(double xPrev, double xNext,
			double gPrev, double gNext,
			double fAverage, double eta3,
			double prevPrimitive)
		{
			_xPrev = xPrev;
			_xScaling = xNext - xPrev;
			_gPrev = gPrev;
			_gNext = gNext;
			_fAverage = fAverage;
			_eta3 = eta3;
			_prevPrimitive = prevPrimitive;
		}

		public double GetValue(double x)
		{
			var xVal = (x - _xPrev) / _xScaling;
			if (xVal <= _eta3)
			{
				return (_fAverage + _gNext + (_gPrev - _gNext) / (_eta3 * _eta3) * (_eta3 - xVal) * (_eta3 - xVal));
			}
			else
			{
				return (_fAverage + _gNext);
			}
		}

		public double GetIntegral(double x)
		{
			var xVal = (x - _xPrev) / _xScaling;
			if (xVal <= _eta3)
			{
				return (_prevPrimitive + _xScaling * (_fAverage * xVal + _gNext * xVal + (_gPrev - _gNext) / (_eta3 * _eta3) *
																(1.0 / 3.0 * xVal * xVal * xVal - _eta3 * xVal * xVal + _eta3 * _eta3 * xVal)));
			}
			else
			{
				return (_prevPrimitive + _xScaling * (_fAverage * xVal + _gNext * xVal + (_gPrev - _gNext) / (_eta3 * _eta3) *
																(1.0 / 3.0 * _eta3 * _eta3 * _eta3)));
			}
		}

		public double FNext()
		{
			return (_fAverage + _gNext);
		}

		private readonly double _xPrev;
		private readonly double _xScaling;
		private readonly double _gPrev;
		private readonly double _gNext;
		private readonly double _fAverage;
		private readonly double _eta3;
		private readonly double _prevPrimitive;
	}

	internal class ConvexMonotone4Helper : ISectionHelper
	{
		public ConvexMonotone4Helper(double xPrev, double xNext,
			double gPrev, double gNext,
			double fAverage, double eta4,
			double prevPrimitive)
		{
			XPrev = xPrev;
			XScaling = xNext - xPrev;
			GPrev = gPrev;
			GNext = gNext;
			FAverage = fAverage;
			Eta4 = eta4;
			PrevPrimitive = prevPrimitive;
			A = -0.5 * (Eta4 * GPrev + (1 - Eta4) * GNext);
		}

		public virtual double GetValue(double x)
		{
			var xVal = (x - XPrev) / XScaling;
			if (xVal <= Eta4)
			{
				return (FAverage + A + (GPrev - A) * (Eta4 - xVal) * (Eta4 - xVal) / (Eta4 * Eta4));
			}
			else
			{
				return (FAverage + A + (GNext - A) * (xVal - Eta4) * (xVal - Eta4) / ((1 - Eta4) * (1 - Eta4)));
			}
		}

		public virtual double GetIntegral(double x)
		{
			var xVal = (x - XPrev) / XScaling;
			double retVal;
			if (xVal <= Eta4)
			{
				retVal = PrevPrimitive + XScaling * (FAverage + A + (GPrev - A) / (Eta4 * Eta4) *
															  (Eta4 * Eta4 - Eta4 * xVal + 1.0 / 3.0 * xVal * xVal)) * xVal;
			}
			else
			{
				retVal = PrevPrimitive + XScaling * (FAverage * xVal + A * xVal + (GPrev - A) * (1.0 / 3.0 * Eta4) +
															  (GNext - A) / ((1 - Eta4) * (1 - Eta4)) *
															  (1.0 / 3.0 * xVal * xVal * xVal - Eta4 * xVal * xVal + Eta4 * Eta4 * xVal -
																1.0 / 3.0 * Eta4 * Eta4 * Eta4));
			}
			return retVal;
		}

		public double FNext()
		{
			return (FAverage + GNext);
		}

		protected double XPrev;
		protected double XScaling;
		protected double GPrev;
		protected double GNext;
		protected double FAverage;
		protected double Eta4;
		protected double PrevPrimitive;
		protected double A;
	}

	internal class ConvexMonotone4MinHelper : ConvexMonotone4Helper
	{
		public ConvexMonotone4MinHelper(double xPrev, double xNext,
			double gPrev, double gNext,
			double fAverage, double eta4,
			double prevPrimitive)
			: base(xPrev, xNext, gPrev, gNext, fAverage, eta4, prevPrimitive)
		{
			_splitRegion = false;
			if (A + FAverage <= 0.0)
			{
				_splitRegion = true;
				var fPrev = GPrev + FAverage;
				var fNext = GNext + FAverage;
				var reqdShift = (Eta4 * fPrev + (1 - Eta4) * fNext) / 3.0 - FAverage;
				var reqdPeriod = reqdShift * XScaling / (FAverage + reqdShift);
				var xAdjust = XScaling - reqdPeriod;
				_xRatio = xAdjust / XScaling;

				FAverage += reqdShift;
				GNext = fNext - FAverage;
				GPrev = fPrev - FAverage;
				A = -(Eta4 * GPrev + (1.0 - eta4) * GNext) / 2.0;
				_x2 = XPrev + xAdjust * Eta4;
				_x3 = XPrev + XScaling - xAdjust * (1.0 - Eta4);
			}
		}

		public override double GetValue(double x)
		{
			if (!_splitRegion)
				return base.GetValue(x);

			var xVal = (x - XPrev) / XScaling;
			if (x <= _x2)
			{
				xVal /= _xRatio;
				return (FAverage + A + (GPrev - A) * (Eta4 - xVal) * (Eta4 - xVal) / (Eta4 * Eta4));
			}
			else if (x < _x3)
			{
				return 0.0;
			}
			else
			{
				xVal = 1.0 - (1.0 - xVal) / _xRatio;
				return (FAverage + A + (GNext - A) * (xVal - Eta4) * (xVal - Eta4) / ((1 - Eta4) * (1 - Eta4)));
			}
		}

		public override double GetIntegral(double x)
		{
			if (!_splitRegion)
				return base.GetIntegral(x);

			var xVal = (x - XPrev) / XScaling;
			if (x <= _x2)
			{
				xVal /= _xRatio;
				return (PrevPrimitive + XScaling * _xRatio * (FAverage + A + (GPrev - A) / (Eta4 * Eta4) *
																		(Eta4 * Eta4 - Eta4 * xVal + 1.0 / 3.0 * xVal * xVal)) * xVal);
			}
			else if (x <= _x3)
			{
				return (PrevPrimitive + XScaling * _xRatio * (FAverage * Eta4 + A * Eta4 + (GPrev - A) / (Eta4 * Eta4) *
																		(1.0 / 3.0 * Eta4 * Eta4 * Eta4)));
			}
			else
			{
				xVal = 1.0 - (1.0 - xVal) / _xRatio;
				return (PrevPrimitive + XScaling * _xRatio * (FAverage * xVal + A * xVal + (GPrev - A) * (1.0 / 3.0 * Eta4) +
																		(GNext - A) / ((1.0 - Eta4) * (1.0 - Eta4)) *
																		(1.0 / 3.0 * xVal * xVal * xVal - Eta4 * xVal * xVal + Eta4 * Eta4 * xVal -
																		 1.0 / 3.0 * Eta4 * Eta4 * Eta4)));
			}
		}

		private readonly bool _splitRegion;
		private readonly double _xRatio;
		private readonly double _x2;
		private readonly double _x3;
	}

	internal class ConstantGradHelper : ISectionHelper
	{
		public ConstantGradHelper(double fPrev, double prevPrimitive,
			double xPrev, double xNext, double fNext)
		{
			_fPrev = fPrev;
			_prevPrimitive = prevPrimitive;
			_xPrev = xPrev;
			_fGrad = (fNext - fPrev) / (xNext - xPrev);
			_fNext = fNext;
		}

		public double GetValue(double x)
		{
			return (_fPrev + (x - _xPrev) * _fGrad);
		}

		public double GetIntegral(double x)
		{
			return (_prevPrimitive + (x - _xPrev) * (_fPrev + 0.5 * (x - _xPrev) * _fGrad));
		}

		public double FNext()
		{
			return _fNext;
		}

		private readonly double _fPrev;
		private readonly double _prevPrimitive;
		private readonly double _xPrev;
		private readonly double _fGrad;
		private readonly double _fNext;
	}

	internal class QuadraticHelper : ISectionHelper
	{
		public QuadraticHelper(double xPrev, double xNext,
			double fPrev, double fNext,
			double fAverage,
			double prevPrimitive)
		{
			_xPrev = xPrev;
			_xNext = xNext;
			_fPrev = fPrev;
			_fNext = fNext;
			_fAverage = fAverage;
			_prevPrimitive = prevPrimitive;
			_a = 3 * _fPrev + 3 * _fNext - 6 * _fAverage;
			_b = -(4 * _fPrev + 2 * _fNext - 6 * _fAverage);
			_c = _fPrev;
			_xScaling = _xNext - _xPrev;
		}

		public double GetValue(double x)
		{
			double xVal = (x - _xPrev) / _xScaling;
			return (_a * xVal * xVal + _b * xVal + _c);
		}

		public double GetIntegral(double x)
		{
			double xVal = (x - _xPrev) / _xScaling;
			return (_prevPrimitive + _xScaling * (_a / 3 * xVal * xVal + _b / 2 * xVal + _c) * xVal);
		}

		public double FNext()
		{
			return _fNext;
		}

		private readonly double _xPrev;
		private readonly double _xNext;
		private readonly double _fPrev;
		private readonly double _fNext;
		private readonly double _fAverage;
		private readonly double _prevPrimitive;
		private readonly double _xScaling;
		private readonly double _a;
		private readonly double _b;
		private readonly double _c;
	}

	internal class QuadraticMinHelper : ISectionHelper
	{
		public QuadraticMinHelper(double xPrev, double xNext,
			double fPrev, double fNext,
			double fAverage,
			double prevPrimitive)
		{
			_splitRegion = false;
			_x1 = xPrev;
			_x4 = xNext;
			_primitive1 = prevPrimitive;
			_fAverage = fAverage;
			_fPrev = fPrev;
			_fNext = fNext;
			_a = 3 * _fPrev + 3 * _fNext - 6 * _fAverage;
			_b = -(4 * _fPrev + 2 * _fNext - 6 * _fAverage);
			_c = _fPrev;
			double d = _b * _b - 4 * _a * _c;
			_xScaling = _x4 - _x1;
			_xRatio = 1.0;
			if (d > 0)
			{
				double aAv = 36;
				double bAv = -24 * (_fPrev + _fNext);
				double cAv = 4 * (_fPrev * _fPrev + _fPrev * _fNext + _fNext * _fNext);
				double dAv = bAv * bAv - 4.0 * aAv * cAv;
				if (dAv >= 0.0)
				{
					_splitRegion = true;
					double avRoot = (-bAv - Math.Sqrt(dAv)) / (2 * aAv);

					_xRatio = _fAverage / avRoot;
					_xScaling *= _xRatio;

					_a = 3 * _fPrev + 3 * _fNext - 6 * avRoot;
					_b = -(4 * _fPrev + 2 * _fNext - 6 * avRoot);
					_c = _fPrev;
					double xRoot = -_b / (2 * _a);
					_x2 = _x1 + _xRatio * (_x4 - _x1) * xRoot;
					_x3 = _x4 - _xRatio * (_x4 - _x1) * (1 - xRoot);
					_primitive2 =
						_primitive1 + _xScaling * (_a / 3 * xRoot * xRoot + _b / 2 * xRoot + _c) * xRoot;
				}
			}
		}

		public double GetValue(double x)
		{
			double xVal = (x - _x1) / (_x4 - _x1);
			if (_splitRegion)
			{
				if (x <= _x2)
				{
					xVal /= _xRatio;
				}
				else if (x < _x3)
				{
					return 0.0;
				}
				else
				{
					xVal = 1.0 - (1.0 - xVal) / _xRatio;
				}
			}

			return _c + _b * xVal + _a * xVal * xVal;
		}

		public double GetIntegral(double x)
		{
			var xVal = (x - _x1) / (_x4 - _x1);
			if (_splitRegion)
			{
				if (x < _x2)
				{
					xVal /= _xRatio;
				}
				else if (x < _x3)
				{
					return _primitive2;
				}
				else
				{
					xVal = 1.0 - (1.0 - xVal) / _xRatio;
				}
			}
			return _primitive1 + _xScaling * (_a / 3 * xVal * xVal + _b / 2 * xVal + _c) * xVal;
		}

		public double FNext()
		{
			return _fNext;
		}

		private readonly bool _splitRegion;
		private readonly double _x1;
		private readonly double _x2;
		private readonly double _x3;
		private readonly double _x4;
		private readonly double _a;
		private readonly double _b;
		private readonly double _c;
		private readonly double _primitive1;
		private readonly double _primitive2;
		private readonly double _fAverage;
		private readonly double _fPrev;
		private readonly double _fNext;
		private readonly double _xScaling;
		private readonly double _xRatio;
	}
}
