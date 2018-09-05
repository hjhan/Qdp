using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Base.Implementations;
using Qdp.Pricing.Library.Base.Interfaces;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Engines;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Utilities;
using Qdp.Pricing.Library.Common.Utilities.Coupons;
using Qdp.Pricing.Library.Common.Utilities.Mortgage;

namespace Qdp.Pricing.Library.Common
{
    /// <summary>
    /// 债券类
    /// </summary>
	public class Bond : ICashflowInstrument, IUnderlyingInstrument, ICalibrationSupportedInstrument
	{
		private List<bool> _isRegualDate;
        /// <summary>
        /// Id
        /// </summary>
		public string Id { get; private set; }

        /// <summary>
        /// 类型名
        /// </summary>
        public string TypeName { get { return "Bond"; } }

        /// <summary>
        /// 开始日期
        /// </summary>
        public Date StartDate { get; private set; }

        /// <summary>
        /// 到期日
        /// </summary>
		public Date UnderlyingMaturityDate { get; private set; }

        /// <summary>
        /// 交易日历
        /// </summary>
		public ICalendar Calendar { get; private set; }

        /// <summary>
        /// 利息支付频率
        /// </summary>
		public Frequency PaymentFreq { get; private set; }

        /// <summary>
        /// 计息残段
        /// </summary>
		public Stub Stub { get; private set; }

        /// <summary>
        /// 名义本金
        /// </summary>
		public double Notional { get; set; }

        /// <summary>
        /// 币种
        /// </summary>
		public CurrencyCode Currency { get; private set; }

        /// <summary>
        /// 利息
        /// </summary>
		public ICoupon Coupon { get; private set; }

        /// <summary>
        /// 计息日期规则
        /// </summary>
		public IDayCount AccrualDayCount { get; private set; }

        /// <summary>
        /// 支付日日期规则
        /// </summary>
		public IDayCount PaymentDayCount { get; private set; }

        /// <summary>
        /// 计息交易日调整规则
        /// </summary>
		public BusinessDayConvention AccrualBizDayRule { get; private set; }

        /// <summary>
        /// 支付交易日调整规则
        /// </summary>
		public BusinessDayConvention PaymentBizDayRule { get; private set; }

        /// <summary>
        /// 交割日期规则
        /// </summary>
		public DayGap SettlmentGap { get; private set; }

        /// <summary>
        /// 交易市场
        /// </summary>
		public TradingMarket BondTradeingMarket { get; private set; }

        /// <summary>
        /// 发行价
        /// </summary>
		public double IssuePrice { get; private set; }

        /// <summary>
        /// 发行利率
        /// </summary>
		public double IssueRate { get; private set; }

        /// <summary>
        /// 债券赎回
        /// </summary>
		public IRedemption Redemption { get; private set; }

        /// <summary>
        /// 第一个支付日
        /// </summary>
		public Date FirstPaymentDate { get; private set; }

        /// <summary>
        /// 本金摊还
        /// </summary>
		public IAmortization Amoritzation { get; private set; }

        /// <summary>
        /// 计息日期计划
        /// </summary>
		public Schedule Accruals { get; private set; }

        /// <summary>
        /// 支付日期计划
        /// </summary>
		public Schedule PaymentSchedule { get; private set; }

        /// <summary>
        /// 是否为零息债券
        /// </summary>
		public bool IsZeroCouponBond { get; private set; }

        /// <summary>
        /// 是否总在月末
        /// </summary>
		public bool StickToEom { get; private set; }

        /// <summary>
        /// 摊还本金类型
        /// </summary>
		public AmortizationType AmortizationType { get; private set; }

        /// <summary>
        /// 按日期的摊还计划
        /// </summary>
		public Dictionary<Date, double> AmortizationInDates { get; private set; }

        /// <summary>
        /// 按顺序的摊还计划
        /// </summary>
		public Dictionary<int, double> AmortizationInIndex { get; private set; }

        /// <summary>
        /// 是否在摊还后用新的本金重新计算摊还计划
        /// </summary>
		public bool RenormalizeAfterAmoritzation { get; private set; }

        /// <summary>
        /// 补偿利率
        /// </summary>
		public Dictionary<int, double> StepWiseCompensationRate { get; private set; }

		private double[] _compensationRate;

		public Dictionary<string, double> OptionToCall { get; set; }

		public Dictionary<string, double> OptionToPut { get; set; }

		public Dictionary<string, double> OptionToAssPut { get; set; }

        /// <summary>
        /// 净价
        /// </summary>
		public bool RoundCleanPrice { get; set; }

