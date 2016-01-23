using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework 
{
	public enum NodeSide { Left = 4, Top = 3, Right = 2, Bottom = 1 }

	public abstract class NodeKnob : ScriptableObject
	{
		// Main
		public Node body;
		public string type;
		public TypeData typeData;

		// Style
		protected abstract GUIStyle defaultLabelStyle { get; }
		protected string texturePath;
		[NonSerialized]
		internal Texture2D knobTexture;
		
		// Position
		protected abstract NodeSide defaultSide { get; }
		public NodeSide side;
		public float sidePosition = 0; // Position on the side, top->bottom, left->right
		public float sideOffset = 0; // Offset from the side

		protected abstract void ReloadType ();

		private void Check () 
		{
			if (side == 0)
				side = defaultSide;
			if (knobTexture == null)
				ReloadKnobTexture ();
		}

		protected void ReloadKnobTexture () 
		{
			ReloadType ();
			
			if (side != defaultSide) 
			{ // Rotate Knob texture according to the side it's used on
				int rotationSteps = getRotationStepsAntiCW (defaultSide, side);

				ResourceManager.MemoryTexture memoryTex = ResourceManager.FindInMemory (knobTexture);
				List<string> mods = memoryTex.modifications.ToList ();
				mods.Add ("Rotation:" + rotationSteps);

				Texture2D knobTextureInMemory = ResourceManager.GetTexture (texturePath, mods.ToArray ());

				if (knobTextureInMemory != null)
					knobTexture = knobTextureInMemory;
				else 
				{
					knobTexture = RTEditorGUI.RotateTextureAntiCW (knobTexture, rotationSteps);
					ResourceManager.AddTexture (texturePath, knobTexture, mods.ToArray ());
				}
			}
		}

		#region GUI and Position calls

		/// <summary>
		/// Automatically draw the output with it's name and set the knob next to it at the current side.
		/// </summary>
		public void DisplayLayout () 
		{
			DisplayLayout (new GUIContent (name), defaultLabelStyle);
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
			DisplayLayout (content, defaultLabelStyle);
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
			if (side != nodeSide) 
			{
				side = nodeSide;
				ReloadKnobTexture ();
			}
			SetPosition (position);
		}
		
		/// <summary>
		/// Set the knob position at the current side, from Top->Bottom and Left->Right
		/// </summary>
		public void SetPosition (float position) 
		{
			sidePosition = position;
		}
		
		/// <summary>
		/// Set the knob position at the current side next to the last Layout control
		/// </summary>
		public void SetPosition () 
		{
			Vector2 pos = GUILayoutUtility.GetLastRect ().center + body.contentOffset;
			sidePosition = side == NodeSide.Bottom || side == NodeSide.Top? pos.x : pos.y;
		}

		#endregion
		
		#region Position requests

		/// <summary>
		/// Get the Knob rect in GUI space, NOT ZOOMED
		/// </summary>
		public Rect GetGUIKnob () 
		{
			Check ();
			Vector2 knobSize = new Vector2 ((knobTexture.width/knobTexture.height) * NodeEditorGUI.knobSize,
											(knobTexture.height/knobTexture.width) * NodeEditorGUI.knobSize);
			Vector2 knobCenter = new Vector2 (body.rect.x + (side == NodeSide.Bottom || side == NodeSide.Top? 
						/* Top | Bottom */	sidePosition :
									(side == NodeSide.Left? 
						/* Left */			-sideOffset-knobSize.x/2 : 
						/* Right */			body.rect.width+sideOffset+knobSize.x/2
									)),
									body.rect.y + (side == NodeSide.Left || side == NodeSide.Right? 
						/* Left | Right */	sidePosition :
									(side == NodeSide.Top? 
						/* Top */			-sideOffset-knobSize.y/2 : 
						/* Bottom */		body.rect.height+sideOffset+knobSize.y/2
									)));
			return new Rect (knobCenter.x - knobSize.x/2 + NodeEditor.curEditorState.zoomPanAdjust.x, 
							knobCenter.y - knobSize.y/2 + NodeEditor.curEditorState.zoomPanAdjust.y, 
							knobSize.x, knobSize.y);
		}
		
		/// <summary>
		/// Get the Knob rect in screen space, ZOOMED, for Input purposes
		/// </summary>
		public Rect GetScreenKnob () 
		{
			Rect rect = GetGUIKnob ();
			rect.position -= NodeEditor.curEditorState.zoomPanAdjust; // Remove zoomPanAdjust added in GetGUIKnob
			return NodeEditor.CanvasGUIToScreenRect (rect);
		}

		/// <summary>
		/// Gets the direction of the knob (vertical inverted)
		/// </summary>
		public Vector2 GetDirection () 
		{
			return side == NodeSide.Right? 	Vector2.right : 
					(side == NodeSide.Bottom? Vector2.up : 
				 	(side == NodeSide.Top? 	Vector2.down : 
				 			/* Left */		Vector2.left));
		}

		/// <summary>
		/// Gets the rotation steps anti-clockwise from NodeSide A to B
		/// </summary>
		internal static int getRotationStepsAntiCW (NodeSide sideA, NodeSide sideB) 
		{
			return sideB - sideA + (sideA>sideB? 4 : 0);
		}

		#endregion
	}
}
