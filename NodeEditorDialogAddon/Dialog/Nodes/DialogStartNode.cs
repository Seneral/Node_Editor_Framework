using System;
using NodeEditorFramework;
using UnityEditor;
using UnityEngine;

[Node(false, "Dialog/Dialog Start Node", new Type[] { typeof(DialogNodeCanvas) })]
public class DialogStartNode : BaseDialogNode
{
    private const string Id = "dialogStartNode";
    public override string GetID { get { return Id; } }
    public override Type GetObjectType { get { return typeof (DialogStartNode); } }

    public int DialogID;

    public override Node Create(Vector2 pos)
    {
        DialogStartNode node = CreateInstance<DialogStartNode>();

        node.rect = new Rect(pos.x, pos.y, 300, 230);
        node.name = "Dailog Start Node";

        node.CreateOutput("Next Node", "DialogForward", NodeSide.Right, 30);
        node.CreateInput("Return Here", "DialogBack", NodeSide.Right, 50);

        node.SayingCharacterName = "Morgen Freeman";
        node.WhatTheCharacterSays = "I'm GOD";
        node.SayingCharacterPotrait = null;

        return node;
    }

    public override void NodeGUI()
    {
        GUILayout.BeginHorizontal();

        DialogID = EditorGUILayout.IntField("DialogID", DialogID);

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        SayingCharacterName = EditorGUILayout.TextField("Character Name", SayingCharacterName);

        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();

        WhatTheCharacterSays = EditorGUILayout.TextArea(WhatTheCharacterSays, GUILayout.Height(100));

        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();

        SayingCharacterPotrait = EditorGUILayout.ObjectField("Character Potrait", SayingCharacterPotrait,
            typeof(Sprite), false) as Sprite;

        GUILayout.EndHorizontal();
    }

    public override BaseDialogNode Input(int inputValue)
    {
        switch (inputValue)
        {
            case (int)EDialogInputValue.Next:
                if (this.GetNodeKnob(Outputs[0]) != default(Node))
                    return this.GetNodeKnob(Outputs[0]) as BaseDialogNode;
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
        return this.GetNodeKnob(Outputs[0]) != default(Node);
    }
}
