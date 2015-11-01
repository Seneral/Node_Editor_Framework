using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using NodeEditorFramework;
using NodeEditorFramework.Resources;

namespace NodeEditorFramework 
{
	public enum NodeSide { Left = 4, Top = 3, Right = 2, Bottom = 1 }


	public abstract class NodeKnob : ScriptableObject
	{
		// Main
		public Node body;
		public string type;

		// Texture
		public string texturePath;
		[NonSerialized]
		public Texture2D knobTexture;
		
		// Position
		public abstract NodeSide defaultSide { get; }
		public NodeSide side;
		public float sidePosition = 0; // Position on the side, top->bottom, left->right
		public float sideOffset = 0; // Offset from the side

		public abstract void SetType (string type);

		public void Check () 
		{
			if (side == 0)
				side = defaultSide;
			if (knobTexture == null)
				ReloadKnobTexture ();
		}

		public void ReloadKnobTexture () 
		{
			SetType (type);
			
			if (side != defaultSide) 
			{
				int rotationSteps = getRotationStepsAntiCW (defaultSide, side);

				ResourceManager.MemoryTexture memoryTex =  ResourceManager.FindInMemory (knobTexture);
				List<string> mods = memoryTex.modifications.ToList ();
				mods.Add ("Rotation:" + rotationSteps);

				Texture2D knobTextureInMemory = ResourceManager.GetTexture (texturePath, mods.ToArray ());

				if (knobTextureInMemory != null)
					knobTexture = knobTextureInMemory;
				else 
				{
					knobTexture = NodeEditorGUI.RotateTextureAntiCW (knobTexture, rotationSteps);
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
			DisplayLayout (new GUIContent (name));
		}
		
		/// <summary>
		/// Automatically draw the output with the specified label and set the knob next to it at the current side.
		/// </summary>
		public void DisplayLayout (GUIContent content) 
		{
			GUIStyle style = new GUIStyle (GUI.skin.label);
			style.alignment = TextAnchor.MiddleRight;
			GUILayout.Label (content, style);
			if (Event.current.type == EventType.Repaint) 
				SetPosition ();
		}
		
		/// <summary>
		/// Set the knob position at the specified side, from Top->Bottom and Left->Right
		/// </summary>
		public void SetPosition (float position, NodeSide nodeSide) 
		{
			NodeSide oldSide = side;
			side = nodeSide;
			if (oldSide != nodeSide)
				ReloadKnobTexture ();
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
			Vector2 pos = GUILayoutUtility.GetLastRect ().center;
			sidePosition = side == NodeSide.Bottom || side == NodeSide.Top? pos.x : pos.y+20;
		}

		#endregion
		
		#region Position requests

		/// <summary>
		/// Get the Knob rect in GUI space, NOT ZOOMED
		/// </summary>
		public Rect GetGUIKnob () 
		{
			Check ();
			Vector2 center = new Vector2 (body.rect.x + (side == NodeSide.Bottom || side == NodeSide.Top? sidePosition : (side == NodeSide.Left? -sideOffset : body.rect.width+sideOffset)), 
			                              body.rect.y + (side == NodeSide.Left || side == NodeSide.Right? sidePosition : (side == NodeSide.Top? -sideOffset : body.rect.height+sideOffset)));
			Vector2 size = new Vector2 ((knobTexture.width/knobTexture.height) * NodeEditorGUI.knobSize,
			                            (knobTexture.height/knobTexture.width) * NodeEditorGUI.knobSize);
			Rect knobRect = new Rect (center.x + (side == NodeSide.Left? -size.x : 0), center.y + (side == NodeSide.Top? -size.y : (side == NodeSide.Bottom? 0 : -size.y/2)), size.x, size.y);
			knobRect.position += NodeEditor.curEditorState.zoomPanAdjust;
			return knobRect;
		}
		
		/// <summary>
		/// Get the Knob rect in screen space, ZOOMED
		/// </summary>
		public Rect GetScreenKnob () 
		{
			Rect rect = GetGUIKnob ();
			rect.position -= NodeEditor.curEditorState.zoomPanAdjust;
			return NodeEditor.GUIToScreenRect (rect);
		}

		/// <summary>
		/// Gets the direction of the knob (opposite of the node side)
		/// </summary>
		public Vector2 GetDirection () 
		{
			return side == NodeSide.Right? 	Vector2.right : 
					(side == NodeSide.Bottom? Vector2.up : 
				 	(side == NodeSide.Top? 	Vector2.down : 
				 			/* Left */		Vector2.left));
		}

		public int getRotationStepsAntiCW (NodeSide sideA, NodeSide sideB) 
		{
			return sideB - sideA + (sideA>sideB? 4 : 0);
		}

		#endregion
	}
}