
# Framework Overview

*(NOTE: This page is WIP)*

This section aims to bring you a decent overview on how the framework is structured, so you can get to modify it quickly. 
This does not necessarily include implementation details – code sections that need extra detailing are commented. 
Also, this section is not only for those planning to get into the code, but for everyone to get an overview what he's working with:)

<br>

### `NodeCanvas` and `NodeEditorState`

Those two components essentially make up the save file you can load up into the Editor. 
Basically, the canvas stores all the nodes and any additional information directly related to the Canvas.
In contrary, the `EditorState` holds all information on the state, or in other words, on how the Canvas is presented.
This includes zoom and pan values, selected Nodes, the canvasRect, etc. Not all of these values are actually saved with the asset, though. 
That structure allows for multiple 'views' on the same Canvas and editing it simultaneously.

<br>

### The `DrawCanvas` function

This function acts very similar to any other GUI control, with a few exceptions, and is responsible for drawing the Canvas. 
On the first glance it's just a wrapper for `DrawSubCanvas`, with the exception that it holds the `OverlayGUI` and `NodeGUISkin` code. 
`DrawSubCanvas` is used in the future for Nested Canvases, as the name proposes.

First of all, the background texture is splattered across the canvas area based on zoom and pan values.
Then, the function `NodeEditorInputSystem.HandleInputEvents` invokes all dynamic input handlers of the input system to catch all kinds of Input  events.

Afterwards the scale area gets initiated with a call to the custom solution `GUIScaleUtility.BeginScale`. <br>
Any GUI code afterwards is getting scaled appropriately.
That means that now all elements that need to be scaled are drawn, including connections, node transitions, connections, bodies and knobs. <br>
Thereafter, the scale area gets closed again with another call to `GUIScaleUtility.EndScale`. 

The `NodeEditorInputSystem.HandleLateInputEvents` function then invokes the dynamic input handlers similar to the version before,
with the exception that only those that have to be handled after GUI are invoked.

<br>

#### <u>Framework Part explanations planned</u>
- Dynamic Input System at `NodeEditorInputSystem`
- ConnectionType and Node fetching at `NodeTypes` and `ConnectionTypes`
- Knob Behaviour and Possibilities at `NodeKnob`
- Event/Callback System at `NodeEditorCallbackReceiver`
- Save System at `NodeEditorSaveManager`
- Various Utilities like `GUIScaleUtility`
- Calculation System at `NodeEditor`
- Transitioning System including UnityFunc if they are ready
- Runtime GUI and limitations at `RTEditorGUI` mostly
- Experimental/Conceptional custom NodeCanvases and traversal algorithms

