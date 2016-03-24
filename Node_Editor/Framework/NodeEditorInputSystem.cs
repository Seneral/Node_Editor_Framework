using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

using NodeEditorFramework.Utilities;

namespace NodeEditorFramework 
{
	/// <summary>
	/// The NodeEditor Input System handles dynamic input controls. 
	/// Use the four attributes on functions to make the system recognize them  as event handlers
	/// Default Controls can be found in NodeEditorInputControls
	/// </summary>
	public static class NodeEditorInputSystem
	{
		#region Setup and Fetching

		// NOTE: Using Lists of KeyValuePair as we 1. need it ordered in the first two cases and 2. we do not need extras from Dictionary anyway
		private static List<KeyValuePair<EventHandlerAttribute, Delegate>> eventHandlers;
		private static List<KeyValuePair<HotkeyAttribute, Delegate>> hotkeyHandlers;
		private static List<KeyValuePair<ContextEntryAttribute, PopupMenu.MenuFunctionData>> contextEntries;
		private static List<KeyValuePair<ContextFillerAttribute, Delegate>> contextFillers;

		/// <summary>
		/// Fetches all event handlers
		/// </summary>
		public static void SetupInput () 
		{
			eventHandlers = new List<KeyValuePair<EventHandlerAttribute, Delegate>> ();
			hotkeyHandlers = new List<KeyValuePair<HotkeyAttribute, Delegate>> ();
			contextEntries = new List<KeyValuePair<ContextEntryAttribute, PopupMenu.MenuFunctionData>> ();
			contextFillers = new List<KeyValuePair<ContextFillerAttribute, Delegate>> ();

			// Iterate through each static method
			IEnumerable<Assembly> scriptAssemblies = AppDomain.CurrentDomain.GetAssemblies ().Where ((Assembly assembly) => assembly.FullName.Contains ("Assembly"));
			foreach (Assembly assembly in scriptAssemblies) 
			{
				foreach (Type type in assembly.GetTypes ()) 
				{
					foreach (MethodInfo method in type.GetMethods (BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)) 
					{
						#region Event-Attributes recognition and storing

						// Check the method's attributes for input handler definitions
						Delegate actionDelegate = null;
						foreach (object attr in method.GetCustomAttributes (true))
						{
							Type attrType = attr.GetType ();
							if (attrType == typeof(EventHandlerAttribute))
							{ // Method is an eventHandler
								if (EventHandlerAttribute.AssureValidity (method, attr as EventHandlerAttribute)) 
								{ // Method signature is correct, so we register this handler
									if (actionDelegate == null) actionDelegate = Delegate.CreateDelegate (typeof(Action<NodeEditorInputInfo>), method);
									eventHandlers.Add (new KeyValuePair<EventHandlerAttribute, Delegate> (attr as EventHandlerAttribute, actionDelegate));
								}
							}
							else if (attrType == typeof(HotkeyAttribute))
							{ // Method is an hotkeyHandler
								if (HotkeyAttribute.AssureValidity (method, attr as HotkeyAttribute)) 
								{ // Method signature is correct, so we register this handler
									if (actionDelegate == null) actionDelegate = Delegate.CreateDelegate (typeof(Action<NodeEditorInputInfo>), method);
									hotkeyHandlers.Add (new KeyValuePair<HotkeyAttribute, Delegate> (attr as HotkeyAttribute, actionDelegate));
								}
							}
							else if (attrType == typeof(ContextEntryAttribute))
							{ // Method is an contextEntry
								if (ContextEntryAttribute.AssureValidity (method, attr as ContextEntryAttribute)) 
								{ // Method signature is correct, so we register this handler
									if (actionDelegate == null) actionDelegate = Delegate.CreateDelegate (typeof(Action<NodeEditorInputInfo>), method);
									// Create a proper MenuFunction as a wrapper for the delegate that converts the object to NodeEditorInputInfo
									PopupMenu.MenuFunctionData menuFunction = (object callbackObj) => 
									{
										if (!(callbackObj is NodeEditorInputInfo))
											throw new UnityException ("Callback Object passed by context is not of type NodeEditorMenuCallback!");
										actionDelegate.DynamicInvoke (callbackObj as NodeEditorInputInfo);
									};
									contextEntries.Add (new KeyValuePair<ContextEntryAttribute, PopupMenu.MenuFunctionData> (attr as ContextEntryAttribute, menuFunction));
								}
							}
							else if (attrType == typeof(ContextFillerAttribute))
							{ // Method is an contextFiller
								if (ContextFillerAttribute.AssureValidity (method, attr as ContextFillerAttribute)) 
								{ // Method signature is correct, so we register this handler
									Delegate methodDel = Delegate.CreateDelegate (typeof(Action<NodeEditorInputInfo, GenericMenu>), method);
									contextFillers.Add (new KeyValuePair<ContextFillerAttribute, Delegate> (attr as ContextFillerAttribute, methodDel));
								}
							}
						}

						#endregion
					}
				}
			}

			eventHandlers.Sort ((handlerA, handlerB) => handlerA.Key.priority.CompareTo (handlerB.Key.priority));
			hotkeyHandlers.Sort ((handlerA, handlerB) => handlerA.Key.priority.CompareTo (handlerB.Key.priority));
		}

