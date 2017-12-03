using UnityEngine;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework.Standard
{
	[Node (false, "Example/Graph Root")]
	public class RootGraphNode : Node 
	{
		public const string ID = "rootGraphNode";
		public override string GetID { get { return ID; } }

		public override string Title { get { return "Graph Root Node"; } }
		public override Vector2 DefaultSize { get { return new Vector2 (150, 100); } }

		[ConnectionKnob("Child 1", Direction.Out, "Flow")]
		public ConnectionKnob flowChild1;
		[ConnectionKnob("Child 2", Direction.Out, "Flow")]
		public ConnectionKnob flowChild2;
		[ConnectionKnob("Child 3", Direction.Out, "Flow")]
		public ConnectionKnob flowChild3;

		public override void NodeGUI () 
		{
			name = RTEditorGUI.TextField (name);

			foreach (ConnectionKnob knob in connectionKnobs) 
				knob.DisplayLayout ();
		}
	}
}
