using UnityEngine;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using NodeEditorFramework;
using NodeEditorFramework.Utilities;


#if UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

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

		#region Utility

		/// <summary>
		/// Gets all existing stored saves in the current scenes and returns their names
		/// </summary>
		public static string[] GetSceneSaves()
		{
			return Object.FindObjectsOfType<NodeCanvasSceneSave>()
				.Where((NodeCanvasSceneSave save) => save.savedNodeCanvas != null && save.saveName.ToLower() != "lastsession")
#if UNITY_5_3_OR_NEWER
				.Select((NodeCanvasSceneSave save) => SceneManager.sceneCount > 1 ? (save.gameObject.scene.name + "::" + save.saveName) : save.saveName)
#else
				.Select((NodeCanvasSceneSave save) => save.saveName)
#endif
				.ToArray();
		}

		/// <summary>
		/// Returns whether a sceneSave with the specified name exists in the current scene
		/// </summary>
		public static bool HasSceneSave (string saveName)
		{
			NodeCanvasSceneSave save = FindSceneSave(saveName);
			return save != null && save.savedNodeCanvas != null;
		}

		/// <summary>
		/// Deletes the nodeCanvas stored in the current scene under the specified name
		/// </summary>
		public static void DeleteSceneNodeCanvas (string saveName)
		{
			if (string.IsNullOrEmpty (saveName))
				return;
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

		/// <summary>
		/// Returns the scene save with the specified name in the current scene
		/// </summary>
		private static NodeCanvasSceneSave FindSceneSave(string saveName, bool forceCreation = false)
		{
			bool sceneSpecified = false;
			string scene = null;
#if UNITY_5_3_OR_NEWER
			sceneSpecified = saveName.Contains("::");
			if (sceneSpecified)
			{
				string[] sp = saveName.Split(new string[] { "::" }, System.StringSplitOptions.None);
				scene = sp[0];
				saveName = sp[1];
			}
#endif

			NodeCanvasSceneSave sceneSave = Object.FindObjectsOfType<NodeCanvasSceneSave>()
#if UNITY_5_3_OR_NEWER // Filter by scene
				.Where((NodeCanvasSceneSave save) => !sceneSpecified || save.savedNodeCanvas == null || save.gameObject.scene.name == scene)
#endif
				.FirstOrDefault((NodeCanvasSceneSave save) =>
				   save.saveName.ToLower() == saveName.ToLower() ||
				   (save.savedNodeCanvas != null && save.savedNodeCanvas.name.ToLower() == saveName.ToLower()));
			if (sceneSave == null && forceCreation)
				sceneSave = CreateSceneSave(saveName, scene);
			return sceneSave;
		}

		/// <summary>
		/// Creates a scene save with the specified name in the current scene
		/// </summary>
		private static NodeCanvasSceneSave CreateSceneSave(string saveName, string sceneName = null)
		{
			GameObject holder = GetSceneSaveHolder(sceneName);
			NodeCanvasSceneSave sceneSave = holder.AddComponent<NodeCanvasSceneSave>();
			sceneSave.saveName = saveName;
			return sceneSave;
		}

		/// <summary>
		/// Gets the current sceneSaveHolder or creates it, optionally in the specified scene
		/// </summary>
		private static GameObject GetSceneSaveHolder(string sceneName = null)
		{
#if UNITY_5_3_OR_NEWER // Reset sceneSaveHolder if it's in the wrong scene
			bool sceneSpecified = !string.IsNullOrEmpty(sceneName);
			if (sceneSpecified && sceneSaveHolder != null && sceneSaveHolder.scene.name != sceneName)
				sceneSaveHolder = null;
#endif

			if (sceneSaveHolder == null)
			{ // Fetch scene save holder from scene
				sceneSaveHolder = Object.FindObjectsOfType<NodeCanvasSceneSave>()
					.Select(s => s.gameObject).Distinct()
					.OrderBy(g => g.name == sceneSaveHolderName ? 1 : 2)
#if UNITY_5_3_OR_NEWER
					.Where(g => !sceneSpecified || g.scene.name == sceneName)
#endif
					.FirstOrDefault();
			}

			if (sceneSaveHolder == null)
			{ // Create new scene save holder in scene
#if UNITY_5_3_OR_NEWER // Make sure it is created in the desired scene
				if (sceneSpecified && SceneManager.sceneCount > 1)
					SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
#endif
				sceneSaveHolder = new GameObject(sceneSaveHolderName);
			}

			if (sceneSaveHolder != null)
				sceneSaveHolder.hideFlags = HideFlags.None;//HideFlags.HideInHierarchy | HideFlags.HideInInspector;
			return sceneSaveHolder;
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

			nodeCanvas.Validate();

			if (!nodeCanvas.livesInScene
		#if UNITY_EDITOR // Make sure the canvas has no reference to an asset
			|| UnityEditor.AssetDatabase.Contains (nodeCanvas)
		#endif
			) {
				Debug.LogWarning ("Creating scene save '" + nodeCanvas.name + "' for canvas saved as an asset! Forcing creation of working copy!");
				nodeCanvas = CreateWorkingCopy(nodeCanvas);
			}

			// Update the source of the canvas
			nodeCanvas.UpdateSource ("SCENE/" + saveName);

			// Preprocess the canvas
			NodeCanvas processedCanvas = nodeCanvas;
			processedCanvas.OnBeforeSavingCanvas ();
			if (createWorkingCopy)
				processedCanvas = CreateWorkingCopy(processedCanvas);

			// Get the saveHolder and store the canvas
			NodeCanvas savedCanvas = processedCanvas;
			NodeCanvasSceneSave sceneSave = FindSceneSave(saveName, true);

#if UNITY_EDITOR
			if (sceneSave.savedNodeCanvas != null && safeOverwrite && sceneSave.savedNodeCanvas.GetType() == savedCanvas.GetType()) // OVERWRITE
				OverwriteCanvas(ref sceneSave.savedNodeCanvas, savedCanvas);

			if (!Application.isPlaying)
			{ // Set Dirty
				UnityEditor.EditorUtility.SetDirty (sceneSave.gameObject);
			#if UNITY_5_3_OR_NEWER
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty (sceneSave.gameObject.scene);
			#else
				UnityEditor.EditorApplication.MarkSceneDirty ();
			#endif
			}
#endif
			sceneSave.savedNodeCanvas = savedCanvas;

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
			savedCanvas.Validate();
			if (createWorkingCopy)
				savedCanvas = CreateWorkingCopy(savedCanvas);

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
	#else
			if (string.IsNullOrEmpty (path)) throw new System.ArgumentNullException ("Cannot save NodeCanvas: No path specified!");
			if (nodeCanvas == null) throw new System.ArgumentNullException ("Cannot save NodeCanvas: The specified NodeCanvas that should be saved to path '" + path + "' is null!");
			if (nodeCanvas.GetType () == typeof(NodeCanvas)) throw new System.ArgumentException ("Cannot save NodeCanvas: The NodeCanvas has no explicit type! Please convert it to a valid sub-type of NodeCanvas!");
			
			nodeCanvas.Validate();

			if (nodeCanvas.livesInScene)
			{
				Debug.LogWarning ("Attempting to save scene canvas '" + nodeCanvas.name + "' to an asset, references to scene object may be broken!" + (!createWorkingCopy? " Forcing creation of working copy!" : ""));
				createWorkingCopy = true;
			}
			if (UnityEditor.AssetDatabase.Contains (nodeCanvas) && UnityEditor.AssetDatabase.GetAssetPath (nodeCanvas) != path) 
			{ 
				Debug.LogWarning ("Trying to create a duplicate save file for '" + nodeCanvas.name + "'! Forcing creation of working copy!");
				nodeCanvas = CreateWorkingCopy(nodeCanvas);
			}

			// Prepare and update source path of the canvas
			path = ResourceManager.PreparePath (path);
			nodeCanvas.UpdateSource (path);

			// Preprocess the canvas
			NodeCanvas processedCanvas = nodeCanvas;
			processedCanvas.OnBeforeSavingCanvas ();
			if (createWorkingCopy)
				processedCanvas = CreateWorkingCopy(processedCanvas);

			// Differenciate canvasSave as the canvas asset and nodeCanvas as the source incase an existing save has been overwritten
			NodeCanvas canvasSave = processedCanvas;
			NodeCanvas prevSave;
			if (safeOverwrite && (prevSave = ResourceManager.LoadResource<NodeCanvas> (path)) != null && prevSave.GetType () == canvasSave.GetType ())
			{ // OVERWRITE: Delete contents of old save
				for (int nodeCnt = 0; nodeCnt < prevSave.nodes.Count; nodeCnt++) 
				{
					Node node = prevSave.nodes[nodeCnt];
					// Make sure all node ports are included in the representative connectionPorts list
					ConnectionPortManager.UpdatePortLists(node);
					for (int k = 0; k < node.connectionPorts.Count; k++)
						Object.DestroyImmediate(node.connectionPorts[k], true);
					Object.DestroyImmediate (node, true);
				}
				for (int i = 0; i < prevSave.editorStates.Length; i++)
				{
					if (prevSave.editorStates[i] != null)
						Object.DestroyImmediate (prevSave.editorStates[i], true);
				}
				// Overwrite main canvas
				OverwriteCanvas (ref prevSave, processedCanvas);
				canvasSave = prevSave;
			}
			else
			{ // Write main canvas
				UnityEditor.AssetDatabase.CreateAsset (processedCanvas, path);
			}

			// Write editorStates
			AddSubAssets (processedCanvas.editorStates, canvasSave);
			// Write nodes + contents
			foreach (Node node in processedCanvas.nodes)
			{ // Write node and additional scriptable objects
				AddSubAsset (node, canvasSave);
				AddSubAssets (node.GetScriptableObjects (), node);
				// Make sure all node ports are included in the representative connectionPorts list
				ConnectionPortManager.UpdatePortLists(node);
				foreach (ConnectionPort port in node.connectionPorts)
					AddSubAsset (port, node);
			}

			UnityEditor.AssetDatabase.SaveAssets ();
			UnityEditor.AssetDatabase.Refresh ();

			NodeEditorCallbacks.IssueOnSaveCanvas (canvasSave);
			
	#endif
		}

		/// <summary>
		/// Loads the NodeCanvas from the asset file at path and optionally creates a working copy of it before returning
		/// </summary>
		public static NodeCanvas LoadNodeCanvas (string path, bool createWorkingCopy)
		{
	#if !UNITY_EDITOR
			throw new System.NotImplementedException ();
	#else
			if (string.IsNullOrEmpty (path))
				throw new System.ArgumentNullException ("Cannot load Canvas: No path specified!");
			path = ResourceManager.PreparePath (path);

			// Load only the NodeCanvas from the save file
			NodeCanvas nodeCanvas = ResourceManager.LoadResource<NodeCanvas> (path);
			if (nodeCanvas == null) 
				throw new UnityException ("Cannot load NodeCanvas: The file at the specified path '" + path + "' is no valid save file as it does not contain a NodeCanvas!");
			
			if (!Application.isPlaying && (nodeCanvas.editorStates == null || nodeCanvas.editorStates.Length == 0))
			{ // Try to load any contained editorStates, as the canvas did not reference any
				nodeCanvas.editorStates = ResourceManager.LoadResources<NodeEditorState> (path);
			}

			// Set the path as the new source of the canvas
			nodeCanvas.UpdateSource (path);

			// Postprocess the loaded canvas
			nodeCanvas.Validate();
			if (createWorkingCopy)
				nodeCanvas = CreateWorkingCopy(nodeCanvas);

			NodeEditorCallbacks.IssueOnLoadCanvas (nodeCanvas);
			return nodeCanvas;
	#endif
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

		#endregion

		#endregion

		#region Working Copy

		/// <summary>
		/// Creates a working copy of the specified nodeCanvas, and optionally also of it's associated editorStates.
		/// This breaks the link of this object to any stored assets and references. That means, that all changes to this object will have to be explicitly saved.
		/// </summary>
		public static NodeCanvas CreateWorkingCopy (NodeCanvas nodeCanvas, bool editorStates = true) 
		{
			if (nodeCanvas == null)
				return null;

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

				// Clone Node and additional scriptableObjects
				Node clonedNode = AddClonedSO (allSOs, clonedSOs, node);
				AddClonedSOs (allSOs, clonedSOs, clonedNode.GetScriptableObjects ());

				// Update representative port list connectionPorts with all ports and clone them
				ConnectionPortManager.UpdatePortLists(clonedNode);
				AddClonedSOs(allSOs, clonedSOs, clonedNode.connectionPorts);
			}

			// Replace every reference to any of the initial SOs of the first list with the respective clones of the second list
			nodeCanvas.CopyScriptableObjects (copySOs);

			for (int nodeCnt = 0; nodeCnt < nodeCanvas.nodes.Count; nodeCnt++) 
			{ // Replace SOs in all Nodes
				Node node = nodeCanvas.nodes[nodeCnt];

				// Replace node and additional ScriptableObjects with their copies
				Node clonedNode = nodeCanvas.nodes[nodeCnt] = ReplaceSO (allSOs, clonedSOs, node);
				clonedNode.CopyScriptableObjects (copySOs);

				// Replace ConnectionPorts
				foreach (ConnectionPortDeclaration portDecl in ConnectionPortManager.GetPortDeclarationEnumerator(clonedNode, true))
				{ // Iterate over each static port declaration and replace it with it's connections
					ConnectionPort port = (ConnectionPort)portDecl.portField.GetValue(clonedNode);
					port = ReplaceSO(allSOs, clonedSOs, port);
					for (int c = 0; c < port.connections.Count; c++)
						port.connections[c] = ReplaceSO(allSOs, clonedSOs, port.connections[c]);
					port.body = clonedNode;
					portDecl.portField.SetValue(clonedNode, port);
				}
				for (int i = 0; i < clonedNode.dynamicConnectionPorts.Count; i++)
				{ // Iterate over all dynamic connection ports
					ConnectionPort port = clonedNode.dynamicConnectionPorts[i];
					port = ReplaceSO(allSOs, clonedSOs, port);
					for (int c = 0; c < port.connections.Count; c++)
						port.connections[c] = ReplaceSO(allSOs, clonedSOs, port.connections[c]);
					port.body = clonedNode;
					clonedNode.dynamicConnectionPorts[i] = port;
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
		private static void AddClonedSOs<T> (List<ScriptableObject> scriptableObjects, List<ScriptableObject> clonedScriptableObjects, IEnumerable<T> initialSOs) where T : ScriptableObject
		{
			// Filter out all new SOs to add
			IEnumerable<T> newSOs = initialSOs.Where ((T so) => !scriptableObjects.Contains (so));
			foreach (T SO in newSOs)
			{ // Clone and record them
				scriptableObjects.Add(SO);
				clonedScriptableObjects.Add(Clone(SO));
			}
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
				return clonedScriptableObjects[existing] as T;
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
				Debug.LogError("GetWorkingCopy: ScriptableObject " + initialSO.name + " was not copied before! It will be null!");
			else
			{
				ScriptableObject SO = clonedScriptableObjects[soInd];
				if (!SO)
					Debug.LogError("GetWorkingCopy: ScriptableObject " + initialSO.name + " has been recorded with a null copy!");
				else if (SO is T)
					return clonedScriptableObjects[soInd] as T;
				else
					Debug.LogError("GetWorkingCopy: ScriptableObject " + initialSO.name + " is not of the same type as copy " + clonedScriptableObjects[soInd].name + "!");
			}
			return null;
		}

		#endregion

		#endregion

		#region Utility

		/// <summary>
		/// Returns the editorState with the specified name in canvas.
		/// If not found but others and forceFind is false, a different one is chosen randomly, else a new one is created.
		/// </summary>
		public static NodeEditorState ExtractEditorState (NodeCanvas canvas, string stateName, bool forceFind = false) 
		{
			NodeEditorState state = null;
			if (canvas.editorStates.Length > 0) // Search for the editorState
				state = canvas.editorStates.FirstOrDefault ((NodeEditorState s) => s.name == stateName);
			if (state == null && !forceFind) // Take any other if not found
				state = canvas.editorStates.FirstOrDefault();
			if (state == null)
			{ // Create editorState
				state = ScriptableObject.CreateInstance<NodeEditorState>();
				state.canvas = canvas;
				// Insert into list
				int index = canvas.editorStates.Length;
				System.Array.Resize(ref canvas.editorStates, index + 1);
				canvas.editorStates[index] = state;
			#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(canvas);
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