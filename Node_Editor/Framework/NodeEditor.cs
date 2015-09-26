//#define NODE_EDITOR_LINE_CONNECTION

using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.IO;
using System.Collections.Generic;
using NodeEditorFramework;

using Object = UnityEngine.Object;

namespace NodeEditorFramework
{
	public static class NodeEditor 
	{
		// The NodeCanvas which represents the currently drawn Node Canvas; globally accessed
		public static NodeCanvas curNodeCanvas;
		public static NodeEditorState curEditorState;

		public static Action Repaint;

		// Quick access
		public static Vector2 mousePos;

		// Settings
		public static int knobSize = 18;

		// Constants
		public const string editorPath = "Assets/Plugins/Node_Editor/";
		public const string resourcePath = "Assets/Plugins/Node_Editor/Resources/";

		#region Setup

		[NonSerialized]
		public static bool initiated = false;
		[NonSerialized]
		public static bool InitiationError = false;
		
		public static void checkInit () 
		{
			if (!initiated && !InitiationError) 
			{
	#if UNITY_EDITOR
				Object script = UnityEditor.AssetDatabase.LoadAssetAtPath (editorPath + "Framework/NodeEditor.cs", typeof(Object));
				if (script == null) 
				{
					Debug.LogError ("Node Editor: Not installed in default directory '" + editorPath + "'! Please modify the editorPath variable in the source!");
					InitiationError = true;
					return;
				}
	#endif

				ConnectionTypes.FetchTypes ();

				NodeTypes.FetchNodes ();

				NodeEditorCallbacks.SetupReceivers ();
				NodeEditorCallbacks.IssueOnEditorStartUp ();

				if (!NodeEditorGUI.Init ())
					InitiationError = true;

				GUIScaleUtility.Init ();

				initiated = true;
			}
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
			
			NodeEditorGUI.StartNodeGUI ();
			
			if (Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint)
				GenericMenu.DrawActive ();
			
			DrawSubCanvas (nodeCanvas, editorState);
			
			if (Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint)
				GenericMenu.DrawActive ();
			
			NodeEditorGUI.EndNodeGUI ();
		}

