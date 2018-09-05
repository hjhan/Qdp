using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Qdp.ComputeService.Data.CommonModels.TradeInfos;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.Foundation.TableWithHeader;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;

namespace Qdp.Pricing.Ecosystem.ExcelWrapper
{
	public static class XlUtilities
	{

		/// <summary>
		/// Convert trade info into a two column array of {label, value} paris.
		/// </summary>
		/// <param name="tradeInfo">TradeInfo.</param>
		/// <param name="outputLabels">Output labels. If left null or empty array, all labels will be returned.</param>
		/// <returns></returns>
		public static object ToTradeInfoInLabelData(this TradeInfoBase tradeInfo, string[] outputLabels)
		{
			return GetObjProperties(tradeInfo, outputLabels);
		}

		/// <summary>
		/// Convert tow column array {label, value} paris to TradeInfo.
		/// </summary>
		/// <param name="labelValue">Label value pairs.</param>
		/// <returns>Return a QDP trade info object given two column array of {label, value} pairs.</returns>
		public static TradeInfoBase ToTradeInfoBase(this object[,] labelValue)
		{
			var row = labelValue.GetLength(0);
			var col = labelValue.GetLength(1);

			var deliverableBondIds = new List<string>();
			var instrumentType = InstrumentType.None;
			for (var i = 0; i < row; ++i)
			{
				if ((string) labelValue[i, 0] == "InstrumentType")
				{
					instrumentType = ((string) labelValue[i, 1]).ToInstrumentType();
				}
				//if ((string) labelValue[i, 0] == "DeliverableBondInfos")
				//{
				//	deliverableBondIds.AddRange(labelValue[i, 1] != null ? ((string)labelValue[i, 1]).Split(',').ToList() : null);	
				//}
			}

			if (instrumentType == InstrumentType.BondFutures)
			{
				var tf = labelValue.TransposeRowsAndColumns().ToArrayObj<BondFuturesInfo>().Single();
				tf.DeliverableBondInfos = deliverableBondIds.Select(x => (FixedRateBondInfo) XlManager.GetTrade(x)).ToArray();
				return tf;
			}
			if (instrumentType == InstrumentType.InterestRateSwap)
			{
				return labelValue.TransposeRowsAndColumns().ToArrayObj<InterestRateSwapInfo>().Single();
			}
			if (instrumentType == InstrumentType.FixedRateBond)
			{
				return labelValue.TransposeRowsAndColumns().ToArrayObj<FixedRateBondInfo>().Single();
			}
			if (instrumentType == InstrumentType.FloatingRateBond)
			{
				return labelValue.TransposeRowsAndColumns().ToArrayObj<FloatingRateBondInfo>().Single();
			}
			if (instrumentType == InstrumentType.FixedDateCouonAdjustedBond)
			{
				return labelValue.TransposeRowsAndColumns().ToArrayObj<FixedDateCouonAdjustedBondInfo>().Single();
			}
			return null;
		}

		public static object GetObjProperties(object obj, string[] labels)
		{
			var type = obj.GetType();
			var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(x => x.Name, x => x);
			
			var methodInfos = type.GetMethods();
			if (labels == null || !labels.Any())
			{
				labels = props.ToDictionary(x=>x.Key, y=>{var column = y.Value.GetCustomAttributes(false).FirstOrDefault(c=>c is ColumnAttribute) as ColumnAttribute;
				                                                 return column == null ? 10000 : column.Column;
															}).OrderBy(o=>o.Value).ToDictionary(x=>x.Key,x=>x.Value).Keys.ToArray();
			}

			var ret = new object[labels.Length, 2];
			for (var i = 0; i < labels.Length; ++i)
			{
				ret[i, 0] = labels[i];
                if (props.ContainsKey(labels[i]))
                {
                    var prop = props[labels[i]];
                    var converterName = string.Format("{0}ToLabelData", prop.Name);
                    var converter = methodInfos.FirstOrDefault(x => x.Name.Equals(converterName) && x.GetParameters().Length == 0);
                    if (converter != null)
                    {
                        ret[i, 1] = converter.Invoke(obj, null);
                    }
                    else
                    {
                        ret[i, 1] = prop.GetValue(obj, null) ?? string.Empty;
                    }
                }
                else
                {
                    ret[i, 1] = string.Empty;
                }
			}

			return ret;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="marketInfo"></param>
		/// <param name="outputLabels"></param>
		/// <returns></returns>
		public static object ToMarketInfoInLabelData(this XlMarket marketInfo, string[] outputLabels)
		{
			var ret = new Dictionary<string, object>();
			var allMarketArray = GetObjProperties(marketInfo.MarketInfo, outputLabels);
			ret[marketInfo.MarketName] = allMarketArray;
			
			foreach (var element in marketInfo.QdpMarket.GetAllData())
				ret.Add(element.Key, element.Value);
			return ret;
		}
	}
}
