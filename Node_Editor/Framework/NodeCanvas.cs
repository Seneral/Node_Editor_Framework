namespace NodeEditorFramework
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using NodeEditorFramework.Standard;
	using UnityEngine;

	/// <summary>
	/// The Node Canvas base class.
	/// </summary>
	/// <remarks>
	/// All canvases should derive from this class.
	/// </remarks>
	public class NodeCanvas : ScriptableObject
	{
		public virtual string canvasName { get { return "DEFAULT"; } }

		public NodeCanvasTraversal Traversal;

		public NodeEditorState[] editorStates = new NodeEditorState[0];

		public string saveName;
		public string savePath;

		public bool livesInScene;

		public List<Node> nodes = new List<Node>();
		public List<NodeGroup> groups = new List<NodeGroup>();

		#region Constructors

		/// <summary>
		/// Initializes the <see cref="NodeCanvas"/>.
		/// </summary>
		/// <typeparam name="T">The <see cref="NodeCanvas"/> subclass's type.</typeparam>
		/// <returns>Returns the initialized <see cref="NodeCanvas"/>.</returns>
		public static T CreateCanvas<T>() where T : NodeCanvas
		{
			if (typeof(T) == typeof(NodeCanvas))
			{
				throw new Exception("Cannot create canvas of type 'NodeCanvas' as that is only the base class. Please specify a valid subclass!");
			}

			T canvas = CreateInstance<T>();
			canvas.name = canvas.saveName = "New " + canvas.canvasName;

			NodeEditor.BeginEditingCanvas(canvas);
			canvas.OnCreate();
			NodeEditor.EndEditingCanvas();

			return canvas;
		}

		/// <summary>
		/// Initializes the <see cref="NodeCanvas"/>.
		/// </summary>
		/// <param name="canvasType">The <see cref="NodeCanvas"/> subclass's type.</param>
		/// <returns>Returns the initialized <see cref="NodeCanvas"/>.</returns>
		public static NodeCanvas CreateCanvas(Type canvasType)
		{
			NodeCanvas canvas;

			if (canvasType != null && canvasType.IsSubclassOf(typeof(NodeCanvas)))
			{
				canvas = CreateInstance(canvasType) as NodeCanvas;
			}
			else
			{
				canvas = CreateInstance<CalculationCanvasType>();
			}

			canvas.name = canvas.saveName = "New " + canvas.canvasName;

			NodeEditor.BeginEditingCanvas(canvas);
			canvas.OnCreate();
			NodeEditor.EndEditingCanvas();

			return canvas;
		}

		#endregion

		#region Callbacks

		protected virtual void OnCreate() {}

		protected virtual void OnValidate() {}

		public virtual void OnBeforeSavingCanvas() {}

		public virtual bool CanAddNode(string nodeID)
		{
			return true;
		}

		#endregion

		#region Traversal

		public void TraverseAll()
		{
			if (Traversal != null)
				Traversal.TraverseAll();
		}

		public void OnNodeChange(Node node)
		{
			if (Traversal != null && node != null)
				Traversal.OnChange(node);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Validates the <see cref="NodeCanvas"/> by checking for any broken references.
		/// If there are any, cleans them.
		/// </summary>
		/// <param name="deepValidate">Whether or not should check each node individually.</param>
		public void Validate(bool deepValidate = false)
		{
			if (nodes == null)
			{
				Debug.LogWarning("The NodeCanvas '" + name + "' nodes were erased and set to null! Automatically fixed!");
				nodes = new List<Node>();
			}

			if (deepValidate)
			{
				// Checks for broken references in each group.
				for (int groupCnt = 0; groupCnt < groups.Count; groupCnt++)
				{
					NodeGroup group = groups[groupCnt];

					if (group == null)
					{
						Debug.LogWarning("The NodeCanvas '" + name + "' contained broken (null) group! Automatically fixed!");
						groups.RemoveAt(groupCnt);
						groupCnt--;
					}
				}

				// Checks for broken references in each node.
				for (int nodeCnt = 0; nodeCnt < nodes.Count; nodeCnt++)
				{
					Node node = nodes[nodeCnt];

					if (node == null)
					{
						Debug.LogWarning("The NodeCanvas '" + saveName + "' (" + name + ") contained broken (null) node! Automatically fixed!");
						nodes.RemoveAt(nodeCnt);
						nodeCnt--;
						continue;
					}

					// Checks for broken references in each knob of the node.
					for (int knobCnt = 0; knobCnt < node.nodeKnobs.Count; knobCnt++)
					{
						NodeKnob nodeKnob = node.nodeKnobs[knobCnt];

						if (nodeKnob == null)
						{
							Debug.LogWarning("The NodeCanvas '" + name + "' Node '" + node.name + "' contained broken (null) NodeKnobs! Automatically fixed!");
							nodes.RemoveAt(nodeCnt);
							nodeCnt--;
							break;

							//node.nodeKnobs.RemoveAt(knobCnt);
							//knobCnt--;
							//continue;
						}

						if (nodeKnob is NodeInput)
						{
							NodeInput input = nodeKnob as NodeInput;
							if (input.connection != null && input.connection.body == null)
							{
								// References broken node; Clear connection
								input.connection = null;
							}
							//for (int conCnt = 0; conCnt < (nodeKnob as NodeInput).connection.Count; conCnt++)
						}
						else if (nodeKnob is NodeOutput)
						{
							NodeOutput output = nodeKnob as NodeOutput;
							for (int conCnt = 0; conCnt < output.connections.Count; conCnt++)
							{
								NodeInput con = output.connections[conCnt];
								if (con == null || con.body == null)
								{
									// Broken connection; Clear connection
									output.connections.RemoveAt(conCnt);
									conCnt--;
								}
							}
						}
					}
				}

				// Checks for broken references in the editor states.
				if (editorStates == null)
				{
					Debug.LogWarning("NodeCanvas '" + name + "' editorStates were erased! Automatically fixed!");
					editorStates = new NodeEditorState[0];
				}

				editorStates = editorStates.Where(state => state != null).ToArray();

				for (int i = 0; i < editorStates.Length; i++)
				{
					NodeEditorState state = editorStates[i];
					if (!nodes.Contains(state.selectedNode))
					{
						state.selectedNode = null;
					}
				}
			}
			OnValidate();
		}

		public void UpdateSource(string path)
		{
			string newName;

			if (path.StartsWith("SCENE/"))
			{
				newName = path.Substring(6);
			}
			else
			{
				int nameStart = path.LastIndexOf('/') + 1;
				newName = path.Substring(nameStart, path.Length - nameStart - 6);
			}

			if (newName != "LastSession")
			{
				savePath = path;
				saveName = newName;
			}
		}

		#endregion
	}
}