		/// <summary>
		/// Draws the Node Canvas on the screen in the rect specified by editorState
		/// </summary>
		public static void DrawSubCanvas (NodeCanvas nodeCanvas, NodeEditorState editorState)  
		{
			if (!editorState.drawing)
				return;
			
			NodeCanvas prevNodeCanvas = curNodeCanvas;
			NodeEditorState prevEditorState = curEditorState;
			
			curNodeCanvas = nodeCanvas;
			curEditorState = editorState;
			
			if (Event.current.type == EventType.Repaint) 
			{ // Draw Background when Repainting
				GUI.BeginClip (curEditorState.canvasRect);
				
				float width = NodeEditorGUI.Background.width / curEditorState.zoom;
				float height = NodeEditorGUI.Background.height / curEditorState.zoom;
				Vector2 offset = new Vector2 ((curEditorState.panOffset.x / curEditorState.zoom)%width - width, 
				                              (curEditorState.panOffset.y / curEditorState.zoom)%height - height);
				int tileX = Mathf.CeilToInt ((curEditorState.canvasRect.width + (width - offset.x)) / width);
				int tileY = Mathf.CeilToInt ((curEditorState.canvasRect.height + (height - offset.y)) / height);
				
				for (int x = 0; x < tileX; x++) 
				{
					for (int y = 0; y < tileY; y++) 
					{
						Rect texRect = new Rect (offset.x + x*width, 
						                         offset.y + y*height, 
						                         width, height);
						
						GUI.DrawTexture (texRect, NodeEditorGUI.Background);
					}
				}
				GUI.EndClip ();
			}
			
			// Check the inputs
			InputEvents (curEditorState.ignoreInput);
			curEditorState.ignoreInput = new List<Rect> ();


			// We're using a custom scale methode, as default one is messing up clipping rect
			Rect canvasRect = curEditorState.canvasRect;
			curEditorState.zoomPanAdjust = GUIScaleUtility.BeginScale (ref canvasRect, curEditorState.zoomPos, curEditorState.zoom, true);
			//GUILayout.Label ("Scaling is Great!"); -> Test by changin the last bool parameter

			// ---- BEGIN SCALE ----

			// Some features which require drawing (zoomed)
			if (curEditorState.navigate) 
			{ // Draw a curve to the origin/active node for orientation purposes
				NodeEditorGUI.DrawLine (curEditorState.activeNode != null? curEditorState.activeNode.rect.center + curEditorState.zoomPanAdjust : curEditorState.panOffset, 
				                        ScreenToGUIPos (mousePos) + curEditorState.zoomPos * curEditorState.zoom, 
				                        Color.black, null, 3); 
				if (Repaint != null)
					Repaint ();
			}
			if (curEditorState.connectOutput != null)
			{ // Draw the currently drawn connection
				DrawConnection (curEditorState.connectOutput.GetGUIKnob ().center, 
				                ScreenToGUIPos (mousePos) + curEditorState.zoomPos * curEditorState.zoom, 
				                ConnectionTypes.GetTypeData (curEditorState.connectOutput.type).col);
				if (Repaint != null)
					Repaint ();
			}
			if (curEditorState.makeTransition != null)
			{ // Draw the currently made transition
				NodeEditorGUI.DrawLine (curEditorState.makeTransition.rect.center + curEditorState.zoomPanAdjust, 
				                        ScreenToGUIPos (mousePos) + curEditorState.zoomPos * curEditorState.zoom, 
				                        Color.grey, null, 3); 
				if (Repaint != null)
					Repaint ();
			}
			
			// Draw the transitions. Has to be called before nodes as transitions originate from node centers
			for (int nodeCnt = 0; nodeCnt < curNodeCanvas.nodes.Count; nodeCnt++) 
				curNodeCanvas.nodes [nodeCnt].DrawTransitions ();
			
			// Draw the nodes
			for (int nodeCnt = 0; nodeCnt < curNodeCanvas.nodes.Count; nodeCnt++)
				DrawNode (curNodeCanvas.nodes [nodeCnt]);
			
			// Draw the Node connectors; Seperated because of render order
			for (int nodeCnt = 0; nodeCnt < curNodeCanvas.nodes.Count; nodeCnt++) 
				curNodeCanvas.nodes [nodeCnt].DrawConnections ();
			for (int nodeCnt = 0; nodeCnt < curNodeCanvas.nodes.Count; nodeCnt++) 
				curNodeCanvas.nodes [nodeCnt].DrawKnobs ();
			
			// ---- END SCALE ----

			// End scaling group
			GUIScaleUtility.EndScale ();
			
			// Check events with less priority than node GUI controls
			LateEvents (curEditorState.ignoreInput);
			
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
			
			// Check if we clicked inside a window (or knobSize pixels left or right of it at outputs, for faster knob recognition)
			float KnobSize = (float)NodeEditor.knobSize/editorState.zoom;
			if (editorState.activeNode != null) 
			{ // active Node is drawn ontop, so we check it first
				Rect NodeRect = new Rect (GUIToScreenRect (editorState.activeNode.rect));
				NodeRect = new Rect (NodeRect.x - KnobSize, NodeRect.y, NodeRect.width + KnobSize*2, NodeRect.height);
				if (NodeRect.Contains (pos))
					return editorState.activeNode;
			}
			for (int nodeCnt = nodecanvas.nodes.Count-1; nodeCnt >= 0; nodeCnt--) 
			{ // checked from top to bottom because of the render order
				Rect NodeRect = new Rect (GUIToScreenRect (nodecanvas.nodes [nodeCnt].rect));
				NodeRect = new Rect (NodeRect.x - KnobSize, NodeRect.y, NodeRect.width + KnobSize*2, NodeRect.height);
				if (NodeRect.Contains (pos))
					return nodecanvas.nodes [nodeCnt];
			}
			return null;
		}

