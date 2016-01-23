using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using NodeEditorFramework;

namespace NodeEditorFramework 
{
	[CustomEditor (typeof(RuntimeNodeEditor))]
	public class RTNE_InspectorGUI : Editor
	{
		public RuntimeNodeEditor RTNE;

		public void OnEnable () 
		{
			RTNE = (RuntimeNodeEditor)target;
			RTNE.canvasPath = RTNE.canvas == null? "" : AssetDatabase.GetAssetPath (RTNE.canvas);
		}

		public override void OnInspectorGUI () 
		{
			NodeCanvas canvas = EditorGUILayout.ObjectField ("Canvas", RTNE.canvas, typeof(NodeCanvas), false) as NodeCanvas;
			if (canvas != RTNE.canvas)
			{
				RTNE.canvas = canvas;
				RTNE.canvasPath = RTNE.canvas == null? "" : AssetDatabase.GetAssetPath (RTNE.canvas);
			}

			RTNE.screenSize = !EditorGUILayout.BeginToggleGroup (new GUIContent ("Specify Rect", "Specify Rects explicitly instead of adapting to the screen size"), !RTNE.screenSize);
			RTNE.specifiedRootRect = EditorGUILayout.RectField (new GUIContent ("Root Rect", "The root/group rect of the actual canvas rect. If left blank it is ignored."), RTNE.specifiedRootRect);
			RTNE.specifiedCanvasRect = EditorGUILayout.RectField (new GUIContent ("Canvas Rect", "The rect of the canvas."), RTNE.specifiedCanvasRect);
			EditorGUILayout.EndToggleGroup ();
		}
	}
}
