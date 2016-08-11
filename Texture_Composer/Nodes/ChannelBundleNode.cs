using UnityEngine;
using System.Collections;
using NodeEditorFramework;

[Node (false, "Texture/Channel Bundle")]
public class ChannelBundleNode : Node 
{
	public const string ID = "channelBundleNode";
	public override string GetID { get { return ID; } }

	public override Node Create (Vector2 pos) 
	{
		ChannelBundleNode node = ScriptableObject.CreateInstance <ChannelBundleNode> ();

		node.name = "Channel Bundle";
		node.rect = new Rect (pos.x, pos.y, 200, 100);

		node.CreateOutput ("Bundled", "Texture2D");
		
		node.CreateInput ("Channel R", "Channel");
		node.CreateInput ("Channel G", "Channel");
		node.CreateInput ("Channel B", "Channel");
		node.CreateInput ("Channel A", "Channel");

		return node;
	}
	
	protected override void NodeGUI () 
	{
		GUILayout.BeginHorizontal ();

		GUILayout.BeginVertical ();
		Inputs [0].DisplayLayout (new GUIContent ("Channel R", "The R channel of the bundled texture"));
		Inputs [1].DisplayLayout (new GUIContent ("Channel G", "The G channel of the bundled texture"));
		Inputs [2].DisplayLayout (new GUIContent ("Channel B", "The B channel of the bundled texture"));
		Inputs [3].DisplayLayout (new GUIContent ("Channel A", "The A channel of the bundled texture"));
		GUILayout.EndVertical ();

		Outputs [0].DisplayLayout (new GUIContent ("Bundled", "The bundled texture"));

		GUILayout.EndHorizontal ();

		if (GUI.changed)
			NodeEditor.RecalculateFrom (this);
	}
	
	public override bool Calculate () 
	{
		Channel RChannel = Inputs [0].connection != null && !Inputs [0].connection.IsValueNull? Inputs [0].connection.GetValue<Channel> () : null;
		Channel GChannel = Inputs [1].connection != null && !Inputs [1].connection.IsValueNull? Inputs [1].connection.GetValue<Channel> () : null;
		Channel BChannel = Inputs [2].connection != null && !Inputs [2].connection.IsValueNull? Inputs [2].connection.GetValue<Channel> () : null;
		Channel AChannel = Inputs [3].connection != null && !Inputs [3].connection.IsValueNull? Inputs [3].connection.GetValue<Channel> () : null;

		int width = Mathf.Max (new int[] { RChannel != null? RChannel.width : 0, GChannel != null? GChannel.width : 0, BChannel != null? BChannel.width : 0, AChannel != null? AChannel.width : 0 });
		int height = Mathf.Max (new int[] { RChannel != null? RChannel.height : 0, GChannel != null? GChannel.height : 0, BChannel != null? BChannel.height : 0, AChannel != null? AChannel.height : 0 });

		Texture2D bundled = new Texture2D (width, height, AChannel != null? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
		bundled.name = "Bundled Tex";

		for (int x = 0; x < width; x++) 
		{
			for (int y = 0; y < height; y++) 
			{
				bundled.SetPixel (x, y, new Color (RChannel != null? RChannel.GetValue (x, y, width, height) : 0,
				                                   GChannel != null? GChannel.GetValue (x, y, width, height) : 0,
				                                   BChannel != null? BChannel.GetValue (x, y, width, height) : 0,
				                                   AChannel != null? AChannel.GetValue (x, y, width, height) : 1));
			}
		}

		bundled.Apply ();
		Outputs [0].SetValue<Texture2D> (bundled);
		return true;
	}
}
