using UnityEngine;
using System.Collections.Generic;
using NodeEditorFramework;

namespace NodeEditorFramework 
{
	public class NodeEditorState : ScriptableObject 
	{ // The class that holds the state of an NodeCanvas inside a NodeEditor
		public NodeCanvas canvas;
		public NodeEditorState parent;
		public List<NodeEditorState> childs = new List<NodeEditorState> ();

		// Canvas options
		public bool drawing = true; // whether to draw the canvas

		// Selection
		public Node focusedNode; // under mouse
		// Only one of these is ever active:
		private Node _activeNode;
		public Node activeNode
		{
			get { return _activeNode; }
			set { _selectedTransition = null; _activeNode = value; } 
		}
		private Transition _selectedTransition;
		public Transition selectedTransition 
		{
			get { return _selectedTransition; }
			set { _activeNode = null; _selectedTransition = value; } 
		}

		// Active Node State
		public bool dragNode = false;

		// Connections / Transitions
		public Node makeTransition; // make transition from node
		public NodeOutput connectOutput; // connection this output

		// Navigation State
		public bool navigate = false; // navigation ('N')
		public bool panWindow = false; // window panning
		public Vector2 panOffset = new Vector2 (); // pan offset
		public float zoom = 1; // zoom; Ranges in 0.2er-steps from 0.6-2.0; applied 1/zoom;

		// Global variables
		public Rect canvasRect; // canvas Rect
		public Vector2 zoomPos { get { return canvasRect.size/2; } } // zoom center in canvas space
		public Vector2 zoomPanAdjust; // calculated value to offset elements with when zooming
	}
}