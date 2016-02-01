using UnityEngine;
using System.Collections.Generic;

namespace NodeEditorFramework 
{
	public class NodeEditorState : ScriptableObject 
	{ // holds the state of a NodeCanvas inside a NodeEditor
		public NodeCanvas Canvas;
		public NodeEditorState ParentEditor;

		// Canvas options
		public bool Drawing = true; // whether to draw the canvas

		// Selection State
		public Node FocusedNode; // Node under mouse
		public Node SelectedNode; // selected Node

		// Current Action
		public bool DragNode = false;
		public Node MakeTransition; // make transition from node
		public NodeOutput ConnectOutput; // connection this output

		// Navigation State
		public bool Navigate = false; // navigation ('N')
		public bool PanWindow = false; // window panning
		public Vector2 PanOffset = new Vector2 (); // pan offset
		public float Zoom = 1; // zoom; Ranges in 0.2er-steps from 0.6-2.0; applied 1/zoom;

		// Temporary State variables
		public Rect CanvasRect; // canvas Rect
		public Vector2 ZoomPos { get { return CanvasRect.size/2; } } // zoom center in canvas space
		public Vector2 ZoomPanAdjust; // calculated value to offset elements with when zooming
		public List<Rect> IgnoreInput = new List<Rect> (); // Rects inside the canvas to ignore input in (nested canvases, fE)
	}
}