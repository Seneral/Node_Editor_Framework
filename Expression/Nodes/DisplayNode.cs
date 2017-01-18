using UnityEngine;
using NodeEditorFramework;

[Node (false, "Utility/Display Expression")]
public class DisplayNode : Node 
{
	public const string ID = "displayExpNode";
	public override string GetID { get { return ID; } }

	public object value = null;

	public override Node Create (Vector2 pos) 
	{ // This function has to be registered in Node_Editor.ContextCallback
		DisplayNode node = CreateInstance <DisplayNode> ();
		
		node.name = "Display Node";
		node.rect = new Rect (pos.x, pos.y, 150, 50);
		
		NodeInput.Create (node, "Value", typeof(object).AssemblyQualifiedName);

		return node;
	}
	
	protected override void NodeGUI () 
	{
		string valueString = "NULL";//= value != null? ((Object)value != null? ((Object)value).name : value.ToString ()) : "NULL";
		if (value != null)
			valueString = (value as Object != null)? (value as Object).name : value.ToString ();
		Inputs [0].DisplayLayout (new GUIContent ("Value : " + valueString, "The input value to display"));
	}
	
	public override bool Calculate () 
	{
		value = Inputs [0].connection == null? null : Inputs[0].connection.GetValue (typeof(object));
		return true;
	}
}
