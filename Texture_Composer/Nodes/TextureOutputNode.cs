using UnityEngine;
using System.Collections;
using NodeEditorFramework;

[Node (false, "Texture/Texture Output")]
public class TextureOutputNode : Node 
{
	public const string ID = "texOutNode";
	public override string GetID { get { return ID; } }

	public override string Title { get { return "Texture Output"; } }
	public override Vector2 DefaultSize { get { return new Vector2 (150, 200); } }

	[ValueConnectionKnob("Texture", Direction.In, "Texture2D")]
	public ValueConnectionKnob inputKnob;

	public Texture2D tex;


	public override void NodeGUI () 
	{
		//rect.height = tex == null? 50 : 200;	// How do I change the nodes height????

		inputKnob.DisplayLayout (new GUIContent ("Texture", "The texture to output."));
		inputKnob.SetPosition ();

		if (tex != null) 
		{
			GUILayout.Box (tex, GUIStyle.none, new GUILayoutOption[] { GUILayout.Width (64), GUILayout.Height (64) });
		}

		if (GUI.changed)
			NodeEditor.curNodeCanvas.OnNodeChange (this);
	}
	
	public override bool Calculate () 
	{
		if(!inputKnob.connected () || inputKnob.IsValueNull)
			tex = null;
		else
			tex = inputKnob.GetValue<Texture2D> ();
		return true;
	}
}
