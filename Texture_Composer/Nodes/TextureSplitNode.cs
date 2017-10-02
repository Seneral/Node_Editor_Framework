using UnityEngine;
using System.Collections;
using NodeEditorFramework;

[Node (false, "Texture/Texture Split")]
public class TextureSplitNode : Node 
{
	public const string ID = "texSplitNode";
	public override string GetID { get { return ID; } }

	public override string Title { get { return "Texture Split"; } }
	public override Vector2 DefaultSize { get { return new Vector2 (150, 100); } }

	[ValueConnectionKnob("Texture", Direction.In, "Texture2D")]
	public ValueConnectionKnob inputKnob;

	[ValueConnectionKnob("Channel R", Direction.Out, "Channel")]
	public ValueConnectionKnob output1Knob;
	[ValueConnectionKnob("Channel G", Direction.Out, "Channel")]
	public ValueConnectionKnob output2Knob;
	[ValueConnectionKnob("Channel B", Direction.Out, "Channel")]
	public ValueConnectionKnob output3Knob;
	[ValueConnectionKnob("Channel A", Direction.Out, "Channel")]
	public ValueConnectionKnob output4Knob;


	public override void NodeGUI () 
	{
		GUILayout.BeginHorizontal ();

		inputKnob.DisplayLayout (new GUIContent ("Texture", "The texture to split into channels"));
		inputKnob.SetPosition ();

		GUILayout.BeginVertical ();

		output1Knob.DisplayLayout (new GUIContent ("Channel R", "The R channel of the splitted texture"));
		output1Knob.SetPosition ();
		output2Knob.DisplayLayout (new GUIContent ("Channel G", "The G channel of the splitted texture"));
		output2Knob.SetPosition ();
		output3Knob.DisplayLayout (new GUIContent ("Channel B", "The B channel of the splitted texture"));
		output3Knob.SetPosition ();
		output4Knob.DisplayLayout (new GUIContent ("Channel A", "The A channel of the splitted texture"));
		output4Knob.SetPosition ();
		GUILayout.EndVertical ();

		GUILayout.EndHorizontal ();

		if (GUI.changed)
			NodeEditor.curNodeCanvas.OnNodeChange (this);
	}
	
	public override bool Calculate () 
	{
		if(!inputKnob.connected () || inputKnob.IsValueNull){
			output1Knob.ResetValue ();
			output2Knob.ResetValue ();
			output3Knob.ResetValue ();
			output4Knob.ResetValue ();
		}

		Texture2D tex = inputKnob.GetValue<Texture2D> (); //Inputs [0].connection.GetValue<Texture2D> ();

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

		output1Knob.SetValue<Channel> (new Channel (tex.name + "_R", channelR));
		output2Knob.SetValue<Channel> (new Channel (tex.name + "_G", channelG));
		output3Knob.SetValue<Channel> (new Channel (tex.name + "_B", channelB));
		output4Knob.SetValue<Channel> (new Channel (tex.name + "_A", channelA));

		return true;
	}
}