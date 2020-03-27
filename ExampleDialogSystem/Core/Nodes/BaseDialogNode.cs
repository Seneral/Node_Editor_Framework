using System;
using NodeEditorFramework;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// basic dialog node class, all other dialog nodes are derived from this
/// </summary>
[Node(true, "Dialog/Base Dialog Node", new Type[]{typeof(DialogNodeCanvas)})]
public abstract class BaseDialogNode : Node
{
	public abstract Type GetObjectType { get; }

	public override Vector2 MinSize { get { return new Vector2(350, 200); } }
	public override bool AutoLayout { get { return true;}}  //resizable renamed to autolayout?

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

	///check if the first connection of the specified port points to something
	protected bool IsAvailable(ConnectionPort port)
	{
		return port != null
			&& port.connections != null && port.connections.Count > 0
			&& port.connections[0].body != null
			&& port.connections[0].body != default(Node);
	}

	///return the dialog node pointed to by the first connection in the specified port
	protected BaseDialogNode getTargetNode(ConnectionPort port) {
		if (IsAvailable (port))
			return port.connections [0].body as BaseDialogNode;
		return null;
	}

}


public class DialogBackType : ConnectionKnobStyle //: IConnectionTypeDeclaration
{
	public override string Identifier { get { return "DialogBack"; } }
	public override Color Color { get { return Color.yellow; } }
}

public class DialogForwardType : ValueConnectionType // : IConnectionTypeDeclaration
{
	public override string Identifier { get { return "DialogForward"; } }
	public override Type Type { get { return typeof(float); } }
	public override Color Color { get { return Color.cyan; } }
}

