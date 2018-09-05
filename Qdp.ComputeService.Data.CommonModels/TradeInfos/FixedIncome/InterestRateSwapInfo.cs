using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Qdp.ComputeService.Data.CommonModels.ValuationParams;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome;
using Qdp.Foundation.TableWithHeader;
using Qdp.Pricing.Base.Interfaces;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.Utilities;
using Qdp.Pricing.Base.Utilities;
namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome
{
	[DataContract]
	[Serializable]
	public class InterestRateSwapInfo : TradeInfoBase
	{
		public InterestRateSwapInfo(string tradeId)
			: base(tradeId, "InterestRateSwap")
		{
		}

		public InterestRateSwapInfo()
			: base(Guid.NewGuid().ToString(), "InterestRateSwap")
		{
		}

		[DataMember]
		[Column(2)]
		public string StartDate { get; set; }

		[DataMember]
		[Column(3)]
		public string MaturityDate { get; set; }

		[DataMember]
		[Column(4)]
		public string Tenor { get; set; }

		[DataMember, DefaultValue(10000000.0)]
		[Column(5)]
		public double Notional { get; set; }

		[DataMember]
		[Column(6)]
		public string Currency { get; set; }

		[DataMember]
		[Column(7)]
		public string SwapDirection { get; set; }

		[DataMember]
		[Column(8)]
		public string Calendar { get; set; }

		[DataMember]
		[Column(9)]
		public string FixedLegDC { get; set; }

		[DataMember]
		[Column(10)]
		public string FixedLegFreq { get; set; }

		[DataMember]
		[Column(11)]
		public string FixedLegBD { get; set; }

		[DataMember]
		[Column(12)]
		public string FixedLegStub { get; set; }

		[DataMember]
		[Column(13)]
		public double FixedLegCoupon { get; set; }

		[DataMember]
		[Column(14)]
		public string FloatingLegDC { get; set; }

		[DataMember]
		[Column(15)]
		public string FloatingLegFreq { get; set; }

		[DataMember]
		[Column(16)]
		public string FloatingLegBD { get; set; }

		[DataMember]
		[Column(17)]
		public string FloatingLegStub { get; set; }

		[DataMember]
		[Column(18)]
		public string Index { get; set; }

		[DataMember, DefaultValue(null)]
		[Column(19)]
		public string ResetTerm { get; set; }

		[DataMember]
		[Column(20)]
		public string ResetStub { get; set; }

		[DataMember]
		[Column(21)]
		public string ResetBD { get; set; }

		[DataMember]
		[Column(22)]
		public string ResetToFixingGap { get; set; }

		[DataMember]
		[Column(23)]
		public string ResetCompound { get; set; }

		[DataMember]
		public SimpleCfValuationParameters ValuationParamters { get; set; }
		public override ValuationParameters GetValuationParamters()
		{
			return ValuationParamters;
		}


        #region Upgrade
    
        protected override IDayCount Day_Count
        {
            get
            {
                // todo ToDayCountImpl 这个类应该包含 Instrument,DayCount 
                //string.IsNullOrEmpty(TradeInfo.DayCount) ? null : TradeInfo.DayCount.ToDayCount().Get(),
                // Instument.Daycount; 
                return FixedLegDC.ToDayCountImpl();
            }
        }

        protected override string Maturity_Date
        {
            get
            {
                return MaturityDate;
            }
        }
        #endregion
    }
}