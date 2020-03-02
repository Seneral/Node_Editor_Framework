using UnityEngine;
using NodeEditorFramework.Utilities;
using System.Collections.Generic;

namespace NodeEditorFramework 
{
	public enum ConnectionDrawMethod { Bezier, StraightLine }

	public static partial class NodeEditorGUI 
	{
		internal static bool isEditorWindow;

		// static GUI settings, textures and styles
		public static int knobSize = 16;

		public static bool useUnityEditorToolbar = false;

		public static Color NE_LightColor = new Color (0.4f, 0.4f, 0.4f);
		public static Color NE_TextColor = new Color(0.8f, 0.8f, 0.8f);

		public static Texture2D Background;
		public static Texture2D AALineTex;

		public static GUISkin nodeSkin { get { return overrideSkin ?? defaultSkin; } }
		public static GUISkin overrideSkin;
		public static GUISkin defaultSkin;
		public static GUISkin unitySkin;

		public static bool Init ()
		{
			List<GUIStyle> customStyles = new List<GUIStyle> (); 

			defaultSkin = ResourceManager.LoadResource<GUISkin> ("DefaultSkin.asset");
			if (defaultSkin == null) 
				return CreateDefaultSkin();
			else {
				defaultSkin = Object.Instantiate (defaultSkin);
				
				// Copy default editor styles, modified to fit custom style
				/*
				customStyles = new List<GUIStyle> (nodeSkin.customStyles);
				foreach (GUIStyle style in GUI.skin.customStyles)
				{
					if (nodeSkin.FindStyle(style.name) == null)
					{
						GUIStyle modStyle = new GUIStyle (style);
						modStyle.fontSize = nodeSkin.label.fontSize;
						modStyle.normal.textColor = modStyle.active.textColor = modStyle.focused.textColor = modStyle.hover.textColor = nodeSkin.label.normal.textColor;
						customStyles.Add (modStyle);
					}
				}
				nodeSkin.customStyles = customStyles.ToArray();*/

				Background = ResourceManager.LoadTexture ("Textures/background.png");
				AALineTex = ResourceManager.LoadTexture ("Textures/AALine.png");
				
				return Background && AALineTex;
			}
		}

