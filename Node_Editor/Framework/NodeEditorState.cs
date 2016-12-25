using UnityEngine;
using System;
using System.Collections.Generic;
using NodeEditorFramework;

namespace NodeEditorFramework 
{
	public partial class NodeEditorState : ScriptableObject 
	{ // holds the state of a NodeCanvas inside a NodeEditor
		public NodeCanvas canvas;
		public NodeEditorState parentEditor;

		// Canvas options
		public bool drawing = true; // whether to draw the canvas

		// Selection State
		public Node selectedNode; // selected Node
		[NonSerialized] public Node focusedNode; // Node under mouse
		[NonSerialized] public NodeKnob focusedNodeKnob; // NodeKnob under mouse
		[NonSerialized] public NodeGroup activeGroup; // NodeGroup that is currently interacted with

		// Navigation State
		public Vector2 panOffset = new Vector2 (); // pan offset
		public float zoom = 1; // zoom; Ranges in 0.2er-steps from 0.6-2.0; applied 1/zoom;

		// Current Action
		[NonSerialized] public NodeOutput connectOutput; // connection this output
		[NonSerialized] public bool dragNode; // node dragging
		[NonSerialized] public bool panWindow; // window panning
		[NonSerialized] public bool navigate; // navigation ('N')
		[NonSerialized] public bool resizeGroup; // whether the active group is being resized; if not, it is dragged

		// Temporary variables
		public Vector2 zoomPos { get { return canvasRect.size/2; } } // zoom center in canvas space
		[NonSerialized] public Rect canvasRect; // canvas Rect
		[NonSerialized] public Vector2 zoomPanAdjust; // calculated value to offset elements with when zooming
		[NonSerialized] public List<Rect> ignoreInput = new List<Rect> (); // Rects inside the canvas to ignore input in (nested canvases, fE)

		#region DragHelper

		[NonSerialized] public string dragUserID; // dragging source
		[NonSerialized] public Vector2 dragMouseStart; // drag start position (mouse)
		[NonSerialized] public Vector2 dragObjectStart; // start position of the dragged object
		[NonSerialized] public Vector2 dragOffset; // offset for both node dragging and window panning
		public Vector2 dragObjectPos { get { return dragObjectStart + dragOffset; } } // position of the dragged object

		/// <summary>
		/// Starts a drag operation with the given userID and initial mouse and object position
		/// Returns false when a different user already claims this drag operation
		/// </summary>
		public bool StartDrag (string userID, Vector2 mousePos, Vector2 objectPos) 
		{
			if (!String.IsNullOrEmpty (dragUserID) && dragUserID != userID)
				return false;
			dragUserID = userID;
			dragMouseStart = mousePos;
			dragObjectStart = objectPos;
			dragOffset = Vector2.zero;
			return true;

		}

		/// <summary>
		/// Updates the current drag with the passed new mouse position and returns the drag offset change since the last update 
		/// </summary>
		public Vector2 UpdateDrag (string userID, Vector2 newDragPos)
		{
			if (dragUserID != userID)
				throw new UnityException ("User ID " + userID + " tries to interrupt drag from " + dragUserID);
			Vector2 prevOffset = dragOffset;
			dragOffset = (newDragPos - dragMouseStart) * zoom;
			return dragOffset - prevOffset;
		}

		/// <summary>
		/// Ends the drag of the specified userID
		/// </summary>
		public Vector2 EndDrag (string userID) 
		{
			if (dragUserID != userID)
				throw new UnityException ("User ID " + userID + " tries to end drag from " + dragUserID);
			Vector2 dragPos = dragObjectPos;
			dragUserID = "";
			dragOffset = dragMouseStart = dragObjectStart = Vector2.zero;
			return dragPos;

		}

		#endregion
	}
}