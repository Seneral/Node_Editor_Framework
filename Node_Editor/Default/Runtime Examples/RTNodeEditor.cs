using UnityEngine;
using System;

using NodeEditorFramework.Utilities;
using NodeEditorFramework.IO;

namespace NodeEditorFramework.Standard
{
	/// <summary>
	/// Example of displaying the Node Editor at runtime including GUI
	/// </summary>
	public class RTNodeEditor : MonoBehaviour 
	{
		public NodeCanvas canvas;
		private NodeEditorUserCache canvasCache;
		private NodeEditorInterface editorInterface;

		// Rects
		public bool screenSize = false;
		private Rect canvasRect;
		public Rect specifiedRootRect = new Rect (0, 0, 1000, 500);
		public Rect specifiedCanvasRect = new Rect (50, 50, 900, 400);
		private Rect screenRect { get { return new Rect(0, 0, Screen.width, Screen.height); } }

		private void Start () 
		{
			NodeEditor.checkInit (false);

			canvasCache = new NodeEditorUserCache ();
			canvasCache.SetCanvas(NodeEditorSaveManager.CreateWorkingCopy(canvas));
			editorInterface = new NodeEditorInterface();
			editorInterface.canvasCache = canvasCache;
		}

		private void Update () 
		{
			NodeEditor.Update ();
		}

		private void NormalReInit()
		{
			//NodeEditor.resetInit();
			NodeEditor.ReInit(false);
			if (canvasCache.nodeCanvas)
				canvasCache.nodeCanvas.Validate();
		}

		public void OnGUI ()
		{
			// Initiation
			NodeEditor.checkInit(true);
			if (NodeEditor.InitiationError || canvasCache == null)
			{
				GUILayout.Label("Node Editor Initiation failed! Check console for more information!");
				return;
			}
			canvasCache.AssureCanvas ();

			GUI.BeginGroup(screenSize ? screenRect : specifiedRootRect, NodeEditorGUI.nodeSkin.box);
			NodeEditorGUI.StartNodeGUI("RTNodeEditor", false);

			try
			{ // Perform drawing with error-handling
				canvasRect = screenSize? screenRect : specifiedCanvasRect;
				canvasCache.editorState.canvasRect = canvasRect;
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
			editorInterface.DrawToolbarGUI(canvasRect.width);
			editorInterface.DrawModalPanel();

			NodeEditorGUI.EndNodeGUI();
			GUI.EndGroup();
		}
	}
}