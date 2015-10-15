using UnityEngine;
using System;
using System.Collections.Generic;
using NodeEditorFramework;

namespace NodeEditorFramework
{
	public abstract class Node : ScriptableObject
	{
		public Rect rect = new Rect ();

		// Calculation graph
		public List<NodeInput> Inputs = new List<NodeInput>();
		public List<NodeOutput> Outputs = new List<NodeOutput>();
		[HideInInspector]
		public bool calculated = true;

		public bool allowRecursion = false;
		public bool shouldCalculate = true;

		// State graph
		public List<Transition> transitions = new List<Transition> ();

		#region Abstract Member

		// Abstract member to get the ID of the node
		public abstract string GetID { get; }
		
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
		public virtual void OnDelete () {}

		#endregion

		#region Drawing

		/// <summary>
		/// Draws the node. Depends on curEditorState. Can be overridden by an node type.
		/// </summary>
		public virtual void DrawNode () 
		{
			// TODO: Node Editor Feature: Custom Windowing System
			Rect nodeRect = rect;
			nodeRect.position += NodeEditor.curEditorState.zoomPanAdjust;
			float headerHeight = 20;
			Rect headerRect = new Rect (nodeRect.x, nodeRect.y, nodeRect.width, headerHeight);
			Rect bodyRect = new Rect (nodeRect.x, nodeRect.y + headerHeight, nodeRect.width, nodeRect.height - headerHeight);
			
			GUIStyle headerStyle = new GUIStyle (GUI.skin.box);
			if (NodeEditor.curEditorState.activeNode == this)
				headerStyle.fontStyle = FontStyle.Bold;
			GUI.Label (headerRect, new GUIContent (name), headerStyle);
			
			GUI.changed = false;
			GUILayout.BeginArea (bodyRect, GUI.skin.box);
			NodeGUI ();
			GUILayout.EndArea ();
		}

