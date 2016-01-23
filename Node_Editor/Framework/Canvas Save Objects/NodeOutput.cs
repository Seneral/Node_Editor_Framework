using UnityEngine;
using System;
using System.Collections.Generic;
using NodeEditorFramework;

namespace NodeEditorFramework 
{
	public class NodeOutput : NodeKnob
	{
		protected override NodeSide defaultSide { get { return NodeSide.Right; } }
		private static GUIStyle _defaultStyle;
		protected override GUIStyle defaultLabelStyle { get { if (_defaultStyle == null) { _defaultStyle = new GUIStyle (GUI.skin.label); _defaultStyle.alignment = TextAnchor.MiddleRight; } return _defaultStyle; } }

		public List<NodeInput> connections = new List<NodeInput> ();
		
		// Value
		[NonSerialized]
		private object value = null;
		private Type valueType;
		
		/// <summary>
		/// Creates a new NodeOutput in NodeBody of specified type
		/// </summary>
		public static NodeOutput Create (Node nodeBody, string outputName, string outputType) 
		{
			return Create (nodeBody, outputName, outputType, NodeSide.Right, 20);
		}

		/// <summary>
		/// Creates a new NodeOutput in NodeBody of specified type
		/// </summary>
		public static NodeOutput Create (Node nodeBody, string outputName, string outputType, NodeSide nodeSide) 
		{
			return Create (nodeBody, outputName, outputType, nodeSide, 20);
		}

		/// <summary>
		/// Creates a new NodeOutput in NodeBody of specified type at the specified Node Side
		/// </summary>
		public static NodeOutput Create (Node nodeBody, string outputName, string outputType, NodeSide nodeSide, float sidePosition) 
		{
			NodeOutput output = CreateInstance <NodeOutput> ();
			output.body = nodeBody;
			output.name = outputName;
			output.type = outputType;
			output.side = nodeSide;
			output.sidePosition = sidePosition;
			output.ReloadKnobTexture ();
			nodeBody.Outputs.Add (output);
			return output;
		}

		protected override void ReloadType () 
		{
			if (typeData.declaration == null)
				typeData = ConnectionTypes.GetTypeData (type);
			texturePath = typeData.declaration.OutputKnob_TexPath;
			knobTexture = typeData.OutputKnob;
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
				valueType = ConnectionTypes.GetType (type);
			if (valueType == typeof(T)) 
			{
				if (value == null)
					value = getDefault<T> ();
				return (T)value;
			}
			Debug.LogError ("Trying to GetValue<" + typeof(T).FullName + "> for Output Type: " + valueType.FullName);
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
				Debug.LogError ("Trying to SetValue<" + typeof(T).FullName + "> for Output Type: " + valueType.FullName);
		}
		
		/// <summary>
		/// Resets the value to null.
		/// </summary>
		public void ResetValue () 
		{
			value = null;
		}
		
		/// <summary>
		/// Returns for value types the default value; for reference types, the default constructor if existant, else null
		/// </summary>
		public static T getDefault<T> ()
		{
			if (typeof(T).GetConstructor (Type.EmptyTypes) != null) // Try to create using an empty constructor if existant
				return Activator.CreateInstance<T> ();
			else // Else try to get default. Returns null only on reference types
				return default(T);
		}

		#endregion
	}
}