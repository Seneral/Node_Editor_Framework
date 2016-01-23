using UnityEngine;
using System;
using System.Collections.Generic;
using NodeEditorFramework;

[System.Serializable]
[Node (true, "Group")]
public class GroupNode : Node 
{
	public const string ID = "groupNode";
	public override string GetID { get { return ID; } }

	public bool edit = false;
	public NodeCanvas nodeGroupCanvas;
	public NodeEditorState editorState;

	private Vector2 canvasSize = new Vector2 (400, 400);
	private const int borderWidth = 6;
	public Rect nodeRect;
	public Rect openedRect { get { return new Rect (rect.center.x - canvasSize.x/2 - borderWidth - 100, rect.y, canvasSize.x + borderWidth*2 + 200, 62 + canvasSize.y + borderWidth); } }
	
	public override Node Create (Vector2 pos) 
	{ // This function has to be registered in Node_Editor.ContextCallback
		GroupNode node = CreateInstance <GroupNode> ();
		
		node.name = "Group Node";
		node.rect = node.nodeRect = new Rect (pos.x, pos.y, 300, 80);

		return node;
	}
	
	public override void NodeGUI () 
	{
		GUILayout.BeginHorizontal ();
#if UNITY_EDITOR
		if (GUILayout.Button (new GUIContent ("Load", "Loads the group from an extern Canvas Asset File.")))
		{
			string path = UnityEditor.EditorUtility.OpenFilePanel ("Load Node Canvas", NodeEditor.editorPath + "Saves/", "asset");
			if (!path.Contains (Application.dataPath)) 
			{
				// TODO: Generic Notification
				//if (path != String.Empty)
					//ShowNotification (new GUIContent ("You should select an asset inside your project folder!"));
				return;
			}
			path = path.Replace (Application.dataPath, "Assets");
			LoadNodeCanvas (path);
			//AdoptInputsOutputs ();
		}
		if (GUILayout.Button (new GUIContent ("Save", "Saves the group as a new Canvas Asset File"))) 
		{
			NodeEditor.SaveNodeCanvas (nodeGroupCanvas, UnityEditor.EditorUtility.SaveFilePanelInProject ("Save Group Node Canvas", "Group Canvas", "asset", "", NodeEditor.editorPath + "Saves/"));
		}
#endif
		if (GUILayout.Button (new GUIContent ("New Group Canvas", "Creates a new Canvas"))) 
		{
			nodeGroupCanvas = CreateInstance<NodeCanvas> ();

			editorState = CreateInstance<NodeEditorState> ();
			editorState.drawing = edit;
			editorState.name = "GroupNode_EditorState";

			Node node = NodeTypes.getDefaultNode ("exampleNode");
			if (node != null)
			{
				NodeCanvas prevNodeCanvas = NodeEditor.curNodeCanvas;
				NodeEditor.curNodeCanvas = nodeGroupCanvas;
				node = node.Create (Vector2.zero);
				node.InitBase ();
				NodeEditor.curNodeCanvas = prevNodeCanvas;
			}
		}
		GUILayout.EndHorizontal ();

		if (nodeGroupCanvas != null)
		{
			foreach (NodeInput input in Inputs)
				input.DisplayLayout ();

			foreach (NodeOutput output in Outputs)
				output.DisplayLayout ();

			if (!edit)
			{
				if (GUILayout.Button ("Edit Node Canvas"))
				{
					rect = openedRect;
					edit = true;
					editorState.canvasRect = GUILayoutUtility.GetRect (canvasSize.x, canvasSize.y, GUIStyle.none);
					editorState.drawing = true;
				}
			}
			else
			{
				if (GUILayout.Button ("Stop editing Node Canvas"))
				{
					nodeRect.position = openedRect.position + new Vector2 (canvasSize.x/2 - nodeRect.width/2, 0);
					rect = nodeRect;
					edit = false;
					editorState.drawing = false;
				}

				Rect canvasRect = GUILayoutUtility.GetRect (canvasSize.x, canvasSize.y, new GUILayoutOption[] { GUILayout.ExpandWidth (false) });
				if (Event.current.type != EventType.Layout) 
				{
					editorState.canvasRect = canvasRect;
					Rect canvasControlRect = editorState.canvasRect;
					canvasControlRect.position += rect.position + contentOffset;
					NodeEditor.curEditorState.ignoreInput.Add (NodeEditorFramework.NodeEditor.CanvasGUIToScreenRect (canvasControlRect));	
				}

				NodeEditor.DrawSubCanvas (nodeGroupCanvas, editorState);


				GUILayout.BeginArea (new Rect (canvasSize.x + 8, 45, 200, canvasSize.y), GUI.skin.box);
				GUILayout.Label (new GUIContent ("Node Editor (" + nodeGroupCanvas.name + ")", "The currently opened canvas in the Node Editor"));
				#if UNITY_EDITOR
				editorState.zoom = UnityEditor.EditorGUILayout.Slider (new GUIContent ("Zoom", "Use the Mousewheel. Seriously."), editorState.zoom, 0.6f, 2);
				#endif
				GUILayout.EndArea ();


				// Node is drawn by parent nodeCanvas, usually the mainNodeCanvas, because the zoom feature requires it to be drawn outside of any GUI group
			}
		}
	}

	public void LoadNodeCanvas (string path) 
	{
		nodeGroupCanvas = NodeEditor.LoadNodeCanvas (path);
		if (nodeGroupCanvas != null) 
		{
			List<NodeEditorState> editorStates = NodeEditor.LoadEditorStates (path);
			editorState = editorStates.Count == 0? CreateInstance<NodeEditorState> () : editorStates[0];
			editorState.canvas = nodeGroupCanvas;
			editorState.parentEditor = NodeEditor.curEditorState;
			editorState.drawing = edit;
			editorState.name = "GroupNode_EditorState";

			string[] folders = path.Split (new char[] {'/'}, StringSplitOptions.None);
			string canvasName = folders [folders.Length-1];
			if (canvasName.EndsWith (".asset"))
				canvasName = canvasName.Remove (canvasName.Length-6);
			name = canvasName;
		}
		else 
			name = "Node Group";
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


