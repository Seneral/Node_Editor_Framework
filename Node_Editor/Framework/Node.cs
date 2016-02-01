using UnityEngine;
using System;
using System.Collections.Generic;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework
{
	public abstract class Node : ScriptableObject
	{
		public Rect Rect = new Rect ();
		internal Vector2 ContentOffset = Vector2.zero;

		// Calculation graph
		public List<NodeInput> Inputs = new List<NodeInput>();
		public List<NodeOutput> Outputs = new List<NodeOutput>();
		[HideInInspector]
		[NonSerialized]
		internal bool Calculated = true;
		
		// State graph
		public List<Transition> Transitions = new List<Transition> ();

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
			if (!NodeEditor.CurNodeCanvas.nodes.Contains (this))
				NodeEditor.CurNodeCanvas.nodes.Add (this);
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
			if (!NodeEditor.CurNodeCanvas.nodes.Contains (this))
				throw new UnityException ("The Node " + name + " does not exist on the Canvas " + NodeEditor.CurNodeCanvas.name + "!");
			NodeEditor.CurNodeCanvas.nodes.Remove (this);
			for (int outCnt = 0; outCnt < Outputs.Count; outCnt++) 
			{
				NodeOutput output = Outputs [outCnt];
				while (output.Connections.Count != 0)
					RemoveConnection (output.Connections[0]);
				DestroyImmediate (output, true);
			}
			for (int inCnt = 0; inCnt < Inputs.Count; inCnt++) 
			{
				NodeInput input = Inputs [inCnt];
				if (input.Connection != null)
					input.Connection.Connections.Remove (input);
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
			Rect nodeRect = Rect;
			nodeRect.position += NodeEditor.CurEditorState.ZoomPanAdjust;
			ContentOffset = new Vector2 (0, 20);

			Rect headerRect = new Rect (nodeRect.x, nodeRect.y, nodeRect.width, ContentOffset.y);
			GUI.Label (headerRect, name, NodeEditor.CurEditorState.SelectedNode == this? NodeEditorGUI.NodeBoxBold : NodeEditorGUI.NodeBox);

			Rect bodyRect = new Rect (nodeRect.x, nodeRect.y + ContentOffset.y, nodeRect.width, nodeRect.height - ContentOffset.y);
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
				GUI.DrawTexture (knobRect, output.KnobTexture);
			}
			for (int inCnt = 0; inCnt < Inputs.Count; inCnt++) 
			{
				NodeInput input = Inputs[inCnt];
				Rect knobRect = input.GetGUIKnob ();
				GUI.DrawTexture (knobRect, input.KnobTexture);
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

				for (int conCnt = 0; conCnt < output.Connections.Count; conCnt++) 
				{
					NodeInput input = output.Connections [conCnt];
					NodeEditorGUI.DrawConnection (startPos,
					                           startDir,
					                           input.GetGUIKnob ().center,
					                           input.GetDirection (),
					                           ConnectionTypes.GetTypeData (output.Type).Col);
				}
			}
		}
		
		/// <summary>
		/// Draws the node transitions.
		/// </summary>
		public void DrawTransitions () 
		{
			for (int cnt = 0; cnt < Transitions.Count; cnt++)
			{
				Vector2 startPoint = Transitions[cnt].StartNode.Rect.center + NodeEditor.CurEditorState.ZoomPanAdjust;
				Vector2 endPoint = Transitions[cnt].EndNode.Rect.center + NodeEditor.CurEditorState.ZoomPanAdjust;
				RTEditorGUI.DrawLine (startPoint, endPoint, Color.grey, null, 3);

			    Rect selectRect = new Rect(0, 0, 20, 20) {center = Vector2.Lerp(startPoint, endPoint, 0.5f)};

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
		protected internal bool AllInputsReady ()
		{
			for (var inCnt = 0; inCnt < Inputs.Count; inCnt++) 
			{
				if (Inputs[inCnt].Connection == null || Inputs[inCnt].Connection.IsValueNull)
					return false;
			}
			return true;
		}
		/// <summary>
		/// Checks if there are any unassigned inputs.
		/// </summary>
		protected internal bool HasUnassignedInputs () 
		{
			for (var inCnt = 0; inCnt < Inputs.Count; inCnt++)
				if (Inputs [inCnt].Connection == null)
					return true;
			return false;
		}
		
		/// <summary>
		/// Returns whether every direct dexcendant has been calculated
		/// </summary>
		protected internal bool DescendantsCalculated () 
		{
			for (var cnt = 0; cnt < Inputs.Count; cnt++) 
			{
				if (Inputs [cnt].Connection != null && !Inputs [cnt].Connection.Body.Calculated)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Returns whether the node acts as an input (no inputs or no inputs assigned)
		/// </summary>
		protected internal bool IsInput () 
		{
			for (var cnt = 0; cnt < Inputs.Count; cnt++)
				if (Inputs [cnt].Connection != null)
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
		public bool IsChildOf (Node otherNode)
		{
			if (otherNode == null || otherNode == this)
				return false;
			if (BeginRecursiveSearchLoop ()) return false;
			for (int cnt = 0; cnt < Inputs.Count; cnt++) 
			{
				NodeOutput connection = Inputs [cnt].Connection;
				if (connection != null) 
				{
					if (connection.Body != startRecursiveSearchNode)
					{
						if (connection.Body == otherNode || connection.Body.IsChildOf (otherNode))
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
		internal bool IsInLoop ()
		{
			if (BeginRecursiveSearchLoop ()) return this == startRecursiveSearchNode;
			for (int cnt = 0; cnt < Inputs.Count; cnt++) 
			{
				NodeOutput connection = Inputs [cnt].Connection;
				if (connection != null) 
				{
					if (connection.Body.IsInLoop ())
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
		internal bool AllowsLoopRecursion (Node otherNode)
		{
			if (AllowRecursion)
				return true;
			if (otherNode == null)
				return false;
			if (BeginRecursiveSearchLoop ()) return false;
			for (int cnt = 0; cnt < Inputs.Count; cnt++) 
			{
				NodeOutput connection = Inputs [cnt].Connection;
				if (connection != null) 
				{
					if (connection.Body != startRecursiveSearchNode)
					{
						if (connection.Body.AllowsLoopRecursion (otherNode))
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
			Calculated = false;
			for (int outCnt = 0; outCnt < Outputs.Count; outCnt++)
			{
				NodeOutput output = Outputs [outCnt];
				for (int conCnt = 0; conCnt < output.Connections.Count; conCnt++)
					output.Connections [conCnt].Body.ClearCalculation ();
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
			if (input.Body == output.Body || input.Connection == output)
				return false;
			if (input.Type != output.Type)
				return false;

			bool isRecursive = output.Body.IsChildOf (input.Body);
			if (isRecursive) 
			{
				if (!output.Body.AllowsLoopRecursion (input.Body))
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
				if (input.Connection != null)
					input.Connection.Connections.Remove (input);
				input.Connection = output;
				output.Connections.Add (input);

				NodeEditor.RecalculateFrom (input.Body);
				output.Body.OnAddOutputConnection (output);
				input.Body.OnAddInputConnection (input);
				NodeEditorCallbacks.IssueOnAddConnection (input);
			}
		}

		/// <summary>
		/// Removes the connection from NodeInput.
		/// </summary>
		public static void RemoveConnection (NodeInput input)
		{
			NodeEditorCallbacks.IssueOnRemoveConnection (input);
			input.Connection.Connections.Remove (input);
			input.Connection = null;
			NodeEditor.RecalculateFrom (input.Body);
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
