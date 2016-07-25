using System;
using System.IO;
using System.Linq;
using NodeEditorFramework.Utilities;
using UnityEngine;
using UnityEditor;


namespace NodeEditorFramework.Standard
{
	public class NodeEditorWindow : EditorWindow 
	{
		// Information about current instance
		private static NodeEditorWindow _editor;
		public static NodeEditorWindow editor { get { AssureEditor(); return _editor; } }
		public static void AssureEditor() { if (_editor == null) OpenNodeEditor(); }

		// Opened Canvas
		public NodeCanvas mainNodeCanvas;
		public NodeEditorState mainEditorState;
		public static NodeCanvas MainNodeCanvas { get { return editor.mainNodeCanvas; } }
		public static NodeEditorState MainEditorState { get { return editor.mainEditorState; } }
		public void AssureCanvas() { if (mainNodeCanvas == null) NewNodeCanvas(); }
		public static string openedCanvasPath;
		public static string tempSessionPath;

		// GUI
		private string sceneCanvasName = "";
		private Rect loadScenePos;
		public static int sideWindowWidth = 400;
		private static Texture iconTexture;
		public Rect sideWindowRect { get { return new Rect(position.width - sideWindowWidth, 0, sideWindowWidth, position.height); } }
		public Rect canvasWindowRect { get { return new Rect(0, 0, position.width - sideWindowWidth, position.height); } }
		private Rect createCanvasPos;

		#region General 

		/// <summary>
		/// Opens the Node Editor window and loads the last session
		/// </summary>
		[MenuItem("Window/Node Editor")]
		public static NodeEditorWindow OpenNodeEditor()
		{
			_editor = GetWindow<NodeEditorWindow>();
			_editor.minSize = new Vector2(800, 600);
			NodeEditor.initiated = NodeEditor.InitiationError = false;

			iconTexture = ResourceManager.LoadTexture(EditorGUIUtility.isProSkin ? "Textures/Icon_Dark.png" : "Textures/Icon_Light.png");
			_editor.titleContent = new GUIContent("Node Editor", iconTexture);

			return _editor;
		}

		[UnityEditor.Callbacks.OnOpenAsset(1)]
		private static bool AutoOpenCanvas(int instanceID, int line)
		{
			if (Selection.activeObject != null && Selection.activeObject is NodeCanvas)
			{
				string NodeCanvasPath = AssetDatabase.GetAssetPath(instanceID);
				NodeEditorWindow.OpenNodeEditor();
				EditorWindow.GetWindow<NodeEditorWindow>().LoadNodeCanvas(NodeCanvasPath);
				return true;
			}
			return false;
		}

