using UnityEngine;
using System;
using System.Collections.Generic;

[System.Serializable]
[Node (true, "Group")]
public class GroupNode : Node 
{
	public const string ID = "groupNode";
	public override string GetID { get { return ID; } }

	public bool edit = false;
	public NodeCanvas nodeGroupCanvas;
	public NodeEditorState editorState;

	private Vector2 canvasSize = new Vector2 (800, 800);
	private const int borderWidth = 6;
	public Rect nodeRect;
	public Rect openedRect 
	{
		get { return new Rect (rect.center.x - canvasSize.x/2, rect.y, canvasSize.x, canvasSize.y); }
	}
	public Rect canvasRect 
	{
		get
		{
			Rect rectWithMargin = openedRect;
			return new Rect (rectWithMargin.x + borderWidth, rectWithMargin.y + 62 + borderWidth, rectWithMargin.width - borderWidth*2, rectWithMargin.height - 62-borderWidth*2);
		}
	}
	
	public override Node Create (Vector2 pos) 
	{ // This function has to be registered in Node_Editor.ContextCallback
		GroupNode node = CreateInstance <GroupNode> ();
		
		node.name = "Group Node";
		node.rect = node.nodeRect = new Rect (pos.x, pos.y, 300, 100);

		return node;
	}
	
	public override void NodeGUI () 
	{
		GUILayout.BeginHorizontal ();
		if (GUILayout.Button (new GUIContent ("Load", "Loads the group from an extern Canvas Asset File.")))
		{
#if UNITY_EDITOR
			string path = UnityEditor.EditorUtility.OpenFilePanel ("Load Node Canvas", NodeEditor.editorPath + "Saves/", "asset");
			if (!path.Contains (Application.dataPath)) 
			{
				if (path != String.Empty)
					NodeEditorWindow.editor.ShowNotification (new GUIContent ("You should select an asset inside your project folder!"));
				return;
			}
			path = path.Replace (Application.dataPath, "Assets");

			if (nodeGroupCanvas != null)
				NodeEditor.curNodeCanvas.childs.Remove (nodeGroupCanvas);
			nodeGroupCanvas = NodeEditor.LoadNodeCanvas (path);
			nodeGroupCanvas.parent = NodeEditor.curNodeCanvas;
			NodeEditor.curNodeCanvas.childs.Add (nodeGroupCanvas);

			if (editorState != null)
				NodeEditor.curEditorState.childs.Remove (editorState);
			editorState = NodeEditor.LoadEditorStates (path) [0];
			editorState.parent = NodeEditor.curEditorState;
			editorState.drawing = edit;
			NodeEditor.curEditorState.childs.Add (editorState);

			if (nodeGroupCanvas != null) 
			{ // Set the name
				string[] folders = path.Split (new char[] {'/'}, StringSplitOptions.None);
				string canvasName = folders [folders.Length-1];
				if (canvasName.EndsWith (".asset"))
					canvasName = canvasName.Remove (canvasName.Length-6);
				name = canvasName;
			}
			else 
				name = "Node Group";
			//AdoptInputsOutputs ();
#endif
		}
		if (GUILayout.Button (new GUIContent ("Save", "Saves the group as a new Canvas Asset File"))) 
		{
#if UNITY_EDITOR
			NodeEditor.SaveNodeCanvas (nodeGroupCanvas, UnityEditor.EditorUtility.SaveFilePanelInProject ("Save Group Node Canvas", "Group Canvas", "asset", 
			                                                                                              "Saving to a file is only needed once.", NodeEditor.editorPath + "Saves/"));
#endif
		}
		if (GUILayout.Button (new GUIContent ("New Group Canvas", "Creates a new Canvas (remember to save the previous one to a referenced Canvas Asset File at least once before! Else it'll be lost!)"))) 
		{
			if (nodeGroupCanvas != null)
				NodeEditor.curNodeCanvas.childs.Remove (nodeGroupCanvas);
			nodeGroupCanvas = CreateInstance<NodeCanvas> ();
			nodeGroupCanvas.parent = NodeEditor.curNodeCanvas;
			NodeEditor.curNodeCanvas.childs.Add (nodeGroupCanvas);

			if (editorState != null)
				NodeEditor.curEditorState.childs.Remove (editorState);
			editorState = CreateInstance<NodeEditorState> ();
			editorState.parent = NodeEditor.curEditorState;
			editorState.drawing = edit;
			NodeEditor.curEditorState.childs.Add (editorState);
			editorState.name = "GroupNode_EditorState";
		}
		GUILayout.EndHorizontal ();

		foreach (NodeInput input in Inputs)
			input.DisplayLayout ();

		foreach (NodeOutput output in Outputs)
			output.DisplayLayout ();

		if (!edit && nodeGroupCanvas != null)
		{
			if (GUILayout.Button ("Edit Node Canvas"))
			{
				rect = openedRect;
				edit = true;
				editorState.canvasRect = canvasRect;
				editorState.drawing = true;
			}
		}
		else if (nodeGroupCanvas != null)
		{
			editorState.canvasRect = canvasRect;
			if (GUILayout.Button ("Stop editing Node Canvas"))
			{
				nodeRect.position = openedRect.position + new Vector2 (canvasSize.x/2 - nodeRect.width/2, 0);
				rect = nodeRect;
				edit = false;
				editorState.drawing = false;
			}
			// Node is drawn by parent nodeCanvas, usually the mainNodeCanvas, because the zoom feature requires it to be drawn outside of any GUI group
		}
	}

	public void AdoptInputsOutputs () 
	{
		Inputs = new List<NodeInput> ();
		Outputs = new List<NodeOutput> ();

		if (nodeGroupCanvas == null)
			return;

		Debug.Log ("Adopting Inputs/Outputs");
		foreach (Node node in nodeGroupCanvas.nodes) 
		{
			Debug.Log ("Checking node!");
			if (node.Inputs.Count == 0) 
			{ // Input Node
				Debug.Log ("Adopting input node!");
				foreach (NodeOutput output in node.Outputs)
					Inputs.Add (NodeInput.Create (node, output.name, output.type));
			}
			else if (node.Outputs.Count == 0)
			{ // Output node
				Debug.Log ("Adopting output node!");
				foreach (NodeInput input in node.Inputs)
					Outputs.Add (NodeOutput.Create (node, input.name, input.type));
			}
		}
	}
	
	public override bool Calculate () 
	{
		// And set inputs/outputs
//		if (nodeGroupCanvas != null)
//			NodeEditor.RecalculateAll (nodeGroupCanvas, false);
		return true;
	}
}


