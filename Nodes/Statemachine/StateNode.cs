using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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
		node.rect = new Rect(pos.x, pos.y, 350, 90);
		node.CreateOutput("Leave", "Transition");
		node.CreateInput("Enter", "Transition");
		return node;
	}
	[HideInInspector]
	[SerializeField]
	private List<StateTrigger> triggers;
	public override void DrawConnections() 
	{
		base.DrawConnections();
		var output = Outputs[0];
		var connections = output.connections;

		for (int i = 0; i < connections.Count; i++)
		{
			var connection = connections[i];
			var start = output.GetGUIKnob().center;
			var end = connection.GetGUIKnob().center;

			Vector3 startPos = new Vector3(start.x, start.y);
			Vector3 endPos = new Vector3(end.x, end.y);
			Vector3 startTan = startPos + Vector3.right * 50;
			Vector3 endTan = endPos + Vector3.left * 50;
			var stateTrigger = connection.GetValue<StateTrigger>();
			if (stateTrigger.trigger == null)
				stateTrigger.trigger = "";

			var points = UnityEditor.Handles.MakeBezierPoints(startPos, endPos, startTan, endTan, 4);
			stateTrigger.trigger = GUI.TextField(new Rect(points[2].x - 35, points[2].y - 10, 70, 20), stateTrigger.trigger);
		}
	}

	public override void NodeGUI()
	{
		stateName = PlaceGUITextField("State Name", stateName);
		PlaceGUIInputKnobHere(0);
		PlaceGUIOutputKnobHere(0);
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
