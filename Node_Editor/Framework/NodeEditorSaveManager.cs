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
	public static class NodeEditorSaveManager 
	{
		#region Scene Saving

		private static GameObject sceneSaveHolder;

		/// <summary>
		/// Fetches the saveHolder of the current scene if not already found or creates it and stores it into sceneSaveHolder
		/// </summary>
		private static void FetchSceneSaveHolder () 
		{
			if (sceneSaveHolder == null)
			{
				// TODO: Might need to check here if the object is in the active scene / the system works with multiple scenes
				//#if UNITY_5_3 | UNITY_5_3_OR_NEWER
				//if (UnityEngine.SceneManagement.SceneManager.GetActiveScene ())
				//#endif
				sceneSaveHolder = GameObject.Find ("NodeEditor_SceneSaveHolder");
				if (sceneSaveHolder == null)
					sceneSaveHolder = new GameObject ("NodeEditor_SceneSaveHolder");
			    sceneSaveHolder.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
			}
		}

		/// <summary>
		/// Gets all existing stored saves in the current scene and returns their names
		/// </summary>
		public static string[] GetSceneSaves ()
		{ // Get the saveHolder, find the existing stored saves and return their names
			FetchSceneSaveHolder ();
			return sceneSaveHolder.GetComponents<NodeCanvasSceneSave> ().Select (((NodeCanvasSceneSave save) => save.savedNodeCanvas.name)).ToArray ();
		}

		/// <summary>
		/// Finds a scene save in the current scene with specified name or null if it does not exist
		/// </summary>
		private static NodeCanvasSceneSave FindSceneSave (string saveName)
		{
			FetchSceneSaveHolder ();
			return sceneSaveHolder.GetComponents<NodeCanvasSceneSave> ().ToList ().Find ((NodeCanvasSceneSave save) => save.savedNodeCanvas.name == saveName);
		}

		/// <summary>
		/// Saves the nodeCanvas in the current scene under the specified name along with the specified editorStates or, if specified, their working copies
		/// If also stored as an asset, it will loose the reference to the asset first
		/// </summary>
		public static void SaveSceneNodeCanvas (string saveName, ref NodeCanvas nodeCanvas, bool createWorkingCopy)
		{
            if (string.IsNullOrEmpty (saveName))
			{
				Debug.LogError ("Cannot save Canvas to scene: No save name specified!");
				return;
			}
            
        #if UNITY_EDITOR // Make sure the canvas has no reference to an asset
            if (!createWorkingCopy && UnityEditor.AssetDatabase.Contains (nodeCanvas)) 
			{
				Debug.LogWarning ("Forced to create working copy of '" + saveName + "' when saving to scene because it already exists as an asset!");
                nodeCanvas = CreateWorkingCopy (nodeCanvas, true);
			}
		#endif
			nodeCanvas.livesInScene = true;
			nodeCanvas.name = saveName;

            // Get the saveHolder and the find the existing stored save or create a new one
            NodeCanvasSceneSave sceneSave = FindSceneSave (saveName);
			if (sceneSave == null)
				sceneSave = sceneSaveHolder.AddComponent<NodeCanvasSceneSave> ();
			
			// Store the canvas and editor states or optionally their working copies
			sceneSave.savedNodeCanvas = nodeCanvas;
			if (createWorkingCopy)
			{
				sceneSave.savedNodeCanvas = CreateWorkingCopy (sceneSave.savedNodeCanvas, true);
				Compress (ref sceneSave.savedNodeCanvas);
			}

        #if UNITY_EDITOR
            nodeCanvas.BeforeSavingCanvas();
            UnityEditor.EditorUtility.SetDirty (sceneSaveHolder);
		#endif
		}

		/// <summary>
		/// Loads the nodeCanvas and it's editorState stored in the current scene under the specified name and, if specified, creates working copies before returning
		/// </summary>
		public static NodeCanvas LoadSceneNodeCanvas (string saveName, bool createWorkingCopy)
		{
			if (string.IsNullOrEmpty (saveName))
			{
				Debug.LogError ("Cannot load Canvas from scene: No save name specified!");
				return null;
			}

			NodeCanvasSceneSave sceneSave = FindSceneSave (saveName);
			if (sceneSave == null) // No such save file
				return null;

			// Extract the saved canvas and editorStates
			NodeCanvas savedCanvas = sceneSave.savedNodeCanvas;
			savedCanvas.livesInScene = true;

			// Postprocess the loaded canvas
			if (createWorkingCopy) // Create working copies if specified
				savedCanvas = CreateWorkingCopy (savedCanvas, true);
			Uncompress (ref savedCanvas);

		#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty (sceneSaveHolder);
		#endif

			return savedCanvas;
		}

		#endregion

		#region Asset Saving

		/// <summary>
		/// Saves the the given NodeCanvas along with the given NodeEditorStates if specified as a new asset, optionally as working copies
		/// </summary>
		public static void SaveNodeCanvas (string path, NodeCanvas nodeCanvas, bool createWorkingCopy) 
		{
        #if !UNITY_EDITOR
			throw new System.NotImplementedException ();
        #endif
            
            if (string.IsNullOrEmpty (path)) throw new UnityException ("Cannot save NodeCanvas: No spath specified to save the NodeCanvas " + (nodeCanvas != null? nodeCanvas.name : "") + " to!");
			if (nodeCanvas == null) throw new UnityException ("Cannot save NodeCanvas: The specified NodeCanvas that should be saved to path " + path + " is null!");
			if (nodeCanvas.livesInScene)
				Debug.LogWarning ("Attempting to save scene canvas " + nodeCanvas.name + " to an asset, scene object references will be broken!" + (!createWorkingCopy? " No workingCopy is going to be created, so your scene save is broken, too!" : ""));
        #if UNITY_EDITOR
            if (!createWorkingCopy && UnityEditor.AssetDatabase.Contains (nodeCanvas) && UnityEditor.AssetDatabase.GetAssetPath (nodeCanvas) != path) { Debug.LogError ("Trying to create a duplicate save file for '" + nodeCanvas.name + "'! Forcing to create a working copy!"); createWorkingCopy = true; }
        #endif

            nodeCanvas.livesInScene = false;
			nodeCanvas.Validate ();

        #if UNITY_EDITOR
            // Preprocess the canvas
            if (createWorkingCopy)
			{
				nodeCanvas = CreateWorkingCopy (nodeCanvas, true);
				Compress (ref nodeCanvas);
			}

			// Write canvas and editorStates
			UnityEditor.AssetDatabase.CreateAsset (nodeCanvas, path);
			AddSubAssets (nodeCanvas.editorStates, nodeCanvas);

			// Write nodes + contents
			foreach (Node node in nodeCanvas.nodes)
			{ // Write node and additional scriptable objects
				AddSubAsset (node, nodeCanvas);
				AddSubAssets (node.GetScriptableObjects (), node);
				foreach (NodeKnob knob in node.nodeKnobs)
				{ // Write knobs and their additional scriptable objects
					AddSubAsset (knob, node);
					AddSubAssets (knob.GetScriptableObjects (), knob);
				}
                //foreach (NodeInput knob in node.Inputs)
                //{ // Write knobs and their additional scriptable objects
                //    AddSubAsset(knob, node);
                //    AddSubAssets(knob.GetScriptableObjects(), knob);
                //}
                //foreach (NodeOutput knob in node.Outputs)
                //{ // Write knobs and their additional scriptable objects
                //    AddSubAsset(knob, node);
                //    AddSubAssets(knob.GetScriptableObjects(), knob);
                //}
            }

            nodeCanvas.BeforeSavingCanvas();

            UnityEditor.AssetDatabase.SaveAssets ();
			UnityEditor.AssetDatabase.Refresh ();
		#else
			// TODO: Node Editor: Need to implement ingame-saving (Resources, AsssetBundles, ... won't work)
		#endif

			NodeEditorCallbacks.IssueOnSaveCanvas (nodeCanvas);
		}

		/// <summary>
		/// Loads the NodeCanvas from the asset file at path and optionally creates a working copy of it before returning
		/// </summary>
		public static NodeCanvas LoadNodeCanvas (string path, bool createWorkingCopy) 
		{
			if (!File.Exists (path)) throw new UnityException ("Cannot Load NodeCanvas: File '" + path + "' deos not exist!");

			// Load only the NodeCanvas from the save file
			NodeCanvas nodeCanvas = ResourceManager.LoadResource<NodeCanvas> (path);
			if (nodeCanvas == null) throw new UnityException ("Cannot Load NodeCanvas: The file at the specified path '" + path + "' is no valid save file as it does not contain a NodeCanvas!");

		#if UNITY_EDITOR
			if (!Application.isPlaying && (nodeCanvas.editorStates == null || nodeCanvas.editorStates.Length == 0))
			{ // Try to load any contained editorStates, possibly old format that did not references the states in the canvas
				nodeCanvas.editorStates = ResourceManager.LoadResources<NodeEditorState> (path);
			}
		#endif

			// Postprocess the loaded canvas
			if (createWorkingCopy)
				nodeCanvas = CreateWorkingCopy (nodeCanvas, true);
			else
				nodeCanvas.Validate ();
			Uncompress (ref nodeCanvas);

		#if UNITY_EDITOR
			UnityEditor.AssetDatabase.Refresh ();
		#endif
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

		#endregion

		#endregion

		#region Compression

		/// <summary>
		/// Compresses the nodeCanvas, meaning it will remove duplicate references that are only for convenience
		/// </summary>
		public static void Compress (ref NodeCanvas nodeCanvas)
		{
			//for (int nodeCnt = 0; nodeCnt < nodeCanvas.nodes.Count; nodeCnt++) 
			//{
			//	Node node = nodeCanvas.nodes[nodeCnt];
			//	node.Inputs = new List<NodeInput> ();
			//	node.Outputs = new List<NodeOutput> ();
			//}
		}

		/// <summary>
		/// Uncompresses the nodeCanvas, meaning it will restore duplicate references for convenience
		/// </summary>
		public static void Uncompress (ref NodeCanvas nodeCanvas)
		{
            //For Backward Compatibility of Old Canvas
            for (int nodeCnt = 0; nodeCnt < nodeCanvas.nodes.Count; nodeCnt++)
            {                
                Node node = nodeCanvas.nodes[nodeCnt];
                if((node.Inputs == null || node.Inputs.Count == 0) || (node.Outputs == null || node.Outputs.Count == 0))
                {
                    node.Inputs = new List<NodeInput>();
                    node.Outputs = new List<NodeOutput>();
                    for(int knobCnt = 0; knobCnt < node.nodeKnobs.Count; knobCnt++)
                    {
                        NodeKnob knob = node.nodeKnobs[knobCnt];
                        if(knob is NodeInput)
                            node.Inputs.Add(knob as NodeInput);
                        else if(knob is NodeOutput)
                            node.Outputs.Add(knob as NodeOutput);
                    }
                }
            }
        }

		#endregion

		#region Working Copy

		/// <summary>
		/// Creates a working copy of the specified nodeCanvas, and optionally also of it's associated editorStates.
		/// This breaks the link of this object to any stored assets and references. That means, that all changes to this object will have to be explicitly saved.
		/// </summary>
		public static NodeCanvas CreateWorkingCopy (NodeCanvas nodeCanvas, bool editorStates) 
		{
			nodeCanvas.Validate ();
			nodeCanvas = Clone (nodeCanvas);

			// Take each SO, make a clone of it and store both versions in the respective list
			// This will only iterate over the 'source instances'
			List<ScriptableObject> allSOs = new List<ScriptableObject> ();
			List<ScriptableObject> clonedSOs = new List<ScriptableObject> ();
			for (int nodeCnt = 0; nodeCnt < nodeCanvas.nodes.Count; nodeCnt++) 
			{
				Node node = nodeCanvas.nodes[nodeCnt];
				node.CheckNodeKnobMigration ();

				// Clone Node and additional scriptableObjects
				Node clonedNode = AddClonedSO (allSOs, clonedSOs, node);
				AddClonedSOs (allSOs, clonedSOs, clonedNode.GetScriptableObjects ());

				foreach (NodeKnob knob in clonedNode.nodeKnobs)
				{ // Clone NodeKnobs and additional scriptableObjects
					AddClonedSO (allSOs, clonedSOs, knob);
					AddClonedSOs (allSOs, clonedSOs, knob.GetScriptableObjects ());
				}
                //foreach (NodeInput knob in clonedNode.Inputs)
                //{ // Clone NodeKnobs and additional scriptableObjects
                //    AddClonedSO(allSOs, clonedSOs, knob);
                //    AddClonedSOs(allSOs, clonedSOs, knob.GetScriptableObjects());
                //}
                //foreach (NodeOutput knob in clonedNode.Outputs)
                //{ // Clone NodeKnobs and additional scriptableObjects
                //    AddClonedSO(allSOs, clonedSOs, knob);
                //    AddClonedSOs(allSOs, clonedSOs, knob.GetScriptableObjects());
                //}
            }

			// Replace every reference to any of the initial SOs of the first list with the respective clones of the second list
			for (int nodeCnt = 0; nodeCnt < nodeCanvas.nodes.Count; nodeCnt++) 
			{ // Clone Nodes, structural content and additional scriptableObjects
				Node node = nodeCanvas.nodes[nodeCnt];
				// Replace node and additional ScriptableObjects
				Node clonedNode = nodeCanvas.nodes[nodeCnt] = ReplaceSO (allSOs, clonedSOs, node);
				clonedNode.CopyScriptableObjects ((ScriptableObject so) => ReplaceSO (allSOs, clonedSOs, so));

				// We're going to restore these from NodeKnobs if desired (!compressed)
				//clonedNode.Inputs = new List<NodeInput> ();
				//clonedNode.Outputs = new List<NodeOutput> ();
				for (int knobCnt = 0; knobCnt < clonedNode.nodeKnobs.Count; knobCnt++) 
				{ // Clone generic NodeKnobs
					NodeKnob knob = clonedNode.nodeKnobs[knobCnt] = ReplaceSO (allSOs, clonedSOs, clonedNode.nodeKnobs[knobCnt]);
					knob.body = clonedNode;
					// Replace additional scriptableObjects in the NodeKnob
					knob.CopyScriptableObjects ((ScriptableObject so) => ReplaceSO (allSOs, clonedSOs, so));
				}
                for (int knobCnt = 0; knobCnt < clonedNode.Inputs.Count; knobCnt++)
                { // Clone generic NodeKnobs
                    NodeInput knob = clonedNode.Inputs[knobCnt] = ReplaceSO(allSOs, clonedSOs, clonedNode.Inputs[knobCnt]);
                    knob.body = clonedNode;
                    // Replace additional scriptableObjects in the NodeKnob
                    //knob.CopyScriptableObjects((ScriptableObject so) => ReplaceSO(allSOs, clonedSOs, so));
                }
                for (int knobCnt = 0; knobCnt < clonedNode.Outputs.Count; knobCnt++)
                { // Clone generic NodeKnobs
                    NodeOutput knob = clonedNode.Outputs[knobCnt] = ReplaceSO(allSOs, clonedSOs, clonedNode.Outputs[knobCnt]);
                    knob.body = clonedNode;
                    // Replace additional scriptableObjects in the NodeKnob
                    //knob.CopyScriptableObjects((ScriptableObject so) => ReplaceSO(allSOs, clonedSOs, so));
                }
            }

			if (editorStates)
			{
				nodeCanvas.editorStates = CreateWorkingCopy (nodeCanvas.editorStates, nodeCanvas);
				foreach (NodeEditorState state in nodeCanvas.editorStates)
					state.selectedNode = ReplaceSO (allSOs, clonedSOs, state.selectedNode);
			}
			else
			{
				foreach (NodeEditorState state in nodeCanvas.editorStates)
					state.selectedNode = null;
			}

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
			editorStates = (NodeEditorState[])editorStates.Clone ();
			for (int stateCnt = 0; stateCnt < editorStates.Length; stateCnt++) 
			{
				if (editorStates[stateCnt] == null)
					continue;
				NodeEditorState state = editorStates[stateCnt] = Clone (editorStates[stateCnt]);
				if (state == null) 
				{
					Debug.LogError ("Failed to create a working copy for an NodeEditorState during the loading process of " + associatedNodeCanvas.name + "!");
					continue;
				}
				state.canvas = associatedNodeCanvas;
			}
			associatedNodeCanvas.editorStates = editorStates;
			return editorStates;
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
		/// Clones SO and writes both the initial and cloned versions into the respective list
		/// </summary>
		private static void AddClonedSOs (List<ScriptableObject> scriptableObjects, List<ScriptableObject> clonedScriptableObjects, ScriptableObject[] initialSOs)
		{
			scriptableObjects.AddRange (initialSOs);
			clonedScriptableObjects.AddRange (initialSOs.Select ((ScriptableObject so) => Clone (so)));
		}

		/// <summary>
		/// Clones SO and writes both the initial and cloned versions into the respective list
		/// </summary>
		private static T AddClonedSO<T> (List<ScriptableObject> scriptableObjects, List<ScriptableObject> clonedScriptableObjects, T initialSO) where T : ScriptableObject 
		{
			if (initialSO == null)
				return null;
			scriptableObjects.Add (initialSO);
			T clonedSO = Clone (initialSO);
			clonedScriptableObjects.Add (clonedSO);
			return clonedSO;
		}

		/// <summary>
		/// First two parameters contains SOs and their respective clones. 
		/// Returns the clone of initialSO found in the cloned list at the respective position of initialSO in the initial list
		/// </summary>
		private static T ReplaceSO<T> (List<ScriptableObject> scriptableObjects, List<ScriptableObject> clonedScriptableObjects, T initialSO) where T : ScriptableObject 
		{
			if (initialSO == null)
				return null;
			int soInd = scriptableObjects.IndexOf (initialSO);
			if (soInd == -1)
				Debug.LogError ("GetWorkingCopy: ScriptableObject " + initialSO.name + " was not copied before! It will be null!");
			return soInd == -1? null : (T)clonedScriptableObjects[soInd];
		}

		#endregion

		#endregion

		#region Utility

		/// <summary>
		/// Extracts the state with the specified name out of the canvas, takes a random different one and renames it or creates a new one with that name if not found
		/// </summary>
		public static NodeEditorState ExtractEditorState (NodeCanvas canvas, string stateName) 
		{
			NodeEditorState state = null;
			if (canvas.editorStates.Length > 0)
			{
				state = canvas.editorStates.First ((NodeEditorState s) => s.name == stateName);
				if (state == null)
					state = canvas.editorStates[0];
			}
			if (state == null)
			{
				state = ScriptableObject.CreateInstance<NodeEditorState> ();
				state.canvas = canvas;
				canvas.editorStates = new NodeEditorState[] { state };
			#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty (canvas);
			#endif
			}
			state.name = stateName;
			return state;
		}

		#endregion
	}
}