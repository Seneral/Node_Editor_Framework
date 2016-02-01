using UnityEngine;
using UnityEngine.Events;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;

[System.Serializable]
[Node (false, "State Machine/State")]
public class StateNode : Node
{
	public UnityEvent OnEnter;
	public UnityEvent OnLeave;
	public string StateName = "";

	public override bool AcceptsTranstitions { get { return true; } }

	public override string GetID
	{
		get { return "stateNode"; }
	}

	public override Node Create (Vector2 pos)
	{
		StateNode node = CreateInstance<StateNode> ();

		node.Rect = new Rect (pos.x, pos.y, 150, 100);

		return node;
	}

	public override void NodeGUI ()
	{
		name = StateName = RTEditorGUI.TextField (new GUIContent ("State Name"), StateName);
		if (GUILayout.Button ("Start Transitioning from here!"))
		{
			NodeEditor.BeginTransitioning (NodeEditor.CurNodeCanvas, this);
		}
	}

	public override bool Calculate ()
	{
		return true;
	}

	protected internal override void OnAddTransition (Transition trans) 
	{
		Debug.Log ("Node " + name + " was assigned the new Transition " + trans);
	}
}
