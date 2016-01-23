//#define NODE_EDITOR_LINE_CONNECTION

using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;

using Object = UnityEngine.Object;

namespace NodeEditorFramework
{
	public static class NodeEditor 
	{
		public static string editorPath = "Assets/Plugins/Node_Editor/";

		// The NodeCanvas which represents the currently drawn Node Canvas; globally accessed
		public static NodeCanvas curNodeCanvas;
		public static NodeEditorState curEditorState;

		private static bool unfocusControls;
		public static Vector2 mousePos;

		public static Action ClientRepaints;
		public static void RepaintClients () 
		{
			if (ClientRepaints != null)
				ClientRepaints ();
		}

		#region Setup

		public static bool initiated = false;
		public static bool InitiationError = false;
		
		public static void checkInit () 
		{
			if (!initiated && !InitiationError)
				ReInit (true);
		}

		public static void ReInit (bool GUIFunction) 
		{
			CheckEditorPath ();

			// Init Resource system. Can be called anywhere else, too, if it's needed before.
			ResourceManager.Init (editorPath + "Resources/");
			
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
			RepaintClients ();
	#endif
			initiated = true;
		}

		/// <summary>
		/// Checks the editor path and corrects when possible.
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
				
				float width = NodeEditorGUI.Background.width / curEditorState.zoom;
				float height = NodeEditorGUI.Background.height / curEditorState.zoom;
				Vector2 offset = curEditorState.zoomPos + curEditorState.panOffset/curEditorState.zoom;
				offset = new Vector2 (offset.x%width - width, offset.y%height - height);
				int tileX = Mathf.CeilToInt ((curEditorState.canvasRect.width + (width - offset.x)) / width);
				int tileY = Mathf.CeilToInt ((curEditorState.canvasRect.height + (height - offset.y)) / height);
				
				for (int x = 0; x < tileX; x++) 
				{
					for (int y = 0; y < tileY; y++) 
					{
						GUI.DrawTexture (new Rect (offset.x + x*width, 
												   offset.y + y*height, 
												   width, height), 
										 NodeEditorGUI.Background);
					}
				}
				GUI.EndClip ();
			}
			
			// Check the inputs
			InputEvents ();
			if (Event.current.type != EventType.Layout)
				curEditorState.ignoreInput = new List<Rect> ();


