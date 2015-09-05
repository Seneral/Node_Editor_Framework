using UnityEngine;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;

public static class GUIExt 
{

	/// <summary>
	/// Mimic's UnityEditor.EditorGUILayout.TextField in taking a label and a string and returning the edited string.
	/// </summary>
	public static string TextField (GUIContent label, string text)
	{
		GUILayout.BeginHorizontal ();
		GUILayout.Label (label, GUILayout.ExpandWidth (true)); // GUILayout.Width(146)
		text = GUILayout.TextField (text);
		GUILayout.EndHorizontal ();
		return text;
	}

//#if !UNITY_EDITOR
	private static int activeFloatField;
	private static float activeFloatFieldLastValue;
	private static string activeFloatFieldString;
//#endif
	/// <summary>
	/// Mimics UnityEditor.EditorGUILayout.FloatField by taking a float and returning the edited float.
	/// </summary>
	/// <param name="obj">The object the call comes from. Important for distinction between unique FloatFields!</param>
	public static float FloatField (float value, int hint)
	{
//		#if UNITY_EDITOR 
//		return UnityEditor.EditorGUILayout.FloatField (value);
//		#else

		int floatFieldID = GUIUtility.GetControlID (hint, FocusType.Keyboard) + 1;
		if (floatFieldID == 0)
			return value;

		bool recordedFocus = activeFloatField == floatFieldID;
		bool active = floatFieldID == GUIUtility.keyboardControl;
		bool gainedFocus = active && !recordedFocus;
		bool lostFocus = !active && recordedFocus;

		if (active && recordedFocus)
		{
			if (activeFloatFieldLastValue != value) 
			{ // Value has been modified externally
				activeFloatFieldLastValue = value;
				activeFloatFieldString = value.ToString ();
			}
		}

		string str = recordedFocus? activeFloatFieldString : value.ToString ();
		if (str == null)
			str = "";

		string strValue = GUILayout.TextField (str,  new GUILayoutOption[] { GUILayout.ExpandWidth (false), GUILayout.MinWidth (40) });

		if (recordedFocus)
			activeFloatFieldString = strValue;

		// Try Parse if necessary
		bool parsed = true;
		if (value.ToString () != strValue) 
		{
			float newValue;
			parsed = float.TryParse (strValue, out newValue);
			if (parsed)
				value = activeFloatFieldLastValue = newValue;
		}

		if (gainedFocus)
		{
			activeFloatField = floatFieldID;
			activeFloatFieldString = strValue;
			activeFloatFieldLastValue = value;
		}
		else if (lostFocus) 
		{
			activeFloatField = -1;
			if (!parsed)
				value = strValue.ForceParse ();
		}

		return value;
//		#endif
	}
	
	/// <summary>
	/// Mimics UnityEditor.EditorGUILayout.FloatField by taking a label and a float and returning the edited float.
	/// </summary>
	public static float FloatField (GUIContent label, float value)
	{
//		#if UNITY_EDITOR 
//		return UnityEditor.EditorGUILayout.FloatField (label, value);
//		#else
		GUILayout.BeginHorizontal ();
		
		GUILayout.Label (label, label != GUIContent.none? GUILayout.ExpandWidth (true) : GUILayout.ExpandWidth (false));
		//		Rect sliderRect = GUILayoutUtility.GetLastRect ();

		int hint = GUIUtility.ScreenToGUIRect (GUILayoutUtility.GetLastRect ()).GetHashCode ();
		value = FloatField (value, hint);
		
		// Check if this is active and, if it just became active, set the start slider value
		//		bool active = lastFloatField == activeFloatField;
		//		Event cur = Event.current;
		//		if (cur.type == EventType.MouseDown && sliderRect.Contains (cur.mousePosition))
		//			activeFloatField = lastFloatField;
		//		else if (cur.type != EventType.MouseDrag)
		//			activeFloatField = "";
		//		if (active != (lastFloatField == activeFloatField))
		//		{
		//			active = lastFloatField == activeFloatField;
		//			startSlideValue = active? value : 0;
		//			startSlidePos = active? cur.mousePosition.x : 0;
		//			
		//		}
		//
		//		if (active) 
		//		{
		//			UnityEngine.Debug.Log (startSlideValue + " val, pos: " + startSlidePos);
		//			value = startSlideValue + (cur.mousePosition.x-startSlidePos)*startSlideValue;
		//			cur.Use ();
		//		}
		
		GUILayout.EndHorizontal ();
		return value;
//		#endif
	}


	public static float ForceParse (this string str) 
	{
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

		if (strVal.Count == 0)
			return 0;

		str = new string (strVal.ToArray ());
		float value;
		if (!float.TryParse (str, out value))
			Debug.LogError ("Could not parse " + str);
		return value;
	}
}
