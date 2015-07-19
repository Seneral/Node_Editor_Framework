using UnityEngine;
using System.Collections.Generic;

public class NodeEditorState : ScriptableObject 
{ // The class that holds the state of an NodeCanvas inside a NodeEditor
	public NodeCanvas canvas;
	public NodeEditorState parent;
	public List<NodeEditorState> childs = new List<NodeEditorState> ();

	public bool drawing = true;

	public Node activeNode; // active Node

	public bool dragNode = false; // whether the active node is dragged
	public NodeOutput connectOutput; // the output, always on the activeNode, which is currently drawn a new connection from
	public Vector2 connectMousePos; // the mouse pos at the time of setting connectOutput

	public bool navigate = false; // navigate ('N') feature
	public bool panWindow = false; // panning the window
	public Vector2 panOffset = new Vector2 (); // the pan offset
	public Vector2 zoomPanAdjust; // The offset to adjust the Node pos with before drawing, related to canvasRect.center and zoom
	public float zoom = 1; // Ranges in 0.2er-steps from 0.6-2.0; applied 1/zoom; TODO: Node Editor Feature: Zoom

	public Rect canvasRect; // the rect this is drawn into
	public Vector2 zoomPos { get { return canvasRect.size/2; } }
}
