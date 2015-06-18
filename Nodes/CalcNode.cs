using UnityEngine;
using UnityEditor;
using System.Collections;

[System.Serializable]
public class CalcNode : Node 
{
	public enum CalcType { Add, Substract, Multiply, Divide }
	public CalcType type = CalcType.Add;

	public const string ID = "calcNode";
	public override string GetID { get { return ID; } }

	public float Input1Val = 1f;
	public float Input2Val = 1f;

	public static CalcNode Create (Rect NodeRect) 
	{ // This function has to be registered in Node_Editor.ContextCallback
		CalcNode node = CreateInstance <CalcNode> ();
		
		node.name = "Calc Node";
		node.rect = NodeRect;
		
		NodeInput.Create (node, "Input 1", TypeOf.Float);
		NodeInput.Create (node, "Input 2", TypeOf.Float);
		
		NodeOutput.Create (node, "Output 1", TypeOf.Float);
		
		node.InitBase ();
		return node;
	}

	public override void NodeGUI () 
	{
		GUILayout.BeginHorizontal ();
		GUILayout.BeginVertical ();

		if (Inputs [0].connection != null)
			GUILayout.Label (Inputs [0].name);
		else
			Input1Val = EditorGUILayout.FloatField (Input1Val);
		if (Event.current.type == EventType.Repaint) 
			Inputs [0].SetRect (GUILayoutUtility.GetLastRect ());
		// --
		if (Inputs [1].connection != null)
			GUILayout.Label (Inputs [1].name);
		else
			Input2Val = EditorGUILayout.FloatField (Input2Val);
		if (Event.current.type == EventType.Repaint) 
			Inputs [1].SetRect (GUILayoutUtility.GetLastRect ());

		GUILayout.EndVertical ();
		GUILayout.BeginVertical ();

		Outputs [0].DisplayLayout ();
		// We take that this time, because it has a GuiStyle to aligned to the right :)

		GUILayout.EndVertical ();
		GUILayout.EndHorizontal ();

		type = (CalcType)EditorGUILayout.EnumPopup (new GUIContent ("Calculation Type", "The type of calculation performed on Input 1 and Input 2"), type);

		if (GUI.changed)
			Node_Editor.editor.RecalculateFrom (this);
	}

	public override bool Calculate () 
	{
		if (Inputs [0].connection != null && Inputs [0].connection.value != null) 
			Input1Val = (float)Inputs [0].connection.value;
		if (Inputs [1].connection != null && Inputs [1].connection.value != null) 
			Input2Val = (float)Inputs [1].connection.value;

		switch (type) 
		{
		case CalcType.Add:
			Outputs [0].value = Input1Val + Input2Val;
			break;
		case CalcType.Substract:
			Outputs [0].value = Input1Val - Input2Val;
			break;
		case CalcType.Multiply:
			Outputs [0].value = Input1Val * Input2Val;
			break;
		case CalcType.Divide:
			Outputs [0].value = Input1Val / Input2Val;
			break;
		}

		return true;
	}
}
