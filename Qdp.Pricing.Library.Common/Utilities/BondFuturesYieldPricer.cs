using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Common.Engines;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.MathMethods.Maths;

namespace Qdp.Pricing.Library.Common.Utilities
{
    public class BondFuturesYieldPricer
    {
        private readonly BondEngineCn _qbBondEngine;
        private readonly BondFutures _bondFuture;
        private readonly IMarketCondition _market;

        private Bond _bond;
        private bool _cleanPriceMktFlg;
        private Cashflow[] _cashflows;

        public BondFuturesYieldPricer(BondFutures bondFuture, IMarketCondition market)
        {
            _qbBondEngine = new BondEngineCn(new BondYieldPricerCn());
            _bondFuture = bondFuture;
            _market = market;
        }

        private bool IsCleandPrice(string bondId) {
            var status = (_market.MktQuote.Value.ContainsKey(bondId) && _market.MktQuote.Value[bondId].Item1 == PriceQuoteType.Clean)
                || (!_market.MktQuote.Value.ContainsKey(bondId));
            return status;
        }

        private double RepoCostRate() {
            var fundingCurve = _market.RiskfreeCurve.HasValue ? _market.RiskfreeCurve.Value :
                YieldCurve.GetConstRateCurve(_market.DiscountCurve.Value, 0.0);
            return fundingCurve.GetCompoundedRate2(_market.ValuationDate, _bondFuture.UnderlyingMaturityDate) - 1.0;
        }

        private double FuturePrice() {
            return _market.MktQuote.Value[_bondFuture.Id].Item2;
        }

