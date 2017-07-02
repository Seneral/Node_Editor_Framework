using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using UnityEngine;

namespace NodeEditorFramework.IO
{
	public class XMLImportExport : StructuredImportExportFormat
	{
		public override string FormatIdentifier { get { return "XML"; } }
		public override string FormatExtension { get { return "xml"; } }

		public override void ExportData (CanvasData data, params object[] args) 
		{
			if (args == null || args.Length != 1 || args[0].GetType () != typeof(string))
				throw new ArgumentException ("Location Arguments");
			string path = (string)args[0];
			
			XmlDocument saveDoc = new XmlDocument();
			XmlDeclaration decl = saveDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
			saveDoc.InsertBefore(decl, saveDoc.DocumentElement);

			// CANVAS

			XmlElement canvas = saveDoc.CreateElement("NodeCanvas");
			canvas.SetAttribute("type", data.type.FullName);
			saveDoc.AppendChild(canvas);

			// EDITOR STATES

			XmlElement editorStates = saveDoc.CreateElement("EditorStates");
			canvas.AppendChild(editorStates);
			foreach (EditorStateData stateData in data.editorStates)
			{
				XmlElement editorState = saveDoc.CreateElement("EditorState");
				editorState.SetAttribute("selected", stateData.selectedNode != null ? stateData.selectedNode.nodeID.ToString() : "");
				editorState.SetAttribute("pan", stateData.panOffset.x + "," + stateData.panOffset.y);
				editorState.SetAttribute("zoom", stateData.zoom.ToString());
				editorStates.AppendChild(editorState);
			}

			// NODES

			XmlElement nodes = saveDoc.CreateElement("Nodes");
			canvas.AppendChild(nodes);
			foreach (NodeData nodeData in data.nodes.Values)
			{
				XmlElement node = saveDoc.CreateElement("Node");
				node.SetAttribute("ID", nodeData.nodeID.ToString());
				node.SetAttribute("type", nodeData.typeID);
				node.SetAttribute("pos", nodeData.nodePos.x + "," + nodeData.nodePos.y);
				nodes.AppendChild(node);
				// Write port records
				//XmlElement connectionPorts = saveDoc.CreateElement("ConnectionPorts");
				//node.AppendChild(connectionPorts);
				foreach (PortData portData in nodeData.connectionPorts)
				{
					XmlElement port = saveDoc.CreateElement("Port");
					port.SetAttribute("ID", portData.portID.ToString ());
					port.SetAttribute("varName", portData.varName);
					node.AppendChild(port);
					// Connections
					/*foreach (PortData conData in portData.connections)
					{ // TODO: Write immediate connections. Not needed, only for readability.
						XmlElement connection = saveDoc.CreateElement("Connection");
						connection.SetAttribute("ID", conData.portID.ToString());
						port.AppendChild(connection);
					}*/
				}
				// Write variable data
				//XmlElement variables = saveDoc.CreateElement("Variables");
				//node.AppendChild(variables);
				foreach (VariableData varData in nodeData.variables)
				{
					XmlElement variable = saveDoc.CreateElement("Variable");
					variable.SetAttribute("name", varData.name);
					node.AppendChild(variable);
					if (varData.refObject != null)
						variable.SetAttribute("refID", varData.refObject.refID.ToString());
					else
					{ // Serialize value and append
						variable.SetAttribute("type", varData.value.GetType ().FullName);
						SerializeObjectToXML(variable, varData.value);
					}
				}
			}

			// CONNECTIONS

			XmlElement connections = saveDoc.CreateElement("Connections");
			canvas.AppendChild(connections);
			foreach (ConnectionData connectionData in data.connections)
			{
				XmlElement connection = saveDoc.CreateElement("Connection");
				connection.SetAttribute("port1ID", connectionData.port1.portID.ToString ());
				connection.SetAttribute("port2ID", connectionData.port2.portID.ToString ());
				connections.AppendChild(connection);
			}

			// OBJECTS

			XmlElement objects = saveDoc.CreateElement("Objects");
			canvas.AppendChild(objects);
			foreach (ObjectData objectData in data.objects.Values)
			{
				XmlElement obj = saveDoc.CreateElement("Object");
				obj.SetAttribute("refID", objectData.refID.ToString());
				obj.SetAttribute("type", objectData.data.GetType().FullName);
				objects.AppendChild(obj);
				SerializeObjectToXML(obj, objectData.data);
			}

			Directory.CreateDirectory(Path.GetDirectoryName(path));
			using (XmlTextWriter writer = new XmlTextWriter (path, Encoding.UTF8))
			{
				writer.Formatting = Formatting.Indented;
				writer.Indentation = 1;
				writer.IndentChar = '\t';
				saveDoc.Save (writer);
			}
		}