		/// <summary>
		/// Draws the node. Depends on curEditorState
		/// </summary>
		public static void DrawNode (Node node)
		{
			// TODO: Node Editor Feature: Custom Windowing System
			Rect nodeRect = node.rect;
			nodeRect.position += curEditorState.zoomPanAdjust;
			float headerHeight = 20;
			Rect headerRect = new Rect (nodeRect.x, nodeRect.y, nodeRect.width, headerHeight);
			Rect bodyRect = new Rect (nodeRect.x, nodeRect.y + headerHeight, nodeRect.width, nodeRect.height - headerHeight);
			
			GUIStyle headerStyle = new GUIStyle (GUI.skin.box);
			if (curEditorState.activeNode == node)
				headerStyle.fontStyle = FontStyle.Bold;
			GUI.Label (headerRect, new GUIContent (node.name), headerStyle);
			GUI.changed = false;
			GUILayout.BeginArea (bodyRect, GUI.skin.box);
			node.NodeGUI ();

//			if (GUI.changed) 
//			{
//				Vector2 size = GUIClipHierarchy.GetSizeOfCurrentGroupContent ();
//				if (size != Vector2.zero)
//					node.rect = new Rect (node.rect.x, node.rect.y, size.x, size.y+headerHeight);
//			}
			GUILayout.EndArea ();
		}

		/// <summary>
		/// Draws a node connection from start to end
		/// </summary>
		public static void DrawConnection (Vector2 startPos, Vector2 endPos, Color col) 
		{
#if NODE_EDITOR_LINE_CONNECTION
			DrawLine (startPos, endPos, col * Color.gray, null, 3);
#else
			NodeEditorGUI.DrawBezier (startPos, endPos, startPos + Vector2.right * 50, endPos + Vector2.right * -50, col * Color.gray, null, 3);
#endif
		}

