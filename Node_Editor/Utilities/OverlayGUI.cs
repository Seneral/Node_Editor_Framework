using UnityEngine;
using System.Collections.Generic;

namespace NodeEditorFramework.Utilities 
{
	public static class OverlayGUI 
	{
		public static PopupMenu CurrentPopup;

		/// <summary>
		/// Returns if any popup currently has control.
		/// </summary>
		public static bool HasPopupControl () 
		{
			return CurrentPopup != null;
		}

		/// <summary>
		/// Starts the OverlayGUI (Call before any other GUI code!)
		/// </summary>
		public static void StartOverlayGUI () 
		{
			if (CurrentPopup != null && Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint)
				CurrentPopup.Draw ();
		}

		/// <summary>
		/// Ends the OverlayGUI (Call after any other GUI code!)
		/// </summary>
		public static void EndOverlayGUI () 
		{
			if (CurrentPopup != null && (Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint))
				CurrentPopup.Draw ();
		}
	}

	/// <summary>
	/// A Generic Popupmenu. Used by GenericMenu, Popup (future), etc.
	/// </summary>
	public class PopupMenu 
	{
		public delegate void MenuFunction ();
		public delegate void MenuFunctionData (object userData);
		
		public List<MenuItem> MenuItems = new List<MenuItem> ();
		
		// State
		private Rect position;
		private string selectedPath = "";
		private MenuItem groupToDraw;
		private float currentItemHeight;
		private bool close;
		
		// GUI variables
		public static GUIStyle BackgroundStyle;
		public static Texture2D ExpandRight;
		public static float ItemHeight;
		public static GUIStyle SelectedLabel;
		
		public PopupMenu () 
		{
			SetupGUI ();
		}
		
		public void SetupGUI () 
		{
		    BackgroundStyle = new GUIStyle(GUI.skin.box) {contentOffset = new Vector2(2, 2)};
		    ExpandRight = ResourceManager.LoadTexture ("Textures/expandRight.png");
			ItemHeight = GUI.skin.label.CalcHeight (new GUIContent ("text"), 100);

		    SelectedLabel = new GUIStyle(GUI.skin.label)
		    {
		        normal = {background = RTEditorGUI.ColorToTex(1, new Color(0.4f, 0.4f, 0.4f))}
		    };
		}
		
		public void Show (Rect pos)
		{
			position = pos;
			selectedPath = "";
			OverlayGUI.CurrentPopup = this;
		}
		
		#region Creation
		
		public void AddItem (GUIContent content, bool on, MenuFunctionData func, object userData)
		{
			string path;
			MenuItem parent = AddHierarchy (ref content, out path);
			if (parent != null)
				parent.SubItems.Add (new MenuItem (path, content, func, userData));
			else
				MenuItems.Add (new MenuItem (path, content, func, userData));
		}
		
		public void AddItem (GUIContent content, bool on, MenuFunction func)
		{
			string path;
			MenuItem parent = AddHierarchy (ref content, out path);
			if (parent != null)
				parent.SubItems.Add (new MenuItem (path, content, func));
			else
				MenuItems.Add (new MenuItem (path, content, func));
		}
		
		public void AddSeparator (string path)
		{
			GUIContent content = new GUIContent (path);
			MenuItem parent = AddHierarchy (ref content, out path);
			if (parent != null)
				parent.SubItems.Add (new MenuItem ());
			else
				MenuItems.Add (new MenuItem ());
		}
		
