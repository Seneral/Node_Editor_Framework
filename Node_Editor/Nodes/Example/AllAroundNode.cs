using UnityEngine;
using NodeEditorFramework;

namespace NodeEditorFramework.Standard
{
	[Node (false, "Example/AllAround Node")]
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
			
			node.CreateInput ("Input Top", "Float", NodeSide.Top, 20);
			node.CreateInput ("Input Bottom", "Float", NodeSide.Bottom, 20);
			node.CreateInput ("Input Right", "Float", NodeSide.Right, 20);
			node.CreateInput ("Input Left", "Float", NodeSide.Left, 20);
			
			node.CreateOutput ("Output Top", "Float", NodeSide.Top, 40);
			node.CreateOutput ("Output Bottom", "Float", NodeSide.Bottom, 40);
			node.CreateOutput ("Output Right", "Float", NodeSide.Right, 40);
			node.CreateOutput ("Output Left", "Float", NodeSide.Left, 40);
			
			return node;
		}
		
		protected internal override void DrawNode () 
		{
			Rect nodeRect = rect;
			nodeRect.position += NodeEditor.curEditorState.zoomPanAdjust + NodeEditor.curEditorState.panOffset;
			
			Rect bodyRect = new Rect (nodeRect.x, nodeRect.y, nodeRect.width, nodeRect.height);
			
			GUI.changed = false;
			GUILayout.BeginArea (bodyRect, GUI.skin.box);
			NodeGUI ();
			GUILayout.EndArea ();
		}
		
		protected internal override void NodeGUI () 
		{
			
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
}