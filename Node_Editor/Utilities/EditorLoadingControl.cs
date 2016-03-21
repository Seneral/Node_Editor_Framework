#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
#if UNITY_5_3_OR_NEWER || UNITY_5_3
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
#endif
using System;

namespace NodeEditorFramework.Utilities
{
	[InitializeOnLoad]
	public static class EditorLoadingControl 
	{
#if UNITY_5_3_OR_NEWER || UNITY_5_3
		private static Scene loadedScene;
#else
		private static string loadedScene;
#endif

		private static bool serializationTest = false;
		private static bool playmodeSwitchToEdit = false;
		private static bool toggleLateEnteredPlaymode = false;

		public static Action beforeEnteringPlayMode;
		public static Action justEnteredPlayMode;
		public static Action lateEnteredPlayMode;
		public static Action beforeLeavingPlayMode;
		public static Action justLeftPlayMode;
		public static Action justOpenedNewScene;

		static EditorLoadingControl () 
		{
			EditorApplication.playmodeStateChanged -= PlaymodeStateChanged;
			EditorApplication.playmodeStateChanged += PlaymodeStateChanged;
			EditorApplication.update -= Update;
			EditorApplication.update += Update;
			EditorApplication.hierarchyWindowChanged -= OnHierarchyChange;
			EditorApplication.hierarchyWindowChanged += OnHierarchyChange;
		}

		private static void OnHierarchyChange () 
		{
#if UNITY_5_3_OR_NEWER || UNITY_5_3
			Scene currentScene = EditorSceneManager.GetActiveScene ();
#else
			string currentScene = Application.loadedLevelName;
#endif
			if (loadedScene != currentScene)
			{
				if (justOpenedNewScene != null)
					justOpenedNewScene.Invoke ();
				loadedScene = currentScene;
			}
		}

		// Handles just after switch (non-serialized values lost)
		private static void Update () 
		{
			if (toggleLateEnteredPlaymode)
			{
				toggleLateEnteredPlaymode = false;
				if (lateEnteredPlayMode != null)
					lateEnteredPlayMode.Invoke ();
			}
			serializationTest = true;
		}

		private static void PlaymodeStateChanged () 
		{
			//Debug.Log ("Playmode State Change! isPlaying: " + Application.isPlaying + "; Serialized: " + serializationTest);
			if (!Application.isPlaying)
			{ // Edit Mode
				if (playmodeSwitchToEdit)
				{ // After Playmode
					//Debug.Log ("LOAD PLAY MODE Values in Edit Mode!!");
					if (justLeftPlayMode != null)
						justLeftPlayMode.Invoke ();
					playmodeSwitchToEdit = false;
				}
				else 
				{ // Before Playmode
					//Debug.Log ("SAVE EDIT MODE Values before Play Mode!!");
					if (beforeEnteringPlayMode != null)
						beforeEnteringPlayMode.Invoke ();
				}
			}
			else
			{ // Play Mode
				if (serializationTest) 
				{ // Before Leaving Playmode
					//Debug.Log ("SAVE PLAY MODE Values before Edit Mode!!");
					if (beforeLeavingPlayMode != null)
						beforeLeavingPlayMode.Invoke ();
					playmodeSwitchToEdit = true;
				}
				else
				{ // After Entering Playmode
					//Debug.Log ("LOAD EDIT MODE Values in Play Mode!!");
					if (justEnteredPlayMode != null)
						justEnteredPlayMode.Invoke ();
					toggleLateEnteredPlaymode = true;
				}

			}
		}
	}
}
#endif