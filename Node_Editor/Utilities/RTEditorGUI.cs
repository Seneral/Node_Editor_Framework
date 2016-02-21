using UnityEngine;
using System;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;

using Object = UnityEngine.Object;

namespace NodeEditorFramework.Utilities 
{
	public static class RTEditorGUI 
	{
		/// <summary>
		/// Float Field with label for ingame purposes. Behaves like UnityEditor.EditorGUILayout.TextField
		/// </summary>
		public static string TextField (GUIContent label, string text)
		{
		#if UNITY_EDITOR
			if (!Application.isPlaying)
				return UnityEditor.EditorGUILayout.TextField (label, text);
		#endif
			GUILayout.BeginHorizontal ();
			GUILayout.Label (label, label != GUIContent.none? GUILayout.ExpandWidth (true) : GUILayout.ExpandWidth (false));
			text = GUILayout.TextField (text);
			GUILayout.EndHorizontal ();
			return text;
		}

		#region Slider Extended

		/// <summary>
		/// A slider that emulates the EditorGUILayout version. 
		/// HorizontalSlider with an additional float field thereafter.
		/// </summary>
		public static float Slider (float value, float minValue, float maxValue, params GUILayoutOption[] sliderOptions) 
		{
		#if UNITY_EDITOR
			if (!Application.isPlaying)
				return UnityEditor.EditorGUILayout.Slider (value, minValue, maxValue, sliderOptions);
		#endif
			return Slider (GUIContent.none, value, minValue, maxValue, sliderOptions);
		}

		/// <summary>
		/// A slider that emulates the EditorGUILayout version. 
		/// HorizontalSlider with a label prefixed and an additional float field thereafter if desired.
		/// </summary>
		public static float Slider (GUIContent label, float value, float minValue, float maxValue, params GUILayoutOption[] sliderOptions) 
		{
		#if UNITY_EDITOR
			if (!Application.isPlaying)
				return UnityEditor.EditorGUILayout.Slider (label, value, minValue, maxValue, sliderOptions);
		#endif
			GUILayout.BeginHorizontal ();
			if (label != GUIContent.none)
				GUILayout.Label (label, GUILayout.ExpandWidth (true));
			value = GUILayout.HorizontalSlider (value, minValue, maxValue, sliderOptions);
			value = Mathf.Min (maxValue, Mathf.Max (minValue, FloatField (value)));
			GUILayout.EndHorizontal ();
			return value;
		}

		/// <summary>
		/// An integer slider that emulates the EditorGUILayout version. 
		/// HorizontalSlider with a label prefixed and an additional int field thereafter if desired.
		/// </summary>
		public static int IntSlider (GUIContent label, int value, int minValue, int maxValue, params GUILayoutOption[] sliderOptions) 
		{
			return (int)Slider (label, value, minValue, maxValue, sliderOptions);
		}

		/// <summary>
		/// An integer slider that emulates the EditorGUILayout version. 
		/// HorizontalSlider with a label prefixed and an additional int field thereafter if desired.
		/// </summary>
		public static int IntSlider (int value, int minValue, int maxValue, params GUILayoutOption[] sliderOptions) 
		{
			return (int)Slider (GUIContent.none, value, minValue, maxValue, sliderOptions);
		}

		#endregion

		#region FloatField

		private static int activeFloatField = -1;
		private static float activeFloatFieldLastValue = 0;
		private static string activeFloatFieldString = "";

		/// <summary>
		/// Float Field for ingame purposes. Behaves exactly like UnityEditor.EditorGUILayout.FloatField, besides the label slide field
		/// </summary>
		public static float FloatField (GUIContent label, float value, params GUILayoutOption[] fieldOptions)
		{
			GUILayout.BeginHorizontal ();
			if (label != GUIContent.none)
				GUILayout.Label (label, GUILayout.ExpandWidth (true));
			value = FloatField (value, fieldOptions);
			GUILayout.EndHorizontal ();
			return value;
		}

