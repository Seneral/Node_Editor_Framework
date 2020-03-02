#if UNITY_EDITOR
#define CACHE
#endif

using System;
using System.IO;
using System.Linq;
using UnityEngine;

using NodeEditorFramework.Utilities;

namespace NodeEditorFramework
{
	public class NodeEditorUserCache
	{
		public NodeCanvas nodeCanvas;
		public NodeEditorState editorState;
		public string openedCanvasPath = "";

		public Type defaultNodeCanvasType;
		public NodeCanvasTypeData typeData;

		private const string MainEditorStateIdentifier = "MainEditorState";

#if CACHE
		private const bool cacheWorkingCopy = true;
		private const bool cacheMemorySODump = true;
		private const int cacheIntervalSec = 60;

		private bool useCache = false;
		private double lastCacheTime;
		private string cachePath;
		private string lastSessionPath { get { return cachePath + "LastSession.asset"; } }
		private string SOMemoryDumpPath { get { return cachePath + "CurSession.asset"; } }
#endif


		#region Setup

		public NodeEditorUserCache(NodeCanvas loadedCanvas)
		{
			SetCanvas(loadedCanvas);
		}

		public NodeEditorUserCache()
		{ }

		public NodeEditorUserCache(string CachePath, NodeCanvas loadedCanvas)
		{
#if CACHE
			useCache = true;
			cachePath = CachePath;
			SetupCacheEvents();
#endif
			SetCanvas(loadedCanvas);
		}

		public NodeEditorUserCache(string CachePath)
		{
#if CACHE
			useCache = true;
			cachePath = CachePath;
			SetupCacheEvents();
#endif
		}


		/// <summary>
		/// Assures a canvas is loaded, either from the cache or new
		/// </summary>
		public void AssureCanvas()
		{
#if CACHE
			if (nodeCanvas == null)
				LoadCache ();
#endif
			if (nodeCanvas == null)
				NewNodeCanvas();
			if (editorState == null)
				NewEditorState();
		}

		#endregion

		#region Cache

		/// <summary>
		/// Subscribes the cache events needed for the cache to work properly
		/// </summary>
		public void SetupCacheEvents ()
		{
#if UNITY_EDITOR && CACHE
			if (!useCache)
				return;

			UnityEditor.EditorApplication.update -= CheckCacheUpdate;
			UnityEditor.EditorApplication.update += CheckCacheUpdate;
			lastCacheTime = UnityEditor.EditorApplication.timeSinceStartup;

			EditorLoadingControl.beforeEnteringPlayMode -= SaveCache;
			EditorLoadingControl.beforeEnteringPlayMode += SaveCache;
			EditorLoadingControl.beforeLeavingPlayMode -= SaveCache;
			EditorLoadingControl.beforeLeavingPlayMode += SaveCache;
			EditorLoadingControl.justEnteredPlayMode -= LoadCache;
			EditorLoadingControl.justEnteredPlayMode += LoadCache;
#endif
		}

		/// <summary>
		/// Unsubscribes all cache events
		/// </summary>
		public void ClearCacheEvents ()
		{
#if UNITY_EDITOR && CACHE
			SaveCache();
			UnityEditor.EditorApplication.update -= CheckCacheUpdate;
			EditorLoadingControl.beforeEnteringPlayMode -= SaveCache;
			EditorLoadingControl.beforeLeavingPlayMode -= SaveCache;
			EditorLoadingControl.justEnteredPlayMode -= LoadCache;
#endif
		}

		private void CheckCacheUpdate ()
		{
#if UNITY_EDITOR && CACHE
			if (UnityEditor.EditorApplication.timeSinceStartup-lastCacheTime > cacheIntervalSec)
			{
				AssureCanvas();
				if (editorState.dragUserID == "" && editorState.connectKnob == null && GUIUtility.hotControl <= 0 && !OverlayGUI.HasPopupControl ())
				{ // Only save when the user currently does not perform an action that could be interrupted by the save
					lastCacheTime = UnityEditor.EditorApplication.timeSinceStartup;
					SaveCache ();
				}
			}
#endif
		}

		/// <summary>
		/// Creates a new cache save file for the currently loaded canvas 
		/// Only called when a new canvas is created or loaded
		/// </summary>
		private void RecreateCache () 
		{
#if CACHE
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
			SaveCache(true);
		}

		/// <summary>
		/// Saves the current canvas to the cache
		/// </summary>
		public void SaveCache (bool crashSafe = true) 
		{
#if CACHE
			if (!useCache)
				return;
			if (!nodeCanvas || nodeCanvas.GetType () == typeof(NodeCanvas))
				return;
			UnityEditor.EditorUtility.SetDirty (nodeCanvas);
			if (editorState != null)
				UnityEditor.EditorUtility.SetDirty (editorState);
			lastCacheTime = UnityEditor.EditorApplication.timeSinceStartup;

			nodeCanvas.editorStates = new NodeEditorState[] { editorState };
			if (nodeCanvas.livesInScene || nodeCanvas.allowSceneSaveOnly)
				NodeEditorSaveManager.SaveSceneNodeCanvas ("lastSession", ref nodeCanvas, cacheWorkingCopy);
			else if (crashSafe)
				NodeEditorSaveManager.SaveNodeCanvas (lastSessionPath, ref nodeCanvas, cacheWorkingCopy, true);
			
			if (cacheMemorySODump)
			{ // Functionality for asset saves only
				if (nodeCanvas.livesInScene || nodeCanvas.allowSceneSaveOnly)
				{ // Delete for scene save so that next cache load, correct lastSession is used
					UnityEditor.AssetDatabase.DeleteAsset(SOMemoryDumpPath);
				}
				else
				{ // Dump all SOs used in this session (even if deleted) in this file to keep them alive for undo
					NodeEditorUndoActions.CompleteSOMemoryDump(nodeCanvas);
					NodeEditorSaveManager.ScriptableObjectReferenceDump(nodeCanvas.SOMemoryDump, SOMemoryDumpPath, false);
				}
			}
#endif
		}

