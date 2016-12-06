using System;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEditor;

using NodeEditorFramework;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework.Standard
{
	public class NodeEditorWindow : EditorWindow 
	{
		// Information about current instance
		private static NodeEditorWindow _editor;
		public static NodeEditorWindow editor { get { AssureEditor(); return _editor; } }
		public static void AssureEditor() { if (_editor == null) OpenNodeEditor(); }

		// Opened Canvas
		public static NodeEditorUserCache canvasCache;

		// GUI
		private string sceneCanvasName = "";
		private Rect loadSceneUIPos;
		private Rect createCanvasUIPos;
		private int sideWindowWidth = 400;

		public Rect sideWindowRect { get { return new Rect (position.width - sideWindowWidth, 0, sideWindowWidth, position.height); } }
		public Rect canvasWindowRect { get { return new Rect (0, 0, position.width - sideWindowWidth, position.height); } }

		#region General 

		/// <summary>
		/// Opens the Node Editor window and loads the last session
		/// </summary>
		[MenuItem("Window/Node Editor")]
		public static NodeEditorWindow OpenNodeEditor () 
		{
			_editor = GetWindow<NodeEditorWindow>();
			_editor.minSize = new Vector2(400, 200);
			NodeEditor.ReInit (false);

			Texture iconTexture = ResourceManager.LoadTexture (EditorGUIUtility.isProSkin? "Textures/Icon_Dark.png" : "Textures/Icon_Light.png");
			_editor.titleContent = new GUIContent ("Node Editor", iconTexture);

			return _editor;
		}
		
		[UnityEditor.Callbacks.OnOpenAsset(1)]
		private static bool AutoOpenCanvas(int instanceID, int line)
		{
			if (Selection.activeObject != null && Selection.activeObject is NodeCanvas)
			{
				string NodeCanvasPath = AssetDatabase.GetAssetPath(instanceID);
				NodeEditorWindow.OpenNodeEditor();
				canvasCache.LoadNodeCanvas(NodeCanvasPath);
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

			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			SceneView.onSceneGUIDelegate += OnSceneGUI;

			// Setup Cache
			canvasCache = new NodeEditorUserCache(Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject (this))));
			canvasCache.SetupCacheEvents();
		}

	    private void NormalReInit()
		{
			NodeEditor.ReInit(false);
		}

		private void OnDestroy()
		{
			EditorUtility.SetDirty(canvasCache.nodeCanvas);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			NodeEditor.ClientRepaints -= Repaint;

			EditorLoadingControl.justLeftPlayMode -= NormalReInit;
			EditorLoadingControl.justOpenedNewScene -= NormalReInit;

			SceneView.onSceneGUIDelegate -= OnSceneGUI;

			// Clear Cache
			canvasCache.ClearCacheEvents ();
		}

        #endregion

        #region GUI

        private void OnSceneGUI(SceneView sceneview)
        {
            DrawSceneGUI();
        }

	    private void DrawSceneGUI()
	    {
			if (canvasCache.editorState.selectedNode != null)
				canvasCache.editorState.selectedNode.OnSceneGUI();
            SceneView.lastActiveSceneView.Repaint();
        }

        private void OnGUI()
		{            
			// Initiation
			NodeEditor.checkInit(true);
			if (NodeEditor.InitiationError || canvasCache == null)
			{
				GUILayout.Label("Node Editor Initiation failed! Check console for more information!");
				return;
			}
			AssureEditor ();
			canvasCache.AssureCanvas ();

			// Specify the Canvas rect in the EditorState
			canvasCache.editorState.canvasRect = canvasWindowRect;
			// If you want to use GetRect:
//			Rect canvasRect = GUILayoutUtility.GetRect (600, 600);
//			if (Event.current.type != EventType.Layout)
//				mainEditorState.canvasRect = canvasRect;
			NodeEditorGUI.StartNodeGUI ("NodeEditorWindow", true);

			// Perform drawing with error-handling
			try
			{
				NodeEditor.DrawCanvas (canvasCache.nodeCanvas, canvasCache.editorState);
			}
			catch (UnityException e)
			{ // on exceptions in drawing flush the canvas to avoid locking the ui.
				canvasCache.NewNodeCanvas ();
				NodeEditor.ReInit (true);
				Debug.LogError ("Unloaded Canvas due to an exception during the drawing phase!");
				Debug.LogException (e);
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
			GUILayout.Label (new GUIContent ("Node Editor (" + canvasCache.nodeCanvas.saveName + ")", "Opened Canvas path: " + canvasCache.nodeCanvas.savePath), NodeEditorGUI.nodeLabelBold);
			GUILayout.Label ((canvasCache.nodeCanvas.livesInScene? "Scene Save" : "Asset Save") + ", Type: " + canvasCache.typeData.DisplayString + "");

//			EditorGUILayout.ObjectField ("Loaded Canvas", canvasCache.nodeCanvas, typeof(NodeCanvas), false);
//			EditorGUILayout.ObjectField ("Loaded State", canvasCache.editorState, typeof(NodeEditorState), false);

			if (GUILayout.Button(new GUIContent("New Canvas", "Loads an Specified Empty CanvasType")))
			{
				NodeEditorFramework.Utilities.GenericMenu menu = new NodeEditorFramework.Utilities.GenericMenu();
				NodeCanvasManager.FillCanvasTypeMenu(ref menu, canvasCache.NewNodeCanvas);
				menu.Show(createCanvasUIPos.position, createCanvasUIPos.width);
			}
			if (Event.current.type == EventType.Repaint)
			{
				Rect popupPos = GUILayoutUtility.GetLastRect();
				createCanvasUIPos = new Rect(popupPos.x + 2, popupPos.yMax + 2, popupPos.width - 4, 0);
			}

			if (GUILayout.Button(new GUIContent("Save Canvas", "Save the Canvas to the load location")))
			{
				string path = canvasCache.nodeCanvas.savePath;
				if (!string.IsNullOrEmpty (path))
				{
					if (path.StartsWith ("SCENE/"))
						canvasCache.SaveSceneNodeCanvas (path.Substring (6));
					else
						canvasCache.SaveNodeCanvas (path);
				}
				else
					ShowNotification (new GUIContent ("No save location found. Use 'Save As'!"));
			}

			//GUILayout.Space(6);
			GUILayout.Label ("Asset Saving", NodeEditorGUI.nodeLabel);

			if (GUILayout.Button(new GUIContent("Save Canvas As", "Save the canvas as an asset")))
			{
                string panelPath = NodeEditor.editorPath + "Resources/Saves/";
                if (canvasCache.nodeCanvas != null)
                {
                    if (!string.IsNullOrEmpty(canvasCache.nodeCanvas.savePath))
                    {
                        panelPath = canvasCache.nodeCanvas.savePath;
                    }
                }

                string path = EditorUtility.SaveFilePanelInProject ("Save Node Canvas", "Node Canvas", "asset", "", panelPath);
				if (!string.IsNullOrEmpty (path))
					canvasCache.SaveNodeCanvas (path);
			}

			if (GUILayout.Button(new GUIContent("Load Canvas", "Load the Canvas from an asset")))
			{
                string panelPath = NodeEditor.editorPath + "Resources/Saves/";

                if (canvasCache.nodeCanvas != null)
                {
                    if (!string.IsNullOrEmpty(canvasCache.nodeCanvas.savePath))
                    {
                        panelPath = canvasCache.nodeCanvas.savePath;
                    }
                }

                string path = EditorUtility.OpenFilePanel("Load Node Canvas", panelPath, "asset");

                if (!path.Contains(Application.dataPath))
				{
					if (!string.IsNullOrEmpty(path))
						ShowNotification(new GUIContent("You should select an asset inside your project folder!"));
				}
				else
					canvasCache.LoadNodeCanvas (path);
			}

			//GUILayout.Space(6);
			GUILayout.Label ("Scene Saving", NodeEditorGUI.nodeLabel);

			GUILayout.BeginHorizontal ();
			sceneCanvasName = GUILayout.TextField (sceneCanvasName, GUILayout.ExpandWidth (true));
			if (GUILayout.Button (new GUIContent ("Save to Scene", "Save the canvas to the Scene"), GUILayout.ExpandWidth (false)))
				canvasCache.SaveSceneNodeCanvas (sceneCanvasName);
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

			//GUILayout.Space (6);
			GUILayout.Label ("Utility/Debug", NodeEditorGUI.nodeLabel);

			if (GUILayout.Button (new GUIContent ("Recalculate All", "Initiates complete recalculate. Usually does not need to be triggered manually.")))
				NodeEditor.Calculator.RecalculateAll (canvasCache.nodeCanvas);

			if (GUILayout.Button ("Force Re-Init"))
				NodeEditor.ReInit (true);
			
			NodeEditorGUI.knobSize = EditorGUILayout.IntSlider (new GUIContent ("Handle Size", "The size of the Node Input/Output handles"), NodeEditorGUI.knobSize, 12, 20);
			canvasCache.editorState.zoom = EditorGUILayout.Slider (new GUIContent ("Zoom", "Use the Mousewheel. Seriously."), canvasCache.editorState.zoom, 0.6f, 4);

			if (canvasCache.editorState.selectedNode != null && Event.current.type != EventType.Ignore)
				canvasCache.editorState.selectedNode.DrawNodePropertyEditor();
		}

		public void LoadSceneCanvasCallback (object canvas) 
		{
			canvasCache.LoadSceneNodeCanvas ((string)canvas);
		}

		#endregion
	}
}