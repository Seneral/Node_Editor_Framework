using System;
using NodeEditorFramework;
using UnityEngine;
using UnityEngine.Serialization;

[Node(true, "Dialog/Base Dialog Node", new Type[]{typeof(DialogNodeCanvas)})]
public abstract class BaseDialogNode : Node
{
    public override bool AllowRecursion { get { return true; } }
    public abstract Type GetObjectType { get; }

    [FormerlySerializedAs("SayingCharacterName")]
    public string CharacterName;
    [FormerlySerializedAs("SayingCharacterPotrait")]
    public Sprite CharacterPotrait;
    [FormerlySerializedAs("WhatTheCharacterSays")]
    public string DialogLine;

    public AudioClip SoundDialog;

    public abstract BaseDialogNode Input(int inputValue);
    public abstract bool IsBackAvailable();
    public abstract bool IsNextAvailable();

    public virtual BaseDialogNode PassAhead(int inputValue)
    {
        return this;
    }
}

public class DialogBackType : IConnectionTypeDeclaration
{
    public string Identifier { get { return "DialogBack"; } }
    public Type Type { get { return typeof(void); } }
    public Color Color { get { return Color.red; } }
    public string InKnobTex { get { return "Textures/In_Knob.png"; } }
    public string OutKnobTex { get { return "Textures/Out_Knob.png"; } }
}

public class DialogForwardType : IConnectionTypeDeclaration
{
    public string Identifier { get { return "DialogForward"; } }
    public Type Type { get { return typeof(float); } }
    public Color Color { get { return Color.cyan; } }
    public string InKnobTex { get { return "Textures/In_Knob.png"; } }
    public string OutKnobTex { get { return "Textures/Out_Knob.png"; } }
}