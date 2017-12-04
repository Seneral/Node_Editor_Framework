using UnityEngine;

namespace NodeEditorFramework.TextureComposer
{
	[Node(false, "Texture/Bundle")]
	public class ChannelBundleNode : Node
	{
		public const string ID = "channelBundleNode";
		public override string GetID { get { return ID; } }

		public override string Title { get { return "Channel Bundle"; } }
		public override Vector2 DefaultSize { get { return new Vector2(150, 100); } }

		[ValueConnectionKnob("Channel R", Direction.In, "Channel")]
		public ValueConnectionKnob channelRKnob;
		[ValueConnectionKnob("Channel G", Direction.In, "Channel")]
		public ValueConnectionKnob channelGKnob;
		[ValueConnectionKnob("Channel B", Direction.In, "Channel")]
		public ValueConnectionKnob channelBKnob;
		[ValueConnectionKnob("Channel A", Direction.In, "Channel")]
		public ValueConnectionKnob channelAKnob;

		[ValueConnectionKnob("Bundled", Direction.Out, "Texture")]
		public ValueConnectionKnob bundledKnob;

		public override void NodeGUI()
		{
			GUILayout.BeginHorizontal();

			GUILayout.BeginVertical();
			channelRKnob.DisplayLayout(new GUIContent("Channel R", "The R channel of the bundled texture"));
			channelGKnob.DisplayLayout(new GUIContent("Channel G", "The G channel of the bundled texture"));
			channelBKnob.DisplayLayout(new GUIContent("Channel B", "The B channel of the bundled texture"));
			channelAKnob.DisplayLayout(new GUIContent("Channel A", "The A channel of the bundled texture"));
			GUILayout.EndVertical();

			bundledKnob.DisplayLayout(new GUIContent("Bundled", "The bundled texture"));

			GUILayout.EndHorizontal();

			if (GUI.changed)
				NodeEditor.curNodeCanvas.OnNodeChange(this);
		}

		public override bool Calculate()
		{
			Channel RChannel = channelRKnob.connected() ? channelRKnob.GetValue<Channel>() : null;
			Channel GChannel = channelGKnob.connected() ? channelGKnob.GetValue<Channel>() : null;
			Channel BChannel = channelBKnob.connected() ? channelBKnob.GetValue<Channel>() : null;
			Channel AChannel = channelAKnob.connected() ? channelAKnob.GetValue<Channel>() : null;

			if (RChannel == null && GChannel == null && BChannel == null)
			{
				bundledKnob.ResetValue();
				return true;
			}

			int width = Mathf.Max(new int[] { RChannel != null ? RChannel.width : 0, GChannel != null ? GChannel.width : 0, BChannel != null ? BChannel.width : 0, AChannel != null ? AChannel.width : 0 });
			int height = Mathf.Max(new int[] { RChannel != null ? RChannel.height : 0, GChannel != null ? GChannel.height : 0, BChannel != null ? BChannel.height : 0, AChannel != null ? AChannel.height : 0 });

			Texture2D bundled = new Texture2D(width, height, AChannel != null ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
			bundled.name = "Bundled Tex";

			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					bundled.SetPixel(x, y, new Color(RChannel != null ? RChannel.GetValue(x, y, width, height) : 0,
													   GChannel != null ? GChannel.GetValue(x, y, width, height) : 0,
													   BChannel != null ? BChannel.GetValue(x, y, width, height) : 0,
													   AChannel != null ? AChannel.GetValue(x, y, width, height) : 1));
				}
			}

			bundled.Apply();
			bundledKnob.SetValue(bundled);
			return true;
		}
	}

}