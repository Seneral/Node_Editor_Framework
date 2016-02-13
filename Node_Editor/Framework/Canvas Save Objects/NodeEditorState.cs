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

		// Current Action
		[NonSerialized]
		public bool dragNode = false;
		[NonSerialized]
		public Node makeTransition; // make transition from node
		[NonSerialized]
		public NodeOutput connectOutput; // connection this output

		// Navigation State
		public Vector2 panOffset = new Vector2 (); // pan offset
		public float zoom = 1; // zoom; Ranges in 0.2er-steps from 0.6-2.0; applied 1/zoom;

		// Temporary Navigation State
		[NonSerialized]
		public bool navigate = false; // navigation ('N')
		[NonSerialized]
		public bool panWindow = false; // window panning

		// Temporary State
		[NonSerialized]
		public Rect canvasRect; // canvas Rect
		public Vector2 zoomPos { get { return canvasRect.size/2; } } // zoom center in canvas space
		[NonSerialized]
		public Vector2 zoomPanAdjust; // calculated value to offset elements with when zooming
		[NonSerialized]
		public List<Rect> ignoreInput = new List<Rect> (); // Rects inside the canvas to ignore input in (nested canvases, fE)
	}
}