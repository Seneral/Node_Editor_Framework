using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using Object = UnityEngine.Object;

public enum TypeOf { Float, Texture2D, Channel }

public struct TypeData 
{
	public Color col;
	public Texture2D InputKnob;
	public Texture2D OutputKnob;

	public TypeData (Color color, Texture2D inKnob, Texture2D outKnob) 
	{
		col = color;
		InputKnob = Node_Editor.Tint (inKnob, color);
		OutputKnob = Node_Editor.Tint (outKnob, color);
	}
}

public class Node_Editor : EditorWindow 
{
	// Information about current instances
	public static Node_Canvas_Object nodeCanvas;
	public static Node_Editor editor;

	public const string editorPath = "Assets/Plugins/Node_Editor/Editor/";
	public static string openedCanvas = "New Canvas";
	public static string openedCanvasPath;

	// Settings
	public static int sideWindowWidth = 400;
	public static int knobSize = 18;

	// Variables about the current state
	public static Node activeNode;
	public static bool dragNode = false;
	public static NodeOutput connectOutput;
	public static bool navigate = false;
	public static bool panWindow = false;
	public static Vector2 mousePos;
	public static Vector2 zoomPos;
	public static Vector2 zoomPanAdjust;

	// Static textures and styles
	public static Texture2D InputKnob;
	public static Texture2D OutputKnob;
	public static Texture2D ConnectorKnob;
	public static Texture2D Background;
	public static GUIStyle nodeBase;
	public static GUIStyle nodeBox;
	public static GUIStyle nodeLabelBold;

	// Static information about Types
	public static Dictionary<TypeOf, TypeData> typeData;

	#region Setup

	[MenuItem("Window/Node Editor")]
	static void CreateEditor () 
	{
		Node_Editor.editor = EditorWindow.GetWindow<Node_Editor> ();
		Node_Editor.editor.minSize = new Vector2 (800, 600);
	}

	private bool initiated;

	public void checkInit () 
	{
		if (!initiated || nodeCanvas == null) 
		{
			InputKnob = AssetDatabase.LoadAssetAtPath (editorPath + "Textures/In_Knob.png", typeof(Texture2D)) as Texture2D;
			OutputKnob = AssetDatabase.LoadAssetAtPath (editorPath + "Textures/Out_Knob.png", typeof(Texture2D)) as Texture2D;
			
			ConnectorKnob = EditorGUIUtility.Load ("icons/animationkeyframe.png") as Texture2D;
			Background = AssetDatabase.LoadAssetAtPath (editorPath + "Textures/background.png", typeof(Texture2D)) as Texture2D;

			// TODO: Node Editor: Type Declaration
			typeData = new Dictionary<TypeOf, TypeData> () 
			{
				{ TypeOf.Float, new TypeData (Color.cyan, InputKnob, OutputKnob) },
				{ TypeOf.Texture2D, new TypeData (Color.magenta, InputKnob, OutputKnob) },
				{ TypeOf.Channel, new TypeData (Color.yellow, InputKnob, OutputKnob) }
			};
			
			nodeBase = new GUIStyle (GUI.skin.box);
			nodeBase.normal.background = ColorToTex (new Color (0.2f, 0.2f, 0.2f));
			nodeBase.normal.textColor = new Color (0.7f, 0.7f, 0.7f);
			
			nodeBox = new GUIStyle (nodeBase);
			nodeBox.margin = new RectOffset (8, 8, 5, 8);
			nodeBox.padding = new RectOffset (8, 8, 8, 8);
			
			nodeLabelBold = new GUIStyle (nodeBase);
			nodeLabelBold.fontStyle = FontStyle.Bold;
			nodeLabelBold.wordWrap = false;
			
			NewNodeCanvas ();
			
			// Example of creating Nodes and Connections through code
//			CalcNode calcNode1 = CalcNode.Create (new Rect (200, 200, 200, 100));
//			CalcNode calcNode2 = CalcNode.Create (new Rect (600, 200, 200, 100));
//			Node.ApplyConnection (calcNode1.Outputs [0], calcNode2.Inputs [0]);
			
			initiated = true;
		}
	}

	#endregion

	#region GUI

