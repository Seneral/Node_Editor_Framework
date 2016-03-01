using UnityEngine;
using System.Linq;
using System.Collections.Generic;

using NodeEditorFramework.Utilities;

namespace NodeEditorFramework 
{
	/// <summary>
	/// Defines a side on a Node
	/// </summary>
	public enum NodeSide { Left = 4, Top = 3, Right = 2, Bottom = 1 }

	/// <summary>
	/// Abstract knob on the side of an node that handles positioning drawing with a texture and even labeling and positioning calls
	/// </summary>
	[System.Serializable]
	public class NodeKnob : ScriptableObject
	{
		// Main
		public Node body;

		protected virtual GUIStyle defaultLabelStyle { get { return GUI.skin.label; } }
		[System.NonSerialized]
		protected internal Texture2D knobTexture;
		
		// Position
		protected virtual NodeSide defaultSide { get { return NodeSide.Right; } }
		public NodeSide side;
		public float sidePosition = 0; // Position on the side, top->bottom, left->right
		public float sideOffset = 0; // Offset from the side

		/// <summary>
		/// Inits the base node and subscribes it in the node body for drawing and requests to load the texture through 'ReloadTexture'
		/// </summary>
		protected void InitBase (Node nodeBody, NodeSide nodeSide, float nodeSidePosition, string knobName) 
		{
			body = nodeBody;
			side = nodeSide;
			sidePosition = nodeSidePosition;
			name = knobName;
			nodeBody.nodeKnobs.Add (this);
			ReloadKnobTexture ();
		}

		public virtual void Delete () 
		{
			body.nodeKnobs.Remove (this);
			DestroyImmediate (this, true);
		}

		#region Knob Texture Loading

		/// <summary>
		/// Checks the texture and requests to load it again if necessary
		/// </summary>
		internal void Check () 
		{
			if (side == 0)
				side = defaultSide;
			if (knobTexture == null)
				ReloadKnobTexture ();
		}

		/// <summary>
		/// Requests to teload the knobTexture and adapts it to the position and orientation
		/// </summary>
		protected void ReloadKnobTexture () 
		{
			ReloadTexture ();
			if (knobTexture == null)
				throw new UnityException ("Knob texture could not be loaded!");
			if (side != defaultSide) 
			{ // Rotate Knob texture according to the side it's used on
				ResourceManager.SetDefaultResourcePath (NodeEditor.editorPath + "Resources/");
				int rotationSteps = getRotationStepsAntiCW (defaultSide, side);

				// Get standard texture in memory
				ResourceManager.MemoryTexture memoryTex = ResourceManager.FindInMemory (knobTexture);
				if (memoryTex != null)
				{ // Texture does exist in memory, so built a mod including rotation
					string[] mods = new string[memoryTex.modifications.Length+1];
					memoryTex.modifications.CopyTo (mods, 0);
					mods[mods.Length-1] = "Rotation:" + rotationSteps;
					// Try to find the rotated version in memory
					Texture2D knobTextureInMemory = ResourceManager.GetTexture (memoryTex.path, mods);
					if (knobTextureInMemory != null)
					{ // Rotated version does exist
						knobTexture = knobTextureInMemory;
					}
					else 
					{ // Rotated version does not exist, so create and reord it
						knobTexture = RTEditorGUI.RotateTextureCCW (knobTexture, rotationSteps);
						ResourceManager.AddTextureToMemory (memoryTex.path, knobTexture, mods.ToArray ());
					}
				}
				else
				{ // If it does not exist in memory, we have no path for it so we just silently rotate and use it
					// Note that this way we have to rotate it over and over again later on
					knobTexture = RTEditorGUI.RotateTextureCCW (knobTexture, rotationSteps);
				}
			}
		}

		/// <summary>
		/// Defines an reload. This should assign knobTexture and return the path to knobTexture.
		/// </summary>
		protected virtual void ReloadTexture () 
		{
			knobTexture = RTEditorGUI.ColorToTex (1, Color.red);
		}

		#endregion

		#region Additional Serialization

