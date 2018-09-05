using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;

namespace Qdp.Pricing.Base.Utilities
{
	public static class PricingResultExtension
	{
		public static void Aggregate(this IPricingResult input, IPricingResult result2)
		{
			var result = (input as PricingResult);
			if (result == null)
			{
				return;
			}
			result.Pv = Aggregate(result.Pv, result2.Pv);
			result.Dv01 = Aggregate(result.Dv01, result2.Dv01);
			result.Pv01 = Aggregate(result.Pv01, result2.Pv01);
			//result.Ai = result1.Ai  = Aggregate(result2. Ai);
			//result.AiEod = result1.AiEod  = Aggregate(result2. AiEod);
			//result.Ytm = result1.  = Aggregate(result2. );
			//result.DirtyPrice = result1.  = Aggregate(result2. );
			//result.CleanPrice = result1.  = Aggregate(result2. );
			result.Delta = Aggregate(result.Delta, result2.Delta);
            result.DeltaCash = Aggregate(result.DeltaCash, result2.DeltaCash);
            result.Gamma = Aggregate(result.Gamma, result2.Gamma);
            result.GammaCash = Aggregate(result.GammaCash, result2.GammaCash);
            result.Rho = Aggregate(result.Rho, result2.Rho);
			result.RhoForeign = Aggregate(result.RhoForeign, result2.RhoForeign);
			result.Vega = Aggregate(result.Vega, result2.Vega);
			result.Theta = Aggregate(result.Theta, result2.Theta);
			result.Dv01Underlying = Aggregate(result.Dv01Underlying, result2.Dv01Underlying);
			result.MacDuration = Aggregate(result.MacDuration, result2.MacDuration);
			result.ModifiedDuration = Aggregate(result.ModifiedDuration, result2.ModifiedDuration);
			result.Convexity = Aggregate(result.Convexity, result2.Convexity);
            result.Carry = Aggregate(result.Carry, result2.Carry);
            //result.Sp01 = Aggregate(result.Sp01, result2.Sp01);
            //result.ZeroSpread = result1.  = Aggregate(result2. );
            //result.FairQuote = result1.  = Aggregate(result2. );
            result.KeyRateDv01 = Aggregate(result.KeyRateDv01, result2.KeyRateDv01);
			result.Cashflows = Aggregate(result.Cashflows, result2.Cashflows);
			result.CashflowDict = Aggregate(result.CashflowDict, result2.CashflowDict);
			//result.ComponentPvs = null;
			//result.ProductSpecific = new Dictionary<string, Dictionary<string, RateRecord>>();
			result.Succeeded = true;
			if (string.IsNullOrEmpty(result.ErrorMessage))
			{
				result.ErrorMessage = result2.ErrorMessage;
			}
			else
			{
				result.ErrorMessage += QdpConsts.MsgDelimiter + result2.ErrorMessage;
			}
		}

		public static double Aggregate(double d1, double d2)
		{
			if (double.IsNaN(d1)) return d2;
			if (double.IsNaN(d2)) return d1;
			return d1 + d2;
		}

		public static Dictionary<string, CurveRisk[]> Aggregate(Dictionary<string, CurveRisk[]> cf1Dict, Dictionary<string, CurveRisk[]> cf2Dict)
		{
			if (cf1Dict == null)
			{
				cf1Dict = new Dictionary<string, CurveRisk[]>();
			}

			foreach (var curveName in cf2Dict.Keys)
			{
				if (cf1Dict.ContainsKey(curveName))
				{
					var cr = new List<CurveRisk>();
					for (var i = 0; i < cf1Dict[curveName].Length; ++i)
					{
						cf1Dict[curveName][i].Risk += cf2Dict[curveName][i].Risk;
					}
				}
				else
				{
					cf1Dict[curveName] = cf2Dict[curveName];
				}
			}

			return cf1Dict;
		}

		public static Dictionary<string, double> Aggregate(Dictionary<string, double> cf1Dict, Dictionary<string, double> cf2Dict)
		{
			if (cf1Dict == null)
			{
				cf1Dict = new Dictionary<string, double>();
			}
			//var dict = new Dictionary<CashflowKey, double>(cf1Dict);
			foreach (var key in cf2Dict.Keys)
			{
				if (cf1Dict.ContainsKey(key))
				{
					cf1Dict[key] = cf1Dict[key] + cf2Dict[key];
				}
				else
				{
					cf1Dict[key] = cf2Dict[key];
				}
			}
			return cf1Dict;
		}

		public static Dictionary<string, Cashflow> Aggregate(Dictionary<string, Cashflow> cf1Dict, Dictionary<string, Cashflow> cf2Dict)
		{
			if (cf1Dict == null)
			{
				cf1Dict = new Dictionary<string, Cashflow>();
			}
			//var dict = new Dictionary<CashflowKey, double>(cf1Dict);
			foreach (var key in cf2Dict.Keys)
			{
				if (cf1Dict.ContainsKey(key))
				{
					var cf = cf1Dict[key];
					cf.PaymentAmount = cf1Dict[key].PaymentAmount + cf2Dict[key].PaymentAmount;
					cf1Dict[key] = cf;
				}
				else
				{
					cf1Dict[key] = cf2Dict[key];
				}
			}
			return cf1Dict;
		}

		public static Cashflow[] Aggregate(Cashflow[] cf1, Cashflow[] cf2)
		{
			if (cf1 == null) return cf2;
			if (cf2 == null) return cf1;

			var cf1Dict = cf1.ToDictionary(cf => cf.ToCfKey(), cf => cf);
			var cf2Dict = cf2.ToDictionary(cf => cf.ToCfKey(), cf => cf);

			foreach (var key in cf2Dict.Keys)
			{
				if (cf1Dict.ContainsKey(key))
				{
					var cf = new Cashflow(cf1Dict[key]) {PaymentAmount = cf1Dict[key].PaymentAmount + cf2Dict[key].PaymentAmount};
					cf1Dict[key] = cf;
				}
				else
				{
					cf1Dict[key] = cf2Dict[key];
				}
			}

			return  cf1Dict.Select(x => x.Value).ToArray();
		}
		public static object[,] ToLableData<T>(this T[] datas, List<string> removePropertyNames, int col)
		{
			if (datas != null)
			{
				var ret = new object[datas.Length + 1, col];
				var lableData = typeof (T).GetProperties().Where(x => !removePropertyNames.Contains(x.Name)).ToArray();
				int i = 0;
				foreach (var propertyName in lableData)
				{
					ret[0, i++] = propertyName.Name;
				}
				for (int j = 0; j < datas.Length; j++)
				{
					var array = lableData.ToLabelData(datas[j]);
					i = 0;
					foreach (var str in array)
					{
						ret[j + 1, i++] = str;
					}
				}
				return ret;
			}
			else
			{
				return null;
			}
		}
		public static object[] ToLabelData(this PropertyInfo[] labels, object obj)
		{
			var ret = new object[labels.Length];
			int i = 0;
			foreach (var label in labels)
			{
				var value = label.GetValue(obj, null);
				if (value is Date[])
				{
					ret[i++] = ((Date[])value).Aggregate("",(current, date)=>current.ToString() + "," + date.ToString()).Substring(1);
				}
				else if (value is string[])
				{
					ret[i++] = ((string[])value).Aggregate("", (current, date) => current + "," + date).Substring(1);
				}
				else
				{
					ret[i++] = value;
				}
			}
			return ret;
		}
	}
}
