using System;
using NodeEditorFramework;
using UnityEditor;
using UnityEngine;
/// <summary>
/// This node has one entry and one exit, it is just to display something, then move on
/// </summary>
[Node(false, "Dialog/Dialog Node", new Type[] { typeof(DialogNodeCanvas) })]
public class DialogNode : BaseDialogNode
{
	public override string Title {get { return "Dialog Node"; } }
	public override Vector2 MinSize { get { return new Vector2(350, 60); } }
	public override bool AutoLayout { get { return true; } }

	private const string Id = "dialogNode";
	public override string GetID { get { return Id; } }
	public override Type GetObjectType { get { return typeof(DialogNode); } }

	//Previous Node Connections
	[ValueConnectionKnob("From Previous", Direction.In, "DialogForward", NodeSide.Left, 30)]
	public ValueConnectionKnob fromPreviousIN;
	[ConnectionKnob("To Previous", Direction.Out, "DialogBack", NodeSide.Left, 50)]
	public ConnectionKnob toPreviousOut;

	//Next Node to go to
	[ValueConnectionKnob("To Next", Direction.Out, "DialogForward", NodeSide.Right, 30)]
	public ValueConnectionKnob toNextOUT;
	[ConnectionKnob("From Next",Direction.In, "DialogBack", NodeSide.Right, 50)]
	public ConnectionKnob fromNextIN;

	private Vector2 scroll;

	protected override void OnCreate ()
	{
		CharacterName = "Character Name";
		DialogLine = "Insert dialog text here";
		CharacterPotrait = null;
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

		GUILayout.BeginHorizontal();

		scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(100));
		EditorStyles.textField.wordWrap = true;
		DialogLine = EditorGUILayout.TextArea(DialogLine, GUILayout.ExpandHeight(true));
		EditorStyles.textField.wordWrap = false;
		EditorGUILayout.EndScrollView();
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		EditorGUIUtility.labelWidth = 90;
		SoundDialog = EditorGUILayout.ObjectField("Dialog Audio:", SoundDialog, typeof(AudioClip), false) as AudioClip;
		if (GUILayout.Button("►", GUILayout.Width(20)))
		{
			if (SoundDialog)
				AudioUtils.PlayClip(SoundDialog);
		}
		GUILayout.EndHorizontal();
	}

	public override BaseDialogNode Input(int inputValue)
	{
		switch (inputValue)
		{
		case (int)EDialogInputValue.Next:
			if (IsNextAvailable ())
				return getTargetNode (toNextOUT);
			break;
		case (int)EDialogInputValue.Back:
			if (IsBackAvailable ())
				return getTargetNode (fromPreviousIN);
			break;
		}
		return null;
	}

	public override bool IsBackAvailable()
	{
		return IsAvailable (toPreviousOut);
	}

	public override bool IsNextAvailable()
	{
		return IsAvailable (toNextOUT);
	}
}
