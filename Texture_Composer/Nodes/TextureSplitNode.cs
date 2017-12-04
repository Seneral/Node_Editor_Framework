using UnityEngine;

namespace NodeEditorFramework.TextureComposer
{
	[Node(false, "Texture/Split")]
	public class TextureSplitNode : Node
	{
		public const string ID = "texSplitNode";
		public override string GetID { get { return ID; } }

		public override string Title { get { return "Texture Split"; } }
		public override Vector2 DefaultSize { get { return new Vector2(150, 100); } }

		[ValueConnectionKnob("Texture", Direction.In, "Texture")]
		public ValueConnectionKnob textureInputKnob;

		[ValueConnectionKnob("Channel R", Direction.Out, "Channel")]
		public ValueConnectionKnob channelRKnob;
		[ValueConnectionKnob("Channel G", Direction.Out, "Channel")]
		public ValueConnectionKnob channelGKnob;
		[ValueConnectionKnob("Channel B", Direction.Out, "Channel")]
		public ValueConnectionKnob channelBKnob;
		[ValueConnectionKnob("Channel A", Direction.Out, "Channel")]
		public ValueConnectionKnob channelAKnob;

		public override void NodeGUI()
		{
			GUILayout.BeginHorizontal();

			textureInputKnob.DisplayLayout();

			GUILayout.BeginVertical();
			channelRKnob.DisplayLayout();
			channelGKnob.DisplayLayout();
			channelBKnob.DisplayLayout();
			channelAKnob.DisplayLayout();
			GUILayout.EndVertical();

			GUILayout.EndHorizontal();

			if (GUI.changed)
				NodeEditor.curNodeCanvas.OnNodeChange(this);
		}

		public override bool Calculate()
		{
			Texture2D tex = textureInputKnob.GetValue<Texture2D>();
			if (!textureInputKnob.connected() || tex == null)
			{ // Reset outputs if no texture is available
				channelRKnob.ResetValue();
				channelGKnob.ResetValue();
				channelBKnob.ResetValue();
				channelAKnob.ResetValue();
				return true;
			}

			// Create new channels
			float[,] channelR = new float[tex.width, tex.height];
			float[,] channelG = new float[tex.width, tex.height];
			float[,] channelB = new float[tex.width, tex.height];
			float[,] channelA = new float[tex.width, tex.height];

			for (int x = 0; x < tex.width; x++)
			{
				for (int y = 0; y < tex.height; y++)
				{ // Fill channels
					Color col = tex.GetPixel(x, y);
					channelR[x, y] = col.r;
					channelG[x, y] = col.g;
					channelB[x, y] = col.b;
					channelA[x, y] = col.a;
				}
			}

			// Assign output channels
			channelRKnob.SetValue(new Channel(tex.name + "_R", channelR));
			channelGKnob.SetValue(new Channel(tex.name + "_G", channelG));
			channelBKnob.SetValue(new Channel(tex.name + "_B", channelB));
			channelAKnob.SetValue(new Channel(tex.name + "_A", channelA));

			return true;
		}
	}
}