		private void OnEnable()
		{            
			_editor = this;
			NodeEditor.checkInit(false);

			NodeEditor.ClientRepaints -= Repaint;
			NodeEditor.ClientRepaints += Repaint;

			EditorLoadingControl.justLeftPlayMode -= NormalReInit;
			EditorLoadingControl.justLeftPlayMode += NormalReInit;
			// Here, both justLeftPlayMode and justOpenedNewScene have to act because of timing
			EditorLoadingControl.justOpenedNewScene -= NormalReInit;
			EditorLoadingControl.justOpenedNewScene += NormalReInit;

			// Setup Cache
			tempSessionPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this)));
			SetupCacheEvents();
			LoadCache();
		}

		private void NormalReInit()
		{
			NodeEditor.ReInit(false);
		}

		private void OnDestroy()
		{
			EditorUtility.SetDirty(mainNodeCanvas);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			NodeEditor.ClientRepaints -= Repaint;

			// Clear Cache
			ClearCacheEvents();
		}

		#endregion

		#region GUI

		private void OnGUI()
		{            
			// Initiation
			NodeEditor.checkInit(true);
			if (NodeEditor.InitiationError)
			{
				GUILayout.Label("Node Editor Initiation failed! Check console for more information!");
				return;
			}
			AssureEditor();
			AssureCanvas();

			// Specify the Canvas rect in the EditorState
			mainEditorState.canvasRect = canvasWindowRect;
			// If you want to use GetRect:
			//			Rect canvasRect = GUILayoutUtility.GetRect (600, 600);
			//			if (Event.current.type != EventType.Layout)
			//				mainEditorState.canvasRect = canvasRect;
			NodeEditorGUI.StartNodeGUI();

			// Perform drawing with error-handling
			try
			{
				NodeEditor.DrawCanvas(mainNodeCanvas, mainEditorState);
			}
			catch (UnityException e)
			{ // on exceptions in drawing flush the canvas to avoid locking the ui.
				NewNodeCanvas();
				NodeEditor.ReInit(true);
				Debug.LogError("Unloaded Canvas due to an exception during the drawing phase!");
				Debug.LogException(e);
			}

			// Draw Side Window
			sideWindowWidth = Math.Min(600, Math.Max(200, (int)(position.width / 5)));
			GUILayout.BeginArea(sideWindowRect, GUI.skin.box);
			DrawSideWindow();
			GUILayout.EndArea();

			NodeEditorGUI.EndNodeGUI();
		}

		private void DrawSideWindow()
		{
			GUILayout.Label(new GUIContent("Node Editor (" + mainNodeCanvas.name + ")", "Opened Canvas path: " + openedCanvasPath), NodeEditorGUI.nodeLabelBold);

			if (GUILayout.Button(new GUIContent("New Canvas", "Loads an Specified Empty CanvasType")))
			{
				NodeEditorFramework.Utilities.GenericMenu menu = new NodeEditorFramework.Utilities.GenericMenu();
				NodeCanvasManager.PopulateMenu(ref menu, NewNodeCanvas);
				menu.Show(createCanvasPos.position, createCanvasPos.width);
			}
			if (Event.current.type == EventType.Repaint)
			{
				Rect popupPos = GUILayoutUtility.GetLastRect();
				createCanvasPos = new Rect(popupPos.x + 2, popupPos.yMax + 2, popupPos.width - 4, 0);
			}

			GUILayout.Space(6);

			if (GUILayout.Button(new GUIContent("Save Canvas", "Saves the Canvas to a Canvas Save File in the Assets Folder")))
			{
				string path = EditorUtility.SaveFilePanelInProject("Save Node Canvas", "Node Canvas", "asset", "",
					NodeEditor.editorPath + "Resources/Saves/");
				if (!string.IsNullOrEmpty(path))
					SaveNodeCanvas(path);
			}

			if (GUILayout.Button(new GUIContent("Load Canvas", "Loads the Canvas from a Canvas Save File in the Assets Folder")))
			{
				string path = EditorUtility.OpenFilePanel("Load Node Canvas", NodeEditor.editorPath + "Resources/Saves/", "asset");
				if (!path.Contains(Application.dataPath))
				{
					if (!string.IsNullOrEmpty(path))
						ShowNotification(new GUIContent("You should select an asset inside your project folder!"));
				}
				else
					LoadNodeCanvas(path);
			}

			GUILayout.Space(6);

			GUILayout.BeginHorizontal();
			sceneCanvasName = GUILayout.TextField(sceneCanvasName, GUILayout.ExpandWidth(true));
			if (GUILayout.Button(new GUIContent("Save to Scene", "Saves the Canvas to the Scene"), GUILayout.ExpandWidth(false)))
			{
				SaveSceneNodeCanvas(sceneCanvasName);
			}
			GUILayout.EndHorizontal();

			if (GUILayout.Button(new GUIContent("Load from Scene", "Loads the Canvas from the Scene")))
			{
				NodeEditorFramework.Utilities.GenericMenu menu = new NodeEditorFramework.Utilities.GenericMenu();
				foreach (string sceneSave in NodeEditorSaveManager.GetSceneSaves())
					menu.AddItem(new GUIContent(sceneSave), false, LoadSceneCanvasCallback, (object)sceneSave);
				menu.Show(loadScenePos.position, loadScenePos.width);
			}
			if (Event.current.type == EventType.Repaint)
			{
				Rect popupPos = GUILayoutUtility.GetLastRect();
				loadScenePos = new Rect(popupPos.x + 2, popupPos.yMax + 2, popupPos.width - 4, 0);
			}

			GUILayout.Space(6);

			if (GUILayout.Button(new GUIContent("Recalculate All", "Initiates complete recalculate. Usually does not need to be triggered manually.")))
				NodeEditor.RecalculateAll(mainNodeCanvas);

			if (GUILayout.Button("Force Re-Init"))
				NodeEditor.ReInit(true);

			NodeEditorGUI.knobSize = EditorGUILayout.IntSlider(new GUIContent("Handle Size", "The size of the Node Input/Output handles"), NodeEditorGUI.knobSize, 12, 20);
			mainEditorState.zoom = EditorGUILayout.Slider(new GUIContent("Zoom", "Use the Mousewheel. Seriously."), mainEditorState.zoom, 0.6f, 2);

			if (mainEditorState.selectedNode != null && Event.current.type != EventType.Ignore)
				mainEditorState.selectedNode.DrawNodePropertyEditor();
		}

		#endregion

		#region Cache

		private void SetupCacheEvents()
		{
			// Load the cache after the NodeEditor was cleared
			EditorLoadingControl.lateEnteredPlayMode -= LoadCache;
			EditorLoadingControl.lateEnteredPlayMode += LoadCache;
			EditorLoadingControl.justOpenedNewScene -= LoadCache;
			EditorLoadingControl.justOpenedNewScene += LoadCache;

			// Add new objects to the cache save file
			NodeEditorCallbacks.OnAddNode -= SaveNewNode;
			NodeEditorCallbacks.OnAddNode += SaveNewNode;
			NodeEditorCallbacks.OnAddNodeKnob -= SaveNewNodeKnob;
			NodeEditorCallbacks.OnAddNodeKnob += SaveNewNodeKnob;
		}

		private void ClearCacheEvents()
		{
			EditorLoadingControl.lateEnteredPlayMode -= LoadCache;
			EditorLoadingControl.justLeftPlayMode -= LoadCache;
			EditorLoadingControl.justOpenedNewScene -= LoadCache;
			NodeEditorCallbacks.OnAddNode -= SaveNewNode;
			NodeEditorCallbacks.OnAddNodeKnob -= SaveNewNodeKnob;
		}

		private string lastSessionPath { get { return tempSessionPath + "/LastSession.asset"; } }

		private void SaveNewNode(Node node)
		{
			if (mainNodeCanvas.livesInScene)
				return;
			if (!mainNodeCanvas.nodes.Contains(node))
				return;

			CheckCurrentCache();

			NodeEditorSaveManager.AddSubAsset(node, lastSessionPath);
			foreach (ScriptableObject so in node.GetScriptableObjects())
				NodeEditorSaveManager.AddSubAsset(so, node);

			foreach (NodeKnob knob in node.nodeKnobs)
			{
				NodeEditorSaveManager.AddSubAsset(knob, node);
				foreach (ScriptableObject so in knob.GetScriptableObjects())
					NodeEditorSaveManager.AddSubAsset(so, knob);
			}

			EditorUtility.SetDirty(mainNodeCanvas);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		private void SaveNewNodeKnob(NodeKnob knob)
		{
			if (mainNodeCanvas.livesInScene)
				return;
			if (!mainNodeCanvas.nodes.Contains(knob.body))
				return;

			CheckCurrentCache();

			NodeEditorSaveManager.AddSubAsset(knob, knob.body);
			foreach (ScriptableObject so in knob.GetScriptableObjects())
				NodeEditorSaveManager.AddSubAsset(so, knob);

			EditorUtility.SetDirty(mainNodeCanvas);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		/// <summary>
		/// Creates a new cache save file for the currently loaded canvas 
		/// Only called when a new canvas is created or loaded
		/// </summary>
		private void SaveCache()
		{
			if (mainNodeCanvas.livesInScene)
				return;

			mainNodeCanvas.editorStates = new NodeEditorState[] { mainEditorState };
			NodeEditorSaveManager.SaveNodeCanvas(lastSessionPath, mainNodeCanvas, false);

			CheckCurrentCache();
		}

		/// <summary>
		/// Loads the canvas from the cache save file
		/// Called whenever a reload was made
		/// </summary>
		private void LoadCache()
		{
			// Try to load the NodeCanvas
			if (!File.Exists(lastSessionPath) || (mainNodeCanvas = NodeEditorSaveManager.LoadNodeCanvas(lastSessionPath, false)) == null)
			{
				NewNodeCanvas();
				return;
			}

			// Fetch the associated MainEditorState
			if (mainNodeCanvas.editorStates.Length > 0)
				mainEditorState = mainNodeCanvas.editorStates.Length == 1 ? mainNodeCanvas.editorStates[0] : mainNodeCanvas.editorStates.First((NodeEditorState state) => state.name == "MainEditorState");
			if (mainEditorState == null)
			{
				NewEditorState();
				NodeEditorSaveManager.AddSubAsset(mainEditorState, lastSessionPath);
			}

			CheckCurrentCache();

			NodeEditor.RecalculateAll(mainNodeCanvas);
			Repaint();
		}

		private void CheckCurrentCache()
		{
			if (AssetDatabase.GetAssetPath(mainNodeCanvas) != lastSessionPath)
				throw new UnityException("Cache system error: Current Canvas is not saved as the temporary cache!");
		}

		//		private void DeleteCache () 
		//		{
		//			string lastSession = EditorPrefs.GetString ("NodeEditorLastSession");
		//			if (!String.IsNullOrEmpty (lastSession))
		//			{
		//				AssetDatabase.DeleteAsset (tempSessionPath + "/" + lastSession);
		//				AssetDatabase.Refresh ();
		//			}
		//			EditorPrefs.DeleteKey ("NodeEditorLastSession");
		//		}

		#endregion

		#region Save/Load

		private void LoadSceneCanvasCallback(object save)
		{
			LoadSceneNodeCanvas((string)save);
		}

		/// <summary>
		/// Saves the mainNodeCanvas and it's associated mainEditorState as an asset at path
		/// </summary>
		public void SaveSceneNodeCanvas(string path)
		{
			mainNodeCanvas.editorStates = new NodeEditorState[] { mainEditorState };
			NodeEditorSaveManager.SaveSceneNodeCanvas(path, ref mainNodeCanvas, true);
			Repaint();
		}

		/// <summary>
		/// Loads the mainNodeCanvas and it's associated mainEditorState from an asset at path
		/// </summary>
		public void LoadSceneNodeCanvas(string path)
		{
			// Try to load the NodeCanvas
			if ((mainNodeCanvas = NodeEditorSaveManager.LoadSceneNodeCanvas(path, true)) == null)
			{
				NewNodeCanvas();
				return;
			}
			mainEditorState = NodeEditorSaveManager.ExtractEditorState(mainNodeCanvas, "MainEditorState");

			openedCanvasPath = path;
			NodeEditor.RecalculateAll(mainNodeCanvas);
			Repaint();
		}

		/// <summary>
		/// Saves the mainNodeCanvas and it's associated mainEditorState as an asset at path
		/// </summary>
		public void SaveNodeCanvas(string path)
		{
			mainNodeCanvas.editorStates = new NodeEditorState[] { mainEditorState };
			NodeEditorSaveManager.SaveNodeCanvas(path, mainNodeCanvas, true);
			Repaint();
		}

		/// <summary>
		/// Loads the mainNodeCanvas and it's associated mainEditorState from an asset at path
		/// </summary>
		public void LoadNodeCanvas(string path)
		{
			// Try to load the NodeCanvas
			if (!File.Exists(path) || (mainNodeCanvas = NodeEditorSaveManager.LoadNodeCanvas(path, true)) == null)
			{
				NewNodeCanvas();
				return;
			}
			mainEditorState = NodeEditorSaveManager.ExtractEditorState(mainNodeCanvas, "MainEditorState");

			openedCanvasPath = path;
			SaveCache();
			NodeEditor.RecalculateAll(mainNodeCanvas);
			Repaint();
		}

		///// <summary>
		///// Creates and loads a new NodeCanvas
		///// </summary>
		public void NewNodeCanvas(Type canvasType = null)
		{
			if(canvasType == null)
				mainNodeCanvas = CreateInstance<NodeCanvas>();
			else
				mainNodeCanvas = CreateInstance(canvasType) as NodeCanvas;

			mainNodeCanvas.name = "New Canvas";
		//EditorPrefs.SetString ("NodeEditorLastSession", "New Canvas");
		NewEditorState ();
			openedCanvasPath = "";
			SaveCache();
		}

		/// <summary>
		/// Creates a new EditorState for the current NodeCanvas
		/// </summary>
		public void NewEditorState()
		{
			mainEditorState = CreateInstance<NodeEditorState>();
			mainEditorState.canvas = mainNodeCanvas;
			mainEditorState.name = "MainEditorState";
			mainNodeCanvas.editorStates = new NodeEditorState[] { mainEditorState };
			EditorUtility.SetDirty(mainNodeCanvas);
		}
		
		#endregion
	}
}