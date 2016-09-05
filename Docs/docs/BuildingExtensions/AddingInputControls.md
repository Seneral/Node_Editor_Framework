
# Custom Input Controls

*NOTE: In the latest development branch, the Input system has been completely revamped. The following is not valid for older versions!* <br>
For your Editor Extension you might want to add custom controls or functions to the context clicks of both the canvas and the editor.
Using the new dynamic Input system it is very easy to do just that using four provided attributes which can be stacked as you wish.
Before explaining these in detail, it might be worth checking the default controls out in `NodeEditorInputControls`!
<br>

#### <u>NodeEditorInputInfo</u>

The primary information container, `NodeEditorInputInfo`, contains all informations about an event including the editorState, the mouse position or the invoking event.
It is used to provide all necessary information to the dynamic input handlers.
<br>

#### <u>EventHandler Attribute</u>

The `EventHandlerAttribute` is used to handle arbitrary events for the Node Editor and is the most flexible attribute.
Some default controls like Node dragging, panning, zooming and Node connecting could only be implemented using this attribute.
Tagging a static function with this attribute makes it get called when the specified 'EventType' occurs (or always when no event specified).
The optional variable `priority`, next to the constructor variations, primarily defines the order of execution, but also a way to execute the input after the GUI (priority >= 100).
The method signature **must** be as follows:[ Return: Void; Params: NodeEditorInputInfo ]
<br>

#### <u>Hotkey Attribute</u>

The `HotkeyAttribute` is used to provide a simple interface for hotkeys for the Node Editor.
Some default controls like Navigating ('N') and Snapping ('Control') are implemented using this attribute
It allows you to specify a `KeyCode` / `EventModifier` combination with a limiting `EventType` to specify when the tagged static function gets called.
Again, the optional variable `priority` can be specified. Refer to the `EventHandlerAttribute` for it's effect.
The method signature **must** be as follows:[ Return: Void; Params: NodeEditorInputInfo ]
<br>

#### <u>ContextEntry Attribute</u>

The `ContextAttribute` is used to register context entries for the Node`Editor.
The tagged function is called when the context element at the specified path is selected.
In which context menu to add this element is specified by the type, like the node context click or the canvas context click.
The method signature **must** be as follows:[ Return: Void; Params: NodeEditorInputInfo ]
<br>

#### <u>ContextFiller Attribute</u>

The `ContextFillerAttribute` is used to register context entries in the Node Editor in a dynamic, conditional or procedural way.
This function will be called to fill the passed context `GenericMenu` in any way it likes to.
Again the type specifies the context menu to fill.
The method signature **must** be as follows:[ Return: Void; Params: NodeEditorInputInfo, GenericMenu ]


