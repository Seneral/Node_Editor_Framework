using UnityEngine;
using System.Collections;
using NodeEditorFramework;

namespace NodeEditorFramework 
{
	public class Connection 
	{ // simple calss that represents a connection between an output and an input
		public NodeOutput output;
		public NodeInput input;

		public Connection (NodeOutput nodeOutput, NodeInput nodeInput) 
		{
			output = nodeOutput;
			input = nodeInput;
		}
	}
}