using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

public static class NodeTypes
{
	public static List<Node> nodes;

	/// <summary>
	/// Fetches every Node Declaration in the assembly
	/// </summary>
	public static void FetchNodes() 
	{
		nodes = new List<Node> ();

		Assembly assembly = Assembly.GetExecutingAssembly ();
		foreach (Type type in assembly.GetTypes ().Where (T => T.IsClass && !T.IsAbstract && T.IsSubclassOf (typeof (Node)))) 
		{
			object[] nodeAttributes = type.GetCustomAttributes (typeof (NodeAttribute), false);
			if (nodeAttributes.Length == 0 || !(nodeAttributes [0] as NodeAttribute).hide)
			{
				Node node = ScriptableObject.CreateInstance (type.Name) as Node; // Create a 'raw' instance (not setup using the appropriate Create function)
				node = node.Create (Vector2.zero); // From that, call the appropriate Create Method to init the previously 'raw' instance
				nodes.Add (node);
			}
		}

		if (assembly != Assembly.GetCallingAssembly ())
		{
			assembly = Assembly.GetCallingAssembly ();
			foreach (Type type in assembly.GetTypes ().Where (T => T.IsClass && !T.IsAbstract && T.IsSubclassOf (typeof (Node)))) 
			{
				Node node = ScriptableObject.CreateInstance (type.Name) as Node; // Create a 'raw' instance (not setup using the appropriate Create function)
				node = node.Create (Vector2.zero); // From that, call the appropriate Create Method to init the previously 'raw' instance
				nodes.Add (node);
			}
		}

//		foreach (Node nodeType in nodes)
//			Debug.Log (nodeType.name + " fetched.");
	}
}

public class NodeAttribute : Attribute 
{
	public bool hide { get; set; }
	public string replacedContext { get; set; }

	public NodeAttribute (bool HideNode, string ReplacedContextText) 
	{
		hide = HideNode;
		replacedContext = ReplacedContextText;
	}
}
