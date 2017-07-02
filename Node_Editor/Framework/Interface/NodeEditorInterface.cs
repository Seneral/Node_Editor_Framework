using System;

using UnityEngine;

using NodeEditorFramework;
using NodeEditorFramework.IO;

using GenericMenu = NodeEditorFramework.Utilities.GenericMenu;

namespace NodeEditorFramework.Standard
{
	public class NodeEditorInterface
	{
		public NodeEditorUserCache canvasCache;
		public Action<GUIContent> ShowNotificationAction;

		// GUI
		public string sceneCanvasName = "";
		public float toolbarHeight = 17;

		// Modal Panel
		public bool showModalPanel;
		public Rect modalPanelRect = new Rect(20, 50, 250, 70);
		public Action modalPanelContent;

		// IO Format modal panel
		private ImportExportFormat IOFormat;
		private object[] IOLocationArgs;
		private delegate bool? DefExportLocationGUI(string canvasName, ref object[] locationArgs);
		private delegate bool? DefImportLocationGUI(ref object[] locationArgs);
		private DefImportLocationGUI ImportLocationGUI;
		private DefExportLocationGUI ExportLocationGUI;

		public void ShowNotification(GUIContent message)
		{
			if (ShowNotificationAction != null)
				ShowNotificationAction(message);
		}

#region GUI

		public void DrawToolbarGUI(float width)
		{
			GUILayout.BeginHorizontal(NodeEditorGUI.toolbar, GUILayout.Width (width));
			float curToolbarHeight = 0;

			if (GUILayout.Button("File", NodeEditorGUI.toolbarDropdown, GUILayout.Width(50)))
			{
				GenericMenu menu = new GenericMenu(!Application.isPlaying);

				// New Canvas filled with canvas types
				NodeCanvasManager.FillCanvasTypeMenu(ref menu, NewNodeCanvas, "New Canvas/");
				menu.AddSeparator("");

				// Load / Save
#if UNITY_EDITOR
				menu.AddItem(new GUIContent("Load Canvas"), false, LoadCanvas);
				menu.AddItem(new GUIContent("Reload Canvas"), false, ReloadCanvas);
				menu.AddSeparator("");
				menu.AddItem(new GUIContent("Save Canvas"), false, SaveCanvas);
				menu.AddItem(new GUIContent("Save Canvas As"), false, SaveCanvasAs);
				menu.AddSeparator("");
#endif

				// Import / Export filled with import/export types
				ImportExportManager.FillImportFormatMenu(ref menu, ImportCanvasCallback, "Import/");
				ImportExportManager.FillExportFormatMenu(ref menu, ExportCanvasCallback, "Export/");
				menu.AddSeparator("");

				// Scene Saving
				string[] sceneSaves = NodeEditorSaveManager.GetSceneSaves();
				if (sceneSaves.Length <= 0) // Display disabled item
					menu.AddItem(new GUIContent("Load Canvas from Scene"), false, null);
				else foreach (string sceneSave in sceneSaves) // Display scene saves to load
						menu.AddItem(new GUIContent("Load Canvas from Scene/" + sceneSave), false, LoadSceneCanvasCallback, sceneSave);
				menu.AddItem(new GUIContent("Save Canvas to Scene"), false, SaveSceneCanvasCallback);

				// Show dropdown
				menu.Show(new Vector2(5, toolbarHeight));
			}
			curToolbarHeight = Mathf.Max(curToolbarHeight, GUILayoutUtility.GetLastRect().yMax);

			GUILayout.Space(10);
			GUILayout.FlexibleSpace();

			GUILayout.Label(new GUIContent("" + canvasCache.nodeCanvas.saveName + " (" + (canvasCache.nodeCanvas.livesInScene ? "Scene Save" : "Asset Save") + ")", 
											"Opened Canvas path: " + canvasCache.nodeCanvas.savePath), NodeEditorGUI.toolbarLabel);
			GUILayout.Label("Type: " + canvasCache.typeData.DisplayString, NodeEditorGUI.toolbarLabel);
			curToolbarHeight = Mathf.Max(curToolbarHeight, GUILayoutUtility.GetLastRect().yMax);

			GUI.backgroundColor = new Color(1, 0.3f, 0.3f, 1);
			if (GUILayout.Button("Force Re-init", NodeEditorGUI.toolbarButton, GUILayout.Width(100)))
			{
				NodeEditor.ReInit(true);
				canvasCache.nodeCanvas.Validate();
			}
#if !UNITY_EDITOR
			GUILayout.Space(5);
			if (GUILayout.Button("Quit", NodeEditorGUI.toolbarButton, GUILayout.Width(100)))
				Application.Quit ();
#endif
			curToolbarHeight = Mathf.Max(curToolbarHeight, GUILayoutUtility.GetLastRect().yMax);
			GUI.backgroundColor = Color.white;

			GUILayout.EndHorizontal();
			if (Event.current.type == EventType.Repaint)
				toolbarHeight = curToolbarHeight;
		}

