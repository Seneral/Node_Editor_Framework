using System;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

public static class AudioUtils
{
	public static void PlayClip(AudioClip clip)
	{
		Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
		Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
		MethodInfo method = audioUtilClass.GetMethod("PlayClip", BindingFlags.Static | BindingFlags.Public, null, new System.Type[] { typeof(AudioClip) }, null );
		method.Invoke( null, new object[] { clip } );
	}
}
#endif