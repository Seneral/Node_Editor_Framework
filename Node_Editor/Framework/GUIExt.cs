using UnityEngine;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;

public static class GUIExt 
{

	/// <summary>
	/// Mimic's UnityEditor.EditorGUILayout.TextField in taking a label and a string and returning the edited string.
	/// </summary>
	public static string TextField (GUIContent label, string text)
	{
		GUILayout.BeginHorizontal ();
		GUILayout.Label (label, label != GUIContent.none? GUILayout.ExpandWidth (true) : GUILayout.ExpandWidth (false));
		text = GUILayout.TextField (text);
		GUILayout.EndHorizontal ();
		return text;
	}


	private static int activeFloatField = -1;
	private static float activeFloatFieldLastValue = 0;
	private static string activeFloatFieldString = "";
	/// <summary>
	/// Float Field for ingame purposes. Behaves exactly like UnityEditor.EditorGUILayout.FloatField
	/// </summary>
	public static float FloatField (float value)
	{
		// Get rect and control for this float field for identification
		Rect pos = GUILayoutUtility.GetRect (new GUIContent (value.ToString ()), GUI.skin.label, new GUILayoutOption[] { GUILayout.ExpandWidth (false), GUILayout.MinWidth (40) });
		int floatFieldID = GUIUtility.GetControlID ("FloatField".GetHashCode (), FocusType.Keyboard, pos) + 1;
		if (floatFieldID == 0)
			return value;

		bool recorded = activeFloatField == floatFieldID;
		bool active = floatFieldID == GUIUtility.keyboardControl;

		if (active && recorded && activeFloatFieldLastValue != value)
		{ // Value has been modified externally
			activeFloatFieldLastValue = value;
			activeFloatFieldString = value.ToString ();
		}

		// Get stored string for the text field if this one is recorded
		string str = recorded? activeFloatFieldString : value.ToString ();

		// pass it in the text field
		string strValue = GUI.TextField (pos, str);

		// Update stored value if this one is recorded
		if (recorded)
			activeFloatFieldString = strValue;

		// Try Parse if value got changed. If the string could not be parsed, ignore it and keep last value
		bool parsed = true;
		if (strValue != value.ToString ())
		{
			float newValue;
			parsed = float.TryParse (strValue, out newValue);
			if (parsed)
				value = activeFloatFieldLastValue = newValue;
		}

		if (active && !recorded)
		{ // Gained focus this frame
			activeFloatField = floatFieldID;
			activeFloatFieldString = strValue;
			activeFloatFieldLastValue = value;
		}
		else if (!active && recorded) 
		{ // Lost focus this frame
			activeFloatField = -1;
			if (!parsed)
				value = strValue.ForceParse ();
		}

		return value;
	}
	
	/// <summary>
	/// Float Field for ingame purposes. Behaves exactly like UnityEditor.EditorGUILayout.FloatField
	/// </summary>
	public static float FloatField (GUIContent label, float value)
	{
		GUILayout.BeginHorizontal ();
		
		GUILayout.Label (label, label != GUIContent.none? GUILayout.ExpandWidth (true) : GUILayout.ExpandWidth (false));
		//		Rect sliderRect = GUILayoutUtility.GetLastRect ();

		value = FloatField (value);
		
		// Check if this is active and, if it just became active, set the start slider value
		//		bool active = lastFloatField == activeFloatField;
		//		Event cur = Event.current;
		//		if (cur.type == EventType.MouseDown && sliderRect.Contains (cur.mousePosition))
		//			activeFloatField = lastFloatField;
		//		else if (cur.type != EventType.MouseDrag)
		//			activeFloatField = "";
		//		if (active != (lastFloatField == activeFloatField))
		//		{
		//			active = lastFloatField == activeFloatField;
		//			startSlideValue = active? value : 0;
		//			startSlidePos = active? cur.mousePosition.x : 0;
		//			
		//		}
		//
		//		if (active) 
		//		{
		//			UnityEngine.Debug.Log (startSlideValue + " val, pos: " + startSlidePos);
		//			value = startSlideValue + (cur.mousePosition.x-startSlidePos)*startSlideValue;
		//			cur.Use ();
		//		}
		
		GUILayout.EndHorizontal ();
		return value;
	}

