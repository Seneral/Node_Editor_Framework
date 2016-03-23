using UnityEngine;
using System.Collections.Generic;
using NodeEditorFramework;

namespace NodeEditorFramework 
{
	public class NodeCanvas : ScriptableObject 
	{ // Just contains the nodes and global canvas stuff; an associated NodeEditorState holds the actual state now
		public List<Node> nodes = new List<Node> ();
		public List<NodeGroup> groups = new List<NodeGroup> ();

		public bool livesInScene = false;
	}
}