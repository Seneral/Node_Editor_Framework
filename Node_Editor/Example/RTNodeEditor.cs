using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using NodeEditorFramework;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework.Standard
{
	/// <summary>
	/// Example of displaying the Node Editor at runtime including GUI
	/// </summary>
	public class RTNodeEditor : MonoBehaviour 
	{
		public NodeCanvas canvas;

		public NodeEditorUserCache cache = new NodeEditorUserCache ();

		public bool screenSize = false;
		private Rect canvasRect;
		public Rect specifiedRootRect = new Rect (0, 0, 1000, 500);
		public Rect specifiedCanvasRect = new Rect (50, 50, 900, 400);

		// GUI
		private string sceneCanvasName = "";
		private Rect loadScenePos;
		private Rect createCanvasPos;
		private Rect screenRect { get { return new Rect (0, 0, Screen.width, Screen.height); } }

		public void Start () 
		{
			NodeEditor.checkInit (false);
			FPSCounter.Create ();

			cache = new NodeEditorUserCache ();
			if (canvas != null)
				cache.SetCanvas (NodeEditorSaveManager.CreateWorkingCopy (canvas, true));
		}

		public void Update () 
		{
			NodeEditor.Update ();
			FPSCounter.Update ();
		}

		#region GUI

		public void OnGUI ()
		{
			cache.AssureCanvas ();
			NodeEditor.checkInit (true);
			if (NodeEditor.InitiationError) 
			{
				GUILayout.Label ("Initiation failed! Check console for more information!");
				return;
			}

			try
			{
				GUI.BeginGroup (screenSize? screenRect : specifiedRootRect, NodeEditorGUI.nodeSkin.box);
				NodeEditorGUI.StartNodeGUI ();

				canvasRect = screenSize? screenRect : specifiedCanvasRect;
				canvasRect.width -= 200;
				cache.editorState.canvasRect = canvasRect;
				NodeEditor.DrawCanvas (cache.nodeCanvas, cache.editorState);

				GUILayout.BeginArea (new Rect (canvasRect.x + cache.editorState.canvasRect.width, cache.editorState.canvasRect.y, 200, cache.editorState.canvasRect.height), NodeEditorGUI.nodeSkin.box);
				SideGUI ();
				GUILayout.EndArea ();

				NodeEditorGUI.EndNodeGUI ();
				GUI.EndGroup ();
			}
			catch (UnityException e)
			{ // on exceptions in drawing flush the canvas to avoid locking the ui.
				cache.NewNodeCanvas ();
				NodeEditor.ReInit (true);
				Debug.LogError ("Unloaded Canvas due to exception in Draw!");
				Debug.LogException (e);
			}
		}

		public void SideGUI () 
		{
			screenSize = GUILayout.Toggle (screenSize, "Adapt to Screen");
			GUILayout.Label ("FPS: " + FPSCounter.currentFPS);

			GUILayout.Label (new GUIContent ("Node Editor (" + cache.nodeCanvas.name + ")"), NodeEditorGUI.nodeLabelBold);

			if (GUILayout.Button(new GUIContent("New Canvas", "Loads an Specified Empty CanvasType")))
			{
				NodeEditorFramework.Utilities.GenericMenu menu = new NodeEditorFramework.Utilities.GenericMenu();
				NodeCanvasManager.FillCanvasTypeMenu(ref menu, cache.NewNodeCanvas);
				menu.Show(createCanvasPos.position, createCanvasPos.width);
			}
			if (Event.current.type == EventType.Repaint)
			{
				Rect popupPos = GUILayoutUtility.GetLastRect();
				createCanvasPos = new Rect(popupPos.x + 2, popupPos.yMax + 2, popupPos.width - 4, 0);
			}

			GUILayout.Space (6);

		#if UNITY_EDITOR
			if (GUILayout.Button (new GUIContent ("Save Canvas", "Saves the Canvas to a Canvas Save File in the Assets Folder")))
			{
				string path = UnityEditor.EditorUtility.SaveFilePanelInProject ("Save Node Canvas", "Node Canvas", "asset", "", NodeEditor.editorPath + "Resources/Saves/");
				if (!string.IsNullOrEmpty (path))
					NodeEditorSaveManager.SaveNodeCanvas (path, cache.nodeCanvas, true);
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
					cache.LoadNodeCanvas (path);
				}
			}
			GUILayout.Space (6);
		#endif

			GUILayout.BeginHorizontal ();
			sceneCanvasName = GUILayout.TextField (sceneCanvasName, GUILayout.ExpandWidth (true));
			if (GUILayout.Button (new GUIContent ("Save to Scene", "Saves the Canvas to the Scene"), GUILayout.ExpandWidth (false)))
			{
				cache.SaveSceneNodeCanvas (sceneCanvasName);
			}
			GUILayout.EndHorizontal ();

			if (GUILayout.Button (new GUIContent ("Load from Scene", "Loads the Canvas from the Scene"))) 
			{
				NodeEditorFramework.Utilities.GenericMenu menu = new NodeEditorFramework.Utilities.GenericMenu ();
				foreach (string sceneSave in NodeEditorSaveManager.GetSceneSaves ())
					menu.AddItem (new GUIContent (sceneSave), false, LoadSceneCanvasCallback, (object)sceneSave);
				menu.Show (loadScenePos.position, loadScenePos.width);
			}
			if (Event.current.type == EventType.Repaint)
			{
				Rect popupPos = GUILayoutUtility.GetLastRect ();
				loadScenePos = new Rect (popupPos.x+2, popupPos.yMax+2, popupPos.width-4, 0);
			}

			GUILayout.Space (6);

			if (GUILayout.Button (new GUIContent ("Recalculate All", "Initiates complete recalculate. Usually does not need to be triggered manually.")))
				NodeEditor.Calculator.RecalculateAll (cache.nodeCanvas);

			if (GUILayout.Button ("Force Re-Init"))
				NodeEditor.ReInit (true);

			NodeEditorGUI.knobSize = RTEditorGUI.IntSlider (new GUIContent ("Handle Size", "The size of the Node Input/Output handles"), NodeEditorGUI.knobSize, 12, 20);
			cache.editorState.zoom = RTEditorGUI.Slider (new GUIContent ("Zoom", "Use the Mousewheel. Seriously."), cache.editorState.zoom, 0.6f, 2);
		}

		private void LoadSceneCanvasCallback (object save)
		{
			cache.LoadSceneNodeCanvas ((string)save);
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
