using UnityEngine;
using NodeEditorFramework;

[System.Serializable]
[Node (false, "Float/Display")]
public class DisplayNode : Node 
{
	public const string ID = "displayNode";
	public override string GetID { get { return ID; } }

	[HideInInspector]
	public bool Assigned;
	public float Value;

	public override Node Create (Vector2 pos) 
	{ // This function has to be registered in Node_Editor.ContextCallback
		DisplayNode node = CreateInstance <DisplayNode> ();
		
		node.name = "Display Node";
		node.Rect = new Rect (pos.x, pos.y, 150, 50);
		
		NodeInput.Create (node, "Value", "Float");

		return node;
	}
	
	public override void NodeGUI () 
	{
		Inputs [0].DisplayLayout (new GUIContent ("Value : " + (Assigned? Value.ToString () : ""), "The input value to display"));
	}
	
	public override bool Calculate () 
	{
		if (!AllInputsReady ()) 
		{
			Value = 0;
			Assigned = false;
			return false;
		}

		Value = Inputs[0].Connection.GetValue<float>();
		Assigned = true;

		return true;
	}
}