        public Dictionary<string, Dictionary<string, RateRecord>> CalcEquation(string calcType)
        {
            var psDict = new Dictionary<string, Dictionary<string, RateRecord>>();
            var fundingCurve = _market.RiskfreeCurve.HasValue ? _market.RiskfreeCurve.Value : YieldCurve.GetConstRateCurve(_market.DiscountCurve.Value, 0.0);
            var reinvestmentCurve = _market.DiscountCurve.HasValue ? _market.DiscountCurve.Value : YieldCurve.GetConstRateCurve(_market.RiskfreeCurve.Value, 0.0);

            var bonds = _bondFuture.Deliverables;
            var length = bonds.Length;

            var resultRateRecord = new Dictionary<string, double>[length];
            for (var i = 0; i < length; ++i)
            {
                var resultDic = new Dictionary<string, double>();
                _bond = bonds[i];
                var tfBondId = _bondFuture.Id + "_" + _bond.Id;

                _cleanPriceMktFlg = IsCleandPrice(_bond.Id);

                resultDic["AiStart"] = _bond.GetAccruedInterest(_market.ValuationDate, _market, false);
                resultDic["AiEnd"] = _bond.GetAccruedInterest(_bondFuture.UnderlyingMaturityDate, _market, false);
                resultDic["ConversionFactor"] = _bondFuture.GetConversionFactor(_bond, _market);
                resultDic["fundingRate"] = fundingCurve.GetSpotRate(0.0);

                _cashflows = _bond.GetCashflows(_market, false).Where(x => x.PaymentDate > _market.ValuationDate && x.PaymentDate <= _bondFuture.UnderlyingMaturityDate).ToArray();
                if (_cashflows.Any())
                {
                    resultDic["Coupon"] = _cashflows.Sum(x => x.PaymentAmount);
                    resultDic["TimeWeightedCoupon"] = _cashflows.Sum(x => x.PaymentAmount * _bondFuture.DayCount.CalcDayCountFraction(x.PaymentDate, _bondFuture.UnderlyingMaturityDate));
                    resultDic["couponsAccruedByReinvestment"] = _cashflows.Sum(x => x.PaymentAmount * (reinvestmentCurve.GetCompoundedRate2(x.PaymentDate, _bondFuture.UnderlyingMaturityDate) - 1.0));
                }
                else
                {
                    resultDic["Coupon"] = 0.0;
                    resultDic["TimeWeightedCoupon"] = 0.0;
                    resultDic["couponsAccruedByReinvestment"] = 0.0;
                }
                //Note: what is this interest income for?
                resultDic["InterestIncome"] = resultDic["AiEnd"] - resultDic["AiStart"]; //  * resultDic["ConversionFactor"];
                resultDic["interestCostRate"] = RepoCostRate();
                resultDic["dt"] = _bondFuture.DayCount.CalcDayCountFraction(_market.ValuationDate, _bondFuture.UnderlyingMaturityDate);

                var inputValues = new double[3];
                double calcMin = 0.0;
                double calcMax = 0.0;
                int changIndex = 0;
                FunctionVariable.UnivariateFunction fcn = null;
                switch (calcType)
                {
                    case "FromFuturesPriceAndBondPrice":
                        inputValues[0] = FuturePrice();
                        inputValues[1] = double.NaN;
                        inputValues[2] = double.NaN;

                        calcMin = -500.0;
                        calcMax = 500.0;
                        changIndex = 2;
                        fcn = new FunctionVariable.UnivariateFunction(resultDic, IrrEquation, inputValues);
                        break;
                    case "FromBondPriceAndIrr":
                        inputValues[0] = double.NaN;
                        inputValues[1] = double.NaN;
                        fcn = GetCalcEquation(tfBondId, inputValues, resultDic);

                        calcMin = 1.0;
                        calcMax = 50000.0;
                        changIndex = 0;
                        //BrentZero2<IUnivariateFunction>.DoSolve(fcn, 0.0, 50000.0, 0);
                        break;
                    case "FromFuturesPriceAndIrr":
                        inputValues[0] = FuturePrice();
                        inputValues[1] = double.NaN;
                        fcn = GetCalcEquation(tfBondId, inputValues, resultDic);

                        calcMin = 1.0;
                        calcMax = 50000.0;
                        changIndex = 1;
                        //BrentZero2<IUnivariateFunction>.DoSolve(fcn, calcMin, calcMax, changIndex);
                        break;
                }
                BrentZero2<IUnivariateFunction>.DoSolve(fcn, calcMin, calcMax, changIndex);

                resultRateRecord[i] = resultDic;
            }

            var resultKeyList = new List<string>(){"FuturesPrice"
            ,"BondDirtyPrice"
            ,"BondCleanPrice"
            ,"BondYieldToMaturity"
            ,"ConversionFactor"
            ,"AiStart"
            ,"AiEnd"
            ,"Coupon"
            ,"InterestIncome"
            ,"InterestCost"
            ,"PnL"
            ,"InvoicePrice"
            ,"Margin"
            ,"Spread"
            ,"Irr"
            ,"Basis"
            ,"NetBasis"
            ,"ModifiedDuration"
            ,"TimeWeightedCoupon"};

            foreach (var resultKey in resultKeyList)
            {
                psDict[resultKey] = resultRateRecord.Select((x, i) => Tuple.Create(bonds[i].Id, new RateRecord { Rate = x[resultKey] })).ToDictionary(x => x.Item1, x => x.Item2);
            }
            return psDict;
        }

        private FunctionVariable.UnivariateFunction GetCalcEquation(string tfBondId, double[] inputValues, Dictionary<string, double> resultDic)
        {
            FunctionVariable.UnivariateFunction fcn = null;
            if (_market.MktQuote.Value.ContainsKey(tfBondId))
            {
                inputValues[2] = _market.MktQuote.Value[tfBondId].Item2;
                if (_market.MktQuote.Value[tfBondId].Item1 == PriceQuoteType.Basis)
                {
                    fcn = new FunctionVariable.UnivariateFunction(resultDic, BasisEquation, inputValues);
                }
                else if (_market.MktQuote.Value[tfBondId].Item1 == PriceQuoteType.NetBasis)
                {
                    fcn = new FunctionVariable.UnivariateFunction(resultDic, NetBasisEquation, inputValues);
                }
                else if (_market.MktQuote.Value[tfBondId].Item1 == PriceQuoteType.Irr)
                {
                    fcn = new FunctionVariable.UnivariateFunction(resultDic, IrrEquation, inputValues);
                }
            }
            return fcn;
        }

