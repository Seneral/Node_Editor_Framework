using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

using NodeEditorFramework;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework
{
	[Serializable]
	public class NodeGroup
	{
		public string title;

		public Rect groupRect;

		private bool edit;

		// Appearance
		public Color color { get { return _color; } set { _color = value; } }
		private Color _color = Color.blue;
		private GUIStyle headerStyle;
		private GUIStyle bodyStyle;
		private GUIStyle headerTitleStyle;
		private GUIStyle headerTitleEditStyle;

		// Functionality
		private List<Node> pinnedNodes = new List<Node> ();

		[ContextEntryAttribute (ContextType.Canvas, "Create Group")]
		public static void CreateGroup (NodeEditorInputInfo info) 
		{
			NodeEditor.curEditorState = info.editorState;
			NodeEditor.curNodeCanvas = info.editorState.canvas;
			NodeGroup newGroup = new NodeGroup ("Group", NodeEditor.ScreenToCanvasSpace (info.inputPos));
			info.editorState.canvas.groups.Add (newGroup);
		}

		private NodeGroup (string groupTitle, Vector2 pos)
		{
			title = groupTitle;
			groupRect = new Rect (pos.x, pos.y, 400, 400);

			GenerateStyles ();
			UpdatePinnedNodes ();

			NodeEditorCallbacks.OnMoveNode += (Node node) => UpdatePinnedNodes ();
		}

		public void Delete () 
		{
			NodeEditor.curNodeCanvas.groups.Remove (this);
		}

		private void GenerateStyles ()
		{
			// Transparent background
			Texture2D background = RTEditorGUI.ColorToTex (8, _color * new Color (1, 1, 1, 0.5f));
			// ligher, less transparent background
			Texture2D altBackground = RTEditorGUI.ColorToTex (8, _color * new Color (2, 2, 2, 0.8f));

			bodyStyle = new GUIStyle ();
			bodyStyle.normal.background = background;

			headerStyle = new GUIStyle ();
			headerStyle.normal.background = altBackground;

			headerTitleStyle = new GUIStyle ();
			headerTitleStyle.fontSize = 22;
			headerTitleStyle.alignment = TextAnchor.MiddleLeft;

			headerTitleEditStyle = new GUIStyle (headerTitleStyle);
			headerTitleEditStyle.normal.background = background;
			headerTitleEditStyle.focused.background = background;
		}

		public void DrawGroup ()
		{
			// Create a rect that is adjusted to the editor zoom
			Rect rect = groupRect;
			rect.position += NodeEditor.curEditorState.zoomPanAdjust + NodeEditor.curEditorState.panOffset;
			int headerHeight = 30;

			Rect headerRect = new Rect (rect.x, rect.y, rect.width, headerHeight);
			GUILayout.BeginArea (headerRect, headerStyle);
			GUILayout.BeginHorizontal ();

			GUILayout.Space (8);

			// Header Title
			if (edit)
				title = GUILayout.TextField (title, headerTitleEditStyle);
			else
				GUILayout.Label (title, headerTitleStyle);
			
			// Header Color Edit
			#if UNITY_EDITOR
			if (edit)
			{
				Color col = UnityEditor.EditorGUILayout.ColorField (_color);
				if (col != _color)
				{
					_color = col;
					GenerateStyles ();
				}
			}
			#endif

			GUILayout.FlexibleSpace ();
			if (GUILayout.Button ("E", new GUILayoutOption [] { GUILayout.ExpandWidth (false), GUILayout.ExpandHeight (false) }))
				edit = !edit;
			
			GUILayout.EndHorizontal ();
			GUILayout.EndArea ();

			// Begin the body frame around the NodeGUI
			Rect bodyRect = new Rect (rect.x, rect.y + headerHeight, rect.width, rect.height - headerHeight);
			GUI.Box (bodyRect, GUIContent.none, bodyStyle);

			// Resize handle
			Rect resizeRect = new Rect (rect.x + rect.width - 20, rect.y + rect.height - 20, 20, 20);
			GUI.Box (resizeRect, "R");
		}

		public void UpdatePinnedNodes ()
		{
			pinnedNodes = new List<Node> ();
			foreach (Node node in NodeEditor.curNodeCanvas.nodes) 
			{
				if (groupRect.Overlaps (node.rect))
					pinnedNodes.Add (node);
			}
		}

		public void Drag (Vector2 moveOffset)
		{
			groupRect.position += moveOffset;
			foreach (Node pinnedNode in pinnedNodes)
				pinnedNode.rect.position += moveOffset;
		}

		#region Input

		private static NodeGroup GroupAtPosition (NodeEditorState state, Vector2 canvasPos)
		{
			if (NodeEditorInputSystem.shouldIgnoreInput (state))
				return null;
			NodeCanvas canvas = state.canvas;
			for (int groupCnt = canvas.groups.Count-1; groupCnt >= 0; groupCnt--) 
			{ // Check from top to bottom because of the render order
				if (canvas.groups [groupCnt].groupRect.Contains (canvasPos)) // Node Body
					return canvas.groups [groupCnt];
			}
			return null;
		}

		[EventHandlerAttribute (EventType.MouseDown, priority = -1)] // Before the other context clicks because they won't account for groups
		private static void HandleGroupContextClick (NodeEditorInputInfo inputInfo) 
		{
			NodeEditorState state = inputInfo.editorState;
			if (inputInfo.inputEvent.button == 1 && state.focusedNode == null)
			{ // Right-click NOT on a node
				NodeGroup focusedGroup = GroupAtPosition (state, NodeEditor.ScreenToCanvasSpace (inputInfo.inputPos)); 
				if (focusedGroup != null)
				{ // Context click for the group. This is static, not dynamic, because it would be useless
					GenericMenu context = new GenericMenu ();
					context.AddItem (new GUIContent ("Delete"), false, () => { NodeEditor.curNodeCanvas = state.canvas; focusedGroup.Delete (); });
					context.ShowAsContext ();
					inputInfo.inputEvent.Use ();
				}
			}
		}

		// No need for selecting or anything other controls nodes need, group simply needs dragging

		[EventHandlerAttribute (EventType.MouseDown, priority = 115)] // Priority over hundred to make it call after the GUI, and after Node dragging
		private static void HandleGroupDraggingStart (NodeEditorInputInfo inputInfo) 
		{
			if (GUIUtility.hotControl > 0)
				return; // GUI has control

			NodeEditorState state = inputInfo.editorState;
			if (inputInfo.inputEvent.button == 0 && state.focusedNode == null && state.dragNode == false) 
			{ // Do not interfere with other dragging stuff
				NodeGroup focusedGroup = GroupAtPosition (state, NodeEditor.ScreenToCanvasSpace (inputInfo.inputPos)); 
				if (focusedGroup != null)
				{ // Start dragging the focused group
					state.draggedGroup = focusedGroup;
					state.dragStart = inputInfo.inputPos;
					state.dragPos = focusedGroup.groupRect.position; // Need this here because of snapping
					state.dragOffset = Vector2.zero;
					inputInfo.inputEvent.delta = Vector2.zero;
					inputInfo.inputEvent.Use ();
				}
			}
		}

		[EventHandlerAttribute (EventType.MouseDrag)]
		private static void HandleGroupDragging (NodeEditorInputInfo inputInfo) 
		{
			NodeEditorState state = inputInfo.editorState;
			if (state.draggedGroup != null) 
			{ // We currently drag a group
				if (state.focusedNode != null || state.dragNode != false)
				{
					state.draggedGroup = null;
					return;
				}
				// Calculate new position for the dragged object
				Vector2 dragOffsetChange = state.dragOffset;
				state.dragOffset = inputInfo.inputPos-state.dragStart;
				dragOffsetChange = (state.dragOffset - dragOffsetChange) * state.zoom;
				state.draggedGroup.Drag (dragOffsetChange);
				inputInfo.inputEvent.Use ();
				NodeEditor.RepaintClients ();

			}
		}

		[EventHandlerAttribute (EventType.MouseDown)]
		[EventHandlerAttribute (EventType.MouseUp)]
		private static void HandleNodeDraggingEnd (NodeEditorInputInfo inputInfo) 
		{
			inputInfo.editorState.draggedGroup = null;
		}

		#endregion
	}
}