			// We're using a custom scale method, as default one is messing up clipping rect
			Rect canvasRect = curEditorState.canvasRect;
			curEditorState.zoomPanAdjust = GUIScaleUtility.BeginScale (ref canvasRect, curEditorState.zoomPos, curEditorState.zoom, false);
			//GUILayout.Label ("Scaling is Great!"); -> TODO: Test by changing the last bool parameter

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
				NodeEditorGUI.DrawConnection (startPos, endDir, endPos, NodeEditorGUI.GetSecondConnectionVector (startPos, endPos, endDir), ConnectionTypes.GetTypeData (output.type).col);
				RepaintClients ();
			}
			if (curEditorState.makeTransition != null)
			{ // Draw the currently made transition
				RTEditorGUI.DrawLine (curEditorState.makeTransition.rect.center + curEditorState.zoomPanAdjust, 
				                      ScreenToGUIPos (mousePos) + curEditorState.zoomPos * curEditorState.zoom,
				                      Color.grey, null, 3); 
				RepaintClients ();
			}

			// Push the active node at the bottom of the draw order.
			if (Event.current.type == EventType.Layout && curEditorState.selectedNode != null)
			{
				curNodeCanvas.nodes.Remove (curEditorState.selectedNode);
				curNodeCanvas.nodes.Add (curEditorState.selectedNode);
			}

			if (Event.current.type == EventType.Repaint)
			{
				// Draw the transitions and connections. Has to be drawn before nodes as transitions originate from node centers
				for (int nodeCnt = 0; nodeCnt < curNodeCanvas.nodes.Count; nodeCnt++)  
				{
					Node node = curNodeCanvas.nodes [nodeCnt];
					node.DrawTransitions ();
					node.DrawConnections ();
				}
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
		public static Node NodeAtPosition (NodeEditorState editorState, NodeCanvas nodecanvas, Vector2 pos)
		{	
			if (!editorState.canvasRect.Contains (pos))
				return null;
			for (int nodeCnt = nodecanvas.nodes.Count-1; nodeCnt >= 0; nodeCnt--) 
			{ // Check from top to bottom because of the render order
				Node node = nodecanvas.nodes [nodeCnt];
				if (CanvasGUIToScreenRect (node.rect).Contains (pos))
					return node;
				for (int outCnt = 0; outCnt < node.Outputs.Count; outCnt++)
					if (node.Outputs[outCnt].GetScreenKnob ().Contains (pos))
						return node;
				for (int inCnt = 0; inCnt < node.Inputs.Count; inCnt++)
					if (node.Inputs[inCnt].GetScreenKnob ().Contains (pos))
						return node;
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
			if (editorState.parentEditor != null)
				rect = GUIScaleUtility.ScaleRect (rect, editorState.zoomPos, new Vector2 (1/(editorState.parentEditor.zoom*editorState.zoom), 1/(editorState.parentEditor.zoom*editorState.zoom)));
			else
				rect = GUIScaleUtility.ScaleRect (rect, editorState.zoomPos, new Vector2 (1/editorState.zoom, 1/editorState.zoom));
			rect.position += editorState.canvasRect.position;
			return rect;
		}

		/// <summary>
		/// Transforms screen position pos (like mouse pos) to a point in current GUI space
		/// </summary>
		/// 
		public static Vector2 ScreenToGUIPos (Vector2 pos) 
		{
			return ScreenToGUIPos (curEditorState, pos);
		}
		/// <summary>
		/// Transforms screen position pos (like mouse pos) to a point in specified GUI space
		/// </summary>
		/// 
		public static Vector2 ScreenToGUIPos (NodeEditorState editorState, Vector2 pos) 
		{
			return Vector2.Scale (pos - editorState.zoomPos - editorState.canvasRect.position, new Vector2 (editorState.zoom, editorState.zoom));
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

			if (OverlayGUI.HasPopupControl ()) // Give custom popup solution control when it needs to
				return;

			// Check if we are inside the canvas, accounting for ignoring groups
			if (!curEditorState.canvasRect.Contains (e.mousePosition))
				return;
			for (int ignoreCnt = 0; ignoreCnt < curEditorState.ignoreInput.Count; ignoreCnt++) 
			{
				if (curEditorState.ignoreInput [ignoreCnt].Contains (e.mousePosition)) 
					return;
			}

			// Choose focused and selected Node, accounting for focus changes
			curEditorState.focusedNode = null;
			if (e.type == EventType.MouseDown || e.type == EventType.MouseUp)
			{
				curEditorState.focusedNode = NodeEditor.NodeAtPosition (e.mousePosition);
				if (curEditorState.focusedNode != curEditorState.selectedNode)
					unfocusControls = true;
				if (e.button == 0) 
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

			switch (e.type) 
			{
			case EventType.MouseDown:

				curEditorState.dragNode = false;
				curEditorState.panWindow = false;
				
				if (curEditorState.focusedNode != null) 
				{ // A click on a node
					if (e.button == 1)
					{ // Right click -> Node Context Click
						GenericMenu menu = new GenericMenu ();

						menu.AddItem (new GUIContent ("Delete Node"), false, ContextCallback, new NodeEditorMenuCallback ("deleteNode", curNodeCanvas, curEditorState));
						menu.AddItem (new GUIContent ("Duplicate Node"), false, ContextCallback, new NodeEditorMenuCallback ("duplicateNode", curNodeCanvas, curEditorState));
						if (curEditorState.focusedNode.AcceptsTranstitions)
						{
							menu.AddSeparator ("Seperator");
							menu.AddItem (new GUIContent ("Make Transition"), false, ContextCallback, new NodeEditorMenuCallback ("startTransition", curNodeCanvas, curEditorState));
						}

						menu.ShowAsContext ();

						e.Use ();
					}
					else if (e.button == 0)
					{
						if (!CanvasGUIToScreenRect (curEditorState.focusedNode.rect).Contains (e.mousePosition))
						{ // Left click at node edges -> Check for clicked connections to edit
							NodeOutput nodeOutput = curEditorState.focusedNode.GetOutputAtPos (e.mousePosition);
							if (nodeOutput != null)
							{ // Output Node -> New Connection drawn from this
								curEditorState.connectOutput = nodeOutput;
								e.Use();
							}
							else 
							{ // no output clicked, check input
								NodeInput nodeInput = curEditorState.focusedNode.GetInputAtPos (e.mousePosition);
								if (nodeInput != null && nodeInput.connection != null)
								{ // Input node -> Loose and edit Connection
									curEditorState.connectOutput = nodeInput.connection;
									Node.RemoveConnection (nodeInput);
									e.Use();
								}
							}
						}
					}
				}
				else
				{ // A click on the empty canvas
					if (e.button == 2 || e.button == 0)
					{ // Left/Middle Click -> Start scrolling
						curEditorState.panWindow = true;
						e.delta = Vector2.zero;
					}
					else if (e.button == 1) 
					{ // Right click -> Editor Context Click
						if (curEditorState.connectOutput != null || curEditorState.makeTransition != null) 
						{
							GenericMenu menu = new GenericMenu ();

							// Iterate through all compatible nodes
							foreach (Node node in NodeTypes.nodes.Keys)
							{
								if (curEditorState.connectOutput != null) 
								{
									foreach (var input in node.Inputs)
									{
										if (input.type == curEditorState.connectOutput.type)
										{
											menu.AddItem (new GUIContent ("Add " + NodeTypes.nodes[node].adress), false, ContextCallback, new NodeEditorMenuCallback (node.GetID, curNodeCanvas, curEditorState));
											break;
										}
									}
								}
								else if (curEditorState.makeTransition != null && node.AcceptsTranstitions) 
								{
									menu.AddItem (new GUIContent ("Add " + NodeTypes.nodes[node].adress), false, ContextCallback, new NodeEditorMenuCallback (node.GetID, curNodeCanvas, curEditorState));
								}
							}
							
							menu.ShowAsContext ();
						}
						else 
						{ // Else add every Node avaiable
							GenericMenu menu = new GenericMenu ();
							foreach (Node node in NodeTypes.nodes.Keys)
								menu.AddItem (new GUIContent ("Add " + NodeTypes.nodes [node].adress), false, ContextCallback, new NodeEditorMenuCallback (node.GetID, curNodeCanvas, curEditorState));
							menu.ShowAsContext ();
						}
						e.Use ();
					}
				}
				
				break;
				
			case EventType.MouseUp:

				if (curEditorState.focusedNode != null) 
				{
					if (curEditorState.makeTransition != null)
					{
						Node.CreateTransition (curEditorState.makeTransition, curEditorState.focusedNode);
					}
					else if (curEditorState.connectOutput != null) 
					{ // Apply a connection if theres a clicked input
						if (!curEditorState.focusedNode.Outputs.Contains (curEditorState.connectOutput)) 
						{ // If an input was clicked, it'll will now be connected
							NodeInput clickedInput = curEditorState.focusedNode.GetInputAtPos (e.mousePosition);
							if (Node.CanApplyConnection (curEditorState.connectOutput, clickedInput)) 
							{ // If it can connect (type is equals, it does not cause recursion, ...)
								Node.ApplyConnection (curEditorState.connectOutput, clickedInput);
							}
						}
						e.Use ();
					}
				}
				
				curEditorState.makeTransition = null;
				curEditorState.connectOutput = null;
				curEditorState.dragNode = false;
				curEditorState.panWindow = false;
				
				break;
				
			case EventType.ScrollWheel:

				curEditorState.zoom = (float)Math.Round (Math.Min (2.0f, Math.Max (0.6f, curEditorState.zoom + e.delta.y / 15)), 2);
				RepaintClients ();

				break;
				
			case EventType.KeyDown:

				// TODO: Node Editor: Shortcuts
				if (e.keyCode == KeyCode.N) // Start Navigating (curve to origin / active Node)
					curEditorState.navigate = true;
				
				if (e.keyCode == KeyCode.LeftControl && curEditorState.selectedNode != null) // Snap
					curEditorState.selectedNode.rect.position = new Vector2 (Mathf.RoundToInt ((curEditorState.selectedNode.rect.position.x - curEditorState.panOffset.x) / 10) * 10 + curEditorState.panOffset.x, 
					                                                         Mathf.RoundToInt ((curEditorState.selectedNode.rect.position.y - curEditorState.panOffset.y) / 10) * 10 + curEditorState.panOffset.y);
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
				else 
					curEditorState.panWindow = false;
				
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

			// Check if we are inside the canvas, accounting for ignoring groups
			if (!curEditorState.canvasRect.Contains (e.mousePosition))
				return;
			for (int ignoreCnt = 0; ignoreCnt < curEditorState.ignoreInput.Count; ignoreCnt++) 
			{
				if (curEditorState.ignoreInput [ignoreCnt].Contains (e.mousePosition)) 
					return;
			}

			if (e.type == EventType.MouseDown && e.button == 0 && curEditorState.selectedNode != null && CanvasGUIToScreenRect (curEditorState.selectedNode.rect).Contains (e.mousePosition))
			{ // Left click inside activeNode -> Drag Node
				// Because of hotControl we have to put it after the GUI Functions
				if (GUIUtility.hotControl == 0)
				{ // We didn't clicked on GUI module, so we'll start dragging the node
					curEditorState.dragNode = true;
					// Because this is the delta from when it was last checked, we have to reset it each time
					e.delta = Vector2.zero;
					RepaintClients ();
				}
			}
		}

		/// <summary>
		/// Context Click selection. Here you'll need to register your own using a string identifier
		/// </summary>
		public static void ContextCallback (object obj)
		{
			NodeEditorMenuCallback callback = obj as NodeEditorMenuCallback;
			curNodeCanvas = callback.canvas;
			curEditorState = callback.editor;

			switch (callback.message)
			{
			case "deleteNode":
				if (curEditorState.focusedNode != null) 
					curEditorState.focusedNode.Delete ();
				break;
				
			case "duplicateNode":
				if (curEditorState.focusedNode != null) 
				{
					ContextCallback (new NodeEditorMenuCallback (curEditorState.focusedNode.GetID, curNodeCanvas, curEditorState));
					Node duplicatedNode = curNodeCanvas.nodes [curNodeCanvas.nodes.Count-1];

					curEditorState.focusedNode = duplicatedNode;
					curEditorState.dragNode = true;
					curEditorState.makeTransition = null;
					curEditorState.connectOutput = null;
					curEditorState.panWindow = false;
				}
				break;

			case "startTransition":
				if (curEditorState.focusedNode != null) 
				{
					curEditorState.makeTransition = curEditorState.focusedNode;
					curEditorState.connectOutput = null;
				}
				curEditorState.dragNode = false;
				curEditorState.panWindow = false;

				break;

			default:
				Vector2 createPos = ScreenToGUIPos (mousePos);

				Node node = NodeTypes.getDefaultNode (callback.message);
				if (node == null)
					break;

				node = node.Create (createPos);
				node.InitBase ();
				NodeEditorCallbacks.IssueOnAddNode (node);

				if (curEditorState.connectOutput != null)
				{ // If nodeOutput is defined, link it to the first input of the same type
					foreach (NodeInput input in node.Inputs)
					{
						if (Node.CanApplyConnection (curEditorState.connectOutput, input))
						{ // If it can connect (type is equals, it does not cause recursion, ...)
							Node.ApplyConnection (curEditorState.connectOutput, input);
							break;
						}
					}
				}
				else if (node.AcceptsTranstitions && curEditorState.makeTransition != null) 
				{
					Node.CreateTransition (curEditorState.makeTransition, node);
				}

				curEditorState.makeTransition = null;
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

			public NodeEditorMenuCallback (string Message, NodeCanvas nodecanvas, NodeEditorState editorState) 
			{
				message = Message;
				canvas = nodecanvas;
				editor = editorState;
			}
		}
		
		#endregion
		
		#region Calculation

		// STATE SYSTEM:

		internal static List<NodeCanvas> transitioningNodeCanvases = new List<NodeCanvas> ();

		public static void BeginTransitioning (NodeCanvas nodeCanvas, Node beginNode) 
		{
			if (!nodeCanvas.nodes.Contains (beginNode)) 
				throw new UnityException ("Node to begin transitioning from has to be associated with the passed NodeEditorState!");

			nodeCanvas.currentNode = beginNode;
			if (!transitioningNodeCanvases.Contains (nodeCanvas))
				transitioningNodeCanvases.Add (nodeCanvas);

			Debug.Log ("Beginning transitioning " + nodeCanvas.name + " from Node " + beginNode.name);

		#if UNITY_EDITOR
			UnityEditor.EditorApplication.update -= WaitForTransitions;
			UnityEditor.EditorApplication.update += WaitForTransitions;
		#endif
		}

		public static void StopTransitioning (NodeCanvas nodeCanvas) 
		{
			if (transitioningNodeCanvases.Contains (nodeCanvas))
				transitioningNodeCanvases.Remove (nodeCanvas);
			nodeCanvas.currentNode = null;
			Debug.Log ("Stopped transitioning " + nodeCanvas.name + " at Node " + nodeCanvas.currentNode.name);
		}

		private static void WaitForTransitions () 
		{
			for (int cnt = 0; cnt < transitioningNodeCanvases.Count; cnt++)
			{
				NodeCanvas nodeCanvas = transitioningNodeCanvases[cnt];
				if (!nodeCanvas.currentNode.AcceptsTranstitions) 
					throw new UnityException ("Cannot transition from Node " + nodeCanvas.currentNode.name + " as it does not accept transitions!");

				Debug.Log ("Approaching transitioning " + nodeCanvas.name + " at Node " + nodeCanvas.currentNode.name);

				Node nextNode;
				if (!GetNext (nodeCanvas.currentNode, null, out nextNode)) // No further nodes to transition to
				{
					StopTransitioning (nodeCanvas);
					cnt--;
					continue;
				}
				if (nextNode != nodeCanvas.currentNode) // Transitioned to the next node
				{
					Debug.Log ("Transitioning from " + nodeCanvas.currentNode.name + " to " + nextNode.name);
					nodeCanvas.currentNode = nextNode;
				}
			}
		}

		/// <summary>
		/// If any transitions of the node have their conditions met, it saves the nextNode the transition points to, else returns the passed node.
		/// Returns false when the node hasn't any transitions to transition to.
		/// </summary>
		public static bool GetNext (Node node, Node prevNode, out Node nextNode) 
		{
			nextNode = null;
			if (node.transitions.Count == 0)
				return false;
			for (int transCnt = 0; transCnt < node.transitions.Count; transCnt++) 
			{
				Transition trans = node.transitions[transCnt];
				if ((trans.endNode != prevNode || trans.startNode != prevNode) && trans.conditionsMet ()) 
				{
					if (trans.startNode == node)
						nextNode = trans.endNode;
					else if (trans.endNode == node)
						nextNode = trans.startNode;
					else
						Debug.Log ("Node " + node.name + " is not part of the transition " + trans.name);
					break;
				}
			}
			return true;
		}

		// CALCULATION SYSTEM:
		
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
			calculationCount = 0;
			// this blocks iterates through the worklist and starts calculating
			// if a node returns false state it stops and adds the node to the worklist
			// later on, this worklist is reworked
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

		#region Save/Load

		// SAVE:

		/// <summary>
		/// Saves the current node canvas as a new asset and links optional editorStates with it
		/// </summary>
		public static void SaveNodeCanvas (NodeCanvas nodeCanvas, string path, params NodeEditorState[] editorStates) 
		{
			if (String.IsNullOrEmpty (path))
				return;
			path = path.Replace (Application.dataPath, "Assets");
	#if UNITY_EDITOR
			// Copy and write Canvas
			nodeCanvas = GetWorkingCopy (nodeCanvas);
			UnityEditor.AssetDatabase.CreateAsset (nodeCanvas, path);

			// Copy and write Editor States
			for (int stateCnt = 0; stateCnt < editorStates.Length; stateCnt++)
			{
				NodeEditorState editorState = editorStates[stateCnt] = GetWorkingCopy (editorStates[stateCnt]);
				editorState.canvas = nodeCanvas;
				AddSubAsset (editorState, nodeCanvas);
			}

			// Write Node+contents
			for (int nodeCnt = 0; nodeCnt < nodeCanvas.nodes.Count; nodeCnt++) 
			{ // Write every node and each of it's subcomponents of type ScriptableObject into the file 
				Node node = nodeCanvas.nodes [nodeCnt];
				AddSubAsset (node, nodeCanvas);
				for (int inCnt = 0; inCnt < node.Inputs.Count; inCnt++) 
					AddSubAsset (node.Inputs [inCnt], node);
				for (int outCnt = 0; outCnt < node.Outputs.Count; outCnt++)
					AddSubAsset (node.Outputs [outCnt], node);
				for (int transCnt = 0; transCnt < node.transitions.Count; transCnt++)
					AddSubAsset (node.transitions [transCnt], node);
			}
			UnityEditor.AssetDatabase.SaveAssets ();
			UnityEditor.AssetDatabase.Refresh ();
	#else
			// TODO: Node Editor: Need to implement ingame-saving (Resources, AsssetBundles, ... won't work)
	#endif
			NodeEditorCallbacks.IssueOnSaveCanvas (nodeCanvas);
		}



		// LOAD:

		/// <summary>
		/// Loads the NodeCanvas in the asset file at path
		/// </summary>
		public static NodeCanvas LoadNodeCanvas (string path) 
		{
			if (String.IsNullOrEmpty (path))
				return null;
			path = path.Replace (Application.dataPath, "Assets");
			// Fetch all objects in the save file
			Object[] objects = null;
			if (Application.isPlaying)
			{
				if (path.Contains ("/Resources"))
					path = path.Substring (path.LastIndexOf ("/Resources/") + 11);
				else if (path.Contains ("Resources"))
					path = path.Substring (path.LastIndexOf ("Resources") + 10);
				path = path.Split ('.') [0];
				objects = UnityEngine.Resources.LoadAll (path);
			}
	#if UNITY_EDITOR
			else
				objects = UnityEditor.AssetDatabase.LoadAllAssetsAtPath (path);
	#endif
			if (objects == null || objects.Length == 0) 
				return null;

			// Filter out the NodeCanvas out of these objects
			NodeCanvas nodeCanvas = objects.Single ((Object obj) => (obj as NodeCanvas) != null) as NodeCanvas;
			if (nodeCanvas == null)
				return null;
			
			#if UNITY_EDITOR // Create a working copy of it
			nodeCanvas = GetWorkingCopy (nodeCanvas);
			UnityEditor.AssetDatabase.Refresh ();
			#endif	
			NodeEditorCallbacks.IssueOnLoadCanvas (nodeCanvas);
			return nodeCanvas;
		}

		/// <summary>
		/// Loads the editorStates found in the nodeCanvas asset file at path
		/// </summary>
		public static List<NodeEditorState> LoadEditorStates (string path) 
		{
			if (String.IsNullOrEmpty (path))
				return new List<NodeEditorState> ();
			// Fetch all objects in the save file
	#if UNITY_EDITOR
			Object[] objects = UnityEditor.AssetDatabase.LoadAllAssetsAtPath (path);
	#else
			Object[] objects = UnityEngine.Resources.LoadAll (path);
	#endif
			if (objects.Length == 0) 
				return new List<NodeEditorState> ();
			
			// Obtain the editorStates in that asset file and create a working copy of them
			List<NodeEditorState> editorStates = objects.Where ((Object obj) => (obj as NodeEditorState) != null)
	#if UNITY_EDITOR
				.Select ((Object obj) => GetWorkingCopy (obj as NodeEditorState))
	#else
				.Select ((Object obj) => obj as NodeEditorState)
	#endif
				.ToList ();

			for (int cnt = 0; cnt < editorStates.Count; cnt++) 
				NodeEditorCallbacks.IssueOnLoadEditorState (editorStates[cnt]);
	#if UNITY_EDITOR
			UnityEditor.AssetDatabase.Refresh ();
	#endif
			return editorStates;
		}



		// HELPERS:

		// <summary>
		/// Gets a working copy of the editor state. This will break the link to the asset and thus all changes made to the working copy have to be explicitly saved.
		/// </summary>
		public static NodeEditorState GetWorkingCopy (NodeEditorState editorState) 
		{
			editorState = Clone (editorState);
			editorState.focusedNode = null;
			editorState.selectedNode = null;
			editorState.makeTransition = null;
			editorState.connectOutput = null;
			return editorState;
		}

		/// <summary>
		/// Gets a working copy of the canvas. This will break the link to the canvas asset and thus all changes made to the working copy have to be explicitly saved.
		/// </summary>
		public static NodeCanvas GetWorkingCopy (NodeCanvas nodeCanvas) 
		{
			// In order to break the asset link, we have to clone each scriptable object asset individually.
			// That means, as they are reference types, we need to take care of the references to the object.

			// TODO: Additional ScriptableObjects in Nodes
			// Enable both comments below if any of your Nodes contain additional ScriptableObjects that need to cloned and saved along with the canvas

			nodeCanvas = Clone (nodeCanvas);

			// First, we write each scriptable object into a list, and a cloned copy of it into another list
			List<ScriptableObject> allSOs = new List<ScriptableObject> ();
			List<ScriptableObject> clonedSOs = new List<ScriptableObject> ();
			for (int nodeCnt = 0; nodeCnt < nodeCanvas.nodes.Count; nodeCnt++) 
			{
				Node node = nodeCanvas.nodes [nodeCnt];
				AddClonedSO (allSOs, clonedSOs, node);
				for (int inCnt = 0; inCnt < node.Inputs.Count; inCnt++)
					AddClonedSO (allSOs, clonedSOs, node.Inputs [inCnt]);
				for (int outCnt = 0; outCnt < node.Outputs.Count; outCnt++)
					AddClonedSO (allSOs, clonedSOs, node.Outputs [outCnt]);
				for (int transCnt = 0; transCnt < node.transitions.Count; transCnt++)
					AddClonedSO (allSOs, clonedSOs, node.transitions [transCnt]);

				// Enable if any of your Nodes contain additional ScriptableObjects that need to cloned and saved along with the canvas
//				List<FieldInfo> additionalSOs = GetAllDirectReferences<ScriptableObject> (node);
//				foreach (FieldInfo SOField in additionalSOs) 
//				{
//					ScriptableObject SO = SOField.GetValue (node) as ScriptableObject;
//					if (SO != null && !allSOs.Contains (SO))
//						AddClonedSO (allSOs, clonedSOs, SO);
//				}
			}

			// Then we replace every reference to any of the original SOs in the first list with the cloned ones in the second list
			nodeCanvas.currentNode = ReplaceSO (allSOs, clonedSOs, nodeCanvas.currentNode);

			for (int nodeCnt = 0; nodeCnt < nodeCanvas.nodes.Count; nodeCnt++) 
			{
				Node node = nodeCanvas.nodes [nodeCnt] = ReplaceSO (allSOs, clonedSOs, nodeCanvas.nodes [nodeCnt]);
				for (int inCnt = 0; inCnt < node.Inputs.Count; inCnt++) 
				{
					NodeInput nodeInput = node.Inputs [inCnt] = ReplaceSO (allSOs, clonedSOs, node.Inputs [inCnt]);
					nodeInput.body = node;
					nodeInput.connection = ReplaceSO (allSOs, clonedSOs, nodeInput.connection);
				}
				for (int outCnt = 0; outCnt < node.Outputs.Count; outCnt++)
				{
					NodeOutput nodeOutput = node.Outputs [outCnt] = ReplaceSO (allSOs, clonedSOs, node.Outputs [outCnt]);
					nodeOutput.body = node;
					for (int conCnt = 0; conCnt < nodeOutput.connections.Count; conCnt++) 
						nodeOutput.connections [conCnt] = ReplaceSO (allSOs, clonedSOs, nodeOutput.connections [conCnt]);
				}
				for (int transCnt = 0; transCnt < node.transitions.Count; transCnt++)
				{
					Transition trans = node.transitions [transCnt] = ReplaceSO (allSOs, clonedSOs, node.transitions [transCnt]);
					trans.startNode = ReplaceSO (allSOs, clonedSOs, trans.startNode);
					trans.endNode = ReplaceSO (allSOs, clonedSOs, trans.endNode);
				}

				// Enable if any of your Nodes contain additional ScriptableObjects that need to cloned and saved along with the canvas
//				List<FieldInfo> additionalSOs = GetAllDirectReferences<ScriptableObject> (node);
//				foreach (FieldInfo SOField in additionalSOs) 
//				{
//					ScriptableObject SO = SOField.GetValue (node) as ScriptableObject;
//					if (SO != null) SOField.SetValue (node, ReplaceSO (allSOs, clonedSOs, SO));
//				}
			}

			return nodeCanvas;
		}

	#if UNITY_EDITOR
		// Adds a hidden sub asset to the main asset
		private static void AddSubAsset (ScriptableObject subAsset, ScriptableObject mainAsset) 
		{
			UnityEditor.AssetDatabase.AddObjectToAsset (subAsset, mainAsset);
			subAsset.hideFlags = HideFlags.HideInHierarchy;
		}
	#endif

		// Returns all references to T in obj, using reflection
		private static List<FieldInfo> GetAllDirectReferences<T> (object obj) 
		{
			return obj.GetType ()
					.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
					.Where ((FieldInfo field) => field.FieldType == typeof(T) || field.FieldType.IsSubclassOf (typeof(T)))
					.ToList ();
		}

		// Clones the SO of type T, preserving it's name
		private static T Clone<T> (T SO) where T : ScriptableObject 
		{
			string soName = SO.name;
			SO = Object.Instantiate (SO);
			SO.name = soName;
			return SO;
		}

		// Adds the SO to the first list and a clone of it to the second. For creating working copys
		private static T AddClonedSO<T> (List<ScriptableObject> scriptableObjects, List<ScriptableObject> clonedScriptableObjects, T initialSO) where T : ScriptableObject 
		{
			if (initialSO == null)
				return null;
			scriptableObjects.Add (initialSO);
			clonedScriptableObjects.Add (initialSO = Clone (initialSO));
			return initialSO;
		}

		// Returns the SO in the second List matching to the one in the first list, which is specified
		private static T ReplaceSO<T> (List<ScriptableObject> scriptableObjects, List<ScriptableObject> clonedScriptableObjects, T initialSO) where T : ScriptableObject 
		{
			if (initialSO == null)
				return null;
			int soInd = scriptableObjects.IndexOf (initialSO);
			if (soInd == -1)
				throw new UnityException ("GetWorkingCopy: Scriptable Object not cloned in first run!");
			return (T)clonedScriptableObjects[soInd];
		}

		#endregion
	}
}














