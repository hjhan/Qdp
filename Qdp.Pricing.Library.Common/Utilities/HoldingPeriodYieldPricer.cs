using System;
using System.Collections.Generic;
using Qdp.Foundation.Implementations;
using Qdp.Foundation.Utilities;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.Maths;

namespace Qdp.Pricing.Library.Common.Utilities
{
	public class HoldingPeriodYieldPricer : IHoldingPeriodYieldPricer
	{
		private readonly Date _startDate;
		private readonly Date _endDate;
		private readonly double _side;
		private readonly double _notional;
		private readonly double _faceAmount;
		private readonly double _startCommission;
		private readonly double _endCommission;
		private readonly double _allCommission;
		private readonly double _yearFraction;
		private readonly double _businessTaxRate;
		private readonly double _interestTaxRate;
		private readonly double _holdingCost;

		private readonly double _startAi;
		private readonly double _endAi;
		private readonly double _principalBetweenTemp;
		private readonly double _interestBetweenTemp;

		public HoldingPeriodYieldPricer(HoldingPeriod holdingPeriod, double startAi, double endAi, double principalBetweenTemp, double interestBetweenTemp)
		{
			_startDate = holdingPeriod.StartDate;
			_endDate = holdingPeriod.UnderlyingMaturityDate;
			_side = holdingPeriod.Direction == Direction.BuyThenSell ? 1.0 : -1.0;
			_notional = holdingPeriod.Notional;
			_faceAmount = 100.0;
			_startCommission = holdingPeriod.StartFrontCommission + holdingPeriod.StartBackCommission;
			_endCommission = holdingPeriod.EndFrontCommission + holdingPeriod.EndBackCommission;
			_allCommission = _startCommission + _endCommission;
			_yearFraction = holdingPeriod.PaymentBusinessDayCounter.CalcDayCountFraction(_startDate, _startDate == _endDate ? _endDate.AddDays(1) : _endDate);
			_businessTaxRate = holdingPeriod.BusinessTaxRate;
			_interestTaxRate = holdingPeriod.InterestTaxRate;
			_holdingCost = holdingPeriod.HoldingCost;

			_startAi = startAi;
			_endAi = endAi;
			_principalBetweenTemp = principalBetweenTemp;
			_interestBetweenTemp = interestBetweenTemp;
		}

		public double CalcAnnualizedYieldCleanPrice(string calcRequestType, double inputValue1, double inputValue2, Dictionary<string, double> result)
		{
			double returnValue = 0.0;
			try
			{
				IUnivariateFunction fcn;
				switch (calcRequestType)
				{
					case "AnnualYieldFromCleanPrice":
						fcn = new FunctionVariable.UnivariateFunction(result, YieldEquation, inputValue1, inputValue2, double.NaN);
						returnValue = BrentZero2<IUnivariateFunction>.DoSolve(fcn, -1000, 1000, 0.05, 2);
						CalcTotal(result);
						break;
					case "EndCleanPriceFromAnnual":
						fcn = new FunctionVariable.UnivariateFunction(result, YieldEquation, inputValue1, double.NaN, inputValue2);
						returnValue = BrentZero2<IUnivariateFunction>.DoSolve(fcn, 0.0, 10000.0, 100.0, 1);
						CalcTotal(result);
						break;
					case "StartCleanPriceFromAnnual":
						fcn = new FunctionVariable.UnivariateFunction(result, YieldEquation, double.NaN, inputValue1, inputValue2);
						returnValue = BrentZero2<IUnivariateFunction>.DoSolve(fcn, 0.0, 10000.0, 100.0, 0);
						CalcTotal(result);
						break;
					case "EndCleanPriceFromPnl":
						fcn = new FunctionVariable.UnivariateFunction(result, NetPnlEquation, inputValue1, double.NaN, inputValue2);
						returnValue = BrentZero2<IUnivariateFunction>.DoSolve(fcn, 0.0, 10000.0, 100.0, 1);
						CalcTotal(result);
						break;
					case "StartCleanPriceFromPnl":
						fcn = new FunctionVariable.UnivariateFunction(result, NetPnlEquation, double.NaN, inputValue1, inputValue2);
						returnValue = BrentZero2<IUnivariateFunction>.DoSolve(fcn, 0.0, 10000.0, 100.0, 0);
						CalcTotal(result);
						break;
				}
			}
			catch (Exception ex)
			{
				throw new PricingBaseException("HoldingPeriod AnnualizedYield does not converbe " + ex.GetDetail());
			}
			return returnValue;
		}

