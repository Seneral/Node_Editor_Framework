using UnityEngine;
using System.Collections;
using NodeEditorFramework;

namespace NodeEditorFramework 
{
	public static class NodeEditorGUI 
	{
		// Static textures and styles
		public static Texture2D Background;
		public static Texture2D AALineTex;
		public static Texture2D GUIBox;
		public static Texture2D GUIButton;

		public static GUISkin nodeSkin;
		public static GUISkin defaultSkin;

		public static GUIStyle nodeLabel;
		public static GUIStyle nodeLabelBold;
		public static GUIStyle nodeLabelSelected;
		
		public static bool Init () 
		{
			Background = LoadTexture ("Textures/background.png");
			AALineTex = LoadTexture ("Textures/AALine.png");
			GUIBox = LoadTexture ("Textures/NE_Box.png");
			GUIButton = LoadTexture ("Textures/NE_Button.png");
			
			if (!Background || !AALineTex || !GUIBox || !GUIButton)
				return false;
			
			// Styles

			nodeSkin = Object.Instantiate<GUISkin> (GUI.skin);

			nodeSkin.label.normal.textColor = new Color (0.7f, 0.7f, 0.7f);

			nodeSkin.box.normal.textColor = new Color (0.7f, 0.7f, 0.7f);
			nodeSkin.box.normal.background = GUIBox;

			nodeSkin.button.normal.textColor = new Color (0.7f, 0.7f, 0.7f);
			nodeSkin.button.normal.background = GUIButton;

			nodeSkin.textArea.normal.background = GUIBox;
			nodeSkin.textArea.active.background = GUIBox;

			nodeLabel = nodeSkin.label;

			nodeLabelBold = new GUIStyle (nodeLabel);
			nodeLabelBold.fontStyle = FontStyle.Bold;

			nodeLabelSelected = new GUIStyle (nodeLabel);
			nodeLabelSelected.normal.background = GUIExt.ColorToTex (new Color (0.4f, 0.4f, 0.4f));

			return true;
		}

		public static void StartNodeGUI () 
		{
			defaultSkin = GUI.skin;
			GUI.skin = nodeSkin;
		}

		public static void EndNodeGUI () 
		{
			GUI.skin = defaultSkin;
		}
		
		/// <summary>
		/// Draws a Bezier curve just as UnityEditor.Handles.DrawBezier
		/// </summary>
		public static void DrawBezier (Vector2 startPos, Vector2 endPos, Vector2 startTan, Vector2 endTan, Color col, Texture2D tex, float width)
		{
			if (Event.current.type != EventType.Repaint)
				return;
			
			if (tex == null)
				tex = AALineTex;
			tex = col == Color.white? tex : Tint (tex, col);
			
			int segmentCount = (int)(((startPos-startTan).magnitude + (startTan-endTan).magnitude + (endTan-endPos).magnitude) / 10);
			Vector2 curPoint = startPos;
			for (int segCnt = 1; segCnt <= segmentCount; segCnt++) 
			{
				float t = (float)segCnt/segmentCount;
				Vector2 nextPoint = new Vector2 (startPos.x * Mathf.Pow (1-t, 3) + 
				                                 startTan.x * 3 * Mathf.Pow (1-t, 2) * t + 
				                                 endTan.x 	* 3 * (1-t) * Mathf.Pow (t, 2) + 
				                                 endPos.x 	* Mathf.Pow (t, 3),
				                                 
				                                 startPos.y * Mathf.Pow (1-t, 3) + 
				                                 startTan.y * 3 * Mathf.Pow (1-t, 2) * t + 
				                                 endTan.y 	* 3 * (1-t) * Mathf.Pow (t, 2) + 
				                                 endPos.y 	* Mathf.Pow (t, 3));
				DrawLine (curPoint, nextPoint, Color.white, tex, width);
				curPoint = nextPoint;
			}
		}
		
