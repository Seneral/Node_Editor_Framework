#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
#if UNITY_5_3
using UnityEngine.SceneManagement;
#endif
using System;

namespace NodeEditorFramework
{
	[InitializeOnLoad]
	public static class EditorLoadingControl 
	{
#if UNITY_5_3
        private static Scene loadedScene;
#else
	    private static string loadedScene;
#endif

        private static bool serializationTest;
		private static bool playmodeSwitchToEdit;
		private static bool toggleLateEnteredPlaymode;

		public static Action BeforeEnteringPlayMode;
		public static Action JustEnteredPlayMode;
		public static Action LateEnteredPlayMode;
		public static Action BeforeLeavingPlayMode;
		public static Action JustLeftPlayMode;
		public static Action JustOpenedNewScene;

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
#if UNITY_5_3
            var currentSceneName = SceneManager.GetActiveScene ();
#else
		    string currentSceneName = Application.loadedLevelName;
#endif
            if (loadedScene != currentSceneName)
			{
				if (JustOpenedNewScene != null)
					JustOpenedNewScene.Invoke ();
				loadedScene = currentSceneName;
			}
		}

		// Handles just after switch (non-serialized values lost)
		private static void Update () 
		{
			if (toggleLateEnteredPlaymode)
			{
				toggleLateEnteredPlaymode = false;
				if (LateEnteredPlayMode != null)
					LateEnteredPlayMode.Invoke ();
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
					if (JustLeftPlayMode != null)
						JustLeftPlayMode.Invoke ();
					playmodeSwitchToEdit = false;
				}
				else 
				{ // Before Playmode
					//Debug.Log ("SAVE EDIT MODE Values before Play Mode!!");
					if (BeforeEnteringPlayMode != null)
						BeforeEnteringPlayMode.Invoke ();
				}
			}
			else
			{ // Play Mode
				if (serializationTest) 
				{ // Before Leaving Playmode
					//Debug.Log ("SAVE PLAY MODE Values before Edit Mode!!");
					if (BeforeLeavingPlayMode != null)
						BeforeLeavingPlayMode.Invoke ();
					playmodeSwitchToEdit = true;
				}
				else
				{ // After Entering Playmode
					//Debug.Log ("LOAD EDIT MODE Values in Play Mode!!");
					if (JustEnteredPlayMode != null)
						JustEnteredPlayMode.Invoke ();
					toggleLateEnteredPlaymode = true;
				}

			}
		}
	}
}
#endif