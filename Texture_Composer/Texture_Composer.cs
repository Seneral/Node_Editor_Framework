using UnityEngine;
using System;
using System.Collections;
using NodeEditorFramework;



public class Texture2DType : ValueConnectionType //IConnectionTypeDeclaration 
{
	public override string Identifier { get { return "Texture2D"; } }
	public override Type Type { get { return typeof(Texture2D); } }
	public override Color Color { get { return Color.magenta; } }
	//public override string InKnobTex { get { return "Textures/In_Knob.png"; } }
	//public override string OutKnobTex { get { return "Textures/Out_Knob.png"; } }
}

public class ChannelType : ValueConnectionType //IConnectionTypeDeclaration 
{
	public override string Identifier { get { return "Channel"; } }
	public override Type Type { get { return typeof(Channel); } }
	public override Color Color { get { return Color.yellow; } }
	//public override string InKnobTex { get { return "Textures/In_Knob.png"; } }
	//public override string OutKnobTex { get { return "Textures/Out_Knob.png"; } }
}

public class Channel 
{
	public string name;
	public float[,] data;

	public Channel () 
	{
		name = "Null";
		data = null;
	}

	public Channel (string channelName, float[,] channelData) 
	{
		name = channelName;
		data = channelData;
	}
	
	public float GetValue (int x, int y, int totalWidth, int totalHeight) 
	{
		return data [Mathf.FloorToInt ((float)x / totalWidth * data.GetLength (0)), 
		             Mathf.FloorToInt ((float)y / totalHeight * data.GetLength (1))];
	}
	
	public int width 
	{
		get { return data.GetLength (0); }
	}
	
	public int height
	{
		get { return data.GetLength (1); }
	}
}