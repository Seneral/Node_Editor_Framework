using UnityEngine;
using System;
using System.Linq;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework.Standard
{
    [Node(false, "Group as Node")]
    public class GroupNode : Node
    {
        public const string ID = "groupNode";
        public override string GetID { get { return ID; } }

        private Color _color = Color.blue;
        public Color color { get { return _color; } set { _color = value; } }
        private GUIStyle headerStyle;
        private GUIStyle bodyStyle;

        /// <summary>
		/// Init the Node Base after the Node has been created. This includes adding to canvas, and to calculate for the first time
		/// </summary>
		protected internal override void InitBase()
        {
            NodeEditor.RecalculateFrom(this);
            if (!NodeEditor.curNodeCanvas.groups.Contains(this))
                NodeEditor.curNodeCanvas.groups.Add(this);
            #if UNITY_EDITOR
            if (String.IsNullOrEmpty(name))
                name = UnityEditor.ObjectNames.NicifyVariableName(GetID);
            #endif
            NodeEditor.RepaintClients();
        }

        /// <summary>
		/// Deletes this Node from curNodeCanvas and the save file
		/// </summary>
		public override void Delete()
        {
            if (!NodeEditor.curNodeCanvas.groups.Contains(this))
                throw new UnityException("The GroupNode " + name + " does not exist on the Canvas " + NodeEditor.curNodeCanvas.name + "!");
            NodeEditorCallbacks.IssueOnDeleteNode(this);
            NodeEditor.curNodeCanvas.groups.Remove(this);
            
            DestroyImmediate(this, true);
        }

        public override Node Create(Vector2 pos)
        {
            GroupNode node = CreateInstance<GroupNode>();

            node.rect = new Rect(pos.x, pos.y, 400, 400);
            node.name = "Group";
            node.prevPos = pos;

            node.GenerateStyles();

            return node;
        }

        private void GenerateStyles()
        {
            // Transparent background
            Texture2D background = RTEditorGUI.ColorToTex(8, _color * new Color(1, 1, 1, 0.5f));

            bodyStyle = new GUIStyle();
            bodyStyle.normal.background = background;

            // ligher, less transparent background
            background = RTEditorGUI.ColorToTex(8, _color * new Color(2, 2, 2, 0.8f));

            headerStyle = new GUIStyle();
            headerStyle.normal.background = background;
            headerStyle.fontSize = 16;
            headerStyle.alignment = TextAnchor.MiddleLeft;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.contentOffset = new Vector2(20, 0);
        }

        protected internal override void DrawNode()
        {
            // Create a rect that is adjusted to the editor zoom
            Rect nodeRect = rect;
            nodeRect.position += NodeEditor.curEditorState.zoomPanAdjust;
            int headerHeight = 30;

            // header base
            Rect headerRect = new Rect(nodeRect.x, nodeRect.y, nodeRect.width, headerHeight);
            GUI.Label(headerRect, name, headerStyle);

            // TODO: header button to edit name, color, etc.
            Rect editButtonRect = new Rect(nodeRect.x + nodeRect.width - 40, nodeRect.y, 40, headerHeight);
            if (GUI.Button(editButtonRect, "X"))
            {
                Delete();
            }
            
            // Begin the body frame around the NodeGUI
            Rect bodyRect = new Rect(nodeRect.x, nodeRect.y + headerHeight, nodeRect.width, nodeRect.height - headerHeight);
            GUI.BeginGroup(bodyRect, bodyStyle);
            bodyRect.position = Vector2.zero;
            GUI.EndGroup();

            // TODO: handle at the bottom right to resize?
            // GUI here, Input as an extension using attributes
            Rect resizeButtonRect = new Rect(nodeRect.x + nodeRect.width - 40, nodeRect.y + nodeRect.height - 30, 40, headerHeight);
            if (GUI.Button(resizeButtonRect, "*"))
            {

            }
        }

        private Vector2 prevPos;

        protected internal override void OnMove()
        {
            Vector2 moveOffset = rect.position - prevPos;
            prevPos = rect.position;

            // TODO: Get all pinned nodes and move them by moveOffset
            // Attention: Pinned nodes have to be fetched before moving so nodes on the edge won't un-pinned when moving away
            var pinnedNodes = NodeEditorInputControls.pinnedNodes;

            for (int i = 0; i < pinnedNodes.Count; ++i)
            {
                pinnedNodes[i].rect.position += moveOffset;
            }
        }

        protected internal override void NodeGUI() { }

        public override bool Calculate() { return true; }
    }
}