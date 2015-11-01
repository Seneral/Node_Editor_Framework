using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;

[System.Serializable]
[Node(false, "State Machine/State", true)]
public class StateNode : Node
{
	public UnityEvent OnEnter;
	public UnityEvent OnLeave;
	public string stateName = "";

	public override string GetID
	{
		get { return "stateNode"; }
	}

	public override Node Create (Vector2 pos)
	{
		StateNode node = CreateInstance<StateNode> ();

		node.rect = new Rect (pos.x, pos.y, 150, 50);

		return node;
	}

	public override void NodeGUI ()
	{
		stateName = RTEditorGUI.TextField (new GUIContent ("State Name"), stateName);
	}

	public override bool Calculate ()
	{
		return true;
	}
}