	public void OnGUI () 
	{
		checkInit ();

		// Draw Background when Repainting
		if (Event.current.type == EventType.Repaint) 
		{
			float width = Background.width / nodeCanvas.zoom;
			float height = Background.height / nodeCanvas.zoom;
			Vector2 offset = new Vector2 ((nodeCanvas.panOffset.x / nodeCanvas.zoom)%width - width, 
			                              (nodeCanvas.panOffset.y / nodeCanvas.zoom)%height - height);
			int tileX = Mathf.CeilToInt ((position.width + (width - offset.x)) / width);
			int tileY = Mathf.CeilToInt ((position.height + (height - offset.y)) / height);
			
			for (int x = 0; x < tileX; x++) 
			{
				for (int y = 0; y < tileY; y++) 
				{
					Rect texRect = new Rect (offset.x + x*width, 
					                         offset.y + y*height, 
					                         width, height);
					GUI.DrawTexture (texRect, Background);
				}
			}
		}

		InputEvents ();

		// We want to scale our nodes, but as GUI.matrix also scales our widnow's clipping group, 
		// we have to scale it up first to receive a correct one as a result
		#region Scale Setup

		// End the default clipping group
		GUI.EndGroup ();
		
		// The Rect of the new clipping group to draw our nodes in
		Rect CanvasRect = canvasWindowRect;
		Rect ScaledCanvasRect = ScaleRect (CanvasRect, zoomPos, new Vector2 (nodeCanvas.zoom, nodeCanvas.zoom));
		ScaledCanvasRect.y += 23; // Header tab height

		// Now continue drawing using the new clipping group
		GUI.BeginGroup (ScaledCanvasRect);
		ScaledCanvasRect.position = Vector2.zero; // Adjust because we entered the new group

		// Because I currently found no way to actually scale to the center of the window rather than (0, 0),
		// I'm going to cheat and just pan it accordingly to let it appear as if it would scroll to the center
		// Note, due to that, other controls are still scaled to (0, 0)
		zoomPos = CanvasRect.center; // Set it to whatever you prefer
		zoomPanAdjust = ScaledCanvasRect.center - CanvasRect.size/2 + zoomPos;
		
		// Take a matrix backup to restore back later on
		Matrix4x4 GUIMatrix = GUI.matrix;
		
		// Scale GUI.matrix. After that we have the correct clipping group again.
		GUIUtility.ScaleAroundPivot (new Vector2 (1/nodeCanvas.zoom, 1/nodeCanvas.zoom), zoomPanAdjust);

		#endregion

		// Some features which require drawing:
		if (navigate) 
		{ // Draw a curve to the origin/active node for orientation purposes
			DrawNodeCurve ((activeNode != null? activeNode.rect.center : nodeCanvas.panOffset), mousePos*nodeCanvas.zoom, Color.black); 
			Repaint ();
		}
		if (connectOutput != null)
		{ // Draw the currently drawn connection
			DrawNodeCurve (connectOutput.GetGUIKnob ().center, mousePos*nodeCanvas.zoom, typeData [connectOutput.type].col);
			Repaint ();
		}

		// Draw the nodes:
//		BeginWindows ();
		for (int nodeCnt = 0; nodeCnt < nodeCanvas.nodes.Count; nodeCnt++) 
		{
			// TODO: Node Editor Feature: Custom Windowing System
			// To remove it, switch comments here and uncomment Begin/EndWindows. No warranty!
//			if (nodeCanvas.nodes [nodeCnt] != activeNode)
			DrawNode (nodeCnt);
//			nodeCanvas.nodes [nodeCnt].rect = GUILayout.Window (nodeCnt, nodeCanvas.nodes [nodeCnt].zoomedRect, DrawNode, nodeCanvas.nodes [nodeCnt].name);
		}
//		EndWindows ();

		// Draw their connectors; Seperated because of render order
		for (int nodeCnt = 0; nodeCnt < nodeCanvas.nodes.Count; nodeCnt++) 
			nodeCanvas.nodes [nodeCnt].DrawConnections ();
		for (int nodeCnt = 0; nodeCnt < nodeCanvas.nodes.Count; nodeCnt++) 
			nodeCanvas.nodes [nodeCnt].DrawKnobs ();

		// Draw the active Node ontop
//		if (activeNode != null)	
//			DrawNode (nodeCanvas.nodes.IndexOf (activeNode));
		
		// End scaling group:
		// Set default matrix and clipping group for the rest
		GUI.matrix = GUIMatrix;
		GUI.EndGroup ();
		GUI.BeginGroup (new Rect (0, 23, position.width, position.height));

		LateEvents ();

		// Draw Side Window:
		sideWindowWidth = Math.Min (600, Math.Max (200, (int)(position.width / 5)));
		GUILayout.BeginArea (sideWindowRect, nodeBox);
		DrawSideWindow ();
		GUILayout.EndArea ();
	}
	
