using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

using NodeEditorFramework.Utilities;

namespace NodeEditorFramework
{
	/// <summary>
	/// Represents a Transition between two nodes [states] with conditions and a transition time. WIP
	/// </summary>
	[Serializable]
	public class Transition : ScriptableObject
	{
		// Unfortunately, unity cannot serialize properties, so we create a serialized backing variable
		public Node startNode { get { return _startNode; } internal set { _startNode = value; } }
		[SerializeField]
		internal Node _startNode;
		public Node endNode { get { return _endNode; } internal set { _endNode = value; } }
		[SerializeField]
		internal Node _endNode;

		[SerializeField]
		public List<TransitionCondition> conditions = new List<TransitionCondition> ();
		//public List<Func<Transition, bool>> conditions;

		public bool isTransitioning { get; private set; }
		private float startTransitioningTime;
		public float transitionTime = 2; // In seconds

		private static float totalTime { get {
				#if UNITY_EDITOR
				return (float)UnityEditor.EditorApplication.timeSinceStartup;
				#else
				return Time.time;
				#endif
			} }

		#region General

		/// <summary>
		/// Creates a Transition between the given Nodes with a transitioning time of two secs
		/// </summary>
		public static Transition Create (Node fromNode, Node toNode) 
		{
			return Create (fromNode, toNode, 2);
		}

		/// <summary>
		/// Creates a Transition between the given Nodes with the given transitioning time in secs
		/// </summary>
		public static Transition Create (Node fromNode, Node toNode, float TransitionTimeInSec) 
		{
			if (fromNode.AcceptsTranstitions == false || toNode.AcceptsTranstitions == false || fromNode == toNode)
				return null;

			Transition transition = CreateInstance<Transition> ();
			transition.name = "Transition " + fromNode.name + "-" + toNode.name;
			transition.startNode = fromNode;
			transition.endNode = toNode;
			transition.transitionTime = TransitionTimeInSec;

			fromNode.transitions.Add (transition);
			toNode.transitions.Add (transition);

			return transition;
		}

		/// <summary>
		/// Deletes this Transition from the Nodes
		/// </summary>
		public void Delete () 
		{
			NodeEditorCallbacks.IssueOnRemoveTransition (this);
			startNode.transitions.Remove (this);
			endNode.transitions.Remove (this);
			UnityEngine.Object.DestroyImmediate (this, true);
		}

		#endregion

		#region Transition Logic

		/// <summary>
		/// Returns whether all conditions for this transition to start are met
		/// </summary>
		public bool conditionsMet () 
		{
			for (int condCnt = 0; condCnt < conditions.Count; condCnt++) 
			{
				if (!conditions[condCnt].Invoke (this))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Starts the transitioning process with timing
		/// </summary>
		public void startTransition () 
		{
			startTransitioningTime = totalTime;
			isTransitioning = true;
		}

		/// <summary>
		/// Returns the transitioning process based on timing
		/// </summary>
		public float transitionProgress () 
		{
			if (!isTransitioning)
				throw new UnityException ("Can't check transitioning progress when not transitioning!");
			//Debug.Log ("TransitionProgress = ( CurTime:" + totalTime + " - StartTime:" + startTransitioningTime + " ) / TimeLenght:" + transitionTime + " = " + ((totalTime-startTransitioningTime)/transitionTime));
			return (totalTime-startTransitioningTime)/transitionTime;
		}

		/// <summary>
		/// Returns whether the transitioning process has finished and automatically stops it if true
		/// </summary>
		public bool finishedTransition () 
		{
			if (!isTransitioning)
				throw new UnityException ("Can't check whether transitioning finished when not transitioning!");
			bool finished = (totalTime-startTransitioningTime) >= transitionTime;
			if (finished)
				stopTransition ();
			return finished;
		}

		/// <summary>
		/// Stops the transitioning process
		/// </summary>
		public void stopTransition () 
		{
			isTransitioning = false;
			startTransitioningTime = 0;
		}

		#endregion

		#region Drawing

		/// <summary>
		/// Draws this transition as a line and a button to select it. Has to be called from the start node
		/// </summary>
		public void DrawFromStartNode ()
		{
			Vector2 StartPoint = startNode.rect.center + NodeEditor.curEditorState.zoomPanAdjust;
			Vector2 EndPoint = endNode.rect.center + NodeEditor.curEditorState.zoomPanAdjust;

			if (isTransitioning)
				RTEditorGUI.DrawLine (StartPoint, Vector2.Lerp (StartPoint, EndPoint, transitionProgress ()), Color.cyan, null, 4);
			RTEditorGUI.DrawLine (StartPoint, EndPoint, Color.grey, null, 3);

			Rect selectRect = new Rect (0, 0, 20, 20);
			selectRect.center = Vector2.Lerp (StartPoint, EndPoint, 0.5f);
			if (GUI.Button (selectRect, "#"))
			{
				Debug.Log ("Selected " + name);
				// TODO: Select
		#if UNITY_EDITOR
				UnityEditor.Selection.activeObject = this;
		#endif
			}
		}

		#endregion
	}
}