using UnityEngine;
using System;
using System.Collections.Generic;
using NodeEditorFramework;

namespace NodeEditorFramework 
{
	public class NodeEditorState : ScriptableObject 
	{ // holds the state of a NodeCanvas inside a NodeEditor
		
		public NodeCanvas canvas;

		[NonSerialized]
		public bool drawing = true; // specifies whether this editor should be drawn

		public Node selectedNode; // selected Node -> explicitly clicked

		// Navigation State
		public Vector2 panOffset = new Vector2 (); // Pan offset value; applied when drawing
		public float zoom = 1; // zoom value; ranges in 0.2er-steps from 0.6 - 2.0

		// Current Action
		[NonSerialized]
		public NodeOutput connectOutput; // connection this output
		[NonSerialized]
		public bool dragNode; // whether the selected node is being dragged
		[NonSerialized]
		public bool panWindow; // whether the window is being panned
		[NonSerialized]
		public bool navigate; // navigation ('N')
		[NonSerialized]
		public bool resizeGroup; // whether the active group is being resized; if not, it is dragged
 
		// Temporary variables
		[NonSerialized]
		public Rect canvasRect; // rect in which the canvas is drawn
		public Vector2 zoomPos { get { return canvasRect.size/2; } } // zoom center in canvas space
		[NonSerialized]
		public Vector2 zoomPanAdjust; // offset value for canvas elements due to zooming
		[NonSerialized]
		public List<Rect> ignoreInput = new List<Rect> (); // rects to ignore input in
		[NonSerialized]
		public Node focusedNode; // focused Node -> under mouse
		[NonSerialized]
		public NodeKnob focusedNodeKnob; // focused NodeKnob -> under mouse
		[NonSerialized]
		public NodeGroup activeGroup; // group that is currently interacted with

		#region DragHelper

		[NonSerialized]
		public string dragUserID; // dragging source
		[NonSerialized]
		public Vector2 dragMouseStart; // drag start position (mouse)
		[NonSerialized]
		public Vector2 dragObjectStart; // start position of the dragged object
		public Vector2 dragObjectPos { get { return dragObjectStart + dragOffset; } } // position of the dragged object
		[NonSerialized]
		public Vector2 dragOffset; // offset for both node dragging and window panning

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
			if (userID != dragUserID)
				throw new UnityException ("User ID " + userID + " tries to interrupt drag from " + dragUserID);
			Vector2 prevOffset = dragOffset;
			dragOffset = (newDragPos - dragMouseStart) * zoom;
			return dragOffset - prevOffset;
		}

		#endregion
	}
}