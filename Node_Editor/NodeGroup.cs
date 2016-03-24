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
		public Rect rect;

		private bool edit;

		// Appearance
		public Color color { get { return _color; } set { _color = value; } }
		[SerializeField]
		private Color _color = Color.blue;
		private GUIStyle backgroundStyle;
		private GUIStyle altBackgroundStyle;
		private GUIStyle opBackgroundStyle;
		private GUIStyle headerTitleStyle;
		private GUIStyle headerTitleEditStyle;

		// Only important for active Node
		private static BorderSelection resizeDir;
		private static List<Node> pinnedNodes = new List<Node> ();

		// Settings
		private const int dragAreaWidth = 10;
		private const int minGroupSize = 200;
		private const int headerHeight = 30;

		/// <summary>
		/// Defines which border was selected, including corners by flagging two neighboured sides
		/// </summary>
		[Flags]
		enum BorderSelection { 
			None = 0, 
			Left = 1, 
			Right = 2, 
			Top = 4, 
			Bottom = 8
		};

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
			rect = new Rect (pos.x, pos.y, 400, 400);

			GenerateStyles ();
		}

		public void Delete () 
		{
			NodeEditor.curNodeCanvas.groups.Remove (this);
		}

		public void UpdatePinnedNodes ()
		{
			pinnedNodes = new List<Node> ();
			foreach (Node node in NodeEditor.curNodeCanvas.nodes) 
			{ // Get all pinned nodes -> all nodes atleast half in the group
				if (rect.Contains (node.rect.center))
					pinnedNodes.Add (node);
			}
		}

		#region GUI

		private void GenerateStyles ()
		{
			// Transparent background
			Texture2D background = RTEditorGUI.ColorToTex (8, _color * new Color (1, 1, 1, 0.4f));
			// ligher, less transparent background
			Texture2D altBackground = RTEditorGUI.ColorToTex (8, _color * new Color (1, 1, 1, 0.6f));
			// ligher, less transparent background
			Texture2D opBackground = RTEditorGUI.ColorToTex (8, _color * new Color (1, 1, 1, 1));

			backgroundStyle = new GUIStyle ();
			backgroundStyle.normal.background = background;
			backgroundStyle.padding = new RectOffset (10, 10, 5, 5);

			altBackgroundStyle = new GUIStyle ();
			altBackgroundStyle.normal.background = altBackground;
			altBackgroundStyle.padding = new RectOffset (10, 10, 5, 5);

			opBackgroundStyle = new GUIStyle();
			opBackgroundStyle.normal.background = opBackground;
			opBackgroundStyle.padding = new RectOffset (10, 10, 5, 5);

			headerTitleStyle = new GUIStyle ();
			headerTitleStyle.fontSize = 20;

			headerTitleEditStyle = new GUIStyle (headerTitleStyle);
			headerTitleEditStyle.normal.background = background;
			headerTitleEditStyle.focused.background = background;
		}

		public void DrawGroup ()
		{
			if (backgroundStyle == null)
				GenerateStyles ();
			NodeEditorState state = NodeEditor.curEditorState;
			// Create a rect that is adjusted to the editor zoom
			Rect groupRect = rect;
			groupRect.position += state.zoomPanAdjust + state.panOffset;

			if (state.activeGroup == this && state.resizeGroup)
			{ // TODO: Only show resize handle according to which side is dragged
				Rect handleRect = groupRect;
				if ((NodeGroup.resizeDir&BorderSelection.Left) != 0)
					handleRect.xMax = handleRect.xMin + dragAreaWidth;
				else if ((NodeGroup.resizeDir&BorderSelection.Right) != 0)
					handleRect.xMin = handleRect.xMax - dragAreaWidth;

				if ((NodeGroup.resizeDir&BorderSelection.Top) != 0)
					handleRect.yMax = handleRect.yMin + dragAreaWidth;
				else if ((NodeGroup.resizeDir&BorderSelection.Bottom) != 0)
					handleRect.yMin = handleRect.yMax - dragAreaWidth;
				
				GUI.Box (handleRect, GUIContent.none, opBackgroundStyle);
			}

			// Body
			Rect bodyRect = new Rect (groupRect.x, groupRect.y + headerHeight, groupRect.width, groupRect.height - headerHeight);
			GUI.Box (bodyRect, GUIContent.none, backgroundStyle);

			// Header
			Rect headerRect = new Rect (groupRect.x, groupRect.y, groupRect.width, headerHeight);
			GUILayout.BeginArea (headerRect, altBackgroundStyle);
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
				GUILayout.Space (10);

				Color col = UnityEditor.EditorGUILayout.ColorField (_color);
				if (col != _color)
				{
					_color = col;
					GenerateStyles ();
				}
			}
			#endif

			GUILayout.FlexibleSpace ();

			// Edit Button
			if (GUILayout.Button ("E", new GUILayoutOption [] { GUILayout.ExpandWidth (false), GUILayout.ExpandHeight (false) }))
				edit = !edit;

			GUILayout.EndHorizontal ();
			GUILayout.EndArea ();
		}

		#endregion

		#region Hit tests

		private static NodeGroup HeaderAtPosition(NodeEditorState state, Vector2 canvasPos)
		{
			if (NodeEditorInputSystem.shouldIgnoreInput(state))
				return null;
			NodeCanvas canvas = state.canvas;
			for (int groupCnt = canvas.groups.Count - 1; groupCnt >= 0; groupCnt--)
			{ // Check from top to bottom because of the render order
				NodeGroup group = canvas.groups[groupCnt];
				if (new Rect (group.rect.x, group.rect.y, group.rect.width, 30).Contains(canvasPos))
					return group;
			}
			return null;
		}

		private static NodeGroup GroupAtPosition (NodeEditorState state, Vector2 canvasPos)
		{
			if (NodeEditorInputSystem.shouldIgnoreInput (state))
				return null;
			NodeCanvas canvas = state.canvas;
			for (int groupCnt = canvas.groups.Count-1; groupCnt >= 0; groupCnt--) 
			{ // Check from top to bottom because of the render order
				if (canvas.groups [groupCnt].rect.Contains (canvasPos))
					return canvas.groups [groupCnt];
			}
			return null;
		}

		/// <summary>
		/// Returns true if the mouse position is on the border of the focused node
		/// </summary>
		private static bool CheckBorderSelection(NodeEditorState state, Rect rect, Vector2 canvasPos, out BorderSelection selection)
		{
			selection = 0;
			if (!rect.Contains (canvasPos))
				return false;

			Vector2 min = new Vector2(rect.xMin + 10, rect.yMax - 10);
			Vector2 max = new Vector2(rect.xMax - 10, rect.yMin + 10);

			// Check bordes and mark flags accordingly
			// Horizontal
			if (canvasPos.x < min.x)
				selection = BorderSelection.Left;
			else if (canvasPos.x > max.x)
				selection = BorderSelection.Right;
			// Vertical
			if (canvasPos.y < max.y)
				selection |= BorderSelection.Top;
			else if (canvasPos.y > min.y)
				selection |= BorderSelection.Bottom;

			return selection != BorderSelection.None;
		}

		#endregion

		#region Input

		[EventHandlerAttribute (EventType.MouseDown, priority = -1)] // Before the other context clicks because they won't account for groups
		private static void HandleGroupContextClick (NodeEditorInputInfo inputInfo) 
		{
			NodeEditorState state = inputInfo.editorState;
			if (inputInfo.inputEvent.button == 1 && state.focusedNode == null)
			{ // Right-click NOT on a node
				NodeGroup focusedGroup = HeaderAtPosition (state, NodeEditor.ScreenToCanvasSpace (inputInfo.inputPos)); 
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

		[EventHandlerAttribute (EventType.MouseDown, priority = 104)] // Priority over hundred to make it call after the GUI, and before Node dragging (110) and window panning (105)
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
					Vector2 canvasInputPos = NodeEditor.ScreenToCanvasSpace(inputInfo.inputPos);
					bool resizing = CheckBorderSelection (state, focusedGroup.rect, canvasInputPos, out NodeGroup.resizeDir);
					bool dragging = new Rect (focusedGroup.rect.x, focusedGroup.rect.y, focusedGroup.rect.width, headerHeight).Contains (canvasInputPos);
					if (resizing || dragging)
					{ // Apply either resize or drag
						state.activeGroup = focusedGroup;
						state.StartDrag ("group", inputInfo.inputPos, inputInfo.inputPos);
						if (resizing)
							state.resizeGroup = true;
						else if (dragging)
							state.activeGroup.UpdatePinnedNodes ();
						inputInfo.inputEvent.Use ();
					}
				}
			}
		}

		[EventHandlerAttribute (EventType.MouseDrag)]
		private static void HandleGroupDragging (NodeEditorInputInfo inputInfo) 
		{
			NodeEditorState state = inputInfo.editorState;
			NodeGroup active = state.activeGroup;
			if (active != null) 
			{ // Handle dragging and resizing of active group
				if (state.dragUserID != "group")
				{
					state.activeGroup = null;
					state.resizeGroup = false;
					return;
				}
				// Calculate drag change
				Vector2 drag = state.UpdateDrag ("group", inputInfo.inputPos);
				if (state.resizeGroup)
				{ // Resizing -> Apply drag to selected borders while keeping a minimum size
					// Note: Binary operator and !=0 checks of the flag is enabled, but not necessarily the only flag (in which case you would use ==)
					Rect r = active.rect;
					if ((NodeGroup.resizeDir&BorderSelection.Left) != 0)
						active.rect.xMin = r.xMax - Math.Max (minGroupSize, r.xMax - (r.xMin + drag.x));
					else if ((NodeGroup.resizeDir&BorderSelection.Right) != 0)
						active.rect.xMax = r.xMin + Math.Max (minGroupSize, (r.xMax + drag.x) - r.xMin);

					if ((NodeGroup.resizeDir&BorderSelection.Top) != 0)
						active.rect.yMin = r.yMax - Math.Max (minGroupSize, r.yMax - (r.yMin + drag.y));
					else if ((NodeGroup.resizeDir&BorderSelection.Bottom) != 0)
						active.rect.yMax = r.yMin + Math.Max (minGroupSize, (r.yMax + drag.y) - r.yMin);
				}
				else 
				{ // Dragging -> Apply drag to body and pinned nodes
					active.rect.position += drag;
					foreach (Node pinnedNode in pinnedNodes)
						pinnedNode.rect.position += drag;
				}
				inputInfo.inputEvent.Use ();
				NodeEditor.RepaintClients ();
			}
		}

		[EventHandlerAttribute (EventType.MouseDown)]
		[EventHandlerAttribute (EventType.MouseUp)]
		private static void HandleDraggingEnd (NodeEditorInputInfo inputInfo) 
		{
			if (inputInfo.editorState.activeGroup != null || inputInfo.editorState.dragUserID == "group")
			{
				if (inputInfo.editorState.activeGroup != null )
					inputInfo.editorState.activeGroup.UpdatePinnedNodes ();
				inputInfo.editorState.activeGroup = null;
				inputInfo.editorState.resizeGroup = false;
				inputInfo.editorState.dragUserID = "";
			}

			NodeEditor.RepaintClients();
		}

		#endregion
	}
}