		private IMortgageCalculator _mortgageCalculator;
		private double _settlementCoupon;

		public Bond(
			string id,
			Date startDate,
			Date maturityDate,
			double notional,
			CurrencyCode currency,
			ICoupon coupon,
			ICalendar calendar,
			Frequency paymentFreq,
			Stub stub,
			IDayCount accrualDayCount,
			IDayCount paymentDayCount,
			BusinessDayConvention accrualBizDayRule,
			BusinessDayConvention paymentBizDayRule,
			DayGap settlementGap,
			TradingMarket bondTradingMarket,
			bool stickToEom = false,
			IRedemption redemption = null,
			Date firstPaymentDate = null,
			bool isZeroCouponBond = false,
			double issuePrice = double.NaN,
			double issueRate = double.NaN,
			AmortizationType amortionType = AmortizationType.None,
			Dictionary<Date, double> amortizationInDates = null,
			Dictionary<int, double> amortizationInIndex = null,
			bool renormalizeAfterAmoritzation = false,
			Dictionary<int, double> stepWiseCompensationRate = null,
			Dictionary<string, double> optionToCall = null,
			Dictionary<string, double> optionToPut = null,
			Dictionary<string, double> optionToAssPut = null,
			double settlementCoupon = double.NaN,
			bool roundCleanPrice = false
			)
		{
			Id = id;
			StartDate = startDate;
			UnderlyingMaturityDate = maturityDate;
			Notional = notional;
			Currency = currency;
			Coupon = coupon;
			Calendar = calendar;
			PaymentFreq = paymentFreq;
			Stub = stub;
			AccrualDayCount = accrualDayCount;
			PaymentDayCount = paymentDayCount;
			AccrualBizDayRule = accrualBizDayRule;
			PaymentBizDayRule = paymentBizDayRule;
			SettlmentGap = settlementGap;
			_settlementCoupon = settlementCoupon;
			BondTradeingMarket = bondTradingMarket;
			FirstPaymentDate = firstPaymentDate;
			IsZeroCouponBond = isZeroCouponBond;
			IssuePrice = issuePrice;
			IssueRate = issueRate;
			StickToEom = stickToEom;
			StepWiseCompensationRate = stepWiseCompensationRate;
			RoundCleanPrice = roundCleanPrice;

			OptionToCall = optionToCall;
			OptionToPut = optionToPut;
			OptionToAssPut = optionToAssPut;

            Tenor = string.Format("{0},{1}", (int)(UnderlyingMaturityDate - StartDate), "D");

            IrregularPayment = false;
			if (Coupon is CustomizedCoupon)
			{

			}
			else
			{
				List<Date> tmpDate;
				if (FirstPaymentDate == null)
				{
					var schedule = new Schedule(StartDate, UnderlyingMaturityDate, PaymentFreq.GetTerm(), Stub, Calendar,
						AccrualBizDayRule);
					tmpDate = schedule.ToList();
					_isRegualDate = schedule.IsRegular;
				}
				else
				{
					var schedule = new Schedule(FirstPaymentDate, UnderlyingMaturityDate, PaymentFreq.GetTerm(), Stub, Calendar, AccrualBizDayRule);
					var regAccruals = schedule.ToList();
					tmpDate = new List<Date> {StartDate};
					tmpDate.AddRange(regAccruals);
					IrregularPayment = false;
					_isRegualDate = new List<bool> { IrregularPayment };
					_isRegualDate.AddRange(schedule.IsRegular);
				}

				if (tmpDate.Count > 2)
				{
					if (PaymentBizDayRule.Adjust(calendar, tmpDate[tmpDate.Count - 2]).Equals(tmpDate.Last()))
					{
						tmpDate.RemoveAt(tmpDate.Count -2);
						_isRegualDate.RemoveAt(_isRegualDate.Count - 2);
					}
				}

				Accruals = new Schedule(tmpDate);

				if (FirstPaymentDate == null)
				{
					PaymentSchedule =
						new Schedule(
							new Schedule(StartDate, UnderlyingMaturityDate, PaymentFreq.GetTerm(), Stub, Calendar, PaymentBizDayRule).Skip(1));
				}
				else
				{
					PaymentSchedule =
						new Schedule(
							new Schedule(FirstPaymentDate, UnderlyingMaturityDate, PaymentFreq.GetTerm(), Stub, Calendar, PaymentBizDayRule));
				}
			}

			if (Accruals.Count() != PaymentSchedule.Count() + 1)
			{
				throw new PricingLibraryException("Bond's number of accrual periods do not match number of payments");
			}

			AmortizationType = amortionType;
			AmortizationInDates = amortizationInDates;
			AmortizationInIndex = amortizationInIndex;
			RenormalizeAfterAmoritzation = renormalizeAfterAmoritzation;
			IAmortization amortization;
			if (AmortizationInDates != null)
			{
				amortization = new Amortization(amortizationInDates, RenormalizeAfterAmoritzation);
			}
			else if (AmortizationInIndex != null)
			{
				amortization = new Amortization(ToAmortizationSchedule(PaymentSchedule.ToArray(), AmortizationInIndex), RenormalizeAfterAmoritzation);
			}
			else
			{
				//EqualPrincipal or EqualPrincipalAndInterest will be calculated later
				amortization = new Amortization();
			}
			Amoritzation = amortization;
			_mortgageCalculator = new MortgageCalculator(new Psa(0.0), new Sda(0.0));

            Redemption = redemption ?? new Redemption(1.0, RedemptionType.None);
            //Redemption = redemption ?? new Redemption(1.0, PriceQuoteType.Clean);

            if (PaymentFreq == Frequency.None)
			{
				IrregularPayment = true;
			}
			else
			{
				for (var i = 0; i < Accruals.Count() - 1; ++i)
				{
					if (PaymentFreq.GetTerm().Next(Accruals.ToArray()[i]) != Accruals.ToArray()[i + 1])
					{
						IrregularPayment = false;
						break;
					}
				}
			}

			_compensationRate = Accruals.Skip(1).Select(x => 0.0).ToArray();
			
			if (stepWiseCompensationRate != null)
			{
				var compensationCoupons = new List<double>();
				var arr = StepWiseCompensationRate.OrderBy(x => x.Key).Select(x => Tuple.Create(x.Key, x.Value)).ToArray();
				for (var i = 0; i < Accruals.Count() - 1; ++i)
				{
					compensationCoupons.Add(i > 0 ? compensationCoupons[i - 1] : 0.0);
					var updateCoupon = arr.FirstOrDefault(x => x.Item1 == (i + 1));
					var compensationCoupon = updateCoupon != null ? updateCoupon.Item2 : 0.0;
					compensationCoupons[i] += compensationCoupon;
				}
				_compensationRate = compensationCoupons.ToArray();
			}
		
		}

