using System.IO;
using UnityEngine;
using NodeEditorFramework.Utilities;

namespace NodeEditorFramework.IO
{
	/// <summary>
	/// Base class of an arbitrary Import/Export format based directly on the NodeCanvas
	/// </summary>
	public abstract class ImportExportFormat
	{
		/// <summary>
		/// Identifier for this format, must be unique (e.g. 'XML')
		/// </summary>
		public abstract string FormatIdentifier { get; }

		/// <summary>
		/// Optional format description (e.g. 'Legacy', shown as 'XML (Legacy)')
		/// </summary>
		public virtual string FormatDescription { get { return ""; } }

		/// <summary>
		/// Optional extension for this format if saved as a file, e.g. 'xml', default equals FormatIdentifier
		/// </summary>
		public virtual string FormatExtension { get { return FormatIdentifier; } }

		/// <summary>
		/// Returns whether the location selection needs to be performed through a GUI (e.g. for a custom database access)
		/// If true, the Import-/ExportLocationArgsGUI functions are called, else Import-/ExportLocationArgsSelection
		/// </summary>
		public virtual bool RequiresLocationGUI { get {
#if UNITY_EDITOR
				return false; // In the editor, use file browser seletion
#else
				return true; // At runtime, use GUI to select a file in a fixed folder
#endif
			}
		}

		/// <summary>
		/// Folder for runtime IO operations relative to the game folder.
		/// </summary>
		public virtual string RuntimeIOPath { get { return "Files/NodeEditor/"; } }

#if !UNITY_EDITOR
		private string fileSelection = "";
		private Rect fileSelectionMenuRect;
#endif

		/// <summary>
		/// Called only if RequiresLocationGUI is true.
		/// Displays GUI filling in locationArgs with the information necessary to locate the import operation.
		/// Override along with RequiresLocationGUI for custom database access.
		/// Return true or false to perform or cancel the import operation.
		/// </summary>
		public virtual bool? ImportLocationArgsGUI (ref object[] locationArgs)
		{
#if UNITY_EDITOR
			return ImportLocationArgsSelection (out locationArgs);
#else
			GUILayout.Label("Import canvas from " + FormatIdentifier);
			GUILayout.BeginHorizontal();
			GUILayout.Label(RuntimeIOPath, GUILayout.ExpandWidth(false));
			if (GUILayout.Button(string.IsNullOrEmpty(fileSelection)? "Select..." : fileSelection + "." + FormatExtension, GUILayout.ExpandWidth(true)))
			{
				// Find save files
				DirectoryInfo dir = Directory.CreateDirectory(RuntimeIOPath);
				FileInfo[] files = dir.GetFiles("*." + FormatExtension);
				// Fill save file selection menu
				GenericMenu fileSelectionMenu = new GenericMenu(false);
				foreach (FileInfo file in files)
					fileSelectionMenu.AddItem(new GUIContent(file.Name), false, () => fileSelection = Path.GetFileNameWithoutExtension(file.Name));
				fileSelectionMenu.DropDown(fileSelectionMenuRect);
			}
			if (Event.current.type == EventType.Repaint)
			{
				Rect popupPos = GUILayoutUtility.GetLastRect();
				fileSelectionMenuRect = new Rect(popupPos.x + 2, popupPos.yMax + 2, popupPos.width - 4, 0);
			}
			GUILayout.EndHorizontal();

			// Finish operation buttons
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Cancel"))
				return false;
			if (GUILayout.Button("Import"))
			{
				if (string.IsNullOrEmpty(fileSelection) || !File.Exists(RuntimeIOPath + fileSelection + "." + FormatExtension))
					return false;
				fileSelection = Path.GetFileNameWithoutExtension(fileSelection);
				locationArgs = new object[] { RuntimeIOPath + fileSelection + "." + FormatExtension };
				return true;
			}
			GUILayout.EndHorizontal();

			return null;
#endif
		}

