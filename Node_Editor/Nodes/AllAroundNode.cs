using UnityEngine;
using NodeEditorFramework;

[Node (false, "AllAround Node", false)]
public class AllAroundNode : Node 
{
	public const string ID = "allaroundNode";
	public override string GetID { get { return ID; } }
	public override bool AllowRecursion { get { return true; } }
	public override bool ContinueCalculation { get { return true; } }

	public override Node Create (Vector2 pos) 
	{
		AllAroundNode node = CreateInstance<AllAroundNode> ();
		
		node.rect = new Rect (pos.x, pos.y, 60, 60);
		node.name = "AllAround Node";
		
		node.CreateInput ("Input Top", "Float");
		node.CreateInput ("Input Bottom", "Float");
		node.CreateInput ("Input Right", "Float");
		node.CreateInput ("Input Left", "Float");
		
		node.CreateOutput ("Output Top", "Float");
		node.CreateOutput ("Output Bottom", "Float");
		node.CreateOutput ("Output Right", "Float");
		node.CreateOutput ("Output Left", "Float");
		
		return node;
	}
	
	public override void DrawNode () 
	{
		Rect nodeRect = rect;
		nodeRect.position += NodeEditor.curEditorState.zoomPanAdjust;
		
		Rect bodyRect = new Rect (nodeRect.x, nodeRect.y, nodeRect.width, nodeRect.height);
		
		GUI.changed = false;
		GUILayout.BeginArea (bodyRect, GUI.skin.box);
		NodeGUI ();
		GUILayout.EndArea ();
	}
	
	public override void NodeGUI () 
	{
		Outputs [0].SetPosition (10, NodeSide.Top);
		Outputs [1].SetPosition (10, NodeSide.Bottom);
		Outputs [2].SetPosition (20, NodeSide.Right);
		Outputs [3].SetPosition (20, NodeSide.Left);
		
		Inputs [0].SetPosition (30, NodeSide.Top);
		Inputs [1].SetPosition (30, NodeSide.Bottom);
		Inputs [2].SetPosition (40, NodeSide.Right);
		Inputs [3].SetPosition (40, NodeSide.Left);
	}
	
	public override bool Calculate () 
	{
		Outputs [0].SetValue<float> (Inputs [0].GetValue<float> ());
		Outputs [1].SetValue<float> (Inputs [1].GetValue<float> ());
		Outputs [2].SetValue<float> (Inputs [2].GetValue<float> ());
		Outputs [3].SetValue<float> (Inputs [3].GetValue<float> ());

		return true;
	}
}