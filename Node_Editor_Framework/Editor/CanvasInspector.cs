using UnityEngine;
using UnityEditor;

namespace NodeEditorFramework.Standard
{
	[CustomEditor(typeof(NodeCanvas), true)]
	public class CanvasInspector : Editor
	{
		public static GUIStyle titleStyle;
		public static GUIStyle subTitleStyle;
		public static GUIStyle boldLabelStyle;

		public NodeCanvas canvas;

		public void OnEnable()
		{
			canvas = (NodeCanvas)target;
			canvas.Validate();
		}

		public override void OnInspectorGUI()
		{
			if (canvas == null)
				canvas = (NodeCanvas)target;
			if (canvas == null)
				return;
			if (titleStyle == null)
			{
				titleStyle = new GUIStyle(GUI.skin.label);
				titleStyle.fontStyle = FontStyle.Bold;
				titleStyle.alignment = TextAnchor.MiddleCenter;
				titleStyle.fontSize = 16;
			}
			if (subTitleStyle == null)
			{
				subTitleStyle = new GUIStyle(GUI.skin.label);
				subTitleStyle.fontStyle = FontStyle.Bold;
				subTitleStyle.alignment = TextAnchor.MiddleCenter;
				subTitleStyle.fontSize = 12;
			}
			if (boldLabelStyle == null)
			{
				boldLabelStyle = new GUIStyle(GUI.skin.label);
				boldLabelStyle.fontStyle = FontStyle.Bold;
			}

			EditorGUI.BeginChangeCheck();

			GUILayout.Space(10);

			GUILayout.Label(new GUIContent(canvas.saveName, canvas.savePath), titleStyle);
			GUILayout.Label(canvas.livesInScene? "Scene Save" : "Asset Save", subTitleStyle);
			GUILayout.Label("Type: " + canvas.canvasName, subTitleStyle);

			GUILayout.Space(10);

			EditorGUI.BeginDisabledGroup(NodeEditor.curNodeCanvas != null && NodeEditor.curNodeCanvas.savePath == canvas.savePath);
			if (GUILayout.Button("Open"))
			{
				string NodeCanvasPath = AssetDatabase.GetAssetPath(canvas);
				NodeEditorWindow.OpenNodeEditor().canvasCache.LoadNodeCanvas(NodeCanvasPath);
			}
			EditorGUI.EndDisabledGroup();

			GUILayout.Space(10);
			
			GUILayout.Label("Nodes", boldLabelStyle);
			foreach (Node node in canvas.nodes)
			{
				EditorGUILayout.ObjectField(node.Title, node, node.GetType(), true);
			}

			GUILayout.Space(10);

			canvas.DrawCanvasPropertyEditor();

			if (EditorGUI.EndChangeCheck())
				NodeEditor.RepaintClients();
		}
	}
}
