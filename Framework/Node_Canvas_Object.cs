using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class Node_Canvas_Object : ScriptableObject 
{
	// All necessary things to save placed in the Node Canvas Scriptable Object
	public List<Node> nodes; // All nodes on the canvas. They include the connections themselves
	public Vector2 panOffset = new Vector2 (); // The Scroll offset
	public float zoom = 2; // Zoom Factor; (1-5)/2: One step to zoom in, three to zoom out. Not implemented yet!
}
