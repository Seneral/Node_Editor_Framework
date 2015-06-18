using UnityEngine;
using System;
using System.Collections.Generic;

public class NodeOutput : ScriptableObject
{
	public Node body;
	public List<NodeInput> connections = new List<NodeInput> ();
	public TypeOf type;
	[NonSerialized]
	public object value = null;

	public Rect rect = new Rect ();

	/// <summary>
	/// Creates a new NodeOutput in NodeBody of specified type
	/// </summary>
	public static NodeOutput Create (Node NodeBody, string OutputName, TypeOf OutputType) 
	{
		NodeOutput output = CreateInstance <NodeOutput> ();
		output.body = NodeBody;
		output.type = OutputType;
		output.name = OutputName;
		NodeBody.Outputs.Add (output);
		return output;
	}

	/// <summary>
	/// Function to automatically draw and update the output with a label for it's name
	/// </summary>
	public void DisplayLayout () 
	{
		DisplayLayout (new GUIContent (name));
	}
	/// <summary>
	/// Function to automatically draw and update the output
	/// </summary>
	public void DisplayLayout (GUIContent content) 
	{
		GUIStyle style = new GUIStyle (UnityEditor.EditorStyles.label);
		style.alignment = TextAnchor.MiddleRight;
		GUILayout.Label (content, style);
		if (Event.current.type == EventType.Repaint) 
			SetRect (GUILayoutUtility.GetLastRect ());
	}
	
	/// <summary>
	/// Set the output rect as labelrect in global canvas space and extend it to the right node edge
	/// </summary>
	public void SetRect (Rect labelRect) 
	{
		rect = new Rect (body.rect.x + labelRect.x, 
		                 body.rect.y + labelRect.y + 20, 
		                 body.rect.width - labelRect.x, 
		                 labelRect.height);
	}
	
	/// <summary>
	/// Get the rect of the knob right to the output NOT ZOOMED; Used for GUI drawing in scaled areas
	/// </summary>
	public Rect GetGUIKnob () 
	{
		Rect knobRect = new Rect (rect);
		knobRect.position += Node_Editor.zoomPanAdjust;
		float knobSize = (float)Node_Editor.knobSize;
		return new Rect (knobRect.x + knobRect.width,
		                 knobRect.y + (knobRect.height - knobSize) / 2,
		                 knobSize, knobSize);
	}

	/// <summary>
	/// Get the rect of the knob right to the output ZOOMED; Used for input checks; Representative of the actual screen rect
	/// </summary>
	public Rect GetScreenKnob () 
	{
		Rect knobRect = GetGUIKnob ();
		knobRect.position = knobRect.position - Node_Editor.zoomPanAdjust + Node_Editor.zoomPos; // Change spaces, as GUIKnob was built for scaled GUI.matrix.
		return Node_Editor.ScaleRect (knobRect, Node_Editor.zoomPos, new Vector2 (1/Node_Editor.nodeCanvas.zoom, 1/Node_Editor.nodeCanvas.zoom));
	}
}