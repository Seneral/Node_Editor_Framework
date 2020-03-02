using UnityEngine;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework.Standard
{
	[System.Serializable]
	[Node(false, "Float/Input")]
	public class InputNode : Node
	{
		public const string ID = "inputNode";
		public override string GetID { get { return ID; } }

		public override string Title { get { return "Input Node"; } }
		public override Vector2 DefaultSize { get { return new Vector2(200, 50); } }

		[ValueConnectionKnob("Value", Direction.Out, "Float")]
		public ValueConnectionKnob outputKnob;

		public float value = 1f;

		public override void NodeGUI()
		{
			value = RTEditorGUI.FloatField(new GUIContent("Value", "The input value of type float"), value);
			outputKnob.SetPosition();

			if (GUI.changed)
				NodeEditor.curNodeCanvas.OnNodeChange(this);
		}

		public override bool Calculate()
		{
			outputKnob.SetValue<float>(value);
			return true;
		}
	}
}