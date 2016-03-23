
# Building a custom Editor

The provided Editor Window serves as the default Node Canvas Explorer for all dependant extensions that gets the job done.
But in order to make extensions that are built on top of this Framework unique, you'll sooner or later need to built your own Editor Interface.
The following outlines the most important things to consider in order to build a basic Node Editor Interface in both Runtime and the Editor.

<br>

### The Canvas and Editor States

The Editor obviously has to stores the currently opened NodeCanvas and it's NodeEditorState in the first place.
For a detailed explanation of these, please look up the [Framework Overview](FrameworkOverview.md). <br>
You can save both using `NodeEditor.SaveNodeCanvas` and load them with `NodeEditor.LoadNodeCanvas` and `NodeEditor.LoadEditorStates` respectively.
Take reference from the default `NodeEditorWindow` to see how exactly these functions are integrated. 
The function `AutoOpenCanvas` also shows how to automatically open a canvas by double-clicking it's asset in the Project View. 
The default window also features a cache system, which is currently not the most optimal implementation possible.

<br>

### The Canvas GUI

The GUI for the canvas is mainly compressed into the single function `NodeEditor.DrawCanvas`. 
Before you can call it though you need to make sure that the `NodeEditor` is initiated using `NodeEditor.checkInit` and that there is always a canvas loaded. <br>
Additionally you'll want to define the area in which the canvas is drawn by assigning the `EditorState.canvasRect` property.
No limitations exist anymore on where the canvas is drawn, in how many groups, etc, only the case of modifying the `GUI.matrix` scale before is not yet supported. <br>
In order to best account for errors that may be thrown, the drawing function should be embedded in a try-catch block that unloads the canvas when an error was thrown.
Make sure you only catch `UnityExceptions` though, because of a Unity bug all pickers like `ColorField`, `CurveField` or `ObjectField` will throw an error when inside a `System.Exception` try-catch-block. <br>
In this try-catch-block you can safely call `NodeEditor.DrawCanvas`, passing both the `NodeCanvas` and the `EditorState`, in order to draw the canvas in the specified area.
All additional interface elements like toolbar, side panel, etc. are up to you to handle.

<br>

### Custom GUI Skin

The GUISkin of the Node Editor can currently only be changed by modifying the `NodeEditorGUI` source file or by replacing the textures. 
For the future a more extensive and separated control over the GUISkin is planned.