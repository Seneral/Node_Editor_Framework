
# Custom Nodes

The implementation of additional, custom nodes is fairly easy. You have to create a script anywhere in the project extending the `NodeEditorFramework.Node` class.
It will provide the Framework all needed information about the node itself, the optional `Node` attribute contains information about the presentation in the editor.
The Framework will search all script assemblies for additional nodes, so extra setup is not required. If you do need a custom assembly to be included, you can add it manually in `NodeTypes.cs`.

The following outlines the necessary Node members. You can take reference from the ExampleNode found in '*Plugins/Node_Editor/Nodes/Example*'.
First to mention is that even though the Framework is programmed in C#, you can add nodes in UnityScript with the limitation that they have to be compiled in phase 2,3 or 4, 
as described [here](http://docs.unity3d.com/Manual/ScriptCompileOrderFolders.html). Therefore the following members are described language independently.

- import/use `NodeEditorFramework`
- Class extending `Node`
- _Optional_: Attribute `Node` *[params: [Bool] hide; [String] contextPath; optional [Type] canvasType]*
- Unique Node ID; declare: `ID` *[constant string]*; expose: property `GetID`*[Override]*
- _Optional_: Behaviour Options
	- `AllowRecursion` *[override, default: false]*
	- `ContinueCalculation` *[override, default: true]*
	- `AcceptsTransitions` *[override, default: false]*
- Method `Create` *[override; Params: [Vector2] position; Returns : [Node] created node]*
	- Create a new Node of your type using `CreateInstance` and assign it's property `rect` using the position parameter
	- Add connections using `CreateInput`/`CreateOutput` or `NodeInput.Create`/`NodeOuput.Create` *[Params: name; type ID; side; position]*
	- Perform any other additional setup steps and return your created node
- Method `NodeGUI` *[protected (internal) override]*
	- Draw your Node's GUI; you may use GUILayout functions
	- Access the Inputs/Outputs using the respective arrays in the order of creation. 
	  Use their methods `DisplayLayout` or `SetPosition` to position (and draw) them.
- Method `Calculate` *[override]*
	- The methods `allInputsReady`, `hasUnassignedInputs` and `descendantsCalculated` 
	  may help to check if the node is ready, based on the needs and purposes of it.
	- Get the input values by calling `GetValue` on the NodeInputs and set the output values with `SetValue` the same way.
	- Return _true_ when you're done calculating and _false_ when you are not ready yet and need another attempt. 
	  But be aware, you cannot yield calculation that way, after a maximum of a _thousand_ repeated tries the calculation will be aborted!
