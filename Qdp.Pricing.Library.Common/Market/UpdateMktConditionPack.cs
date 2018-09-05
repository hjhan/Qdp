using System;
using System.Linq.Expressions;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Market
{
	public class UpdateMktConditionPack<T> : IUpdateMktConditionPack
	{
		public Expression<Func<IMarketCondition, object>> ConditionExpression { get; private set; }
		public object NewCondition { get; private set; }

		public UpdateMktConditionPack(Expression<Func<IMarketCondition, object>> conditionExpression, T newCondition)
		{
			ConditionExpression = conditionExpression;
			NewCondition = newCondition;
		} 
	}
}
