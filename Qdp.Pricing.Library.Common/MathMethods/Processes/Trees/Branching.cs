using System;
using System.Collections.Generic;

namespace Qdp.Pricing.Library.Common.MathMethods.Processes.Trees
{
	/// <summary>
	/// Class defines the branching for a Trinomial tree.
	/// </summary>
	public class Branching
	{
		/// <summary>
		/// The minimum index of node for next time step.
		/// </summary>
		public int JMin { get; private set; }

		/// <summary>
		/// The maximum index of node for next time step.
		/// </summary>
		public int JMax { get; private set; }

		/// <summary>
		/// Array of index which is the index of the central node of [i,j]'s descendants.
		/// Take node [i,j] for example, its descendants have values  K[j]+1, K[j], K[j]-1
		/// </summary>
		public List<int> K { get; private set; }

		/// <summary>
		/// The minimum value of K.
		/// </summary>
		public int KMin { get; private set; }

		/// <summary>
		/// The maximum value of K.
		/// </summary>
		public int KMax { get; private set; }

		/// <summary>
		/// The number of nodes at this time step.
		/// </summary>
		public int Size { get; private set; }

		/// <summary>
		/// The probabilities of Up, Middle, Down branching.
		/// Probability[0]: array of up branching probability for all nodes.
		/// Probability[1]: array of middle branching probability for all nodes.
		/// Probability[2]: array of down branching probability for all nodes.
		/// 
		/// </summary>
		public List<double>[] Probability { get; private set; }

		public Branching()
		{
			Probability = new List<double>[3];
			for (var i = 0; i < Probability.Length; ++i)
			{
				Probability[i] = new List<double>();
			}
			K = new List<int>();
			KMin = int.MaxValue;
			KMax = int.MinValue;
			JMin = int.MaxValue;
			JMax = int.MinValue;
			Size = 0;
		}

		/// <summary>
		/// Adds a node.
		/// </summary>
		/// <param name="k">The index of the node to add.</param>
		/// <param name="pUp">The up branching probability.</param>
		/// <param name="pMiddle">The middle branching probability.</param>
		/// <param name="pDown">The down branching probability.</param>
		public void Add(int k, double pUp, double pMiddle, double pDown)
		{
			K.Add(k);
			Probability[0].Add(pUp);
			Probability[1].Add(pMiddle);
			Probability[2].Add(pDown);
			KMin = Math.Min(KMin, k);
			JMin = KMin - 1;
			KMax = Math.Max(KMax, k);
			JMax = KMax + 1;
			Size = K.Count;
		}
	}
}