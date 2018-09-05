using System;
using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Equity.Interfaces;
using Qdp.Pricing.Library.Equity.Options;

namespace Qdp.Pricing.Library.Equity.Engines.Analytical
{
	public class AnalyticalDoubleBarrierEuropeanOptionEngine : BaseNumericalOptionEngine
	{
		private bool _useFourier;
		public AnalyticalDoubleBarrierEuropeanOptionEngine(bool useFourier)
		{
			_useFourier = useFourier;
		}
        //Todo:Payoff 
        protected override double CalcIntrinsicValue(IOption option, IMarketCondition market)
        {
            if (option.OptionType == OptionType.Call)
                return (market.SpotPrices.Value.Values.First() - option.Strike) * option.Notional;
            else
                return (option.Strike - market.SpotPrices.Value.Values.First()) * option.Notional;
        }
        protected override double CalcPv(IOption option, IMarketCondition market, double timeIncrement = 0.0)
		{
			if (!(option is BarrierOption))
			{
				throw new PricingBaseException("");
			}
			var trade = (BarrierOption)option;
			var exerciseDate = trade.ExerciseDates.Last();
			var maturityDate = trade.UnderlyingMaturityDate;

			double pv = double.NaN;
			if (_useFourier)
			{
				var barrierCalculator = new BarrierOptionFourierPvPricer(trade, market, timeIncrement: timeIncrement);
				pv = barrierCalculator.Pv();
			}
			else
			{
				var barrierCalculator = new BarrierOptionPvPricer(trade, market, timeIncrement: timeIncrement);
				pv = barrierCalculator.Pv();
			}
			return pv * trade.ParticipationRate * trade.Notional;
		}

        //Note: stopping time does not fine-tuned option-time-value pricing mode yet.
        protected override double StoppingTime(IOption option, IMarketCondition[] markets)
        {
            var trade = (BarrierOption)option;
            if (_useFourier)
            {
                var calculator = new BarrierOptionFourierPvPricer(trade, markets[0]);
                return calculator.StoppingTime();
            }
            else {
                var calculator = new BarrierOptionPvPricer(trade, markets[0]);
                return calculator.StoppingTime();
            }
                
        }
    }

	#region
	internal class BarrierOptionFourierPvPricer
	{
		private OptionType _optionType;

		private double _S;	// spot price
		private double _K;	// strike
		private double _A;	// barrier, lower barrier in case of double barrier option
		private double _B; // upper barrier in case of double barrier option
		private double _T;	// maturity in years
		private double _r; // insterest rate
		private double _v; // vol

		private double b;
		private double a;
		private double x0;
		private double v2;
		private double u;
		private double u2;

		private double _rebate;
		private double _coupon;

		public BarrierOptionFourierPvPricer(BarrierOption barrierOption, IMarketCondition market, double timeIncrement = 0.0)
		{
			var exerciseDate = barrierOption.ExerciseDates.Last();
			var riskFreeRate = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, exerciseDate);
			var spotPrice = market.SpotPrices.Value.Values.First();
			var vol = market.VolSurfaces.Value.Values.First().GetValue(market.ValuationDate, barrierOption.Strike, spotPrice);
            var exerciseInYears = AnalyticalOptionPricerUtil.timeToMaturityFraction(market.ValuationDate, exerciseDate, barrierOption) + timeIncrement;

            _optionType = barrierOption.OptionType;
			_rebate = barrierOption.Rebate;
			_coupon = barrierOption.Coupon;
			_K = barrierOption.Strike;
			_S = spotPrice;
			_T = exerciseInYears;
			_v = vol;
			_r = riskFreeRate;
			_B = barrierOption.UpperBarrier;
			_A = barrierOption.Barrier;

			b = Math.Log(_B);
			a = Math.Log(_A);
			x0 = Math.Log(_S);
			v2 = Math.Pow(_v, 2);
			u = _r - v2 / 2;
			u2 = Math.Pow(u, 2);
		}

		public double Pv()
		{
			var q = Q(0, b)- Q(0, a);
			var rebate = (1 - q) * _rebate;
			var coupon = q * _coupon;
			switch (_optionType)
			{
				case OptionType.Call:
					return  Math.Exp(-_r * _T) * (CallCalc() + rebate + coupon);
				case OptionType.Put:
					return Math.Exp(-_r * _T) * (PutCalc() + coupon + coupon);
			}
			return double.NaN;
		}

		private double CallCalc()
		{
			var result = 0.0;
			if (_K < _B)
			{
				var y = Math.Max(a, Math.Log(_K));
				var e = (Q(1, b) - Q(1, y));
				var d = _K*(Q(0, b) - Q(0, y));
				
				result = e - d;
			}
			return result;
		}

		private double PutCalc()
		{
			var result = 0.0;
			if (_K > _A)
			{
				var y = Math.Min(b, Math.Log(_K));
				var e = _K * (Q(0, y) - Q(0, a));
				var d = Q(1, y) - Q(1, a);

				result = e - d;
			}
			return result;
		}

