using UnityEngine;
using UnityEngine.Events;
using UnityEditor;

namespace NodeEditorFramework.Utilities
{
	[CustomPropertyDrawer(typeof(UnityFuncBase), true)]
	public class UnityFuncEditor : PropertyDrawer
	{
		private static float lineHeight { get { return GUI.skin.label.lineHeight + 4; } }
		public override float GetPropertyHeight (SerializedProperty prop, GUIContent label) 
		{
			return lineHeight * 2;
		}

		public override void OnGUI (Rect pos, SerializedProperty property, GUIContent label) 
		{
			EditorGUI.BeginProperty (pos, label, property);

			SerializedProperty methodNameProp = property.FindPropertyRelative ("_commandName");
			SerializedProperty argumentsProp = property.FindPropertyRelative ("_argumentTypes");
			SerializedProperty targetObjProp = property.FindPropertyRelative ("_targetObject");

			Rect[] gridRects = calcRectGrid (pos, 2, new float[] { pos.width/2, pos.width/2 });

			string arguments = "";
			for (int argCnt = 0; argCnt < argumentsProp.arraySize; argCnt++) 
				arguments += argumentsProp.GetArrayElementAtIndex (argCnt).FindPropertyRelative ("argAssemblyTypeName").stringValue + (argCnt < argumentsProp.arraySize-1? ", " : "");
			GUI.Label (gridRects[0], new GUIContent (label.text + " (" + arguments + ")"));

			targetObjProp.objectReferenceValue = EditorGUI.ObjectField (gridRects[2], targetObjProp.objectReferenceValue, typeof(Object), true);
			methodNameProp.stringValue = EditorGUI.TextField (gridRects[3], methodNameProp.stringValue);

			targetObjProp.serializedObject.ApplyModifiedProperties ();

			EditorGUI.EndProperty ();
		}

		/// <summary>
		/// Splites the parentRect up into a grid.
		/// The number of rows is determined by the number of lines matching into the parentRect, 
		/// the number of columns by the passed array which also determines the column's widths.
		/// </summary>
		private Rect[] calcRectGrid (Rect parentRect, int rows, float[] columnWidths)
		{
			int columns = columnWidths.Length;
			float cellHeight = parentRect.height/rows;

			Rect[] gridRects = new Rect[columns*rows];
			for (int rowCnt = 0; rowCnt < rows; rowCnt++) 
			{
				float colShift = 0;
				for (int colCnt = 0; colCnt < columns; colCnt++) 
				{
					float colWidth = columnWidths[colCnt];
					gridRects[rowCnt*columns + colCnt] = new Rect (parentRect.x + colShift, parentRect.y + cellHeight*rowCnt, colWidth, cellHeight);
					colShift += colWidth;
				}
			}
			return gridRects;
		}
	}

}