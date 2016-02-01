//#define NODE_EDITOR_LINE_CONNECTION

using UnityEngine;
using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using NodeEditorFramework.Utilities;

using Object = UnityEngine.Object;

namespace NodeEditorFramework
{
	public static class NodeEditor 
	{
		public static string EditorPath = "Assets/Plugins/Node_Editor/";

		// The NodeCanvas which represents the currently drawn Node Canvas; globally accessed
		public static NodeCanvas CurNodeCanvas;
		public static NodeEditorState CurEditorState;

		private static bool unfocusControls;
		public static Vector2 MousePos;

		public static Action ClientRepaints;
		public static void RepaintClients () 
		{
			if (ClientRepaints != null)
				ClientRepaints ();
		}

		#region Setup

		public static bool Initiated;
		public static bool InitiationError;
		
		public static void CheckInit () 
		{
			if (!Initiated && !InitiationError)
				ReInit (true);
		}

		public static void ReInit (bool guiFunction) 
		{
			CheckEditorPath ();

			// Init Resource system. Can be called anywhere else, too, if it's needed before.
			ResourceManager.Init (EditorPath + "Resources/");
			
			// Init NE GUI. I may throw an error if a texture was not found.	
			if (!NodeEditorGUI.Init (guiFunction)) 
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
			Initiated = true;
		}

