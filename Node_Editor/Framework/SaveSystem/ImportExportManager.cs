using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

using NodeEditorFramework.Utilities;

namespace NodeEditorFramework.IO
{
	/// <summary>
	/// Manager for Import Export operations, including fetching of all supported formats
	/// </summary>
	public static class ImportExportManager
	{
		private static Dictionary<string, ImportExportFormat> IOFormats;

		private static Action<string> _importMenuCallback;
		private static Action<string> _exportMenuCallback;

		/// <summary>
		/// Fetches every IO Format in the script assemblies to provide the framework with custom import and export formats
		/// </summary>
		public static void FetchIOFormats()
		{
			IOFormats = new Dictionary<string, ImportExportFormat>();
			foreach (Type type in ReflectionUtility.getSubTypes (typeof(ImportExportFormat)))
			{
				ImportExportFormat formatter = (ImportExportFormat)Activator.CreateInstance (type);
				IOFormats.Add (formatter.FormatIdentifier, formatter);
			}
		}

		/// <summary>
		/// Returns the format specified by the given IO format identifier
		/// </summary>
		public static ImportExportFormat ParseFormat (string IOformat)
		{
			ImportExportFormat formatter;
			if (IOFormats.TryGetValue (IOformat, out formatter))
				return formatter;
			else
				throw new ArgumentException ("Unknown format '" + IOformat + "'");
		}

		/// <summary>
		/// Imports the canvas with the given formatter from the location specified in the args and returns it
		/// </summary>
		public static NodeCanvas ImportCanvas (ImportExportFormat formatter, params object[] args)
		{
			return formatter.Import (args);
		}

		/// <summary>
		/// Exports the given canvas with the given formatter to the location specified in the args
		/// </summary>
		public static void ExportCanvas (NodeCanvas canvas, ImportExportFormat formatter, params object[] args)
		{
			formatter.Export (canvas, args);
		}

		#region Canvas Type Menu

		public static void FillImportFormatMenu(ref GenericMenu menu, Action<string> IOFormatSelection, string path = "")
		{
			_importMenuCallback = IOFormatSelection;
			foreach (string formatID in IOFormats.Keys)
				menu.AddItem(new GUIContent(path + formatID), false, unwrapInputFormatIDCallback, (object)formatID);
		}

	#if UNITY_EDITOR
		public static void FillImportFormatMenu(ref UnityEditor.GenericMenu menu, Action<string> IOFormatSelection, string path = "")
		{
			_importMenuCallback = IOFormatSelection;
			foreach (string formatID in IOFormats.Keys)
				menu.AddItem(new GUIContent(path + formatID), false, unwrapInputFormatIDCallback, (object)formatID);
		}
	#endif

		public static void FillExportFormatMenu(ref GenericMenu menu, Action<string> IOFormatSelection, string path = "")
		{
			_exportMenuCallback = IOFormatSelection;
			foreach (string formatID in IOFormats.Keys)
				menu.AddItem(new GUIContent(path + formatID), false, unwrapExportFormatIDCallback, (object)formatID);
		}

	#if UNITY_EDITOR
		public static void FillExportFormatMenu(ref UnityEditor.GenericMenu menu, Action<string> IOFormatSelection, string path = "")
		{
			_exportMenuCallback = IOFormatSelection;
			foreach (string formatID in IOFormats.Keys)
				menu.AddItem(new GUIContent(path + formatID), false, unwrapExportFormatIDCallback, (object)formatID);
		}
	#endif

		private static void unwrapInputFormatIDCallback(object formatID)
		{
			_importMenuCallback((string)formatID);
		}

		private static void unwrapExportFormatIDCallback(object formatID)
		{
			_exportMenuCallback((string)formatID);
		}

		#endregion

		#region Converter