		#endregion

		#region Invoking Dynamic Input Handlers

		/// <summary>
		/// Calls the eventHandlers for either late or early input (pre- or post GUI) with the inputInfo
		/// </summary>
		private static void CallEventHandlers (NodeEditorInputInfo inputInfo, bool late) 
		{
			object[] parameter = new object[] { inputInfo };
			foreach (KeyValuePair<EventHandlerAttribute, Delegate> eventHandler in eventHandlers)
			{
				if ((eventHandler.Key.handledEvent == null || eventHandler.Key.handledEvent == inputInfo.inputEvent.type) &&
					(late? eventHandler.Key.priority >= 100 : eventHandler.Key.priority < 100))
				{ // Event is happening and specified priority is ok with the late-state
					eventHandler.Value.DynamicInvoke (parameter);
				}
			}
		}

		/// <summary>
		/// Calls the hotkeys that match the keyCode and mods with the inputInfo
		/// </summary>
		private static void CallHotkeys (NodeEditorInputInfo inputInfo, KeyCode keyCode, EventModifiers mods) 
		{
			object[] parameter = new object[] { inputInfo };
			foreach (KeyValuePair<HotkeyAttribute, Delegate> hotKey in hotkeyHandlers)
			{
				if (hotKey.Key.handledHotKey == keyCode && 
					(hotKey.Key.modifiers == null || hotKey.Key.modifiers == mods) && 
					(hotKey.Key.limitingEventType == null || hotKey.Key.limitingEventType == inputInfo.inputEvent.type))
				{
					hotKey.Value.DynamicInvoke (parameter);
				}
			}
		}

		/// <summary>
		/// Fills the contextMenu of the specified contextType with the inputInfo
		/// </summary>
		private static void FillContextMenu (NodeEditorInputInfo inputInfo, GenericMenu contextMenu, ContextType contextType) 
		{
			foreach (KeyValuePair<ContextEntryAttribute, PopupMenu.MenuFunctionData> contextEntry in contextEntries)
			{ // Add all registered menu entries for the specified type to the contextMenu
				if (contextEntry.Key.contextType == contextType)
					contextMenu.AddItem (new GUIContent (contextEntry.Key.contextPath), false, contextEntry.Value, inputInfo);
			}

			object[] fillerParams = new object[] { inputInfo, contextMenu };
			foreach (KeyValuePair<ContextFillerAttribute, Delegate> contextFiller in contextFillers)
			{ // Let all registered menu fillers for the specified type add their entries to the contextMenu
				if (contextFiller.Key.contextType == contextType)
					contextFiller.Value.DynamicInvoke (fillerParams);
			}
		}

		#endregion

		#region Event Handling

