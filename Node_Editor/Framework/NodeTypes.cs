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
		public static Dictionary<Node, NodeData> nodes;

		/// <summary>
		/// Fetches every Node Declaration in the assembly
		/// </summary>
		public static void FetchNodes() 
		{
			nodes = new Dictionary<Node, NodeData> ();

			List<Assembly> scriptAssemblies = AppDomain.CurrentDomain.GetAssemblies ().ToList ();
			if (!scriptAssemblies.Contains (Assembly.GetExecutingAssembly ()))
				scriptAssemblies.Add (Assembly.GetExecutingAssembly ());
			foreach (Assembly assembly in scriptAssemblies) 
			{
				foreach (Type type in assembly.GetTypes ().Where (T => T.IsClass && !T.IsAbstract && T.IsSubclassOf (typeof (Node)))) 
				{
					object[] nodeAttributes = type.GetCustomAttributes (typeof (NodeAttribute), false);
					NodeAttribute attr = nodeAttributes [0] as NodeAttribute;
					if (attr == null || !attr.hide)
					{
						Node node = ScriptableObject.CreateInstance (type.Name) as Node; // Create a 'raw' instance (not setup using the appropriate Create function)
						node = node.Create (Vector2.zero); // From that, call the appropriate Create Method to init the previously 'raw' instance
						nodes.Add (node, attr == null? new NodeData (node.name) : new NodeData (attr.contextText, attr.transitions));
					}
				}
			}
		}

		public static NodeData getNodeData (Node node)
		{
			return nodes [getDefaultNode (node.GetID)];
		}

		public static Node getDefaultNode (string ID)
		{
			return nodes.Keys.Single<Node> ((Node node) => node.GetID == ID);
		}
		public static T getDefaultNode<T> () where T : Node
		{
			return nodes.Keys.Single<Node> ((Node node) => node.GetType () == typeof (T)) as T;
		}
	}

	public struct NodeData 
	{
		public string adress;
		public bool transitions;
		
		public NodeData (string ReplacedContextText, bool acceptTransitions) 
		{
			adress = ReplacedContextText;
			transitions = acceptTransitions;
		}

		public NodeData (string name) 
		{
			adress = name;
			transitions = false;
		}
	}

	public class NodeAttribute : Attribute 
	{
		public bool hide { get; private set; }
		public string contextText { get; private set; }
		public bool transitions { get; private set; }

		public NodeAttribute (bool HideNode, string ReplacedContextText, bool acceptTransitions) 
		{
			hide = HideNode;
			contextText = ReplacedContextText;
			transitions = acceptTransitions;
		}
	}
}