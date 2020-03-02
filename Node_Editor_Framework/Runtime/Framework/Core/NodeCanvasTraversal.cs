using System;

namespace NodeEditorFramework
{
	[Serializable]
	public abstract class NodeCanvasTraversal
	{
		public NodeCanvas nodeCanvas;

		public NodeCanvasTraversal (NodeCanvas canvas)
		{
			nodeCanvas = canvas;
		}

		public virtual void OnLoadCanvas () { }
		public virtual void OnSaveCanvas () { }

		public abstract void TraverseAll ();
		public virtual void OnChange (Node node) {}
	}
}

