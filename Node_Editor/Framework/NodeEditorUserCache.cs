//#define EDITOR_CACHE_ASSET

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

		#if EDITOR_CACHE_ASSET
		private const bool cacheWorkingCopy = false;
		#else
		private const bool cacheWorkingCopy = true;
		public static int cacheIntervalSec = 60;
		private double lastCacheTime;
		#endif
		private bool useCache;
		private string cachePath;
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
			#if UNITY_EDITOR
			if (!useCache)
				return;

			#if EDITOR_CACHE_ASSET
			// Add new objects to the cache save file
			NodeEditorCallbacks.OnAddNode -= SaveNewNode;
			NodeEditorCallbacks.OnAddNode += SaveNewNode;
			NodeEditorCallbacks.OnAddNodeKnob -= SaveNewNodeKnob;
			NodeEditorCallbacks.OnAddNodeKnob += SaveNewNodeKnob;
			#else
			UnityEditor.EditorApplication.update -= CheckCacheUpdate;
			UnityEditor.EditorApplication.update += CheckCacheUpdate;
			lastCacheTime = UnityEditor.EditorApplication.timeSinceStartup;
			#endif

			LoadCache ();
			#endif
		}

		public void ClearCacheEvents () 
		{
			#if UNITY_EDITOR && EDITOR_CACHE_ASSET
			NodeEditorCallbacks.OnAddNode -= SaveNewNode;
			NodeEditorCallbacks.OnAddNodeKnob -= SaveNewNodeKnob;
			#elif UNITY_EDITOR
			RecreateCache ();
			UnityEditor.EditorApplication.update -= CheckCacheUpdate;
			#endif
		}

		#if UNITY_EDITOR && !EDITOR_CACHE_ASSET
		private void CheckCacheUpdate () 
		{
			//Debug.Log ("Checking for cache save!");
			if (UnityEditor.EditorApplication.timeSinceStartup-lastCacheTime > cacheIntervalSec)
			{
				if (editorState.dragUserID == "" && editorState.connectOutput == null && GUIUtility.hotControl <= 0 && !OverlayGUI.HasPopupControl ())
				{ // Only save when the user currently does not perform an action that could be interrupted by the save
					lastCacheTime = UnityEditor.EditorApplication.timeSinceStartup;
					RecreateCache ();
				}
			}
		}
		#endif

		#if UNITY_EDITOR && EDITOR_CACHE_ASSET

		private void SaveNewNode (Node node) 
		{
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
		}

		private void SaveNewNodeKnob (NodeKnob knob) 
		{
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
		}

		#endif

		/// <summary>
		/// Creates a new cache save file for the currently loaded canvas 
		/// Only called when a new canvas is created or loaded
		/// </summary>
		private void RecreateCache () 
		{
			#if UNITY_EDITOR
			if (!useCache)
				return;
			DeleteCache ();
			SaveCache ();
			#endif
		}

		/// <summary>
		/// Creates a new cache save file for the currently loaded canvas 
		/// Only called when a new canvas is created or loaded
		/// </summary>
		private void SaveCache () 
		{
			#if UNITY_EDITOR
			if (!useCache)
				return;
			if (nodeCanvas.GetType () == typeof(NodeCanvas))
				return;
			UnityEditor.EditorUtility.SetDirty (nodeCanvas);
			if (editorState != null)
				UnityEditor.EditorUtility.SetDirty (editorState);
			#if !EDITOR_CACHE_ASSET
			lastCacheTime = UnityEditor.EditorApplication.timeSinceStartup;
			#endif
			nodeCanvas.editorStates = new NodeEditorState[] { editorState };
			if (nodeCanvas.livesInScene)
				NodeEditorSaveManager.SaveSceneNodeCanvas ("lastSession", ref nodeCanvas, cacheWorkingCopy);
			else
				NodeEditorSaveManager.SaveNodeCanvas (lastSessionPath, nodeCanvas, cacheWorkingCopy, false);

			CheckCurrentCache ();
			#endif
		}

		/// <summary>
		/// Loads the canvas from the cache save file
		/// Called whenever a reload was made
		/// </summary>
		private void LoadCache () 
		{
			#if UNITY_EDITOR
			if (!useCache)
			{
				NewNodeCanvas ();
				return;
			}
			// Try to load the NodeCanvas
			if (
				(!File.Exists (lastSessionPath) || (nodeCanvas = NodeEditorSaveManager.LoadNodeCanvas (lastSessionPath, cacheWorkingCopy)) == null) &&	// Check for asset cache
				(nodeCanvas = NodeEditorSaveManager.LoadSceneNodeCanvas ("lastSession", cacheWorkingCopy)) == null)										// Check for scene cache
			{
				NewNodeCanvas ();
				return;
			}

			// Fetch the associated MainEditorState
			editorState = NodeEditorSaveManager.ExtractEditorState (nodeCanvas, MainEditorStateIdentifier);
			if (!nodeCanvas.livesInScene && !UnityEditor.AssetDatabase.Contains (editorState))
				NodeEditorSaveManager.AddSubAsset (editorState, lastSessionPath);

			CheckCurrentCache ();
			UpdateCanvasInfo ();
			nodeCanvas.TraverseAll ();
			NodeEditor.RepaintClients ();
			#endif
		}
		
		private void CheckCurrentCache () 
		{
			#if UNITY_EDITOR && EDITOR_CACHE_ASSET
			if (!useCache)
				return;
			if (nodeCanvas.livesInScene)
			{
				if (NodeEditorSaveManager.FindOrCreateSceneSave ("lastSession").savedNodeCanvas != nodeCanvas)
					Debug.LogError ("Cache system error: Current scene canvas is not saved as the temporary cache scene save!");
			}
			else if (UnityEditor.AssetDatabase.GetAssetPath (nodeCanvas) != lastSessionPath)
				Debug.LogError ("Cache system error: Current asset canvas is not saved as the temporary cache asset!");
			#elif UNITY_EDITOR
			if (!useCache)
				return;
			if (nodeCanvas.livesInScene)
			{
				if (NodeEditorSaveManager.FindOrCreateSceneSave ("lastSession").savedNodeCanvas == null)
					RecreateCache ();
			}
			else if (UnityEditor.AssetDatabase.LoadAssetAtPath<NodeCanvas> (lastSessionPath) == null)
				RecreateCache ();
			#endif
		}
		
		private void DeleteCache () 
		{
			#if UNITY_EDITOR
			if (!useCache)
				return;
			UnityEditor.AssetDatabase.DeleteAsset (lastSessionPath);
			UnityEditor.AssetDatabase.Refresh ();
			NodeEditorSaveManager.DeleteSceneNodeCanvas ("lastSession");
			#endif
		}

		private void UpdateCacheFile () 
		{
			#if UNITY_EDITOR
			if (!useCache)
				return;
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
				canvas.Validate (true);
				nodeCanvas = canvas;
				editorState = NodeEditorSaveManager.ExtractEditorState (nodeCanvas, MainEditorStateIdentifier);
				RecreateCache ();
				UpdateCanvasInfo ();
				nodeCanvas.TraverseAll ();
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
			nodeCanvas.TraverseAll ();
			NodeEditor.RepaintClients ();
		}

		/// <summary>
		/// Saves the mainNodeCanvas and it's associated mainEditorState as an asset at path
		/// </summary>
		public void SaveNodeCanvas (string path) 
		{
			nodeCanvas.editorStates = new NodeEditorState[] { editorState };
			bool switchedToFile = nodeCanvas.livesInScene;
			NodeEditorSaveManager.SaveNodeCanvas (path, nodeCanvas, true);
			if (switchedToFile)
				RecreateCache ();
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
			nodeCanvas.TraverseAll ();
			NodeEditor.RepaintClients ();
		}

		/// <summary>
		/// Creates and loads a new NodeCanvas
		/// </summary>
		public void NewNodeCanvas (Type canvasType = null) 
		{
			nodeCanvas = NodeCanvas.CreateCanvas (canvasType);
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

		public void ConvertCanvasType (Type newType)
		{
			NodeCanvas canvas = NodeCanvasManager.ConvertCanvasType (nodeCanvas, newType);
			if (canvas != nodeCanvas)
			{
				nodeCanvas = canvas;
				RecreateCache ();
				UpdateCanvasInfo ();
				nodeCanvas.TraverseAll ();
				NodeEditor.RepaintClients ();
			}
		}

		private void UpdateCanvasInfo () 
		{
			typeData = NodeCanvasManager.getCanvasTypeData (nodeCanvas);
		}
	}

}