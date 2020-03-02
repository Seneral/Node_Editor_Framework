using UnityEngine;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework.Standard
{
	[Node (false, "Float/Calculation")]
	public class CalcNode : Node 
	{
		public const string ID = "calcNode";
		public override string GetID { get { return ID; } }

		public override string Title { get { return "Calc Node"; } }
		public override Vector2 DefaultSize { get { return new Vector2 (200, 100); } }

		public enum CalcType { Add, Substract, Multiply, Divide }
		public CalcType type = CalcType.Add;

		[ValueConnectionKnob("Input 1", Direction.In, "Float")]
		public ValueConnectionKnob input1Knob;
		[ValueConnectionKnob("Input 2", Direction.In, "Float")]
		public ValueConnectionKnob input2Knob;

		[ValueConnectionKnob("Output", Direction.Out, "Float")]
		public ValueConnectionKnob outputKnob;

		public float Input1Val = 1f;
		public float Input2Val = 1f;

		public override void NodeGUI () 
		{
			GUILayout.BeginHorizontal ();
			GUILayout.BeginVertical ();

			// First input
			if (input1Knob.connected ())
				GUILayout.Label (input1Knob.name);
			else
				Input1Val = RTEditorGUI.FloatField (GUIContent.none, Input1Val);
			input1Knob.SetPosition ();

			// Second input
			if (input2Knob.connected ())
				GUILayout.Label (input2Knob.name);
			else
				Input2Val = RTEditorGUI.FloatField (GUIContent.none, Input2Val);
			input2Knob.SetPosition ();

			GUILayout.EndVertical ();
			GUILayout.BeginVertical ();

			// Output
			outputKnob.DisplayLayout ();

			GUILayout.EndVertical ();
			GUILayout.EndHorizontal ();

			type = (CalcType)RTEditorGUI.EnumPopup (new GUIContent ("Calculation Type", "The type of calculation performed on Input 1 and Input 2"), type);

			if (GUI.changed)
				NodeEditor.curNodeCanvas.OnNodeChange (this);
		}

		public override bool Calculate () 
		{
			if (input1Knob.connected ())
				Input1Val = input1Knob.GetValue<float> ();
			if (input2Knob.connected ())
				Input2Val = input2Knob.GetValue<float> ();
			
			switch (type) 
			{
			case CalcType.Add:
				outputKnob.SetValue<float> (Input1Val + Input2Val);
				break;
			case CalcType.Substract:
				outputKnob.SetValue<float> (Input1Val - Input2Val);
				break;
			case CalcType.Multiply:
				outputKnob.SetValue<float> (Input1Val * Input2Val);
				break;
			case CalcType.Divide:
				outputKnob.SetValue<float> (Input1Val / Input2Val);
				break;
			}

			return true;
		}
	}
}
