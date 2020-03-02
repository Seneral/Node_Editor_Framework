using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using MenuFunction = UnityEditor.GenericMenu.MenuFunction;
using MenuFunctionData = UnityEditor.GenericMenu.MenuFunction2;
#else
using MenuFunction = NodeEditorFramework.Utilities.OverlayGUI.CustomMenuFunction;
using MenuFunctionData = NodeEditorFramework.Utilities.OverlayGUI.CustomMenuFunctionData;
#endif

namespace NodeEditorFramework.Utilities 
{
	public static class OverlayGUI 
	{
		public delegate void CustomMenuFunction();
		public delegate void CustomMenuFunctionData(object userData);

		private static string currentEditorUser;

		public static string openedPopupUser = "NONE";
		public static PopupMenu openedPopup;

		/// <summary>
		/// Returns if any popup currently has control.
		/// </summary>
		public static bool HasPopupControl () 
		{
			return openedPopup != null && currentEditorUser == openedPopupUser;
		}

		/// <summary>
		/// Starts the OverlayGUI (Call before any other GUI code!)
		/// </summary>
		public static void StartOverlayGUI (string editorUser) 
		{
			currentEditorUser = editorUser;
			if (HasPopupControl () && Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint)
				openedPopup.Draw ();
		}

		/// <summary>
		/// Ends the OverlayGUI (Call after any other GUI code!)
		/// </summary>
		public static void EndOverlayGUI () 
		{
			if (HasPopupControl () && (Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint))
				openedPopup.Draw ();
		}

		/// <summary>
		/// Opens the specified popupMenu in the current editor users and closes all other popups
		/// </summary>
		public static void OpenPopup (PopupMenu popup)
		{
			openedPopup = popup;
			openedPopupUser = currentEditorUser;
		}

		/// <summary>
		/// Closes the popup in the current editor if existant
		/// </summary>
		public static void ClosePopup ()
		{
			if (HasPopupControl ())
			{
				openedPopup = null;
				openedPopupUser = "NONE";
			}
		}
	}

	/// <summary>
	/// A Generic Popupmenu. Used by GenericMenu, Popup (future), etc.
	/// </summary>
	public class PopupMenu 
	{
		public List<MenuItem> menuItems = new List<MenuItem> ();
		
		// State
		private Rect position;
		private string selectedPath;
		private MenuItem groupToDraw;
		private float currentItemHeight;
		private bool close;
		
		// GUI variables
		public static GUIStyle popupBox;
		public static Texture2D expandRight;
		public static float itemHeight;

		public float minWidth;

		private const float minCloseDistance = 200;

		public PopupMenu () 
		{
			SetupGUI ();
		}
		
		public void SetupGUI () 
		{
			popupBox = new GUIStyle (NodeEditorGUI.nodeSkin.box);
			popupBox.contentOffset = new Vector2 (2, 2);
			expandRight = ResourceManager.LoadTexture ("Textures/expandRight.png");
			itemHeight = NodeEditorGUI.nodeSkin.label.CalcHeight (new GUIContent ("text"), 100);
		}

		public void Show (Vector2 pos, float MinWidth = 40)
		{
			minWidth = MinWidth;
			position = calculateRect (pos, menuItems, minWidth);
			selectedPath = "";
			OverlayGUI.OpenPopup (this);
		}

		public Vector2 Position { get { return position.position; } }

#region Creation
		
		public void AddItem (GUIContent content, bool on, MenuFunctionData func, object userData)
		{
			string path;
			MenuItem parent = AddHierarchy (ref content, out path);
			if (parent != null)
				parent.subItems.Add (new MenuItem (path, content, func, userData));
			else
				menuItems.Add (new MenuItem (path, content, func, userData));
		}
		
		public void AddItem (GUIContent content, bool on, MenuFunction func)
		{
			string path;
			MenuItem parent = AddHierarchy (ref content, out path);
			if (parent != null)
				parent.subItems.Add (new MenuItem (path, content, func));
			else
				menuItems.Add (new MenuItem (path, content, func));
		}
		
		public void AddSeparator (string path)
		{
			GUIContent content = new GUIContent (path);
			MenuItem parent = AddHierarchy (ref content, out path);
			if (parent != null)
				parent.subItems.Add (new MenuItem ());
			else
				menuItems.Add (new MenuItem ());
		}
		
