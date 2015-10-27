using UnityEngine;
using System.Collections.Generic;

public static class ResourceManager 
{
	public static string resourcePath;

	public static void Init (string _resourcePath) 
	{
		resourcePath = _resourcePath;
	}

	public static Dictionary<string, Texture2D> loadedTextures = new Dictionary<string, Texture2D> ();

	/// <summary>
	/// Loads a texture in the resources folder in both the editor and at runtime
	/// </summary>
	public static Texture2D LoadTexture (string texPath)
	{
		if (loadedTextures.ContainsKey (texPath))
			return loadedTextures[texPath];
		Texture2D tex;
		#if UNITY_EDITOR
		string fullPath = System.IO.Path.Combine (resourcePath, texPath);
		tex = UnityEditor.AssetDatabase.LoadAssetAtPath (fullPath, typeof (Texture2D)) as Texture2D;
		if (tex == null)
			Debug.LogError (string.Format ("ResourceManager: Texture not found at '{0}', did you install the plugin correctly?", fullPath));
		#else
		texPath = texPath.Split ('.') [0];
		tex = Resources.Load<Texture2D> (texPath);
		if (tex == null)
			Debug.LogError (string.Format ("ResourceManager: Texture not found at '{0}' in any Resource Folder!", texPath));
		#endif
		loadedTextures.Add (texPath, tex);
		return tex;
	}
	
	/// <summary>
	/// Loads a resource in the resources folder in both the editor and at runtime
	/// </summary>
	public static T LoadResource<T> (string path) where T : Object
	{
		#if UNITY_EDITOR
		string fullPath = System.IO.Path.Combine (resourcePath, path);
		T obj = UnityEditor.AssetDatabase.LoadAssetAtPath (fullPath, typeof (T)) as T;
		if (obj == null)
			Debug.LogError (string.Format ("ResourceManager: Resource not found at '{0}', did you install the plugin correctly?", fullPath));
		#else
		path = path.Split ('.') [0];
		T obj = Resources.Load<T> (path);
		if (obj == null)
			Debug.LogError (string.Format ("ResourceManager: Resource not found at '{0}' in any Resource Folder!", path));
		#endif
		return obj;
	}
}
