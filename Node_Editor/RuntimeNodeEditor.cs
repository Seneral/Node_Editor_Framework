using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using NodeEditorFramework;
using NodeEditorFramework.Utilities;

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
		NodeEditor.Update ();
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
			catch (Exception e)
			{ // on exceptions in drawing flush the canvas to avoid locking the ui.
				NewNodeCanvas ();
				NodeEditor.ReInit (true);
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

		GUILayout.Label (new GUIContent ("Node Editor (" + canvas.name + ")"), NodeEditorGUI.nodeLabelBold);

		#if UNITY_EDITOR
		if (GUILayout.Button (new GUIContent ("Save Canvas", "Saves the Canvas to a Canvas Save File in the Assets Folder")))
		{
			string path = UnityEditor.EditorUtility.SaveFilePanelInProject ("Save Node Canvas", "Node Canvas", "asset", "", NodeEditor.editorPath + "Resources/Saves/");
			if (!string.IsNullOrEmpty (path))
				NodeEditorSaveManager.SaveNodeCanvas (path, true, canvas, state);
		}

		if (GUILayout.Button (new GUIContent ("Load Canvas", "Loads the Canvas from a Canvas Save File in the Assets Folder"))) 
		{
			string path = UnityEditor.EditorUtility.OpenFilePanel ("Load Node Canvas", NodeEditor.editorPath + "Resources/Saves/", "asset");
			if (!path.Contains (Application.dataPath)) 
			{
				if (!string.IsNullOrEmpty (path))
					Debug.LogWarning ("You should select an asset inside your project folder!");
			}
			else
			{
				path = path.Replace (Application.dataPath, "Assets");
				LoadNodeCanvas (path);
			}
		}

		#endif

		if (GUILayout.Button (new GUIContent ("New Canvas", "Loads an empty Canvas")))
			NewNodeCanvas ();

		if (GUILayout.Button (new GUIContent ("Recalculate All", "Initiates complete recalculate. Usually does not need to be triggered manually.")))
			NodeEditor.RecalculateAll (canvas);

		if (GUILayout.Button ("Force Re-Init"))
			NodeEditor.ReInit (true);

		if (NodeEditor.isTransitioning (canvas) && GUILayout.Button ("Stop Transitioning"))
			NodeEditor.StopTransitioning (canvas);

		NodeEditorGUI.knobSize = RTEditorGUI.IntSlider (new GUIContent ("Handle Size", "The size of the Node Input/Output handles"), NodeEditorGUI.knobSize, 12, 20);
		state.zoom = RTEditorGUI.Slider (new GUIContent ("Zoom", "Use the Mousewheel. Seriously."), state.zoom, 0.6f, 2);

		GUILayout.EndArea ();
	}

	public void LoadNodeCanvas (string path) 
	{
		// Else it will be stuck forever
		NodeEditor.StopTransitioning (canvas);

		// Load the NodeCanvas
		canvas = NodeEditorSaveManager.LoadNodeCanvas (path);
		if (canvas == null)
		{
			NewNodeCanvas ();
			return;
		}

		// Load the associated MainEditorState
		List<NodeEditorState> editorStates = NodeEditorSaveManager.LoadEditorStates (path);
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
		// Else it will be stuck forever
		NodeEditor.StopTransitioning (canvas);

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
