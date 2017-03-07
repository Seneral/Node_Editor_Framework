using System;
using NodeEditorFramework;
using UnityEditor;
using UnityEngine;

[Node(false, "Dialog/Dialog Start Node", new Type[] { typeof(DialogNodeCanvas) })]
public class DialogStartNode : BaseDialogNode
{
	public override Vector2 MinSize { get { return new Vector2(350, 60); } }
	public override bool Resizable { get { return true; } }

	private const string Id = "dialogStartNode";
	public override string GetID { get { return Id; } }
	public override Type GetObjectType { get { return typeof (DialogStartNode); } }

	private Vector2 scroll;
	public int DialogID;

	public override Node Create(Vector2 pos)
	{
		DialogStartNode node = CreateInstance<DialogStartNode>();

		//node.rect = new Rect(pos.x, pos.y, 300, 250);
		node.rect.position = pos;
		node.name = "Dialog Start Node";

		node.CreateOutput("Next Node", "DialogForward", NodeSide.Right, 30);
		node.CreateInput("Return Here", "DialogBack", NodeSide.Right, 50);

		node.CharacterName = "Character name";
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

		EditorGUIUtility.labelWidth = 90;
		DialogID = EditorGUILayout.IntField("DialogID", DialogID, GUILayout.ExpandWidth(true));
		GUILayout.Space(5);

		EditorStyles.textField.wordWrap = true;

		GUILayout.BeginHorizontal();
		scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(100));
		DialogLine = EditorGUILayout.TextArea(DialogLine, GUILayout.ExpandHeight(true));
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
				if (Outputs[0].GetNodeAcrossConnection() != default(Node))
					return Outputs[0].GetNodeAcrossConnection() as BaseDialogNode;
				break;
		}
		return null;
	}

	public override bool IsBackAvailable()
	{
		return false;
	}

	public override bool IsNextAvailable()
	{
		return Outputs[0].GetNodeAcrossConnection() != default(Node);
	}
}
