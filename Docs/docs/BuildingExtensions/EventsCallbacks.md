
# Events

The Framework supports a collection of events which might be important during the editing process. 
Those Events can either be received by subscribing to the appropriate delegate in the NodeEditorCallbacks class 
or by extending from NodeEditorCallbackReceiver (which is a MonoBehaviour) and overriding the appropriate method. 
Both classes can be found in `NodeEditorCallbackReceiver` <br>
##### Current Events

- `OnEditorStartup` : The Node Editor gets initiated (can also happen when switching scene or playmode)
- `OnLoadCanvas` (NodeCanvas): The passed canvas has been loaded as a copy
- `OnLoadEditorState` (NodeEditorState): The passed editorState has been loaded as a copy
- `OnSaveCanvas` (NodeCanvas): The passed canvas has been saved as a copy
- `OnSaveEditorState` (NodeEditorState): The passed editorState has been saved as a copy
<br> <br>
- `OnAddNode` (Node): The passed node has been created or duplicated
- `OnDeleteNode` (Node): The passed node will get deleted
- `OnMoveNode` (Node): The passed node has been moved by the user
<br> <br>
- `OnAddConnection` (NodeInput): A new connection has been added to the passed input. If it had a connection before, *OnRemoveConnection* has been called, too
- `OnRemoveConnection` (NodeInput): The connection will get removed from this input
<br> <br>
##### WIP Transitioning System:
- `OnAddTransition` (Transition): The passed transition has been created
- `OnRemoveTransition` (Transition): The passed transition will be removed

<br>

-> Some of the Node-specific callbacks can also be accessed from the Node directly by overriding the appropriate method.
<br>
-> You can always implement additional callbacks or request them to be implemented!