		public static void DrawLine (Vector2 startPos, Vector2 endPos, Color col, Texture2D tex, float width)
		{
			if (Event.current.type != EventType.Repaint)
				return;
			
			if (width == 1)
			{
				GL.Begin (GL.LINES);
				GL.Color (col);
				GL.Vertex (startPos);
				GL.Vertex (endPos);
				GL.End ();
			}
			else 
			{
				if (tex == null)
					tex = AALineTex;
				tex = col == Color.white? tex : Tint (tex, col);
				
				Vector2 perpWidthOffset = new Vector2 ((endPos-startPos).y, -(endPos-startPos).x).normalized * width / 2;
				
				Material mat = new Material (Shader.Find ("Unlit/Transparent"));
				mat.SetTexture ("_MainTex", tex);
				mat.SetPass (0);
				
				GL.Begin (GL.TRIANGLE_STRIP);
				GL.TexCoord2 (0, 0);
				GL.Vertex (startPos - perpWidthOffset);
				GL.TexCoord2 (0, 1);
				GL.Vertex (startPos + perpWidthOffset);
				GL.TexCoord2 (1, 0);
				GL.Vertex (endPos - perpWidthOffset);
				GL.TexCoord2 (1, 1);
				GL.Vertex (endPos + perpWidthOffset);
				GL.End ();
			}
		}
		
		/// <summary>
		/// Loads a texture in both the editor and at runtime (coming soon)
		/// </summary>
		public static Texture2D LoadTexture (string texPath)
		{
			#if UNITY_EDITOR
			string fullPath = System.IO.Path.Combine (NodeEditor.resourcePath, texPath);
			Texture2D tex = UnityEditor.AssetDatabase.LoadAssetAtPath (fullPath, typeof (Texture2D)) as Texture2D;
			if (tex == null)
				Debug.LogError (string.Format ("NodeEditor: Texture not found at '{0}', did you install Node_Editor correctly in the 'Plugins' folder?", fullPath));
			return tex;
			#else
			texPath = texPath.Split ('.') [0];
			Texture2D tex = Resources.Load<Texture2D> (texPath);
			if (tex == null)
				Debug.LogError (string.Format ("NodeEditor: Texture not found at '{0}' in any Resource Folder!", texPath));
			return tex;
			#endif
		}

		/// <summary>
		/// 
		/// </summary>
		public static T LoadResource<T> (string path) where T : Object
		{
//			#if UNITY_EDITOR
//			string fullPath = System.IO.Path.Combine (NodeEditor.resourcePath, path);
//			T obj = UnityEditor.AssetDatabase.LoadAssetAtPath (fullPath, typeof (T)) as T;
//			if (obj == null)
//				Debug.LogError (string.Format ("NodeEditor: Resource not found at '{0}', did you install Node_Editor correctly in the 'Plugins' folder?", fullPath));
//			#else
			path = path.Split ('.') [0];
			T obj = Resources.Load<T> (path);
			if (obj == null)
				Debug.LogError (string.Format ("NodeEditor: Resource not found at '{0}' in any Resource Folder!", path));
//			#endif
			return obj;
		}
		
		/// <summary>
		/// Create a 1x1 tex with color col
		/// </summary>
		public static Texture2D ColorToTex (Color col) 
		{
			Texture2D tex = new Texture2D (1, 1);
			tex.SetPixel (1, 1, col);
			tex.Apply ();
			return tex;
		}
		
		/// <summary>
		/// Tint the texture with the color.
		/// </summary>
		public static Texture2D Tint (Texture2D tex, Color color) 
		{
			Texture2D tintedTex = UnityEngine.Object.Instantiate (tex);
			for (int x = 0; x < tex.width; x++) 
				for (int y = 0; y < tex.height; y++) 
					tintedTex.SetPixel (x, y, tex.GetPixel (x, y) * color);
			tintedTex.Apply ();
			return tintedTex;
		}

		public static Texture2D RotateTexture90Degrees (Texture2D tex) 
		{
			if (tex == null)
				return null;
			int width = tex.width, height = tex.height;
			Color[] col = tex.GetPixels ();
			Color[] rotatedCol = new Color[width*height];
			for (int x = 0; x < width; x++) 
			{
				for (int y = 0; y < height; y++) 
				{
					rotatedCol[x*width + y] = col[(width-y-1) * width + x];
				}
			}
			tex = new Texture2D (width, height, tex.format, tex.mipmapCount != 0);
			tex.SetPixels (rotatedCol);
			tex.Apply ();
			return tex;
		}
		
	}

}