		public static bool CreateDefaultSkin ()
		{
			// Textures
			Background = ResourceManager.LoadTexture ("Textures/background.png");
			AALineTex = ResourceManager.LoadTexture ("Textures/AALine.png");
			Texture2D GUIBox = ResourceManager.LoadTexture ("Textures/NE_Box.png");
			Texture2D GUISelectedBG = ResourceManager.LoadTexture ("Textures/NE_SelectedBG.png");
			Texture2D GUIButton = ResourceManager.LoadTexture ("Textures/NE_Button.png");
			Texture2D GUIButtonHover = ResourceManager.LoadTexture ("Textures/NE_Button_Hover.png");
			Texture2D GUIButtonSelected = ResourceManager.LoadTexture ("Textures/NE_Button_Selected.png");
			Texture2D GUIToolbar = ResourceManager.LoadTexture("Textures/NE_Toolbar.png");
			Texture2D GUIToolbarButton = ResourceManager.LoadTexture("Textures/NE_ToolbarButton.png");
			Texture2D GUIToolbarLabel = ResourceManager.LoadTexture("Textures/NE_ToolbarLabel.png");

			if (!Background || !AALineTex || !GUIBox || !GUIButton || !GUIToolbar || !GUIToolbarButton)
				return false;

			// Skin & Styles
			GUI.skin = null;
			defaultSkin = Object.Instantiate (GUI.skin);

			foreach (GUIStyle style in defaultSkin)
			{
				style.fontSize = 12;
				//style.normal.textColor = style.active.textColor = style.focused.textColor = style.hover.textColor = NE_TextColor;
			}

			List<GUIStyle> customStyles = new List<GUIStyle> (); 

			// Label
			defaultSkin.label.normal.textColor = NE_TextColor;
			customStyles.Add(new GUIStyle (defaultSkin.label) { name = "labelBold", fontStyle = FontStyle.Bold });
			customStyles.Add(new GUIStyle (defaultSkin.label) { name = "labelCentered", alignment = TextAnchor.MiddleCenter });
			customStyles.Add(new GUIStyle (defaultSkin.label) { name = "labelBoldCentered", alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
			customStyles.Add(new GUIStyle (defaultSkin.label) { name = "labelLeft", alignment = TextAnchor.MiddleLeft });
			customStyles.Add(new GUIStyle (defaultSkin.label) { name = "labelRight", alignment = TextAnchor.MiddleRight });
			GUIStyle labelSelected = new GUIStyle (defaultSkin.label) { name = "labelSelected" };
			labelSelected.normal.background = GUISelectedBG;
			customStyles.Add(labelSelected);

			// Box
			defaultSkin.box.normal.background = GUIBox;
			defaultSkin.box.normal.textColor = NE_TextColor;
			defaultSkin.box.active.textColor = NE_TextColor;
			customStyles.Add(new GUIStyle (defaultSkin.box) { name = "boxBold", fontStyle = FontStyle.Bold });

			// Button
			defaultSkin.button.normal.textColor = defaultSkin.button.active.textColor = defaultSkin.button.focused.textColor = defaultSkin.button.hover.textColor = NE_TextColor;
			defaultSkin.button.normal.background = GUIButton;
			defaultSkin.button.hover.background = GUIButtonHover;
			defaultSkin.button.active.background = GUIButtonSelected;
			defaultSkin.button.border = new RectOffset(1, 1, 1, 1);
			defaultSkin.button.margin = new RectOffset(2, 2, 1, 1);
			defaultSkin.button.padding = new RectOffset(4, 4, 1, 1);
			defaultSkin.button.fontSize = 12;

			// Toolbar
			if (useUnityEditorToolbar && defaultSkin.FindStyle("toolbar") != null && defaultSkin.FindStyle("toolbarButton") != null && defaultSkin.FindStyle("toolbarDropdown") != null)
			{
				customStyles.Add(new GUIStyle(defaultSkin.GetStyle("toolbar")));
				customStyles.Add(new GUIStyle(defaultSkin.GetStyle("toolbarButton")));
				customStyles.Add(new GUIStyle(defaultSkin.GetStyle("toolbarDropdown")));
				customStyles.Add(new GUIStyle(defaultSkin.GetStyle("toolbarButton")) { name = "toolbarLabel" });
			}
			else
			{ // No editor style - use custom skin
				GUIStyle toolbar = new GUIStyle(defaultSkin.box) { name = "toolbar" };
				toolbar.normal.background = GUIToolbar;
				toolbar.active.background = GUIToolbar;
				toolbar.border = new RectOffset(0, 0, 1, 1);
				toolbar.margin = new RectOffset(0, 0, 0, 0);
				toolbar.padding = new RectOffset(1, 1, 1, 1);
				toolbar.overflow = new RectOffset(0, 0, 0, 1);
				customStyles.Add(toolbar);

				GUIStyle toolbarLabel = new GUIStyle(defaultSkin.box) { name = "toolbarLabel" };
				toolbarLabel.normal.background = GUIToolbarLabel;
				toolbarLabel.border = new RectOffset(2, 2, 0, 0);
				toolbarLabel.margin = new RectOffset(0, 0, 0, 0);
				toolbarLabel.padding = new RectOffset(6, 6, 2, 2);
				customStyles.Add(toolbarLabel);

				GUIStyle toolbarButton = new GUIStyle(toolbarLabel) { name = "toolbarButton" };
				toolbarButton.normal.background = GUIToolbarButton;
				toolbarButton.border = new RectOffset(2, 2, 0, 0);
				toolbarButton.active.background = GUISelectedBG;
				customStyles.Add(toolbarButton);

				GUIStyle toolbarDropdown = new GUIStyle(toolbarButton) { name = "toolbarDropdown" };
				customStyles.Add(toolbarDropdown);
			}

			// Delete Editor-only resources
			defaultSkin.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
			defaultSkin.window.normal.background = null;
			defaultSkin.window.onNormal.background = null;


			defaultSkin.customStyles = customStyles.ToArray();
#if UNITY_EDITOR
			UnityEditor.AssetDatabase.CreateAsset(Object.Instantiate (nodeSkin), ResourceManager.resourcePath +  "DefaultSkin.asset");
#endif

			return true;
		}

		public static void StartNodeGUI (bool IsEditorWindow) 
		{
			NodeEditor.checkInit(true);

			isEditorWindow = IsEditorWindow;

			unitySkin = GUI.skin;
			GUI.skin = nodeSkin;
		}

		public static void EndNodeGUI () 
		{
			GUI.skin = unitySkin;
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
				OptimiseBezierDirections (startPos, ref startDir, endPos, ref endDir);
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