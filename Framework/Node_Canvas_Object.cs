using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class Node_Canvas_Object : ScriptableObject 
{ // All necessary things to save placed in the Node Canvas Scriptable Object

	// every node on the canvas
	public List<Node> nodes;

	// the pan offset
	public Vector2 panOffset = new Vector2 ();

	// TODO: Node Editor Feature: Zoom
	// Delete this member, Node_Editor.zoomPos and Node_Editor.zoomPanAdjust to remove zoom feature
	// Then fix every error by simply removing the factors causing the error. No warranty!
	public float zoom = 1; 
	// Zoom Factors: 0.6f, 0.8f, 1.0f, 1.2f, 1.4f, 1.6f, 1.8f, 2.0f,
	// Applied: 1 / zoom -> 2 Zoom In; 5 Zoom Out
}