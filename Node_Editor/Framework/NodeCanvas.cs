using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace NodeEditorFramework 
{
	[NodeCanvasType("Default")]
	public partial class NodeCanvas : ScriptableObject 
	{ // Just contains the nodes and global canvas stuff; an associated NodeEditorState holds the actual state now
		public virtual string canvasName { get { return "Calculation Canvas"; } }

		public string saveName;
		public string savePath;

		public bool livesInScene = false;

		public List<Node> nodes = new List<Node> ();
		public List<NodeGroup> groups = new List<NodeGroup> ();

		public NodeEditorState[] editorStates = new NodeEditorState[0];

		public virtual void BeforeSavingCanvas () { }

		/// <summary>
		/// Will validate this canvas for any broken nodes or references and cleans them.
		/// </summary>
		public void Validate () 
		{
			if (string.IsNullOrEmpty (saveName))
			{
				if (name != "LastSession")
					saveName = name;
				else
					saveName = "New " + canvasName;
			}
			if (nodes == null)
			{
				Debug.LogWarning ("NodeCanvas '" + name + "' nodes were erased and set to null! Automatically fixed!");
				nodes = new List<Node> ();
			}
			for (int groupCnt = 0; groupCnt < groups.Count; groupCnt++) 
			{
				NodeGroup group = groups[groupCnt];
				if (group == null)
				{
					Debug.LogWarning ("NodeCanvas '" + name + "' contained broken (null) group! Automatically fixed!");
					groups.RemoveAt (groupCnt);
					groupCnt--;
					continue;
				}
			}
			for (int nodeCnt = 0; nodeCnt < nodes.Count; nodeCnt++) 
			{
				Node node = nodes[nodeCnt];
				if (node == null)
				{
					Debug.LogWarning ("NodeCanvas '" + name + "' contained broken (null) node! Automatically fixed!");
					nodes.RemoveAt (nodeCnt);
					nodeCnt--;
					continue;
				}
				for (int knobCnt = 0; knobCnt < node.nodeKnobs.Count; knobCnt++) 
				{
					NodeKnob nodeKnob = node.nodeKnobs[knobCnt];
					if (nodeKnob == null)
					{
						Debug.LogWarning ("NodeCanvas '" + name + "' Node '" + node.name + "' contained broken (null) NodeKnobs! Automatically fixed!");
						node.nodeKnobs.RemoveAt (knobCnt);
						knobCnt--;
						continue;
					}

					if (nodeKnob is NodeInput)
					{
						NodeInput input = nodeKnob as NodeInput;
						if (input.connection != null && input.connection.body == null)
						{ // References broken node; Clear connection
							input.connection = null;
						}
//						for (int conCnt = 0; conCnt < (nodeKnob as NodeInput).connection.Count; conCnt++)
					}
					else if (nodeKnob is NodeOutput)
					{
						NodeOutput output = nodeKnob as NodeOutput;
						for (int conCnt = 0; conCnt < output.connections.Count; conCnt++) 
						{
							NodeInput con = output.connections[conCnt];
							if (con == null || con.body == null)
							{ // Broken connection; Clear connection
								output.connections.RemoveAt (conCnt);
								conCnt--;
							}
						}
					}
				}
			}

			if (editorStates == null)
			{
				Debug.LogWarning ("NodeCanvas '" + name + "' editorStates were erased! Automatically fixed!");
				editorStates = new NodeEditorState[0];
			}
			editorStates = editorStates.Where ((NodeEditorState state) => state != null).ToArray ();
			foreach (NodeEditorState state in editorStates)
			{
				if (!nodes.Contains (state.selectedNode))
					state.selectedNode = null;
			}
		}

		public void UpdateSource (string path) 
		{
			string newName;
			if (path.StartsWith ("SCENE/"))
			{
				newName = path.Substring (6);
			}
			else
			{
				int nameStart = path.LastIndexOf ('/')+1;
				newName = path.Substring (nameStart, path.Length-nameStart-6);
			}
			if (newName != "LastSession")
			{
				savePath = path;
				saveName = newName;
			}
		}
	}
}