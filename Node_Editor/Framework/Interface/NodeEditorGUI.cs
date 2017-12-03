using UnityEngine;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework 
{
	public enum ConnectionDrawMethod { Bezier, StraightLine }

	public static partial class NodeEditorGUI 
	{
		internal static bool isEditorWindow;

		// static GUI settings, textures and styles
		public static int knobSize = 16;

		public static Color NE_LightColor = new Color (0.4f, 0.4f, 0.4f);
		public static Color NE_TextColor = new Color(0.8f, 0.8f, 0.8f);

		public static Texture2D Background;
		public static Texture2D AALineTex;
		public static Texture2D GUIBox;
		public static Texture2D GUIButton;
		public static Texture2D GUIBoxSelection;
		public static Texture2D GUIToolbar;
		public static Texture2D GUIToolbarButton;

		public static GUISkin nodeSkin;
		public static GUISkin defaultSkin;

		public static GUIStyle nodeLabel;
		public static GUIStyle nodeLabelBold;
		public static GUIStyle nodeLabelSelected;
		public static GUIStyle nodeLabelCentered;
		public static GUIStyle nodeLabelBoldCentered;
		public static GUIStyle nodeLabelLeft;
		public static GUIStyle nodeLabelRight;

		public static GUIStyle nodeBox;
		public static GUIStyle nodeBoxBold;

		public static GUIStyle toolbar;
		public static GUIStyle toolbarLabel;
		public static GUIStyle toolbarDropdown;
		public static GUIStyle toolbarButton;

		public static bool Init ()
		{
			// Textures
			Background = ResourceManager.LoadTexture ("Textures/background.png");
			AALineTex = ResourceManager.LoadTexture ("Textures/AALine.png");
			GUIBox = ResourceManager.LoadTexture ("Textures/NE_Box.png");
			GUIButton = ResourceManager.LoadTexture ("Textures/NE_Button.png");
			//GUIBoxSelection = ResourceManager.LoadTexture("Textures/BoxSelection.png");
			GUIToolbar = ResourceManager.LoadTexture("Textures/NE_Toolbar.png");
			GUIToolbarButton = ResourceManager.LoadTexture("Textures/NE_ToolbarButton.png");

			if (!Background || !AALineTex || !GUIBox || !GUIButton || !GUIToolbar || !GUIToolbarButton)
				return false;

			// Skin & Styles
			nodeSkin = Object.Instantiate (GUI.skin);
			GUI.skin = nodeSkin;

			foreach (GUIStyle style in GUI.skin)
			{
				style.fontSize = 11;
				//style.normal.textColor = style.active.textColor = style.focused.textColor = style.hover.textColor = NE_TextColor;
			}

			// Label
			nodeSkin.label.normal.textColor = NE_TextColor;
			nodeLabel = nodeSkin.label;
			nodeLabelBold = new GUIStyle (nodeLabel) { fontStyle = FontStyle.Bold };
			nodeLabelSelected = new GUIStyle (nodeLabel);
			nodeLabelSelected.normal.background = RTEditorGUI.ColorToTex (1, NE_LightColor);
			nodeLabelCentered = new GUIStyle (nodeLabel) { alignment = TextAnchor.MiddleCenter };
			nodeLabelBoldCentered = new GUIStyle (nodeLabelBold) { alignment = TextAnchor.MiddleCenter };
			nodeLabelLeft = new GUIStyle (nodeLabel) { alignment = TextAnchor.MiddleLeft };
			nodeLabelRight = new GUIStyle (nodeLabel) { alignment = TextAnchor.MiddleRight };

			// Box
			nodeSkin.box.normal.background = GUIBox;
			nodeSkin.box.normal.textColor = NE_TextColor;
			nodeSkin.box.active.textColor = NE_TextColor;
			nodeBox = nodeSkin.box;
			nodeBoxBold = new GUIStyle (nodeBox) { fontStyle = FontStyle.Bold };

			// Button
			nodeSkin.button.normal.textColor = NE_TextColor;
			nodeSkin.button.normal.background = GUIButton;

			// Toolbar
			toolbar = GUI.skin.FindStyle("toolbar");
			toolbarButton = GUI.skin.FindStyle("toolbarButton");
			toolbarLabel = GUI.skin.FindStyle("toolbarButton");
			toolbarDropdown = GUI.skin.FindStyle("toolbarDropdown");
			if (toolbar == null || toolbarButton == null || toolbarLabel == null || toolbarDropdown == null)
			{ // No editor styles available - use custom skin
				toolbar = new GUIStyle(nodeSkin.box);
				toolbar.normal.background = GUIToolbar;
				toolbar.active.background = GUIToolbar;
				toolbar.border = new RectOffset(0, 0, 1, 1);
				toolbar.margin = new RectOffset(0, 0, 0, 0);
				toolbar.padding = new RectOffset(10, 10, 1, 1);

				toolbarLabel = new GUIStyle(nodeSkin.box);
				toolbarLabel.normal.background = GUIToolbarButton;
				toolbarLabel.border = new RectOffset(2, 2, 0, 0);
				toolbarLabel.margin = new RectOffset(-2, -2, 0, 0);
				toolbarLabel.padding = new RectOffset(6, 6, 4, 4);

				toolbarButton = new GUIStyle(toolbarLabel);
				toolbarButton.active.background = RTEditorGUI.ColorToTex(1, NE_LightColor);

				toolbarDropdown = new GUIStyle(toolbarButton);
			}
			GUI.skin = null;

			return true;
		}

		public static void StartNodeGUI (bool IsEditorWindow) 
		{
			NodeEditor.checkInit(true);

			isEditorWindow = IsEditorWindow;

			defaultSkin = GUI.skin;
			if (nodeSkin != null)
				GUI.skin = nodeSkin;
		}

		public static void EndNodeGUI () 
		{
			GUI.skin = defaultSkin;
		}

		#region Connection Drawing

		// Curve parameters
		public static float curveBaseDirection = 1.5f, curveBaseStart = 2f, curveDirectionScale = 0.004f;

		/// <summary>
		/// Draws a node connection from start to end, horizontally
		/// </summary>
		public static void DrawConnection (Vector2 startPos, Vector2 endPos, Color col) 
		{
			Vector2 startVector = startPos.x <= endPos.x? Vector2.right : Vector2.left;
			DrawConnection (startPos, startVector, endPos, -startVector, col);
		}

		/// <summary>
		/// Draws a node connection from start to end, horizontally
		/// </summary>
		public static void DrawConnection (Vector2 startPos, Vector2 endPos, ConnectionDrawMethod drawMethod, Color col) 
		{
			Vector2 startVector = startPos.x <= endPos.x? Vector2.right : Vector2.left;
			DrawConnection (startPos, startVector, endPos, -startVector, drawMethod, col);
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
				NodeEditorGUI.OptimiseBezierDirections (startPos, ref startDir, endPos, ref endDir);
				float dirFactor = 80;//Mathf.Pow ((startPos-endPos).magnitude, 0.3f) * 20;
				//Debug.Log ("DirFactor is " + dirFactor + "with a bezier lenght of " + (startPos-endPos).magnitude);
				RTEditorGUI.DrawBezier (startPos, endPos, startPos + startDir * dirFactor, endPos + endDir * dirFactor, col * Color.gray, null, 3);
			}
			else if (drawMethod == ConnectionDrawMethod.StraightLine)
				RTEditorGUI.DrawLine (startPos, endPos, col * Color.gray, null, 3);
		}

		/// <summary>
		/// Optimises the bezier directions scale so that the bezier looks good in the specified position relation.
		/// Only the magnitude of the directions are changed, not their direction!
		/// </summary>
		public static void OptimiseBezierDirections (Vector2 startPos, ref Vector2 startDir, Vector2 endPos, ref Vector2 endDir) 
		{
			Vector2 offset = (endPos - startPos) * curveDirectionScale;
			float baseDir = Mathf.Min (offset.magnitude/curveBaseStart, 1) * curveBaseDirection;
			Vector2 scale = new Vector2 (Mathf.Abs (offset.x) + baseDir, Mathf.Abs (offset.y) + baseDir);
			// offset.x and offset.y linearly increase at scale of curveDirectionScale
			// For 0 < offset.magnitude < curveBaseStart, baseDir linearly increases from 0 to curveBaseDirection. For offset.magnitude > curveBaseStart, baseDir = curveBaseDirection
			startDir = Vector2.Scale(startDir.normalized, scale);
			endDir = Vector2.Scale(endDir.normalized, scale);
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

		/// <summary>
		/// Unified method to generate a random HSV color value across versions
		/// </summary>
		public static Color RandomColorHSV(int seed, float hueMin, float hueMax, float saturationMin, float saturationMax, float valueMin, float valueMax)
		{
			// Set seed
#if UNITY_5_4_OR_NEWER
			UnityEngine.Random.InitState (seed);
#else
			UnityEngine.Random.seed = seed;
#endif
			// Consistent random H,S,V values
			float hue = UnityEngine.Random.Range(hueMin, hueMax);
			float saturation = UnityEngine.Random.Range(saturationMin, saturationMax);
			float value = UnityEngine.Random.Range(valueMin, valueMax);

			// Convert HSV to RGB
#if UNITY_5_3_OR_NEWER
			return UnityEngine.Color.HSVToRGB (hue, saturation, value, false);
#else
			int hi = Mathf.FloorToInt(hue / 60) % 6;
			float frac = hue / 60 - Mathf.Floor(hue / 60);

			float v = value;
			float p = value * (1 - saturation);
			float q = value * (1 - frac * saturation);
			float t = value * (1 - (1 - frac) * saturation);

			if (hi == 0)
				return new Color(v, t, p);
			else if (hi == 1)
				return new Color(q, v, p);
			else if (hi == 2)
				return new Color(p, v, t);
			else if (hi == 3)
				return new Color(p, q, v);
			else if (hi == 4)
				return new Color(t, p, v);
			else
				return new Color(v, p, q);
#endif
		}
	}
}