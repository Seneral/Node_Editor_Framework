using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

public abstract class Node : ScriptableObject
{
	public Rect rect = new Rect ();
	public List<NodeInput> Inputs = new List<NodeInput> ();
	public List<NodeOutput> Outputs = new List<NodeOutput> ();
	public bool calculated = true;
	// Abstract member to get the ID of the node
	public abstract string GetID { get; }

	/// <summary>
	/// Gets the zoomed rect: the rect of this node how it's actually represented on the screen.
	/// </summary>
	public Rect screenRect 
	{
		get 
		{
			Rect nodeRect = new Rect (rect);
			nodeRect.position += Node_Editor.zoomPos;
			return Node_Editor.ScaleRect (nodeRect, Node_Editor.zoomPos, new Vector2 (1/Node_Editor.nodeCanvas.zoom, 1/Node_Editor.nodeCanvas.zoom)); 
		}
	}

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

	#region Member Functions

	/// <summary>
	/// Checks if there are no unassigned and no null-value inputs.
	/// </summary>
	public bool allInputsReady () 
	{
		for (int inCnt = 0; inCnt < Inputs.Count; inCnt++) 
		{
			if (Inputs [inCnt].connection == null || Inputs [inCnt].connection.value == null)
				return false;
		}
		return true;
	}
	/// <summary>
	/// Checks if there are any unassigned inputs.
	/// </summary>
	public bool hasNullInputs () 
	{
		for (int inCnt = 0; inCnt < Inputs.Count; inCnt++) 
		{
			if (Inputs [inCnt].connection == null)
				return true;
		}
		return false;
	}
	/// <summary>
	/// Checks if there are any null-value inputs.
	/// </summary>
	public bool hasNullInputValues () 
	{
		for (int inCnt = 0; inCnt < Inputs.Count; inCnt++) 
		{
			if (Inputs [inCnt].connection != null && Inputs [inCnt].connection.value == null)
				return true;
		}
		return false;
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
	/// Init this node. Has to be called when creating a child node
	/// </summary>
	protected void InitBase () 
	{
		Calculate ();
		Node_Editor.nodeCanvas.nodes.Add (this);
		if (!String.IsNullOrEmpty (AssetDatabase.GetAssetPath (Node_Editor.nodeCanvas)))
		{
			AssetDatabase.AddObjectToAsset (this, Node_Editor.nodeCanvas);
			for (int inCnt = 0; inCnt < Inputs.Count; inCnt++) 
				AssetDatabase.AddObjectToAsset (Inputs [inCnt], this);
			for (int outCnt = 0; outCnt < Outputs.Count; outCnt++) 
				AssetDatabase.AddObjectToAsset (Outputs [outCnt], this);
			
			AssetDatabase.ImportAsset (Node_Editor.openedCanvasPath);
			AssetDatabase.Refresh ();
		}
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

	/// <summary>
	/// Draws the node knobs; splitted from curves because of the render order
	/// </summary>
	public void DrawKnobs () 
	{
		for (int outCnt = 0; outCnt < Outputs.Count; outCnt++) 
		{
			GUI.DrawTexture (Outputs [outCnt].GetGUIKnob (), Node_Editor.typeData [Outputs [outCnt].type].OutputKnob);
		}
		for (int inCnt = 0; inCnt < Inputs.Count; inCnt++) 
		{
			GUI.DrawTexture (Inputs [inCnt].GetGUIKnob (), Node_Editor.typeData [Inputs [inCnt].type].InputKnob);
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
			for (int conCnt = 0; conCnt < output.connections.Count; conCnt++) 
			{
				Node_Editor.DrawNodeCurve (output.GetGUIKnob ().center, 
				                           output.connections [conCnt].GetGUIKnob ().center,
				                           Node_Editor.typeData [output.type].col);
			}
		}
	}

	/// <summary>
	/// Deletes this Node
	/// </summary>
	public void Delete () 
	{
		Node_Editor.nodeCanvas.nodes.Remove (this);
		for (int outCnt = 0; outCnt < Outputs.Count; outCnt++) 
		{
			NodeOutput output = Outputs [outCnt];
			for (int conCnt = 0; conCnt < output.connections.Count; conCnt++) 
				output.connections [outCnt].connection = null;
		}
		for (int inCnt = 0; inCnt < Inputs.Count; inCnt++) 
		{
			NodeInput input = Inputs [inCnt];
			if (input.connection != null)
				input.connection.connections.Remove (input);
		}
		
		DestroyImmediate (this, true);
		
		if (!String.IsNullOrEmpty (Node_Editor.openedCanvasPath)) 
		{
			AssetDatabase.ImportAsset (Node_Editor.openedCanvasPath);
			AssetDatabase.Refresh ();
		}
		OnDelete ();
	}
	
	#endregion
	
	#region Static Functions

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

		if (output.body.isChildOf (input.body)) 
		{
			Node_Editor.editor.ShowNotification (new GUIContent ("Recursion detected!"));
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

			Node_Editor.editor.RecalculateFrom (input.body);
		}
	}

	#endregion
}
