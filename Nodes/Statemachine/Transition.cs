using UnityEngine;
using System.Collections;
using System;

public class NodeTransitionType : ITypeDeclaration
{
	public string name { get { return "Transition"; } }
	public Color col { get { return Color.cyan; } }
	public string InputKnob_TexPath { get { return "Textures/In_Knob.png"; } }
	public string OutputKnob_TexPath { get { return "Textures/Out_Knob.png"; } }
	public Type InputType { get { return typeof(StateTrigger); } }
	public Type OutputType { get { return null; } }
}

[Serializable]
public class StateTrigger
{
	public StateTrigger() { }
	public string trigger = "";
}