using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using NodeEditorFramework.Utilities;

namespace NodeEditorFramework 
{
	public static class ConnectionPortManager
	{
		private static Dictionary<string, ConnectionPortDeclaration[]> nodePortDeclarations;

		/// <summary>
		/// Fetches every node connection declaration for each node type for later use
		/// </summary>
		public static void FetchNodeConnectionDeclarations()
		{
			nodePortDeclarations = new Dictionary<string, ConnectionPortDeclaration[]> ();
			foreach (NodeTypeData nodeData in NodeTypes.getNodeDefinitions ())	
			{
				Type nodeType = nodeData.type;
				List<ConnectionPortDeclaration> declarations = new List<ConnectionPortDeclaration> ();
				// Get all declared port fields
				FieldInfo[] declaredPorts = ReflectionUtility.getFieldsOfType (nodeType, typeof(ConnectionPort));
				foreach (FieldInfo portField in declaredPorts)
				{ // Get info about that port declaration using the attribute
					object[] declAttrs = portField.GetCustomAttributes (typeof(ConnectionPortAttribute), true);
					if (declAttrs.Length < 1)
						continue;
					ConnectionPortAttribute declarationAttr = (ConnectionPortAttribute)declAttrs[0];
					if (declarationAttr.MatchFieldType (portField.FieldType))
						declarations.Add (new ConnectionPortDeclaration (portField, declarationAttr));
					else
						Debug.LogError ("Mismatched " + declarationAttr.GetType ().Name + " for " + portField.FieldType.Name + " '" + declarationAttr.Name + "' on " + nodeData.type.Name + "!");
				}
				nodePortDeclarations.Add (nodeData.typeID, declarations.ToArray ());
			}
		}

		/// <summary>
		/// Updates all node connection ports in the given node and creates or adjusts them according to the declaration
		/// </summary>
		public static void UpdateConnectionPorts (Node node)
		{
			foreach (ConnectionPortDeclaration portDecl in GetPortDeclarationEnumerator (node, true))
			{
				ConnectionPort port = (ConnectionPort)portDecl.portField.GetValue (node);
				if (port == null)
				{ // Create new port from declaration
					port = portDecl.portInfo.CreateNew (node);
					portDecl.portField.SetValue (node, port);
				}
				else 
				{ // Check port values against port declaration
					portDecl.portInfo.UpdateProperties (port);
				}
			}
		}

		/// <summary>
		/// Updates the connectionPorts and connectionKnobs lists of the given node with all declared nodes
		/// </summary>
		public static void UpdatePortLists (Node node) 
		{
			foreach (ConnectionPortDeclaration portDecl in GetPortDeclarationEnumerator (node, true))
			{ /* Triggering is enough to update the list */ }
		}

		/// <summary>
		/// Returns the ConnectionPortDeclarations for the given node type
		/// </summary>
		public static ConnectionPortDeclaration[] GetPortDeclarations (string nodeTypeID) 
		{
			ConnectionPortDeclaration[] portDecls;
			if (nodePortDeclarations.TryGetValue (nodeTypeID, out portDecls))
				return portDecls;
			else
				throw new ArgumentException ("Could not find node port declarations for node type '" + nodeTypeID + "'!");
		}

		/// <summary>
		/// Returns an enumerator of type ConnectionPortDeclaration over all port declarations of the given node
		/// </summary>
		public static IEnumerable GetPortDeclarationEnumerator (Node node, bool triggerUpdate = false) 
		{
			List<ConnectionPort> connectionPorts = new List<ConnectionPort> ();
			ConnectionPortDeclaration[] portDecls;
			if (nodePortDeclarations.TryGetValue (node.GetID, out portDecls))
			{
				foreach (ConnectionPortDeclaration portDecl in portDecls)
				{ // Iterate over each connection port and yield it's declaration
					yield return portDecl;
					connectionPorts.Add ((ConnectionPort)portDecl.portField.GetValue (node));
				}
			}
			if (triggerUpdate)
			{ // Update lists as values might have changes when calling this function
				// Ports
				node.connectionPorts = connectionPorts;
				node.inputPorts = node.connectionPorts.Where ((ConnectionPort port) => port.direction == Direction.In).ToList ();
				node.outputPorts = node.connectionPorts.Where ((ConnectionPort port) => port.direction == Direction.Out).ToList ();
				// Knobs
				node.connectionKnobs = connectionPorts.OfType<ConnectionKnob> ().ToList ();
				node.inputKnobs = node.connectionKnobs.Where ((ConnectionKnob knob) => knob.direction == Direction.In).ToList ();
				node.outputKnobs = node.connectionKnobs.Where ((ConnectionKnob knob) => knob.direction == Direction.Out).ToList ();
			}
		}
	}

	public class ConnectionPortDeclaration
	{
		public FieldInfo portField;
		public ConnectionPortAttribute portInfo;

		public ConnectionPortDeclaration (FieldInfo field, ConnectionPortAttribute attr) 
		{
			portField = field;
			portInfo = attr;
		}
	}
}