using UnityEngine;
using UnityEditor;
using System.Collections;

[System.Serializable]
public class InputNode : Node 
{
	public const string ID = "inputNode";
	public override string GetID { get { return ID; } }

	public float value = 1f;

	public static InputNode Create (Rect NodeRect) 
	{ // This function has to be registered in Node_Editor.ContextCallback
		InputNode node = CreateInstance <InputNode> ();
		
		node.name = "Input Node";
		node.rect = NodeRect;
		
		NodeOutput.Create (node, "Value", TypeOf.Float);
		
		node.InitBase ();
		return node;
	}

	public override void NodeGUI () 
	{
		value = EditorGUILayout.FloatField (new GUIContent ("Value", "The input value of type float"), value);
		if (Event.current.type == EventType.Repaint) 
			Outputs [0].SetRect (GUILayoutUtility.GetLastRect ());

		if (GUI.changed)
			Node_Editor.editor.RecalculateFrom (this);
	}
	
	public override bool Calculate () 
	{
		Outputs [0].value = value;
		return true;
	}
}