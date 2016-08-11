using UnityEngine;
using System.Collections;
using NodeEditorFramework;

[Node (false, "Texture/Texture Split")]
public class TextureSplitNode : Node 
{
	public const string ID = "texSplitNode";
	public override string GetID { get { return ID; } }
	
	public override Node Create (Vector2 pos) 
	{
		TextureSplitNode node = ScriptableObject.CreateInstance <TextureSplitNode> ();

		node.name = "Texture Split";
		node.rect = new Rect (pos.x, pos.y, 150, 100);

		node.CreateInput ("Texture", "Texture2D");

		node.CreateOutput ("Channel R", "Channel");
		node.CreateOutput ("Channel G", "Channel");
		node.CreateOutput ("Channel B", "Channel");
		node.CreateOutput ("Channel A", "Channel");

		return node;
	}
	
	protected override void NodeGUI () 
	{
		GUILayout.BeginHorizontal ();

		Inputs [0].DisplayLayout (new GUIContent ("Texture", "The texture to split into channels"));

		GUILayout.BeginVertical ();
		Outputs [0].DisplayLayout (new GUIContent ("Channel R", "The R channel of the splitted texture"));
		Outputs [1].DisplayLayout (new GUIContent ("Channel G", "The G channel of the splitted texture"));
		Outputs [2].DisplayLayout (new GUIContent ("Channel B", "The B channel of the splitted texture"));
		Outputs [3].DisplayLayout (new GUIContent ("Channel A", "The A channel of the splitted texture"));
		GUILayout.EndVertical ();

		GUILayout.EndHorizontal ();

		if (GUI.changed)
			NodeEditor.RecalculateFrom (this);
	}
	
	public override bool Calculate () 
	{
		if (Inputs [0].connection == null || Inputs [0].connection.IsValueNull) 
		{
			Outputs [0].ResetValue ();
			Outputs [1].ResetValue ();
			Outputs [2].ResetValue ();
			Outputs [3].ResetValue ();
			return true;
		}
		Texture2D tex = Inputs [0].connection.GetValue<Texture2D> ();

		if (tex == null)
			return false;

		float[,] channelR = new float [tex.width, tex.height];
		float[,] channelG = new float [tex.width, tex.height];
		float[,] channelB = new float [tex.width, tex.height];
		float[,] channelA = new float [tex.width, tex.height];

		for (int x = 0; x < tex.width; x++) 
		{
			for (int y = 0; y < tex.height; y++) 
			{
				Color col = tex.GetPixel (x, y);
				channelR [x, y] = col.r;
				channelG [x, y] = col.g;
				channelB [x, y] = col.b;
				channelA [x, y] = col.a;
			}
		}

		Outputs [0].SetValue<Channel> (new Channel (tex.name + "_R", channelR));
		Outputs [1].SetValue<Channel> (new Channel (tex.name + "_G", channelG));
		Outputs [2].SetValue<Channel> (new Channel (tex.name + "_B", channelB));
		Outputs [3].SetValue<Channel> (new Channel (tex.name + "_A", channelA));

		return true;
	}
}