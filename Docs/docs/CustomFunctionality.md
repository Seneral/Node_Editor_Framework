
# Custom Content and Functionality

This page helps you get started creating custom Nodes, ConnectionTypes and even controls in the simplest form, without touching the Framework code.
This is possible by dynamically fetching extending content from all assemblies and even enables user extensions.
Using that knowledge you can already create some extensions requiring the Node Editor Framework to be installed separately.

<br> <br>

### Custom Nodes

The implementation of additional, custom nodes is fairly easy. You have to create a script anywhere in the project extending the `NodeEditorFramework.Node` class.
It will provide the Framework all needed information about the node itself, the optional `Node` attribute contains information about the presentation in the editor.
The Framework will search all script assemblies for additional nodes, so extra setup is not required.

In the following are the necessary Node members outlined. You can take reference from the ExampleNode found in '*Plugins/Node_Editor/Nodes*'.
First to mention is that even though the Framework is programmed in C#, you can add nodes in UnityScript with the limitation that they have to be compiled in phase 2,3 or 4, 
as described [here](http://docs.unity3d.com/Manual/ScriptCompileOrderFolders.html). Therefore the following members are described language independently.

- import/use `NodeEditorFramework`
- Class extending `Node`
- _Optional_: Attribute `Node` *[params: [Bool] hide; [String] contextPath]*
- Unique Node ID; declare: `ID` *[constant string]*; expose: property `GetID`*[Override]*
- _Optional_: Behaviour Options
	- `AllowRecursion` *[override, default: false]*
	- `ContinueCalculation` *[override, default: true]*
	- `AcceptsTransitions` *[override, default: false]*
- Method `Create` *[override; Params: [Vector2] position; Returns : [Node] created node]*
	- Create a new Node of your type using `CreateInstance` and assign it's property `rect` using the position parameter
	- Add connections using `CreateInput`/`CreateOutput` or `NodeInput.Create`/`NodeOuput.Create` *[Params: name; type ID; side; position]*
	- Perform any other additional setup steps and return your created node
- Method `NodeGUI` *[override]*
	- Draw your Node's GUI; you may use GUILayout functions
	- Access the Inputs/Outputs using the respective arrays in the order of creation. 
	  Use their methods `DisplayLayout` or `SetPosition` to position (and draw) them.
- Method `NodeGUI` *[override]*
	- The methods `allInputsReady`, `hasUnassignedInputs` and `descendantsCalculated` 
	  may help to check if the node is ready, based on the needs and purposes of it.
	- Get the input values by calling `GetValue` on the NodeInputs and set the output values with `SetValue` the same way.
	- Return _true_ when you're done calculating and _false_ when you are not ready yet and need another attempt. 
	  But be aware, you cannot yield calculation that way, after a maximum of a _thousand_ repeated tries the calculation will be aborted!

<br> <br> <br>

### Custom ConnectionTypes

Implementing custom ConnectionTypes is similar to Node implementation, as it uses the same fetching system: 
Declare a class inheriting from the `ITypeDeclaration` interface and specify it's properties.

<center>
	![ConnectionType with ITypeDeclaration] (/img/ConnectionTypes.png "ConnectionTypes.cs: Top: ITypeDeclaration; Bottom: Built-in Float type")
	<br>
	ConnectionTypes.cs: Top: ITypeDeclaration; Bottom: Built-in Float type
	<br>
	Do not that the names may differ from previous versions!
</center>

- The `string Identifier` is used to address the type later on
- The `Type Type` is the type this declaration representates (e.g. 'typeof(float)')
- The `Color Color` is the color associated with the type, in which the knob textures as well as the connections are tinted with
- The strings `InKnobTex` and `OutKnobTex` are the paths to the knob textures relative to '_Node\_Editor/Resources_'. Defaults are '_Textures/In\_Knob.png_' and '_Textures/Out\_Knob.png_'

<br> <br> <br>

### Custom Input Controls

In the latest developer branch, the Input system has been completely revamped. The following is not valid for older versions!
For your Editor Extension you might want to add custom controls, as well as adding functions to the context clicks of both the canvas and the editor.
Using the new dynamic Input system it is very easy to do just that using four provided attributes which can be stacked as you wish.
Before explaining these in detail, it might be worth checking the default controls out in `NodeEditorInputControls`!
<br>

#### <u>NodeEditorInputInfo</u>

Before diving into the attributes and their usage, the primary information container will be explained. 
`NodeEditorInputInfo` contains all informations about an event including the editorState, the mouse position or the invoking event.
It is used to provide all necessary information to the dynamic input handlers.
<br>

#### <u>EventHandler Attribute</u>

The EventHandlerAttribute is used to handle arbitrary events for the Node Editor and is the most flexible attribute.
Some default controls like Node dragging, panning, zooming and Node connecting could only be implemented using this attribute.
Tagging a static function with this attribute makes it get called when the specified EventType occurs (or always when no event specified).
The optional variable 'priority', next to the constructor variations, primarily defines the order of executrion, but also a way to execute the input after the GUI (priority >= 100).
The method signature **must** be as follows:[ Return: Void; Params: NodeEditorInputInfo ]
<br>

#### <u>Hotkey Attribute</u>

The HotkeyAttribute is used to provide a simple interface for hotkeys for the Node Editor.
Some default controls like Navigating ('N') and Snapping ('Control') are implemented using this attribute
It allows you to specify a KeyCode / Modifier combination with a limiting EventType to specify when the tagged static function gets called.
Again, the optional variable `priority` can be specified. Refer to the EventHandler Attribute for it's effect.
The method signature **must** be as follows:[ Return: Void; Params: NodeEditorInputInfo ]
<br>

#### <u>ContextEntry Attribute</u>

The ContextAttribute is used to register context entries for the NodeEditor.
The tagged function is called when the context element at the specified path is selected.
In which contect menu to add this element is specified by the type, like the Node context click or the Canvas context click.
The method signature **must** be as follows:[ Return: Void; Params: NodeEditorInputInfo ]
<br>

#### <u>ContextFiller Attribute</u>

The ContextFillerAttribute is used to register context entries in the NodeEditor in a dynamic, conditional or procedural way.
This function will be called to fill the passed context GenericMenu in any way it likes to.
Again the type specifies the context menu to fill.
The method signature **must** be as follows:[ Return: Void; Params: NodeEditorInputInfo, GenericMenu ]


