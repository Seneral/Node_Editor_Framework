using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;

[System.Serializable]
public class TransitionCondition : UnityFunc<Transition, bool> { public TransitionCondition (System.Delegate func) : base (func) {} }

[System.Serializable]
[Node(false, "State Machine/State")]
public class StateNode : Node
{
	[System.Serializable]
	public class StateChange : UnityEvent<Transition> {}

	public StateChange OnStateEnter;
	public StateChange OnStateLeave;
	public string stateName = "";

	public override bool AcceptsTranstitions { get { return true; } }

	public override string GetID
	{
		get { return "stateNode"; }
	}

	public override Node Create (Vector2 pos)
	{
		StateNode node = CreateInstance<StateNode> ();
		name = "StateNode";
		node.rect = new Rect (pos.x, pos.y, 200, 80);

		OnStateEnter = new StateChange ();
		OnStateLeave = new StateChange ();

		return node;
	}

	protected internal override void NodeGUI ()
	{
		name = stateName = RTEditorGUI.TextField (new GUIContent ("State Name"), stateName);
		if (GUILayout.Button ("Start Transitioning!"))
			NodeEditor.BeginTransitioning (NodeEditor.curNodeCanvas, this);
	}

	public override bool Calculate ()
	{
		return true;
	}

	protected internal override void OnEnter (Transition originTransition) 
	{
		OnStateEnter.Invoke (originTransition);
	}

	protected internal override void OnLeave (Transition transition) 
	{
		OnStateLeave.Invoke (transition);
	}

//	protected internal override void OnAddTransition (Transition trans) 
//	{
//		Debug.Log ("Node " + name + " was assigned the new Transition " + trans);
//	}
}
