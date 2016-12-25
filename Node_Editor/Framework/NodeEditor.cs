//#define NODE_EDITOR_LINE_CONNECTION

using UnityEngine;
using System;
using System.Collections.Generic;

using NodeEditorFramework;
using NodeEditorFramework.Utilities;

using Object = UnityEngine.Object;

namespace NodeEditorFramework
{
	/// <summary>
	/// Central class of NodeEditor providing the GUI to draw the Node Editor Canvas, bundling all other parts of the Framework
	/// </summary>
	public static partial class NodeEditor 
	{
		public static INodeCalculator Calculator;

		public static string editorPath = "Assets/Plugins/Node_Editor/";

		// The NodeCanvas which represents the currently drawn Node Canvas; globally accessed
		public static NodeCanvas curNodeCanvas;
		public static NodeEditorState curEditorState;

		// GUI callback control
		internal static Action NEUpdate;
		public static void Update () { if (NEUpdate != null) NEUpdate (); }
		public static Action ClientRepaints;
		public static void RepaintClients () { if (ClientRepaints != null) ClientRepaints (); }

		// Canvas Editing
		private static Stack<NodeCanvas> editCanvasStack = new Stack<NodeCanvas> (4);
		private static Stack<NodeEditorState> editEditorStateStack = new Stack<NodeEditorState> (4);

		#region Setup

		private static bool initiatedBase;
		private static bool initiatedGUI;
		public static bool InitiationError;

		/// <summary>
		/// Initiates the Node Editor if it wasn't yet
		/// </summary>
		public static void checkInit (bool GUIFunction) 
		{
			if (!InitiationError)
			{
				if (!initiatedBase)
					setupBaseFramework ();
				if (GUIFunction && !initiatedGUI)
					setupGUI ();
			}
		}

		/// <summary>
		/// Re-Inits the NodeCanvas regardless of whetehr it was initiated before
		/// </summary>
		public static void ReInit (bool GUIFunction) 
		{
			InitiationError = initiatedBase = initiatedGUI = false;
			
			setupBaseFramework ();
			if (GUIFunction)
				setupGUI ();
		}

		/// <summary>
		/// Setup of the base framework. Enough to manage and calculate canvases.
		/// </summary>
		private static void setupBaseFramework ()
		{
			CheckEditorPath ();
			Calculator = new DefaultNodeCalculator ();
			// Init Resource system. Can be called anywhere else, too, if it's needed before.
			ResourceManager.SetDefaultResourcePath (editorPath + "Resources/");

			// Run fetching algorithms searching the script assemblies for Custom Nodes / Connection Types / NodeCanvas Types
			ConnectionTypes.FetchTypes ();
			NodeTypes.FetchNodes ();
			NodeCanvasManager.GetAllCanvasTypes();

			// Setup Callback system
			NodeEditorCallbacks.SetupReceivers ();
			NodeEditorCallbacks.IssueOnEditorStartUp ();

			// Init input
			NodeEditorInputSystem.SetupInput ();

		#if UNITY_EDITOR
			UnityEditor.EditorApplication.update -= Update;
			UnityEditor.EditorApplication.update += Update;
		#endif

			initiatedBase = true;
		}

		/// <summary>
		/// Setup of the GUI. Only called when a GUI representation is actually used.
		/// </summary>
		private static void setupGUI ()
		{
			if (!initiatedBase)
				setupBaseFramework ();
			initiatedGUI = false;
			
			// Init GUIScaleUtility. This fetches reflected calls and my throw a message notifying about incompability.
			GUIScaleUtility.CheckInit ();

			if (!NodeEditorGUI.Init ()) 
			{	
				InitiationError = true;
				return;
			}

		#if UNITY_EDITOR
			RepaintClients ();
		#endif

			initiatedGUI = true;
		}

