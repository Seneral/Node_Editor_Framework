using UnityEngine;
using System.Collections.Generic;

namespace NodeEditorFramework 
{
	/// <summary>
	/// Node output accepts multiple connections to NodeInputs by default
	/// </summary>
	public class NodeOutput : NodeKnob
	{
		// NodeKnob Members
		protected override NodeSide defaultSide { get { return NodeSide.Right; } }
		private static GUIStyle _defaultStyle;
		protected override GUIStyle defaultLabelStyle { get { if (_defaultStyle == null) { _defaultStyle = new GUIStyle (GUI.skin.label); _defaultStyle.alignment = TextAnchor.MiddleRight; } return _defaultStyle; } }

		// NodeInput Members
		public List<NodeInput> connections = new List<NodeInput> ();
		public string type;
		[System.NonSerialized]
		internal TypeData typeData;
		[System.NonSerialized]
		private object value = null;

		#region Contructors

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
			output.type = outputType;
			output.InitBase (nodeBody, nodeSide, sidePosition, outputName);
			nodeBody.Outputs.Add (output);
			return output;
		}

		#endregion

		#region Additional Serialization

		protected internal override void CopyScriptableObjects (System.Func<ScriptableObject, ScriptableObject> replaceSerializableObject) 
		{
			for (int conCnt = 0; conCnt < connections.Count; conCnt++) 
				connections[conCnt] = replaceSerializableObject.Invoke (connections[conCnt]) as NodeInput;
		}

		#endregion

		#region KnobType

		protected override void ReloadTexture () 
		{
			CheckType ();
			knobTexture = typeData.OutKnobTex;
		}

		private void CheckType () 
		{
			if (typeData == null || !typeData.isValid ()) 
				typeData = ConnectionTypes.GetTypeData (type, true);
		}

		#endregion

		#region Value
		
		public bool IsValueNull { get { return value == null; } }
		
		/// <summary>
		/// Gets the output value if the type matches
		/// </summary>
		/// <returns>Value, if null default(T) (-> For reference types, null. For value types, default value)</returns>
		public T GetValue<T> ()
		{
			CheckType ();
			if (typeData.Type == typeof(T) || typeData.Type.IsSubclassOf(typeof(T)))
				return (T)(value?? (value = GetDefault<T> ()));
			Debug.LogError ("Trying to GetValue<" + typeof(T).FullName + "> for Output Type: " + typeData.Type.FullName);
			return GetDefault<T> ();
		}
		
		/// <summary>
		/// Sets the output value if the type matches
		/// </summary>
		public void SetValue<T> (T Value)
		{
			CheckType ();
			if (typeData.Type == typeof(T))
				value = Value;
			else
				Debug.LogError ("Trying to SetValue<" + typeof(T).FullName + "> for Output Type: " + typeData.Type.FullName);
		}
		
		/// <summary>
		/// Resets the output value to null.
		/// </summary>
		public void ResetValue () 
		{
			value = null;
		}
		
		/// <summary>
		/// For value types, the default value; for reference types, the default constructor if existant, else null
		/// </summary>
		public static T GetDefault<T> ()
		{
			if (typeof(T).GetConstructor (System.Type.EmptyTypes) != null) // Try to create using an empty constructor if existant
				return System.Activator.CreateInstance<T> ();
			else // Else try to get default. Returns null only on reference types
				return default(T);
		}

		#endregion
	}
}