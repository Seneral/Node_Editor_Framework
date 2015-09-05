using UnityEngine;
using System;
using System.Collections.Generic;
using NodeEditorFramework;

namespace NodeEditorFramework 
{
	public abstract class NodeEditorCallbackReceiver : MonoBehaviour
	{
		public virtual void OnEditorStartUp () {}

		public virtual void OnLoadCanvas (NodeCanvas canvas) {}
		public virtual void OnLoadEditorState (NodeEditorState editorState) {}
		public virtual void OnSaveCanvas (NodeCanvas canvas) {}
		public virtual void OnSaveEditorState (NodeEditorState editorState) {}
		
		public virtual void OnAddNode (Node node) {}
		public virtual void OnDeleteNode (Node node) {}
		public virtual void OnMoveNode (Node node) {}
		
		public virtual void OnAddConnection (NodeInput input) {}
		public virtual void OnRemoveConnection (NodeInput input) {}
	}

	public static class NodeEditorCallbacks
	{
		private static int receiverCount;
		private static List<NodeEditorCallbackReceiver> callbackReceiver;

		public static void SetupReceivers () 
		{
			callbackReceiver = new List<NodeEditorCallbackReceiver> (MonoBehaviour.FindObjectsOfType<NodeEditorCallbackReceiver> ());
			receiverCount = callbackReceiver.Count;
		}

		// EDITOR
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

		// CANVAS
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

		// NODE
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
				{
					callbackReceiver [cnt].OnDeleteNode (node);
					node.OnDelete ();
				}
			}
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

		// CONNECTION
		public static Action<NodeInput> OnAddConnection;
		public static void IssueOnAddConnection (NodeInput input) 
		{
			if (OnAddConnection != null)
				OnAddConnection.Invoke (input);
			for (int cnt = 0; cnt < receiverCount; cnt++) 
			{
				if (callbackReceiver [cnt] == null)
					callbackReceiver.RemoveAt (cnt--);
				else
					callbackReceiver [cnt].OnAddConnection (input);
			}
		}
		public static Action<NodeInput> OnRemoveConnection;
		public static void IssueOnRemoveConnection (NodeInput input) 
		{
			if (OnRemoveConnection != null)
				OnRemoveConnection.Invoke (input);
			for (int cnt = 0; cnt < receiverCount; cnt++) 
			{
				if (callbackReceiver [cnt] == null)
					callbackReceiver.RemoveAt (cnt--);
				else
					callbackReceiver [cnt].OnRemoveConnection (input);
			}
		}
	}
}