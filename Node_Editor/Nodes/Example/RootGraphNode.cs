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

			node.CreateOutput ("Child 1", "Flow");
			node.CreateOutput ("Child 2", "Flow");
			node.CreateOutput ("Child 3", "Flow");

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
