using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using NodeEditorFramework;

namespace NodeEditorFramework 
{
	[CustomEditor (typeof(RTCanvasCalculator))]
	public class RTCanvasCalculatorEditor : Editor
	{
		public RTCanvasCalculator RTCalc;

		public List<Node> inputNodes;

		public void OnEnable () 
		{
			RTCalc = (RTCanvasCalculator)target;
		}

		public override void OnInspectorGUI () 
		{
			NodeCanvas canvas = EditorGUILayout.ObjectField ("Canvas", RTCalc.canvas, typeof(NodeCanvas), false) as NodeCanvas;
			if (canvas != RTCalc.canvas)
			{ // Need it to reload so it creates a copy
				string canvasPath = canvas == null? "" : AssetDatabase.GetAssetPath (canvas);
				RTCalc.LoadCanvas (canvasPath);
				inputNodes = null;
			}

			if (GUILayout.Button ("Calculate and debug Output")) 
			{
				RTCalc.CalculateCanvas ();
			}

			if (inputNodes == null)
				inputNodes = RTCalc.getInputNodes ();
			DisplayInputValues ();
		}

		private void DisplayInputValues () 
		{
			foreach (Node inputNode in inputNodes) 
			{
				string outID = "(IN) " + inputNode.name + ": ";
				foreach (NodeOutput output in inputNode.Outputs)
					outID += output.typeID + " " + (output.IsValueNull? "NULL" : output.GetValue ().ToString ()) + "; ";
				GUILayout.Label (outID);
			}
		}
	}
}
