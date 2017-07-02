using System;
using System.Collections;
using UnityEngine;

using NodeEditorFramework;

namespace NodeEditorFramework.Standard
{
	[Node (false, "Float/Display")]
	public class DisplayNode : Node 
	{
		public const string ID = "displayNode";
		public override string GetID { get { return ID; } }

		public override string Title { get { return "Display Node"; } }
		public override Vector2 DefaultSize { get { return new Vector2 (150, 50); } }

		private float value = 0;

		[ValueConnectionKnob("Value", Direction.In, "Float")]
		public ValueConnectionKnob inputKnob;
		
		public override void NodeGUI () 
		{
			inputKnob.DisplayLayout (new GUIContent ("Value : " + value.ToString (), "The input value to display"));
		}
		
		public override bool Calculate () 
		{
			value = inputKnob.GetValue<float> ();
			return true;
		}
	}
}