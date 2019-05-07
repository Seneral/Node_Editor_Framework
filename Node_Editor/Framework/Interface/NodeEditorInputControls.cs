using UnityEngine;
using System;
using System.Collections.Generic;

using NodeEditorFramework.Utilities;

namespace NodeEditorFramework 
{
	/// <summary>
	/// Collection of default Node Editor controls for the NodeEditorInputSystem
	/// </summary>
	public static class NodeEditorInputControls
	{
		#region Canvas Context Entries

		[ContextFillerAttribute (ContextType.Canvas)]
		private static void FillAddNodes (NodeEditorInputInfo inputInfo, GenericMenu canvasContextMenu) 
		{ // Fill context menu with nodes to add to the canvas
			NodeEditorState state = inputInfo.editorState;
			List<string> nodes = NodeTypes.getCompatibleNodes (state.connectKnob);
			foreach (string node in nodes)
			{ // Only add nodes to the context menu that are compatible
				if (NodeCanvasManager.CheckCanvasCompability (node, inputInfo.editorState.canvas.GetType ()) && inputInfo.editorState.canvas.CanAddNode (node))
					canvasContextMenu.AddItem (new GUIContent ("Add " + NodeTypes.getNodeData(node).adress), false, CreateNodeCallback, new NodeEditorInputInfo (node, state));
			}
		}

		private static void CreateNodeCallback (object infoObj)
		{
			NodeEditorInputInfo callback = infoObj as NodeEditorInputInfo;
			if (callback == null)
				throw new UnityException ("Callback Object passed by context is not of type NodeEditorInputInfo!");

			Node.Create (callback.message, NodeEditor.ScreenToCanvasSpace (callback.inputPos), callback.editorState.canvas, callback.editorState.connectKnob);
			callback.editorState.connectKnob = null;
			NodeEditor.RepaintClients ();
		}

		#endregion

		#region Node Context Entries

		[ContextEntryAttribute (ContextType.Node, "Delete Node")]
		private static void DeleteNode (NodeEditorInputInfo inputInfo) 
		{
			if (inputInfo.editorState.focusedNode != null) 
			{
				inputInfo.editorState.focusedNode.Delete ();
				inputInfo.inputEvent.Use ();
			}
		}

		[ContextEntryAttribute (ContextType.Node, "Duplicate Node")]
		private static void DuplicateNode (NodeEditorInputInfo inputInfo) 
		{
			NodeEditorState state = inputInfo.editorState;
			if (state.focusedNode != null && state.canvas.CanAddNode (state.focusedNode.GetID)) 
			{ // Create new node of same type
				Node duplicatedNode = Node.Create (state.focusedNode.GetID, NodeEditor.ScreenToCanvasSpace (inputInfo.inputPos), state.canvas, state.connectKnob);
				state.selectedNode = state.focusedNode = duplicatedNode;
				state.connectKnob = null;
				inputInfo.inputEvent.Use ();
			}
		}

		[HotkeyAttribute(KeyCode.Delete, EventType.KeyUp)]
		private static void DeleteNodeKey(NodeEditorInputInfo inputInfo)
		{
			if (GUIUtility.keyboardControl > 0)
				return;
			if (inputInfo.editorState.focusedNode != null)
			{
				inputInfo.editorState.focusedNode.Delete();
				inputInfo.inputEvent.Use();
			}
		}

		#endregion

		#region Node Keyboard Control

		// Main Keyboard_Move method
		[HotkeyAttribute(KeyCode.UpArrow, EventType.KeyDown)]
		[HotkeyAttribute(KeyCode.LeftArrow, EventType.KeyDown)]
		[HotkeyAttribute(KeyCode.RightArrow, EventType.KeyDown)]
		[HotkeyAttribute(KeyCode.DownArrow, EventType.KeyDown)]
		private static void KB_MoveNode(NodeEditorInputInfo inputInfo)
		{
			if (GUIUtility.keyboardControl > 0)
				return;
			NodeEditorState state = inputInfo.editorState;
			if (state.selectedNode != null)
			{ 
				Vector2 pos = state.selectedNode.rect.position;
				int shiftAmount = inputInfo.inputEvent.shift? 50 : 10;

				if (inputInfo.inputEvent.keyCode == KeyCode.RightArrow)
					pos = new Vector2(pos.x + shiftAmount, pos.y);
				else if (inputInfo.inputEvent.keyCode == KeyCode.LeftArrow)
					pos = new Vector2(pos.x - shiftAmount, pos.y);
				else if (inputInfo.inputEvent.keyCode == KeyCode.DownArrow)
					pos = new Vector2(pos.x, pos.y + shiftAmount);
				else if (inputInfo.inputEvent.keyCode == KeyCode.UpArrow)
					pos = new Vector2(pos.x, pos.y - shiftAmount);

				state.selectedNode.position = pos;
				inputInfo.inputEvent.Use();
			}
			NodeEditor.RepaintClients();

		}


