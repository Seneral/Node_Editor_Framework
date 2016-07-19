using System;
using System.Collections.Generic;
using System.Linq;
using NodeEditorFramework;
using UnityEditor;
using UnityEngine;

[Node(false, "Dialog/Dialog With Options Node", new Type[]{typeof(DialogNodeCanvas)})]
public class DialogMultiOptionsNode : BaseDialogNode
{
    private const string Id = "multiOptionDialogNode";
    public override string GetID { get { return Id; } }
    public override Type GetObjectType { get { return typeof(DialogMultiOptionsNode); } }

    private const int StartValue = 222;
    private const int SizeValue = 22;

    [SerializeField]
    List<DataHolderForOption> _options;

    public override Node Create(Vector2 pos)
    {
        DialogMultiOptionsNode node = CreateInstance<DialogMultiOptionsNode>();

        node.rect = new Rect(pos.x, pos.y, 300, 265);
        node.name = "Dailog with Options Node";

        //Previous Node Connections
        node.CreateInput("Previous Node", "DialogForward", NodeSide.Left, 30);
        node.CreateOutput("Back Node", "DialogBack", NodeSide.Left, 50);

        ////Next Node to go to
        //node.CreateOutput("Next Node", "DialogForward", NodeSide.Right, 30);

        node.SayingCharacterName = "Morgen Freeman";
        node.WhatTheCharacterSays = "I'm GOD";
        node.SayingCharacterPotrait = null;

        node._options = new List<DataHolderForOption>();

        node.AddNewOption();
        
        return node;
    }

    public override void NodeGUI()
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

        GUILayout.Space(5);
        DrawOptions();

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();

        GUILayout.Space(5);
        if(GUILayout.Button("Add New Option"))
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
            Debug.Log("Remove options is clicked");
            RemoveLastOption();
        }

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
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
        foreach(DataHolderForOption option in _options)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            option.OptionDisplay = EditorGUILayout.TextField("Option : ", option.OptionDisplay);
            GUILayout.Space(4);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
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
                if (this.GetNodeKnob(Outputs[1]) != default(Node))
                    return this.GetNodeKnob(Outputs[1]) as BaseDialogNode;
                break;
            case (int)EDialogInputValue.Back:
                if (this.GetNodeKnob(Outputs[0]) != default(Node))
                    return this.GetNodeKnob(Outputs[0]) as BaseDialogNode;
                break;
            default:
                if (this.GetNodeKnob(Outputs[_options[inputValue].NodeOutputIndex]) != default(Node))
                    return this.GetNodeKnob(Outputs[_options[inputValue].NodeOutputIndex]) as BaseDialogNode;
                break;
        }
        return null;
    }

    public override bool IsBackAvailable()
    {
        return this.GetNodeKnob(Outputs[0]) != default(Node);
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