		public override CanvasData ImportData (params object[] args)
		{
			if (args == null || args.Length != 1 || args[0].GetType () != typeof(string))
				throw new ArgumentException ("Location Arguments");
			string path = (string)args[0];

			using (FileStream fs = new FileStream (path, FileMode.Open))
			{
				XmlDocument data = new XmlDocument ();
				data.Load (fs);

				// CANVAS

				string canvasName = Path.GetFileNameWithoutExtension(path);
				XmlElement xmlCanvas = (XmlElement)data.SelectSingleNode ("//NodeCanvas");
				Type canvasType = NodeCanvasManager.GetCanvasTypeData (xmlCanvas.GetAttribute ("type")).CanvasType;
				if (canvasType == null)
					throw new XmlException ("Could not find NodeCanvas of type '" + xmlCanvas.GetAttribute ("type") + "'!");
				CanvasData canvasData = new CanvasData (canvasType, canvasName);
				Dictionary<int, PortData> ports = new Dictionary<int, PortData>();

				// OBJECTS

				IEnumerable<XmlElement> xmlObjects = xmlCanvas.SelectNodes("Objects/Object").OfType<XmlElement>();
				foreach (XmlElement xmlObject in xmlObjects)
				{
					int refID = GetIntegerAttribute(xmlObject, "refID");
					string typeName = xmlObject.GetAttribute("type");
					Type type = Type.GetType(typeName, true);
					object obj = DeserializeObjectFromXML(xmlObject, type);
					ObjectData objData = new ObjectData(refID, obj);
					canvasData.objects.Add(refID, objData);
				}

				// NODES

				IEnumerable<XmlElement> xmlNodes = xmlCanvas.SelectNodes("Nodes/Node").OfType<XmlElement>();
				foreach (XmlElement xmlNode in xmlNodes)
				{
					int nodeID = GetIntegerAttribute(xmlNode, "ID");
					string typeID = xmlNode.GetAttribute("type");
					Vector2 nodePos = GetVectorAttribute(xmlNode, "pos");
					// Record
					NodeData node = new NodeData(typeID, nodeID, nodePos);
					canvasData.nodes.Add(nodeID, node);
					// Validate and record ports
					IEnumerable<XmlElement> xmlConnectionPorts = xmlNode.SelectNodes("Port").OfType<XmlElement>();
					foreach (XmlElement xmlPort in xmlConnectionPorts)
					{
						int portID = GetIntegerAttribute(xmlPort, "ID");
						string varName = xmlPort.GetAttribute("varName");
						PortData port = new PortData(node, varName, portID);
						node.connectionPorts.Add(port);
						ports.Add(portID, port);
					}
					// Read in variable data
					IEnumerable<XmlElement> xmlVariables = xmlNode.SelectNodes("Variable").OfType<XmlElement>();
					foreach (XmlElement xmlVariable in xmlVariables)
					{
						string varName = xmlVariable.GetAttribute("name");
						VariableData varData = new VariableData(varName);
						if (xmlVariable.HasAttribute("refID"))
						{ // Read from objects
							int refID = GetIntegerAttribute(xmlVariable, "refID");
							ObjectData objData;
							if (canvasData.objects.TryGetValue(refID, out objData))
								varData.refObject = objData;
						}
						else
						{ // Read directly
							string typeName = xmlVariable.GetAttribute("type");
							Type type = Type.GetType(typeName, true);
							varData.value = DeserializeObjectFromXML(xmlVariable, type);
						}
						node.variables.Add(varData);
					}
				}

				// CONNECTIONS

				IEnumerable<XmlElement> xmlConnections = xmlCanvas.SelectNodes("Connections/Connection").OfType<XmlElement>();
				foreach (XmlElement xmlConnection in xmlConnections)
				{
					int port1ID = GetIntegerAttribute(xmlConnection, "port1ID");
					int port2ID = GetIntegerAttribute(xmlConnection, "port2ID");
					PortData port1, port2;
					if (ports.TryGetValue(port1ID, out port1) && ports.TryGetValue(port2ID, out port2))
						canvasData.RecordConnection(port1, port2);
				}

				// EDITOR STATES

				IEnumerable<XmlElement> xmlEditorStates = xmlCanvas.SelectNodes("EditorStates/EditorState").OfType<XmlElement>();
				List<EditorStateData> editorStates = new List<EditorStateData>();
				foreach (XmlElement xmlEditorState in xmlEditorStates)
				{
					Vector2 pan = GetVectorAttribute(xmlEditorState, "pan");
					float zoom;
					if (!float.TryParse(xmlEditorState.GetAttribute("zoom"), out zoom))
						zoom = 1;
					// Selected Node
					NodeData selectedNode = null;
					int selectedNodeID;
					if (int.TryParse(xmlEditorState.GetAttribute("selected"), out selectedNodeID))
						selectedNode = canvasData.FindNode(selectedNodeID);
					// Create state
					editorStates.Add(new EditorStateData(selectedNode, pan, zoom));
				}
				canvasData.editorStates = editorStates.ToArray();

				return canvasData;
			}
		}

