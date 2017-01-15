using UnityEngine;
using System;
using NodeEditorFramework;

namespace NodeEditorFramework.Standard
{
	public class FloatType : IConnectionTypeDeclaration 
	{
		public string Identifier { get { return "Float"; } }
		public Type Type { get { return typeof(float); } }
		public Color Color { get { return Color.cyan; } }
		public string InKnobTex { get { return "Textures/In_Knob.png"; } }
		public string OutKnobTex { get { return "Textures/Out_Knob.png"; } }
	}
}
