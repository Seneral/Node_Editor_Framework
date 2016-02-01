using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework 
{
	public enum NodeSide { Left = 4, Top = 3, Right = 2, Bottom = 1 }

	public abstract class NodeKnob : ScriptableObject
	{
		// Main
		public Node Body;
		public string Type;
		public TypeData TypeData;

		// Style
		protected abstract GUIStyle DefaultLabelStyle { get; }
		protected string TexturePath;
		[NonSerialized]
		internal Texture2D KnobTexture;
		
		// Position
		protected abstract NodeSide DefaultSide { get; }
		public NodeSide Side;
		public float SidePosition; // Position on the side, top->bottom, left->right
		public float SideOffset = 0; // Offset from the side

		protected abstract void ReloadType ();

		private void Check () 
		{
			if (Side == 0)
				Side = DefaultSide;
			if (KnobTexture == null)
				ReloadKnobTexture ();
		}

		protected void ReloadKnobTexture () 
		{
			ReloadType ();
			
			if (Side != DefaultSide) 
			{ // Rotate Knob texture according to the side it's used on
				int rotationSteps = GetRotationStepsAntiCW (DefaultSide, Side);

				ResourceManager.MemoryTexture memoryTex = ResourceManager.FindInMemory (KnobTexture);
				List<string> mods = memoryTex.Modifications.ToList ();
				mods.Add ("Rotation:" + rotationSteps);

				Texture2D knobTextureInMemory = ResourceManager.GetTexture (TexturePath, mods.ToArray ());

				if (knobTextureInMemory != null)
					KnobTexture = knobTextureInMemory;
				else 
				{
					KnobTexture = RTEditorGUI.RotateTextureAntiCW (KnobTexture, rotationSteps);
					ResourceManager.AddTexture (TexturePath, KnobTexture, mods.ToArray ());
				}
			}
		}

		#region GUI and Position calls

		/// <summary>
		/// Automatically draw the output with it's name and set the knob next to it at the current side.
		/// </summary>
		public void DisplayLayout () 
		{
			DisplayLayout (new GUIContent (name), DefaultLabelStyle);
		}
		
		public void DisplayLayout (GUIStyle style)
		{
			DisplayLayout (new GUIContent (name), style);
		}

		/// <summary>
		/// Automatically draw the output with the specified label and set the knob next to it at the current side.
		/// </summary>
		public void DisplayLayout (GUIContent content) 
		{
			DisplayLayout (content, DefaultLabelStyle);
		}

		/// <summary>
		/// Automatically draw the output with the specified label and it's style and set the knob next to it at the current side.
		/// </summary>
		public void DisplayLayout (GUIContent content, GUIStyle style)
		{
			GUILayout.Label (content, style);
			if (Event.current.type == EventType.Repaint)
				SetPosition ();
		}
		
		/// <summary>
		/// Set the knob position at the specified side, from Top->Bottom and Left->Right
		/// </summary>
		public void SetPosition (float position, NodeSide nodeSide) 
		{
			if (Side != nodeSide) 
			{
				Side = nodeSide;
				ReloadKnobTexture ();
			}
			SetPosition (position);
		}
		
		/// <summary>
		/// Set the knob position at the current side, from Top->Bottom and Left->Right
		/// </summary>
		public void SetPosition (float position) 
		{
			SidePosition = position;
		}
		
		/// <summary>
		/// Set the knob position at the current side next to the last Layout control
		/// </summary>
		public void SetPosition () 
		{
			Vector2 pos = GUILayoutUtility.GetLastRect ().center + Body.ContentOffset;
			SidePosition = Side == NodeSide.Bottom || Side == NodeSide.Top? pos.x : pos.y;
		}

		#endregion
		
		#region Position requests

		/// <summary>
		/// Get the Knob rect in GUI space, NOT ZOOMED
		/// </summary>
		public Rect GetGUIKnob () 
		{
			Check ();
			Vector2 knobSize = new Vector2 ((KnobTexture.width/KnobTexture.height) * NodeEditorGUI.KnobSize,
											(KnobTexture.height/KnobTexture.width) * NodeEditorGUI.KnobSize);
			Vector2 knobCenter = new Vector2 (Body.Rect.x + (Side == NodeSide.Bottom || Side == NodeSide.Top? 
						/* Top | Bottom */	SidePosition :
									(Side == NodeSide.Left? 
						/* Left */			-SideOffset-knobSize.x/2 : 
						/* Right */			Body.Rect.width+SideOffset+knobSize.x/2
									)),
									Body.Rect.y + (Side == NodeSide.Left || Side == NodeSide.Right? 
						/* Left | Right */	SidePosition :
									(Side == NodeSide.Top? 
						/* Top */			-SideOffset-knobSize.y/2 : 
						/* Bottom */		Body.Rect.height+SideOffset+knobSize.y/2
									)));
			return new Rect (knobCenter.x - knobSize.x/2 + NodeEditor.CurEditorState.ZoomPanAdjust.x, 
							knobCenter.y - knobSize.y/2 + NodeEditor.CurEditorState.ZoomPanAdjust.y, 
							knobSize.x, knobSize.y);
		}
		
		/// <summary>
		/// Get the Knob rect in screen space, ZOOMED, for Input purposes
		/// </summary>
		public Rect GetScreenKnob () 
		{
			Rect rect = GetGUIKnob ();
			rect.position -= NodeEditor.CurEditorState.ZoomPanAdjust; // Remove zoomPanAdjust added in GetGUIKnob
			return NodeEditor.CanvasGUIToScreenRect (rect);
		}

		/// <summary>
		/// Gets the direction of the knob (vertical inverted)
		/// </summary>
		public Vector2 GetDirection () 
		{
			return Side == NodeSide.Right? 	Vector2.right : 
					(Side == NodeSide.Bottom? Vector2.up : 
				 	(Side == NodeSide.Top? 	Vector2.down : 
				 			/* Left */		Vector2.left));
		}

		/// <summary>
		/// Gets the rotation steps anti-clockwise from NodeSide A to B
		/// </summary>
		internal static int GetRotationStepsAntiCW (NodeSide sideA, NodeSide sideB) 
		{
			return sideB - sideA + (sideA>sideB? 4 : 0);
		}

		#endregion
	}
}
