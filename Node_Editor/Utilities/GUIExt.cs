using UnityEngine;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;

namespace NodeEditorFramework.Utilities 
{
	public static class RTEditorGUI 
	{

		/// <summary>
		/// Mimic's UnityEditor.EditorGUILayout.TextField in taking a label and a string and returning the edited string.
		/// </summary>
		public static string TextField (GUIContent label, string text)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Label (label, label != GUIContent.none? GUILayout.ExpandWidth (true) : GUILayout.ExpandWidth (false));
			text = GUILayout.TextField (text);
			GUILayout.EndHorizontal ();
			return text;
		}


		private static int activeFloatField = -1;
		private static float activeFloatFieldLastValue = 0;
		private static string activeFloatFieldString = "";
		/// <summary>
		/// Float Field for ingame purposes. Behaves exactly like UnityEditor.EditorGUILayout.FloatField
		/// </summary>
		public static float FloatField (float value)
		{
			// Get rect and control for this float field for identification
			Rect pos = GUILayoutUtility.GetRect (new GUIContent (value.ToString ()), GUI.skin.label, new GUILayoutOption[] { GUILayout.ExpandWidth (false), GUILayout.MinWidth (40) });

			int floatFieldID = GUIUtility.GetControlID ("FloatField".GetHashCode (), FocusType.Keyboard, pos) + 1;
			if (floatFieldID == 0)
				return value;

			bool recorded = activeFloatField == floatFieldID;
			bool active = floatFieldID == GUIUtility.keyboardControl;

			if (active && recorded && activeFloatFieldLastValue != value)
			{ // Value has been modified externally
				activeFloatFieldLastValue = value;
				activeFloatFieldString = value.ToString ();
			}

			// Get stored string for the text field if this one is recorded
			string str = recorded? activeFloatFieldString : value.ToString ();

			string strValue = GUI.TextField (pos, str);
			if (recorded)
				activeFloatFieldString = strValue;

			// Try Parse if value got changed. If the string could not be parsed, ignore it and keep last value
			bool parsed = true;
			if (strValue == "")
				value = activeFloatFieldLastValue = 0;
			else if (strValue != value.ToString ())
			{
				float newValue;
				parsed = float.TryParse (strValue, out newValue);
				if (parsed)
					value = activeFloatFieldLastValue = newValue;
			}

			if (active && !recorded)
			{ // Gained focus this frame
				activeFloatField = floatFieldID;
				activeFloatFieldString = strValue;
				activeFloatFieldLastValue = value;
			}
			else if (!active && recorded) 
			{ // Lost focus this frame
				activeFloatField = -1;
				if (!parsed)
					value = strValue.ForceParse ();
			}

			return value;
		}
		
		/// <summary>
		/// Float Field for ingame purposes. Behaves exactly like UnityEditor.EditorGUILayout.FloatField, besides the label slide field
		/// </summary>
		public static float FloatField (GUIContent label, float value)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Label (label, label != GUIContent.none? GUILayout.ExpandWidth (true) : GUILayout.ExpandWidth (false));
			value = FloatField (value);
			GUILayout.EndHorizontal ();
			return value;
		}

		/// <summary>
		/// Forces to parse to float by cleaning string if necessary
		/// </summary>
		public static float ForceParse (this string str) 
		{
			// try parse
			float value;
			if (float.TryParse (str, out value))
				return value;

			// Clean string if it could not be parsed
			bool recordedDecimalPoint = false;
			List<char> strVal = new List<char> (str);
			for (int cnt = 0; cnt < strVal.Count; cnt++) 
			{
				UnicodeCategory type = CharUnicodeInfo.GetUnicodeCategory (str[cnt]);
				if (type != UnicodeCategory.DecimalDigitNumber)
				{
					strVal.RemoveRange (cnt, strVal.Count-cnt);
					break;
				}
				else if (str[cnt] == '.')
				{
					if (recordedDecimalPoint)
					{
						strVal.RemoveRange (cnt, strVal.Count-cnt);
						break;
					}
					recordedDecimalPoint = true;
				}
			}

			// Parse again
			if (strVal.Count == 0)
				return 0;
			str = new string (strVal.ToArray ());
			if (!float.TryParse (str, out value))
				Debug.LogError ("Could not parse " + str);
			return value;
		}

		/// <summary>
		/// Provides an object field both for editor (using default) and for runtime (not yet implemented other that displaying object)
		/// </summary>
		public static T ObjectField<T> (T obj, bool allowSceneObjects) where T : Object
		{
			return ObjectField<T> (GUIContent.none, obj, allowSceneObjects);
		}
		/// <summary>
		/// Provides an object field both for editor (using default) and for runtime (not yet implemented other that displaying object)
		/// </summary>
		public static T ObjectField<T> (GUIContent label, T obj, bool allowSceneObjects) where T : Object
		{
	#if UNITY_EDITOR
			if (!Application.isPlaying)
				obj = UnityEditor.EditorGUILayout.ObjectField (GUIContent.none, obj, typeof (T), allowSceneObjects) as T;
	#endif	
			if (Application.isPlaying)
			{
				bool open = false;
				if (typeof(T).Name == "UnityEngine.Texture2D") 
				{
					label.image = obj as Texture2D;
					GUIStyle style = new GUIStyle (GUI.skin.box);
					style.imagePosition = ImagePosition.ImageAbove;
					open = GUILayout.Button (label, style);
				}
				else
				{
					GUIStyle style = new GUIStyle (GUI.skin.box);
					open = GUILayout.Button (label, style);
				}
				if (open)
				{
					Debug.Log ("Selecting Object!");
				}
			}
			return obj;
		}

		public static Texture2D ColorToTex (Color col) 
		{
			Texture2D tex = new Texture2D (1,1);
			tex.SetPixel (1, 1, col);
			tex.Apply ();
			return tex;
		}


		/// <summary>
		/// A GUI Function which simulates the default seperator
		/// </summary>
		public static void Seperator () 
		{
			setupSeperator ();
			GUILayout.Box (GUIContent.none, seperator, new GUILayoutOption[] { GUILayout.Height (1) });
		}
		/// <summary>
		/// A GUI Function which simulates the default seperator
		/// </summary>
		public static void Seperator (Rect rect) 
		{
			setupSeperator ();
			GUI.Box (new Rect (rect.x, rect.y, rect.width, 1), GUIContent.none, seperator);
		}
		private static GUIStyle seperator;
		private static void setupSeperator () 
		{
			if (seperator == null) 
			{
				seperator = new GUIStyle();
				seperator.normal.background = ColorToTex (new Color (0.6f, 0.6f, 0.6f));
				seperator.stretchWidth = true;
				seperator.margin = new RectOffset(0, 0, 7, 7);
			}
		}
	}
}