		/// <summary>
		/// Converts the NodeCanvas to a simplified CanvasData
		/// </summary>
		internal static CanvasData ConvertToCanvasData (NodeCanvas canvas) 
		{
			if (canvas == null)
				return null;
			
			// Validate canvas and create canvas data for it
			canvas.Validate ();
			CanvasData canvasData = new CanvasData (canvas);

			// Store Lookup-Table for all ports
			Dictionary<ConnectionPort, PortData> portDatas = new Dictionary<ConnectionPort, PortData>();

			foreach (Node node in canvas.nodes)
			{
				// Create node data
				NodeData nodeData = new NodeData (node);
				canvasData.nodes.Add (nodeData.nodeID, nodeData);

				foreach (ConnectionPortDeclaration portDecl in ConnectionPortManager.GetPortDeclarationEnumerator(node))
				{ // Fetch all static connection port declarations and record them
					ConnectionPort port = (ConnectionPort)portDecl.portField.GetValue(node);
					PortData portData = new PortData(nodeData, port, portDecl.portField.Name);
					nodeData.connectionPorts.Add(portData);
					portDatas.Add(port, portData);
				}

				foreach (ConnectionPort port in node.dynamicConnectionPorts)
				{ // Fetch all dynamic connection ports and record them
					PortData portData = new PortData(nodeData, port);
					nodeData.connectionPorts.Add(portData);
					portDatas.Add(port, portData);
				}

				// Fetch all serialized node variables specific to each node's implementation
				FieldInfo[] serializedFields = ReflectionUtility.getSerializedFields (node.GetType (), typeof(Node));
				foreach (FieldInfo field in serializedFields)
				{ // Create variable data and enter the 
					if (field.FieldType.IsSubclassOf(typeof(ConnectionPort)))
						continue;
					VariableData varData = new VariableData (field);
					nodeData.variables.Add (varData);
					object varValue = field.GetValue (node);
					if (field.FieldType.IsValueType) // Store value of the object
						varData.value = varValue;
					else // Store reference to the object
						varData.refObject = canvasData.ReferenceObject (varValue);
				}
			}

			foreach (PortData portData in portDatas.Values)
			{ // Record the connections of this port
				foreach (ConnectionPort conPort in portData.port.connections)
				{
					PortData conPortData; // Get portData associated with the connection port
					if (portDatas.TryGetValue(conPort, out conPortData))
						canvasData.RecordConnection(portData, conPortData);
				}
			}

			foreach (NodeGroup group in canvas.groups)
			{ // Record all groups
				canvasData.groups.Add(new GroupData(group));
			}

			canvasData.editorStates = new EditorStateData[canvas.editorStates.Length];
			for (int i = 0; i < canvas.editorStates.Length; i++)
			{ // Record all editorStates
				NodeEditorState state = canvas.editorStates[i];
				NodeData selected = state.selectedNode == null ? null : canvasData.FindNode(state.selectedNode);
				canvasData.editorStates[i] = new EditorStateData(selected, state.panOffset, state.zoom);
			}

			return canvasData;
		}

		/// <summary>
		/// Converts the simplified CanvasData back to a proper NodeCanvas
		/// </summary>
		internal static NodeCanvas ConvertToNodeCanvas (CanvasData canvasData)
		{
			if (canvasData == null)
				return null;
			NodeCanvas nodeCanvas = NodeCanvas.CreateCanvas(canvasData.type);
			nodeCanvas.name = nodeCanvas.saveName = canvasData.name;
			nodeCanvas.nodes.Clear();
			NodeEditor.BeginEditingCanvas(nodeCanvas);

			foreach (NodeData nodeData in canvasData.nodes.Values)
			{ // Read all nodes
				Node node = Node.Create (nodeData.typeID, nodeData.nodePos, nodeCanvas, null, true, false);
				if (!string.IsNullOrEmpty(nodeData.name))
					node.name = nodeData.name;
				if (node == null)
					continue;

				foreach (ConnectionPortDeclaration portDecl in ConnectionPortManager.GetPortDeclarationEnumerator(node))
				{ // Find stored ports for each node port declaration
					PortData portData = nodeData.connectionPorts.Find((PortData data) => data.name == portDecl.portField.Name);
					if (portData != null) // Stored port has been found, record
						portData.port = (ConnectionPort)portDecl.portField.GetValue(node);
				}

				foreach (PortData portData in nodeData.connectionPorts.Where(port => port.dynamic))
				{ // Find stored dynamic connection ports
					if (portData.port != null) // Stored port has been recreated
					{
						portData.port.body = node;
						node.dynamicConnectionPorts.Add(portData.port);
					}
				}

				foreach (VariableData varData in nodeData.variables)
				{ // Restore stored variable to node
					FieldInfo field = node.GetType().GetField(varData.name);
					if (field != null)
						field.SetValue(node, varData.refObject != null ? varData.refObject.data : varData.value);
				}
			}

			foreach (ConnectionData conData in canvasData.connections)
			{ // Restore all connections
				if (conData.port1.port == null || conData.port2.port == null)
				{ // Not all ports where saved in canvasData
					Debug.Log("Incomplete connection " + conData.port1.name + " and " + conData.port2.name + "!");
					continue;
				}
				conData.port1.port.TryApplyConnection(conData.port2.port, true);
			}
			
			foreach (GroupData groupData in canvasData.groups)
			{ // Recreate groups
				NodeGroup group = new NodeGroup();
				group.title = groupData.name;
				group.rect = groupData.rect;
				group.color = groupData.color;
				nodeCanvas.groups.Add(group);
			}

			nodeCanvas.editorStates = new NodeEditorState[canvasData.editorStates.Length];
			for (int i = 0; i < canvasData.editorStates.Length; i++)
			{ // Read all editorStates
				EditorStateData stateData = canvasData.editorStates[i];
				NodeEditorState state = ScriptableObject.CreateInstance<NodeEditorState>();
				state.selectedNode = stateData.selectedNode == null ? null : canvasData.FindNode(stateData.selectedNode.nodeID).node;
				state.panOffset = stateData.panOffset;
				state.zoom = stateData.zoom;
				state.canvas = nodeCanvas;
				state.name = "EditorState";
				nodeCanvas.editorStates[i] = state;
			}

			NodeEditor.EndEditingCanvas();
			return nodeCanvas;
		}

		#endregion
	}
}