		#region Utility

		private void SerializeObjectToXML(XmlElement parent, object obj)
		{
			XmlSerializer serializer = new XmlSerializer(obj.GetType());
			XPathNavigator navigator = parent.CreateNavigator();
			using (XmlWriter writer = navigator.AppendChild())
				serializer.Serialize(writer, obj);
		}

		private object DeserializeObjectFromXML(XmlElement parent, Type type)
		{
			if (!parent.HasChildNodes)
				return null;
			XmlSerializer serializer = new XmlSerializer(type);
			XPathNavigator navigator = parent.FirstChild.CreateNavigator();
			using (XmlReader reader = navigator.ReadSubtree())
				return serializer.Deserialize(reader);
		}

		private int GetIntegerAttribute(XmlElement element, string attribute, bool throwIfInvalid = true)
		{
			int result = 0;
			if (!int.TryParse(element.GetAttribute(attribute), out result) && throwIfInvalid)
				throw new XmlException("Invalid " + attribute + " for element " + element.Name + "!");
			return result;
		}

		private float GetFloatAttribute(XmlElement element, string attribute, bool throwIfInvalid = true)
		{
			float result = 0;
			if (!float.TryParse(element.GetAttribute(attribute), out result) && throwIfInvalid)
				throw new XmlException("Invalid " + attribute + " for element " + element.Name + "!");
			return result;
		}

		private Vector2 GetVectorAttribute(XmlElement element, string attribute, bool throwIfInvalid = true)
		{
			string[] vecString = element.GetAttribute(attribute).Split(',');
			Vector2 vector = new Vector2(0, 0);
			float vecX, vecY;
			if (vecString.Length == 2 && float.TryParse(vecString[0], out vecX) && float.TryParse(vecString[1], out vecY))
				vector = new Vector2(vecX, vecY);
			return vector;
		}

		#endregion
	}
}