		private Dictionary<Date, double> ToAmortizationSchedule(Date[] payDates, Dictionary<int, double> amortizationInIndex)
		{
			var amortizationSum = amortizationInIndex.Where(x => x.Key > payDates.Length).Sum(x => x.Value);
			if (amortizationInIndex.ContainsKey(payDates.Length))
			{
				amortizationInIndex[payDates.Length] += amortizationSum;
			}
			else
			{
				amortizationInIndex.Add(payDates.Length, amortizationSum);
			}
			foreach (var removeIndex in amortizationInIndex.Where(x => x.Key > payDates.Length).ToDictionary(x => x.Key, y => y.Value).Keys)
			{
				amortizationInIndex.Remove(removeIndex);
			}
			return amortizationInIndex.ToDictionary(x => payDates[x.Key - 1], x => x.Value);
		} 

		public Cashflow[] GetAiCashflows(IMarketCondition market, bool netted = true)
		{
			return GetBondCashflows(market, netted, true);
		}

		public Cashflow[] GetCashflows(IMarketCondition market, bool netted = true)
		{
			return GetBondCashflows(market, netted, false);
		}

		private Cashflow[] GetBondCashflows(IMarketCondition market, bool netted = true, bool calcAi = false)
		{
			var accruals = Accruals.ToArray();
			var schedulePaymentDates = calcAi ? Accruals.Skip(1).ToArray() : PaymentSchedule.ToArray();
			if(schedulePaymentDates.Length == 0) throw  new PricingLibraryException("Number of payments is 0");

			List<CfCalculationDetail[]> cfCalcDetails;

			var cashflows = new List<Cashflow>();
			var prevCfDate = accruals[0];

            var coupons = Coupon.GetCoupon(
                Accruals,
                market.FixingCurve.HasValue ? market.FixingCurve.Value : null,
                market.HistoricalIndexRates.HasValue ? market.HistoricalIndexRates.Value : null,
                out cfCalcDetails,
                IssueRate,
                _compensationRate);
			var refStartDates = new List<Date>();
			var refEndDates = new List<Date>();
			for (var i = 0; i < coupons.Length; ++i)
			{
				var refStartDate = accruals[i];
				var refEndDate = accruals[i + 1];
				if (i == 0 && !_isRegualDate[i])
				{
					if (PaymentFreq != Frequency.None)
					{
						if (Stub == Stub.LongStart || Stub == Stub.ShortStart)
						{
							refStartDate = PaymentFreq.GetTerm().Prev(refEndDate);
						}
						else
						{
							refEndDate = PaymentFreq.GetTerm().Next(refStartDate);
						}
					}
				}
				refStartDates.Add(refStartDate);
				refEndDates.Add(refEndDate);
			}

			IAmortization amortization;
			if (AmortizationType == AmortizationType.EqualPrincipal || AmortizationType == AmortizationType.EqualPrincipalAndInterest)
			{
				//for these two types, amortization is calculated in cash flow calculation
				var tArr = coupons.Select((x, i) => AccrualDayCount.CalcDayCountFraction(accruals[i], accruals[i + 1], refStartDates[i], refEndDates[i])).ToArray();
				double[] begPrincipal;
				double[] interest;
				double[] principalPay;
				double[] prepayment;
				double[] defaultPrincipal;
				_mortgageCalculator.GetPaymentDetails(tArr, Notional, coupons, PaymentFreq, AmortizationType,  out begPrincipal, out interest, out principalPay, out prepayment, out defaultPrincipal, 0);
				amortization = new Amortization(ToAmortizationSchedule(schedulePaymentDates, principalPay.Select((x,i) => Tuple.Create(i+1,x)).ToDictionary(x => x.Item1, x => x.Item2)), false);
			}
			else
			{
				amortization = Amoritzation.Adjust(new Schedule(schedulePaymentDates), Calendar, calcAi ? AccrualBizDayRule : PaymentBizDayRule, PaymentFreq);
				amortization = amortization.ResetAmortization(market.ValuationDate);
			}

			for (var i = 0; i < coupons.Length; ++i)
			{
				var amortizationRedemption = 0.0;
				var remainPrincipal = 100.0;
				var couponPay = 0.0;
				var prevDate = prevCfDate;
				if (amortization.AmortizationSchedule != null && amortization.AmortizationSchedule.Any(x => x.Key > prevCfDate && x.Key < schedulePaymentDates[i]))
				{
					var intermediatePrePayDate = amortization.AmortizationSchedule.Where(x => x.Key > prevCfDate && x.Key < schedulePaymentDates[i]).Select(x => x.Key).OrderBy(x => x).ToArray();
					CfCalculationDetail[] tmpCfCalculationDetails;
					foreach (var date in intermediatePrePayDate)
					{
						amortizationRedemption = amortization.GetRemainingPrincipal(prevDate);
						remainPrincipal = Notional * amortizationRedemption;
						var periodCoupon = Coupon.GetCoupon(prevDate, date, market.FixingCurve.Value, market.HistoricalIndexRates, out tmpCfCalculationDetails, _compensationRate[i]);
						couponPay = remainPrincipal * periodCoupon * AccrualDayCount.CalcDayCountFraction(prevDate, date, refStartDates[i], refEndDates[i]);
						cashflows.Add(new Cashflow(prevDate, date, date, couponPay, Currency, CashflowType.Coupon, tmpCfCalculationDetails.Aggregate(true, (current, v) => current & v.IsFixed), market.GetDf(date), cfCalcDetails[i], refStartDates[i], refEndDates[i], remainPrincipal, periodCoupon));
						prevDate = date;
					}
					coupons[i] = Coupon.GetCoupon(prevDate, accruals[i + 1], market.FixingCurve.Value, market.HistoricalIndexRates, out tmpCfCalculationDetails, _compensationRate[i]);
					cfCalcDetails[i] = tmpCfCalculationDetails;
				}

				amortizationRedemption = amortization.GetRemainingPrincipal(prevDate);
				remainPrincipal = Notional * amortizationRedemption;
				couponPay = remainPrincipal * coupons[i] * AccrualDayCount.CalcDayCountFraction(prevDate, accruals[i + 1], refStartDates[i], refEndDates[i]);

				if (i == coupons.Length - 1)
				{
					amortizationRedemption = amortization.GetRemainingPrincipal(prevCfDate);
					remainPrincipal = Notional * amortizationRedemption;

					//final settlement might be adjusted, coupon [MaturityDate, settlementDate) is added
					if (SettlmentGap != null)
					{
						var settlementDate = SettlmentGap.Get(Calendar, UnderlyingMaturityDate);
						if (settlementDate > accruals[i + 1])
						{
							var tmpCoupon = double.IsNaN(_settlementCoupon) ? coupons[i] : _settlementCoupon;
							var additionalCoupon = remainPrincipal * tmpCoupon * AccrualDayCount.CalcDayCountFraction(UnderlyingMaturityDate, settlementDate, refStartDates[i], refEndDates[i]);
							couponPay += additionalCoupon;
						}
					}

					couponPay = Redemption.GetRedemptionPayment(couponPay, remainPrincipal) - remainPrincipal;
				}

				cashflows.Add(new Cashflow(prevDate, accruals[i + 1], schedulePaymentDates[i], couponPay, Currency, CashflowType.Coupon, cfCalcDetails[i] == null ? true : cfCalcDetails[i].Aggregate(true, (current, v) => current & v.IsFixed), market.GetDf(schedulePaymentDates[i]), cfCalcDetails[i], refStartDates[i], refEndDates[i], remainPrincipal, coupons[i]));
				prevCfDate = accruals[i + 1];
			}

			cashflows.AddRange(amortization.AmortizationSchedule.Keys.Select(key => new Cashflow(key, key, key, amortization.AmortizationSchedule[key] * Notional, Currency, CashflowType.Principal, true, market.GetDf(key), null)));

			if (netted)
			{
				return cashflows
					.GroupBy(cf => cf.PaymentDate)
					.Select(item => new Cashflow(item.Min(x => x.AccrualStartDate), item.Max(x => x.AccrualEndDate), item.Key, item.Sum(entry => entry.PaymentAmount), Currency, CashflowType.Coupon, item.Aggregate(true, (current, v) => current && v.IsFixed), market.GetDf(item.Key), item.Min(x => x.CalculationDetails), item.Min(x => x.RefStartDate), item.Max(x => x.RefEndDate), item.Max(entry => entry.StartPrincipal), item.Sum(entry => entry.CouponRate)))
					.OrderBy(cf => cf.PaymentDate)
					.ToArray();
			}
			else
			{
				return cashflows.ToArray();	
			}
		}

