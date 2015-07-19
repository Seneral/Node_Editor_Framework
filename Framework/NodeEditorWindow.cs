using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

public class NodeEditorWindow : EditorWindow 
{
	// Information about current instances
	static NodeEditorWindow _editor;
	public static NodeEditorWindow editor
	{
		get
		{
			if (_editor == null)
			{
				CreateEditor();
				_editor.Repaint();
			}
			return _editor;
		}
	}

	public static NodeCanvas MainNodeCanvas { get { return editor.mainNodeCanvas; } }
	public static NodeEditorState MainEditorState { get { return editor.mainEditorState; } }

	// The main Node Canvas
	public NodeCanvas mainNodeCanvas;
	public NodeEditorState mainEditorState;

	public static string openedCanvas = "New Canvas";
	public static string openedCanvasPath;

	// Settings
	public static int sideWindowWidth = 400;

	[MenuItem("Window/Node Editor")]
	static void CreateEditor () 
	{
		_editor = GetWindow<NodeEditorWindow> ("Node Editor");
		_editor.minSize = new Vector2 (800, 600);
	}

	#region GUI

	public void OnGUI () 
	{
		if (!mainNodeCanvas || !mainEditorState)
		{
			NewNodeCanvas ();
			// Example of creating Nodes and Connections through code
//			CalcNode calcNode1 = CalcNode.Create (new Rect (200, 200, 200, 100));
//			CalcNode calcNode2 = CalcNode.Create (new Rect (600, 200, 200, 100));
//			Node.ApplyConnection (calcNode1.Outputs [0], calcNode2.Inputs [0]);
		}
		NodeEditor.checkInit ();
		
		mainEditorState.canvasRect = canvasWindowRect;
		NodeEditor.DrawCanvas (mainNodeCanvas, mainEditorState);
		
		// Draw Side Window
		sideWindowWidth = Math.Min (600, Math.Max (200, (int)(position.width / 5)));
		GUILayout.BeginArea (sideWindowRect, NodeEditor.nodeBox);
		DrawSideWindow ();
		GUILayout.EndArea ();
	}

	public void DrawSideWindow () 
	{
		GUILayout.Label (new GUIContent ("Node Editor (" + openedCanvas + ")", "The currently opened canvas in the Node Editor"), NodeEditor.nodeLabelBold);
		GUILayout.Label (new GUIContent ("Do note that changes will be saved automatically!", "All changes are automatically saved to the currently opened canvas (see above) if it's present in the Project view."), NodeEditor.nodeLabel);
		if (GUILayout.Button (new GUIContent ("Save Canvas", "Saves the canvas as a new Canvas Asset File in the Assets Folder"))) 
		{
			SaveNodeCanvas (EditorUtility.SaveFilePanelInProject ("Save Node Canvas", "Node Canvas", "asset", "Saving to a file is only needed once.", NodeEditor.editorPath + "Saves/"));
		}
		if (GUILayout.Button (new GUIContent ("Load Canvas", "Loads the canvas from a Canvas Asset File in the Assets Folder"), NodeEditor.nodeButton)) 
		{
			string path = EditorUtility.OpenFilePanel ("Load Node Canvas", NodeEditor.editorPath + "Saves/", "asset");
			if (!path.Contains (Application.dataPath)) 
			{
				if (path != String.Empty)
					ShowNotification (new GUIContent ("You should select an asset inside your project folder!"));
				return;
			}
			path = path.Replace (Application.dataPath, "Assets");
			LoadNodeCanvas (path);
		}
		if (GUILayout.Button (new GUIContent ("New Canvas", "Creates a new Canvas (remember to save the previous one to a referenced Canvas Asset File at least once before! Else it'll be lost!)"), NodeEditor.nodeButton)) 
		{
			NewNodeCanvas ();
		}
		if (GUILayout.Button (new GUIContent ("Recalculate All", "Starts to calculate from the beginning off."), NodeEditor.nodeButton)) 
		{
			NodeEditor.RecalculateAll (mainNodeCanvas);
		}
		NodeEditor.knobSize = EditorGUILayout.IntSlider (new GUIContent ("Handle Size", "The size of the handles of the Node Inputs/Outputs"), NodeEditor.knobSize, 12, 20);
		mainEditorState.zoom = EditorGUILayout.Slider (new GUIContent ("Zoom"), mainEditorState.zoom, 0.6f, 2);
	}
	
	public Rect sideWindowRect 
	{
		get { return new Rect (position.width - sideWindowWidth, 0, sideWindowWidth, position.height); }
	}
	public Rect canvasWindowRect 
	{
		get { return new Rect (0, 0, position.width - sideWindowWidth, position.height); }
	}

	#endregion

	#region Save/Load
	
	/// <summary>
	/// Saves the mainNodeCanvas and it's associated mainEditorState as an asset at path
	/// </summary>
	public void SaveNodeCanvas (string path) 
	{
		NodeEditor.SaveNodeCanvas (mainNodeCanvas, path, mainEditorState);
		Repaint ();
	}
	
	/// <summary>
	/// Loads the mainNodeCanvas and it's associated mainEditorState from an asset at path
	/// </summary>
	public void LoadNodeCanvas (string path) 
	{
		// Load the NodeCanvas
		NodeCanvas nodeCanvas = NodeEditor.LoadNodeCanvas (path);
		if (nodeCanvas == null)
			return;
		mainNodeCanvas = nodeCanvas;
		
		// Load the associated MainEditorState
		List<NodeEditorState> editorStates = NodeEditor.LoadEditorStates (path);
		mainEditorState = editorStates.Find (x => x.name == "MainEditorState");
		if (mainEditorState == null)
			mainEditorState = CreateInstance<NodeEditorState> ();
		
		// Set some editor properties
		string[] folders = path.Split (new char[] {'/'}, StringSplitOptions.None);
		openedCanvas = folders [folders.Length-1];
		openedCanvasPath = path;
		
		NodeEditor.RecalculateAll (mainNodeCanvas);
		Repaint ();
	}

	/// <summary>
	/// Creates and opens a new empty node canvas
	/// </summary>
	public void NewNodeCanvas () 
	{
		// New NodeCanvas
		mainNodeCanvas = CreateInstance<NodeCanvas> ();
		mainNodeCanvas.nodes = new List<Node> ();
		// New NodeEditorState
		mainEditorState = CreateInstance<NodeEditorState> ();
		mainEditorState.canvas = mainNodeCanvas;
		mainEditorState.name = "MainEditorState";
		// Set some properties
		openedCanvas = "New Canvas";
		openedCanvasPath = "";
	}
	
	#endregion
}