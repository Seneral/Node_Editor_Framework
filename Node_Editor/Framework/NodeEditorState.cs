using UnityEngine;
using System;
using System.Collections.Generic;
using NodeEditorFramework;

namespace NodeEditorFramework 
{
	public class NodeEditorState : ScriptableObject 
	{ // holds the state of a NodeCanvas inside a NodeEditor
		public NodeCanvas canvas;
		public NodeEditorState parentEditor;

		// Canvas options
		public bool drawing = true; // whether to draw the canvas

		// Selection State
		public Node selectedNode; // selected Node
		[NonSerialized]
		public Node focusedNode; // Node under mouse
		[NonSerialized]
		public NodeKnob focusedNodeKnob; // NodeKnob under mouse

		// Navigation State
		public Vector2 panOffset = new Vector2 (); // pan offset
		public float zoom = 1; // zoom; Ranges in 0.2er-steps from 0.6-2.0; applied 1/zoom;

		// Current Action
		[NonSerialized]
		public NodeOutput connectOutput; // connection this output
		[NonSerialized]
		public bool dragNode; // node dragging
		[NonSerialized]
		public bool panWindow; // window panning
		[NonSerialized]
		public Vector2 dragStart; // start mouse position for both node dragging and window panning
		[NonSerialized]
		public Vector2 dragPos; // start object position for both node dragging and window panning
		[NonSerialized]
		public Vector2 dragOffset; // offset for both node dragging and window panning
		[NonSerialized]
		public bool navigate; // navigation ('N')
		[NonSerialized]
		public NodeGroup draggedGroup; // The group that is currently dragged
 
		// Temporary variables
		[NonSerialized]
		public Rect canvasRect; // canvas Rect
		public Vector2 zoomPos { get { return canvasRect.size/2; } } // zoom center in canvas space
		[NonSerialized]
		public Vector2 zoomPanAdjust; // calculated value to offset elements with when zooming
		[NonSerialized]
		public List<Rect> ignoreInput = new List<Rect> (); // Rects inside the canvas to ignore input in (nested canvases, fE)
	}
}