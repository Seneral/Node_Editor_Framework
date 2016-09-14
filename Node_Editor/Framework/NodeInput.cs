using UnityEngine;
using UnityEngine.Serialization;
using System;
using System.Collections.Generic;

namespace NodeEditorFramework
{
	/// <summary>
	/// NodeInput accepts one connection to a NodeOutput by default
	/// </summary>
	public partial class NodeInput : NodeKnob
	{
		// NodeKnob Members
		protected override NodeSide defaultSide { get { return NodeSide.Left; } }

		// NodeInput Members
		public NodeOutput connection;
		[FormerlySerializedAs("type")]
		public string typeID;
		private TypeData _typeData;
		internal TypeData typeData { get { CheckType (); return _typeData; } }
		// Multiple connections
//		public List<NodeOutput> connections;

		#region General

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
			input.typeID = inputType;
			input.InitBase (nodeBody, nodeSide, sidePosition, inputName);
			nodeBody.Inputs.Add (input);
			return input;
		}

		public override void Delete () 
		{
			RemoveConnection ();
			body.Inputs.Remove (this);
			base.Delete ();
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
			knobTexture = typeData.InKnobTex;
		}

		private void CheckType () 
		{
			if (_typeData == null || !_typeData.isValid ()) 
				_typeData = ConnectionTypes.GetTypeData (typeID);
			if (_typeData == null || !_typeData.isValid ()) 
			{
				ConnectionTypes.FetchTypes ();
				_typeData = ConnectionTypes.GetTypeData (typeID);
				if (_typeData == null || !_typeData.isValid ())
					throw new UnityException ("Could not find type " + typeID + "!");
			}
		}

		#endregion

		#region Value

		public bool IsValueNull { get { return connection != null? connection.IsValueNull : true; } }

		/// <summary>
		/// Gets the value of the connection anonymously. Not advised as it may lead to unwanted behaviour!
		/// </summary>
		public object GetValue ()
		{
			return connection != null? connection.GetValue () : null;
		}

		/// <summary>
		/// Gets the value of the connection or null. If possible, use strongly typed version instead.
		/// </summary>
		public object GetValue (Type type)
		{
			return connection != null? connection.GetValue (type) : null;
		}

		/// <summary>
		/// Sets the value of the connection if the type matches. If possible, use strongly typed version instead.
		/// </summary>
		public void SetValue (object value)
		{
			if (connection != null)
				connection.SetValue (value);
		}

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
		/// Try to connect the passed NodeOutput to this NodeInput. Returns success / failure
		/// </summary>
		public bool TryApplyConnection (NodeOutput output)
		{
			if (CanApplyConnection (output)) 
			{ // It can connect (type is equals, it does not cause recursion, ...)
				ApplyConnection (output);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Check if the passed NodeOutput can be connected to this NodeInput
		/// </summary>
		public bool CanApplyConnection (NodeOutput output)
		{
			if (output == null || body == output.body || connection == output || !typeData.Type.IsAssignableFrom (output.typeData.Type)) 
			{
//				Debug.LogError ("Cannot assign " + typeData.Type.ToString () + " to " + output.typeData.Type.ToString ());
				return false;
			}

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
			NodeEditor.Calculator.RecalculateFrom (body);
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

			NodeEditor.Calculator.RecalculateFrom (body);
		}


		#endregion

		#region Utility

		public override Node GetNodeAcrossConnection()
		{
			return connection != null ? connection.body : null;
		}

		#endregion
	}
}