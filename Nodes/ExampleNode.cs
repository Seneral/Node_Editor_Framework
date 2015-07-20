using UnityEngine;
using System.Collections;

[System.Serializable]
[Node (true, "Example")]
public class ExampleNode : Node 
{
	public const string ID = "exampleNode";
	public override string GetID { get { return ID; } }
	
	public override Node Create (Vector2 pos) 
	{
		ExampleNode node = CreateInstance<ExampleNode> ();
		
		node.rect = new Rect (pos.x, pos.y, 100, 50);
		node.name = "Example Node";
		
		NodeInput.Create (node, "Value", "Float");
		NodeOutput.Create (node, "Output val", "Float");

		return node;
	}
	
	public override void NodeGUI () 
	{
		GUILayout.Label ("This is a custom Node!");
		
		GUILayout.Label ("Input");
		if (Event.current.type == EventType.Repaint)
			Inputs [0].SetRect (GUILayoutUtility.GetLastRect ());
		
	}
	
	public override bool Calculate () 
	{
		if (!allInputsReady ())
			return false;
		Outputs[0].GetValue<FloatValue>().value = Inputs[0].connection.GetValue<FloatValue>().value * 5;
		return true;
	}
}
