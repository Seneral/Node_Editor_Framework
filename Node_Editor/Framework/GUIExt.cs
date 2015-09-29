using UnityEngine;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;

using NodeEditorFramework;

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

		if (pos != new Rect (0, 0, 1, 1) && !pos.Contains (Event.current.mousePosition))
			GUIUtility.keyboardControl = -1;

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

	public static T ObjectField<T> (T obj, bool allowSceneObjects) where T : Object
	{
		return ObjectField<T> (GUIContent.none, obj, allowSceneObjects);
	}

	public static T ObjectField<T> (GUIContent label, T obj, bool allowSceneObjects) where T : Object
	{
#if UNITY_EDITOR
		if (!Application.isPlaying)
			obj = UnityEditor.EditorGUILayout.ObjectField (GUIContent.none, obj, typeof (T), allowSceneObjects) as T;
#endif	
		if (Application.isPlaying)
		{
			bool open = false;
			if (typeof(T).Name == "UnityEngine.Texture2D") 
			{
				label.image = obj as Texture2D;
				GUIStyle style = new GUIStyle (GUI.skin.box);
				style.imagePosition = ImagePosition.ImageAbove;
				open = GUILayout.Button (label, style);
			}
			else
			{
				GUIStyle style = new GUIStyle (GUI.skin.box);
				open = GUILayout.Button (label, style);
			}
			if (open)
			{
				Debug.Log ("Selecting Object!");
			}
		}
		return obj;
	}


	public static int SelectableListPopup (GUIContent[] contents, int selected) 
	{
		return selected;
	}



	public static GUIStyle seperator;
	
	public static Texture2D ColorToTex (Color col) 
	{
		Texture2D tex = new Texture2D (1,1);
		tex.SetPixel (1, 1, col);
		tex.Apply ();
		return tex;
	}
	
	public static void setupSeperator () 
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

	/// <summary>
	/// A GUI Function which simulates the default seperator
	/// </summary>
	public static void Seperator (Rect rect) 
	{
		setupSeperator ();
		
		GUI.Box (new Rect (rect.x, rect.y, rect.width, 1), GUIContent.none, seperator);
	}
}

public class GenericMenu 
{
	public delegate void MenuFunction ();
	public delegate void MenuFunctionData (object userData);

	private static GUIStyle backgroundStyle;
	private static Texture2D expandRight;
	private static float itemHeight;
	public static GUIStyle seperator;

	private List<MenuItem> menuItems = new List<MenuItem> ();
	private Rect position;

	private string selectedPath = "";
	private MenuItem groupToDraw;
	private float currentItemHeight;


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
		expandRight = NodeEditorGUI.LoadTexture ("Textures/expandRight.png");
		itemHeight = GUI.skin.label.CalcHeight (new GUIContent ("text"), 100);

		seperator = new GUIStyle();
		seperator.normal.background = GUIExt.ColorToTex (new Color (0.6f, 0.6f, 0.6f));
		seperator.margin = new RectOffset(0, 0, 7, 7);
	}

#region Creation

	private MenuItem AddHierarchy (ref GUIContent content, out string path) 
	{
		path = content.text;
		if (path.Contains ('/'))
		{ // is inside a group
			string[] subContents = path.Split ('/');
			string folderPath = subContents[0];

			// top level group
			MenuItem parent = menuItems.Find ((MenuItem item) => item.content != null && item.content.text == folderPath);
			if (parent == null)
				menuItems.Add (parent = new MenuItem (folderPath, new GUIContent (folderPath), 0));

			// additional level groups
			for (int groupCnt = 1; groupCnt < subContents.Length-1; groupCnt++)
			{
				string folder = subContents[groupCnt];
				folderPath += "/" + folder;
				MenuItem subGroup = parent.subItems.Find ((MenuItem item) => item.content != null && item.content.text == folder);
				if (subGroup == null)
					parent.subItems.Add (subGroup = new MenuItem (folderPath, new GUIContent (folder), 0));
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

		currentItemHeight = backgroundStyle.contentOffset.y;
		GUI.BeginGroup (extendRect (rect, backgroundStyle.contentOffset), GUIContent.none, backgroundStyle);
		for (int itemCnt = 0; itemCnt < menuItems.Count; itemCnt++)
		{
			DrawItem (menuItems[itemCnt], rect);
			if (activeMenu == null) 
				break;
		}
		GUI.EndGroup ();

		return inRect;
	}

	private void DrawItem (MenuItem item, Rect groupRect) 
	{
		if (item.separator) 
		{
			if (Event.current.type == EventType.Repaint)
				seperator.Draw (new Rect (backgroundStyle.contentOffset.x+1, currentItemHeight+1, groupRect.width-2, 1), GUIContent.none, "Seperator".GetHashCode ());
			currentItemHeight += 3;
		}
		else 
		{
			Rect labelRect = new Rect (backgroundStyle.contentOffset.x, currentItemHeight, groupRect.width, itemHeight);

			bool selected = selectedPath.Contains (item.path);
			if (labelRect.Contains (Event.current.mousePosition))
			{
				selectedPath = item.path;
				selected = true;
			}

			GUI.Label (labelRect, item.content, selected? NodeEditorGUI.nodeLabelSelected : NodeEditorGUI.nodeLabel);

			if (item.group) 
			{
				GUI.DrawTexture (new Rect (labelRect.x+labelRect.width-12, labelRect.y+(labelRect.height-12)/2, 12, 12), expandRight);
				if (selected)
				{
					item.groupPos = new Rect (groupRect.x+groupRect.width+4, groupRect.y+currentItemHeight-2, 0, 0);
					groupToDraw = item;
				}
			}
			else if (selected && (Event.current.type == EventType.MouseDown || Event.current.button != 1 && Event.current.type == EventType.MouseUp))
			{
				ExecuteMenuItem (item);
				activeMenu = null;
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
