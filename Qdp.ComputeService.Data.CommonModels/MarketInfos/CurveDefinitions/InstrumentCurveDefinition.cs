using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Qdp.Pricing.Base.Utilities;

namespace Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions
{
	[DataContract]
	[Serializable]
	public class InstrumentCurveDefinition : MarketDataDefinition
	{
		public InstrumentCurveDefinition(string curveName,
			CurveConvention curveConvention,
			RateMktData[] rateDefinitions,
			string trait,
			InstrumentCurveDefinition baseCurveDefinition = null,
			string[] regriddedTenors = null)
			: base(curveName)
		{
			RateDefinitions = rateDefinitions;
			CurveConvention = curveConvention;
			Trait = trait;
			BaseCurveDefinition = baseCurveDefinition;
			RegriddedTenors = regriddedTenors;
		}

		public InstrumentCurveDefinition()
		{
		}

		[DataMember]
		public RateMktData[] RateDefinitions { get; set; }

		[DataMember]
		public InstrumentCurveDefinition BaseCurveDefinition { get; set; }

		[DataMember]
		public string Trait { get; set; }

		[DataMember]
		public CurveConvention CurveConvention { get; set; }

		[DataMember]
		public string[] RegriddedTenors { get; set; }

		public override MarketDataDefinition[] GetDependencies()
		{
			if (BaseCurveDefinition == null)
			{
				return RateDefinitions.Cast<MarketDataDefinition>().ToArray()
					.Union(new[] {CurveConvention})
					.ToArray();
			}
			return new MarketDataDefinition[]
			{
				BaseCurveDefinition
			}
				.Union(RateDefinitions.Cast<MarketDataDefinition>().ToArray())
				.Union(new[] {CurveConvention})
				.ToArray();
		}

		public override void MergeDependencies(MarketDataDefinition mergeData)
		{
			if (mergeData is RateMktData)
			{
				if (RateDefinitions.Count(x => x.Name.Equals(mergeData.Name)) > 0)
				{
					for (var i = 0; i < RateDefinitions.Length; i++)
					{
						if (RateDefinitions[i].Name.Equals(mergeData.Name))
						{
							RateDefinitions[i] = (RateMktData) mergeData;
						}
					}
				}
				else
				{
					var list = new List<RateMktData>();
					list.AddRange(RateDefinitions);
					list.Add((RateMktData) mergeData);
					RateDefinitions = list.ToArray();
				}
			}
			else if (mergeData is CurveConvention)
			{
				CurveConvention = (CurveConvention)mergeData;
			}
		}

		public override void RemoveDependencies(MarketDataDefinition mergeData)
		{
			if (mergeData is RateMktData)
			{
				RateMktData rateMkt = RateDefinitions.FirstOrDefault(x => x.Name.Equals(mergeData.Name));
				var list = new List<RateMktData>();
				list.AddRange(RateDefinitions);
				list.Remove(rateMkt);
				RateDefinitions = list.ToArray();
			}
		}
		public object[] ToLabelData(PropertyInfo[] labels)
		{
			return labels.ToLabelData(this);
		}
	}
}