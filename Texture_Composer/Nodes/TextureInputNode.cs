using UnityEngine;
using System.Collections;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;

[Node (false, "Texture/Texture Input")]
public class TextureInputNode : Node 
{
	public const string ID = "texInNode";
	public override string GetID { get { return ID; } }

	public Texture2D tex;
	
	public override Node Create (Vector2 pos) 
	{
		TextureInputNode node = ScriptableObject.CreateInstance <TextureInputNode> ();

		node.name = "Texture Input";
		node.rect = new Rect (pos.x, pos.y, 100, 120);
		
		node.CreateOutput ("Texture", "Texture2D");

		return node;
	}
	
	protected override void NodeGUI () 
	{
		Outputs [0].DisplayLayout (new GUIContent ("Texture", "The input texture"));

		tex = RTEditorGUI.ObjectField<Texture2D> (tex, false) as Texture2D;

		// TODO: Check if texture is readable

		if (GUI.changed)
			NodeEditor.curNodeCanvas.OnNodeChange (this);
	}
	
	public override bool Calculate () 
	{
		Outputs [0].SetValue<Texture2D> (tex);
		return true;
	}
}