		#endregion

		#region Node Dragging

		[EventHandlerAttribute (EventType.MouseDown, 110)] // Priority over hundred to make it call after the GUI
		private static void HandleNodeDraggingStart (NodeEditorInputInfo inputInfo) 
		{
			if (GUIUtility.hotControl > 0)
				return; // GUI has control

			NodeEditorState state = inputInfo.editorState;
			if (inputInfo.inputEvent.button == 0 && state.focusedNode != null && state.focusedNode == state.selectedNode && state.focusedConnectionKnob == null) 
			{ // Clicked inside the selected Node, so start dragging it
				state.dragNode = true;
				state.StartDrag ("node", inputInfo.inputPos, state.focusedNode.rect.position);
			}
		}

		[EventHandlerAttribute (EventType.MouseDrag)]
		private static void HandleNodeDragging (NodeEditorInputInfo inputInfo) 
		{
			NodeEditorState state = inputInfo.editorState;
			if (state.dragNode) 
			{ // If conditions apply, drag the selected node, else disable dragging
				if (state.selectedNode != null && inputInfo.editorState.dragUserID == "node")
				{ // Apply new position for the dragged node
					state.UpdateDrag ("node", inputInfo.inputPos);
					state.selectedNode.position = state.dragObjectPos;
					NodeEditor.RepaintClients ();
				} 
				else
					state.dragNode = false;
			}
		}

		[EventHandlerAttribute (EventType.MouseDown)]
		[EventHandlerAttribute (EventType.MouseUp)]
		private static void HandleNodeDraggingEnd (NodeEditorInputInfo inputInfo) 
		{
			if (inputInfo.editorState.dragUserID == "node") 
			{
				Vector2 totalDrag = inputInfo.editorState.EndDrag ("node");
				if (inputInfo.editorState.dragNode && inputInfo.editorState.selectedNode != null)
				{
					inputInfo.editorState.selectedNode.position = totalDrag;
					NodeEditorCallbacks.IssueOnMoveNode (inputInfo.editorState.selectedNode);
				}
			}
			inputInfo.editorState.dragNode = false;
		}

		#endregion

		#region Window Panning

		[EventHandlerAttribute (EventType.MouseDown, 105)] // Priority over hundred to make it call after the GUI
		private static void HandleWindowPanningStart (NodeEditorInputInfo inputInfo) 
		{
			if (GUIUtility.hotControl > 0)
				return; // GUI has control

			NodeEditorState state = inputInfo.editorState;
			if ((inputInfo.inputEvent.button == 0 || inputInfo.inputEvent.button == 2) && state.focusedNode == null) 
			{ // Left- or Middle clicked on the empty canvas -> Start panning
				state.panWindow = true;
				state.StartDrag ("window", inputInfo.inputPos, state.panOffset);
			}
		}

		[EventHandlerAttribute (EventType.MouseDrag)]
		private static void HandleWindowPanning (NodeEditorInputInfo inputInfo) 
		{
			NodeEditorState state = inputInfo.editorState;
			if (state.panWindow) 
			{ // Calculate change in panOffset
				if (inputInfo.editorState.dragUserID == "window")
					state.panOffset += state.UpdateDrag ("window", inputInfo.inputPos);
				else
					state.panWindow = false;
				NodeEditor.RepaintClients ();
			}
		}

		[EventHandlerAttribute (EventType.MouseDown)]
		[EventHandlerAttribute (EventType.MouseUp)]
		private static void HandleWindowPanningEnd (NodeEditorInputInfo inputInfo) 
		{
			if (inputInfo.editorState.dragUserID == "window")
				inputInfo.editorState.panOffset = inputInfo.editorState.EndDrag ("window");
			inputInfo.editorState.panWindow = false;
		}

		#endregion

		#region Connection

