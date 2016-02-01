using UnityEngine;
using System;
using System.Collections.Generic;

namespace NodeEditorFramework 
{
	public class NodeOutput : NodeKnob
	{
		protected override NodeSide DefaultSide { get { return NodeSide.Right; } }
		private static GUIStyle defaultStyle;
		protected override GUIStyle DefaultLabelStyle { get { if (defaultStyle == null) { defaultStyle = new GUIStyle (GUI.skin.label); defaultStyle.alignment = TextAnchor.MiddleRight; } return defaultStyle; } }

		public List<NodeInput> Connections = new List<NodeInput> ();
		
		// Value
		[NonSerialized]
		private object value;
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
			output.Body = nodeBody;
			output.name = outputName;
			output.Type = outputType;
			output.Side = nodeSide;
			output.SidePosition = sidePosition;
			output.ReloadKnobTexture ();
			nodeBody.Outputs.Add (output);
			return output;
		}

		protected override void ReloadType () 
		{
			if (TypeData.Declaration == null)
				TypeData = ConnectionTypes.GetTypeData (Type);
			TexturePath = TypeData.Declaration.OutputKnobTexPath;
			KnobTexture = TypeData.OutputKnob;
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
				valueType = ConnectionTypes.GetType (Type);
			if (valueType == typeof(T)) 
			{
				if (value == null)
					value = GetDefault<T> ();
				return (T)value;
			}
			Debug.LogError ("Trying to GetValue<" + typeof(T).FullName + "> for Output Type: " + valueType.FullName);
			return GetDefault<T> ();
		}
		
		/// <summary>
		/// Sets the value.
		/// </summary>
		public void SetValue<T> (T Value)
		{
			if (valueType == null)
				valueType = ConnectionTypes.GetType(Type);
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
		public static T GetDefault<T> ()
		{
			if (typeof(T).GetConstructor (System.Type.EmptyTypes) != null) // Try to create using an empty constructor if existant
				return Activator.CreateInstance<T> ();
			else // Else try to get default. Returns null only on reference types
				return default(T);
		}

		#endregion
	}
}