		/// <summary>
		/// Processes pre-GUI input events using dynamic input handlers
		/// </summary>
		public static void HandleInputEvents (NodeEditorState state)
		{
			if (shouldIgnoreInput (state))
				return;

			// Call input and hotkey handlers
			NodeEditorInputInfo inputInfo = new NodeEditorInputInfo (state);
			CallEventHandlers (inputInfo, false);
			CallHotkeys (inputInfo, Event.current.keyCode, Event.current.modifiers);


		}

		/// <summary>
		/// Processes late post-GUI input events using dynamic input handlers
		/// </summary>
		public static void HandleLateInputEvents (NodeEditorState state) 
		{
			if (shouldIgnoreInput (state))
				return;
			// Call late input handlers
			NodeEditorInputInfo inputInfo = new NodeEditorInputInfo (state);
			CallEventHandlers (inputInfo, true);
		}

		/// <summary>
		/// Returns whether to account for input in the given state using the mousePosition
		/// </summary>
		internal static bool shouldIgnoreInput (NodeEditorState state) 
		{
			// Account for any opened popups
			if (OverlayGUI.HasPopupControl ())
				return true;
			// Check if mouse is outside of canvas rect
			if (!state.canvasRect.Contains (Event.current.mousePosition))
				return true;
			// Check if mouse is inside an ignoreInput rect
			for (int ignoreCnt = 0; ignoreCnt < state.ignoreInput.Count; ignoreCnt++) 
			{
				if (state.ignoreInput [ignoreCnt].Contains (Event.current.mousePosition)) 
					return true;
			}
			return false;
		}

		#endregion

		#region Essential Controls
		// Contains only the most essential controls, rest is found in NodeEditorInputControls

		// NODE SELECTION

		private static NodeEditorState unfocusControlsForState;

		[EventHandlerAttribute (priority = -4)] // Absolute first to call!
		private static void HandleFocussing (NodeEditorInputInfo inputInfo) 
		{
			NodeEditorState state = inputInfo.editorState;
			// Choose focused Node
			state.focusedNode = NodeEditor.NodeAtPosition (NodeEditor.ScreenToCanvasSpace (inputInfo.inputPos), out state.focusedNodeKnob);
			// Perform focus changes in Repaint, which is the only suitable time to do this
			if (unfocusControlsForState == state && Event.current.type == EventType.Repaint) 
			{
				GUIUtility.hotControl = 0;
				GUIUtility.keyboardControl = 0;
				unfocusControlsForState = null;
			}
		}

		[EventHandlerAttribute (EventType.MouseDown, priority = -2)] // Absolute second to call!
		private static void HandleSelecting (NodeEditorInputInfo inputInfo) 
		{
			NodeEditorState state = inputInfo.editorState;
			if (inputInfo.inputEvent.button == 0 && state.focusedNode != state.selectedNode)
			{ // Select focussed Node
				unfocusControlsForState = state;
				state.selectedNode = state.focusedNode;
				NodeEditor.RepaintClients ();
			#if UNITY_EDITOR
				if (state.selectedNode != null)
					UnityEditor.Selection.activeObject = state.selectedNode;
			#endif
			}
		}

		// CONTEXT CLICKS

		[EventHandlerAttribute (EventType.MouseDown, priority = 0)] // One of the highest priorities after node selection
		private static void HandleContextClicks (NodeEditorInputInfo inputInfo) 
		{
			if (Event.current.button == 1) 
			{ // Handle context clicks on Node and canvas
				GenericMenu contextMenu = new GenericMenu ();
				if (inputInfo.editorState.focusedNode != null) // Node Context Click
					FillContextMenu (inputInfo, contextMenu, ContextType.Node);
				else // Editor Context Click
					FillContextMenu (inputInfo, contextMenu, ContextType.Canvas);
				contextMenu.Show (inputInfo.inputPos);
				Event.current.Use ();
			}
		}

		#endregion
	}

