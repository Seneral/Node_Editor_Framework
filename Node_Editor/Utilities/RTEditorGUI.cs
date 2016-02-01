using UnityEngine;
using System;
using System.Globalization;
using System.Collections.Generic;

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

		#region FloatField

		private static int activeFloatField = -1;
		private static float activeFloatFieldLastValue;
		private static string activeFloatFieldString = "";

		/// <summary>
		/// Float Field for ingame purposes. Behaves exactly like UnityEditor.EditorGUILayout.FloatField, besides the label slide field
		/// </summary>
		public static float FloatField (GUIContent label, float value)
		{
		#if UNITY_EDITOR
			if (!Application.isPlaying)
				return UnityEditor.EditorGUILayout.FloatField (label, value);
		#endif
			GUILayout.BeginHorizontal ();
			GUILayout.Label (label, label != GUIContent.none? GUILayout.ExpandWidth (true) : GUILayout.ExpandWidth (false));
			value = FloatField (value);
			GUILayout.EndHorizontal ();
			return value;
		}

		/// <summary>
		/// Float Field for ingame purposes. Behaves exactly like UnityEditor.EditorGUILayout.FloatField
		/// </summary>
		public static float FloatField (float value)
		{
		#if UNITY_EDITOR
			if (!Application.isPlaying)
				return UnityEditor.EditorGUILayout.FloatField (value);
		#endif

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
		public static T ObjectField<T> (T obj, bool allowSceneObjects) where T : UnityEngine.Object
		{
		#if UNITY_EDITOR
			if (!Application.isPlaying)
				return UnityEditor.EditorGUILayout.ObjectField (obj, typeof (T), allowSceneObjects) as T;
		#endif	
			return ObjectField<T> (GUIContent.none, obj, allowSceneObjects);
		}

		/// <summary>
		/// Provides an object field both for editor (using default) and for runtime (not yet implemented other that displaying object)
		/// </summary>
		public static T ObjectField<T> (GUIContent label, T obj, bool allowSceneObjects) where T : UnityEngine.Object
		{
		#if UNITY_EDITOR
			if (!Application.isPlaying)
				return UnityEditor.EditorGUILayout.ObjectField (label, obj, typeof (T), allowSceneObjects) as T;
		#endif
			throw new NotImplementedException ();
//			bool open = false;
//			if ((obj as Texture2D) != null) 
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
//			return obj;
		}

		#endregion

		#region Seperator

		/// <summary>
		/// A GUI Function which simulates the default seperator
		/// </summary>
		public static void Seperator () 
		{
			SetupSeperator ();
			GUILayout.Box (GUIContent.none, seperator, new GUILayoutOption[] { GUILayout.Height (1) });
		}

		/// <summary>
		/// A GUI Function which simulates the default seperator
		/// </summary>
		public static void Seperator (Rect rect) 
		{
			SetupSeperator ();
			GUI.Box (new Rect (rect.x, rect.y, rect.width, 1), GUIContent.none, seperator);
		}

		private static GUIStyle seperator;
		private static void SetupSeperator () 
		{
			if (seperator == null) 
			{
			    seperator = new GUIStyle
			    {
			        normal = {background = ColorToTex(1, new Color(0.6f, 0.6f, 0.6f))},
			        stretchWidth = true,
			        margin = new RectOffset(0, 0, 7, 7)
			    };
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
		/// Draws a Bezier curve just as UnityEditor.Handles.DrawBezier, non-clipped, with width of 1
		/// </summary>
		public static void DrawBezier (Vector2 startPos, Vector2 endPos, Vector2 startTan, Vector2 endTan, Color col)
		{
			if (Event.current.type != EventType.Repaint)
				return;
			
			if (!Application.isPlaying)
			{
				#if UNITY_EDITOR
				UnityEditor.Handles.DrawBezier (startPos, endPos, startTan, endTan, col, null, 1);
				return;
				#endif
			}

			// Own Bezier Formula; Slower than handles because of the horrendous amount of calls into the native api
			// Setup:
			GL.Begin (GL.LINES);
			GL.Color (col);
			// Calculate optimal segment count:
			int segmentCount = CalculateBezierSegmentCount (startPos, endPos, startTan, endTan);
			// Caluclate and draw segments:
			Vector2 curPoint = startPos;
			for (int segCnt = 1; segCnt <= segmentCount; segCnt++) 
			{
				Vector2 nextPoint = GetBezierPoint ((float)segCnt/segmentCount, startPos, endPos, startTan, endTan);
				GL.Vertex (curPoint);
				GL.Vertex (nextPoint);
				curPoint = nextPoint;
			}
			// Finish loop and finalize drawing
			GL.Vertex (curPoint);
			GL.Vertex (endPos);
			GL.End ();
			GL.Color (Color.white);
		}

		/// <summary>
		/// Draws a Bezier curve just as UnityEditor.Handles.DrawBezier, non-clipped. If width is 1, tex is ignored; Else if tex is null, a anti-aliased texture tinted with col will be used; else, col is ignored and tex is used.
		/// </summary>
		public static void DrawBezier (Vector2 startPos, Vector2 endPos, Vector2 startTan, Vector2 endTan, Color col, Texture2D tex, float width)
		{
			if (Event.current.type != EventType.Repaint)
				return;

			if (!Application.isPlaying)
			{
			#if UNITY_EDITOR
				UnityEditor.Handles.DrawBezier (startPos, endPos, startTan, endTan, col, tex, width);
				return;
			#endif
			}

			if (width == 1)
			{
				DrawBezier (startPos, endPos, startTan, endTan, col);
				return;
			}

			// Own Bezier Formula
			// Slower than handles because of the horrendous amount of calls into the native api

			// Aproximate Bounds and clip

			// Setup
			SetupLineMat (tex, col);
			GL.Begin (GL.TRIANGLE_STRIP);
			GL.Color (Color.white);

			// Calculate optimal segment count
			int segmentCount = CalculateBezierSegmentCount (startPos, endPos, startTan, endTan);
			// Caluclate and draw segments:
			Vector2 curPoint = startPos;
			for (int segCnt = 1; segCnt <= segmentCount; segCnt++) 
			{
				Vector2 nextPoint = GetBezierPoint ((float)segCnt/segmentCount, startPos, endPos, startTan, endTan);
				DrawLineSegment (curPoint, new Vector2 (nextPoint.y-curPoint.y, curPoint.x-nextPoint.x).normalized * width/2);
				curPoint = nextPoint;
			}
			// Finish loop and finalize drawing
			DrawLineSegment (curPoint, new Vector2 (endTan.y, -endTan.x).normalized * width/2);
			GL.End ();
			GL.Color (Color.white);
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
		/// Gets the point of the bezier at t
		/// </summary>
		public static Vector2 GetBezierPoint (float t, Vector2 startPos, Vector2 endPos, Vector2 startTan, Vector2 endTan) 
		{
			return 	startPos * Mathf.Pow (1-t, 3) + 
					startTan * 3 * Mathf.Pow (1-t, 2) * t + 
					endTan	 * 3 * (1-t) * Mathf.Pow (t, 2) + 
					endPos	 * Mathf.Pow (t, 3);	
		}

		/// <summary>
		/// Adds a line sgement to the GL buffer. Useed in a row to create a line
		/// </summary>
		private static void DrawLineSegment (Vector2 point, Vector2 perpendicular) 
		{
			Vector2 straight = new Vector2 (perpendicular.y, -perpendicular.x) * 2;
			GL.TexCoord2 (0, 0);
			GL.Vertex (point-straight - perpendicular);
			GL.TexCoord2 (0, 1);
			GL.Vertex (point-straight + perpendicular);
			// Showcase line segmentation
			//			GL.TexCoord2 (0, 0);
			//			GL.Vertex (point - perpendicular*2);
			//			GL.TexCoord2 (0, 1);
			//			GL.Vertex (point + perpendicular*2);
			//
			//			GL.TexCoord2 (0, 0);
			//			GL.Vertex (point+straight - perpendicular);
			//			GL.TexCoord2 (0, 1);
			//			GL.Vertex (point+straight + perpendicular);
		}

		/// <summary>
		/// Draws a non-clipped line. If tex is null, a anti-aliased texture tinted with col will be used; else, col is ignored and tex is used.
		/// </summary>
		public static void DrawLine (Vector2 startPos, Vector2 endPos, Color col, Texture2D tex, float width)
		{
			if (Event.current.type != EventType.Repaint)
				return;
			
			if (width <= 1)
			{
				GL.Begin (GL.LINES);
				GL.Color (col);
				GL.Vertex (startPos);
				GL.Vertex (endPos);
				GL.End ();
				GL.Color (Color.white);
			}
			else 
			{
				SetupLineMat (tex, col);

				GL.Begin (GL.TRIANGLE_STRIP);
				GL.Color (Color.white);
				Vector2 perpWidthOffset = new Vector2 ((endPos-startPos).y, -(endPos-startPos).x).normalized * width / 2;
				DrawLineSegment (startPos, perpWidthOffset);
				DrawLineSegment (endPos, perpWidthOffset);
				GL.End ();
			}
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
			var tintedTex = UnityEngine.Object.Instantiate (tex);
			for (var x = 0; x < tex.width; x++) 
				for (var y = 0; y < tex.height; y++) 
					tintedTex.SetPixel (x, y, tex.GetPixel (x, y) * color);
			tintedTex.Apply ();
			return tintedTex;
		}
		
		/// <summary>
		/// Rotates the texture Anti-Clockwise, 'NintyDegrSteps' specifying the times
		/// </summary>
		public static Texture2D RotateTextureAntiCW (Texture2D tex, int NintyDegrSteps) 
		{
			if (tex == null)
				return null;
			tex = UnityEngine.Object.Instantiate (tex);
			int width = tex.width, height = tex.height;
			Color[] col = tex.GetPixels ();
			Color[] rotatedCol = new Color[width*height];
			for (int itCnt = 0; itCnt < NintyDegrSteps; itCnt++) 
			{
				for (int x = 0; x < width; x++) 
				{
					for (int y = 0; y < height; y++) 
					{
						rotatedCol[x*width + y] = col[(width-y-1) * width + x];
					}
				}
				if (itCnt < NintyDegrSteps-1)
				{
					col = rotatedCol;
					rotatedCol = new Color[width*height];
				}
			}
			tex.SetPixels (rotatedCol);
			tex.Apply ();
			return tex;
		}
		
		#endregion
	}
}