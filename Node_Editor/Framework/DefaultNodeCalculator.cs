using System;
using NodeEditorFramework;
using UnityEngine;
using System.Collections.Generic;

namespace NodeEditorFramework
{
	public class DefaultNodeCalculator : INodeCalculator
	{
		// A list of Nodes from which calculation originates -> Call StartCalculation
		public List<Node> workList;
		private int calculationCount;

		/// <summary>
		/// Recalculate from every Input Node.
		/// Usually does not need to be called at all, the smart calculation system is doing the job just fine
		/// </summary>
		public void RecalculateAll (NodeCanvas nodeCanvas) 
		{
			workList = new List<Node> ();
			foreach (Node node in nodeCanvas.nodes) 
			{
				if (node.isInput ())
				{ // Add all Inputs
					node.ClearCalculation ();
					workList.Add (node);
				}
			}
			StartCalculation ();
		}

		/// <summary>
		/// Recalculate from this node. 
		/// Usually does not need to be called manually
		/// </summary>
		public void RecalculateFrom (Node node) 
		{
			node.ClearCalculation ();
			workList = new List<Node> { node };
			StartCalculation ();
		}

		/// <summary>
		/// Iterates through workList and calculates everything, including children
		/// </summary>
		private void StartCalculation () 
		{
			if (workList == null || workList.Count == 0)
				return;
			// this blocks iterates through the worklist and starts calculating
			// if a node returns false, it stops and adds the node to the worklist
			// this workList is worked on until it's empty or a limit is reached
			calculationCount = 0;
			bool limitReached = false;
			for (int roundCnt = 0; !limitReached; roundCnt++)
			{ // Runs until every node possible is calculated
				limitReached = true;
				for (int workCnt = 0; workCnt < workList.Count; workCnt++)
				{
					if (ContinueCalculation (workList[workCnt]))
						limitReached = false;
				}
				if (roundCnt > 1000)
					limitReached = true;
			}
		}

		/// <summary>
		/// Recursive function which continues calculation on this node and all the child nodes
		/// Usually does not need to be called manually
		/// Returns success/failure of this node only
		/// </summary>
		private bool ContinueCalculation (Node node) 
		{
			if (node.calculated)
				return false;
			if ((node.descendantsCalculated () || node.isInLoop ()) && node.Calculate ())
			{ // finished Calculating, continue with the children
				node.calculated = true;
				calculationCount++;
				workList.Remove (node);
				if (node.ContinueCalculation && calculationCount < 1000) 
				{
					foreach (NodeOutput output in node.Outputs)
					{
						foreach (NodeInput connection in output.connections)
							ContinueCalculation (connection.body);
					}
				}
				else if (calculationCount >= 1000)
					Debug.LogError ("Stopped calculation because of suspected Recursion. Maximum calculation iteration is currently at 1000!");
				return true;
			}
			else if (!workList.Contains (node)) 
			{ // failed to calculate, add it to check later
				workList.Add (node);
			}
			return false;
		}
	}
}

