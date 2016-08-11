using UnityEngine;
using System.Collections;
using NodeEditorFramework;

[Node (false, "Texture/Texture Info")]
public class TextureInfoNode : Node 
{
	public const string ID = "texInfoNode";
	public override string GetID { get { return ID; } }

	public Texture2D tex;
	
	public override Node Create (Vector2 pos) 
	{
		TextureInfoNode node = ScriptableObject.CreateInstance <TextureInfoNode> ();

		node.name = "Texture Info";
		node.rect = new Rect (pos.x, pos.y, 150, 50);

		node.CreateInput ("Texture", "Texture2D");

		return node;
	}
	
	protected override void NodeGUI () 
	{
		rect.height = tex == null? 50 : 200;

		Inputs [0].DisplayLayout (new GUIContent ("Texture" + (tex != null? " :" : ""), "The texture to display information about."));

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
		if (Inputs [0].connection == null || Inputs [0].connection.IsValueNull)
			tex = null;
		else
			tex = Inputs [0].connection.GetValue<Texture2D> ();
		return true;
	}
}
