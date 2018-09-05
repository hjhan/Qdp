using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.Interfaces;

namespace Qdp.Pricing.Library.Common.MathMethods.Processes.Trees
{
	/// <summary>
	/// Fits the TrinomialTree to the given yieldCurve,  Stage 2 Hull White tree construction follows Damino and Fabio edition 2, Page 78.
	/// </summary>
	public class NumericTree
	{
		/// <summary>
		/// The present value of a payoff = 1.00 if state (i,j) is reached, 0 otherwise
		/// </summary>
		public List<double>[] Q { get; private set; }

		/// <summary>
		/// The adjustment to interest rate at each time point on the Trinomial tree to match the specified yieldcurve.
		/// </summary>
		public double[] Adjustment { get; private set; }


		public TrinomialTree TrinomialTree { get; private set; }

		private readonly IOneFactorModel _model;
		private readonly IYieldCurve _yieldCurve;

		/// <summary>
		/// Initializes a new instance of the <see cref="NumericTree"/> class.
		/// </summary>
		/// <param name="model"></param>
		/// <param name="trinomialTree">The trinomial tree, <see cref="TrinomialTree"/>.</param>
		/// <param name="yieldCurve">The yield curve, <see cref="IYieldCurve"/>.</param>
		public NumericTree(IOneFactorModel model, TrinomialTree trinomialTree, IYieldCurve yieldCurve)
		{
			TrinomialTree = trinomialTree;
			_model = model;
			_yieldCurve = yieldCurve;

			Q = new List<double>[TrinomialTree.Size - 1];
			for (var i = 0; i < Q.Length; ++i)
			{
				Q[i] = new List<double>();
			}
			

			Adjustment = new double[TrinomialTree.Size - 1];
			for (var i = 0; i < TrinomialTree.Size - 1; ++i)
			{
				var discountBond = yieldCurve.GetDf(TrinomialTree.Times[i + 1]);
				
				if (i == 0)
				{
					Q[0].Add(1.0);
				}
				else
				{
					//initialize Q at i^{th} step to 0
					for (var j = 0; j < TrinomialTree.NumberOfNodes(i); ++j)
					{
						Q[i].Add(0.0);
					}

					//induct Q from all nodes of i-1^{th} step
					for (var j = 0; j < TrinomialTree.NumberOfNodes(i - 1); ++j)
					{
						foreach (var branching in TrinomialTree.AllBranchDirections)
						{
							Q[i][TrinomialTree.Descendant(i - 1, j, branching)] += Q[i - 1][j] * Discount(i - 1, j) * TrinomialTree.Probability(i - 1, j, branching);
						}
					}
				}

				var numOfNodes = TrinomialTree.NumberOfNodes(i);
				var dt = TrinomialTree.Dt(i);
				var x = TrinomialTree.StateValue(i, 0);
				var dx = trinomialTree.Dx[i];
				double value = 0.0;
				for (var ind = 0; ind < numOfNodes; ++ind)
				{
					value += Q[i][ind] * Math.Exp(-x * dt);
					x += dx;
				}
				Adjustment[i] = Math.Log(value / discountBond) / dt;
			}
		}

		public double DiscountBond(double t, double T, double rate)
		{
			return _model.DiscountBond(t, T, rate, _yieldCurve);
		}

		/// <summary>
		/// Discount factor at node [i,j].
		/// </summary>
		/// <param name="i">The i.</param>
		/// <param name="j">The j.</param>
		/// <returns></returns>
		public double Discount(int i, int j, double spread = 0.0)
		{
			return Math.Exp(- (ShortRate(i,j)+spread) * TrinomialTree.Dt(i));
		}

		public double ShortRate(int i, int j)
		{
			return Adjustment[i] + TrinomialTree.StateValue(i, j);
		}

