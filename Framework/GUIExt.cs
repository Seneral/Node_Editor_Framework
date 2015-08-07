using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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

//	private static string lastFloatField;
	private static string activeFloatField;
//	private static float startSlideValue = 0;
//	private static float startSlidePos = 0;
#if UNITY_EDITOR
#else
	private static Dictionary<string, string> floatFields = new Dictionary<string, string> ();
#endif
	/// <summary>
	/// Mimics UnityEditor.EditorGUILayout.FloatField by taking a float and returning the edited float.
	/// </summary>
	/// <param name="obj">The object the call comes from. Important for distinction between unique FloatFields!</param>
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static float FloatField (float value, object callingObj)
	{
#if UNITY_EDITOR 
		return UnityEditor.EditorGUILayout.FloatField (value);
#else
		// Fetch information about the calling instance (to identify the floatField)
		StackFrame frame = new StackFrame (1, true);
		string floatFieldID = "Obj:" + (callingObj != null? callingObj.GetHashCode ().ToString () : "Null") + 
				"_File:" + frame.GetFileName () + 
				"_Line:" + frame.GetFileLineNumber ();

		// Display field
		bool recordedField = floatFields.ContainsKey (floatFieldID);
		string prevStr = recordedField? floatFields [floatFieldID] : value.ToString ();
		GUI.SetNextControlName (floatFieldID);
		string textValue = GUILayout.TextField (prevStr, new GUILayoutOption[] { GUILayout.ExpandWidth (false), GUILayout.MinWidth (40) });

		// Check focus
		bool unfocusField = GUI.GetNameOfFocusedControl () != floatFieldID && activeFloatField == floatFieldID;
		if (unfocusField) // field got out of focus
			UnityEngine.Debug.Log ("Unfocus!");
		else if (GUI.GetNameOfFocusedControl () == floatFieldID) // Focus
			activeFloatField = floatFieldID;

		// Cleanup
		if (textValue.Split (new char[] {'.', ','}, System.StringSplitOptions.None).Length > 2) // if there are two dots, remove the second one and every fellow digit
			textValue.Remove (textValue.LastIndexOf ('.'));

		// Update text
		if (recordedField)
			floatFields [floatFieldID] = textValue;

//		lastFloatField = floatFieldID;

		// Parse and handle records
		float newValue;
		bool parsed = float.TryParse (textValue, out newValue);
		if ((parsed && !textValue.EndsWith (".")) || unfocusField)
		{ // if we don't have something to keep (any information that would be lost when parsing)
			if (recordedField || unfocusField) // but we have a record of this (previously not parseable), remove it now (as it has become parseable)
				floatFields.Remove (floatFieldID);
		}
		else if (!recordedField) // we have something we want to keep (any information that would be lost when parsing) and we don't already have it recorded, add it
			floatFields.Add (floatFieldID, textValue);

		return parsed? newValue : value;
#endif
	}

	/// <summary>
	/// Mimics UnityEditor.EditorGUILayout.FloatField by taking a label and a float and returning the edited float.
	/// </summary>
	/// <param name="obj">The object the call comes from. Important for distinction between unique FloatFields!</param>
	public static float FloatField (GUIContent label, float value, object callingObj)
	{
#if UNITY_EDITOR 
		return UnityEditor.EditorGUILayout.FloatField (label, value);
#else
		GUILayout.BeginHorizontal ();

		GUILayout.Label (label, GUILayout.ExpandWidth (true));
//		Rect sliderRect = GUILayoutUtility.GetLastRect ();

		value = FloatField (value, callingObj);

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
#endif
	}
}