		public double GetAccruedInterest(Date calcDate, IMarketCondition market, bool isEod = false)
		{
			var cashflows = GetAiCashflows(market, false);
			return GetAccruedInterest(calcDate, cashflows, isEod);
		}

		public double GetAccruedInterest(Date calcDate, Cashflow[] cashflows, bool isEod = false)
		{
			if (calcDate < StartDate || calcDate >= UnderlyingMaturityDate)
			{
				return 0.0;
			}
			if (IsZeroCouponBond)
			{
				if (double.IsNaN(IssuePrice))
				{
					throw new PricingLibraryException("Issue price is missing in calculating ai of zero coupon bond");
				}
				var totalInterest = Notional - IssuePrice;
				return (PaymentDayCount.DaysInPeriod(StartDate, calcDate) + (isEod ? 1 : 0)) / PaymentDayCount.DaysInPeriod(StartDate, UnderlyingMaturityDate) * totalInterest;
			}
			return AiCalculation.GetAccruedInterest(calcDate, cashflows, AccrualDayCount, isEod);
		}

		public int GetAccruedInterestDays(Date calcDate, IMarketCondition market, bool isEod = false)
		{
			if (calcDate < StartDate || calcDate >= UnderlyingMaturityDate)
			{
				return 0;
			}
			var cashflows = GetAiCashflows(market, false);
			return AiCalculation.GetAccruedInterestDays(calcDate, cashflows, isEod);
		}

		public int GetAccruedInterestDays(Date calcDate, Cashflow[] cashflows, bool isEod = false)
		{
			if (calcDate < StartDate || calcDate >= UnderlyingMaturityDate)
			{
				return 0;
			}
			return AiCalculation.GetAccruedInterestDays(calcDate, cashflows, isEod);
		}

		public bool IrregularPayment {get; set; }
        
		public double GetSpotPrice(IMarketCondition market)
		{
			return market.MktQuote.Value[Id].Item2;
		}

		public string Tenor { get; private set; }
		public Date GetCalibrationDate()
		{
			return PaymentBizDayRule.Adjust(Calendar, UnderlyingMaturityDate);
		}

		public ICalibrationSupportedInstrument Bump(int bp)
		{
			throw new NotImplementedException();
		}

		public ICalibrationSupportedInstrument Bump(double resetRate)
		{
			throw new NotImplementedException();
		}

		public double ModelValue(IMarketCondition market, MktInstrumentCalibMethod calibMethod = MktInstrumentCalibMethod.Default)
		{
			var engine = new BondEngine();
			return engine.CalcPv(this, market);
		}
	}

}
