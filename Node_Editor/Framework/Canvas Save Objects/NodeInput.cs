using System;
using UnityEngine;
using NodeEditorFramework;

namespace NodeEditorFramework
{
	public class NodeInput : NodeKnob
	{
		protected override NodeSide defaultSide { get { return NodeSide.Left; } }
		protected override GUIStyle defaultLabelStyle { get { return GUI.skin.label; } }

		public NodeOutput connection;

		/// <summary>
		/// Creates a new NodeInput in NodeBody of specified type
		/// </summary>
		public static NodeInput Create (Node nodeBody, string inputName, string inputType)
		{
			return Create (nodeBody, inputName, inputType, NodeSide.Left, 20);
		}

		/// <summary>
		/// Creates a new NodeInput in NodeBody of specified type
		/// </summary>
		public static NodeInput Create (Node nodeBody, string inputName, string inputType, NodeSide nodeSide)
		{
			return Create (nodeBody, inputName, inputType, nodeSide, 20);
		}

		/// <summary>
		/// Creates a new NodeInput in NodeBody of specified type at the specified Node Side
		/// </summary>
		public static NodeInput Create (Node nodeBody, string inputName, string inputType, NodeSide nodeSide, float sidePosition)
		{
			NodeInput input = CreateInstance <NodeInput> ();
			input.body = nodeBody;
			input.name = inputName;
			input.type = inputType;
			input.side = nodeSide;
			input.sidePosition = sidePosition;
			input.ReloadKnobTexture ();
			nodeBody.Inputs.Add (input);
			return input;
		}

		protected override void ReloadType () 
		{
			if (typeData.declaration == null)
				typeData = ConnectionTypes.GetTypeData (type);
			texturePath = typeData.declaration.InputKnob_TexPath;
			knobTexture = typeData.InputKnob;
		}

		public T GetValue<T> ()
		{
			return connection != null? connection.GetValue<T> () : NodeOutput.getDefault<T> ();
		}
		
		public void SetValue<T> (T value)
		{
			if (connection != null)
				connection.SetValue<T> (value);
		}
	}
}