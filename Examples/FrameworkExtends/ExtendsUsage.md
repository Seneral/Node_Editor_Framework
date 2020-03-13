## Example of extending Framework classes
Certain classes in the framework can be extended directly by using the C# partial class feature.
With the framework code now residing in their own assemblies this requires some modification to get to work.

This example shows how you can add custom fields to some hardcoded NEF classes, such as:
- NodeEditor
- NodeEditorState
- Node
- NodeEditorCallbackReceiver
- NodeEditorGUI
- NodeCanvasSceneSave
- NodeEditorSaveManager

Prerequisites:
- Copy the class header, including partial keyword, without base classes, etc.
- Wrap it in the same namespace the original partial class is in
- Add it to the same Assembly (Seneral.NodeEditorFramework.** - Editor, Runtime, Standard)
	- Put it into a folder with other extends of the same assembly
	- Create a Assembly Definition Reference asset in that folder
	- Add a reference to the original Assembly Definition asset to it using a normal GUID reference
	- Now each script in that folder are compiled into that same assembly and their partial content is embedded into the class as if it belonged to the original framework source

This can be used for certain state information that cannot be added by subclassing, e.g. a custom NodeCanvas