using System;
using System.Runtime.Serialization;

namespace Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions
{
	[DataContract]
	[Serializable]
	public class CurveConvention : MarketDataDefinition
	{
		public CurveConvention(string curveConventionName,
			string currency,
			string businessDayConvention,
			string calendar,
			string dayCount,
			string compound,
			string interpolation)
			: base(curveConventionName)
		{
			Currency = currency;
			BusinessDayConvention = businessDayConvention;
			Calendar = calendar;
			DayCount = dayCount;
			Compound = compound;
			Interpolation = interpolation;
		}

		public CurveConvention()
		{
		}

		[DataMember]
		public string Currency { get; protected set; }

		[DataMember]
		public string BusinessDayConvention { get; protected set; }

		[DataMember]
		public string Calendar { get; protected set; }

		[DataMember]
		public string DayCount { get; protected set; }

		[DataMember]
		public string Compound { get; protected set; }

		[DataMember]
		public string Interpolation { get; protected set; }

		public object[,] ToLabelObjects()
		{
			var ret = new object[7, 2];
			ret[0, 0] = "Name";
			ret[0, 1] = Name;
			ret[1, 0] = "Currency";
			ret[1, 1] = Currency;
			ret[2, 0] = "BusinessDayConvention";
			ret[2, 1] = BusinessDayConvention;
			ret[3, 0] = "Calendar";
			ret[3, 1] = Calendar;
			ret[4, 0] = "DayCount";
			ret[4, 1] = DayCount;
			ret[5, 0] = "Compound";
			ret[5, 1] = Compound;
			ret[6, 0] = "Interpolation";
			ret[6, 1] = Interpolation;
			return ret;
		}
	}
}