        private double IrrEquation(Dictionary<string, double> resultDic, params double[] paramsArray)
        {
            var futuresPrice = paramsArray[0];
            var cleanPrice = paramsArray[1];
            var irr = paramsArray[2];

            resultDic["FuturesPrice"] = futuresPrice;
            resultDic["BondCleanPrice"] = cleanPrice;
            resultDic["Irr"] = irr;
            IrrEquation(resultDic);

            return resultDic["FuturesPrice"] - futuresPrice;
        }

        private double BasisEquation(Dictionary<string, double> resultDic, params double[] paramsArray)
        {
            var futuresPrice = paramsArray[0];
            var cleanPrice = paramsArray[1];
            var basis = paramsArray[2];

            resultDic["FuturesPrice"] = futuresPrice;
            resultDic["BondCleanPrice"] = cleanPrice;
            resultDic["Basis"] = basis;
            BasisEquation(resultDic, true);

            return resultDic["FuturesPrice"] - futuresPrice;
        }

        private double NetBasisEquation(Dictionary<string, double> resultDic, params double[] paramsArray)
        {
            var futuresPrice = paramsArray[0];
            var cleanPrice = paramsArray[1];
            var netBasis = paramsArray[2];

            resultDic["FuturesPrice"] = futuresPrice;
            resultDic["BondCleanPrice"] = cleanPrice;
            resultDic["NetBasis"] = netBasis;
            BasisEquation(resultDic, false);

            return resultDic["FuturesPrice"] - futuresPrice;
        }

        private void BasisEquation(Dictionary<string, double> resultDic, bool basisType)
        {
            // Calc bond
            CalcBond(resultDic);

            resultDic["InterestCost"] = resultDic["BondDirtyPrice"] * resultDic["interestCostRate"];
            resultDic["InterestIncome"] = resultDic["AiEnd"] - resultDic["AiStart"] + resultDic["Coupon"] + resultDic["couponsAccruedByReinvestment"];
            resultDic["PnL"] = resultDic["InterestIncome"] - resultDic["InterestCost"];
            resultDic["timeWeightedDirtyPrice"] = resultDic["BondDirtyPrice"] * resultDic["dt"];

            if (basisType)
            {
                resultDic["NetBasis"] = resultDic["Basis"] - resultDic["PnL"];
            }
            else
            {
                resultDic["Basis"] = resultDic["NetBasis"] + resultDic["PnL"];
            }

            resultDic["FuturesPrice"] = (resultDic["BondDirtyPrice"] * (1 + resultDic["interestCostRate"]) - (resultDic["InterestIncome"] + resultDic["NetBasis"] + resultDic["AiStart"])) / resultDic["ConversionFactor"];
            resultDic["InvoicePrice"] = resultDic["FuturesPrice"] * resultDic["ConversionFactor"] + resultDic["AiEnd"];
            resultDic["Irr"] = _market.ValuationDate == _bondFuture.UnderlyingMaturityDate
                             ? 0.0
                             : (resultDic["InvoicePrice"] + resultDic["Coupon"] - resultDic["BondDirtyPrice"]) / (resultDic["timeWeightedDirtyPrice"] - resultDic["TimeWeightedCoupon"]);
            resultDic["Margin"] = resultDic["InvoicePrice"] - resultDic["BondDirtyPrice"] + resultDic["Coupon"];
            resultDic["Spread"] = resultDic["Irr"] - resultDic["fundingRate"];
        }

