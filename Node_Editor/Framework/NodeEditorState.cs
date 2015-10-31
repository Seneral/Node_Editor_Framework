using UnityEngine;
using System.Collections.Generic;
using NodeEditorFramework;

namespace NodeEditorFramework 
{
	public class NodeEditorState : ScriptableObject 
	{ // holds the state of an NodeCanvas inside a NodeEditor
		public NodeCanvas canvas;

		// Canvas options
		public bool drawing = true; // whether to draw the canvas

		// Selection State
		public Node focusedNode; // Node under mouse
		public Node selectedNode; // selected Node
		public Node currentNode; // current node in state system

		// Current Action
		public bool dragNode = false;
		public Node makeTransition; // make transition from node
		public NodeOutput connectOutput; // connection this output

		// Navigation State
		public bool navigate = false; // navigation ('N')
		public bool panWindow = false; // window panning
		public Vector2 panOffset = new Vector2 (); // pan offset
		public float zoom = 1; // zoom; Ranges in 0.2er-steps from 0.6-2.0; applied 1/zoom;

		// Temporary State variables
		public Rect canvasRect; // canvas Rect
		public Vector2 zoomPos { get { return canvasRect.size/2; } } // zoom center in canvas space
		public Vector2 zoomPanAdjust; // calculated value to offset elements with when zooming
		public List<Rect> ignoreInput = new List<Rect> (); // Rects inside the canvas to ignore input in (nested canvases, fE)
	}
}