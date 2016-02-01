using UnityEngine;
using System;
using System.Collections.Generic;
using NodeEditorFramework;

[Serializable]
[Node (true, "Group")]
public class GroupNode : Node 
{
	public const string ID = "groupNode";
	public override string GetID { get { return ID; } }

	public bool Edit;
	public NodeCanvas NodeGroupCanvas;
	public NodeEditorState EditorState;

	private Vector2 canvasSize = new Vector2 (400, 400);
	private const int BorderWidth = 6;
	public Rect NodeRect;
	public Rect OpenedRect { get { return new Rect (Rect.center.x - canvasSize.x/2 - BorderWidth - 100, Rect.y, canvasSize.x + BorderWidth*2 + 200, 62 + canvasSize.y + BorderWidth); } }
	
	public override Node Create (Vector2 pos) 
	{ // This function has to be registered in Node_Editor.ContextCallback
		GroupNode node = CreateInstance <GroupNode> ();
		
		node.name = "Group Node";
		node.Rect = node.NodeRect = new Rect (pos.x, pos.y, 300, 80);

		return node;
	}
	
	public override void NodeGUI () 
	{
		GUILayout.BeginHorizontal ();
#if UNITY_EDITOR
		if (GUILayout.Button (new GUIContent ("Load", "Loads the group from an extern Canvas Asset File.")))
		{
			string path = UnityEditor.EditorUtility.OpenFilePanel ("Load Node Canvas", NodeEditor.EditorPath + "Saves/", "asset");
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
			NodeEditor.SaveNodeCanvas (NodeGroupCanvas, UnityEditor.EditorUtility.SaveFilePanelInProject ("Save Group Node Canvas", "Group Canvas", "asset", "", NodeEditor.EditorPath + "Saves/"));
		}
#endif
		if (GUILayout.Button (new GUIContent ("New Group Canvas", "Creates a new Canvas"))) 
		{
			NodeGroupCanvas = CreateInstance<NodeCanvas> ();

			EditorState = CreateInstance<NodeEditorState> ();
			EditorState.Drawing = Edit;
			EditorState.name = "GroupNode_EditorState";

			Node node = NodeTypes.GetDefaultNode ("exampleNode");
			if (node != null)
			{
				NodeCanvas prevNodeCanvas = NodeEditor.CurNodeCanvas;
				NodeEditor.CurNodeCanvas = NodeGroupCanvas;
				node = node.Create (Vector2.zero);
				node.InitBase ();
				NodeEditor.CurNodeCanvas = prevNodeCanvas;
			}
		}
		GUILayout.EndHorizontal ();

		if (NodeGroupCanvas != null)
		{
			foreach (NodeInput input in Inputs)
				input.DisplayLayout ();

			foreach (NodeOutput output in Outputs)
				output.DisplayLayout ();

			if (!Edit)
			{
				if (GUILayout.Button ("Edit Node Canvas"))
				{
					Rect = OpenedRect;
					Edit = true;
					EditorState.CanvasRect = GUILayoutUtility.GetRect (canvasSize.x, canvasSize.y, GUIStyle.none);
					EditorState.Drawing = true;
				}
			}
			else
			{
				if (GUILayout.Button ("Stop editing Node Canvas"))
				{
					NodeRect.position = OpenedRect.position + new Vector2 (canvasSize.x/2 - NodeRect.width/2, 0);
					Rect = NodeRect;
					Edit = false;
					EditorState.Drawing = false;
				}

				Rect canvasRect = GUILayoutUtility.GetRect (canvasSize.x, canvasSize.y, new GUILayoutOption[] { GUILayout.ExpandWidth (false) });
				if (Event.current.type != EventType.Layout) 
				{
					EditorState.CanvasRect = canvasRect;
					Rect canvasControlRect = EditorState.CanvasRect;
					canvasControlRect.position += Rect.position + ContentOffset;
					NodeEditor.CurEditorState.IgnoreInput.Add (NodeEditor.CanvasGUIToScreenRect (canvasControlRect));	
				}

				NodeEditor.DrawSubCanvas (NodeGroupCanvas, EditorState);


				GUILayout.BeginArea (new Rect (canvasSize.x + 8, 45, 200, canvasSize.y), GUI.skin.box);
				GUILayout.Label (new GUIContent ("Node Editor (" + NodeGroupCanvas.name + ")", "The currently opened canvas in the Node Editor"));
				#if UNITY_EDITOR
				EditorState.Zoom = UnityEditor.EditorGUILayout.Slider (new GUIContent ("Zoom", "Use the Mousewheel. Seriously."), EditorState.Zoom, 0.6f, 2);
				#endif
				GUILayout.EndArea ();


				// Node is drawn by parent nodeCanvas, usually the mainNodeCanvas, because the zoom feature requires it to be drawn outside of any GUI group
			}
		}
	}

	public void LoadNodeCanvas (string path) 
	{
		NodeGroupCanvas = NodeEditor.LoadNodeCanvas (path);
		if (NodeGroupCanvas != null) 
		{
			List<NodeEditorState> editorStates = NodeEditor.LoadEditorStates (path);
			EditorState = editorStates.Count == 0? CreateInstance<NodeEditorState> () : editorStates[0];
			EditorState.Canvas = NodeGroupCanvas;
			EditorState.ParentEditor = NodeEditor.CurEditorState;
			EditorState.Drawing = Edit;
			EditorState.name = "GroupNode_EditorState";

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

		if (NodeGroupCanvas == null)
			return;

		Debug.Log ("Adopting Inputs/Outputs");
		foreach (Node node in NodeGroupCanvas.nodes) 
		{
			Debug.Log ("Checking node!");
			if (node.Inputs.Count == 0) 
			{ // Input Node
				Debug.Log ("Adopting input node!");
				foreach (NodeOutput output in node.Outputs)
					Inputs.Add (NodeInput.Create (node, output.name, output.Type));
			}
			else if (node.Outputs.Count == 0)
			{ // Output node
				Debug.Log ("Adopting output node!");
				foreach (NodeInput input in node.Inputs)
					Outputs.Add (NodeOutput.Create (node, input.name, input.Type));
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


