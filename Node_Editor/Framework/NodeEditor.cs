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
	/// Only Calculation is yet to be split from this
	/// </summary>
	public static class NodeEditor 
	{
		public static string editorPath = "Assets/Plugins/Node_Editor/";

		// The NodeCanvas which represents the currently drawn Node Canvas; globally accessed
		public static NodeCanvas curNodeCanvas;
		public static NodeEditorState curEditorState;

		// Temp GUI state variables
		private static bool unfocusControls;
		private static Vector2 mousePos;

		// GUI callback control
		internal static Action NEUpdate;
		public static void Update () { if (NEUpdate != null) NEUpdate (); }
		public static Action ClientRepaints;
		public static void RepaintClients () { if (ClientRepaints != null) ClientRepaints (); }

		#region Setup

		public static bool initiated;
		public static bool InitiationError;

		/// <summary>
		/// Initiates the Node Editor if it wasn't yet
		/// </summary>
		public static void checkInit () 
		{
			if (!initiated && !InitiationError)
				ReInit (true);
		}

		/// <summary>
		/// Re-Inits the NodeCanvas regardless of whetehr it was initiated before
		/// </summary>
		public static void ReInit (bool GUIFunction) 
		{
			CheckEditorPath ();

			// Init Resource system. Can be called anywhere else, too, if it's needed before.
			ResourceManager.SetDefaultResourcePath (editorPath + "Resources/");
			
			// Init NE GUI. I may throw an error if a texture was not found.	
			if (!NodeEditorGUI.Init (GUIFunction)) 
			{	
				InitiationError = true;
				return;
			}

			// Run fetching algorithms searching the script assemblies for Custom Nodes / Connection Types
			ConnectionTypes.FetchTypes ();
			NodeTypes.FetchNodes ();

			// Setup Callback system
			NodeEditorCallbacks.SetupReceivers ();
			NodeEditorCallbacks.IssueOnEditorStartUp ();

			// Init GUIScaleUtility. This fetches reflected calls and my throw a message notifying about incompability.
			GUIScaleUtility.CheckInit ();

	#if UNITY_EDITOR
			UnityEditor.EditorApplication.update -= Update;
			UnityEditor.EditorApplication.update += Update;
			RepaintClients ();
	#endif
			initiated = true;
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
			checkInit ();

			NodeEditorGUI.StartNodeGUI ();
			OverlayGUI.StartOverlayGUI ();
			DrawSubCanvas (nodeCanvas, editorState);
			OverlayGUI.EndOverlayGUI ();
			NodeEditorGUI.EndNodeGUI ();
		}

		/// <summary>
		/// Draws the Node Canvas on the screen in the rect specified by editorState without one-time wrappers like GUISkin and OverlayGUI. Made for nested Canvases (WIP)
		/// </summary>
		public static void DrawSubCanvas (NodeCanvas nodeCanvas, NodeEditorState editorState)  
		{
			if (!editorState.drawing)
				return;

			// Store and restore later on in case of this being a nested Canvas
			NodeCanvas prevNodeCanvas = curNodeCanvas;
			NodeEditorState prevEditorState = curEditorState;
			
			curNodeCanvas = nodeCanvas;
			curEditorState = editorState;

			if (Event.current.type == EventType.Repaint) 
			{ // Draw Background when Repainting
				GUI.BeginClip (curEditorState.canvasRect);

				Vector2 offset = curEditorState.zoomPos + curEditorState.panOffset/curEditorState.zoom;
				Vector2 ratio = new Vector2 (curEditorState.zoom/NodeEditorGUI.Background.width,
											 curEditorState.zoom/NodeEditorGUI.Background.height);
				GUI.DrawTextureWithTexCoords(curEditorState.canvasRect, 
											 NodeEditorGUI.Background,
											 new Rect(  -offset.x*ratio.x,
											        	offset.y*ratio.y,
														ratio.x*curEditorState.canvasRect.width,
														ratio.y*curEditorState.canvasRect.height));

				GUI.EndClip ();
			}
			
			// Check the inputs
			InputEvents ();
			if (Event.current.type != EventType.Layout)
				curEditorState.ignoreInput = new List<Rect> ();

			// We're using a custom scale method, as default one is messing up clipping rect
			Rect canvasRect = curEditorState.canvasRect;
			curEditorState.zoomPanAdjust = GUIScaleUtility.BeginScale (ref canvasRect, curEditorState.zoomPos, curEditorState.zoom, false);

			// ---- BEGIN SCALE ----

			// Some features which require drawing (zoomed)
			if (curEditorState.navigate) 
			{ // Draw a curve to the origin/active node for orientation purposes
				RTEditorGUI.DrawLine ((curEditorState.selectedNode != null? curEditorState.selectedNode.rect.center : curEditorState.panOffset) + curEditorState.zoomPanAdjust, 
				                      ScreenToGUIPos (mousePos) + curEditorState.zoomPos * curEditorState.zoom, 
				                      Color.black, null, 3); 
				RepaintClients ();
			}
			if (curEditorState.connectOutput != null)
			{ // Draw the currently drawn connection
				NodeOutput output = curEditorState.connectOutput;
				Vector2 startPos = output.GetGUIKnob ().center;
				Vector2 endPos = ScreenToGUIPos (mousePos) + curEditorState.zoomPos * curEditorState.zoom;
				Vector2 endDir = output.GetDirection ();
				NodeEditorGUI.DrawConnection (startPos, endDir, endPos, NodeEditorGUI.GetSecondConnectionVector (startPos, endPos, endDir), output.typeData.col);
				RepaintClients ();
			}

			// Push the active node at the bottom of the draw order.
			if (Event.current.type == EventType.Layout && curEditorState.selectedNode != null)
			{
				curNodeCanvas.nodes.Remove (curEditorState.selectedNode);
				curNodeCanvas.nodes.Add (curEditorState.selectedNode);
			}

			// Draw the transitions and connections. Has to be drawn before nodes as transitions originate from node centers
			for (int nodeCnt = 0; nodeCnt < curNodeCanvas.nodes.Count; nodeCnt++)  
			{
				Node node = curNodeCanvas.nodes [nodeCnt];
				node.DrawConnections ();
			}

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
			
			// Check events with less priority than node GUI controls
			LateEvents ();
			
			curNodeCanvas = prevNodeCanvas;
			curEditorState = prevEditorState;
		}
		
		#endregion
		
		#region GUI Functions

		/// <summary>
		/// Returns the node at the position in the current canvas spcae. Depends on curEditorState and curNodecanvas
		/// </summary>
		public static Node NodeAtPosition (Vector2 pos)
		{
			return NodeAtPosition (curEditorState, curNodeCanvas, pos);
		}
		/// <summary>
		/// Returns the node at the position in specified canvas space.
		/// </summary>
		public static Node NodeAtPosition (NodeEditorState editorState, NodeCanvas nodeCanvas, Vector2 pos)
		{	
			if (!editorState.canvasRect.Contains (pos))
				return null;
			for (int nodeCnt = nodeCanvas.nodes.Count-1; nodeCnt >= 0; nodeCnt--) 
			{ // Check from top to bottom because of the render order
				Node node = nodeCanvas.nodes [nodeCnt];
				if (CanvasGUIToScreenRect (node.rect).Contains (pos)) // Node Body
					return node;
				for (int knobCnt = 0; knobCnt < node.nodeKnobs.Count; knobCnt++)
				{ // Any edge control
					if (node.nodeKnobs[knobCnt].GetScreenKnob ().Contains (pos))
						return node;
				}
			}
			return null;
		}

		/// <summary>
		/// Transforms the Rect in GUI space into Screen space. Depends on curEditorState
		/// </summary>
		public static Rect CanvasGUIToScreenRect (Rect rect) 
		{
			return CanvasGUIToScreenRect (curEditorState, rect);
		}
		/// <summary>
		/// Transforms the Rect in GUI space into Screen space
		/// </summary>
		public static Rect CanvasGUIToScreenRect (NodeEditorState editorState, Rect rect) 
		{
			rect.position += editorState.zoomPos;
			rect = GUIScaleUtility.Scale (rect, editorState.zoomPos, 
					editorState.parentEditor != null? new Vector2 (1/(editorState.parentEditor.zoom*editorState.zoom), 1/(editorState.parentEditor.zoom*editorState.zoom)) : 
														new Vector2 (1/editorState.zoom, 1/editorState.zoom));
			rect.position += editorState.canvasRect.position;
			return rect;
		}

		/// <summary>
		/// Transforms screen position pos (like mouse pos) to a point in current GUI space
		/// </summary>
		public static Vector2 ScreenToGUIPos (Vector2 pos) 
		{
			return ScreenToGUIPos (curEditorState, pos);
		}
		/// <summary>
		/// Transforms screen position pos (like mouse pos) to a point in specified GUI space
		/// </summary>
		public static Vector2 ScreenToGUIPos (NodeEditorState editorState, Vector2 pos) 
		{
			return Vector2.Scale (pos - editorState.zoomPos - editorState.canvasRect.position, new Vector2 (editorState.zoom, editorState.zoom));
		}

		/// <summary>
		/// Returns whether to account for input in curEditorState
		/// </summary>
		private static bool ignoreInput (Vector2 mousePos) 
		{
			// Account for any opened popups
			if (OverlayGUI.HasPopupControl ())
				return true;
			// Mouse outside of canvas rect or inside an ignoreInput rect
			if (!curEditorState.canvasRect.Contains (mousePos))
				return true;
			for (int ignoreCnt = 0; ignoreCnt < curEditorState.ignoreInput.Count; ignoreCnt++) 
			{
				if (curEditorState.ignoreInput [ignoreCnt].Contains (mousePos)) 
					return true;
			}
			return false;
		}
		
		#endregion
		
		#region Input Events

		/// <summary>
		/// Processes input events
		/// </summary>
		public static void InputEvents ()
		{
			Event e = Event.current;
			mousePos = e.mousePosition;

			bool leftClick = e.button == 0, rightClick = e.button == 1,
				mouseDown = e.type == EventType.MouseDown, mousUp = e.type == EventType.MouseUp;

			if (ignoreInput (mousePos))
				return;

			#region Change Node selection and focus
			// Choose focused and selected Node, accounting for focus changes
			curEditorState.focusedNode = null;
			if (mouseDown || mousUp)
			{
				curEditorState.focusedNode = NodeEditor.NodeAtPosition (mousePos);
				if (curEditorState.focusedNode != curEditorState.selectedNode)
					unfocusControls = true;
				if (mouseDown && leftClick) 
				{
					curEditorState.selectedNode = curEditorState.focusedNode;
					RepaintClients ();
				}
			}
			// Perform above mentioned focus changes in Repaint, which is the only suitable time to do this
			if (unfocusControls && Event.current.type == EventType.Repaint) 
			{
				GUIUtility.hotControl = 0;
				GUIUtility.keyboardControl = 0;
				unfocusControls = false;
			}
		#if UNITY_EDITOR
			if (curEditorState.focusedNode != null)
				UnityEditor.Selection.activeObject = curEditorState.focusedNode;
		#endif
			#endregion

			switch (e.type) 
			{
			case EventType.MouseDown:

				curEditorState.dragNode = false;
				curEditorState.panWindow = false;
				
				if (curEditorState.focusedNode != null) 
				{ // Clicked a Node
					if (rightClick)
					{ // Node Context Click
						GenericMenu menu = new GenericMenu ();
						menu.AddItem (new GUIContent ("Delete Node"), false, ContextCallback, new NodeEditorMenuCallback ("deleteNode", curNodeCanvas, curEditorState));
						menu.AddItem (new GUIContent ("Duplicate Node"), false, ContextCallback, new NodeEditorMenuCallback ("duplicateNode", curNodeCanvas, curEditorState));
						menu.ShowAsContext ();
						e.Use ();
					}
					else if (leftClick)
					{ // Detect click on a connection knob
						if (!CanvasGUIToScreenRect (curEditorState.focusedNode.rect).Contains (mousePos))
						{ // Clicked NodeEdge, check Node Inputs and Outputs
							NodeOutput nodeOutput = curEditorState.focusedNode.GetOutputAtPos (e.mousePosition);
							if (nodeOutput != null)
							{ // Output clicked -> New Connection drawn from this
								curEditorState.connectOutput = nodeOutput;
								e.Use();
								return;
							}

							NodeInput nodeInput = curEditorState.focusedNode.GetInputAtPos (e.mousePosition);
							if (nodeInput != null && nodeInput.connection != null)
							{ // Input clicked -> Loose and edit Connection
								curEditorState.connectOutput = nodeInput.connection;
								nodeInput.RemoveConnection ();
								e.Use();
							}
						}
					}
				}
				else
				{ // Clicked on canvas
					
					// NOTE: Panning is not done here but in LateEvents, so buttons on the canvas won't be blocked when clicking

					if (rightClick) 
					{ // Editor Context Click
						GenericMenu menu = new GenericMenu ();
						if (curEditorState.connectOutput != null) 
						{ // A connection is drawn, so provide a context menu with apropriate nodes to auto-connect
							foreach (Node node in NodeTypes.nodes.Keys)
							{ // Iterate through all nodes and check for compability
								for (int inputCnt = 0; inputCnt < node.Inputs.Count; inputCnt++)
								{
									if (node.Inputs[inputCnt].CanApplyConnection (curEditorState.connectOutput))
									{
										menu.AddItem (new GUIContent ("Add " + NodeTypes.nodes[node].adress), false, ContextCallback, new NodeEditorMenuCallback (node.GetID, curNodeCanvas, curEditorState));
										break;
									}
								}
							}
						}
						else 
						{ // Ordinary context click, add all nodes to add
							foreach (Node node in NodeTypes.nodes.Keys)
								menu.AddItem (new GUIContent ("Add " + NodeTypes.nodes [node].adress), false, ContextCallback, new NodeEditorMenuCallback (node.GetID, curNodeCanvas, curEditorState));
						}
						menu.ShowAsContext ();
						e.Use ();
					}
				}
				
				break;
				
			case EventType.MouseUp:

				if (curEditorState.focusedNode != null) 
				{ // Apply Drawn connections on node
					if (curEditorState.connectOutput != null) 
					{ // Apply a connection if theres a clicked input
						if (!curEditorState.focusedNode.Outputs.Contains (curEditorState.connectOutput)) 
						{ // An input was clicked, it'll will now be connected
							NodeInput clickedInput = curEditorState.focusedNode.GetInputAtPos (e.mousePosition);
							if (clickedInput.CanApplyConnection (curEditorState.connectOutput)) 
							{ // It can connect (type is equals, it does not cause recursion, ...)
								clickedInput.ApplyConnection (curEditorState.connectOutput);
							}
						}
						e.Use ();
					}
				}

				curEditorState.connectOutput = null;
				curEditorState.dragNode = false;
				curEditorState.panWindow = false;
				
				break;
				
			case EventType.ScrollWheel:

				// Apply Zoom
				curEditorState.zoom = (float)Math.Round (Math.Min (2.0f, Math.Max (0.6f, curEditorState.zoom + e.delta.y / 15)), 2);

				RepaintClients ();
				break;
				
			case EventType.KeyDown:

				// TODO: Node Editor: Shortcuts

				if (e.keyCode == KeyCode.N) // Start Navigating (curve to origin / active Node)
					curEditorState.navigate = true;
				
				if (e.keyCode == KeyCode.LeftControl && curEditorState.selectedNode != null)
				{ // Snap selected Node's position to multiples of 10
					Vector2 pos = curEditorState.selectedNode.rect.position;
					pos = (pos - curEditorState.panOffset) / 10;
					pos = new Vector2 (Mathf.RoundToInt (pos.x), Mathf.RoundToInt (pos.y));
					curEditorState.selectedNode.rect.position = pos * 10 + curEditorState.panOffset;
				}

				RepaintClients ();
				break;
				
			case EventType.KeyUp:
				
				if (e.keyCode == KeyCode.N) // Stop Navigating
					curEditorState.navigate = false;
				
				RepaintClients ();
				break;
			
			case EventType.MouseDrag:

				if (curEditorState.panWindow) 
				{ // Scroll everything with the current mouse delta
					curEditorState.panOffset += e.delta * curEditorState.zoom;
					for (int nodeCnt = 0; nodeCnt < curNodeCanvas.nodes.Count; nodeCnt++) 
						curNodeCanvas.nodes [nodeCnt].rect.position += e.delta * curEditorState.zoom;
					e.delta = Vector2.zero;
					RepaintClients ();
				}
				
				if (curEditorState.dragNode && curEditorState.selectedNode != null && GUIUtility.hotControl == 0) 
				{ // Drag the active node with the current mouse delta
					curEditorState.selectedNode.rect.position += e.delta * curEditorState.zoom;
					NodeEditorCallbacks.IssueOnMoveNode (curEditorState.selectedNode);
					e.delta = Vector2.zero;
					RepaintClients ();
				} 
				else
					curEditorState.dragNode = false;

				break;
			}
		}
		
		/// <summary>
		/// Proccesses late events. Called after GUI Functions, when they have higher priority in focus
		/// </summary>
		public static void LateEvents () 
		{
			Event e = Event.current;

			if (ignoreInput (mousePos))
				return;

			if (e.type == EventType.MouseDown && e.button == 0)
			{ // Left click
				if (GUIUtility.hotControl <= 0)
				{ // Did not click on a GUI Element
					if (curEditorState.selectedNode != null && CanvasGUIToScreenRect (curEditorState.selectedNode.rect).Contains (e.mousePosition)) 
					{ // Clicked inside the selected Node, so start dragging it
						curEditorState.dragNode = true;
						e.delta = Vector2.zero;
						RepaintClients ();
					}
					else if (curEditorState.focusedNode == null) 
					{ // Clicked on the empty canvas
						if (e.button == 0 || e.button == 2)
						{ // Start panning
							curEditorState.panWindow = true;
							e.delta = Vector2.zero;
						}
					}
				}
			}
		}

		/// <summary>
		/// Evaluates context callbacks previously registered
		/// </summary>
		public static void ContextCallback (object obj)
		{
			NodeEditorMenuCallback callback = obj as NodeEditorMenuCallback;
			if (callback == null)
				throw new UnityException ("Callback Object passed by context is not of type NodeEditorMenuCallback!");
			curNodeCanvas = callback.canvas;
			curEditorState = callback.editor;

			switch (callback.message)
			{
			case "deleteNode": // Delete request
				if (curEditorState.focusedNode != null) 
					curEditorState.focusedNode.Delete ();
				break;
				
			case "duplicateNode": // Duplicate request
				if (curEditorState.focusedNode != null) 
				{
					ContextCallback (new NodeEditorMenuCallback (curEditorState.focusedNode.GetID, curNodeCanvas, curEditorState));
					Node duplicatedNode = curNodeCanvas.nodes [curNodeCanvas.nodes.Count-1];

					curEditorState.focusedNode = duplicatedNode;
					curEditorState.dragNode = true;
					curEditorState.connectOutput = null;
					curEditorState.panWindow = false;
				}
				break;
			
			default: // Node creation request
				Node node = Node.Create (callback.message, ScreenToGUIPos (callback.contextClickPos));

				// Handle auto-connection
				if (curEditorState.connectOutput != null)
				{ // If nodeOutput is defined, link it to the first input of the same type
					foreach (NodeInput input in node.Inputs)
					{
						if (input.CanApplyConnection (curEditorState.connectOutput))
						{ // If it can connect (type is equals, it does not cause recursion, ...)
							input.ApplyConnection (curEditorState.connectOutput);
							break;
						}
					}
				}

				curEditorState.connectOutput = null;
				curEditorState.dragNode = false;
				curEditorState.panWindow = false;

				break;
			}
			RepaintClients ();
		}

		public class NodeEditorMenuCallback
		{
			public string message;
			public NodeCanvas canvas;
			public NodeEditorState editor;
			public Vector2 contextClickPos;

			public NodeEditorMenuCallback (string Message, NodeCanvas nodecanvas, NodeEditorState editorState) 
			{
				message = Message;
				canvas = nodecanvas;
				editor = editorState;
				contextClickPos = Event.current.mousePosition;
			}
		}
		
		#endregion

		#region Calculation
		
		// A list of Nodes from which calculation originates -> Call StartCalculation
		public static List<Node> workList;
		private static int calculationCount;

		/// <summary>
		/// Recalculate from every Input Node.
		/// Usually does not need to be called at all, the smart calculation system is doing the job just fine
		/// </summary>
		public static void RecalculateAll (NodeCanvas nodeCanvas) 
		{
			workList = new List<Node> ();
			foreach (Node node in nodeCanvas.nodes) 
			{
				if (node.isInput ())
				{ // Add all Inputs
					node.ClearCalculation ();
					workList.Add (node);
				}
			}
			StartCalculation ();
		}
		
		/// <summary>
		/// Recalculate from this node. 
		/// Usually does not need to be called manually
		/// </summary>
		public static void RecalculateFrom (Node node) 
		{
			node.ClearCalculation ();
			workList = new List<Node> { node };
			StartCalculation ();
		}
		
		/// <summary>
		/// Iterates through workList and calculates everything, including children
		/// </summary>
		public static void StartCalculation () 
		{
			if (workList == null || workList.Count == 0)
				return;
			// this blocks iterates through the worklist and starts calculating
			// if a node returns false, it stops and adds the node to the worklist
			// this workList is worked on until it's empty or a limit is reached
			calculationCount = 0;
			bool limitReached = false;
			for (int roundCnt = 0; !limitReached; roundCnt++)
			{ // Runs until every node possible is calculated
				limitReached = true;
				for (int workCnt = 0; workCnt < workList.Count; workCnt++) 
				{
					if (ContinueCalculation (workList [workCnt]))
						limitReached = false;
				}
			}
		}
		
		/// <summary>
		/// Recursive function which continues calculation on this node and all the child nodes
		/// Usually does not need to be called manually
		/// Returns success/failure of this node only
		/// </summary>
		public static bool ContinueCalculation (Node node) 
		{
			if (node.calculated)
				return false;
			if ((node.descendantsCalculated () || node.isInLoop ()) && node.Calculate ())
			{ // finished Calculating, continue with the children
				node.calculated = true;
				calculationCount++;
				workList.Remove (node);
				if (node.ContinueCalculation && calculationCount < 1000) 
				{
					for (int outCnt = 0; outCnt < node.Outputs.Count; outCnt++)
					{
						NodeOutput output = node.Outputs [outCnt];
						for (int conCnt = 0; conCnt < output.connections.Count; conCnt++)
							ContinueCalculation (output.connections [conCnt].body);
					}
				}
				else if (calculationCount >= 1000)
					Debug.LogError ("Stopped calculation because of suspected Recursion. Maximum calculation iteration is currently at 1000!");
				return true;
			}
			else if (!workList.Contains (node)) 
			{ // failed to calculate, add it to check later
				workList.Add (node);
			}
			return false;
		}
		
		#endregion
	}
}