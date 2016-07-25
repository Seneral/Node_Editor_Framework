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
    
    public override Node Create(Vector2 pos)
    {
        DialogNode node = CreateInstance<DialogNode>();

        node.rect = new Rect(pos.x, pos.y, 300, 210);
        node.name = "Dailog Node";

        //Previous Node Connections
        node.CreateInput("Previous Node", "DialogForward", NodeSide.Left, 30);
        node.CreateOutput("Back Node", "DialogBack", NodeSide.Left, 50);

        //Next Node to go to
        node.CreateOutput("Next Node", "DialogForward", NodeSide.Right, 30);
        node.CreateInput("Return Node", "DialogBack", NodeSide.Right, 50);

        node.SayingCharacterName = "Morgen Freeman";
        node.WhatTheCharacterSays = "I'm GOD";
        node.SayingCharacterPotrait = null;

        return node;
    }

    protected internal override void NodeGUI()
    {
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
