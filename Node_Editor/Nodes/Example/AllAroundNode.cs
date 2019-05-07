using UnityEngine;

namespace NodeEditorFramework.Standard
{
	[Node (false, "Example/AllAround Node")]
	public class AllAroundNode : Node 
	{
		public const string ID = "allaroundNode";
		public override string GetID { get { return ID; } }

		public override string Title { get { return "AllAround Node"; } }
		public override Vector2 DefaultSize { get { return new Vector2 (60, 60); } }

		public override bool ContinueCalculation { get { return true; } }

		[ValueConnectionKnob("Input Top", Direction.In, "Float", NodeSide.Top, 20)]
		public ValueConnectionKnob inputTop;
		[ValueConnectionKnob("Input Bottom", Direction.In, "Float", NodeSide.Bottom, 20)]
		public ValueConnectionKnob inputBottom;
		[ValueConnectionKnob("Input Right", Direction.In, "Float", NodeSide.Right, 20)]
		public ValueConnectionKnob inputRight;
		[ValueConnectionKnob("Input Left", Direction.In, "Float", NodeSide.Left, 20)]
		public ValueConnectionKnob inputLeft;

		[ValueConnectionKnob("Output Top", Direction.Out, "Float", NodeSide.Top, 40)]
		public ValueConnectionKnob outputTop;
		[ValueConnectionKnob("Output Bottom", Direction.Out, "Float", NodeSide.Bottom, 40)]
		public ValueConnectionKnob outputBottom;
		[ValueConnectionKnob("Output Right", Direction.Out, "Float", NodeSide.Right, 40)]
		public ValueConnectionKnob outputRight;
		[ValueConnectionKnob("Output Left", Direction.Out, "Float", NodeSide.Left, 40)]
		public ValueConnectionKnob outputLeft;
		
		protected internal override void DrawNode () 
		{
			Rect nodeRect = rect;
			nodeRect.position += NodeEditor.curEditorState.zoomPanAdjust + NodeEditor.curEditorState.panOffset;
			GUI.Box (nodeRect, GUIContent.none, GUI.skin.box);
		}
		
		public override bool Calculate () 
		{
			outputTop.SetValue<float> (inputTop.GetValue<float> ());
			outputBottom.SetValue<float> (inputBottom.GetValue<float> ());
			outputRight.SetValue<float> (inputRight.GetValue<float> ());
			outputLeft.SetValue<float> (inputLeft.GetValue<float> ());

			return true;
		}
	}
}