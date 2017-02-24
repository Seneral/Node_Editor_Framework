using UnityEngine;
using System.Collections;
using NodeEditorFramework;

namespace NodeEditorFramework.Standard
{
	[NodeCanvasType("Calculation")]
	public class CalculationCanvasType : NodeCanvas
	{
		public override string canvasName { get { return "Calculation Canvas"; } }

		protected override void OnCreate () 
		{
			Traversal = new CanvasCalculator (this);
		}

		public void OnEnable () 
		{
			if (Traversal == null)
				Traversal = new CanvasCalculator (this);
			// Register to other callbacks, f.E.:
			//NodeEditorCallbacks.OnDeleteNode += OnDeleteNode;
		}

		protected override void ValidateSelf ()
		{
			if (Traversal == null)
				Traversal = new CanvasCalculator (this);
		}
	}
}
