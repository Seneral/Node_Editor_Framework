using UnityEngine;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;

[Node (false, "Example/Example Node")]
public class ExampleNode : Node 
{
	public const string ID = "exampleNode";
	public override string GetID { get { return ID; } }
	
	public override Node Create (Vector2 pos) 
	{
		ExampleNode node = CreateInstance<ExampleNode> ();
		
		node.rect = new Rect (pos.x, pos.y, 150, 60);
		node.name = "Example Node";
		
		node.CreateInput ("Value", "Float");
		node.CreateOutput ("Output val", "Float");
		
		return node;
	}
	
	protected internal override void NodeGUI () 
	{
		GUILayout.Label ("This is a custom Node!");

		GUILayout.BeginHorizontal ();
		GUILayout.BeginVertical ();

		Inputs [0].DisplayLayout ();

		GUILayout.EndVertical ();
		GUILayout.BeginVertical ();
		
		Outputs [0].DisplayLayout ();
		
		GUILayout.EndVertical ();
		GUILayout.EndHorizontal ();
		
	}
	
	public override bool Calculate () 
	{
		if (!allInputsReady ())
			return false;
		Outputs[0].SetValue<float> (Inputs[0].GetValue<float> () * 5);
		return true;
	}
}