		/// <summary>
		/// Checks the editor path and corrects it when possible.
		/// </summary>
		public static void CheckEditorPath () 
		{
	#if UNITY_EDITOR
			Object script = UnityEditor.AssetDatabase.LoadAssetAtPath (editorPath + "Framework/NodeEditor.cs", typeof(Object));
			if (script == null) 
			{
				string[] assets = UnityEditor.AssetDatabase.FindAssets ("NodeEditorCallbackReceiver"); // Something relatively unique
				if (assets.Length != 1) 
				{
					assets = UnityEditor.AssetDatabase.FindAssets ("ConnectionTypes"); // Another try
					if (assets.Length != 1) 
						throw new UnityException ("Node Editor: Not installed in default directory '" + editorPath + "'! Correct path could not be detected! Please correct the editorPath variable in NodeEditor.cs!");
				}
				
				string correctEditorPath = UnityEditor.AssetDatabase.GUIDToAssetPath (assets[0]);
				int subFolderIndex = correctEditorPath.LastIndexOf ("Framework/");
				if (subFolderIndex == -1)
					throw new UnityException ("Node Editor: Not installed in default directory '" + editorPath + "'! Correct path could not be detected! Please correct the editorPath variable in NodeEditor.cs!");
				correctEditorPath = correctEditorPath.Substring (0, subFolderIndex);
				
				Debug.LogWarning ("Node Editor: Not installed in default directory '" + editorPath + "'! " +
								  "Editor-only automatic detection adjusted the path to " + correctEditorPath + ", but if you plan to use at runtime, please correct the editorPath variable in NodeEditor.cs!");
				editorPath = correctEditorPath;
			}
	#endif
		}
		
		#endregion
		
		#region GUI

		/// <summary>
		/// Draws the Node Canvas on the screen in the rect specified by editorState
		/// </summary>
		public static void DrawCanvas (NodeCanvas nodeCanvas, NodeEditorState editorState)  
		{
			if (!editorState.drawing)
				return;
			checkInit (true);

			DrawSubCanvas (nodeCanvas, editorState);
		}

		/// <summary>
		/// Draws the Node Canvas on the screen in the rect specified by editorState without one-time wrappers like GUISkin and OverlayGUI. Made for nested Canvases (WIP)
		/// </summary>
		private static void DrawSubCanvas (NodeCanvas nodeCanvas, NodeEditorState editorState)  
		{
			if (!editorState.drawing)
				return;
			
			BeginEditingCanvas (nodeCanvas, editorState);

			if (Event.current.type == EventType.Repaint) 
			{ // Draw Background when Repainting
				// Offset from origin in tile units
				Vector2 tileOffset = new Vector2 (-(curEditorState.zoomPos.x * curEditorState.zoom + curEditorState.panOffset.x) / NodeEditorGUI.Background.width, 
					((curEditorState.zoomPos.y - curEditorState.canvasRect.height) * curEditorState.zoom + curEditorState.panOffset.y) / NodeEditorGUI.Background.height);
				// Amount of tiles
				Vector2 tileAmount = new Vector2 (Mathf.Round (curEditorState.canvasRect.width * curEditorState.zoom) / NodeEditorGUI.Background.width,
					Mathf.Round (curEditorState.canvasRect.height * curEditorState.zoom) / NodeEditorGUI.Background.height);
				// Draw tiled background
				GUI.DrawTextureWithTexCoords (curEditorState.canvasRect, NodeEditorGUI.Background, new Rect (tileOffset, tileAmount));
			}

			// Handle input events
			NodeEditorInputSystem.HandleInputEvents (curEditorState);
			if (Event.current.type != EventType.Layout)
				curEditorState.ignoreInput = new List<Rect> ();

			// We're using a custom scale method, as default one is messing up clipping rect
			Rect canvasRect = curEditorState.canvasRect;
			curEditorState.zoomPanAdjust = GUIScaleUtility.BeginScale (ref canvasRect, curEditorState.zoomPos, curEditorState.zoom, NodeEditorGUI.isEditorWindow, false);

			// ---- BEGIN SCALE ----

			// Some features which require zoomed drawing:

			if (curEditorState.navigate) 
			{ // Draw a curve to the origin/active node for orientation purposes
				Vector2 startPos = (curEditorState.selectedNode != null? curEditorState.selectedNode.rect.center : curEditorState.panOffset) + curEditorState.zoomPanAdjust;
				Vector2 endPos = Event.current.mousePosition;
				RTEditorGUI.DrawLine (startPos, endPos, Color.green, null, 3); 
				RepaintClients ();
			}

			if (curEditorState.connectOutput != null)
			{ // Draw the currently drawn connection
				NodeOutput output = curEditorState.connectOutput;
				Vector2 startPos = output.GetGUIKnob ().center;
				Vector2 startDir = output.GetDirection ();
				Vector2 endPos = Event.current.mousePosition;
				// There is no specific direction of the end knob so we pick the best according to the relative position
				Vector2 endDir = NodeEditorGUI.GetSecondConnectionVector (startPos, endPos, startDir);
				NodeEditorGUI.DrawConnection (startPos, startDir, endPos, endDir, output.typeData.Color);
				RepaintClients ();
			}

			// Draw the groups below everything else
			for (int groupCnt = 0; groupCnt < curNodeCanvas.groups.Count; groupCnt++)
				curNodeCanvas.groups [groupCnt].DrawGroup ();
			
			// Push the active node to the top of the draw order.
			if (Event.current.type == EventType.Layout && curEditorState.selectedNode != null)
			{
				curNodeCanvas.nodes.Remove (curEditorState.selectedNode);
				curNodeCanvas.nodes.Add (curEditorState.selectedNode);
			}

			// Draw the transitions and connections. Has to be drawn before nodes as transitions originate from node centers
			for (int nodeCnt = 0; nodeCnt < curNodeCanvas.nodes.Count; nodeCnt++)
				curNodeCanvas.nodes [nodeCnt].DrawConnections ();

			// Draw the nodes
			for (int nodeCnt = 0; nodeCnt < curNodeCanvas.nodes.Count; nodeCnt++)
			{
				Node node = curNodeCanvas.nodes [nodeCnt];
				node.DrawNode ();
				if (Event.current.type == EventType.Repaint)
					node.DrawKnobs ();
			}

			// ---- END SCALE ----

			// End scaling group
			GUIScaleUtility.EndScale ();

			// Handle input events with less priority than node GUI controls
			NodeEditorInputSystem.HandleLateInputEvents (curEditorState);

			EndEditingCanvas ();
		}

