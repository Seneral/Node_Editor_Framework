using UnityEngine;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using NodeEditorFramework.Utilities;

namespace NodeEditorFramework
{
	public enum ConnectionDrawMethod { Bezier, StraightLine }

	/// <summary>
	/// Handles fetching and storing of all ConnecionTypes
	/// </summary>
	public static class ConnectionTypes
	{
		private static Type NullType { get { return typeof(void); } }
		
		// Static consistent information about types
		private static Dictionary<string, TypeData> types;

		/// <summary>
		/// Gets the Type the specified type name representates or creates it if not defined
		/// </summary>
		public static Type GetType (string typeName)
		{
			return GetTypeData (typeName).Type ?? NullType;
		}

		/// <summary>
		/// Gets the type data for the specified type name or creates it if not defined
		/// </summary>
		public static TypeData GetTypeData (string typeName)
		{
			if (types == null || types.Count == 0)
				FetchTypes ();
			TypeData typeData;
			if (!types.TryGetValue (typeName, out typeData))
			{
				Type type = Type.GetType (typeName);
				if (type == null)
				{
					typeData = types.First ().Value;
					Debug.LogError ("No TypeData defined for: " + typeName + " and type could not be found either");
				}
				else 
				{
					typeData = types.Values.Count <= 0? null : types.Values.FirstOrDefault ((TypeData data) => data.isValid () && data.Type == type);
					if (typeData == null)
						types.Add (typeName, typeData = new TypeData (type));
				}
			}
			return typeData;
		}

		/// <summary>
		/// Gets the type data for the specified type or creates it if not defined
		/// </summary>
		public static TypeData GetTypeData (Type type)
		{
			if (types == null || types.Count == 0)
				FetchTypes ();
			TypeData typeData = types.Values.Count <= 0? null : types.Values.First ((TypeData data) => data.isValid () && data.Type == type);
			if (typeData == null)
				types.Add (type.Name, typeData = new TypeData (type));
			return typeData;
		}
		
		/// <summary>
		/// Fetches every Type Declaration in the script assemblies and the executing one, if the NodeEditor is packed into a .dll
		/// </summary>
		internal static void FetchTypes () 
		{
			types = new Dictionary<string, TypeData> { { "None", new TypeData (typeof(System.Object)) } };

			IEnumerable<Assembly> scriptAssemblies = AppDomain.CurrentDomain.GetAssemblies ().Where ((Assembly assembly) => assembly.FullName.Contains ("Assembly"));
			foreach (Assembly assembly in scriptAssemblies) 
			{
				foreach (Type type in assembly.GetTypes ().Where (T => T.IsClass && !T.IsAbstract && T.GetInterfaces ().Contains (typeof (IConnectionTypeDeclaration)))) 
				{
					IConnectionTypeDeclaration typeDecl = assembly.CreateInstance (type.FullName) as IConnectionTypeDeclaration;
					if (typeDecl == null)
						throw new UnityException ("Error with Type Declaration " + type.FullName);
					types.Add (typeDecl.Identifier, new TypeData (typeDecl));
				}
			}
		}
	}

	public class TypeData 
	{
		private IConnectionTypeDeclaration declaration;
		public string Identifier { get; private set; }
		public Type Type { get; private set; }
		public Color Color { get; private set; }
		public Texture2D InKnobTex { get; private set; }
		public Texture2D OutKnobTex { get; private set; }
		
		internal TypeData (IConnectionTypeDeclaration typeDecl) 
		{
			Identifier = typeDecl.Identifier;
			declaration = typeDecl;
			Type = declaration.Type;
			Color = declaration.Color;

			InKnobTex = ResourceManager.GetTintedTexture (declaration.InKnobTex, Color);
			OutKnobTex = ResourceManager.GetTintedTexture (declaration.OutKnobTex, Color);

			if (!isValid ())
				throw new DataMisalignedException ("Type Declaration " + typeDecl.Identifier + " contains invalid data!");
		}

		public TypeData (Type type) 
		{
			Identifier = type.Name;
			declaration = null;
			Type = type;

			// Generate consistent color for a type - using string because it delivers greater variety of colors than type hashcode
			int srcInt = (int)(type.AssemblyQualifiedName.GetHashCode ());
			UnityEngine.Random.InitState (srcInt);
			Color = UnityEngine.Random.ColorHSV (0, 1, 0.6f, 0.8f, 0.8f, 1.4f);

			InKnobTex = ResourceManager.GetTintedTexture ("Textures/In_Knob.png", Color);
			OutKnobTex = ResourceManager.GetTintedTexture ("Textures/Out_Knob.png", Color);
		}

		public bool isValid () 
		{
			return Type != null && InKnobTex != null && OutKnobTex != null;
		}
	}

	public interface IConnectionTypeDeclaration
	{
		string Identifier { get; }
		Type Type { get; }
		Color Color { get; }
		string InKnobTex { get; }
		string OutKnobTex { get; }
	}
}