		private void CalcTotal(Dictionary<string, double> result)
		{
			result["incomeBetween"] = (result.ContainsKey("principalBetween") ? result["principalBetween"] : 0.0) + (result.ContainsKey("interestBetween") ? result["interestBetween"] : 0.0);
			result["frontCommissionPreTax"] = (result.ContainsKey("startFrontCommission") ? result["startFrontCommission"] : 0.0) + (result.ContainsKey("endFrontCommission") ? result["endFrontCommission"] : 0.0);
			result["backCommissionPreTax"] = (result.ContainsKey("startBackCommission") ? result["startBackCommission"] : 0.0) + (result.ContainsKey("endBackCommission") ? result["endBackCommission"] : 0.0);
			result["frontCommissionAT"] = result["frontCommissionPreTax"];
			result["backCommissionAT"] = result["backCommissionPreTax"];
		}

		private double YieldEquation(Dictionary<string, double> result, params double[] paramsArray)
		{
			var startCleanPrice = paramsArray[0];
			var endCleanPrice = paramsArray[1];
			var annualizedYield = paramsArray[2];

			NetPnl(startCleanPrice, endCleanPrice, result);
			result["netAnnualizedYield"] = -_side * result["netPnL"] / result["startTotal"] / _yearFraction;
			return result["netAnnualizedYield"] - annualizedYield;
		}

		private double NetPnlEquation(Dictionary<string, double> result, params double[] paramsArray)
		{
			var startCleanPrice = paramsArray[0];
			var endCleanPrice = paramsArray[1];
			var netPnl = paramsArray[2];

			double pnl = NetPnl(startCleanPrice, endCleanPrice, result);
			result["netAnnualizedYield"] = -_side * result["netPnL"] / result["startTotal"] / _yearFraction;
			return pnl - netPnl;
		}

		private double NetPnl(double startCleanPrice, double endCleanPrice, Dictionary<string, double> result)
		{
			result["startCleanPrice"] = startCleanPrice;
			result["endCleanPrice"] = endCleanPrice;

			result["startCleanAmount"] = -_side * _notional / _faceAmount * startCleanPrice;
			result["endCleanAmount"] = _side * _notional / _faceAmount * endCleanPrice;
			result["startInterest"] = -_side * _notional / _faceAmount * _startAi;
			result["endInterest"] = _side * _notional / _faceAmount * _endAi;
			result["startTotal"] = result["startCleanAmount"] + result["startInterest"] - _startCommission;
			result["endTotal"] = result["endCleanAmount"] + result["endInterest"] - _endCommission;

			result["principalBetween"] = (_side > 0 ? _side : 0.0) * _notional / _faceAmount * _principalBetweenTemp;
			result["interestBetween"] = Math.Max(0.0, _notional / _faceAmount * _interestBetweenTemp);

			result["cleanAmountPnLPreTax"] = result["startCleanAmount"] + result["endCleanAmount"] + result["principalBetween"];
			result["interestPnLPreTax"] = result["startInterest"] + result["endInterest"] + result["interestBetween"];
			result["annualReturnPreCost"] = -_side * (result["cleanAmountPnLPreTax"] + result["interestPnLPreTax"]) / result["startTotal"] / _yearFraction;

			result["pnLPretax"] = result["cleanAmountPnLPreTax"] + result["interestPnLPreTax"] - _allCommission;
			result["yieldPreTax"] = -_side * result["pnLPretax"] / result["startTotal"];

			result["businessTax"] = Math.Max(0, result["cleanAmountPnLPreTax"] * _businessTaxRate);
			result["interestTax"] = Math.Max(0, result["interestPnLPreTax"] * _interestTaxRate);
			result["totalTax"] = result["businessTax"] + result["interestTax"];

			result["cleanAmountPnLAT"] = result["cleanAmountPnLPreTax"] - result["businessTax"];
			result["interestPnLAT"] = result["interestPnLPreTax"] - result["interestTax"];
			result["pnLAT"] = result["cleanAmountPnLAT"] + result["interestPnLAT"] - _allCommission;
			result["yieldAT"] = -_side * result["pnLAT"] / result["startTotal"];
			result["annualizedYieldAT"] = result["yieldAT"] / _yearFraction;
			result["netPnL"] = result["pnLAT"] - _holdingCost;

			return result["netPnL"];
		}
	}
}