	/// <summary>
	/// Forces to parse to float by cleaning string if necessary
	/// </summary>
	public static float ForceParse (this string str) 
	{
		// try parse
		float value;
		if (float.TryParse (str, out value))
			return value;

		// Clean string if it could not be parsed
		bool recordedDecimalPoint = false;
		List<char> strVal = new List<char> (str);
		for (int cnt = 0; cnt < strVal.Count; cnt++) 
		{
			UnicodeCategory type = CharUnicodeInfo.GetUnicodeCategory (str[cnt]);
			if (type != UnicodeCategory.DecimalDigitNumber)
			{
				strVal.RemoveRange (cnt, strVal.Count-cnt);
				break;
			}
			else if (str[cnt] == '.')
			{
				if (recordedDecimalPoint)
				{
					strVal.RemoveRange (cnt, strVal.Count-cnt);
					break;
				}
				recordedDecimalPoint = true;
			}
		}

		// Parse again
		if (strVal.Count == 0)
			return 0;
		str = new string (strVal.ToArray ());
		if (!float.TryParse (str, out value))
			Debug.LogError ("Could not parse " + str);
		return value;
	}

	public static int SelectableListPopup (GUIContent[] contents, int selected) 
	{
		return selected;
	}



	private static GUIStyle seperator;
	
	public static Texture2D ColorToTex (Color col) 
	{
		Texture2D tex = new Texture2D (1,1);
		tex.SetPixel (1, 1, col);
		tex.Apply ();
		return tex;
	}
	
	private static void setupSeperator () 
	{
		if (seperator == null) 
		{
			seperator = new GUIStyle();
			seperator.normal.background = ColorToTex (new Color (0.6f, 0.6f, 0.6f));
			seperator.stretchWidth = true;
			seperator.margin = new RectOffset(0, 0, 7, 7);
		}
	}

	/// <summary>
	/// A GUI Function which simulates the default seperator
	/// </summary>
	public static void Seperator () 
	{
		setupSeperator ();
		
		GUILayout.Box (GUIContent.none, seperator, new GUILayoutOption[] { GUILayout.Height (1) });
	}
}

public class GenericMenu 
{
	public delegate void MenuFunction ();
	public delegate void MenuFunctionData (object userData);

	private static GUIStyle backgroundStyle;
	private static GUIStyle itemStyle;
	private static GUIStyle selectedStyle;
	private static Texture2D expandRight;
	private static float itemHeight;

	private List<MenuItem> menuItems = new List<MenuItem> ();
	private Rect position;

	private string selectedPath = "";
	private MenuItem groupToDraw;


	private static GenericMenu activeMenu;

	public static void DrawActive () 
	{
		if (activeMenu != null)
			activeMenu.Draw ();
	}

	public static GenericMenu current { get { return activeMenu; } }



	public GenericMenu () 
	{
		backgroundStyle = new GUIStyle (GUI.skin.box);
		backgroundStyle.contentOffset = new Vector2 (2, 2);
		itemStyle = new GUIStyle (GUI.skin.label);
		selectedStyle = new GUIStyle (GUI.skin.label);
		selectedStyle.normal.background = GUIExt.ColorToTex (new Color (0.8f, 0.8f, 0.8f));
		expandRight = NodeEditorFramework.NodeEditor.LoadTexture ("Textures/expandRight.png");
		itemHeight = itemStyle.CalcHeight (new GUIContent ("text"), 100);
	}

#region Creation

	private MenuItem AddHierarchy (ref GUIContent content, out string path) 
	{
		path = content.text;
		if (path.Contains ('/'))
		{ // is inside a group
			string[] subContents = path.Split ('/');
			path = subContents[0];

			// top level group
			MenuItem parent = menuItems.Find ((MenuItem item) => item.content.text == subContents[0]);
			if (parent == null)
				menuItems.Add (parent = new MenuItem (path, new GUIContent (subContents[0]), 0));

			// additional level groups
			for (int groupCnt = 1; groupCnt < subContents.Length-1; groupCnt++)
			{
				string groupName = subContents[groupCnt];
				path += "/" + groupName;
				MenuItem subGroup = parent.subItems.Find ((MenuItem item) => item.content.text == groupName);
				if (subGroup == null)
					parent.subItems.Add (subGroup = new MenuItem (path, new GUIContent (groupName), 0));
				parent = subGroup;
			}

			// actual item
			path = content.text;
			content = new GUIContent (subContents[subContents.Length-1], content.tooltip);
			return parent;
		}
		return null;
	}