	/// <summary>
	/// Class that representates an input to handle containing all avaible data
	/// </summary>
	public class NodeEditorInputInfo
	{
		public string message;
		public NodeEditorState editorState;
		public Event inputEvent;
		public Vector2 inputPos;

		public NodeEditorInputInfo (NodeEditorState EditorState) 
		{
			message = null;
			editorState = EditorState;
			inputEvent = Event.current;
			inputPos = inputEvent.mousePosition;
		}

		public NodeEditorInputInfo (string Message, NodeEditorState EditorState) 
		{
			message = Message;
			editorState = EditorState;
			inputEvent = Event.current;
			inputPos = inputEvent.mousePosition;
		}

		/// <summary>
		/// Sets both curEditorState and curNodeCanvas to these of the environment this input originates from
		/// </summary>
		public void SetAsCurrentEnvironment () 
		{
			NodeEditor.curEditorState = editorState;
			NodeEditor.curNodeCanvas = editorState.canvas;
		}
	}

	#region Event Attributes

	/// <summary>
	/// The EventHandlerAttribute is used to handle arbitrary events for the Node Editor.
	/// 'priority' can additionally be specified. A priority over or equals hundred will be called AFTER the GUI
	/// Method Signature must be [ Return: Void; Params: NodeEditorInputInfo, EventType ]
	/// </summary>
	[AttributeUsage (AttributeTargets.Method, AllowMultiple = true)]
	public class EventHandlerAttribute : Attribute 
	{
		public EventType? handledEvent { get; private set; }
		public int priority { get; private set; }

		/// <summary>
		/// Handle all events of the specified eventType
		/// </summary>
		public EventHandlerAttribute (EventType eventType) 
		{
			handledEvent = eventType;
			priority = 50;
		}

		/// <summary>
		/// Handle all EventTypes
		/// </summary>
		public EventHandlerAttribute () 
		{
			handledEvent = null;
		}

		internal static bool AssureValidity (MethodInfo method, EventHandlerAttribute attr) 
		{
			if (!method.IsGenericMethod && !method.IsGenericMethodDefinition && (method.ReturnType == null || method.ReturnType == typeof(void)))
			{ // Check if the method has the correct signature
				ParameterInfo[] methodParams = method.GetParameters ();
				if (methodParams.Length == 1 && methodParams[0].ParameterType == typeof(NodeEditorInputInfo))
					return true;
				else
					Debug.LogWarning ("Method " + method.Name + " has incorrect signature for EventHandlerAttribute!");
			}
			return false;
		}
	}

	/// <summary>
	/// The HotkeyAttribute is used to provide hotkeys for the Node Editor.
	/// 'priority' can additionally be specified. A priority over or equals hundred will be called AFTER the GUI
	/// Method Signature must be [ Return: Void; Params: NodeEditorInputInfo ]
	/// </summary>
	[AttributeUsage (AttributeTargets.Method, AllowMultiple = true)]
	public class HotkeyAttribute : Attribute 
	{
		public KeyCode handledHotKey { get; private set; }
		public EventModifiers? modifiers { get; private set; }
		public EventType? limitingEventType { get; private set; }
		public int priority { get; private set; }

		/// <summary>
		/// Handle the specified hotkey
		/// </summary>
		public HotkeyAttribute (KeyCode handledKey) 
		{
			handledHotKey = handledKey;
			modifiers = null;
			limitingEventType = null;
			priority = 50;
		}	

		/// <summary>
		/// Handle the specified hotkey with modifiers
		/// </summary>
		public HotkeyAttribute (KeyCode handledKey, EventModifiers eventModifiers) 
		{
			handledHotKey = handledKey;
			modifiers = eventModifiers;
			limitingEventType = null;
			priority = 50;
		}

		/// <summary>
		/// Handle the specified hotkey limited to the specified eventType
		/// </summary>
		public HotkeyAttribute (KeyCode handledKey, EventType LimitEventType) 
		{
			handledHotKey = handledKey;
			modifiers = null;
			limitingEventType = LimitEventType;
			priority = 50;
		}

