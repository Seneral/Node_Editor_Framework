using System;
using System.Collections.Generic;
using System.Linq;
using NodeEditorFramework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One entry and multiple exits, one for each possible answer
/// </summary>
[Node(false, "Dialog/Dialog With Options Node", new Type[]{typeof(DialogNodeCanvas)})]
public class DialogMultiOptionsNode : BaseDialogNode
{
	public override string Title { get { return "Dialog with Options Node"; } }
	public override Vector2 MinSize { get { return new Vector2(400, 60); } }
	public override bool AutoLayout { get { return true; } }

	private const string Id = "multiOptionDialogNode";
	public override string GetID { get { return Id; } }
	public override Type GetObjectType { get { return typeof(DialogMultiOptionsNode); } }

	//previous node connections
	[ValueConnectionKnob("From Previous", Direction.In, "DialogForward", NodeSide.Left, 30)]
	public ValueConnectionKnob frinPreviousIN;
	[ConnectionKnob("To Previous", Direction.Out, "DialogBack", NodeSide.Left, 50)]
	public ConnectionKnob toPreviousOUT;

	///Next node 
	[ConnectionKnob("From Next",Direction.In, "DialogBack", NodeSide.Right, 50)]
	public ConnectionKnob fromNextIN;

	private const int StartValue = 276;
	private const int SizeValue = 24;

	[SerializeField]
	List<DataHolderForOption> _options;
	private Vector2 scroll;

	private ValueConnectionKnobAttribute dynaCreationAttribute 
	    = new ValueConnectionKnobAttribute(
		   "Next Node", Direction.Out, "DialogForward", NodeSide.Right);
	

	protected override void OnCreate ()
	{
		CharacterName = "Character Name";
		DialogLine = "Dialog Line Here";
		CharacterPotrait = null;

		_options = new List<DataHolderForOption>();

		AddNewOption();
	}

	public override void NodeGUI()
	{
		EditorGUILayout.BeginVertical("Box");
		GUILayout.BeginHorizontal();
		CharacterPotrait = (Sprite)EditorGUILayout.ObjectField(CharacterPotrait, typeof(Sprite), false, GUILayout.Width(65f), GUILayout.Height(65f));
		CharacterName = EditorGUILayout.TextField("", CharacterName);
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();

		GUILayout.Space(5);

		EditorStyles.textField.wordWrap = true;

		GUILayout.BeginHorizontal();

		scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(100));
		DialogLine = EditorGUILayout.TextArea(DialogLine, GUILayout.ExpandHeight(true));
		EditorGUILayout.EndScrollView();
		GUILayout.EndHorizontal();

		GUILayout.Space(5);

		GUILayout.BeginHorizontal();
		EditorGUIUtility.labelWidth = 90;
		SoundDialog = EditorGUILayout.ObjectField("Dialog Audio:", SoundDialog, typeof(AudioClip), false) as AudioClip;
		if (GUILayout.Button("►", GUILayout.Width(20)))
		{
			if (SoundDialog)
				AudioUtils.PlayClip(SoundDialog);
		}
		GUILayout.EndHorizontal();

		GUILayout.Space(5);

		#region Options
		GUILayout.BeginVertical("box");
		GUILayout.ExpandWidth(false);

		GUILayout.BeginHorizontal();
		GUILayout.Label("Options", GUI.skin.GetStyle("labelBoldCentered"));
		if (GUILayout.Button("+", GUILayout.Width(20)))
		{
			AddNewOption();
			IssueEditorCallBacks();
		}

		GUILayout.EndHorizontal();
		GUILayout.Space(5);

		DrawOptions();

		GUILayout.ExpandWidth(false);
		GUILayout.EndVertical();
	#endregion

	}
	
	private void RemoveLastOption()
	{
		if(_options.Count > 1)
		{
			DataHolderForOption option = _options.Last();
			_options.Remove(option);
			DeleteConnectionPort(dynamicConnectionPorts.Count-1);
		}
	}

	private void DrawOptions()
	{
		EditorGUILayout.BeginVertical();
		for (var i = 0; i < _options.Count; i++)
		{
			DataHolderForOption option = _options[i];
			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(i + ".", GUILayout.MaxWidth(15));
			option.OptionDisplay = EditorGUILayout.TextArea(option.OptionDisplay, GUILayout.MinWidth(80));
			((ValueConnectionKnob)dynamicConnectionPorts[i]).SetPosition();
			if (GUILayout.Button("‒", GUILayout.Width(20)))
			{
				_options.RemoveAt(i);
				DeleteConnectionPort (i);
				i--;
			}

			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
			GUILayout.Space(4);
		}
		GUILayout.EndVertical();
	}

	private void AddNewOption()
	{
		DataHolderForOption option = new DataHolderForOption {OptionDisplay = "Write Here"};
		CreateValueConnectionKnob(dynaCreationAttribute);
		_options.Add(option);
	}

	//For Resolving the Type Mismatch Issue
	private void IssueEditorCallBacks()
	{
		NodeEditorCallbacks.IssueOnAddConnectionPort (dynamicConnectionPorts[_options.Count - 1]);
	}

	public override BaseDialogNode Input(int inputValue)
	{
		switch (inputValue)
		{
		case (int)EDialogInputValue.Next:
			break;

		case (int)EDialogInputValue.Back:
			if (IsAvailable(toPreviousOUT))
				return getTargetNode(toPreviousOUT);
			break;

		default:
				//if(Outputs[_options[inputValue].dynamicConnectionPortsIndex].GetNodeAcrossConnection() != default(Node))
				//	return Outputs[_options[inputValue].dynamicConnectionPortsIndex].GetNodeAcrossConnection() as BaseDialogNode;
			//I think we -2 for next and back, but not really sure yet
			//TODO is this right?
			Debug.Log("checking dynamic connection port " + inputValue);
			if (IsAvailable (dynamicConnectionPorts [inputValue]))
				return getTargetNode (dynamicConnectionPorts [inputValue]);
			break;
		}
		return null;
	}

	public override bool IsBackAvailable()
	{
		return IsAvailable (toPreviousOUT);
	}

	public override bool IsNextAvailable()
	{
		return false;
	}

	[Serializable]
	class DataHolderForOption
	{
		public string OptionDisplay;
		//public int dynamicConnectionPortsIndex;				
	}

	public List<string> GetAllOptions()
	{
		return _options.Select(option => option.OptionDisplay).ToList();
	}
}