		/// <summary>
		/// Transforms the Rect in GUI space into Screen space. Depends on curEditorState
		/// </summary>
		public static Rect GUIToScreenRect (Rect rect) 
		{
			return GUIToScreenRect (curEditorState, rect);
		}
		/// <summary>
		/// Transforms the Rect in GUI space into Screen space
		/// </summary>
		public static Rect GUIToScreenRect (NodeEditorState editorState, Rect rect) 
		{
			rect.position += editorState.zoomPos;
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
		public static void InputEvents (List<Rect> ignoreInput)
		{
			Event e = Event.current;
			mousePos = e.mousePosition;

			if (GenericMenu.current != null)
				return;

			bool insideCanvas = curEditorState.canvasRect.Contains (e.mousePosition);
			for (int ignoreCnt = 0; ignoreCnt < ignoreInput.Count; ignoreCnt++) 
			{
				if (ignoreInput [ignoreCnt].Contains (e.mousePosition)) 
				{
					insideCanvas = false;
					break;
				}
			}

			if (!insideCanvas)
				return;
			
			curEditorState.focusedNode = null;
			if (insideCanvas && (e.type == EventType.MouseDown || e.type == EventType.MouseUp))
			{
				curEditorState.focusedNode = NodeEditor.NodeAtPosition (e.mousePosition);
				if (e.button == 0) 
				{
					curEditorState.activeNode = curEditorState.focusedNode;
					if (Repaint != null)
						Repaint ();
				}
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
						// TODO: Node Editor: Editor-Independancy - GenericMenu conversion
//	#if UNITY_EDITOR
//						UnityEditor.GenericMenu menu = new UnityEditor.GenericMenu ();
//	#else
						GenericMenu menu = new GenericMenu ();
//	#endif		
						menu.AddItem (new GUIContent ("Delete Node"), false, ContextCallback, new callbackObject ("deleteNode", curNodeCanvas, curEditorState));
						menu.AddItem (new GUIContent ("Duplicate Node"), false, ContextCallback, new callbackObject ("duplicateNode", curNodeCanvas, curEditorState));
						if (NodeTypes.getNodeData (curEditorState.focusedNode).transitions)
						{
							menu.AddSeparator ("Seperator");
							menu.AddItem (new GUIContent ("Make Transition"), false, ContextCallback, new callbackObject ("startTransition", curNodeCanvas, curEditorState));
						}

						menu.ShowAsContext ();

						e.Use ();
					}
					else if (e.button == 0)
					{
						if (!GUIToScreenRect (curEditorState.focusedNode.rect).Contains (e.mousePosition))
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
						// TODO: Node Editor: Editor-Independancy - GenericMenu conversion
						if (curEditorState.connectOutput != null || curEditorState.makeTransition != null) 
						{
//							#if UNITY_EDITOR
//							UnityEditor.GenericMenu menu = new UnityEditor.GenericMenu ();
//							#else
							GenericMenu menu = new GenericMenu ();
//							#endif	

							// Iterate through all compatible nodes
							foreach (Node node in NodeTypes.nodes.Keys)
							{
								if (curEditorState.connectOutput != null) 
								{
									foreach (var input in node.Inputs)
									{
										if (input.type == curEditorState.connectOutput.type)
										{
											menu.AddItem (new GUIContent ("Add " + NodeTypes.nodes[node].adress), false, ContextCallback, new callbackObject (node.GetID, curNodeCanvas, curEditorState));
											break;
										}
									}
								}
								else if (curEditorState.makeTransition != null && NodeTypes.nodes [node].transitions) 
								{
									menu.AddItem (new GUIContent ("Add " + NodeTypes.nodes[node].adress), false, ContextCallback, new callbackObject (node.GetID, curNodeCanvas, curEditorState));
								}
							}
							
							menu.ShowAsContext ();
						}
						else 
						{
//							#if UNITY_EDITOR
//							UnityEditor.GenericMenu menu = new UnityEditor.GenericMenu ();
//							#else
							GenericMenu menu = new GenericMenu ();
//							#endif		

							foreach (Node node in NodeTypes.nodes.Keys) 
							{
								menu.AddItem (new GUIContent ("Add " + NodeTypes.nodes [node].adress), false, ContextCallback, new callbackObject (node.GetID, curNodeCanvas, curEditorState));
							}
							
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
						e.Use();
					}
				}
				
				curEditorState.makeTransition = null;
				curEditorState.connectOutput = null;
				curEditorState.dragNode = false;
				curEditorState.panWindow = false;
				
				break;
				
			case EventType.ScrollWheel:

				curEditorState.zoom = Mathf.Min (2.0f, Mathf.Max (0.6f, curEditorState.zoom + e.delta.y / 15));
				if (Repaint != null)
					Repaint ();

				break;
				
			case EventType.KeyDown:

				// TODO: Node Editor: Shortcuts
				if (e.keyCode == KeyCode.N) // Start Navigating (curve to origin / active Node)
					curEditorState.navigate = true;
				
				if (e.keyCode == KeyCode.LeftControl && curEditorState.activeNode != null) // Snap
					curEditorState.activeNode.rect.position = new Vector2 (Mathf.RoundToInt ((curEditorState.activeNode.rect.position.x - curEditorState.panOffset.x) / 10) * 10 + curEditorState.panOffset.x, 
					                                                       Mathf.RoundToInt ((curEditorState.activeNode.rect.position.y - curEditorState.panOffset.y) / 10) * 10 + curEditorState.panOffset.y);
				if (Repaint != null)
					Repaint ();
				
				break;
				
			case EventType.KeyUp:
				
				if (e.keyCode == KeyCode.N) // Stop Navigating
					curEditorState.navigate = false;
				
				if (Repaint != null)
					Repaint ();
				
				break;
			
			case EventType.MouseDrag:

				if (curEditorState.panWindow) 
				{ // Scroll everything with the current mouse delta
					curEditorState.panOffset += e.delta * curEditorState.zoom;
					for (int nodeCnt = 0; nodeCnt < curNodeCanvas.nodes.Count; nodeCnt++) 
						curNodeCanvas.nodes [nodeCnt].rect.position += e.delta * curEditorState.zoom;
					e.delta = Vector2.zero;
					if (Repaint != null)
						Repaint ();
				}
				else 
					curEditorState.panWindow = false;
				
				if (curEditorState.dragNode && curEditorState.activeNode != null && GUIUtility.hotControl == 0) 
				{ // Drag the active node with the current mouse delta
					curEditorState.activeNode.rect.position += e.delta * curEditorState.zoom;
					NodeEditorCallbacks.IssueOnMoveNode (curEditorState.activeNode);
					e.delta = Vector2.zero;
					if (Repaint != null)
						Repaint ();
				} 
				else
					curEditorState.dragNode = false;

				break;
			}
		}
		
