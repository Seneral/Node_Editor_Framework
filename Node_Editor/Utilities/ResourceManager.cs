using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace NodeEditorFramework.Utilities 
{
	public static class ResourceManager 
	{
		public static string ResourcePath;
		
		public static void Init (string resourcePath) 
		{
			ResourcePath = resourcePath;
		}
		
		/// <summary>
		/// Loads a resource in the resources folder in both the editor and at runtime
		/// </summary>
		public static T LoadResource<T> (string path) where T : UnityEngine.Object
		{
			T obj = null;
			if (!Application.isPlaying) 
			{
				#if UNITY_EDITOR
				string fullPath = System.IO.Path.Combine (ResourcePath, path);
				obj = UnityEditor.AssetDatabase.LoadAssetAtPath (fullPath, typeof (T)) as T;
				if (obj == null)
					Debug.LogError (string.Format ("ResourceManager: Resource not found at '{0}', did you install the plugin correctly?", fullPath));
				#endif
			}
			else
			{
				path = path.Split ('.') [0];
				obj = Resources.Load<T> (path);
				if (obj == null)
					Debug.LogError (string.Format ("ResourceManager: Resource not found at '{0}' in any Resource Folder!", path));
			}
			return obj;
		}
		
		private static List<MemoryTexture> loadedTextures = new List<MemoryTexture> ();
		
		/// <summary>
		/// Loads a texture in the resources folder in both the editor and at runtime
		/// </summary>
		public static Texture2D LoadTexture (string texPath)
		{
			if (String.IsNullOrEmpty (texPath))
				return null;
			var existingInd = loadedTextures.FindIndex (memTex => memTex.Path == texPath);
			if (existingInd != -1) 
			{
				if (loadedTextures[existingInd].Texture == null)
					loadedTextures.RemoveAt (existingInd);
				else
					return loadedTextures[existingInd].Texture;
			}
			//Debug.Log ("Loading " + texPath + " first time");
			
			Texture2D tex = null;
			
			#if UNITY_EDITOR
			{
				string fullPath = System.IO.Path.Combine(ResourcePath, texPath);
				tex = UnityEditor.AssetDatabase.LoadAssetAtPath(fullPath, typeof(Texture2D)) as Texture2D;
				if (tex == null)
					Debug.LogError(string.Format("ResourceManager: Texture not found at '{0}', did you install the plugin correctly?", fullPath));
			}
			#else
			{
				texPath = texPath.Split ('.') [0];
				tex = Resources.Load<Texture2D> (texPath);
				if (tex == null)
					Debug.LogError (string.Format ("ResourceManager: Texture not found at '{0}' in any Resource Folder!", texPath));
			}
			#endif

			loadedTextures.Add (new MemoryTexture (texPath, tex));
			return tex;
		}
		
		#region Texture Management
		
		public static Texture2D GetTintedTexture (string texPath, Color col) 
		{
		    string texMod = "Tint:" + col.ToString ();
			var tintedTexture = GetTexture (texPath, texMod);
			if (tintedTexture == null)
			{
				tintedTexture = LoadTexture (texPath);
				tintedTexture = RTEditorGUI.Tint (tintedTexture, col);
				AddTexture (texPath, tintedTexture, texMod); // Register texture for re-use
			}
			return tintedTexture;
		}
		
		/// <summary>
		/// Adds an additional texture into the manager memory with optional modifications
		/// </summary>
		public static void AddTexture (string texturePath, Texture2D texture, params string[] modifications)
		{
			if (texture == null)
				return;
			loadedTextures.Add (new MemoryTexture (texturePath, texture, modifications));
		}
		
		/// <summary>
		/// Whether the manager memory contains a texture with optional modifications
		/// </summary>
		public static MemoryTexture FindInMemory (Texture2D tex)
		{
			var existingInd = loadedTextures.FindIndex (memTex => memTex.Texture == tex);
			return existingInd != -1? loadedTextures[existingInd] : null;
		}
		
		/// <summary>
		/// Whether the manager memory contains a texture with optional modifications
		/// </summary>
		public static bool Contains (string texturePath, params string[] modifications)
		{
			int existingInd = loadedTextures.FindIndex ((memTex) => memTex.Path == texturePath);
			return existingInd != -1 && EqualModifications (loadedTextures[existingInd].Modifications, modifications);
		}
		
		/// <summary>
		/// Gets a texture already in manager memory with specified modifications (check with contains before!)
		/// </summary>
		public static MemoryTexture GetMemoryTexture (string texturePath, params string[] modifications)
		{
			var textures = loadedTextures.FindAll (memTex => memTex.Path == texturePath);
			if (textures == null || textures.Count == 0)
				return null;
			foreach (var tex in textures)
				if (EqualModifications (tex.Modifications, modifications))
					return tex;
			return null;
		}
		
		/// <summary>
		/// Gets a texture already in manager memory with specified modifications (check with contains before!)
		/// </summary>
		public static Texture2D GetTexture (string texturePath, params string[] modifications)
		{
			var memTex = GetMemoryTexture (texturePath, modifications);
			return memTex == null? null : memTex.Texture;
		}
		
		private static bool EqualModifications (string[] modsA, string[] modsB) 
		{
			return modsA.Length == modsB.Length && Array.TrueForAll (modsA, mod => modsB.Count (oMod => mod == oMod) == modsA.Count (oMod => mod == oMod));
		}
		
		public class MemoryTexture 
		{
			public string Path;
			public Texture2D Texture;
			public string[] Modifications;
			
			public MemoryTexture (string texPath, Texture2D tex, params string[] mods) 
			{
				Path = texPath;
				Texture = tex;
				Modifications = mods;
			}
		}
		
		#endregion
	}

}