using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;

public static class GUIScaleUtility
{
	private static MethodInfo GetTopRect;
	private static PropertyInfo topmostRect;

	public static Rect getTopRect
	{
		get 
		{
			return (Rect)GetTopRect.Invoke (null, null);
		}
	}

	public static Rect getTopRectScreenSpace
	{
		get 
		{
			return (Rect)topmostRect.GetValue (null, null);
		}
	}

	public static List<Rect> currentRectStack { get; private set; }
	private static List<List<Rect>> rectStackGroups;

	private static List<Matrix4x4> GUIMatrices;
	private static List<bool> adjustedGUILayout;

	private static FieldInfo currentGUILayoutCache;
	private static FieldInfo currentTopLevelGroup;

//	private static Type GUILayoutGroupType;
//	private static Type GUILayoutEntryType;
//
//	private static FieldInfo LayoutGroupRect;
//	private static FieldInfo LayoutEntryHeight;
//	private static FieldInfo LayoutEntryWidth;
//	private static PropertyInfo LayoutEntryStyle;

	private static Matrix4x4 cachedMatrix;


	public static void Init () 
	{
		Assembly UnityEngine = Assembly.GetAssembly (typeof (UnityEngine.GUI));

		Type GUIClipType = UnityEngine.GetType ("UnityEngine.GUIClip");

		topmostRect = GUIClipType.GetProperty ("topmostRect");
		GetTopRect = GUIClipType.GetMethod ("GetTopRect", BindingFlags.Static | BindingFlags.NonPublic);

		// As we can call Begin/Ends inside another, we need to save their states hierarchial in Lists:
		currentRectStack = new List<Rect> ();
		rectStackGroups = new List<List<Rect>> ();

		GUIMatrices = new List<Matrix4x4> ();
		adjustedGUILayout = new List<bool> ();

//		Type GUILayoutUtilityType = UnityEngine.GetType ("UnityEngine.GUILayoutUtility");
//		currentGUILayoutCache = GUILayoutUtilityType.GetField ("current", BindingFlags.Static | BindingFlags.NonPublic);
//
//		Type GUILayoutCacheType = GUILayoutUtilityType.GetNestedType ("LayoutCache", BindingFlags.NonPublic);
//		currentTopLevelGroup = GUILayoutCacheType.GetField ("topLevel", BindingFlags.NonPublic | BindingFlags.Instance);
//
//		GUILayoutGroupType = UnityEngine.GetType ("UnityEngine.GUILayoutGroup");
//		GUILayoutEntryType = UnityEngine.GetType ("UnityEngine.GUILayoutEntry");
//
//		LayoutGroupRect = GUILayoutGroupType.GetField ("rect");
//
//		LayoutEntryHeight = GUILayoutEntryType.GetField ("maxHeight");
//		LayoutEntryWidth = GUILayoutEntryType.GetField ("maxWidth");
//		LayoutEntryStyle = GUILayoutEntryType.GetProperty ("style");


	}

	// SCALE
	public static Vector2 BeginScale (ref Rect rect, Vector2 zoomPivot, float zoom, bool adjustGUILayout) 
	{
		GUIScaleUtility.BeginNoClip ();
		
		Rect screenRect = GUIScaleUtility.InnerToScreenRect (rect);
		
		// The Rect of the new clipping group to draw our nodes in
		rect = ScaleRect (screenRect, screenRect.position + zoomPivot, new Vector2 (zoom, zoom));
		
		// Now continue drawing using the new clipping group
		GUI.BeginGroup (rect);
		rect.position = Vector2.zero; // Adjust because we entered the new group

		// Because I currently found no way to actually scale to the center of the window rather than (0, 0),
		// I'm going to cheat and just pan it accordingly to let it appear as if it would scroll to the center
		// Note, due to that, other controls are still scaled to (0, 0)
		Vector2 zoomPosAdjust = rect.center - screenRect.size/2 + zoomPivot;

		// For GUILayout, we can make this adjustment here
		adjustedGUILayout.Add (adjustGUILayout);
		if (adjustGUILayout)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Space (rect.center.x - screenRect.size.x + zoomPivot.x);
			GUILayout.BeginVertical ();
			GUILayout.Space (rect.center.y - screenRect.size.y + zoomPivot.y);
		}

		// Take a matrix backup to restore back later on
		GUIMatrices.Add (GUI.matrix);
		
		// Scale GUI.matrix. After that we have the correct clipping group again.
		GUIUtility.ScaleAroundPivot (new Vector2 (1/zoom, 1/zoom), zoomPosAdjust);