	public void AddDisabledItem (GUIContent content)
	{
		string path;
		MenuItem parent = AddHierarchy (ref content, out path);
		if (parent != null)
			parent.subItems.Add (new MenuItem (path, content, 1));
		else
			menuItems.Add (new MenuItem (path, content, 1));
	}
	
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
#endregion

	public void ShowAsContext ()
	{
		Show (new Rect (Event.current.mousePosition.x, Event.current.mousePosition.y, 0f, 0f));
	}

	public void Show (Rect pos)
	{
		position = pos;
		activeMenu = this;
		selectedPath = "";
	}

	private void Draw () 
	{
		bool inRect = DrawGroup (position, menuItems);

		while (groupToDraw != null && activeMenu != null)
		{
			MenuItem group = groupToDraw;
			groupToDraw = null;
			if (group.group)
			{
				if (DrawGroup (group.groupPos, group.subItems))
					inRect = true;
			}
		}

		if (!inRect)
			activeMenu = null;

		if (NodeEditorFramework.NodeEditor.Repaint != null)
			NodeEditorFramework.NodeEditor.Repaint ();
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

		GUI.BeginGroup (extendRect (rect, backgroundStyle.contentOffset), GUIContent.none, backgroundStyle);
		for (int itemCnt = 0; itemCnt < menuItems.Count; itemCnt++)
		{
			DrawItem (menuItems, itemCnt, rect);
			if (activeMenu == null) 
				break;
		}
		GUI.EndGroup ();

		return inRect;
	}

	private void DrawItem (List<MenuItem> menuItems, int index, Rect groupRect) 
	{
		if (menuItems[index].separator)
			GUIExt.Seperator ();
		else 
		{
			MenuItem item = menuItems[index];

			bool selected = selectedPath.Contains (item.path);
			GUIStyle style = selected?selectedStyle:itemStyle;

			float itemYPos = backgroundStyle.contentOffset.y;
			for (int itemCnt = 0; itemCnt < index; itemCnt++)
				itemYPos += menuItems[index].separator? 5 : itemHeight;
			Rect labelRect = new Rect (backgroundStyle.contentOffset.x, itemYPos, groupRect.width, itemHeight);// GUILayoutUtility.GetRect (item.content, style, GUILayout.Width (groupRect.width));

			if (item.group) 
			{
				GUI.Label (new Rect (labelRect.x, labelRect.y, labelRect.width, labelRect.height), 
				           item.content, style);
				GUI.DrawTexture (new Rect (labelRect.x+labelRect.width-12, labelRect.y+(labelRect.height-12)/2, 12, 12),
				                 expandRight);
			}
			else 
			{
				if (GUI.Button (labelRect, item.content, style)) 
				{
					ExecuteMenuItem (item);
					activeMenu = null;
					return;
				}
			}
			
			if (item.group && selected)
			{
				item.groupPos = new Rect (groupRect.x+groupRect.width+4, groupRect.y+itemYPos-2, 0, 0);
				groupToDraw = item;
			}

			if (labelRect.Contains (Event.current.mousePosition))
			{
				selectedPath = item.path;
				selected = true;
			}
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
		float width = 0, height = 0;

		for (int itemCnt = 0; itemCnt < menuItems.Count; itemCnt++)
		{
			MenuItem item = menuItems[itemCnt];
			if (item.separator)
				height += 5;
			else
			{
				width = Mathf.Max (width, itemStyle.CalcSize (item.content).x + (item.group? 12 : 0));
				height += itemHeight;
			}
		}

		size = new Vector2 (width, height);
		bool down = (position.y+size.y) <= Screen.height;
		return new Rect (position.x, position.y - (down? 0 : size.y), size.x, size.y);
	}

	private void ExecuteMenuItem (MenuItem menuItem)
	{
		if (menuItem.funcData != null)
			menuItem.funcData (menuItem.userData);
		else if (menuItem.func != null)
			menuItem.func ();
	}

	private class MenuItem
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
		public bool disabled = false;
		// --Group
		public bool group = false;
		public Rect groupPos;
		public List<MenuItem> subItems;

		public MenuItem ()
		{
			separator = true;
		}

		public MenuItem (string _path, GUIContent _content, int mode) // Mode - 0: group, 1: disabled
		{
			path = _path;
			content = _content;
			group = mode == 0;
			disabled = mode == 1;

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
	}
}
