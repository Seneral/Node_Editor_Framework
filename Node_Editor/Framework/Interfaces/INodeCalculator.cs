using System;
using NodeEditorFramework;

namespace NodeEditorFramework
{
	public interface INodeCalculator
	{
		void RecalculateAll (NodeCanvas nodeCanvas);
		void RecalculateFrom (Node node) ;
	}
}

