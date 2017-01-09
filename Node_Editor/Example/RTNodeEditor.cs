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
		private Rect loadSceneUIPos, createCanvasUIPos, convertCanvasUIPos;
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
				NodeEditorGUI.StartNodeGUI ("RTNodeEditor", false);

				canvasRect = screenSize? screenRect : specifiedCanvasRect;
				canvasRect.width -= 300;
				cache.editorState.canvasRect = canvasRect;
				NodeEditor.DrawCanvas (cache.nodeCanvas, cache.editorState);

				GUILayout.BeginArea (new Rect (canvasRect.x + cache.editorState.canvasRect.width, cache.editorState.canvasRect.y, 300, cache.editorState.canvasRect.height), NodeEditorGUI.nodeSkin.box);
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
			GUILayout.Label (new GUIContent ("" + cache.nodeCanvas.saveName + " (" + (cache.nodeCanvas.livesInScene? "Scene Save" : "Asset Save") + ")", "Opened Canvas path: " + cache.nodeCanvas.savePath), NodeEditorGUI.nodeLabelBold);
			GUILayout.Label ("Type: " + cache.typeData.DisplayString + "/" + cache.nodeCanvas.GetType ().Name + "");




			if (GUILayout.Button(new GUIContent("New Canvas", "Loads an Specified Empty CanvasType")))
			{
				NodeEditorFramework.Utilities.GenericMenu menu = new NodeEditorFramework.Utilities.GenericMenu();
				NodeCanvasManager.FillCanvasTypeMenu(ref menu, cache.NewNodeCanvas);
				menu.Show(createCanvasUIPos.position, createCanvasUIPos.width);
			}
			if (Event.current.type == EventType.Repaint)
			{
				Rect popupPos = GUILayoutUtility.GetLastRect();
				createCanvasUIPos = new Rect(popupPos.x + 2, popupPos.yMax + 2, popupPos.width - 4, 0);
			}
			if (cache.nodeCanvas.GetType () == typeof(NodeCanvas) && GUILayout.Button(new GUIContent("Convert Canvas", "Converts the current canvas to a new type.")))
			{
				NodeEditorFramework.Utilities.GenericMenu menu = new NodeEditorFramework.Utilities.GenericMenu();
				NodeCanvasManager.FillCanvasTypeMenu(ref menu, cache.ConvertCanvasType);
				menu.Show(convertCanvasUIPos.position, convertCanvasUIPos.width);
			}
			if (Event.current.type == EventType.Repaint)
			{
				Rect popupPos = GUILayoutUtility.GetLastRect();
				convertCanvasUIPos = new Rect(popupPos.x + 2, popupPos.yMax + 2, popupPos.width - 4, 0);
			}

			if (GUILayout.Button(new GUIContent("Save Canvas", "Save the Canvas to the load location")))
			{
				string path = cache.nodeCanvas.savePath;
				if (!string.IsNullOrEmpty (path))
				{
					if (path.StartsWith ("SCENE/"))
						cache.SaveSceneNodeCanvas (path.Substring (6));
					else
						cache.SaveNodeCanvas (path);
				}
			}



		#if UNITY_EDITOR
			GUILayout.Label ("Asset Saving", NodeEditorGUI.nodeLabel);

			if (GUILayout.Button(new GUIContent("Save Canvas As", "Save the canvas as an asset")))
			{
				string panelPath = NodeEditor.editorPath + "Resources/Saves/";
				if (cache.nodeCanvas != null && !string.IsNullOrEmpty(cache.nodeCanvas.savePath))
					panelPath = cache.nodeCanvas.savePath;
				string path = UnityEditor.EditorUtility.SaveFilePanelInProject ("Save Node Canvas", "Node Canvas", "asset", "", panelPath);
				if (!string.IsNullOrEmpty (path))
					cache.SaveNodeCanvas (path);
			}

			if (GUILayout.Button(new GUIContent("Load Canvas", "Load the Canvas from an asset")))
			{
				string panelPath = NodeEditor.editorPath + "Resources/Saves/";
				if (cache.nodeCanvas != null && !string.IsNullOrEmpty(cache.nodeCanvas.savePath))
					panelPath = cache.nodeCanvas.savePath;
				string path = UnityEditor.EditorUtility.OpenFilePanel("Load Node Canvas", panelPath, "asset");
				if (!path.Contains(Application.dataPath))
				{
					if (!string.IsNullOrEmpty(path))
						Debug.LogWarning (new GUIContent("You should select an asset inside your project folder!"));
				}
				else
					cache.LoadNodeCanvas (path);
				if (cache.nodeCanvas.GetType () == typeof(NodeCanvas))
					Debug.LogWarning (new GUIContent("The Canvas has no specific type. Please use the convert button to assign a type and re-save the canvas!"));
			}
		#endif

			GUILayout.Label ("Scene Saving", NodeEditorGUI.nodeLabel);

			GUILayout.BeginHorizontal ();
			sceneCanvasName = GUILayout.TextField (sceneCanvasName, GUILayout.ExpandWidth (true));
			if (GUILayout.Button (new GUIContent ("Save to Scene", "Save the canvas to the Scene"), GUILayout.ExpandWidth (false)))
				cache.SaveSceneNodeCanvas (sceneCanvasName);
			GUILayout.EndHorizontal ();

			if (GUILayout.Button (new GUIContent ("Load from Scene", "Load the canvas from the Scene"))) 
			{
				NodeEditorFramework.Utilities.GenericMenu menu = new NodeEditorFramework.Utilities.GenericMenu();
				foreach (string sceneSave in NodeEditorSaveManager.GetSceneSaves())
					menu.AddItem(new GUIContent(sceneSave), false, LoadSceneCanvasCallback, (object)sceneSave);
				menu.Show (loadSceneUIPos.position, loadSceneUIPos.width);
			}
			if (Event.current.type == EventType.Repaint)
			{
				Rect popupPos = GUILayoutUtility.GetLastRect ();
				loadSceneUIPos = new Rect (popupPos.x+2, popupPos.yMax+2, popupPos.width-4, 0);
			}



			GUILayout.Label ("Utility/Debug", NodeEditorGUI.nodeLabel);

			if (GUILayout.Button (new GUIContent ("Recalculate All", "Initiates complete recalculate. Usually does not need to be triggered manually.")))
				cache.nodeCanvas.TraverseAll ();

			if (GUILayout.Button ("Force Re-Init"))
				NodeEditor.ReInit (true);

			NodeEditorGUI.knobSize = RTEditorGUI.IntSlider (new GUIContent ("Handle Size", "The size of the Node Input/Output handles"), NodeEditorGUI.knobSize, 12, 20);
			//cache.editorState.zoom = EditorGUILayout.Slider (new GUIContent ("Zoom", "Use the Mousewheel. Seriously."), cache.editorState.zoom, 0.6f, 4);
			NodeEditorUserCache.cacheIntervalSec = RTEditorGUI.IntSlider (new GUIContent ("Cache Interval (Sec)", "The interval in seconds the canvas is temporarily saved into the cache as a precaution for crashes."), NodeEditorUserCache.cacheIntervalSec, 30, 300);

			screenSize = GUILayout.Toggle (screenSize, "Adapt to Screen");
			GUILayout.Label ("FPS: " + FPSCounter.currentFPS);




			if (cache.editorState.selectedNode != null && Event.current.type != EventType.Ignore)
				cache.editorState.selectedNode.DrawNodePropertyEditor();
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
