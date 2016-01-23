using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using NodeEditorFramework;

public class RuntimeNodeEditor : MonoBehaviour 
{
	public string canvasPath;
	public NodeCanvas canvas;
	private NodeEditorState state;

	public bool screenSize = false;
	private Rect canvasRect;
	public Rect specifiedRootRect;
	public Rect specifiedCanvasRect;

	public void Start () 
	{
		if (!string.IsNullOrEmpty (canvasPath))
			LoadNodeCanvas (canvasPath);
		else
			NewNodeCanvas ();
		NodeEditor.initiated = false;
		FPSCounter.Create ();
	}

	public void Update () 
	{
		FPSCounter.Update ();
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
				if (!screenSize && specifiedRootRect.max != specifiedRootRect.min) GUI.BeginGroup (specifiedRootRect, NodeEditorGUI.nodeSkin.box);

				canvasRect = screenSize? new Rect (0, 0, Screen.width, Screen.height) : specifiedCanvasRect;
				canvasRect.width -= 200;
				state.canvasRect = canvasRect;
				NodeEditor.DrawCanvas (canvas, state);
				SideGUI ();

				if (!screenSize && specifiedRootRect.max != specifiedRootRect.min) GUI.EndGroup ();
			}
			catch (UnityException e)
			{ // on exceptions in drawing flush the canvas to avoid locking the ui.
				NewNodeCanvas ();
				Debug.LogError ("Unloaded Canvas due to exception in Draw!");
				Debug.LogException (e);
			}
		}
	}

	public void SideGUI () 
	{
		GUILayout.BeginArea (new Rect (canvasRect.x + state.canvasRect.width, state.canvasRect.y, 200, state.canvasRect.height), NodeEditorGUI.nodeSkin.box);
		GUILayout.Label (new GUIContent ("Node Editor (" + canvas.name + ")", "The currently opened canvas in the Node Editor"));
		screenSize = GUILayout.Toggle (screenSize, "Adapt to Screen");
		GUILayout.Label ("FPS: " + FPSCounter.currentFPS);
		GUILayout.EndArea ();
	}

	public void LoadNodeCanvas (string path) 
	{
		// Load the NodeCanvas
		canvas = NodeEditor.LoadNodeCanvas (path);
		if (canvas == null)
		{
			canvas = ScriptableObject.CreateInstance<NodeCanvas> ();
		}

		// Load the associated MainEditorState
		List<NodeEditorState> editorStates = NodeEditor.LoadEditorStates (path);
		if (editorStates.Count == 0)
			state = ScriptableObject.CreateInstance<NodeEditorState> ();
		else 
		{
			state = editorStates.Find (x => x.name == "MainEditorState");
			if (state == null) state = editorStates[0];
		}
		state.canvas = canvas;
		
		NodeEditor.RecalculateAll (canvas);
	}

	public void NewNodeCanvas () 
	{
		canvas = ScriptableObject.CreateInstance<NodeCanvas> ();
		state = ScriptableObject.CreateInstance<NodeEditorState> ();
		state.canvas = canvas;
		state.name = "MainEditorState";
	}
}


public class FPSCounter
{
	public static float FPSMeasurePeriod = 0.1f;
	private int FPSAccumulator;
	private float FPSNextPeriod;
	public static int currentFPS;

	private static FPSCounter instance;

	public static void Create ()
	{
		if (instance == null)
		{
			instance = new FPSCounter ();
			instance.FPSNextPeriod = Time.realtimeSinceStartup + FPSMeasurePeriod;
		}
	}

	// has to be called
	public static void Update ()
	{
		Create ();
		instance.FPSAccumulator++;
		if (Time.realtimeSinceStartup > instance.FPSNextPeriod)
		{
			currentFPS = (int) (instance.FPSAccumulator/FPSMeasurePeriod);
			instance.FPSAccumulator = 0;
			instance.FPSNextPeriod += FPSMeasurePeriod;
		}
	}
}