		/// <summary>
		/// Checks the editor path and corrects when possible.
		/// </summary>
		public static void CheckEditorPath () 
		{
	#if UNITY_EDITOR
			Object script = UnityEditor.AssetDatabase.LoadAssetAtPath (EditorPath + "Framework/NodeEditor.cs", typeof(Object));
			if (script == null) 
			{
				string[] assets = UnityEditor.AssetDatabase.FindAssets ("NodeEditorCallbackReceiver"); // Something relatively unique
				if (assets.Length != 1) 
				{
					assets = UnityEditor.AssetDatabase.FindAssets ("ConnectionTypes"); // Another try
					if (assets.Length != 1) 
						throw new UnityException ("Node Editor: Not installed in default directory '" + EditorPath + "'! Correct path could not be detected! Please correct the editorPath variable in NodeEditor.cs!");
				}
				
				string correctEditorPath = UnityEditor.AssetDatabase.GUIDToAssetPath (assets[0]);
				int subFolderIndex = correctEditorPath.LastIndexOf ("Framework/");
				if (subFolderIndex == -1)
					throw new UnityException ("Node Editor: Not installed in default directory '" + EditorPath + "'! Correct path could not be detected! Please correct the editorPath variable in NodeEditor.cs!");
				correctEditorPath = correctEditorPath.Substring (0, subFolderIndex);
				
				Debug.LogWarning ("Node Editor: Not installed in default directory '" + EditorPath + "'! " +
				                  "Editor-only automatic detection adjusted the path to " + correctEditorPath + ", but if you plan to use at runtime, please correct the editorPath variable in NodeEditor.cs!");
				EditorPath = correctEditorPath;
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
			if (!editorState.Drawing)
				return;
			CheckInit ();

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
			if (!editorState.Drawing)
				return;

			// Store and restore later on in case of this being a nested Canvas
			NodeCanvas prevNodeCanvas = CurNodeCanvas;
			NodeEditorState prevEditorState = CurEditorState;
			
			CurNodeCanvas = nodeCanvas;
			CurEditorState = editorState;

			if (Event.current.type == EventType.Repaint) 
			{ // Draw Background when Repainting
				GUI.BeginClip (CurEditorState.CanvasRect);
				
				float width = NodeEditorGUI.Background.width / CurEditorState.Zoom;
				float height = NodeEditorGUI.Background.height / CurEditorState.Zoom;
				Vector2 offset = CurEditorState.ZoomPos + CurEditorState.PanOffset/CurEditorState.Zoom;
				offset = new Vector2 (offset.x%width - width, offset.y%height - height);
				int tileX = Mathf.CeilToInt ((CurEditorState.CanvasRect.width + (width - offset.x)) / width);
				int tileY = Mathf.CeilToInt ((CurEditorState.CanvasRect.height + (height - offset.y)) / height);
				
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
				CurEditorState.IgnoreInput = new List<Rect> ();


			// We're using a custom scale method, as default one is messing up clipping rect
			Rect canvasRect = CurEditorState.CanvasRect;
			CurEditorState.ZoomPanAdjust = GUIScaleUtility.BeginScale (ref canvasRect, CurEditorState.ZoomPos, CurEditorState.Zoom, false);
			//GUILayout.Label ("Scaling is Great!"); -> TODO: Test by changing the last bool parameter

			// ---- BEGIN SCALE ----

			// Some features which require drawing (zoomed)
			if (CurEditorState.Navigate) 
			{ // Draw a curve to the origin/active node for orientation purposes
				RTEditorGUI.DrawLine ((CurEditorState.SelectedNode != null? CurEditorState.SelectedNode.Rect.center : CurEditorState.PanOffset) + CurEditorState.ZoomPanAdjust, 
				                      ScreenToGUIPos (MousePos) + CurEditorState.ZoomPos * CurEditorState.Zoom, 
				                      Color.black, null, 3); 
				RepaintClients ();
			}
			if (CurEditorState.ConnectOutput != null)
			{ // Draw the currently drawn connection
				NodeOutput output = CurEditorState.ConnectOutput;
				Vector2 startPos = output.GetGUIKnob ().center;
				Vector2 endPos = ScreenToGUIPos (MousePos) + CurEditorState.ZoomPos * CurEditorState.Zoom;
				Vector2 endDir = output.GetDirection ();
				NodeEditorGUI.DrawConnection (startPos, endDir, endPos, NodeEditorGUI.GetSecondConnectionVector (startPos, endPos, endDir), ConnectionTypes.GetTypeData (output.Type).Col);
				RepaintClients ();
			}
			if (CurEditorState.MakeTransition != null)
			{ // Draw the currently made transition
				RTEditorGUI.DrawLine (CurEditorState.MakeTransition.Rect.center + CurEditorState.ZoomPanAdjust, 
				                      ScreenToGUIPos (MousePos) + CurEditorState.ZoomPos * CurEditorState.Zoom,
				                      Color.grey, null, 3); 
				RepaintClients ();
			}

			// Push the active node at the bottom of the draw order.
			if (Event.current.type == EventType.Layout && CurEditorState.SelectedNode != null)
			{
				CurNodeCanvas.nodes.Remove (CurEditorState.SelectedNode);
				CurNodeCanvas.nodes.Add (CurEditorState.SelectedNode);
			}

			if (Event.current.type == EventType.Repaint)
			{
				// Draw the transitions and connections. Has to be drawn before nodes as transitions originate from node centers
				for (int nodeCnt = 0; nodeCnt < CurNodeCanvas.nodes.Count; nodeCnt++)  
				{
					Node node = CurNodeCanvas.nodes [nodeCnt];
					node.DrawTransitions ();
					node.DrawConnections ();
				}
			}

			// Draw the nodes
			for (int nodeCnt = 0; nodeCnt < CurNodeCanvas.nodes.Count; nodeCnt++)
			{
				Node node = CurNodeCanvas.nodes [nodeCnt];
				node.DrawNode ();
				if (Event.current.type == EventType.Repaint)
					node.DrawKnobs ();
			}

			// ---- END SCALE ----

			// End scaling group
			GUIScaleUtility.EndScale ();
			
			// Check events with less priority than node GUI controls
			LateEvents ();
			
			CurNodeCanvas = prevNodeCanvas;
			CurEditorState = prevEditorState;
		}
		
		#endregion
		
		#region GUI Functions

		/// <summary>
		/// Returns the node at the position in the current canvas spcae. Depends on curEditorState and curNodecanvas
		/// </summary>
		public static Node NodeAtPosition (Vector2 pos)
		{
			return NodeAtPosition (CurEditorState, CurNodeCanvas, pos);
		}
		/// <summary>
		/// Returns the node at the position in specified canvas space.
		/// </summary>
		public static Node NodeAtPosition (NodeEditorState editorState, NodeCanvas nodecanvas, Vector2 pos)
		{	
			if (!editorState.CanvasRect.Contains (pos))
				return null;
			for (int nodeCnt = nodecanvas.nodes.Count-1; nodeCnt >= 0; nodeCnt--) 
			{ // Check from top to bottom because of the render order
				Node node = nodecanvas.nodes [nodeCnt];
				if (CanvasGUIToScreenRect (node.Rect).Contains (pos))
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
			return CanvasGUIToScreenRect (CurEditorState, rect);
		}
		/// <summary>
		/// Transforms the Rect in GUI space into Screen space
		/// </summary>
		public static Rect CanvasGUIToScreenRect (NodeEditorState editorState, Rect rect) 
		{
			rect.position += editorState.ZoomPos;
			if (editorState.ParentEditor != null)
				rect = GUIScaleUtility.ScaleRect (rect, editorState.ZoomPos, new Vector2 (1/(editorState.ParentEditor.Zoom*editorState.Zoom), 1/(editorState.ParentEditor.Zoom*editorState.Zoom)));
			else
				rect = GUIScaleUtility.ScaleRect (rect, editorState.ZoomPos, new Vector2 (1/editorState.Zoom, 1/editorState.Zoom));
			rect.position += editorState.CanvasRect.position;
			return rect;
		}

		/// <summary>
		/// Transforms screen position pos (like mouse pos) to a point in current GUI space
		/// </summary>
		/// 
		public static Vector2 ScreenToGUIPos (Vector2 pos) 
		{
			return ScreenToGUIPos (CurEditorState, pos);
		}
		/// <summary>
		/// Transforms screen position pos (like mouse pos) to a point in specified GUI space
		/// </summary>
		/// 
		public static Vector2 ScreenToGUIPos (NodeEditorState editorState, Vector2 pos) 
		{
			return Vector2.Scale (pos - editorState.ZoomPos - editorState.CanvasRect.position, new Vector2 (editorState.Zoom, editorState.Zoom));
		}
		
		#endregion
		
		#region Input Events

		/// <summary>
		/// Processes input events
		/// </summary>
		public static void InputEvents ()
		{
			Event e = Event.current;
			MousePos = e.mousePosition;

			if (OverlayGUI.HasPopupControl ()) // Give custom popup solution control when it needs to
				return;

			// Check if we are inside the canvas, accounting for ignoring groups
			if (!CurEditorState.CanvasRect.Contains (e.mousePosition))
				return;
			for (int ignoreCnt = 0; ignoreCnt < CurEditorState.IgnoreInput.Count; ignoreCnt++) 
			{
				if (CurEditorState.IgnoreInput [ignoreCnt].Contains (e.mousePosition)) 
					return;
			}

			// Choose focused and selected Node, accounting for focus changes
			CurEditorState.FocusedNode = null;
			if (e.type == EventType.MouseDown || e.type == EventType.MouseUp)
			{
				CurEditorState.FocusedNode = NodeAtPosition (e.mousePosition);
				if (CurEditorState.FocusedNode != CurEditorState.SelectedNode)
					unfocusControls = true;
				if (e.button == 0) 
				{
					CurEditorState.SelectedNode = CurEditorState.FocusedNode;
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
			if (CurEditorState.FocusedNode != null)
				UnityEditor.Selection.activeObject = CurEditorState.FocusedNode;
	#endif

			switch (e.type) 
			{
			case EventType.MouseDown:

				CurEditorState.DragNode = false;
				CurEditorState.PanWindow = false;
				
				if (CurEditorState.FocusedNode != null) 
				{ // A click on a node
					if (e.button == 1)
					{ // Right click -> Node Context Click
						GenericMenu menu = new GenericMenu ();

						menu.AddItem (new GUIContent ("Delete Node"), false, ContextCallback, new NodeEditorMenuCallback ("deleteNode", CurNodeCanvas, CurEditorState));
						menu.AddItem (new GUIContent ("Duplicate Node"), false, ContextCallback, new NodeEditorMenuCallback ("duplicateNode", CurNodeCanvas, CurEditorState));
						if (CurEditorState.FocusedNode.AcceptsTranstitions)
						{
							menu.AddSeparator ("Seperator");
							menu.AddItem (new GUIContent ("Make Transition"), false, ContextCallback, new NodeEditorMenuCallback ("startTransition", CurNodeCanvas, CurEditorState));
						}

						menu.ShowAsContext ();

						e.Use ();
					}
					else if (e.button == 0)
					{
						if (!CanvasGUIToScreenRect (CurEditorState.FocusedNode.Rect).Contains (e.mousePosition))
						{ // Left click at node edges -> Check for clicked connections to edit
							NodeOutput nodeOutput = CurEditorState.FocusedNode.GetOutputAtPos (e.mousePosition);
							if (nodeOutput != null)
							{ // Output Node -> New Connection drawn from this
								CurEditorState.ConnectOutput = nodeOutput;
								e.Use();
							}
							else 
							{ // no output clicked, check input
								NodeInput nodeInput = CurEditorState.FocusedNode.GetInputAtPos (e.mousePosition);
								if (nodeInput != null && nodeInput.Connection != null)
								{ // Input node -> Loose and edit Connection
									CurEditorState.ConnectOutput = nodeInput.Connection;
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
						CurEditorState.PanWindow = true;
						e.delta = Vector2.zero;
					}
					else if (e.button == 1) 
					{ // Right click -> Editor Context Click
						if (CurEditorState.ConnectOutput != null || CurEditorState.MakeTransition != null) 
						{
							GenericMenu menu = new GenericMenu ();

							// Iterate through all compatible nodes
							foreach (Node node in NodeTypes.Nodes.Keys)
							{
								if (CurEditorState.ConnectOutput != null) 
								{
									foreach (var input in node.Inputs)
									{
										if (input.Type == CurEditorState.ConnectOutput.Type)
										{
											menu.AddItem (new GUIContent ("Add " + NodeTypes.Nodes[node].Adress), false, ContextCallback, new NodeEditorMenuCallback (node.GetID, CurNodeCanvas, CurEditorState));
											break;
										}
									}
								}
								else if (CurEditorState.MakeTransition != null && node.AcceptsTranstitions) 
								{
									menu.AddItem (new GUIContent ("Add " + NodeTypes.Nodes[node].Adress), false, ContextCallback, new NodeEditorMenuCallback (node.GetID, CurNodeCanvas, CurEditorState));
								}
							}
							
							menu.ShowAsContext ();
						}
						else 
						{ // Else add every Node avaiable
							GenericMenu menu = new GenericMenu ();
							foreach (Node node in NodeTypes.Nodes.Keys)
								menu.AddItem (new GUIContent ("Add " + NodeTypes.Nodes [node].Adress), false, ContextCallback, new NodeEditorMenuCallback (node.GetID, CurNodeCanvas, CurEditorState));
							menu.ShowAsContext ();
						}
						e.Use ();
					}
				}
				
				break;
				
			case EventType.MouseUp:

				if (CurEditorState.FocusedNode != null) 
				{
					if (CurEditorState.MakeTransition != null)
					{
						Node.CreateTransition (CurEditorState.MakeTransition, CurEditorState.FocusedNode);
					}
					else if (CurEditorState.ConnectOutput != null) 
					{ // Apply a connection if theres a clicked input
						if (!CurEditorState.FocusedNode.Outputs.Contains (CurEditorState.ConnectOutput)) 
						{ // If an input was clicked, it'll will now be connected
							NodeInput clickedInput = CurEditorState.FocusedNode.GetInputAtPos (e.mousePosition);
							if (Node.CanApplyConnection (CurEditorState.ConnectOutput, clickedInput)) 
							{ // If it can connect (type is equals, it does not cause recursion, ...)
								Node.ApplyConnection (CurEditorState.ConnectOutput, clickedInput);
							}
						}
						e.Use ();
					}
				}
				
				CurEditorState.MakeTransition = null;
				CurEditorState.ConnectOutput = null;
				CurEditorState.DragNode = false;
				CurEditorState.PanWindow = false;
				
				break;
				
			case EventType.ScrollWheel:

				CurEditorState.Zoom = (float)Math.Round (Math.Min (2.0f, Math.Max (0.6f, CurEditorState.Zoom + e.delta.y / 15)), 2);
				RepaintClients ();

				break;
				
			case EventType.KeyDown:

				// TODO: Node Editor: Shortcuts
				if (e.keyCode == KeyCode.N) // Start Navigating (curve to origin / active Node)
					CurEditorState.Navigate = true;
				
				if (e.keyCode == KeyCode.LeftControl && CurEditorState.SelectedNode != null) // Snap
					CurEditorState.SelectedNode.Rect.position = new Vector2 (Mathf.RoundToInt ((CurEditorState.SelectedNode.Rect.position.x - CurEditorState.PanOffset.x) / 10) * 10 + CurEditorState.PanOffset.x, 
					                                                         Mathf.RoundToInt ((CurEditorState.SelectedNode.Rect.position.y - CurEditorState.PanOffset.y) / 10) * 10 + CurEditorState.PanOffset.y);
				RepaintClients ();
				
				break;
				
			case EventType.KeyUp:
				
				if (e.keyCode == KeyCode.N) // Stop Navigating
					CurEditorState.Navigate = false;
				RepaintClients ();
				
				break;
			
			case EventType.MouseDrag:

				if (CurEditorState.PanWindow) 
				{ // Scroll everything with the current mouse delta
					CurEditorState.PanOffset += e.delta * CurEditorState.Zoom;
					for (int nodeCnt = 0; nodeCnt < CurNodeCanvas.nodes.Count; nodeCnt++) 
						CurNodeCanvas.nodes [nodeCnt].Rect.position += e.delta * CurEditorState.Zoom;
					e.delta = Vector2.zero;
					RepaintClients ();
				}
				else 
					CurEditorState.PanWindow = false;
				
				if (CurEditorState.DragNode && CurEditorState.SelectedNode != null && GUIUtility.hotControl == 0) 
				{ // Drag the active node with the current mouse delta
					CurEditorState.SelectedNode.Rect.position += e.delta * CurEditorState.Zoom;
					NodeEditorCallbacks.IssueOnMoveNode (CurEditorState.SelectedNode);
					e.delta = Vector2.zero;
					RepaintClients ();
				} 
				else
					CurEditorState.DragNode = false;

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
			if (!CurEditorState.CanvasRect.Contains (e.mousePosition))
				return;
			for (int ignoreCnt = 0; ignoreCnt < CurEditorState.IgnoreInput.Count; ignoreCnt++) 
			{
				if (CurEditorState.IgnoreInput [ignoreCnt].Contains (e.mousePosition)) 
					return;
			}

			if (e.type == EventType.MouseDown && e.button == 0 && CurEditorState.SelectedNode != null && CanvasGUIToScreenRect (CurEditorState.SelectedNode.Rect).Contains (e.mousePosition))
			{ // Left click inside activeNode -> Drag Node
				// Because of hotControl we have to put it after the GUI Functions
				if (GUIUtility.hotControl == 0)
				{ // We didn't clicked on GUI module, so we'll start dragging the node
					CurEditorState.DragNode = true;
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
			CurNodeCanvas = callback.Canvas;
			CurEditorState = callback.Editor;

			switch (callback.Message)
			{
			case "deleteNode":
				if (CurEditorState.FocusedNode != null) 
					CurEditorState.FocusedNode.Delete ();
				break;
				
			case "duplicateNode":
				if (CurEditorState.FocusedNode != null) 
				{
					ContextCallback (new NodeEditorMenuCallback (CurEditorState.FocusedNode.GetID, CurNodeCanvas, CurEditorState));
					Node duplicatedNode = CurNodeCanvas.nodes [CurNodeCanvas.nodes.Count-1];

					CurEditorState.FocusedNode = duplicatedNode;
					CurEditorState.DragNode = true;
					CurEditorState.MakeTransition = null;
					CurEditorState.ConnectOutput = null;
					CurEditorState.PanWindow = false;
				}
				break;

			case "startTransition":
				if (CurEditorState.FocusedNode != null) 
				{
					CurEditorState.MakeTransition = CurEditorState.FocusedNode;
					CurEditorState.ConnectOutput = null;
				}
				CurEditorState.DragNode = false;
				CurEditorState.PanWindow = false;

				break;

			default:
				Vector2 createPos = ScreenToGUIPos (MousePos);

				Node node = NodeTypes.GetDefaultNode (callback.Message);
				if (node == null)
					break;

				node = node.Create (createPos);
				node.InitBase ();
				NodeEditorCallbacks.IssueOnAddNode (node);

				if (CurEditorState.ConnectOutput != null)
				{ // If nodeOutput is defined, link it to the first input of the same type
					foreach (NodeInput input in node.Inputs)
					{
						if (Node.CanApplyConnection (CurEditorState.ConnectOutput, input))
						{ // If it can connect (type is equals, it does not cause recursion, ...)
							Node.ApplyConnection (CurEditorState.ConnectOutput, input);
							break;
						}
					}
				}
				else if (node.AcceptsTranstitions && CurEditorState.MakeTransition != null) 
				{
					Node.CreateTransition (CurEditorState.MakeTransition, node);
				}

				CurEditorState.MakeTransition = null;
				CurEditorState.ConnectOutput = null;
				CurEditorState.DragNode = false;
				CurEditorState.PanWindow = false;

				break;
			}
			RepaintClients ();
		}

		public class NodeEditorMenuCallback
		{
			public string Message;
			public NodeCanvas Canvas;
			public NodeEditorState Editor;

			public NodeEditorMenuCallback (string message, NodeCanvas nodecanvas, NodeEditorState editorState) 
			{
				Message = message;
				Canvas = nodecanvas;
				Editor = editorState;
			}
		}
		
		#endregion
		
		#region Calculation

		// STATE SYSTEM:

		internal static List<NodeCanvas> TransitioningNodeCanvases = new List<NodeCanvas> ();

		public static void BeginTransitioning (NodeCanvas nodeCanvas, Node beginNode) 
		{
			if (!nodeCanvas.nodes.Contains (beginNode)) 
				throw new UnityException ("Node to begin transitioning from has to be associated with the passed NodeEditorState!");

			nodeCanvas.currentNode = beginNode;
			if (!TransitioningNodeCanvases.Contains (nodeCanvas))
				TransitioningNodeCanvases.Add (nodeCanvas);

			Debug.Log ("Beginning transitioning " + nodeCanvas.name + " from Node " + beginNode.name);

		#if UNITY_EDITOR
			UnityEditor.EditorApplication.update -= WaitForTransitions;
			UnityEditor.EditorApplication.update += WaitForTransitions;
		#endif
		}

		public static void StopTransitioning (NodeCanvas nodeCanvas) 
		{
			if (TransitioningNodeCanvases.Contains (nodeCanvas))
				TransitioningNodeCanvases.Remove (nodeCanvas);
			nodeCanvas.currentNode = null;
			Debug.Log ("Stopped transitioning " + nodeCanvas.name + " at Node " + nodeCanvas.currentNode.name);
		}

		private static void WaitForTransitions () 
		{
			for (int cnt = 0; cnt < TransitioningNodeCanvases.Count; cnt++)
			{
				NodeCanvas nodeCanvas = TransitioningNodeCanvases[cnt];
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
			if (node.Transitions.Count == 0)
				return false;
			for (int transCnt = 0; transCnt < node.Transitions.Count; transCnt++) 
			{
				Transition trans = node.Transitions[transCnt];
				if ((trans.EndNode != prevNode || trans.StartNode != prevNode) && trans.ConditionsMet ()) 
				{
					if (trans.StartNode == node)
						nextNode = trans.EndNode;
					else if (trans.EndNode == node)
						nextNode = trans.StartNode;
					else
						Debug.Log ("Node " + node.name + " is not part of the transition " + trans.name);
					break;
				}
			}
			return true;
		}

		// CALCULATION SYSTEM:
		
		// A list of Nodes from which calculation originates -> Call StartCalculation
		public static List<Node> WorkList;
		private static int calculationCount;

		/// <summary>
		/// Recalculate from every Input Node.
		/// Usually does not need to be called at all, the smart calculation system is doing the job just fine
		/// </summary>
		public static void RecalculateAll (NodeCanvas nodeCanvas) 
		{
			WorkList = new List<Node> ();
			foreach (Node node in nodeCanvas.nodes) 
			{
				if (node.IsInput ())
				{ // Add all Inputs
					node.ClearCalculation ();
					WorkList.Add (node);
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
			WorkList = new List<Node> { node };
			StartCalculation ();
		}
		
		/// <summary>
		/// Iterates through workList and calculates everything, including children
		/// </summary>
		public static void StartCalculation () 
		{
			if (WorkList == null || WorkList.Count == 0)
				return;
			calculationCount = 0;
			// this blocks iterates through the worklist and starts calculating
			// if a node returns false state it stops and adds the node to the worklist
			// later on, this worklist is reworked
			bool limitReached = false;
			for (int roundCnt = 0; !limitReached; roundCnt++)
			{ // Runs until every node possible is calculated
				limitReached = true;
				for (int workCnt = 0; workCnt < WorkList.Count; workCnt++) 
				{
					if (ContinueCalculation (WorkList [workCnt]))
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
			if (node.Calculated)
				return false;
			if ((node.DescendantsCalculated () || node.IsInLoop ()) && node.Calculate ())
			{ // finished Calculating, continue with the children
				node.Calculated = true;
				calculationCount++;
				WorkList.Remove (node);
				if (node.ContinueCalculation && calculationCount < 1000) 
				{
					for (int outCnt = 0; outCnt < node.Outputs.Count; outCnt++)
					{
						NodeOutput output = node.Outputs [outCnt];
						for (int conCnt = 0; conCnt < output.Connections.Count; conCnt++)
							ContinueCalculation (output.Connections [conCnt].Body);
					}
				}
				else if (calculationCount >= 1000)
					Debug.LogError ("Stopped calculation because of suspected Recursion. Maximum calculation iteration is currently at 1000!");
				return true;
			}
			else if (!WorkList.Contains (node)) 
			{ // failed to calculate, add it to check later
				WorkList.Add (node);
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
				editorState.Canvas = nodeCanvas;
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
				for (int transCnt = 0; transCnt < node.Transitions.Count; transCnt++)
					AddSubAsset (node.Transitions [transCnt], node);
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
			editorState.FocusedNode = null;
			editorState.SelectedNode = null;
			editorState.MakeTransition = null;
			editorState.ConnectOutput = null;
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
				for (int transCnt = 0; transCnt < node.Transitions.Count; transCnt++)
					AddClonedSO (allSOs, clonedSOs, node.Transitions [transCnt]);

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
					nodeInput.Body = node;
					nodeInput.Connection = ReplaceSO (allSOs, clonedSOs, nodeInput.Connection);
				}
				for (int outCnt = 0; outCnt < node.Outputs.Count; outCnt++)
				{
					NodeOutput nodeOutput = node.Outputs [outCnt] = ReplaceSO (allSOs, clonedSOs, node.Outputs [outCnt]);
					nodeOutput.Body = node;
					for (int conCnt = 0; conCnt < nodeOutput.Connections.Count; conCnt++) 
						nodeOutput.Connections [conCnt] = ReplaceSO (allSOs, clonedSOs, nodeOutput.Connections [conCnt]);
				}
				for (int transCnt = 0; transCnt < node.Transitions.Count; transCnt++)
				{
					Transition trans = node.Transitions [transCnt] = ReplaceSO (allSOs, clonedSOs, node.Transitions [transCnt]);
					trans.StartNode = ReplaceSO (allSOs, clonedSOs, trans.StartNode);
					trans.EndNode = ReplaceSO (allSOs, clonedSOs, trans.EndNode);
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
					.Where (field => field.FieldType == typeof(T) || field.FieldType.IsSubclassOf (typeof(T)))
					.ToList ();
		}

		// Clones the SO of type T, preserving it's name
		private static T Clone<T> (T so) where T : ScriptableObject 
		{
			string soName = so.name;
			so = Object.Instantiate (so);
			so.name = soName;
			return so;
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














