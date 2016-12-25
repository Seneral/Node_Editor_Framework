
# Getting Started

<br>

## Installation
Installing is as simple as dragging and dropping the *Editor* and *Node_Editor* folders into your project at *Assets/Plugins*. It will work anywhere but would require a change in source for long term usage.
You are then able to open the window at '*Window/Node Editor*' when there are no errors in the project.


<br>

## Examples

### Editor & Canvas
You can start off by opening the Editor Window at '*Window/Node Editor*' and loading an example canvas, such as the *CalculationCanvas*. 
Use either the button at the top right or locate it in the project folder and double-click it.

Using context-clicks you can manipulate the canvas, using drag'n'drop you can drag around and connect node outputs and inputs with each other. 
`Control` will snap nodes to the grid and `N` will help you navigate back to the origin or the selected node!

### Example Extensions
For examples on simple extensions, check out all '*Examples/*'-Subbranches on the repository! <br>
One of the currently available examples is the [Texture Composer](https://github.com/Baste-RainGames/Node_Editor/tree/Examples/Texture_Composer), 
an example of adding Nodes and ConnectionTypes to extend the framework to simple texture manipulation capabilities. <br>
Another is the [Dialogue System](https://github.com/Baste-RainGames/Node_Editor/tree/Examples/Dialogue-System), 
which demonstrates the actual usage of a canvas at runtime to drive a simple dialogue.

### Example Runtime Usage
For more general ideas on how to use the canvas at runtime, you can check out `RTCanvasCalculator`, which is a component that can 
calculate and debug the canvas at runtime and also implements some basic but useful helper functions to traverse the canvas at runtime. <br>
It is also possible to show the actual GUI at runtime, as `RuntimeNodeEditor` demonstrates. It works and looks very similar to the editor window 
with some limitations due to inaccessibility to the UnityEditor namespace. But aslong as the Nodes use `RTEditorGUI` for available UI controls 
and encapsulate all editor-only GUI parts into a preprocessor checks, it is totally possible to give your player access to a Node Editor:)