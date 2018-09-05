using System;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Base.Curves.Interfaces;

namespace Qdp.Pricing.Library.Base.Curves.Interpolators
{
	public static class InterpolationExtension
	{
		public static IInterpolator GetInterpolator(this Interpolation interpolation, Tuple<double, double>[] keyPoints)
		{
			switch (interpolation)
			{
				case Interpolation.CubicHermiteMonotic:
					return new CubicHermiteMonotonicInterpolator(keyPoints);
				case Interpolation.CubicHermiteFd:
					return new CubicHermiteFdInterpolator(keyPoints);
				case Interpolation.ForwardFlat:
					return new ForwardFlatInterpolator(keyPoints);
				case Interpolation.CubicSpline:
					return new CubicHermiteFdInterpolator(keyPoints);
				case Interpolation.LinearCubicSpline:
					return new CubicHermiteFdInterpolator(keyPoints);
				case Interpolation.LogLinear:
					return new LogLinearInterpolator(keyPoints);
				case Interpolation.ConvexMonotic:
					return new ConvexMonoticInterpolator(keyPoints);
				case Interpolation.ExponentialSpline:
					return new ExponentialSplineInterpolator(keyPoints);
				default:
					return new LinearInterpolator(keyPoints);
			}
		}

		public static IInterpolator2D GetInterpolator(this Interpolation2D interpolation, double[] rowGrid, double[] colGrid, double[,] valueOnGrid)
		{
			switch (interpolation)
			{
				case Interpolation2D.BiLinear:
                    return new BiLinearInterpolator(rowGrid, colGrid, valueOnGrid);
                case Interpolation2D.BiCubicSpline:
                    return new BiCubicSplineInterpolator(rowGrid, colGrid, valueOnGrid);
                case Interpolation2D.VarianceBiLinear:
                    return new VarianceBiLinearInterpolator(rowGrid, colGrid, valueOnGrid);
                case Interpolation2D.VarianceBiCubicSpline:
                    return new VarianceBiCubicSplineInterpolator(rowGrid, colGrid, valueOnGrid);
                default:
					return new BiLinearInterpolator(rowGrid, colGrid, valueOnGrid);
			}
		}
	}
}
