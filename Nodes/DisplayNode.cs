using UnityEngine;
using UnityEditor;
using System.Collections;

[System.Serializable]
public class DisplayNode : Node {
    public bool assigned;
    public float value;

    public static DisplayNode Create(Rect NodeRect) { // This function has to be registered in Node_Editor.ContextCallback
        DisplayNode node = CreateInstance<DisplayNode>();

        node.name = "Display Node";
        node.rect = NodeRect;

        NodeInput.Create(node, "Value", typeof(float));

        node.Init();
        return node;
    }

    public override void DrawNode() {
        GUILayout.BeginHorizontal();
        GUILayout.Label(new GUIContent("Value : " + (assigned ? value.ToString() : ""), "The input value to display"));
        if (Event.current.type == EventType.Repaint)
            Inputs[0].SetRect(GUILayoutUtility.GetLastRect());
        GUILayout.EndHorizontal();
    }

    public override bool Calculate() {
        if (!allInputsReady()) {
            value = 0;
            assigned = false;
            return false;
        }

        value = (float) Inputs[0].connection.value;
        assigned = true;

        return true;
    }
}
