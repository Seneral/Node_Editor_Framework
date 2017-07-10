using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

using NodeEditorFramework.Utilities;

namespace NodeEditorFramework 
{
	[System.Serializable]
	public class ConnectionKnob : ConnectionPort
	{
		// Properties
		public override ConnectionShape shape { get { return ConnectionShape.Bezier; } }

		// Connections
		new public List<ConnectionKnob> connections { get { return _connections.OfType<ConnectionKnob> ().ToList (); } }

		// Knob Style
		protected override Type styleBaseClass { get { return typeof(ConnectionKnobStyle); } }
		new protected ConnectionKnobStyle ConnectionStyle { get { CheckConnectionStyle (); return (ConnectionKnobStyle)_connectionStyle; } }

		// Knob GUI
		protected Texture2D knobTexture;
		private float knobAspect { get { return knobTexture != null? knobTexture.width/knobTexture.height : 1; } }
		private GUIStyle labelStyle { get { return side == NodeSide.Right? NodeEditorGUI.nodeLabelRight : NodeEditorGUI.nodeLabelLeft; } }

		// Knob Position
		private NodeSide defaultSide { get { return direction == Direction.Out? NodeSide.Right : NodeSide.Left; } }
		public NodeSide side;
		public float sidePosition = 0; // Position on the side, top->bottom, left->right
		public float sideOffset = 0; // Offset from the side

		public void Init (Node body, string name, Direction dir)
		{
			base.Init (body, name);
			direction = dir;
			maxConnectionCount = dir == Direction.In? ConnectionCount.Single : ConnectionCount.Multi; 
			side = dir == Direction.Out? NodeSide.Right : NodeSide.Left;
			sidePosition = 0;
		}

		public void Init (Node body, string name, Direction dir, NodeSide nodeSide, float nodeSidePosition = 0)
		{
			base.Init (body, name);
			direction = dir;
			maxConnectionCount = dir == Direction.In? ConnectionCount.Single : ConnectionCount.Multi; 
			side = nodeSide;
			sidePosition = nodeSidePosition;
		}

		new public ConnectionKnob connection (int index) 
		{
			if (connections.Count <= index)
				throw new IndexOutOfRangeException ("connections[" + index + "] of '" + name + "'");
			return connections[index];
		}

		#region Knob Texture

		/// <summary>
		/// Checks the texture and requests to load it again if necessary
		/// </summary>
		internal void CheckKnobTexture () 
		{
			if (side == 0)
				side = defaultSide;
			if (knobTexture == null)
				UpdateKnobTexture ();
		}

		/// <summary>
		/// Requests to reload the knobTexture and adapts it to the position and orientation
		/// </summary>
		protected void UpdateKnobTexture () 
		{
			ReloadTexture ();
			if (knobTexture == null)
				throw new UnityException ("Knob texture of " + name + " could not be loaded!");
			if (side != defaultSide) 
			{ // Rotate Knob texture according to the side it's used on
				int rotationSteps = getRotationStepsAntiCW (defaultSide, side);
				string[] mods = new string[] { "Rotation:" + rotationSteps };
				Texture2D modKnobTex = null;

				// Try to get standard texture in memory
				ResourceManager.MemoryTexture memoryTex = ResourceManager.FindInMemory (knobTexture);
				if (memoryTex != null)
				{ // Texture does exist in memory, so built a mod including rotation and try to find it again
					mods = ResourceManager.AppendMod (memoryTex.modifications, "Rotation:" + rotationSteps);
					ResourceManager.TryGetTexture (memoryTex.path, ref modKnobTex, mods);
				}

				if (modKnobTex == null)
				{ // Rotated version does not exist yet, so create and record it
					modKnobTex = RTEditorGUI.RotateTextureCCW (knobTexture, rotationSteps);
					ResourceManager.AddTextureToMemory (memoryTex.path, modKnobTex, mods);
				}

				knobTexture = modKnobTex;
			}
		}

		/// <summary>
		/// Requests to reload the source knob texture
		/// </summary>
		protected virtual void ReloadTexture () 
		{
//			knobTexture = RTEditorGUI.ColorToTex (1, Color.red);
//			knobTexture = ResourceManager.GetTintedTexture (direction == Direction.Out? "Textures/Out_Knob.png" : "Textures/In_Knob.png", color);
			knobTexture = direction == Direction.Out? ConnectionStyle.OutKnobTex : ConnectionStyle.InKnobTex;
		}

		#endregion

		#region Knob Position

		/// <summary>
		/// Gets the Knob rect in GUI space, NOT ZOOMED
		/// </summary>
		public Rect GetGUIKnob () 
		{
			Rect rect = GetCanvasSpaceKnob ();
			rect.position += NodeEditor.curEditorState.zoomPanAdjust + NodeEditor.curEditorState.panOffset;
			return rect;
		}