        private void IrrEquation(Dictionary<string, double> resultDic)
        {
            // Calc bond
            CalcBond(resultDic);

            resultDic["InterestCost"] = resultDic["BondDirtyPrice"] * resultDic["interestCostRate"];
            resultDic["InterestIncome"] = resultDic["AiEnd"] - resultDic["AiStart"] + resultDic["Coupon"] + resultDic["couponsAccruedByReinvestment"];
            resultDic["PnL"] = resultDic["InterestIncome"] - resultDic["InterestCost"];
            resultDic["timeWeightedDirtyPrice"] = resultDic["BondDirtyPrice"] * resultDic["dt"];

            if (_cashflows.Any())
            {
                resultDic["couponsAccruedByIrr"] = double.IsNaN(resultDic["Irr"]) ? 0.0 :
                    _cashflows.Sum(
                        x => x.PaymentAmount * (
                        //Note: wind can do IRR to settlement day
                        (1 + resultDic["Irr"] * new Act365().CalcDayCountFraction(_market.ValuationDate, _bondFuture.UnderlyingMaturityDate, null, null)) /
                        (1 + resultDic["Irr"] * new Act365().CalcDayCountFraction(_market.ValuationDate, x.PaymentDate, null, null)) - 1.0));
            }
            else
            {
                resultDic["couponsAccruedByIrr"] = 0.0;
            }

            //Note: 单利，不是复利。  万得可以选，irr到settlement (maturity + 3)  or payment(maturity +2 )
            var compoundedRate = double.IsNaN(resultDic["Irr"]) ? 0.0 : (1 + resultDic["Irr"] * new Act365().CalcDayCountFraction(_market.ValuationDate, _bondFuture.UnderlyingMaturityDate, null, null));

            //按wind 最廉券计算，应该是 (bondDirty - couponDuringThePeriod) * compoundedRate,  再看看
            var bondPricesCompounedByIrr = resultDic["BondDirtyPrice"] * compoundedRate;
            resultDic["FuturesPrice"] = (bondPricesCompounedByIrr - resultDic["AiEnd"] - resultDic["Coupon"] - resultDic["couponsAccruedByIrr"]) / resultDic["ConversionFactor"];
            resultDic["InvoicePrice"] = resultDic["FuturesPrice"] * resultDic["ConversionFactor"] + resultDic["AiEnd"];
            resultDic["Basis"] = resultDic["BondCleanPrice"] - resultDic["FuturesPrice"] * resultDic["ConversionFactor"];
            resultDic["NetBasis"] = resultDic["Basis"] - resultDic["PnL"];
            resultDic["Margin"] = resultDic["InvoicePrice"] - resultDic["BondDirtyPrice"] + resultDic["Coupon"];
            resultDic["Spread"] = resultDic["Irr"] - resultDic["fundingRate"];
        }

        private void CalcBond(Dictionary<string, double> resultDic)
        {
            IMarketCondition newMarket = _market;
            if (_cleanPriceMktFlg && resultDic.ContainsKey("BondCleanPrice") && !double.IsNaN(resultDic["BondCleanPrice"]))
            {
                newMarket =
                       _market.UpdateCondition(
                           new UpdateMktConditionPack<Dictionary<string, Tuple<PriceQuoteType, double>>>(x => x.MktQuote,
                               new Dictionary<string, Tuple<PriceQuoteType, double>> { { _bond.Id, Tuple.Create(PriceQuoteType.Clean, resultDic["BondCleanPrice"]) } }));
            }
            if (newMarket.MktQuote.Value.ContainsKey(_bond.Id))
            {
                var bResult = _qbBondEngine.Calculate(_bond, newMarket, PricingRequest.Ytm | PricingRequest.ModifiedDuration);
                resultDic["BondDirtyPrice"] = bResult.DirtyPrice;
                resultDic["BondCleanPrice"] = bResult.CleanPrice;
                resultDic["BondYieldToMaturity"] = bResult.Ytm;
                resultDic["ModifiedDuration"] = bResult.ModifiedDuration;
            }
        }

        public double CalcFutureCtdBasis(Bond ctdBond, double conversionFactor) =>
            FuturePrice() - CalcBondDirtyPrice(ctdBond) / conversionFactor;

        private double CalcBondDirtyPrice(Bond bond)
        {
            double dirtyPrice;
            if (IsCleandPrice(bond.Id))
                dirtyPrice =  _qbBondEngine.Calculate(bond, _market, PricingRequest.Ytm | PricingRequest.ModifiedDuration).DirtyPrice;
            else
                dirtyPrice =  _market.MktQuote.Value[bond.Id].Item2;
            return dirtyPrice;
        }
	}
}
