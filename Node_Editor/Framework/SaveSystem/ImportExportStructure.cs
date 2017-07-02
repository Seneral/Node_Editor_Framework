using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace NodeEditorFramework.IO
{
	public class CanvasData
	{
		public NodeCanvas canvas;

		public string name;
		public Type type;

		public EditorStateData[] editorStates;

		public Dictionary<int, NodeData> nodes = new Dictionary<int, NodeData>();
		public List<ConnectionData> connections = new List<ConnectionData>();
		public Dictionary<int, ObjectData> objects = new Dictionary<int, ObjectData>();

		public CanvasData(NodeCanvas nodeCanvas)
		{
			canvas = nodeCanvas;
			name = nodeCanvas.name;
			type = nodeCanvas.GetType();
		}

		public CanvasData(Type canvasType, string canvasName)
		{
			type = canvasType;
			name = canvasName;
		}

		public ObjectData ReferenceObject(object obj)
		{
			foreach (ObjectData data in objects.Values)
			{
				if (data.data == obj)
					return data;
			}
			ObjectData objData = new ObjectData(obj);
			objects.Add(objData.refID, objData);
			return objData;
		}

		public ObjectData FindObject(int refID)
		{
			ObjectData data;
			objects.TryGetValue(refID, out data);
			return data;
		}

		public NodeData FindNode(Node node)
		{
			foreach (NodeData data in nodes.Values)
			{
				if (data.node == node)
					return data;
			}
			return null;
		}

		public NodeData FindNode(int nodeID)
		{
			NodeData data;
			nodes.TryGetValue(nodeID, out data);
			return data;
		}

		public bool RecordConnection(PortData portData1, PortData portData2)
		{
			if (!portData1.connections.Contains(portData2))
				portData1.connections.Add(portData2);
			if (!portData2.connections.Contains(portData1))
				portData2.connections.Add(portData1);
			if (!connections.Exists((ConnectionData conData) => conData.isPart(portData1) && conData.isPart(portData2)))
			{ // Connection hasn't already been recorded
				ConnectionData conData = new ConnectionData(portData1, portData2);
				connections.Add(conData);
				return true;
			}
			return false;
		}
	}

	public class EditorStateData
	{
		public NodeData selectedNode;
		public Vector2 panOffset;
		public float zoom;

		public EditorStateData(NodeData SelectedNode, Vector2 PanOffset, float Zoom)
		{
			selectedNode = SelectedNode;
			panOffset = PanOffset;
			zoom = Zoom;
		}
	}

	public class NodeData
	{
		public Node node;

		public int nodeID;
		public string typeID;
		public Vector2 nodePos;
		
		public List<PortData> connectionPorts = new List<PortData>();
		public List<VariableData> variables = new List<VariableData>();

		public NodeData(Node n)
		{
			node = n;
			typeID = node.GetID;
			nodeID = node.GetHashCode();
			nodePos = node.rect.position;
		}

		public NodeData(string TypeID, int NodeID, Vector2 Pos)
		{
			typeID = TypeID;
			nodeID = NodeID;
			nodePos = Pos;
		}
	}

	public class PortData
	{
		public ConnectionPort port;

		public int portID;
		public NodeData body;
		public string varName;

		public List<PortData> connections = new List<PortData>();

		public PortData(NodeData Body, ConnectionPort Port, string VarName)
		{
			port = Port;
			portID = port.GetHashCode();
			body = Body;
			varName = VarName;
		}

		public PortData(NodeData Body, string VarName, int PortID)
		{
			portID = PortID;
			body = Body;
			varName = VarName;
		}
	}

	public class ConnectionData
	{
		public PortData port1;
		public PortData port2;

		public ConnectionData(PortData Port1, PortData Port2)
		{
			port1 = Port1;
			port2 = Port2;
		}

		public bool isPart (PortData port)
		{
			return port1 == port || port2 == port;
		}
	}

	public class VariableData
	{
		public string name;
		public ObjectData refObject;
		public object value;

		public VariableData(FieldInfo field)
		{
			name = field.Name;
		}

		public VariableData(string fieldName)
		{
			name = fieldName;
		}
	}

	public class ObjectData
	{
		public int refID;
		public Type type;
		public object data;

		public ObjectData(object obj)
		{
			refID = obj.GetHashCode();
			type = obj.GetType();
			data = obj;
		}

		public ObjectData(int objRefID, object obj)
		{
			refID = objRefID;
			type = obj.GetType();
			data = obj;
		}
	}
}