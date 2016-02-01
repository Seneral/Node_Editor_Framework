using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace NodeEditorFramework 
{
	public static class NodeTypes
	{
		public static Dictionary<Node, NodeData> Nodes;

		/// <summary>
		/// Fetches every Node Declaration in the assembly
		/// </summary>
		public static void FetchNodes() 
		{
			Nodes = new Dictionary<Node, NodeData> ();

			List<Assembly> scriptAssemblies = AppDomain.CurrentDomain.GetAssemblies ().Where ((Assembly assembly) => assembly.FullName.Contains ("Assembly")).ToList ();
			if (!scriptAssemblies.Contains (Assembly.GetExecutingAssembly ()))
				scriptAssemblies.Add (Assembly.GetExecutingAssembly ());
			foreach (Assembly assembly in scriptAssemblies) 
			{
				foreach (Type type in assembly.GetTypes ().Where (T => T.IsClass && !T.IsAbstract && T.IsSubclassOf (typeof (Node)))) 
				{
					object[] nodeAttributes = type.GetCustomAttributes (typeof (NodeAttribute), false);
					NodeAttribute attr = nodeAttributes [0] as NodeAttribute;
					if (attr == null || !attr.Hide)
					{
						Node node = ScriptableObject.CreateInstance (type.Name) as Node; // Create a 'raw' instance (not setup using the appropriate Create function)
						node = node.Create (Vector2.zero); // From that, call the appropriate Create Method to init the previously 'raw' instance
						Nodes.Add (node, new NodeData (attr == null? node.name : attr.ContextText));
					}
				}
			}
		}

		public static NodeData GetNodeData (Node node)
		{
			return Nodes [GetDefaultNode (node.GetID)];
		}

		public static Node GetDefaultNode (string nodeID)
		{
			return Nodes.Keys.Single<Node> ((Node node) => node.GetID == nodeID);
		}
		public static T GetDefaultNode<T> () where T : Node
		{
			return Nodes.Keys.Single<Node> ((Node node) => node.GetType () == typeof (T)) as T;
		}
	}

	public struct NodeData 
	{
		public string Adress;

		public NodeData (string name) 
		{
			Adress = name;
		}
	}

	public class NodeAttribute : Attribute 
	{
		public bool Hide { get; private set; }
		public string ContextText { get; private set; }

		public NodeAttribute (bool hideNode, string replacedContextText) 
		{
			Hide = hideNode;
			ContextText = replacedContextText;
		}
	}
}