		private double Q(double lambda, double x)
		{
			var tmp1 = lambda + u/v2;
			var tmp2 = Math.PI/(b - a);

			var result = 0.0;
			var n = 1;
			var newTerm = 1.0e100;
			while (Math.Abs(newTerm) > 1e-28)
			{
				newTerm = tmp1*Math.Sin(n*tmp2*(x - a)) - n*tmp2*Math.Cos(n*tmp2*(x - a));
				newTerm *= Math.Sin(n*tmp2*(x0 - a));
				newTerm /= tmp1*tmp1 + n*n*tmp2*tmp2;
				newTerm *= Math.Exp(-n*n*tmp2*tmp2*v2*_T/2);
				result += newTerm;
				n ++;
			}

			result *= (2/(b - a))*Math.Exp(-u*x0/v2 - u2*_T/(2*v2) + tmp1*x);
			return result;
		}

        public double StoppingTime()
        {
            double lambda = _r / _v - _v / 2;
            double h = 1 / _v * Math.Log(_B / _S);
            double l = 1 / _v * Math.Log(_A / _S);

            double E = ((h - l) * (Math.Exp(lambda * h) * Math.Sinh(lambda * h) - Math.Exp(lambda * l) * Math.Sinh(lambda * l))
                * Math.Cosh(lambda * (h - l)) - (h * Math.Exp(lambda * l) * Math.Cosh(lambda * h) - l * Math.Exp(lambda * h) * Math.Cosh(lambda * l))
                * Math.Sinh(lambda * (h - l))) / lambda / Math.Pow(Math.Sinh(lambda * (h - l)), 2);

            double D = 0;
            double n;

            for (n = 1; n <= 100; n = n + 1)
            {
                double u = n * n * Math.PI * Math.PI / 2 / Math.Pow(h - l, 2) + lambda * lambda;
                D = D + Math.PI / Math.Pow(h - l, 2) * Math.Pow(-1, n - 1) * n * Math.Exp(-u * _T) / u / u * (Math.Exp(lambda * l) *
                    Math.Sin(n * Math.PI * h / (h - l)) - Math.Exp(lambda * h) * Math.Sin(n * Math.PI * l / (h - l)));
            }

            return E - D;
        }
    }
	#endregion

	#region
	internal class BarrierOptionPvPricer
	{
		private OptionType _optionType;

		private double _S;	// spot price
		private double _K;	// strike
		private double _A;	// barrier, lower barrier in case of double barrier option
		private double _B; // upper barrier in case of double barrier option
		private double _T;	// maturity in years
		private double _r; // insterest rate
		private double _v; // vol

		private double b;
		private double a;
		private double x0;
		private double v2;
		private double u;

		private double _rebate;
		private double _coupon;

		public BarrierOptionPvPricer(BarrierOption barrierOption, IMarketCondition market, double timeIncrement = 0.0)
		{
			var exerciseDate = barrierOption.ExerciseDates.Last();
			var riskFreeRate = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, exerciseDate);
			var spotPrice = market.SpotPrices.Value.Values.First();
			var vol = market.VolSurfaces.Value.Values.First().GetValue(market.ValuationDate, barrierOption.Strike);
            var exerciseInYears = AnalyticalOptionPricerUtil.timeToMaturityFraction(market.ValuationDate, exerciseDate, barrierOption) + timeIncrement;

            _optionType = barrierOption.OptionType;
			_rebate = barrierOption.Rebate;
			_coupon = barrierOption.Coupon;
			_K = barrierOption.Strike;
			_S = spotPrice;
			_T = exerciseInYears;
			_v = vol;
			_r = riskFreeRate;
			_B = barrierOption.UpperBarrier;
			_A = barrierOption.Barrier;

			b = Math.Log(_B);
			a = Math.Log(_A);
			x0 = Math.Log(_S);
			v2 = Math.Pow(_v, 2);
			u = _r - v2 / 2;
		}

		public double Pv()
		{
			var q = Q();
			var rebate = (1 - q) * _rebate;
			var coupon = q * _coupon;
			switch (_optionType)
			{
				case OptionType.Call:
					return CallCalc() + rebate;
				case OptionType.Put:
					return PutCalc() + coupon;
			}
			return double.NaN;
		}

		private double Q()
		{
			var result = M(x0) - M(2 * b - x0);
			var n = 1;
			var newTerm = 1.0e100;
			while (Math.Abs(newTerm) > 1e-28)
			{
				newTerm = M(x0 - 2 * n * (b - a)) - M(2 * b - x0 - 2 * n * (b - a));
				result += newTerm;
				n++;
			}
			n = 1;
			newTerm = 1.0e100;
			while (Math.Abs(newTerm) > 1e-28)
			{
				newTerm = M(x0 - 2 * n * (b - a)) - M(2 * b - x0 - 2 * n * (b - a));
				result += newTerm;
				n--;
			}
			return result;
		}

		private double M(double c)
		{
			var temp1 = 1/Math.Sqrt(v2*_T)*Math.Exp(u*(c - x0)/v2);
			var temp2 = MathNet.Numerics.Distributions.Normal.CDF(0, 1, (b - c - u * _T) / Math.Sqrt(v2 * _T)) - MathNet.Numerics.Distributions.Normal.CDF(0, 1, (a - c - u * _T) / Math.Sqrt(v2 * _T));
			return temp1*temp2;
		}

		private double CallCalc()
		{
			var result = L(x0 , Math.Max(a, Math.Log(_K)), b) - L((2 * b - x0), Math.Max(a, Math.Log(_K)), b);
			if (_K < _B)
			{
				var n = 1;
				var newTerm = 1.0e100;
				while (Math.Abs(newTerm) > 1e-28)
				{
					newTerm = L((x0 - 2 * n * (b - a)), Math.Max(a, Math.Log(_K)), b) - L((2 * b - x0 - 2 * n * (b - a)), Math.Max(a, Math.Log(_K)), b);
					result += newTerm;
					n++;
				}
				n = -1;
				newTerm = 1.0e100;
				while (Math.Abs(newTerm) > 1e-28)
				{
					newTerm = L((x0 - 2 * n * (b - a)), Math.Max(a, Math.Log(_K)), b) - L((2 * b - x0 - 2 * n * (b - a)), Math.Max(a, Math.Log(_K)), b);
					result += newTerm;
					n--;
				}
			}
			return result;
		}

		private double PutCalc()
		{
			var result = L(2 * b - x0, a, Math.Min(b, Math.Log(_K))) - L(x0 , a, Math.Min(b, Math.Log(_K)));
			if (_K > _A)
			{
				var n = 1;
				var newTerm = 1.0e100;
				while (Math.Abs(newTerm) > 1e-28)
				{
					newTerm = L((2 * b - x0 - 2 * n * (b - a)), a, Math.Min(b, Math.Log(_K))) - L((x0 - 2 * n * (b - a)), a, Math.Min(b, Math.Log(_K)));
					result += newTerm;
					n++;
				}
				n = -1;
				newTerm = 1.0e100;
				while (Math.Abs(newTerm) > 1e-28)
				{
					newTerm = L((2 * b - x0 - 2 * n * (b - a)), a, Math.Min(b, Math.Log(_K))) - L((x0 - 2 * n * (b - a)), a, Math.Min(b, Math.Log(_K)));
					result += newTerm;
					n--;
				}
			}
			return result;
		}

		private double L(double c, double g, double h)
		{
			var tmp1 = Math.Exp(u/v2*(c-x0)-_r*_T);
			var tmp2 = Math.Exp(c + (u + v2 / 2) * _T) * MathNet.Numerics.Distributions.Normal.CDF(0, 1, (h - c - (u + v2) * _T) / (_v * Math.Sqrt(_T)))
				- _K * MathNet.Numerics.Distributions.Normal.CDF(0, 1, (h - c - u * _T)/(_v * Math.Sqrt(_T)));
			var tmp3 = Math.Exp(c + (u + v2 / 2) * _T) * MathNet.Numerics.Distributions.Normal.CDF(0, 1, (g - c - (u + v2) * _T)) / (_v * Math.Sqrt(_T))
				- _K * MathNet.Numerics.Distributions.Normal.CDF(0, 1, (g - c - u * _T) / (_v * Math.Sqrt(_T)));

			var result = tmp1 * (tmp2 - tmp3);
			return result;
		}

        public double StoppingTime()
        {
            double lambda = _r / _v - _v / 2;
            double h = 1 / _v * Math.Log(_B / _S);
            double l = 1 / _v * Math.Log(_A / _S);

            double E = ((h - l) * (Math.Exp(lambda * h) * Math.Sinh(lambda * h) - Math.Exp(lambda * l) * Math.Sinh(lambda * l))
                * Math.Cosh(lambda * (h - l)) - (h * Math.Exp(lambda * l) * Math.Cosh(lambda * h) - l * Math.Exp(lambda * h) * Math.Cosh(lambda * l))
                * Math.Sinh(lambda * (h - l))) / lambda / Math.Pow(Math.Sinh(lambda * (h - l)), 2);

            double D = 0;
            double n;
           
            for (n = 1; n <= 100; n = n + 1)
            {
                double u = n * n * Math.PI * Math.PI / 2 / Math.Pow(h - l, 2) + lambda * lambda;
                D = D + Math.PI / Math.Pow(h - l, 2) * Math.Pow(-1, n-1) * n * Math.Exp(-u * _T) / u / u * (Math.Exp(lambda * l) *
                    Math.Sin(n * Math.PI * h / (h - l)) - Math.Exp(lambda * h) * Math.Sin(n * Math.PI * l / (h - l)));
            }

            return E - D;
        }

	}
	#endregion
}
