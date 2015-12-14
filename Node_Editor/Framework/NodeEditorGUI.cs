using UnityEngine;
using System.Collections;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework 
{
	public static class NodeEditorGUI 
	{
		// static GUI settings, textures and styles
		public static int knobSize = 16;

		public static Color NE_LightColor = new Color (0.4f, 0.4f, 0.4f);
		public static Color NE_TextColor = new Color (0.7f, 0.7f, 0.7f);

		public static Texture2D Background;
		public static Texture2D AALineTex;
		public static Texture2D GUIBox;
		public static Texture2D GUIButton;

		public static GUISkin nodeSkin;
		public static GUISkin defaultSkin;

		public static GUIStyle nodeLabel;
		public static GUIStyle nodeLabelBold;
		public static GUIStyle nodeLabelSelected;
		
		public static bool Init (bool GUIFunction) 
		{
			// Textures
			Background = ResourceManager.LoadTexture ("Textures/background.png");
			AALineTex = ResourceManager.LoadTexture ("Textures/AALine.png");
			GUIBox = ResourceManager.LoadTexture ("Textures/NE_Box.png");
			GUIButton = ResourceManager.LoadTexture ("Textures/NE_Button.png");
			
			if (!Background || !AALineTex || !GUIBox || !GUIButton)
				return false;
			if (!GUIFunction)
				return true;

			// Skin & Styles
			nodeSkin = Object.Instantiate<GUISkin> (GUI.skin);

			// Label
			nodeSkin.label.normal.textColor = NE_TextColor;
			nodeLabel = nodeSkin.label;
			// Box
			nodeSkin.box.normal.textColor = NE_TextColor;
			nodeSkin.box.normal.background = GUIBox;
			// Button
			nodeSkin.button.normal.textColor = NE_TextColor;
			nodeSkin.button.normal.background = GUIButton;
			// TextArea
			nodeSkin.textArea.normal.background = GUIBox;
			nodeSkin.textArea.active.background = GUIBox;
			// Bold Label
			nodeLabelBold = new GUIStyle (nodeLabel);
			nodeLabelBold.fontStyle = FontStyle.Bold;
			// Selected Label
			nodeLabelSelected = new GUIStyle (nodeLabel);
			nodeLabelSelected.normal.background = RTEditorGUI.ColorToTex (1, NE_LightColor);

			return true;
		}

		public static void StartNodeGUI () 
		{
			defaultSkin = GUI.skin;
			if (nodeSkin == null)
				Init (true);
			GUI.skin = nodeSkin;
		}

		public static void EndNodeGUI () 
		{
			GUI.skin = defaultSkin;
		}
	}
}