	public void DrawSideWindow () 
	{
		GUILayout.Label (new GUIContent ("Node Editor (" + openedCanvas + ")", "The currently opened canvas in the Node Editor"), nodeLabelBold);
		GUILayout.Label (new GUIContent ("Do note that changes will be saved automatically!", "All changes are automatically saved to the currently opened canvas (see above) if it's present in the Project view."), nodeBase);
		if (GUILayout.Button (new GUIContent ("Save Canvas", "Saves the canvas as a new Canvas Asset File in the Assets Folder"))) 
		{
			SaveNodeCanvas (EditorUtility.SaveFilePanelInProject ("Save Node Canvas", "Node Canvas", "asset", "Saving to a file is only needed once.", editorPath + "Saves/"));
		}
		if (GUILayout.Button (new GUIContent ("Load Canvas", "Loads the canvas from a Canvas Asset File in the Assets Folder"))) 
		{
			string path = EditorUtility.OpenFilePanel ("Load Node Canvas", editorPath + "Saves/", "asset");
			if (!path.Contains (Application.dataPath)) 
			{
				if (path != String.Empty)
					ShowNotification (new GUIContent ("You should select an asset inside your project folder!"));
				return;
			}
			path = path.Replace (Application.dataPath, "Assets");
			LoadNodeCanvas (path);
		}
		if (GUILayout.Button (new GUIContent ("New Canvas", "Creates a new Canvas (remember to save the previous one to a referenced Canvas Asset File at least once before! Else it'll be lost!)"))) 
		{
			NewNodeCanvas ();
		}
		if (GUILayout.Button (new GUIContent ("Recalculate All", "Starts to calculate from the beginning off."))) 
		{
			RecalculateAll ();
		}
		knobSize = EditorGUILayout.IntSlider (new GUIContent ("Handle Size", "The size of the handles of the Node Inputs/Outputs"), knobSize, 12, 20);
		nodeCanvas.zoom = EditorGUILayout.Slider (new GUIContent ("Zoom"), nodeCanvas.zoom, 0.6f, 2);
	}

	#endregion
	
	#region GUI Functions
	
	/// <summary>
	/// Context Click selection. Here you'll need to register your own using a string identifier
	/// </summary>
	public void ContextCallback (object obj)
	{
		// TODO: Node Editor: Custom Node Regristration here!
		switch (obj.ToString ()) 
		{
		case CalcNode.ID:
			CalcNode.Create (new Rect (mousePos.x - zoomPos.x, mousePos.y - zoomPos.y, 200, 100));
			break;
			
		case InputNode.ID:
			InputNode.Create (new Rect (mousePos.x - zoomPos.x, mousePos.y - zoomPos.y, 200, 50));
			break;
			
		case DisplayNode.ID:
			DisplayNode.Create (new Rect (mousePos.x - zoomPos.x, mousePos.y - zoomPos.y, 150, 50));
			break;

//		case ExampleNode.ID:
//			ExampleNode.Create (new Rect (mousePos.x - zoomPos.x, mousePos.y - zoomPos.y, 100, 50));
//			break;
			
		case "deleteNode":
			Node nodeToDelete = NodeAtPosition (mousePos);
			if (nodeToDelete != null) 
				nodeToDelete.Delete ();
			break;

		case "duplicateNode":
			Node nodeToDuplicate = NodeAtPosition (mousePos);
			if (nodeToDuplicate != null) 
			{
				ContextCallback (nodeToDuplicate.GetID);
				Node duplicatedNode = nodeCanvas.nodes [nodeCanvas.nodes.Count-1];
				activeNode = duplicatedNode;
				dragNode = true;
			}
			break;
		}
	}
	
