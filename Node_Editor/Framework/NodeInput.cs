using System;
using UnityEngine;
using NodeEditorFramework;

namespace NodeEditorFramework
{
	public class NodeInput : ScriptableObject
	{
		public Node body;
		[HideInInspector]
		public Rect inputRect = new Rect();
		public NodeOutput connection;
		public string type;
		public Texture2D knobTexture;

		// Position
		[HideInInspector]
		public NodeSide side = NodeSide.Left;
		public float sidePosition = 0; // Position on the side, top->bottom, left->right
		public float sideOffset = 0; // Offset from the side

		/// <summary>
		/// Creates a new NodeInput in NodeBody of specified type
		/// </summary>
		public static NodeInput Create (Node NodeBody, string InputName, string InputType)
		{
			NodeInput input = CreateInstance <NodeInput> ();
			input.body = NodeBody;
			input.type = InputType;
			input.knobTexture = ConnectionTypes.GetTypeData (InputType).InputKnob;
			input.name = InputName;
			NodeBody.Inputs.Add (input);
			return input;
		}

		#region Value

		public T GetValue<T> ()
		{
			return connection.GetValue<T> ();
		}
		
		public void SetValue<T> (T value)
		{
			connection.SetValue<T>(value);
		}

		#endregion

		#region Position

		/// <summary>
		/// Automatically draw the output with it's name and set the knob next to it at the current side.
		/// </summary>
		public void DisplayLayout () 
		{
			DisplayLayout (new GUIContent (name));
		}
		
		/// <summary>
		/// Automatically draw the output with the specified label and set the knob next to it at the current side.
		/// </summary>
		public void DisplayLayout (GUIContent content) 
		{
			GUIStyle style = new GUIStyle (GUI.skin.label);
			style.alignment = TextAnchor.MiddleLeft;
			GUILayout.Label (content, style);
			if (Event.current.type == EventType.Repaint) 
				SetPosition ();
		}
		
		/// <summary>
		/// Set the knob position at the specified side, from Top->Bottom and Left->Right
		/// </summary>
		public void SetPosition (float position, NodeSide nodeSide) 
		{
			side = nodeSide;
			SetPosition (position);
		}
		
		/// <summary>
		/// Set the knob position at the current side, from Top->Bottom and Left->Right
		/// </summary>
		public void SetPosition (float position) 
		{
			sidePosition = position;
		}
		
		/// <summary>
		/// Set the knob position at the current side next to the last Layout control
		/// </summary>
		public void SetPosition () 
		{
			Vector2 pos = GUILayoutUtility.GetLastRect ().center;
			sidePosition = side == NodeSide.Bottom || side == NodeSide.Top? pos.x : pos.y+20;
		}
		
		/// <summary>
		/// Get the Knob rect in GUI space, NOT ZOOMED
		/// </summary>
		public Rect GetGUIKnob () 
		{
			if (knobTexture == null) 
			{
				knobTexture = ConnectionTypes.GetTypeData (type).OutputKnob;
				if (knobTexture == null)
					throw new UnityException ("Connection type " + type + " has no knob texture assigned!");
			}
			
			Vector2 center = new Vector2 (body.rect.x + (side == NodeSide.Bottom || side == NodeSide.Top? sidePosition : (side == NodeSide.Left? -sideOffset : body.rect.width+sideOffset)), 
			                              body.rect.y + (side == NodeSide.Left || side == NodeSide.Right? sidePosition : (side == NodeSide.Top? -sideOffset : body.rect.height+sideOffset)));
			Vector2 size = new Vector2 ((knobTexture.width/knobTexture.height) * NodeEditor.knobSize,
			                            (knobTexture.height/knobTexture.width) * NodeEditor.knobSize);
			Rect knobRect = new Rect (center.x + (side == NodeSide.Left? -size.x : 0), center.y + (side == NodeSide.Top? -size.y : (side == NodeSide.Bottom? 0 : -size.y/2)), size.x, size.y);
			knobRect.position += NodeEditor.curEditorState.zoomPanAdjust;
			return knobRect;
		}
		
		/// <summary>
		/// Get the Knob rect in screen space, ZOOMED
		/// </summary>
		public Rect GetScreenKnob () 
		{
			Rect rect = GetGUIKnob ();
			rect.position -= NodeEditor.curEditorState.zoomPanAdjust;
			return NodeEditor.GUIToScreenRect (rect);
		}

		public Vector2 GetDirection () 
		{
			return side == NodeSide.Left? Vector2.right : (side == NodeSide.Top? Vector2.up : (side == NodeSide.Right? Vector2.left : Vector2.down));
		}
		
		public float GetRotation () 
		{
			return side == NodeSide.Left? 0 : (side == NodeSide.Top? 90 : (side == NodeSide.Bottom? -90 : 180));
		}

		#endregion
	}
}