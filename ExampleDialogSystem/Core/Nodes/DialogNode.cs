using System;
using NodeEditorFramework;
using UnityEditor;
using UnityEngine;

[Node(false, "Dialog/Dialog Node", new Type[] { typeof(DialogNodeCanvas) })]
public class DialogNode : BaseDialogNode
{
	private const string Id = "dialogNode";
	public override string GetID { get { return Id; } }
	public override Type GetObjectType { get { return typeof(DialogNode); } }

	private Vector2 scroll;

	public override Node Create(Vector2 pos)
	{
		DialogNode node = CreateInstance<DialogNode>();

		node.rect = new Rect(pos.x, pos.y, 300, 230);
		node.name = "Dialog Node";

		//Previous Node Connections
		node.CreateInput("Previous Node", "DialogForward", NodeSide.Left, 30);
		node.CreateOutput("Back Node", "DialogBack", NodeSide.Left, 50);

		//Next Node to go to
		node.CreateOutput("Next Node", "DialogForward", NodeSide.Right, 30);
		node.CreateInput("Return Node", "DialogBack", NodeSide.Right, 50);

		node.CharacterName = "Character Name";
		node.DialogLine = "Insert dialog text here";
		node.CharacterPotrait = null;

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
		EditorGUILayout.EndVertical();
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
				if (Outputs[0].GetNodeAcrossConnection() != default(Node))
					return Outputs[0].GetNodeAcrossConnection() as BaseDialogNode;
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
		return Outputs[1].GetNodeAcrossConnection() != default(Node);
	}
}
