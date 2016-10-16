using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;

using NodeEditorFramework;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework
{
	public class NodeEditorUserCache
	{
		public NodeCanvas nodeCanvas;
		public NodeEditorState editorState;
		public void AssureCanvas () { if (nodeCanvas == null) LoadCache (); if (nodeCanvas == null) NewNodeCanvas (); if (editorState == null) NewEditorState (); }

		public NodeCanvasTypeData typeData;

		private string cachePath;
		private bool useCache;
		private const string MainEditorStateIdentifier = "MainEditorState";

		private string lastSessionPath { get { return cachePath + "/LastSession.asset"; } }

		public string openedCanvasPath = "";

		public NodeEditorUserCache (NodeCanvas loadedCanvas)
		{
			useCache = false;
			SetCanvas (loadedCanvas);
		}

		public NodeEditorUserCache ()
		{
			useCache = false;
		}

		#if UNITY_EDITOR
		public NodeEditorUserCache (string CachePath, NodeCanvas loadedCanvas)
		{
			useCache = true;
			cachePath = CachePath;
			SetCanvas (loadedCanvas);
		}

		public NodeEditorUserCache (string CachePath)
		{
			useCache = true;
			cachePath = CachePath;
		}
		#endif

		#region Cache

		public void SetupCacheEvents () 
		{ 
			if (!useCache)
				return;

			// Add new objects to the cache save file
			NodeEditorCallbacks.OnAddNode -= SaveNewNode;
			NodeEditorCallbacks.OnAddNode += SaveNewNode;
			NodeEditorCallbacks.OnAddNodeKnob -= SaveNewNodeKnob;
			NodeEditorCallbacks.OnAddNodeKnob += SaveNewNodeKnob;

			LoadCache ();
		}

		public void ClearCacheEvents () 
		{
			NodeEditorCallbacks.OnAddNode -= SaveNewNode;
			NodeEditorCallbacks.OnAddNodeKnob -= SaveNewNodeKnob;
		}

		private void SaveNewNode (Node node) 
		{
			#if UNITY_EDITOR
			if (!useCache)
				return;
			CheckCurrentCache ();

			if (nodeCanvas.livesInScene)
				return;
			if (!nodeCanvas.nodes.Contains (node))
				return;

			NodeEditorSaveManager.AddSubAsset (node, lastSessionPath);
			foreach (ScriptableObject so in node.GetScriptableObjects ())
				NodeEditorSaveManager.AddSubAsset (so, node);

			foreach (NodeKnob knob in node.nodeKnobs)
			{
				NodeEditorSaveManager.AddSubAsset (knob, node);
				foreach (ScriptableObject so in knob.GetScriptableObjects ())
					NodeEditorSaveManager.AddSubAsset (so, knob);
			}

			UpdateCacheFile ();
			#endif
		}

		private void SaveNewNodeKnob (NodeKnob knob) 
		{
			#if UNITY_EDITOR
			if (!useCache)
				return;
			CheckCurrentCache ();

			if (nodeCanvas.livesInScene)
				return;
			if (!nodeCanvas.nodes.Contains (knob.body))
				return;

			NodeEditorSaveManager.AddSubAsset (knob, knob.body);
			foreach (ScriptableObject so in knob.GetScriptableObjects ())
				NodeEditorSaveManager.AddSubAsset (so, knob);

			UpdateCacheFile ();
			#endif
		}

		/// <summary>
		/// Creates a new cache save file for the currently loaded canvas 
		/// Only called when a new canvas is created or loaded
		/// </summary>
		private void RecreateCache () 
		{
			if (!useCache)
				return;
			DeleteCache ();
			SaveCache ();
		}

		/// <summary>
		/// Creates a new cache save file for the currently loaded canvas 
		/// Only called when a new canvas is created or loaded
		/// </summary>
		private void SaveCache () 
		{
			if (!useCache)
				return;
			nodeCanvas.editorStates = new NodeEditorState[] { editorState };
			if (nodeCanvas.livesInScene)
				NodeEditorSaveManager.SaveSceneNodeCanvas ("lastSession", ref nodeCanvas, false);
			#if UNITY_EDITOR
			else
				NodeEditorSaveManager.SaveNodeCanvas (lastSessionPath, nodeCanvas, false);
			#endif

			CheckCurrentCache ();
		}

		/// <summary>
		/// Loads the canvas from the cache save file
		/// Called whenever a reload was made
		/// </summary>
		private void LoadCache () 
		{
			if (!useCache)
			{
				NewNodeCanvas ();
				return;
			}
			// Try to load the NodeCanvas
			if (
			#if UNITY_EDITOR
				(!File.Exists (lastSessionPath) || (nodeCanvas = NodeEditorSaveManager.LoadNodeCanvas (lastSessionPath, false)) == null) &&	// Check for asset cache
			#endif
				(nodeCanvas = NodeEditorSaveManager.LoadSceneNodeCanvas ("lastSession", false)) == null)									// Check for scene cache
			{
				NewNodeCanvas ();
				return;
			}

			// Fetch the associated MainEditorState
			editorState = NodeEditorSaveManager.ExtractEditorState (nodeCanvas, MainEditorStateIdentifier);
			#if UNITY_EDITOR
			if (!nodeCanvas.livesInScene && !UnityEditor.AssetDatabase.Contains (editorState))
				NodeEditorSaveManager.AddSubAsset (editorState, lastSessionPath);
			#endif

			CheckCurrentCache ();
			UpdateCanvasInfo ();
			NodeEditor.Calculator.RecalculateAll (nodeCanvas);
			NodeEditor.RepaintClients ();
		}
		
		private void CheckCurrentCache () 
		{
			if (!useCache)
				return;
			if (nodeCanvas.livesInScene)
			{
				if (NodeEditorSaveManager.FindOrCreateSceneSave ("lastSession").savedNodeCanvas != nodeCanvas)
					throw new UnityException ("Cache system error: Current scene canvas is not saved as the temporary cache scene save!");
			}
			#if UNITY_EDITOR
			else if (UnityEditor.AssetDatabase.GetAssetPath (nodeCanvas) != lastSessionPath)
				throw new UnityException ("Cache system error: Current asset canvas is not saved as the temporary cache asset!");
			#endif
		}
		
		private void DeleteCache () 
		{
			if (!useCache)
				return;
			#if UNITY_EDITOR
			UnityEditor.AssetDatabase.DeleteAsset (lastSessionPath);
			UnityEditor.AssetDatabase.Refresh ();
			//UnityEditor.EditorPrefs.DeleteKey ("NodeEditorLastSession");
			#endif
			NodeEditorSaveManager.DeleteSceneNodeCanvas ("lastSession");
		}

		private void UpdateCacheFile () 
		{
			if (!useCache)
				return;
			#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty (nodeCanvas);
			UnityEditor.AssetDatabase.SaveAssets ();
			UnityEditor.AssetDatabase.Refresh ();
			#endif
		}

		#endregion

		#region Save/Load

		public void SetCanvas (NodeCanvas canvas)
		{
			if (nodeCanvas != canvas)
			{
				nodeCanvas = canvas;
				editorState = NodeEditorSaveManager.ExtractEditorState (nodeCanvas, MainEditorStateIdentifier);
				RecreateCache ();
				UpdateCanvasInfo ();
				NodeEditor.Calculator.RecalculateAll (nodeCanvas);
				NodeEditor.RepaintClients ();
			}
		}

		/// <summary>
		/// Saves the mainNodeCanvas and it's associated mainEditorState as an asset at path
		/// </summary>
		public void SaveSceneNodeCanvas (string path) 
		{
			nodeCanvas.editorStates = new NodeEditorState[] { editorState };
			bool switchedToScene = !nodeCanvas.livesInScene;
			NodeEditorSaveManager.SaveSceneNodeCanvas (path, ref nodeCanvas, true);
			editorState = NodeEditorSaveManager.ExtractEditorState (nodeCanvas, MainEditorStateIdentifier);
			if (switchedToScene)
				RecreateCache ();
			else
				SaveCache ();
			NodeEditor.RepaintClients ();
		}

		/// <summary>
		/// Loads the mainNodeCanvas and it's associated mainEditorState from an asset at path
		/// </summary>
		public void LoadSceneNodeCanvas (string path) 
		{
			// Try to load the NodeCanvas
			if ((nodeCanvas = NodeEditorSaveManager.LoadSceneNodeCanvas (path, true)) == null)
			{
				NewNodeCanvas ();
				return;
			}
			editorState = NodeEditorSaveManager.ExtractEditorState (nodeCanvas, MainEditorStateIdentifier);

			openedCanvasPath = path;
			RecreateCache ();
			UpdateCanvasInfo ();
			NodeEditor.Calculator.RecalculateAll (nodeCanvas);
			NodeEditor.RepaintClients ();
		}

		/// <summary>
		/// Saves the mainNodeCanvas and it's associated mainEditorState as an asset at path
		/// </summary>
		public void SaveNodeCanvas (string path) 
		{
			nodeCanvas.editorStates = new NodeEditorState[] { editorState };
			NodeEditorSaveManager.SaveNodeCanvas (path, nodeCanvas, true);
			SaveCache ();
			NodeEditor.RepaintClients ();
		}

		/// <summary>
		/// Loads the mainNodeCanvas and it's associated mainEditorState from an asset at path
		/// </summary>
		public void LoadNodeCanvas (string path) 
		{
			// Try to load the NodeCanvas
			if (!File.Exists (path) || (nodeCanvas = NodeEditorSaveManager.LoadNodeCanvas (path, true)) == null)
			{
				NewNodeCanvas ();
				return;
			}
			editorState = NodeEditorSaveManager.ExtractEditorState (nodeCanvas, MainEditorStateIdentifier);

			openedCanvasPath = path;
			RecreateCache ();
			UpdateCanvasInfo ();
			NodeEditor.Calculator.RecalculateAll (nodeCanvas);
			NodeEditor.RepaintClients ();
		}

		/// <summary>
		/// Creates and loads a new NodeCanvas
		/// </summary>
		public void NewNodeCanvas (Type canvasType = null) 
		{
			if (canvasType != null && canvasType.IsSubclassOf (typeof(NodeCanvas)))
				nodeCanvas = ScriptableObject.CreateInstance(canvasType) as NodeCanvas;
			else
				nodeCanvas = ScriptableObject.CreateInstance<NodeCanvas>();
			nodeCanvas.name = "New " + nodeCanvas.canvasName;

			//EditorPrefs.SetString ("NodeEditorLastSession", "New Canvas");
			NewEditorState ();
			openedCanvasPath = "";
			RecreateCache ();
			UpdateCanvasInfo ();
		}

		/// <summary>
		/// Creates a new EditorState for the current NodeCanvas
		/// </summary>
		public void NewEditorState () 
		{
			editorState = ScriptableObject.CreateInstance<NodeEditorState> ();
			editorState.canvas = nodeCanvas;
			editorState.name = MainEditorStateIdentifier;
			nodeCanvas.editorStates = new NodeEditorState[] { editorState };
			#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty (nodeCanvas);
			#endif
		}

		#endregion

		private void UpdateCanvasInfo () 
		{
			typeData = NodeCanvasManager.getCanvasTypeData (nodeCanvas);
		}
	}

}