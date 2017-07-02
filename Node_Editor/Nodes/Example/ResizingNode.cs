using UnityEngine;
using System.Collections.Generic;

namespace NodeEditorFramework.Standard
{
	[Node(false, "Example/Resizing Node")]
	public class ResizingNode : Node
	{
		public const string ID = "resizingNode";
		public override string GetID { get { return ID; } }

		public override string Title { get { return "Resizing Node"; } }
		public override Vector2 MinSize { get { return new Vector2(200, 10); } }
		public override bool AutoLayout { get { return true; } }

		private List<string> labels = new List<string>();
		private string newLabel = "";

		public override void NodeGUI()
		{
			GUILayout.Label("This node resizes to fit all inputs!");

			// Display text field and add button
			GUILayout.BeginHorizontal();
			newLabel = GUILayout.TextField(newLabel);
			if (GUILayout.Button("Add", GUILayout.ExpandWidth(false)))
				labels.Add(newLabel);
			GUILayout.EndHorizontal();

			for (int i = 0; i < labels.Count; i++)
			{ // Display label and delete button
				GUILayout.BeginHorizontal();
				GUILayout.Label(labels[i]);
				if(GUILayout.Button("x", GUILayout.ExpandWidth(false)))
				{ // Remove current label
					labels.RemoveAt (i);
					i--;
				}
				GUILayout.EndHorizontal();
			}
		}
	}
}