	public Rect sideWindowRect 
	{
		get { return new Rect (position.width - sideWindowWidth, 0, sideWindowWidth, position.height); }
	}
	public Rect canvasWindowRect 
	{
		get { return new Rect (0, 0, position.width - sideWindowWidth, position.height); }
	}
	
	/// <summary>
	/// Create a 1x1 tex with color col
	/// </summary>
	public static Texture2D ColorToTex (Color col) 
	{
		Texture2D tex = new Texture2D (1, 1);
		tex.SetPixel (1, 1, col);
		tex.Apply ();
		return tex;
	}
	
	/// <summary>
	/// Tint the texture with the color.
	/// </summary>
	public static Texture2D Tint (Texture2D tex, Color color) 
	{
		Texture2D tintedTex = Instantiate (tex);
		
		for (int x = 0; x < tex.width; x++) 
			for (int y = 0; y < tex.height; y++) 
				tintedTex.SetPixel (x, y, tex.GetPixel (x, y) * color);
		
		tintedTex.Apply ();
		
		return tintedTex;
	}
	
	/// <summary>
	/// Returns the node at the position
	/// </summary>
	public Node NodeAtPosition (Vector2 pos)
	{	
		if (sideWindowRect.Contains (pos))
			return null;
		// Check if we clicked inside a window (or knobSize pixels left or right of it at outputs, for faster knob recognition)
		for (int nodeCnt = nodeCanvas.nodes.Count-1; nodeCnt >= 0; nodeCnt--) 
		{ // From top to bottom because of the render order (though overwritten by active Window, so be aware!)
			Rect NodeRect = new Rect (nodeCanvas.nodes [nodeCnt].screenRect);
			float zoomedKnobSize = (float)knobSize/nodeCanvas.zoom;
			NodeRect = new Rect (NodeRect.x - zoomedKnobSize, NodeRect.y, NodeRect.width + zoomedKnobSize*2, NodeRect.height);
			if (NodeRect.Contains (pos))
				return nodeCanvas.nodes [nodeCnt];
		}
		return null;
	}
	
	/// <summary>
	/// Draws the node
	/// </summary>
	private void DrawNode (int id)
	{
		// TODO: Node Editor Feature: Custom Windowing System
		// To remove it, Replace following comments. No warranty!
		
		//nodeCanvas.nodes [id].NodeGUI ();
		//GUI.DragWindow ();

		Node node = nodeCanvas.nodes [id];
		Rect nodeRect = node.rect;
		nodeRect.position += zoomPanAdjust;
		float headerHeight = 20;
		Rect headerRect = new Rect (nodeRect.x, nodeRect.y, nodeRect.width, headerHeight);
		Rect bodyRect = new Rect (nodeRect.x, nodeRect.y + headerHeight, nodeRect.width, nodeRect.height - headerHeight);

		GUIStyle headerStyle = new GUIStyle (GUI.skin.box);
		if (activeNode == node)
			headerStyle.fontStyle = FontStyle.Bold;
		GUI.Label (headerRect, new GUIContent (node.name), headerStyle);
		GUILayout.BeginArea (bodyRect, GUI.skin.box);
		node.NodeGUI ();
		GUILayout.EndArea ();
	}

	public static Rect ScaleRect (Rect rect, Vector2 pivot, Vector2 scale) 
	{
		rect.position = Vector2.Scale (rect.position - pivot, scale) + pivot;
		rect.size = Vector2.Scale (rect.size, scale);
		return rect;
	}
	
	/// <summary>
	/// Draws a node curve from start to end (with three shades of shadows)
	/// </summary>
	public static void DrawNodeCurve (Vector2 start, Vector2 end, Color col) 
	{
		Vector3 startPos = new Vector3 (start.x, start.y);
		Vector3 endPos = new Vector3 (end.x, end.y);
		Vector3 startTan = startPos + Vector3.right * 50;
		Vector3 endTan = endPos + Vector3.left * 50;
		Color shadowColor = new Color (0, 0, 0, 0.1f);
		
		for (int i = 0; i < 3; i++) // Draw a shadow with 3 shades
			Handles.DrawBezier (startPos, endPos, startTan, endTan, shadowColor, null, (i + 1) * 5); // increasing width for fading shadow
		Handles.DrawBezier(startPos, endPos, startTan, endTan, col * Color.gray, null, 3);
	}
	
