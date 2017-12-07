using UnityEngine;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework.Standard
{
	/// <summary>
	/// Example of displaying the Node Editor at runtime including GUI
	/// </summary>
	public class RTNodeEditor : MonoBehaviour 
	{
		// Startup-canvas, cache and interface
		public NodeCanvas canvas;
		public string loadSceneName;
		private NodeEditorUserCache canvasCache;
		private NodeEditorInterface editorInterface;

		// GUI rects
		public bool screenSize = false;
		public Rect specifiedRootRect = new Rect(0, 0, 1000, 500);
		public Rect specifiedCanvasRect = new Rect(50, 50, 900, 400);
		public Rect rootRect { get { return new Rect(screenSize ? screenRect : specifiedRootRect); } }
		public Rect canvasRect { get { return screenSize ? screenRect : specifiedCanvasRect; } }
		private Rect screenRect { get { return new Rect(0, 0, Screen.width, Screen.height); } }


		private void Start () 
		{
			NormalReInit();
		}

		private void Update () 
		{
			NodeEditor.Update ();
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
			{ // Create cache and load startup-canvas
				canvasCache = new NodeEditorUserCache();
				if (canvas != null)
					canvasCache.SetCanvas(NodeEditorSaveManager.CreateWorkingCopy(canvas));
				else if (!string.IsNullOrEmpty (loadSceneName))
					canvasCache.LoadSceneNodeCanvas(loadSceneName);
			}
			canvasCache.AssureCanvas();
			if (editorInterface == null)
			{ // Setup editor interface
				editorInterface = new NodeEditorInterface();
				editorInterface.canvasCache = canvasCache;
			}
		}

		private void OnGUI ()
		{
			// Initiation
			NodeEditor.checkInit(true);
			if (NodeEditor.InitiationError)
			{
				GUILayout.Label("Node Editor Initiation failed! Check console for more information!");
				return;
			}
			AssureSetup();

			// ROOT: Start Overlay GUI for popups
			OverlayGUI.StartOverlayGUI("RTNodeEditor");
			
			// Set various nested groups
			GUI.BeginGroup(rootRect, GUI.skin.box);

			// Begin Node Editor GUI and set canvas rect
			NodeEditorGUI.StartNodeGUI(false);
			canvasCache.editorState.canvasRect = new Rect (canvasRect.x, canvasRect.y + editorInterface.toolbarHeight, canvasRect.width, canvasRect.height - editorInterface.toolbarHeight);

			try
			{ // Perform drawing with error-handling
				NodeEditor.DrawCanvas (canvasCache.nodeCanvas, canvasCache.editorState);
			}
			catch (UnityException e)
			{ // On exceptions in drawing flush the canvas to avoid locking the UI
				canvasCache.NewNodeCanvas ();
				NodeEditor.ReInit (true);
				Debug.LogError ("Unloaded Canvas due to exception in Draw!");
				Debug.LogException (e);
			}
			
			// Draw Interface
			editorInterface.DrawToolbarGUI(canvasRect);
			editorInterface.DrawModalPanel();

			// End Node Editor GUI
			NodeEditorGUI.EndNodeGUI();

			// End various nested groups
			GUI.EndGroup();

			// END ROOT: End Overlay GUI and draw popups
			OverlayGUI.EndOverlayGUI();
		}
	}
}