		/// <summary>
		/// Float Field for ingame purposes. Behaves exactly like UnityEditor.EditorGUILayout.FloatField
		/// </summary>
		public static float FloatField (float value, params GUILayoutOption[] fieldOptions)
		{
			// Get rect and control for this float field for identification
			if (fieldOptions.Length == 0)
				fieldOptions = new GUILayoutOption[] { GUILayout.ExpandWidth (false), GUILayout.MinWidth (50) };
			Rect pos = GUILayoutUtility.GetRect (new GUIContent (value.ToString ()), GUI.skin.label, fieldOptions);

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

			string strValue = GUI.TextField (pos, str);
			if (recorded)
				activeFloatFieldString = strValue;

			// Try Parse if value got changed. If the string could not be parsed, ignore it and keep last value
			bool parsed = true;
			if (strValue == "")
				value = activeFloatFieldLastValue = 0;
			else if (strValue != value.ToString ())
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

		#endregion

		#region ObjectField

		/// <summary>
		/// Provides an object field both for editor (using default) and for runtime (not yet implemented other that displaying object)
		/// </summary>
		public static T ObjectField<T> (T obj, bool allowSceneObjects) where T : Object
		{
			return ObjectField<T> (GUIContent.none, obj, allowSceneObjects);
		}

		/// <summary>
		/// Provides an object field both for editor (using default) and for runtime (not yet implemented other that displaying object)
		/// </summary>
		public static T ObjectField<T> (GUIContent label, T obj, bool allowSceneObjects) where T : Object
		{
			#if UNITY_EDITOR
			if (!Application.isPlaying)
				return UnityEditor.EditorGUILayout.ObjectField (GUIContent.none, obj, typeof (T), allowSceneObjects) as T;
			#endif	
			throw new System.NotImplementedException ();
//		if (Application.isPlaying)
//		{
//			bool open = false;
//			if (typeof(T).Name == "UnityEngine.Texture2D") 
//			{
//				label.image = obj as Texture2D;
//				GUIStyle style = new GUIStyle (GUI.skin.box);
//				style.imagePosition = ImagePosition.ImageAbove;
//				open = GUILayout.Button (label, style);
//			}
//			else
//			{
//				GUIStyle style = new GUIStyle (GUI.skin.box);
//				open = GUILayout.Button (label, style);
//			}
//			if (open)
//			{
//				Debug.Log ("Selecting Object!");
//			}
//		}
//		return obj;
		}

		#endregion

		#region Popups

		// TODO: Implement RT Popup

		public static System.Enum EnumPopup (GUIContent label, System.Enum selected) 
		{
			#if UNITY_EDITOR
			selected = UnityEditor.EditorGUILayout.EnumPopup (label, selected);
			#else
			label.text += ": " + selected.ToString ();
			GUILayout.Label (label);
			#endif
			return selected;
		}

		public static System.Enum EnumPopup (string label, System.Enum selected) 
		{
			#if UNITY_EDITOR
			selected = UnityEditor.EditorGUILayout.EnumPopup (label, selected);
			#else
			GUILayout.Label (label + ": " + selected.ToString ());
			#endif
			return selected;
		}

		public static System.Enum EnumPopup (System.Enum selected) 
		{
			#if UNITY_EDITOR
			selected = UnityEditor.EditorGUILayout.EnumPopup (selected);
			#else
			GUILayout.Label (selected.ToString ());
			#endif
			return selected;
		}

		public static int Popup (GUIContent label, int selected, string[] displayedOptions) 
		{
			GUILayout.BeginHorizontal ();
			#if UNITY_EDITOR
			GUILayout.Label (label);
			selected = UnityEditor.EditorGUILayout.Popup (selected, displayedOptions);
			#else
			label.text += ": " + selected.ToString ();
			GUILayout.Label (label);
			#endif
			GUILayout.EndHorizontal ();
			return selected;
		}

		public static int Popup (string label, int selected, string[] displayedOptions) 
		{
			#if UNITY_EDITOR
			selected = UnityEditor.EditorGUILayout.Popup (label, selected, displayedOptions);
			#else
			GUILayout.Label (label + ": " + selected.ToString ());
			#endif
			return selected;
		}

		public static int Popup (int selected, string[] displayedOptions) 
		{
			#if UNITY_EDITOR
			selected = UnityEditor.EditorGUILayout.Popup (selected, displayedOptions);
			#else
			GUILayout.Label (selected.ToString ());
			#endif
			return selected;
		}

		#endregion

		#region Seperator

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

		private static GUIStyle seperator;
		private static void setupSeperator () 
		{
			if (seperator == null) 
			{
				seperator = new GUIStyle();
				seperator.normal.background = ColorToTex (1, new Color (0.6f, 0.6f, 0.6f));
				seperator.stretchWidth = true;
				seperator.margin = new RectOffset(0, 0, 7, 7);
			}
		}

		#endregion

		#region Low-Level Drawing

		private static Material lineMaterial;
		private static Texture2D lineTexture;

		private static void SetupLineMat (Texture tex, Color col) 
		{
			if (lineMaterial == null)
				lineMaterial = new Material (Shader.Find ("Hidden/LineShader"));
			if (tex == null)
				tex = lineTexture != null? lineTexture : lineTexture = NodeEditorFramework.Utilities.ResourceManager.LoadTexture ("Textures/AALine.png");
			lineMaterial.SetTexture ("_LineTexture", tex);
			lineMaterial.SetColor ("_LineColor", col);
			lineMaterial.SetPass (0);
		}

		/// <summary>
		/// Draws a Bezier curve just as UnityEditor.Handles.DrawBezier, non-clipped. If width is 1, tex is ignored; Else if tex is null, a anti-aliased texture tinted with col will be used; else, col is ignored and tex is used.
		/// </summary>
		public static void DrawBezier (Vector2 startPos, Vector2 endPos, Vector2 startTan, Vector2 endTan, Color col, Texture2D tex, float width = 1)
		{
			if (Event.current.type != EventType.Repaint)
				return;

			// Own Bezier Formula
			// Slower than handles because of the horrendous amount of calls into the native api

			// Calculate optimal segment count
			int segmentCount = CalculateBezierSegmentCount (startPos, endPos, startTan, endTan);
			// Draw bezier with calculated segment count
			DrawBezier (startPos, endPos, startTan, endTan, col, tex, segmentCount, width);
		}

		/// <summary>
		/// Draws a clipped Bezier curve just as UnityEditor.Handles.DrawBezier.
		/// If width is 1, tex is ignored; Else if tex is null, a anti-aliased texture tinted with col will be used; else, col is ignored and tex is used.
		/// </summary>
		public static void DrawBezier (Vector2 startPos, Vector2 endPos, Vector2 startTan, Vector2 endTan, Color col, Texture2D tex, int segmentCount, float width)
		{
			if (Event.current.type != EventType.Repaint && Event.current.type != EventType.KeyDown)
				return;

			// Own Bezier Formula
			// Slower than handles because of the horrendous amount of calls into the native api

			// Calculate bezier points
			Vector2[] bezierPoints = new Vector2[segmentCount+1];
			for (int pointCnt = 0; pointCnt <= segmentCount; pointCnt++) 
				bezierPoints[pointCnt] = GetBezierPoint ((float)pointCnt/segmentCount, startPos, endPos, startTan, endTan);
			// Draw polygon line from the bezier points
			DrawPolygonLine (bezierPoints, col, tex, width);
		}

		/// <summary>
		/// Draws a clipped polygon line from the given points. 
		/// If width is 1, tex is ignored; Else if tex is null, a anti-aliased texture tinted with col will be used; else, col is ignored and tex is used.
		/// </summary>
		public static void DrawPolygonLine (Vector2[] points, Color col, Texture2D tex, float width = 1)
		{
			if (Event.current.type != EventType.Repaint && Event.current.type != EventType.KeyDown)
				return;

			// Simplify basic cases
			if (points.Length == 1)
				return;
			else if (points.Length == 2)
				DrawLine (points[0], points[1], col, tex, width);

			// Setup for drawing
			SetupLineMat (tex, col);
			GL.Begin (GL.TRIANGLE_STRIP);
			GL.Color (Color.white);

			// Fetch clipping rect
			Rect clippingRect = GUIScaleUtility.getTopRect;
			clippingRect.x = clippingRect.y = 0;

			Vector2 curPoint = points[0], nextPoint, perpendicular;
			bool clippedP0, clippedP1;
			for (int pointCnt = 1; pointCnt < points.Length; pointCnt++) 
			{
				nextPoint = points[pointCnt];

				// Clipping test
				Vector2 curPointOriginal = curPoint, nextPointOriginal = nextPoint;
				if (SegmentRectIntersection (clippingRect, ref curPoint, ref nextPoint, out clippedP0, out clippedP1))
				{ // (partially) visible
					// Calculate apropriate perpendicular
					if (pointCnt < points.Length-1) // Interpolate perpendicular inbetween the point chain
						perpendicular = CalculatePointPerpendicular (curPointOriginal, nextPointOriginal, points[pointCnt+1]);
					else // At the end, there's no further point to interpolate the perpendicular from
						perpendicular = CalculateLinePerpendicular (curPointOriginal, nextPointOriginal);

					if (clippedP0)
					{ // Just became visible, so enable GL again and draw the clipped line start point
						GL.End();
						GL.Begin (GL.TRIANGLE_STRIP);
						DrawLineSegment (curPoint, perpendicular * width/2);
					}

					// Draw first point before starting with the point chain. Placed here instead of before because of clipping
					if (pointCnt == 1)
						DrawLineSegment (curPoint, CalculateLinePerpendicular (curPoint, nextPoint) * width/2);
					// Draw the actual point
					DrawLineSegment (nextPoint, perpendicular * width/2);
				}
				else if (clippedP1)
				{ // Just became invisible, so disable GL
					GL.End ();
					GL.Begin (GL.TRIANGLE_STRIP);
				}

				// Update state variable
				curPoint = nextPointOriginal;
			}
			// Finalize drawing
			GL.End ();
		}

		/// <summary>
		/// Calculates the optimal bezier segment count for the given bezier
		/// </summary>
		private static int CalculateBezierSegmentCount (Vector2 startPos, Vector2 endPos, Vector2 startTan, Vector2 endTan) 
		{
			float straightFactor = Vector2.Angle (startTan-startPos, endPos-startPos) * Vector2.Angle (endTan-endPos, startPos-endPos) * (endTan.magnitude+startTan.magnitude);
			straightFactor = 2 + Mathf.Pow (straightFactor / 400, 1.0f/8);
			float distanceFactor = 1 + (startPos-endPos).magnitude;
			distanceFactor = Mathf.Pow (distanceFactor, 1.0f/4);
			return 4 + (int)(straightFactor * distanceFactor);
		}

		/// <summary>
		/// Calculates the normalized perpendicular vector of the give line
		/// </summary>
		private static Vector2 CalculateLinePerpendicular (Vector2 startPos, Vector2 endPos) 
		{
			return new Vector2 (endPos.y-startPos.y, startPos.x-endPos.x).normalized;
		}

		/// <summary>
		/// Calculates the normalized perpendicular vector for the pointPos interpolated with its two neighbours prevPos and nextPos
		/// </summary>
		private static Vector2 CalculatePointPerpendicular (Vector2 prevPos, Vector2 pointPos, Vector2 nextPos) 
		{
			return CalculateLinePerpendicular (pointPos, pointPos + (nextPos-prevPos));
		}

		/// <summary>
		/// Gets the point of the bezier at t
		/// </summary>
		private static Vector2 GetBezierPoint (float t, Vector2 startPos, Vector2 endPos, Vector2 startTan, Vector2 endTan) 
		{
			float rt = 1 - t;
			float rtt = rt * t;

			return startPos  * rt*rt*rt + 
					startTan * 3 * rt * rtt + 
					endTan   * 3 * rtt * t + 
					endPos   * t*t*t;
		}

		/// <summary>
		/// Adds a line sgement to the GL buffer. Useed in a row to create a line
		/// </summary>
		private static void DrawLineSegment (Vector2 point, Vector2 perpendicular) 
		{
			GL.TexCoord2 (0, 0);
			GL.Vertex (point - perpendicular);
			GL.TexCoord2 (0, 1);
			GL.Vertex (point + perpendicular);
		}

		/// <summary>
		/// Draws a non-clipped line. If tex is null, a anti-aliased texture tinted with col will be used; else, col is ignored and tex is used.
		/// </summary>
		public static void DrawLine (Vector2 startPos, Vector2 endPos, Color col, Texture2D tex, float width = 1)
		{
			if (Event.current.type != EventType.Repaint)
				return;

			// Setup
			SetupLineMat (tex, col);
			GL.Begin (GL.TRIANGLE_STRIP);
			GL.Color (Color.white);
			// Fetch clipping rect
			Rect clippingRect = GUIScaleUtility.getTopRect;
			clippingRect.x = clippingRect.y = 0;
			// Clip to rect
			if (SegmentRectIntersection (clippingRect, ref startPos, ref endPos))
			{ // Draw with clipped line if it is visible
				Vector2 perpWidthOffset = CalculateLinePerpendicular (startPos, endPos) * width / 2;
				DrawLineSegment (startPos, perpWidthOffset);
				DrawLineSegment (endPos, perpWidthOffset);
			}
			// Finalize drawing
			GL.End ();
		}

		/// <summary>
		/// Clips the line between the points p1 and p2 to the bounds rect.
		/// Uses Liang-Barsky Line Clipping Algorithm.
		/// </summary>
		private static bool SegmentRectIntersection(Rect bounds, ref Vector2 p0, ref Vector2 p1)
		{
			bool cP0, cP1;
			return SegmentRectIntersection (bounds, ref p0, ref p1, out cP0, out cP1);
		}


		/// <summary>
		/// Clips the line between the points p1 and p2 to the bounds rect.
		/// Uses Liang-Barsky Line Clipping Algorithm.
		/// </summary>
		private static bool SegmentRectIntersection (Rect bounds, ref Vector2 p0, ref Vector2 p1, out bool clippedP0, out bool clippedP1)
		{
			float t0 = 0.0f;
			float t1 = 1.0f;
			float dx = p1.x - p0.x;
			float dy = p1.y - p0.y;

			if (ClipTest (-dx, p0.x - bounds.xMin, ref t0, ref t1)) // Left
			{
				if (ClipTest (dx, bounds.xMax - p0.x, ref t0, ref t1)) // Right
				{
					if (ClipTest (-dy, p0.y - bounds.yMin, ref t0, ref t1)) // Bottom
					{
						if (ClipTest (dy, bounds.yMax - p0.y, ref t0, ref t1)) // Top
						{
							clippedP0 = t0 > 0;
							clippedP1 = t1 < 1;

							if (clippedP1)
							{
								p1.x = p0.x + t1 * dx;
								p1.y = p0.y + t1 * dy;
							}

							if (clippedP0)
							{
								p0.x = p0.x + t0 * dx;
								p0.y = p0.y + t0 * dy;
							}

							return true;
						}
					}
				}
			}

			clippedP1 = clippedP0 = true;
			return false;
		}

		/// <summary>
		/// Liang-Barsky Line Clipping Test
		/// </summary>
		private static bool ClipTest(float p, float q, ref float t0, ref float t1)
		{
			float u = q / p;

			if (p < 0.0f)
			{
				if (u > t1)
					return false;
				if (u > t0)
					t0 = u;
			}
			else if (p > 0.0f)
			{
				if (u < t0)
					return false;
				if (u < t1)
					t1 = u;
			}
			else if (q < 0.0f)
				return false;

			return true;
		}

		#endregion
		
		#region Texture Utilities
		
		/// <summary>
		/// Create a 1x1 tex with color col
		/// </summary>
		public static Texture2D ColorToTex (int pxSize, Color col) 
		{
			Texture2D tex = new Texture2D (pxSize, pxSize);
			for (int x = 0; x < pxSize; x++) 
				for (int y = 0; y < pxSize; y++) 
					tex.SetPixel (x, y, col);
			tex.Apply ();
			return tex;
		}
		
		/// <summary>
		/// Tint the texture with the color. It's advised to use ResourceManager.GetTintedTexture to account for doubles.
		/// </summary>
		public static Texture2D Tint (Texture2D tex, Color color) 
		{
			Texture2D tintedTex = UnityEngine.Object.Instantiate (tex);
			for (int x = 0; x < tex.width; x++) 
				for (int y = 0; y < tex.height; y++) 
					tintedTex.SetPixel (x, y, tex.GetPixel (x, y) * color);
			tintedTex.Apply ();
			return tintedTex;
		}
		
		/// <summary>
		/// Rotates the texture Counter-Clockwise, 'quarterSteps' specifying the times
		/// </summary>
		public static Texture2D RotateTextureCCW (Texture2D tex, int quarterSteps) 
		{
			if (tex == null)
				return null;
			// Copy and setup working arrays
			tex = UnityEngine.Object.Instantiate (tex);
			int width = tex.width, height = tex.height;
			Color[] col = tex.GetPixels ();
			Color[] rotatedCol = new Color[width*height];
			for (int itCnt = 0; itCnt < quarterSteps; itCnt++) 
			{ // For each iteration, perform rotation of 90 degrees
				for (int x = 0; x < width; x++)
					for (int y = 0; y < height; y++)
						rotatedCol[x*width + y] = col[(width-y-1) * width + x];
				rotatedCol.CopyTo (col, 0); // Push rotation for next iteration
			}
			// Apply rotated working arrays
			tex.SetPixels (col);
			tex.Apply ();
			return tex;
		}
		
		#endregion
	}
}