	#endregion

	#region Events
	
	/// <summary>
	/// Processes input events
	/// </summary>
	private void InputEvents () 
	{
		Event e = Event.current;
		mousePos = e.mousePosition;
		
		Node clickedNode = null;
		if (e.type == EventType.MouseDown || e.type == EventType.MouseUp)
			clickedNode = NodeAtPosition (e.mousePosition);

		switch (e.type) 
		{
		case EventType.MouseDown:

			if (e.button == 0)
				activeNode = clickedNode;

			connectOutput = null;
			dragNode = false;
			panWindow = false;
			
			if (clickedNode != null) 
			{ // A click on a node
				if (e.button == 1)
				{ // Right click -> Node Context Click
					GenericMenu menu = new GenericMenu ();
					
					menu.AddItem (new GUIContent ("Delete Node"), false, ContextCallback, "deleteNode");
					menu.AddItem (new GUIContent ("Duplicate Node"), false, ContextCallback, "duplicateNode");
					
					menu.ShowAsContext ();
					e.Use();
				}
				else if (e.button == 0)
				{
					if (!clickedNode.screenRect.Contains (mousePos))
					{ // Left click at node edges -> Check for clicked connections to edit
						NodeOutput nodeOutput = clickedNode.GetOutputAtPos (mousePos);
						if (nodeOutput != null)
						{ // Output Node -> New Connection drawn from this
							connectOutput = nodeOutput;
							e.Use();
						}
						else 
						{ // no output clicked, check input
							NodeInput nodeInput = clickedNode.GetInputAtPos (mousePos);
							if (nodeInput != null && nodeInput.connection != null)
							{ // Input node -> Loose and edit Connection
								connectOutput = nodeInput.connection;
								nodeInput.connection.connections.Remove (nodeInput);
								nodeInput.connection = null;
								RecalculateFrom (clickedNode);
								e.Use();
							}
						}
					}
				}
			}
			else if (canvasWindowRect.Contains (mousePos))
			{ // A click on the empty canvas
				if (e.button == 2 || e.button == 0)
				{ // Left/Middle Click -> Start scrolling
					panWindow = true;
					e.delta = Vector2.zero;
				}
				else if (e.button == 1) 
				{ // Right click -> Editor Context Click
					GenericMenu menu = new GenericMenu ();
					
					menu.AddItem (new GUIContent ("Add Input Node"), false, ContextCallback, InputNode.ID);
					menu.AddItem (new GUIContent ("Add Display Node"), false, ContextCallback, DisplayNode.ID);
					menu.AddItem (new GUIContent ("Add Calculation Node"), false, ContextCallback, CalcNode.ID);
					menu.AddSeparator ("");
					
					//menu.AddItem(new GUIContent("Add Example Node"), false, ContextCallback, "exampleNode");
					
					menu.ShowAsContext ();
					e.Use();
				}
			}

			break;

		case EventType.MouseUp:

			if (connectOutput != null) 
			{ // Apply a connection if theres a clicked input
				if (clickedNode != null && !clickedNode.Outputs.Contains (connectOutput)) 
				{ // If an input was clicked, it'll will now be connected
					NodeInput clickedInput = clickedNode.GetInputAtPos (mousePos);
					if (Node.CanApplyConnection (connectOutput, clickedInput)) 
					{ // If it can connect (type is equals, it does not cause recursion, ...)
						Node.ApplyConnection (connectOutput, clickedInput);
					}
				}
				e.Use();
			}

			connectOutput = null;
			dragNode = false;
			panWindow = false;

			break;

		case EventType.ScrollWheel:

			nodeCanvas.zoom = Mathf.Min (2.0f, Mathf.Max (0.6f, nodeCanvas.zoom + e.delta.y / 15));
			Repaint ();
			
			break;

		case EventType.KeyDown:

			// TODO: Node Editor: Shortcuts
			if (e.keyCode == KeyCode.N) // Start Navigating (curve to origin / active Node)
				navigate = true;

			if (e.keyCode == KeyCode.LeftControl && activeNode != null) // Snap
				activeNode.rect.position = new Vector2 (Mathf.RoundToInt ((activeNode.rect.position.x - nodeCanvas.panOffset.x) / 10) * 10 + nodeCanvas.panOffset.x, 
					                                    Mathf.RoundToInt ((activeNode.rect.position.y - nodeCanvas.panOffset.y) / 10) * 10 + nodeCanvas.panOffset.y);

			Repaint ();

			break;

		case EventType.KeyUp:

			if (e.keyCode == KeyCode.N) // Stop Navigating
				navigate = false;

			Repaint ();

			break;

		}

		if (panWindow) 
		{ // Scroll everything with the current mouse delta
			nodeCanvas.panOffset += e.delta / 2 * nodeCanvas.zoom;
			for (int nodeCnt = 0; nodeCnt < nodeCanvas.nodes.Count; nodeCnt++) 
				nodeCanvas.nodes [nodeCnt].rect.position += e.delta / 2 * nodeCanvas.zoom;
			Repaint ();
		}

		if (dragNode && activeNode != null && GUIUtility.hotControl == 0) 
		{ // Drag the active node with the current mouse delta
			activeNode.rect.position += e.delta / 2 * nodeCanvas.zoom;
			Repaint ();
		}
	}

