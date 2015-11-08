using UnityEngine;
using System;
using System.Collections.Generic;
using NodeEditorFramework;

public class RuntimeNodeEditor : MonoBehaviour 
{
	public string CanvasString;
	public NodeCanvas canvas;
	public NodeEditorState state;

	public Rect rootRect;
	public Rect subRootRect;
	public Rect canvasRect;

	public void Start () 
	{
		//rootRect = new Rect (0, 0, Screen.width, Screen.height);
		//canvasRect = new Rect (0, 0, Screen.width, Screen.height);

		if ((canvas == null || state == null))
		{
			if (!string.IsNullOrEmpty (CanvasString))
				LoadNodeCanvas (CanvasString);
			else
				Debug.LogWarning ("Please use one option to select a canvas!");
		}
		else
			NodeEditor.RecalculateAll (canvas);

		NodeEditor.initiated = false;
	}

	public void OnGUI ()
	{
		if (canvas != null && state != null) 
		{
			NodeEditor.checkInit ();
			if (NodeEditor.InitiationError) 
			{
				GUILayout.Label ("Initiation failed! Check console for more information!");
				return;
			}

			try
			{
				//GUI.BeginGroup (rootRect, NodeEditorGUI.nodeSkin.box);

				//GUILayout.FlexibleSpace ();

				//GUI.BeginGroup (subRootRect, NodeEditorGUI.nodeSkin.box);

				state.canvasRect = new Rect (0, 0, Screen.width, Screen.height);
				NodeEditor.DrawCanvas (canvas, state);

				//GUI.EndGroup ();

				//GUI.EndGroup ();
			}
			catch (UnityException e)
			{ // on exceptions in drawing flush the canvas to avoid locking the ui.
				NewNodeCanvas ();
				Debug.LogError ("Unloaded Canvas due to exception in Draw!");
				Debug.LogException (e);
			}
		}
	}

	public void LoadNodeCanvas (string path) 
	{
		// Load the NodeCanvas
		canvas = NodeEditor.LoadNodeCanvas (path);
		if (canvas == null)
			canvas = ScriptableObject.CreateInstance<NodeCanvas> ();
		
		// Load the associated MainEditorState
		List<NodeEditorState> editorStates = NodeEditor.LoadEditorStates (path);
		if (editorStates.Count == 0)
			state = ScriptableObject.CreateInstance<NodeEditorState> ();
		else 
		{
			state = editorStates.Find (x => x.name == "MainEditorState");
			if (state == null)
				state = editorStates[0];
		}
		
		NodeEditor.RecalculateAll (canvas);
	}

	public void NewNodeCanvas () 
	{
		// New NodeCanvas
		canvas = ScriptableObject.CreateInstance<NodeCanvas> ();;
		// New NodeEditorState
		state = ScriptableObject.CreateInstance<NodeEditorState> ();
		state.canvas = canvas;
		state.name = "MainEditorState";
	}
}
