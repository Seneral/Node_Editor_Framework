using UnityEngine;

[HideInInspector]
public class NodeInput : ScriptableObject
{
	[HideInInspector]
	public Node body;
	[HideInInspector]
	public Rect inputRect = new Rect();
	[HideInInspector]
	public NodeOutput connection;
	[HideInInspector]
	public string type;
	public object value;

	/// <summary>
	/// Creates a new NodeInput in NodeBody of specified type
	/// </summary>
	public static NodeInput Create (Node NodeBody, string InputName, string InputType)
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
		inputRect = new Rect (body.rect.x,
		                      body.rect.y + labelRect.y + 20,
		                      labelRect.width + labelRect.x,
		                      labelRect.height);
	}
	
	/// <summary>
	/// Get the rect of the knob left to the input NOT ZOOMED; Used for GUI drawing in scaled areas
	/// </summary>
	public Rect GetGUIKnob () 
	{
		Rect knobRect = new Rect (inputRect);
		knobRect.position += NodeEditor.curEditorState.zoomPanAdjust;
		return TransformInputRect (knobRect);
	}

	/// <summary>
	/// Get the rect of the knob left to the input ZOOMED; Used for input checks; Representative of the actual screen rect
	/// </summary>
	public Rect GetScreenKnob () 
	{
		return NodeEditor.GUIToScreenRect (TransformInputRect (inputRect));
	}

	/// <summary>
	/// Transforms the input rect to the knob format
	/// </summary>
	private Rect TransformInputRect (Rect knobRect)
	{
		float knobSize = (float)NodeEditor.knobSize;
		return new Rect (knobRect.x - knobSize,
		                 knobRect.y + (knobRect.height - knobSize) / 2,
		                 knobSize, knobSize);
	}
}