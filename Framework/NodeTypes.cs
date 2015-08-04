using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using NodeEditorFramework;

namespace NodeEditorFramework 
{
	public static class NodeTypes
	{
		public static Dictionary<Node, string> nodes;

		/// <summary>
		/// Fetches every Node Declaration in the assembly
		/// </summary>
		public static void FetchNodes() 
		{
			nodes = new Dictionary<Node, string> ();

			List<Assembly> scriptAssemblies = AppDomain.CurrentDomain.GetAssemblies ()
				.Where ((Assembly a) => a.FullName.StartsWith ("Assembly-"))
					.ToList (); // This filters out all script assemblies
			if (!scriptAssemblies.Contains (Assembly.GetExecutingAssembly ()))
				scriptAssemblies.Add (Assembly.GetExecutingAssembly ());
			foreach (Assembly assembly in scriptAssemblies) 
			{
				Debug.Log (assembly.FullName);
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

		public static T getDefault<T> () where T : Node
		{
			return nodes.Keys.Single<Node> ((Node node) => node.GetType () == typeof (T)) as T;
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
}