	/// <summary>
	/// Proccesses late events. Called after GUI Functions, used when they have higher priority in focus
	/// </summary>
	private void LateEvents () 
	{
		Event e = Event.current;

		if (e.type == EventType.MouseDown && e.button == 0 && 
		    activeNode != null && activeNode.screenRect.Contains (mousePos))
		{ // Left click inside node -> Drag Node
			// Because of hotControl we have to put it after the GUI Functions
			if (GUIUtility.hotControl == 0)
			{ // We didn't clicked on GUI module, so we'll start drag the node
				dragNode = true;
				e.delta = Vector2.zero; // Because this is the delta from when it was last checked, we have to reset it
			}
		}
	}
	
	#endregion
	
	#region Calculation

	// A list of Nodes from which calculation originates -> Call StartCalculation
	public static List<Node> workList;

	/// <summary>
	/// Recalculate from every Input Node.
	/// Usually does not need to be called at all, the smart calculation system is doing the job just fine
	/// </summary>
	public void RecalculateAll () 
	{
		workList = new List<Node> ();
		for (int nodeCnt = 0; nodeCnt < nodeCanvas.nodes.Count; nodeCnt++) 
		{
			if (nodeCanvas.nodes [nodeCnt].Inputs.Count == 0) 
			{ // Add all Inputs
				ClearCalculation (nodeCanvas.nodes [nodeCnt]);
				workList.Add (nodeCanvas.nodes [nodeCnt]);
			}
		}
		StartCalculation ();
	}

	/// <summary>
	/// Recalculate from this node. 
	/// Usually does not need to be called manually
	/// </summary>
	public void RecalculateFrom (Node node) 
	{
		ClearCalculation (node);
		workList = new List<Node> { node };
		StartCalculation ();
	}

	/// <summary>
	/// Iterates through the worklist and calculates everything, including children
	/// </summary>
	public void StartCalculation () 
	{
		// this blocks iterates through the worklist and starts calculating
		// if a node returns false state it stops and adds the node to the worklist
		// later on, this worklist is reworked
		bool limitReached = false;
		for (int roundCnt = 0; !limitReached; roundCnt++)
		{ // Runs until every node possible is calculated
			limitReached = true;
			for (int workCnt = 0; workCnt < workList.Count; workCnt++) 
			{
				Node node = workList [workCnt];
				if (ContinueCalculation (node))
					limitReached = false;
			}
		}
	}

	/// <summary>
	/// Recursive function which continues calculation on this node and all the child nodes
	/// Usually does not need to be called manually
	/// Returns success/failure on this node only
	/// </summary>
	private bool ContinueCalculation (Node node) 
	{
		if (descendantsCalculated (node) && node.Calculate ())
		{ // finished Calculating, continue with the children
			for (int outCnt = 0; outCnt < node.Outputs.Count; outCnt++)
			{
				NodeOutput output = node.Outputs [outCnt];
				for (int conCnt = 0; conCnt < output.connections.Count; conCnt++)
					ContinueCalculation (output.connections [conCnt].body);
			}
			workList.Remove (node);
			node.calculated = true;
			//Debug.Log ("Calculated " + node.name + (node.Outputs.Count != 0? ("; First Ouput: " + node.Outputs [0].value + " !") : "!"));
			return true;
		}
		else if (!workList.Contains (node)) 
		{ // failed to calculate, add it to check later
			//Debug.Log ("Failed to calculate " + node.name + (node.Outputs.Count != 0? ("; First Ouput: " + node.Outputs [0].value + " !") : "!"));
			workList.Add (node);
		}
		return false;
	}