		/// <summary>
		/// Get the Knob rect in screen space, ZOOMED, for Input detection purposes
		/// </summary>
		public Rect GetCanvasSpaceKnob () 
		{
			Vector2 knobSize = new Vector2 (NodeEditorGUI.knobSize * knobAspect,
											NodeEditorGUI.knobSize / knobAspect);
			Vector2 knobCenter = GetKnobCenter (knobSize);
			return new Rect (knobCenter.x - knobSize.x/2, knobCenter.y - knobSize.y/2, knobSize.x, knobSize.y);
		}

		private Vector2 GetKnobCenter (Vector2 knobSize) 
		{
			if (side == NodeSide.Left)
				return body.rect.position + new Vector2 (-sideOffset-knobSize.x/2, sidePosition);
			else if (side == NodeSide.Right)
				return body.rect.position + new Vector2 ( sideOffset+knobSize.x/2 + body.rect.width, sidePosition);
			else if (side == NodeSide.Bottom)
				return body.rect.position + new Vector2 (sidePosition,  sideOffset+knobSize.y/2 + body.rect.height);
			else if (side == NodeSide.Top)
				return body.rect.position + new Vector2 (sidePosition, -sideOffset-knobSize.y/2);
			else
				throw new Exception ("Unspecified nodeSide of NodeKnop " + name + ": " + side.ToString ());
		}

		/// <summary>
		/// Gets the direction of the knob (vertical inverted) for connection drawing purposes
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
		private static int getRotationStepsAntiCW (NodeSide sideA, NodeSide sideB) 
		{
			return sideB - sideA + (sideA>sideB? 4 : 0);
		}

		#endregion

		#region Knob GUI

		/// <summary>
		/// Draw the knob at the defined position
		/// </summary>
		public virtual void DrawKnob () 
		{
			CheckKnobTexture ();
			GUI.DrawTexture (GetGUIKnob (), knobTexture);
		}

		/// <summary>
		/// Draws the connection curves from this knob to all it's connections
		/// </summary>
		public override void DrawConnections () 
		{
			if (Event.current.type != EventType.Repaint)
				return;
			Vector2 startPos = GetGUIKnob ().center;
			Vector2 startDir = GetDirection();
			foreach (ConnectionKnob connectionKnob in connections)
			{
				Vector2 endPos = connectionKnob.GetGUIKnob().center;
				Vector2 endDir = connectionKnob.GetDirection();
				NodeEditorGUI.DrawConnection(startPos, startDir, endPos, endDir, color);
			}
		}

		/// <summary>
		/// Draws a connection line from the current knob to the specified position
		/// </summary>
		public override void DrawConnection (Vector2 endPos) 
		{
			Vector2 startPos = GetGUIKnob ().center;
			Vector2 startDir = GetDirection ();
			NodeEditorGUI.DrawConnection (startPos, startDir, endPos, -startDir, color);
		}

		/// <summary>
		/// Displays a label and places the knob next to it, apropriately
		/// </summary>
		public void DisplayLayout () 
		{
			DisplayLayout (new GUIContent (name), labelStyle);
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
			DisplayLayout (content, labelStyle);
		}

		/// <summary>
		/// Draws a label with the given GUIContent and the given style. Places the knob next to it at it's nodeSide
		/// </summary>
		public void DisplayLayout (GUIContent content, GUIStyle style)
		{
			GUILayout.Label (content, style);
			SetPosition ();
		}

		/// <summary>
		/// Sets the knob's position at the specified nodeSide next to the last GUILayout control
		/// </summary>
		public void SetPosition () 
		{
			if (Event.current.type != EventType.Repaint || body.ignoreGUIKnobPlacement)
				return;
			Vector2 pos = GUILayoutUtility.GetLastRect ().center + body.contentOffset;
			SetPosition (side == NodeSide.Bottom || side == NodeSide.Top? pos.x : pos.y);
		}

		/// <summary>
		/// Sets the knob's position at the specified nodeSide, from Top->Bottom and Left->Right
		/// </summary>
		public void SetPosition(float position, NodeSide nodeSide)
		{
			if (body.ignoreGUIKnobPlacement)
				return;
			if (side != nodeSide)
			{
				side = nodeSide;
				UpdateKnobTexture();
			}
			SetPosition(position);
		}

		/// <summary>
		/// Sets the knob's position at it's nodeSide, from Top->Bottom and Left->Right
		/// </summary>
		public void SetPosition(float position)
		{
			if (body.ignoreGUIKnobPlacement)
				return;
			sidePosition = position;
		}

		#endregion
	}

