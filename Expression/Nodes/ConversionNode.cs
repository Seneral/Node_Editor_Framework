using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

using NodeEditorFramework;
using NodeEditorFramework.Utilities;

[Node (false, "Utility/Conversion Node")]
public class ConversionNode : Node 
{
	public const string ID = "conversionNode";
	public override string GetID { get { return ID; } }

	public SerializedType inputType;
	public SerializedType outputType;

	[NonSerialized]
	private UnityEditor.GenericMenu inputTypeSelectionMenu;
	[NonSerialized]
	private UnityEditor.GenericMenu outputTypeSelectionMenu;

	private Type defaultType = typeof(object);

	public override Node Create (Vector2 pos) 
	{
		ConversionNode node = CreateInstance<ConversionNode> ();

		node.rect = new Rect (pos.x, pos.y, 250, 100);
		node.name = "Conversion Node";

		node.inputType = new SerializedType (defaultType);
		node.CreateInput ("Input", defaultType.AssemblyQualifiedName);
		node.outputType = new SerializedType (defaultType);
		node.CreateOutput ("Output", defaultType.AssemblyQualifiedName);

		return node;
	}

	protected override void NodeGUI () 
	{
		// Input type selection
		GUILayout.BeginHorizontal ();
		if (GUILayout.Button ("Input Type"))
		{
			if (inputTypeSelectionMenu == null)
			{ // Create type selection menu if not existant
				Type convertible = typeof(IConvertible);
				inputTypeSelectionMenu = TypeSelector.BuildTypeSelection (SetInputType, null, (Type type) => type.GetInterfaces ().Contains (convertible));
			}
			inputTypeSelectionMenu.ShowAsContext ();
		}
		if (inputType == null || !inputType.Validate ())
			inputType = new SerializedType (defaultType);
		GUILayout.Label (inputType.GetRuntimeType ().FullName);
		GUILayout.EndHorizontal ();

		// Output type selection
		GUILayout.BeginHorizontal ();
		if (GUILayout.Button ("Output Type"))
		{
			if (outputTypeSelectionMenu == null)
			{ // Create type selection menu if not existant 
				Func<Type, bool> typeSelector = (Type type) => type.GetInterfaces ().Contains (typeof(IConvertible));
				outputTypeSelectionMenu = TypeSelector.BuildTypeSelection (SetOutputType, null, typeSelector);
			}
			outputTypeSelectionMenu.ShowAsContext ();
		}
		if (outputType == null || !outputType.Validate ())
			outputType = new SerializedType (defaultType);
		GUILayout.Label (outputType.GetRuntimeType ().FullName);
		GUILayout.EndHorizontal ();

		// Input and Output knobs
		GUILayout.BeginHorizontal ();
		Inputs [0].DisplayLayout ();
		Outputs [0].DisplayLayout ();
		GUILayout.EndHorizontal ();

		if (GUI.changed)
			NodeEditor.curNodeCanvas.OnNodeChange (this);
	}

	private void SetInputType (object selectorData) 
	{
		if (selectorData == null)
			throw new UnityException ("Type selection is null!");
		Type selectedType = selectorData as Type;
		if (selectedType == null)
			throw new UnityException ("Invalid type selection " + selectorData.ToString () + ", it is of type " + selectorData.GetType ().FullName + "!");

//		// Delete the output but first check if the connection is still valid with the new type
//		NodeOutput validConnection = null;
//		if (inputType.GetRuntimeType ().IsAssignableFrom (selectedType))
//			validConnection = Inputs[0].connection;
//		Inputs[0].Delete ();
//
//		// Create input with new type
//		NodeEditorCallbacks.IssueOnAddNodeKnob (CreateInput ("Input", selectedType.AssemblyQualifiedName));
//		if (validConnection != null) 
//		{ // Restore the valid connection
//			Inputs[0].ApplyConnection (validConnection);
//		}

		NodeInput input = Inputs[0];
		ReassignInputType (ref input, selectedType);
		// Assign new input type
		inputType = new SerializedType (selectedType);
		NodeEditor.curNodeCanvas.OnNodeChange (this);
	}

	private void SetOutputType (object selectorData) 
	{
		if (selectorData == null)
			throw new UnityException ("Type selection is null!");
		Type selectedType = selectorData as Type;
		if (selectedType == null)
			throw new UnityException ("Invalid type selection " + selectorData.ToString () + ", it is of type " + selectorData.GetType ().FullName + "!");

//		// Delete the output but first check if the connections are still valid with the new type
//		List<NodeInput> validConnections = null;
//		if (outputType.GetRuntimeType ().IsAssignableFrom (selectedType))
//			validConnections = new List<NodeInput> (Outputs[0].connections);
//		Outputs[0].Delete ();
//
//		// Create Output with new type
//		NodeEditorCallbacks.IssueOnAddNodeKnob (CreateOutput ("Output", selectedType.AssemblyQualifiedName));
//		if (validConnections != null)
//		{ // Restore the valid connections
//			NodeOutput output = Outputs[0];
//			foreach (NodeInput input in validConnections)
//				input.ApplyConnection (output);
//		}

		NodeOutput output = Outputs[0];
		ReassignOutputType (ref output, selectedType);
		// Assign new output type
		outputType = new SerializedType (selectedType);
		NodeEditor.curNodeCanvas.OnNodeChange (this);
	}

	public override bool Calculate () 
	{
		if (!allInputsReady ())
			return false;
		object inputValue = Inputs[0].GetValue (inputType.GetRuntimeType ());
		object convertedObject = null;
		if (inputValue != null) 
		{
			try 
			{
				if (outputType.GetRuntimeType () == typeof(string))
					convertedObject = inputValue.ToString ();
				else
					convertedObject = Convert.ChangeType (inputValue, outputType.GetRuntimeType ());
			}
			catch (Exception e)
			{
				Debug.LogWarning ("Could not convert " + inputValue.ToString () + " from type " + inputType.GetRuntimeType ().FullName + " to " + outputType.GetRuntimeType ().FullName + "!");
				Debug.LogWarning (e);
				convertedObject = null;
			}
		}
		Outputs[0].SetValue (convertedObject);
		return true;
	}
}
