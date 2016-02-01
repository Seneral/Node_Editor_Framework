using UnityEngine;

namespace NodeEditorFramework
{
	public class NodeInput : NodeKnob
	{
		protected override NodeSide DefaultSide { get { return NodeSide.Left; } }
		protected override GUIStyle DefaultLabelStyle { get { return GUI.skin.label; } }

		public NodeOutput Connection;

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
			input.Body = nodeBody;
			input.name = inputName;
			input.Type = inputType;
			input.Side = nodeSide;
			input.SidePosition = sidePosition;
			input.ReloadKnobTexture ();
			nodeBody.Inputs.Add (input);
			return input;
		}

		protected override void ReloadType () 
		{
			if (TypeData.Declaration == null)
				TypeData = ConnectionTypes.GetTypeData (Type);
			TexturePath = TypeData.Declaration.InputKnobTexPath;
			KnobTexture = TypeData.InputKnob;
		}

		public T GetValue<T> ()
		{
			return Connection != null? Connection.GetValue<T> () : NodeOutput.GetDefault<T> ();
		}
		
		public void SetValue<T> (T value)
		{
			if (Connection != null)
				Connection.SetValue<T> (value);
		}
	}
}