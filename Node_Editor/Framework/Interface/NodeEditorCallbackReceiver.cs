using UnityEngine;
using System;
using System.Collections.Generic;

namespace NodeEditorFramework 
{
	public abstract partial class NodeEditorCallbackReceiver : MonoBehaviour
	{
		// Editor
		public virtual void OnEditorStartUp () {}
		// Save and Load
		public virtual void OnLoadCanvas (NodeCanvas canvas) {}
		public virtual void OnLoadEditorState (NodeEditorState editorState) {}
		public virtual void OnSaveCanvas (NodeCanvas canvas) {}
		public virtual void OnSaveEditorState (NodeEditorState editorState) {}
		// Node
		public virtual void OnAddNode (Node node) {}
		public virtual void OnDeleteNode (Node node) {}
		public virtual void OnMoveNode (Node node) {}
		public virtual void OnAddConnectionPort (ConnectionPort knob) {}
		// Connection
		public virtual void OnAddConnection (ConnectionPort port1, ConnectionPort port2) {}
		public virtual void OnRemoveConnection (ConnectionPort port1, ConnectionPort port2) {}
	}

	public static partial class NodeEditorCallbacks
	{
		private static int receiverCount;
		private static List<NodeEditorCallbackReceiver> callbackReceiver;

		public static void SetupReceivers () 
		{
			callbackReceiver = new List<NodeEditorCallbackReceiver> (MonoBehaviour.FindObjectsOfType<NodeEditorCallbackReceiver> ());
			receiverCount = callbackReceiver.Count;
		}

		#region Editor (1)

		public static Action OnEditorStartUp = null;
		public static void IssueOnEditorStartUp () 
		{
			if (OnEditorStartUp != null)
				OnEditorStartUp.Invoke ();
			for (int cnt = 0; cnt < receiverCount; cnt++) 
			{
				if (callbackReceiver [cnt] == null)
					callbackReceiver.RemoveAt (cnt--);
				else
					callbackReceiver [cnt].OnEditorStartUp ();
			}
		}

		#endregion

		#region Save and Load (4)

		public static Action<NodeCanvas> OnLoadCanvas;
		public static void IssueOnLoadCanvas (NodeCanvas canvas) 
		{
			if (OnLoadCanvas != null)
				OnLoadCanvas.Invoke (canvas);
			for (int cnt = 0; cnt < receiverCount; cnt++) 
			{
				if (callbackReceiver [cnt] == null)
					callbackReceiver.RemoveAt (cnt--);
				else
					callbackReceiver [cnt].OnLoadCanvas (canvas) ;
			}
		}

		public static Action<NodeEditorState> OnLoadEditorState;
		public static void IssueOnLoadEditorState (NodeEditorState editorState) 
		{
			if (OnLoadEditorState != null)
				OnLoadEditorState.Invoke (editorState);
			for (int cnt = 0; cnt < receiverCount; cnt++) 
			{
				if (callbackReceiver [cnt] == null)
					callbackReceiver.RemoveAt (cnt--);
				else
					callbackReceiver [cnt].OnLoadEditorState (editorState) ;
			}
		}

		public static Action<NodeCanvas> OnSaveCanvas;
		public static void IssueOnSaveCanvas (NodeCanvas canvas) 
		{
			if (OnSaveCanvas != null)
				OnSaveCanvas.Invoke (canvas);
			for (int cnt = 0; cnt < receiverCount; cnt++) 
			{
				if (callbackReceiver [cnt] == null)
					callbackReceiver.RemoveAt (cnt--);
				else
					callbackReceiver [cnt].OnSaveCanvas (canvas) ;
			}
		}

		public static Action<NodeEditorState> OnSaveEditorState;
		public static void IssueOnSaveEditorState (NodeEditorState editorState) 
		{
			if (OnSaveEditorState != null)
				OnSaveEditorState.Invoke (editorState);
			for (int cnt = 0; cnt < receiverCount; cnt++) 
			{
				if (callbackReceiver [cnt] == null)
					callbackReceiver.RemoveAt (cnt--);
				else
					callbackReceiver [cnt].OnSaveEditorState (editorState) ;
			}
		}

		#endregion

		#region Node (4)

		public static Action<Node> OnAddNode;
		public static void IssueOnAddNode (Node node) 
		{
			if (OnAddNode != null)
				OnAddNode.Invoke (node);
			for (int cnt = 0; cnt < receiverCount; cnt++) 
			{
				if (callbackReceiver [cnt] == null)
					callbackReceiver.RemoveAt (cnt--);
				else
					callbackReceiver [cnt].OnAddNode (node);
			}
		}

		public static Action<Node> OnDeleteNode;
		public static void IssueOnDeleteNode (Node node) 
		{
			if (OnDeleteNode != null)
				OnDeleteNode.Invoke (node);
			for (int cnt = 0; cnt < receiverCount; cnt++) 
			{
				if (callbackReceiver [cnt] == null)
					callbackReceiver.RemoveAt (cnt--);
				else
					callbackReceiver [cnt].OnDeleteNode (node);
			}
			node.OnDelete ();
		}

		public static Action<Node> OnMoveNode;
		public static void IssueOnMoveNode (Node node) 
		{
			if (OnMoveNode != null)
				OnMoveNode.Invoke (node);
			for (int cnt = 0; cnt < receiverCount; cnt++) 
			{
				if (callbackReceiver [cnt] == null)
					callbackReceiver.RemoveAt (cnt--);
				else
					callbackReceiver [cnt].OnMoveNode (node);
			}
		}

		public static Action<ConnectionPort> OnAddConnectionPort;
		public static void IssueOnAddConnectionPort (ConnectionPort connectionPort) 
		{
			if (OnAddConnectionPort != null)
				OnAddConnectionPort.Invoke (connectionPort);
			for (int cnt = 0; cnt < receiverCount; cnt++) 
			{
				if (callbackReceiver [cnt] == null)
					callbackReceiver.RemoveAt (cnt--);
				else
					callbackReceiver [cnt].OnAddConnectionPort (connectionPort);
			}
		}

		#endregion

		#region Connection (2)

		public static Action<ConnectionPort, ConnectionPort> OnAddConnection;
		public static void IssueOnAddConnection (ConnectionPort port1, ConnectionPort port2) 
		{
			if (OnAddConnection != null)
				OnAddConnection.Invoke (port1, port2);
			for (int cnt = 0; cnt < receiverCount; cnt++) 
			{
				if (callbackReceiver [cnt] == null)
					callbackReceiver.RemoveAt (cnt--);
				else
					callbackReceiver [cnt].OnAddConnection (port1, port2);
			}
		}

		public static Action<ConnectionPort, ConnectionPort> OnRemoveConnection;
		public static void IssueOnRemoveConnection (ConnectionPort port1, ConnectionPort port2)
		{
			if (OnRemoveConnection != null)
				OnRemoveConnection.Invoke (port1, port2);
			for (int cnt = 0; cnt < receiverCount; cnt++) 
			{
				if (callbackReceiver [cnt] == null)
					callbackReceiver.RemoveAt (cnt--);
				else
					callbackReceiver [cnt].OnRemoveConnection (port1, port2);
			}
		}

		#endregion

	}
}