using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using NodeEditorFramework;
using NodeEditorFramework.Utilities;

[Node (false, "Utility/Expression Node")]
public class ExpressionNode : Node 
{
	public const string ID = "expressionNode";
	public override string GetID { get { return ID; } }

	public string expression = "";
	public SerializedType expressionType;
	private bool canEvaluateExpression;

	[NonSerialized]
	private UnityEditor.GenericMenu typeSelectionMenu;

	public override Node Create (Vector2 pos) 
	{
		ExpressionNode node = CreateInstance <ExpressionNode> ();
		
		node.name = "Expression Node";
		node.rect = new Rect (pos.x, pos.y, 250, 80);
		
		NodeOutput.Create (node, "Expression", typeof(UnityEngine.Object).AssemblyQualifiedName);

		return node;
	}

	protected override void NodeGUI () 
	{
		if (expressionType == null || !expressionType.Validate () || Outputs.Count == 0 || Outputs[0] == null)
			SetExpressionType (typeof(UnityEngine.Object));

		// Expression Input
		expression = GUILayout.TextField (expression);
		OutputKnob (0);

		// Expression type selection
		GUILayout.BeginHorizontal ();
		if (GUILayout.Button ("Type"))
		{
			if (typeSelectionMenu == null) 
			{ // Create type selection menu if not existant
				Type convertible = typeof(IConvertible);
				typeSelectionMenu = TypeSelector.BuildTypeSelection (SetExpressionType, null, (Type type) => type.GetInterfaces ().Contains (convertible));
			}
			typeSelectionMenu.ShowAsContext ();
		}
		GUILayout.Label (expressionType.GetRuntimeType ().FullName);
		GUILayout.EndHorizontal ();

		// Validation label
		if (!canEvaluateExpression)
			GUILayout.Label ("-Cannot convert expression-");

		if (GUI.changed)
			NodeEditor.curNodeCanvas.OnNodeChange (this);
	}
	
	public override bool Calculate () 
	{
		if (!allInputsReady ())
			return false;
		if (expressionType == null || !expressionType.Validate ())
			SetExpressionType (typeof(UnityEngine.Object));
		
		object evaluatedExpression;
		try 
		{
			evaluatedExpression = Convert.ChangeType (expression, expressionType.GetRuntimeType ());
			canEvaluateExpression = true;
		}
		catch
		{
			evaluatedExpression = TypeSelector.GetDefault (expressionType.GetRuntimeType ());
			canEvaluateExpression = false;
		}
		Outputs[0].SetValue (evaluatedExpression);
		return true;
	}

	private void SetExpressionType (object selectorData) 
	{
		if (selectorData == null)
			throw new UnityException ("Type selection is null!");
		Type selectedType = selectorData as Type;
		if (selectedType == null)
			throw new UnityException ("Invalid type selection " + selectorData.ToString () + ", it is of type " + selectorData.GetType ().FullName + "!");
		if (expressionType == null || !expressionType.Validate ())
			expressionType = new SerializedType (typeof(UnityEngine.Object));
		
		if (expressionType.GetRuntimeType () == selectedType)
			return;

		NodeOutput output = Outputs[0];
		ReassignOutputType (ref output, selectedType);
		// Assign new output type
		expressionType = new SerializedType (selectedType);
		NodeEditor.curNodeCanvas.OnNodeChange (this);
	}
}