using UnityEngine;
using System.Collections;
using NodeEditorFramework;

[System.Serializable]
[Node (false, "Float/Display")]
public class DisplayNode : Node 
{
	public const string ID = "displayNode";
	public override string GetID { get { return ID; } }

	[HideInInspector]
	public bool assigned = false;
	public float value = 0;

	public override Node Create (Vector2 pos) 
	{ // This function has to be registered in Node_Editor.ContextCallback
		DisplayNode node = CreateInstance <DisplayNode> ();
		
		node.name = "Display Node";
		node.rect = new Rect (pos.x, pos.y, 150, 50);
		
		NodeInput.Create (node, "Value", "Float");

		return node;
	}
	
	public override void NodeGUI () 
	{
		Inputs [0].DisplayLayout (new GUIContent ("Value : " + (assigned? value.ToString () : ""), "The input value to display"));
	}
	
	public override bool Calculate () 
	{
		if (!allInputsReady ()) 
		{
			value = 0;
			assigned = false;
			return false;
		}

		value = Inputs[0].connection.GetValue<float>();
		assigned = true;

		return true;
	}
}
