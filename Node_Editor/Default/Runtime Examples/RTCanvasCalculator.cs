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
			canvas.Validate ();
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
			{ // Display value of all output nodes
				string outValueLog = "(OUT) " + outputNode.name + ": ";
				// Use knob values - either output knobs, or input knobs if there are now output knobs
				List<ConnectionKnob> sourceValueKnobs = outputNode.outputKnobs.Count == 0? outputNode.inputKnobs : outputNode.outputKnobs;
				foreach (ValueConnectionKnob knob in sourceValueKnobs.OfType<ValueConnectionKnob> ())
					outValueLog += knob.styleID + " " + knob.name + " = " + (knob.IsValueNull? "NULL" : knob.GetValue ().ToString ()) + "; ";
				Debug.Log (outValueLog);
			}
		}

		/// <summary>
		/// Gets all nodes that either have no inputs or no input connections assigned
		/// </summary>
		public List<Node> getInputNodes () 
		{
			AssureCanvas ();
			return canvas.nodes.Where ((Node node) => node.isInput ()).ToList ();
		}

		/// <summary>
		/// Gets all nodes that either have no output or no output connections leading to a followup node
		/// </summary>
		public List<Node> getOutputNodes () 
		{
			AssureCanvas ();
			return canvas.nodes.Where ((Node node) => node.isOutput ()).ToList ();
		}
	}
}