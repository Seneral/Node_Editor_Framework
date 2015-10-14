using UnityEngine;
using System;
using System.Collections.Generic;
using NodeEditorFramework;

namespace NodeEditorFramework 
{
	public class NodeOutput : ScriptableObject
	{
		public Node body;
		[HideInInspector]
		public Rect outputRect = new Rect();
		public List<NodeInput> connections = new List<NodeInput>();
		public string type;

		[System.NonSerialized]
		private object value = null;
		private System.Type valueType;

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

		/// <summary>
		/// Creates a new NodeOutput in NodeBody of specified type
		/// </summary>
		public static NodeOutput Create (Node NodeBody, string OutputName, string OutputType) 
		{
			NodeOutput output = CreateInstance <NodeOutput> ();
			output.body = NodeBody;
			output.type = OutputType;
			output.name = OutputName;
			NodeBody.Outputs.Add (output);
			return output;
		}

		/// <summary>
		/// Function to automatically draw and update the output with a label for it's name
		/// </summary>
		public void DisplayLayout () 
		{
			DisplayLayout (new GUIContent (name));
		}
		/// <summary>
		/// Function to automatically draw and update the output
		/// </summary>
		public void DisplayLayout (GUIContent content) 
		{
			GUIStyle style = new GUIStyle (GUI.skin.label);
			style.alignment = TextAnchor.MiddleRight;
			GUILayout.Label (content, style);
			if (Event.current.type == EventType.Repaint) 
				SetRect (GUILayoutUtility.GetLastRect ());
		}
		
		/// <summary>
		/// Set the output rect as labelrect in global canvas space and extend it to the right node edge
		/// </summary>
		public void SetRect (Rect labelRect) 
		{
			outputRect = new Rect (body.rect.x + labelRect.x, 
			                       body.rect.y + labelRect.y + 20, 
			                       body.rect.width - labelRect.x,
			                       labelRect.height);
		}
		
		/// <summary>
		/// Get the rect of the knob right to the output NOT ZOOMED; Used for GUI drawing in scaled areas
		/// </summary>
		public Rect GetGUIKnob () 
		{
			Rect knobRect = new Rect (outputRect);
			knobRect.position += NodeEditor.curEditorState.zoomPanAdjust;
			return TransformOutputRect (knobRect);
		}

		/// <summary>
		/// Get the rect of the knob right to the output ZOOMED; Used for input checks; Representative of the actual screen rect
		/// </summary>
		public Rect GetScreenKnob () 
		{
			return NodeEditor.GUIToScreenRect (TransformOutputRect (outputRect));
		}

		/// <summary>
		/// Transforms the output rect to the knob format
		/// </summary>
		private Rect TransformOutputRect (Rect knobRect) 
		{
			float knobSize = (float)NodeEditor.knobSize;
			return new Rect (knobRect.x + knobRect.width,
			                 knobRect.y + (knobRect.height - knobSize) / 2,
			                 knobSize, knobSize);
		}
	}
}