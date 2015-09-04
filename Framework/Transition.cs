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

		public static Transition Create (Node from, Node to) 
		{
			if (NodeTypes.getNodeData (from).transitions == false || NodeTypes.getNodeData (to).transitions == false)
				return null;
			Transition transition = CreateInstance<Transition> ();
			transition.name = "Transition " + from.name + "-" + to.name;
			transition.startNode = from;
			transition.endNode = to;
			transition.conditions = new List<Func<Transition, bool>> ();
			return transition;
		}
	}
}