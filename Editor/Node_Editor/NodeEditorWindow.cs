using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework
{
	public class NodeEditorWindow : EditorWindow 
	{
		// Information about current instance
		private static NodeEditorWindow editor;
		public static NodeEditorWindow Editor { get { AssureEditor (); return editor; } }
		public static void AssureEditor () { if (editor == null) CreateEditor (); }

		// Opened Canvas
		public NodeCanvas mainNodeCanvas;
		public NodeEditorState mainEditorState;
		public static NodeCanvas MainNodeCanvas { get { return Editor.mainNodeCanvas; } }
		public static NodeEditorState MainEditorState { get { return Editor.mainEditorState; } }
		public void AssureCanvas () { if (mainNodeCanvas == null) NewNodeCanvas (); }
		public static string OpenedCanvasPath;
		public string TempSessionPath;

		// GUI
		public static int SideWindowWidth = 400;
		private static Texture iconTexture;
		public Rect SideWindowRect { get { return new Rect (position.width - SideWindowWidth, 0, SideWindowWidth, position.height); } }
		public Rect CanvasWindowRect { get { return new Rect (0, 0, position.width - SideWindowWidth, position.height); } }

		#region General 

		[MenuItem ("Window/Node Editor")]
		public static void CreateEditor () 
		{
			editor = GetWindow<NodeEditorWindow> ();
			editor.minSize = new Vector2 (800, 600);
			NodeEditor.ClientRepaints += editor.Repaint;
			NodeEditor.Initiated = NodeEditor.InitiationError = false;

			// Setup Title content
			ResourceManager.Init (NodeEditor.EditorPath + "Resources/");
			iconTexture = ResourceManager.LoadTexture (EditorGUIUtility.isProSkin? "Textures/Icon_Dark.png" : "Textures/Icon_Light.png");
			editor.titleContent = new GUIContent ("Node Editor", iconTexture);
		}
		
		/// <summary>
		/// Handle opening canvas when double-clicking asset
		/// </summary>
		[UnityEditor.Callbacks.OnOpenAsset(1)]
		public static bool AutoOpenCanvas (int instanceID, int line) 
		{
			if (Selection.activeObject != null && Selection.activeObject.GetType () == typeof(NodeCanvas))
			{
				var nodeCanvasPath = AssetDatabase.GetAssetPath (instanceID);
				CreateEditor ();
				GetWindow<NodeEditorWindow> ().LoadNodeCanvas (nodeCanvasPath);
				return true;
			}
			return false;
		}

		public void OnDestroy () 
		{
			NodeEditor.ClientRepaints -= editor.Repaint;
			SaveCache ();

	#if UNITY_EDITOR
			// Remove callbacks
			EditorLoadingControl.BeforeEnteringPlayMode -= SaveCache;
			EditorLoadingControl.LateEnteredPlayMode -= LoadCache;
			EditorLoadingControl.BeforeLeavingPlayMode -= SaveCache;
			EditorLoadingControl.JustLeftPlayMode -= LoadCache;
			EditorLoadingControl.JustOpenedNewScene -= LoadCache;

			// TODO: BeforeOpenedScene to save Cache, aswell as assembly reloads... 
	#endif
		}

		// Following section is all about caching the last editor session

		public void OnEnable () 
		{
			TempSessionPath = Path.GetDirectoryName (AssetDatabase.GetAssetPath (MonoScript.FromScriptableObject (this)));
			LoadCache ();

	#if UNITY_EDITOR
			// This makes sure the Node Editor is reinitiated after the Playmode changed
			EditorLoadingControl.BeforeEnteringPlayMode -= SaveCache;
			EditorLoadingControl.BeforeEnteringPlayMode += SaveCache;
			EditorLoadingControl.LateEnteredPlayMode -= LoadCache;
			EditorLoadingControl.LateEnteredPlayMode += LoadCache;

			EditorLoadingControl.BeforeLeavingPlayMode -= SaveCache;
			EditorLoadingControl.BeforeLeavingPlayMode += SaveCache;
			EditorLoadingControl.JustLeftPlayMode -= LoadCache;
			EditorLoadingControl.JustLeftPlayMode += LoadCache;

			EditorLoadingControl.JustOpenedNewScene -= LoadCache;
			EditorLoadingControl.JustOpenedNewScene += LoadCache;

			// TODO: BeforeOpenedScene to save Cache, aswell as assembly reloads... 
	#endif
		}

		private void SaveCache () 
		{
			DeleteCache ();
			EditorPrefs.SetString ("NodeEditorLastSession", mainNodeCanvas.name + ".asset");
			NodeEditor.SaveNodeCanvas (mainNodeCanvas, TempSessionPath + "/" + mainNodeCanvas.name + ".asset", mainEditorState);
			AssetDatabase.SaveAssets ();
			AssetDatabase.Refresh ();
		}
		private void LoadCache () 
		{
			string lastSession = EditorPrefs.GetString ("NodeEditorLastSession");
			if (String.IsNullOrEmpty (lastSession))
				return;
			LoadNodeCanvas (TempSessionPath + "/" + lastSession);
			NodeEditor.Initiated = NodeEditor.InitiationError = false;
		}
		private void DeleteCache () 
		{
			string lastSession = EditorPrefs.GetString ("NodeEditorLastSession");
			if (!String.IsNullOrEmpty (lastSession))
				AssetDatabase.DeleteAsset (TempSessionPath + "/" + lastSession);
			AssetDatabase.Refresh ();
			EditorPrefs.DeleteKey ("NodeEditorLastSession");
		}

		#endregion

		#region GUI

		public void OnGUI () 
		{
			// Initiation
			NodeEditor.CheckInit ();
			if (NodeEditor.InitiationError) 
			{
				GUILayout.Label ("Node Editor Initiation failed! Check console for more information!");
				return;
			}
			AssureEditor ();
			AssureCanvas ();

			// Specify the Canvas rect in the EditorState
			mainEditorState.CanvasRect = CanvasWindowRect;
			// If you want to use GetRect:
//			Rect canvasRect = GUILayoutUtility.GetRect (600, 600);
//			if (Event.current.type != EventType.Layout)
//				mainEditorState.canvasRect = canvasRect;

			// Perform drawing with error-handling
			try
			{
				NodeEditor.DrawCanvas (mainNodeCanvas, mainEditorState);
			}
			catch (UnityException e)
			{ // on exceptions in drawing flush the canvas to avoid locking the ui.
				NewNodeCanvas ();
				Debug.LogError ("Unloaded Canvas due to exception when drawing!");
				Debug.LogException (e);
			}

			// Draw Side Window
			SideWindowWidth = Math.Min (600, Math.Max (200, (int)(position.width / 5)));
			NodeEditorGUI.StartNodeGUI ();
			GUILayout.BeginArea (SideWindowRect, GUI.skin.box);
			DrawSideWindow ();
			GUILayout.EndArea ();
			NodeEditorGUI.EndNodeGUI ();
		}

		public void DrawSideWindow () 
		{
			GUILayout.Label (new GUIContent ("Node Editor (" + mainNodeCanvas.name + ")", "Opened Canvas path: " + OpenedCanvasPath), NodeEditorGUI.NodeLabelBold);

			if (GUILayout.Button (new GUIContent ("Save Canvas", "Saves the Canvas to a Canvas Save File in the Assets Folder")))
				SaveNodeCanvas (EditorUtility.SaveFilePanelInProject ("Save Node Canvas", "Node Canvas", "asset", "", ResourceManager.ResourcePath + "Saves/"));
			
			if (GUILayout.Button (new GUIContent ("Load Canvas", "Loads the Canvas from a Canvas Save File in the Assets Folder"))) 
			{
				string path = EditorUtility.OpenFilePanel ("Load Node Canvas", ResourceManager.ResourcePath + "Saves/", "asset");
				if (!path.Contains (Application.dataPath)) 
				{
					if (path != String.Empty)
						ShowNotification (new GUIContent ("You should select an asset inside your project folder!"));
					return;
				}
				path = path.Replace (Application.dataPath, "Assets");
				LoadNodeCanvas (path);
			}

			if (GUILayout.Button (new GUIContent ("New Canvas", "Loads an empty Canvas")))
				NewNodeCanvas ();

			if (GUILayout.Button (new GUIContent ("Recalculate All", "Initiates complete recalculate. Usually does not need to be triggered manually.")))
				NodeEditor.RecalculateAll (mainNodeCanvas);

			if (GUILayout.Button ("Force Re-Init"))
				NodeEditor.ReInit (true);

			NodeEditorGUI.KnobSize = EditorGUILayout.IntSlider (new GUIContent ("Handle Size", "The size of the Node Input/Output handles"), NodeEditorGUI.KnobSize, 12, 20);
			mainEditorState.Zoom = EditorGUILayout.Slider (new GUIContent ("Zoom", "Use the Mousewheel. Seriously."), mainEditorState.Zoom, 0.6f, 2);
		}

		#endregion

		#region Save/Load
		
		/// <summary>
		/// Saves the mainNodeCanvas and it's associated mainEditorState as an asset at path
		/// </summary>
		public void SaveNodeCanvas (string path) 
		{
			NodeEditor.SaveNodeCanvas (mainNodeCanvas, path, mainEditorState);
			//SaveCache ();
			Repaint ();
		}
		
		/// <summary>
		/// Loads the mainNodeCanvas and it's associated mainEditorState from an asset at path
		/// </summary>
		public void LoadNodeCanvas (string path) 
		{
			// Load the NodeCanvas
			mainNodeCanvas = NodeEditor.LoadNodeCanvas (path);
			if (mainNodeCanvas == null) 
			{
				NewNodeCanvas ();
				return;
			}
			
			// Load the associated MainEditorState
			List<NodeEditorState> editorStates = NodeEditor.LoadEditorStates (path);
			if (editorStates.Count == 0)
				mainEditorState = CreateInstance<NodeEditorState> ();
			else 
			{
				mainEditorState = editorStates.Find (x => x.name == "MainEditorState");
				if (mainEditorState == null) mainEditorState = editorStates[0];
			}
			mainEditorState.Canvas = mainNodeCanvas;

			OpenedCanvasPath = path;
			NodeEditor.RecalculateAll (mainNodeCanvas);
			//SaveCache ();
			Repaint ();
		}

		/// <summary>
		/// Creates and opens a new empty node canvas
		/// </summary>
		public void NewNodeCanvas () 
		{
			// New NodeCanvas
			mainNodeCanvas = CreateInstance<NodeCanvas> ();
			mainNodeCanvas.name = "New Canvas";
			// New NodeEditorState
			mainEditorState = CreateInstance<NodeEditorState> ();
			mainEditorState.Canvas = mainNodeCanvas;
			mainEditorState.name = "MainEditorState";

			OpenedCanvasPath = "";
			//SaveCache ();
		}
		
		#endregion
	}
}