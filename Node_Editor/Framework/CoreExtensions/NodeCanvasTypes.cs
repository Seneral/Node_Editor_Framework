using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;

using NodeEditorFramework.Utilities;

namespace NodeEditorFramework
{
	public class NodeCanvasManager
	{
		private static Dictionary<Type, NodeCanvasTypeData> CanvasTypes;

		private static Action<Type> _menuCallback;

		/// <summary>
		/// Fetches every CanvasType Declaration in the script assemblies to provide the framework with custom canvas types
		/// </summary>
		public static void FetchCanvasTypes ()
		{
			CanvasTypes = new Dictionary<Type, NodeCanvasTypeData>();
			foreach (Type type in ReflectionUtility.getSubTypes (typeof(NodeCanvas), typeof(NodeCanvasTypeAttribute)))
			{
				object[] nodeAttributes = type.GetCustomAttributes (typeof(NodeCanvasTypeAttribute), false);
				NodeCanvasTypeAttribute attr = nodeAttributes[0] as NodeCanvasTypeAttribute;
				CanvasTypes.Add(type, new NodeCanvasTypeData() { CanvasType = type, DisplayString = attr.Name });
			}
		}

		/// <summary>
		/// Returns all recorded canvas definitions found by the system
		/// </summary>
		public static List<NodeCanvasTypeData> getCanvasDefinitions () 
		{
			return CanvasTypes.Values.ToList ();
		}

		/// <summary>
		/// Returns the NodeData for the given canvas
		/// </summary>
		public static NodeCanvasTypeData GetCanvasTypeData (NodeCanvas canvas)
		{
			return GetCanvasTypeData (canvas.GetType ());
		}

		/// <summary>
		/// Returns the NodeData for the given canvas type
		/// </summary>
		public static NodeCanvasTypeData GetCanvasTypeData (Type canvasType)
		{
			NodeCanvasTypeData data;
			CanvasTypes.TryGetValue (canvasType, out data);
			return data;
		}

		/// <summary>
		/// Returns the NodeData for the given canvas name (type name, display string, etc.)
		/// </summary>
		public static NodeCanvasTypeData GetCanvasTypeData (string name)
		{
			return CanvasTypes.Values.FirstOrDefault ((NodeCanvasTypeData data) => data.CanvasType.FullName.Contains (name) || data.DisplayString.Contains (name) || name.Contains (data.DisplayString));
		}

		/// <summary>
		/// Checks whether the süecified nodeID is compatible with the given canvas type
		/// </summary>
		public static bool CheckCanvasCompability (string nodeID, Type canvasType) 
		{
			NodeTypeData data = NodeTypes.getNodeData (nodeID);
			return data.limitToCanvasTypes == null || data.limitToCanvasTypes.Length == 0 || data.limitToCanvasTypes.Contains (canvasType);
		}

		/// <summary>
		/// Converts the given canvas to the specified type
		/// </summary>
		public static NodeCanvas ConvertCanvasType (NodeCanvas canvas, Type newType)
		{
			NodeCanvas convertedCanvas = canvas;
			if (canvas.GetType () != newType && newType.IsSubclassOf (typeof(NodeCanvas)))
			{
				canvas.Validate();
				canvas = NodeEditorSaveManager.CreateWorkingCopy (canvas);
				convertedCanvas = NodeCanvas.CreateCanvas(newType);
				convertedCanvas.nodes = canvas.nodes;
				convertedCanvas.groups = canvas.groups;
				convertedCanvas.editorStates = canvas.editorStates;
				for (int i = 0; i < convertedCanvas.nodes.Count; i++)
				{
					if (!CheckCanvasCompability (convertedCanvas.nodes[i].GetID, newType))
					{ // Check if nodes is even compatible with the canvas, if not delete it
						convertedCanvas.nodes[i].Delete ();
						i--;
					}
				}
				convertedCanvas.Validate ();
			}
			return convertedCanvas;
		}

		#region Canvas Type Menu

		public static void FillCanvasTypeMenu(ref GenericMenu menu, Action<Type> NodeCanvasSelection, string path = "")
		{
			_menuCallback = NodeCanvasSelection;
			foreach (NodeCanvasTypeData data in CanvasTypes.Values)
				menu.AddItem(new GUIContent(path + data.DisplayString), false, unwrapCanvasTypeCallback, (object)data);
		}

	#if UNITY_EDITOR
		public static void FillCanvasTypeMenu(ref UnityEditor.GenericMenu menu, Action<Type> NodeCanvasSelection, string path = "")
		{
			_menuCallback = NodeCanvasSelection;
			foreach (NodeCanvasTypeData data in CanvasTypes.Values)
				menu.AddItem(new GUIContent(path + data.DisplayString), false, unwrapCanvasTypeCallback, (object)data);
		}
	#endif

		private static void unwrapCanvasTypeCallback(object data)
		{
			NodeCanvasTypeData typeData = (NodeCanvasTypeData)data;
			_menuCallback(typeData.CanvasType);
		}

		#endregion
	}

	public struct NodeCanvasTypeData
	{
		public string DisplayString;
		public Type CanvasType;
	}

	public class NodeCanvasTypeAttribute : Attribute
	{
		public string Name;

		public NodeCanvasTypeAttribute(string displayName)
		{
			Name = displayName;
		}
	}
}