		/// <summary>
		/// Handle the specified hotkey with modifiers limited to the specified eventType
		/// </summary>
		public HotkeyAttribute (KeyCode handledKey, EventModifiers eventModifiers, EventType LimitEventType) 
		{
			handledHotKey = handledKey;
			modifiers = eventModifiers;
			limitingEventType = LimitEventType;
			priority = 50;
		}

		internal static bool AssureValidity (MethodInfo method, HotkeyAttribute attr) 
		{
			if (!method.IsGenericMethod && !method.IsGenericMethodDefinition && (method.ReturnType == null || method.ReturnType == typeof(void)))
			{ // Check if the method has the correct signature
				ParameterInfo[] methodParams = method.GetParameters ();
				if (methodParams.Length == 1 && methodParams[0].ParameterType.IsAssignableFrom (typeof(NodeEditorInputInfo)))
					return true;
				else
					Debug.LogWarning ("Method " + method.Name + " has incorrect signature for HotkeyAttribute!");
			}
			return false;
		}
	}

	/// <summary>
	/// The type of a context menu. Defines on what occasion the context menu should show in the NodeEditor
	/// </summary>
	public enum ContextType { Node, Canvas, Toolbar }

	/// <summary>
	/// The ContextAttribute is used to register context entries in the NodeEditor.
	/// This function will be called when the user clicked at the item at path
	/// Type defines the type of context menu to appear in, like the right-click on a Node or the Canvas.
	/// Method Signature must be [ Return: Void; Params: NodeEditorInputInfo ]
	/// </summary>
	[AttributeUsage (AttributeTargets.Method)]
	public class ContextEntryAttribute : Attribute 
	{
		public ContextType contextType { get; private set; }
		public string contextPath { get; private set; }

		/// <summary>
		/// Place this function at path in the specified contextType
		/// </summary>
		public ContextEntryAttribute (ContextType type, string path) 
		{
			contextType = type;
			contextPath = path;
		}

		internal static bool AssureValidity (MethodInfo method, ContextEntryAttribute attr) 
		{
			if (!method.IsGenericMethod && !method.IsGenericMethodDefinition && (method.ReturnType == null || method.ReturnType == typeof(void)))
			{ // Check if the method has the correct signature
				ParameterInfo[] methodParams = method.GetParameters ();
				if (methodParams.Length == 1 && methodParams[0].ParameterType == typeof(NodeEditorInputInfo))
					return true;
				else
					Debug.LogWarning ("Method " + method.Name + " has incorrect signature for ContextAttribute!");
			}
			return false;
		}
	}

	/// <summary>
	/// The ContextFillerAttribute is used to register context entries in the NodeEditor.
	/// This function will be called to fill the context GenericMenu in any way it likes to.
	/// Type defines the type of context menu to appear in, like the right-click on a Node or the Canvas.
	/// Method Signature must be [ Return: Void; Params: NodeEditorInputInfo, GenericMenu ]
	/// </summary>
	[AttributeUsage (AttributeTargets.Method)]
	public class ContextFillerAttribute : Attribute 
	{
		public ContextType contextType { get; private set; }

		/// <summary>
		/// Fill the specified contextType
		/// </summary>
		public ContextFillerAttribute (ContextType type)
		{
			contextType = type;
		}

		internal static bool AssureValidity (MethodInfo method, ContextFillerAttribute attr) 
		{
			if (!method.IsGenericMethod && !method.IsGenericMethodDefinition && (method.ReturnType == null || method.ReturnType == typeof(void)))
			{ // Check if the method has the correct signature
				ParameterInfo[] methodParams = method.GetParameters ();
				if (methodParams.Length == 2 && methodParams[0].ParameterType == typeof(NodeEditorInputInfo) && methodParams[1].ParameterType == typeof(GenericMenu))
					return true;
				else
					Debug.LogWarning ("Method " + method.Name + " has incorrect signature for ContextAttribute!");
			}
			return false;
		}
	}

	#endregion
}