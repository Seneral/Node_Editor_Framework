using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

using NodeEditorFramework;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework
{
	public class NodeEditorWindow : EditorWindow 
	{
		// Information about current instance
		private static NodeEditorWindow _editor;
		public static NodeEditorWindow editor { get { AssureEditor (); return _editor; } }
		public static void AssureEditor () { if (_editor == null) CreateEditor (); }

		// Opened Canvas
		public NodeCanvas mainNodeCanvas;
		public NodeEditorState mainEditorState;
		public static NodeCanvas MainNodeCanvas { get { return editor.mainNodeCanvas; } }
		public static NodeEditorState MainEditorState { get { return editor.mainEditorState; } }
		public void AssureCanvas () { if (mainNodeCanvas == null) NewNodeCanvas (); }
		public static string openedCanvasPath;
		public string tempSessionPath;

		// GUI
		public static int sideWindowWidth = 400;
		private static Texture iconTexture;
		public Rect sideWindowRect { get { return new Rect (position.width - sideWindowWidth, 0, sideWindowWidth, position.height); } }
		public Rect canvasWindowRect { get { return new Rect (0, 0, position.width - sideWindowWidth, position.height); } }

		#region General 

		[MenuItem ("Window/Node Editor")]
		public static void CreateEditor () 
		{
			_editor = GetWindow<NodeEditorWindow> ();
			_editor.minSize = new Vector2 (800, 600);
			NodeEditor.ClientRepaints += _editor.Repaint;
			NodeEditor.initiated = NodeEditor.InitiationError = false;

			iconTexture = ResourceManager.LoadTexture (EditorGUIUtility.isProSkin? "Textures/Icon_Dark.png" : "Textures/Icon_Light.png");
			_editor.titleContent = new GUIContent ("Node Editor", iconTexture);
		}
		
		/// <summary>
		/// Handle opening canvas when double-clicking asset
		/// </summary>
		[UnityEditor.Callbacks.OnOpenAsset(1)]
		public static bool AutoOpenCanvas (int instanceID, int line) 
		{
			if (Selection.activeObject != null && Selection.activeObject.GetType () == typeof(NodeCanvas))
			{
				string NodeCanvasPath = AssetDatabase.GetAssetPath (instanceID);
				NodeEditorWindow.CreateEditor ();
				EditorWindow.GetWindow<NodeEditorWindow> ().LoadNodeCanvas (NodeCanvasPath);
				return true;
			}
			return false;
		}

		public void OnDestroy () 
		{
			NodeEditor.ClientRepaints -= _editor.Repaint;
			//SaveCache ();

	#if UNITY_EDITOR
			// Remove callbacks
			//EditorLoadingControl.beforeEnteringPlayMode -= SaveCache;
			EditorLoadingControl.lateEnteredPlayMode -= LoadCache;
			//EditorLoadingControl.beforeLeavingPlayMode -= SaveCache;
			EditorLoadingControl.justLeftPlayMode -= LoadCache;
			EditorLoadingControl.justOpenedNewScene -= LoadCache;

			NodeEditorCallbacks.OnAddNode -= SaveNewNode;
			NodeEditorCallbacks.OnAddTransition -= SaveNewTransition;

			// TODO: BeforeOpenedScene to save Cache, aswell as assembly reloads... 
	#endif
		}

		// Following section is all about caching the last editor session

		private void OnEnable () 
		{
			tempSessionPath = Path.GetDirectoryName (AssetDatabase.GetAssetPath (MonoScript.FromScriptableObject (this)));
			LoadCache ();

	#if UNITY_EDITOR
			// This makes sure the Node Editor is reinitiated after the Playmode changed
			//EditorLoadingControl.beforeEnteringPlayMode -= SaveCache;
			//EditorLoadingControl.beforeEnteringPlayMode += SaveCache;
			EditorLoadingControl.lateEnteredPlayMode -= LoadCache;
			EditorLoadingControl.lateEnteredPlayMode += LoadCache;

			//EditorLoadingControl.beforeLeavingPlayMode -= SaveCache;
			//EditorLoadingControl.beforeLeavingPlayMode += SaveCache;
			EditorLoadingControl.justLeftPlayMode -= LoadCache;
			EditorLoadingControl.justLeftPlayMode += LoadCache;

			EditorLoadingControl.justOpenedNewScene -= LoadCache;
			EditorLoadingControl.justOpenedNewScene += LoadCache;

			NodeEditorCallbacks.OnAddNode -= SaveNewNode;
			NodeEditorCallbacks.OnAddNode += SaveNewNode;
			NodeEditorCallbacks.OnAddTransition -= SaveNewTransition;
			NodeEditorCallbacks.OnAddTransition += SaveNewTransition;

			// TODO: BeforeOpenedScene to save Cache, aswell as assembly reloads... 
	#endif
		}

		#endregion

		#region GUI

		private void OnGUI () 
		{
			// Initiation
			NodeEditor.checkInit ();
			if (NodeEditor.InitiationError) 
			{
				GUILayout.Label ("Node Editor Initiation failed! Check console for more information!");
				return;
			}
			AssureEditor ();
			AssureCanvas ();

			// Specify the Canvas rect in the EditorState
			mainEditorState.canvasRect = canvasWindowRect;
			// If you want to use GetRect:
//			Rect canvasRect = GUILayoutUtility.GetRect (600, 600);
//			if (Event.current.type != EventType.Layout)
//				mainEditorState.canvasRect = canvasRect;

			// Perform drawing with error-handling
			try
			{
				NodeEditor.DrawCanvas (mainNodeCanvas, mainEditorState);
			}
			catch (Exception e)
			{ // on exceptions in drawing flush the canvas to avoid locking the ui.
				NewNodeCanvas ();
				NodeEditor.ReInit (true);
				Debug.LogError ("Unloaded Canvas due to exception when drawing!");
				Debug.LogException (e);
			}

			// Draw Side Window
			sideWindowWidth = Math.Min (600, Math.Max (200, (int)(position.width / 5)));
			NodeEditorGUI.StartNodeGUI ();
			GUILayout.BeginArea (sideWindowRect, GUI.skin.box);
			DrawSideWindow ();
			GUILayout.EndArea ();
			NodeEditorGUI.EndNodeGUI ();
		}

		private void DrawSideWindow () 
		{
			GUILayout.Label (new GUIContent ("Node Editor (" + mainNodeCanvas.name + ")", "Opened Canvas path: " + openedCanvasPath), NodeEditorGUI.nodeLabelBold);

			if (GUILayout.Button (new GUIContent ("Save Canvas", "Saves the Canvas to a Canvas Save File in the Assets Folder")))
			{
				string path = EditorUtility.SaveFilePanelInProject ("Save Node Canvas", "Node Canvas", "asset", "", NodeEditor.editorPath + "Resources/Saves/");
				if (!string.IsNullOrEmpty (path))
					SaveNodeCanvas (path);
			}

			if (GUILayout.Button (new GUIContent ("Load Canvas", "Loads the Canvas from a Canvas Save File in the Assets Folder"))) 
			{
				string path = EditorUtility.OpenFilePanel ("Load Node Canvas", NodeEditor.editorPath + "Resources/Saves/", "asset");
				if (!path.Contains (Application.dataPath)) 
				{
					if (!string.IsNullOrEmpty (path))
						ShowNotification (new GUIContent ("You should select an asset inside your project folder!"));
				}
				else
				{
					path = path.Replace (Application.dataPath, "Assets");
					LoadNodeCanvas (path);
				}
			}

			if (GUILayout.Button (new GUIContent ("New Canvas", "Loads an empty Canvas")))
				NewNodeCanvas ();

			if (GUILayout.Button (new GUIContent ("Recalculate All", "Initiates complete recalculate. Usually does not need to be triggered manually.")))
				NodeEditor.RecalculateAll (mainNodeCanvas);

			if (GUILayout.Button ("Force Re-Init"))
				NodeEditor.ReInit (true);

			if (NodeEditor.isTransitioning (mainNodeCanvas) && GUILayout.Button ("Stop Transitioning"))
				NodeEditor.StopTransitioning (mainNodeCanvas);

			NodeEditorGUI.knobSize = EditorGUILayout.IntSlider (new GUIContent ("Handle Size", "The size of the Node Input/Output handles"), NodeEditorGUI.knobSize, 12, 20);
			mainEditorState.zoom = EditorGUILayout.Slider (new GUIContent ("Zoom", "Use the Mousewheel. Seriously."), mainEditorState.zoom, 0.6f, 2);

            if (mainEditorState.selectedNode != null && Event.current.type != EventType.Ignore)
                    mainEditorState.selectedNode.DrawNodePropertyEditor();
        }

		#endregion

		#region Cache

		private void SaveNewNode (Node node) 
		{
			if (!mainNodeCanvas.nodes.Contains (node))
				throw new UnityException ("Cache system: Writing new Node to save file failed as Node is not part of the Cache!");
			string path = tempSessionPath + "/LastSession.asset";
			if (AssetDatabase.GetAssetPath (mainNodeCanvas) != path)
				throw new UnityException ("Cache system error: Current Canvas is not saved as the temporary cache!");
			NodeEditorSaveManager.AddSubAsset (node, path);
			for (int knobCnt = 0; knobCnt < node.nodeKnobs.Count; knobCnt++)
				NodeEditorSaveManager.AddSubAsset (node.nodeKnobs [knobCnt], path);
			for (int transCnt = 0; transCnt < node.transitions.Count; transCnt++)
			{
				if (node.transitions[transCnt].startNode == node)
					NodeEditorSaveManager.AddSubAsset (node.transitions [transCnt], path);
			}

			AssetDatabase.SaveAssets ();
			AssetDatabase.Refresh ();
		}

		private void SaveNewTransition (Transition transition) 
		{
			if (!mainNodeCanvas.nodes.Contains (transition.startNode) || !mainNodeCanvas.nodes.Contains (transition.endNode))
				throw new UnityException ("Cache system: Writing new Transition to save file failed as Node members are not part of the Cache!");
			string path = tempSessionPath + "/LastSession.asset";
			if (AssetDatabase.GetAssetPath (mainNodeCanvas) != path)
				throw new UnityException ("Cache system error: Current Canvas is not saved as the temporary cache!");
			NodeEditorSaveManager.AddSubAsset (transition, path);

			AssetDatabase.SaveAssets ();
			AssetDatabase.Refresh ();
		}

		private void SaveCache () 
		{
			//DeleteCache (); // Delete old cache
			string canvasName = mainNodeCanvas.name;
			EditorPrefs.SetString ("NodeEditorLastSession", canvasName);
			NodeEditorSaveManager.SaveNodeCanvas (tempSessionPath + "/LastSession.asset", false, mainNodeCanvas, mainEditorState);
			mainNodeCanvas.name = canvasName;

			AssetDatabase.SaveAssets ();
			AssetDatabase.Refresh ();
		}

		private void LoadCache () 
		{
			string lastSessionName = EditorPrefs.GetString ("NodeEditorLastSession");
			string path = tempSessionPath + "/LastSession.asset";
			mainNodeCanvas = NodeEditorSaveManager.LoadNodeCanvas (path, false);
			if (mainNodeCanvas == null)
				NewNodeCanvas ();
			else 
			{
				mainNodeCanvas.name = lastSessionName;
				List<NodeEditorState> editorStates = NodeEditorSaveManager.LoadEditorStates (path, false);
				if (editorStates == null || editorStates.Count == 0 || (mainEditorState = editorStates.Find (x => x.name == "MainEditorState")) == null )
				{ // New NodeEditorState
					mainEditorState = CreateInstance<NodeEditorState> ();
					mainEditorState.canvas = mainNodeCanvas;
					mainEditorState.name = "MainEditorState";
					NodeEditorSaveManager.AddSubAsset (mainEditorState, path);
					AssetDatabase.SaveAssets ();
					AssetDatabase.Refresh ();
				}
			}
		}

		private void DeleteCache () 
		{
			string lastSession = EditorPrefs.GetString ("NodeEditorLastSession");
			if (!String.IsNullOrEmpty (lastSession))
			{
				AssetDatabase.DeleteAsset (tempSessionPath + "/" + lastSession);
				AssetDatabase.Refresh ();
			}
			EditorPrefs.DeleteKey ("NodeEditorLastSession");
		}

		#endregion

		#region Save/Load
		
		/// <summary>
		/// Saves the mainNodeCanvas and it's associated mainEditorState as an asset at path
		/// </summary>
		public void SaveNodeCanvas (string path) 
		{
			NodeEditorSaveManager.SaveNodeCanvas (path, true, mainNodeCanvas, mainEditorState);
			//SaveCache ();
			Repaint ();
		}
		
		/// <summary>
		/// Loads the mainNodeCanvas and it's associated mainEditorState from an asset at path
		/// </summary>
		public void LoadNodeCanvas (string path) 
		{
			// Else it will be stuck forever
			NodeEditor.StopTransitioning (mainNodeCanvas);

			// Load the NodeCanvas
			mainNodeCanvas = NodeEditorSaveManager.LoadNodeCanvas (path, true);
			if (mainNodeCanvas == null) 
			{
				Debug.Log ("Error loading NodeCanvas from '" + path + "'!");
				NewNodeCanvas ();
				return;
			}
			
			// Load the associated MainEditorState
			List<NodeEditorState> editorStates = NodeEditorSaveManager.LoadEditorStates (path, true);
			if (editorStates.Count == 0) 
			{
				mainEditorState = ScriptableObject.CreateInstance<NodeEditorState> ();
				Debug.LogError ("The save file '" + path + "' did not contain an associated NodeEditorState!");
			}
			else 
			{
				mainEditorState = editorStates.Find (x => x.name == "MainEditorState");
				if (mainEditorState == null) mainEditorState = editorStates[0];
			}
			mainEditorState.canvas = mainNodeCanvas;

			openedCanvasPath = path;
			NodeEditor.RecalculateAll (mainNodeCanvas);
			SaveCache ();
			Repaint ();
		}

		/// <summary>
		/// Creates and opens a new empty node canvas
		/// </summary>
		public void NewNodeCanvas () 
		{
			// Else it will be stuck forever
			NodeEditor.StopTransitioning (mainNodeCanvas);

			// New NodeCanvas
			mainNodeCanvas = CreateInstance<NodeCanvas> ();
			mainNodeCanvas.name = "New Canvas";
			// New NodeEditorState
			mainEditorState = CreateInstance<NodeEditorState> ();
			mainEditorState.canvas = mainNodeCanvas;
			mainEditorState.name = "MainEditorState";

			openedCanvasPath = "";
			SaveCache ();
		}
		
		#endregion
	}
}