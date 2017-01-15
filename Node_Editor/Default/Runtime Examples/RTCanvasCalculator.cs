using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using NodeEditorFramework;

namespace NodeEditorFramework.Standard
{
	/// <summary>
	/// Example of accessing and using the canvas at runtime 
	/// </summary>
	public class RTCanvasCalculator : MonoBehaviour 
	{
		public NodeCanvas canvas;

		/// <summary>
		/// Assures the canvas is loaded
		/// </summary>
		public void AssureCanvas () 
		{
			if (canvas == null)
				throw new UnityException ("No canvas specified to calculate on " + name + "!");
		}

		/// <summary>
		/// Calculates the currently loaded canvas and debugs the various outputs
		/// </summary>
		public void CalculateCanvas () 
		{
			AssureCanvas ();
			NodeEditor.checkInit (false);
			canvas.Validate (true);
			canvas.TraverseAll ();
			DebugOutputResults ();
		}

		/// <summary>
		/// Debugs the values of all possible output nodes
		/// Could be done more precisely but it atleast shows how to get them
		/// </summary>
		private void DebugOutputResults () 
		{
			AssureCanvas ();
			Debug.Log ("Calculating '" + canvas.saveName + "':");
			List<Node> outputNodes = getOutputNodes ();
			foreach (Node outputNode in outputNodes) 
			{
				string outID = "(OUT) " + outputNode.name + ": ";
				if (outputNode.Outputs.Count == 0)
				{ // If the node has no outputs, display it's inputs, because that's what the output node works with
					foreach (NodeInput input in outputNode.Inputs)
						outID += input.typeID + " " + (input.IsValueNull? "NULL" : input.GetValue ().ToString ()) + "; ";
				}
				else
				{ // Else display the final output of the output node
					foreach (NodeOutput output in outputNode.Outputs)
						outID += output.typeID + " " + (output.IsValueNull? "NULL" : output.GetValue ().ToString ()) + "; ";
				}
				Debug.Log (outID);
			}
		}

		/// <summary>
		/// Gets all nodes that either have no inputs or no input connections assigned
		/// </summary>
		public List<Node> getInputNodes () 
		{
			AssureCanvas ();
			return canvas.nodes.Where ((Node node) => (node.Inputs.Count == 0 && node.Outputs.Count != 0) || node.Inputs.TrueForAll ((NodeInput input) => input.connection == null)).ToList ();
		}

		/// <summary>
		/// Gets all nodes that either have no output or no output connections leading to a followup node
		/// </summary>
		public List<Node> getOutputNodes () 
		{
			AssureCanvas ();
			return canvas.nodes.Where ((Node node) => (node.Outputs.Count == 0 && node.Inputs.Count != 0) || node.Outputs.TrueForAll ((NodeOutput output) => output.connections.Count == 0)).ToList ();
		}
	}
}