
# Building a custom Editor

The provided Editor Window serves as the default Node Canvas Explorer for all dependant extensions that gets the job done.
But in order to make extensions that are built on top of this Framework unique, you'll sooner or later need to built your own Editor Interface.
The following outlines the most important things to consider in order to build a basic Node Editor Interface in both Runtime and the Editor.

<br>

### The Canvas and Editor States

The Editor obviously has to stores the currently opened NodeCanvas and it's NodeEditorState in the first place.
For a detailed explanation of these, please look up the [Framework Overview](../FrameworkOverview.md). <br>
`NodeEditorUserCache` is a wrapper class to aid your extension managing the canvas and editor state. For the majority of cases, it is perfectly fine.
The easy API for saving/loading and even caching in the editor works both in the editor and at runtime.

<br>

### The Canvas GUI

For the GUI to look the same in the whole window and to allow for custom popups in your GUI, you first need to call `NodeEditorGUI.StartNodeGUI`. At the end you need to call `NodeEditorGUI.EndNodeGUI`. <br>
Before you can draw the canvas area, first make sure a canvas is loaded and assign the rect for the canvas area to your `NodeEditorState.canvasRect` property.
Also, not that modifying the `GUI.matrix` scale while when drawing the canvas area is not yet supported. <br>
In order to best account for errors that may be thrown, the drawing function should be embedded in a try-catch block that unloads the canvas when an error was thrown.
Make sure you only catch `UnityExceptions` though, because of a Unity bug all pickers like `ColorField`, `CurveField` or `ObjectField` will throw an error when inside a `System.Exception` try-catch-block. <br>
In this try-catch-block you can safely call `NodeEditor.DrawCanvas`, passing both the `NodeCanvas` and the `EditorState`, in order to draw the canvas in the specified area.
All additional interface elements like toolbar, side panel, etc. are up to you to handle, and are easily filled using the API of the Framework.

<br>

### Custom GUI Skin

The GUISkin of the Node Editor can currently only be changed by modifying the `NodeEditorGUI` source file or by replacing the textures. 
For the future a more extensive and separated control over the GUISkin is planned.