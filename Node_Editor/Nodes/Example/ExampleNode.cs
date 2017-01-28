using UnityEngine;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework.Standard
{
	[Node (false, "Example/Example Node")]
	public class ExampleNode : Node 
	{
		public override Vector2 MinSize { get { return new Vector2(150, 10); } }
		public override bool Resizable { get { return true; } }

		public const string ID = "exampleNode";
		public override string GetID { get { return ID; } }
		
		public override Node Create (Vector2 pos) 
		{
			ExampleNode node = CreateInstance<ExampleNode> ();

			node.rect.position = pos;
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
}