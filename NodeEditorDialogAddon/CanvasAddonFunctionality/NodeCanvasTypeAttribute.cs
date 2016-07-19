

using System;

public class NodeCanvasTypeAttribute : Attribute
{
    public string Name;

    public NodeCanvasTypeAttribute(string displayName)
    {
        Name = displayName;
    }
}
