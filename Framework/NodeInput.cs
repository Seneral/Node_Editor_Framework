using UnityEngine;
using System;

public class NodeInput : ScriptableObject
{
	public Node body;
	public Rect rect = new Rect ();
	public NodeOutput connection;
	public string type;

	/// <summary>
	/// Creates a new NodeInput in NodeBody of specified type
	/// </summary>
	public static NodeInput Create (Node NodeBody, string InputName, Type InputType) 
	{
		NodeInput input = NodeInput.CreateInstance (typeof (NodeInput)) as NodeInput;
		input.body = NodeBody;
		input.type = InputType.AssemblyQualifiedName;
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
		                 body.rect.y + labelRect.y,
		                 labelRect.width + labelRect.x,
		                 labelRect.height);
	}
	
	/// <summary>
	/// Get the rect of the knob left to the input
	/// </summary>
	public Rect GetKnob () 
	{
		int knobSize = Node_Editor.editor.knobSize;
		return new Rect (rect.x - knobSize,
		                 rect.y + (rect.height - knobSize) / 2, 
		                 knobSize, knobSize);
	}
}