	[AttributeUsage(AttributeTargets.Field)]
	public class ConnectionKnobAttribute : ConnectionPortAttribute
	{
		public NodeSide NodeSide;
		public float NodeSidePos;

		public override Type ConnectionType { get { return typeof(ConnectionKnob); } }

		public ConnectionKnobAttribute(string name, Direction direction) : base (name, direction)
		{
			Setup (direction == Direction.Out? NodeSide.Right : NodeSide.Left, 0);
		}
		public ConnectionKnobAttribute(string name, Direction direction, ConnectionCount maxCount) : base (name, direction)
		{
			Setup (maxCount, direction == Direction.Out? NodeSide.Right : NodeSide.Left, 0);
		}
		public ConnectionKnobAttribute(string name, Direction direction, string styleID) : base (name, direction, styleID)
		{
			Setup (direction == Direction.Out? NodeSide.Right : NodeSide.Left, 0);
		}
		public ConnectionKnobAttribute(string name, Direction direction, string styleID, ConnectionCount maxCount) : base (name, direction, styleID)
		{
			Setup (maxCount, direction == Direction.Out? NodeSide.Right : NodeSide.Left, 0);
		}
		public ConnectionKnobAttribute(string name, Direction direction, NodeSide nodeSide, float nodeSidePos = 0) : base (name, direction)
		{
			Setup (nodeSide, nodeSidePos);
		}
		public ConnectionKnobAttribute(string name, Direction direction, ConnectionCount maxCount, NodeSide nodeSide, float nodeSidePos = 0) : base (name, direction)
		{
			Setup (maxCount, nodeSide, nodeSidePos);
		}
		public ConnectionKnobAttribute(string name, Direction direction, string styleID, NodeSide nodeSide, float nodeSidePos = 0) : base (name, direction, styleID)
		{
			Setup (nodeSide, nodeSidePos);
		}
		public ConnectionKnobAttribute(string name, Direction direction, string styleID, ConnectionCount maxCount, NodeSide nodeSide, float nodeSidePos = 0) : base (name, direction, styleID)
		{
			Setup (maxCount, nodeSide, nodeSidePos);
		}


		private void Setup (NodeSide nodeSide, float nodeSidePos) 
		{
			MaxConnectionCount = Direction == Direction.In? ConnectionCount.Single : ConnectionCount.Multi;
			NodeSide = nodeSide;
			NodeSidePos = nodeSidePos;
		}
		private void Setup (ConnectionCount maxCount, NodeSide nodeSide, float nodeSidePos) 
		{
			MaxConnectionCount = maxCount;
			NodeSide = nodeSide;
			NodeSidePos = nodeSidePos;
		}

		public override ConnectionPort CreateNew (Node body) 
		{
			ConnectionKnob knob = ScriptableObject.CreateInstance<ConnectionKnob> ();
			knob.Init (body, Name, Direction, NodeSide, NodeSidePos);
			knob.styleID = StyleID;
			knob.maxConnectionCount = MaxConnectionCount;
			return knob;
		}

		public override void UpdateProperties (ConnectionPort port) 
		{
			ConnectionKnob knob = (ConnectionKnob)port;
			knob.name = Name;
			knob.direction = Direction;
			knob.styleID = StyleID;
			knob.maxConnectionCount = MaxConnectionCount;
			knob.side = NodeSide;
			if (NodeSidePos != 0)
				knob.sidePosition = NodeSidePos;
			knob.sideOffset = 0;
		}
	}


	[ReflectionUtility.ReflectionSearchIgnoreAttribute ()]
	public class ConnectionKnobStyle : ConnectionPortStyle
	{
		public virtual string InKnobTexPath { get { return "Textures/In_Knob.png"; } }
		public virtual string OutKnobTexPath { get { return "Textures/Out_Knob.png"; } }

		private Texture2D _inKnobTex;
		private Texture2D _outKnobTex;
		public Texture2D InKnobTex { get { if (_inKnobTex == null) LoadKnobTextures(); return _inKnobTex; } }
		public Texture2D OutKnobTex { get { if (_outKnobTex == null) LoadKnobTextures(); return _outKnobTex; } }

		public ConnectionKnobStyle () : base () {}

		public ConnectionKnobStyle (string name) : base (name) { }

		protected void LoadKnobTextures()
		{
			_inKnobTex = ResourceManager.GetTintedTexture (InKnobTexPath, Color);
			_outKnobTex = ResourceManager.GetTintedTexture (OutKnobTexPath, Color);
			if (InKnobTex == null || OutKnobTex == null)
				Debug.LogError("Invalid style '" + Identifier + "': Could not load knob textures from '" + InKnobTexPath + "' and '" + OutKnobTexPath + "'!");
		}

		public override bool isValid ()
		{
			return InKnobTex != null && OutKnobTex != null;
		}
	}
}
