using UnityEngine;
using UnityEditor;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework.Standard
{
	[CustomEditor(typeof(Node), true)]
	public class NodeInspector : Editor
	{
		public static GUIStyle titleStyle;
		public static GUIStyle boldLabelStyle;
		public Node node;

		public void OnEnable()
		{
			node = (Node)target;
		}

		public override void OnInspectorGUI()
		{
			if (node == null)
				node = (Node)target;
			if (node == null)
				return;
			if (titleStyle == null)
			{
				titleStyle = new GUIStyle(GUI.skin.label);
				titleStyle.fontStyle = FontStyle.Bold;
				titleStyle.alignment = TextAnchor.MiddleCenter;
				titleStyle.fontSize = 16;
			}
			if (boldLabelStyle == null)
			{
				boldLabelStyle = new GUIStyle(GUI.skin.label);
				boldLabelStyle.fontStyle = FontStyle.Bold;
			}

			OverlayGUI.StartOverlayGUI("NodeInspector");

			EditorGUI.BeginChangeCheck();

			GUILayout.Space(10);

			GUILayout.Label(node.Title, titleStyle);

			GUILayout.Space(10);

			GUILayout.Label("Rect: " + node.rect.ToString());
			node.backgroundColor = EditorGUILayout.ColorField("Color", node.backgroundColor);

			GUILayout.Space(10);

			GUILayout.Label("Connection Ports", boldLabelStyle);
			foreach (ConnectionPort port in node.connectionPorts)
			{
				string labelPrefix = port.direction == Direction.In ? "Input " : (port.direction == Direction.Out ? "Output " : "");
				string label = labelPrefix + port.styleID + " '" + port.name + "'";
				EditorGUILayout.ObjectField(label, port, port.GetType(), true);
			}

			GUILayout.Space(10);

			GUILayout.Label("Property Editor", boldLabelStyle);
			node.DrawNodePropertyEditor(true);

			if (EditorGUI.EndChangeCheck())
				NodeEditor.RepaintClients();

			OverlayGUI.EndOverlayGUI();
		}
	}
}