		private MenuItem AddHierarchy (ref GUIContent content, out string path) 
		{
			path = content.text;
			if (path.Contains ("/"))
			{ // is inside a group
				string[] subContents = path.Split ('/');
				string folderPath = subContents[0];
				
				// top level group
				MenuItem parent = menuItems.Find ((MenuItem item) => item.content != null && item.content.text == folderPath && item.group);
				if (parent == null)
					menuItems.Add (parent = new MenuItem (folderPath, new GUIContent (folderPath), true));
				// additional level groups
				for (int groupCnt = 1; groupCnt < subContents.Length-1; groupCnt++)
				{
					string folder = subContents[groupCnt];
					folderPath += "/" + folder;
					if (parent == null)
						Debug.LogError ("Parent is null!");
					else if (parent.subItems == null)
						Debug.LogError ("Subitems of " + parent.content.text + " is null!");
					MenuItem subGroup = parent.subItems.Find ((MenuItem item) => item.content != null && item.content.text == folder && item.group);
					if (subGroup == null)
						parent.subItems.Add (subGroup = new MenuItem (folderPath, new GUIContent (folder), true));
					parent = subGroup;
				}
				
				// actual item
				path = content.text;
				content = new GUIContent (subContents[subContents.Length-1], content.tooltip);
				return parent;
			}
			return null;
		}
		
#endregion
		
#region Drawing
		
		public void Draw () 
		{
			int inRect = DrawGroup (position, menuItems);
			
			while (groupToDraw != null && !close)
			{
				MenuItem group = groupToDraw;
				groupToDraw = null;
				if (group.group) // Draw group and find if the mouse is in group rect
					inRect = Mathf.Max(inRect, DrawGroup(group.groupPos, group.subItems));
			}

			if (close || inRect < 2 || (Event.current.type == EventType.MouseDown && inRect < 3)) 
				OverlayGUI.ClosePopup ();

			NodeEditor.RepaintClients ();
		}
		
		private int DrawGroup (Rect pos, List<MenuItem> menuItems) 
		{
			Rect rect = calculateRect (pos.position, menuItems, minWidth);

			// DRAW GROUP
			currentItemHeight = popupBox.contentOffset.y;
			GUI.BeginGroup (extendRect (rect, popupBox.contentOffset), GUIContent.none, popupBox);
			for (int itemCnt = 0; itemCnt < menuItems.Count; itemCnt++)
			{
				DrawItem (menuItems[itemCnt], rect);
				if (close) break;
			}
			GUI.EndGroup ();

			// MOUSE POS RECT TEST
			int inRect = 1; // State 1: Outside of all rects
			if (rect.Contains(Event.current.mousePosition))
				inRect = 3; // State 3: Inside group rect
			else
			{
				Rect clickRect = new Rect(rect.x - minCloseDistance, rect.y - minCloseDistance, rect.width + 2 * minCloseDistance, rect.height + 2 * minCloseDistance);
				if (clickRect.Contains(Event.current.mousePosition))
					inRect = 2; // State 2: Inside extended click rect
			}

			return inRect;
		}
		
		private void DrawItem (MenuItem item, Rect groupRect) 
		{
			if (item.separator) 
			{
				if (Event.current.type == EventType.Repaint)
					RTEditorGUI.Seperator (new Rect (popupBox.contentOffset.x+1, currentItemHeight+1, groupRect.width-2, 1));
				currentItemHeight += 3;
			}
			else 
			{
				Rect labelRect = new Rect (popupBox.contentOffset.x, currentItemHeight, groupRect.width, itemHeight);

				if (labelRect.Contains (Event.current.mousePosition))
					selectedPath = item.path;

				bool selected = selectedPath == item.path || selectedPath.Contains (item.path + "/");
				GUI.Label (labelRect, item.content, NodeEditorGUI.nodeSkin.GetStyle(selected? "labelSelected" : "label"));
				
				if (item.group) 
				{
					GUI.DrawTexture (new Rect (labelRect.x+labelRect.width-12, labelRect.y+(labelRect.height-12)/2, 12, 12), expandRight);
					if (selected)
					{
						item.groupPos = new Rect (groupRect.x+groupRect.width+4, groupRect.y+currentItemHeight-2, 0, 0);
						groupToDraw = item;
					}
				}
				else if (selected && (Event.current.type == EventType.MouseDown || (Event.current.button != 1 && Event.current.type == EventType.MouseUp)))
				{
					item.Execute ();
					close = true;
					Event.current.Use ();
				}
				
				currentItemHeight += itemHeight;
			}
		}
		
		private static Rect extendRect (Rect rect, Vector2 extendValue) 
		{
			rect.x -= extendValue.x;
			rect.y -= extendValue.y;
			rect.width += extendValue.x+extendValue.x;
			rect.height += extendValue.y+extendValue.y;
			return rect;
		}
		
