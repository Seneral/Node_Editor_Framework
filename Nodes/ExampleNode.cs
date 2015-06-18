
using UnityEngine;
using UnityEditor;
using System.Collections;

[System.Serializable]
public class ExampleNode : Node 
{
	public const string ID = "exampleNode";
	public override string GetID { get { return ID; } }

	public static ExampleNode Create (Rect NodeRect) 
	{}

	public override void NodeGUI () 
	{}

	public override bool Calculate () 
	{}
}




//using UnityEngine;
//using UnityEditor;
//using System.Collections;
//
//[System.Serializable]
//public class ExampleNode : Node 
//{
//	public const string ID = "exampleNode";
//	public override string GetID { get { return ID; } }
//	
//	public static ExampleNode Create (Rect NodeRect) 
//	{
//		ExampleNode node = CreateInstance<ExampleNode> ();
//		
//		node.rect = NodeRect;
//		node.name = "Example Node";
//		
//		NodeInput.Create (node, "Value", TypeOf.Float);
//		NodeOutput.Create (node, "Output val", TypeOf.Float);
//		
//		node.InitBase ();
//		return node;
//	}
//	
//	public override void NodeGUI () 
//	{
//		GUILayout.Label ("This is a custom Node!");
//		
//		GUILayout.Label ("Input");
//		if (Event.current.type == EventType.Repaint)
//			Inputs [0].SetRect (GUILayoutUtility.GetLastRect ());
//		
//	}
//	
//	public override bool Calculate () 
//	{
//		if (!allInputsReady ())
//			return false;
//		Outputs [0].value = (float)Inputs [0].connection.value * 5;
//		return true;
//	}
//}