		public static void BeginEditingCanvas (NodeCanvas canvas)
		{
			NodeEditorState state = canvas.editorStates.Length >= 1? canvas.editorStates[0] : null;
			BeginEditingCanvas (canvas, state);
		}

		public static void BeginEditingCanvas (NodeCanvas canvas, NodeEditorState state)
		{
			editCanvasStack.Push (canvas);
			editEditorStateStack.Push (state);
			curNodeCanvas = canvas;
			curEditorState = state;
		}

		public static void EndEditingCanvas ()
		{
			curNodeCanvas = editCanvasStack.Pop ();
			curEditorState = editEditorStateStack.Pop ();
		}

		#endregion

		#region Space Transformations

		/// <summary>
		/// Returns the node at the specified canvas-space position in the current editor
		/// </summary>
		public static Node NodeAtPosition (Vector2 canvasPos)
		{
			NodeKnob focusedKnob;
			return NodeAtPosition (curEditorState, canvasPos, out focusedKnob);
		}

		/// <summary>
		/// Returns the node at the specified canvas-space position in the current editor and returns a possible focused knob aswell
		/// </summary>
		public static Node NodeAtPosition (Vector2 canvasPos, out NodeKnob focusedKnob)
		{
			return NodeAtPosition (curEditorState, canvasPos, out focusedKnob);
		}

		/// <summary>
		/// Returns the node at the specified canvas-space position in the specified editor and returns a possible focused knob aswell
		/// </summary>
		public static Node NodeAtPosition (NodeEditorState editorState, Vector2 canvasPos, out NodeKnob focusedKnob)
		{
			focusedKnob = null;
			if (NodeEditorInputSystem.shouldIgnoreInput (editorState))
				return null;
			NodeCanvas canvas = editorState.canvas;
			for (int nodeCnt = canvas.nodes.Count-1; nodeCnt >= 0; nodeCnt--) 
			{ // Check from top to bottom because of the render order
				Node node = canvas.nodes [nodeCnt];
				if (node.rect.Contains (canvasPos))
					return node;
				for (int knobCnt = 0; knobCnt < node.nodeKnobs.Count; knobCnt++)
				{ // Check if any nodeKnob is focused instead
					if (node.nodeKnobs[knobCnt].GetCanvasSpaceKnob ().Contains (canvasPos)) 
					{
						focusedKnob = node.nodeKnobs[knobCnt];
						return node;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Transforms screen space elements in the current editor into canvas space (Level of Nodes, ...) 
		/// </summary>
		public static Vector2 ScreenToCanvasSpace (Vector2 screenPos) 
		{
			return ScreenToCanvasSpace (curEditorState, screenPos);
		}
		/// <summary>
		/// Transforms screen space elements in the specified editor into canvas space (Level of Nodes, ...) 
		/// </summary>
		public static Vector2 ScreenToCanvasSpace (NodeEditorState editorState, Vector2 screenPos) 
		{
			return (screenPos - editorState.canvasRect.position - editorState.zoomPos) * editorState.zoom - editorState.panOffset;
		}

		#endregion
	}
}
