using System;
using System.Linq.Expressions;

namespace Qdp.Pricing.Library.Common.Interfaces
{
	public interface IUpdateMktConditionPack
	{
		Expression<Func<IMarketCondition, object>> ConditionExpression { get; }
		object NewCondition { get; }
	}
}
