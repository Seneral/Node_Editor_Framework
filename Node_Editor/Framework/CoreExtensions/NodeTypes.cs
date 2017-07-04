using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using NodeEditorFramework.Utilities;

namespace NodeEditorFramework 
{
	/// <summary>
	/// Handles fetching and storing of all Node declarations
	/// </summary>
	public static class NodeTypes
	{
		private static Dictionary<string, NodeTypeData> nodes;

		/// <summary>
		/// Fetches every Node Declaration in the script assemblies to provide the framework with custom node types
		/// </summary>
		public static void FetchNodeTypes() 
		{
			nodes = new Dictionary<string, NodeTypeData> ();
			foreach (Type type in ReflectionUtility.getSubTypes (typeof(Node)))	
			{
				object[] nodeAttributes = type.GetCustomAttributes(typeof(NodeAttribute), false);                    
				NodeAttribute attr = nodeAttributes[0] as NodeAttribute;
				if(attr == null || !attr.hide)
				{ // Only regard if it is not marked as hidden
				  // Fetch node information
					string ID, Title = "None";
					FieldInfo IDField = type.GetField("ID");
					if (IDField == null || attr == null)
					{ // Cannot read ID from const field or need to read Title because of missing attribute -> Create sample to read from properties
						Node sample = (Node)ScriptableObject.CreateInstance(type);
						ID = sample.GetID;
						Title = sample.Title;
						UnityEngine.Object.DestroyImmediate(sample);
					}
					else // Can read ID directly from const field
						ID = (string)IDField.GetValue(null);
					// Create Data from information
					NodeTypeData data = attr == null?  // Switch between explicit information by the attribute or node information
						new NodeTypeData(ID, Title, type, new Type[0]) :
						new NodeTypeData(ID, attr.contextText, type, attr.limitToCanvasTypes);
					nodes.Add (ID, data);
				}
			}
		}

		/// <summary>
		/// Returns all recorded node definitions found by the system
		/// </summary>
		public static List<NodeTypeData> getNodeDefinitions () 
		{
			return nodes.Values.ToList ();
		}

		/// <summary>
		/// Returns the NodeData for the given node type ID
		/// </summary>
		public static NodeTypeData getNodeData (string typeID)
		{
			NodeTypeData data;
			nodes.TryGetValue (typeID, out data);
			return data;
		}

		/// <summary>
		/// Returns all node IDs that can automatically connect to the specified port.
		/// If port is null, all node IDs are returned.
		/// </summary>
		public static List<string> getCompatibleNodes (ConnectionPort port)
		{
			if (port == null)
				return NodeTypes.nodes.Keys.ToList ();
			List<string> compatibleNodes = new List<string> ();
			foreach (NodeTypeData nodeData in NodeTypes.nodes.Values)
			{ // Iterate over all nodes to check compability of any of their connection ports
				if (ConnectionPortManager.GetPortDeclarations (nodeData.typeID).Any (
					(ConnectionPortDeclaration portDecl) => portDecl.portInfo.IsCompatibleWith (port)))
					compatibleNodes.Add (nodeData.typeID);
			}
			return compatibleNodes;
		}
	}

	/// <summary>
	/// The NodeData contains the additional, editor specific data of a node type
	/// </summary>
	public struct NodeTypeData 
	{
		public string typeID;
		public string adress;
		public Type type;
		public Type[] limitToCanvasTypes;

		public NodeTypeData(string ID, string name, Type nodeType, Type[] limitedCanvasTypes)
		{
			typeID = ID;
			adress = name;
			type = nodeType;
			limitToCanvasTypes = limitedCanvasTypes;
		}
	}

	/// <summary>
	/// The NodeAttribute is used to specify editor specific data for a node type, later stored using a NodeData
	/// </summary>
	public class NodeAttribute : Attribute 
	{
		public bool hide { get; private set; }
		public string contextText { get; private set; }
		public Type[] limitToCanvasTypes { get; private set; }

		public NodeAttribute (bool HideNode, string ReplacedContextText, params Type[] limitedCanvasTypes)
		{
			hide = HideNode;
			contextText = ReplacedContextText;
			limitToCanvasTypes = limitedCanvasTypes;
		}
	}
}