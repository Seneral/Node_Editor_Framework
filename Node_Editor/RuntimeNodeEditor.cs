using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using NodeEditorFramework;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework.Standard
{
	public class RuntimeNodeEditor : MonoBehaviour 
	{
		public string canvasPath;
		public NodeCanvas canvas;
		private NodeEditorState state;

		public bool screenSize = false;
		private Rect canvasRect;
		public Rect specifiedRootRect;
		public Rect specifiedCanvasRect;

		// GUI
		private string sceneCanvasName = "";
		private Vector2 loadScenePos;

		public void Start () 
		{
			NodeEditor.checkInit (false);
			NodeEditor.initiated = false;
			LoadNodeCanvas (canvasPath);
			FPSCounter.Create ();
		}

		public void Update () 
		{
			NodeEditor.Update ();
			FPSCounter.Update ();
		}

		#region GUI

		public void OnGUI ()
		{
			if (canvas != null) 
			{
				if (state == null)
					NewEditorState ();
				NodeEditor.checkInit (true);
				if (NodeEditor.InitiationError) 
				{
					GUILayout.Label ("Initiation failed! Check console for more information!");
					return;
				}

				try
				{
					if (!screenSize && specifiedRootRect.max != specifiedRootRect.min) GUI.BeginGroup (specifiedRootRect, NodeEditorGUI.nodeSkin.box);

					NodeEditorGUI.StartNodeGUI ();

					canvasRect = screenSize? new Rect (0, 0, Screen.width, Screen.height) : specifiedCanvasRect;
					canvasRect.width -= 200;
					state.canvasRect = canvasRect;
					NodeEditor.DrawCanvas (canvas, state);

					GUILayout.BeginArea (new Rect (canvasRect.x + state.canvasRect.width, state.canvasRect.y, 200, state.canvasRect.height), NodeEditorGUI.nodeSkin.box);
					SideGUI ();
					GUILayout.EndArea ();

					NodeEditorGUI.EndNodeGUI ();

					if (!screenSize && specifiedRootRect.max != specifiedRootRect.min) GUI.EndGroup ();
				}
				catch (UnityException e)
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
			GUILayout.Label (new GUIContent ("Node Editor (" + canvas.name + ")", "The currently opened canvas in the Node Editor"));
			screenSize = GUILayout.Toggle (screenSize, "Adapt to Screen");
			GUILayout.Label ("FPS: " + FPSCounter.currentFPS);

			GUILayout.Label (new GUIContent ("Node Editor (" + canvas.name + ")"), NodeEditorGUI.nodeLabelBold);

			if (GUILayout.Button (new GUIContent ("New Canvas", "Loads an empty Canvas")))
				NewNodeCanvas ();

			GUILayout.Space (6);

		#if UNITY_EDITOR
			if (GUILayout.Button (new GUIContent ("Save Canvas", "Saves the Canvas to a Canvas Save File in the Assets Folder")))
			{
				string path = UnityEditor.EditorUtility.SaveFilePanelInProject ("Save Node Canvas", "Node Canvas", "asset", "", NodeEditor.editorPath + "Resources/Saves/");
				if (!string.IsNullOrEmpty (path))
					NodeEditorSaveManager.SaveNodeCanvas (path, canvas, true);
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
			GUILayout.Space (6);
		#endif

			GUILayout.BeginHorizontal ();
			sceneCanvasName = GUILayout.TextField (sceneCanvasName, GUILayout.ExpandWidth (true));
			if (GUILayout.Button (new GUIContent ("Save to Scene", "Saves the Canvas to the Scene"), GUILayout.ExpandWidth (false)))
			{
				SaveSceneNodeCanvas (sceneCanvasName);
			}
			GUILayout.EndHorizontal ();

			if (GUILayout.Button (new GUIContent ("Load from Scene", "Loads the Canvas from the Scene"))) 
			{
				NodeEditorFramework.Utilities.GenericMenu menu = new NodeEditorFramework.Utilities.GenericMenu ();
				foreach (string sceneSave in NodeEditorSaveManager.GetSceneSaves ())
					menu.AddItem (new GUIContent (sceneSave), false, LoadSceneCanvasCallback, (object)sceneSave);
				menu.Show (loadScenePos);
			}
			if (Event.current.type == EventType.Repaint)
			{
				Rect popupPos = GUILayoutUtility.GetLastRect ();
				loadScenePos = new Vector2 (popupPos.x+2, popupPos.yMax+2);
			}

			GUILayout.Space (6);

			if (GUILayout.Button (new GUIContent ("Recalculate All", "Initiates complete recalculate. Usually does not need to be triggered manually.")))
				NodeEditor.RecalculateAll (canvas);

			if (GUILayout.Button ("Force Re-Init"))
				NodeEditor.ReInit (true);

			NodeEditorGUI.knobSize = RTEditorGUI.IntSlider (new GUIContent ("Handle Size", "The size of the Node Input/Output handles"), NodeEditorGUI.knobSize, 12, 20);
			state.zoom = RTEditorGUI.Slider (new GUIContent ("Zoom", "Use the Mousewheel. Seriously."), state.zoom, 0.6f, 2);
		}

		#endregion

		#region Canvas management

		private void LoadSceneCanvasCallback (object save)
		{
			LoadSceneNodeCanvas ((string)save);
		}

		public void SaveSceneNodeCanvas (string path) 
		{
			canvas.editorStates = new NodeEditorState[] { state };
			NodeEditorSaveManager.SaveSceneNodeCanvas (path, ref canvas, true);
		}

		public void LoadSceneNodeCanvas (string path) 
		{
			// Try to load the NodeCanvas
			if ((canvas = NodeEditorSaveManager.LoadSceneNodeCanvas (path, true)) == null)
			{
				NewNodeCanvas ();
				return;
			}
			state = NodeEditorSaveManager.ExtractEditorState (canvas, "MainEditorState");

			NodeEditor.RecalculateAll (canvas);
		}

		public void LoadNodeCanvas (string path) 
		{
			if (!File.Exists (path) || (canvas = NodeEditorSaveManager.LoadNodeCanvas (path, true)) == null)
			{
				NewNodeCanvas ();
				return;
			}
			state = NodeEditorSaveManager.ExtractEditorState (canvas, "MainEditorState");
			NodeEditor.RecalculateAll (canvas);
		}

		public void NewNodeCanvas () 
		{
			canvas = ScriptableObject.CreateInstance<NodeCanvas> ();
			canvas.name = "New Canvas";
			NewEditorState ();
		}

		private void NewEditorState () 
		{
			state = ScriptableObject.CreateInstance<NodeEditorState> ();
			state.canvas = canvas;
			state.name = "MainEditorState";
			canvas.editorStates = new NodeEditorState[] { state };
		}

		#endregion
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
