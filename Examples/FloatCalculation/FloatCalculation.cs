using UnityEngine;
using System;

namespace NodeEditorFramework.Standard
{
	public class FloatConnectionType : ValueConnectionType
	{
		public override string Identifier { get { return "Float"; } }
		public override Color Color { get { return Color.cyan; } }
		public override Type Type { get { return typeof(float); } }
	}
}