		/// <summary>
		/// Loads the canvas from the cache save file
		/// Called whenever a reload was made
		/// </summary>
		public void LoadCache ()
		{
#if CACHE
			if (!useCache)
			{ // Simply create a ne canvas
				NewNodeCanvas ();
				return;
			}

			bool skipLoad = false;
			if (cacheMemorySODump)
			{ // Check if a memory dump has been found, if so, load that
				nodeCanvas = ResourceManager.LoadResource<NodeCanvas>(SOMemoryDumpPath);
				if (nodeCanvas != null && !nodeCanvas.Validate(false))
				{
					Debug.LogWarning("Cache Dump corrupted! Loading crash-proof lastSession, you might have lost a bit of work. \n "
						+ "To prevent this from happening in the future, allow the Node Editor to properly save the cache "
						+ "by clicking out of the window before switching scenes, since there are no callbacks to facilitate this!");
					nodeCanvas = null;
					UnityEditor.AssetDatabase.DeleteAsset(SOMemoryDumpPath);

				}
				if (nodeCanvas != null)
					skipLoad = true;

			}

			// Try to load the NodeCanvas
			if (!skipLoad && 
				(!File.Exists (lastSessionPath) || (nodeCanvas = NodeEditorSaveManager.LoadNodeCanvas (lastSessionPath, cacheWorkingCopy)) == null) &&	// Check for asset cache
				(nodeCanvas = NodeEditorSaveManager.LoadSceneNodeCanvas ("lastSession", cacheWorkingCopy)) == null)										// Check for scene cache
			{
				NewNodeCanvas ();
				return;
			}

			// Fetch the associated MainEditorState
			editorState = NodeEditorSaveManager.ExtractEditorState (nodeCanvas, MainEditorStateIdentifier);
			UpdateCanvasInfo ();
			nodeCanvas.Validate ();
			nodeCanvas.TraverseAll ();
			NodeEditor.RepaintClients ();
#endif
		}

#if CACHE

		/// <summary>
		/// Makes sure the current canvas is saved to the cache
		/// </summary>
		private void CheckCurrentCache () 
		{
			if (!useCache)
				return;
			if (nodeCanvas.livesInScene)
			{
				if (!NodeEditorSaveManager.HasSceneSave ("lastSession"))
					SaveCache ();
			}
			else if (UnityEditor.AssetDatabase.LoadAssetAtPath<NodeCanvas> (lastSessionPath) == null)
				SaveCache ();
		}
		
		/// <summary>
		/// Deletes the cache
		/// </summary>
		private void DeleteCache () 
		{
			if (!useCache)
				return;
			UnityEditor.AssetDatabase.DeleteAsset (SOMemoryDumpPath);
			UnityEditor.AssetDatabase.DeleteAsset (lastSessionPath);
			UnityEditor.AssetDatabase.Refresh ();
			NodeEditorSaveManager.DeleteSceneNodeCanvas ("lastSession");
		}

		/// <summary>
		/// Sets the cache dirty and as makes sure it's saved
		/// </summary>
		private void UpdateCacheFile () 
		{
			if (!useCache)
				return;
			UnityEditor.EditorUtility.SetDirty (nodeCanvas);
			UnityEditor.AssetDatabase.SaveAssets ();
			UnityEditor.AssetDatabase.Refresh ();
		}
#endif

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
			else 
				SaveCache (false);
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
			canvasType = canvasType ?? defaultNodeCanvasType ?? // Pick first canvas in alphabetical order (Calculation usually)
				NodeCanvasManager.getCanvasDefinitions().OrderBy(c => c.DisplayString).First().CanvasType;
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
			if (!nodeCanvas) return;
			editorState.canvas = nodeCanvas;
			editorState.name = MainEditorStateIdentifier;
			nodeCanvas.editorStates = new NodeEditorState[] { editorState };
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty (nodeCanvas);
#endif
		}

		#endregion

		#region Utility

		public void ConvertCanvasType(Type newType)
		{
			NodeCanvas canvas = NodeCanvasManager.ConvertCanvasType(nodeCanvas, newType);
			if (canvas != nodeCanvas)
			{
				nodeCanvas = canvas;
				RecreateCache();
				UpdateCanvasInfo();
				nodeCanvas.TraverseAll();
				NodeEditor.RepaintClients();
			}
		}

		private void UpdateCanvasInfo()
		{
			typeData = NodeCanvasManager.GetCanvasTypeData(nodeCanvas);
		}

		#endregion
	}

}