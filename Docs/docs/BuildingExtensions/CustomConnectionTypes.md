
# Custom ConnectionTypes

Implementing custom ConnectionTypes is similar to Node implementation, as it uses the same fetching system: 
Declare a class inheriting from the `IConnectionTypeDeclaration` interface and specify it's properties.

<center>
	![ConnectionType with IConnectionTypeDeclaration] (/img/ConnectionTypes.png "ConnectionTypes.cs: Top: IConnectionTypeDeclaration; Bottom: Built-in Float type")
	<br>
	ConnectionTypes.cs: Top: IConnectionTypeDeclaration; Bottom: Built-in Float type
</center>

- The `string Identifier` is used to address the type
- The `Type Type` is the type this declaration representates and which is used to check for connection compability
- The `Color Color` is the color associated with the type, in which the knob textures as well as the connections are tinted with
- The strings `InKnobTex` and `OutKnobTex` are the paths to the knob textures relative to '_Node\_Editor/Resources_'. Defaults are '_Textures/In\_Knob.png_' and '_Textures/Out\_Knob.png_'

Do not that the names may differ in previous versions!