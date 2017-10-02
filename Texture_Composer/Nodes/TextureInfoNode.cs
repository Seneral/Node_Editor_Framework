using UnityEngine;
using System.Collections;
using NodeEditorFramework;

[Node (false, "Texture/Texture Info")]
public class TextureInfoNode : Node 
{
	public const string ID = "texInfoNode";
	public override string GetID { get { return ID; } }

	public override string Title { get { return "Texture Info"; } }
	public override Vector2 DefaultSize { get { return new Vector2 (150, 200); } }

	[ValueConnectionKnob("Texture", Direction.In, "Texture2D")]
	public ValueConnectionKnob inputKnob;

	public Texture2D tex;



	public override void NodeGUI () 
	{
		//rect.height = tex == null? 50 : 200; 	// Is there a way to change the node Rect ??

		inputKnob.DisplayLayout (new GUIContent ("Texture" + (tex != null? " :" : ""), "The texture to display information about."));
		inputKnob.SetPosition ();

		if (tex != null) 
		{
			GUILayout.Box (tex, GUIStyle.none, new GUILayoutOption[] { GUILayout.Width (64), GUILayout.Height (64) });
			GUILayout.Label ("Name: " + tex.name);
			GUILayout.Label ("Width: " + tex.width + "; Height: " + tex.height);
			GUILayout.Label ("Format: " + tex.format);
			GUILayout.Label ("Filter Mode: " + tex.filterMode);
			GUILayout.Label ("Wrap Mode: " + tex.wrapMode);
		}
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
