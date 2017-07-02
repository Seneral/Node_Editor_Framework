using UnityEngine;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework.Standard
{
	[Node (false, "Example/Example Node")]
	public class ExampleNode : Node 
	{
		public const string ID = "exampleNode";
		public override string GetID { get { return ID; } }

		public override string Title { get { return "Example Node"; } }
		public override Vector2 DefaultSize { get { return new Vector2 (150, 60); } }

		[ValueConnectionKnob("Input", Direction.In, "Float")]
		public ValueConnectionKnob inputKnob;
		[ValueConnectionKnob("Output", Direction.Out, "Float")]
		public ValueConnectionKnob outputKnob;
		
		public override void NodeGUI () 
		{
			GUILayout.Label ("This is a custom Node!");

			GUILayout.BeginHorizontal ();
			GUILayout.BeginVertical ();

			inputKnob.DisplayLayout ();

			GUILayout.EndVertical ();
			GUILayout.BeginVertical ();
			
			outputKnob.DisplayLayout ();
			
			GUILayout.EndVertical ();
			GUILayout.EndHorizontal ();
			
		}
		
		public override bool Calculate () 
		{
			outputKnob.SetValue<float> (inputKnob.GetValue<float> () * 5);
			return true;
		}
	}
}