		/// <summary>
		/// Draws the node knobs; splitted from curves because of the render order
		/// </summary>
		public void DrawKnobs () 
		{
			for (int outCnt = 0; outCnt < Outputs.Count; outCnt++) 
			{
				NodeOutput output = Outputs[outCnt];
				Rect knobRect = output.GetGUIKnob ();
//				Matrix4x4 GUIMatrix = GUI.matrix;
//				if (output.side != NodeSide.Right)
//					GUIUtility.RotateAroundPivot (output.GetRotation (), knobRect.center);
				GUI.DrawTexture (knobRect, output.knobTexture);
//				GUI.matrix = GUIMatrix;
			}
			for (int inCnt = 0; inCnt < Inputs.Count; inCnt++) 
			{
				NodeInput input = Inputs[inCnt];
				Rect knobRect = input.GetGUIKnob ();
//				Matrix4x4 GUIMatrix = GUI.matrix;
//				if (input.side != NodeSide.Left)
//					GUIUtility.RotateAroundPivot (input.GetRotation (), knobRect.center);
				GUI.DrawTexture (knobRect, input.knobTexture);
//				GUI.matrix = GUIMatrix;
			}
		}
		/// <summary>
		/// Draws the node curves; splitted from knobs because of the render order
		/// </summary>
		public void DrawConnections () 
		{
			for (int outCnt = 0; outCnt < Outputs.Count; outCnt++) 
			{
				NodeOutput output = Outputs [outCnt];
				Vector2 startPos = output.GetGUIKnob ().center;
				Vector2 startDir = output.GetDirection ();

				for (int conCnt = 0; conCnt < output.connections.Count; conCnt++) 
				{
					NodeInput input = output.connections [conCnt];
					NodeEditor.DrawConnection (startPos,
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
				NodeEditorGUI.DrawLine (StartPoint, EndPoint, Color.grey, null, 3);
				
				Rect selectRect = new Rect (0, 0, 20, 20);
				selectRect.center = Vector2.Lerp (StartPoint, EndPoint, 0.5f);
				
				if (GUI.Button (selectRect, "#"))
				{
					// TODO: Select
				}
				
			}
		}
	
		#endregion

		#region Node Functions

		/// <summary>
		/// Init this node
		/// </summary>
		public void InitBase () 
		{
			if (shouldCalculate) 
				Calculate ();

			NodeEditor.curNodeCanvas.nodes.Add (this);
			#if UNITY_EDITOR
			if (name == "")
			{
				name = UnityEditor.ObjectNames.NicifyVariableName (GetID);
			}
			#endif
		}

		/// <summary>
		/// Deletes this Node from curNodeCanvas
		/// </summary>
		public void Delete () 
		{
			NodeEditor.curNodeCanvas.nodes.Remove (this);
			for (int outCnt = 0; outCnt < Outputs.Count; outCnt++) 
			{
				NodeOutput output = Outputs [outCnt];
				for (int conCnt = 0; conCnt < output.connections.Count; conCnt++) 
					output.connections [conCnt].connection = null;
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
		
		#region Instance Utility
		
		/// <summary>
		/// Checks if there are no unassigned and no null-value inputs.
		/// </summary>
		public bool allInputsReady () 
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
		public bool hasUnassignedInputs () 
		{
			for (int inCnt = 0; inCnt < Inputs.Count; inCnt++) 
			{
				if (Inputs [inCnt].connection == null)
					return true;
			}
			return false;
		}
		
		/// <summary>
		/// Returns whether every node this node depends on has been calculated
		/// </summary>
		public bool descendantsCalculated () 
		{
			for (int cnt = 0; cnt < Inputs.Count; cnt++) 
			{
				if (Inputs [cnt].connection != null && !Inputs [cnt].connection.body.calculated)
					return false;
			}
			return true;
		}
		
		/// <summary>
		/// Recursively checks whether this node is a child of the other node
		/// </summary>
		public bool isChildOf (Node otherNode)
		{
			if (otherNode == null)
				return false;
			for (int cnt = 0; cnt < Inputs.Count; cnt++) 
			{
				if (Inputs [cnt].connection != null) 
				{
					if (Inputs [cnt].connection.body == otherNode)
						return true;
					else if (Inputs [cnt].connection.body.isChildOf (otherNode)) // Recursively searching
						return true;
				}
			}
			return false;
		}
		
		/// <summary>
		/// Call this method in your NodeGUI to setup an output knob aligning with the y position of the last GUILayout control drawn.
		/// </summary>
		/// <param name="outputIdx">The index of the output in the Node's Outputs list</param>
		protected void OutputKnob (int outputIdx)
		{
			if (Event.current.type == EventType.Repaint)
				Outputs[outputIdx].SetPosition ();
		}
		
		/// <summary>
		/// Call this method in your NodeGUI to setup an input knob aligning with the y position of the last GUILayout control drawn.
		/// </summary>
		/// <param name="inputIdx">The index of the input in the Node's Inputs list</param>
		protected void InputKnob (int inputIdx)
		{
			if (Event.current.type == EventType.Repaint)
				Inputs[inputIdx].SetPosition ();
		}
		
		/// <summary>
		/// Call this method to create an output on your node
		/// </summary>
		public void CreateOutput(string outputName, string outputType)
		{
			NodeOutput.Create(this, outputName, outputType);
		}
		
		/// <summary>
		/// Call this method to create an input on your node
		/// </summary>
		public void CreateInput(string inputName, string inputType)
		{
			NodeInput.Create(this, inputName, inputType);
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

		#endregion

		#region Static Utility

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

			if ((!output.body.allowRecursion || !input.body.allowRecursion) && output.body.isChildOf (input.body)) 
			{
				// TODO: Generic Notification
				//ShowNotification (new GUIContent ("Recursion detected!"));
				return false;
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
				{
					input.connection.connections.Remove (input);
				}
				input.connection = output;
				output.connections.Add (input);

				if (input.body.shouldCalculate)
					NodeEditor.RecalculateFrom (input.body);

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
			if (input.body.shouldCalculate)
				NodeEditor.RecalculateFrom (input.body);
		}

		public static void CreateTransition (Node from, Node to) 
		{
			from.transitions.Add (Transition.Create (from, to));
		}

		#endregion
	}
}