		/// <summary>
		/// Called only if RequiresLocationGUI is false.
		/// Returns the information necessary to locate the import operation.
		/// By default it lets the user select a path as string[1].
		/// </summary>
		public virtual bool ImportLocationArgsSelection (out object[] locationArgs)
		{
			string path = null;
#if UNITY_EDITOR
			path = UnityEditor.EditorUtility.OpenFilePanel(
					"Import " + FormatIdentifier + (!string.IsNullOrEmpty (FormatDescription)? (" (" + FormatDescription + ")") : ""), 
					"Assets", FormatExtension.ToLower ());
#endif
			locationArgs = new object[] { path };
			return !string.IsNullOrEmpty (path);
		}

		/// <summary>
		/// Called only if RequiresLocationGUI is true.
		/// Displays GUI filling in locationArgs with the information necessary to locate the export operation.
		/// Override along with RequiresLocationGUI for custom database access.
		/// Return true or false to perform or cancel the export operation.
		/// </summary>
		public virtual bool? ExportLocationArgsGUI (string canvasName, ref object[] locationArgs)
		{
#if UNITY_EDITOR
			return ExportLocationArgsSelection(canvasName, out locationArgs);
#else
			GUILayout.Label("Export canvas to " + FormatIdentifier);

			// File save field
			GUILayout.BeginHorizontal();
			GUILayout.Label(RuntimeIOPath, GUILayout.ExpandWidth(false));
			fileSelection = GUILayout.TextField(fileSelection, GUILayout.ExpandWidth(true));
			GUILayout.Label("." + FormatExtension, GUILayout.ExpandWidth (false));
			GUILayout.EndHorizontal();

			// Finish operation buttons
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Cancel"))
				return false;
			if (GUILayout.Button("Export"))
			{
				if (string.IsNullOrEmpty(fileSelection))
					return false;
				fileSelection = Path.GetFileNameWithoutExtension(fileSelection);
				locationArgs = new object[] { RuntimeIOPath + fileSelection + "." + FormatExtension };
				return true;
			}
			GUILayout.EndHorizontal();

			return null;
#endif
		}

		/// <summary>
		/// Called only if RequiresLocationGUI is false.
		/// Returns the information necessary to locate the export operation.
		/// By default it lets the user select a path as string[1].
		/// </summary>
		public virtual bool ExportLocationArgsSelection (string canvasName, out object[] locationArgs)
		{
			string path = null;
#if UNITY_EDITOR
			path = UnityEditor.EditorUtility.SaveFilePanel(
				"Export " + FormatIdentifier + (!string.IsNullOrEmpty (FormatDescription)? (" (" + FormatDescription + ")") : ""), 
				"Assets", canvasName, FormatExtension.ToLower ());
#endif
			locationArgs = new object[] { path };
			return !string.IsNullOrEmpty (path);
		}

		/// <summary>
		/// Imports the canvas at the location specified in the args (usually string[1] containing the path) and returns it's simplified canvas data
		/// </summary>
		public abstract NodeCanvas Import (params object[] locationArgs);

		/// <summary>
		/// Exports the given simplified canvas data to the location specified in the args (usually string[1] containing the path)
		/// </summary>
		public abstract void Export (NodeCanvas canvas, params object[] locationArgs);
	}

	/// <summary>
	/// Base class of an arbitrary Import/Export format based on a simple structural data best for most formats
	/// </summary>
	public abstract class StructuredImportExportFormat : ImportExportFormat
	{
		public override NodeCanvas Import (params object[] locationArgs) 
		{
			CanvasData data = ImportData (locationArgs);
			if (data == null)
				return null;
			return ImportExportManager.ConvertToNodeCanvas (data);
		}

		public override void Export (NodeCanvas canvas, params object[] locationArgs)
		{
			CanvasData data = ImportExportManager.ConvertToCanvasData (canvas);
			ExportData (data, locationArgs);
		}

		/// <summary>
		/// Imports the canvas at the location specified in the args (usually string[1] containing the path) and returns it's simplified canvas data
		/// </summary>
		public abstract CanvasData ImportData (params object[] locationArgs);

		/// <summary>
		/// Exports the given simplified canvas data to the location specified in the args (usually string[1] containing the path)
		/// </summary>
		public abstract void ExportData (CanvasData data, params object[] locationArgs);
	}
}