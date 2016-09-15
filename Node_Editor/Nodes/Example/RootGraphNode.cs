using UnityEngine;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework.Standard
{
	/// <summary>
	/// Example Node for showing how to create child nodes along with a parent node directly from the create menu and automatically link them with connections
	/// </summary>
	[Node (false, "Example/Graph Root")]
	public class RootGraphNode : Node 
	{
		public const string ID = "rootGraphNode";
		public override string GetID { get { return ID; } }

		public override Node Create (Vector2 pos) 
		{
			RootGraphNode node = CreateInstance<RootGraphNode> ();

			node.rect = new Rect (pos.x, pos.y, 150, 100);
			node.name = "Graph Root Node";

			NodeOutput out1 = node.CreateOutput ("Child 1", "Flow");
			NodeOutput out2 = node.CreateOutput ("Child 2", "Flow");
			NodeOutput out3 = node.CreateOutput ("Child 3", "Flow");

			// Creates three child nodes that automatically connect to the respective outputs
			//Node.Create ("flowNode", new Vector2 (pos.x+300, pos.y-200), out1);
			//Node.Create ("flowNode", new Vector2 (pos.x+300, pos.y+  0), out2);
			//Node.Create ("flowNode", new Vector2 (pos.x+300, pos.y+200), out3);

			return node;
		}

		protected internal override void NodeGUI () 
		{
			name = GUILayout.TextField (name);

			foreach (NodeOutput output in Outputs)
				output.DisplayLayout ();
		}

		public override bool Calculate () 
		{
			return true;
		}
	}
}
