using UnityEngine;
using NodeEditorFramework;

[Node (true, "Example Node")]
public class ExampleNode : Node 
{
	public const string ID = "exampleNode";
	public override string GetID { get { return ID; } }
	
	public override Node Create (Vector2 pos) 
	{
		ExampleNode node = CreateInstance<ExampleNode> ();
		
		node.Rect = new Rect (pos.x, pos.y, 150, 60);
		node.name = "Example Node";
		
		node.CreateInput ("Value", "Float");
		node.CreateOutput ("Output val", "Float");
		
		return node;
	}
	
	public override void NodeGUI () 
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
		if (!AllInputsReady ())
			return false;
		Outputs[0].SetValue (Inputs[0].GetValue<float> () * 5);
		return true;
	}
}