		[EventHandlerAttribute (EventType.MouseDown)]
		private static void HandleConnectionDrawing (NodeEditorInputInfo inputInfo) 
		{ // TODO: Revamp Multi-Multi knob editing
			NodeEditorState state = inputInfo.editorState;
			if (inputInfo.inputEvent.button == 0 && state.focusedConnectionKnob != null)
			{ // Left-Clicked on a ConnectionKnob, handle editing
				if (state.focusedConnectionKnob.maxConnectionCount == ConnectionCount.Multi)
				{ // Knob with multiple connections clicked -> Draw new connection from it
					state.connectKnob = state.focusedConnectionKnob;
					inputInfo.inputEvent.Use ();
				}
				else if (state.focusedConnectionKnob.maxConnectionCount == ConnectionCount.Single)
				{ // Knob with single connection clicked
					if (state.focusedConnectionKnob.connected())
					{ // Loose and edit existing connection from it
						state.connectKnob = state.focusedConnectionKnob.connection(0);
						state.focusedConnectionKnob.RemoveConnection(state.connectKnob);
						inputInfo.inputEvent.Use();
					}
					else
					{ // Not connected, draw a new connection from it
						state.connectKnob = state.focusedConnectionKnob;
						inputInfo.inputEvent.Use();
					}
				}
			}
		}

		[EventHandlerAttribute (EventType.MouseUp)]
		private static void HandleApplyConnection (NodeEditorInputInfo inputInfo) 
		{
			NodeEditorState state = inputInfo.editorState;
			if (inputInfo.inputEvent.button == 0 && state.connectKnob != null && state.focusedNode != null && state.focusedConnectionKnob != null && state.focusedConnectionKnob != state.connectKnob) 
			{ // A connection curve was dragged and released onto a connection knob
				state.focusedConnectionKnob.TryApplyConnection (state.connectKnob);
				inputInfo.inputEvent.Use ();
			}
			state.connectKnob = null;
		}

		#endregion

		#region Zoom

		[EventHandlerAttribute (EventType.ScrollWheel)]
		private static void HandleZooming (NodeEditorInputInfo inputInfo) 
		{
			inputInfo.editorState.zoom = (float)Math.Round (Math.Min (4.0, Math.Max (0.6, inputInfo.editorState.zoom + inputInfo.inputEvent.delta.y / 15)), 2);
			NodeEditor.RepaintClients ();
		}

		#endregion

		#region Navigation

		[HotkeyAttribute (KeyCode.N, EventType.KeyDown)]
		private static void HandleStartNavigating (NodeEditorInputInfo inputInfo) 
		{
			if (GUIUtility.keyboardControl > 0)
				return;
			inputInfo.editorState.navigate = true;
		}

		[HotkeyAttribute (KeyCode.N, EventType.KeyUp)]
		private static void HandleEndNavigating (NodeEditorInputInfo inputInfo) 
		{
			if (GUIUtility.keyboardControl > 0)
				return;
			inputInfo.editorState.navigate = false;
		}

		#endregion

		#region Node Snap

		[EventHandlerAttribute(EventType.MouseUp, 60)]
		[EventHandlerAttribute(EventType.MouseDown, 60)]
		[EventHandlerAttribute(EventType.MouseDrag, 60)]
		[HotkeyAttribute(KeyCode.LeftControl, EventType.KeyDown , 60)]
		private static void HandleNodeSnap (NodeEditorInputInfo inputInfo) 
		{
			if (inputInfo.inputEvent.modifiers == EventModifiers.Control || inputInfo.inputEvent.keyCode == KeyCode.LeftControl)
			{
				NodeEditorState state = inputInfo.editorState;
				if (state.selectedNode != null)
				{ // Snap selected Node's position to multiples of 10
					state.selectedNode.position.x = Mathf.Round(state.selectedNode.rect.x / 10) * 10;
					state.selectedNode.position.y = Mathf.Round(state.selectedNode.rect.y / 10) * 10;
					NodeEditor.RepaintClients();
				}
				if (state.activeGroup != null)
				{ // Snap active Group's position to multiples of 10
					state.activeGroup.rect.x = Mathf.Round(state.activeGroup.rect.x / 10) * 10;
					state.activeGroup.rect.y = Mathf.Round(state.activeGroup.rect.y / 10) * 10;
					NodeEditor.RepaintClients();
				}
			}
		}

		#endregion

	}
}

