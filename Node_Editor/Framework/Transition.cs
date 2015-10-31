using UnityEngine;
using System;
using System.Collections.Generic;
using NodeEditorFramework;

namespace NodeEditorFramework
{
	public class Transition : ScriptableObject
	{
		public Node startNode;
		public Node endNode;

		public List<Func<Transition, bool>> conditions;

		public bool conditionsMet () 
		{
			for (int condCnt = 0; condCnt < conditions.Count; condCnt++) 
			{
				if (!conditions[condCnt].Invoke (this))
					return false;
			}
			return true;
		}

		public static Transition Create (Node fromNode, Node toNode) 
		{
			if (NodeTypes.getNodeData (fromNode).transitions == false || NodeTypes.getNodeData (toNode).transitions == false)
				return null;

			Transition transition = CreateInstance<Transition> ();
			transition.name = "Transition " + fromNode.name + "-" + toNode.name;
			transition.startNode = fromNode;
			transition.endNode = toNode;
			transition.conditions = new List<Func<Transition, bool>> ();

			fromNode.transitions.Add (transition);
			toNode.transitions.Add (transition);

			return transition;
		}

		public void Delete () 
		{
			startNode.transitions.Remove (this);
			endNode.transitions.Remove (this);
			UnityEngine.Object.DestroyImmediate (this);
		}
	}
}