using UnityEngine;
using System.Collections.Generic;

namespace NodeEditorFramework.Standard
{
    [Node(false, "Example/Resizing Node")]
    public class ResizingNode : Node
    {
        public override Vector2 MinSize { get { return new Vector2(200, 10); } }
        public override bool Resizable { get { return true; } }

        public override string GetID { get { return "resizingNode"; } }

        public override Node Create(Vector2 pos)
        {
            ResizingNode node = CreateInstance<ResizingNode>();

            node.rect.position = pos;
            node.name = "Resizing Node";

            return node;
        }

        private List<string> labels = new List<string>();
        private string newLabel = "";
        protected internal override void NodeGUI()
        {
            GUILayout.Label("This node resizes to fit all inputs!");

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            newLabel = GUILayout.TextField(newLabel);
            if (GUILayout.Button("Add", GUILayout.ExpandWidth(false)))
                labels.Add(newLabel);

            GUILayout.EndHorizontal();

            string labelToRemove = null;
            foreach(var text in labels)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Label(text);

                if(GUILayout.Button("x", GUILayout.ExpandWidth(false)))
                    labelToRemove = text;

                GUILayout.EndHorizontal();
            }

            if (!string.IsNullOrEmpty(labelToRemove))
            {
                labels.Remove(labelToRemove);
                labelToRemove = null;
            }

            GUILayout.EndVertical();
        }
    }
}