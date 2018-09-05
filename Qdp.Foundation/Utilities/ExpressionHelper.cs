using System;
using System.Linq.Expressions;

namespace Qdp.Foundation.Utilities
{
	public static class ExpressionHelper
	{
		public static string GetMemberName<T>(this T instance, Expression<Func<T, object>> expression)
		{
			return GetMemberName(expression);
		}

		public static string GetMemberName<T>(Expression<Func<T, object>> expression)
		{
			if (expression == null)
			{
				throw new Exception("Cannot get member name since expression is null");
			}
			return GetMemberName(expression.Body);
		}

		public static string GetMemberName<T>(this T instance, Expression<Action<T>> expression)
		{
			return GetMemberName(expression);
		}

		public static string GetMemberName<T>(Expression<Action<T>> expression)
		{
			if (expression == null)
			{
				throw new Exception("Cannot get member name since expression is null");
			}
			return GetMemberName(expression.Body);
		}

		private static string GetMemberName(Expression expression)
		{
			if (expression == null)
			{
				throw new Exception("Cannot get member name since expression is null");
			}

			var memberExpression = expression as MemberExpression;
			if (memberExpression != null)
			{
				return memberExpression.Member.Name;
			}

			var methodCallExpression = expression as MethodCallExpression;
			if (methodCallExpression != null)
			{
				return methodCallExpression.Method.Name;
			}


			var unaryExpression = expression as UnaryExpression;
			if (unaryExpression != null)
			{
				return GetMemberName(unaryExpression);
			}

			throw new Exception("Cannot get member name since expression is invalid");
		}

		private static string GetMemberName(UnaryExpression expression)
		{
			var methodCallExpression = expression.Operand as MethodCallExpression;
			return methodCallExpression != null
				? methodCallExpression.Method.Name
				: ((MemberExpression) expression.Operand).Member.Name;
		}
	}
}
