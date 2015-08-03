using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using NodeEditorFramework;

[System.Serializable]
[Node(false, "State Machine/State")]
public class StateNode : Node, ISerializationCallbackReceiver
{
	public UnityEvent OnEnter;
	public UnityEvent OnLeave;
	public string stateName = "";

	public override string GetID
	{
		get { return "stateNode"; }
	}

	public override Node Create(Vector2 pos)
	{
		var node = CreateInstance<StateNode>();
		node.rect = new Rect (pos.x, pos.y, 150, 50);
		node.CreateOutput ("Leave", "Transition");
		node.CreateInput ("Enter", "Transition");
		return node;
	}
	[HideInInspector]
	[SerializeField]
	private List<StateTrigger> triggers;
	public override void DrawConnections() 
	{
		base.DrawConnections ();

		NodeOutput output = Outputs [0];
		for (int cnt = 0; cnt < output.connections.Count; cnt++)
		{
			NodeInput con = output.connections [cnt];
			Vector2 bezierCenter = Vector2.Lerp (output.GetGUIKnob ().center, con.GetGUIKnob().center, 0.5f);
			StateTrigger stateTrigger = con.GetValue<StateTrigger>();
			if (stateTrigger.trigger == null)
				stateTrigger.trigger = "";

			Rect rect = new Rect (0, 0, 70, 20);
			rect.center = bezierCenter;
			stateTrigger.trigger = GUI.TextField (rect, stateTrigger.trigger);
		}
	}

	public override void NodeGUI()
	{
		stateName = GUIExt.TextField (new GUIContent ("State Name"), stateName);
		InputKnob (0);
		OutputKnob (0);
	}

	public override bool Calculate()
	{
		return true;
	}

	#region ISerializationCallbackReceiver Members

	public void OnAfterDeserialize()
	{
		if (Outputs.Count > 0)
		{
			var output = Outputs[0];
			var connections = output.connections;
			for (int i = 0; i < connections.Count; i++)
			{
				if (i >= triggers.Count) break;
				var connection = connections[i];
				connection.SetValue(triggers[i]);
			}
		}
		triggers = null;
	}

	public void OnBeforeSerialize()
	{
		triggers = new List<StateTrigger>(); 
		if (Outputs.Count > 0)
		{
			var output = Outputs[0];
			var connections = output.connections;
			for (int i = 0; i < connections.Count; i++)
			{
				var connection = connections[i];
				triggers.Add(connection.GetValue<StateTrigger>());
			}
		}
	}

	#endregion
}
