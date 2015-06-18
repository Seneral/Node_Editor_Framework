using UnityEngine;
using System;

public class NodeInput : ScriptableObject
{
	public Node body;
	public NodeOutput connection;
	public TypeOf type;

	public Rect rect = new Rect ();

	/// <summary>
	/// Creates a new NodeInput in NodeBody of specified type
	/// </summary>
	public static NodeInput Create (Node NodeBody, string InputName, TypeOf InputType) 
	{
		NodeInput input = CreateInstance <NodeInput> ();
		input.body = NodeBody;
		input.type = InputType;
		input.name = InputName;
		NodeBody.Inputs.Add (input);
		return input;
	}

	/// <summary>
	/// Function to automatically draw and update the input with a label for it's name
	/// </summary>
	public void DisplayLayout () 
	{
		DisplayLayout (new GUIContent (name));
	}
	/// <summary>
	/// Function to automatically draw and update the input
	/// </summary>
	public void DisplayLayout (GUIContent content) 
	{
		GUIStyle style = new GUIStyle (UnityEditor.EditorStyles.label);
		GUILayout.Label (content, style);
		if (Event.current.type == EventType.Repaint) 
			SetRect (GUILayoutUtility.GetLastRect ());
	}
	
	/// <summary>
	/// Set the input rect as labelrect in global canvas space and extend it to the left node edge
	/// </summary>
	public void SetRect (Rect labelRect) 
	{
		rect = new Rect (body.rect.x,
		                 body.rect.y + labelRect.y + 20, 
		                 labelRect.width + labelRect.x,
		                 labelRect.height);
	}
	
	/// <summary>
	/// Get the rect of the knob left to the input NOT ZOOMED; Used for GUI drawing in scaled areas
	/// </summary>
	public Rect GetGUIKnob () 
	{
		Rect knobRect = new Rect (rect);
		knobRect.position += Node_Editor.zoomPanAdjust;
		float knobSize = (float)Node_Editor.knobSize;
		return new Rect (knobRect.x - knobSize,
		                 knobRect.y + (knobRect.height - knobSize) / 2,
		                 knobSize, knobSize);
	}

	/// <summary>
	/// Get the rect of the knob left to the input ZOOMED; Used for input checks; Representative of the actual screen rect
	/// </summary>
	public Rect GetScreenKnob () 
	{
		Rect knobRect = GetGUIKnob ();
		knobRect.position = knobRect.position - Node_Editor.zoomPanAdjust + Node_Editor.zoomPos; // Change spaces, as GUIKnob was built for scaled GUI.matrix.
		return Node_Editor.ScaleRect (knobRect, Node_Editor.zoomPos, new Vector2 (1/Node_Editor.nodeCanvas.zoom, 1/Node_Editor.nodeCanvas.zoom));
	}
}