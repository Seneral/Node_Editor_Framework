using System;
using System.Collections.Generic;
using System.Linq;
using NodeEditorFramework;
using UnityEditor;
using UnityEngine;

[Node(false, "Dialog/Dialog With Options Node", new Type[]{typeof(DialogNodeCanvas)})]
public class DialogMultiOptionsNode : BaseDialogNode
{
	public override Vector2 MinSize { get { return new Vector2(400, 60); } }
	public override bool Resizable { get { return true; } }

	private const string Id = "multiOptionDialogNode";
	public override string GetID { get { return Id; } }
	public override Type GetObjectType { get { return typeof(DialogMultiOptionsNode); } }

	private const int StartValue = 276;
	private const int SizeValue = 24;

	[SerializeField]
	List<DataHolderForOption> _options;
	private Vector2 scroll;

	public override Node Create(Vector2 pos)
	{
		DialogMultiOptionsNode node = CreateInstance<DialogMultiOptionsNode>();

		//node.rect = new Rect(pos.x, pos.y, 300, 275);
		node.rect.position = pos;
		node.name = "Dialog with Options Node";

		//Previous Node Connections
		node.CreateInput("Previous Node", "DialogForward", NodeSide.Left, 30);
		node.CreateOutput("Back Node", "DialogBack", NodeSide.Left, 50);

		////Next Node to go to
		//node.CreateOutput("Next Node", "DialogForward", NodeSide.Right, 30);

		node.CharacterName = "Character Name";
		node.DialogLine = "Dialog Line Here";
		node.CharacterPotrait = null;

		node._options = new List<DataHolderForOption>();

		node.AddNewOption();
		
		return node;
	}

	protected internal override void NodeGUI()
	{
		EditorGUILayout.BeginVertical("Box", GUILayout.ExpandHeight(true));

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
		GUILayout.Label("Options", NodeEditorGUI.nodeLabelBoldCentered);
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

		EditorGUILayout.EndVertical();
	}
	
	private void RemoveLastOption()
	{
		if(_options.Count > 1)
		{
			DataHolderForOption option = _options.Last();
			_options.Remove(option);
			Outputs[option.NodeOutputIndex].Delete();
			rect = new Rect(rect.x, rect.y, rect.width, rect.height - SizeValue);
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
			EditorGUILayout.LabelField(option.NodeOutputIndex + ".", GUILayout.MaxWidth(15));
			option.OptionDisplay = EditorGUILayout.TextArea(option.OptionDisplay, GUILayout.MinWidth(80));
			OutputKnob (_options[i].NodeOutputIndex);
			if (GUILayout.Button("‒", GUILayout.Width(20)))
			{
				_options.RemoveAt(i);
				Outputs[option.NodeOutputIndex].Delete();
				rect = new Rect(rect.x, rect.y, rect.width, rect.height - SizeValue);
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
		CreateOutput("Next Node", "DialogForward", NodeSide.Right,
			StartValue + _options.Count * SizeValue);
		option.NodeOutputIndex = Outputs.Count - 1;		
		rect = new Rect(rect.x, rect.y, rect.width, rect.height + SizeValue);
		_options.Add(option);
	}

	//For Resolving the Type Mismatch Issue
	private void IssueEditorCallBacks()
	{
		DataHolderForOption option = _options.Last();
		NodeEditorCallbacks.IssueOnAddNodeKnob(Outputs[option.NodeOutputIndex]);
	}

	public override BaseDialogNode Input(int inputValue)
	{
		switch (inputValue)
		{
			case (int)EDialogInputValue.Next:
				if (Outputs[1].GetNodeAcrossConnection() != default(Node))
					return Outputs[1].GetNodeAcrossConnection() as BaseDialogNode;
				break;
			case (int)EDialogInputValue.Back:
				if(Outputs[0].GetNodeAcrossConnection() != default(Node))
					return Outputs[0].GetNodeAcrossConnection() as BaseDialogNode;
				break;
			default:
				if(Outputs[_options[inputValue].NodeOutputIndex].GetNodeAcrossConnection() != default(Node))
					return Outputs[_options[inputValue].NodeOutputIndex].GetNodeAcrossConnection() as BaseDialogNode;
				break;
		}
		return null;
	}

	public override bool IsBackAvailable()
	{
		return Outputs[0].GetNodeAcrossConnection() != default(Node);
	}

	public override bool IsNextAvailable()
	{
		return false;
	}

	[Serializable]
	class DataHolderForOption
	{
		public string OptionDisplay;
		public int NodeOutputIndex;				
	}

	public List<string> GetAllOptions()
	{
		return _options.Select(option => option.OptionDisplay).ToList();
	}
}
