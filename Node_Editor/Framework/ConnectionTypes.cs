using UnityEngine;
using System;
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
		private static Type NullType { get { return typeof(ConnectionTypes); } }
		
		// Static consistent information about types
		private static Dictionary<string, TypeData> types;

		/// <summary>
		/// Gets the Type the specified identifier representates or, if not declared and checked, creates a new type data for the passed type
		/// </summary>
		public static Type GetType (string typeName, bool createIfNotDeclared)
		{
			TypeData data = GetTypeData (typeName, createIfNotDeclared);
			return data != null? data.Type : NullType;
		}

		/// <summary>
		/// Gets the type data with the specified identifier or, if not declared and checked, creates a new one when a valid type name with namespace is passed
		/// </summary>
		public static TypeData GetTypeData (string typeName, bool createIfNotDeclared)
		{
			if (types == null || types.Count == 0)
				NodeEditor.ReInit (false);
			TypeData typeData;
			if (!types.TryGetValue (typeName, out typeData))
			{
				if (createIfNotDeclared) 
				{
					Type type = Type.GetType (typeName);
					if (type == null)
					{
						typeData = types.First ().Value;
						Debug.LogError ("No TypeData defined for: " + typeName + " and type could not be found either!");
					}
					else 
					{
						typeData = new TypeData (type);
						types.Add (typeName, typeData);
					}
				}
				else 
				{
					typeData = types.First ().Value;
					Debug.LogError ("No TypeData defined for: " + typeName + "!");
				}
			}
			return typeData;
		}

		/// <summary>
		/// Gets the type data for the specified type or, if not declared and checked, creates a new one for that type
		/// </summary>
		public static TypeData GetTypeData (Type type, bool createIfNotDeclared)
		{
			if (types == null || types.Count == 0)
				NodeEditor.ReInit (false);
			TypeData typeData = types.Values.First ((TypeData tData) => tData.Type == type);
			if (typeData == null)
			{
				if (createIfNotDeclared)
				{
					typeData = new TypeData (type);
					types.Add (type.FullName, typeData);
				}
				else 
				{
					typeData = types.First ().Value;
					Debug.LogError ("No TypeData defined for: " + type.FullName + "!");
				}
			}
			return typeData;
		}
		
		/// <summary>
		/// Fetches every Type Declaration in the script assemblies and the executing one, if the NodeEditor is packed into a .dll
		/// </summary>
		internal static void FetchTypes () 
		{
			types = new Dictionary<string, TypeData> { { "None", new TypeData () } };

			IEnumerable<Assembly> scriptAssemblies = AppDomain.CurrentDomain.GetAssemblies ().Where ((Assembly assembly) => assembly.FullName.Contains ("Assembly"));
			foreach (Assembly assembly in scriptAssemblies) 
			{ // Iterate through each script assembly
				IEnumerable<Type> typeDeclarations = assembly.GetTypes ().Where (T => T.IsClass && !T.IsAbstract && T.GetInterface (typeof (IConnectionTypeDeclaration).FullName) != null);
				foreach (Type type in typeDeclarations) 
				{ // get all type declarations and create a typeData for them
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
		public Type Type { get; private set; }
		public Color Color { get; private set; }
		public Texture2D InKnobTex { get; private set; }
		public Texture2D OutKnobTex { get; private set; }

		internal TypeData (IConnectionTypeDeclaration typeDecl) 
		{
			declaration = typeDecl;
			Type = declaration.Type;
			Color = declaration.Color;

			InKnobTex = ResourceManager.GetTintedTexture (declaration.InKnobTex, Color);
			OutKnobTex = ResourceManager.GetTintedTexture (declaration.OutKnobTex, Color);

			if (InKnobTex == null || InKnobTex == null)
				throw new UnityException ("Invalid textures for default typeData " + declaration.Identifier + "!");
		}

		internal TypeData (Type type) 
		{
			declaration = null;
			Type = type;
			Color = Color.white;//(float)type.GetHashCode() / (int.MaxValue/3);

			InKnobTex = ResourceManager.GetTintedTexture ("Textures/In_Knob.png", Color);
			OutKnobTex = ResourceManager.GetTintedTexture ("Textures/Out_Knob.png", Color);

			if (InKnobTex == null || InKnobTex == null)
				throw new UnityException ("Invalid textures for default typeData " + type.ToString () + "!");
		}

		internal TypeData () 
		{
			declaration = null;
			Type = typeof(object);
			Color = Color.white;
			InKnobTex = ResourceManager.LoadTexture ("Textures/In_Knob.png");
			OutKnobTex = ResourceManager.LoadTexture ("Textures/Out_Knob.png");

			if (InKnobTex == null || InKnobTex == null)
				throw new UnityException ("Invalid textures for default typeData!");
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

	// TODO: Node Editor: Built-In Connection Types
	public class FloatType : IConnectionTypeDeclaration 
	{
		public string Identifier { get { return "Float"; } }
		public Type Type { get { return typeof(float); } }
		public Color Color { get { return Color.cyan; } }
		public string InKnobTex { get { return "Textures/In_Knob.png"; } }
		public string OutKnobTex { get { return "Textures/Out_Knob.png"; } }
	}


}