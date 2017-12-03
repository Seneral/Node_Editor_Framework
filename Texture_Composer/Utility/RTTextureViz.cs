using UnityEngine;

namespace NodeEditorFramework.TextureComposer
{
	public static class RTTextureViz
	{
		private static Material texVizMat;

		#region GUI Layout

		/// <summary> Draw colored texture. </summary>
		public static void DrawTexture(Texture tex, int size, bool alpha = false, GUIStyle style = null, params GUILayoutOption[] options)
		{
			DrawTexture(tex, size, 1, 2, 3, alpha ? 4 : 5, Color.white, style, options);
		}
		/// <summary> Draw colored texture. </summary>
		public static void DrawTexture(Texture tex, int size, Color tint, bool alpha = false, GUIStyle style = null, params GUILayoutOption[] options)
		{
			DrawTexture(tex, size, 1, 2, 3, alpha ? 4 : 5, tint, style, options);
		}

		/// <summary> Draw grayscale texture. </summary>
		public static void DrawTexture(Texture tex, int size, float grayscale, bool alpha = false, GUIStyle style = null, params GUILayoutOption[] options)
		{
			Rect texRect = getTexRect(size, tex, style, options);
			DrawTexture(tex, texRect, grayscale, alpha, style);
		}

		/// <summary> Draw Texture with Channel Shuffle; Values: 0=black - 1=red - 2=green - 3=blue - 4=alpha - 5=white </summary>
		public static void DrawTexture(Texture tex, int size, int shuffleR, int shuffleG, int shuffleB, int shuffleA, GUIStyle style = null, params GUILayoutOption[] options)
		{
			DrawTexture(tex, size, shuffleR, shuffleG, shuffleB, shuffleA, Color.white, style, options);
		}
		/// <summary> Draw Texture with Channel Shuffle; Values: 0=black - 1=red - 2=green - 3=blue - 4=alpha - 5=white </summary>
		public static void DrawTexture(Texture tex, int size, int shuffleR, int shuffleG, int shuffleB, int shuffleA, Color tint, GUIStyle style = null, params GUILayoutOption[] options)
		{
			Rect texRect = getTexRect(size, tex, style, options);
			DrawTexture(tex, texRect, shuffleR, shuffleG, shuffleB, shuffleA, tint, style);
		}

		private static Rect getTexRect(int size, Texture tex, GUIStyle style, params GUILayoutOption[] options)
		{
			if (options == null || options.Length == 0)
				options = new GUILayoutOption[] { GUILayout.ExpandWidth(false) };
			float aspect = tex == null || tex.height <= 0 || tex.width <= 0 ? 1 : (tex.height / tex.width);
			Rect rect = style == null ?
				GUILayoutUtility.GetRect(size, size * aspect, options) :
				GUILayoutUtility.GetRect(size, size * aspect, style, options);
			return rect;
		}

		#endregion

		#region Legacy GUI

		/// <summary> Draw colored texture. </summary>
		public static void DrawTexture(Texture tex, Rect rect, bool alpha = false, GUIStyle style = null)
		{
			DrawTexture(tex, rect, 1, 2, 3, alpha ? 4 : 5, Color.white);
		}
		/// <summary> Draw colored texture. </summary>
		public static void DrawTexture(Texture tex, Rect rect, Color tint, bool alpha = false, GUIStyle style = null)
		{
			DrawTexture(tex, rect, 1, 2, 3, alpha ? 4 : 5, tint);
		}

		/// <summary> Draw grayscale texture. </summary>
		public static void DrawTexture(Texture tex, Rect rect, float grayscale, bool alpha = false, GUIStyle style = null)
		{
			if (tex == null)
				return;
			if (Event.current.type == EventType.Repaint)
			{
				AssureTexVizMaterial();

				texVizMat.EnableKeyword("GRAYSCALE");
				texVizMat.SetFloat("_grayscale", grayscale);
				texVizMat.SetInt("_alpha", alpha ? 1 : 0);

				if (style != null)
					GUI.Box(rect, GUIContent.none, style);
				Graphics.DrawTexture(rect, tex, texVizMat);

				texVizMat.DisableKeyword("GRAYSCALE");
			}
		}

		/// <summary> Channel Shuffle Values: 0-black - 1-red - 2-green - 3-blue - 4-alpha - 5-white  </summary>
		public static void DrawTexture(Texture tex, Rect rect, int shuffleR, int shuffleG, int shuffleB, int shuffleA, GUIStyle style = null)
		{
			DrawTexture(tex, rect, shuffleR, shuffleG, shuffleB, shuffleA, Color.white, style);
		}
		/// <summary> Channel Shuffle Values: 0-black - 1-red - 2-green - 3-blue - 4-alpha - 5-white  </summary>
		public static void DrawTexture(Texture tex, Rect rect, int shuffleR, int shuffleG, int shuffleB, int shuffleA, Color tint, GUIStyle style = null)
		{
			if (tex == null)
				return;
			if (Event.current.type == EventType.Repaint)
			{
				AssureTexVizMaterial();

				texVizMat.SetInt("shuffleR", shuffleR);
				texVizMat.SetInt("shuffleG", shuffleG);
				texVizMat.SetInt("shuffleB", shuffleB);
				texVizMat.SetInt("shuffleA", shuffleA);
				texVizMat.SetColor("tintColor", tint);

				if (style != null)
					GUI.Box(rect, GUIContent.none, style);
				Graphics.DrawTexture(rect, tex, texVizMat);
			}
		}

		private static void AssureTexVizMaterial()
		{
			if (texVizMat == null)
			{
				Shader texVizShader = Shader.Find("Hidden/GUITextureClip_ChannelControl");
				if (texVizShader == null)
					throw new System.NotImplementedException("Missing texture visualization shader implementation!");
				texVizMat = new Material(texVizShader);
			}
		}

		#endregion
	}
}