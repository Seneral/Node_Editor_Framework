using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

public static class NodeTypes
{
	public static Dictionary<Node, string> nodes;

	/// <summary>
	/// Fetches every Node Declaration in the assembly
	/// </summary>
	public static void FetchNodes() 
	{
		nodes = new Dictionary<Node, string> ();

		Assembly assembly = Assembly.GetExecutingAssembly ();
		foreach (Type type in assembly.GetTypes ().Where (T => T.IsClass && !T.IsAbstract && T.IsSubclassOf (typeof (Node)))) 
		{
			object[] nodeAttributes = type.GetCustomAttributes (typeof (NodeAttribute), false);
			if (nodeAttributes.Length == 0 || !(nodeAttributes [0] as NodeAttribute).hide)
			{
				Node node = ScriptableObject.CreateInstance (type.Name) as Node; // Create a 'raw' instance (not setup using the appropriate Create function)
				node = node.Create (Vector2.zero); // From that, call the appropriate Create Method to init the previously 'raw' instance
				nodes.Add (node, nodeAttributes.Length == 0? node.name : (nodeAttributes [0] as NodeAttribute).contextText);
			}
		}

		if (assembly != Assembly.GetCallingAssembly ())
		{
			assembly = Assembly.GetCallingAssembly ();
			foreach (Type type in assembly.GetTypes ().Where (T => T.IsClass && !T.IsAbstract && T.IsSubclassOf (typeof (Node)))) 
			{
				object[] nodeAttributes = type.GetCustomAttributes (typeof (NodeAttribute), false);
				if (nodeAttributes.Length == 0 || !(nodeAttributes [0] as NodeAttribute).hide)
				{
					Node node = ScriptableObject.CreateInstance (type.Name) as Node; // Create a 'raw' instance (not setup using the appropriate Create function)
					node = node.Create (Vector2.zero); // From that, call the appropriate Create Method to init the previously 'raw' instance
					nodes.Add (node, nodeAttributes.Length == 0? node.name : (nodeAttributes [0] as NodeAttribute).contextText);
				}
			}
		}

//		foreach (Node node in nodes.Keys)
//			Debug.Log (node.name + " fetched.");
	}
}

public class NodeAttribute : Attribute 
{
	public bool hide { get; set; }
	public string contextText { get; set; }

	public NodeAttribute (bool HideNode, string ReplacedContextText) 
	{
		hide = HideNode;
		contextText = ReplacedContextText;
	}
}
