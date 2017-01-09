using UnityEngine;
using System.Collections;
using NodeEditorFramework;

namespace NodeEditorFramework.Standard
{
	[NodeCanvasType("Graph Canvas")]
	public class GraphCanvasType : NodeCanvas
	{
		public override string canvasName { get { return "Graph"; } }

		private string rootNodeID { get { return "rootGraphNode"; } }
		public RootGraphNode rootNode;

		protected override void OnCreate () 
		{
			Traversal = new GraphTraversal (this);
			rootNode = Node.Create (rootNodeID, Vector2.zero) as RootGraphNode;
		}

		private void OnEnable () 
		{
			// Register to other callbacks
			//NodeEditorCallbacks.OnDeleteNode += CheckDeleteNode;
		}

		protected override void OnValidate ()
		{
			if (Traversal == null)
				Traversal = new GraphTraversal (this);
			if (rootNode == null && (rootNode = nodes.Find ((Node n) => n.GetID == rootNodeID) as RootGraphNode) == null)
				rootNode = Node.Create (rootNodeID, Vector2.zero) as RootGraphNode;
		}

		public override bool CanAddNode (string nodeID)
		{
			//Debug.Log ("Check can add node " + nodeID);
			if (nodeID == rootNodeID)
				return !nodes.Exists ((Node n) => n.GetID == rootNodeID);
			return true;
		}
	}
}