		return zoomPosAdjust;
	}

	public static void EndScale () 
	{
		// Set last matrix and clipping group
		if (GUIMatrices.Count == 0 || adjustedGUILayout.Count == 0)
			throw new UnityException ("GUIScaleutility: You are ending more scales than you are beginning!");
		GUI.matrix = GUIMatrices[GUIMatrices.Count-1];
		GUIMatrices.RemoveAt (GUIMatrices.Count-1);

		// End GUILayout zoomPosAdjustment
		if (adjustedGUILayout[adjustedGUILayout.Count-1])
		{
			GUILayout.EndVertical ();
			GUILayout.EndHorizontal ();
		}
		adjustedGUILayout.RemoveAt (adjustedGUILayout.Count-1);

		GUI.EndGroup ();
		
		GUIScaleUtility.RestoreClips ();
	}

	
	/// <summary>
	/// Scales the rect around the pivot with scale
	/// </summary>
	public static Rect ScaleRect (Rect rect, Vector2 pivot, Vector2 scale) 
	{
		rect.position = Vector2.Scale (rect.position - pivot, scale) + pivot;
		rect.size = Vector2.Scale (rect.size, scale);
		return rect;
	}

	// CLIPS

	public static void BeginNoClip () 
	{
		List<Rect> rectStackGroup = new List<Rect> ();
		Rect topMostClip = getTopRect;
		while (topMostClip != new Rect (-10000, -10000, 40000, 40000)) 
		{
			rectStackGroup.Add (topMostClip);
			GUI.EndClip ();
			topMostClip = getTopRect;
		}
		rectStackGroup.Reverse ();
		rectStackGroups.Add (rectStackGroup);
		currentRectStack.AddRange (rectStackGroup);
	}

	public static void MoveClipsUp (int count) 
	{
		List<Rect> rectStackGroup = new List<Rect> ();
		Rect topMostClip = getTopRect;
		while (topMostClip != new Rect (-10000, -10000, 40000, 40000) && count > 0)
		{
			rectStackGroup.Add (topMostClip);
			GUI.EndClip ();
			topMostClip = getTopRect;
			count--;
		}
		rectStackGroup.Reverse ();
		rectStackGroups.Add (rectStackGroup);
		currentRectStack.AddRange (rectStackGroup);
	}

	public static void RestoreClips () 
	{
		if (rectStackGroups.Count == 0)
		{
			Debug.LogError ("GUIClipHierarchy: BeginNoClip/MoveClipsUp - RestoreClips count not balanced!");
			return;
		}

		List<Rect> rectStackGroup = rectStackGroups[rectStackGroups.Count-1];
		for (int clipCnt = 0; clipCnt < rectStackGroup.Count; clipCnt++)
		{
			GUI.BeginClip (rectStackGroup[clipCnt]);
			currentRectStack.RemoveAt (currentRectStack.Count-1);
		}
		rectStackGroups.RemoveAt (rectStackGroups.Count-1);
	}


	// LAYOUT & MATRIX

	public static void BeginNewLayout () 
	{
		Rect topMostClip = getTopRect;
		if (topMostClip != new Rect (-10000, -10000, 40000, 40000))
			GUILayout.BeginArea (new Rect (0, 0, topMostClip.width, topMostClip.height));
		else
			GUILayout.BeginArea (new Rect (0, 0, Screen.width, Screen.height));
	}

	public static void EndNewLayout () 
	{
		GUILayout.EndArea ();
	}

	public static void BeginIgnoreMatrix () 
	{
		cachedMatrix = GUI.matrix;
		GUI.matrix = Matrix4x4.identity;
	}
	
	public static void EndIgnoreMatrix () 
	{
		GUI.matrix = cachedMatrix;
	}

//	public static Vector2 GetSizeOfCurrentGroupContent () 
//	{
//		object currentLayoutCache = currentGUILayoutCache.GetValue (null);
//		object currentTopGroup = currentTopLevelGroup.GetValue (currentLayoutCache);
//
//		IList entries = GUILayoutGroupType.GetField ("entries").GetValue (currentTopGroup) as IList;
//		float height = 0, width = 0;
//		foreach (object entry in entries) 
//		{
//			Vector2 size = new Vector2 ((float)LayoutEntryWidth.GetValue (entry), (float)LayoutEntryHeight.GetValue (entry));
//			GUIStyle style = (GUIStyle)LayoutEntryStyle.GetValue (entry, null);
//			size = style.CalcScreenSize (size);
//
//			width = Math.Max (size.x, width);
//			height = Math.Max (size.y, height);
//		}
//
//		Vector2 groupContentSize = new Vector2 (width, height);
//
//
//
//		Debug.Log (groupContentSize.ToString ());
//		return groupContentSize;
//	}


	// HELPERS

	public static Rect InnerToScreenRect (Rect innerRect) 
	{
		if (rectStackGroups.Count == 0)
			return innerRect;
		
		List<Rect> rectStackGroup = rectStackGroups[rectStackGroups.Count-1];
		for (int clipCnt = 0; clipCnt < rectStackGroup.Count; clipCnt++)
		{
			innerRect.position += rectStackGroup[clipCnt].position;
		}
		return innerRect;
	}

	public static Rect GUIToScreenRect (Rect guiRect) 
	{
		guiRect.position += getTopRectScreenSpace.position;
		return guiRect;
	}

	public static void ClipTest () 
	{
		List<Rect> Clips = new List<Rect> ();
		
		Rect topMostClip = getTopRect;
		int cnt = 0;
		while (topMostClip != new Rect (-10000, -10000, 40000, 40000)) 
		{
			cnt++;
			Clips.Add (topMostClip);
			GUI.EndClip ();
			Debug.Log ("Rect " + cnt + ": " + topMostClip.ToString ());
			topMostClip = getTopRect;
		}
		
		for (int clipCnt = Clips.Count-1; clipCnt > -1; clipCnt--) 
		{
			GUI.BeginClip (Clips[clipCnt]);
		}
	}

	public static void InspectType (string typeName) 
	{
		Assembly UnityEngine = Assembly.Load ("Assembly");
		
		Type type = UnityEngine.GetType (typeName);
		
		foreach (MemberInfo member in type.GetMembers ()) 
		{
			Debug.Log (member.MemberType.ToString () + ": " + member.ToString ());
		}
		Debug.Log ("---------------------");
		Debug.Log ("Private Members: ");
		foreach (MemberInfo member in type.GetMembers (BindingFlags.NonPublic)) 
		{
			Debug.Log (member.MemberType.ToString () + ": " + member.ToString ());
		}
	}
}
