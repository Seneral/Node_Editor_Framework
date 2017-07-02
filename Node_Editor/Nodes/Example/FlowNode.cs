using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework.Standard
{
	[Node (false, "Example/Flow Node")]
	public class FlowNode : Node 
	{
		public const string ID = "flowNode";
		public override string GetID { get { return ID; } }

		public override string Title { get { return "Flow Node"; } }
		public override Vector2 DefaultSize { get { return new Vector2 (200, 180); } }

		[ConnectionKnob("Flow In", Direction.In, "Flow", NodeSide.Left, 10)]
		public ConnectionKnob flowIn;
		[ConnectionKnob("Flow Out", Direction.Out, "Flow", NodeSide.Right, 10)]
		public ConnectionKnob flowOut;

		[ValueConnectionKnob("Input", Direction.In, "Float")]
		public ValueConnectionKnob inputKnob;
		[ValueConnectionKnob("Output", Direction.Out, "Float")]
		public ValueConnectionKnob outputKnob;

		public override void NodeGUI () 
		{
			// Display Float connections
			GUILayout.BeginHorizontal ();
			inputKnob.DisplayLayout ();
			outputKnob.DisplayLayout ();
			GUILayout.EndHorizontal ();

			// Get adjacent flow elements
			Node flowSource = flowIn.connected ()? flowIn.connections[0].body : null;
			List<Node> flowTargets = flowOut.connections.Select ((ConnectionKnob input) => input.body).ToList ();

			// Display adjacent flow elements
			GUILayout.Label ("Flow Source: " + (flowSource != null? flowSource.name : "null"));
			GUILayout.Label ("Flow Targets:");
			foreach (Node flowTarget in flowTargets)
				GUILayout.Label ("-> " + flowTarget.name);
		}
		
		public override bool Calculate () 
		{
			outputKnob.SetValue<float> (inputKnob.GetValue<float> () * 5);
			return true;
		}
	}

	// Flow connection visual style
	public class FlowConnection : ConnectionKnobStyle
	{
		public override string Identifier { get { return "Flow"; } }
		public override Color Color { get { return Color.red; } }
	}
}