using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace NodeEditorFramework.Utilities 
{

	public static class GUIScaleUtility
	{
		// General
		private static bool compabilityMode;
		private static bool initiated;

		// cache the reflected methods
		private static FieldInfo currentGUILayoutCache;
		private static FieldInfo currentTopLevelGroup;

		// Delegates to the reflected methods
		private static Func<Rect> GetTopRectDelegate;
		private static Func<Rect> topmostRectDelegate;

		// Delegate accessors
		public static Rect getTopRect { get { return (Rect)GetTopRectDelegate.Invoke (); } }
		public static Rect getTopRectScreenSpace { get { return (Rect)topmostRectDelegate.Invoke (); } }

		// Rect stack for manipulating groups
		public static List<Rect> currentRectStack { get; private set; }
		private static List<List<Rect>> rectStackGroups;

		// Matrices stack
		private static List<Matrix4x4> GUIMatrices;
		private static List<bool> adjustedGUILayout;

		#region Init

		public static void CheckInit () 
		{
			if (!initiated)
				Init ();
		}

		public static void Init () 
		{
			// Fetch rect acessors using Reflection
			Assembly UnityEngine = Assembly.GetAssembly (typeof (UnityEngine.GUI));
			Type GUIClipType = UnityEngine.GetType ("UnityEngine.GUIClip", true);

			PropertyInfo topmostRect = GUIClipType.GetProperty ("topmostRect", BindingFlags.Static | BindingFlags.Public);
			MethodInfo GetTopRect = GUIClipType.GetMethod ("GetTopRect", BindingFlags.Static | BindingFlags.NonPublic);
			MethodInfo ClipRect = GUIClipType.GetMethod ("Clip", BindingFlags.Static | BindingFlags.Public, Type.DefaultBinder, new Type[] { typeof(Rect) }, new ParameterModifier[] {});

			if (GUIClipType == null || topmostRect == null || GetTopRect == null || ClipRect == null) 
			{
				Debug.LogWarning ("GUIScaleUtility cannot run on this system! Compability mode enabled. For you that means you're not able to use the Node Editor inside more than one group:( Please PM me (Seneral @UnityForums) so I can figure out what causes this! Thanks!");
				Debug.LogWarning ((GUIClipType == null? "GUIClipType is Null, " : "") + (topmostRect == null? "topmostRect is Null, " : "") + (GetTopRect == null? "GetTopRect is Null, " : "") + (ClipRect == null? "ClipRect is Null, " : ""));
				compabilityMode = true;
				initiated = true;
				return;
			}

			// Create simple acessor delegates
			GetTopRectDelegate = (Func<Rect>)Delegate.CreateDelegate (typeof(Func<Rect>), GetTopRect);
			topmostRectDelegate = (Func<Rect>)Delegate.CreateDelegate (typeof(Func<Rect>), topmostRect.GetGetMethod ());

			if (GetTopRectDelegate == null || topmostRectDelegate == null)
			{
				Debug.LogWarning ("GUIScaleUtility cannot run on this system! Compability mode enabled. For you that means you're not able to use the Node Editor inside more than one group:( Please PM me (Seneral @UnityForums) so I can figure out what causes this! Thanks!");
				Debug.LogWarning ((GUIClipType == null? "GUIClipType is Null, " : "") + (topmostRect == null? "topmostRect is Null, " : "") + (GetTopRect == null? "GetTopRect is Null, " : "") + (ClipRect == null? "ClipRect is Null, " : ""));
				compabilityMode = true;
				initiated = true;
				return;
			}

			// As we can call Begin/Ends inside another, we need to save their states hierarchial in Lists (not Stack, as we need to iterate over them!):
			currentRectStack = new List<Rect> ();
			rectStackGroups = new List<List<Rect>> ();
			GUIMatrices = new List<Matrix4x4> ();
			adjustedGUILayout = new List<bool> ();

			// Sometimes, strange errors pop up (related to Mac?), which we try to catch and enable a compability Mode no supporting zooming in groups
			/*try
			{
				topmostRectDelegate.Invoke ();
			}
			catch (Exception e)
			{
				Debug.LogWarning ("GUIScaleUtility cannot run on this system! Compability mode enabled. For you that means you're not able to use the Node Editor inside more than one group:( Please PM me (Seneral @UnityForums) so I can figure out what causes this! Thanks!");
				Debug.Log (e.Message);
				compabilityMode = true;
			}*/
		
			initiated = true;
		}

		#endregion

		#region Scale Area

		public static Vector2 getCurrentScale { get { return new Vector2 (1/GUI.matrix.GetColumn (0).magnitude, 1/GUI.matrix.GetColumn (1).magnitude); } } 

		/// <summary>
		/// Begins a scaled local area. 
		/// Returns vector to offset GUI controls with to account for zooming to the pivot. 
		/// Using adjustGUILayout does that automatically for GUILayout rects. Theoretically can be nested!
		/// </summary>
		public static Vector2 BeginScale (ref Rect rect, Vector2 zoomPivot, float zoom, bool adjustGUILayout) 
		{
			Rect screenRect;
			if (compabilityMode) 
			{ // In compability mode, we will assume only one top group and do everything manually, not using reflected calls (-> practically blind)
				GUI.EndGroup ();
				screenRect = rect;
				#if UNITY_EDITOR
				if (!Application.isPlaying)
					screenRect.y += 23;
				#endif
			}
			else
			{ // If it's supported, we take the completely generic way using reflected calls
				GUIScaleUtility.BeginNoClip ();
				screenRect = GUIScaleUtility.GUIToScaledSpace (rect);
			}

			rect = Scale (screenRect, screenRect.position + zoomPivot, new Vector2 (zoom, zoom));

			// Now continue drawing using the new clipping group
			GUI.BeginGroup (rect);
			rect.position = Vector2.zero; // Adjust because we entered the new group

			// Because I currently found no way to actually scale to a custom pivot rather than (0, 0),
			// we'll make use of a cheat and just offset it accordingly to let it appear as if it would scroll to the center
			// Note, due to that, controls not adjusted are still scaled to (0, 0)
			Vector2 zoomPosAdjust = rect.center - screenRect.size/2 + zoomPivot;

			// For GUILayout, we can make this adjustment here if desired
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

		/// <summary>
		/// Ends a scale region previously opened with BeginScale
		/// </summary>
		public static void EndScale () 
		{
			// Set last matrix and clipping group
			if (GUIMatrices.Count == 0 || adjustedGUILayout.Count == 0)
				throw new UnityException ("GUIScaleUtility: You are ending more scale regions than you are beginning!");
			GUI.matrix = GUIMatrices[GUIMatrices.Count-1];
			GUIMatrices.RemoveAt (GUIMatrices.Count-1);

			// End GUILayout zoomPosAdjustment
			if (adjustedGUILayout[adjustedGUILayout.Count-1])
			{
				GUILayout.EndVertical ();
				GUILayout.EndHorizontal ();
			}
			adjustedGUILayout.RemoveAt (adjustedGUILayout.Count-1);

			// End the scaled group
			GUI.EndGroup ();

			if (compabilityMode)
			{ // In compability mode, we don't know the previous group rect, but as we cannot use top groups there either way, we restore the screen group
				if (!Application.isPlaying) // We're in an editor window
					GUI.BeginClip (new Rect (0, 23, Screen.width, Screen.height-23));
				else
					GUI.BeginClip (new Rect (0, 0, Screen.width, Screen.height));
			}
			else
			{ // Else, restore the clips (groups)
				GUIScaleUtility.RestoreClips ();
			}
		}

		#endregion

		#region Clips Hierarchy

		/// <summary>
		/// Begins a field without groups. They should be restored using RestoreClips. Can be nested!
		/// </summary>
		public static void BeginNoClip () 
		{
			// Record and close all clips one by one, from bottom to top, until we hit the 'origin'
			List<Rect> rectStackGroup = new List<Rect> ();
			Rect topMostClip = getTopRect;
			while (topMostClip != new Rect (-10000, -10000, 40000, 40000)) 
			{
				rectStackGroup.Add (topMostClip);
				GUI.EndClip ();
				topMostClip = getTopRect;
			}
			// Store the clips appropriately
			rectStackGroup.Reverse ();
			rectStackGroups.Add (rectStackGroup);
			currentRectStack.AddRange (rectStackGroup);
		}

		/// <summary>
		/// Begins a field without the last count groups. They should be restored using RestoreClips. Can be nested!
		/// </summary>
		public static void MoveClipsUp (int count) 
		{
			// Record and close all clips one by one, from bottom to top, until reached the count or hit the 'origin'
			List<Rect> rectStackGroup = new List<Rect> ();
			Rect topMostClip = getTopRect;
			while (topMostClip != new Rect (-10000, -10000, 40000, 40000) && count > 0)
			{
				rectStackGroup.Add (topMostClip);
				GUI.EndClip ();
				topMostClip = getTopRect;
				count--;
			}
			// Store the clips appropriately
			rectStackGroup.Reverse ();
			rectStackGroups.Add (rectStackGroup);
			currentRectStack.AddRange (rectStackGroup);
		}

		/// <summary>
		/// Restores the clips removed in BeginNoClip or MoveClipsUp
		/// </summary>
		public static void RestoreClips () 
		{
			if (rectStackGroups.Count == 0)
			{
				Debug.LogError ("GUIClipHierarchy: BeginNoClip/MoveClipsUp - RestoreClips count not balanced!");
				return;
			}

			// Read and restore clips one by one, from top to bottom
			List<Rect> rectStackGroup = rectStackGroups[rectStackGroups.Count-1];
			for (int clipCnt = 0; clipCnt < rectStackGroup.Count; clipCnt++)
			{
				GUI.BeginClip (rectStackGroup[clipCnt]);
				currentRectStack.RemoveAt (currentRectStack.Count-1);
			}
			rectStackGroups.RemoveAt (rectStackGroups.Count-1);
		}

		#endregion

		#region Layout & Matrix Ignores

		/// <summary>
		/// Ignores the current GUILayout cache and begins a new one. Cannot be nested!
		/// </summary>
		public static void BeginNewLayout () 
		{
			if (compabilityMode)
				return;
			// Will mimic a new layout by creating a new group at (0, 0). Will be restored though after ending the new Layout
			Rect topMostClip = getTopRect;
			if (topMostClip != new Rect (-10000, -10000, 40000, 40000))
				GUILayout.BeginArea (new Rect (0, 0, topMostClip.width, topMostClip.height));
			else
				GUILayout.BeginArea (new Rect (0, 0, Screen.width, Screen.height));
		}

		/// <summary>
		/// Ends the last GUILayout cache
		/// </summary>
		public static void EndNewLayout () 
		{
			if (!compabilityMode)
				GUILayout.EndArea ();
		}

		/// <summary>
		/// Begins an area without GUIMatrix transformations. Can be nested!
		/// </summary>
		public static void BeginIgnoreMatrix () 
		{
			// Store and clean current matrix
			GUIMatrices.Add (GUI.matrix);
			GUI.matrix = Matrix4x4.identity;
		}

		/// <summary>
		/// Restores last matrix ignored with BeginIgnoreMatrix
		/// </summary>
		public static void EndIgnoreMatrix () 
		{
			if (GUIMatrices.Count == 0)
				throw new UnityException ("GUIScaleutility: You are ending more ignoreMatrices than you are beginning!");
			// Read and assign previous matrix
			GUI.matrix = GUIMatrices[GUIMatrices.Count-1];
			GUIMatrices.RemoveAt (GUIMatrices.Count-1);
		}

		#endregion

		#region Space Transformations

		/// <summary>
		/// Scales the position around the pivot with scale
		/// </summary>
		public static Vector2 Scale (Vector2 pos, Vector2 pivot, Vector2 scale) 
		{
			return Vector2.Scale (pos - pivot, scale) + pivot;
		}

		/// <summary>
		/// Scales the rect around the pivot with scale
		/// </summary>
		public static Rect Scale (Rect rect, Vector2 pivot, Vector2 scale) 
		{
			rect.position = Vector2.Scale (rect.position - pivot, scale) + pivot;
			rect.size = Vector2.Scale (rect.size, scale);
			return rect;
		}

		/// <summary>
		/// Transforms the position from the space aquired with BeginNoClip or MoveClipsUp to it's previous space
		/// </summary>
		public static Vector2 ScaledToGUISpace (Vector2 scaledPosition) 
		{
			if (rectStackGroups == null || rectStackGroups.Count == 0)
				return scaledPosition;
			// Iterate through the clips and substract positions
			List<Rect> rectStackGroup = rectStackGroups[rectStackGroups.Count-1];
			for (int clipCnt = 0; clipCnt < rectStackGroup.Count; clipCnt++)
				scaledPosition -= rectStackGroup[clipCnt].position;
			return scaledPosition;
		}
		/// <summary>
		/// Transforms the rect from the space aquired with BeginNoClip or MoveClipsUp to it's previous space.
		/// DOES NOT scale the rect, only offsets it!
		/// </summary>
		public static Rect ScaledToGUISpace (Rect scaledRect) 
		{
			if (rectStackGroups == null || rectStackGroups.Count == 0)
				return scaledRect;
			scaledRect.position = ScaledToGUISpace (scaledRect.position);
			return scaledRect;
		}

		/// <summary>
		/// Transforms the position to the new space aquired with BeginNoClip or MoveClipsUp
		/// It's way faster to call GUIToScreenSpace before modifying the space though!
		/// </summary>
		public static Vector2 GUIToScaledSpace (Vector2 guiPosition) 
		{
			if (rectStackGroups == null || rectStackGroups.Count == 0)
				return guiPosition;
			// Iterate through the clips and add positions ontop
			List<Rect> rectStackGroup = rectStackGroups[rectStackGroups.Count-1];
			for (int clipCnt = 0; clipCnt < rectStackGroup.Count; clipCnt++)
				guiPosition += rectStackGroup[clipCnt].position;
			return guiPosition;
		}
		/// <summary>
		/// Transforms the rect to the new space aquired with BeginNoClip or MoveClipsUp.
		/// DOES NOT scale the rect, only offsets it!
		/// It's way faster to call GUIToScreenSpace before modifying the space though!
		/// </summary>
		public static Rect GUIToScaledSpace (Rect guiRect) 
		{
			if (rectStackGroups == null || rectStackGroups.Count == 0)
				return guiRect;
			guiRect.position = GUIToScaledSpace (guiRect.position);
			return guiRect;
		}

		/// <summary>
		/// Transforms the position to screen space.
		/// You can use GUIToScaledSpace when you want to transform an old rect to the new space aquired with BeginNoClip or MoveClipsUp (slower, try to call this function before any of these two)!
		/// ATTENTION: This does not work well when any of the top groups is negative, means extends to the top or left of the screen. You may consider to use GUIToScaledSpace then, if possible!
		/// </summary>
		public static Vector2 GUIToScreenSpace (Vector2 guiPosition) 
		{
			#if UNITY_EDITOR
			if (!Application.isPlaying)
				return guiPosition + getTopRectScreenSpace.position - new Vector2 (0, 22);
			#endif
			return guiPosition + getTopRectScreenSpace.position;
		}

		/// <summary>
		/// Transforms the rect to screen space.
		/// You can use GUIToScaledSpace when you want to transform an old rect to the new space aquired with BeginNoClip or MoveClipsUp (slower, try to call this function before any of these two)!
		/// ATTENTION: This does not work well when any of the top groups is negative, means extends to the top or left of the screen. You may consider to use GUIToScaledSpace then, if possible!
		/// </summary>
		public static Rect GUIToScreenSpace (Rect guiRect) 
		{
			guiRect.position += getTopRectScreenSpace.position;
			#if UNITY_EDITOR
			if (!Application.isPlaying)
				guiRect.y -= 22;
			#endif
			return guiRect;
		}

		#endregion
	}
}
