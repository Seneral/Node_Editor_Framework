using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(UnityFuncBase), true)]
public class UnityFuncEditor : PropertyDrawer
{
	private static float lineHeight { get { return GUI.skin.label.lineHeight + 4; } }
	public override float GetPropertyHeight (SerializedProperty prop, GUIContent label) 
	{
		SerializedProperty argumentsProp = prop.FindPropertyRelative ("_arguments");
		return lineHeight * (2 + argumentsProp.arraySize);
	}

	public override void OnGUI (Rect pos, SerializedProperty property, GUIContent label) 
	{
		EditorGUI.BeginProperty (pos, label, property);

		SerializedProperty callStateProp = property.FindPropertyRelative ("callState");
		SerializedProperty methodNameProp = property.FindPropertyRelative ("_methodName");
		SerializedProperty argumentsProp = property.FindPropertyRelative ("_arguments");
		SerializedProperty targetObjProp = property.FindPropertyRelative ("_targetObject");

		int indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		Rect[] gridRects = calcRectGrid (pos, lineHeight, new float[] { pos.width/2, pos.width/2 });

		GUI.Label (gridRects[0], label);

		EditorGUI.PropertyField (gridRects[1], callStateProp);

		//targetObjProp.objectReferenceValue = EditorGUI.ObjectField (gridRects[2], targetObjProp.objectReferenceValue, typeof(Object), true);

		EditorGUI.PropertyField (gridRects[2], targetObjProp);

		methodNameProp.stringValue = EditorGUI.TextField (gridRects[3], methodNameProp.stringValue);

		for (int argCnt = 0; argCnt < argumentsProp.arraySize; argCnt++) 
		{
			SerializedProperty argProp = argumentsProp.GetArrayElementAtIndex (argCnt).FindPropertyRelative ("objectValue");
			EditorGUI.PropertyField (gridRects[4+argCnt], argProp);
		}

//		GUILayout.BeginHorizontal ();
//		// Label
//		GUILayout.Label (label);
//		// CallState
//		EditorGUILayout.PropertyField (callStateProp);
//		GUILayout.EndHorizontal ();
//
//		GUILayout.BeginHorizontal ();
//		// Target Object
//		targetObjProp.objectReferenceValue = EditorGUILayout.ObjectField (targetObjProp.objectReferenceValue, typeof(Object), true);
//		// Method
//		methodNameProp.stringValue = EditorGUILayout.TextField (methodNameProp.stringValue);
//		GUILayout.EndHorizontal ();

//		int controlCount = argumentsProp.arraySize;
//		float xPos = pos.x;
//		
//		Rect objectFieldRect = new Rect (xPos, pos.y, pos.width/controlCount, pos.height);
//		xPos += pos.width/controlCount;
//		targetObjProp.objectReferenceValue = EditorGUI.ObjectField (objectFieldRect, targetObjProp.objectReferenceValue, typeof(Object), true);
//
//		Rect methodDropdownRect = new Rect (xPos, pos.y, pos.width/controlCount, pos.height);
//		xPos += pos.width/controlCount;
//		methodNameProp.stringValue = EditorGUI.TextField (methodDropdownRect, methodNameProp.stringValue);
//
//		for (int argCnt = 0; argCnt < argumentsProp.arraySize; argCnt++) 
//		{
//			Rect paramRect = new Rect (xPos, pos.y, pos.width/controlCount, pos.height);
//			xPos += pos.width/controlCount;
//			SerializedProperty argProp = argumentsProp.GetArrayElementAtIndex (argCnt).FindPropertyRelative ("objectValue");
//			EditorGUI.PropertyField (paramRect, argProp);
//		}

		EditorGUI.indentLevel = indent;

		EditorGUI.EndProperty ();
	}

	/// <summary>
	/// Splites the parentRect up into a grid.
	/// The number of rows is determined by the number of lines matching into the parentRect, 
	/// the number of columns by the passed array which also determines the column's widths.
	/// </summary>
	private Rect[] calcRectGrid (Rect parentRect, float cellHeight, float[] columnWidths)
	{
		int columns = columnWidths.Length, rows = (int)(parentRect.height/cellHeight);
		Debug.Log ("Created " + columns + " cols and " + rows + " rows");
		Rect[] gridRects = new Rect[columns*rows];
		for (int rowCnt = 0; rowCnt < rows; rowCnt++) 
		{
			for (int colCnt = 0; colCnt < columns; colCnt++) 
			{
				float colWidth = columnWidths[colCnt];
				gridRects[rowCnt*columns + colCnt] = new Rect (parentRect.x + colWidth*colCnt, parentRect.y + cellHeight*rowCnt, colWidth, cellHeight);
			}
		}
		return gridRects;
	}
}
