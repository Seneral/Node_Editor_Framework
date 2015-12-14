using System;
using UnityEngine;
using NodeEditorFramework;

namespace NodeEditorFramework
{
	public class NodeInput : NodeKnob
	{
		public override NodeSide defaultSide { get { return NodeSide.Left; } }
		public override GUIStyle defaultStyle { get { return new GUIStyle (GUI.skin.label); } }

		public NodeOutput connection;

		/// <summary>
		/// Creates a new NodeInput in NodeBody of specified type
		/// </summary>
		public static NodeInput Create (Node NodeBody, string InputName, string InputType)
		{
			NodeInput input = CreateInstance <NodeInput> ();
			input.body = NodeBody;
			input.name = InputName;
			input.SetType (InputType);
			NodeBody.Inputs.Add (input);
			return input;
		}

		public override void SetType (string Type) 
		{
			type = Type;
			TypeData typeData = ConnectionTypes.GetTypeData (type);
			texturePath = typeData.declaration.InputKnob_TexPath;
			knobTexture = typeData.InputKnob;
		}

		public T GetValue<T> ()
		{
			return connection != null? connection.GetValue<T> () : NodeOutput.getDefault<T> ();
		}
		
		public void SetValue<T> (T value)
		{
			connection.SetValue<T>(value);
		}
	}
}