		/// <summary>
		/// Proccesses late events. Called after GUI Functions, when they have higher priority in focus
		/// </summary>
		public static void LateEvents (List<Rect> ignoreInput) 
		{
			Event e = Event.current;

			bool insideCanvas = curEditorState.canvasRect.Contains (e.mousePosition);
			for (int ignoreCnt = 0; ignoreCnt < ignoreInput.Count; ignoreCnt++) 
			{
				if (ignoreInput [ignoreCnt].Contains (e.mousePosition)) 
				{
					insideCanvas = false;
					break;
				}
			}

			if (e.type == EventType.MouseDown && e.button == 0 && 
			    curEditorState.activeNode != null && insideCanvas && GUIToScreenRect (curEditorState.activeNode.rect).Contains (e.mousePosition))
			{ // Left click inside activeNode -> Drag Node
				// Because of hotControl we have to put it after the GUI Functions
				if (GUIUtility.hotControl == 0)
				{ // We didn't clicked on GUI module, so we'll start dragging the node
					curEditorState.dragNode = true;
					// Because this is the delta from when it was last checked, we have to reset it each time
					e.delta = Vector2.zero;
					if (Repaint != null)
						Repaint ();
				}
			}
		}

		/// <summary>
		/// Context Click selection. Here you'll need to register your own using a string identifier
		/// </summary>
		public static void ContextCallback (object obj)
		{
			callbackObject cbObj = obj as callbackObject;
			curNodeCanvas = cbObj.canvas;
			curEditorState = cbObj.editor;

			switch (cbObj.message)
			{
			case "deleteNode":
				if (curEditorState.focusedNode != null) 
					curEditorState.focusedNode.Delete ();
				break;
				
			case "duplicateNode":
				if (curEditorState.focusedNode != null) 
				{
					ContextCallback (new callbackObject (curEditorState.focusedNode.GetID, curNodeCanvas, curEditorState));
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

				Node node = NodeTypes.getDefaultNode (cbObj.message);
				if (node == null)
					break;

				bool acceptTransitions = NodeTypes.nodes [node].transitions;

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
				else if (acceptTransitions && curEditorState.makeTransition != null) 
				{
					Node.CreateTransition (curEditorState.makeTransition, node);
				}

				curEditorState.makeTransition = null;
				curEditorState.connectOutput = null;
				curEditorState.dragNode = false;
				curEditorState.panWindow = false;

				break;
			}

			if (NodeEditor.Repaint != null)
				NodeEditor.Repaint ();
		}

		public class callbackObject 
		{
			public string message;
			public NodeCanvas canvas;
			public NodeEditorState editor;

			public callbackObject (string Message, NodeCanvas nodecanvas, NodeEditorState editorState) 
			{
				message = Message;
				canvas = nodecanvas;
				editor = editorState;
			}
		}
		
		#endregion
		
		#region Calculation

		public static Node MoveNext (Node node) 
		{
			if (NodeTypes.getNodeData (node).transitions == false)
			{
				Debug.LogError ("Node " + node.ToString () + " does not accept Transitions!");
				return null;
			}

			for (int transCnt = 0; transCnt < node.transitions.Count; transCnt++) 
			{
				if (node.transitions[transCnt].conditionsMet ())
					return node.transitions[transCnt].endNode;
			}

			return node;
		}

		
		// A list of Nodes from which calculation originates -> Call StartCalculation
		public static List<Node> workList;
		
		/// <summary>
		/// Recalculate from every Input Node.
		/// Usually does not need to be called at all, the smart calculation system is doing the job just fine
		/// </summary>
		public static void RecalculateAll (NodeCanvas nodeCanvas) 
		{
			RecalculateAll (nodeCanvas, true);
		}

		/// <summary>
		/// Recalculate from every Input Node.
		/// Usually does not need to be called at all, the smart calculation system is doing the job just fine.
		/// Option to not recalculate the inputs, when they are already set manually
		/// </summary>
		public static void RecalculateAll (NodeCanvas nodeCanvas, bool calculateInputs) 
		{
			workList = new List<Node> ();
			foreach (Node node in nodeCanvas.nodes) 
			{
				if (node.Inputs.Count == 0) 
				{ // Add all Inputs
					if (calculateInputs)
					{
						ClearCalculation (node);
						workList.Add (node);
					}
					else 
					{
						foreach (NodeOutput output in node.Outputs) 
						{
							for (int conCnt = 0; conCnt < output.connections.Count; conCnt++) 
							{
								ClearCalculation (output.connections [conCnt].body);
								workList.Add (output.connections [conCnt].body);
							}
						}
					}
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
			ClearCalculation (node);
			workList = new List<Node> { node };
			StartCalculation ();
		}
		
		/// <summary>
		/// Iterates through workList and calculates everything, including children
		/// </summary>
		public static void StartCalculation () 
		{
			// this blocks iterates through the worklist and starts calculating
			// if a node returns false state it stops and adds the node to the worklist
			// later on, this worklist is reworked
			bool limitReached = false;
			for (int roundCnt = 0; !limitReached; roundCnt++)
			{ // Runs until every node possible is calculated
				limitReached = true;
				for (int workCnt = 0; workCnt < workList.Count; workCnt++) 
				{
					Node node = workList [workCnt];
					if (ContinueCalculation (node))
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
			if (node.descendantsCalculated () && node.Calculate ())
			{ // finished Calculating, continue with the children
				for (int outCnt = 0; outCnt < node.Outputs.Count; outCnt++)
				{
					NodeOutput output = node.Outputs [outCnt];
					for (int conCnt = 0; conCnt < output.connections.Count; conCnt++)
						ContinueCalculation (output.connections [conCnt].body);
				}
				workList.Remove (node);
				node.calculated = true;
				return true;
			}
			else if (!workList.Contains (node)) 
			{ // failed to calculate, add it to check later
				workList.Add (node);
			}
			return false;
		}
		
		/// <summary>
		/// A recursive function to clear all calculations depending on this node.
		/// Usually does not need to be called manually
		/// </summary>
		public static void ClearCalculation (Node node) 
		{
			node.calculated = false;
			for (int outCnt = 0; outCnt < node.Outputs.Count; outCnt++)
			{
				NodeOutput output = node.Outputs [outCnt];
				for (int conCnt = 0; conCnt < output.connections.Count; conCnt++)
					ClearCalculation (output.connections [conCnt].body);
			}
		}
		
		#endregion

		#region Save/Load

		/// <summary>
		/// Loads the editorStates found in the nodeCanvas asset file at path
		/// </summary>
		public static List<NodeEditorState> LoadEditorStates (string path) 
		{
			if (String.IsNullOrEmpty (path))
				return new List<NodeEditorState> ();
	#if UNITY_EDITOR
			Object[] objects = UnityEditor.AssetDatabase.LoadAllAssetsAtPath (path);
	#else
			Object[] objects = Resources.LoadAll (path);
	#endif
			if (objects.Length == 0) 
				return new List<NodeEditorState> ();
			
			// Obtain the editorStates in that asset file
			List<NodeEditorState> editorStates = new List<NodeEditorState> ();
			for (int cnt = 0; cnt < objects.Length; cnt++) 
			{
				if (objects [cnt].GetType () == typeof (NodeEditorState)) 
				{
					editorStates.Add (objects [cnt] as NodeEditorState);
					NodeEditorCallbacks.IssueOnLoadEditorState (editorStates[editorStates.Count-1]);
				}
			}

	#if UNITY_EDITOR
			UnityEditor.AssetDatabase.Refresh ();
	#endif

			return editorStates;
		}

		/// <summary>
		/// Saves the current node canvas as a new asset and links optional editorStates with it
		/// </summary>
		public static void SaveNodeCanvas (NodeCanvas nodeCanvas, string path, params NodeEditorState[] editorStates) 
		{
			if (String.IsNullOrEmpty (path))
				return;
	#if UNITY_EDITOR
			string existingPath = UnityEditor.AssetDatabase.GetAssetPath (nodeCanvas);
			if (!String.IsNullOrEmpty (existingPath))
			{
				if (existingPath != path) 
				{
					UnityEditor.AssetDatabase.CopyAsset (existingPath, path);
					UnityEditor.AssetDatabase.SaveAssets ();
					UnityEditor.AssetDatabase.Refresh ();
				}
				return;
			}
			UnityEditor.AssetDatabase.CreateAsset (nodeCanvas, path);
			foreach (NodeEditorState editorState in editorStates)
				UnityEditor.AssetDatabase.AddObjectToAsset (editorState, nodeCanvas);
			for (int nodeCnt = 0; nodeCnt < nodeCanvas.nodes.Count; nodeCnt++) 
			{ // Add every node and every of it's inputs/outputs into the file. 
				// Results in a big mess but there's no other way (like a hierarchy)
				Node node = nodeCanvas.nodes [nodeCnt];
				UnityEditor.AssetDatabase.AddObjectToAsset (node, nodeCanvas);
				for (int inCnt = 0; inCnt < node.Inputs.Count; inCnt++) 
					UnityEditor.AssetDatabase.AddObjectToAsset (node.Inputs [inCnt], node);
				for (int outCnt = 0; outCnt < node.Outputs.Count; outCnt++) 
					UnityEditor.AssetDatabase.AddObjectToAsset (node.Outputs [outCnt], node);
				for (int transCnt = 0; transCnt < node.transitions.Count; transCnt++) 
					UnityEditor.AssetDatabase.AddObjectToAsset (node.transitions [transCnt], node);
			}
			UnityEditor.AssetDatabase.SaveAssets ();
			UnityEditor.AssetDatabase.Refresh ();
	#else
			// TODO: Node Editor: Need to implement ingame-saving (Resources, AsssetBundles, ... won't work)
	#endif
			NodeEditorCallbacks.IssueOnSaveCanvas (nodeCanvas);
		}
		
		/// <summary>
		/// Loads the NodeCanvas in the asset file at path
		/// </summary>
		public static NodeCanvas LoadNodeCanvas (string path) 
		{
			if (String.IsNullOrEmpty (path))
				return null;
	#if UNITY_EDITOR
			Object[] objects = UnityEditor.AssetDatabase.LoadAllAssetsAtPath (path);
	#else
			Object[] objects = Resources.LoadAll (path);
	#endif
			if (objects.Length == 0) 
				return null;
			NodeCanvas nodeCanvas = null;
			
			for (int cnt = 0; cnt < objects.Length; cnt++) 
			{ // We only have to search for the NodeCanvas itself in the mess, because it still holds references to all of it's nodes and their connections
				if (objects [cnt] && objects [cnt].GetType () == typeof (NodeCanvas)) 
					nodeCanvas = objects [cnt] as NodeCanvas;
			}
			if (nodeCanvas == null)
				return null;
	#if UNITY_EDITOR
			UnityEditor.AssetDatabase.Refresh ();
	#endif	
			NodeEditorCallbacks.IssueOnLoadCanvas (nodeCanvas);
			return nodeCanvas;
		}

		#endregion
	}
}











