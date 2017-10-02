using UnityEngine;
using System.Collections;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;

[Node (false, "Texture/Texture Input")]
public class TextureInputNode : Node 
{
	public const string ID = "texInNode";
	public override string GetID { get { return ID; } }

	public override string Title { get { return "Texture Input"; } }
	public override Vector2 DefaultSize { get { return new Vector2 (100, 120); } }

	[ValueConnectionKnob("Texture", Direction.Out, "Texture2D")]
	public ValueConnectionKnob outputKnob;

	public Texture2D tex;


	public override void NodeGUI () 
	{
		outputKnob.DisplayLayout (new GUIContent ("Texture", "The input texture"));
		outputKnob.SetPosition ();

		tex = RTEditorGUI.ObjectField<Texture2D> (tex, false) as Texture2D;

		// TODO: Check if texture is readable

		if (GUI.changed)
			NodeEditor.curNodeCanvas.OnNodeChange (this);
	}
	
	public override bool Calculate () 
	{
		outputKnob.SetValue<Texture2D> (tex);
		return true;
	}
}