		public double[][] ReverseInduction(List<Tuple<double, double>> cashflows, List<Tuple<double, INumericCondition>> conditions, int fromIndex = -1, double[] endStatePrice = null, bool withDiscount = true, double spread = 0.0)
		{
			if (fromIndex == -1)
			{
				fromIndex = Index(TrinomialTree.Times, cashflows.Last().Item1);
			}

			const int toIndex = 0;
			var callableBondPricesOnTree = new double[fromIndex - toIndex + 1][];

			//initialize the state at the final layer of the tree
			var size = TrinomialTree.NumberOfNodes(fromIndex);
			callableBondPricesOnTree[fromIndex] = new double[size];
			if (endStatePrice != null)
			{
				for (var j = 0; j < size; ++j)
				{
					callableBondPricesOnTree[fromIndex][j] = endStatePrice[j];
				}
			}

			var cashflowAtT = GetCashFlowAtT(TrinomialTree.Times[fromIndex], cashflows);
			for (var j = 0; j < size; ++j)
			{
				callableBondPricesOnTree[fromIndex][j] += cashflowAtT;
			}

			//reverse inductive each step from the end
			for (var i = fromIndex - 1; i >= toIndex; --i)
			{
				cashflowAtT = GetCashFlowAtT(TrinomialTree.Times[i], cashflows);

				size = TrinomialTree.NumberOfNodes(i);
				callableBondPricesOnTree[i] = new double[size];
				for (var j = 0; j < size; ++j)
				{
					callableBondPricesOnTree[i][j] = 0.0;
					var branching = TrinomialTree.AllBranchDirections;
					foreach (var x in branching)
					{
						callableBondPricesOnTree[i][j] += TrinomialTree.Probability(i, j, x) *
																							callableBondPricesOnTree[i + 1][TrinomialTree.Descendant(i, j, x)] *
																							(withDiscount ? Discount(i, j, spread) : 1.0);
					}
				}

				//call or put
				var callPut = ConditonsOnTime(conditions, TrinomialTree.Times[i]);
				if (callPut != null && callPut.Any())
				{
					foreach (var numericCondition in callPut)
					{
						for (var j = 0; j < size; ++j)
						{
							callableBondPricesOnTree[i][j] = numericCondition.Apply(callableBondPricesOnTree[i][j]);
						}
					}
				}
				//add coupon amount
				for (var j = 0; j < size; ++j)
				{
					callableBondPricesOnTree[i][j] += cashflowAtT;
				}
			}

			return callableBondPricesOnTree;
		}

		private double GetCashFlowAtT(double time, List<Tuple<double, double>> cashflows)
		{
			if (cashflows == null)
			{
				return 0.0;
			}
			else
			{
				var ind = Index(cashflows.Select(cf => cf.Item1).ToArray(), time);
				return ind == -1 ? 0.0 : cashflows[ind].Item2;

			}
		}

		private INumericCondition[] ConditonsOnTime(IEnumerable<Tuple<double, INumericCondition>> callPutSchedule, double t)
		{
			if (callPutSchedule == null) return null;
			return callPutSchedule
				.Where(x => CloseEnough(x.Item1, t))
				.Select(x => x.Item2)
				.ToArray();
		}

		public int IndexAtT(double t)
		{
			return Index(TrinomialTree.Times, t);
		}
		/// <summary>
		/// Find target time in a list of times without duplicate.
		/// </summary>
		/// <param name="times">The times.</param>
		/// <param name="t">The t.</param>
		/// <returns></returns>
		/// <exception cref="PricingLibraryException"></exception>
		private int Index(double[] times, double t)
		{
			int i;
			for (i = 0; i < times.Length; ++i)
			{
				// 1Day ~ 0.0027 year, 1e-6 is accurate enough
				if (CloseEnough(t, times[i]))
				{
					break;
				}
			}

			if (i == times.Length) return -1;
			return i;
		}

		private bool CloseEnough(double t1, double t2)
		{
			return Math.Abs(t1 - t2) < 1.0e-6;
		}
	}
}