		private MenuItem AddHierarchy (ref GUIContent content, out string path) 
		{
			path = content.text;
			if (path.Contains ("/"))
			{ // is inside a group
				string[] subContents = path.Split ('/');
				string folderPath = subContents[0];
				
				// top level group
				MenuItem parent = MenuItems.Find (item => item.Content != null && item.Content.text == folderPath);
				if (parent == null)
					MenuItems.Add (parent = new MenuItem (folderPath, new GUIContent (folderPath), true));
				
				// additional level groups
				for (int groupCnt = 1; groupCnt < subContents.Length-1; groupCnt++)
				{
					string folder = subContents[groupCnt];
					folderPath += "/" + folder;
					MenuItem subGroup = parent.SubItems.Find (item => item.Content != null && item.Content.text == folder);
					if (subGroup == null)
						parent.SubItems.Add (subGroup = new MenuItem (folderPath, new GUIContent (folder), true));
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
			bool inRect = DrawGroup (position, MenuItems);
			
			while (groupToDraw != null && !close)
			{
				MenuItem group = groupToDraw;
				groupToDraw = null;
				if (group.Group)
				{
					if (DrawGroup (group.GroupPos, group.SubItems))
						inRect = true;
				}
			}
			
			if (!inRect || close)
				OverlayGUI.CurrentPopup = null;
			
			NodeEditor.RepaintClients ();
		}
		
		private bool DrawGroup (Rect pos, List<MenuItem> menuItems) 
		{
			Rect rect = CalculateRect (pos.position, menuItems);
			
			Rect clickRect = new Rect (rect);
			clickRect.xMax += 20;
			clickRect.xMin -= 20;
			clickRect.yMax += 20;
			clickRect.yMin -= 20;
			bool inRect = clickRect.Contains (Event.current.mousePosition);
			
			currentItemHeight = BackgroundStyle.contentOffset.y;
			GUI.BeginGroup (ExtendRect (rect, BackgroundStyle.contentOffset), GUIContent.none, BackgroundStyle);
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
			if (item.Separator) 
			{
				if (Event.current.type == EventType.Repaint)
					RTEditorGUI.Seperator (new Rect (BackgroundStyle.contentOffset.x+1, currentItemHeight+1, groupRect.width-2, 1));
				currentItemHeight += 3;
			}
			else 
			{
				Rect labelRect = new Rect (BackgroundStyle.contentOffset.x, currentItemHeight, groupRect.width, ItemHeight);
				
				bool selected = selectedPath.Contains (item.Path);
				if (labelRect.Contains (Event.current.mousePosition))
				{
					selectedPath = item.Path;
					selected = true;
				}
				
				GUI.Label (labelRect, item.Content, selected? SelectedLabel : GUI.skin.label);
				
				if (item.Group) 
				{
					GUI.DrawTexture (new Rect (labelRect.x+labelRect.width-12, labelRect.y+(labelRect.height-12)/2, 12, 12), ExpandRight);
					if (selected)
					{
						item.GroupPos = new Rect (groupRect.x+groupRect.width+4, groupRect.y+currentItemHeight-2, 0, 0);
						groupToDraw = item;
					}
				}
				else if (selected && (Event.current.type == EventType.MouseDown || (Event.current.button != 1 && Event.current.type == EventType.MouseUp)))
				{
					item.Execute ();
					close = true;
					Event.current.Use ();
				}
				
				currentItemHeight += ItemHeight;
			}
		}
		
		private static Rect ExtendRect (Rect rect, Vector2 extendValue) 
		{
			rect.x -= extendValue.x;
			rect.y -= extendValue.y;
			rect.width += extendValue.x+extendValue.x;
			rect.height += extendValue.y+extendValue.y;
			return rect;
		}
		
		private static Rect CalculateRect (Vector2 position, List<MenuItem> menuItems) 
		{
			Vector2 size;
			float width = 40, height = 0;
			
			for (var itemCnt = 0; itemCnt < menuItems.Count; itemCnt++)
			{
				MenuItem item = menuItems[itemCnt];
				if (item.Separator)
					height += 3;
				else
				{
					width = Mathf.Max (width, GUI.skin.label.CalcSize (item.Content).x + (item.Group? 22 : 10));
					height += ItemHeight;
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
			public string Path;
			// -!Separator
			public GUIContent Content;
			// -Executable Item
			public MenuFunction Func;
			public MenuFunctionData FuncData;
			public object UserData;
			// -Non-executables
			public bool Separator;
			// --Group
			public bool Group;
			public Rect GroupPos;
			public List<MenuItem> SubItems;
			
			public MenuItem ()
			{
				Separator = true;
			}
			
			public MenuItem (string path, GUIContent content, bool group)
			{
				Path = path;
				Content = content;
				Group = group;
				
				if (Group)
					SubItems = new List<MenuItem> ();
			}
			
			public MenuItem (string path, GUIContent content, MenuFunction func)
			{
				Path = path;
				Content = content;
				Func = func;
			}
			
			public MenuItem (string path, GUIContent content, MenuFunctionData func, object userData)
			{
				Path = path;
				Content = content;
				FuncData = func;
				UserData = userData;
			}
			
			public void Execute ()
			{
				if (FuncData != null)
					FuncData (UserData);
				else if (Func != null)
					Func ();
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
		
		public GenericMenu () 
		{
			popup = new PopupMenu ();
		}
		
		public void ShowAsContext ()
		{
			popup.Show (new Rect (Event.current.mousePosition.x, Event.current.mousePosition.y, 0f, 0f));
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