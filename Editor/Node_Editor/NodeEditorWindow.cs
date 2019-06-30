using UnityEngine;
using UnityEditor;
using System.IO;

using NodeEditorFramework.Utilities;

namespace NodeEditorFramework.Standard
{
	public class NodeEditorWindow : EditorWindow 
	{
		// Information about current instance
		private static NodeEditorWindow _editor;
		public static NodeEditorWindow editor { get { AssureEditor(); return _editor; } }
		public static void AssureEditor() { if (_editor == null) OpenNodeEditor(); }

		// Canvas cache
		public NodeEditorUserCache canvasCache;
		public NodeEditorInterface editorInterface;

		// GUI
		private Rect canvasWindowRect { get { return new Rect(0, editorInterface.toolbarHeight, position.width, position.height - editorInterface.toolbarHeight); } }


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

		/*
		/// <summary>
		/// Assures that the canvas is opened when double-clicking a canvas asset
		/// </summary>
		[UnityEditor.Callbacks.OnOpenAsset(1)]
		private static bool AutoOpenCanvas(int instanceID, int line)
		{
			if (Selection.activeObject != null && Selection.activeObject is NodeCanvas)
			{
				string NodeCanvasPath = AssetDatabase.GetAssetPath(instanceID);
				OpenNodeEditor().canvasCache.LoadNodeCanvas(NodeCanvasPath);
				return true;
			}
			return false;
		}
		*/
			
		private void OnEnable()
		{
			_editor = this;
			NormalReInit();

			// Subscribe to events
			NodeEditor.ClientRepaints -= Repaint;
			NodeEditor.ClientRepaints += Repaint;
			EditorLoadingControl.justLeftPlayMode -= NormalReInit;
			EditorLoadingControl.justLeftPlayMode += NormalReInit;
			EditorLoadingControl.justOpenedNewScene -= NormalReInit;
			EditorLoadingControl.justOpenedNewScene += NormalReInit;
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			SceneView.onSceneGUIDelegate += OnSceneGUI;
			Undo.undoRedoPerformed -= NodeEditor.RepaintClients;
			Undo.undoRedoPerformed += NodeEditor.RepaintClients;
			Undo.undoRedoPerformed -= UndoRedoRecalculate;
			Undo.undoRedoPerformed += UndoRedoRecalculate;
		}
		
		private void OnDestroy()
		{
			// Unsubscribe from events
			NodeEditor.ClientRepaints -= Repaint;
			EditorLoadingControl.justLeftPlayMode -= NormalReInit;
			EditorLoadingControl.justOpenedNewScene -= NormalReInit;
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			Undo.undoRedoPerformed -= NodeEditor.RepaintClients;
			Undo.undoRedoPerformed -= UndoRedoRecalculate;

			// Clear Cache
			canvasCache.ClearCacheEvents();
		}

		private void UndoRedoRecalculate()
		{
			canvasCache.nodeCanvas.TraverseAll();
		}

		private void OnLostFocus () 
		{ // Save any changes made while focussing this window
			// Will also save before possible assembly reload, scene switch, etc. because these require focussing of a different window
			canvasCache.SaveCache(false);
		}

		private void OnFocus () 
		{ // Make sure the canvas hasn't been corrupted externally
			NormalReInit();
		}

		private void NormalReInit()
		{
			NodeEditor.ReInit(false);
			AssureSetup();
			if (canvasCache.nodeCanvas)
				canvasCache.nodeCanvas.Validate();
		}

		private void AssureSetup()
		{
			if (canvasCache == null)
			{ // Create cache
				canvasCache = new NodeEditorUserCache(Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this))));
			}
			canvasCache.AssureCanvas();
			if (editorInterface == null)
			{ // Setup editor interface
				editorInterface = new NodeEditorInterface();
				editorInterface.canvasCache = canvasCache;
				editorInterface.ShowNotificationAction = ShowNotification;
			}
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
			AssureEditor ();
			AssureSetup();

			// ROOT: Start Overlay GUI for popups
			OverlayGUI.StartOverlayGUI("NodeEditorWindow");

			// Begin Node Editor GUI and set canvas rect
			NodeEditorGUI.StartNodeGUI(true);
			canvasCache.editorState.canvasRect = canvasWindowRect;

			try
			{ // Perform drawing with error-handling
				NodeEditor.DrawCanvas(canvasCache.nodeCanvas, canvasCache.editorState);
			}
			catch (UnityException e)
			{ // On exceptions in drawing flush the canvas to avoid locking the UI
				canvasCache.NewNodeCanvas();
				NodeEditor.ReInit(true);
				Debug.LogError("Unloaded Canvas due to an exception during the drawing phase!");
				Debug.LogException(e);
			}

			// Draw Interface
			editorInterface.DrawToolbarGUI(new Rect(0, 0, Screen.width, 0));
			editorInterface.DrawModalPanel();

			// End Node Editor GUI
			NodeEditorGUI.EndNodeGUI();

			// END ROOT: End Overlay GUI and draw popups
			OverlayGUI.EndOverlayGUI();
		}

		private void OnSceneGUI(SceneView sceneview)
		{
			AssureSetup();
			if (canvasCache.editorState != null && canvasCache.editorState.selectedNode != null)
				canvasCache.editorState.selectedNode.OnSceneGUI();
			SceneView.lastActiveSceneView.Repaint();
		}

		#endregion
	}
}