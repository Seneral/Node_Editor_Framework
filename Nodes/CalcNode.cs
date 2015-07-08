using UnityEngine;
using System.Collections;

[System.Serializable]
[Node (false, "Float/Calculation")]
public class CalcNode : Node 
{
	public enum CalcType { Add, Substract, Multiply, Divide }
	public CalcType type = CalcType.Add;

	public const string ID = "calcNode";
	public override string GetID { get { return ID; } }

	public float Input1Val = 1f;
	public float Input2Val = 1f;

	public override Node Create (Vector2 pos) 
	{
		CalcNode node = CreateInstance <CalcNode> ();
		
		node.name = "Calc Node";
		node.rect = new Rect (pos.x, pos.y, 200, 100);
		
		NodeInput.Create (node, "Input 1", "Float");
		NodeInput.Create (node, "Input 2", "Float");
		
		NodeOutput.Create (node, "Output 1", "Float");

		return node;
	}

	public override void NodeGUI () 
	{
		GUILayout.BeginHorizontal ();
		GUILayout.BeginVertical ();

		if (Inputs [0].connection != null)
			GUILayout.Label (Inputs [0].name);
#if UNITY_EDITOR
		else
			Input1Val = UnityEditor.EditorGUILayout.FloatField (Input1Val);
#endif
		if (Event.current.type == EventType.Repaint) 
			Inputs [0].SetRect (GUILayoutUtility.GetLastRect ());
		// --
		if (Inputs [1].connection != null)
			GUILayout.Label (Inputs [1].name);
#if UNITY_EDITOR
		else
			Input2Val = UnityEditor.EditorGUILayout.FloatField (Input2Val);
#endif
		if (Event.current.type == EventType.Repaint) 
			Inputs [1].SetRect (GUILayoutUtility.GetLastRect ());

		GUILayout.EndVertical ();
		GUILayout.BeginVertical ();

		Outputs [0].DisplayLayout ();
		// We take that this time, because it has a GuiStyle to aligned to the right :)

		GUILayout.EndVertical ();
		GUILayout.EndHorizontal ();

		type = (CalcType)UnityEditor.EditorGUILayout.EnumPopup (new GUIContent ("Calculation Type", "The type of calculation performed on Input 1 and Input 2"), type);

		if (GUI.changed)
			NodeEditor.RecalculateFrom (this);
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
