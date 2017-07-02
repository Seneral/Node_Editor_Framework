using System;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEditor;

using NodeEditorFramework.Utilities;
using NodeEditorFramework.IO;

using GenericMenu = NodeEditorFramework.Utilities.GenericMenu;

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

		/// <summary>
		/// Assures that the canvas is opened when double-clicking a canvas asset
		/// </summary>
		[UnityEditor.Callbacks.OnOpenAsset(1)]
		private static bool AutoOpenCanvas(int instanceID, int line)
		{
			if (Selection.activeObject != null && Selection.activeObject is NodeCanvas)
			{
				string NodeCanvasPath = AssetDatabase.GetAssetPath(instanceID);
				NodeEditorWindow.OpenNodeEditor().canvasCache.LoadNodeCanvas(NodeCanvasPath);
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
			editorInterface = new NodeEditorInterface();
			editorInterface.canvasCache = canvasCache;
			editorInterface.ShowNotificationAction = ShowNotification;
		}

		private void NormalReInit()
		{
			NodeEditor.ReInit(false);
			if (canvasCache.nodeCanvas)
				canvasCache.nodeCanvas.Validate ();
		}

		private void OnDestroy()
		{
			NodeEditor.ClientRepaints -= Repaint;

			EditorLoadingControl.justLeftPlayMode -= NormalReInit;
			EditorLoadingControl.justOpenedNewScene -= NormalReInit;

			SceneView.onSceneGUIDelegate -= OnSceneGUI;

			// Clear Cache
			canvasCache.ClearCacheEvents ();
		}

		private void OnLostFocus () 
		{ // Save any changes made while focussing this window
			// Will also save before possible assembly reload, scene switch, etc. because these require focussing of a different window
			canvasCache.SaveCache ();
		}

		private void OnFocus () 
		{ // Make sure the canvas hasn't been corrupted externally
			if (canvasCache == null)
				return;
			if (canvasCache.nodeCanvas != null)
				canvasCache.nodeCanvas.Validate ();
		}

		#endregion

		#region GUI

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

			NodeEditorGUI.StartNodeGUI ("NodeEditorWindow", true);

			try
			{ // Perform drawing with error-handling
				canvasCache.editorState.canvasRect = canvasWindowRect;
				NodeEditor.DrawCanvas (canvasCache.nodeCanvas, canvasCache.editorState);
			}
			catch (UnityException e)
			{ // On exceptions in drawing flush the canvas to avoid locking the UI
				canvasCache.NewNodeCanvas ();
				NodeEditor.ReInit (true);
				Debug.LogError ("Unloaded Canvas due to an exception during the drawing phase!");
				Debug.LogException (e);
			}

			// Draw Interface
			editorInterface.DrawToolbarGUI (Screen.width);
			editorInterface.DrawModalPanel ();

			NodeEditorGUI.EndNodeGUI();
		}

		private void OnSceneGUI(SceneView sceneview)
		{
			if (canvasCache.editorState != null && canvasCache.editorState.selectedNode != null)
				canvasCache.editorState.selectedNode.OnSceneGUI();
			SceneView.lastActiveSceneView.Repaint();
		}

		#endregion
	}
}