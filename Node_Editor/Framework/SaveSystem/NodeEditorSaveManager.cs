using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using NodeEditorFramework;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework 
{
	/// <summary>
	/// Manager handling all save and load operations on NodeCanvases and NodeEditorStates of the Node Editor, both as assets and in the scene
	/// </summary>
	public static partial class NodeEditorSaveManager 
	{
		#region Scene Saving

		private static GameObject sceneSaveHolder;
		private const string sceneSaveHolderName = "NodeEditor_SceneSaveHolder";

		/// <summary>
		/// Fetches the sceneSaveHolder of the current scene or creates it
		/// </summary>
		private static void FetchSceneSaveHolder () 
		{
			if (sceneSaveHolder == null)
			{
				// TODO: Might need to check here if the object is in the active scene / the system works with multiple scenes
				sceneSaveHolder = GameObject.Find (sceneSaveHolderName);
				if (sceneSaveHolder == null)
					sceneSaveHolder = new GameObject (sceneSaveHolderName);
				sceneSaveHolder.hideFlags = HideFlags.None;//HideFlags.HideInHierarchy | HideFlags.HideInInspector;
			}
		}

		#region Utility

		/// <summary>
		/// Gets all existing stored saves in the current scene and returns their names
		/// </summary>
		public static string[] GetSceneSaves ()
		{
			FetchSceneSaveHolder ();
			return sceneSaveHolder.GetComponents<NodeCanvasSceneSave> ().Select (((NodeCanvasSceneSave save) => save.saveName)).ToArray ();
		}

		/// <summary>
		/// Returns whether a sceneSave with the specified name exists in the current scene
		/// </summary>
		public static bool HasSceneSave (string saveName)
		{
			FetchSceneSaveHolder ();
			return sceneSaveHolder.GetComponents<NodeCanvasSceneSave> ().ToList ().Exists ((NodeCanvasSceneSave save) => 
				save.saveName.ToLower () == saveName.ToLower () || (save.savedNodeCanvas != null && save.savedNodeCanvas.name.ToLower () == saveName.ToLower ()));
		}

		/// <summary>
		/// Returns the scene save with the specified name in the current scene
		/// </summary>
		internal static NodeCanvasSceneSave FindSceneSave (string saveName)
		{
			FetchSceneSaveHolder ();
			NodeCanvasSceneSave sceneSave = sceneSaveHolder.GetComponents<NodeCanvasSceneSave> ().ToList ().Find ((NodeCanvasSceneSave save) => 
				save.saveName.ToLower () == saveName.ToLower () || (save.savedNodeCanvas != null && save.savedNodeCanvas.name.ToLower () == saveName.ToLower ()));
			if (sceneSave != null)
				sceneSave.saveName = saveName;
			return sceneSave;
		}

		/// <summary>
		/// Returns the scene save with the specified name in the current scene and creates it when it does not exist
		/// </summary>
		internal static NodeCanvasSceneSave FindOrCreateSceneSave (string saveName)
		{
			FetchSceneSaveHolder ();
			NodeCanvasSceneSave sceneSave = sceneSaveHolder.GetComponents<NodeCanvasSceneSave> ().ToList ().Find ((NodeCanvasSceneSave save) => 
				save.saveName.ToLower () == saveName.ToLower () || (save.savedNodeCanvas != null && save.savedNodeCanvas.name.ToLower () == saveName.ToLower ()));
			if (sceneSave == null)
				sceneSave = sceneSaveHolder.AddComponent<NodeCanvasSceneSave> ();
			sceneSave.saveName = saveName;
			return sceneSave;
		}

		/// <summary>
		/// Creates a scene save with the specified name in the current scene
		/// </summary>
		internal static NodeCanvasSceneSave CreateSceneSave (string saveName)
		{
			FetchSceneSaveHolder ();
			NodeCanvasSceneSave sceneSave = sceneSaveHolder.AddComponent<NodeCanvasSceneSave> ();
			sceneSave.saveName = saveName;
			return sceneSave;
		}

		/// <summary>
		/// Deletes the nodeCanvas stored in the current scene under the specified name
		/// </summary>
		public static void DeleteSceneNodeCanvas (string saveName)
		{
			if (string.IsNullOrEmpty (saveName))
				return;
			FetchSceneSaveHolder ();
			NodeCanvasSceneSave sceneSave = FindSceneSave (saveName);
			if (sceneSave != null)
			{
			#if UNITY_EDITOR
				Object.DestroyImmediate (sceneSave);
			#else
				Object.Destroy (sceneSave);
			#endif
			}
		}

		#endregion

		/// <summary>
		/// Saves the nodeCanvas in the current scene under the specified name, optionally as a working copy and overwriting any existing save at path
		/// If the specified canvas is stored as an asset, the saved canvas will loose the reference to the asset
		/// </summary>
		public static void SaveSceneNodeCanvas (string saveName, ref NodeCanvas nodeCanvas, bool createWorkingCopy, bool safeOverwrite = true) 
		{
			if (string.IsNullOrEmpty (saveName)) throw new System.ArgumentNullException ("Cannot save Canvas to scene: No save name specified!");
			if (nodeCanvas == null) throw new System.ArgumentNullException ("Cannot save NodeCanvas: The specified NodeCanvas that should be saved as '" + saveName + "' is null!");
			if (nodeCanvas.GetType () == typeof(NodeCanvas)) throw new System.ArgumentException ("Cannot save NodeCanvas: The NodeCanvas has no explicit type! Please convert it to a valid sub-type of NodeCanvas!");

			if (saveName.StartsWith ("SCENE/"))
				saveName = saveName.Substring (6);
			
			if (!createWorkingCopy && (!nodeCanvas.livesInScene
		#if UNITY_EDITOR // Make sure the canvas has no reference to an asset
			|| UnityEditor.AssetDatabase.Contains (nodeCanvas)
		#endif
			)) {
				//Debug.LogWarning ("Forced to create working copy of '" + saveName + "' when saving to scene because it already exists as an asset!");
				createWorkingCopy = true;
			}

			NodeCanvas savedCanvas = nodeCanvas;
			// Preprocess canvas
			nodeCanvas.OnBeforeSavingCanvas();
			ProcessCanvas (ref savedCanvas, createWorkingCopy);

			// Update the source of the canvas
			nodeCanvas.UpdateSource ("SCENE/" + saveName);

			// Get the saveHolder and store the canvas
			NodeCanvasSceneSave sceneSave;
		#if UNITY_EDITOR
			if ((sceneSave = FindSceneSave (saveName)) != null && safeOverwrite)
			{ // OVERWRITE
				OverwriteCanvas (ref sceneSave.savedNodeCanvas, savedCanvas);
			}
			else
			{
				if (sceneSave == null) 
					sceneSave = CreateSceneSave (saveName);
				sceneSave.savedNodeCanvas = savedCanvas;
			}
			if (!Application.isPlaying)
			{
				UnityEditor.EditorUtility.SetDirty (sceneSaveHolder);
			#if UNITY_5_3_OR_NEWER
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty (UnityEngine.SceneManagement.SceneManager.GetActiveScene ());
			#else
				UnityEditor.EditorApplication.MarkSceneDirty ();
			#endif
			}
		#else
			sceneSave = FindOrCreateSceneSave (saveName);
			sceneSave.savedNodeCanvas = savedCanvas;
		#endif

			NodeEditorCallbacks.IssueOnSaveCanvas (savedCanvas);
		}

		/// <summary>
		/// Loads the nodeCanvas stored in the current scene under the specified name and optionally creates a working copy of it before returning
		/// </summary>
		public static NodeCanvas LoadSceneNodeCanvas (string saveName, bool createWorkingCopy)
		{
			if (string.IsNullOrEmpty (saveName))
				throw new System.ArgumentNullException ("Cannot load Canvas from scene: No save name specified!");

			if (saveName.StartsWith ("SCENE/"))
				saveName = saveName.Substring (6);
			
			// Load SceneSave
			NodeCanvasSceneSave sceneSave = FindSceneSave (saveName);
			if (sceneSave == null || sceneSave.savedNodeCanvas == null)
				return null;

			// Extract the saved canvas and editorStates
			NodeCanvas savedCanvas = sceneSave.savedNodeCanvas;

			// Set the saveName as the new source of the canvas
			savedCanvas.UpdateSource ("SCENE/" + saveName);

			// Postprocess the loaded canvas
			ProcessCanvas (ref savedCanvas, createWorkingCopy);

			NodeEditorCallbacks.IssueOnLoadCanvas (savedCanvas);
			return savedCanvas;
		}

		#endregion

		#region Asset Saving

		/// <summary>
		/// Saves the the specified NodeCanvas as a new asset at path, optionally as a working copy and overwriting any existing save at path
		/// </summary>
		public static void SaveNodeCanvas (string path, ref NodeCanvas nodeCanvas, bool createWorkingCopy, bool safeOverwrite = true) 
		{
		#if !UNITY_EDITOR
			throw new System.NotImplementedException ();

			// TODO: Node Editor: Need to implement ingame-saving (Resources, AsssetBundles, ... won't work)
		#endif

			if (string.IsNullOrEmpty (path)) throw new System.ArgumentNullException ("Cannot save NodeCanvas: No path specified!");
			if (nodeCanvas == null) throw new System.ArgumentNullException ("Cannot save NodeCanvas: The specified NodeCanvas that should be saved to path '" + path + "' is null!");
			if (nodeCanvas.GetType () == typeof(NodeCanvas)) throw new System.ArgumentException ("Cannot save NodeCanvas: The NodeCanvas has no explicit type! Please convert it to a valid sub-type of NodeCanvas!");

			bool updateRef = false;
			if (nodeCanvas.livesInScene)
			{
				Debug.LogWarning ("Attempting to save scene canvas '" + nodeCanvas.name + "' to an asset, references to scene object may be broken!" + (!createWorkingCopy? " Forcing creation of working copy!" : ""));
				createWorkingCopy = updateRef = true;
			}
		#if UNITY_EDITOR
			if (!createWorkingCopy && UnityEditor.AssetDatabase.Contains (nodeCanvas) && UnityEditor.AssetDatabase.GetAssetPath (nodeCanvas) != path) 
			{ 
				Debug.LogError ("Trying to create a duplicate save file for '" + nodeCanvas.name + "'! Forcing creation of working copy!"); 
				createWorkingCopy = true; 
			}
		#endif

			NodeCanvas canvasSave = nodeCanvas;

			// Preprocess the canvas
			canvasSave.OnBeforeSavingCanvas ();
			ProcessCanvas (ref canvasSave, createWorkingCopy);
			if (updateRef)
				nodeCanvas = canvasSave;

			// Prepare and update source path of the canvas
			path = ResourceManager.PreparePath (path);
			canvasSave.UpdateSource (path);

			// Differenciate canvasSave as the canvas asset and nodeCanvas as the source incase an existing save has been overwritten
			NodeCanvas prevSave;
			if (safeOverwrite && (prevSave = ResourceManager.LoadResource<NodeCanvas> (path)) != null && prevSave.GetType () == canvasSave.GetType ())
			{ // OVERWRITE: Delete contents of old save
				for (int nodeCnt = 0; nodeCnt < prevSave.nodes.Count; nodeCnt++) 
				{
					Node node = prevSave.nodes[nodeCnt];
					for (int knobCnt = 0; knobCnt < node.nodeKnobs.Count; knobCnt++)
					{
						if (node.nodeKnobs[knobCnt] != null)
							Object.DestroyImmediate (node.nodeKnobs[knobCnt], true);
					}
					Object.DestroyImmediate (node, true);
				}
				for (int i = 0; i < prevSave.editorStates.Length; i++)
				{
					if (prevSave.editorStates[i] != null)
						Object.DestroyImmediate (prevSave.editorStates[i], true);
				}
				// Overwrite main canvas
				OverwriteCanvas (ref prevSave, nodeCanvas);
				canvasSave = prevSave;
			}
			else
			{ // Write main canvas
				UnityEditor.AssetDatabase.CreateAsset (nodeCanvas, path);
			}

			// Write editorStates
			AddSubAssets (nodeCanvas.editorStates, canvasSave);
			// Write nodes + contents
			foreach (Node node in nodeCanvas.nodes)
			{ // Write node and additional scriptable objects
				AddSubAsset (node, canvasSave);
				AddSubAssets (node.GetScriptableObjects (), node);
				foreach (NodeKnob knob in node.nodeKnobs)
					AddSubAsset (knob, node);
			}

			//UnityEditor.AssetDatabase.SaveAssets ();
			//UnityEditor.AssetDatabase.Refresh ();

			NodeEditorCallbacks.IssueOnSaveCanvas (canvasSave);
		}

		/// <summary>
		/// Loads the NodeCanvas from the asset file at path and optionally creates a working copy of it before returning
		/// </summary>
		public static NodeCanvas LoadNodeCanvas (string path, bool createWorkingCopy) 
		{
			if (string.IsNullOrEmpty (path))
				throw new System.ArgumentNullException ("Cannot load Canvas: No path specified!");
			path = ResourceManager.PreparePath (path);

			// Load only the NodeCanvas from the save file
			NodeCanvas nodeCanvas = ResourceManager.LoadResource<NodeCanvas> (path);
			if (nodeCanvas == null) 
				throw new UnityException ("Cannot load NodeCanvas: The file at the specified path '" + path + "' is no valid save file as it does not contain a NodeCanvas!");

		#if UNITY_EDITOR
			if (!Application.isPlaying && (nodeCanvas.editorStates == null || nodeCanvas.editorStates.Length == 0))
			{ // Try to load any contained editorStates, as the canvas did not reference any
				nodeCanvas.editorStates = ResourceManager.LoadResources<NodeEditorState> (path);
			}
		#endif

			// Set the path as the new source of the canvas
			nodeCanvas.UpdateSource (path);

			// Postprocess the loaded canvas
			ProcessCanvas (ref nodeCanvas, createWorkingCopy);

			NodeEditorCallbacks.IssueOnLoadCanvas (nodeCanvas);
			return nodeCanvas;
		}

		#region Utility

		#if UNITY_EDITOR

		/// <summary>
		/// Adds the specified hidden subAssets to the mainAsset
		/// </summary>
		public static void AddSubAssets (ScriptableObject[] subAssets, ScriptableObject mainAsset) 
		{
			foreach (ScriptableObject subAsset in subAssets)
				AddSubAsset (subAsset, mainAsset); 
		}

		/// <summary>
		/// Adds the specified hidden subAsset to the mainAsset
		/// </summary>
		public static void AddSubAsset (ScriptableObject subAsset, ScriptableObject mainAsset) 
		{
			if (subAsset != null && mainAsset != null)
			{
				UnityEditor.AssetDatabase.AddObjectToAsset (subAsset, mainAsset);
				subAsset.hideFlags = HideFlags.HideInHierarchy;
			}
		}

		/// <summary>
		/// Adds the specified hidden subAsset to the mainAsset at path
		/// </summary>
		public static void AddSubAsset (ScriptableObject subAsset, string path) 
		{
			if (subAsset != null && !string.IsNullOrEmpty (path))
			{
				UnityEditor.AssetDatabase.AddObjectToAsset (subAsset, path);
				subAsset.hideFlags = HideFlags.HideInHierarchy;
			}
		}

		#endif

		/// <summary>
		/// Applies a general process on the canvas for loading/saving operations
		/// </summary>
		private static void ProcessCanvas (ref NodeCanvas canvas, bool workingCopy) 
		{
			//Uncompress (ref canvas);
			if (workingCopy)
				canvas = CreateWorkingCopy (canvas, true);
			else
				canvas.Validate (true);
		}

		#endregion

		#endregion

		#region Working Copy

		/// <summary>
		/// Creates a working copy of the specified nodeCanvas, and optionally also of it's associated editorStates.
		/// This breaks the link of this object to any stored assets and references. That means, that all changes to this object will have to be explicitly saved.
		/// </summary>
		public static NodeCanvas CreateWorkingCopy (NodeCanvas nodeCanvas, bool editorStates) 
		{
			nodeCanvas.Validate (true);

			// Lists holding initial and cloned version of each ScriptableObject for later replacement of references
			List<ScriptableObject> allSOs = new List<ScriptableObject> ();
			List<ScriptableObject> clonedSOs = new List<ScriptableObject> ();
			System.Func<ScriptableObject, ScriptableObject> copySOs = (ScriptableObject so) => ReplaceSO (allSOs, clonedSOs, so);

			// Clone and enter the canvas object and it's referenced SOs
			nodeCanvas = AddClonedSO (allSOs, clonedSOs, nodeCanvas);
			AddClonedSOs (allSOs, clonedSOs, nodeCanvas.GetScriptableObjects ());

			// Iterate over the core ScriptableObjects in the canvas and clone them
			for (int nodeCnt = 0; nodeCnt < nodeCanvas.nodes.Count; nodeCnt++) 
			{
				Node node = nodeCanvas.nodes[nodeCnt];
				node.CheckNodeKnobMigration ();

				// Clone Node and additional scriptableObjects
				Node clonedNode = AddClonedSO (allSOs, clonedSOs, node);
				AddClonedSOs (allSOs, clonedSOs, clonedNode.GetScriptableObjects ());

				// Clone NodeKnobs
				foreach (NodeKnob knob in clonedNode.nodeKnobs)
					AddClonedSO (allSOs, clonedSOs, knob);
			}

			// Replace every reference to any of the initial SOs of the first list with the respective clones of the second list
			nodeCanvas.CopyScriptableObjects (copySOs);

			for (int nodeCnt = 0; nodeCnt < nodeCanvas.nodes.Count; nodeCnt++) 
			{ // Replace SOs in all Nodes
				Node node = nodeCanvas.nodes[nodeCnt];

				// Replace node and additional ScriptableObjects with their copies
				Node clonedNode = nodeCanvas.nodes[nodeCnt] = ReplaceSO (allSOs, clonedSOs, node);
				clonedNode.CopyScriptableObjects (copySOs);

				// Replace NodeKnobs and restore Inputs/Outputs by NodeKnob type
				clonedNode.Inputs = new List<NodeInput> ();
				clonedNode.Outputs = new List<NodeOutput> ();
				for (int knobCnt = 0; knobCnt < clonedNode.nodeKnobs.Count; knobCnt++) 
				{ // Clone generic NodeKnobs
					NodeKnob knob = clonedNode.nodeKnobs[knobCnt] = ReplaceSO (allSOs, clonedSOs, clonedNode.nodeKnobs[knobCnt]);
					knob.body = clonedNode;
					knob.CopyScriptableObjects (copySOs);
					// Add inputs/outputs to their lists again
					if (knob is NodeInput)
						clonedNode.Inputs.Add (knob as NodeInput);
					else if (knob is NodeOutput) 
						clonedNode.Outputs.Add (knob as NodeOutput);
				}
			}

			if (editorStates) // Clone the editorStates
				nodeCanvas.editorStates = CreateWorkingCopy (nodeCanvas.editorStates, nodeCanvas);
			// Fix references in editorStates to Nodes in the canvas
			foreach (NodeEditorState state in nodeCanvas.editorStates)
				state.selectedNode = ReplaceSO (allSOs, clonedSOs, state.selectedNode);

			return nodeCanvas;
		}

		/// <summary>
		/// Creates a working copy of the specified editorStates. Also remains the link of the canvas to these associated editorStates.
		/// This breaks the link of this object to any stored assets and references. That means, that all changes to this object will have to be explicitly saved.
		/// </summary>
		private static NodeEditorState[] CreateWorkingCopy (NodeEditorState[] editorStates, NodeCanvas associatedNodeCanvas) 
		{
			if (editorStates == null)
				return new NodeEditorState[0];
			// Clone list
			List<NodeEditorState> clonedStates = new List<NodeEditorState> (editorStates.Length);
			for (int stateCnt = 0; stateCnt < editorStates.Length; stateCnt++) 
			{
				if (editorStates[stateCnt] == null)
					continue;
				// Clone editorState
				NodeEditorState state = Clone (editorStates[stateCnt]);
				if (state != null) 
				{ // Add it to the clone list
					state.canvas = associatedNodeCanvas;
					clonedStates.Add (state);
				}
			}
			associatedNodeCanvas.editorStates = clonedStates.ToArray ();
			return associatedNodeCanvas.editorStates;
		}

		#region Utility

		/// <summary>
		/// Clones the specified SO, preserving its name
		/// </summary>
		private static T Clone<T> (T SO) where T : ScriptableObject 
		{
			string soName = SO.name;
			SO = Object.Instantiate<T> (SO);
			SO.name = soName;
			return SO;
		}

		/// <summary>
		/// Clones SOs and writes both the initial and cloned versions into the respective list
		/// </summary>
		private static void AddClonedSOs (List<ScriptableObject> scriptableObjects, List<ScriptableObject> clonedScriptableObjects, ScriptableObject[] initialSOs)
		{
			// Filter out all new SOs and clone them
			IEnumerable<ScriptableObject> newSOs = initialSOs.Where ((ScriptableObject so) => !scriptableObjects.Contains (so));
			scriptableObjects.AddRange (newSOs);
			clonedScriptableObjects.AddRange (newSOs.Select ((ScriptableObject so) => Clone (so)));
		}

		/// <summary>
		/// Clones SO and writes both the initial and cloned versions into the respective list
		/// </summary>
		private static T AddClonedSO<T> (List<ScriptableObject> scriptableObjects, List<ScriptableObject> clonedScriptableObjects, T initialSO) where T : ScriptableObject 
		{
			if (initialSO == null)
				return null;
			// Do not clone again if it already has been
			int existing;
			if ((existing = scriptableObjects.IndexOf (initialSO)) >= 0)
				return (T)clonedScriptableObjects[existing];
			// Clone SO and add both versions to the respective list
			scriptableObjects.Add (initialSO);
			T clonedSO = Clone (initialSO);
			clonedScriptableObjects.Add (clonedSO);
			return clonedSO;
		}

		/// <summary>
		/// Returns a clone of initialSO and adds both versions to their respective list for later replacement
		/// </summary>
		private static T ReplaceSO<T> (List<ScriptableObject> scriptableObjects, List<ScriptableObject> clonedScriptableObjects, T initialSO) where T : ScriptableObject 
		{
			if (initialSO == null)
				return null;
			int soInd = scriptableObjects.IndexOf (initialSO);
			if (soInd < 0)
				Debug.LogError ("GetWorkingCopy: ScriptableObject " + initialSO.name + " was not copied before! It will be null!");
			return soInd < 0? null : (T)clonedScriptableObjects[soInd];
		}

		#endregion

		#endregion

		#region Utility

		/// <summary>
		/// Returns the editorState with the specified name in canvas. If not found it will create a new one with that name.
		/// </summary>
		public static NodeEditorState ExtractEditorState (NodeCanvas canvas, string stateName) 
		{
			NodeEditorState state = null;
			if (canvas.editorStates.Length > 0)
			{ // Search for the editorState
				state = canvas.editorStates.First ((NodeEditorState s) => s != null && s.name == stateName);
			}
			if (state == null)
			{ // Create editorState
				state = ScriptableObject.CreateInstance<NodeEditorState> ();
				state.canvas = canvas;
				// Insert into list
				int index = canvas.editorStates.Length;
				System.Array.Resize (ref canvas.editorStates, index+1);
				canvas.editorStates[index] = state;
			#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty (canvas);
			#endif
			}
			state.name = stateName;
			return state;
		}

		/// <summary>
		/// Overwrites canvas with the contents of canvasData, so that all references to canvas will be remained, but both canvases are still seperate.
		/// Only works in the editor!
		/// </summary>
		public static void OverwriteCanvas (ref NodeCanvas targetCanvas, NodeCanvas canvasData)
		{
		#if UNITY_EDITOR
			if (canvasData == null)
				throw new System.ArgumentNullException ("Cannot overwrite canvas as data is null!");
			if (targetCanvas == null)
				targetCanvas = NodeCanvas.CreateCanvas (canvasData.GetType ());
			UnityEditor.EditorUtility.CopySerialized (canvasData, targetCanvas);
			targetCanvas.name = canvasData.name;
		#else
			throw new System.NotSupportedException ("Cannot overwrite canvas in player!");
		#endif
		}

		#endregion
	}
}