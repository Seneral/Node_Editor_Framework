using UnityEngine;
using System.Collections.Generic;

namespace NodeEditorFramework.Standard
{
	public class CanvasCalculator : NodeCanvasTraversal
	{
		// A list of Nodes from which calculation originates -> Call StartCalculation
		public List<Node> workList;

		public CanvasCalculator (NodeCanvas canvas) : base(canvas) {}

		/// <summary>
		/// Recalculate from every node regarded as an input node
		/// </summary>
		public override void TraverseAll () 
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
		/// Recalculate from the specified node
		/// </summary>
		public override void OnChange (Node node) 
		{
			node.ClearCalculation ();
			workList = new List<Node> { node };
			StartCalculation ();
		}

		/// <summary>
		/// Iteratively calculates all nodes from the worklist, including child nodes, until no further calculation is possible
		/// </summary>
		private void StartCalculation () 
		{
			if (workList == null || workList.Count == 0)
				return;
			
			bool limitReached = false;
			while (!limitReached)
			{ // Runs until the whole workList is calculated thoroughly or no further calculation is possible
				limitReached = true;
				for (int workCnt = 0; workCnt < workList.Count; workCnt++)
				{ // Iteratively check workList
					if (ContinueCalculation (workList[workCnt]))
						limitReached = false;
				}
			}
			if (workList.Count > 0)
			{
				Debug.LogError("Did not complete calculation! " + workList.Count + " nodes block calculation from advancing!");
				foreach (Node node in workList)
					Debug.LogError("" + node.name + " blocks calculation!");
			}
		}

		/// <summary>
		/// Recursively calculates this node and it's children
		/// All nodes that could not be calculated in the current state are added to the workList for later calculation
		/// Returns whether calculation could advance at all
		/// </summary>
		private bool ContinueCalculation (Node node) 
		{
			if (node.calculated && !node.AllowRecursion)
			{ // Already calulated
				workList.Remove (node);
				return true;
			}
			if (node.descendantsCalculated () && node.Calculate ())
			{ // Calculation was successful
				node.calculated = true;
				workList.Remove (node);
				if (node.ContinueCalculation)
				{ // Continue with children
					foreach (ConnectionPort outputPort in node.outputPorts)
						foreach (ConnectionPort connectionPort in outputPort.connections)
							ContinueCalculation (connectionPort.body);
				}
				return true;
			}
			else if (!workList.Contains (node)) 
			{ // Calculation failed, record to calculate later on
				workList.Add (node);
			}
			return false;
		}
	}
}

