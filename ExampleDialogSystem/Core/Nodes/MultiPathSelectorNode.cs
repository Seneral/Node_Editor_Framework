using System;
using System.Collections.Generic;
using System.Linq;
using NodeEditorFramework;
using UnityEditor;
using UnityEngine;

[Node(false, "Dialog/MultiPath Node", new Type[] { typeof(DialogNodeCanvas) })]
public class MultiPathSelectorNode : BaseDialogNode
{
	private const string Id = "multiPathNode";
	public override string GetID { get { return Id; } }
	public override Type GetObjectType { get { return typeof(MultiPathSelectorNode); } }

	public DialogBlackboard.EDialogMultiChoiceVariables ValueToTest;

	[SerializeField]
	List<DataHolderForOption> _options;

	private const int StartValue = 54;
	private const int SizeValue = 24;

	public override Node Create(Vector2 pos)
	{
		MultiPathSelectorNode node = CreateInstance<MultiPathSelectorNode>();

		node.rect = new Rect(pos.x, pos.y, 300, 100);
		node.name = "Multi Path Node";

		//Previous Node Connections
		node.CreateInput("Previous Node", "DialogForward", NodeSide.Left, 30);		

		node.CharacterName = "Character";
		node.DialogLine = "Insert dialog text here";
		node.CharacterPotrait = null;
		node.ValueToTest = DialogBlackboard.EDialogMultiChoiceVariables.Random;
		node._options = new List<DataHolderForOption>();

		node.AddNewOption();

		return node;
	}

	protected internal override void NodeGUI()
	{
		GUILayout.BeginHorizontal();
		ValueToTest =
			(DialogBlackboard.EDialogMultiChoiceVariables) EditorGUILayout.EnumPopup("Value to Test", ValueToTest);
		GUILayout.EndHorizontal();

		GUILayout.Space(5);
		DrawOptions();

		GUILayout.BeginHorizontal();
		GUILayout.BeginVertical();

		GUILayout.Space(5);
		if (GUILayout.Button("Add New Option"))
		{
			AddNewOption();
			IssueEditorCallBacks();
		}

		GUILayout.EndVertical();
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.BeginVertical();

		GUILayout.Space(5);
		if (GUILayout.Button("Remove Last Option"))
		{
			RemoveLastOption();
		}

		GUILayout.EndVertical();
		GUILayout.EndHorizontal();
	}

	private void RemoveLastOption()
	{
		if (_options.Count > 1)
		{
			DataHolderForOption option = _options.Last();
			_options.Remove(option);
			Outputs[option.NodeOutputIndex].Delete();
			rect = new Rect(rect.x, rect.y, rect.width, rect.height - SizeValue);
			SetNewMaxAndMin();
		}
	}

	private void DrawOptions()
	{
		foreach (DataHolderForOption option in _options)
		{
			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical();
			GUILayout.Label("Value : Greater or Equal to " + Math.Round(option.MinValue, 2) + " and Less than " +
							Math.Round(option.MaxValue, 2));
			GUILayout.Space(6);
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}
	}

	private void AddNewOption()
	{
		DataHolderForOption option = new DataHolderForOption();
		CreateOutput("Next Node", "DialogForward", NodeSide.Right, StartValue + _options.Count * SizeValue);
		option.NodeOutputIndex = Outputs.Count - 1;
		rect = new Rect(rect.x, rect.y, rect.width, rect.height + SizeValue);
		_options.Add(option);
		SetNewMaxAndMin();
	}

	private void SetNewMaxAndMin()
	{
		int count = _options.Count;
		float interval = 1.0f/count;
		float startValue = 0.0f;
		foreach (DataHolderForOption option in _options)
		{
			option.MinValue = startValue;
			startValue += interval;
			option.MaxValue = startValue;
		}
	}

	//For Resolving the Type Mismatch Issue
	private void IssueEditorCallBacks()
	{
		DataHolderForOption option = _options.Last();
		NodeEditorCallbacks.IssueOnAddNodeKnob(Outputs[option.NodeOutputIndex]);
	}

	public override BaseDialogNode Input(int inputValue)
	{
		float value = ValueToTest == DialogBlackboard.EDialogMultiChoiceVariables.Random
			? GetRandomValue()
			: GetValueFromBlackboard();

		int nodeIndex = GetNodeIndexFor(value);

		if (Outputs[nodeIndex].GetNodeAcrossConnection() != default(Node))
			return Outputs[nodeIndex].GetNodeAcrossConnection() as BaseDialogNode;

		return null;
	}

	private int GetNodeIndexFor(float value)
	{
		value = Mathf.Clamp(value, 0.0f, 1f);
		return
			(from option in _options
				where option.MinValue <= value && value <= option.MaxValue
				select option.NodeOutputIndex).FirstOrDefault();
	}

	private float GetValueFromBlackboard()
	{
		return DialogBlackboard.GetValueFor(ValueToTest);
	}

	private float GetRandomValue()
	{
		return UnityEngine.Random.Range(0.0f, 1.0f);
	}

	public override bool IsBackAvailable()
	{
		return false;
	}

	public override bool IsNextAvailable()
	{
		return false;
	}

	public override BaseDialogNode PassAhead(int inputValue)
	{
		return Input(inputValue);
	}

	[Serializable]
	class DataHolderForOption
	{
		public float MinValue;
		public float MaxValue;
		public int NodeOutputIndex;
	}
}
