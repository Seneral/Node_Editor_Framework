
using UnityEngine;
using System.Collections.Generic;

namespace NodeEditorFramework
{
#if UNITY_EDITOR
	/// <summary>
	/// Helper class to simplify the anonymous Undo methods and hide the error checking
	/// </summary>
	public static class NodeEditorUndoActions
	{
		/// <summary>
		/// Register and dump any and all NEF-SOs ever used by that canvas into the dump list
		/// Used to keep older SOs alive that might still be used for undo purposes
		/// </summary>
		public static void CompleteSOMemoryDump(NodeCanvas canvas)
		{
			List<ScriptableObject> dump = canvas.SOMemoryDump;
			if (!dump.Contains(canvas)) dump.Add(canvas);
			foreach (NodeEditorState state in canvas.editorStates)
				if (!dump.Contains(state)) dump.Add(state);
			foreach (Node node in canvas.nodes)
			{
				if (!dump.Contains(node)) dump.Add(node);
				foreach (ConnectionPort port in node.connectionPorts)
					if (!dump.Contains(port)) dump.Add(port);
			}
		}

		public static void SetNodeSelection(NodeEditorState state, Node selection)
		{
			if (state == null)
			{ // Not critical. Happens when undo records are triggered, but it's canvas has been unloaded
				// Debug.LogWarning("Lost reference to Undo SO 'state'!");
				return;
			}
			// Workaround for undo records created in playmode that have being deleted (by Unity) after exiting
			if (selection == null || selection.canvas.nodes.Contains(selection))
				state.selectedNode = selection;
		}

		public static void SetNodePosition(Node node, Vector2 position)
		{
			if (node == null)
			{ // Not critical. Happens when undo records are triggered, but it's canvas has been unloaded
				// Debug.LogWarning("Lost reference to Undo SO 'node'!");
				return;
			}
			node.position = position;
		}

		public static void DeleteConnection(ConnectionPort port1, ConnectionPort port2)
		{
			if (port1 == null || port2 == null)
			{ // Not critical. Happens when undo records are triggered, but it's canvas has been unloaded
				// Debug.LogWarning("Lost reference to Undo SO 'port'!");
				return;
			}
			port1.RemoveConnection(port2, true);
		}

		public static void CreateConnection(ConnectionPort port1, ConnectionPort port2)
		{
			if (port1 == null || port2 == null)
			{ // Not critical. Happens when undo records are triggered, but it's canvas has been unloaded
				// Debug.LogWarning("Lost reference to Undo SO 'port'!");
				return;
			}
			// Workaround for undo records created in playmode that have being deleted (by Unity) after exiting
			if (port1.body.canvas.nodes.Contains(port1.body) && port2.body.canvas.nodes.Contains(port2.body))
				port1.TryApplyConnection(port2, true);
		}

		public static void RemoveNode(Node node)
		{
			if (node == null)
			{ // Not critical. Happens when undo records are triggered, but it's canvas has been unloaded
				// Debug.LogWarning("Lost reference to Undo SO 'node'!");
				return;
			}
			node.canvas.nodes.Remove(node);
			node.canvas.nodes.Remove(node);
			for (int i = 0; i < node.connectionPorts.Count; i++)
				node.connectionPorts[i].ClearConnections(true);
		}

		public static void ReinstateNode(Node node, List<ConnectionPort> connectedPorts)
		{
			if (node == null)
			{ // Not critical. Happens when undo records are triggered, but it's canvas has been unloaded
				// Debug.LogWarning("Lost reference to Undo SO 'node'!");
				return;
			}
			if (connectedPorts == null) // Should not happen if node has not been lost
				Debug.LogWarning("Lost reference to Undo SO 'connectedPorts'!");
			node.canvas.nodes.Remove(node);
			node.canvas.nodes.Add(node);
			ConnectionPortManager.UpdateConnectionPorts(node);
			if (connectedPorts != null)
			{
				int portIndex = 0;
				for (int i = 0; i < connectedPorts.Count; i++)
				{
					if (connectedPorts[i] == null)
					{ // 'Decode' null-seperated list
						portIndex++;
						continue;
					}
					if (node.connectionPorts.Count <= portIndex)
					{
						Debug.LogWarning("Misaligned port count in reinstated node!");
						break;
					}
					ConnectionPort port1 = node.connectionPorts[portIndex], port2 = connectedPorts[i];
					// Workaround for undo records created in playmode that have being deleted (by Unity) after exiting
					if (port1.body.canvas.nodes.Contains(port1.body) && port2.body.canvas.nodes.Contains(port2.body))
						port1.TryApplyConnection(port2, true);
				}

				if (node.connectionPorts.Count > portIndex)
					Debug.LogWarning("Misaligned port count in reinstated node!");
			}
		}


	}
#endif
}
