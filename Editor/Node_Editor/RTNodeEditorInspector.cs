using UnityEngine;
using UnityEditor;

namespace NodeEditorFramework.Standard
{
	[CustomEditor (typeof(RTNodeEditor))]
	public class RTNodeEditorInspector : Editor
	{
		public RTNodeEditor RTNE;
		private string[] sceneCanvasNames;
		
		public void OnEnable () 
		{
			RTNE = (RTNodeEditor)target;
			sceneCanvasNames = NodeEditorSaveManager.GetSceneSaves();
		}

		public override void OnInspectorGUI () 
		{
			GUILayout.BeginHorizontal();
			// Reference canvas
			RTNE.canvas = EditorGUILayout.ObjectField ("Canvas", RTNE.canvas, typeof(NodeCanvas), true,
				RTNE.canvas == null ? GUILayout.ExpandWidth(false) : GUILayout.ExpandWidth(true)) as NodeCanvas;
			if (RTNE.canvas != null)
				RTNE.loadSceneName = null;
			// Select canvas to load from scene
			int prevCanvasIndex = ArrayUtility.IndexOf(sceneCanvasNames, RTNE.loadSceneName);
			int newCanvasIndex = EditorGUILayout.Popup(prevCanvasIndex, sceneCanvasNames,
				string.IsNullOrEmpty(RTNE.loadSceneName) ? GUILayout.Width(30) : GUILayout.ExpandWidth (true));
			if (prevCanvasIndex != newCanvasIndex && newCanvasIndex >= 0)
			{
				RTNE.loadSceneName = sceneCanvasNames[newCanvasIndex];
				RTNE.canvas = null;
			}
			else if (newCanvasIndex < 0)
				RTNE.loadSceneName = null;
			GUILayout.EndHorizontal();

			RTNE.screenSize = !EditorGUILayout.BeginToggleGroup (new GUIContent ("Specify Rect", "Specify Rects explicitly instead of adapting to the screen size"), !RTNE.screenSize);
			RTNE.specifiedRootRect = EditorGUILayout.RectField (new GUIContent ("Root Rect", "The root/group rect of the actual canvas rect. If left blank it is ignored."), RTNE.specifiedRootRect);
			RTNE.specifiedCanvasRect = EditorGUILayout.RectField (new GUIContent ("Canvas Rect", "The rect of the canvas."), RTNE.specifiedCanvasRect);
			EditorGUILayout.EndToggleGroup ();
		}
	}
}
