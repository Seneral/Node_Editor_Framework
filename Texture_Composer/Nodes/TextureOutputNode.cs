using UnityEngine;
using System.Collections;
using NodeEditorFramework;

[Node (false, "Texture/Texture Output")]
public class TextureOutputNode : Node 
{
	public const string ID = "texOutNode";
	public override string GetID { get { return ID; } }

	public Texture2D tex;
	
	public override Node Create (Vector2 pos) 
	{
		TextureOutputNode node = ScriptableObject.CreateInstance <TextureOutputNode> ();

		node.name = "Texture Output";
		node.rect = new Rect (pos.x, pos.y, 150, 50);
		
		node.CreateInput ("Texture", "Texture2D");

		return node;
	}

	protected override void NodeGUI () 
	{
		rect.height = tex == null? 50 : 200;

		Inputs [0].DisplayLayout (new GUIContent ("Texture", "The texture to output."));

		if (tex != null) 
		{
			GUILayout.Box (tex, GUIStyle.none, new GUILayoutOption[] { GUILayout.Width (64), GUILayout.Height (64) });
		}

		if (GUI.changed)
			NodeEditor.RecalculateFrom (this);
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
