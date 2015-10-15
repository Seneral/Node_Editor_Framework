using UnityEngine;
using System;
using System.Collections.Generic;
using NodeEditorFramework;

namespace NodeEditorFramework 
{
	public class NodeOutput : ScriptableObject
	{
		// Main
		public Node body;
		public List<NodeInput> connections = new List<NodeInput> ();
		public string type;
		[NonSerialized]
		public Texture2D knobTexture;

		// Position
		[NonSerialized]
		public NodeSide side = NodeSide.Right;
		[NonSerialized]
		public float sidePosition = 0; // Position on the side, top->bottom, left->right
		[NonSerialized]
		public float sideOffset = 0; // Offset from the side

		// Value
		[System.NonSerialized]
		private object value = null;
		private System.Type valueType;

		/// <summary>
		/// Creates a new NodeOutput in NodeBody of specified type
		/// </summary>
		public static NodeOutput Create (Node NodeBody, string OutputName, string OutputType) 
		{
			NodeOutput output = CreateInstance <NodeOutput> ();
			output.body = NodeBody;
			output.type = OutputType;
			output.knobTexture = ConnectionTypes.GetTypeData (OutputType).OutputKnob;
			output.name = OutputName;
			NodeBody.Outputs.Add (output);
			return output;
		}

		#region Value

		public bool IsValueNull { get { return value == null; } }

		/// <summary>
		/// Gets the output value.
		/// </summary>
		/// <returns>Value, if null default(T) (-> For reference types, null. For value types, default value)</returns>
		public T GetValue<T> ()
		{
			if (valueType == null)
				valueType = ConnectionTypes.GetType(type);
			if (valueType == typeof(T))
			{
				if (value == null)
					value = getDefault<T> ();
				return (T)value;
			}
			UnityEngine.Debug.LogError ("Trying to GetValue<" + typeof(T).FullName + "> for Output Type: " + valueType.FullName);
			return getDefault<T> ();
		}
		
		/// <summary>
		/// Sets the value.
		/// </summary>
		public void SetValue<T> (T Value)
		{
			if (valueType == null)
				valueType = ConnectionTypes.GetType(type);
			if (valueType == typeof(T))
				value = Value;
			else
				UnityEngine.Debug.LogError("Trying to SetValue<" + typeof(T).FullName + "> for Output Type: " + valueType.FullName);
		}
		
		/// <summary>
		/// Resets the value to null.
		/// </summary>
		public void ResetValue () 
		{
			value = null;
		}
		
		/// <summary>
		/// Returns for value types the default value; for reference types:, the default constructor if existant, else null
		/// </summary>
		public static T getDefault<T> ()
		{
			T var;
			if (typeof(T).GetConstructor (Type.EmptyTypes) != null)
			{ // Try to create using an empty constructor if existant
				var = Activator.CreateInstance<T> ();
			}
			else
			{ // Else try to get default. Returns null only on reference types
				var = default(T);
			}
			return var;
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
			style.alignment = TextAnchor.MiddleRight;
			GUILayout.Label (content, style);
			if (Event.current.type == EventType.Repaint) 
				SetPosition ();
		}

		/// <summary>
		/// Set the knob position at the specified side, from Top->Bottom and Left->Right
		/// </summary>
		public void SetPosition (float position, NodeSide nodeSide) 
		{
			if (side != nodeSide) 
			{
				int cur = (int)side, next = (int)nodeSide;
				if (next < cur)
					next += 4;
				while (cur < next) 
				{
					knobTexture = NodeEditorGUI.RotateTexture90Degrees (knobTexture);
					cur++;
				}
			}
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
			CheckTexture ();
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
			return side == NodeSide.Right? Vector2.right : (side == NodeSide.Bottom? Vector2.up : (side == NodeSide.Left? Vector2.left : Vector2.down));
		}

		public float GetRotation () 
		{
			return side == NodeSide.Right? 0 : (side == NodeSide.Bottom? 90 : (side == NodeSide.Top? -90 : 180));
		}

		public void CheckTexture () 
		{
			if (knobTexture == null) 
			{
				knobTexture = ConnectionTypes.GetTypeData (type).OutputKnob;
				Debug.Log ("Checking textures!");
				if (knobTexture == null)
					throw new UnityException ("Connection type " + type + " has no knob texture assigned!");
				if (side != NodeSide.Right) 
				{
					int cur = (int)NodeSide.Right, next = (int)side;
					if (next < cur)
						next += 4;
					while (cur < next) 
					{
						knobTexture = NodeEditorGUI.RotateTexture90Degrees (knobTexture);
						cur++;
					}
				}
			}
		}

		#endregion
	}
}