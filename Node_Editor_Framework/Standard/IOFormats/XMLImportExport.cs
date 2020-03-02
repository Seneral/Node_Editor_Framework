using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Reflection;
using UnityEngine;

namespace NodeEditorFramework.IO
{
	public class XMLImportExport : StructuredImportExportFormat
	{
		public override string FormatIdentifier { get { return "XML"; } }
		public override string FormatExtension { get { return "xml"; } }

		public override void ExportData(CanvasData data, params object[] args)
		{
			if (args == null || args.Length != 1 || args[0].GetType() != typeof(string))
				throw new ArgumentException("Location Arguments");
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

			// GROUPS

			XmlElement groups = saveDoc.CreateElement("Groups");
			canvas.AppendChild(groups);
			foreach (GroupData groupData in data.groups)
			{
				XmlElement group = saveDoc.CreateElement("Group");
				group.SetAttribute("name", groupData.name);
				group.SetAttribute("rect", groupData.rect.x + "," + groupData.rect.y + "," + groupData.rect.width + "," + groupData.rect.height);
				group.SetAttribute("color", groupData.color.r + "," + groupData.color.g + "," + groupData.color.b + "," + groupData.color.a);
				groups.AppendChild(group);
			}

			// NODES

			XmlElement nodes = saveDoc.CreateElement("Nodes");
			canvas.AppendChild(nodes);
			foreach (NodeData nodeData in data.nodes.Values)
			{
				XmlElement node = saveDoc.CreateElement("Node");
				node.SetAttribute("name", nodeData.name);
				node.SetAttribute("ID", nodeData.nodeID.ToString());
				node.SetAttribute("type", nodeData.typeID);
				node.SetAttribute("pos", nodeData.nodePos.x + "," + nodeData.nodePos.y);
				nodes.AppendChild(node);

				// NODE PORTS

				foreach (PortData portData in nodeData.connectionPorts)
				{
					XmlElement port = saveDoc.CreateElement("Port");
					port.SetAttribute("ID", portData.portID.ToString());
					port.SetAttribute("name", portData.name);	
					port.SetAttribute("dynamic", portData.dynamic.ToString());
					if (portData.dynamic)
					{ // Serialize dynamic port
						port.SetAttribute("type", portData.dynaType.AssemblyQualifiedName);
						foreach (string fieldName in portData.port.AdditionalDynamicKnobData())
							SerializeFieldToXML(port, portData.port, fieldName); // Serialize all dynamic knob variables
					}
					node.AppendChild(port);
				}

				// NODE VARIABLES

				foreach (VariableData varData in nodeData.variables)
				{ // Serialize all node variables
					if (varData.refObject != null)
					{ // Serialize reference-type variables as 'Variable' element
						XmlElement variable = saveDoc.CreateElement("Variable");
						variable.SetAttribute("name", varData.name);
						variable.SetAttribute("refID", varData.refObject.refID.ToString());
						node.AppendChild(variable);
					}
					else // Serialize value-type fields in-line
					{
						XmlElement serializedValue = SerializeObjectToXML(node, varData.value);
						serializedValue.SetAttribute("name", varData.name);
					}
				}
			}

			// CONNECTIONS

			XmlElement connections = saveDoc.CreateElement("Connections");
			canvas.AppendChild(connections);
			foreach (ConnectionData connectionData in data.connections)
			{
				XmlElement connection = saveDoc.CreateElement("Connection");
				connection.SetAttribute("port1ID", connectionData.port1.portID.ToString());
				connection.SetAttribute("port2ID", connectionData.port2.portID.ToString());
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

			// WRITE

			Directory.CreateDirectory(Path.GetDirectoryName(path));
			using (XmlTextWriter writer = new XmlTextWriter(path, Encoding.UTF8))
			{
				writer.Formatting = Formatting.Indented;
				writer.Indentation = 1;
				writer.IndentChar = '\t';
				saveDoc.Save(writer);
			}
		}

		public override CanvasData ImportData(params object[] args)
		{
			if (args == null || args.Length != 1 || args[0].GetType() != typeof(string))
				throw new ArgumentException("Location Arguments");
			string path = (string)args[0];

			using (FileStream fs = new FileStream(path, FileMode.Open))
			{
				XmlDocument data = new XmlDocument();
				data.Load(fs);

				// CANVAS

				string canvasName = Path.GetFileNameWithoutExtension(path);
				XmlElement xmlCanvas = (XmlElement)data.SelectSingleNode("//NodeCanvas");
				Type canvasType = NodeCanvasManager.GetCanvasTypeData(xmlCanvas.GetAttribute("type")).CanvasType;
				if (canvasType == null)
					throw new XmlException("Could not find NodeCanvas of type '" + xmlCanvas.GetAttribute("type") + "'!");
				CanvasData canvasData = new CanvasData(canvasType, canvasName);
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
					string name = xmlNode.GetAttribute("name");
					int nodeID = GetIntegerAttribute(xmlNode, "ID");
					string typeID = xmlNode.GetAttribute("type");
					Vector2 nodePos = GetVectorAttribute(xmlNode, "pos");
					// Record
					NodeData node = new NodeData(name, typeID, nodeID, nodePos);
					canvasData.nodes.Add(nodeID, node);

					// NODE PORTS

					IEnumerable<XmlElement> xmlConnectionPorts = xmlNode.SelectNodes("Port").OfType<XmlElement>();
					foreach (XmlElement xmlPort in xmlConnectionPorts)
					{
						int portID = GetIntegerAttribute(xmlPort, "ID");
						string portName = xmlPort.GetAttribute("name");
						if (string.IsNullOrEmpty(portName)) // Fallback to old save
							portName = xmlPort.GetAttribute("varName");
						bool dynamic = GetBooleanAttribute(xmlPort, "dynamic", false);
						PortData portData;
						if (!dynamic) // Record static port
							portData = new PortData(node, portName, portID);
						else
						{ // Deserialize dynamic port
							string typeName = xmlPort.GetAttribute("type");
							Type portType = Type.GetType(typeName, true);
							if (portType != typeof(ConnectionPort) && !portType.IsSubclassOf(typeof(ConnectionPort)))
								continue; // Invalid type stored
							ConnectionPort port = (ConnectionPort)ScriptableObject.CreateInstance(portType);
							port.name = portName;
							foreach (XmlElement portVar in xmlPort.ChildNodes.OfType<XmlElement>())
								DeserializeFieldFromXML(portVar, port);
							portData = new PortData(node, port, portID);
						}
						node.connectionPorts.Add(portData);
						ports.Add(portID, portData);
					}

					// NODE VARIABLES
					
					foreach (XmlElement variable in xmlNode.ChildNodes.OfType<XmlElement>())
					{ // Deserialize all value-type variables
						if (variable.Name != "Variable" && variable.Name != "Port")
						{
							string varName = variable.GetAttribute("name");
							object varValue = DeserializeFieldFromXML(variable, node.type, null);
							VariableData varData = new VariableData(varName);
							varData.value = varValue;
							node.variables.Add(varData);
						}
					}

					IEnumerable<XmlElement> xmlVariables = xmlNode.SelectNodes("Variable").OfType<XmlElement>();
					foreach (XmlElement xmlVariable in xmlVariables)
					{ // Deserialize all reference-type variables (and also value type variables in old save files)
						string varName = xmlVariable.GetAttribute("name");
						VariableData varData = new VariableData(varName);
						if (xmlVariable.HasAttribute("refID"))
						{ // Read reference-type variables from objects
							int refID = GetIntegerAttribute(xmlVariable, "refID");
							ObjectData objData;
							if (canvasData.objects.TryGetValue(refID, out objData))
								varData.refObject = objData;
						}
						else
						{ // Read value-type variable (old save file only) TODO: Remove
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

				// GROUPS

				IEnumerable<XmlElement> xmlGroups = xmlCanvas.SelectNodes("Groups/Group").OfType<XmlElement>();
				foreach (XmlElement xmlGroup in xmlGroups)
				{
					string name = xmlGroup.GetAttribute("name");
					Rect rect = GetRectAttribute(xmlGroup, "rect");
					Color color = GetColorAttribute(xmlGroup, "color");
					canvasData.groups.Add(new GroupData(name, rect, color));
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

		private XmlElement SerializeFieldToXML(XmlElement parent, object obj, string fieldName)
		{
			Type type = obj.GetType();
			FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance| BindingFlags.FlattenHierarchy);
			if (field == null)
			{
				Debug.LogWarning("Failed to find field " + fieldName + " on type " + type.Name);
				return null;
			}
			object fieldValue = field.GetValue(obj);
			XmlElement serializedValue = SerializeObjectToXML(parent, fieldValue);
			if (serializedValue != null)
			{
				serializedValue.SetAttribute("name", fieldName);
				return serializedValue;
			}
			return null;
		}

		private object DeserializeFieldFromXML(XmlElement xmlElement, object obj)
		{
			Type type = obj.GetType();
			return DeserializeFieldFromXML(xmlElement, type, obj);
		}

		private object DeserializeFieldFromXML(XmlElement xmlElement, Type type, object obj = null)
		{
			string fieldName = xmlElement.GetAttribute("name");
			FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
			if (field == null)
			{
				Debug.LogWarning("Failed to find field " + fieldName + " on type " + type.Name);
				return null;
			}
			object fieldValue = DeserializeObjectFromXML(xmlElement, field.FieldType, false);
			if (obj != null)
				field.SetValue(obj, fieldValue);
			return fieldValue;
		}

		private XmlElement SerializeObjectToXML(XmlElement parent, object obj)
		{
			// TODO: Need to handle asset references
			// Because of runtime compability, always try to embed objects
			// If that fails, try to find references to assets (e.g. for textures)
			try
			{ // Try to embed object
				XmlSerializer serializer = new XmlSerializer(obj.GetType());
				XPathNavigator navigator = parent.CreateNavigator();
				using (XmlWriter writer = navigator.AppendChild())
				{ // Workaround: XMLSerializer now always calls WriteStartDocument when writer is unused - even though it's a fragment - and will throw an error...
					writer.WriteWhitespace("");
					serializer.Serialize(writer, obj);
				}
				return (XmlElement)parent.LastChild;
			}
			catch (Exception)
			{
				Debug.Log("Could not serialize " + obj.ToString());
				return null;
			}
		}

		private object DeserializeObjectFromXML(XmlElement xmlElement, Type type, bool isParent = true)
		{
			if (isParent && !xmlElement.HasChildNodes)
				return null;
			XmlSerializer serializer = new XmlSerializer(type);
			XPathNavigator navigator = (isParent ? xmlElement.FirstChild : xmlElement).CreateNavigator();
			using (XmlReader reader = navigator.ReadSubtree())
				return serializer.Deserialize(reader);
		}

		public delegate bool TryParseHandler<T>(string value, out T result);
		private T GetAttribute<T>(XmlElement element, string attribute, TryParseHandler<T> handler, T defaultValue)
		{
			T result;
			if (handler(element.GetAttribute(attribute), out result))
				return result;
			return defaultValue;
		}
		private T GetAttribute<T>(XmlElement element, string attribute, TryParseHandler<T> handler)
		{
			T result;
			if (!handler(element.GetAttribute(attribute), out result))
				throw new XmlException("Invalid " + typeof(T).Name + " " + attribute + " for element " + element.Name + "!");
			return result;
		}

		private int GetIntegerAttribute(XmlElement element, string attribute, bool throwIfInvalid = true)
		{
			int result = 0;
			if (!int.TryParse(element.GetAttribute(attribute), out result) && throwIfInvalid)
				throw new XmlException("Invalid Int " + attribute + " for element " + element.Name + "!");
			return result;
		}

		private float GetFloatAttribute(XmlElement element, string attribute, bool throwIfInvalid = true)
		{
			float result = 0;
			if (!float.TryParse(element.GetAttribute(attribute), out result) && throwIfInvalid)
				throw new XmlException("Invalid Float " + attribute + " for element " + element.Name + "!");
			return result;
		}

		private bool GetBooleanAttribute(XmlElement element, string attribute, bool throwIfInvalid = true)
		{
			bool result = false;
			if (!bool.TryParse(element.GetAttribute(attribute), out result) && throwIfInvalid)
				throw new XmlException("Invalid Bool " + attribute + " for element " + element.Name + "!");
			return result;
		}

		private Vector2 GetVectorAttribute(XmlElement element, string attribute, bool throwIfInvalid = false)
		{
			string[] vecString = element.GetAttribute(attribute).Split(',');
			Vector2 vector = new Vector2(0, 0);
			float vecX, vecY;
			if (vecString.Length == 2 && float.TryParse(vecString[0], out vecX) && float.TryParse(vecString[1], out vecY))
				vector = new Vector2(vecX, vecY);
			else if (throwIfInvalid)
				throw new XmlException("Invalid Vector2 " + attribute + " for element " + element.Name + "!");
			return vector;
		}

		private Color GetColorAttribute(XmlElement element, string attribute, bool throwIfInvalid = false)
		{
			string[] vecString = element.GetAttribute(attribute).Split(',');
			Color color = Color.white;
			float colR, colG, colB, colA;
			if (vecString.Length == 4 && float.TryParse(vecString[0], out colR) && float.TryParse(vecString[1], out colG) && float.TryParse(vecString[2], out colB) && float.TryParse(vecString[3], out colA))
				color = new Color(colR, colG, colB, colA);
			else if (throwIfInvalid)
				throw new XmlException("Invalid Color " + attribute + " for element " + element.Name + "!");
			return color;
		}

		private Rect GetRectAttribute(XmlElement element, string attribute, bool throwIfInvalid = false)
		{
			string[] vecString = element.GetAttribute(attribute).Split(',');
			Rect rect = new Rect(0, 0, 100, 100);
			float x, y, w, h;
			if (vecString.Length == 4 && float.TryParse(vecString[0], out x) && float.TryParse(vecString[1], out y) && float.TryParse(vecString[2], out w) && float.TryParse(vecString[3], out h))
				rect = new Rect(x, y, w, h);
			else if (throwIfInvalid)
				throw new XmlException("Invalid Rect " + attribute + " for element " + element.Name + "!");
			return rect;
		}

		#endregion
	}
}