	/// <summary>
	/// A recursive function to clear all calculations depending on this node.
	/// Usually does not need to be called manually
	/// </summary>
	private void ClearCalculation (Node node) 
	{
		node.calculated = false;
		for (int outCnt = 0; outCnt < node.Outputs.Count; outCnt++)
		{
			NodeOutput output = node.Outputs [outCnt];
			output.value = null;
			for (int conCnt = 0; conCnt < output.connections.Count; conCnt++)
				ClearCalculation (output.connections [conCnt].body);
		}
	}

	/// <summary>
	/// Returns whether every node this node depends on, have been calculated
	/// </summary>
	public bool descendantsCalculated (Node node) 
	{
		for (int cnt = 0; cnt < node.Inputs.Count; cnt++) 
		{
			if (node.Inputs [cnt].connection != null && !node.Inputs [cnt].connection.body.calculated)
				return false;
		}
		return true;
	}

	#endregion

	#region Save/Load
	
	/// <summary>
	/// Saves the current node canvas as a new asset
	/// </summary>
	public void SaveNodeCanvas (string path) 
	{
		if (String.IsNullOrEmpty (path))
			return;
		string existingPath = AssetDatabase.GetAssetPath (nodeCanvas);
		if (!String.IsNullOrEmpty (existingPath))
		{
			if (existingPath != path) 
			{
				AssetDatabase.CopyAsset (existingPath, path);
				LoadNodeCanvas (path);
			}
			return;
		}
		AssetDatabase.CreateAsset (nodeCanvas, path);
		for (int nodeCnt = 0; nodeCnt < nodeCanvas.nodes.Count; nodeCnt++) 
		{ // Add every node and every of it's inputs/outputs into the file. 
			// Results in a big mess but there's no other way
			Node node = nodeCanvas.nodes [nodeCnt];
			AssetDatabase.AddObjectToAsset (node, nodeCanvas);
			for (int inCnt = 0; inCnt < node.Inputs.Count; inCnt++) 
				AssetDatabase.AddObjectToAsset (node.Inputs [inCnt], node);
			for (int outCnt = 0; outCnt < node.Outputs.Count; outCnt++) 
				AssetDatabase.AddObjectToAsset (node.Outputs [outCnt], node);
		}
		AssetDatabase.SaveAssets ();
		AssetDatabase.Refresh ();
		Repaint ();
	}

	/// <summary>
	/// Loads the a node canvas from an asset
	/// </summary>
	public void LoadNodeCanvas (string path) 
	{
		if (String.IsNullOrEmpty (path))
			return;
		Object[] objects = AssetDatabase.LoadAllAssetsAtPath (path);
		if (objects.Length == 0) 
			return;
		Node_Canvas_Object newNodeCanvas = null;
		
		for (int cnt = 0; cnt < objects.Length; cnt++) 
		{ // We only have to search for the Node Canvas itself in the mess, because it still hold references to all of it's nodes and their connections
			object obj = objects [cnt];
			if (obj.GetType () == typeof (Node_Canvas_Object)) 
				newNodeCanvas = obj as Node_Canvas_Object;
		}
		if (newNodeCanvas == null)
			return;
		nodeCanvas = newNodeCanvas;

		string[] folders = path.Split (new char[] {'/'}, StringSplitOptions.None);
		openedCanvas = folders [folders.Length-1];
		openedCanvasPath = path;
		RecalculateAll ();

		Repaint ();
		AssetDatabase.Refresh ();
	}

	/// <summary>
	/// Creates and opens a new empty node canvas
	/// </summary>
	public void NewNodeCanvas () 
	{
		nodeCanvas = CreateInstance<Node_Canvas_Object> ();
		nodeCanvas.nodes = new List<Node> ();
		openedCanvas = "New Canvas";
		openedCanvasPath = "";
	}
	
	#endregion
}
