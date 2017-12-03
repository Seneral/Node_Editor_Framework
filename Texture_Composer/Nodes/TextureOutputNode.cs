using UnityEngine;
using System.IO;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework.TextureComposer
{
	[Node(false, "Texture/Output")]
	public class TextureOutputNode : Node
	{
		public const string ID = "texOutNode";
		public override string GetID { get { return ID; } }

		public override string Title { get { return "Texture Output"; } }
		public override Vector2 DefaultSize { get { return new Vector2(150, 50); } }
		public override bool AutoLayout { get { return true; } }

		[ValueConnectionKnob("Texture", Direction.In, "Texture")]
		public ValueConnectionKnob inputKnob;

		[System.NonSerialized]
		public Texture2D tex;

		public string savePath = null;

		public override void NodeGUI()
		{
			inputKnob.DisplayLayout();

			if (tex != null)
				RTTextureViz.DrawTexture(tex, 64);

			GUILayout.BeginHorizontal();
			RTEditorGUI.TextField(savePath);
#if UNITY_EDITOR
			if (GUILayout.Button("#", GUILayout.ExpandWidth (false)))
			{
				savePath = UnityEditor.EditorUtility.SaveFilePanel("Save Texture Path", Application.dataPath, "OutputTex", "png");
			}
#endif
			GUILayout.EndHorizontal();

			if (GUI.changed)
				NodeEditor.curNodeCanvas.OnNodeChange(this);
		}

		public override bool Calculate()
		{
			tex = inputKnob.connected() ? inputKnob.GetValue<Texture2D>() : null;
			if (!string.IsNullOrEmpty(savePath))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(savePath));
				Debug.Log("Saving to '" + savePath + "'!");
				File.WriteAllBytes(savePath, tex.EncodeToPNG());
#if UNITY_EDITOR
				UnityEditor.AssetDatabase.Refresh();
#endif
			}
			return true;
		}
	}
}
