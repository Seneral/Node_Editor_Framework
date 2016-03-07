using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;

[Node (false, "Example/Flow Node")]
public class FlowNode : Node 
{
	public const string ID = "flowNode";
	public override string GetID { get { return ID; } }
	
	public override Node Create (Vector2 pos) 
	{
		FlowNode node = CreateInstance<FlowNode> ();
		
		node.rect = new Rect (pos.x, pos.y, 200, 180);
		node.name = "Flow Node";

		// Flow connections
		node.CreateInput ("Flow", "Flow", NodeSide.Left, 10);
		node.CreateOutput ("Flow", "Flow", NodeSide.Right, 10);

		// Some Connections
		node.CreateInput ("Value", "Float");
		node.CreateOutput ("Output val", "Float");

		return node;
	}
	
	protected internal override void NodeGUI () 
	{
		// Display Connections
		// Start counter at 1 to ignore flow connections
		for (int inCnt = 1; inCnt < Inputs.Count; inCnt++)
			Inputs[inCnt].DisplayLayout ();
		for (int outCnt = 1; outCnt < Outputs.Count; outCnt++)
			Outputs[outCnt].DisplayLayout ();

		// Get adjacent flow elements
		Node flowSource = Inputs[0].connection != null? Inputs[0].connection.body : null;
		List<Node> flowTargets = Outputs[0].connections.Select ((NodeInput input) => input.body).ToList ();

		// Display adjacent flow elements
		GUILayout.Label ("Flow Source: " + (flowSource != null? flowSource.name : "null"));
		GUILayout.Label ("Flow Targets:");
		foreach (Node flowTarget in flowTargets) 
		{
			GUILayout.Label ("-> " + flowTarget.name);
		}
	}
	
	public override bool Calculate () 
	{
		// The following can NOT be used anymore until I implement conenction blocking though as the flow connections never have a value
//		if (!allInputsReady ())
//			return false;

		// Do your calc stuff
		Outputs[1].SetValue<float> (Inputs[1].GetValue<float> () * 5);
		return true;
	}
}

// Connection Type only for visual purposes
public class FlowType : ITypeDeclaration 
{
	public string name { get { return "Flow"; } }
	public Color col { get { return Color.red; } }
	public string InputKnob_TexPath { get { return "Textures/In_Knob.png"; } }
	public string OutputKnob_TexPath { get { return "Textures/Out_Knob.png"; } }
	public Type Type { get { return typeof(void); } }
}
