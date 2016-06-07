using UnityEngine;
using System.Collections.Generic;

namespace NodeEditorFramework.Utilities 
{
	public static class OverlayGUI 
	{
		public static PopupMenu currentPopup;

		/// <summary>
		/// Returns if any popup currently has control.
		/// </summary>
		public static bool HasPopupControl () 
		{
			return currentPopup != null;
		}

		/// <summary>
		/// Starts the OverlayGUI (Call before any other GUI code!)
		/// </summary>
		public static void StartOverlayGUI () 
		{
			if (currentPopup != null && Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint)
				currentPopup.Draw ();
		}

		/// <summary>
		/// Ends the OverlayGUI (Call after any other GUI code!)
		/// </summary>
		public static void EndOverlayGUI () 
		{
			if (currentPopup != null && (Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint))
				currentPopup.Draw ();
		}
	}

	/// <summary>
	/// A Generic Popupmenu. Used by GenericMenu, Popup (future), etc.
	/// </summary>
	public class PopupMenu 
	{
		public delegate void MenuFunction ();
		public delegate void MenuFunctionData (object userData);
		
		public List<MenuItem> menuItems = new List<MenuItem> ();
		
		// State
		private Rect position;
		private string selectedPath;
		private MenuItem groupToDraw;
		private float currentItemHeight;
		private bool close;
		
		// GUI variables
		public static GUIStyle backgroundStyle;
		public static Texture2D expandRight;
		public static float itemHeight;
		public static GUIStyle selectedLabel;
		
		public PopupMenu () 
		{
			SetupGUI ();
		}
		
		public void SetupGUI () 
		{
			backgroundStyle = new GUIStyle (GUI.skin.box);
			backgroundStyle.contentOffset = new Vector2 (2, 2);
			expandRight = NodeEditorFramework.Utilities.ResourceManager.LoadTexture ("Textures/expandRight.png");
			itemHeight = GUI.skin.label.CalcHeight (new GUIContent ("text"), 100);
			
			selectedLabel = new GUIStyle (GUI.skin.label);
			selectedLabel.normal.background = NodeEditorFramework.Utilities.RTEditorGUI.ColorToTex (1, new Color (0.4f, 0.4f, 0.4f));
		}
		
		public void Show (Vector2 pos)
		{
			position = calculateRect (pos, menuItems);
			selectedPath = "";
			OverlayGUI.currentPopup = this;
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
			bool inRect = DrawGroup (position, menuItems);
			
			while (groupToDraw != null && !close)
			{
				MenuItem group = groupToDraw;
				groupToDraw = null;
				if (group.group)
				{
					if (DrawGroup (group.groupPos, group.subItems))
						inRect = true;
				}
			}
			
			if (!inRect || close) 
			{
				OverlayGUI.currentPopup = null;
			}

			NodeEditorFramework.NodeEditor.RepaintClients ();
		}
		
		private bool DrawGroup (Rect pos, List<MenuItem> menuItems) 
		{
			Rect rect = calculateRect (pos.position, menuItems);
			
			Rect clickRect = new Rect (rect);
			clickRect.xMax += 20;
			clickRect.xMin -= 20;
			clickRect.yMax += 20;
			clickRect.yMin -= 20;
			bool inRect = clickRect.Contains (Event.current.mousePosition);

			currentItemHeight = backgroundStyle.contentOffset.y;
			GUI.BeginGroup (extendRect (rect, backgroundStyle.contentOffset), GUIContent.none, backgroundStyle);
			for (int itemCnt = 0; itemCnt < menuItems.Count; itemCnt++)
			{
				DrawItem (menuItems[itemCnt], rect);
				if (close) break;
			}
			GUI.EndGroup ();
			
			return inRect;
		}
		
		private void DrawItem (MenuItem item, Rect groupRect) 
		{
			if (item.separator) 
			{
				if (Event.current.type == EventType.Repaint)
					RTEditorGUI.Seperator (new Rect (backgroundStyle.contentOffset.x+1, currentItemHeight+1, groupRect.width-2, 1));
				currentItemHeight += 3;
			}
			else 
			{
				Rect labelRect = new Rect (backgroundStyle.contentOffset.x, currentItemHeight, groupRect.width, itemHeight);

				if (labelRect.Contains (Event.current.mousePosition))
					selectedPath = item.path;

				bool selected = selectedPath.Contains (item.path);
				GUI.Label (labelRect, item.content, selected? selectedLabel : GUI.skin.label);
				
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
		
		private static Rect calculateRect (Vector2 position, List<MenuItem> menuItems) 
		{
			Vector2 size;
			float width = 40, height = 0;
			
			for (int itemCnt = 0; itemCnt < menuItems.Count; itemCnt++)
			{
				MenuItem item = menuItems[itemCnt];
				if (item.separator)
					height += 3;
				else
				{
					width = Mathf.Max (width, GUI.skin.label.CalcSize (item.content).x + (item.group? 22 : 10));
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
		private static PopupMenu popup;

		public Vector2 Position { get { return popup.Position; } }
		
		public GenericMenu () 
		{
			popup = new PopupMenu ();
		}
		
		public void ShowAsContext ()
		{
			popup.Show (GUIScaleUtility.GUIToScreenSpace (Event.current.mousePosition));
		}

		public void Show (Vector2 pos)
		{
			popup.Show (GUIScaleUtility.GUIToScreenSpace (pos));
		}
		
		public void AddItem (GUIContent content, bool on, PopupMenu.MenuFunctionData func, object userData)
		{
			popup.AddItem (content, on, func, userData);
		}
		
		public void AddItem (GUIContent content, bool on, PopupMenu.MenuFunction func)
		{
			popup.AddItem (content, on, func);
		}
		
		public void AddSeparator (string path)
		{
			popup.AddSeparator (path);
		}
	}
}