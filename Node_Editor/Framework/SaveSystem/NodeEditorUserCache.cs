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

		public Type defaultNodeCanvasType;
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
			SetupCacheEvents();
		}

		public NodeEditorUserCache (string CachePath)
		{
			useCache = true;
			cachePath = CachePath;
			SetupCacheEvents();
		}
#endif

		/// <summary>
		/// Assures a canvas is loaded, either from the cache or new
		/// </summary>
		public void AssureCanvas()
		{
			if (nodeCanvas == null)
				LoadCache ();
			if (nodeCanvas == null)
				NewNodeCanvas();
			if (editorState == null)
				NewEditorState();
		}

		#region Cache

		/// <summary>
		/// Subscribes the cache events needed for the cache to work properly
		/// </summary>
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

			EditorLoadingControl.beforeEnteringPlayMode -= SaveCache;
			EditorLoadingControl.beforeEnteringPlayMode += SaveCache;
			EditorLoadingControl.beforeLeavingPlayMode -= SaveCache;
			EditorLoadingControl.beforeLeavingPlayMode += SaveCache;
		#endif
		#endif
		}

		/// <summary>
		/// Unsubscribes all cache events
		/// </summary>
		public void ClearCacheEvents () 
		{
		#if UNITY_EDITOR && EDITOR_CACHE_ASSET
			NodeEditorCallbacks.OnAddNode -= SaveNewNode;
			NodeEditorCallbacks.OnAddNodeKnob -= SaveNewNodeKnob;
		#elif UNITY_EDITOR
			SaveCache ();
			UnityEditor.EditorApplication.update -= CheckCacheUpdate;
			EditorLoadingControl.beforeEnteringPlayMode -= SaveCache;
			EditorLoadingControl.beforeLeavingPlayMode -= SaveCache;
		#endif
		}

		#if UNITY_EDITOR && !EDITOR_CACHE_ASSET
		private void CheckCacheUpdate () 
		{
			if (UnityEditor.EditorApplication.timeSinceStartup-lastCacheTime > cacheIntervalSec)
			{
				AssureCanvas();
				if (editorState.dragUserID == "" && editorState.connectKnob == null && GUIUtility.hotControl <= 0 && !OverlayGUI.HasPopupControl ())
				{ // Only save when the user currently does not perform an action that could be interrupted by the save
					lastCacheTime = UnityEditor.EditorApplication.timeSinceStartup;
					SaveCache ();
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
		/// Saves the current canvas to the cache
		/// </summary>
		public void SaveCache () 
		{
		#if UNITY_EDITOR
			if (!useCache)
				return;
			if (!nodeCanvas || nodeCanvas.GetType () == typeof(NodeCanvas))
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
				NodeEditorSaveManager.SaveNodeCanvas (lastSessionPath, ref nodeCanvas, cacheWorkingCopy, true);
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
			{ // Simply create a ne canvas
				NewNodeCanvas ();
				return;
			}

			// Try to load the NodeCanvas
			if ((!File.Exists (lastSessionPath) || (nodeCanvas = NodeEditorSaveManager.LoadNodeCanvas (lastSessionPath, cacheWorkingCopy)) == null) &&	// Check for asset cache
				(nodeCanvas = NodeEditorSaveManager.LoadSceneNodeCanvas ("lastSession", cacheWorkingCopy)) == null)										// Check for scene cache
			{
				NewNodeCanvas ();
				return;
			}

			// Fetch the associated MainEditorState
			editorState = NodeEditorSaveManager.ExtractEditorState (nodeCanvas, MainEditorStateIdentifier);
		#if EDITOR_CACHE_ASSET
			if (!nodeCanvas.livesInScene && !UnityEditor.AssetDatabase.Contains (editorState))
				NodeEditorSaveManager.AddSubAsset (editorState, lastSessionPath);
		#endif

			UpdateCanvasInfo ();
			nodeCanvas.Validate ();
			nodeCanvas.TraverseAll ();
			NodeEditor.RepaintClients ();

		#endif
		}
		
		/// <summary>
		/// Makes sure the current canvas is saved to the cache
		/// </summary>
		private void CheckCurrentCache () 
		{
#if UNITY_EDITOR
			if (!useCache)
				return;
#if EDITOR_CACHE_ASSET
			if (nodeCanvas.livesInScene)
			{
				if (NodeEditorSaveManager.FindOrCreateSceneSave ("lastSession").savedNodeCanvas != nodeCanvas)
					Debug.LogError ("Cache system error: Current scene canvas is not saved as the temporary cache scene save!");
			}
			else if (UnityEditor.AssetDatabase.GetAssetPath (nodeCanvas) != lastSessionPath)
				Debug.LogError ("Cache system error: Current asset canvas is not saved as the temporary cache asset!");
#else
			if (nodeCanvas.livesInScene)
			{
				if (NodeEditorSaveManager.FindOrCreateSceneSave ("lastSession").savedNodeCanvas == null)
					SaveCache ();
			}
			else if (UnityEditor.AssetDatabase.LoadAssetAtPath<NodeCanvas> (lastSessionPath) == null)
				SaveCache ();
#endif
#endif
		}
		
		/// <summary>
		/// Deletes the cache
		/// </summary>
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

		/// <summary>
		/// Sets the cache dirty and as makes sure it's saved
		/// </summary>
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

		/// <summary>
		/// Sets the current canvas, handling all cache operations
		/// </summary>
		public void SetCanvas (NodeCanvas canvas)
		{
			if (canvas == null)
				NewNodeCanvas();
			else if (nodeCanvas != canvas)
			{
				canvas.Validate ();
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
			if (path.StartsWith ("SCENE/"))
				path = path.Substring (6);
			
			// Try to load the NodeCanvas
			if ((nodeCanvas = NodeEditorSaveManager.LoadSceneNodeCanvas (path, true)) == null)
			{
				NewNodeCanvas ();
				return;
			}
			editorState = NodeEditorSaveManager.ExtractEditorState (nodeCanvas, MainEditorStateIdentifier);

			openedCanvasPath = path;
			nodeCanvas.Validate();
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
			NodeEditorSaveManager.SaveNodeCanvas (path, ref nodeCanvas, true);
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
			nodeCanvas.Validate();
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
			canvasType = canvasType ?? defaultNodeCanvasType;
			nodeCanvas = NodeCanvas.CreateCanvas (canvasType);
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
			typeData = NodeCanvasManager.GetCanvasTypeData (nodeCanvas);
		}
	}

}