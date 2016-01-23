using System.Collections.Generic;
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
	public string stateName = "";

	public override bool AcceptsTranstitions { get { return true; } }

	public override string GetID
	{
		get { return "stateNode"; }
	}

	public override Node Create (Vector2 pos)
	{
		StateNode node = CreateInstance<StateNode> ();

		node.rect = new Rect (pos.x, pos.y, 150, 100);

		return node;
	}

	public override void NodeGUI ()
	{
		name = stateName = RTEditorGUI.TextField (new GUIContent ("State Name"), stateName);
		if (GUILayout.Button ("Start Transitioning from here!"))
		{
			NodeEditor.BeginTransitioning (NodeEditor.curNodeCanvas, this);
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
