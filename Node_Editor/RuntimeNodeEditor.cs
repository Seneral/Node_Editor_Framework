using UnityEngine;
using System.Collections.Generic;
using NodeEditorFramework;

public class RuntimeNodeEditor : MonoBehaviour 
{
	public string CanvasPath;
	public NodeCanvas Canvas;
	private NodeEditorState state;

	public bool ScreenSize;
	private Rect canvasRect;
	public Rect SpecifiedRootRect;
	public Rect SpecifiedCanvasRect;

	public void Start () 
	{
		if (!string.IsNullOrEmpty (CanvasPath))
			LoadNodeCanvas (CanvasPath);
		else
			NewNodeCanvas ();
		NodeEditor.Initiated = false;
		FPSCounter.Create ();
	}

	public void Update () 
	{
		FPSCounter.Update ();
	}

	public void OnGUI ()
	{
		if (Canvas != null && state != null) 
		{
			NodeEditor.CheckInit ();
			if (NodeEditor.InitiationError) 
			{
				GUILayout.Label ("Initiation failed! Check console for more information!");
				return;
			}

			try
			{
				if (!ScreenSize && SpecifiedRootRect.max != SpecifiedRootRect.min) GUI.BeginGroup (SpecifiedRootRect, NodeEditorGUI.NodeSkin.box);

				canvasRect = ScreenSize? new Rect (0, 0, Screen.width, Screen.height) : SpecifiedCanvasRect;
				canvasRect.width -= 200;
				state.CanvasRect = canvasRect;
				NodeEditor.DrawCanvas (Canvas, state);
				SideGUI ();

				if (!ScreenSize && SpecifiedRootRect.max != SpecifiedRootRect.min) GUI.EndGroup ();
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
		GUILayout.BeginArea (new Rect (canvasRect.x + state.CanvasRect.width, state.CanvasRect.y, 200, state.CanvasRect.height), NodeEditorGUI.NodeSkin.box);
		GUILayout.Label (new GUIContent ("Node Editor (" + Canvas.name + ")", "The currently opened canvas in the Node Editor"));
		ScreenSize = GUILayout.Toggle (ScreenSize, "Adapt to Screen");
		GUILayout.Label ("FPS: " + FPSCounter.CurrentFPS);
		GUILayout.EndArea ();
	}

	public void LoadNodeCanvas (string path) 
	{
		// Load the NodeCanvas
		Canvas = NodeEditor.LoadNodeCanvas (path);
		if (Canvas == null)
		{
			Canvas = ScriptableObject.CreateInstance<NodeCanvas> ();
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
		state.Canvas = Canvas;
		
		NodeEditor.RecalculateAll (Canvas);
	}

	public void NewNodeCanvas () 
	{
		Canvas = ScriptableObject.CreateInstance<NodeCanvas> ();
		state = ScriptableObject.CreateInstance<NodeEditorState> ();
		state.Canvas = Canvas;
		state.name = "MainEditorState";
	}
}


public class FPSCounter
{
	public static float FPSMeasurePeriod = 0.1f;
	private int fpsAccumulator;
	private float fpsNextPeriod;
	public static int CurrentFPS;

	private static FPSCounter instance;

	public static void Create ()
	{
		if (instance == null)
		{
		    instance = new FPSCounter {fpsNextPeriod = Time.realtimeSinceStartup + FPSMeasurePeriod};
		}
	}

	// has to be called
	public static void Update ()
	{
		Create ();
		instance.fpsAccumulator++;
		if (Time.realtimeSinceStartup > instance.fpsNextPeriod)
		{
			CurrentFPS = (int) (instance.fpsAccumulator/FPSMeasurePeriod);
			instance.fpsAccumulator = 0;
			instance.fpsNextPeriod += FPSMeasurePeriod;
		}
	}
}
