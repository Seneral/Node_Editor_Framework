using UnityEngine;
using System.Collections;
using NodeEditorFramework;

[Node (false, "Texture/Channel Bundle")]
public class ChannelBundleNode : Node 
{
	public const string ID = "channelBundleNode";
	public override string GetID { get { return ID; } }

	public override string Title { get { return "Channel Bundle"; } }
	public override Vector2 DefaultSize { get { return new Vector2 (200, 100); } }

	[ValueConnectionKnob("Channel R", Direction.In, "Channel")]
	public ValueConnectionKnob input1Knob;
	[ValueConnectionKnob("Channel G", Direction.In, "Channel")]
	public ValueConnectionKnob input2Knob;
	[ValueConnectionKnob("Channel B", Direction.In, "Channel")]
	public ValueConnectionKnob input3Knob;
	[ValueConnectionKnob("Channel A", Direction.In, "Channel")]
	public ValueConnectionKnob input4Knob;

	[ValueConnectionKnob("Bundled", Direction.Out, "Texture2D")]
	public ValueConnectionKnob outputKnob;


	public override void NodeGUI () 
	{
		GUILayout.BeginHorizontal ();
		GUILayout.BeginVertical ();

		input1Knob.DisplayLayout (new GUIContent ("Channel R", "The R channel of the bundled texture"));
		input1Knob.SetPosition ();

		input2Knob.DisplayLayout (new GUIContent ("Channel G", "The G channel of the bundled texture"));
		input2Knob.SetPosition ();

		input3Knob.DisplayLayout (new GUIContent ("Channel B", "The B channel of the bundled texture"));
		input3Knob.SetPosition ();

		input4Knob.DisplayLayout (new GUIContent ("Channel A", "The A channel of the bundled texture"));
		input4Knob.SetPosition ();

		GUILayout.EndVertical ();
		GUILayout.BeginVertical ();

		// Output
		outputKnob.DisplayLayout (new GUIContent ("Bundled", "The bundled texture"));

		GUILayout.EndVertical ();
		GUILayout.EndHorizontal ();

		if (GUI.changed)
			NodeEditor.curNodeCanvas.OnNodeChange (this);
	}
	
	public override bool Calculate () 
	{
		Channel RChannel = (input1Knob.connected () && !input1Knob.IsValueNull) ? input1Knob.GetValue<Channel> () : null;
		Channel GChannel = (input2Knob.connected () && !input2Knob.IsValueNull) ? input2Knob.GetValue<Channel> () : null;
		Channel BChannel = (input3Knob.connected () && !input3Knob.IsValueNull) ? input3Knob.GetValue<Channel> () : null;
		Channel AChannel = (input4Knob.connected () && !input4Knob.IsValueNull) ? input4Knob.GetValue<Channel> () : null;

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
		outputKnob.SetValue<Texture2D> (bundled);
		return true;
	}
}