		private void SaveSceneCanvasPanel()
		{
			GUILayout.Label("Save Canvas To Scene");

			GUILayout.BeginHorizontal();
			sceneCanvasName = GUILayout.TextField(sceneCanvasName, GUILayout.ExpandWidth(true));
			bool overwrite = NodeEditorSaveManager.HasSceneSave(sceneCanvasName);
			if (overwrite)
				GUILayout.Label(new GUIContent("!!!", "A canvas with the specified name already exists. It will be overwritten!"), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Cancel"))
				showModalPanel = false;
			if (GUILayout.Button(new GUIContent(overwrite ? "Overwrite" : "Save", "Save the canvas to the Scene")))
			{
				showModalPanel = false;
				if (!string.IsNullOrEmpty (sceneCanvasName))
					canvasCache.SaveSceneNodeCanvas(sceneCanvasName);
			}
			GUILayout.EndHorizontal();
		}

		public void DrawModalPanel()
		{
			if (showModalPanel)
			{
				if (modalPanelContent == null)
					return;
				GUILayout.BeginArea(modalPanelRect, NodeEditorGUI.nodeBox);
				modalPanelContent.Invoke();
				GUILayout.EndArea();
			}
		}

#endregion

#region Menu Callbacks

		private void NewNodeCanvas(Type canvasType)
		{
			canvasCache.NewNodeCanvas(canvasType);
		}

#if UNITY_EDITOR
		private void LoadCanvas()
		{
			string path = UnityEditor.EditorUtility.OpenFilePanel("Load Node Canvas", NodeEditor.editorPath + "Resources/Saves/", "asset");
			if (!path.Contains(Application.dataPath))
			{
				if (!string.IsNullOrEmpty(path))
					ShowNotification(new GUIContent("You should select an asset inside your project folder!"));
			}
			else
				canvasCache.LoadNodeCanvas(path);
		}

		private void ReloadCanvas()
		{
			string path = canvasCache.nodeCanvas.savePath;
			if (!string.IsNullOrEmpty(path))
			{
				if (path.StartsWith("SCENE/"))
					canvasCache.LoadSceneNodeCanvas(path.Substring(6));
				else
					canvasCache.LoadNodeCanvas(path);
				ShowNotification(new GUIContent("Canvas Reloaded!"));
			}
			else
				ShowNotification(new GUIContent("Cannot reload canvas as it has not been saved yet!"));
		}

		private void SaveCanvas()
		{
			string path = canvasCache.nodeCanvas.savePath;
			if (!string.IsNullOrEmpty(path))
			{
				if (path.StartsWith("SCENE/"))
					canvasCache.SaveSceneNodeCanvas(path.Substring(6));
				else
					canvasCache.SaveNodeCanvas(path);
				ShowNotification(new GUIContent("Canvas Saved!"));
			}
			else
				ShowNotification(new GUIContent("No save location found. Use 'Save As'!"));
		}

		private void SaveCanvasAs()
		{
			string panelPath = NodeEditor.editorPath + "Resources/Saves/";
			if (canvasCache.nodeCanvas != null && !string.IsNullOrEmpty(canvasCache.nodeCanvas.savePath))
				panelPath = canvasCache.nodeCanvas.savePath;
			string path = UnityEditor.EditorUtility.SaveFilePanelInProject("Save Node Canvas", "Node Canvas", "asset", "", panelPath);
			if (!string.IsNullOrEmpty(path))
				canvasCache.SaveNodeCanvas(path);
		}
#endif

		private void LoadSceneCanvasCallback(object canvas)
		{
			canvasCache.LoadSceneNodeCanvas((string)canvas);
			sceneCanvasName = canvasCache.nodeCanvas.name;
		}

		private void SaveSceneCanvasCallback()
		{
			modalPanelContent = SaveSceneCanvasPanel;
			showModalPanel = true;
		}

		private void ImportCanvasCallback(string formatID)
		{
			IOFormat = ImportExportManager.ParseFormat(formatID);
			if (IOFormat.RequiresLocationGUI)
			{
				ImportLocationGUI = IOFormat.ImportLocationArgsGUI;
				modalPanelContent = ImportCanvasGUI;
				showModalPanel = true;
			}
			else if (IOFormat.ImportLocationArgsSelection(out IOLocationArgs))
				canvasCache.SetCanvas(ImportExportManager.ImportCanvas(IOFormat, IOLocationArgs));
		}

		private void ImportCanvasGUI()
		{
			if (ImportLocationGUI != null)
			{
				bool? state = ImportLocationGUI(ref IOLocationArgs);
				if (state == null)
					return;

				if (state == true)
					canvasCache.SetCanvas(ImportExportManager.ImportCanvas(IOFormat, IOLocationArgs));

				ImportLocationGUI = null;
				modalPanelContent = null;
				showModalPanel = false;
			}
			else
				showModalPanel = false;
		}

		private void ExportCanvasCallback(string formatID)
		{
			IOFormat = ImportExportManager.ParseFormat(formatID);
			if (IOFormat.RequiresLocationGUI)
			{
				ExportLocationGUI = IOFormat.ExportLocationArgsGUI;
				modalPanelContent = ExportCanvasGUI;
				showModalPanel = true;
			}
			else if (IOFormat.ExportLocationArgsSelection(canvasCache.nodeCanvas.saveName, out IOLocationArgs))
				ImportExportManager.ExportCanvas(canvasCache.nodeCanvas, IOFormat, IOLocationArgs);
		}

		private void ExportCanvasGUI()
		{
			if (ExportLocationGUI != null)
			{
				bool? state = ExportLocationGUI(canvasCache.nodeCanvas.saveName, ref IOLocationArgs);
				if (state == null)
					return;

				if (state == true)
					ImportExportManager.ExportCanvas(canvasCache.nodeCanvas, IOFormat, IOLocationArgs);

				ImportLocationGUI = null;
				modalPanelContent = null;
				showModalPanel = false;
			}
			else
				showModalPanel = false;
		}

#endregion
	}
}
