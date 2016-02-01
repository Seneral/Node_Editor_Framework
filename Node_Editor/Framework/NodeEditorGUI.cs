using UnityEngine;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework 
{
	public static class NodeEditorGUI 
	{
		// static GUI settings, textures and styles
		public static int KnobSize = 16;

		public static Color NELightColor = new Color (0.4f, 0.4f, 0.4f);
		public static Color NETextColor = new Color (0.7f, 0.7f, 0.7f);

		public static Texture2D Background;
		public static Texture2D AALineTex;
		public static Texture2D GUIBox;
		public static Texture2D GUIButton;

		public static GUISkin NodeSkin;
		public static GUISkin DefaultSkin;

		public static GUIStyle NodeLabel;
		public static GUIStyle NodeLabelBold;
		public static GUIStyle NodeLabelSelected;

		public static GUIStyle NodeBox;
		public static GUIStyle NodeBoxBold;
		
		public static bool Init (bool guiFunction) 
		{
			// Textures
			Background = ResourceManager.LoadTexture ("Textures/background.png");
			AALineTex = ResourceManager.LoadTexture ("Textures/AALine.png");
			GUIBox = ResourceManager.LoadTexture ("Textures/NE_Box.png");
			GUIButton = ResourceManager.LoadTexture ("Textures/NE_Button.png");
			
			if (!Background || !AALineTex || !GUIBox || !GUIButton)
				return false;
			if (!guiFunction)
				return true;

			// Skin & Styles
			NodeSkin = Object.Instantiate (GUI.skin);

			// Label
			NodeSkin.label.normal.textColor = NETextColor;
			NodeLabel = NodeSkin.label;
			// Box
			NodeSkin.box.normal.textColor = NETextColor;
			NodeSkin.box.normal.background = GUIBox;
			NodeBox = NodeSkin.box;
			// Button
			NodeSkin.button.normal.textColor = NETextColor;
			NodeSkin.button.normal.background = GUIButton;
			// TextArea
			NodeSkin.textArea.normal.background = GUIBox;
			NodeSkin.textArea.active.background = GUIBox;
			// Bold Label
			NodeLabelBold = new GUIStyle (NodeLabel);
			NodeLabelBold.fontStyle = FontStyle.Bold;
			// Selected Label
			NodeLabelSelected = new GUIStyle (NodeLabel);
			NodeLabelSelected.normal.background = RTEditorGUI.ColorToTex (1, NELightColor);
			// Bold Box
			NodeBoxBold = new GUIStyle (NodeBox);
			NodeBoxBold.fontStyle = FontStyle.Bold;

			return true;
		}

		public static void StartNodeGUI () 
		{
			DefaultSkin = GUI.skin;
			if (NodeSkin == null)
				Init (true);
			GUI.skin = NodeSkin;
		}

		public static void EndNodeGUI () 
		{
			GUI.skin = DefaultSkin;
		}

		#region Connection Drawing

		/// <summary>
		/// Draws a node connection from start to end, horizontally
		/// </summary>
		public static void DrawConnection (Vector2 startPos, Vector2 endPos, Color col) 
		{
			Vector2 startVector = startPos.x <= endPos.x? Vector2.right : Vector2.left;
			DrawConnection (startPos, startVector, endPos, -startVector, col);
		}
		/// <summary>
		/// Draws a node connection from start to end with specified vectors
		/// </summary>
		public static void DrawConnection (Vector2 startPos, Vector2 startDir, Vector2 endPos, Vector2 endDir, Color col) 
		{
			#if NODE_EDITOR_LINE_CONNECTION
			DrawConnection (startPos, startDir, endPos, endDir, ConnectionDrawMethod.StraightLine, col);
			#else
			DrawConnection (startPos, startDir, endPos, endDir, ConnectionDrawMethod.Bezier, col);
			#endif
		}
		/// <summary>
		/// Draws a node connection from start to end with specified vectors
		/// </summary>
		public static void DrawConnection (Vector2 startPos, Vector2 startDir, Vector2 endPos, Vector2 endDir, ConnectionDrawMethod drawMethod, Color col) 
		{
			if (drawMethod == ConnectionDrawMethod.Bezier) 
			{
				float dirFactor = 80;//Mathf.Pow ((startPos-endPos).magnitude, 0.3f) * 20;
				//Debug.Log ("DirFactor is " + dirFactor + "with a bezier lenght of " + (startPos-endPos).magnitude);
				RTEditorGUI.DrawBezier (startPos, endPos, startPos + startDir * dirFactor, endPos + endDir * dirFactor, col * Color.gray, null, 3);
			}
			else if (drawMethod == ConnectionDrawMethod.StraightLine)
				RTEditorGUI.DrawLine (startPos, endPos, col * Color.gray, null, 3);
		}

		/// <summary>
		/// Gets the second connection vector that matches best, accounting for positions
		/// </summary>
		internal static Vector2 GetSecondConnectionVector (Vector2 startPos, Vector2 endPos, Vector2 firstVector) 
		{
			if (firstVector.x != 0 && firstVector.y == 0)
				return startPos.x <= endPos.x? -firstVector : firstVector;
			else if (firstVector.y != 0 && firstVector.x == 0)
				return startPos.y <= endPos.y? -firstVector : firstVector;
			else
				return -firstVector;
		}

		#endregion
	}
}