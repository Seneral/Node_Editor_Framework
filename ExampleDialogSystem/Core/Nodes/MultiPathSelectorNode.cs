using System;
using System.Collections.Generic;
using System.Linq;
using NodeEditorFramework;
using UnityEditor;
using UnityEngine;

[Node(false, "Dialog/MultiPath Node", new Type[] { typeof(DialogNodeCanvas) })]
public class MultiPathSelectorNode : BaseDialogNode
{
	public override string Title { get { return "Multi Path Node"; } }
	public override Vector2 MinSize { get { return new Vector2(400, 80); } }
	public override bool AutoLayout { get { return true; } }

	private const string Id = "multiPathNode";
	public override string GetID { get { return Id; } }
	public override Type GetObjectType { get { return typeof(MultiPathSelectorNode); } }

	//Previous Node Connections
	[ValueConnectionKnob("From Previous", Direction.In, "DialogForward", NodeSide.Left, 30)]
	public ValueConnectionKnob fromPreviousIN;

	private ValueConnectionKnobAttribute dynaCreationAttribute 
	= new ValueConnectionKnobAttribute(
		"To Next", Direction.Out, "DialogForward", NodeSide.Right);

	public DialogBlackboard.EDialogMultiChoiceVariables ValueToTest;

	[SerializeField]
	List<DataHolderForOption> _options;

	private const int StartValue = 54;
	private const int SizeValue = 24;

	protected override void OnCreate ()
	{
		base.OnCreate ();
		CharacterName = "Character";
		DialogLine = "Insert dialog text here";
		CharacterPotrait = null;
		ValueToTest = DialogBlackboard.EDialogMultiChoiceVariables.Random;
		_options = new List<DataHolderForOption>();

		AddNewOption();
	}

	public override void NodeGUI()
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
			DeleteConnectionPort(dynamicConnectionPorts.Count-1);
			SetNewMaxAndMin();
		}
	}

	private void DrawOptions()
	{
		int i = 0;
		foreach (DataHolderForOption option in _options)
		{
			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical();
			GUILayout.Label("Value : Greater or Equal to " + Math.Round(option.MinValue, 2) + " and Less than " +
							Math.Round(option.MaxValue, 2));
			((ConnectionKnob)dynamicConnectionPorts[i++]).SetPosition();
			GUILayout.Space(6);
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}
	}

	private void AddNewOption()
	{
		DataHolderForOption option = new DataHolderForOption();
		CreateValueConnectionKnob(dynaCreationAttribute);

		option.NodeOutputIndex = dynamicConnectionPorts.Count - 1;
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
		NodeEditorCallbacks.IssueOnAddConnectionPort (dynamicConnectionPorts[_options.Count - 1]);
	}

	public override BaseDialogNode Input(int inputValue)
	{
		float value = ValueToTest == DialogBlackboard.EDialogMultiChoiceVariables.Random
			? GetRandomValue()
			: GetValueFromBlackboard();

		int nodeIndex = GetNodeIndexFor(value);

		if (IsAvailable (dynamicConnectionPorts [nodeIndex]))
			return getTargetNode (dynamicConnectionPorts [nodeIndex]);

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