		private static Rect calculateRect (Vector2 position, List<MenuItem> menuItems, float minWidth) 
		{
			Vector2 size;
			float width = minWidth, height = 0;
			
			for (int itemCnt = 0; itemCnt < menuItems.Count; itemCnt++)
			{
				MenuItem item = menuItems[itemCnt];
				if (item.separator)
					height += 3;
				else
				{
					width = Mathf.Max (width, NodeEditorGUI.nodeSkin.label.CalcSize (item.content).x + (item.group? 22 : 10));
					height += itemHeight;
				}
			}
			
			size = new Vector2 (width, height);
			bool down = (position.y+size.y) <= Screen.height;
			return new Rect (position.x, position.y - (down? 0 : size.y), size.x, size.y);
		}
		
#endregion
		
#region Nested MenuItem
		
		public class MenuItem
		{
			public string path;
			// -!Separator
			public GUIContent content;
			// -Executable Item
			public MenuFunction func;
			public MenuFunctionData funcData;
			public object userData;
			// -Non-executables
			public bool separator = false;
			// --Group
			public bool group = false;
			public Rect groupPos;
			public List<MenuItem> subItems;
			
			public MenuItem ()
			{
				separator = true;
			}
			
			public MenuItem (string _path, GUIContent _content, bool _group)
			{
				path = _path;
				content = _content;
				group = _group;
				
				if (group)
					subItems = new List<MenuItem> ();
			}
			
			public MenuItem (string _path, GUIContent _content, MenuFunction _func)
			{
				path = _path;
				content = _content;
				func = _func;
			}
			
			public MenuItem (string _path, GUIContent _content, MenuFunctionData _func, object _userData)
			{
				path = _path;
				content = _content;
				funcData = _func;
				userData = _userData;
			}
			
			public void Execute ()
			{
				if (funcData != null)
					funcData (userData);
				else if (func != null)
					func ();
			}
		}
		
#endregion
	}

	/// <summary>
	/// Generic Menu which mimics UnityEditor.GenericMenu class pretty much exactly. Wrapper for the generic PopupMenu.
	/// </summary>
	public class GenericMenu
	{
#if UNITY_EDITOR
		private UnityEditor.GenericMenu editorMenu;
#endif
		private static PopupMenu popup;

		public Vector2 Position { get { return popup.Position; } }
		
		public GenericMenu (bool emulateEditor = false) 
		{
#if UNITY_EDITOR
			if (emulateEditor)
				editorMenu = new UnityEditor.GenericMenu();
			else
#endif
				popup = new PopupMenu ();
		}
		
		public void ShowAsContext ()
		{
#if UNITY_EDITOR
			if (editorMenu != null)
				editorMenu.ShowAsContext();
			else
#endif
				popup.Show (GUIScaleUtility.GUIToScreenSpace(Event.current.mousePosition));
		}

		public void Show(Vector2 pos, float MinWidth = 40)
		{
#if UNITY_EDITOR
			if (editorMenu != null)
				editorMenu.DropDown(new Rect (pos, Vector2.zero));
			else
#endif
				popup.Show(GUIScaleUtility.GUIToScreenSpace (pos), MinWidth);
		}

		public void DropDown(Rect rect, float MinWidth = 40)
		{
#if UNITY_EDITOR
			if (editorMenu != null)
				editorMenu.DropDown(rect);
			else
#endif
				popup.Show(GUIScaleUtility.GUIToScreenSpace (rect.position), Mathf.Max (rect.width, MinWidth));
		}

		public void AddItem (GUIContent content, bool on, MenuFunctionData func, object userData)
		{
#if UNITY_EDITOR
			if (editorMenu != null)
				editorMenu.AddItem (content, on, func, userData);
			else
#endif
				popup.AddItem (content, on, func, userData);
		}

		public void AddItem(GUIContent content, bool on, MenuFunction func)
		{
#if UNITY_EDITOR
			if (editorMenu != null)
				editorMenu.AddItem(content, on, func);
			else
#endif
				popup.AddItem(content, on, func);
		}

		public void AddDisabledItem(GUIContent content)
		{
#if UNITY_EDITOR
			if (editorMenu != null)
				editorMenu.AddDisabledItem(content);
			else
#endif
				popup.AddItem(content, false, null);
		}

		public void AddSeparator (string path)
		{
#if UNITY_EDITOR
			if (editorMenu != null)
				editorMenu.AddSeparator(path);
			else
#endif
				popup.AddSeparator (path);
		}
	}
}