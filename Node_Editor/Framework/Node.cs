using UnityEngine;
using System;
using System.Collections.Generic;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework
{
	public abstract class Node : ScriptableObject
	{
		public Rect rect = new Rect ();
		internal Vector2 contentOffset = Vector2.zero;

		// Calculation graph
		public List<NodeInput> Inputs = new List<NodeInput>();
		public List<NodeOutput> Outputs = new List<NodeOutput>();
		[HideInInspector]
		[NonSerialized]
		internal bool calculated = true;
		
		// State graph
		public List<Transition> transitions = new List<Transition> ();

		#region General

		/// <summary>
		/// Get the ID of the Node
		/// </summary>
		public abstract string GetID { get; }

		/// <summary>
		/// Should we allow recursion? Recursion is allowed if atleast a single Node in the loop allows for recursion
		/// </summary>
		public virtual bool AllowRecursion { get { return false; } }
		/// <summary>
		/// After the Calculate function is called on this node, should the Nodes afterwards be calculated?
		/// </summary>
		public virtual bool ContinueCalculation { get { return true; } }
		/// <summary>
		/// Does this Node accepts Transitions?
		/// </summary>
		public virtual bool AcceptsTranstitions { get { return false; } }

		/// <summary>
		/// Function implemented by the children to create the node
		/// </summary>
		/// <param name="pos">Position.</param>
		public abstract Node Create (Vector2 pos);
		
		/// <summary>
		/// Function implemented by the children to draw the node
		/// </summary>
		public abstract void NodeGUI ();
		
		/// <summary>
		/// Function implemented by the children to calculate their outputs
		/// Should return Success/Fail
		/// </summary>
		public abstract bool Calculate ();
		
		/// <summary>
		/// Optional callback when the node is deleted
		/// </summary>
		protected internal virtual void OnDelete () {}
		/// <summary>
		/// Optional callback when the NodeInput input was assigned a new connection
		/// </summary>
		protected internal virtual void OnAddInputConnection (NodeInput input) {}
		/// <summary>
		/// Optional callback when the NodeOutput output was assigned a new connection (the last in the list)
		/// </summary>
		protected internal virtual void OnAddOutputConnection (NodeOutput output) {}
		/// <summary>
		/// Optional callback when the transition was created
		/// </summary>
		protected internal virtual void OnAddTransition (Transition transition) {}


		/// <summary>
		/// Init the Node Base after the Node has been created. This includes adding to canvas, and to calculate for the first time
		/// </summary>
		protected internal void InitBase () 
		{
			Calculate ();
			if (!NodeEditor.curNodeCanvas.nodes.Contains (this))
				NodeEditor.curNodeCanvas.nodes.Add (this);
			#if UNITY_EDITOR
			if (name == "")
				name = UnityEditor.ObjectNames.NicifyVariableName (GetID);
			#endif
		}

		/// <summary>
		/// Deletes this Node from curNodeCanvas
		/// </summary>
		public void Delete () 
		{
			if (!NodeEditor.curNodeCanvas.nodes.Contains (this))
				throw new UnityException ("The Node " + name + " does not exist on the Canvas " + NodeEditor.curNodeCanvas.name + "!");
			NodeEditor.curNodeCanvas.nodes.Remove (this);
			for (int outCnt = 0; outCnt < Outputs.Count; outCnt++) 
			{
				NodeOutput output = Outputs [outCnt];
				while (output.connections.Count != 0)
					RemoveConnection (output.connections[0]);
				DestroyImmediate (output, true);
			}
			for (int inCnt = 0; inCnt < Inputs.Count; inCnt++) 
			{
				NodeInput input = Inputs [inCnt];
				if (input.connection != null)
					input.connection.connections.Remove (input);
				DestroyImmediate (input, true);
			}
			NodeEditorCallbacks.IssueOnDeleteNode (this);
			DestroyImmediate (this, true);
		}

		#endregion

		#region Drawing

		/// <summary>
		/// Draws the node. Depends on curEditorState. Can be overridden by an node type.
		/// </summary>
		protected internal virtual void DrawNode () 
		{
			// TODO: Node Editor Feature: Custom Windowing System
			Rect nodeRect = rect;
			nodeRect.position += NodeEditor.curEditorState.zoomPanAdjust;
			contentOffset = new Vector2 (0, 20);

			Rect headerRect = new Rect (nodeRect.x, nodeRect.y, nodeRect.width, contentOffset.y);
			GUI.Label (headerRect, name, NodeEditor.curEditorState.selectedNode == this? NodeEditorGUI.nodeBoxBold : NodeEditorGUI.nodeBox);

			Rect bodyRect = new Rect (nodeRect.x, nodeRect.y + contentOffset.y, nodeRect.width, nodeRect.height - contentOffset.y);
			GUI.changed = false;

			GUI.BeginGroup (bodyRect, GUI.skin.box);
			bodyRect.position = Vector2.zero;
			GUILayout.BeginArea (bodyRect, GUI.skin.box);

			NodeGUI ();

			GUILayout.EndArea ();
			GUI.EndGroup ();
		}

		/// <summary>
		/// Draws the node knobs; splitted from curves because of the render order
		/// </summary>
		protected internal virtual void DrawKnobs () 
		{
			for (int outCnt = 0; outCnt < Outputs.Count; outCnt++) 
			{
				NodeOutput output = Outputs[outCnt];
				Rect knobRect = output.GetGUIKnob ();
				GUI.DrawTexture (knobRect, output.knobTexture);
			}
			for (int inCnt = 0; inCnt < Inputs.Count; inCnt++) 
			{
				NodeInput input = Inputs[inCnt];
				Rect knobRect = input.GetGUIKnob ();
				GUI.DrawTexture (knobRect, input.knobTexture);
			}
		}
		/// <summary>
		/// Draws the node curves; splitted from knobs because of the render order
		/// </summary>
		protected internal virtual void DrawConnections () 
		{
			for (int outCnt = 0; outCnt < Outputs.Count; outCnt++) 
			{
				NodeOutput output = Outputs [outCnt];
				Vector2 startPos = output.GetGUIKnob ().center;
				Vector2 startDir = output.GetDirection ();

				for (int conCnt = 0; conCnt < output.connections.Count; conCnt++) 
				{
					NodeInput input = output.connections [conCnt];
					NodeEditorGUI.DrawConnection (startPos,
					                           startDir,
					                           input.GetGUIKnob ().center,
					                           input.GetDirection (),
					                           ConnectionTypes.GetTypeData (output.type).col);
				}
			}
		}
		
		/// <summary>
		/// Draws the node transitions.
		/// </summary>
		public void DrawTransitions () 
		{
			for (int cnt = 0; cnt < transitions.Count; cnt++)
			{
				Vector2 StartPoint = transitions[cnt].startNode.rect.center + NodeEditor.curEditorState.zoomPanAdjust;
				Vector2 EndPoint = transitions[cnt].endNode.rect.center + NodeEditor.curEditorState.zoomPanAdjust;
				RTEditorGUI.DrawLine (StartPoint, EndPoint, Color.grey, null, 3);
				
				Rect selectRect = new Rect (0, 0, 20, 20);
				selectRect.center = Vector2.Lerp (StartPoint, EndPoint, 0.5f);
				
				if (GUI.Button (selectRect, "#"))
				{
					// TODO: Select
				}
				
			}
		}

		#endregion
		
		#region Node Calculation Utility
		
		/// <summary>
		/// Checks if there are no unassigned and no null-value inputs.
		/// </summary>
		protected internal bool allInputsReady ()
		{
			for (int inCnt = 0; inCnt < Inputs.Count; inCnt++) 
			{
				if (Inputs[inCnt].connection == null || Inputs[inCnt].connection.IsValueNull)
					return false;
			}
			return true;
		}
		/// <summary>
		/// Checks if there are any unassigned inputs.
		/// </summary>
		protected internal bool hasUnassignedInputs () 
		{
			for (int inCnt = 0; inCnt < Inputs.Count; inCnt++)
				if (Inputs [inCnt].connection == null)
					return true;
			return false;
		}
		
		/// <summary>
		/// Returns whether every direct dexcendant has been calculated
		/// </summary>
		protected internal bool descendantsCalculated () 
		{
			for (int cnt = 0; cnt < Inputs.Count; cnt++) 
			{
				if (Inputs [cnt].connection != null && !Inputs [cnt].connection.body.calculated)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Returns whether the node acts as an input (no inputs or no inputs assigned)
		/// </summary>
		protected internal bool isInput () 
		{
			for (int cnt = 0; cnt < Inputs.Count; cnt++)
				if (Inputs [cnt].connection != null)
					return false;
			return true;
		}

		#endregion

		#region Node Knob Utility

		// -- OUTPUTS --

		/// <summary>
		/// Creates and output on your Node of the given type.
		/// </summary>
		public void CreateOutput (string outputName, string outputType)
		{
			NodeOutput.Create (this, outputName, outputType);
		}
		/// <summary>
		/// Creates and output on this Node of the given type at the specified NodeSide.
		/// </summary>
		public void CreateOutput (string outputName, string outputType, NodeSide nodeSide)
		{
			NodeOutput.Create (this, outputName, outputType, nodeSide);
		}
		/// <summary>
		/// Creates and output on this Node of the given type at the specified NodeSide and position.
		/// </summary>
		public void CreateOutput (string outputName, string outputType, NodeSide nodeSide, float sidePosition)
		{
			NodeOutput.Create (this, outputName, outputType, nodeSide, sidePosition);
		}

		/// <summary>
		/// Aligns the OutputKnob on it's NodeSide with the last GUILayout control drawn.
		/// </summary>
		/// <param name="outputIdx">The index of the output in the Node's Outputs list</param>
		protected void OutputKnob (int outputIdx)
		{
			if (Event.current.type == EventType.Repaint)
				Outputs[outputIdx].SetPosition ();
		}

		/// <summary>
		/// Returns the output knob that is at the position on this node or null
		/// </summary>
		public NodeOutput GetOutputAtPos (Vector2 pos) 
		{
			for (int outCnt = 0; outCnt < Outputs.Count; outCnt++) 
			{ // Search for an output at the position
				if (Outputs [outCnt].GetScreenKnob ().Contains (new Vector3 (pos.x, pos.y)))
					return Outputs [outCnt];
			}
			return null;
		}


		// -- INPUTS --

		/// <summary>
		/// Creates and input on your Node of the given type.
		/// </summary>
		public void CreateInput (string inputName, string inputType)
		{
			NodeInput.Create (this, inputName, inputType);
		}
		/// <summary>
		/// Creates and input on this Node of the given type at the specified NodeSide.
		/// </summary>
		public void CreateInput (string inputName, string inputType, NodeSide nodeSide)
		{
			NodeInput.Create (this, inputName, inputType, nodeSide);
		}
		/// <summary>
		/// Creates and input on this Node of the given type at the specified NodeSide and position.
		/// </summary>
		public void CreateInput (string inputName, string inputType, NodeSide nodeSide, float sidePosition)
		{
			NodeInput.Create (this, inputName, inputType, nodeSide, sidePosition);
		}

		/// <summary>
		/// Aligns the InputKnob on it's NodeSide with the last GUILayout control drawn.
		/// </summary>
		/// <param name="inputIdx">The index of the input in the Node's Inputs list</param>
		protected void InputKnob (int inputIdx)
		{
			if (Event.current.type == EventType.Repaint)
				Inputs[inputIdx].SetPosition ();
		}

		/// <summary>
		/// Returns the input knob that is at the position on this node or null
		/// </summary>
		public NodeInput GetInputAtPos (Vector2 pos) 
		{
			for (int inCnt = 0; inCnt < Inputs.Count; inCnt++) 
			{ // Search for an input at the position
				if (Inputs [inCnt].GetScreenKnob ().Contains (new Vector3 (pos.x, pos.y)))
					return Inputs [inCnt];
			}
			return null;
		}

		#endregion

		#region Recursive Search Utility

		/// <summary>
		/// Recursively checks whether this node is a child of the other node
		/// </summary>
		public bool isChildOf (Node otherNode)
		{
			if (otherNode == null || otherNode == this)
				return false;
			if (BeginRecursiveSearchLoop ()) return false;
			for (int cnt = 0; cnt < Inputs.Count; cnt++) 
			{
				NodeOutput connection = Inputs [cnt].connection;
				if (connection != null) 
				{
					if (connection.body != startRecursiveSearchNode)
					{
						if (connection.body == otherNode || connection.body.isChildOf (otherNode))
						{
							StopRecursiveSearchLoop ();
							return true;
						}
					}
				}
			}
			EndRecursiveSearchLoop ();
			return false;
		}

		/// <summary>
		/// Recursively checks whether this node is in a loop
		/// </summary>
		internal bool isInLoop ()
		{
			if (BeginRecursiveSearchLoop ()) return this == startRecursiveSearchNode;
			for (int cnt = 0; cnt < Inputs.Count; cnt++) 
			{
				NodeOutput connection = Inputs [cnt].connection;
				if (connection != null) 
				{
					if (connection.body.isInLoop ())
					{
						StopRecursiveSearchLoop ();
						return true;
					}
				}
			}
			EndRecursiveSearchLoop ();
			return false;
		}

		/// <summary>
		/// Recursively checks whether any node in the loop to be made allows recursion.
		/// Other node is the node this node needs connect to in order to fill the loop (other node being the node coming AFTER this node).
		/// That means isChildOf has to be confirmed before calling this!
		/// </summary>
		internal bool allowsLoopRecursion (Node otherNode)
		{
			if (AllowRecursion)
				return true;
			if (otherNode == null)
				return false;
			if (BeginRecursiveSearchLoop ()) return false;
			for (int cnt = 0; cnt < Inputs.Count; cnt++) 
			{
				NodeOutput connection = Inputs [cnt].connection;
				if (connection != null) 
				{
					if (connection.body != startRecursiveSearchNode)
					{
						if (connection.body.allowsLoopRecursion (otherNode))
						{
							StopRecursiveSearchLoop ();
							return true;
						}
					}
				}
			}
			EndRecursiveSearchLoop ();
			return false;
		}

		/// <summary>
		/// A recursive function to clear all calculations depending on this node.
		/// Usually does not need to be called manually
		/// </summary>
		public void ClearCalculation () 
		{
			if (BeginRecursiveSearchLoop ()) return;
			calculated = false;
			for (int outCnt = 0; outCnt < Outputs.Count; outCnt++)
			{
				NodeOutput output = Outputs [outCnt];
				for (int conCnt = 0; conCnt < output.connections.Count; conCnt++)
					output.connections [conCnt].body.ClearCalculation ();
			}
			EndRecursiveSearchLoop ();
		}

		#region Recursive Search Helpers

		private List<Node> recursiveSearchSurpassed;
		private Node startRecursiveSearchNode; // Temporary start node for recursive searches

		/// <summary>
		/// Begins the recursive search loop and returns whether this node has already been searched
		/// </summary>
		internal bool BeginRecursiveSearchLoop ()
		{
			if (startRecursiveSearchNode == null || recursiveSearchSurpassed == null) 
			{ // Start search
				recursiveSearchSurpassed = new List<Node> ();
				startRecursiveSearchNode = this;
			}

			if (recursiveSearchSurpassed.Contains (this))
				return true;
			recursiveSearchSurpassed.Add (this);
			return false;
		}

		/// <summary>
		/// Ends the recursive search loop if this was the start node
		/// </summary>
		internal void EndRecursiveSearchLoop () 
		{
			if (startRecursiveSearchNode == this) 
			{ // End search
				recursiveSearchSurpassed = null;
				startRecursiveSearchNode = null;
			}
		}

		/// <summary>
		/// Stops the recursive search loop immediately. Call when you found what you needed.
		/// </summary>
		internal void StopRecursiveSearchLoop () 
		{
			recursiveSearchSurpassed = null;
			startRecursiveSearchNode = null;
		}

		#endregion

		#endregion

		#region Static Connection Utility

		/// <summary>
		/// Check if an output and an input can be connected (same type, ...)
		/// </summary>
		public static bool CanApplyConnection (NodeOutput output, NodeInput input)
		{
			if (input == null || output == null)
				return false;
			if (input.body == output.body || input.connection == output)
				return false;
			if (input.type != output.type)
				return false;

			bool isRecursive = output.body.isChildOf (input.body);
			if (isRecursive) 
			{
				if (!output.body.allowsLoopRecursion (input.body))
				{
					// TODO: Generic Notification
					Debug.LogWarning ("Cannot apply connection: Recursion detected!");
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Applies a connection between output and input. 'CanApplyConnection' has to be checked before
		/// </summary>
		public static void ApplyConnection (NodeOutput output, NodeInput input)
		{
			if (input != null && output != null) 
			{
				if (input.connection != null)
					input.connection.connections.Remove (input);
				input.connection = output;
				output.connections.Add (input);

				NodeEditor.RecalculateFrom (input.body);
				output.body.OnAddOutputConnection (output);
				input.body.OnAddInputConnection (input);
				NodeEditorCallbacks.IssueOnAddConnection (input);
			}
		}

		/// <summary>
		/// Removes the connection from NodeInput.
		/// </summary>
		public static void RemoveConnection (NodeInput input)
		{
			NodeEditorCallbacks.IssueOnRemoveConnection (input);
			input.connection.connections.Remove (input);
			input.connection = null;
			NodeEditor.RecalculateFrom (input.body);
		}

		public static void CreateTransition (Node fromNode, Node toNode) 
		{
			Transition trans = Transition.Create (fromNode, toNode);
			if (trans != null)
			{
				fromNode.OnAddTransition (trans);
				toNode.OnAddTransition (trans);
			}
		}

		#endregion
	}
}
