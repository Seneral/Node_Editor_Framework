using UnityEngine;
using UnityEditor;

namespace NodeEditorFramework 
{
	[CustomEditor (typeof(RuntimeNodeEditor))]
	public class RTNEInspectorGUI : Editor
	{
		public RuntimeNodeEditor RTNE;

		public void OnEnable () 
		{
			RTNE = (RuntimeNodeEditor)target;
			RTNE.CanvasPath = RTNE.Canvas == null? "" : AssetDatabase.GetAssetPath (RTNE.Canvas);
		}

		public override void OnInspectorGUI () 
		{
			var canvas = EditorGUILayout.ObjectField ("Canvas", RTNE.Canvas, typeof(NodeCanvas), false) as NodeCanvas;
			if (canvas != RTNE.Canvas)
			{
				RTNE.Canvas = canvas;
				RTNE.CanvasPath = RTNE.Canvas == null? "" : AssetDatabase.GetAssetPath (RTNE.Canvas);
			}

			RTNE.ScreenSize = !EditorGUILayout.BeginToggleGroup (new GUIContent ("Specify Rect", "Specify Rects explicitly instead of adapting to the screen size"), !RTNE.ScreenSize);
			RTNE.SpecifiedRootRect = EditorGUILayout.RectField (new GUIContent ("Root Rect", "The root/group rect of the actual canvas rect. If left blank it is ignored."), RTNE.SpecifiedRootRect);
			RTNE.SpecifiedCanvasRect = EditorGUILayout.RectField (new GUIContent ("Canvas Rect", "The rect of the canvas."), RTNE.SpecifiedCanvasRect);
			EditorGUILayout.EndToggleGroup ();
		}
	}
}