		/// <summary>
		/// Returns all additional ScriptableObjects this NodeKnob holds. 
		/// That means only the actual SOURCES, simple REFERENCES will not be returned
		/// This means all SciptableObjects returned here do not have it's source elsewhere
		/// </summary>
		public virtual ScriptableObject[] GetScriptableObjects () { return new ScriptableObject[0]; }

		/// <summary>
		/// Replaces all REFERENCES aswell as SOURCES of any ScriptableObjects this NodeKnob holds with the cloned versions in the serialization process.
		/// </summary>
		protected internal virtual void CopyScriptableObjects (System.Func<ScriptableObject, ScriptableObject> replaceSerializableObject) {}

		#endregion

		#region GUI drawing and Positioning

		/// <summary>
		/// Draw this knob at it's position with it's knobTexture
		/// </summary>
		public virtual void DrawKnob () 
		{
			Rect knobRect = GetGUIKnob ();
			GUI.DrawTexture (knobRect, knobTexture);
		}

		/// <summary>
		/// Draws a label with the knob's name. Places the knob next to it at it's nodeSide
		/// </summary>
		public void DisplayLayout () 
		{
			DisplayLayout (new GUIContent (name), defaultLabelStyle);
		}

		/// <summary>
		/// Draws a label with the knob's name and the given style. Places the knob next to it at it's nodeSide
		/// </summary>
		public void DisplayLayout (GUIStyle style)
		{
			DisplayLayout (new GUIContent (name), style);
		}

		/// <summary>
		/// Draws a label with the given GUIContent. Places the knob next to it at it's nodeSide
		/// </summary>
		public void DisplayLayout (GUIContent content) 
		{
			DisplayLayout (content, defaultLabelStyle);
		}

		/// <summary>
		/// Draws a label with the given GUIContent and the given style. Places the knob next to it at it's nodeSide
		/// </summary>
		public void DisplayLayout (GUIContent content, GUIStyle style)
		{
			GUILayout.Label (content, style);
			if (Event.current.type == EventType.Repaint)
				SetPosition ();
		}
		
		/// <summary>
		/// Sets the knob's position at the specified nodeSide, from Top->Bottom and Left->Right
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
		/// Sets the knob's position at it's nodeSide, from Top->Bottom and Left->Right
		/// </summary>
		public void SetPosition (float position) 
		{
			sidePosition = position;
		}
		
		/// <summary>
		/// Sets the knob's position at the specified nodeSide next to the last GUILayout control
		/// </summary>
		public void SetPosition () 
		{
			Vector2 pos = GUILayoutUtility.GetLastRect ().center + body.contentOffset;
			sidePosition = side == NodeSide.Bottom || side == NodeSide.Top? pos.x : pos.y;
		}

		#endregion
		
		#region Position requests

		/// <summary>
		/// Gets the Knob rect in GUI space, NOT ZOOMED
		/// </summary>
		internal Rect GetGUIKnob () 
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
		/// Get the Knob rect in screen space, ZOOMED, for Input detection purposes
		/// </summary>
		internal Rect GetScreenKnob () 
		{
			Rect rect = GetGUIKnob ();
			rect.position -= NodeEditor.curEditorState.zoomPanAdjust; // Remove zoomPanAdjust added in GetGUIKnob
			return NodeEditor.CanvasGUIToScreenRect (rect);
		}

		/// <summary>
		/// Gets the direction of the knob (vertical inverted) for connection drawing purposes
		/// </summary>
		internal Vector2 GetDirection () 
		{
			return side == NodeSide.Right? 	Vector2.right : 
					(side == NodeSide.Bottom? Vector2.up : 
				 	(side == NodeSide.Top? 	Vector2.down : 
				 			/* Left */		Vector2.left));
		}

		/// <summary>
		/// Gets the rotation steps anti-clockwise from NodeSide A to B
		/// </summary>
		private static int getRotationStepsAntiCW (NodeSide sideA, NodeSide sideB) 
		{
			return sideB - sideA + (sideA>sideB? 4 : 0);
		}

		#endregion
	}
}
