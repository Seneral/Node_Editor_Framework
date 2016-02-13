using UnityEngine;
using System.Linq;
using System.Collections.Generic;

using NodeEditorFramework;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework 
{
	/// <summary>
	/// Manager handling all save and load operations on NodeCanvases and NodeEditorStates of the Node Editor
	/// </summary>
	public static class NodeEditorSaveManager 
	{
		#region Save

		/// <summary>
		/// Saves a working copy of the given NodeCanvas as a new asset along with working copies of the given NodeEditorStates
		/// </summary>
		public static void SaveNodeCanvas (string path, NodeCanvas nodeCanvas, params NodeEditorState[] editorStates) 
		{
			SaveNodeCanvas (path, true, nodeCanvas, editorStates);
		}

		/// <summary>
		/// Saves the the given NodeCanvas or, if specified, a working copy of it, as a new asset along with working copies, if specified, of the given NodeEditorStates
		/// </summary>
		public static void SaveNodeCanvas (string path, bool createWorkingCopy, NodeCanvas nodeCanvas, params NodeEditorState[] editorStates) 
		{
			if (string.IsNullOrEmpty (path))
				throw new UnityException ("Cannot save NodeCanvas: No spath specified to save the NodeCanvas " + (nodeCanvas != null? nodeCanvas.name : "") + " to!");
			if (nodeCanvas == null)
				throw new UnityException ("Cannot save NodeCanvas: The specified NodeCanvas that should be saved to path " + path + " is null!");

			for (int stateCnt = 0; stateCnt < editorStates.Length; stateCnt++) 
			{
				if (editorStates[stateCnt] == null)
					Debug.LogError ("A NodeEditorState that should be saved to path " + path + " is null!");
			}

			path = path.Replace (Application.dataPath, "Assets");

	#if UNITY_EDITOR

			if (createWorkingCopy)
			{ // Copy canvas and editorStates
				CreateWorkingCopy (ref nodeCanvas, editorStates, true);
			}

			// Write canvas and editorStates
			UnityEditor.AssetDatabase.CreateAsset (nodeCanvas, path);
			for (int stateCnt = 0; stateCnt < editorStates.Length; stateCnt++) 
			{
				if (editorStates[stateCnt] != null)
					AddSubAsset (editorStates[stateCnt], nodeCanvas);
			}

			// Write nodes + contents
			for (int nodeCnt = 0; nodeCnt < nodeCanvas.nodes.Count; nodeCnt++) 
			{ // Every node and each ScriptableObject in it
				Node node = nodeCanvas.nodes [nodeCnt];
				AddSubAsset (node, nodeCanvas);
				for (int knobCnt = 0; knobCnt < node.nodeKnobs.Count; knobCnt++) 
				{
					AddSubAsset (node.nodeKnobs [knobCnt], node);
				}
				for (int transCnt = 0; transCnt < node.transitions.Count; transCnt++)
				{
					if (node.transitions[transCnt].startNode == node) 
					{
						AddSubAsset (node.transitions [transCnt], node);
//						Debug.Log ("Did save Transition " + node.transitions [transCnt].name + " because its Node " + node.name + " is the start node!");
					}
//					else
//						Debug.Log ("Did NOT save Transition " + node.transitions [transCnt].name + " because its Node " + node.name + " is NOT the start node (" + (node.transitions[transCnt].startNode != null? node.transitions[transCnt].startNode.name : "Null") + ")!");
				}
			}

			UnityEditor.AssetDatabase.SaveAssets ();
			UnityEditor.AssetDatabase.Refresh ();
	#else
			// TODO: Node Editor: Need to implement ingame-saving (Resources, AsssetBundles, ... won't work)
	#endif

			NodeEditorCallbacks.IssueOnSaveCanvas (nodeCanvas);
		}

	#if UNITY_EDITOR

		/// <summary>
		/// Adds a hidden sub asset to the main asset
		/// </summary>
		public static void AddSubAsset (ScriptableObject subAsset, ScriptableObject mainAsset) 
		{
			UnityEditor.AssetDatabase.AddObjectToAsset (subAsset, mainAsset);
			subAsset.hideFlags = HideFlags.HideInHierarchy;
		}

		/// <summary>
		/// Adds a hidden sub asset to the main asset
		/// </summary>
		public static void AddSubAsset (ScriptableObject subAsset, string path) 
		{
			UnityEditor.AssetDatabase.AddObjectToAsset (subAsset, path);
			subAsset.hideFlags = HideFlags.HideInHierarchy;
		}

	#endif

		#endregion

		#region Load

		/// <summary>
		/// Loads the NodeCanvas from the asset file at path and returns a working copy of it
		/// </summary>
		public static NodeCanvas LoadNodeCanvas (string path) 
		{
			return LoadNodeCanvas (path, true);
		}

		/// <summary>
		/// Loads the NodeCanvas from the asset file at path and optionally creates a working copy of it before returning
		/// </summary>
		public static NodeCanvas LoadNodeCanvas (string path, bool createWorkingCopy) 
		{
			if (string.IsNullOrEmpty (path))
				throw new UnityException ("Cannot Load NodeCanvas: No path specified to load the NodeCanvas from!");

			// Fetch all objects in the save file
			ScriptableObject[] objects = ResourceManager.LoadResources<ScriptableObject> (path);
			if (objects == null || objects.Length == 0) 
				throw new UnityException ("Cannot Load NodeCanvas: The specified path '" + path + "' does not point to a save file!");

			// Filter out the NodeCanvas out of these objects
			NodeCanvas nodeCanvas = objects.Single ((ScriptableObject obj) => (obj as NodeCanvas) != null) as NodeCanvas;
			if (nodeCanvas == null)
				throw new UnityException ("Cannot Load NodeCanvas: The file at the specified path '" + path + "' is no valid save file as it does not contain a NodeCanvas!");

	#if UNITY_EDITOR // Create a working copy of it
			if (createWorkingCopy)
			{
				CreateWorkingCopy (ref nodeCanvas, false);
				if (nodeCanvas == null)
					throw new UnityException ("Cannot Load NodeCanvas: Failed to create a working copy for the NodeCanvas at path '" + path + "' during the loading process!");
			}
			UnityEditor.AssetDatabase.Refresh ();
	#endif	
			NodeEditorCallbacks.IssueOnLoadCanvas (nodeCanvas);
			return nodeCanvas;
		}

		/// <summary>
		/// Loads the editorStates found in the nodeCanvas asset file at path and returns a working copy of it
		/// </summary>
		public static List<NodeEditorState> LoadEditorStates (string path) 
		{
			return LoadEditorStates (path, true);
		}
		/// <summary>
		/// Loads the editorStates found in the nodeCanvas asset file at path and optionally creates a working copy of it before returning
		/// </summary>
		public static List<NodeEditorState> LoadEditorStates (string path, bool createWorkingCopy) 
		{
			if (string.IsNullOrEmpty (path))
				throw new UnityException ("Cannot load NodeEditorStates: No path specified to load the EditorStates from!");
			
			// Fetch all objects in the save file
			ScriptableObject[] objects = ResourceManager.LoadResources<ScriptableObject> (path);
			if (objects == null || objects.Length == 0) 
				throw new UnityException ("Cannot load NodeEditorStates: The specified path '" + path + "' does not point to a save file!");

			// Obtain the editorStates in that asset file and create a working copy of them
			List<NodeEditorState> editorStates = objects.OfType<NodeEditorState> ().ToList ();
	#if UNITY_EDITOR
			if (createWorkingCopy) 
			{
				for (int cnt = 0; cnt < editorStates.Count; cnt++) 
				{
					NodeEditorState state = editorStates[cnt];
					CreateWorkingCopy (ref state);
					editorStates[cnt] = state;
					if (state == null) throw new UnityException ("Failed to create a working copy for an NodeEditorState at path '" + path + "' during the loading process!");
				}
			}
	#endif

			// Perform Event and Database refresh
			for (int cnt = 0; cnt < editorStates.Count; cnt++) 
				NodeEditorCallbacks.IssueOnLoadEditorState (editorStates[cnt]);
	#if UNITY_EDITOR
			UnityEditor.AssetDatabase.Refresh ();
	#endif
			return editorStates;
		}

		#endregion

		#region Working Copy Creation

		// <summary>
		/// Gets a working copy of the editor state. This will break the link to the asset and thus all changes made to the working copy have to be explicitly saved.
		/// NOTE: If possible, create the working copy with the associated editor state. Else, you have to manually assign the working copy canvas!
		/// </summary>
		public static void CreateWorkingCopy (ref NodeEditorState editorState) 
		{
			if (editorState == null)
				return;
			
			editorState = Clone (editorState);
			editorState.focusedNode = null;
			editorState.selectedNode = null;
			editorState.makeTransition = null;
			editorState.connectOutput = null;
		}

		/// <summary>
		/// Creates a working copy of the canvas. This will break the link to the canvas asset and thus all changes made to the working copy have to be explicitly saved.
		/// Check compressed if the copy is not intended for useage but for storage, this will leave the Inputs and Outputs list of Node empty
		/// </summary>
		public static void CreateWorkingCopy (ref NodeCanvas nodeCanvas, bool compressed) 
		{
			CreateWorkingCopy (ref nodeCanvas, null, compressed);
		}

		/// <summary>
		/// CreateWorkingCopy a working copy of the canvas and each editorState. This will break the link to the canvas asset and thus all changes made to the working copy have to be explicitly saved.
		/// Check compressed if the copy is not intended for useage but for storage, this will leave the Inputs and Outputs list of Node empty
		/// </summary>
		public static void CreateWorkingCopy (ref NodeCanvas nodeCanvas, NodeEditorState[] editorStates, bool compressed) 
		{
			nodeCanvas = Clone (nodeCanvas);

			// Take each SO, make a clone of it and store both versions in the respective list
			// This will only iterate over the 'source instances'
			List<ScriptableObject> allSOs = new List<ScriptableObject> ();
			List<ScriptableObject> clonedSOs = new List<ScriptableObject> ();
			for (int nodeCnt = 0; nodeCnt < nodeCanvas.nodes.Count; nodeCnt++) 
			{
				Node node = nodeCanvas.nodes[nodeCnt];
				node.CheckNodeKnobMigration ();
				Node clonedNode = (Node)AddClonedSO (allSOs, clonedSOs, node);
				for (int knobCnt = 0; knobCnt < clonedNode.nodeKnobs.Count; knobCnt++) 
				{ // Clone NodeKnobs
//					Debug.Log ("Cloned " + knobCnt + " " + (clonedNode.nodeKnobs[knobCnt] == null? "null" : clonedNode.nodeKnobs[knobCnt].name) + " on node " + node.name);
					AddClonedSO (allSOs, clonedSOs, clonedNode.nodeKnobs[knobCnt]);
					// Clone additional scriptableObjects
					ScriptableObject[] additionalKnobSOs = clonedNode.nodeKnobs[knobCnt].GetScriptableObjects ();
					foreach (ScriptableObject so in additionalKnobSOs)
						AddClonedSO (allSOs, clonedSOs, so);
				}
				for (int transCnt = 0; transCnt < clonedNode.transitions.Count; transCnt++)
				{ // Clone Transitions
					Transition trans = clonedNode.transitions[transCnt];
					if (trans.startNode == node)
					{
						AddClonedSO (allSOs, clonedSOs, trans);
//						Debug.Log ("Did copy Transition " + trans.name + " because its Node " + clonedNode.name + " is the start node!");
					}
					else 
					{
//						Debug.Log ("Did NOT copy Transition " + trans.name + " because its Node " + clonedNode.name + " is NOT the start node (" + trans.startNode.name + ")!");
						clonedNode.transitions.RemoveAt (transCnt);
						transCnt--;
					}
				}
				// Clone additional scriptableObjects
				ScriptableObject[] additionalNodeSOs = clonedNode.GetScriptableObjects ();
				foreach (ScriptableObject so in additionalNodeSOs)
					AddClonedSO (allSOs, clonedSOs, so);
			}

			// Replace every reference to any of the initial SOs of the first list with the respective clones of the second list

			nodeCanvas.currentNode = ReplaceSO (allSOs, clonedSOs, nodeCanvas.currentNode);
			nodeCanvas.currentTransition = ReplaceSO (allSOs, clonedSOs, nodeCanvas.currentTransition);
			for (int nodeCnt = 0; nodeCnt < nodeCanvas.nodes.Count; nodeCnt++) 
			{ // Clone Nodes, structural content and additional scriptableObjects
				Node node = nodeCanvas.nodes[nodeCnt];
				Node clonedNode = nodeCanvas.nodes[nodeCnt] = ReplaceSO (allSOs, clonedSOs, node);
				// We're going to restore these from NodeKnobs if desired (!compressed)
				clonedNode.Inputs = new List<NodeInput> ();
				clonedNode.Outputs = new List<NodeOutput> ();
				for (int knobCnt = 0; knobCnt < clonedNode.nodeKnobs.Count; knobCnt++) 
				{ // Clone generic NodeKnobs
					NodeKnob knob = clonedNode.nodeKnobs[knobCnt] = ReplaceSO (allSOs, clonedSOs, clonedNode.nodeKnobs[knobCnt]);
					knob.body = clonedNode;
					// Replace additional scriptableObjects in the NodeKnob
					knob.CopyScriptableObjects ((ScriptableObject so) => ReplaceSO (allSOs, clonedSOs, so));
					if (!compressed) 
					{ // Add NodeInputs and NodeOutputs to the apropriate lists in Node if desired (!compressed)
						if (knob is NodeInput)
							clonedNode.Inputs.Add (knob as NodeInput);
						else if (knob is NodeOutput)
							clonedNode.Outputs.Add (knob as NodeOutput);
					}
				}
				for (int transCnt = 0; transCnt < clonedNode.transitions.Count; transCnt++)
				{ // Clone transitions
					Transition trans = clonedNode.transitions[transCnt];
					if (trans.startNode != node)
						continue;
					trans = clonedNode.transitions[transCnt] = ReplaceSO (allSOs, clonedSOs, trans);
					if (trans == null)
					{
						Debug.LogError ("Could not copy transition number " + transCnt + " of Node " + clonedNode.name + "!");
						continue;
					}

//					Debug.Log ("Did replace contents of Transition " + trans.name + " because its Node " + clonedNode.name + " is the start node!");
					trans.startNode = ReplaceSO (allSOs, clonedSOs, trans.startNode);
					trans.endNode = ReplaceSO (allSOs, clonedSOs, trans.endNode);

					if (!compressed)
						trans.endNode.transitions.Add (trans);
				}
				// Replace additional scriptableObjects in the Node
				node.CopyScriptableObjects ((ScriptableObject so) => ReplaceSO (allSOs, clonedSOs, so));
			}

			// Also create working copies for specified editorStates, if any
			// Needs to be in the same function as the EditorState references nodes from the NodeCanvas
			if (editorStates != null)
			{
				for (int stateCnt = 0; stateCnt < editorStates.Length; stateCnt++) 
				{
					if (editorStates[stateCnt] == null)
						continue;

					NodeEditorState state = editorStates[stateCnt] = Clone (editorStates[stateCnt]);
					state.canvas = nodeCanvas;
					state.focusedNode = null;
					state.selectedNode = state.selectedNode != null? ReplaceSO (allSOs, clonedSOs, state.selectedNode) : null;
					state.makeTransition = null;
					state.connectOutput = null;
				}	
			}
		}

// Returns all references to T in obj, using reflection
//	private static List<System.Reflection.FieldInfo> GetAllDirectReferences<T> (object obj) 
//	{
//		return obj.GetType ()
//			.GetFields (System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy)
//			.Where ((System.Reflection.FieldInfo field) => field.FieldType == typeof(T) || field.FieldType.IsSubclassOf (typeof(T)))
//			.ToList ();
//	}

		/// <summary>
		/// Clones the specified SO, preserving it's name
		/// </summary>
		private static T Clone<T> (T SO) where T : ScriptableObject 
		{
			string soName = SO.name;
			SO = Object.Instantiate<T> (SO);
			SO.name = soName;
			return SO;
		}

		/// <summary>
		/// Clones SO and writes both versions into the respective list
		/// </summary>
		private static ScriptableObject AddClonedSO<T> (List<ScriptableObject> scriptableObjects, List<ScriptableObject> clonedScriptableObjects, T initialSO) where T : ScriptableObject 
		{
			if (initialSO == null)
				return null;
			scriptableObjects.Add (initialSO);
			ScriptableObject clonedSO = Clone (initialSO);
			clonedScriptableObjects.Add (clonedSO);
			return clonedSO;
		}

		/// <summary>
		/// First two parameters are SOs and their respective clones. Will return the saved clone of initialSO
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
	}
}