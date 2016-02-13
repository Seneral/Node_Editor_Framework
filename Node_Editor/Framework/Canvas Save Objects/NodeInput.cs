using UnityEngine;
using System.Collections.Generic;

namespace NodeEditorFramework
{
	/// <summary>
	/// NodeInput accepts one connection to a NodeOutput by default
	/// </summary>
	public class NodeInput : NodeKnob
	{
		// NodeKnob Members
		protected override NodeSide defaultSide { get { return NodeSide.Left; } }

		// NodeInput Members
		public NodeOutput connection;
		public string type;
		[System.NonSerialized]
		internal TypeData typeData;
		// Multiple connections
//		public List<NodeOutput> connections;

		#region Contructors

		/// <summary>
		/// Creates a new NodeInput in NodeBody of specified type
		/// </summary>
		public static NodeInput Create (Node nodeBody, string inputName, string inputType)
		{
			return Create (nodeBody, inputName, inputType, NodeSide.Left, 20);
		}

		/// <summary>
		/// Creates a new NodeInput in NodeBody of specified type at the specified NodeSide
		/// </summary>
		public static NodeInput Create (Node nodeBody, string inputName, string inputType, NodeSide nodeSide)
		{
			return Create (nodeBody, inputName, inputType, nodeSide, 20);
		}

		/// <summary>
		/// Creates a new NodeInput in NodeBody of specified type at the specified NodeSide and position
		/// </summary>
		public static NodeInput Create (Node nodeBody, string inputName, string inputType, NodeSide nodeSide, float sidePosition)
		{
			NodeInput input = CreateInstance <NodeInput> ();
			input.type = inputType;
			input.InitBase (nodeBody, nodeSide, sidePosition, inputName);
			nodeBody.Inputs.Add (input);
			return input;
		}

		#endregion

		#region Additional Serialization

		protected internal override void CopyScriptableObjects (System.Func<ScriptableObject, ScriptableObject> replaceSerializableObject) 
		{
			connection = replaceSerializableObject.Invoke (connection) as NodeOutput;
			// Multiple connections
//			for (int conCnt = 0; conCnt < connections.Count; conCnt++) 
//				connections[conCnt] = replaceSerializableObject.Invoke (connections[conCnt]) as NodeOutput;
		}

		#endregion

		#region KnobType

		protected override void ReloadTexture () 
		{
			CheckType ();
			knobTexture = typeData.InputKnob;
		}

		private void CheckType () 
		{
			if (typeData.declaration == null || typeData.Type == null) 
				typeData = ConnectionTypes.GetTypeData (type);
		}

		#endregion

		#region Value

		/// <summary>
		/// Gets the value of the connection or the default value
		/// </summary>
		public T GetValue<T> ()
		{
			return connection != null? connection.GetValue<T> () : NodeOutput.GetDefault<T> ();
		}

		/// <summary>
		/// Sets the value of the connection if the type matches
		/// </summary>
		public void SetValue<T> (T value)
		{
			if (connection != null)
				connection.SetValue<T> (value);
		}

		#endregion

		#region Connecting Utility

		/// <summary>
		/// Check if the passed NodeOutput can be connected to this NodeInput
		/// </summary>
		public bool CanApplyConnection (NodeOutput output)
		{
			if (output == null || body == output.body || connection == output || typeData.Type != output.typeData.Type)
				return false;

			if (output.body.isChildOf (body)) 
			{ // Recursive
				if (!output.body.allowsLoopRecursion (body))
				{
					// TODO: Generic Notification
					Debug.LogWarning ("Cannot apply connection: Recursion detected!");
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Applies a connection between the passed NodeOutput and this NodeInput. 'CanApplyConnection' has to be checked before to avoid interferences!
		/// </summary>
		public void ApplyConnection (NodeOutput output)
		{
			if (output == null) 
				return;
			
			if (connection != null) 
			{
				NodeEditorCallbacks.IssueOnRemoveConnection (this);
				connection.connections.Remove (this);
			}
			connection = output;
			output.connections.Add (this);

			NodeEditor.RecalculateFrom (body);
			output.body.OnAddOutputConnection (output);
			body.OnAddInputConnection (this);
			NodeEditorCallbacks.IssueOnAddConnection (this);
		}

		/// <summary>
		/// Removes the connection from this NodeInput
		/// </summary>
		public void RemoveConnection ()
		{
			if (connection == null)
				return;
			
			NodeEditorCallbacks.IssueOnRemoveConnection (this);
			connection.connections.Remove (this);
			connection = null;

			NodeEditor.RecalculateFrom (body);
		}


		#endregion
	}
}