using UnityEngine;
using System;
using System.Collections.Generic;

namespace NodeEditorFramework
{
	public class Transition : ScriptableObject
	{
		public Node StartNode;
		public Node EndNode;

		public List<Func<Transition, bool>> Conditions;

		public float TransitionTime;

		public bool ConditionsMet () 
		{
			for (var condCnt = 0; condCnt < Conditions.Count; condCnt++) 
			{
				if (!Conditions[condCnt].Invoke (this))
					return false;
			}
			return true;
		}

		public static Transition Create (Node fromNode, Node toNode) 
		{
			if (fromNode.AcceptsTranstitions == false || toNode.AcceptsTranstitions == false || fromNode == toNode)
				return null;

			var transition = CreateInstance<Transition> ();
			transition.name = "Transition " + fromNode.name + "-" + toNode.name;
			transition.StartNode = fromNode;
			transition.EndNode = toNode;
			transition.Conditions = new List<Func<Transition, bool>> ();

			fromNode.Transitions.Add (transition);
			toNode.Transitions.Add (transition);

			return transition;
		}

		public void Delete () 
		{
			StartNode.Transitions.Remove (this);
			EndNode.Transitions.Remove (this);
			DestroyImmediate (this);
		}
	}
}